import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { SessionStore } from './auth.service';

export const authGuard: CanActivateFn = () => {
  const store = inject(SessionStore);
  const router = inject(Router);
  return store.token() ? true : router.createUrlTree(['/login']);
};

export const guestGuard: CanActivateFn = () => {
  const store = inject(SessionStore);
  const router = inject(Router);
  return store.token() ? router.createUrlTree(['/']) : true;
};

export const roleGuard: CanActivateFn = (route) => {
  const store = inject(SessionStore);
  const router = inject(Router);
  const roles = (route.data['roles'] as string[] | undefined) ?? [];
  return store.hasRole(...roles) ? true : router.createUrlTree(['/']);
};
