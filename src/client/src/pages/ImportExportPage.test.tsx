import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest';
import { ImportExportPage } from './ImportExportPage';
import { AuthProvider } from '../contexts/AuthContext';

// Mock fetch globally
global.fetch = vi.fn();

// Mock URL.createObjectURL / revokeObjectURL (not available in jsdom)
global.URL.createObjectURL = vi.fn(() => 'blob:mock-url');
global.URL.revokeObjectURL = vi.fn();

/**
 * Helper to render the component with required providers.
 */
function renderWithProviders(component: React.ReactElement) {
  return render(
    <BrowserRouter>
      <AuthProvider>{component}</AuthProvider>
    </BrowserRouter>
  );
}

/**
 * Helper to mock the /api/users/me profile fetch that AuthProvider triggers.
 */
function mockAdminProfile() {
  (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
    ok: true,
    json: async () => ({
      id: 'admin-id',
      username: 'admin',
      displayName: 'Admin User',
      email: 'admin@example.com',
      role: 'Admin',
      avatarImageId: undefined,
      showPublicCollections: true,
    }),
  });
}

/** Get the export encryption key input specifically (not the toggle button). */
const getExportKeyInput = () =>
  screen.getByLabelText(/Encryption key/i, { selector: 'input' });

/** Get the import decryption key input specifically (not the toggle button). */
const getImportKeyInput = () =>
  screen.getByLabelText(/Decryption key/i, { selector: 'input' });

/** Get the backup file input. */
const getFileInput = () =>
  screen.getByLabelText(/Backup file/i, { selector: 'input' });

