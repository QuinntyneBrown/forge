import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateSessionRequest } from './models/create-session-request.model';
import { Session, SessionPage } from './models/session.model';
import { SessionListQuery } from './models/session-list-query.model';
import { UpdateSessionRequest } from './models/update-session-request.model';

export interface ISessionsService {
  list(query?: SessionListQuery): Observable<SessionPage>;
  getById(id: string): Observable<Session>;
  create(request: CreateSessionRequest): Observable<{ id: string }>;
  update(id: string, request: UpdateSessionRequest): Observable<void>;
  duplicate(id: string): Observable<{ id: string }>;
  delete(id: string): Observable<void>;
}

export const SESSIONS_SERVICE = new InjectionToken<ISessionsService>('ISessionsService');
