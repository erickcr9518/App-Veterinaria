import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { environment } from '../../../environments/environment';
import { AuthResult } from '../models/auth.models';
import { AuthService } from '../services/auth.service';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let accessToken: string | null;
  let refreshToken: string | null;
  let authService: {
    readonly accessToken: string | null;
    readonly refreshToken: string | null;
    refresh: ReturnType<typeof vi.fn<() => Observable<AuthResult>>>;
    logout: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    accessToken = 'access-token';
    refreshToken = 'refresh-token';
    authService = {
      get accessToken() {
        return accessToken;
      },
      get refreshToken() {
        return refreshToken;
      },
      refresh: vi.fn(() => {
        accessToken = 'fresh-access-token';
        return of(createAuthResult('fresh-access-token'));
      }),
      logout: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: authService },
      ],
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    vi.restoreAllMocks();
  });

  it('adds the bearer token to API requests', () => {
    http.get(`${environment.apiUrl}/owners`).subscribe();

    const request = httpMock.expectOne(`${environment.apiUrl}/owners`);

    expect(request.request.headers.get('Authorization')).toBe('Bearer access-token');
    request.flush([]);
  });

  it('does not add the bearer token to non-API requests', () => {
    http.get('/assets/config.json').subscribe();

    const request = httpMock.expectOne('/assets/config.json');

    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({});
  });

  it('refreshes once and retries the original API request after a 401', () => {
    let response: unknown;
    http.get(`${environment.apiUrl}/patients`).subscribe((value) => {
      response = value;
    });

    const firstRequest = httpMock.expectOne(`${environment.apiUrl}/patients`);
    expect(firstRequest.request.headers.get('Authorization')).toBe('Bearer access-token');
    firstRequest.flush(null, { status: 401, statusText: 'Unauthorized' });

    const retryRequest = httpMock.expectOne(`${environment.apiUrl}/patients`);
    expect(authService.refresh).toHaveBeenCalledOnce();
    expect(retryRequest.request.headers.get('Authorization')).toBe('Bearer fresh-access-token');
    retryRequest.flush([{ id: 'patient-1' }]);

    expect(response).toEqual([{ id: 'patient-1' }]);
    expect(authService.logout).not.toHaveBeenCalled();
  });

  it('logs out when refresh fails after an API 401', () => {
    authService.refresh.mockReturnValue(throwError(() => new Error('refresh failed')));
    let error: unknown;

    http.get(`${environment.apiUrl}/patients`).subscribe({
      error: (value) => {
        error = value;
      },
    });

    const request = httpMock.expectOne(`${environment.apiUrl}/patients`);
    request.flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(authService.refresh).toHaveBeenCalledOnce();
    expect(authService.logout).toHaveBeenCalledOnce();
    expect(error).toEqual(new Error('refresh failed'));
  });

  it('does not try to refresh auth endpoint failures', () => {
    let status: number | undefined;

    http.post(`${environment.apiUrl}/auth/login`, { email: 'demo@test', password: 'bad' }).subscribe({
      error: (error) => {
        status = error.status;
      },
    });

    const request = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
    request.flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(authService.refresh).not.toHaveBeenCalled();
    expect(authService.logout).not.toHaveBeenCalled();
    expect(status).toBe(401);
  });

  function createAuthResult(token: string): AuthResult {
    return {
      accessToken: token,
      accessTokenExpiresAtUtc: '2026-08-25T18:00:00Z',
      refreshToken: 'fresh-refresh-token',
      refreshTokenExpiresAtUtc: '2026-09-01T18:00:00Z',
      userId: 'user-1',
      email: 'user@vetplatform.test',
      fullName: 'QA User',
      clinicId: 'clinic-1',
      clinicName: 'Clinica Demo',
      role: 'Veterinario',
      permissions: [],
    };
  }
});
