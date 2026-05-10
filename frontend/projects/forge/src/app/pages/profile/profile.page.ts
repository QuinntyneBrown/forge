import { Component, Inject, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { CurrentUser, IMeService, ME_SERVICE } from 'api';
import { AppShellComponent, NavDestination } from 'components';
import { ProfileFormComponent } from 'domain';
import { AuthStateService } from '../../auth-state.service';

const PRIMARY_DESTINATIONS: NavDestination[] = [
  { label: 'Home', icon: 'home', routerLink: '/dashboard' },
  { label: 'Workouts', icon: 'fitness_center', routerLink: '/workouts' },
  { label: 'Rewards', icon: 'redeem', routerLink: '/rewards' },
  { label: 'Profile', icon: 'person', routerLink: '/profile' }
];

@Component({
  selector: 'app-profile-page',
  imports: [AppShellComponent, ProfileFormComponent],
  templateUrl: './profile.page.html',
  styleUrl: './profile.page.scss'
})
export class ProfilePage implements OnInit {
  private readonly auth = inject(AuthStateService);
  private readonly router = inject(Router);
  private readonly meApi = inject<IMeService>(ME_SERVICE);

  protected readonly destinations = PRIMARY_DESTINATIONS;
  protected readonly currentUser = signal<CurrentUser | null>(null);

  protected readonly displayName = computed(() => {
    const u = this.currentUser();
    if (u?.firstName || u?.lastName) {
      return `${u.firstName ?? ''} ${u.lastName ?? ''}`.trim();
    }
    return u?.email?.split('@')[0] ?? 'You';
  });

  protected readonly email = computed(() =>
    this.currentUser()?.email ?? this.auth.snapshot()?.email ?? 'unknown'
  );

  protected readonly avatarInitials = computed(() => {
    const u = this.currentUser();
    const a = (u?.firstName ?? this.email().charAt(0)).charAt(0);
    const b = (u?.lastName ?? '').charAt(0);
    return `${a}${b}`.toUpperCase() || '?';
  });

  protected readonly memberSince = computed(() =>
    new Date().toLocaleDateString(undefined, { month: 'short', year: 'numeric' })
  );

  ngOnInit(): void {
    this.meApi.getMe().subscribe({
      next: (user) => this.currentUser.set(user),
      error: () => undefined
    });
  }

  protected onDeleted(): void {
    this.auth.clear();
    this.router.navigate(['/sign-in']);
  }

  protected onSignOut(): void {
    this.auth.clear();
    this.router.navigate(['/sign-in']);
  }
}
