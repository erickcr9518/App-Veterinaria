import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuditEntry } from '../models/audit.models';

@Injectable({ providedIn: 'root' })
export class AuditService {
  constructor(private readonly http: HttpClient) {}

  getAuditLog(fromUtc?: string, toUtc?: string): Observable<AuditEntry[]> {
    let params = new HttpParams();
    if (fromUtc) {
      params = params.set('fromUtc', fromUtc);
    }
    if (toUtc) {
      params = params.set('toUtc', toUtc);
    }

    return this.http.get<AuditEntry[]>(`${environment.apiUrl}/audit`, { params });
  }
}
