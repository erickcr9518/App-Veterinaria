import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { ResetPassword } from './reset-password';

describe('ResetPassword', () => {
  it('submits the token and new password when confirmation matches', async () => {
    let payload: { email: string; token: string; newPassword: string } | null = null;
    const fixture = await createComponent({
      resetPassword: (email: string, token: string, newPassword: string) => {
        payload = { email, token, newPassword };
        return of(undefined);
      },
    });

    fixture.componentInstance.form.setValue({
      email: 'admin@vetplatform.test',
      token: 'reset-token',
      newPassword: 'Changed123!',
      confirmPassword: 'Changed123!',
    });
    fixture.componentInstance.submit();
    fixture.detectChanges();

    expect(payload).toEqual({
      email: 'admin@vetplatform.test',
      token: 'reset-token',
      newPassword: 'Changed123!',
    });
    expect(fixture.nativeElement.textContent).toContain('Contraseña actualizada');
  });

  it('does not submit when password confirmation differs', async () => {
    let calls = 0;
    const fixture = await createComponent({
      resetPassword: () => {
        calls++;
        return of(undefined);
      },
    });

    fixture.componentInstance.form.setValue({
      email: 'admin@vetplatform.test',
      token: 'reset-token',
      newPassword: 'Changed123!',
      confirmPassword: 'Other123!',
    });
    fixture.componentInstance.submit();
    fixture.detectChanges();

    expect(calls).toBe(0);
    expect(fixture.nativeElement.textContent).toContain('Las contraseñas no coinciden.');
  });

  async function createComponent(authService: Partial<AuthService>): Promise<ComponentFixture<ResetPassword>> {
    await TestBed.configureTestingModule({
      imports: [ResetPassword],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authService },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(ResetPassword);
    fixture.detectChanges();
    return fixture;
  }
});
