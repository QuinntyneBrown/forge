import { Locator, Page } from '@playwright/test';

export class WorkoutDetailPage {
  readonly hero: Locator;
  readonly heroTitle: Locator;
  readonly actionsCard: Locator;
  readonly duplicateButton: Locator;
  readonly deleteButton: Locator;
  readonly infoCaption: Locator;

  constructor(private readonly page: Page) {
    this.hero = page.getByTestId('workout-detail-hero');
    this.heroTitle = page.getByTestId('workout-detail-hero-title');
    this.actionsCard = page.getByTestId('workout-detail-actions-card');
    this.duplicateButton = page.getByTestId('workout-detail-duplicate');
    this.deleteButton = page.getByTestId('workout-detail-delete');
    this.infoCaption = page.getByTestId('workout-detail-info-caption');
  }

  async goto(sessionId: string): Promise<void> {
    await this.page.goto(`/workouts/${sessionId}`);
    await this.actionsCard.waitFor();
  }
}
