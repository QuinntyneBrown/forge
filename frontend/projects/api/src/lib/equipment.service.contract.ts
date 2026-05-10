import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';
import { EquipmentItem } from './models/equipment-type.model';

export interface IEquipmentService {
  list(): Observable<EquipmentItem[]>;
}

export const EQUIPMENT_SERVICE = new InjectionToken<IEquipmentService>('IEquipmentService');
