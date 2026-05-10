import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { SyncErrorPanelComponent } from 'domain';

@Component({
  selector: 'app-error-page',
  imports: [SyncErrorPanelComponent],
  templateUrl: './error.page.html',
  styleUrl: './error.page.scss'
})
export class ErrorPage {
  private readonly route = inject(ActivatedRoute);
  protected readonly traceId = this.route.snapshot.queryParamMap.get('traceId');
}
