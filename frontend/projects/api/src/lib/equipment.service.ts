import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './auth.service';
import { IEquipmentService } from './equipment.service.contract';
import { EquipmentItem } from './models/equipment-type.model';

@Injectable()
export class EquipmentService implements IEquipmentService {
  constructor(
    private readonly http: HttpClient,
    @Inject(API_BASE_URL) private readonly baseUrl: string
  ) {}

  list(): Observable<EquipmentItem[]> {
    return this.http.get<EquipmentItem[]>(`${this.baseUrl}/api/equipment`);
  }
}
