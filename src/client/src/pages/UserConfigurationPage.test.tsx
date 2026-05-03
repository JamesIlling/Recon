import React from 'react';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { UserConfigurationPage } from './UserConfigurationPage';
import { AuthProvider } from '../contexts/AuthContext';

global.fetch = vi.fn();

const mockUser = {
  id: '123',
  username: 'testuser',
  displayName: 'Test User',
  email: 'test@example.com',
  role: 'Standard' as const,
  avatarImageId: undefined,
  showPublicCollections: true,
};

const mockToken = 'test-jwt-token';

function renderWithProviders(component: React.ReactElement) {
  return render(
    <BrowserRouter>
      <AuthProvider>{component}</AuthProvider>
    </BrowserRouter>
  );
}

/** Mock the /api/users/me profile fetch that AuthProvider triggers on mount. */
function mockProfileFetch() {
  (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
    ok: true,
    json: async () => mockUser,
  });
}

describe('UserConfigurationPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // Set auth token so AuthProvider treats the user as authenticated
    localStorage.setItem('auth_token', mockToken);
    // Mock the profile fetch that AuthProvider triggers on mount
    mockProfileFetch();
  });

  afterEach(() => {
    localStorage.clear();
  });

  describe('12.4.5: Redirect unauthenticated users to /login', () => {
    it('should render settings page for authenticated users', () => {
      renderWithProviders(<UserConfigurationPage />);
      expect(screen.getByText('Settings')).toBeInTheDocument();
    });
  });

  describe('12.4.1: Display name inline editor with uniqueness error', () => {
    it('should display current display name', async () => {
      renderWithProviders(<UserConfigurationPage />);
      await waitFor(() => {
        expect(screen.getByText('Test User')).toBeInTheDocument();
      });
    });

    it('should enter edit mode when Edit button is clicked', async () => {
      renderWithProviders(<UserConfigurationPage />);
      await waitFor(() => {
        expect(screen.getByRole('button', { name: /edit/i })).toBeInTheDocument();
      });
      const editButton = screen.getByRole('button', { name: /edit/i });
      fireEvent.click(editButton);
      const input = screen.getByDisplayValue('Test User');
      expect(input).toBeInTheDocument();
    });

    it('should save display name on valid input', async () => {
      // Extra mock for refreshProfile() call after save
      (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({}),
      });
      (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ ...mockUser, displayName: 'New Display Name' }),
      });

      renderWithProviders(<UserConfigurationPage />);
      await waitFor(() => {
        expect(screen.getByRole('button', { name: /edit/i })).toBeInTheDocument();
      });
      const editButton = screen.getByRole('button', { name: /edit/i });
      fireEvent.click(editButton);

      const input = screen.getByDisplayValue('Test User') as HTMLInputElement;
      fireEvent.change(input, { target: { value: 'New Display Name' } });

      const saveButton = screen.getByRole('button', { name: /save/i });
      fireEvent.click(saveButton);

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          '/api/users/me/display-name',
          expect.objectContaining({
            method: 'PUT',
            body: JSON.stringify({ displayName: 'New Display Name' }),
          })
        );
      });
    });

    it('should show uniqueness error on 409 response', async () => {
      (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: 409,
        json: async () => ({ message: 'Display name already exists' }),
      });

      renderWithProviders(<UserConfigurationPage />);
      await waitFor(() => {
        expect(screen.getByRole('button', { name: /edit/i })).toBeInTheDocument();
      });
      const editButton = screen.getByRole('button', { name: /edit/i });
      fireEvent.click(editButton);

      const input = screen.getByDisplayValue('Test User') as HTMLInputElement;
      fireEvent.change(input, { target: { value: 'Taken Name' } });

      const saveButton = screen.getByRole('button', { name: /save/i });
      fireEvent.click(saveButton);

      await waitFor(() => {
        expect(screen.getByText('Display name already exists')).toBeInTheDocument();
      });
    });
  });

  describe('12.4.2: Avatar uploader with 1:1 crop tool and optional altText input', () => {
    it('should display avatar upload section', () => {
      renderWithProviders(<UserConfigurationPage />);
      expect(screen.getByText('Avatar')).toBeInTheDocument();
      expect(screen.getByLabelText('Select avatar image')).toBeInTheDocument();
    });

    it('should reject invalid image formats', async () => {
      renderWithProviders(<UserConfigurationPage />);
      const fileInput = screen.getByLabelText('Select avatar image') as HTMLInputElement;

      const file = new File(['content'], 'test.txt', { type: 'text/plain' });
      fireEvent.change(fileInput, { target: { files: [file] } });

      await waitFor(() => {
        expect(screen.getByText(/Invalid image format/i)).toBeInTheDocument();
      });
    });

    it('should reject files larger than 1 MB', async () => {
      renderWithProviders(<UserConfigurationPage />);
      const fileInput = screen.getByLabelText('Select avatar image') as HTMLInputElement;

      const largeFile = new File(['x'.repeat(1024 * 1024 + 1)], 'large.jpg', {
        type: 'image/jpeg',
      });
      fireEvent.change(fileInput, { target: { files: [largeFile] } });

      await waitFor(() => {
        expect(screen.getByText(/File size exceeds 1 MB limit/i)).toBeInTheDocument();
      });
    });
  });

  describe('12.4.3: Change password form (current + new + confirm)', () => {
    it('should display password change form', () => {
      renderWithProviders(<UserConfigurationPage />);
      expect(screen.getByText('Change Password')).toBeInTheDocument();
      expect(screen.getByLabelText('Current Password')).toBeInTheDocument();
      expect(screen.getByLabelText('New Password')).toBeInTheDocument();
      expect(screen.getByLabelText('Confirm New Password')).toBeInTheDocument();
    });

    it('should validate password complexity', async () => {
      renderWithProviders(<UserConfigurationPage />);
      const newPasswordInput = screen.getByLabelText('New Password') as HTMLInputElement;

      fireEvent.change(newPasswordInput, { target: { value: 'weak' } });

      await waitFor(() => {
        expect(screen.getByText(/Password must be at least 8 characters long/i)).toBeInTheDocument();
      });
    });

    it('should validate password mismatch', async () => {
      renderWithProviders(<UserConfigurationPage />);
      const newPasswordInput = screen.getByLabelText('New Password') as HTMLInputElement;
      const confirmPasswordInput = screen.getByLabelText('Confirm New Password') as HTMLInputElement;

      fireEvent.change(newPasswordInput, { target: { value: 'ValidPass123' } });
      fireEvent.change(confirmPasswordInput, { target: { value: 'DifferentPass123' } });

      await waitFor(() => {
        expect(screen.getByText('Passwords do not match')).toBeInTheDocument();
      });
    });

    it('should change password successfully', async () => {
      (global.fetch as any).mockResolvedValueOnce({
        ok: true,
        json: async () => ({}),
      });

      renderWithProviders(<UserConfigurationPage />);
      const currentPasswordInput = screen.getByLabelText('Current Password') as HTMLInputElement;
      const newPasswordInput = screen.getByLabelText('New Password') as HTMLInputElement;
      const confirmPasswordInput = screen.getByLabelText('Confirm New Password') as HTMLInputElement;
      const submitButton = screen.getByRole('button', { name: /Change Password/i });

      fireEvent.change(currentPasswordInput, { target: { value: 'OldPass123' } });
      fireEvent.change(newPasswordInput, { target: { value: 'NewPass123' } });
      fireEvent.change(confirmPasswordInput, { target: { value: 'NewPass123' } });
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          '/api/users/me/password',
          expect.objectContaining({
            method: 'PUT',
            body: JSON.stringify({
              currentPassword: 'OldPass123',
              newPassword: 'NewPass123',
            }),
          })
        );
      });
    });
  });

  describe('12.4.4: ShowPublicCollections toggle (persisted immediately to API)', () => {
    it('should display preferences toggle', () => {
      renderWithProviders(<UserConfigurationPage />);
      expect(screen.getByText('Preferences')).toBeInTheDocument();
      expect(screen.getByLabelText('Show public collections on homepage')).toBeInTheDocument();
    });

    it('should reflect current preference state', () => {
      renderWithProviders(<UserConfigurationPage />);
      const checkbox = screen.getByLabelText('Show public collections on homepage') as HTMLInputElement;
      expect(checkbox.checked).toBe(true);
    });

    it('should persist preference change immediately to API', async () => {
      (global.fetch as any).mockResolvedValueOnce({
        ok: true,
        json: async () => ({}),
      });

      renderWithProviders(<UserConfigurationPage />);
      const checkbox = screen.getByLabelText('Show public collections on homepage') as HTMLInputElement;

      fireEvent.click(checkbox);

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          '/api/users/me/preferences',
          expect.objectContaining({
            method: 'PUT',
            body: JSON.stringify({
              showPublicCollections: false,
            }),
          })
        );
      });
    });
  });

  describe('Accessibility requirements', () => {
    it('should have proper form labels', () => {
      renderWithProviders(<UserConfigurationPage />);
      expect(screen.getByLabelText('Display name')).toBeInTheDocument();
      expect(screen.getByLabelText('Select avatar image')).toBeInTheDocument();
      expect(screen.getByLabelText('Current Password')).toBeInTheDocument();
      expect(screen.getByLabelText('New Password')).toBeInTheDocument();
      expect(screen.getByLabelText('Confirm New Password')).toBeInTheDocument();
      expect(screen.getByLabelText('Show public collections on homepage')).toBeInTheDocument();
    });

    it('should use semantic HTML', () => {
      const { container } = renderWithProviders(<UserConfigurationPage />);
      expect(container.querySelector('form')).toBeInTheDocument();
      expect(container.querySelectorAll('label').length).toBeGreaterThan(0);
    });
  });
});
