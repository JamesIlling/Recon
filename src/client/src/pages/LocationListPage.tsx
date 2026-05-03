import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';

/**
 * Location summary for list display.
 */
interface LocationSummary {
  id: string;
  name: string;
  latitude: number;
  longitude: number;
  creatorDisplayName: string;
  createdAt: string;
}

/**
 * LocationListPage displays a paginated list of all locations with navigation links.
 * Implements task 8.8.
 */
export function LocationListPage(): JSX.Element {
  const [locations, setLocations] = useState<LocationSummary[]>([]);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const fetchLocations = async () => {
      try {
        setIsLoading(true);
        setError('');
        const response = await fetch(`/api/locations?page=${page}&pageSize=${pageSize}`);

        if (!response.ok) {
          throw new Error('Failed to fetch locations');
        }

        const data = await response.json();
        setLocations(data.items || []);
        setTotalCount(data.totalCount || 0);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'An error occurred');
      } finally {
        setIsLoading(false);
      }
    };

    fetchLocations();
  }, [page, pageSize]);

  const totalPages = Math.ceil(totalCount / pageSize);

  return (
    <div className="location-list-page">
      <div className="page-header">
        <h1>Locations</h1>
        <p className="page-subtitle">Browse all locations in the system</p>
      </div>

      {error && <div className="error-message" role="alert">{error}</div>}

      {isLoading ? (
        <div className="loading-skeleton">
          <div className="skeleton-item" />
          <div className="skeleton-item" />
          <div className="skeleton-item" />
        </div>
      ) : locations.length > 0 ? (
        <>
          <div className="locations-grid">
            {locations.map((location) => (
              <Link
                key={location.id}
                to={`/locations/${location.id}`}
                className="location-card"
              >
                <div className="card-header">
                  <h2 className="card-title">{location.name}</h2>
                </div>
                <div className="card-body">
                  <p className="card-meta">
                    <span className="creator">By {location.creatorDisplayName}</span>
                    <span className="date">
                      {new Date(location.createdAt).toLocaleDateString()}
                    </span>
                  </p>
                  <p className="card-coordinates">
                    {location.latitude.toFixed(4)}, {location.longitude.toFixed(4)}
                  </p>
                </div>
              </Link>
            ))}
          </div>

          {/* Pagination */}
          <div className="pagination" role="navigation" aria-label="Pagination">
            <button
              onClick={() => setPage(Math.max(1, page - 1))}
              disabled={page === 1}
              className="btn-secondary"
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
            >
              Next
            </button>
          </div>
        </>
      ) : (
        <div className="empty-state">
          <p>No locations found.</p>
          <Link to="/" className="btn-primary">
            Back to Home
          </Link>
        </div>
      )}
    </div>
  );
}
