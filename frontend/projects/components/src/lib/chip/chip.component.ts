import { Component, Input } from '@angular/core';
import { MatChipsModule } from '@angular/material/chips';

@Component({
  selector: 'forge-chip',
  imports: [MatChipsModule],
  templateUrl: './chip.component.html',
  styleUrl: './chip.component.scss'
})
export class ChipComponent {
  @Input() selected = false;
  @Input() disabled = false;
  @Input() value: string | number | null = null;
  @Input() testid: string | null = null;
}
