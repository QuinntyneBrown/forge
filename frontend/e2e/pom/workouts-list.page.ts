import { Locator, Page } from '@playwright/test';

export class WorkoutsListPage {
  readonly title: Locator;
  readonly newButton: Locator;
  readonly equipmentChips: Locator;
  readonly rangeChips: Locator;
  readonly empty: Locator;
  readonly rows: Locator;

  constructor(private readonly page: Page) {
    this.title = page.getByTestId('workout-list');
    this.newButton = page.getByTestId('workout-list-new');
    this.equipmentChips = page.getByTestId('workout-list-equipment-chips');
    this.rangeChips = page.getByTestId('workout-list-range-chips');
    this.empty = page.getByTestId('workout-list-empty');
    this.rows = page.getByTestId('workout-list-rows');
  }

  async goto(): Promise<void> {
    await this.page.goto('/workouts');
    await this.title.waitFor();
  }

  equipmentChip(id: 'all' | 'Treadmill' | 'IndoorBike' | 'BenchPress' | 'Elliptical'): Locator {
    return this.page.getByTestId(`workout-list-equipment-${id}`);
  }

  rangeChip(id: 'all' | 'today' | 'week' | 'month'): Locator {
    return this.page.getByTestId(`workout-list-range-${id}`);
  }

  rowCount(): Promise<number> {
    return this.rows.locator('li').count();
  }
}
