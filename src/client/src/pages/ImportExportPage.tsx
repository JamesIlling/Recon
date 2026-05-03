import React, { useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import './ImportExportPage.css';

/**
 * Summary returned by POST /api/admin/import.
 */
interface ImportResult {
  importUserId: string;
  usersImported: number;
  usersSkipped: number;
  locationsImported: number;
  locationsSkipped: number;
  collectionsImported: number;
  collectionsSkipped: number;
  membersImported: number;
  membersSkipped: number;
  namedShapesImported: number;
  namedShapesSkipped: number;
  imagesImported: number;
  imagesSkipped: number;
  warnings: string[];
}

const MIN_KEY_LENGTH = 32;

/**
 * ImportExportPage provides admin-only controls for:
 * - Exporting all application data as an AES-256 encrypted ZIP archive.
 * - Importing data from a previously exported archive.
 *
 * Implements task 15.3 with WCAG 2.1 Level AA accessibility compliance.
 */
export function ImportExportPage(): JSX.Element {
  const navigate = useNavigate();
  const { token, user, isLoading: authLoading } = useAuth();

  // Export form state
  const [exportKey, setExportKey] = useState('');
  const [exportKeyVisible, setExportKeyVisible] = useState(false);
  const [exportLoading, setExportLoading] = useState(false);
  const [exportError, setExportError] = useState('');

  // Import form state
  const [importKey, setImportKey] = useState('');
  const [importKeyVisible, setImportKeyVisible] = useState(false);
  const [importFile, setImportFile] = useState<File | null>(null);
  const [importLoading, setImportLoading] = useState(false);
  const [importError, setImportError] = useState('');
  const [importResult, setImportResult] = useState<ImportResult | null>(null);

  const fileInputRef = useRef<HTMLInputElement>(null);

  // Redirect unauthenticated or non-admin users
  React.useEffect(() => {
    if (!authLoading && !token) {
      navigate('/login', { replace: true });
      return;
    }
    if (!authLoading && user && user.role !== 'Admin') {
      navigate('/', { replace: true });
    }
  }, [token, user, authLoading, navigate]);

  // -------------------------------------------------------------------------
  // Validation helper
  // -------------------------------------------------------------------------

  const validateKey = (key: string, fieldLabel: string): string => {
    if (!key.trim()) return `${fieldLabel} is required.`;
    if (key.length < MIN_KEY_LENGTH) {
      return `${fieldLabel} must be at least ${MIN_KEY_LENGTH} characters.`;
    }
    return '';
  };

  // -------------------------------------------------------------------------
  // Export handler
  // -------------------------------------------------------------------------

  const handleExport = async (e: React.FormEvent) => {
    e.preventDefault();
    setExportError('');

    const keyError = validateKey(exportKey, 'Encryption key');
    if (keyError) {
      setExportError(keyError);
      return;
    }

    setExportLoading(true);
    try {
      const response = await fetch('/api/admin/export', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({ encryptionKey: exportKey }),
      });

      if (!response.ok) {
        const data = await response.json().catch(() => ({}));
        throw new Error((data as { error?: string }).error ?? `Export failed (${response.status})`);
      }

      // Trigger browser download
      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      const disposition = response.headers.get('Content-Disposition') ?? '';
      const match = disposition.match(/filename="?([^"]+)"?/);
      anchor.download = match?.[1] ?? 'backup.enc.zip';
      anchor.href = url;
      anchor.click();
      URL.revokeObjectURL(url);
    } catch (err) {
      setExportError(err instanceof Error ? err.message : 'An unexpected error occurred.');
    } finally {
      setExportLoading(false);
    }
  };

  // -------------------------------------------------------------------------
  // Import handlers
  // -------------------------------------------------------------------------

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0] ?? null;
    setImportFile(file);
    setImportError('');
    setImportResult(null);
  };

  const handleImport = async (e: React.FormEvent) => {
    e.preventDefault();
    setImportError('');
    setImportResult(null);

    const keyError = validateKey(importKey, 'Decryption key');
    if (keyError) {
      setImportError(keyError);
      return;
    }

    if (!importFile) {
      setImportError('Please select a backup file.');
      return;
    }

    setImportLoading(true);
    try {
      const formData = new FormData();
      formData.append('file', importFile);
      formData.append('decryptionKey', importKey);

      const response = await fetch('/api/admin/import', {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${token}`,
        },
        body: formData,
      });

      if (!response.ok) {
        const data = await response.json().catch(() => ({}));
        throw new Error((data as { error?: string }).error ?? `Import failed (${response.status})`);
      }

      const result: ImportResult = await response.json();
      setImportResult(result);

      // Reset file input
      setImportFile(null);
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    } catch (err) {
      setImportError(err instanceof Error ? err.message : 'An unexpected error occurred.');
    } finally {
      setImportLoading(false);
    }
  };

  // -------------------------------------------------------------------------
  // Render
  // -------------------------------------------------------------------------

  if (authLoading) {
    return (
      <div className="import-export-page">
        <div className="loading-skeleton" aria-busy="true" aria-label="Loading">
          <div className="skeleton-row" />
          <div className="skeleton-row" />
        </div>
      </div>
    );
  }

  return (
    <div className="import-export-page">
      <div className="page-header">
        <h1>Data Export &amp; Import</h1>
        <p className="page-subtitle">
          Export all application data as an encrypted archive, or restore data from a
          previously exported archive.
        </p>
      </div>

      {/* ------------------------------------------------------------------ */}
      {/* Export section                                                       */}
      {/* ------------------------------------------------------------------ */}
      <section className="card" aria-labelledby="export-heading">
        <h2 id="export-heading">Export Data</h2>
        <p className="section-description">
          Downloads all users, locations, collections, and images as an AES-256
          encrypted ZIP file. Keep the encryption key safe — it is required to
          restore the archive.
        </p>

        <form onSubmit={handleExport} noValidate aria-label="Export form">
          <div className="form-group">
            <label htmlFor="export-key">
              Encryption key
              <span className="required-indicator" aria-hidden="true"> *</span>
            </label>
            <div className="password-input-wrapper">
              <input
                id="export-key"
                type={exportKeyVisible ? 'text' : 'password'}
                value={exportKey}
                onChange={(e) => setExportKey(e.target.value)}
                autoComplete="new-password"
                aria-required="true"
                aria-describedby={exportError ? 'export-key-error' : 'export-key-hint'}
                aria-invalid={exportError ? 'true' : 'false'}
                disabled={exportLoading}
                placeholder={`Minimum ${MIN_KEY_LENGTH} characters`}
              />
              <button
                type="button"
                className="btn-toggle-visibility"
                onClick={() => setExportKeyVisible((v) => !v)}
                aria-label={exportKeyVisible ? 'Hide encryption key' : 'Show encryption key'}
              >
                {exportKeyVisible ? '🙈' : '👁'}
              </button>
            </div>
            <span id="export-key-hint" className="field-hint">
              Minimum {MIN_KEY_LENGTH} characters. Store this passphrase securely — it cannot be
              recovered.
            </span>
            {exportError && (
              <span
                id="export-key-error"
                className="field-error"
                role="alert"
                aria-live="polite"
              >
                {exportError}
              </span>
            )}
          </div>

          <button
            type="submit"
            className="btn-primary"
            disabled={exportLoading}
            aria-busy={exportLoading}
          >
            {exportLoading ? 'Exporting…' : 'Download encrypted backup'}
          </button>
        </form>
      </section>

      {/* ------------------------------------------------------------------ */}
      {/* Import section                                                       */}
      {/* ------------------------------------------------------------------ */}
      <section className="card" aria-labelledby="import-heading">
        <h2 id="import-heading">Import Data</h2>
        <p className="section-description">
          Restore data from an encrypted backup archive. Existing records are
          preserved — import is additive and assigns new IDs to avoid conflicts.
        </p>

        <form onSubmit={handleImport} noValidate aria-label="Import form">
          <div className="form-group">
            <label htmlFor="import-file">
              Backup file
              <span className="required-indicator" aria-hidden="true"> *</span>
            </label>
            <input
              id="import-file"
              ref={fileInputRef}
              type="file"
              accept=".zip,.enc.zip"
              onChange={handleFileChange}
              aria-required="true"
              aria-describedby="import-file-hint"
              disabled={importLoading}
            />
            <span id="import-file-hint" className="field-hint">
              Select the encrypted backup archive (.zip).
            </span>
            {importFile && (
              <span className="selected-file" aria-live="polite">
                Selected: {importFile.name} ({(importFile.size / 1024).toFixed(1)} KB)
              </span>
            )}
          </div>

          <div className="form-group">
            <label htmlFor="import-key">
              Decryption key
              <span className="required-indicator" aria-hidden="true"> *</span>
            </label>
            <div className="password-input-wrapper">
              <input
                id="import-key"
                type={importKeyVisible ? 'text' : 'password'}
                value={importKey}
                onChange={(e) => setImportKey(e.target.value)}
                autoComplete="current-password"
                aria-required="true"
                aria-describedby={importError ? 'import-key-error' : 'import-key-hint'}
                aria-invalid={importError ? 'true' : 'false'}
                disabled={importLoading}
                placeholder={`Minimum ${MIN_KEY_LENGTH} characters`}
              />
              <button
                type="button"
                className="btn-toggle-visibility"
                onClick={() => setImportKeyVisible((v) => !v)}
                aria-label={importKeyVisible ? 'Hide decryption key' : 'Show decryption key'}
              >
                {importKeyVisible ? '🙈' : '👁'}
              </button>
            </div>
            <span id="import-key-hint" className="field-hint">
              The passphrase used when the backup was created.
            </span>
            {importError && (
              <span
                id="import-key-error"
                className="field-error"
                role="alert"
                aria-live="polite"
              >
                {importError}
              </span>
            )}
          </div>

          <button
            type="submit"
            className="btn-primary"
            disabled={importLoading}
            aria-busy={importLoading}
          >
            {importLoading ? 'Importing…' : 'Import backup'}
          </button>
        </form>

        {/* Import result summary */}
        {importResult && (
          <div
            className="import-result"
            role="region"
            aria-labelledby="import-result-heading"
            aria-live="polite"
          >
            <h3 id="import-result-heading">Import complete</h3>
            <table className="result-table" aria-label="Import summary">
              <thead>
                <tr>
                  <th scope="col">Resource</th>
                  <th scope="col">Imported</th>
                  <th scope="col">Skipped</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td>Users</td>
                  <td>{importResult.usersImported}</td>
                  <td>{importResult.usersSkipped}</td>
                </tr>
                <tr>
                  <td>Locations</td>
                  <td>{importResult.locationsImported}</td>
                  <td>{importResult.locationsSkipped}</td>
                </tr>
                <tr>
                  <td>Collections</td>
                  <td>{importResult.collectionsImported}</td>
                  <td>{importResult.collectionsSkipped}</td>
                </tr>
                <tr>
                  <td>Members</td>
                  <td>{importResult.membersImported}</td>
                  <td>{importResult.membersSkipped}</td>
                </tr>
                <tr>
                  <td>Named shapes</td>
                  <td>{importResult.namedShapesImported}</td>
                  <td>{importResult.namedShapesSkipped}</td>
                </tr>
                <tr>
                  <td>Images</td>
                  <td>{importResult.imagesImported}</td>
                  <td>{importResult.imagesSkipped}</td>
                </tr>
              </tbody>
            </table>

            {importResult.warnings.length > 0 && (
              <div className="import-warnings" role="alert">
                <h4>Warnings ({importResult.warnings.length})</h4>
                <ul>
                  {importResult.warnings.map((w, i) => (
                    <li key={i}>{w}</li>
                  ))}
                </ul>
              </div>
            )}
          </div>
        )}
      </section>
    </div>
  );
}

export default ImportExportPage;
