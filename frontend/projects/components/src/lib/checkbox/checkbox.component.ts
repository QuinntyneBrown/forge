import { Component, EventEmitter, Input, Output } from '@angular/core';
import { MatCheckboxChange, MatCheckboxModule } from '@angular/material/checkbox';

@Component({
  selector: 'forge-checkbox',
  imports: [MatCheckboxModule],
  templateUrl: './checkbox.component.html',
  styleUrl: './checkbox.component.scss'
})
export class CheckboxComponent {
  @Input() checked = false;
  @Input() disabled = false;
  @Input() testid: string | null = null;
  @Output() readonly checkedChange = new EventEmitter<boolean>();

  protected onChange(event: MatCheckboxChange): void {
    this.checkedChange.emit(event.checked);
  }
}
