import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Clinic } from '../models/clinic.models';

@Injectable({ providedIn: 'root' })
export class ClinicsService {
  constructor(private readonly http: HttpClient) {}

  getClinics(): Observable<Clinic[]> {
    return this.http.get<Clinic[]>(`${environment.apiUrl}/clinics`);
  }
}
