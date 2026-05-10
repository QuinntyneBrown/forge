export interface CurrentUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  units: 'Imperial' | 'Metric';
  timeZoneId: string;
  dailyActiveCaloriesTarget: number;
  dailyWorkoutMinutesTarget: number;
  monthlyWeightGoalLb: number;
  morningWindowStart: string;
  morningWindowEnd: string;
  kitchenClosedStart: string;
  kitchenClosedEnd: string;
  kitchenNudgeEnabled: boolean;
  morningReminderEnabled: boolean;
  leaderboardOptIn: boolean;
}
