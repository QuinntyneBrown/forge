import { Component, DestroyRef, EventEmitter, Inject, Input, Output, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { AUTH_SERVICE, IAuthService } from 'api';
import { ButtonComponent, CardComponent } from 'components';

@Component({
  selector: 'forge-password-reset-confirm-form',
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, CardComponent, ButtonComponent],
  templateUrl: './password-reset-confirm-form.component.html',
  styleUrl: './password-reset-confirm-form.component.scss'
})
export class PasswordResetConfirmFormComponent {
  @Input({ required: true }) token = '';
  @Output() readonly confirmed = new EventEmitter<void>();

  protected readonly form;
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly submitting = signal(false);
  private readonly destroyRef = inject(DestroyRef);

  constructor(
    private readonly fb: FormBuilder,
    @Inject(AUTH_SERVICE) private readonly auth: IAuthService
  ) {
    this.form = this.fb.nonNullable.group({
      newPassword: [
        '',
        [
          Validators.required,
          Validators.minLength(12),
          Validators.pattern(/(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[^A-Za-z0-9]).+/)
        ]
      ]
    });
  }

  protected onSubmit(): void {
    if (this.form.invalid || this.submitting() || !this.token) {
      return;
    }
    this.submitting.set(true);
    this.errorMessage.set(null);
    this.auth.confirmPasswordReset(this.token, this.form.controls.newPassword.value).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.submitting.set(false);
        this.confirmed.emit();
      },
      error: (err) => {
        this.submitting.set(false);
        this.errorMessage.set(
          err?.error?.title ?? 'That reset link is invalid, expired, or already used.'
        );
      }
    });
  }
}
