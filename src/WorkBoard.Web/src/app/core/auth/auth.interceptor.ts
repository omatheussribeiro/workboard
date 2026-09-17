import type { HttpInterceptorFn } from '@angular/common/http';
import { HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { RuntimeConfigService } from '../config/runtime-config';
import { AuthService } from './auth.service';

/**
 * Attaches the access token, and retries once through a refresh on a 401.
 *
 * The retry is what makes a fifteen-minute access token invisible to the user: the request that
 * happens to straddle the expiry is refreshed and replayed rather than surfacing as an error. It
 * retries exactly once — a second 401 after a fresh token means the problem is not expiry, and
 * retrying again would loop.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const config = inject(RuntimeConfigService);

  // Only requests to our own API get the token. Sending it to any other origin the app might call
  // would be handing the credential away.
  const isApiRequest = request.url.startsWith(config.apiBaseUrl) || request.url.startsWith('/api/');
  if (!isApiRequest) {
    return next(request);
  }

  const token = auth.token();
  const authorised = token
    ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : request;

  return next(authorised).pipe(
    catchError((error: unknown) => {
      const isUnauthorised = error instanceof HttpErrorResponse && error.status === 401;

      // The refresh call itself must never trigger this path, or a failing refresh becomes a loop.
      const isRefreshCall = request.url.includes('/api/auth/');

      if (!isUnauthorised || isRefreshCall || !token) {
        return throwError(() => error);
      }

      return auth
        .refresh()
        .pipe(
          switchMap((fresh) =>
            next(request.clone({ setHeaders: { Authorization: `Bearer ${fresh}` } })),
          ),
        );
    }),
  );
};

/**
 * Rewrites relative `/api/...` URLs onto the configured API base.
 *
 * Lets every service write `/api/workspaces` and stay unaware of where the API lives — which is what
 * makes the runtime-configured base URL work without threading it through each call.
 */
export const apiBaseUrlInterceptor: HttpInterceptorFn = (request, next) => {
  const config = inject(RuntimeConfigService);

  if (!request.url.startsWith('/api/')) {
    return next(request);
  }

  return next(request.clone({ url: `${config.apiBaseUrl}${request.url}` }));
};
