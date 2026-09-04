import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { permissionGuard } from './permission.guard';

describe('permissionGuard', () => {
  let router: Router;
  let fakeAuthService: { allowed: boolean; requestedPermission: string | null; hasPermission: (code: string) => boolean };

  beforeEach(() => {
    fakeAuthService = {
      allowed: false,
      requestedPermission: null,
      hasPermission(code: string): boolean {
        this.requestedPermission = code;
        return this.allowed;
      },
    };

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: fakeAuthService },
      ],
    });

    router = TestBed.inject(Router);
  });

  it('allows routes without a required permission', () => {
    const result = runGuard();

    expect(result).toBe(true);
    expect(fakeAuthService.requestedPermission).toBeNull();
  });

  it('allows routes when the user has the required permission', () => {
    fakeAuthService.allowed = true;

    const result = runGuard('owners.read');

    expect(result).toBe(true);
    expect(fakeAuthService.requestedPermission).toBe('owners.read');
  });

  it('redirects to the dashboard when the user lacks the required permission', () => {
    const result = runGuard('patients.read');

    expect(result instanceof UrlTree).toBe(true);
    expect(router.serializeUrl(result as UrlTree)).toBe('/dashboard');
    expect(fakeAuthService.requestedPermission).toBe('patients.read');
  });

  it('allows routes when the user has any one of several required permissions', () => {
    const grantedOnly = new Set(['audit.read.own']);
    fakeAuthService.hasPermission = (code: string) => grantedOnly.has(code);

    const result = runGuard(['audit.read.all', 'audit.read.own']);

    expect(result).toBe(true);
  });

  it('redirects to the dashboard when the user has none of several required permissions', () => {
    const result = runGuard(['audit.read.all', 'audit.read.own']);

    expect(result instanceof UrlTree).toBe(true);
    expect(router.serializeUrl(result as UrlTree)).toBe('/dashboard');
  });

  function runGuard(permission?: string | string[]): boolean | UrlTree {
    const route = { data: permission ? { permission } : {} } as ActivatedRouteSnapshot;
    const state = {} as RouterStateSnapshot;

    return TestBed.runInInjectionContext(() => permissionGuard(route, state)) as boolean | UrlTree;
  }
});
