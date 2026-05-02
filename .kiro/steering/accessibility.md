---
inclusion: always
---

# Accessibility

All user-facing features MUST meet **WCAG 2.1 Level AA** conformance. Accessibility is a non-negotiable part of the Definition of Done — not an afterthought.

---

## Standards

- Target: **WCAG 2.1 Level AA**
- Automated gate: `@axe-core/playwright` — zero critical or serious violations required before merge
- Manual validation: screen reader testing and keyboard-only navigation are required for complex interactive components (maps, modals, rich content editors)

> Full WCAG 2.1 AA validation requires manual testing with assistive technologies and expert accessibility review. Automated tooling catches a subset of issues — passing axe-core is necessary but not sufficient.

---

## Required for Every UI Component

### Semantic HTML
- Use semantic elements: `<nav>`, `<main>`, `<header>`, `<footer>`, `<section>`, `<article>`, `<button>`, `<a>`.
- Never use `<div>` or `<span>` as interactive elements — use `<button>` for actions, `<a>` for navigation.
- Heading levels (`<h1>`-`<h3>`) MUST reflect document hierarchy, not visual styling.

### Interactive Controls
- Every button, link, input, toggle, and select MUST have an accessible name.
- Prefer visible labels over `aria-label`; use `aria-label` only when a visible label is not possible.
- All interactive elements MUST be reachable and operable via keyboard (Tab, Enter, Space, arrow keys where appropriate).
- A visible focus indicator MUST be present on all focusable elements — never suppress `outline` without providing an equivalent.

### Images
- Informative images MUST have meaningful `alt` text describing their content or purpose.
- Decorative images MUST use `alt=""` so screen readers skip them.
- Images in `<picture>` elements with `srcset` MUST carry `alt` on the `<img>` element.
- User-uploaded images (Location ContentBlocks, collection images, avatars) MUST prompt the uploader for alt text.

### Forms
- Every `<input>`, `<select>`, and `<textarea>` MUST have an associated `<label>` (via `for`/`id` or wrapping).
- Validation error messages MUST be programmatically associated with their field using `aria-describedby`.
- Required fields MUST be indicated both visually and via `aria-required="true"`.
- Error summaries at the top of a form MUST use `role="alert"` or `aria-live="polite"` so screen readers announce them.

### Colour and Contrast
- Normal text (less than 18pt / less than 14pt bold): minimum contrast ratio **4.5:1** against background.
- Large text (18pt or above / 14pt bold or above): minimum contrast ratio **3:1**.
- UI components and focus indicators: minimum contrast ratio **3:1**.
- Never convey information by colour alone — always pair colour with a text label, icon, or pattern.

### Maps (Leaflet)
- The Leaflet map MUST NOT be the only way to access Location data.
- A keyboard-accessible text alternative MUST always be available: a list of all Locations with their names and coordinates, rendered as standard HTML below or alongside the map.
- Map pins and overlays MUST have accessible tooltips or labels readable by assistive technologies.

### Notifications and Dynamic Content
- In-app notification counts and new notification arrivals MUST use `aria-live="polite"` so screen readers announce updates without interrupting the user.
- Loading skeletons and placeholders MUST use `aria-busy="true"` on the container while data is in flight.
- Modal dialogs MUST trap focus within the modal while open and return focus to the trigger element on close.

---

## Testing Requirements

### Automated (required in CI)
- Run `@axe-core/playwright` on every page as part of the Playwright E2E suite.
- Zero **critical** or **serious** axe violations are permitted before a feature is merged.
- Include accessibility assertions in existing Playwright tests — do not create a separate accessibility-only suite.

```typescript
// Example — add to every Playwright page test
import AxeBuilder from '@axe-core/playwright';

test('page has no critical accessibility violations', async ({ page }) => {
  await page.goto('/');
  const results = await new AxeBuilder({ page }).analyze();
  const criticalOrSerious = results.violations.filter(
    v => v.impact === 'critical' || v.impact === 'serious'
  );
  expect(criticalOrSerious).toHaveLength(0);
});
```

### Manual (required before feature sign-off)
- **Keyboard-only navigation**: verify all interactive elements are reachable and operable without a mouse.
- **Screen reader smoke test**: verify page structure, headings, form labels, and error messages are announced correctly (use NVDA + Chrome or VoiceOver + Safari).
- **Zoom to 200%**: verify no content is clipped or overlapping at double zoom.
- **High-contrast mode**: verify the UI remains usable with OS-level high-contrast settings enabled.

---

## Definition of Done Gate

A feature is NOT complete until:

1. `@axe-core/playwright` passes with zero critical or serious violations on all affected pages.
2. Keyboard-only navigation has been manually verified for all new interactive components.
3. All new images have `alt` text (or `alt=""` for decorative images).
4. All new form fields have associated labels and error messages linked via `aria-describedby`.
5. Colour contrast has been verified for all new text and UI components.

---

## Accessibility Review Triggers

Stop and raise an accessibility concern WHEN:
- A new interactive component is added (modal, dropdown, map overlay, rich text editor).
- A new form is introduced or an existing form is modified.
- Colour palette or typography changes are made.
- Dynamic content updates (notifications, live data) are introduced.
- A map or data visualisation is added.
