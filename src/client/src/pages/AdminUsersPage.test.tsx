import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest';
import { AdminUsersPage } from './AdminUsersPage';
import { AuthProvider } from '../contexts/AuthContext';

// Mock fetch globally
global.fetch = vi.fn();

/**
 * Mock user data for testing
 */
const mockUsers = [
  {
    id: '1',
    username: 'alice',
    displayName: 'Alice Smith',
    email: 'alice@example.com',
    role: 'Admin' as const,
    createdAt: '2024-01-01T00:00:00Z',
  },
  {
    id: '2',
    username: 'bob',
    displayName: 'Bob Johnson',
    email: 'bob@example.com',
    role: 'Standard' as const,
    createdAt: '2024-01-02T00:00:00Z',
  },
  {
    id: '3',
    username: 'charlie',
    displayName: 'Charlie Brown',
    email: 'charlie@example.com',
    role: 'Standard' as const,
    createdAt: '2024-01-03T00:00:00Z',
  },
];

/**
 * Helper to render component with required providers
 */
function renderWithProviders(component: React.ReactElement) {
  return render(
    <BrowserRouter>
      <AuthProvider>{component}</AuthProvider>
    </BrowserRouter>
  );
}

/**
 * Helper to mock successful user list fetch
 */
function mockFetchUsersList(users = mockUsers, totalCount = users.length) {
  (global.fetch as any).mockResolvedValueOnce({
    ok: true,
    json: async () => ({
      users,
      pagination: {
        page: 1,
        pageSize: 20,
        totalCount,
        totalPages: 1,
      },
    }),
  });
}

/**
 * Helper to mock successful role change
 */
function mockFetchRoleChange(userId: string, newRole: 'Admin' | 'Standard') {
  (global.fetch as any).mockResolvedValueOnce({
    ok: true,
    json: async () => ({
      id: userId,
      role: newRole,
    }),
  });
}

