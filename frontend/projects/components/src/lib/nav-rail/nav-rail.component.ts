import { Component, Input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { NavDestination } from '../bottom-nav/bottom-nav.component';

@Component({
  selector: 'forge-nav-rail',
  imports: [RouterLink, RouterLinkActive, MatIconModule, MatListModule],
  templateUrl: './nav-rail.component.html',
  styleUrl: './nav-rail.component.scss'
})
export class NavRailComponent {
  @Input({ required: true }) destinations: NavDestination[] = [];
}
