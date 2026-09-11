import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  VethecaAskResult,
  VethecaLibraryDocument,
  VethecaSavedSearchDetail,
  VethecaSavedSearchSummary,
} from '../models/vetheca.models';

@Injectable({ providedIn: 'root' })
export class VethecaService {
  constructor(private readonly http: HttpClient) {}

  ask(question: string, maxResults = 5): Observable<VethecaAskResult> {
    return this.http.post<VethecaAskResult>(`${environment.apiUrl}/vetheca/ask`, { question, maxResults });
  }

  saveSearch(id: string, title: string | null): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/vetheca/${id}/save`, { title });
  }

  unsaveSearch(id: string): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/vetheca/${id}/unsave`, {});
  }

  getSavedSearches(): Observable<VethecaSavedSearchSummary[]> {
    return this.http.get<VethecaSavedSearchSummary[]>(`${environment.apiUrl}/vetheca/saved`);
  }

  getSavedSearchById(id: string): Observable<VethecaSavedSearchDetail> {
    return this.http.get<VethecaSavedSearchDetail>(`${environment.apiUrl}/vetheca/saved/${id}`);
  }

  submitFeedback(id: string, helpful: boolean, note: string | null): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/vetheca/${id}/feedback`, { helpful, note });
  }

  getLibraryDocuments(): Observable<VethecaLibraryDocument[]> {
    return this.http.get<VethecaLibraryDocument[]>(`${environment.apiUrl}/vetheca/library`);
  }

  uploadLibraryDocument(title: string, file: File): Observable<VethecaLibraryDocument> {
    const formData = new FormData();
    formData.append('Title', title);
    formData.append('File', file);
    return this.http.post<VethecaLibraryDocument>(`${environment.apiUrl}/vetheca/library`, formData);
  }

  deleteLibraryDocument(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/vetheca/library/${id}`);
  }
}
