import { EquipmentType } from './session.model';

export type SessionRange = 'all' | 'today' | 'week' | 'month';

export interface SessionListQuery {
  equipment?: EquipmentType;
  range?: SessionRange;
  search?: string;
  page?: number;
  pageSize?: number;
}
