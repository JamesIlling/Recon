import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen, waitFor } from './test-utils';
import { mockDataGenerators, createMockFetch } from './mocks';

/**
 * Sample test demonstrating the test utilities and configuration.
 * This test verifies that the testing setup is properly configured.
 */

// Mock component for demonstration
function SampleComponent() {
  return (
    <div>
      <h1>Sample Component</h1>
      <p>This demonstrates the test setup.</p>
      <button>Click me</button>
    </div>
  );
}

describe('Sample Test Suite', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders component with correct content', () => {
    render(<SampleComponent />);

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(
      'Sample Component'
    );
    expect(screen.getByText(/demonstrates the test setup/)).toBeInTheDocument();
  });

  it('renders interactive elements', () => {
    render(<SampleComponent />);

    const button = screen.getByRole('button', { name: /click me/i });
    expect(button).toBeInTheDocument();
  });

  it('demonstrates mock data generation', () => {
    const mockUser = mockDataGenerators.user({ displayName: 'Alice' });
    const mockLocation = mockDataGenerators.location({
      name: 'Test Location',
      creatorId: mockUser.id,
    });

    expect(mockUser.displayName).toBe('Alice');
    expect(mockLocation.name).toBe('Test Location');
    expect(mockLocation.creatorId).toBe(mockUser.id);
  });

  it('demonstrates fetch mocking', async () => {
    const mockData = { message: 'Success' };
    global.fetch = createMockFetch(mockData);

    const response = await fetch('/api/test');
    const data = await response.json();

    expect(response.ok).toBe(true);
    expect(data).toEqual(mockData);
  });

  it('demonstrates error response mocking', async () => {
    global.fetch = createMockFetch(
      { error: 'Not found' },
      { status: 404 }
    );

    const response = await fetch('/api/nonexistent');

    expect(response.ok).toBe(false);
    expect(response.status).toBe(404);
  });
});
