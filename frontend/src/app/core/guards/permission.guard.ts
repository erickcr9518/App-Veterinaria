import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const permissionGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const permission = route.data['permission'] as string | string[] | undefined;

  const permissions = Array.isArray(permission) ? permission : permission ? [permission] : [];

  if (permissions.length === 0 || permissions.some((code) => authService.hasPermission(code))) {
    return true;
  }

  return router.createUrlTree(['/dashboard']);
};
