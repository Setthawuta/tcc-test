import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { APP_PATHS } from '../constants/routes';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.hasValidToken()) {
    return true;
  }

  auth.logout();
  router.navigate([APP_PATHS.login]);
  return false;
};
