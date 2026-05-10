import { Component, computed, Inject, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AUTH_SERVICE, IAuthService } from 'api';
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

  constructor(@Inject(AUTH_SERVICE) private readonly authApi: IAuthService) {}

  protected signOut(): void {
    const refreshToken = this.auth.refreshToken;
    const finalize = (): void => {
      this.auth.clear();
      this.router.navigate(['/sign-in']);
    };

    if (!refreshToken) {
      finalize();
      return;
    }

    this.authApi.signOut(refreshToken).subscribe({
      next: () => finalize(),
      error: () => finalize()
    });
  }
}
