import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { ClinicsService } from '../../../core/services/clinics.service';
import { UsersService } from '../../../core/services/users.service';
import { Clinic } from '../../../core/models/clinic.models';
import { UserSummary } from '../../../core/models/user.models';

const PLATFORM_ROLE = 'SuperAdministrador';
const DEFAULT_CLINIC_ROLE = 'Veterinario';

const ROLE_OPTIONS: { value: string; label: string }[] = [
  { value: 'Administrador', label: 'Administrador' },
  { value: DEFAULT_CLINIC_ROLE, label: 'Veterinario' },
  { value: 'Recepcion', label: 'Recepcion' },
  { value: PLATFORM_ROLE, label: 'Superadministrador (plataforma)' },
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
  readonly isPlatformAdmin = computed(() => this.currentUser()?.roles.includes(PLATFORM_ROLE) ?? false);
  readonly roleOptions = computed(() =>
    this.isPlatformAdmin() ? ROLE_OPTIONS : ROLE_OPTIONS.filter((role) => role.value !== 'SuperAdministrador'),
  );

  readonly clinics = signal<Clinic[]>([]);
  readonly selectedClinicId = signal<string | null>(null);
  readonly users = signal<UserSummary[]>([]);
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly editingRolesUserId = signal<string | null>(null);
  readonly editingRoles = signal<string[]>([]);
  readonly isUpdatingRoles = signal(false);
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
    roles: this.fb.nonNullable.control<string[]>([DEFAULT_CLINIC_ROLE], [Validators.required]),
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
    if (this.isSaving()) {
      return;
    }

    const value = this.form.getRawValue();
    const roles = this.normalizeRoles(value.roles);
    if (roles.length === 0) {
      this.form.markAllAsTouched();
      this.errorMessage.set('Selecciona al menos un rol.');
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const createsPlatformUser = this.isPlatformAdmin() && roles.includes(PLATFORM_ROLE);
    const needsClinic = this.isPlatformAdmin() && !createsPlatformUser;
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
      role: roles[0],
      roles,
      clinicId: needsClinic ? this.selectedClinicId() : null,
    }).subscribe({
      next: () => {
        this.form.reset({ fullName: '', email: '', password: '', roles: [DEFAULT_CLINIC_ROLE] });
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

  canEditRoles(user: UserSummary): boolean {
    return !this.isSelf(user) && !this.userHasPlatformRole(user);
  }

  startRoleEdit(user: UserSummary): void {
    if (!this.canEditRoles(user)) {
      return;
    }

    this.editingRolesUserId.set(user.userId);
    this.editingRoles.set(this.normalizeClinicRoles(user.roles?.length ? user.roles : [user.role]));
    this.errorMessage.set(null);
  }

  cancelRoleEdit(): void {
    this.editingRolesUserId.set(null);
    this.editingRoles.set([]);
  }

  isEditingRoles(user: UserSummary): boolean {
    return this.editingRolesUserId() === user.userId;
  }

  isEditableRoleSelected(role: string): boolean {
    return this.editingRoles().includes(role);
  }

  toggleEditableRole(role: string, checked: boolean): void {
    const selected = new Set(this.editingRoles());

    if (checked) {
      selected.add(role);
    } else {
      selected.delete(role);
    }

    this.editingRoles.set(this.clinicRoleOptions()
      .map((option) => option.value)
      .filter((value) => selected.has(value)));
  }

  saveRoleEdit(user: UserSummary): void {
    const roles = this.normalizeClinicRoles(this.editingRoles());
    if (roles.length === 0) {
      this.errorMessage.set('Selecciona al menos un rol.');
      return;
    }

    this.isUpdatingRoles.set(true);
    this.errorMessage.set(null);

    this.usersService.updateUserRoles(user.userId, {
      role: roles[0],
      roles,
    }).subscribe({
      next: () => {
        this.isUpdatingRoles.set(false);
        this.cancelRoleEdit();
        this.loadUsers();
      },
      error: (error: unknown) => {
        this.errorMessage.set(this.getErrorMessage(error, 'No tienes permiso para cambiar roles.', 'No se pudieron cambiar los roles.'));
        this.isUpdatingRoles.set(false);
      },
    });
  }

  clinicRoleOptions(): { value: string; label: string }[] {
    return ROLE_OPTIONS.filter((role) => role.value !== PLATFORM_ROLE);
  }

  isRoleSelected(role: string): boolean {
    return this.form.controls.roles.value.includes(role);
  }

  toggleRole(role: string, checked: boolean): void {
    const selected = new Set(this.form.controls.roles.value);

    if (checked && role === PLATFORM_ROLE) {
      this.form.controls.roles.setValue([PLATFORM_ROLE]);
      return;
    }

    selected.delete(PLATFORM_ROLE);
    if (checked) {
      selected.add(role);
    } else {
      selected.delete(role);
    }

    this.form.controls.roles.setValue(this.roleOptions()
      .map((option) => option.value)
      .filter((value) => selected.has(value)));
  }

  roleLabels(user: UserSummary): string {
    const roles = user.roles?.length ? user.roles : [user.role];
    return roles.map((role) => this.roleLabel(role)).join(' + ');
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

  private normalizeRoles(roles: string[]): string[] {
    return this.roleOptions()
      .map((option) => option.value)
      .filter((role) => roles.includes(role));
  }

  private normalizeClinicRoles(roles: string[]): string[] {
    return this.clinicRoleOptions()
      .map((option) => option.value)
      .filter((role) => roles.includes(role));
  }

  private roleLabel(role: string): string {
    return ROLE_OPTIONS.find((option) => option.value === role)?.label ?? role;
  }

  private userHasPlatformRole(user: UserSummary): boolean {
    return (user.roles?.length ? user.roles : [user.role]).includes(PLATFORM_ROLE);
  }
}
