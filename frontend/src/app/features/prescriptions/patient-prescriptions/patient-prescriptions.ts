import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { ClinicalService } from '../../../core/services/clinical.service';
import { PrescriptionSummary, Patient } from '../../../core/models/clinical.models';

@Component({
  selector: 'app-patient-prescriptions',
  standalone: true,
  imports: [RouterLink, DatePipe],
  templateUrl: './patient-prescriptions.html',
  styleUrl: './patient-prescriptions.scss',
})
export class PatientPrescriptions implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly clinicalService = inject(ClinicalService);

  readonly patientId = this.route.snapshot.paramMap.get('patientId')!;
  readonly patient = signal<Patient | null>(null);
  readonly prescriptions = signal<PrescriptionSummary[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.clinicalService.getPatientById(this.patientId).subscribe({
      next: (patient) => this.patient.set(patient),
      error: () => this.errorMessage.set('No se pudo cargar el paciente.'),
    });

    this.clinicalService.getPrescriptionsByPatient(this.patientId).subscribe({
      next: (prescriptions) => {
        this.prescriptions.set(prescriptions);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('No se pudieron cargar las recetas.');
        this.isLoading.set(false);
      },
    });
  }
}
