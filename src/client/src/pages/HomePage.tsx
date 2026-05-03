import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { ProtectedRoute } from '../components/ProtectedRoute';
import { CollectionCard, CollectionCardData } from '../components/CollectionCard';

/**
 * HomePage displays a paginated list of collections combining public and owned collections.
 * Implements task 11.2 with all 5 sub-tasks:
 * 11.2.1 - Paginated card list using GET /api/collections/combined
 * 11.2.2 - Filter by ShowPublicCollections preference (read from API, not client state)
 * 11.2.3 - Empty-state with "Create collection" prompt
 * 11.2.4 - Loading skeleton while data is in flight
 * 11.2.5 - Redirect unauthenticated users to /login
 */
export function HomePage(): JSX.Element {
  const navigate = useNavigate();
  const { token, user, isLoading: authLoading } = useAuth();

  const [collections, setCollections] = useState<CollectionCardData[]>([]);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  // Redirect unauthenticated users to login (11.2.5)
  useEffect(() => {
    if (!authLoading && !token) {
      navigate('/login', { replace: true });
    }
  }, [token, authLoading, navigate]);

  // Fetch collections based on ShowPublicCollections preference (11.2.1, 11.2.2)
  useEffect(() => {
    if (!token || authLoading) return;

    const fetchCollections = async () => {
      try {
        setIsLoading(true);
        setError('');

        // Get combined list (public + owned) from API
        const response = await fetch(
          `/api/collections/combined?page=${page}&pageSize=${pageSize}`,
          {
            headers: {
              Authorization: `Bearer ${token}`,
            },
          }
        );

        if (!response.ok) {
          throw new Error('Failed to fetch collections');
        }

        const data = await response.json();

        // Filter based on ShowPublicCollections preference from user profile
        let filteredCollections = data.items || [];
        if (user && !user.showPublicCollections) {
          // Only show owned collections
          filteredCollections = filteredCollections.filter(
            (c: CollectionCardData) => c.isOwner
          );
        }

        setCollections(filteredCollections);
        setTotalCount(data.totalCount || 0);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'An error occurred');
      } finally {
        setIsLoading(false);
      }
    };

    fetchCollections();
  }, [page, pageSize, token, authLoading, user?.showPublicCollections]);

  const handleCollectionClick = (collectionId: string) => {
    navigate(`/collections/${collectionId}`);
  };

  const totalPages = Math.ceil(totalCount / pageSize);

  // Show loading skeleton while data is in flight (11.2.4)
  if (authLoading || isLoading) {
    return (
      <div className="home-page">
        <div className="page-header">
          <h1>Collections</h1>
        </div>
        <div className="loading-skeleton">
          <div className="skeleton-card" />
          <div className="skeleton-card" />
          <div className="skeleton-card" />
          <div className="skeleton-card" />
        </div>
      </div>
    );
  }

  // Empty state with "Create collection" prompt (11.2.3)
  if (collections.length === 0) {
    return (
      <div className="home-page">
        <div className="page-header">
          <h1>Collections</h1>
        </div>
        <div className="empty-state">
          <div className="empty-state-icon">📦</div>
          <h2>No collections yet</h2>
          <p>
            {user?.showPublicCollections
              ? 'Create your first collection or browse public collections from other users.'
              : 'Create your first collection to get started.'}
          </p>
          <button
            onClick={() => navigate('/collections/create')}
            className="btn-primary"
            aria-label="Create a new collection"
          >
            Create Collection
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="home-page">
      <div className="page-header">
        <h1>Collections</h1>
        <p className="page-subtitle">
          {user?.showPublicCollections
            ? 'Your collections and public collections from other users'
            : 'Your collections'}
        </p>
      </div>

      {error && (
        <div className="error-message" role="alert">
          {error}
        </div>
      )}

      {/* Collections grid */}
      <div className="collections-grid">
        {collections.map((collection) => (
          <CollectionCard
            key={collection.id}
            collection={collection}
            onClick={handleCollectionClick}
          />
        ))}
      </div>

      {/* Pagination controls */}
      {totalPages > 1 && (
        <div className="pagination" role="navigation" aria-label="Pagination">
          <button
            onClick={() => setPage(Math.max(1, page - 1))}
            disabled={page === 1}
            className="btn-secondary"
            aria-label="Previous page"
          >
            Previous
          </button>

          <span className="page-info">
            Page {page} of {totalPages}
          </span>

          <button
            onClick={() => setPage(Math.min(totalPages, page + 1))}
            disabled={page === totalPages}
            className="btn-secondary"
            aria-label="Next page"
          >
            Next
          </button>
        </div>
      )}
    </div>
  );
}

/**
 * Wrapped HomePage with ProtectedRoute to ensure only authenticated users can access it.
 */
export function HomePageProtected(): JSX.Element {
  return (
    <ProtectedRoute>
      <HomePage />
    </ProtectedRoute>
  );
}
