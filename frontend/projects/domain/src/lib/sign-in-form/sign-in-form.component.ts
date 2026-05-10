import { Component, EventEmitter, Inject, Output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AUTH_SERVICE, AuthResult, IAuthService } from 'api';
import { ButtonComponent, CardComponent, CheckboxComponent } from 'components';

export interface SignedInEvent {
  result: AuthResult;
  rememberMe: boolean;
}

@Component({
  selector: 'forge-sign-in-form',
  imports: [ReactiveFormsModule, CardComponent, ButtonComponent, CheckboxComponent],
  templateUrl: './sign-in-form.component.html',
  styleUrl: './sign-in-form.component.scss'
})
export class SignInFormComponent {
  @Output() readonly signedIn = new EventEmitter<SignedInEvent>();

  protected readonly form;
  protected errorMessage: string | null = null;
  protected submitting = false;
  protected rememberMe = false;

  constructor(
    private readonly fb: FormBuilder,
    @Inject(AUTH_SERVICE) private readonly auth: IAuthService
  ) {
    this.form = this.fb.nonNullable.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(1)]]
    });
  }

  protected onRememberMeChange(checked: boolean): void {
    this.rememberMe = checked;
  }

  protected onSubmit(): void {
    if (this.form.invalid || this.submitting) {
      return;
    }
    this.submitting = true;
    this.errorMessage = null;
    this.auth.signIn(this.form.getRawValue()).subscribe({
      next: (result) => {
        this.submitting = false;
        this.signedIn.emit({ result, rememberMe: this.rememberMe });
      },
      error: (err) => {
        this.submitting = false;
        this.errorMessage = err?.error?.title ?? 'Sign-in failed.';
      }
    });
  }
}
