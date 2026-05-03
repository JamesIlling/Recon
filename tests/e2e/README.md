# End-to-End Tests

Playwright-based end-to-end tests for the Location Management application, including accessibility testing with axe-core.

## Setup

```bash
npm install
npx playwright install
```

## Running Tests

```bash
# Run all tests
npm test

# Run tests in headed mode (visible browser)
npm run test:headed

# Run tests in UI mode (interactive)
npm run test:ui

# Debug tests
npm run test:debug

# View test report
npm run test:report
```

## Test Structure

Tests are organized in `specs/` directory:

```
specs/
  example.spec.ts       # Sample tests
  auth.spec.ts          # Authentication flows
  locations.spec.ts     # Location CRUD operations
  collections.spec.ts   # Collection management
  admin.spec.ts         # Admin features
```

## Writing Tests

### Basic Test

```typescript
import { test, expect } from '@playwright/test';

test('user can navigate to home', async ({ page }) => {
  await page.goto('/');
  await expect(page).toHaveTitle(/Location Management/i);
});
```

### Accessibility Testing

```typescript
import { test, expect } from '@playwright/test';
import { injectAxe, checkA11y } from 'axe-playwright';

test('page is accessible', async ({ page }) => {
  await page.goto('/');
  await injectAxe(page);
  
  const violations = await checkA11y(page);
  const criticalOrSerious = violations.filter(
    (v) => v.impact === 'critical' || v.impact === 'serious'
  );
  expect(criticalOrSerious).toHaveLength(0);
});
```

### User Interactions

```typescript
test('user can login', async ({ page }) => {
  await page.goto('/login');
  
  // Fill form
  await page.fill('input[name="username"]', 'testuser');
  await page.fill('input[name="password"]', 'password123');
  
  // Submit
  await page.click('button[type="submit"]');
  
  // Verify redirect
  await expect(page).toHaveURL('/');
});
```

## Best Practices

1. **Use semantic selectors** — prefer role-based selectors over test IDs
2. **Test user flows** — test complete workflows, not individual components
3. **Check accessibility** — include axe-core checks on all pages
4. **Use fixtures** — leverage Playwright fixtures for setup/teardown
5. **Isolate tests** — each test should be independent
6. **Use page objects** — create helper classes for complex pages

## Configuration

- **baseURL**: `http://localhost:5173`
- **Browsers**: Chromium, Firefox, WebKit
- **Retries**: 2 in CI, 0 locally
- **Timeout**: 30 seconds per test
- **Screenshots**: Captured on failure
- **Traces**: Recorded on first retry

## CI/CD Integration

Tests run automatically on pull requests. To run locally before pushing:

```bash
npm test
```

All tests must pass before merging.

## Troubleshooting

### Tests timeout

Increase timeout in `playwright.config.ts`:

```typescript
use: {
  navigationTimeout: 30000,
  actionTimeout: 10000,
}
```

### Tests fail in CI but pass locally

- Check that the frontend dev server is running
- Verify baseURL matches your environment
- Check for timing issues with `waitFor()`

### Accessibility violations

Review the axe-core report in test output. Common issues:

- Missing alt text on images
- Missing labels on form inputs
- Insufficient color contrast
- Missing heading hierarchy

## Resources

- [Playwright Documentation](https://playwright.dev/)
- [axe-core Playwright Integration](https://github.com/dequelabs/axe-core-npm/tree/develop/packages/playwright)
- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)
