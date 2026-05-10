import { Component, EventEmitter, Input, Output } from '@angular/core';
import { MatSlideToggleChange, MatSlideToggleModule } from '@angular/material/slide-toggle';

@Component({
  selector: 'forge-switch',
  imports: [MatSlideToggleModule],
  templateUrl: './switch.component.html',
  styleUrl: './switch.component.scss'
})
export class SwitchComponent {
  @Input() checked = false;
  @Input() disabled = false;
  @Input() testid: string | null = null;
  @Output() readonly checkedChange = new EventEmitter<boolean>();

  protected onChange(event: MatSlideToggleChange): void {
    this.checkedChange.emit(event.checked);
  }
}
