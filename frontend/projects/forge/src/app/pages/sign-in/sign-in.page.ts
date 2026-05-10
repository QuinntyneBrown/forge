import { Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { SignedInEvent, SignInFormComponent } from 'domain';
import { AuthStateService } from '../../auth-state.service';

@Component({
  selector: 'app-sign-in-page',
  imports: [SignInFormComponent, RouterLink],
  templateUrl: './sign-in.page.html',
  styleUrl: './sign-in.page.scss'
})
export class SignInPage {
  constructor(
    private readonly auth: AuthStateService,
    private readonly router: Router
  ) {}

  protected onSignedIn(event: SignedInEvent): void {
    this.auth.setSession(event.result, event.rememberMe);
    this.router.navigate(['/dashboard']);
  }
}
