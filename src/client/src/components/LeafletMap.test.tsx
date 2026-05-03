import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { LeafletMap, MapLocation } from './LeafletMap';

// Mock leaflet
vi.mock('leaflet', () => {
  const mockMarker = {
    bindTooltip: vi.fn(function() { return this; }),
    addTo: vi.fn(function() { return this; }),
    on: vi.fn(function() { return this; }),
    remove: vi.fn(),
  };

  const mockGeoJSON = {
    addTo: vi.fn(function() { return this; }),
  };

  const mockMap = {
    setView: vi.fn(function() { return this; }),
    addLayer: vi.fn(),
    remove: vi.fn(),
    fitBounds: vi.fn(),
    removeLayer: vi.fn(),
  };

  return {
    default: {
      map: vi.fn(() => mockMap),
      tileLayer: vi.fn(() => ({
        addTo: vi.fn(() => ({})),
      })),
      marker: vi.fn(() => mockMarker),
      geoJSON: vi.fn(() => mockGeoJSON),
      FeatureGroup: vi.fn(() => ({
        addLayer: vi.fn(),
        getLayers: vi.fn(() => []),
        getBounds: vi.fn(() => ({ pad: vi.fn(() => ({})) })),
      })),
    },
  };
});

describe('LeafletMap', () => {
  const mockLocations: MapLocation[] = [
    {
      id: '1',
      name: 'Location 1',
      latitude: 51.5074,
      longitude: -0.1278,
    },
    {
      id: '2',
      name: 'Location 2',
      latitude: 48.8566,
      longitude: 2.3522,
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders map container', () => {
    render(<LeafletMap locations={[]} />);
    const mapContainer = screen.getByRole('region', { name: /interactive map/i });
    expect(mapContainer).toBeInTheDocument();
  });

  it('renders keyboard-accessible text alternative', () => {
    render(<LeafletMap locations={mockLocations} />);
    const textAlternative = screen.getByRole('region', { name: /location list/i });
    expect(textAlternative).toBeInTheDocument();
  });

  it('displays location names in text alternative', () => {
    render(<LeafletMap locations={mockLocations} />);
    expect(screen.getByText('Location 1')).toBeInTheDocument();
    expect(screen.getByText('Location 2')).toBeInTheDocument();
  });

  it('displays coordinates in text alternative', () => {
    render(<LeafletMap locations={mockLocations} />);
    expect(screen.getByText('51.507400')).toBeInTheDocument();
    expect(screen.getByText('-0.127800')).toBeInTheDocument();
  });

  it('shows empty state when no locations', () => {
    render(<LeafletMap locations={[]} />);
    expect(screen.getByText('No locations to display.')).toBeInTheDocument();
  });

  it('renders location table with correct headers', () => {
    render(<LeafletMap locations={mockLocations} />);
    expect(screen.getByText('Name')).toBeInTheDocument();
    expect(screen.getByText('Latitude')).toBeInTheDocument();
    expect(screen.getByText('Longitude')).toBeInTheDocument();
  });

  it('calls onLocationClick when location button is clicked', () => {
    const onLocationClick = vi.fn();
    render(<LeafletMap locations={mockLocations} onLocationClick={onLocationClick} />);

    const buttons = screen.getAllByRole('button');
    const locationButton = buttons.find((btn) => btn.textContent === 'Location 1');
    expect(locationButton).toBeDefined();
  });
});
