# Notification Panel Accessibility Tests

## Overview

This document describes the Playwright E2E tests for the notification panel component with comprehensive Axe-core accessibility assertions.

## Test File

**Location**: `tests/e2e/specs/notifications.spec.ts`

## Test Suite

The notification panel test suite includes 16 comprehensive tests covering:

1. **Automated Accessibility Scanning**
   - Zero critical violations
   - Zero serious violations
   - Full WCAG 2.1 AA compliance

2. **Screen Reader Support**
   - Unread count badge with aria-live="polite"
   - Notification list semantic structure
   - Descriptive text for all notifications

3. **Keyboard Accessibility**
   - Mark as read button keyboard access
   - Delete button keyboard access
   - Full keyboard navigation support
   - Focus management and return

4. **Visual Accessibility**
   - Color contrast verification
   - Badge visibility and readability
   - Distinct button labels

5. **Dynamic Content**
   - Notification count change announcements
   - Empty state handling
   - Focus management on open/close

## Test Cases

### 1. notification panel has zero critical accessibility violations
**Purpose**: Verify no critical accessibility issues exist
**Method**: Axe-core scan with impact filter
**Expected**: Zero critical violations

### 2. notification panel has zero serious accessibility violations
**Purpose**: Verify no serious accessibility issues exist
**Method**: Axe-core scan with impact filter
**Expected**: Zero serious violations

### 3. unread count badge is announced by screen readers with aria-live
**Purpose**: Verify screen reader announcements for count updates
**Method**: Check aria-live="polite" attribute and badge content
**Expected**: aria-live="polite" present, badge contains number

### 4. notification list items have proper semantic structure
**Purpose**: Verify semantic HTML structure for list items
**Method**: Check list role and item structure
**Expected**: Proper list/listitem roles or semantic elements

### 5. mark as read button is keyboard accessible
**Purpose**: Verify mark as read button can be activated via keyboard
**Method**: Focus and press Enter key
**Expected**: Button is focusable and activatable

### 6. delete button is keyboard accessible
**Purpose**: Verify delete button can be activated via keyboard
**Method**: Focus and press Enter key
**Expected**: Button is focusable and activatable

### 7. notification panel buttons have proper color contrast
**Purpose**: Verify sufficient color contrast on buttons
**Method**: Check button visibility and text content
**Expected**: All buttons visible with readable text

### 8. notification panel supports keyboard navigation
**Purpose**: Verify Tab key navigation through panel
**Method**: Tab through focusable elements
**Expected**: All elements reachable via Tab key

### 9. notification count changes are announced to screen readers
**Purpose**: Verify dynamic count updates are announced
**Method**: Check aria-live and aria-atomic attributes
**Expected**: aria-live="polite" present

### 10. notification panel has proper focus management
**Purpose**: Verify focus is properly managed in panel
**Method**: Check panel visibility and focusable elements
**Expected**: Panel visible with focusable elements

### 11. notification panel closes and returns focus to trigger
**Purpose**: Verify focus returns to trigger button on close
**Method**: Close panel and check focus
**Expected**: Focus returns to user menu button

### 12. notification items have descriptive text for screen readers
**Purpose**: Verify notification items have meaningful content
**Method**: Check item text content
**Expected**: Items contain descriptive text

### 13. notification panel passes full accessibility scan
**Purpose**: Comprehensive accessibility validation
**Method**: Full Axe-core scan with WCAG 2.1 AA tags
**Expected**: No critical or serious violations

### 14. notification panel maintains accessibility when empty
**Purpose**: Verify accessibility in empty state
**Method**: Scan empty state message
**Expected**: No violations in empty state

### 15. notification panel badge is visible and readable
**Purpose**: Verify badge visibility and size
**Method**: Check badge visibility and bounding box
**Expected**: Badge visible with sufficient size

### 16. mark as read and delete buttons have distinct labels
**Purpose**: Verify buttons have distinct accessible names
**Method**: Check aria-label and text content
**Expected**: Labels are different and descriptive

## Running the Tests

### Run all notification tests
```bash
npx playwright test tests/e2e/specs/notifications.spec.ts --run
```

