import { HttpBackend, HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface RuntimeConfig {
  /** Absolute base URL of the WorkBoard API, with no trailing slash. */
  apiBaseUrl: string;
}

/** The build-time values, swapped per configuration by `angular.json`. */
const BUILD_TIME: RuntimeConfig = {
  apiBaseUrl: environment.apiBaseUrl,
};

/**
 * Configuration in two layers: compiled in, then optionally overridden at startup.
 *
 * `src/environments/environment.ts` supplies the default and is swapped for
 * `environment.development.ts` under the `development` configuration — the ordinary Angular
 * mechanism, and what `ng serve` uses.
 *
 * On top of that, `config.json` is fetched before the first route renders and wins when present.
 * That second layer is what lets one container image run in every environment: an Angular build
 * inlines whatever it is given, so without it the API URL would be compiled into the bundle and
 * staging and production would need separate images — meaning the artefact reaching production is
 * not the one that was tested. `docker/web-entrypoint.sh` writes the file from `API_BASE_URL` at
 * container start.
 *
 * The file is fetched through `HttpBackend`, deliberately bypassing the interceptor chain: the
 * interceptors need the base URL this call is fetching, and the auth interceptor would otherwise try
 * to attach a token before anything is configured.
 */
@Injectable({ providedIn: 'root' })
export class RuntimeConfigService {
  private readonly http = new HttpClient(inject(HttpBackend));
  private readonly config = signal<RuntimeConfig>(BUILD_TIME);

  get apiBaseUrl(): string {
    return this.config().apiBaseUrl;
  }

  async load(): Promise<void> {
    try {
      const loaded = await firstValueFrom(this.http.get<RuntimeConfig>('config.json'));

      // Merged, not replaced: a config.json with an empty or missing apiBaseUrl must not wipe out
      // the build-time default, which is exactly what happens during `ng serve` where the file is
      // served from public/ but is not the authority.
      this.config.set({
        apiBaseUrl: loaded?.apiBaseUrl?.trim() || BUILD_TIME.apiBaseUrl,
      });
    } catch {
      // No config.json is the normal case outside the container. The build-time values stand.
      this.config.set(BUILD_TIME);
    }
  }
}
