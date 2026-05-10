export interface NextRewardDto {
  id: string;
  name: string;
  costPoints: number;
}

export interface DashboardSummary {
  caloriesToday: number;
  targetCalories: number;
  minutesToday: number;
  targetMinutes: number;
  currentStreak: number;
  currentBalance: number;
  lifetimePoints: number;
  tier: string;
  nextRewardWithinReach: NextRewardDto | null;
  monthToDateWeightLossLb: number;
  monthlyWeightGoalLb: number;
}
