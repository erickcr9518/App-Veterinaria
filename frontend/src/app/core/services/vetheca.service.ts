import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { VethecaAskResult } from '../models/vetheca.models';

@Injectable({ providedIn: 'root' })
export class VethecaService {
  constructor(private readonly http: HttpClient) {}

  ask(question: string, maxResults = 5): Observable<VethecaAskResult> {
    return this.http.post<VethecaAskResult>(`${environment.apiUrl}/vetheca/ask`, { question, maxResults });
  }
}
