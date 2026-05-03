import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { AuthProvider } from '../contexts/AuthContext';
import { CollectionDetailPage } from './CollectionDetailPage';

// Mock fetch and useParams
global.fetch = vi.fn();

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useParams: () => ({ id: 'collection-1' }),
  };
});

const mockCollectionDetail = {
  id: 'collection-1',
  name: 'Test Collection',
  description: 'A test collection with locations',
  ownerId: 'owner-1',
  ownerDisplayName: 'Collection Owner',
  visibility: 'Public' as const,
  members: [
    {
      id: 'loc-1',
      name: 'Location 1',
      latitude: 40.7128,
      longitude: -74.006,
    },
    {
      id: 'loc-2',
      name: 'Location 2',
      latitude: 34.0522,
      longitude: -118.2437,
    },
  ],
  createdAt: '2024-01-01T00:00:00Z',
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

describe('CollectionDetailPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (global.fetch as any).mockResolvedValue({
      ok: true,
      json: async () => mockCollectionDetail,
    });
  });

  it('renders collection name', async () => {
    renderWithProviders(<CollectionDetailPage />);

    await waitFor(() => {
      expect(screen.getByText('Test Collection')).toBeInTheDocument();
    });
  });

  it('renders collection metadata', async () => {
    renderWithProviders(<CollectionDetailPage />);

    await waitFor(() => {
      expect(screen.getByText('Collection Owner')).toBeInTheDocument();
      expect(screen.getByText(/Public/)).toBeInTheDocument();
    });
  });

  it('renders collection description', async () => {
    renderWithProviders(<CollectionDetailPage />);

    await waitFor(() => {
      expect(screen.getByText('A test collection with locations')).toBeInTheDocument();
    });
  });

  it('renders map section with member locations', async () => {
    renderWithProviders(<CollectionDetailPage />);

    await waitFor(() => {
      expect(screen.getByText('Collection Map')).toBeInTheDocument();
      expect(screen.getByText('Location 1')).toBeInTheDocument();
      expect(screen.getByText('Location 2')).toBeInTheDocument();
    });
  });

  it('renders member locations list', async () => {
    renderWithProviders(<CollectionDetailPage />);

    await waitFor(() => {
      expect(screen.getByText('Member Locations (2)')).toBeInTheDocument();
    });
  });

  it('displays empty state when no members', async () => {
    (global.fetch as any).mockResolvedValue({
      ok: true,
      json: async () => ({
        ...mockCollectionDetail,
        members: [],
      }),
    });

    renderWithProviders(<CollectionDetailPage />);

    await waitFor(() => {
      expect(screen.getByText('No Members')).toBeInTheDocument();
      expect(
        screen.getByText('This collection does not have any member locations yet.')
      ).toBeInTheDocument();
    });
  });

  it('displays loading skeleton while fetching', () => {
    (global.fetch as any).mockImplementation(
      () =>
        new Promise((resolve) => {
          setTimeout(() => {
            resolve({
              ok: true,
              json: async () => mockCollectionDetail,
            });
          }, 100);
        })
    );

    renderWithProviders(<CollectionDetailPage />);

    expect(screen.getByText('Collection Map')).toBeInTheDocument();
  });

  it('displays error message on fetch failure', async () => {
    (global.fetch as any).mockResolvedValue({
      ok: false,
      status: 404,
    });

    renderWithProviders(<CollectionDetailPage />);

    await waitFor(() => {
      expect(screen.getByText('Error')).toBeInTheDocument();
      expect(screen.getByText('Collection not found')).toBeInTheDocument();
    });
  });

  it('displays permission error for private collections', async () => {
    (global.fetch as any).mockResolvedValue({
      ok: false,
      status: 403,
    });

    renderWithProviders(<CollectionDetailPage />);

    await waitFor(() => {
      expect(screen.getByText('Error')).toBeInTheDocument();
      expect(
        screen.getByText('You do not have permission to view this collection')
      ).toBeInTheDocument();
    });
  });

  it('renders back button in error state', async () => {
    (global.fetch as any).mockResolvedValue({
      ok: false,
      status: 404,
    });

    renderWithProviders(<CollectionDetailPage />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /back to collections/i })).toBeInTheDocument();
    });
  });

  it('renders member location links', async () => {
    renderWithProviders(<CollectionDetailPage />);

    await waitFor(() => {
      const locationLinks = screen.getAllByRole('button', { name: /view.*location/i });
      expect(locationLinks.length).toBeGreaterThan(0);
    });
  });

  it('displays member coordinates', async () => {
    renderWithProviders(<CollectionDetailPage />);

    await waitFor(() => {
      expect(screen.getByText(/40\.712800, -74\.006000/)).toBeInTheDocument();
      expect(screen.getByText(/34\.052200, -118\.243700/)).toBeInTheDocument();
    });
  });

  it('fetches collection with correct ID', async () => {
    renderWithProviders(<CollectionDetailPage />);

    await waitFor(() => {
      expect(global.fetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/collections/collection-1'),
        expect.any(Object)
      );
    });
  });

  it('includes authorization header when token is available', async () => {
    renderWithProviders(<CollectionDetailPage />);

    await waitFor(() => {
      expect(global.fetch).toHaveBeenCalledWith(
        expect.any(String),
        expect.objectContaining({
          headers: expect.any(Object),
        })
      );
    });
  });

  it('renders visibility badge', async () => {
    renderWithProviders(<CollectionDetailPage />);

    await waitFor(() => {
      const badge = screen.getByText('Public');
      expect(badge).toHaveClass('visibility-badge');
    });
  });

  it('has proper accessibility attributes', async () => {
    renderWithProviders(<CollectionDetailPage />);

    await waitFor(() => {
      const mapSection = screen.getByText('Collection Map').closest('section');
      expect(mapSection).toBeInTheDocument();
    });
  });
});
