import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { ButtonComponent, EmptyStateComponent } from 'components';

@Component({
  selector: 'app-not-found-page',
  imports: [EmptyStateComponent, ButtonComponent],
  templateUrl: './not-found.page.html',
  styleUrl: './not-found.page.scss'
})
export class NotFoundPage {
  private readonly router = inject(Router);

  protected goHome(): void {
    this.router.navigate(['/dashboard']);
  }
}
