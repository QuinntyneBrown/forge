export interface UpdateProfileRequest {
  email: string;
  firstName: string;
  lastName: string;
  units: 'Imperial' | 'Metric';
  timeZoneId: string;
  dailyActiveCaloriesTarget: number;
  dailyWorkoutMinutesTarget: number;
}
