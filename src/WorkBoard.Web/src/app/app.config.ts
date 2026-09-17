import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import type { ApplicationConfig } from '@angular/core';
import {
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection,
} from '@angular/core';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';
import { apiBaseUrlInterceptor, authInterceptor } from './core/auth/auth.interceptor';
import { RuntimeConfigService } from './core/config/runtime-config';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),

    // Zoneless. The app is entirely signal-driven, so there is nothing for zone.js to patch on behalf
    // of — dropping it removes a runtime dependency and the change detection passes it triggers for
    // every setTimeout and XHR the app makes.
    provideZonelessChangeDetection(),

    // Configuration is fetched before the first route renders, because the API base URL every HTTP
    // call needs comes from it. Doing this lazily would mean the first request goes to the fallback.
    provideAppInitializer(() => inject(RuntimeConfigService).load()),

    provideRouter(
      routes,
      // Route params arrive as component inputs, so a component can declare what it needs instead of
      // reaching into ActivatedRoute.
      withComponentInputBinding(),
      // Without this, navigating from the bottom of a long list to a detail page keeps the scroll
      // position, and the new page appears to open half-way down.
      withInMemoryScrolling({ scrollPositionRestoration: 'top', anchorScrolling: 'enabled' }),
    ),

    provideHttpClient(
      // fetch rather than XHR: it is the platform primitive, and it is what makes the app work
      // unchanged if server-side rendering is added later.
      withFetch(),
      // Order matters. apiBaseUrlInterceptor rewrites "/api/..." to an absolute URL first, so
      // authInterceptor can recognise it as one of ours and attach the token.
      withInterceptors([apiBaseUrlInterceptor, authInterceptor]),
    ),
  ],
};
