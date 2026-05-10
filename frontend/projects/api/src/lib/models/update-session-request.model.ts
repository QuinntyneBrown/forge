import { EquipmentType } from './session.model';

export interface UpdateSessionRequest {
  equipment: EquipmentType;
  startedAt: string;
  durationMinutes: number;
  distanceMiles: number | null;
  avgHeartRateBpm: number | null;
  activeCalories: number;
  notes: string | null;
}
