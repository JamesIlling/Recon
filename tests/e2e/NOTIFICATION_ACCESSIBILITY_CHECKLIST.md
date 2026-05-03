# Notification Panel - Manual Accessibility Checklist

This document provides a manual accessibility verification checklist for the notification panel component, complementing the automated Axe-core tests.

## WCAG 2.1 Level AA Compliance

All items below must be verified manually before the notification panel feature is considered complete.

---

## 1. Keyboard Navigation

### 1.1 Tab Order
- [ ] User menu button is reachable via Tab key
- [ ] Notification panel opens when user menu button is activated with Enter or Space
- [ ] All buttons within the notification panel are reachable via Tab key
- [ ] Tab order follows logical reading order (left-to-right, top-to-bottom)
- [ ] Tab order does not skip any interactive elements
- [ ] Shift+Tab navigates backwards through elements in reverse order

### 1.2 Focus Visibility
- [ ] Focus indicator is clearly visible on all focusable elements
- [ ] Focus indicator has sufficient contrast (minimum 3:1 ratio)
- [ ] Focus indicator is not suppressed or hidden
- [ ] Focus indicator is visible on buttons, links, and interactive elements

### 1.3 Keyboard Activation
- [ ] User menu button can be activated with Enter key
- [ ] User menu button can be activated with Space key
- [ ] Mark as read button can be activated with Enter key
- [ ] Delete button can be activated with Enter key
- [ ] Notification items can be navigated with arrow keys (if applicable)
- [ ] Escape key closes the notification panel

### 1.4 Focus Management
- [ ] Focus is placed on the first interactive element when panel opens
- [ ] Focus is trapped within the panel while it's open (optional but recommended)
- [ ] Focus returns to the user menu button when panel closes
- [ ] Focus is not lost when notifications are marked as read or deleted

---

## 2. Screen Reader Announcements

### 2.1 Unread Count Badge
- [ ] Screen reader announces the unread notification count
- [ ] Count updates are announced when new notifications arrive
- [ ] aria-live="polite" is set on the badge
- [ ] Badge text is descriptive (e.g., "3 unread notifications")

### 2.2 Notification Panel
- [ ] Screen reader announces "Notification panel" or similar when opened
- [ ] Panel is identified as a region or dialog
- [ ] Panel purpose is clear to screen reader users

### 2.3 Notification Items
- [ ] Each notification item is announced as a list item
- [ ] Notification content is read in logical order
- [ ] Event type is announced (e.g., "Edit approved", "Membership request")
- [ ] Resource name is announced (e.g., "Location: My Park")
- [ ] Timestamp is announced (e.g., "2 minutes ago")

### 2.4 Action Buttons
- [ ] "Mark as read" button is announced with clear label
- [ ] "Delete" button is announced with clear label
- [ ] Button purpose is clear without additional context

### 2.5 Empty State
- [ ] Empty state message is announced when no notifications exist
- [ ] Message is clear and helpful (e.g., "No unread notifications")

---

## 3. Color and Contrast

### 3.1 Text Contrast
- [ ] Notification text has minimum 4.5:1 contrast ratio against background
- [ ] Button text has minimum 4.5:1 contrast ratio against button background
- [ ] Unread badge text has minimum 4.5:1 contrast ratio
- [ ] Timestamp text has minimum 4.5:1 contrast ratio

### 3.2 UI Component Contrast
- [ ] Focus indicator has minimum 3:1 contrast ratio
- [ ] Button borders have minimum 3:1 contrast ratio
- [ ] Unread badge has minimum 3:1 contrast ratio against background

### 3.3 Color Alone
- [ ] Information is not conveyed by color alone
- [ ] Unread notifications are not distinguished by color alone
- [ ] Read notifications are not distinguished by color alone
- [ ] Error states are not indicated by color alone

---

## 4. Semantic Structure

### 4.1 HTML Semantics
- [ ] Notification panel uses semantic HTML elements
- [ ] Buttons are implemented as `<button>` elements
- [ ] Links are implemented as `<a>` elements
- [ ] Notification list uses `<ul>` or `<ol>` with `<li>` items
- [ ] Headings use appropriate levels (`<h1>`, `<h2>`, etc.)

### 4.2 ARIA Attributes
- [ ] aria-live="polite" is used for dynamic updates
- [ ] aria-label is used for icon-only buttons
- [ ] aria-describedby links error messages to form fields (if applicable)
- [ ] aria-expanded indicates panel open/closed state
- [ ] role attributes are used correctly (if needed)

### 4.3 Labels and Names
- [ ] All buttons have accessible names
- [ ] All interactive elements have accessible names
- [ ] Accessible names are descriptive and unique

---

## 5. Zoom and Responsive Design

### 5.1 Zoom to 200%
- [ ] Notification panel is fully visible at 200% zoom
- [ ] No content is clipped or hidden at 200% zoom
- [ ] All buttons are clickable at 200% zoom
- [ ] Text is readable at 200% zoom
- [ ] Horizontal scrolling is not required (or minimal)

