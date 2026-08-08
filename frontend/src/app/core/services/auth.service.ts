import { HttpClient } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';
import { Observable, catchError, of, tap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResult, CurrentUser } from '../models/auth.models';

const ACCESS_TOKEN_KEY = 'vetplatform.accessToken';
const REFRESH_TOKEN_KEY = 'vetplatform.refreshToken';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly currentUserSignal = signal<CurrentUser | null>(null);

  readonly currentUser = this.currentUserSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.currentUserSignal() !== null);

  constructor(private readonly http: HttpClient) {}

  get accessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  }

  get refreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  }

  login(email: string, password: string): Observable<AuthResult> {
    return this.http.post<AuthResult>(`${environment.apiUrl}/auth/login`, { email, password }).pipe(
      tap((result) => this.applyAuthResult(result)),
    );
  }

  refresh(): Observable<AuthResult> {
    const refreshToken = this.refreshToken;
    if (!refreshToken) {
      return throwError(() => new Error('No hay un token de renovación disponible.'));
    }

    return this.http.post<AuthResult>(`${environment.apiUrl}/auth/refresh`, { refreshToken }).pipe(
      tap((result) => this.applyAuthResult(result)),
    );
  }

  restoreSession(): Observable<CurrentUser | null> {
    if (!this.accessToken) {
      return of(null);
    }

    return this.http.get<CurrentUser>(`${environment.apiUrl}/auth/me`).pipe(
      tap((user) => this.currentUserSignal.set(user)),
      catchError(() => {
        this.clearSession();
        return of(null);
      }),
    );
  }

  logout(): void {
    this.clearSession();
  }

  hasPermission(code: string): boolean {
    return this.currentUserSignal()?.permissions.includes(code) ?? false;
  }

  private applyAuthResult(result: AuthResult): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, result.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, result.refreshToken);
    this.currentUserSignal.set({
      userId: result.userId,
      email: result.email,
      fullName: result.fullName,
      clinicId: result.clinicId,
      clinicName: result.clinicName,
      role: result.role,
      permissions: result.permissions,
    });
  }

  private clearSession(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    this.currentUserSignal.set(null);
  }
}
