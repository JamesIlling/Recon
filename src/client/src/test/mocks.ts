import { vi } from 'vitest';

/**
 * Mock utilities for common testing scenarios.
 */

/**
 * Creates a mock for the Fetch API with a default successful response.
 */
export const createMockFetch = (
  responseData: unknown = {},
  options: { status?: number; headers?: Record<string, string> } = {}
) => {
  const { status = 200, headers = {} } = options;
  return vi.fn().mockResolvedValue({
    ok: status >= 200 && status < 300,
    status,
    headers: new Headers(headers),
    json: vi.fn().mockResolvedValue(responseData),
    text: vi.fn().mockResolvedValue(JSON.stringify(responseData)),
    blob: vi.fn().mockResolvedValue(new Blob()),
  });
};

/**
 * Creates a mock for localStorage.
 */
export const createMockLocalStorage = () => {
  let store: Record<string, string> = {};

  return {
    getItem: vi.fn((key: string) => store[key] || null),
    setItem: vi.fn((key: string, value: string) => {
      store[key] = value.toString();
    }),
    removeItem: vi.fn((key: string) => {
      delete store[key];
    }),
    clear: vi.fn(() => {
      store = {};
    }),
  };
};

/**
 * Creates a mock for sessionStorage.
 */
export const createMockSessionStorage = () => {
  let store: Record<string, string> = {};

  return {
    getItem: vi.fn((key: string) => store[key] || null),
    setItem: vi.fn((key: string, value: string) => {
      store[key] = value.toString();
    }),
    removeItem: vi.fn((key: string) => {
      delete store[key];
    }),
    clear: vi.fn(() => {
      store = {};
    }),
  };
};

/**
 * Creates a mock for IntersectionObserver.
 */
export const createMockIntersectionObserver = () => {
  return vi.fn().mockImplementation(() => ({
    observe: vi.fn(),
    unobserve: vi.fn(),
    disconnect: vi.fn(),
  }));
};

/**
 * Creates a mock for ResizeObserver.
 */
export const createMockResizeObserver = () => {
  return vi.fn().mockImplementation(() => ({
    observe: vi.fn(),
    unobserve: vi.fn(),
    disconnect: vi.fn(),
  }));
};

/**
 * Mock data generators for common entities.
 */
export const mockDataGenerators = {
  /**
   * Generates a mock user object.
   */
  user: (overrides = {}) => ({
    id: 'user-123',
    username: 'testuser',
    displayName: 'Test User',
    email: 'test@example.com',
    role: 'Standard',
    ...overrides,
  }),

  /**
   * Generates a mock location object.
   */
  location: (overrides = {}) => ({
    id: 'location-123',
    name: 'Test Location',
    coordinates: { latitude: 40.7128, longitude: -74.006 },
    srid: 4326,
    creatorId: 'user-123',
    creatorDisplayName: 'Test User',
    createdAt: new Date().toISOString(),
    contentSequence: [
      { type: 'Heading', text: 'Test Heading', level: 1 },
      { type: 'Paragraph', text: 'Test paragraph content.' },
    ],
    ...overrides,
  }),

  /**
   * Generates a mock image object.
   */
  image: (overrides = {}) => ({
    id: 'image-123',
    mimeType: 'image/jpeg',
    altText: 'Test image',
    thumbnailUrl: '/api/images/image-123/thumbnail',
    variant400Url: '/api/images/image-123/variants/400',
    variant700Url: '/api/images/image-123/variants/700',
    variant1000Url: '/api/images/image-123/variants/1000',
    ...overrides,
  }),

  /**
   * Generates a mock collection object.
   */
  collection: (overrides = {}) => ({
    id: 'collection-123',
    name: 'Test Collection',
    description: 'A test collection',
    ownerId: 'user-123',
    ownerDisplayName: 'Test User',
    visibility: 'Public',
    createdAt: new Date().toISOString(),
    memberCount: 0,
    ...overrides,
  }),
};
