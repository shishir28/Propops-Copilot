import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { AuthService } from './auth.service';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        {
          provide: AuthService,
          useValue: {
            accessToken: () => 'jwt-token'
          }
        }
      ]
    });
  });

  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
  });

  it('adds the bearer token to API requests', () => {
    const client = TestBed.inject(HttpClient);
    const http = TestBed.inject(HttpTestingController);

    client.get('/api/maintenanceRequests').subscribe();

    const request = http.expectOne('/api/maintenanceRequests');
    expect(request.request.headers.get('Authorization')).toBe('Bearer jwt-token');
    request.flush({});
  });

  it('does not modify non-API requests', () => {
    const client = TestBed.inject(HttpClient);
    const http = TestBed.inject(HttpTestingController);

    client.get('/assets/logo.svg').subscribe();

    const request = http.expectOne('/assets/logo.svg');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({});
  });
});
