import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthResult } from 'api';
import { SignInFormComponent } from 'domain';
import { AuthStateService } from '../../auth-state.service';

@Component({
  selector: 'app-sign-in-page',
  imports: [SignInFormComponent],
  templateUrl: './sign-in.page.html',
  styleUrl: './sign-in.page.scss'
})
export class SignInPage {
  constructor(
    private readonly auth: AuthStateService,
    private readonly router: Router
  ) {}

  protected onSignedIn(result: AuthResult): void {
    this.auth.setSession(result);
    this.router.navigate(['/dashboard']);
  }
}
