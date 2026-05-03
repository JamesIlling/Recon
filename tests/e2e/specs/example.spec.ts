import { test, expect } from '@playwright/test';
import { injectAxe, checkA11y } from 'axe-playwright';

test.describe('Location Management E2E', () => {
  test('homepage loads and is accessible', async ({ page }) => {
    // Navigate to the application
    await page.goto('/');

    // Verify page loaded
    await expect(page).toHaveTitle(/Location Management/i);

    // Inject axe-core for accessibility testing
    await injectAxe(page);

    // Check for accessibility violations
    const violations = await checkA11y(page);
    const criticalOrSerious = violations.filter(
      (v) => v.impact === 'critical' || v.impact === 'serious'
    );
    expect(criticalOrSerious).toHaveLength(0);
  });

  test('welcome message is visible', async ({ page }) => {
    await page.goto('/');
    const welcomeText = page.locator('text=Welcome to Location Management');
    await expect(welcomeText).toBeVisible();
  });
});
