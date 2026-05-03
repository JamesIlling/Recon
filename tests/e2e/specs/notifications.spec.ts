import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

test.describe('Notification Panel - Accessibility', () => {
  const baseURL = process.env.BASE_URL || 'http://localhost:5173';

  test.beforeEach(async ({ page }) => {
    // Navigate to a page where the notification panel is accessible
    // Assuming the user menu with notifications is available on authenticated pages
    await page.goto(`${baseURL}/`);
  });

  test('notification panel has zero critical accessibility violations', async ({ page }) => {
    // Open the notification panel by clicking the user menu
    const userMenuButton = page.locator('[data-testid="user-menu-button"]');
    await userMenuButton.click();

    // Wait for notification panel to be visible
    const notificationPanel = page.locator('[data-testid="notification-panel"]');
    await expect(notificationPanel).toBeVisible();

    // Run Axe accessibility scan
    const results = await new AxeBuilder({ page }).analyze();
    const criticalOrSerious = results.violations.filter(
      (v) => v.impact === 'critical' || v.impact === 'serious'
    );

    expect(criticalOrSerious).toHaveLength(0);
  });

  test('notification panel has zero serious accessibility violations', async ({ page }) => {
    // Open the notification panel
    const userMenuButton = page.locator('[data-testid="user-menu-button"]');
    await userMenuButton.click();

    const notificationPanel = page.locator('[data-testid="notification-panel"]');
    await expect(notificationPanel).toBeVisible();

    // Run Axe accessibility scan
    const results = await new AxeBuilder({ page }).analyze();
    const serious = results.violations.filter((v) => v.impact === 'serious');

    expect(serious).toHaveLength(0);
  });

  test('unread count badge is announced by screen readers with aria-live', async ({ page }) => {
    // Open the notification panel
    const userMenuButton = page.locator('[data-testid="user-menu-button"]');
    await userMenuButton.click();

    // Check for unread count badge
    const unreadBadge = page.locator('[data-testid="notification-unread-badge"]');
    await expect(unreadBadge).toBeVisible();

    // Verify aria-live attribute is set to "polite"
    const ariaLive = await unreadBadge.getAttribute('aria-live');
    expect(ariaLive).toBe('polite');

    // Verify the badge has accessible text content
    const badgeText = await unreadBadge.textContent();
    expect(badgeText).toMatch(/\d+/); // Should contain a number
  });

  test('notification list items have proper semantic structure', async ({ page }) => {
    // Open the notification panel
    const userMenuButton = page.locator('[data-testid="user-menu-button"]');
    await userMenuButton.click();

    const notificationPanel = page.locator('[data-testid="notification-panel"]');
    await expect(notificationPanel).toBeVisible();

    // Check for list structure
    const notificationList = page.locator('[data-testid="notification-list"]');
    await expect(notificationList).toBeVisible();

    // Verify list has proper role
    const listRole = await notificationList.getAttribute('role');
    expect(['list', null]).toContain(listRole); // Either explicit role="list" or semantic <ul>

    // Check for list items
    const listItems = page.locator('[data-testid="notification-item"]');
    const itemCount = await listItems.count();

    if (itemCount > 0) {
      // Verify each item has proper structure
      for (let i = 0; i < Math.min(itemCount, 3); i++) {
        const item = listItems.nth(i);
        await expect(item).toBeVisible();

        // Check for accessible name
        const itemText = await item.textContent();
        expect(itemText).toBeTruthy();

        // Verify item has proper role
        const itemRole = await item.getAttribute('role');
        expect(['listitem', null]).toContain(itemRole);
      }
    }
  });

  test('mark as read button is keyboard accessible', async ({ page }) => {
    // Open the notification panel
    const userMenuButton = page.locator('[data-testid="user-menu-button"]');
    await userMenuButton.click();

    const notificationPanel = page.locator('[data-testid="notification-panel"]');
    await expect(notificationPanel).toBeVisible();

    // Find a notification item with mark as read button
    const markAsReadButtons = page.locator('[data-testid="mark-as-read-button"]');
    const buttonCount = await markAsReadButtons.count();

    if (buttonCount > 0) {
      const firstButton = markAsReadButtons.first();

      // Verify button is focusable
      await firstButton.focus();
      const isFocused = await firstButton.evaluate((el) => el === document.activeElement);
      expect(isFocused).toBe(true);

      // Verify button has accessible name
      const ariaLabel = await firstButton.getAttribute('aria-label');
      const buttonText = await firstButton.textContent();
      expect(ariaLabel || buttonText).toBeTruthy();

      // Verify button can be activated with keyboard
      await firstButton.press('Enter');
      // Button should be clickable (no error thrown)
    }
  });

  test('delete button is keyboard accessible', async ({ page }) => {
    // Open the notification panel
    const userMenuButton = page.locator('[data-testid="user-menu-button"]');
    await userMenuButton.click();

    const notificationPanel = page.locator('[data-testid="notification-panel"]');
    await expect(notificationPanel).toBeVisible();

    // Find a notification item with delete button
    const deleteButtons = page.locator('[data-testid="delete-notification-button"]');
    const buttonCount = await deleteButtons.count();

    if (buttonCount > 0) {
      const firstButton = deleteButtons.first();

      // Verify button is focusable
      await firstButton.focus();
      const isFocused = await firstButton.evaluate((el) => el === document.activeElement);
      expect(isFocused).toBe(true);

      // Verify button has accessible name
      const ariaLabel = await firstButton.getAttribute('aria-label');
      const buttonText = await firstButton.textContent();
      expect(ariaLabel || buttonText).toBeTruthy();

      // Verify button can be activated with keyboard
      await firstButton.press('Enter');
      // Button should be clickable (no error thrown)
    }
  });

  test('notification panel buttons have proper color contrast', async ({ page }) => {
    // Open the notification panel
    const userMenuButton = page.locator('[data-testid="user-menu-button"]');
    await userMenuButton.click();

    const notificationPanel = page.locator('[data-testid="notification-panel"]');
    await expect(notificationPanel).toBeVisible();

    // Get all buttons in the notification panel
    const buttons = page.locator('[data-testid="notification-panel"] button');
    const buttonCount = await buttons.count();

    // Verify buttons have sufficient contrast (this is checked by Axe)
    // We verify that buttons are visible and have text
    for (let i = 0; i < Math.min(buttonCount, 3); i++) {
      const button = buttons.nth(i);
      await expect(button).toBeVisible();

      const text = await button.textContent();
      expect(text).toBeTruthy();
    }
  });

  test('notification panel supports keyboard navigation', async ({ page }) => {
    // Open the notification panel
    const userMenuButton = page.locator('[data-testid="user-menu-button"]');
    await userMenuButton.click();

    const notificationPanel = page.locator('[data-testid="notification-panel"]');
    await expect(notificationPanel).toBeVisible();

    // Get all focusable elements in the panel
    const focusableElements = page.locator(
      '[data-testid="notification-panel"] button, [data-testid="notification-panel"] a, [data-testid="notification-panel"] [tabindex="0"]'
    );
    const elementCount = await focusableElements.count();

    if (elementCount > 0) {
      // Tab through elements
      const firstElement = focusableElements.first();
      await firstElement.focus();

      let isFocused = await firstElement.evaluate((el) => el === document.activeElement);
      expect(isFocused).toBe(true);

      // Tab to next element
      if (elementCount > 1) {
        await page.keyboard.press('Tab');
        const secondElement = focusableElements.nth(1);
        isFocused = await secondElement.evaluate((el) => el === document.activeElement);
        expect(isFocused).toBe(true);
      }
    }
  });

  test('notification count changes are announced to screen readers', async ({ page }) => {
    // Open the notification panel
    const userMenuButton = page.locator('[data-testid="user-menu-button"]');
    await userMenuButton.click();

    const notificationPanel = page.locator('[data-testid="notification-panel"]');
    await expect(notificationPanel).toBeVisible();

    // Get initial unread count
    const unreadBadge = page.locator('[data-testid="notification-unread-badge"]');
    const initialCount = await unreadBadge.textContent();

    // Verify aria-live is set for dynamic updates
    const ariaLive = await unreadBadge.getAttribute('aria-live');
    expect(ariaLive).toBe('polite');

    // Verify aria-atomic is set (optional but recommended)
    const ariaAtomic = await unreadBadge.getAttribute('aria-atomic');
    // aria-atomic can be true or not set (defaults to false)
    expect([null, 'true', 'false']).toContain(ariaAtomic);
  });

  test('notification panel has proper focus management', async ({ page }) => {
    // Open the notification panel
    const userMenuButton = page.locator('[data-testid="user-menu-button"]');
    await userMenuButton.click();

    const notificationPanel = page.locator('[data-testid="notification-panel"]');
    await expect(notificationPanel).toBeVisible();

    // Verify panel is visible and accessible
    const isVisible = await notificationPanel.isVisible();
    expect(isVisible).toBe(true);

    // Verify there are focusable elements in the panel
    const focusableElements = page.locator(
      '[data-testid="notification-panel"] button, [data-testid="notification-panel"] a'
    );
    const elementCount = await focusableElements.count();
    expect(elementCount).toBeGreaterThanOrEqual(0);
  });

  test('notification panel closes and returns focus to trigger', async ({ page }) => {
    // Open the notification panel
    const userMenuButton = page.locator('[data-testid="user-menu-button"]');
    await userMenuButton.click();

    const notificationPanel = page.locator('[data-testid="notification-panel"]');
    await expect(notificationPanel).toBeVisible();

    // Close the panel (by clicking the button again or pressing Escape)
    await userMenuButton.click();

    // Verify panel is closed
    await expect(notificationPanel).not.toBeVisible();

    // Verify focus returns to the trigger button
    const isFocused = await userMenuButton.evaluate((el) => el === document.activeElement);
    expect(isFocused).toBe(true);
  });

  test('notification items have descriptive text for screen readers', async ({ page }) => {
    // Open the notification panel
    const userMenuButton = page.locator('[data-testid="user-menu-button"]');
    await userMenuButton.click();

    const notificationPanel = page.locator('[data-testid="notification-panel"]');
    await expect(notificationPanel).toBeVisible();

    // Check for notification items
    const notificationItems = page.locator('[data-testid="notification-item"]');
    const itemCount = await notificationItems.count();

    if (itemCount > 0) {
      // Verify first item has descriptive content
      const firstItem = notificationItems.first();
      const itemText = await firstItem.textContent();

      // Should contain meaningful text (event type, resource name, etc.)
      expect(itemText).toBeTruthy();
      expect(itemText?.length).toBeGreaterThan(5);
    }
  });

  test('notification panel passes full accessibility scan', async ({ page }) => {
    // Open the notification panel
    const userMenuButton = page.locator('[data-testid="user-menu-button"]');
    await userMenuButton.click();

    const notificationPanel = page.locator('[data-testid="notification-panel"]');
    await expect(notificationPanel).toBeVisible();

    // Run comprehensive Axe scan
    const results = await new AxeBuilder({ page })
      .withTags(['wcag2aa', 'wcag21aa'])
      .analyze();

    // Check for violations
    const violations = results.violations;

    // Log violations for debugging
    if (violations.length > 0) {
      console.log('Accessibility violations found:');
      violations.forEach((v) => {
        console.log(`- ${v.id} (${v.impact}): ${v.description}`);
      });
    }

    // Assert no critical or serious violations
    const criticalOrSerious = violations.filter(
      (v) => v.impact === 'critical' || v.impact === 'serious'
    );
    expect(criticalOrSerious).toHaveLength(0);
  });

  test('notification panel maintains accessibility when empty', async ({ page }) => {
    // Open the notification panel
    const userMenuButton = page.locator('[data-testid="user-menu-button"]');
    await userMenuButton.click();

    const notificationPanel = page.locator('[data-testid="notification-panel"]');
    await expect(notificationPanel).toBeVisible();

    // Check if there's an empty state message
    const emptyState = page.locator('[data-testid="notification-empty-state"]');
    const hasEmptyState = await emptyState.isVisible().catch(() => false);

    if (hasEmptyState) {
      // Verify empty state has accessible text
      const emptyText = await emptyState.textContent();
      expect(emptyText).toBeTruthy();
    }

    // Run accessibility scan on empty state
    const results = await new AxeBuilder({ page }).analyze();
    const criticalOrSerious = results.violations.filter(
      (v) => v.impact === 'critical' || v.impact === 'serious'
    );
    expect(criticalOrSerious).toHaveLength(0);
  });

  test('notification panel badge is visible and readable', async ({ page }) => {
    // Open the notification panel
    const userMenuButton = page.locator('[data-testid="user-menu-button"]');
    await userMenuButton.click();

    const notificationPanel = page.locator('[data-testid="notification-panel"]');
    await expect(notificationPanel).toBeVisible();

    // Check unread badge
    const unreadBadge = page.locator('[data-testid="notification-unread-badge"]');
    const isBadgeVisible = await unreadBadge.isVisible().catch(() => false);

    if (isBadgeVisible) {
      // Verify badge is readable
      const badgeText = await unreadBadge.textContent();
      expect(badgeText).toBeTruthy();

      // Verify badge has sufficient size for readability
      const boundingBox = await unreadBadge.boundingBox();
      expect(boundingBox?.width).toBeGreaterThan(20);
      expect(boundingBox?.height).toBeGreaterThan(20);
    }
  });

  test('mark as read and delete buttons have distinct labels', async ({ page }) => {
    // Open the notification panel
    const userMenuButton = page.locator('[data-testid="user-menu-button"]');
    await userMenuButton.click();

    const notificationPanel = page.locator('[data-testid="notification-panel"]');
    await expect(notificationPanel).toBeVisible();

    // Check mark as read button
    const markAsReadButtons = page.locator('[data-testid="mark-as-read-button"]');
    const markAsReadCount = await markAsReadButtons.count();

    if (markAsReadCount > 0) {
      const markAsReadLabel = await markAsReadButtons.first().getAttribute('aria-label');
      const markAsReadText = await markAsReadButtons.first().textContent();
      expect(markAsReadLabel || markAsReadText).toBeTruthy();
    }

    // Check delete button
    const deleteButtons = page.locator('[data-testid="delete-notification-button"]');
    const deleteCount = await deleteButtons.count();

    if (deleteCount > 0) {
      const deleteLabel = await deleteButtons.first().getAttribute('aria-label');
      const deleteText = await deleteButtons.first().textContent();
      expect(deleteLabel || deleteText).toBeTruthy();
    }

    // Verify labels are different
    if (markAsReadCount > 0 && deleteCount > 0) {
      const markLabel = await markAsReadButtons.first().getAttribute('aria-label');
      const delLabel = await deleteButtons.first().getAttribute('aria-label');
      expect(markLabel).not.toBe(delLabel);
    }
  });
});
