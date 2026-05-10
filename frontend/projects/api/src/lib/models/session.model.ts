export type EquipmentType = 'Treadmill' | 'IndoorBike' | 'BenchPress' | 'Elliptical';

export interface Session {
  id: string;
  equipment: EquipmentType;
  startedAt: string;
  durationMinutes: number;
  distanceMiles: number | null;
  avgHeartRateBpm: number | null;
  activeCalories: number;
  notes: string | null;
  createdAt: string;
}

export interface SessionPage {
  items: Session[];
  page: number;
  pageSize: number;
  total: number;
}
