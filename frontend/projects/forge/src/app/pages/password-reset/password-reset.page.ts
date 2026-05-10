import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import {
  PasswordResetConfirmFormComponent,
  PasswordResetRequestFormComponent
} from 'domain';

@Component({
  selector: 'app-password-reset-page',
  imports: [PasswordResetRequestFormComponent, PasswordResetConfirmFormComponent],
  templateUrl: './password-reset.page.html',
  styleUrl: './password-reset.page.scss'
})
export class PasswordResetPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  private readonly queryParams = toSignal(this.route.queryParamMap, {
    initialValue: this.route.snapshot.queryParamMap
  });

  protected readonly token = computed(() => this.queryParams().get('token') ?? '');
  protected readonly mode = computed<'request' | 'confirm'>(() =>
    this.token() ? 'confirm' : 'request'
  );

  protected onConfirmed(): void {
    this.router.navigate(['/sign-in']);
  }
}
