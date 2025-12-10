import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { CanActivateFn } from '@angular/router';
import { map, take } from 'rxjs/operators';
import { AuthStateService } from '../services/auth-state.service';
import { AUTH_CONSTANTS } from '../constants/auth.constants';

export const authGuard: CanActivateFn = (route, state) => {
  const authStateService = inject(AuthStateService);
  const router = inject(Router);

  return authStateService.isAuthenticated$.pipe(
    take(1),
    map(isAuthenticated => {
      if (isAuthenticated) {
        return true;
      } else {
        // Store the attempted URL for redirecting after login
        const returnUrl = state.url;
        router.navigate([AUTH_CONSTANTS.ROUTES.LOGIN], { 
          queryParams: { returnUrl } 
        });
        return false;
      }
    })
  );
};

export const guestGuard: CanActivateFn = (route, state) => {
  const authStateService = inject(AuthStateService);
  const router = inject(Router);

  return authStateService.isAuthenticated$.pipe(
    take(1),
    map(isAuthenticated => {
      if (!isAuthenticated) {
        return true;
      } else {
        // Already authenticated, redirect to dashboard
        router.navigate([AUTH_CONSTANTS.ROUTES.REDIRECT_AFTER_LOGIN]);
        return false;
      }
    })
  );
};