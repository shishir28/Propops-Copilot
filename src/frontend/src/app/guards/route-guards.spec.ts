import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, provideRouter, Router, UrlTree } from '@angular/router';

import { AuthService } from '../core/auth.service';
import { PortalRole } from '../models/propops.models';
import { authGuard } from './auth.guard';
import { guestGuard } from './guest.guard';
import { roleGuard } from './role.guard';

describe('route guards', () => {
  function configureAuthService(isAuthenticated: boolean, role?: PortalRole): void {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: {
            isAuthenticated: () => isAuthenticated,
            user: () =>
              role
                ? {
                    id: 'user-1',
                    fullName: 'Jordan Blake',
                    email: 'user@propops.local',
                    role
                  }
                : null
          }
        }
      ]
    });
  }

  function runGuard(guard: ReturnType<typeof roleGuard> | typeof authGuard | typeof guestGuard): boolean | UrlTree {
    const router = TestBed.inject(Router);
    const result = TestBed.runInInjectionContext(() =>
      guard(new ActivatedRouteSnapshot(), router.routerState.snapshot)
    );

    if (typeof result === 'boolean' || result instanceof UrlTree) {
      return result;
    }

    throw new Error('Expected the guard to return a synchronous boolean or UrlTree result.');
  }

  function serialize(result: boolean | UrlTree): string | boolean {
    if (typeof result === 'boolean') {
      return result;
    }

    return TestBed.inject(Router).serializeUrl(result);
  }

  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('allows authenticated users through authGuard', () => {
    configureAuthService(true, 'PropertyManager');

    const result = runGuard(authGuard);

    expect(result).toBe(true);
  });

  it('redirects unauthenticated users to login through authGuard', () => {
    configureAuthService(false);

    const result = runGuard(authGuard);

    expect(serialize(result)).toBe('/login');
  });

  it('redirects authenticated guests away from the login page', () => {
    configureAuthService(true, 'Dispatcher');

    const result = runGuard(guestGuard);

    expect(serialize(result)).toBe('/overview');
  });

  it('allows unauthenticated users through guestGuard', () => {
    configureAuthService(false);

    const result = runGuard(guestGuard);

    expect(result).toBe(true);
  });

  it('redirects unauthenticated users to login through roleGuard', () => {
    configureAuthService(false);

    const result = runGuard(roleGuard(['PropertyManager']));

    expect(serialize(result)).toBe('/login');
  });

  it('allows users with an allowed role through roleGuard', () => {
    configureAuthService(true, 'Tenant');

    const result = runGuard(roleGuard(['Tenant', 'PropertyOwner']));

    expect(result).toBe(true);
  });

  it('redirects users without an allowed role to the workspace', () => {
    configureAuthService(true, 'Vendor');

    const result = runGuard(roleGuard(['PropertyManager', 'Dispatcher']));

    expect(serialize(result)).toBe('/workspace');
  });
});
