import { inject } from '@angular/core';
import type { CanActivateFn } from '@angular/router';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';

/**
 * Keeps unauthenticated visitors out of the application shell.
 *
 * This is a usability control, not a security one, and the distinction matters: the guard runs in the
 * browser, where anyone can bypass it. What actually protects data is the API refusing the request.
 * The guard exists so an expired session lands on the login page instead of on a screen full of
 * failed requests, and so the user returns to where they were afterwards.
 */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

/** Sends an already-signed-in visitor away from the login and register pages. */
export const anonymousGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.isAuthenticated() ? router.createUrlTree(['/']) : true;
};
