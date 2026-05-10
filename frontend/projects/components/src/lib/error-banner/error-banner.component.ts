import { Component, Input } from '@angular/core';

@Component({
  selector: 'forge-error-banner',
  imports: [],
  templateUrl: './error-banner.component.html',
  styleUrl: './error-banner.component.scss'
})
export class ErrorBannerComponent {
  @Input() title = '';
  @Input() message = '';
  @Input() testid: string | null = null;
}
