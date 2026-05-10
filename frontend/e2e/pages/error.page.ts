import { Locator, Page } from '@playwright/test';

export class ErrorPagePom {
  readonly pageRoot: Locator;

  constructor(private readonly page: Page) {
    this.pageRoot = page.locator('.error-page');
  }

  async goto(query: string = ''): Promise<void> {
    const suffix = query ? (query.startsWith('?') ? query : `?${query}`) : '';
    await this.page.goto(`/error${suffix}`);
  }

  topBanner(): Locator {
    return this.page.getByTestId('error-page-banner');
  }

  heroIllustration(): Locator {
    return this.page.getByTestId('error-page-hero');
  }

  errorCodeChip(): Locator {
    return this.page.getByTestId('error-page-code-chip');
  }

  retryButton(): Locator {
    return this.page.getByTestId('error-page-retry-button');
  }

  diagnosticsCard(): Locator {
    return this.page.getByTestId('error-page-diagnostics');
  }

  diagnosticItems(): Locator {
    return this.page.getByTestId('error-page-diagnostic-row');
  }

  diagnosticItem(label: string | RegExp): Locator {
    return this.diagnosticItems().filter({ hasText: label });
  }
}
