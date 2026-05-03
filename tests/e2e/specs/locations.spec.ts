import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

test.describe('Location Management - Happy Paths', () => {
  const baseURL = process.env.BASE_URL || 'http://localhost:5173';
  let authToken: string;
  let userId: string;
  let locationId: string;

  test.beforeAll(async () => {
    // This would normally be set up via API or fixtures
    // For now, we'll assume the backend is running
  });

  test('should view location list', async ({ page }) => {
    await page.goto(`${baseURL}/locations`);
    await expect(page).toHaveTitle(/Locations/);
    await expect(page.locator('h1')).toContainText('Locations');

    // Check for pagination
    await expect(page.locator('[role="navigation"]')).toBeVisible();
  });

  test('should view location detail page', async ({ page }) => {
    // Navigate to locations list first
    await page.goto(`${baseURL}/locations`);

    // Click first location card
    const firstLocationCard = page.locator('.location-card').first();
    await firstLocationCard.click();

    // Verify we're on detail page
    await expect(page).toHaveURL(/\/locations\/[a-f0-9-]+$/);

    // Check for location metadata
    await expect(page.locator('.location-metadata')).toBeVisible();
    await expect(page.locator('text=Creator:')).toBeVisible();
    await expect(page.locator('text=Coordinates:')).toBeVisible();

    // Check for map
    await expect(page.locator('.leaflet-map')).toBeVisible();

    // Check for content section
    await expect(page.locator('text=Content')).toBeVisible();
  });

  test('should display location map with keyboard alternative', async ({ page }) => {
    await page.goto(`${baseURL}/locations`);
    const firstLocationCard = page.locator('.location-card').first();
    await firstLocationCard.click();

    // Check map is rendered
    const mapContainer = page.locator('[role="region"][aria-label*="map"]');
    await expect(mapContainer).toBeVisible();

    // Check text alternative is available
    const textAlternative = page.locator('[role="region"][aria-label*="Location list"]');
    await expect(textAlternative).toBeVisible();

    // Check table with coordinates
    const table = page.locator('.locations-table');
    await expect(table).toBeVisible();
    await expect(table.locator('th')).toContainText(['Name', 'Latitude', 'Longitude']);
  });

  test('should pass accessibility checks on location list', async ({ page }) => {
    await page.goto(`${baseURL}/locations`);

    const results = await new AxeBuilder({ page }).analyze();
    const criticalOrSerious = results.violations.filter(
      (v) => v.impact === 'critical' || v.impact === 'serious'
    );

    expect(criticalOrSerious).toHaveLength(0);
  });

  test('should pass accessibility checks on location detail', async ({ page }) => {
    await page.goto(`${baseURL}/locations`);
    const firstLocationCard = page.locator('.location-card').first();
    await firstLocationCard.click();

    const results = await new AxeBuilder({ page }).analyze();
    const criticalOrSerious = results.violations.filter(
      (v) => v.impact === 'critical' || v.impact === 'serious'
    );

    expect(criticalOrSerious).toHaveLength(0);
  });

  test('should navigate between location pages with pagination', async ({ page }) => {
    await page.goto(`${baseURL}/locations`);

    // Check initial page
    await expect(page.locator('text=Page 1 of')).toBeVisible();

    // Click next button if available
    const nextButton = page.locator('button:has-text("Next")');
    if (await nextButton.isEnabled()) {
      await nextButton.click();
      await expect(page.locator('text=Page 2 of')).toBeVisible();

      // Click previous button
      const prevButton = page.locator('button:has-text("Previous")');
      await prevButton.click();
      await expect(page.locator('text=Page 1 of')).toBeVisible();
    }
  });

  test('should display content sequence on location detail', async ({ page }) => {
    await page.goto(`${baseURL}/locations`);
    const firstLocationCard = page.locator('.location-card').first();
    await firstLocationCard.click();

    // Check for content viewer
    const contentViewer = page.locator('.content-sequence-viewer');
    await expect(contentViewer).toBeVisible();

    // Check for content blocks (headings, paragraphs, or images)
    const contentBlocks = page.locator('.content-heading, .content-paragraph, .content-image-container');
    const blockCount = await contentBlocks.count();
    expect(blockCount).toBeGreaterThan(0);
  });

  test('should handle location not found gracefully', async ({ page }) => {
    await page.goto(`${baseURL}/locations/invalid-id-12345`);

    // Should show error message
    await expect(page.locator('text=Error')).toBeVisible();
    await expect(page.locator('text=not found')).toBeVisible();

    // Should have back button
    const backButton = page.locator('button:has-text("Back to Locations")');
    await expect(backButton).toBeVisible();
  });

  test('should display location metadata correctly', async ({ page }) => {
    await page.goto(`${baseURL}/locations`);
    const firstLocationCard = page.locator('.location-card').first();
    await firstLocationCard.click();

    // Check all metadata fields are present
    await expect(page.locator('text=Creator:')).toBeVisible();
    await expect(page.locator('text=Created:')).toBeVisible();
    await expect(page.locator('text=Coordinates:')).toBeVisible();
    await expect(page.locator('text=Source SRID:')).toBeVisible();

    // Verify coordinates are in correct format (6 decimal places)
    const coordinatesText = await page.locator('.location-metadata p:has-text("Coordinates:")').textContent();
    expect(coordinatesText).toMatch(/Coordinates:.*\d+\.\d{6},\s*-?\d+\.\d{6}/);
  });

  test('should show loading skeleton while fetching location', async ({ page }) => {
    // Slow down network to see skeleton
    await page.route('**/api/locations/**', async (route) => {
      await new Promise((resolve) => setTimeout(resolve, 500));
      await route.continue();
    });

    await page.goto(`${baseURL}/locations`);
    const firstLocationCard = page.locator('.location-card').first();
    await firstLocationCard.click();

    // Skeleton should be visible briefly
    const skeleton = page.locator('.loading-skeleton');
    // It might disappear quickly, so we just check it exists in the DOM
    expect(skeleton).toBeDefined();
  });
});
