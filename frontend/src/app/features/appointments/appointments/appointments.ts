import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Observable } from 'rxjs';
import { Appointment, AppointmentRequest, AppointmentStatus, Patient } from '../../../core/models/clinical.models';
import { AuthService } from '../../../core/services/auth.service';
import { ClinicalService } from '../../../core/services/clinical.service';

type ViewMode = 'day' | 'week';

@Component({
  selector: 'app-appointments',
  standalone: true,
  imports: [ReactiveFormsModule, DatePipe],
  templateUrl: './appointments.html',
  styleUrl: './appointments.scss',
})
export class Appointments implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly clinicalService = inject(ClinicalService);
  private readonly authService = inject(AuthService);

  readonly appointments = signal<Appointment[]>([]);
  readonly patients = signal<Patient[]>([]);
  readonly viewMode = signal<ViewMode>('day');
  readonly anchorDate = signal(this.startOfDay(new Date()));
  readonly selectedStatus = signal<AppointmentStatus | ''>('');
  readonly editingAppointment = signal<Appointment | null>(null);
  readonly statusTarget = signal<{ appointment: Appointment; status: AppointmentStatus } | null>(null);
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly isChangingStatus = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly canWrite = computed(() =>
    this.authService.hasPermission('appointments.write') || this.authService.hasPermission('appointments.write.own'));
  readonly hasAppointments = computed(() => this.appointments().length > 0);
  readonly formTitle = computed(() => this.editingAppointment() ? 'Editar cita' : 'Nueva cita');
  readonly submitLabel = computed(() => this.editingAppointment() ? 'Guardar cambios' : 'Crear cita');
  readonly rangeLabel = computed(() => {
    const { from, to } = this.currentRange();
    if (this.viewMode() === 'day') {
      return this.formatDate(from);
    }

    const inclusiveEnd = new Date(to);
    inclusiveEnd.setDate(inclusiveEnd.getDate() - 1);
    return `${this.formatDate(from)} - ${this.formatDate(inclusiveEnd)}`;
  });

  readonly form = this.fb.group({
    patientId: ['', [Validators.required]],
    startsAtLocal: ['', [Validators.required]],
    endsAtLocal: ['', [Validators.required]],
    visitType: ['Consulta', [Validators.required, Validators.maxLength(80)]],
    reason: ['', [Validators.required, Validators.maxLength(500)]],
    notes: ['', [Validators.maxLength(1000)]],
    reminderChannel: [''],
    reminderNotes: ['', [Validators.maxLength(500)]],
  });

  readonly statusForm = this.fb.group({
    reason: [''],
  });

  ngOnInit(): void {
    this.loadPatients();
    this.resetForm();
    const patientId = this.route.snapshot.queryParamMap.get('patientId');
    if (patientId) {
      this.form.patchValue({ patientId });
    }
    this.loadAppointments();
  }

  loadPatients(): void {
    this.clinicalService.getPatients().subscribe({
      next: (patients) => this.patients.set(patients),
      error: () => this.errorMessage.set('No se pudieron cargar los pacientes.'),
    });
  }

  loadAppointments(): void {
    const { from, to } = this.currentRange();
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.clinicalService.getAppointments({
      fromUtc: from.toISOString(),
      toUtc: to.toISOString(),
      status: this.selectedStatus(),
    }).subscribe({
      next: (appointments) => {
        this.appointments.set(appointments);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('No se pudo cargar la agenda.');
        this.isLoading.set(false);
      },
    });
  }

  setViewMode(mode: ViewMode): void {
    this.viewMode.set(mode);
    this.loadAppointments();
  }

  move(delta: number): void {
    const next = new Date(this.anchorDate());
    next.setDate(next.getDate() + (this.viewMode() === 'day' ? delta : delta * 7));
    this.anchorDate.set(this.startOfDay(next));
    this.loadAppointments();
  }

  goToday(): void {
    this.anchorDate.set(this.startOfDay(new Date()));
    this.loadAppointments();
  }

  updateStatusFilter(status: string): void {
    this.selectedStatus.set(status as AppointmentStatus | '');
    this.loadAppointments();
  }

  submit(): void {
    if (this.form.invalid || this.isSaving()) {
      this.form.markAllAsTouched();
      return;
    }

    const request = this.buildRequest();
    const editingAppointment = this.editingAppointment();
    const save$: Observable<string | void> = editingAppointment
      ? this.clinicalService.updateAppointment(editingAppointment.id, request)
      : this.clinicalService.createAppointment(request);

    this.isSaving.set(true);
    this.errorMessage.set(null);
    save$.subscribe({
      next: () => {
        this.resetForm();
        this.isSaving.set(false);
        this.loadAppointments();
      },
      error: () => {
        this.errorMessage.set('No se pudo guardar la cita.');
        this.isSaving.set(false);
      },
    });
  }

  editAppointment(appointment: Appointment): void {
    this.editingAppointment.set(appointment);
    this.form.reset({
      patientId: appointment.patientId,
      startsAtLocal: this.toLocalInputValue(appointment.startsAtUtc),
      endsAtLocal: this.toLocalInputValue(appointment.endsAtUtc),
      visitType: appointment.visitType,
      reason: appointment.reason,
      notes: appointment.notes ?? '',
      reminderChannel: appointment.reminderChannel ?? '',
      reminderNotes: appointment.reminderNotes ?? '',
    });
  }

  cancelEdit(): void {
    this.resetForm();
  }

  prepareStatusChange(appointment: Appointment, status: AppointmentStatus): void {
    this.statusTarget.set({ appointment, status });
    this.statusForm.reset({ reason: '' });
  }

  cancelStatusChange(): void {
    this.statusTarget.set(null);
    this.statusForm.reset({ reason: '' });
  }

  submitStatusChange(): void {
    const target = this.statusTarget();
    if (!target || this.isChangingStatus()) {
      return;
    }

    const reason = this.statusForm.getRawValue().reason?.trim() || null;
    if ((target.status === 'Cancelled' || target.status === 'NoShow') && !reason) {
      this.statusForm.markAllAsTouched();
      this.errorMessage.set('Indica la razon para cancelar o marcar como no asistio.');
      return;
    }

    this.isChangingStatus.set(true);
    this.errorMessage.set(null);
    this.clinicalService.changeAppointmentStatus(target.appointment.id, target.status, reason).subscribe({
      next: () => {
        this.isChangingStatus.set(false);
        this.cancelStatusChange();
        this.loadAppointments();
      },
      error: () => {
        this.errorMessage.set('No se pudo cambiar el estado de la cita.');
        this.isChangingStatus.set(false);
      },
    });
  }

  statusLabel(status: AppointmentStatus): string {
    const labels: Record<AppointmentStatus, string> = {
      Scheduled: 'Programada',
      Confirmed: 'Confirmada',
      Cancelled: 'Cancelada',
      Completed: 'Completada',
      NoShow: 'No asistio',
    };
    return labels[status];
  }

  private buildRequest(): AppointmentRequest {
    const value = this.form.getRawValue();
    return {
      patientId: value.patientId!,
      assignedVeterinarianUserId: null,
      startsAtUtc: new Date(value.startsAtLocal!).toISOString(),
      endsAtUtc: new Date(value.endsAtLocal!).toISOString(),
      visitType: value.visitType!,
      reason: value.reason!,
      notes: value.notes,
      reminderChannel: value.reminderChannel,
      reminderNotes: value.reminderNotes,
    };
  }

  private resetForm(): void {
    const start = new Date();
    start.setMinutes(start.getMinutes() < 30 ? 30 : 60, 0, 0);
    const end = new Date(start);
    end.setMinutes(end.getMinutes() + 30);

    this.editingAppointment.set(null);
    this.form.reset({
      startsAtLocal: this.toLocalInputValue(start.toISOString()),
      endsAtLocal: this.toLocalInputValue(end.toISOString()),
      visitType: 'Consulta',
    });
  }

  private currentRange(): { from: Date; to: Date } {
    const from = this.startOfDay(this.anchorDate());
    const to = new Date(from);
    to.setDate(to.getDate() + (this.viewMode() === 'day' ? 1 : 7));
    return { from, to };
  }

  private startOfDay(date: Date): Date {
    const value = new Date(date);
    value.setHours(0, 0, 0, 0);
    return value;
  }

  private toLocalInputValue(value: string): string {
    const date = new Date(value);
    const offsetDate = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
    return offsetDate.toISOString().slice(0, 16);
  }

  private formatDate(date: Date): string {
    return new Intl.DateTimeFormat('es-CR', {
      weekday: 'short',
      day: '2-digit',
      month: 'short',
      year: 'numeric',
    }).format(date);
  }
}
