import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { ClinicsService } from '../../../core/services/clinics.service';
import { UsersService } from '../../../core/services/users.service';
import { Clinic } from '../../../core/models/clinic.models';
import { UserSummary } from '../../../core/models/user.models';

const ROLE_OPTIONS: { value: string; label: string }[] = [
  { value: 'Administrador', label: 'Administrador' },
  { value: 'Veterinario', label: 'Veterinario' },
  { value: 'Recepcion', label: 'Recepcion' },
  { value: 'SuperAdministrador', label: 'Superadministrador (plataforma)' },
];

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './users.html',
  styleUrl: './users.scss',
})
export class Users implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly usersService = inject(UsersService);
  private readonly clinicsService = inject(ClinicsService);

  readonly currentUser = this.authService.currentUser;
  readonly isPlatformAdmin = computed(() => !this.currentUser()?.clinicId);
  readonly roleOptions = computed(() =>
    this.isPlatformAdmin() ? ROLE_OPTIONS : ROLE_OPTIONS.filter((role) => role.value !== 'SuperAdministrador'),
  );

  readonly clinics = signal<Clinic[]>([]);
  readonly selectedClinicId = signal<string | null>(null);
  readonly users = signal<UserSummary[]>([]);
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly hasUsers = computed(() => this.users().length > 0);
  readonly listTitle = computed(() =>
    this.isPlatformAdmin() && !this.selectedClinicId() ? 'Cuentas de plataforma' : 'Personal de la clinica',
  );
  readonly emptyMessage = computed(() =>
    this.isPlatformAdmin() && !this.selectedClinicId()
      ? 'No hay cuentas de plataforma registradas todavia.'
      : 'No hay usuarios registrados todavia.',
  );

  readonly form = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.maxLength(200)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(200)]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    role: ['Veterinario', [Validators.required]],
  });

  ngOnInit(): void {
    if (this.isPlatformAdmin()) {
      this.clinicsService.getClinics().subscribe({
        next: (clinics) => this.clinics.set(clinics),
        error: () => this.errorMessage.set('No se pudieron cargar las clinicas.'),
      });
      this.loadUsers();
      return;
    }

    this.loadUsers();
  }

  selectClinic(clinicId: string): void {
    this.selectedClinicId.set(clinicId || null);
    this.loadUsers();
  }

  loadUsers(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.usersService.getUsers(this.isPlatformAdmin() ? this.selectedClinicId() : undefined).subscribe({
      next: (users) => {
        this.users.set(users);
        this.isLoading.set(false);
      },
      error: (error: unknown) => {
        this.errorMessage.set(this.getErrorMessage(error, 'No tienes permiso para ver usuarios.', 'No se pudieron cargar los usuarios.'));
        this.isLoading.set(false);
      },
    });
  }

  submit(): void {
    if (this.form.invalid || this.isSaving()) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const needsClinic = this.isPlatformAdmin() && value.role !== 'SuperAdministrador';
    const createsPlatformUser = this.isPlatformAdmin() && value.role === 'SuperAdministrador';
    if (needsClinic && !this.selectedClinicId()) {
      this.errorMessage.set('Selecciona una clinica antes de crear el usuario.');
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);

    this.usersService.createUser({
      fullName: value.fullName,
      email: value.email,
      password: value.password,
      role: value.role,
      clinicId: needsClinic ? this.selectedClinicId() : null,
    }).subscribe({
      next: () => {
        this.form.reset({ fullName: '', email: '', password: '', role: 'Veterinario' });
        if (createsPlatformUser) {
          this.selectedClinicId.set(null);
        }
        this.isSaving.set(false);
        this.loadUsers();
      },
      error: (error: unknown) => {
        this.errorMessage.set(this.getErrorMessage(error, 'No tienes permiso para crear usuarios.', 'No se pudo crear el usuario.'));
        this.isSaving.set(false);
      },
    });
  }

  isSelf(user: UserSummary): boolean {
    return user.userId === this.currentUser()?.userId;
  }

  toggleActive(user: UserSummary): void {
    if (this.isSelf(user)) {
      return;
    }

    this.usersService.setUserActive(user.userId, !user.isActive).subscribe({
      next: () => this.loadUsers(),
      error: (error: unknown) => {
        this.errorMessage.set(this.getErrorMessage(error, 'No tienes permiso para cambiar el estado de este usuario.', 'No se pudo actualizar el usuario.'));
      },
    });
  }

  private getErrorMessage(error: unknown, forbiddenMessage: string, fallbackMessage: string): string {
    return error instanceof HttpErrorResponse && error.status === 403
      ? forbiddenMessage
      : fallbackMessage;
  }
}
