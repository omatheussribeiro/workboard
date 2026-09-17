import { HttpBackend, HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import type { Observable } from 'rxjs';
import { firstValueFrom, from, shareReplay } from 'rxjs';
import { RuntimeConfigService } from '../config/runtime-config';
import type { AuthResponse, AuthenticatedUser } from '../models';

const ACCESS_TOKEN_KEY = 'workboard.accessToken';
const REFRESH_TOKEN_KEY = 'workboard.refreshToken';
const USER_KEY = 'workboard.user';

/**
 * Holds the session and keeps the access token fresh.
 *
 * ## Where the tokens live
 *
 * In `localStorage`, which is a choice with a real downside worth stating: anything that achieves
 * script execution on this origin can read them. The alternative, an HttpOnly cookie, is immune to
 * that and unavailable here — the SPA and the API are separate origins, and a cross-site cookie now
 * needs third-party-cookie tolerance that current browsers no longer give.
 *
 * What is done about it instead: the API sets `Content-Security-Policy: default-src 'none'`, the
 * access token lives fifteen minutes, and refresh tokens rotate on every use so a stolen one is
 * detectable (see ADR-006). A same-origin deployment behind one reverse proxy would let this move to
 * cookies, and that is the upgrade path.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly config = inject(RuntimeConfigService);
  private readonly router = inject(Router);

  /** Refreshes bypass the interceptor chain: attaching an expired token to a refresh is circular. */
  private readonly rawHttp = new HttpClient(inject(HttpBackend));

  private readonly currentUser = signal<AuthenticatedUser | null>(
    readStored<AuthenticatedUser>(USER_KEY),
  );
  private readonly accessToken = signal<string | null>(readRaw(ACCESS_TOKEN_KEY));

  /**
   * The in-flight refresh, if any.
   *
   * Several requests usually fail with 401 at the same moment — a dashboard fires five calls at once.
   * Without this they would each start a refresh, and since refresh tokens rotate, the second would
   * present an already-rotated token and trip the server's reuse detection, logging the user out.
   * They share one refresh instead.
   */
  private refreshInFlight: Observable<string> | null = null;

  readonly user = this.currentUser.asReadonly();
  readonly isAuthenticated = computed(
    () => this.currentUser() !== null && this.accessToken() !== null,
  );

  token(): string | null {
    return this.accessToken();
  }

  async login(email: string, password: string): Promise<void> {
    const response = await firstValueFrom(
      this.rawHttp.post<AuthResponse>(`${this.config.apiBaseUrl}/api/auth/login`, {
        email,
        password,
      }),
    );

    this.store(response);
  }

  async register(email: string, displayName: string, password: string): Promise<void> {
    const response = await firstValueFrom(
      this.rawHttp.post<AuthResponse>(`${this.config.apiBaseUrl}/api/auth/register`, {
        email,
        displayName,
        password,
      }),
    );

    this.store(response);
  }

  /**
   * Exchanges the refresh token for a new pair, coalescing concurrent callers.
   */
  refresh(): Observable<string> {
    if (this.refreshInFlight) {
      return this.refreshInFlight;
    }

    const refreshToken = readRaw(REFRESH_TOKEN_KEY);
    if (!refreshToken) {
      this.clear();
      return from(Promise.reject(new Error('No refresh token')));
    }

    this.refreshInFlight = from(
      firstValueFrom(
        this.rawHttp.post<AuthResponse>(`${this.config.apiBaseUrl}/api/auth/refresh`, {
          refreshToken,
        }),
      )
        .then((response) => {
          this.store(response);
          return response.accessToken;
        })
        .catch((error) => {
          // A failed refresh is terminal: the token is expired, revoked or replayed, and nothing the
          // client can do will fix it. Clearing rather than retrying stops a refresh loop.
          this.clear();
          void this.router.navigate(['/login']);
          throw error;
        })
        .finally(() => {
          this.refreshInFlight = null;
        }),
      // shareReplay so late subscribers receive the already-resolved token rather than re-running it.
    ).pipe(shareReplay({ bufferSize: 1, refCount: false }));

    return this.refreshInFlight;
  }

  async logout(): Promise<void> {
    const refreshToken = readRaw(REFRESH_TOKEN_KEY);

    // Signing out locally must not depend on the API being online. Clear first so the guards allow
    // the login route immediately; revoking the refresh token remains a best-effort server cleanup.
    this.clear();
    const navigation = this.router.navigate(['/login'], { replaceUrl: true });

    if (refreshToken) {
      // Do not await this request. A slow or unavailable backend must never leave the user looking
      // signed in after they explicitly ended the session.
      void firstValueFrom(
        this.rawHttp.post(`${this.config.apiBaseUrl}/api/auth/logout`, { refreshToken }),
      ).catch(() => undefined);
    }

    await navigation;
  }

  private store(response: AuthResponse): void {
    writeRaw(ACCESS_TOKEN_KEY, response.accessToken);
    writeRaw(REFRESH_TOKEN_KEY, response.refreshToken);
    writeRaw(USER_KEY, JSON.stringify(response.user));

    this.accessToken.set(response.accessToken);
    this.currentUser.set(response.user);
  }

  private clear(): void {
    [ACCESS_TOKEN_KEY, REFRESH_TOKEN_KEY, USER_KEY].forEach((key) => {
      try {
        localStorage.removeItem(key);
      } catch {
        // Private browsing and blocked site data both throw here. There is nothing to clean up in
        // that case, and throwing would leave the app stuck mid-logout.
      }
    });

    this.accessToken.set(null);
    this.currentUser.set(null);
  }
}

function readRaw(key: string): string | null {
  try {
    return localStorage.getItem(key);
  } catch {
    return null;
  }
}

function writeRaw(key: string, value: string): void {
  try {
    localStorage.setItem(key, value);
  } catch {
    // Storage can be unavailable. The session then lasts until the tab closes, which still works.
  }
}

function readStored<T>(key: string): T | null {
  const raw = readRaw(key);
  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw) as T;
  } catch {
    return null;
  }
}
