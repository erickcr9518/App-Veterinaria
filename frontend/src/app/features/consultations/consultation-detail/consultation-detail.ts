import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ClinicalService } from '../../../core/services/clinical.service';
import { AuthService } from '../../../core/services/auth.service';
import { ConsultationDetail as ConsultationDetailModel } from '../../../core/models/clinical.models';

const FIELD_LABELS: Record<string, string> = {
  ReasonForVisit: 'Motivo de consulta',
  HistoryOfPresentIllness: 'Historia del problema actual',
  PhysicalExamFindings: 'Examen fisico',
  TemperatureCelsius: 'Temperatura (grados C)',
  HeartRateBpm: 'Frecuencia cardiaca (lpm)',
  RespiratoryRateRpm: 'Frecuencia respiratoria (rpm)',
  DiagnosticPlan: 'Plan diagnostico',
  Treatment: 'Tratamiento',
  Recommendations: 'Recomendaciones',
  FollowUpDate: 'Fecha de seguimiento',
  Subjective: 'S - Subjetivo',
  Objective: 'O - Objetivo',
  Assessment: 'A - Evaluacion',
  Plan: 'P - Plan',
};

interface PreviousValueEntry {
  label: string;
  value: string;
}

@Component({
  selector: 'app-consultation-detail',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, DatePipe],
  templateUrl: './consultation-detail.html',
  styleUrl: './consultation-detail.scss',
})
export class ConsultationDetail implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly clinicalService = inject(ClinicalService);
  private readonly authService = inject(AuthService);

  private readonly id = this.route.snapshot.paramMap.get('id')!;

  readonly consultation = signal<ConsultationDetailModel | null>(null);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  readonly isConfirmingFinalize = signal(false);
  readonly isFinalizing = signal(false);

  readonly isAmending = signal(false);
  readonly isSavingAmendment = signal(false);
  readonly amendmentError = signal<string | null>(null);
  readonly expandedAmendmentIds = signal<Set<string>>(new Set());

  readonly amendForm = this.fb.nonNullable.group({
    reason: ['', [Validators.required, Validators.maxLength(1000)]],
    reasonForVisit: ['', [Validators.required, Validators.maxLength(500)]],
    historyOfPresentIllness: [''],
    physicalExamFindings: [''],
    temperatureCelsius: [null as number | null],
    heartRateBpm: [null as number | null],
    respiratoryRateRpm: [null as number | null],
    subjective: [''],
    objective: [''],
    assessment: [''],
    plan: [''],
    diagnosticPlan: [''],
    treatment: [''],
    recommendations: [''],
    followUpDate: [null as string | null],
  });

  ngOnInit(): void {
    this.load();
  }

  canWrite(): boolean {
    return this.authService.hasPermission('consultations.write');
  }

  canSign(): boolean {
    return this.authService.hasPermission('consultations.sign');
  }

  private load(): void {
    this.isLoading.set(true);
    this.clinicalService.getConsultationById(this.id).subscribe({
      next: (consultation) => {
        this.consultation.set(consultation);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('No se pudo cargar la consulta.');
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
    this.clinicalService.finalizeConsultation(this.id).subscribe({
      next: () => {
        this.isFinalizing.set(false);
        this.isConfirmingFinalize.set(false);
        this.load();
      },
      error: (error) => {
        this.isFinalizing.set(false);
        this.isConfirmingFinalize.set(false);
        this.errorMessage.set(this.extractValidationMessage(error) ?? 'No se pudo finalizar la consulta.');
      },
    });
  }

  startAmend(): void {
    const consultation = this.consultation();
    if (!consultation) {
      return;
    }

    this.amendForm.reset({
      reason: '',
      reasonForVisit: consultation.reasonForVisit,
      historyOfPresentIllness: consultation.historyOfPresentIllness ?? '',
      physicalExamFindings: consultation.physicalExamFindings ?? '',
      temperatureCelsius: consultation.temperatureCelsius,
      heartRateBpm: consultation.heartRateBpm,
      respiratoryRateRpm: consultation.respiratoryRateRpm,
      subjective: consultation.subjective ?? '',
      objective: consultation.objective ?? '',
      assessment: consultation.assessment ?? '',
      plan: consultation.plan ?? '',
      diagnosticPlan: consultation.diagnosticPlan ?? '',
      treatment: consultation.treatment ?? '',
      recommendations: consultation.recommendations ?? '',
      followUpDate: consultation.followUpDate,
    });
    this.amendmentError.set(null);
    this.isAmending.set(true);
  }

  cancelAmend(): void {
    this.isAmending.set(false);
  }

  submitAmendment(): void {
    if (this.amendForm.invalid || this.isSavingAmendment()) {
      this.amendForm.markAllAsTouched();
      return;
    }

    this.isSavingAmendment.set(true);
    this.amendmentError.set(null);
    const value = this.amendForm.getRawValue();

    this.clinicalService.amendConsultation(this.id, value).subscribe({
      next: () => {
        this.isSavingAmendment.set(false);
        this.isAmending.set(false);
        this.load();
      },
      error: (error) => {
        this.isSavingAmendment.set(false);
        this.amendmentError.set(this.extractValidationMessage(error) ?? 'No se pudo guardar la enmienda.');
      },
    });
  }

  toggleAmendment(id: string): void {
    const expanded = new Set(this.expandedAmendmentIds());
    if (expanded.has(id)) {
      expanded.delete(id);
    } else {
      expanded.add(id);
    }
    this.expandedAmendmentIds.set(expanded);
  }

  isAmendmentExpanded(id: string): boolean {
    return this.expandedAmendmentIds().has(id);
  }

  parsePreviousValues(json: string): PreviousValueEntry[] {
    try {
      const parsed = JSON.parse(json) as Record<string, unknown>;
      return Object.entries(parsed)
        .filter(([, value]) => value !== null && value !== '')
        .map(([key, value]) => ({
          label: FIELD_LABELS[key] ?? key,
          value: String(value),
        }));
    } catch {
      return [];
    }
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
