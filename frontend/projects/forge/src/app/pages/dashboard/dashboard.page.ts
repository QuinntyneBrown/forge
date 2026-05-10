import { Component, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { HealthBadgeComponent } from 'domain';
import { AuthStateService } from '../../auth-state.service';

@Component({
  selector: 'app-dashboard-page',
  imports: [HealthBadgeComponent],
  templateUrl: './dashboard.page.html',
  styleUrl: './dashboard.page.scss'
})
export class DashboardPage {
  private readonly auth = inject(AuthStateService);
  private readonly router = inject(Router);

  protected readonly email = computed(() => this.auth.snapshot()?.email ?? 'unknown');
  protected readonly role = computed(() => this.auth.snapshot()?.role ?? 'unknown');

  constructor() {
    if (!this.auth.snapshot()) {
      this.router.navigate(['/sign-in']);
    }
  }

  protected signOut(): void {
    this.auth.clear();
    this.router.navigate(['/sign-in']);
  }
}
