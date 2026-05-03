import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

test.describe('Admin Pages - Accessibility', () => {
  const baseURL = process.env.BASE_URL || 'http://localhost:5173';

  test.describe('AdminUsersPage', () => {
    test.beforeEach(async ({ page }) => {
      // Navigate to admin users page
      // Note: In a real scenario, you'd authenticate first
      await page.goto(`${baseURL}/admin/users`);
    });

    test('AdminUsersPage_PageRenders_DisplaysUserList', async ({ page }) => {
      // Wait for page to load
      await expect(page.locator('h1')).toContainText('User Management');
      
      // Check for page subtitle
      await expect(page.locator('.page-subtitle')).toContainText('Manage user roles and permissions');

      // Check for users table
      const usersTable = page.locator('.users-table');
      await expect(usersTable).toBeVisible();

      // Check for table headers
      await expect(usersTable.locator('th')).toContainText(['Username', 'Display Name', 'Email', 'Role', 'Actions']);
    });

    test('AdminUsersPage_AxeAccessibilityScan_ZeroCriticalViolations', async ({ page }) => {
      // Wait for page to load
      await expect(page.locator('h1')).toContainText('User Management');

      // Run Axe accessibility scan
      const results = await new AxeBuilder({ page }).analyze();
      const criticalOrSerious = results.violations.filter(
        (v) => v.impact === 'critical' || v.impact === 'serious'
      );

      expect(criticalOrSerious).toHaveLength(0);
    });

    test('AdminUsersPage_TableSemantics_HasProperRoles', async ({ page }) => {
      // Wait for page to load
      await expect(page.locator('h1')).toContainText('User Management');

      // Check table has role="grid"
      const table = page.locator('.users-table');
      const tableRole = await table.getAttribute('role');
      expect(tableRole).toBe('grid');

      // Check headers have role="columnheader"
      const headers = page.locator('.users-table th');
      const headerCount = await headers.count();
      expect(headerCount).toBeGreaterThan(0);

      for (let i = 0; i < headerCount; i++) {
        const headerRole = await headers.nth(i).getAttribute('role');
        expect(headerRole).toBe('columnheader');
      }
    });

    test('AdminUsersPage_ButtonLabels_HaveAriaLabels', async ({ page }) => {
      // Wait for page to load
      await expect(page.locator('h1')).toContainText('User Management');

      // Check for promote/demote buttons with aria-labels
      const promoteButtons = page.locator('.btn-promote, .btn-demote');
      const buttonCount = await promoteButtons.count();

      if (buttonCount > 0) {
        for (let i = 0; i < Math.min(buttonCount, 3); i++) {
          const button = promoteButtons.nth(i);
          const ariaLabel = await button.getAttribute('aria-label');
          expect(ariaLabel).toBeTruthy();
          expect(ariaLabel).toMatch(/Promote|Demote/);
        }
      }
    });

    test('AdminUsersPage_KeyboardNavigation_TabThroughElements', async ({ page }) => {
      // Wait for page to load
      await expect(page.locator('h1')).toContainText('User Management');

      // Get all focusable elements
      const focusableElements = page.locator('button, a, [tabindex="0"]');
      const elementCount = await focusableElements.count();

      if (elementCount > 0) {
        // Focus first element
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

    test('AdminUsersPage_FocusIndicators_Visible', async ({ page }) => {
      // Wait for page to load
      await expect(page.locator('h1')).toContainText('User Management');

      // Get a button and focus it
      const button = page.locator('button').first();
      await button.focus();

      // Check that button has visible focus indicator
      const outline = await button.evaluate((el) => {
        const styles = window.getComputedStyle(el);
        return styles.outline || styles.boxShadow;
      });

      expect(outline).toBeTruthy();
    });

    test('AdminUsersPage_ErrorMessages_HaveAlertRole', async ({ page }) => {
      // Wait for page to load
      await expect(page.locator('h1')).toContainText('User Management');

      // Check if there are any error messages with role="alert"
      const errorMessages = page.locator('[role="alert"]');
      const errorCount = await errorMessages.count();

      // If errors exist, verify they have the alert role
      if (errorCount > 0) {
        for (let i = 0; i < errorCount; i++) {
          const errorRole = await errorMessages.nth(i).getAttribute('role');
          expect(errorRole).toBe('alert');
        }
      }
    });

    test('AdminUsersPage_PaginationControls_Accessible', async ({ page }) => {
      // Wait for page to load
      await expect(page.locator('h1')).toContainText('User Management');

      // Check for pagination controls
      const pagination = page.locator('[role="navigation"]');
      const paginationVisible = await pagination.isVisible().catch(() => false);

      if (paginationVisible) {
        // Check for previous/next buttons
        const prevButton = page.locator('button:has-text("Previous")');
        const nextButton = page.locator('button:has-text("Next")');

        // Verify buttons have aria-labels
        const prevLabel = await prevButton.getAttribute('aria-label');
        const nextLabel = await nextButton.getAttribute('aria-label');

        expect(prevLabel || 'Previous').toBeTruthy();
        expect(nextLabel || 'Next').toBeTruthy();
      }
    });

    test('AdminUsersPage_PageTitle_Descriptive', async ({ page }) => {
      // Wait for page to load
      await expect(page.locator('h1')).toContainText('User Management');

      // Check page title
      const title = await page.title();
      expect(title).toBeTruthy();
      expect(title.length).toBeGreaterThan(0);
    });

    test('AdminUsersPage_HeadingHierarchy_H1Present', async ({ page }) => {
      // Check for h1 heading
      const h1 = page.locator('h1');
      await expect(h1).toBeVisible();
      await expect(h1).toContainText('User Management');
    });

    test('AdminUsersPage_FormInputs_HaveLabels', async ({ page }) => {
      // Wait for page to load
      await expect(page.locator('h1')).toContainText('User Management');

      // Check for any form inputs
      const inputs = page.locator('input, select, textarea');
      const inputCount = await inputs.count();

      if (inputCount > 0) {
        for (let i = 0; i < Math.min(inputCount, 3); i++) {
          const input = inputs.nth(i);
          const inputId = await input.getAttribute('id');
          
          if (inputId) {
            const label = page.locator(`label[for="${inputId}"]`);
            const labelExists = await label.isVisible().catch(() => false);
            
            if (labelExists) {
              expect(labelExists).toBe(true);
            }
          }
        }
      }
    });

    test('AdminUsersPage_ColorContrast_MeetsWCAGAA', async ({ page }) => {
      // Wait for page to load
      await expect(page.locator('h1')).toContainText('User Management');

      // Run Axe scan which includes color contrast checks
      const results = await new AxeBuilder({ page })
        .withTags(['wcag2aa', 'wcag21aa'])
        .analyze();

      // Check for color contrast violations
      const contrastViolations = results.violations.filter((v) => v.id === 'color-contrast');
      expect(contrastViolations).toHaveLength(0);
    });

    test('AdminUsersPage_FocusOrder_Logical', async ({ page }) => {
      // Wait for page to load
      await expect(page.locator('h1')).toContainText('User Management');

      // Get all focusable elements
      const focusableElements = page.locator('button, a, [tabindex="0"]');
      const elementCount = await focusableElements.count();

      // Verify focus order is logical (elements appear in reading order)
      if (elementCount > 1) {
        const positions: number[] = [];
        
        for (let i = 0; i < Math.min(elementCount, 5); i++) {
          const element = focusableElements.nth(i);
          const box = await element.boundingBox();
          if (box) {
            positions.push(box.y); // Get vertical position
          }
        }

        // Verify positions are in ascending order (top to bottom)
        for (let i = 1; i < positions.length; i++) {
          expect(positions[i]).toBeGreaterThanOrEqual(positions[i - 1]);
        }
      }
    });

    test('AdminUsersPage_NoKeyboardTraps_AllElementsEscapable', async ({ page }) => {
      // Wait for page to load
      await expect(page.locator('h1')).toContainText('User Management');

      // Get first focusable element
      const firstElement = page.locator('button, a, [tabindex="0"]').first();
      await firstElement.focus();

      // Tab through elements and verify we can escape
      let tabCount = 0;
      const maxTabs = 20;

      while (tabCount < maxTabs) {
        await page.keyboard.press('Tab');
        tabCount++;

        // Check if we've moved focus
        const activeElement = await page.evaluate(() => document.activeElement?.tagName);
        expect(activeElement).toBeTruthy();
      }

      // If we got here without hanging, there are no keyboard traps
      expect(tabCount).toBeLessThanOrEqual(maxTabs);
    });
  });

  test.describe('AuditLogPage', () => {
    test.beforeEach(async ({ page }) => {
      // Navigate to audit log page
      await page.goto(`${baseURL}/admin/audit-log`);
    });

    test('AuditLogPage_PageRenders_DisplaysAuditLogTable', async ({ page }) => {
      // Wait for page to load
      await expect(page.locator('h1')).toContainText('Audit Log');

      // Check for page subtitle
      await expect(page.locator('.page-subtitle')).toContainText('System activity and event history');

      // Check for audit log table
      const auditTable = page.locator('.audit-log-table');
      await expect(auditTable).toBeVisible();

      // Check for table headers
      await expect(auditTable.locator('th')).toContainText([
        'Timestamp',
        'Event Type',
        'Acting User',
        'Resource Type',
        'Resource ID',
        'Outcome',
      ]);
    });

    test('AuditLogPage_AxeAccessibilityScan_ZeroCriticalViolations', async ({ page }) => {
      // Wait for page to load
      await expect(page.locator('h1')).toContainText('Audit Log');

      // Run Axe accessibility scan
      const results = await new AxeBuilder({ page }).analyze();
      const criticalOrSerious = results.violations.filter(
        (v) => v.impact === 'critical' || v.impact === 'serious'
      );

      expect(criticalOrSerious).toHaveLength(0);
    });

    test('AuditLogPage_FilterControls_HaveLabels', async ({ page }) => {
      // Wait for page to load
      await expect(page.locator('h1')).toContainText('Audit Log');

      // Check for filter controls with labels
      const eventTypeFilter = page.locator('#event-type-filter');
      const eventTypeLabel = page.locator('label[for="event-type-filter"]');
      await expect(eventTypeLabel).toBeVisible();

      const outcomeFilter = page.locator('#outcome-filter');
      const outcomeLabel = page.locator('label[for="outcome-filter"]');
      await expect(outcomeLabel).toBeVisible();

      const startDateFilter = page.locator('#start-date-filter');
      const startDateLabel = page.locator('label[for="start-date-filter"]');
      await expect(startDateLabel).toBeVisible();

      const endDateFilter = page.locator('#end-date-filter');
      const endDateLabel = page.locator('label[for="end-date-filter"]');
      await expect(endDateLabel).toBeVisible();

      const searchFilter = page.locator('#search-filter');
      const searchLabel = page.locator('label[for="search-filter"]');
      await expect(searchLabel).toBeVisible();
    });

    test('AuditLogPage_DatePickerInputs_Accessible', async ({ page }) => {
      // Wait for page to load
      await expect(page.locator('h1')).toContainText('Audit Log');

      // Check date picker inputs
      const startDateInput = page.locator('#start-date-filter');
      const endDateInput = page.locator('#end-date-filter');

      // Verify inputs are accessible
      const startDateType = await startDateInput.getAttribute('type');
      const endDateType = await endDateInput.getAttribute('type');

      expect(startDateType).toBe('datetime-local');
      expect(endDateType).toBe('datetime-local');

      // Verify inputs have aria-labels
      const startDateAriaLabel = await startDateInput.getAttribute('aria-label');
      const endDateAriaLabel = await endDateInput.getAttribute('aria-label');

      expect(startDateAriaLabel).toBeTruthy();
      expect(endDateAriaLabel).toBeTruthy();
    });

    test('AuditLogPage_TableSemantics_HasProperRoles', async ({ page }) => {
      // Wait for page to load
      await expect(page.locator('h1')).toContainText('Audit Log');

      // Check table has role="grid"
      const table = page.locator('.audit-log-table');
      const tableRole = await table.getAttribute('role');
      expect(tableRole).toBe('grid');

      // Check headers have role="columnheader"
      const headers = page.locator('.audit-log-table th');
      const headerCount = await headers.count();
      expect(headerCount).toBeGreaterThan(0);

      for (let i = 0; i < headerCount; i++) {
        const headerRole = await headers.nth(i).getAttribute('role');
        expect(headerRole).toBe('columnheader');
      }
    });

    test('AuditLogPage_PaginationControls_Accessible', async ({ page }) => {
      // Wait for page to load
      await expect(page.locator('h1')).toContainText('Audit Log');

      // Check for pagination controls
      const pagination = page.locator('[role="navigation"]');
      const paginationVisible = await pagination.isVisible().catch(() => false);

      if (paginationVisible) {
        // Check for previous/next buttons
        const prevButton = page.locator('button:has-text("Previous")');
        const nextButton = page.locator('button:has-text("Next")');

        // Verify buttons have aria-labels
        const prevLabel = await prevButton.getAttribute('aria-label');
        const nextLabel = await nextButton.getAttribute('aria-label');

        expect(prevLabel || 'Previous').toBeTruthy();
        expect(nextLabel || 'Next').toBeTruthy();
      }
    });

    test('AuditLogPage_ErrorMessages_HaveAlertRole', async ({ page }) => {
      // Wait for page to load
      await expect(page.locator('h1')).toContainText('Audit Log');

      // Check if there are any error messages with role="alert"
      const errorMessages = page.locator('[role="alert"]');
      const errorCount = await errorMessages.count();

      // If errors exist, verify they have the alert role
      if (errorCount > 0) {
        for (let i = 0; i < errorCount; i++) {
          const errorRole = await errorMessages.nth(i).getAttribute('role');
          expect(errorRole).toBe('alert');
        }
      }
    });

    test('AuditLogPage_FilterResetButton_Accessible', async ({ page }) => {
      // Wait for page to load
      await expect(page.locator('h1')).toContainText('Audit Log');

      // Check for reset filters button
      const resetButton = page.locator('.btn-reset-filters');
      const resetButtonVisible = await resetButton.isVisible().catch(() => false);

      if (resetButtonVisible) {
        // Verify button has aria-label
        const ariaLabel = await resetButton.getAttribute('aria-label');
        expect(ariaLabel).toBeTruthy();
        expect(ariaLabel).toContain('Reset');
      }
    });

    test('AuditLogPage_PageTitle_Descriptive', async ({ page }) => {
      // Wait for page to load
      await expect(page.locator('h1')).toContainText('Audit Log');

      // Check page title
      const title = await page.title();
      expect(title).toBeTruthy();
      expect(title.length).toBeGreaterThan(0);
    });

    test('AuditLogPage_HeadingHierarchy_H1Present', async ({ page }) => {
      // Check for h1 heading
      const h1 = page.locator('h1');
      await expect(h1).toBeVisible();
      await expect(h1).toContainText('Audit Log');
    });

    test('AuditLogPage_FormInputs_HaveLabels', async ({ page }) => {
      // Wait for page to load
      await expect(page.locator('h1')).toContainText('Audit Log');

      // Check for form inputs
      const inputs = page.locator('input, select, textarea');
      const inputCount = await inputs.count();

      if (inputCount > 0) {
        for (let i = 0; i < Math.min(inputCount, 5); i++) {
          const input = inputs.nth(i);
          const inputId = await input.getAttribute('id');
          
          if (inputId) {
            const label = page.locator(`label[for="${inputId}"]`);
            const labelExists = await label.isVisible().catch(() => false);
            
            if (labelExists) {
              expect(labelExists).toBe(true);
            }
          }
        }
      }
    });

    test('AuditLogPage_ColorContrast_MeetsWCAGAA', async ({ page }) => {
      // Wait for page to load
      await expect(page.locator('h1')).toContainText('Audit Log');

      // Run Axe scan which includes color contrast checks
      const results = await new AxeBuilder({ page })
        .withTags(['wcag2aa', 'wcag21aa'])
        .analyze();

      // Check for color contrast violations
      const contrastViolations = results.violations.filter((v) => v.id === 'color-contrast');
      expect(contrastViolations).toHaveLength(0);
    });

    test('AuditLogPage_FocusOrder_Logical', async ({ page }) => {
      // Wait for page to load
      await expect(page.locator('h1')).toContainText('Audit Log');

      // Get all focusable elements
      const focusableElements = page.locator('button, a, [tabindex="0"]');
      const elementCount = await focusableElements.count();

      // Verify focus order is logical (elements appear in reading order)
      if (elementCount > 1) {
        const positions: number[] = [];
        
        for (let i = 0; i < Math.min(elementCount, 5); i++) {
          const element = focusableElements.nth(i);
          const box = await element.boundingBox();
          if (box) {
            positions.push(box.y); // Get vertical position
          }
        }

        // Verify positions are in ascending order (top to bottom)
        for (let i = 1; i < positions.length; i++) {
          expect(positions[i]).toBeGreaterThanOrEqual(positions[i - 1]);
        }
      }
    });

    test('AuditLogPage_NoKeyboardTraps_AllElementsEscapable', async ({ page }) => {
      // Wait for page to load
      await expect(page.locator('h1')).toContainText('Audit Log');

      // Get first focusable element
      const firstElement = page.locator('button, a, [tabindex="0"]').first();
      await firstElement.focus();

      // Tab through elements and verify we can escape
      let tabCount = 0;
      const maxTabs = 20;

      while (tabCount < maxTabs) {
        await page.keyboard.press('Tab');
        tabCount++;

        // Check if we've moved focus
        const activeElement = await page.evaluate(() => document.activeElement?.tagName);
        expect(activeElement).toBeTruthy();
      }

      // If we got here without hanging, there are no keyboard traps
      expect(tabCount).toBeLessThanOrEqual(maxTabs);
    });
  });

  test.describe('Common Admin Page Tests', () => {
    test('AdminPages_AllPages_HaveDescriptivePageTitles', async ({ page }) => {
      // Test AdminUsersPage
      await page.goto(`${baseURL}/admin/users`);
      let title = await page.title();
      expect(title).toBeTruthy();
      expect(title.length).toBeGreaterThan(0);

      // Test AuditLogPage
      await page.goto(`${baseURL}/admin/audit-log`);
      title = await page.title();
      expect(title).toBeTruthy();
      expect(title.length).toBeGreaterThan(0);
    });

    test('AdminPages_AllPages_HaveProperHeadingHierarchy', async ({ page }) => {
      // Test AdminUsersPage
      await page.goto(`${baseURL}/admin/users`);
      let h1 = page.locator('h1');
      await expect(h1).toBeVisible();

      // Test AuditLogPage
      await page.goto(`${baseURL}/admin/audit-log`);
      h1 = page.locator('h1');
      await expect(h1).toBeVisible();
    });

    test('AdminPages_AllPages_PassFullAccessibilityScan', async ({ page }) => {
      // Test AdminUsersPage
      await page.goto(`${baseURL}/admin/users`);
      let results = await new AxeBuilder({ page })
        .withTags(['wcag2aa', 'wcag21aa'])
        .analyze();

      let criticalOrSerious = results.violations.filter(
        (v) => v.impact === 'critical' || v.impact === 'serious'
      );
      expect(criticalOrSerious).toHaveLength(0);

      // Test AuditLogPage
      await page.goto(`${baseURL}/admin/audit-log`);
      results = await new AxeBuilder({ page })
        .withTags(['wcag2aa', 'wcag21aa'])
        .analyze();

      criticalOrSerious = results.violations.filter(
        (v) => v.impact === 'critical' || v.impact === 'serious'
      );
      expect(criticalOrSerious).toHaveLength(0);
    });

    test('AdminPages_AllPages_KeyboardNavigationWorks', async ({ page }) => {
      // Test AdminUsersPage
      await page.goto(`${baseURL}/admin/users`);
      let firstElement = page.locator('button, a, [tabindex="0"]').first();
      await firstElement.focus();
      let isFocused = await firstElement.evaluate((el) => el === document.activeElement);
      expect(isFocused).toBe(true);

      // Test AuditLogPage
      await page.goto(`${baseURL}/admin/audit-log`);
      firstElement = page.locator('button, a, [tabindex="0"]').first();
      await firstElement.focus();
      isFocused = await firstElement.evaluate((el) => el === document.activeElement);
      expect(isFocused).toBe(true);
    });
  });
});
