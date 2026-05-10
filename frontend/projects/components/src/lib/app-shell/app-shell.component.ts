import { Component, Input, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { BreakpointObserver, BreakpointState } from '@angular/cdk/layout';
import { MatIconModule } from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';
import { BottomNavComponent, NavDestination } from '../bottom-nav/bottom-nav.component';
import { NavRailComponent } from '../nav-rail/nav-rail.component';

const RAIL_BREAKPOINT = '(min-width: 992px)';

@Component({
  selector: 'forge-app-shell',
  imports: [MatToolbarModule, MatIconModule, BottomNavComponent, NavRailComponent],
  templateUrl: './app-shell.component.html',
  styleUrl: './app-shell.component.scss'
})
export class AppShellComponent {
  @Input({ required: true }) destinations: NavDestination[] = [];
  @Input() title = 'Forge Fit';

  private readonly breakpoints = inject(BreakpointObserver);

  protected readonly isDesktop = toSignal(
    this.breakpoints.observe(RAIL_BREAKPOINT).pipe(takeUntilDestroyed()),
    { initialValue: { matches: false, breakpoints: {} } as BreakpointState }
  );

  protected readonly showNavRail = computed(() => this.isDesktop().matches);
  protected readonly showBottomNav = computed(() => !this.isDesktop().matches);
}
