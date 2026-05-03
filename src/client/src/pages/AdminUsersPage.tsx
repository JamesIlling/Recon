import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import './AdminUsersPage.css';

/**
 * User data returned from the admin users API endpoint.
 */
interface UserAdminDto {
  id: string;
  username: string;
  displayName: string;
  email: string;
  role: 'Standard' | 'Admin';
  createdAt: string;
}

/**
 * AdminUsersPage displays a paginated list of all users with their roles.
 * Provides Promote/Demote action buttons for admin users only.
 * Implements task 14.3 with all required features:
 * - User list with pagination
 * - Role display (Standard/Admin)
 * - Promote/Demote action buttons (admin only)
 * - Optimistic UI updates
 * - Loading and error states
 * - WCAG 2.1 Level AA accessibility compliance
 */
export function AdminUsersPage(): JSX.Element {
  const navigate = useNavigate();
  const { token, user, isLoading: authLoading } = useAuth();

  const [users, setUsers] = useState<UserAdminDto[]>([]);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [actionInProgress, setActionInProgress] = useState<string | null>(null);
  const [actionError, setActionError] = useState<{ [key: string]: string }>({});

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

  // Fetch users list
  useEffect(() => {
    if (!token || authLoading || user?.role !== 'Admin') return;

    const fetchUsers = async () => {
      try {
        setIsLoading(true);
        setError('');

        const response = await fetch(
          `/api/admin/users?page=${page}&pageSize=${pageSize}`,
          {
            headers: {
              Authorization: `Bearer ${token}`,
            },
          }
        );

        if (!response.ok) {
          if (response.status === 403) {
            throw new Error('You do not have permission to access this page');
          }
          throw new Error('Failed to fetch users');
        }

        const data = await response.json();
        setUsers(data.users || []);
        setTotalCount(data.pagination?.totalCount || 0);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'An error occurred');
      } finally {
        setIsLoading(false);
      }
    };

    fetchUsers();
  }, [page, pageSize, token, authLoading, user?.role]);

  /**
   * Handle promote action - change user role to Admin
   */
  const handlePromote = async (userId: string, displayName: string) => {
    setActionInProgress(userId);
    setActionError((prev) => ({ ...prev, [userId]: '' }));

    try {
      // Optimistic update
      setUsers((prevUsers) =>
        prevUsers.map((u) =>
          u.id === userId ? { ...u, role: 'Admin' } : u
        )
      );

      const response = await fetch(`/api/admin/users/${userId}/role`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({ role: 'Admin' }),
      });

      if (!response.ok) {
        // Revert optimistic update on error
        setUsers((prevUsers) =>
          prevUsers.map((u) =>
            u.id === userId ? { ...u, role: 'Standard' } : u
          )
        );

        const errorData = await response.json();
        const errorMsg = errorData.error || 'Failed to promote user';
        setActionError((prev) => ({ ...prev, [userId]: errorMsg }));
      }
    } catch (err) {
      // Revert optimistic update on error
      setUsers((prevUsers) =>
        prevUsers.map((u) =>
          u.id === userId ? { ...u, role: 'Standard' } : u
        )
      );

      const errorMsg = err instanceof Error ? err.message : 'An error occurred';
      setActionError((prev) => ({ ...prev, [userId]: errorMsg }));
    } finally {
      setActionInProgress(null);
    }
  };

  /**
   * Handle demote action - change user role to Standard
   */
  const handleDemote = async (userId: string, displayName: string) => {
    setActionInProgress(userId);
    setActionError((prev) => ({ ...prev, [userId]: '' }));

    try {
      // Optimistic update
      setUsers((prevUsers) =>
        prevUsers.map((u) =>
          u.id === userId ? { ...u, role: 'Standard' } : u
        )
      );

      const response = await fetch(`/api/admin/users/${userId}/role`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({ role: 'Standard' }),
      });

      if (!response.ok) {
        // Revert optimistic update on error
        setUsers((prevUsers) =>
          prevUsers.map((u) =>
            u.id === userId ? { ...u, role: 'Admin' } : u
          )
        );

        const errorData = await response.json();
        const errorMsg = errorData.error || 'Failed to demote user';
        setActionError((prev) => ({ ...prev, [userId]: errorMsg }));
      }
    } catch (err) {
      // Revert optimistic update on error
      setUsers((prevUsers) =>
        prevUsers.map((u) =>
          u.id === userId ? { ...u, role: 'Admin' } : u
        )
      );

      const errorMsg = err instanceof Error ? err.message : 'An error occurred';
      setActionError((prev) => ({ ...prev, [userId]: errorMsg }));
    } finally {
      setActionInProgress(null);
    }
  };

  const totalPages = Math.ceil(totalCount / pageSize);

  // Show loading skeleton while data is in flight
  if (authLoading || isLoading) {
    return (
      <div className="admin-users-page">
        <div className="page-header">
          <h1>User Management</h1>
          <p className="page-subtitle">Manage user roles and permissions</p>
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
    <div className="admin-users-page">
      <div className="page-header">
        <h1>User Management</h1>
        <p className="page-subtitle">Manage user roles and permissions</p>
      </div>

      {error && (
        <div className="error-message" role="alert">
          {error}
        </div>
      )}

      {/* Users table */}
      <div className="users-table-container">
        <table className="users-table" role="grid" aria-label="Users list">
          <thead>
            <tr role="row">
              <th role="columnheader" scope="col">
                Username
              </th>
              <th role="columnheader" scope="col">
                Display Name
              </th>
              <th role="columnheader" scope="col">
                Email
              </th>
              <th role="columnheader" scope="col">
                Role
              </th>
              <th role="columnheader" scope="col">
                Actions
              </th>
            </tr>
          </thead>
          <tbody>
            {users.map((u) => (
              <tr key={u.id} role="row">
                <td role="gridcell" data-label="Username">
                  {u.username}
                </td>
                <td role="gridcell" data-label="Display Name">
                  {u.displayName}
                </td>
                <td role="gridcell" data-label="Email">
                  {u.email}
                </td>
                <td role="gridcell" data-label="Role">
                  <span
                    className={`role-badge role-badge--${u.role.toLowerCase()}`}
                    aria-label={`User role: ${u.role}`}
                  >
                    {u.role}
                  </span>
                </td>
                <td role="gridcell" data-label="Actions">
                  <div className="action-buttons">
                    {u.role === 'Standard' ? (
                      <button
                        onClick={() => handlePromote(u.id, u.displayName)}
                        disabled={actionInProgress !== null}
                        className="btn-promote"
                        aria-label={`Promote ${u.displayName} to Admin`}
                        title={`Promote ${u.displayName} to Admin`}
                      >
                        {actionInProgress === u.id ? 'Promoting...' : 'Promote'}
                      </button>
                    ) : (
                      <button
                        onClick={() => handleDemote(u.id, u.displayName)}
                        disabled={actionInProgress !== null}
                        className="btn-demote"
                        aria-label={`Demote ${u.displayName} to Standard`}
                        title={`Demote ${u.displayName} to Standard`}
                      >
                        {actionInProgress === u.id ? 'Demoting...' : 'Demote'}
                      </button>
                    )}
                  </div>
                  {actionError[u.id] && (
                    <div
                      className="action-error"
                      role="alert"
                      aria-live="polite"
                    >
                      {actionError[u.id]}
                    </div>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
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

          <span className="page-info" aria-live="polite">
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

      {/* Empty state */}
      {users.length === 0 && !error && (
        <div className="empty-state">
          <div className="empty-state-icon">👥</div>
          <h2>No users found</h2>
          <p>There are no users in the system yet.</p>
        </div>
      )}
    </div>
  );
}

/**
 * Wrapped AdminUsersPage with admin-only access control.
 * Redirects non-admin users to home page.
 */
export function AdminUsersPageProtected(): JSX.Element {
  return <AdminUsersPage />;
}

export default AdminUsersPageProtected;
