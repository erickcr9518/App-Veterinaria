import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ClinicalService } from '../../../core/services/clinical.service';
import { AuthService } from '../../../core/services/auth.service';
import { PrescriptionDetail as PrescriptionDetailModel } from '../../../core/models/clinical.models';

@Component({
  selector: 'app-prescription-detail',
  standalone: true,
  imports: [RouterLink, DatePipe],
  templateUrl: './prescription-detail.html',
  styleUrl: './prescription-detail.scss',
})
export class PrescriptionDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly clinicalService = inject(ClinicalService);
  private readonly authService = inject(AuthService);

  private readonly id = this.route.snapshot.paramMap.get('id')!;

  readonly prescription = signal<PrescriptionDetailModel | null>(null);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  readonly isConfirmingFinalize = signal(false);
  readonly isFinalizing = signal(false);

  ngOnInit(): void {
    this.load();
  }

  canWrite(): boolean {
    return this.authService.hasPermission('prescriptions.write');
  }

  private load(): void {
    this.isLoading.set(true);
    this.clinicalService.getPrescriptionById(this.id).subscribe({
      next: (prescription) => {
        this.prescription.set(prescription);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('No se pudo cargar la receta.');
        this.isLoading.set(false);
      },
    });
  }

  confirmFinalize(): void {
    this.isConfirmingFinalize.set(true);
  }

  cancelFinalize(): void {
    this.isConfirmingFinalize.set(false);
  }

  finalize(): void {
    this.isFinalizing.set(true);
    this.errorMessage.set(null);
    this.clinicalService.finalizePrescription(this.id).subscribe({
      next: () => {
        this.isFinalizing.set(false);
        this.isConfirmingFinalize.set(false);
        this.load();
      },
      error: (error: unknown) => {
        this.isFinalizing.set(false);
        this.isConfirmingFinalize.set(false);
        this.errorMessage.set(this.extractValidationMessage(error) ?? 'No se pudo finalizar la receta.');
      },
    });
  }

  private extractValidationMessage(error: unknown): string | null {
    const body = (error as { error?: { errors?: Record<string, string[]> } })?.error;
    if (!body?.errors) {
      return null;
    }
    const firstEntry = Object.values(body.errors)[0];
    return firstEntry?.[0] ?? null;
  }
}
