import { Component, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AppShellComponent, NavDestination } from 'components';
import { WorkoutDetailFormComponent } from 'domain';

const DESTINATIONS: NavDestination[] = [
  { label: 'Home', icon: 'home', routerLink: '/dashboard' },
  { label: 'Workouts', icon: 'fitness_center', routerLink: '/workouts' },
  { label: 'Rewards', icon: 'redeem', routerLink: '/rewards' },
  { label: 'Profile', icon: 'person', routerLink: '/profile' }
];

@Component({
  selector: 'app-workout-new-page',
  imports: [AppShellComponent, WorkoutDetailFormComponent],
  templateUrl: './workout-new.page.html',
  styleUrl: './workout-new.page.scss'
})
export class WorkoutNewPage {
  protected readonly destinations = DESTINATIONS;
  private readonly router = inject(Router);

  protected readonly today = computed(() =>
    new Date().toLocaleDateString(undefined, { weekday: 'long', month: 'long', day: 'numeric' })
  );

  // Default 22-min session at 2 pts/min — matches the mock's worked example
  // and reflects the form's default duration. Updated when the form emits
  // value changes (future enhancement).
  protected readonly basePoints = computed(() => 44);

  protected onCreated(_result: { id: string }): void {
    this.router.navigate(['/dashboard']);
  }

  protected onCancel(): void {
    this.router.navigate(['/dashboard']);
  }
}
