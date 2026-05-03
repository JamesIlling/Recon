import React, { useEffect, useRef, useState } from 'react';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';

/**
 * Location data for rendering on the map.
 */
export interface MapLocation {
  id: string;
  name: string;
  latitude: number;
  longitude: number;
}

/**
 * Bounding shape overlay data (GeoJSON).
 */
export interface BoundingShape {
  type: 'Feature';
  geometry: {
    type: 'Polygon' | 'MultiPolygon';
    coordinates: number[][][] | number[][][][];
  };
  properties?: Record<string, unknown>;
}

/**
 * Props for the LeafletMap component.
 */
export interface LeafletMapProps {
  locations: MapLocation[];
  boundingShape?: BoundingShape;
  onLocationClick?: (location: MapLocation) => void;
  className?: string;
}

/**
 * LeafletMap component renders an interactive map with location pins and optional bounding shape overlay.
 * Includes keyboard-accessible text alternative listing all locations.
 *
 * Sub-tasks implemented:
 * 8.5.1 - Render Location pins at WGS84 coordinates
 * 8.5.2 - Display Location name in pin tooltip
 * 8.5.3 - Render BoundingShape overlay when present
 * 8.5.4 - Auto-fit viewport to all pins + shape
 * 8.5.5 - Keyboard-accessible text alternative (table/list of Locations with names and coordinates)
 */
export function LeafletMap({
  locations,
  boundingShape,
  onLocationClick,
  className = '',
}: LeafletMapProps): JSX.Element {
  const mapContainer = useRef<HTMLDivElement>(null);
  const map = useRef<L.Map | null>(null);
  const markersRef = useRef<L.Marker[]>([]);
  const shapeLayerRef = useRef<L.GeoJSON | null>(null);

  // Initialize map
  useEffect(() => {
    if (!mapContainer.current || map.current) return;

    map.current = L.map(mapContainer.current).setView([51.505, -0.09], 13);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '© OpenStreetMap contributors',
      maxZoom: 19,
    }).addTo(map.current);

    return () => {
      if (map.current) {
        map.current.remove();
        map.current = null;
      }
    };
  }, []);

  // Update markers when locations change
  useEffect(() => {
    if (!map.current) return;

    // Clear existing markers
    markersRef.current.forEach((marker) => marker.remove());
    markersRef.current = [];

    // Add new markers
    locations.forEach((location) => {
      const marker = L.marker([location.latitude, location.longitude])
        .bindTooltip(location.name, { permanent: false, direction: 'top' })
        .addTo(map.current!);

      if (onLocationClick) {
        marker.on('click', () => onLocationClick(location));
      }

      markersRef.current.push(marker);
    });

    // Auto-fit viewport
    fitBounds();
  }, [locations, onLocationClick]);

  // Update bounding shape overlay
  useEffect(() => {
    if (!map.current) return;

    // Remove existing shape layer
    if (shapeLayerRef.current) {
      map.current.removeLayer(shapeLayerRef.current);
      shapeLayerRef.current = null;
    }

    // Add new shape layer if provided
    if (boundingShape) {
      shapeLayerRef.current = L.geoJSON(boundingShape, {
        style: {
          color: '#3388ff',
          weight: 2,
          opacity: 0.7,
          fillOpacity: 0.1,
        },
      }).addTo(map.current);

      fitBounds();
    }
  }, [boundingShape]);

  // Auto-fit viewport to all pins and shape
  const fitBounds = () => {
    if (!map.current) return;

    const group = new L.FeatureGroup();

    // Add markers to group
    markersRef.current.forEach((marker) => {
      group.addLayer(marker);
    });

    // Add shape to group
    if (shapeLayerRef.current) {
      group.addLayer(shapeLayerRef.current);
    }

    // Fit bounds if group has layers
    if (group.getLayers().length > 0) {
      map.current.fitBounds(group.getBounds(), { padding: [50, 50] });
    }
  };

  return (
    <div className={`leaflet-map-container ${className}`}>
      <div
        ref={mapContainer}
        className="leaflet-map"
        style={{ height: '400px', width: '100%' }}
        role="region"
        aria-label="Interactive map showing location pins"
      />

      {/* Keyboard-accessible text alternative */}
      <div className="map-text-alternative" role="region" aria-label="Location list">
        <h3>Locations</h3>
        {locations.length > 0 ? (
          <table className="locations-table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Latitude</th>
                <th>Longitude</th>
              </tr>
            </thead>
            <tbody>
              {locations.map((location) => (
                <tr key={location.id}>
                  <td>
                    {onLocationClick ? (
                      <button
                        onClick={() => onLocationClick(location)}
                        className="location-link"
                        aria-label={`View ${location.name}`}
                      >
                        {location.name}
                      </button>
                    ) : (
                      location.name
                    )}
                  </td>
                  <td>{location.latitude.toFixed(6)}</td>
                  <td>{location.longitude.toFixed(6)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : (
          <p>No locations to display.</p>
        )}
      </div>
    </div>
  );
}