describe('AdminUsersPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.setItem('auth_token', 'test-token');
  });

  afterEach(() => {
    localStorage.clear();
  });

  describe('Rendering and Loading States', () => {
    it('should render user list with all columns', async () => {
      // Mock profile fetch
      (global.fetch as any).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          id: 'admin-user',
          username: 'admin',
          displayName: 'Admin User',
          email: 'admin@example.com',
          role: 'Admin',
          avatarImageId: undefined,
          showPublicCollections: true,
        }),
      });

      mockFetchUsersList();

      renderWithProviders(<AdminUsersPage />);

      await waitFor(() => {
        expect(screen.getByText('alice')).toBeInTheDocument();
      });

      // Verify all columns are present
      expect(screen.getByText('Username')).toBeInTheDocument();
      expect(screen.getByText('Display Name')).toBeInTheDocument();
      expect(screen.getByText('Email')).toBeInTheDocument();
      expect(screen.getByText('Role')).toBeInTheDocument();
      expect(screen.getByText('Actions')).toBeInTheDocument();

      // Verify user data is displayed
      expect(screen.getByText('alice')).toBeInTheDocument();
      expect(screen.getByText('Alice Smith')).toBeInTheDocument();
      expect(screen.getByText('alice@example.com')).toBeInTheDocument();
      expect(screen.getByText('bob')).toBeInTheDocument();
      expect(screen.getByText('Bob Johnson')).toBeInTheDocument();
    });

    it('should display role badges with correct styling', async () => {
      // Mock profile fetch
      (global.fetch as any).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          id: 'admin-user',
          username: 'admin',
          displayName: 'Admin User',
          email: 'admin@example.com',
          role: 'Admin',
          avatarImageId: undefined,
          showPublicCollections: true,
        }),
      });

      mockFetchUsersList();

      renderWithProviders(<AdminUsersPage />);

      await waitFor(() => {
        expect(screen.getByText('alice')).toBeInTheDocument();
      });

      // Check for role badges
      const adminBadges = screen.getAllByText('Admin');
      expect(adminBadges.length).toBeGreaterThan(0);

      const standardBadges = screen.getAllByText('Standard');
      expect(standardBadges.length).toBeGreaterThan(0);
    });

    it('should display empty state when no users exist', async () => {
      // Mock profile fetch
      (global.fetch as any).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          id: 'admin-user',
          username: 'admin',
          displayName: 'Admin User',
          email: 'admin@example.com',
          role: 'Admin',
          avatarImageId: undefined,
          showPublicCollections: true,
        }),
      });

      mockFetchUsersList([], 0);

      renderWithProviders(<AdminUsersPage />);

      await waitFor(() => {
        expect(screen.getByText('No users found')).toBeInTheDocument();
      });

      expect(screen.getByText('There are no users in the system yet.')).toBeInTheDocument();
    });
  });

  describe('Promote/Demote Actions', () => {
    it('should handle promote error and revert optimistic update', async () => {
      // Mock profile fetch
      (global.fetch as any).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          id: 'admin-user',
          username: 'admin',
          displayName: 'Admin User',
          email: 'admin@example.com',
          role: 'Admin',
          avatarImageId: undefined,
          showPublicCollections: true,
        }),
      });

      mockFetchUsersList();

      renderWithProviders(<AdminUsersPage />);

      await waitFor(() => {
        expect(screen.getByText('bob')).toBeInTheDocument();
      });

      // Mock failed role change
      (global.fetch as any).mockResolvedValueOnce({
        ok: false,
        json: async () => ({
          error: 'Cannot promote user',
        }),
      });

      const promoteButtons = screen.getAllByText('Promote');
      fireEvent.click(promoteButtons[0]);

      // Wait for error to appear
      await waitFor(() => {
        expect(screen.getByText('Cannot promote user')).toBeInTheDocument();
      });

      // Verify role was reverted
      const standardBadges = screen.getAllByText('Standard');
      expect(standardBadges.length).toBeGreaterThan(0);
    });

    it('should handle demote error and revert optimistic update', async () => {
      // Mock profile fetch
      (global.fetch as any).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          id: 'admin-user',
          username: 'admin',
          displayName: 'Admin User',
          email: 'admin@example.com',
          role: 'Admin',
          avatarImageId: undefined,
          showPublicCollections: true,
        }),
      });

      mockFetchUsersList();

      renderWithProviders(<AdminUsersPage />);

      await waitFor(() => {
        expect(screen.getByText('alice')).toBeInTheDocument();
      });

      // Mock failed role change
      (global.fetch as any).mockResolvedValueOnce({
        ok: false,
        json: async () => ({
          error: 'Cannot demote the last admin in the system',
        }),
      });

      const demoteButtons = screen.getAllByText('Demote');
      fireEvent.click(demoteButtons[0]);

      // Wait for error to appear
      await waitFor(() => {
        expect(screen.getByText('Cannot demote the last admin in the system')).toBeInTheDocument();
      });

      // Verify role was reverted
      const adminBadges = screen.getAllByText('Admin');
      expect(adminBadges.length).toBeGreaterThan(0);
    });
  });

  describe('Error Handling', () => {
    it('should display error message when fetch fails', async () => {
      // Mock profile fetch
      (global.fetch as any).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          id: 'admin-user',
          username: 'admin',
          displayName: 'Admin User',
          email: 'admin@example.com',
          role: 'Admin',
          avatarImageId: undefined,
          showPublicCollections: true,
        }),
      });

      // Mock failed users fetch
      (global.fetch as any).mockResolvedValueOnce({
        ok: false,
        status: 500,
        json: async () => ({ error: 'Server error' }),
      });

      renderWithProviders(<AdminUsersPage />);

      await waitFor(() => {
        expect(screen.getByRole('alert')).toBeInTheDocument();
      });

      expect(screen.getByText('Failed to fetch users')).toBeInTheDocument();
    });
  });

  describe('Accessibility', () => {
    it('should have proper ARIA labels on buttons', async () => {
      // Mock profile fetch
      (global.fetch as any).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          id: 'admin-user',
          username: 'admin',
          displayName: 'Admin User',
          email: 'admin@example.com',
          role: 'Admin',
          avatarImageId: undefined,
          showPublicCollections: true,
        }),
      });

      mockFetchUsersList();

      renderWithProviders(<AdminUsersPage />);

      await waitFor(() => {
        expect(screen.getByText('alice')).toBeInTheDocument();
      });

      // Check for aria-labels on action buttons
      expect(screen.getByLabelText(/Promote Bob Johnson to Admin/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/Demote Alice Smith to Standard/i)).toBeInTheDocument();
    });

    it('should have proper table semantics', async () => {
      // Mock profile fetch
      (global.fetch as any).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          id: 'admin-user',
          username: 'admin',
          displayName: 'Admin User',
          email: 'admin@example.com',
          role: 'Admin',
          avatarImageId: undefined,
          showPublicCollections: true,
        }),
      });

      mockFetchUsersList();

      renderWithProviders(<AdminUsersPage />);

      await waitFor(() => {
        expect(screen.getByText('alice')).toBeInTheDocument();
      });

      // Check for table role and aria-label
      const table = screen.getByRole('grid', { name: /Users list/i });
      expect(table).toBeInTheDocument();

      // Check for column headers
      const headers = screen.getAllByRole('columnheader');
      expect(headers.length).toBeGreaterThan(0);
    });
  });
});
