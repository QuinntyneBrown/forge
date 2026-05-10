import { Component, Input } from '@angular/core';
import { MatChipsModule } from '@angular/material/chips';

export type BadgeVariant = 'success' | 'warning' | 'error' | 'neutral';

@Component({
  selector: 'forge-badge',
  imports: [MatChipsModule],
  templateUrl: './badge.component.html',
  styleUrl: './badge.component.scss'
})
export class BadgeComponent {
  @Input() variant: BadgeVariant = 'neutral';
  @Input() testid: string | null = null;
}
