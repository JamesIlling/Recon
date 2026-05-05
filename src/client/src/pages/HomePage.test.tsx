import React from 'react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { AuthProvider } from '../contexts/AuthContext';
import { HomePage } from './HomePage';

// Mock fetch
global.fetch = vi.fn();

const mockCollections = {
  items: [
    {
      id: '1',
      name: 'Collection 1',
      description: 'First collection',
      thumbnailUrl: 'https://example.com/thumb1.jpg',
      isOwner: true,
      visibility: 'Private' as const,
    },
    {
      id: '2',
      name: 'Collection 2',
      description: 'Second collection',
      thumbnailUrl: 'https://example.com/thumb2.jpg',
      isOwner: false,
      visibility: 'Public' as const,
    },
  ],
  totalCount: 2,
};

const mockUser = {
  id: 'user-1',
  username: 'testuser',
  displayName: 'Test User',
  email: 'test@example.com',
  role: 'Standard' as const,
  avatarImageId: undefined,
  showPublicCollections: true,
};

const renderWithProviders = (component: React.ReactElement) => {
  return render(
    <BrowserRouter>
      <AuthProvider>
        {component}
      </AuthProvider>
    </BrowserRouter>
  );
};

describe('HomePage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.setItem('auth_token', 'test-token');
    // First call: profile fetch from AuthProvider
    (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      ok: true,
      json: async () => mockUser,
    });
    // Subsequent calls: collections fetch
    (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValue({
      ok: true,
      json: async () => mockCollections,
    });
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('renders page header', async () => {
    renderWithProviders(<HomePage />);

    await waitFor(() => {
      expect(screen.getByText('Collections')).toBeInTheDocument();
    });
  });

  it('fetches and displays collections', async () => {
    renderWithProviders(<HomePage />);

    await waitFor(() => {
      expect(screen.getByText('Collection 1')).toBeInTheDocument();
      expect(screen.getByText('Collection 2')).toBeInTheDocument();
    });
  });

  it('displays loading skeleton while fetching', () => {
    (global.fetch as any).mockImplementation(
      () =>
        new Promise((resolve) => {
          setTimeout(() => {
            resolve({
              ok: true,
              json: async () => mockCollections,
            });
          }, 100);
        })
    );

    renderWithProviders(<HomePage />);

    expect(screen.getByText('Collections')).toBeInTheDocument();
  });

  it('displays empty state when no collections', async () => {
    (global.fetch as any).mockResolvedValue({
      ok: true,
      json: async () => ({ items: [], totalCount: 0 }),
    });

    renderWithProviders(<HomePage />);

    await waitFor(() => {
      expect(screen.getByText('No collections yet')).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /create collection/i })).toBeInTheDocument();
    });
  });

  it('displays error message on fetch failure', async () => {
    (global.fetch as any).mockResolvedValue({
      ok: false,
      status: 500,
    });

    renderWithProviders(<HomePage />);

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument();
    });
  });

  it('displays pagination controls when multiple pages', async () => {
    (global.fetch as any).mockResolvedValue({
      ok: true,
      json: async () => ({
        items: mockCollections.items,
        totalCount: 50, // More than one page
      }),
    });

    renderWithProviders(<HomePage />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /previous page/i })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /next page/i })).toBeInTheDocument();
    });
  });

  it('does not display pagination when only one page', async () => {
    renderWithProviders(<HomePage />);

    await waitFor(() => {
      expect(screen.queryByRole('button', { name: /previous page/i })).not.toBeInTheDocument();
    });
  });

  it('filters collections based on showPublicCollections preference', async () => {
    renderWithProviders(<HomePage />);

    await waitFor(() => {
      expect(screen.getByText('Collection 1')).toBeInTheDocument();
    });
  });

  it('displays subtitle based on showPublicCollections preference', async () => {
    renderWithProviders(<HomePage />);

    await waitFor(() => {
      expect(
        screen.getByText('Your collections and public collections from other users')
      ).toBeInTheDocument();
    });
  });

  it('renders collection cards with click handlers', async () => {
    renderWithProviders(<HomePage />);

    await waitFor(() => {
      const viewButtons = screen.getAllByRole('button', { name: /view collection/i });
      expect(viewButtons.length).toBeGreaterThan(0);
    });
  });

  it('calls API with correct pagination parameters', async () => {
    renderWithProviders(<HomePage />);

    await waitFor(() => {
      expect(global.fetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/collections/combined?page=1&pageSize=20'),
        expect.any(Object)
      );
    });
  });

  it('includes authorization header in API call', async () => {
    renderWithProviders(<HomePage />);

    await waitFor(() => {
      expect(global.fetch).toHaveBeenCalledWith(
        expect.any(String),
        expect.objectContaining({
          headers: expect.objectContaining({
            Authorization: expect.stringContaining('Bearer'),
          }),
        })
      );
    });
  });
});
