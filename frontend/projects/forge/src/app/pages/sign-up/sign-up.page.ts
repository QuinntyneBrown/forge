import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthResult } from 'api';
import { SignUpFormComponent } from 'domain';
import { AuthStateService } from '../../auth-state.service';

@Component({
  selector: 'app-sign-up-page',
  imports: [SignUpFormComponent],
  templateUrl: './sign-up.page.html',
  styleUrl: './sign-up.page.scss'
})
export class SignUpPage {
  constructor(
    private readonly auth: AuthStateService,
    private readonly router: Router
  ) {}

  protected onSignedUp(result: AuthResult): void {
    this.auth.setSession(result);
    this.router.navigate(['/dashboard']);
  }
}
