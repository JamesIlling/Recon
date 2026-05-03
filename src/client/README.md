# Location Management Client

React 18+ TypeScript frontend for the Location Management system, built with Vite and React Router.

## Getting Started

### Prerequisites

- Node.js 18+ and npm

### Installation

```bash
npm install
```

### Development

Start the development server:

```bash
npm run dev
```

The app will be available at `http://localhost:5173` and proxies API requests to `http://localhost:5000`.

### Building

Build for production:

```bash
npm run build
```

Preview the production build:

```bash
npm run preview
```

### Testing

Run unit tests:

```bash
npm run test
```

### Linting

Check code style:

```bash
npm run lint
```

Type check:

```bash
npm run type-check
```

## Project Structure

```
src/
  components/       # Reusable React components
  pages/           # Page components (routed)
  hooks/           # Custom React hooks
  services/        # API and utility services
  types/           # TypeScript type definitions
  test/            # Test utilities and setup
  App.tsx          # Main app component with routes
  main.tsx         # Entry point
  index.css        # Global styles
```

## Architecture

- **React Router v6** for client-side routing
- **Vite** for fast development and optimized builds
- **Vitest** for unit testing
- **React Testing Library** for component testing
- **Axios** for HTTP requests
- **TypeScript** for type safety

## API Integration

The frontend proxies API requests to the backend API running on `http://localhost:5000`. All requests to `/api/*` are forwarded to the backend.

## Accessibility

All components follow WCAG 2.1 Level AA standards. Automated accessibility testing is performed via `@axe-core/playwright` in E2E tests.

## Code Standards

- Follow TypeScript strict mode
- Use functional components with hooks
- Prefer composition over inheritance
- Keep components under 25 lines when possible
- Use descriptive names for variables and functions
- Add JSDoc comments to public functions
