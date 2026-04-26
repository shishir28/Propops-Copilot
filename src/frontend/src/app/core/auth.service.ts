import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';

import { LoginPayload, PortalRole, PortalSession, PortalUser } from '../models/propops.models';
import { REQUEST_CREATOR_ROLES, STAFF_ROLES } from './portal-roles';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly apiBaseUrl = `${window.location.protocol}//${window.location.hostname}:8095/api/auth`;
  private readonly storageKey = 'propops.portal.session';
  private readonly sessionState = signal<PortalSession | null>(this.readStoredSession());

  readonly session = computed(() => this.sessionState());
  readonly user = computed<PortalUser | null>(() => this.sessionState()?.user ?? null);
  readonly accessToken = computed(() => this.sessionState()?.accessToken ?? null);
  readonly isAuthenticated = computed(() => this.sessionState() !== null);

  login(payload: LoginPayload): Observable<PortalSession> {
    return this.http.post<PortalSession>(`${this.apiBaseUrl}/login`, payload).pipe(
      tap((session) => this.setSession(session))
    );
  }

  logout(redirectToLogin = true): void {
    this.sessionState.set(null);
    localStorage.removeItem(this.storageKey);

    if (redirectToLogin) {
      void this.router.navigate(['/login']);
    }
  }

  private setSession(session: PortalSession): void {
    this.sessionState.set(session);
    localStorage.setItem(this.storageKey, JSON.stringify(session));
    void this.router.navigate([this.getLandingRoute(session.user.role as PortalRole)]);
  }

  private readStoredSession(): PortalSession | null {
    const stored = localStorage.getItem(this.storageKey);
    if (!stored) {
      return null;
    }

    try {
      const session = JSON.parse(stored) as PortalSession;
      const expiresAt = Date.parse(session.expiresAtUtc);
      if (Number.isNaN(expiresAt) || expiresAt <= Date.now()) {
        localStorage.removeItem(this.storageKey);
        return null;
      }

      return session;
    } catch {
      localStorage.removeItem(this.storageKey);
      return null;
    }
  }

  private getLandingRoute(role: PortalRole): string {
    if (STAFF_ROLES.includes(role)) {
      return '/overview';
    }

    if (REQUEST_CREATOR_ROLES.includes(role)) {
      return '/workspace';
    }

    return '/workspace';
  }
}
