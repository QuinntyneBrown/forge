import { Component, Input } from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'forge-progress-ring',
  imports: [MatProgressSpinnerModule],
  templateUrl: './progress-ring.component.html',
  styleUrl: './progress-ring.component.scss'
})
export class ProgressRingComponent {
  @Input() value = 0;
  @Input() max = 100;
  @Input() diameter = 96;
  @Input() strokeWidth = 8;
  @Input() testid: string | null = null;

  get percentage(): number {
    if (this.max <= 0) {
      return 0;
    }
    const ratio = (this.value / this.max) * 100;
    if (ratio < 0) {
      return 0;
    }
    if (ratio > 100) {
      return 100;
    }
    return ratio;
  }
}
