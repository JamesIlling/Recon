# Testing Guide

This document describes the testing setup and best practices for the Location Management frontend.

## Test Stack

- **Vitest** — Fast unit test runner with Jest-compatible API
- **React Testing Library** — Component testing focused on user behavior
- **@testing-library/jest-dom** — Custom matchers for DOM assertions
- **jsdom** — DOM environment for Node.js tests

## Running Tests

```bash
# Run all tests once
npm run test

# Run tests in watch mode (during development)
npm run test -- --watch

# Run tests with coverage
npm run test -- --coverage

# Run a specific test file
npm run test -- src/components/Button.test.tsx

# Run tests matching a pattern
npm run test -- --grep "Button"
```

## Test Structure

Tests follow the **Arrange / Act / Assert** pattern:

```typescript
import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import Button from './Button';

describe('Button', () => {
  it('renders with text', () => {
    // Arrange
    const text = 'Click me';

    // Act
    render(<Button>{text}</Button>);

    // Assert
    expect(screen.getByRole('button', { name: text })).toBeInTheDocument();
  });
});
```

## Best Practices

### 1. Use Semantic Queries

Prefer queries that reflect how users interact with the component:

```typescript
// Good — queries by role (most accessible)
screen.getByRole('button', { name: /submit/i });
screen.getByRole('textbox', { name: /email/i });

// Acceptable — queries by label
screen.getByLabelText(/email/i);

// Avoid — queries by test ID (last resort)
screen.getByTestId('submit-button');
```

### 2. Use Custom Render Function

Always use the custom `render` from `src/test/test-utils.tsx` to ensure providers are available:

```typescript
import { render, screen } from '@/test/test-utils';

describe('MyComponent', () => {
  it('works', () => {
    render(<MyComponent />);
    // Component has access to Router, Context, etc.
  });
});
```

### 3. Test User Behavior, Not Implementation

```typescript
// Good — tests what the user sees
it('submits form when button is clicked', async () => {
  const { user } = render(<LoginForm />);
  await user.type(screen.getByLabelText(/email/i), 'test@example.com');
  await user.click(screen.getByRole('button', { name: /login/i }));
  expect(screen.getByText(/welcome/i)).toBeInTheDocument();
});

// Avoid — tests implementation details
it('calls handleSubmit when button is clicked', () => {
  const handleSubmit = vi.fn();
  render(<LoginForm onSubmit={handleSubmit} />);
  fireEvent.click(screen.getByRole('button'));
  expect(handleSubmit).toHaveBeenCalled();
});
```

### 4. One Behavior Per Test

```typescript
// Good — focused test
it('displays error when email is invalid', () => {
  render(<LoginForm />);
  fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'invalid' } });
  expect(screen.getByText(/invalid email/i)).toBeInTheDocument();
});

// Avoid — multiple behaviors
it('validates email and password', () => {
  // Tests two things at once
});
```

### 5. Use Async Utilities for Async Operations

```typescript
import { waitFor, screen } from '@testing-library/react';

it('loads data', async () => {
  render(<DataComponent />);
  
  // Wait for element to appear
  await waitFor(() => {
    expect(screen.getByText(/loaded/i)).toBeInTheDocument();
  });
});
```

## Mocking

### Mock API Calls

```typescript
import { vi } from 'vitest';

vi.mock('@/services/api', () => ({
  getLocations: vi.fn(() => Promise.resolve([{ id: '1', name: 'Test' }])),
}));
```

### Mock Modules

```typescript
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => vi.fn(),
  };
});
```

## Coverage

Coverage reports are generated in `coverage/` directory:

```bash
npm run test -- --coverage
open coverage/index.html
```

Target coverage:
- **Statements**: ≥ 80%
- **Branches**: ≥ 75%
- **Functions**: ≥ 80%
- **Lines**: ≥ 80%

## Debugging Tests

### Run Single Test

```bash
npm run test -- src/components/Button.test.tsx
```

### Run with Debug Output

```bash
npm run test -- --reporter=verbose
```

### Use `screen.debug()`

```typescript
it('renders', () => {
  render(<MyComponent />);
  screen.debug(); // Prints DOM to console
});
```

## File Naming

- Test files: `ComponentName.test.tsx` or `ComponentName.spec.tsx`
- Test utilities: `src/test/`
- Fixtures: `src/test/fixtures/`
- Mocks: `src/test/mocks/`

## Accessibility Testing

All tests should verify accessibility:

```typescript
it('has accessible form', () => {
  render(<LoginForm />);
  
  // Check for labels
  expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
  
  // Check for button text
  expect(screen.getByRole('button', { name: /login/i })).toBeInTheDocument();
  
  // Check for error messages linked to fields
  expect(screen.getByText(/required/i)).toHaveAttribute('id');
});
```

## Resources

- [Vitest Documentation](https://vitest.dev/)
- [React Testing Library Docs](https://testing-library.com/react)
- [Testing Best Practices](https://kentcdodds.com/blog/common-mistakes-with-react-testing-library)