describe('ImportExportPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.setItem('auth_token', 'test-token');
  });

  afterEach(() => {
    localStorage.clear();
  });

  // -------------------------------------------------------------------------
  // Rendering
  // -------------------------------------------------------------------------

  describe('Rendering', () => {
    it('renders the page heading', async () => {
      mockAdminProfile();
      renderWithProviders(<ImportExportPage />);

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /Data Export & Import/i })).toBeInTheDocument();
      });
    });

    it('renders the Export section', async () => {
      mockAdminProfile();
      renderWithProviders(<ImportExportPage />);

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /Export Data/i })).toBeInTheDocument();
      });

      expect(getExportKeyInput()).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /Download encrypted backup/i })).toBeInTheDocument();
    });

    it('renders the Import section', async () => {
      mockAdminProfile();
      renderWithProviders(<ImportExportPage />);

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /Import Data/i })).toBeInTheDocument();
      });

      expect(getFileInput()).toBeInTheDocument();
      expect(getImportKeyInput()).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /Import backup/i })).toBeInTheDocument();
    });
  });

  // -------------------------------------------------------------------------
  // Export form — validation
  // -------------------------------------------------------------------------

  describe('Export form validation', () => {
    it('shows error when encryption key is empty', async () => {
      mockAdminProfile();
      renderWithProviders(<ImportExportPage />);

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /Download encrypted backup/i })).toBeInTheDocument();
      });

      fireEvent.click(screen.getByRole('button', { name: /Download encrypted backup/i }));

      await waitFor(() => {
        expect(screen.getByRole('alert')).toBeInTheDocument();
      });

      expect(screen.getByText(/Encryption key is required/i)).toBeInTheDocument();
    });

    it('shows error when encryption key is shorter than 32 characters', async () => {
      mockAdminProfile();
      renderWithProviders(<ImportExportPage />);

      await waitFor(() => {
        expect(getExportKeyInput()).toBeInTheDocument();
      });

      fireEvent.change(getExportKeyInput(), { target: { value: 'short-key' } });
      fireEvent.click(screen.getByRole('button', { name: /Download encrypted backup/i }));

      await waitFor(() => {
        expect(screen.getByText(/must be at least 32 characters/i)).toBeInTheDocument();
      });
    });

    it('does not show error when key is exactly 32 characters and fetch succeeds', async () => {
      mockAdminProfile();
      renderWithProviders(<ImportExportPage />);

      await waitFor(() => {
        expect(getExportKeyInput()).toBeInTheDocument();
      });

      (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        blob: async () => new Blob([new Uint8Array([1, 2, 3])]),
        headers: { get: () => 'attachment; filename="backup.enc.zip"' },
      });

      fireEvent.change(getExportKeyInput(), {
        target: { value: 'exactly-32-characters-long-key!!' },
      });
      fireEvent.click(screen.getByRole('button', { name: /Download encrypted backup/i }));

      await waitFor(() => {
        expect(screen.queryByRole('alert')).not.toBeInTheDocument();
      });
    });
  });

  // -------------------------------------------------------------------------
  // Export form — API interaction
  // -------------------------------------------------------------------------

  describe('Export form API interaction', () => {
    it('shows error message when export API returns an error', async () => {
      mockAdminProfile();
      renderWithProviders(<ImportExportPage />);

      await waitFor(() => {
        expect(getExportKeyInput()).toBeInTheDocument();
      });

      (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: 400,
        json: async () => ({ error: 'EncryptionKey must be at least 32 characters.' }),
      });

      fireEvent.change(getExportKeyInput(), {
        target: { value: 'exactly-32-characters-long-key!!' },
      });
      fireEvent.click(screen.getByRole('button', { name: /Download encrypted backup/i }));

      await waitFor(() => {
        expect(screen.getByText(/EncryptionKey must be at least 32 characters/i)).toBeInTheDocument();
      });
    });

    it('disables the export button while loading', async () => {
      mockAdminProfile();
      renderWithProviders(<ImportExportPage />);

      await waitFor(() => {
        expect(getExportKeyInput()).toBeInTheDocument();
      });

      (global.fetch as ReturnType<typeof vi.fn>).mockImplementationOnce(
        () =>
          new Promise((resolve) =>
            setTimeout(
              () =>
                resolve({
                  ok: true,
                  blob: async () => new Blob([]),
                  headers: { get: () => null },
                }),
              200
            )
          )
      );

      fireEvent.change(getExportKeyInput(), {
        target: { value: 'exactly-32-characters-long-key!!' },
      });
      fireEvent.click(screen.getByRole('button', { name: /Download encrypted backup/i }));

      expect(screen.getByRole('button', { name: /Exporting/i })).toBeDisabled();
    });
  });

  // -------------------------------------------------------------------------
  // Import form — validation
  // -------------------------------------------------------------------------

  describe('Import form validation', () => {
    it('shows error when no file is selected', async () => {
      mockAdminProfile();
      renderWithProviders(<ImportExportPage />);

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /Import backup/i })).toBeInTheDocument();
      });

      fireEvent.change(getImportKeyInput(), {
        target: { value: 'exactly-32-characters-long-key!!' },
      });
      fireEvent.click(screen.getByRole('button', { name: /Import backup/i }));

      await waitFor(() => {
        expect(screen.getByText(/Please select a backup file/i)).toBeInTheDocument();
      });
    });

    it('shows error when decryption key is empty', async () => {
      mockAdminProfile();
      renderWithProviders(<ImportExportPage />);

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /Import backup/i })).toBeInTheDocument();
      });

      fireEvent.click(screen.getByRole('button', { name: /Import backup/i }));

      await waitFor(() => {
        expect(screen.getByText(/Decryption key is required/i)).toBeInTheDocument();
      });
    });

    it('shows error when decryption key is shorter than 32 characters', async () => {
      mockAdminProfile();
      renderWithProviders(<ImportExportPage />);

      await waitFor(() => {
        expect(getImportKeyInput()).toBeInTheDocument();
      });

      fireEvent.change(getImportKeyInput(), { target: { value: 'short' } });
      fireEvent.click(screen.getByRole('button', { name: /Import backup/i }));

      await waitFor(() => {
        expect(screen.getByText(/must be at least 32 characters/i)).toBeInTheDocument();
      });
    });
  });

  // -------------------------------------------------------------------------
  // Import form — API interaction
  // -------------------------------------------------------------------------

  describe('Import form API interaction', () => {
    it('displays import result summary on success', async () => {
      mockAdminProfile();
      renderWithProviders(<ImportExportPage />);

      await waitFor(() => {
        expect(getFileInput()).toBeInTheDocument();
      });

      (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          importUserId: 'import-user-id',
          usersImported: 3,
          usersSkipped: 1,
          locationsImported: 10,
          locationsSkipped: 2,
          collectionsImported: 2,
          collectionsSkipped: 0,
          membersImported: 5,
          membersSkipped: 0,
          namedShapesImported: 0,
          namedShapesSkipped: 0,
          imagesImported: 0,
          imagesSkipped: 0,
          warnings: ['Location "Bad Place" has invalid coordinates; skipped'],
        }),
      });

      const file = new File([new Uint8Array([1, 2, 3])], 'backup.zip', {
        type: 'application/octet-stream',
      });
      fireEvent.change(getFileInput(), { target: { files: [file] } });
      fireEvent.change(getImportKeyInput(), {
        target: { value: 'exactly-32-characters-long-key!!' },
      });
      fireEvent.click(screen.getByRole('button', { name: /Import backup/i }));

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /Import complete/i })).toBeInTheDocument();
      });

      expect(screen.getByText('3')).toBeInTheDocument();
      expect(screen.getByText('10')).toBeInTheDocument();
    });

    it('displays warnings in the result summary', async () => {
      mockAdminProfile();
      renderWithProviders(<ImportExportPage />);

      await waitFor(() => {
        expect(getFileInput()).toBeInTheDocument();
      });

      (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          importUserId: 'id',
          usersImported: 1,
          usersSkipped: 0,
          locationsImported: 0,
          locationsSkipped: 1,
          collectionsImported: 0,
          collectionsSkipped: 0,
          membersImported: 0,
          membersSkipped: 0,
          namedShapesImported: 0,
          namedShapesSkipped: 0,
          imagesImported: 0,
          imagesSkipped: 0,
          warnings: ['Location "Bad Place" has invalid coordinates; skipped'],
        }),
      });

      const file = new File([new Uint8Array([1, 2, 3])], 'backup.zip', {
        type: 'application/octet-stream',
      });
      fireEvent.change(getFileInput(), { target: { files: [file] } });
      fireEvent.change(getImportKeyInput(), {
        target: { value: 'exactly-32-characters-long-key!!' },
      });
      fireEvent.click(screen.getByRole('button', { name: /Import backup/i }));

      await waitFor(() => {
        expect(
          screen.getByText(/Location "Bad Place" has invalid coordinates; skipped/i)
        ).toBeInTheDocument();
      });
    });

    it('shows error message when import API returns 422', async () => {
      mockAdminProfile();
      renderWithProviders(<ImportExportPage />);

      await waitFor(() => {
        expect(getFileInput()).toBeInTheDocument();
      });

      (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: 422,
        json: async () => ({ error: 'Backup archive does not contain manifest.json' }),
      });

      const file = new File([new Uint8Array([1, 2, 3])], 'backup.zip', {
        type: 'application/octet-stream',
      });
      fireEvent.change(getFileInput(), { target: { files: [file] } });
      fireEvent.change(getImportKeyInput(), {
        target: { value: 'exactly-32-characters-long-key!!' },
      });
      fireEvent.click(screen.getByRole('button', { name: /Import backup/i }));

      await waitFor(() => {
        expect(
          screen.getByText(/Backup archive does not contain manifest\.json/i)
        ).toBeInTheDocument();
      });
    });
  });

  // -------------------------------------------------------------------------
  // Accessibility
  // -------------------------------------------------------------------------

  describe('Accessibility', () => {
    it('encryption key input has aria-required', async () => {
      mockAdminProfile();
      renderWithProviders(<ImportExportPage />);

      await waitFor(() => {
        expect(getExportKeyInput()).toBeInTheDocument();
      });

      expect(getExportKeyInput()).toHaveAttribute('aria-required', 'true');
    });

    it('decryption key input has aria-required', async () => {
      mockAdminProfile();
      renderWithProviders(<ImportExportPage />);

      await waitFor(() => {
        expect(getImportKeyInput()).toBeInTheDocument();
      });

      expect(getImportKeyInput()).toHaveAttribute('aria-required', 'true');
    });

    it('error messages use role=alert for screen reader announcement', async () => {
      mockAdminProfile();
      renderWithProviders(<ImportExportPage />);

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /Download encrypted backup/i })).toBeInTheDocument();
      });

      fireEvent.click(screen.getByRole('button', { name: /Download encrypted backup/i }));

      await waitFor(() => {
        expect(screen.getByRole('alert')).toBeInTheDocument();
      });
    });

    it('toggle visibility buttons have descriptive aria-labels', async () => {
      mockAdminProfile();
      renderWithProviders(<ImportExportPage />);

      await waitFor(() => {
        expect(screen.getByLabelText(/Show encryption key/i)).toBeInTheDocument();
      });

      expect(screen.getByLabelText(/Show decryption key/i)).toBeInTheDocument();
    });

    it('toggles key visibility when show/hide button is clicked', async () => {
      mockAdminProfile();
      renderWithProviders(<ImportExportPage />);

      await waitFor(() => {
        expect(getExportKeyInput()).toBeInTheDocument();
      });

      const input = getExportKeyInput();
      expect(input).toHaveAttribute('type', 'password');

      fireEvent.click(screen.getByLabelText(/Show encryption key/i));
      expect(input).toHaveAttribute('type', 'text');

      fireEvent.click(screen.getByLabelText(/Hide encryption key/i));
      expect(input).toHaveAttribute('type', 'password');
    });

    it('import result table has accessible label', async () => {
      mockAdminProfile();
      renderWithProviders(<ImportExportPage />);

      await waitFor(() => {
        expect(getFileInput()).toBeInTheDocument();
      });

      (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          importUserId: 'id',
          usersImported: 1,
          usersSkipped: 0,
          locationsImported: 0,
          locationsSkipped: 0,
          collectionsImported: 0,
          collectionsSkipped: 0,
          membersImported: 0,
          membersSkipped: 0,
          namedShapesImported: 0,
          namedShapesSkipped: 0,
          imagesImported: 0,
          imagesSkipped: 0,
          warnings: [],
        }),
      });

      const file = new File([new Uint8Array([1])], 'backup.zip', {
        type: 'application/octet-stream',
      });
      fireEvent.change(getFileInput(), { target: { files: [file] } });
      fireEvent.change(getImportKeyInput(), {
        target: { value: 'exactly-32-characters-long-key!!' },
      });
      fireEvent.click(screen.getByRole('button', { name: /Import backup/i }));

      await waitFor(() => {
        expect(screen.getByRole('table', { name: /Import summary/i })).toBeInTheDocument();
      });
    });
  });
});
