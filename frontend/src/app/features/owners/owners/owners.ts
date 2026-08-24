import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable, Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { ClinicalService } from '../../../core/services/clinical.service';
import { Owner } from '../../../core/models/clinical.models';
import { AuthService } from '../../../core/services/auth.service';

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
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly searchChanged = new Subject<string>();

  readonly owners = signal<Owner[]>([]);
  readonly search = signal('');
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly editingOwner = signal<Owner | null>(null);
  readonly formTitle = computed(() => this.editingOwner() ? 'Editar propietario' : 'Nuevo propietario');
  readonly submitLabel = computed(() => this.editingOwner() ? 'Guardar cambios' : 'Guardar propietario');
  readonly hasOwners = computed(() => this.owners().length > 0);
  readonly canWrite = computed(() => this.authService.hasPermission('owners.write'));

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
    this.searchChanged.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe((value) => {
      this.search.set(value);
      this.loadOwners();
    });

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
      error: (error: unknown) => {
        this.errorMessage.set(this.getErrorMessage(
          error,
          'No tienes permiso para ver propietarios.',
          'No se pudieron cargar los propietarios.',
        ));
        this.isLoading.set(false);
      },
    });
  }

  updateSearch(value: string): void {
    this.searchChanged.next(value);
  }

  submit(): void {
    if (this.form.invalid || this.isSaving()) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);

    const value = this.form.getRawValue();
    const request = {
      fullName: value.fullName!,
      identificationNumber: value.identificationNumber,
      phone: value.phone!,
      email: value.email,
      address: value.address,
      alternateContact: value.alternateContact,
      consentNotes: value.consentNotes,
    };

    const editingOwner = this.editingOwner();
    const save$: Observable<string | void> = editingOwner
      ? this.clinicalService.updateOwner(editingOwner.id, request)
      : this.clinicalService.createOwner(request);

    save$.subscribe({
      next: () => {
        this.resetForm();
        this.isSaving.set(false);
        this.loadOwners();
      },
      error: (error: unknown) => {
        this.errorMessage.set(this.getErrorMessage(
          error,
          'No tienes permiso para guardar propietarios.',
          'No se pudo guardar el propietario.',
        ));
        this.isSaving.set(false);
      },
    });
  }

  editOwner(owner: Owner): void {
    if (!this.canWrite()) {
      return;
    }

    this.editingOwner.set(owner);
    this.form.reset({
      fullName: owner.fullName,
      identificationNumber: owner.identificationNumber ?? '',
      phone: owner.phone,
      email: owner.email ?? '',
      address: owner.address ?? '',
      alternateContact: owner.alternateContact ?? '',
      consentNotes: owner.consentNotes ?? '',
    });
  }

  cancelEdit(): void {
    this.resetForm();
  }

  private resetForm(): void {
    this.editingOwner.set(null);
    this.form.reset();
  }

  private getErrorMessage(error: unknown, forbiddenMessage: string, fallbackMessage: string): string {
    return error instanceof HttpErrorResponse && error.status === 403
      ? forbiddenMessage
      : fallbackMessage;
  }
}
