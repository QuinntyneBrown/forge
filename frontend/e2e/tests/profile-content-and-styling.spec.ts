// Acceptance Test for Bug 011
// Description: /profile must align with docs/mocks/profile.html. The page
// must render a hero block (avatar + display name + tier chip) plus four
// fully populated cards: Goals (with editable target inputs), Windows
// (with the four time inputs), Integrations & alerts (with three live
// toggles), and a Save card with a working Save action. The dev seeded
// user (`dev@forge.local`) carries the entity defaults so the inputs
// must reflect those values exactly.

import { expect, test } from '@playwright/test';
import { ProfilePage } from '../pages/profile.page';
import { SignInPage } from '../pages/sign-in.page';

const DEV_EMAIL = 'dev@forge.local';
const DEV_PASSWORD = 'DevPassword123!';

test.describe('Bug 011: profile content + styling', () => {
  test.beforeEach(async ({ page }) => {
    const signIn = new SignInPage(page);
    await signIn.goto();
    await signIn.signIn(DEV_EMAIL, DEV_PASSWORD);
    await page.waitForURL('**/dashboard');
    const profile = new ProfilePage(page);
    await profile.goto();
    await page.waitForLoadState('networkidle');
  });

  test('hero shows avatar, display name, and a populated tier chip', async ({ page }) => {
    const profile = new ProfilePage(page);
    await expect(profile.hero).toBeVisible();
    await expect(profile.heroAvatar).toBeVisible();
    // Avatar shows initials derived from firstName[0]+lastName[0] — the
    // dev user is "Dev User" so initials are "DU".
    await expect(profile.heroAvatar).toHaveText(/DU/);
    await expect(profile.heroDisplayName).toBeVisible();
    await expect(profile.heroDisplayName).toHaveText(/Dev User/i);
    await expect(profile.heroTierChip).toBeVisible();
    // The chip must have non-empty tier text — derived from rewards/tier.
    const chipText = (await profile.heroTierChip.innerText()).trim();
    expect(chipText.length).toBeGreaterThan(0);
    // Mock copy uses "Tier" wording.
    expect(chipText.toLowerCase()).toContain('tier');
  });

  test('goals card renders editable inputs prefilled from the user defaults', async ({
    page
  }) => {
    const profile = new ProfilePage(page);
    await expect(profile.goalsCard).toBeVisible();
    await expect(profile.goalCalories).toBeVisible();
    await expect(profile.goalCalories).toHaveValue('1500');
    await expect(profile.goalMinutes).toBeVisible();
    await expect(profile.goalMinutes).toHaveValue('60');
    await expect(profile.goalWeight).toBeVisible();
    await expect(profile.goalWeight).toHaveValue('20');
  });

  test('windows card renders the four time inputs prefilled from the user defaults', async ({
    page
  }) => {
    const profile = new ProfilePage(page);
    await expect(profile.windowsCard).toBeVisible();
    await expect(profile.morningStart).toBeVisible();
    await expect(profile.morningStart).toHaveValue('05:00');
    await expect(profile.morningEnd).toBeVisible();
    await expect(profile.morningEnd).toHaveValue('07:30');
    await expect(profile.kitchenStart).toBeVisible();
    await expect(profile.kitchenStart).toHaveValue('20:00');
    await expect(profile.kitchenEnd).toBeVisible();
    await expect(profile.kitchenEnd).toHaveValue('06:00');
  });

  test('integrations card exposes three toggles bound to the user state', async ({
    page
  }) => {
    const profile = new ProfilePage(page);
    await expect(profile.integrationsCard).toBeVisible();

    await expect(profile.morningReminderToggle).toBeVisible();
    await expect(profile.morningReminderToggle).toHaveAttribute('aria-checked', 'true');

    await expect(profile.kitchenNudgeToggle).toBeVisible();
    await expect(profile.kitchenNudgeToggle).toHaveAttribute('aria-checked', 'true');

    await expect(profile.leaderboardToggle).toBeVisible();
    await expect(profile.leaderboardToggle).toHaveAttribute('aria-checked', 'false');
  });

  test('save card shows an enabled Save action and the Sign out button', async ({
    page
  }) => {
    const profile = new ProfilePage(page);
    await expect(profile.saveCard).toBeVisible();
    await expect(profile.saveAllButton).toBeVisible();
    await expect(profile.saveAllButton).toBeEnabled();
    await expect(profile.signOutButton).toBeVisible();
  });
});
