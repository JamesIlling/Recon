import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import './AuditLogPage.css';

/**
 * Audit event data returned from the audit log API endpoint.
 */
interface AuditEventDto {
  id: string;
  eventType: string;
  actingUserId: string | null;
  actingUserLabel: string;
  targetResourceType: string | null;
  targetResourceId: string | null;
  outcome: 'Success' | 'Failure';
  sourceIp: string;
  occurredAt: string;
}

/**
 * Filter state for the audit log.
 */
interface AuditLogFilters {
  eventType: string;
  outcome: string;
  startDate: string;
  endDate: string;
  searchText: string;
}

/**
 * AuditLogPage displays a paginated table of audit events with comprehensive filter controls.
 * Implements task 14.5 with all required features:
 * - Paginated table with columns: Timestamp, Event Type, Acting User, Resource Type, Resource ID, Outcome
 * - Filter controls: Event Type dropdown, Outcome dropdown, Date range picker, Text search
 * - Page size selector (25, 50, 100, 200)
 * - Loading and error states
 * - WCAG 2.1 Level AA accessibility compliance
 */
export function AuditLogPage(): JSX.Element {
  const navigate = useNavigate();
  const { token, user, isLoading: authLoading } = useAuth();

  const [auditEvents, setAuditEvents] = useState<AuditEventDto[]>([]);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(50);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  // Filter state
  const [filters, setFilters] = useState<AuditLogFilters>({
    eventType: '',
    outcome: '',
    startDate: '',
    endDate: '',
    searchText: '',
  });

  // Available event types (populated from API or hardcoded common ones)
  const [eventTypes] = useState<string[]>([
    'LocationCreated',
    'LocationEdited',
    'LocationDeleted',
    'PendingEditSubmitted',
    'PendingEditApproved',
    'PendingEditRejected',
    'UserRegistered',
    'UserPromoted',
    'UserDemoted',
    'CollectionCreated',
    'CollectionUpdated',
    'CollectionDeleted',
    'MembershipApproved',
    'MembershipRejected',
    'PasswordChanged',
    'PasswordReset',
    'AvatarUploaded',
    'PreferenceChanged',
  ]);

  // Redirect unauthenticated users to login
  useEffect(() => {
    if (!authLoading && !token) {
      navigate('/login', { replace: true });
      return;
    }

    // Redirect non-admin users to home
    if (!authLoading && user && user.role !== 'Admin') {
      navigate('/', { replace: true });
    }
  }, [token, user, authLoading, navigate]);

  // Fetch audit log
  useEffect(() => {
    if (!token || authLoading || user?.role !== 'Admin') return;

    const fetchAuditLog = async () => {
      try {
        setIsLoading(true);
        setError('');

        // Build query parameters
        const params = new URLSearchParams();
        params.append('page', page.toString());
        params.append('pageSize', pageSize.toString());

        if (filters.eventType) {
          params.append('eventType', filters.eventType);
        }
        if (filters.outcome) {
          params.append('outcome', filters.outcome);
        }
        if (filters.startDate) {
          params.append('startDate', filters.startDate);
        }
        if (filters.endDate) {
          params.append('endDate', filters.endDate);
        }
        if (filters.searchText) {
          // Search text can be used for resource ID or acting user label
          params.append('resourceId', filters.searchText);
        }

        const response = await fetch(`/api/admin/audit-log?${params.toString()}`, {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        });

        if (!response.ok) {
          if (response.status === 403) {
            throw new Error('You do not have permission to access this page');
          }
          throw new Error('Failed to fetch audit log');
        }

        const data = await response.json();
        setAuditEvents(data.auditEvents || []);
        setTotalCount(data.pagination?.totalCount || 0);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'An error occurred');
      } finally {
        setIsLoading(false);
      }
    };

    fetchAuditLog();
  }, [page, pageSize, filters, token, authLoading, user?.role]);

  /**
   * Handle filter changes
   */
  const handleFilterChange = (key: keyof AuditLogFilters, value: string) => {
    setFilters((prev) => ({ ...prev, [key]: value }));
    setPage(1); // Reset to first page when filters change
  };

  /**
   * Reset all filters
   */
  const handleResetFilters = () => {
    setFilters({
      eventType: '',
      outcome: '',
      startDate: '',
      endDate: '',
      searchText: '',
    });
    setPage(1);
  };

  /**
   * Format timestamp for display
   */
  const formatTimestamp = (isoString: string): string => {
    try {
      const date = new Date(isoString);
      return date.toLocaleString('en-US', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit',
        hour12: true,
      });
    } catch {
      return isoString;
    }
  };

  const totalPages = Math.ceil(totalCount / pageSize);
  const hasActiveFilters =
    filters.eventType ||
    filters.outcome ||
    filters.startDate ||
    filters.endDate ||
    filters.searchText;

  // Show loading skeleton while data is in flight
  if (authLoading || isLoading) {
    return (
      <div className="audit-log-page">
        <div className="page-header">
          <h1>Audit Log</h1>
          <p className="page-subtitle">System activity and event history</p>
        </div>
        <div className="loading-skeleton">
          <div className="skeleton-row" />
          <div className="skeleton-row" />
          <div className="skeleton-row" />
          <div className="skeleton-row" />
        </div>
      </div>
    );
  }

  return (
    <div className="audit-log-page">
      <div className="page-header">
        <h1>Audit Log</h1>
        <p className="page-subtitle">System activity and event history</p>
      </div>

      {error && (
        <div className="error-message" role="alert">
          {error}
        </div>
      )}

      {/* Filter Controls */}
      <div className="filter-section">
        <div className="filter-controls">
          {/* Event Type Filter */}
          <div className="filter-group">
            <label htmlFor="event-type-filter">Event Type</label>
            <select
              id="event-type-filter"
              value={filters.eventType}
              onChange={(e) => handleFilterChange('eventType', e.target.value)}
              className="filter-select"
              aria-label="Filter by event type"
            >
              <option value="">All Events</option>
              {eventTypes.map((type) => (
                <option key={type} value={type}>
                  {type}
                </option>
              ))}
            </select>
          </div>

          {/* Outcome Filter */}
          <div className="filter-group">
            <label htmlFor="outcome-filter">Outcome</label>
            <select
              id="outcome-filter"
              value={filters.outcome}
              onChange={(e) => handleFilterChange('outcome', e.target.value)}
              className="filter-select"
              aria-label="Filter by outcome"
            >
              <option value="">All Outcomes</option>
              <option value="Success">Success</option>
              <option value="Failure">Failure</option>
            </select>
          </div>

          {/* Start Date Filter */}
          <div className="filter-group">
            <label htmlFor="start-date-filter">Start Date</label>
            <input
              id="start-date-filter"
              type="datetime-local"
              value={filters.startDate}
              onChange={(e) => handleFilterChange('startDate', e.target.value)}
              className="filter-input"
              aria-label="Filter by start date"
            />
          </div>

          {/* End Date Filter */}
          <div className="filter-group">
            <label htmlFor="end-date-filter">End Date</label>
            <input
              id="end-date-filter"
              type="datetime-local"
              value={filters.endDate}
              onChange={(e) => handleFilterChange('endDate', e.target.value)}
              className="filter-input"
              aria-label="Filter by end date"
            />
          </div>

          {/* Text Search Filter */}
          <div className="filter-group">
            <label htmlFor="search-filter">Search (Resource ID)</label>
            <input
              id="search-filter"
              type="text"
              placeholder="Search by resource ID..."
              value={filters.searchText}
              onChange={(e) => handleFilterChange('searchText', e.target.value)}
              className="filter-input"
              aria-label="Search by resource ID"
            />
          </div>

          {/* Reset Filters Button */}
          {hasActiveFilters && (
            <button
              onClick={handleResetFilters}
              className="btn-reset-filters"
              aria-label="Reset all filters"
            >
              Reset Filters
            </button>
          )}
        </div>

        {/* Page Size Selector */}
        <div className="page-size-selector">
          <label htmlFor="page-size-select">Items per page:</label>
          <select
            id="page-size-select"
            value={pageSize}
            onChange={(e) => {
              setPageSize(parseInt(e.target.value, 10));
              setPage(1);
            }}
            className="page-size-select"
            aria-label="Select number of items per page"
          >
            <option value={25}>25</option>
            <option value={50}>50</option>
            <option value={100}>100</option>
            <option value={200}>200</option>
          </select>
        </div>
      </div>

      {/* Audit Log Table */}
      <div className="audit-log-table-container">
        <table className="audit-log-table" role="grid" aria-label="Audit log events">
          <thead>
            <tr role="row">
              <th role="columnheader" scope="col">
                Timestamp
              </th>
              <th role="columnheader" scope="col">
                Event Type
              </th>
              <th role="columnheader" scope="col">
                Acting User
              </th>
              <th role="columnheader" scope="col">
                Resource Type
              </th>
              <th role="columnheader" scope="col">
                Resource ID
              </th>
              <th role="columnheader" scope="col">
                Outcome
              </th>
            </tr>
          </thead>
          <tbody>
            {auditEvents.map((event) => (
              <tr key={event.id} role="row">
                <td role="gridcell" data-label="Timestamp">
                  <time dateTime={event.occurredAt}>
                    {formatTimestamp(event.occurredAt)}
                  </time>
                </td>
                <td role="gridcell" data-label="Event Type">
                  <span className="event-type-badge">{event.eventType}</span>
                </td>
                <td role="gridcell" data-label="Acting User">
                  {event.actingUserLabel || 'System'}
                </td>
                <td role="gridcell" data-label="Resource Type">
                  {event.targetResourceType || '—'}
                </td>
                <td role="gridcell" data-label="Resource ID">
                  <code className="resource-id">
                    {event.targetResourceId ? event.targetResourceId.substring(0, 8) : '—'}
                  </code>
                </td>
                <td role="gridcell" data-label="Outcome">
                  <span
                    className={`outcome-badge outcome-badge--${event.outcome.toLowerCase()}`}
                    aria-label={`Outcome: ${event.outcome}`}
                  >
                    {event.outcome}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Pagination Controls */}
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

          <span className="page-info" aria-live="polite">
            Page {page} of {totalPages} ({totalCount} total events)
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

      {/* Empty State */}
      {auditEvents.length === 0 && !error && (
        <div className="empty-state">
          <div className="empty-state-icon">📋</div>
          <h2>No audit events found</h2>
          <p>
            {hasActiveFilters
              ? 'No events match your filter criteria. Try adjusting your filters.'
              : 'There are no audit events in the system yet.'}
          </p>
        </div>
      )}
    </div>
  );
}

/**
 * Wrapped AuditLogPage with admin-only access control.
 * Redirects non-admin users to home page.
 */
export function AuditLogPageProtected(): JSX.Element {
  return <AuditLogPage />;
}

export default AuditLogPageProtected;
