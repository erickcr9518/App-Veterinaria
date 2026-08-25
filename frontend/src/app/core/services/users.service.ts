import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateUserRequest, UserSummary } from '../models/user.models';

@Injectable({ providedIn: 'root' })
export class UsersService {
  constructor(private readonly http: HttpClient) {}

  getUsers(clinicId?: string | null): Observable<UserSummary[]> {
    let params = new HttpParams();
    if (clinicId) {
      params = params.set('clinicId', clinicId);
    }

    return this.http.get<UserSummary[]>(`${environment.apiUrl}/users`, { params });
  }

  createUser(request: CreateUserRequest): Observable<string> {
    return this.http.post<string>(`${environment.apiUrl}/users`, request);
  }

  setUserActive(userId: string, isActive: boolean): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/users/${userId}/status`, { isActive });
  }
}
