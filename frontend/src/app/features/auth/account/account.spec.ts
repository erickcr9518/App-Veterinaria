import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { CurrentUser } from '../../../core/models/auth.models';
import { AuthService } from '../../../core/services/auth.service';
import { Account } from './account';

describe('Account', () => {
  it('changes the password and logs the user out', async () => {
    let payload: { currentPassword: string; newPassword: string } | null = null;
    let logoutCalls = 0;
    const fixture = await createComponent({
      changePassword: (currentPassword: string, newPassword: string) => {
        payload = { currentPassword, newPassword };
        return of(undefined);
      },
      logout: () => {
        logoutCalls++;
      },
    });

    fixture.componentInstance.form.setValue({
      currentPassword: 'Password123!',
      newPassword: 'Changed123!',
      confirmPassword: 'Changed123!',
    });
    fixture.componentInstance.submit();
    fixture.detectChanges();

    expect(payload).toEqual({ currentPassword: 'Password123!', newPassword: 'Changed123!' });
    expect(logoutCalls).toBe(1);
    expect(fixture.nativeElement.textContent).toContain('Contraseña actualizada');
  });

  it('does not submit when password confirmation differs', async () => {
    let calls = 0;
    const fixture = await createComponent({
      changePassword: () => {
        calls++;
        return of(undefined);
      },
      logout: () => undefined,
    });

    fixture.componentInstance.form.setValue({
      currentPassword: 'Password123!',
      newPassword: 'Changed123!',
      confirmPassword: 'Other123!',
    });
    fixture.componentInstance.submit();
    fixture.detectChanges();

    expect(calls).toBe(0);
    expect(fixture.nativeElement.textContent).toContain('Las contraseñas no coinciden.');
  });

  async function createComponent(authService: Partial<AuthService>): Promise<ComponentFixture<Account>> {
    const currentUser = signal<CurrentUser | null>({
      userId: 'user-1',
      email: 'vet@vetplatform.test',
      fullName: 'Dra. Ana Rojas',
      clinicId: 'clinic-1',
      clinicName: 'Clinica Demo',
      role: 'Veterinario',
      permissions: [],
    });

    await TestBed.configureTestingModule({
      imports: [Account],
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: {
            currentUser: currentUser.asReadonly(),
            ...authService,
          },
        },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(Account);
    fixture.detectChanges();
    return fixture;
  }
});
