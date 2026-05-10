import { Component, Inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { AUTH_SERVICE, IAuthService } from 'api';
import { ButtonComponent, CardComponent } from 'components';

@Component({
  selector: 'forge-password-reset-request-form',
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, CardComponent, ButtonComponent],
  templateUrl: './password-reset-request-form.component.html',
  styleUrl: './password-reset-request-form.component.scss'
})
export class PasswordResetRequestFormComponent {
  protected readonly form;
  protected readonly submitted = signal(false);
  protected readonly submitting = signal(false);

  constructor(
    private readonly fb: FormBuilder,
    @Inject(AUTH_SERVICE) private readonly auth: IAuthService
  ) {
    this.form = this.fb.nonNullable.group({
      email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]]
    });
  }

  protected resend(): void {
    this.submitted.set(false);
  }

  protected onSubmit(): void {
    if (this.form.invalid || this.submitting()) {
      return;
    }
    this.submitting.set(true);
    this.auth.requestPasswordReset(this.form.controls.email.value).subscribe({
      // 202 always — same UX whether the email exists or not (L2-004 ac 1).
      next: () => {
        this.submitting.set(false);
        this.submitted.set(true);
      },
      error: () => {
        // The request leg is designed to never reveal account existence; even
        // network errors land on the same confirmation screen.
        this.submitting.set(false);
        this.submitted.set(true);
      }
    });
  }
}
