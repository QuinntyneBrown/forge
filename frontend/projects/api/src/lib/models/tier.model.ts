export interface Tier {
  name: string;
  lifetimePoints: number;
  nextTierName: string | null;
  pointsToNextTier: number | null;
}
