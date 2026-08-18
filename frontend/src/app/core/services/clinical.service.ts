import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateOwnerRequest, CreatePatientRequest, Owner, Patient } from '../models/clinical.models';

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
}
