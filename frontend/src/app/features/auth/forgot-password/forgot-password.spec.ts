import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { ForgotPassword } from './forgot-password';

describe('ForgotPassword', () => {
  it('requests a password reset and shows the generic response', async () => {
    let requestedEmail = '';
    const fixture = await createComponent({
      requestPasswordReset: (email: string) => {
        requestedEmail = email;
        return of({
          message: 'Si el correo existe, enviaremos instrucciones para restablecer la contraseña.',
          resetUrl: 'http://localhost:4200/reset-password?token=abc',
        });
      },
    });

    fixture.componentInstance.form.setValue({ email: 'admin@vetplatform.test' });
    fixture.componentInstance.submit();
    fixture.detectChanges();

    expect(requestedEmail).toBe('admin@vetplatform.test');
    expect(fixture.nativeElement.textContent).toContain('Si el correo existe');
    expect(fixture.nativeElement.textContent).toContain('Abrir enlace de desarrollo');
  });

  async function createComponent(authService: Partial<AuthService>): Promise<ComponentFixture<ForgotPassword>> {
    await TestBed.configureTestingModule({
      imports: [ForgotPassword],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authService },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(ForgotPassword);
    fixture.detectChanges();
    return fixture;
  }
});
