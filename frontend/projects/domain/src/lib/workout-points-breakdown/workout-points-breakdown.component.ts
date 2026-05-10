import { Component, Input, OnChanges, SimpleChanges, computed, signal } from '@angular/core';
import { Session } from 'api';
import { CardComponent } from 'components';

const BASE_POINTS_PER_MINUTE = 2;

interface BreakdownRow {
  reason: string;
  description: string;
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
    this.rows.set([
      {
        reason: 'Base',
        description: `${session.durationMinutes} min logged`,
        points: base
      }
    ]);
  }
}