### Run tests in headed mode (visible browser)
```bash
npx playwright test tests/e2e/specs/notifications.spec.ts --headed
```

### Run tests in UI mode
```bash
npx playwright test tests/e2e/specs/notifications.spec.ts --ui
```

### Run a specific test
```bash
npx playwright test tests/e2e/specs/notifications.spec.ts -g "unread count badge"
```

### View test report
```bash
npx playwright show-report
```

## Test Data Requirements

The tests assume:
- Frontend is running at http://localhost:5173
- User is authenticated (or tests handle authentication)
- Notification panel is accessible from user menu
- Test data attributes are present on components:
  - data-testid="user-menu-button"
  - data-testid="notification-panel"
  - data-testid="notification-unread-badge"
  - data-testid="notification-list"
  - data-testid="notification-item"
  - data-testid="mark-as-read-button"
  - data-testid="delete-notification-button"
  - data-testid="notification-empty-state"

## Component Requirements

For tests to pass, the NotificationPanel component must:

### Semantic Structure
- Use ul or ol for notification list
- Use li for notification items
- Use button elements for actions
- Use semantic HTML elements

### ARIA Attributes
- Badge: aria-live="polite" for count updates
- Badge: aria-atomic="true" (optional but recommended)
- Buttons: aria-label for icon-only buttons
- Panel: aria-expanded to indicate open/closed state

### Keyboard Support
- All buttons focusable via Tab key
- Buttons activatable with Enter/Space keys
- Escape key closes panel
- Focus management on open/close

### Visual Design
- Sufficient color contrast (4.5:1 for text, 3:1 for UI)
- Visible focus indicators
- Readable text at all zoom levels
- Responsive design for all screen sizes

## Accessibility Standards

All tests validate compliance with:
- WCAG 2.1 Level AA - Web Content Accessibility Guidelines
- ARIA Authoring Practices - Accessible Rich Internet Applications
- Keyboard Accessibility - Full keyboard navigation support
- Screen Reader Support - Proper semantic structure and ARIA

## Manual Testing Checklist

See NOTIFICATION_ACCESSIBILITY_CHECKLIST.md for comprehensive manual testing procedures including:
- Keyboard-only navigation
- Screen reader testing (NVDA, VoiceOver)
- Zoom and responsive design
- High contrast mode
- Color contrast verification

## Debugging Failed Tests

### Test fails: "notification panel has zero critical accessibility violations"
1. Check Axe-core output for violation details
2. Review component HTML structure
3. Verify ARIA attributes are correct
4. Check for missing semantic elements

### Test fails: "unread count badge is announced by screen readers"
1. Verify badge has aria-live="polite"
2. Check badge contains numeric content
3. Verify badge is visible in DOM

### Test fails: "mark as read button is keyboard accessible"
1. Verify button is in DOM and visible
2. Check button is not disabled
3. Verify button can receive focus
4. Check button responds to Enter key

### Test fails: "notification panel closes and returns focus"
1. Verify panel close mechanism works
2. Check focus management on close
3. Verify trigger button is focusable

## CI/CD Integration

These tests should run in CI/CD pipelines:

```bash
# Run all E2E tests including notifications
npx playwright test --run

# Run with specific browser
npx playwright test --project=chromium --run

# Generate HTML report
npx playwright test --reporter=html
```

## Performance Considerations

- Tests use await expect().toBeVisible() for proper waits
- Axe-core scans are performed after UI is fully rendered
- Tests handle optional elements gracefully
- No hardcoded delays; all waits are event-based

## Future Enhancements

- Add tests for notification polling (30-second refresh)
- Add tests for notification creation/deletion animations
- Add tests for notification filtering/sorting
- Add tests for notification persistence
- Add tests for notification sound/visual indicators
- Add tests for notification grouping by type

## References

- Playwright Documentation: https://playwright.dev/
- Axe-core Playwright Integration: https://github.com/dequelabs/axe-core-npm/tree/develop/packages/playwright
- WCAG 2.1 Guidelines: https://www.w3.org/WAI/WCAG21/quickref/
- ARIA Authoring Practices: https://www.w3.org/WAI/ARIA/apg/
