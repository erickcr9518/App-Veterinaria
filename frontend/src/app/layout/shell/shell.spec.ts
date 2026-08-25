import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter, Router } from '@angular/router';
import { signal } from '@angular/core';
import { vi } from 'vitest';
import { CurrentUser } from '../../core/models/auth.models';
import { AuthService } from '../../core/services/auth.service';
import { Shell } from './shell';

describe('Shell', () => {
  it('renders only navigation links allowed by the current permissions', async () => {
    const fixture = await createComponent(['owners.read', 'appointments.read']);

    const navTexts = fixture.debugElement
      .queryAll(By.css('.nav-links a'))
      .map((link) => link.nativeElement.textContent.trim());

    expect(navTexts).toEqual(['Panel', 'Propietarios', 'Agenda']);
    expect(navTexts).not.toContain('Pacientes');
  });

  it('renders all MVP navigation links for a clinical user with full front-office access', async () => {
    const fixture = await createComponent(['owners.read', 'patients.read', 'appointments.read']);

    const navTexts = fixture.debugElement
      .queryAll(By.css('.nav-links a'))
      .map((link) => link.nativeElement.textContent.trim());

    expect(navTexts).toEqual(['Panel', 'Propietarios', 'Pacientes', 'Agenda']);
  });

  it('logs out and navigates back to login', async () => {
    const authService = createAuthService(['owners.read']);
    const fixture = await createComponent(['owners.read'], authService);
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);

    fixture.debugElement.query(By.css('button')).nativeElement.click();

    expect(authService.logout).toHaveBeenCalled();
    expect(navigateSpy).toHaveBeenCalledWith('/login');
  });

  async function createComponent(
    permissions: string[],
    authService = createAuthService(permissions),
  ): Promise<ComponentFixture<Shell>> {
    await TestBed.configureTestingModule({
      imports: [Shell],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authService },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(Shell);
    fixture.detectChanges();
    return fixture;
  }

  function createAuthService(permissions: string[]) {
    const currentUser = signal<CurrentUser | null>({
      userId: 'user-1',
      email: 'qa@vetplatform.test',
      fullName: 'QA User',
      clinicId: 'clinic-1',
      clinicName: 'Clinica Demo',
      role: 'Recepcion',
      permissions,
    });

    return {
      currentUser: currentUser.asReadonly(),
      hasPermission: vi.fn((code: string) => permissions.includes(code)),
      logout: vi.fn(),
    };
  }
});
