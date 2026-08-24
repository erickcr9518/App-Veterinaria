import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AmendConsultationRequest,
  Appointment,
  AppointmentRequest,
  AppointmentStatus,
  ConsultationDetail,
  ConsultationSummary,
  CreateConsultationRequest,
  CreateOwnerRequest,
  CreatePatientRequest,
  Owner,
  Patient,
  UpdateConsultationRequest,
} from '../models/clinical.models';

@Injectable({ providedIn: 'root' })
export class ClinicalService {
  constructor(private readonly http: HttpClient) {}

  getOwners(search?: string): Observable<Owner[]> {
    let params = new HttpParams();
    if (search?.trim()) {
      params = params.set('search', search.trim());
    }

    return this.http.get<Owner[]>(`${environment.apiUrl}/owners`, { params });
  }

  createOwner(request: CreateOwnerRequest): Observable<string> {
    return this.http.post<string>(`${environment.apiUrl}/owners`, request);
  }

  updateOwner(id: string, request: CreateOwnerRequest): Observable<void> {
    return this.http.put<void>(`${environment.apiUrl}/owners/${id}`, request);
  }

  getPatients(filters: { search?: string; ownerId?: string; species?: string } = {}): Observable<Patient[]> {
    let params = new HttpParams();
    if (filters.search?.trim()) {
      params = params.set('search', filters.search.trim());
    }
    if (filters.ownerId) {
      params = params.set('ownerId', filters.ownerId);
    }
    if (filters.species) {
      params = params.set('species', filters.species);
    }

    return this.http.get<Patient[]>(`${environment.apiUrl}/patients`, { params });
  }

  createPatient(request: CreatePatientRequest): Observable<string> {
    return this.http.post<string>(`${environment.apiUrl}/patients`, request);
  }

  updatePatient(id: string, request: CreatePatientRequest): Observable<void> {
    return this.http.put<void>(`${environment.apiUrl}/patients/${id}`, request);
  }

  getPatientById(id: string): Observable<Patient> {
    return this.http.get<Patient>(`${environment.apiUrl}/patients/${id}`);
  }

  getConsultationsByPatient(patientId: string): Observable<ConsultationSummary[]> {
    return this.http.get<ConsultationSummary[]>(`${environment.apiUrl}/patients/${patientId}/consultations`);
  }

  getConsultationById(id: string): Observable<ConsultationDetail> {
    return this.http.get<ConsultationDetail>(`${environment.apiUrl}/consultations/${id}`);
  }

  createConsultation(request: CreateConsultationRequest): Observable<string> {
    return this.http.post<string>(`${environment.apiUrl}/consultations`, request);
  }

  updateConsultation(id: string, request: UpdateConsultationRequest): Observable<void> {
    return this.http.put<void>(`${environment.apiUrl}/consultations/${id}`, request);
  }

  finalizeConsultation(id: string): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/consultations/${id}/finalize`, {});
  }

  amendConsultation(id: string, request: AmendConsultationRequest): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/consultations/${id}/amend`, request);
  }

  getAppointments(filters: {
    fromUtc: string;
    toUtc: string;
    patientId?: string;
    assignedVeterinarianUserId?: string;
    status?: AppointmentStatus | '';
  }): Observable<Appointment[]> {
    let params = new HttpParams()
      .set('fromUtc', filters.fromUtc)
      .set('toUtc', filters.toUtc);

    if (filters.patientId) {
      params = params.set('patientId', filters.patientId);
    }
    if (filters.assignedVeterinarianUserId) {
      params = params.set('assignedVeterinarianUserId', filters.assignedVeterinarianUserId);
    }
    if (filters.status) {
      params = params.set('status', filters.status);
    }

    return this.http.get<Appointment[]>(`${environment.apiUrl}/appointments`, { params });
  }

  getAppointmentById(id: string): Observable<Appointment> {
    return this.http.get<Appointment>(`${environment.apiUrl}/appointments/${id}`);
  }

  createAppointment(request: AppointmentRequest): Observable<string> {
    return this.http.post<string>(`${environment.apiUrl}/appointments`, request);
  }

  updateAppointment(id: string, request: AppointmentRequest): Observable<void> {
    return this.http.put<void>(`${environment.apiUrl}/appointments/${id}`, request);
  }

  changeAppointmentStatus(id: string, status: AppointmentStatus, reason?: string | null): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/appointments/${id}/status`, { status, reason });
  }
}
