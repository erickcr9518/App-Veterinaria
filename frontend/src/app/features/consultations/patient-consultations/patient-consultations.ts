import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { ClinicalService } from '../../../core/services/clinical.service';
import { AuthService } from '../../../core/services/auth.service';
import { ConsultationSummary, Patient } from '../../../core/models/clinical.models';

@Component({
  selector: 'app-patient-consultations',
  standalone: true,
  imports: [RouterLink, DatePipe],
  templateUrl: './patient-consultations.html',
  styleUrl: './patient-consultations.scss',
})
export class PatientConsultations implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly clinicalService = inject(ClinicalService);
  private readonly authService = inject(AuthService);

  readonly patientId = this.route.snapshot.paramMap.get('patientId')!;
  readonly patient = signal<Patient | null>(null);
  readonly consultations = signal<ConsultationSummary[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.clinicalService.getPatientById(this.patientId).subscribe({
      next: (patient) => this.patient.set(patient),
      error: () => this.errorMessage.set('No se pudo cargar el paciente.'),
    });

    this.clinicalService.getConsultationsByPatient(this.patientId).subscribe({
      next: (consultations) => {
        this.consultations.set(consultations);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('No se pudieron cargar las consultas.');
        this.isLoading.set(false);
      },
    });
  }

  canWrite(): boolean {
    return this.authService.hasPermission('consultations.write');
  }
}
