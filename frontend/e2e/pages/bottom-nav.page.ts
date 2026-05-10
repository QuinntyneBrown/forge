import { Locator, Page } from '@playwright/test';

/**
 * POM helper for the mobile bottom navigation strip
 * (see <forge-bottom-nav> with data-testid="app-shell-bottom-nav").
 *
 * Used by the bug-015 spec to verify that the last meaningful piece of
 * page content is not visually covered by the fixed/sticky bottom nav
 * after scrolling to the very bottom of the page.
 */
export class BottomNavPage {
  readonly nav: Locator;

  constructor(private readonly page: Page) {
    this.nav = page.getByTestId('app-shell-bottom-nav');
  }

  /**
   * Scroll the page (and the inner app-shell main scroll container, if
   * present) all the way to the bottom and wait for layout to settle.
   */
  async scrollToBottom(): Promise<void> {
    await this.page.evaluate(() => {
      // Window-level scroll.
      window.scrollTo(0, document.documentElement.scrollHeight);
      // Also scroll any internal scroll container (app-shell uses
      // .app-shell__main { overflow: auto } in some layouts).
      const candidates = document.querySelectorAll<HTMLElement>(
        '.app-shell__main, main'
      );
      for (const el of Array.from(candidates)) {
        el.scrollTop = el.scrollHeight;
      }
    });
    // Let any layout / sticky reflow complete.
    await this.page.waitForTimeout(150);
  }

  /**
   * Returns the bounding box of the bottom nav (throws if not present
   * or not laid out).
   */
  async navBox(): Promise<{ x: number; y: number; width: number; height: number }> {
    const box = await this.nav.boundingBox();
    if (!box) {
      throw new Error('Bottom nav has no bounding box (not visible?)');
    }
    return box;
  }

  /**
   * Asserts that a given content locator (e.g. last card / last form
   * field) is fully visible and does NOT overlap the bottom nav. A
   * small slack (default 4px) accounts for shadow / focus ring
   * artefacts.
   */
  async expectNotCoveredByNav(
    content: Locator,
    options: { slackPx?: number } = {}
  ): Promise<void> {
    const slack = options.slackPx ?? 4;
    const navBox = await this.navBox();
    const contentBox = await content.boundingBox();
    if (!contentBox) {
      throw new Error('Content locator has no bounding box (not visible?)');
    }
    const contentBottom = contentBox.y + contentBox.height;
    const navTop = navBox.y;
    if (contentBottom > navTop + slack) {
      throw new Error(
        `Content bottom (${contentBottom}) is below nav top (${navTop}) ` +
          `(overlap of ${contentBottom - navTop}px > slack ${slack}px). ` +
          `Content is being covered by the bottom navigation.`
      );
    }
  }
}
