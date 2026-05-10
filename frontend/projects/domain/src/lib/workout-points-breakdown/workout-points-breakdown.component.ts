import { Component, Input, OnChanges, SimpleChanges, computed, signal } from '@angular/core';
import { Session } from 'api';
import { CardComponent } from 'components';

const BASE_POINTS_PER_MINUTE = 2;
const MORNING_BONUS_HOUR = 7;
const MORNING_BONUS_POINTS = 25;
const STREAK_MULTIPLIER = 1.1;

interface BreakdownRow {
  reason: string;
  description: string;
  icon: string;
  points: number;
}

@Component({
  selector: 'forge-workout-points-breakdown',
  imports: [CardComponent],
  templateUrl: './workout-points-breakdown.component.html',
  styleUrl: './workout-points-breakdown.component.scss'
})
export class WorkoutPointsBreakdownComponent implements OnChanges {
  @Input() session: Session | null = null;

  protected readonly rows = signal<BreakdownRow[]>([]);
  protected readonly total = computed(() =>
    this.rows().reduce((sum, row) => sum + row.points, 0)
  );

  ngOnChanges(changes: SimpleChanges): void {
    if (!('session' in changes)) {
      return;
    }
    const session = this.session;
    if (!session) {
      this.rows.set([]);
      return;
    }
    const base = BASE_POINTS_PER_MINUTE * session.durationMinutes;
    const startHour = new Date(session.startedAt).getHours();
    const morningBonus = startHour < MORNING_BONUS_HOUR ? MORNING_BONUS_POINTS : 0;
    const streakBonus = Math.round((base + morningBonus) * (STREAK_MULTIPLIER - 1));

    const next: BreakdownRow[] = [
      {
        reason: 'Base',
        description: `${session.durationMinutes} min × 2 pts`,
        icon: 'timer',
        points: base
      },
      {
        reason: 'Morning bonus',
        description:
          morningBonus > 0
            ? 'Logged before 7:00 AM'
            : 'Outside the morning window',
        icon: 'wb_sunny',
        points: morningBonus
      },
      {
        reason: 'Streak multiplier',
        description: `Daily streak active (×${STREAK_MULTIPLIER.toFixed(2)})`,
        icon: 'local_fire_department',
        points: streakBonus
      }
    ];
    this.rows.set(next);
  }
}
