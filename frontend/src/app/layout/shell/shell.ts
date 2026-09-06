import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { CurrentUser } from '../../core/models/auth.models';
import { AuthService } from '../../core/services/auth.service';

const ROLE_LABELS: Record<string, string> = {
  Administrador: 'Administrador',
  Veterinario: 'Veterinario',
  Recepcion: 'Recepcion',
  SuperAdministrador: 'Superadministrador',
};

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class Shell {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly currentUser = this.authService.currentUser;

  hasPermission(code: string): boolean {
    return this.authService.hasPermission(code);
  }

  roleLabels(user: CurrentUser): string {
    const roles = user.roles.length ? user.roles : [user.role];
    return roles.map((role) => ROLE_LABELS[role] ?? role).join(' + ');
  }

  logout(): void {
    this.authService.logout();
    this.router.navigateByUrl('/login');
  }
}
