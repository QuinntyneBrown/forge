export interface Reward {
  id: string;
  name: string;
  description: string;
  costPoints: number;
  sortOrder: number;
}

export interface RedemptionResult {
  redemptionId: string;
  remainingBalance: number;
}
