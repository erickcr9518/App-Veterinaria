import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ClinicalService } from '../../../core/services/clinical.service';
import { ConsultationFormValue, Patient } from '../../../core/models/clinical.models';

@Component({
  selector: 'app-consultation-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './consultation-form.html',
  styleUrl: './consultation-form.scss',
})
export class ConsultationForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly clinicalService = inject(ClinicalService);

  private readonly consultationId = this.route.snapshot.paramMap.get('id');
  readonly isEditMode = this.consultationId !== null;
  readonly patientId = signal(this.route.snapshot.paramMap.get('patientId') ?? '');

  readonly patient = signal<Patient | null>(null);
  readonly isLoading = signal(this.isEditMode);
  readonly isSaving = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly title = computed(() => (this.isEditMode ? 'Editar consulta (borrador)' : 'Nueva consulta'));

  readonly form = this.fb.nonNullable.group({
    reasonForVisit: ['', [Validators.required, Validators.maxLength(500)]],
    historyOfPresentIllness: [''],
    physicalExamFindings: [''],
    temperatureCelsius: [null as number | null],
    heartRateBpm: [null as number | null],
    respiratoryRateRpm: [null as number | null],
    weightKg: [null as number | null],
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
    if (this.isEditMode) {
      this.loadForEdit(this.consultationId!);
    } else {
      this.clinicalService.getPatientById(this.patientId()).subscribe({
        next: (patient) => this.patient.set(patient),
        error: () => this.errorMessage.set('No se pudo cargar el paciente.'),
      });
    }
  }

  private loadForEdit(id: string): void {
    this.clinicalService.getConsultationById(id).subscribe({
      next: (consultation) => {
        if (consultation.status !== 'Draft') {
          this.router.navigate(['/consultations', id]);
          return;
        }

        this.patientId.set(consultation.patientId);
        this.clinicalService.getPatientById(consultation.patientId).subscribe((patient) => this.patient.set(patient));

        this.form.patchValue({
          reasonForVisit: consultation.reasonForVisit,
          historyOfPresentIllness: consultation.historyOfPresentIllness ?? '',
          physicalExamFindings: consultation.physicalExamFindings ?? '',
          temperatureCelsius: consultation.temperatureCelsius,
          heartRateBpm: consultation.heartRateBpm,
          respiratoryRateRpm: consultation.respiratoryRateRpm,
          weightKg: consultation.weightKg,
          subjective: consultation.subjective ?? '',
          objective: consultation.objective ?? '',
          assessment: consultation.assessment ?? '',
          plan: consultation.plan ?? '',
          diagnosticPlan: consultation.diagnosticPlan ?? '',
          treatment: consultation.treatment ?? '',
          recommendations: consultation.recommendations ?? '',
          followUpDate: consultation.followUpDate,
        });
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('No se pudo cargar la consulta.');
        this.isLoading.set(false);
      },
    });
  }

  submit(): void {
    if (this.form.invalid || this.isSaving()) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);
    const value = this.form.getRawValue() as ConsultationFormValue;

    if (this.isEditMode) {
      this.clinicalService.updateConsultation(this.consultationId!, value).subscribe({
        next: () => this.router.navigate(['/consultations', this.consultationId]),
        error: () => {
          this.isSaving.set(false);
          this.errorMessage.set('No se pudo guardar la consulta.');
        },
      });
    } else {
      this.clinicalService.createConsultation({ ...value, patientId: this.patientId() }).subscribe({
        next: (id) => this.router.navigate(['/consultations', id]),
        error: () => {
          this.isSaving.set(false);
          this.errorMessage.set('No se pudo crear la consulta.');
        },
      });
    }
  }
}
