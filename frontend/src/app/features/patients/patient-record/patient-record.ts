import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  Appointment,
  AppointmentStatus,
  ConsultationSummary,
  Patient,
  PrescriptionSummary,
} from '../../../core/models/clinical.models';
import { AuthService } from '../../../core/services/auth.service';
import { ClinicalService } from '../../../core/services/clinical.service';

@Component({
  selector: 'app-patient-record',
  standalone: true,
  imports: [DatePipe, RouterLink],
  templateUrl: './patient-record.html',
  styleUrl: './patient-record.scss',
})
export class PatientRecord implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly clinicalService = inject(ClinicalService);
  private readonly authService = inject(AuthService);

  readonly patientId = this.route.snapshot.paramMap.get('patientId')!;
  readonly patient = signal<Patient | null>(null);
  readonly consultations = signal<ConsultationSummary[]>([]);
  readonly appointments = signal<Appointment[]>([]);
  readonly prescriptions = signal<PrescriptionSummary[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  readonly canWriteConsultations = computed(() => this.authService.hasPermission('consultations.write'));
  readonly canReadAppointments = computed(() => this.authService.hasPermission('appointments.read'));
  readonly canSchedule = computed(() =>
    this.authService.hasPermission('appointments.write') || this.authService.hasPermission('appointments.write.own'));

  readonly nextAppointments = computed(() =>
    this.appointments()
      .filter((appointment) => appointment.status !== 'Cancelled' && appointment.status !== 'NoShow')
      .slice(0, 5));

  readonly latestConsultations = computed(() => this.consultations().slice(0, 5));
  readonly latestPrescriptions = computed(() => this.prescriptions().slice(0, 5));
  readonly hasClinicalAlerts = computed(() => {
    const patient = this.patient();
    return !!(patient?.allergies || patient?.chronicDiseases || patient?.currentMedications);
  });

  ngOnInit(): void {
    this.loadRecord();
  }

  loadRecord(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    let pendingRequests = this.canReadAppointments() ? 4 : 3;

    const finish = () => {
      pendingRequests -= 1;
      if (pendingRequests === 0) {
        this.isLoading.set(false);
      }
    };

    this.clinicalService.getPatientById(this.patientId).subscribe({
      next: (patient) => {
        this.patient.set(patient);
        finish();
      },
      error: () => {
        this.errorMessage.set('No se pudo cargar el paciente.');
        finish();
      },
    });

    this.clinicalService.getConsultationsByPatient(this.patientId).subscribe({
      next: (consultations) => {
        this.consultations.set(consultations);
        finish();
      },
      error: () => {
        this.errorMessage.set('No se pudieron cargar las consultas del expediente.');
        finish();
      },
    });

    this.clinicalService.getPrescriptionsByPatient(this.patientId).subscribe({
      next: (prescriptions) => {
        this.prescriptions.set(prescriptions);
        finish();
      },
      error: () => {
        this.errorMessage.set('No se pudieron cargar las recetas del expediente.');
        finish();
      },
    });

    if (this.canReadAppointments()) {
      const from = this.startOfToday();
      const to = new Date(from);
      to.setDate(to.getDate() + 90);

      this.clinicalService.getAppointments({
        fromUtc: from.toISOString(),
        toUtc: to.toISOString(),
        patientId: this.patientId,
      }).subscribe({
        next: (appointments) => {
          this.appointments.set(appointments);
          finish();
        },
        error: () => {
          this.errorMessage.set('No se pudieron cargar las citas del expediente.');
          finish();
        },
      });
    }
  }

  consultationStatusLabel(status: string): string {
    return status === 'Draft' ? 'Borrador' : 'Finalizada';
  }

  prescriptionStatusLabel(status: string): string {
    return status === 'Draft' ? 'Borrador' : 'Emitida';
  }

  appointmentStatusLabel(status: AppointmentStatus): string {
    const labels: Record<AppointmentStatus, string> = {
      Scheduled: 'Programada',
      Confirmed: 'Confirmada',
      Cancelled: 'Cancelada',
      Completed: 'Completada',
      NoShow: 'No asistio',
    };
    return labels[status];
  }

  private startOfToday(): Date {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return today;
  }
}
