import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ClinicalService } from '../../../core/services/clinical.service';
import { Patient, PrescriptionFormValue } from '../../../core/models/clinical.models';

@Component({
  selector: 'app-prescription-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './prescription-form.html',
  styleUrl: './prescription-form.scss',
})
export class PrescriptionForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly clinicalService = inject(ClinicalService);

  private readonly prescriptionId = this.route.snapshot.paramMap.get('id');
  readonly isEditMode = this.prescriptionId !== null;
  private readonly consultationId = this.route.snapshot.paramMap.get('consultationId');

  readonly patient = signal<Patient | null>(null);
  readonly patientId = signal<string | null>(null);
  readonly isLoading = signal(this.isEditMode);
  readonly isSaving = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly title = computed(() => (this.isEditMode ? 'Editar receta (borrador)' : 'Nueva receta'));

  readonly form = this.fb.nonNullable.group({
    weightKgAtPrescription: [null as number | null],
    generalInstructions: [''],
    warnings: [''],
    items: this.fb.nonNullable.array([this.buildItemGroup()]),
  });

  get items() {
    return this.form.controls.items;
  }

  ngOnInit(): void {
    if (this.isEditMode) {
      this.loadForEdit(this.prescriptionId!);
    } else if (this.consultationId) {
      this.loadForCreate(this.consultationId);
    } else {
      this.errorMessage.set('Falta la consulta de origen para crear la receta.');
      this.isLoading.set(false);
    }
  }

  private buildItemGroup(initial?: {
    productName?: string;
    concentration?: string | null;
    presentation?: string | null;
    quantity?: string;
    route?: string;
    frequency?: string;
    duration?: string;
    instructions?: string | null;
  }) {
    return this.fb.nonNullable.group({
      productName: [initial?.productName ?? '', [Validators.required, Validators.maxLength(200)]],
      concentration: [initial?.concentration ?? ''],
      presentation: [initial?.presentation ?? ''],
      quantity: [initial?.quantity ?? '', [Validators.required, Validators.maxLength(100)]],
      route: [initial?.route ?? '', [Validators.required, Validators.maxLength(100)]],
      frequency: [initial?.frequency ?? '', [Validators.required, Validators.maxLength(100)]],
      duration: [initial?.duration ?? '', [Validators.required, Validators.maxLength(100)]],
      instructions: [initial?.instructions ?? ''],
    });
  }

  addItem(): void {
    this.items.push(this.buildItemGroup());
  }

  removeItem(index: number): void {
    if (this.items.length > 1) {
      this.items.removeAt(index);
    }
  }

  private loadForCreate(consultationId: string): void {
    this.clinicalService.getConsultationById(consultationId).subscribe({
      next: (consultation) => {
        this.patientId.set(consultation.patientId);
        this.form.patchValue({ weightKgAtPrescription: consultation.weightKg ?? null });
        this.clinicalService.getPatientById(consultation.patientId).subscribe({
          next: (patient) => {
            this.patient.set(patient);
            if (!consultation.weightKg && patient.currentWeightKg) {
              this.form.patchValue({ weightKgAtPrescription: patient.currentWeightKg });
            }
          },
        });
      },
      error: () => this.errorMessage.set('No se pudo cargar la consulta de origen.'),
    });
  }

  private loadForEdit(id: string): void {
    this.clinicalService.getPrescriptionById(id).subscribe({
      next: (prescription) => {
        if (prescription.status !== 'Draft') {
          this.router.navigate(['/prescriptions', id]);
          return;
        }

        this.patientId.set(prescription.patientId);
        this.clinicalService.getPatientById(prescription.patientId).subscribe((patient) => this.patient.set(patient));

        this.form.patchValue({
          weightKgAtPrescription: prescription.weightKgAtPrescription,
          generalInstructions: prescription.generalInstructions ?? '',
          warnings: prescription.warnings ?? '',
        });

        this.items.clear();
        for (const item of prescription.items) {
          this.items.push(this.buildItemGroup(item));
        }
        if (this.items.length === 0) {
          this.items.push(this.buildItemGroup());
        }

        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('No se pudo cargar la receta.');
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
    const value = this.form.getRawValue() as PrescriptionFormValue;

    if (this.isEditMode) {
      this.clinicalService.updatePrescription(this.prescriptionId!, value).subscribe({
        next: () => this.router.navigate(['/prescriptions', this.prescriptionId]),
        error: () => {
          this.isSaving.set(false);
          this.errorMessage.set('No se pudo guardar la receta.');
        },
      });
    } else {
      this.clinicalService.createPrescription({ ...value, consultationId: this.consultationId! }).subscribe({
        next: (id) => this.router.navigate(['/prescriptions', id]),
        error: () => {
          this.isSaving.set(false);
          this.errorMessage.set('No se pudo crear la receta.');
        },
      });
    }
  }
}
