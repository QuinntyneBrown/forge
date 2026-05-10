import { Component, Input } from '@angular/core';

@Component({
  selector: 'forge-card',
  templateUrl: './card.component.html',
  styleUrl: './card.component.scss'
})
export class CardComponent {
  @Input() title = '';
}
