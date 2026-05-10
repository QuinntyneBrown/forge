import { Component, Inject, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ISessionsService, SESSIONS_SERVICE, Session } from 'api';
import { AppShellComponent, ButtonComponent, NavDestination } from 'components';
import {
  WorkoutDetailFormComponent,
  WorkoutPointsBreakdownComponent
} from 'domain';

const DESTINATIONS: NavDestination[] = [
  { label: 'Home', icon: 'home', routerLink: '/dashboard' },
  { label: 'Workouts', icon: 'fitness_center', routerLink: '/workouts' },
  { label: 'Rewards', icon: 'redeem', routerLink: '/rewards' },
  { label: 'Profile', icon: 'person', routerLink: '/profile' }
];

@Component({
  selector: 'app-workout-detail-page',
  imports: [
    AppShellComponent,
    ButtonComponent,
    WorkoutDetailFormComponent,
    WorkoutPointsBreakdownComponent
  ],
  templateUrl: './workout-detail.page.html',
  styleUrl: './workout-detail.page.scss'
})
export class WorkoutDetailPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly destinations = DESTINATIONS;
  protected readonly session = signal<Session | null>(null);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly busy = signal(false);

  protected sessionId: string | null = null;

  constructor(@Inject(SESSIONS_SERVICE) private readonly sessions: ISessionsService) {}

  ngOnInit(): void {
    this.sessionId = this.route.snapshot.paramMap.get('id');
    if (!this.sessionId) {
      return;
    }
    this.sessions.getById(this.sessionId).subscribe({
      next: (session) => this.session.set(session),
      error: () => this.errorMessage.set('Could not load session.')
    });
  }

  protected onSaved(): void {
    this.router.navigate(['/workouts']);
  }

  protected onDuplicate(): void {
    if (!this.sessionId || this.busy()) {
      return;
    }
    this.busy.set(true);
    this.sessions.duplicate(this.sessionId).subscribe({
      next: () => {
        this.busy.set(false);
        this.router.navigate(['/workouts']);
      },
      error: () => {
        this.busy.set(false);
        this.errorMessage.set('Could not duplicate session.');
      }
    });
  }

  protected onDelete(): void {
    if (!this.sessionId || this.busy()) {
      return;
    }
    this.busy.set(true);
    this.sessions.delete(this.sessionId).subscribe({
      next: () => {
        this.busy.set(false);
        this.router.navigate(['/dashboard']);
      },
      error: () => {
        this.busy.set(false);
        this.errorMessage.set('Could not delete session.');
      }
    });
  }
}
