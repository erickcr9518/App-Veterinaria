import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { DashboardService } from '../../core/services/dashboard.service';
import { DashboardSummary } from '../../core/models/dashboard.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink, DatePipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly dashboardService = inject(DashboardService);

  readonly currentUser = this.authService.currentUser;
  readonly summary = signal<DashboardSummary | null>(null);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.dashboardService.getSummary().subscribe({
      next: (summary) => {
        this.summary.set(summary);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('No se pudo cargar el resumen del panel.');
        this.isLoading.set(false);
      },
    });
  }

  hasPermission(code: string): boolean {
    return this.authService.hasPermission(code);
  }

  statusLabel(status: string): string {
    const labels: Record<string, string> = {
      Scheduled: 'Programada',
      Confirmed: 'Confirmada',
      Cancelled: 'Cancelada',
      Completed: 'Completada',
      NoShow: 'No asistio',
    };
    return labels[status] ?? status;
  }
}
