import { Component, DestroyRef, Inject, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ILeaderboardService, LEADERBOARD_SERVICE, LeaderboardEntry } from 'api';
import { CardComponent } from 'components';

@Component({
  selector: 'forge-leaderboard-card',
  imports: [CardComponent],
  templateUrl: './leaderboard-card.component.html',
  styleUrl: './leaderboard-card.component.scss'
})
export class LeaderboardCardComponent implements OnInit {
  protected readonly entries = signal<LeaderboardEntry[]>([]);
  protected readonly errored = signal(false);
  private readonly destroyRef = inject(DestroyRef);

  constructor(@Inject(LEADERBOARD_SERVICE) private readonly leaderboard: ILeaderboardService) {}

  ngOnInit(): void {
    this.leaderboard.list(1, 5).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (rows) => this.entries.set(rows),
      error: () => this.errored.set(true)
    });
  }
}
