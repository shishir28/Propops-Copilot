import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';

import { AuthService } from '../core/auth.service';
import { PortalRole } from '../models/propops.models';

export const roleGuard =
  (allowedRoles: PortalRole[]): CanActivateFn =>
  () => {
    const authService = inject(AuthService);
    const router = inject(Router);
    const user = authService.user();

    if (!user) {
      return router.createUrlTree(['/login']);
    }

    return allowedRoles.includes(user.role as PortalRole)
      ? true
      : router.createUrlTree(['/workspace']);
  };
