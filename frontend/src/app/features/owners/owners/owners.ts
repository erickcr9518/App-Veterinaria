import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ClinicalService } from '../../../core/services/clinical.service';
import { Owner } from '../../../core/models/clinical.models';

@Component({
  selector: 'app-owners',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './owners.html',
  styleUrl: './owners.scss',
})
export class Owners implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly clinicalService = inject(ClinicalService);

  readonly owners = signal<Owner[]>([]);
  readonly search = signal('');
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly hasOwners = computed(() => this.owners().length > 0);

  readonly form = this.fb.group({
    fullName: ['', [Validators.required, Validators.maxLength(200)]],
    identificationNumber: ['', [Validators.maxLength(50)]],
    phone: ['', [Validators.required, Validators.maxLength(50)]],
    email: ['', [Validators.email, Validators.maxLength(200)]],
    address: ['', [Validators.maxLength(300)]],
    alternateContact: ['', [Validators.maxLength(200)]],
    consentNotes: ['', [Validators.maxLength(1000)]],
  });

  ngOnInit(): void {
    this.loadOwners();
  }

  loadOwners(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.clinicalService.getOwners(this.search()).subscribe({
      next: (owners) => {
        this.owners.set(owners);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('No se pudieron cargar los propietarios.');
        this.isLoading.set(false);
      },
    });
  }

  updateSearch(value: string): void {
    this.search.set(value);
    this.loadOwners();
  }

  submit(): void {
    if (this.form.invalid || this.isSaving()) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);

    const value = this.form.getRawValue();
    this.clinicalService.createOwner({
      fullName: value.fullName!,
      identificationNumber: value.identificationNumber,
      phone: value.phone!,
      email: value.email,
      address: value.address,
      alternateContact: value.alternateContact,
      consentNotes: value.consentNotes,
    }).subscribe({
      next: () => {
        this.form.reset();
        this.isSaving.set(false);
        this.loadOwners();
      },
      error: () => {
        this.errorMessage.set('No se pudo guardar el propietario.');
        this.isSaving.set(false);
      },
    });
  }
}
