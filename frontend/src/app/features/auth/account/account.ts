import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-account',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './account.html',
  styleUrl: './account.scss',
})
export class Account {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);

  readonly currentUser = this.authService.currentUser;
  readonly form = this.fb.group({
    currentPassword: ['', [Validators.required]],
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', [Validators.required]],
  });

  readonly isSubmitting = signal(false);
  readonly successMessage = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);

  submit(): void {
    if (this.form.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }

    const { currentPassword, newPassword, confirmPassword } = this.form.getRawValue();
    if (newPassword !== confirmPassword) {
      this.errorMessage.set('Las contraseñas no coinciden.');
      return;
    }

    this.isSubmitting.set(true);
    this.successMessage.set(null);
    this.errorMessage.set(null);

    this.authService.changePassword(currentPassword!, newPassword!).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.form.reset();
        this.authService.logout();
        this.successMessage.set('Contraseña actualizada. Inicia sesión de nuevo con la nueva contraseña.');
      },
      error: (error: unknown) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(this.resolveErrorMessage(error));
      },
    });
  }

  private resolveErrorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse && error.status === 400) {
      return 'La contraseña actual no es correcta o la nueva contraseña no cumple los requisitos.';
    }

    if (error instanceof HttpErrorResponse && error.status === 429) {
      return 'Demasiados intentos. Espera un momento antes de volver a probar.';
    }

    return 'No se pudo cambiar la contraseña. Intenta de nuevo.';
  }
}
