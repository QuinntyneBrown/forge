import { Component, EventEmitter, Inject, Output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AUTH_SERVICE, AuthResult, IAuthService } from 'api';
import { ButtonComponent, CardComponent, CheckboxComponent } from 'components';

export interface SignedInEvent {
  result: AuthResult;
  rememberMe: boolean;
}

@Component({
  selector: 'forge-sign-in-form',
  imports: [ReactiveFormsModule, RouterLink, CardComponent, ButtonComponent, CheckboxComponent],
  templateUrl: './sign-in-form.component.html',
  styleUrl: './sign-in-form.component.scss'
})
export class SignInFormComponent {
  @Output() readonly signedIn = new EventEmitter<SignedInEvent>();

  protected readonly form;
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly submitting = signal(false);
  protected readonly rememberMe = signal(false);

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
    this.rememberMe.set(checked);
  }

  protected onSubmit(): void {
    if (this.form.invalid || this.submitting()) {
      return;
    }
    this.submitting.set(true);
    this.errorMessage.set(null);
    this.auth.signIn(this.form.getRawValue()).subscribe({
      next: (result) => {
        this.submitting.set(false);
        this.signedIn.emit({ result, rememberMe: this.rememberMe() });
      },
      error: (err) => {
        this.submitting.set(false);
        this.errorMessage.set(err?.error?.title ?? 'Sign-in failed.');
      }
    });
  }
}
