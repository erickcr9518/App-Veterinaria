import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { AuthResult } from '../models/auth.models';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        AuthService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('returns false for permissions when no user is authenticated', () => {
    expect(service.hasPermission('owners.read')).toBe(false);
  });

  it('returns true only for permissions from the authenticated user session', () => {
    service.login('vet@demo.test', 'Admin123!').subscribe();

    const request = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
    request.flush(createAuthResult(['owners.read', 'patients.read']));

    expect(service.hasPermission('owners.read')).toBe(true);
    expect(service.hasPermission('patients.read')).toBe(true);
    expect(service.hasPermission('clinics.manage')).toBe(false);
  });

  it('clears the local session and revokes the refresh token on logout', () => {
    service.login('vet@demo.test', 'Admin123!').subscribe();

    const loginRequest = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
    loginRequest.flush(createAuthResult(['owners.read']));

    service.logout();

    expect(service.accessToken).toBeNull();
    expect(service.refreshToken).toBeNull();
    expect(service.hasPermission('owners.read')).toBe(false);

    const logoutRequest = httpMock.expectOne(`${environment.apiUrl}/auth/logout`);
    expect(logoutRequest.request.method).toBe('POST');
    expect(logoutRequest.request.body).toEqual({ refreshToken: 'refresh-token' });
    logoutRequest.flush(null);
  });

  function createAuthResult(permissions: string[]): AuthResult {
    return {
      accessToken: 'access-token',
      accessTokenExpiresAtUtc: '2026-08-18T18:00:00Z',
      refreshToken: 'refresh-token',
      refreshTokenExpiresAtUtc: '2026-08-25T18:00:00Z',
      userId: 'user-1',
      email: 'vet@demo.test',
      fullName: 'Veterinaria Demo',
      clinicId: 'clinic-1',
      clinicName: 'Clinica Demo',
      role: 'Veterinario',
      permissions,
    };
  }
});
