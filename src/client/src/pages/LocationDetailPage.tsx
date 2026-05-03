import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { LeafletMap, MapLocation } from '../components/LeafletMap';
import { ContentSequenceViewer, ContentBlock } from '../components/ContentSequenceViewer';
import { useAuth } from '../contexts/AuthContext';

/**
 * Location detail data from API.
 */
interface LocationDetail {
  id: string;
  name: string;
  latitude: number;
  longitude: number;
  sourceSrid: number;
  creatorDisplayName: string;
  createdAt: string;
  contentSequence: ContentBlock[];
}

/**
 * Pending edit data.
 */
interface PendingEdit {
  id: string;
  submittedByUserDisplayName: string;
  submittedAt: string;
  contentSequence: ContentBlock[];
  latitude: number;
  longitude: number;
}

/**
 * LocationDetailPage displays a single location with map, content, and pending edits panel.
 * Implements task 8.9 with all 5 sub-tasks:
 * 8.9.1 - Display name, coordinates, SRID metadata, creator, timestamp
 * 8.9.2 - Render ContentSequence via ContentSequenceViewer
 * 8.9.3 - Render Leaflet map with single pin
 * 8.9.4 - Show PendingEditPanel for creator (side-by-side comparison, Approve/Reject actions)
 * 8.9.5 - Show loading skeleton while data is in flight
 */
export function LocationDetailPage(): JSX.Element {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user, token } = useAuth();

  const [location, setLocation] = useState<LocationDetail | null>(null);
  const [pendingEdits, setPendingEdits] = useState<PendingEdit[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [selectedEditIndex, setSelectedEditIndex] = useState<number | null>(null);

  useEffect(() => {
    const fetchLocation = async () => {
      if (!id) return;

      try {
        setIsLoading(true);
        setError('');

        const response = await fetch(`/api/locations/${id}`);
        if (!response.ok) {
          throw new Error('Location not found');
        }

        const data = await response.json();
        setLocation(data);

        // Fetch pending edits if user is the creator
        if (token && data.creatorId === user?.id) {
          const editsResponse = await fetch(`/api/locations/${id}/pending-edits`, {
            headers: { Authorization: `Bearer ${token}` },
          });

          if (editsResponse.ok) {
            const editsData = await editsResponse.json();
            setPendingEdits(editsData || []);
          }
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : 'An error occurred');
      } finally {
        setIsLoading(false);
      }
    };

    fetchLocation();
  }, [id, token, user?.id]);

  const handleApproveEdit = async (editId: string) => {
    if (!id || !token) return;

    try {
      const response = await fetch(`/api/locations/${id}/pending-edits/${editId}/approve`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${token}` },
      });

      if (!response.ok) {
        throw new Error('Failed to approve edit');
      }

      // Refresh location and pending edits
      const locationResponse = await fetch(`/api/locations/${id}`);
      if (locationResponse.ok) {
        setLocation(await locationResponse.json());
      }

      setPendingEdits(pendingEdits.filter((e) => e.id !== editId));
      setSelectedEditIndex(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to approve edit');
    }
  };

  const handleRejectEdit = async (editId: string) => {
    if (!id || !token) return;

    try {
      const response = await fetch(`/api/locations/${id}/pending-edits/${editId}/reject`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${token}` },
      });

      if (!response.ok) {
        throw new Error('Failed to reject edit');
      }

      setPendingEdits(pendingEdits.filter((e) => e.id !== editId));
      setSelectedEditIndex(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to reject edit');
    }
  };

  if (isLoading) {
    return (
      <div className="location-detail-page">
        <div className="loading-skeleton">
          <div className="skeleton-header" />
          <div className="skeleton-map" />
          <div className="skeleton-content" />
        </div>
      </div>
    );
  }

  if (error || !location) {
    return (
      <div className="location-detail-page">
        <div className="error-container">
          <h1>Error</h1>
          <p className="error-message">{error || 'Location not found'}</p>
          <button onClick={() => navigate('/locations')} className="btn-primary">
            Back to Locations
          </button>
        </div>
      </div>
    );
  }

  const mapLocation: MapLocation = {
    id: location.id,
    name: location.name,
    latitude: location.latitude,
    longitude: location.longitude,
  };

  const isCreator = user?.id === location.id;

  return (
    <div className="location-detail-page">
      {/* Header with metadata */}
      <div className="location-header">
        <h1>{location.name}</h1>
        <div className="location-metadata">
          <p>
            <strong>Creator:</strong> {location.creatorDisplayName}
          </p>
          <p>
            <strong>Created:</strong> {new Date(location.createdAt).toLocaleDateString()}
          </p>
          <p>
            <strong>Coordinates:</strong> {location.latitude.toFixed(6)}, {location.longitude.toFixed(6)}
          </p>
          <p>
            <strong>Source SRID:</strong> {location.sourceSrid}
          </p>
        </div>
      </div>

      {error && <div className="error-message" role="alert">{error}</div>}

      <div className="location-content-layout">
        {/* Main content area */}
        <div className="location-main">
          {/* Map */}
          <section className="location-map-section">
            <h2>Location Map</h2>
            <LeafletMap locations={[mapLocation]} />
          </section>

          {/* Content sequence */}
          <section className="location-content-section">
            <h2>Content</h2>
            <ContentSequenceViewer blocks={location.contentSequence} />
          </section>
        </div>

        {/* Pending edits panel (creator only) */}
        {isCreator && pendingEdits.length > 0 && (
          <aside className="pending-edits-panel">
            <h2>Pending Edits ({pendingEdits.length})</h2>
            <div className="edits-list">
              {pendingEdits.map((edit, index) => (
                <div
                  key={edit.id}
                  className={`edit-item ${selectedEditIndex === index ? 'selected' : ''}`}
                  onClick={() => setSelectedEditIndex(selectedEditIndex === index ? null : index)}
                >
                  <p className="edit-submitter">{edit.submittedByUserDisplayName}</p>
                  <p className="edit-date">{new Date(edit.submittedAt).toLocaleDateString()}</p>
                </div>
              ))}
            </div>

            {selectedEditIndex !== null && (
              <div className="edit-comparison">
                <h3>Comparison</h3>
                <div className="comparison-row">
                  <div className="comparison-column">
                    <h4>Current Version</h4>
                    <ContentSequenceViewer blocks={location.contentSequence} />
                  </div>
                  <div className="comparison-column">
                    <h4>Proposed Changes</h4>
                    <ContentSequenceViewer blocks={pendingEdits[selectedEditIndex].contentSequence} />
                  </div>
                </div>

                <div className="edit-actions">
                  <button
                    onClick={() => handleApproveEdit(pendingEdits[selectedEditIndex].id)}
                    className="btn-primary"
                  >
                    Approve
                  </button>
                  <button
                    onClick={() => handleRejectEdit(pendingEdits[selectedEditIndex].id)}
                    className="btn-danger"
                  >
                    Reject
                  </button>
                </div>
              </div>
            )}
          </aside>
        )}
      </div>
    </div>
  );
}
