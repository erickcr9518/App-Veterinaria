import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Owner, Patient } from '../../../core/models/clinical.models';
import { ClinicalService } from '../../../core/services/clinical.service';

@Component({
  selector: 'app-patients',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './patients.html',
  styleUrl: './patients.scss',
})
export class Patients implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly clinicalService = inject(ClinicalService);

  readonly owners = signal<Owner[]>([]);
  readonly patients = signal<Patient[]>([]);
  readonly search = signal('');
  readonly speciesFilter = signal('');
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly hasPatients = computed(() => this.patients().length > 0);

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
    this.loadOwners();
    this.loadPatients();
  }

  loadOwners(): void {
    this.clinicalService.getOwners().subscribe({
      next: (owners) => this.owners.set(owners),
      error: () => this.errorMessage.set('No se pudieron cargar los propietarios.'),
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
      error: () => {
        this.errorMessage.set('No se pudieron cargar los pacientes.');
        this.isLoading.set(false);
      },
    });
  }

  updateSearch(value: string): void {
    this.search.set(value);
    this.loadPatients();
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

    const value = this.form.getRawValue();
    this.clinicalService.createPatient({
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
    }).subscribe({
      next: () => {
        this.form.reset({ species: 'Perro', sex: 'Hembra', status: 'Activo' });
        this.isSaving.set(false);
        this.loadPatients();
      },
      error: () => {
        this.errorMessage.set('No se pudo guardar el paciente.');
        this.isSaving.set(false);
      },
    });
  }
}
