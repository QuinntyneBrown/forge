import { HttpClient, HttpParams } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './auth.service';
import { CreateSessionRequest } from './models/create-session-request.model';
import { Session, SessionPage } from './models/session.model';
import { SessionListQuery } from './models/session-list-query.model';
import { UpdateSessionRequest } from './models/update-session-request.model';
import { ISessionsService } from './sessions.service.contract';

@Injectable()
export class SessionsService implements ISessionsService {
  constructor(
    private readonly http: HttpClient,
    @Inject(API_BASE_URL) private readonly baseUrl: string
  ) {}

  list(query: SessionListQuery = {}): Observable<SessionPage> {
    let params = new HttpParams();
    if (query.equipment) params = params.set('equipment', query.equipment);
    if (query.range) {
      params = params.set('range', query.range.charAt(0).toUpperCase() + query.range.slice(1));
    }
    if (query.search) params = params.set('search', query.search);
    if (query.page !== undefined) params = params.set('page', query.page.toString());
    if (query.pageSize !== undefined) params = params.set('pageSize', query.pageSize.toString());
    return this.http.get<SessionPage>(`${this.baseUrl}/api/sessions`, { params });
  }

  getById(id: string): Observable<Session> {
    return this.http.get<Session>(`${this.baseUrl}/api/sessions/${id}`);
  }

  create(request: CreateSessionRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.baseUrl}/api/sessions`, request);
  }

  update(id: string, request: UpdateSessionRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/api/sessions/${id}`, request);
  }

  duplicate(id: string): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.baseUrl}/api/sessions/${id}/duplicate`, {});
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/api/sessions/${id}`);
  }
}
