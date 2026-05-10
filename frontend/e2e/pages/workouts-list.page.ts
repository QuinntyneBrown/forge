import { Locator, Page } from '@playwright/test';

export class WorkoutsListPage {
  readonly title: Locator;
  readonly newButton: Locator;
  readonly equipmentChips: Locator;
  readonly rangeChips: Locator;
  readonly empty: Locator;
  readonly rows: Locator;
  readonly pageTitle: Locator;
  readonly pageSubtitle: Locator;
  readonly summaryStrip: Locator;
  readonly dayGroups: Locator;

  constructor(private readonly page: Page) {
    this.title = page.getByTestId('workout-list');
    this.newButton = page.getByTestId('workout-list-new');
    this.equipmentChips = page.getByTestId('workout-list-equipment-chips');
    this.rangeChips = page.getByTestId('workout-list-range-chips');
    this.empty = page.getByTestId('workout-list-empty');
    this.rows = page.getByTestId('workout-list-rows');
    this.pageTitle = page.getByTestId('workout-list-page-title');
    this.pageSubtitle = page.getByTestId('workout-list-page-subtitle');
    this.summaryStrip = page.getByTestId('workout-list-summary');
    this.dayGroups = page.getByTestId('workout-list-day-group');
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

  summaryStat(key: 'minutes' | 'calories' | 'points'): Locator {
    return this.page.getByTestId(`workout-list-summary-${key}`);
  }

  dayGroupHeaders(): Locator {
    return this.page.getByTestId('workout-list-day-group-label');
  }

  sessionRow(index: number): Locator {
    return this.page.locator('[data-testid^="workout-list-row-"]').nth(index);
  }

  sessionRowEquipmentIcon(row: Locator): Locator {
    return row.getByTestId('workout-list-row-icon');
  }

  sessionRowMetaIcon(row: Locator, label: 'duration' | 'calories' | 'distance' | 'hr'): Locator {
    return row.getByTestId(`workout-list-row-meta-${label}`);
  }

  sessionRowPoints(row: Locator): Locator {
    return row.getByTestId('workout-list-row-points-value');
  }

  sessionRowTime(row: Locator): Locator {
    return row.getByTestId('workout-list-row-time');
  }
}
