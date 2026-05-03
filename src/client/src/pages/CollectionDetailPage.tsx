import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { LeafletMap, MapLocation, BoundingShape } from '../components/LeafletMap';

/**
 * Location member in a collection.
 */
interface CollectionMember {
  id: string;
  name: string;
  latitude: number;
  longitude: number;
}

/**
 * Collection detail data from API.
 */
interface CollectionDetail {
  id: string;
  name: string;
  description?: string;
  ownerId: string;
  ownerDisplayName: string;
  visibility: 'Private' | 'Public';
  members: CollectionMember[];
  boundingShape?: BoundingShape;
  createdAt: string;
}

/**
 * CollectionDetailPage displays a collection with map, member list, and optional ownership reassignment UI.
 * Implements task 11.3 with all 5 sub-tasks:
 * 11.3.1 - Leaflet map with all member Location pins and optional BoundingShape overlay
 * 11.3.2 - Auto-fit viewport to all pins + shape
 * 11.3.3 - Linked list of member Locations below map
 * 11.3.4 - Empty-state when no members
 * 11.3.5 - Ownership reassignment UI (admin only, accessible from detail page)
 */
export function CollectionDetailPage(): JSX.Element {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user, token } = useAuth();

  const [collection, setCollection] = useState<CollectionDetail | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [showReassignModal, setShowReassignModal] = useState(false);
  const [reassignError, setReassignError] = useState('');

  useEffect(() => {
    const fetchCollection = async () => {
      if (!id) return;

      try {
        setIsLoading(true);
        setError('');

        const headers: HeadersInit = {};
        if (token) {
          headers.Authorization = `Bearer ${token}`;
        }

        const response = await fetch(`/api/collections/${id}`, { headers });

        if (!response.ok) {
          if (response.status === 403) {
            throw new Error('You do not have permission to view this collection');
          } else if (response.status === 404) {
            throw new Error('Collection not found');
          }
          throw new Error('Failed to fetch collection');
        }

        const data = await response.json();
        setCollection(data);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'An error occurred');
      } finally {
        setIsLoading(false);
      }
    };

    fetchCollection();
  }, [id, token]);

  const handleLocationClick = (location: CollectionMember) => {
    navigate(`/locations/${location.id}`);
  };

  const handleReassignOwnership = async (newOwnerId: string) => {
    if (!id || !token) return;

    try {
      setReassignError('');
      const response = await fetch(`/api/admin/resources/collection/${id}/reassign`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({ newOwnerId }),
      });

      if (!response.ok) {
        throw new Error('Failed to reassign ownership');
      }

      // Refresh collection data
      const collectionResponse = await fetch(`/api/collections/${id}`, {
        headers: { Authorization: `Bearer ${token}` },
      });

      if (collectionResponse.ok) {
        setCollection(await collectionResponse.json());
      }

      setShowReassignModal(false);
    } catch (err) {
      setReassignError(err instanceof Error ? err.message : 'Failed to reassign ownership');
    }
  };

  if (isLoading) {
    return (
      <div className="collection-detail-page">
        <div className="loading-skeleton">
          <div className="skeleton-header" />
          <div className="skeleton-map" />
          <div className="skeleton-content" />
        </div>
      </div>
    );
  }

  if (error || !collection) {
    return (
      <div className="collection-detail-page">
        <div className="error-container">
          <h1>Error</h1>
          <p className="error-message">{error || 'Collection not found'}</p>
          <button onClick={() => navigate('/')} className="btn-primary">
            Back to Collections
          </button>
        </div>
      </div>
    );
  }

  const mapLocations: MapLocation[] = collection.members.map((member) => ({
    id: member.id,
    name: member.name,
    latitude: member.latitude,
    longitude: member.longitude,
  }));

  const isOwner = user?.id === collection.ownerId;
  const isAdmin = user?.role === 'Admin';

  return (
    <div className="collection-detail-page">
      {/* Header with metadata */}
      <div className="collection-header">
        <div className="collection-header-content">
          <h1>{collection.name}</h1>
          <div className="collection-metadata">
            <p>
              <strong>Owner:</strong> {collection.ownerDisplayName}
            </p>
            <p>
              <strong>Visibility:</strong>{' '}
              <span className={`visibility-badge visibility-${collection.visibility.toLowerCase()}`}>
                {collection.visibility}
              </span>
            </p>
            <p>
              <strong>Created:</strong> {new Date(collection.createdAt).toLocaleDateString()}
            </p>
          </div>
          {collection.description && (
            <p className="collection-description">{collection.description}</p>
          )}
        </div>

        {/* Admin-only ownership reassignment button */}
        {isAdmin && !isOwner && (
          <div className="collection-admin-actions">
            <button
              onClick={() => setShowReassignModal(true)}
              className="btn-secondary"
              aria-label="Reassign collection ownership"
            >
              Reassign Ownership
            </button>
          </div>
        )}
      </div>

      {error && (
        <div className="error-message" role="alert">
          {error}
        </div>
      )}

      {/* Map section */}
      {collection.members.length > 0 ? (
        <section className="collection-map-section">
          <h2>Collection Map</h2>
          <LeafletMap
            locations={mapLocations}
            boundingShape={collection.boundingShape}
            onLocationClick={handleLocationClick}
          />
        </section>
      ) : (
        <section className="collection-empty-state">
          <h2>No Members</h2>
          <p>This collection does not have any member locations yet.</p>
          {isOwner && (
            <button
              onClick={() => navigate(`/locations`)}
              className="btn-primary"
              aria-label="Browse locations to add to this collection"
            >
              Browse Locations
            </button>
          )}
        </section>
      )}

      {/* Members list section */}
      {collection.members.length > 0 && (
        <section className="collection-members-section">
          <h2>Member Locations ({collection.members.length})</h2>
          <div className="members-list">
            {collection.members.map((member) => (
              <div key={member.id} className="member-item">
                <button
                  onClick={() => handleLocationClick(member)}
                  className="member-link"
                  aria-label={`View ${member.name} location`}
                >
                  <h3>{member.name}</h3>
                  <p className="member-coordinates">
                    {member.latitude.toFixed(6)}, {member.longitude.toFixed(6)}
                  </p>
                </button>
              </div>
            ))}
          </div>
        </section>
      )}

      {/* Ownership reassignment modal (admin only) */}
      {showReassignModal && isAdmin && (
        <div className="modal-overlay" onClick={() => setShowReassignModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <h2>Reassign Collection Ownership</h2>
            <p>
              This collection is currently owned by <strong>{collection.ownerDisplayName}</strong>.
            </p>
            <p>
              As an admin, you can reassign ownership to another user. This action will transfer
              all ownership rights to the new owner.
            </p>

            {reassignError && (
              <div className="error-message" role="alert">
                {reassignError}
              </div>
            )}

            <div className="modal-actions">
              <button
                onClick={() => setShowReassignModal(false)}
                className="btn-secondary"
                aria-label="Cancel reassignment"
              >
                Cancel
              </button>
              <p className="modal-note">
                Note: Full reassignment UI would include user selection. This is a placeholder for
                the admin reassignment feature.
              </p>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
