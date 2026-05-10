import { EquipmentType as EquipmentTypeId } from './session.model';

export type { EquipmentTypeId };

export interface EquipmentItem {
  id: EquipmentTypeId;
  name: string;
}