### 5.2 Responsive Design
- [ ] Notification panel is usable on mobile devices
- [ ] Notification panel is usable on tablets
- [ ] Notification panel is usable on desktop
- [ ] Touch targets are at least 44x44 pixels (mobile)
- [ ] Buttons are appropriately sized for touch interaction

---

## 6. High Contrast Mode

### 6.1 OS-Level High Contrast
- [ ] Notification panel is visible in high-contrast mode
- [ ] Text is readable in high-contrast mode
- [ ] Buttons are distinguishable in high-contrast mode
- [ ] Focus indicators are visible in high-contrast mode
- [ ] No information is lost in high-contrast mode

---

## 7. Motion and Animation

### 7.1 Reduced Motion
- [ ] Panel animations respect prefers-reduced-motion setting
- [ ] Animations are not distracting or disorienting
- [ ] Panel is still usable with animations disabled

---

## 8. Testing Tools and Procedures

### 8.1 Screen Reader Testing
Tools: NVDA (Windows) + Chrome, or VoiceOver (macOS) + Safari

1. Open the notification panel
2. Verify the panel is announced
3. Navigate through notifications with arrow keys
4. Verify each notification is announced correctly
5. Activate mark as read button
6. Verify action is announced
7. Close the panel
8. Verify focus returns to trigger button

### 8.2 Keyboard-Only Testing
1. Disable mouse/trackpad
2. Use Tab to navigate to user menu button
3. Press Enter to open notification panel
4. Use Tab to navigate through all buttons
5. Press Enter to activate buttons
6. Press Escape to close panel
7. Verify all actions work without mouse

### 8.3 Zoom Testing
1. Set browser zoom to 200%
2. Verify notification panel is fully visible
3. Verify all buttons are clickable
4. Verify text is readable
5. Verify no horizontal scrolling is required

### 8.4 High Contrast Testing
1. Enable OS-level high contrast mode
2. Verify notification panel is visible
3. Verify text is readable
4. Verify buttons are distinguishable
5. Verify focus indicators are visible

### 8.5 Color Contrast Testing
Tools: WebAIM Contrast Checker, Axe DevTools, or similar

1. Measure contrast ratio of notification text
2. Measure contrast ratio of button text
3. Measure contrast ratio of badge text
4. Measure contrast ratio of focus indicator
5. Verify all ratios meet WCAG AA standards

---

## 9. Automated Testing Results

### 9.1 Axe-Core Scan
- [ ] Run: `npx playwright test tests/e2e/specs/notifications.spec.ts --run`
- [ ] Result: All tests pass
- [ ] Zero critical violations
- [ ] Zero serious violations
- [ ] Any warnings are documented and justified

### 9.2 Test Coverage
- [ ] Test: "notification panel has zero critical accessibility violations" - PASS
- [ ] Test: "notification panel has zero serious accessibility violations" - PASS
- [ ] Test: "unread count badge is announced by screen readers with aria-live" - PASS
- [ ] Test: "notification list items have proper semantic structure" - PASS
- [ ] Test: "mark as read button is keyboard accessible" - PASS
- [ ] Test: "delete button is keyboard accessible" - PASS
- [ ] Test: "notification panel buttons have proper color contrast" - PASS
- [ ] Test: "notification panel supports keyboard navigation" - PASS
- [ ] Test: "notification count changes are announced to screen readers" - PASS
- [ ] Test: "notification panel has proper focus management" - PASS
- [ ] Test: "notification panel closes and returns focus to trigger" - PASS
- [ ] Test: "notification items have descriptive text for screen readers" - PASS
- [ ] Test: "notification panel passes full accessibility scan" - PASS
- [ ] Test: "notification panel maintains accessibility when empty" - PASS
- [ ] Test: "notification panel badge is visible and readable" - PASS
- [ ] Test: "mark as read and delete buttons have distinct labels" - PASS

---

## 10. Sign-Off

### 10.1 Verification
- [ ] All manual tests completed
- [ ] All automated tests passing
- [ ] No accessibility violations found
- [ ] Feature is WCAG 2.1 Level AA compliant

### 10.2 Sign-Off
- [ ] Accessibility review completed by: ________________
- [ ] Date: ________________
- [ ] Notes: ________________

---

## References

- WCAG 2.1 Guidelines: https://www.w3.org/WAI/WCAG21/quickref/
- ARIA Authoring Practices Guide: https://www.w3.org/WAI/ARIA/apg/
- WebAIM Contrast Checker: https://webaim.org/resources/contrastchecker/
- Axe DevTools: https://www.deque.com/axe/devtools/
- NVDA Screen Reader: https://www.nvaccess.org/
- VoiceOver (macOS): https://www.apple.com/accessibility/voiceover/
