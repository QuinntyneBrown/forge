import { Component, Input } from '@angular/core';

@Component({
  selector: 'forge-empty-state',
  imports: [],
  templateUrl: './empty-state.component.html',
  styleUrl: './empty-state.component.scss'
})
export class EmptyStateComponent {
  @Input() title = '';
  @Input() message = '';
  @Input() testid: string | null = null;
}
