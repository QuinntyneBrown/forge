import { Component, DestroyRef, EventEmitter, Inject, Output, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { toSignal } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { AUTH_SERVICE, AuthResult, IAuthService } from 'api';
import { ButtonComponent, CardComponent } from 'components';

@Component({
  selector: 'forge-sign-up-form',
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, CardComponent, ButtonComponent],
  templateUrl: './sign-up-form.component.html',
  styleUrl: './sign-up-form.component.scss'
})
export class SignUpFormComponent {
  @Output() readonly signedUp = new EventEmitter<AuthResult>();

  protected readonly form;
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly submitting = signal(false);
  private readonly password;
  private readonly destroyRef = inject(DestroyRef);

  protected readonly passwordValue;
  protected readonly strengthScore;
  protected readonly strengthLabel;

  constructor(
    private readonly fb: FormBuilder,
    @Inject(AUTH_SERVICE) private readonly auth: IAuthService
  ) {
    this.form = this.fb.nonNullable.group(
      {
        firstName: ['', [Validators.required, Validators.maxLength(64)]],
        lastName: ['', [Validators.required, Validators.maxLength(64)]],
        email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
        password: [
          '',
          [
            Validators.required,
            Validators.minLength(12),
            Validators.pattern(/(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[^A-Za-z0-9]).+/)
          ]
        ],
        confirmPassword: ['', [Validators.required]],
        acceptTerms: [false, [Validators.requiredTrue]]
      },
      { validators: [matchPasswordValidator] }
    );

    this.password = this.form.controls.password;
    this.passwordValue = toSignal(this.password.valueChanges, { initialValue: '' });
    this.strengthScore = computed(() => this.scorePassword(this.passwordValue() || ''));
    this.strengthLabel = computed(() => {
      const s = this.strengthScore();
      if (s === 0) return '';
      if (s === 1) return 'Weak';
      if (s === 2) return 'Fair';
      if (s === 3) return 'Good';
      return 'Strong';
    });
  }

  private scorePassword(value: string): number {
    if (!value) return 0;
    let score = 0;
    if (value.length >= 8) score++;
    if (value.length >= 12) score++;
    if (/[A-Z]/.test(value) && /[a-z]/.test(value)) score++;
    if (/\d/.test(value) && /[^A-Za-z0-9]/.test(value)) score++;
    return Math.min(4, score);
  }

  protected get passwordMismatch(): boolean {
    return this.form.errors?.['passwordMismatch'] === true;
  }

  protected onSubmit(): void {
    if (this.form.invalid || this.submitting()) {
      return;
    }
    this.submitting.set(true);
    this.errorMessage.set(null);
    const { firstName, lastName, email, password } = this.form.getRawValue();
    this.auth.register({ firstName, lastName, email, password }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.submitting.set(false);
        this.signedUp.emit(result);
      },
      error: (err) => {
        this.submitting.set(false);
        this.errorMessage.set(err?.error?.title ?? 'Sign-up failed.');
      }
    });
  }
}

function matchPasswordValidator(control: AbstractControl): ValidationErrors | null {
  const passwordControl = control.get('password');
  const confirmControl = control.get('confirmPassword');
  const pw = passwordControl?.value;
  const cp = confirmControl?.value;
  const clearMismatch = (): void => {
    if (confirmControl?.hasError('passwordMismatch')) {
      const remaining = { ...(confirmControl.errors ?? {}) };
      delete remaining['passwordMismatch'];
      confirmControl.setErrors(Object.keys(remaining).length ? remaining : null);
    }
  };
  if (!pw || !cp) {
    clearMismatch();
    return null;
  }
  if (pw === cp) {
    clearMismatch();
    return null;
  }
  // Surface the mismatch on the confirm field so Material's <mat-error> shows
  // it once the field is touched, in addition to flagging the FormGroup so the
  // submit button stays disabled.
  confirmControl?.setErrors({ ...(confirmControl.errors ?? {}), passwordMismatch: true });
  return { passwordMismatch: true };
}
