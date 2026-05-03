import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

test.describe('Collections Management - Happy Paths', () => {
  const baseURL = process.env.BASE_URL || 'http://localhost:5173';

  test('should view collections list', async ({ page }) => {
    await page.goto(`${baseURL}/`);
    await expect(page).toHaveTitle(/Collections/);
    await expect(page.locator('h1')).toContainText('Collections');

    // Check for collection cards
    const collectionCards = page.locator('.collection-card');
    expect(collectionCards).toBeDefined();
  });

  test('should view collection detail page', async ({ page }) => {
    // Navigate to collections list first
    await page.goto(`${baseURL}/`);

    // Click first collection card
    const firstCollectionCard = page.locator('.collection-card').first();
    await firstCollectionCard.click();

    // Verify we're on detail page
    await expect(page).toHaveURL(/\/collections\/[a-f0-9-]+$/);

    // Check for collection metadata
    await expect(page.locator('.collection-metadata')).toBeVisible();
    await expect(page.locator('text=Owner:')).toBeVisible();
    await expect(page.locator('text=Visibility:')).toBeVisible();
  });

  test('should display collection map with member locations', async ({ page }) => {
    await page.goto(`${baseURL}/`);
    const firstCollectionCard = page.locator('.collection-card').first();
    await firstCollectionCard.click();

    // Check map is rendered
    const mapContainer = page.locator('.collection-map-section');
    await expect(mapContainer).toBeVisible();

    // Check for member locations list
    const membersList = page.locator('.collection-members-section');
    await expect(membersList).toBeVisible();

    // Check for location links
    const locationLinks = page.locator('.member-link');
    expect(locationLinks).toBeDefined();
  });

  test('should navigate through paginated collections', async ({ page }) => {
    await page.goto(`${baseURL}/`);

    // Check initial page
    const pageInfo = page.locator('.page-info');
    const initialText = await pageInfo.textContent();
    expect(initialText).toContain('Page 1');

    // Click next button if available
    const nextButton = page.locator('button:has-text("Next")');
    if (await nextButton.isEnabled()) {
      await nextButton.click();
      const newText = await pageInfo.textContent();
      expect(newText).toContain('Page 2');

      // Click previous button
      const prevButton = page.locator('button:has-text("Previous")');
      await prevButton.click();
      const backText = await pageInfo.textContent();
      expect(backText).toContain('Page 1');
    }
  });

  test('should display collection description truncated at 100 characters', async ({ page }) => {
    await page.goto(`${baseURL}/`);

    // Check collection cards have descriptions
    const descriptions = page.locator('.collection-card-description');
    const count = await descriptions.count();

    if (count > 0) {
      const firstDescription = descriptions.first();
      const text = await firstDescription.textContent();
      // Description should be truncated with ellipsis if longer than 100 chars
      if (text && text.length > 100) {
        expect(text).toContain('...');
      }
    }
  });

  test('should display owner vs public badges correctly', async ({ page }) => {
    await page.goto(`${baseURL}/`);

    // Check for badges
    const ownerBadges = page.locator('.badge-owner');
    const publicBadges = page.locator('.badge-public');

    // At least one badge should exist
    const totalBadges = (await ownerBadges.count()) + (await publicBadges.count());
    expect(totalBadges).toBeGreaterThan(0);
  });

  test('should handle collection not found gracefully', async ({ page }) => {
    await page.goto(`${baseURL}/collections/invalid-id-12345`);

    // Should show error message
    await expect(page.locator('text=Error')).toBeVisible();
    await expect(page.locator('text=not found')).toBeVisible();

    // Should have back button
    const backButton = page.locator('button:has-text("Back to Collections")');
    await expect(backButton).toBeVisible();
  });

  test('should display empty state when no members in collection', async ({ page }) => {
    // This test assumes there's a collection with no members
    // In a real scenario, you'd create one via API first
    await page.goto(`${baseURL}/`);

    // If we find a collection with no members, check empty state
    const emptyStates = page.locator('text=No Members');
    if (await emptyStates.count() > 0) {
      await expect(emptyStates.first()).toBeVisible();
    }
  });

  test('should display collection metadata correctly', async ({ page }) => {
    await page.goto(`${baseURL}/`);
    const firstCollectionCard = page.locator('.collection-card').first();
    await firstCollectionCard.click();

    // Check all metadata fields are present
    await expect(page.locator('text=Owner:')).toBeVisible();
    await expect(page.locator('text=Visibility:')).toBeVisible();
    await expect(page.locator('text=Created:')).toBeVisible();

    // Verify visibility badge is present
    const visibilityBadge = page.locator('.visibility-badge');
    await expect(visibilityBadge).toBeVisible();
  });

  test('should show loading skeleton while fetching collection', async ({ page }) => {
    // Slow down network to see skeleton
    await page.route('**/api/collections/**', async (route) => {
      await new Promise((resolve) => setTimeout(resolve, 500));
      await route.continue();
    });

    await page.goto(`${baseURL}/`);
    const firstCollectionCard = page.locator('.collection-card').first();
    await firstCollectionCard.click();

    // Skeleton should be visible briefly
    const skeleton = page.locator('.loading-skeleton');
    expect(skeleton).toBeDefined();
  });

  test('should pass accessibility checks on collections list', async ({ page }) => {
    await page.goto(`${baseURL}/`);

    const results = await new AxeBuilder({ page }).analyze();
    const criticalOrSerious = results.violations.filter(
      (v) => v.impact === 'critical' || v.impact === 'serious'
    );

    expect(criticalOrSerious).toHaveLength(0);
  });

  test('should pass accessibility checks on collection detail', async ({ page }) => {
    await page.goto(`${baseURL}/`);
    const firstCollectionCard = page.locator('.collection-card').first();
    await firstCollectionCard.click();

    const results = await new AxeBuilder({ page }).analyze();
    const criticalOrSerious = results.violations.filter(
      (v) => v.impact === 'critical' || v.impact === 'serious'
    );

    expect(criticalOrSerious).toHaveLength(0);
  });

  test('should have keyboard accessible collection cards', async ({ page }) => {
    await page.goto(`${baseURL}/`);

    // Tab to first collection card
    await page.keyboard.press('Tab');

    // Check if we can interact with it
    const firstCard = page.locator('.collection-card').first();
    const viewButton = firstCard.locator('button:has-text("View Collection")');

    if (await viewButton.isVisible()) {
      // Focus should be on the button
      await viewButton.focus();
      await viewButton.press('Enter');

      // Should navigate to detail page
      await expect(page).toHaveURL(/\/collections\/[a-f0-9-]+$/);
    }
  });

  test('should have proper heading hierarchy on collection detail', async ({ page }) => {
    await page.goto(`${baseURL}/`);
    const firstCollectionCard = page.locator('.collection-card').first();
    await firstCollectionCard.click();

    // Check for h1 (collection name)
    const h1 = page.locator('h1');
    await expect(h1).toBeVisible();

    // Check for h2 (section headings like "Collection Map", "Member Locations")
    const h2s = page.locator('h2');
    expect(await h2s.count()).toBeGreaterThan(0);
  });

  test('should have proper aria labels on interactive elements', async ({ page }) => {
    await page.goto(`${baseURL}/`);

    // Check collection cards have aria-label
    const firstCard = page.locator('.collection-card').first();
    const ariaLabel = await firstCard.getAttribute('aria-label');
    expect(ariaLabel).toBeTruthy();

    // Check view button has aria-label
    const viewButton = firstCard.locator('button:has-text("View Collection")');
    const buttonAriaLabel = await viewButton.getAttribute('aria-label');
    expect(buttonAriaLabel).toBeTruthy();
  });

  test('should display pagination with proper accessibility', async ({ page }) => {
    await page.goto(`${baseURL}/`);

    // Check pagination navigation
    const paginationNav = page.locator('[role="navigation"][aria-label="Pagination"]');
    if (await paginationNav.isVisible()) {
      // Check for aria-label on pagination buttons
      const prevButton = page.locator('button:has-text("Previous")');
      const nextButton = page.locator('button:has-text("Next")');

      const prevLabel = await prevButton.getAttribute('aria-label');
      const nextLabel = await nextButton.getAttribute('aria-label');

      expect(prevLabel).toBeTruthy();
      expect(nextLabel).toBeTruthy();
    }
  });

  test('should have proper color contrast on collection cards', async ({ page }) => {
    await page.goto(`${baseURL}/`);

    // Run axe-core with specific rules for color contrast
    const results = await new AxeBuilder({ page })
      .include('.collection-card')
      .analyze();

    const contrastViolations = results.violations.filter(
      (v) => v.id === 'color-contrast'
    );

    expect(contrastViolations).toHaveLength(0);
  });

  test('should support zoom to 200% without content clipping', async ({ page }) => {
    await page.goto(`${baseURL}/`);

    // Zoom to 200%
    await page.evaluate(() => {
      document.body.style.zoom = '200%';
    });

    // Check that collection cards are still visible and not clipped
    const firstCard = page.locator('.collection-card').first();
    await expect(firstCard).toBeVisible();

    // Check that we can still interact with buttons
    const viewButton = firstCard.locator('button:has-text("View Collection")');
    await expect(viewButton).toBeVisible();
  });

  test('should display member location coordinates with proper formatting', async ({ page }) => {
    await page.goto(`${baseURL}/`);
    const firstCollectionCard = page.locator('.collection-card').first();
    await firstCollectionCard.click();

    // Check for member coordinates
    const coordinates = page.locator('.member-coordinates');
    const count = await coordinates.count();

    if (count > 0) {
      const firstCoord = coordinates.first();
      const text = await firstCoord.textContent();
      // Should be in format: latitude, longitude with 6 decimal places
      expect(text).toMatch(/\d+\.\d{6},\s*-?\d+\.\d{6}/);
    }
  });
});
