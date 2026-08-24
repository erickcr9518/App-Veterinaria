import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Observable, Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { Owner, Patient } from '../../../core/models/clinical.models';
import { AuthService } from '../../../core/services/auth.service';
import { ClinicalService } from '../../../core/services/clinical.service';

@Component({
  selector: 'app-patients',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './patients.html',
  styleUrl: './patients.scss',
})
export class Patients implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly clinicalService = inject(ClinicalService);
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly searchChanged = new Subject<string>();

  readonly owners = signal<Owner[]>([]);
  readonly patients = signal<Patient[]>([]);
  readonly search = signal('');
  readonly speciesFilter = signal('');
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly editingPatient = signal<Patient | null>(null);
  readonly formTitle = computed(() => this.editingPatient() ? 'Editar paciente' : 'Nuevo paciente');
  readonly submitLabel = computed(() => this.editingPatient() ? 'Guardar cambios' : 'Guardar paciente');
  readonly hasPatients = computed(() => this.patients().length > 0);
  readonly canSchedule = computed(() => this.authService.hasPermission('appointments.read'));

  readonly form = this.fb.group({
    ownerId: ['', [Validators.required]],
    name: ['', [Validators.required, Validators.maxLength(120)]],
    species: ['Perro', [Validators.required]],
    breed: ['', [Validators.maxLength(120)]],
    estimatedAge: ['', [Validators.maxLength(80)]],
    sex: ['Hembra', [Validators.required]],
    reproductiveStatus: [''],
    color: [''],
    currentWeightKg: [null as number | null, [Validators.min(0.01), Validators.max(499)]],
    microchipNumber: [''],
    allergies: [''],
    chronicDiseases: [''],
    currentMedications: [''],
    vaccinationStatus: [''],
    dewormingStatus: [''],
    status: ['Activo', [Validators.required]],
  });

  ngOnInit(): void {
    this.searchChanged.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe((value) => {
      this.search.set(value);
      this.loadPatients();
    });

    this.loadOwners();
    this.loadPatients();
  }

  loadOwners(): void {
    this.clinicalService.getOwners().subscribe({
      next: (owners) => this.owners.set(owners),
      error: (error: unknown) => this.errorMessage.set(this.getErrorMessage(
        error,
        'No tienes permiso para ver propietarios.',
        'No se pudieron cargar los propietarios.',
      )),
    });
  }

  loadPatients(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.clinicalService.getPatients({
      search: this.search(),
      species: this.speciesFilter() || undefined,
    }).subscribe({
      next: (patients) => {
        this.patients.set(patients);
        this.isLoading.set(false);
      },
      error: (error: unknown) => {
        this.errorMessage.set(this.getErrorMessage(
          error,
          'No tienes permiso para ver pacientes.',
          'No se pudieron cargar los pacientes.',
        ));
        this.isLoading.set(false);
      },
    });
  }

  updateSearch(value: string): void {
    this.searchChanged.next(value);
  }

  updateSpecies(value: string): void {
    this.speciesFilter.set(value);
    this.loadPatients();
  }

  submit(): void {
    if (this.form.invalid || this.isSaving()) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);

    const request = this.buildRequest();
    const editingPatient = this.editingPatient();
    const save$: Observable<string | void> = editingPatient
      ? this.clinicalService.updatePatient(editingPatient.id, request)
      : this.clinicalService.createPatient(request);

    save$.subscribe({
      next: () => {
        this.resetForm();
        this.isSaving.set(false);
        this.loadPatients();
      },
      error: (error: unknown) => {
        this.errorMessage.set(this.getErrorMessage(
          error,
          'No tienes permiso para guardar pacientes.',
          'No se pudo guardar el paciente.',
        ));
        this.isSaving.set(false);
      },
    });
  }

  editPatient(patient: Patient): void {
    this.editingPatient.set(patient);
    this.form.reset({
      ownerId: patient.ownerId,
      name: patient.name,
      species: patient.species,
      breed: patient.breed ?? '',
      estimatedAge: patient.estimatedAge ?? '',
      sex: patient.sex,
      reproductiveStatus: patient.reproductiveStatus ?? '',
      color: patient.color ?? '',
      currentWeightKg: patient.currentWeightKg ?? null,
      microchipNumber: patient.microchipNumber ?? '',
      allergies: patient.allergies ?? '',
      chronicDiseases: patient.chronicDiseases ?? '',
      currentMedications: patient.currentMedications ?? '',
      vaccinationStatus: patient.vaccinationStatus ?? '',
      dewormingStatus: patient.dewormingStatus ?? '',
      status: patient.status,
    });
  }

  cancelEdit(): void {
    this.resetForm();
  }

  private buildRequest() {
    const value = this.form.getRawValue();

    return {
      ownerId: value.ownerId!,
      name: value.name!,
      species: value.species!,
      breed: value.breed,
      birthDate: null,
      estimatedAge: value.estimatedAge,
      sex: value.sex!,
      reproductiveStatus: value.reproductiveStatus,
      color: value.color,
      currentWeightKg: value.currentWeightKg,
      microchipNumber: value.microchipNumber,
      photoUrl: null,
      allergies: value.allergies,
      chronicDiseases: value.chronicDiseases,
      currentMedications: value.currentMedications,
      vaccinationStatus: value.vaccinationStatus,
      dewormingStatus: value.dewormingStatus,
      status: value.status!,
    };
  }

  private resetForm(): void {
    this.editingPatient.set(null);
    this.form.reset({ species: 'Perro', sex: 'Hembra', status: 'Activo' });
  }

  private getErrorMessage(error: unknown, forbiddenMessage: string, fallbackMessage: string): string {
    return error instanceof HttpErrorResponse && error.status === 403
      ? forbiddenMessage
      : fallbackMessage;
  }
}
