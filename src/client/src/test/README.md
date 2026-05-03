# Testing Setup

This directory contains test configuration, utilities, and helpers for the Location Management frontend.

## Overview

The frontend uses the following testing stack:
- **Vitest** — fast unit test runner with ESM support
- **React Testing Library** — component testing focused on user interactions
- **jsdom** — DOM environment for testing React components

## Configuration

### `setup.ts`

The setup file runs before all tests and:
- Imports `@testing-library/jest-dom` for extended matchers
- Cleans up the DOM after each test
- Mocks `window.matchMedia` for responsive design testing

### `vitest.config.ts` (root)

Located at `src/client/vitest.config.ts`, this configuration:
- Enables global test APIs (`describe`, `it`, `expect`)
- Sets the test environment to `jsdom` for DOM testing
- Configures coverage reporting (v8 provider)
- Excludes `node_modules` and test utilities from coverage

## Utilities

### `test-utils.tsx`

Provides a custom `render` function that wraps components with necessary providers:
- **BrowserRouter** — enables React Router in tests

**Usage:**
```typescript
import { render, screen } from './test-utils';

test('renders navigation', () => {
  render(<App />);
  expect(screen.getByRole('navigation')).toBeInTheDocument();
});
```

### `mocks.ts`

Provides factory functions and mock utilities:

#### Mock Creators
- `createMockFetch()` — mock Fetch API responses
- `createMockLocalStorage()` — mock browser storage
- `createMockSessionStorage()` — mock session storage
- `createMockIntersectionObserver()` — mock intersection observer
- `createMockResizeObserver()` — mock resize observer

#### Mock Data Generators
- `mockDataGenerators.user()` — generates mock user objects
- `mockDataGenerators.location()` — generates mock location objects
- `mockDataGenerators.image()` — generates mock image objects
- `mockDataGenerators.collection()` — generates mock collection objects

**Usage:**
```typescript
import { mockDataGenerators, createMockFetch } from './mocks';
import { vi } from 'vitest';

test('fetches and displays locations', async () => {
  const mockLocation = mockDataGenerators.location({ name: 'My Place' });
  global.fetch = createMockFetch([mockLocation]);

  render(<LocationList />);
  expect(await screen.findByText('My Place')).toBeInTheDocument();
});
```

## Writing Tests

### Test File Naming

- Component tests: `ComponentName.test.tsx`
- Hook tests: `useHookName.test.ts`
- Utility tests: `utilityName.test.ts`

### Test Structure

Follow the Arrange-Act-Assert pattern:

```typescript
import { describe, it, expect } from 'vitest';
import { render, screen } from './test-utils';
import { MyComponent } from './MyComponent';

describe('MyComponent', () => {
  it('displays user greeting', () => {
    // Arrange
    const user = mockDataGenerators.user({ displayName: 'Alice' });

    // Act
    render(<MyComponent user={user} />);

    // Assert
    expect(screen.getByText(/Alice/)).toBeInTheDocument();
  });
});
```

### Best Practices

1. **Test user interactions, not implementation** — use `screen.getByRole()`, `screen.getByLabelText()` instead of `container.querySelector()`
2. **Use semantic queries** — prefer `getByRole`, `getByLabelText`, `getByText` over `getByTestId`
3. **Avoid testing implementation details** — test what the user sees and does
4. **Keep tests focused** — one behavior per test
5. **Use descriptive test names** — `it('displays error message when email is invalid')` not `it('works')`

### Async Testing

For async operations, use `waitFor` and `findBy` queries:

```typescript
import { waitFor } from './test-utils';

it('loads and displays data', async () => {
  render(<DataComponent />);
  
  // findBy queries automatically wait
  const item = await screen.findByText('Loaded Item');
  expect(item).toBeInTheDocument();
});
```

### Mocking API Calls

Use `vi.mock()` or mock `fetch` directly:

```typescript
import { vi } from 'vitest';
import { createMockFetch } from './mocks';

it('handles API errors', async () => {
  global.fetch = createMockFetch({}, { status: 500 });
  
  render(<ApiComponent />);
  
  expect(await screen.findByText(/error/i)).toBeInTheDocument();
});
```

## Running Tests

```bash
# Run all tests once
npm run test

# Run tests in watch mode
npm run test -- --watch

# Run tests with coverage
npm run test -- --coverage

# Run a specific test file
npm run test -- src/components/Button.test.tsx

# Run tests matching a pattern
npm run test -- --grep "Button"
```

## Coverage Goals

- **Minimum coverage**: 80% line coverage
- **Focus areas**: business logic, user interactions, error handling
- **Exclude**: generated files, test utilities, type definitions

## Debugging Tests

### Using `screen.debug()`

```typescript
it('renders correctly', () => {
  render(<MyComponent />);
  screen.debug(); // prints the DOM to console
});
```

### Using Vitest UI

```bash
npm run test -- --ui
```

Opens an interactive UI for running and debugging tests.

## Common Issues

### `window.matchMedia is not a function`
Already mocked in `setup.ts`. If you need custom behavior, override in your test.

### `Cannot find module '@testing-library/react'`
Run `npm install` to ensure all dependencies are installed.

### Tests timeout
Increase timeout for slow operations:
```typescript
it('slow operation', async () => {
  // test code
}, { timeout: 10000 });
```

### DOM not cleaning up between tests
The setup file calls `cleanup()` after each test. If issues persist, check for unresolved promises or timers.

## Resources

- [Vitest Documentation](https://vitest.dev/)
- [React Testing Library Documentation](https://testing-library.com/react)
- [Testing Best Practices](https://kentcdodds.com/blog/common-mistakes-with-react-testing-library)
