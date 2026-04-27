import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { vi } from 'vitest';

import { AuthService } from './auth.service';
import { PortalSession } from '../models/propops.models';

describe('AuthService', () => {
  const storageKey = 'propops.portal.session';
  const authBaseUrl = `${window.location.protocol}//${window.location.hostname}:8095/api/auth`;

  function configureTestingModule(): void {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    });
  }

  function createSession(role: PortalSession['user']['role']): PortalSession {
    return {
      accessToken: 'token-123',
      expiresAtUtc: '2099-01-01T00:00:00Z',
      user: {
        id: 'user-1',
        fullName: 'Jordan Blake',
        email: 'manager@propops.local',
        role
      }
    };
  }

  beforeEach(() => {
    localStorage.clear();
    TestBed.resetTestingModule();
  });

  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    localStorage.clear();
  });

  it('stores the session and redirects staff users to the overview after login', () => {
    configureTestingModule();
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    const service = TestBed.inject(AuthService);
    const http = TestBed.inject(HttpTestingController);
    const session = createSession('PropertyManager');

    service.login({ email: session.user.email, password: 'PropOps!Manager1' }).subscribe();

    const request = http.expectOne(`${authBaseUrl}/login`);
    expect(request.request.method).toBe('POST');
    request.flush(session);

    expect(service.isAuthenticated()).toBe(true);
    expect(service.accessToken()).toBe(session.accessToken);
    expect(localStorage.getItem(storageKey)).toBe(JSON.stringify(session));
    expect(navigateSpy).toHaveBeenCalledWith(['/overview']);
  });

  it('redirects non-staff authenticated users to the workspace after login', () => {
    configureTestingModule();
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    const service = TestBed.inject(AuthService);
    const http = TestBed.inject(HttpTestingController);
    const session = createSession('Tenant');

    service.login({ email: 'tenant@propops.local', password: 'PropOps!Tenant1' }).subscribe();

    http.expectOne(`${authBaseUrl}/login`).flush(session);

    expect(navigateSpy).toHaveBeenCalledWith(['/workspace']);
  });

  it('drops expired stored sessions during initialization', () => {
    configureTestingModule();
    localStorage.setItem(
      storageKey,
      JSON.stringify({
        ...createSession('Dispatcher'),
        expiresAtUtc: '2000-01-01T00:00:00Z'
      } satisfies PortalSession)
    );

    const service = TestBed.inject(AuthService);

    expect(service.isAuthenticated()).toBe(false);
    expect(service.session()).toBeNull();
    expect(localStorage.getItem(storageKey)).toBeNull();
  });

  it('clears the stored session and redirects to login on logout', () => {
    configureTestingModule();
    localStorage.setItem(storageKey, JSON.stringify(createSession('PropertyOwner')));
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    const service = TestBed.inject(AuthService);

    service.logout();

    expect(service.isAuthenticated()).toBe(false);
    expect(localStorage.getItem(storageKey)).toBeNull();
    expect(navigateSpy).toHaveBeenCalledWith(['/login']);
  });
});
