import { test as base, expect } from '@playwright/test';
import { injectAxe, checkA11y } from 'axe-playwright';

/**
 * Custom test fixture that injects axe-core for accessibility testing.
 * Use this fixture to automatically check for accessibility violations.
 */
export const test = base.extend({
  accessibilityTest: async ({ page }, use) => {
    await injectAxe(page);
    await use(page);
  },
});

/**
 * Helper to check accessibility on a page.
 * Fails the test if critical or serious violations are found.
 */
export async function checkAccessibility(page: any, context?: string) {
  const violations = await checkA11y(page, context);
  const criticalOrSerious = violations.filter(
    (v: any) => v.impact === 'critical' || v.impact === 'serious'
  );
  expect(criticalOrSerious).toHaveLength(0);
}
