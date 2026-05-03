import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import './UserConfigurationPage.css';

/**
 * DisplayNameEditor component - inline edit with save/cancel and uniqueness error.
 */
function DisplayNameEditor(): JSX.Element {
  const { user, token, refreshProfile } = useAuth();
  const [isEditing, setIsEditing] = useState(false);
  const [displayName, setDisplayName] = useState(user?.displayName || '');
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    setDisplayName(user?.displayName || '');
  }, [user?.displayName]);

  const handleSave = async () => {
    setError('');

    if (!displayName.trim()) {
      setError('Display name cannot be empty');
      return;
    }

    if (displayName.length > 100) {
      setError('Display name must not exceed 100 characters');
      return;
    }

    if (displayName === user?.displayName) {
      setIsEditing(false);
      return;
    }

    setIsLoading(true);
    try {
      const response = await fetch('/api/users/me/display-name', {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({ displayName }),
      });

      if (!response.ok) {
        const errorData = await response.json();
        if (response.status === 409) {
          setError('Display name already exists');
        } else {
          setError(errorData.message || 'Failed to update display name');
        }
        return;
      }

      await refreshProfile();
      setIsEditing(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update display name');
    } finally {
      setIsLoading(false);
    }
  };

  const handleCancel = () => {
    setDisplayName(user?.displayName || '');
    setError('');
    setIsEditing(false);
  };

  return (
    <div className="settings-section">
      <h2>Display Name</h2>
      {isEditing ? (
        <div className="display-name-editor">
          <input
            type="text"
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
            maxLength={100}
            disabled={isLoading}
            aria-label="Display name"
            aria-describedby={error ? 'display-name-error' : undefined}
          />
          {error && (
            <div id="display-name-error" className="error-message" role="alert">
              {error}
            </div>
          )}
          <div className="button-group">
            <button onClick={handleSave} disabled={isLoading}>
              {isLoading ? 'Saving...' : 'Save'}
            </button>
            <button onClick={handleCancel} disabled={isLoading} className="secondary">
              Cancel
            </button>
          </div>
        </div>
      ) : (
        <div className="display-name-view">
          <p>{user?.displayName}</p>
          <button onClick={() => setIsEditing(true)} className="secondary">
            Edit
          </button>
        </div>
      )}
    </div>
  );
}

/**
 * AvatarUploader component - file picker, 1:1 crop tool, optional altText input, preview.
 */
function AvatarUploader(): JSX.Element {
  const { user, token, refreshProfile } = useAuth();
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [altText, setAltText] = useState('');
  const [isUploading, setIsUploading] = useState(false);
  const [error, setError] = useState('');
  const [cropMode, setCropMode] = useState(false);
  const [cropData, setCropData] = useState({ x: 0, y: 0, size: 200 });
  const canvasRef = React.useRef<HTMLCanvasElement>(null);

  const handleFileSelect = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    setError('');

    const validMimes = ['image/jpeg', 'image/png', 'image/webp'];
    if (!validMimes.includes(file.type)) {
      setError('Invalid image format. Please use JPEG, PNG, or WebP.');
      return;
    }

    if (file.size > 1024 * 1024) {
      setError('File size exceeds 1 MB limit.');
      return;
    }

    setSelectedFile(file);
    const reader = new FileReader();
    reader.onload = (e) => {
      setPreviewUrl(e.target?.result as string);
      setCropMode(true);
    };
    reader.readAsDataURL(file);
  };

  const handleCropChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setCropData((prev) => ({
      ...prev,
      [name]: parseInt(value, 10),
    }));
  };

  const applyCrop = async () => {
    if (!previewUrl || !canvasRef.current) return;

    const canvas = canvasRef.current;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const img = new Image();
    img.onload = () => {
      const size = Math.min(cropData.size, img.width - cropData.x, img.height - cropData.y);
      canvas.width = size;
      canvas.height = size;

      ctx.drawImage(
        img,
        cropData.x,
        cropData.y,
        size,
        size,
        0,
        0,
        size,
        size
      );

      canvas.toBlob((blob: Blob | null) => {
        if (blob) {
          const croppedFile = new File([blob], selectedFile?.name || 'avatar.jpg', {
            type: 'image/jpeg',
          });
          setSelectedFile(croppedFile);
          setCropMode(false);
        }
      }, 'image/jpeg');
    };
    img.src = previewUrl;
  };

  const handleUpload = async () => {
    if (!selectedFile) {
      setError('Please select an image.');
      return;
    }

    setIsUploading(true);
    setError('');

    try {
      const formData = new FormData();
      formData.append('file', selectedFile);
      if (altText) {
        formData.append('altText', altText);
      }

      const response = await fetch('/api/users/me/avatar', {
        method: 'PUT',
        headers: {
          Authorization: `Bearer ${token}`,
        },
        body: formData,
      });

      if (!response.ok) {
        const errorData = await response.json();
        setError(errorData.message || 'Failed to upload avatar');
        return;
      }

      await refreshProfile();
      setSelectedFile(null);
      setPreviewUrl(null);
      setAltText('');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to upload avatar');
    } finally {
      setIsUploading(false);
    }
  };

  const currentAvatarUrl = user?.avatarImageId
    ? `/api/images/${user.avatarImageId}/thumbnail`
    : null;

  return (
    <div className="settings-section">
      <h2>Avatar</h2>
      <div className="avatar-uploader">
        {currentAvatarUrl && (
          <div className="current-avatar">
            <img src={currentAvatarUrl} alt="Current avatar" />
            <p>Current avatar</p>
          </div>
        )}

        <div className="upload-controls">
          <input
            type="file"
            accept="image/jpeg,image/png,image/webp"
            onChange={handleFileSelect}
            disabled={isUploading}
            aria-label="Select avatar image"
          />

          {cropMode && previewUrl && (
            <div className="crop-section">
              <img src={previewUrl} alt="Crop preview" id="avatar-crop-image" />
              <div className="crop-controls">
                <label>
                  X Position:
                  <input
                    type="number"
                    name="x"
                    value={cropData.x}
                    onChange={handleCropChange}
                    min="0"
                    disabled={isUploading}
                  />
                </label>
                <label>
                  Y Position:
                  <input
                    type="number"
                    name="y"
                    value={cropData.y}
                    onChange={handleCropChange}
                    min="0"
                    disabled={isUploading}
                  />
                </label>
                <label>
                  Size (1:1):
                  <input
                    type="number"
                    name="size"
                    value={cropData.size}
                    onChange={handleCropChange}
                    min="1"
                    disabled={isUploading}
                  />
                </label>
                <button onClick={applyCrop} disabled={isUploading}>
                  Apply Crop
                </button>
              </div>
              <canvas ref={canvasRef} style={{ display: 'none' }} />
            </div>
          )}

          {selectedFile && !cropMode && (
            <div className="alt-text-section">
              <label htmlFor="avatar-alt-text">
                Image Description (optional, max 200 characters):
                <textarea
                  id="avatar-alt-text"
                  value={altText}
                  onChange={(e) => setAltText(e.target.value.slice(0, 200))}
                  placeholder="Describe your avatar for accessibility"
                  disabled={isUploading}
                  maxLength={200}
                />
              </label>
              <p className="char-count">{altText.length}/200</p>
            </div>
          )}

          {error && (
            <div className="error-message" role="alert">
              {error}
            </div>
          )}

          {selectedFile && !cropMode && (
            <button onClick={handleUpload} disabled={isUploading}>
              {isUploading ? 'Uploading...' : 'Upload Avatar'}
            </button>
          )}
        </div>
      </div>
    </div>
  );
}

/**
 * PasswordChangeForm component - current + new + confirm password fields with validation.
 */
function PasswordChangeForm(): JSX.Element {
  const { token } = useAuth();
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [passwordErrors, setPasswordErrors] = useState<string[]>([]);

  const validatePasswordComplexity = (password: string): string[] => {
    const errors: string[] = [];
    if (password.length < 8) {
      errors.push('Password must be at least 8 characters long');
    }
    if (!/[A-Z]/.test(password)) {
      errors.push('Password must contain at least one uppercase letter');
    }
    if (!/[a-z]/.test(password)) {
      errors.push('Password must contain at least one lowercase letter');
    }
    if (!/\d/.test(password)) {
      errors.push('Password must contain at least one digit');
    }
    return errors;
  };

  const handleNewPasswordChange = (value: string) => {
    setNewPassword(value);
    setPasswordErrors(validatePasswordComplexity(value));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setSuccess('');

    if (!currentPassword) {
      setError('Current password is required');
      return;
    }

    const validation = validatePasswordComplexity(newPassword);
    if (validation.length > 0) {
      setPasswordErrors(validation);
      return;
    }

    if (newPassword !== confirmPassword) {
      setError('New passwords do not match');
      return;
    }

    setIsLoading(true);
    try {
      const response = await fetch('/api/users/me/password', {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({
          currentPassword,
          newPassword,
        }),
      });

      if (!response.ok) {
        const errorData = await response.json();
        if (response.status === 401) {
          setError('Current password is incorrect');
        } else {
          setError(errorData.message || 'Failed to change password');
        }
        return;
      }

      setSuccess('Password changed successfully');
      setCurrentPassword('');
      setNewPassword('');
      setConfirmPassword('');
      setPasswordErrors([]);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to change password');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="settings-section">
      <h2>Change Password</h2>
      <form onSubmit={handleSubmit} className="password-form">
        {error && (
          <div className="error-message" role="alert">
            {error}
          </div>
        )}
        {success && (
          <div className="success-message" role="status">
            {success}
          </div>
        )}

        <div className="form-group">
          <label htmlFor="current-password">Current Password</label>
          <input
            id="current-password"
            type="password"
            value={currentPassword}
            onChange={(e) => setCurrentPassword(e.target.value)}
            required
            disabled={isLoading}
            aria-describedby={error ? 'password-error' : undefined}
          />
        </div>

        <div className="form-group">
          <label htmlFor="new-password">New Password</label>
          <input
            id="new-password"
            type="password"
            value={newPassword}
            onChange={(e) => handleNewPasswordChange(e.target.value)}
            required
            disabled={isLoading}
            aria-describedby={passwordErrors.length > 0 ? 'password-requirements' : undefined}
          />
          {passwordErrors.length > 0 && (
            <div id="password-requirements" className="password-errors" role="alert">
              <p>Password must:</p>
              <ul>
                {passwordErrors.map((err, idx) => (
                  <li key={idx}>{err}</li>
                ))}
              </ul>
            </div>
          )}
        </div>

        <div className="form-group">
          <label htmlFor="confirm-password">Confirm New Password</label>
          <input
            id="confirm-password"
            type="password"
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
            required
            disabled={isLoading}
          />
          {newPassword && confirmPassword && newPassword !== confirmPassword && (
            <div className="error-message" role="alert">
              Passwords do not match
            </div>
          )}
        </div>

        <button
          type="submit"
          disabled={isLoading || passwordErrors.length > 0 || newPassword !== confirmPassword}
        >
          {isLoading ? 'Changing...' : 'Change Password'}
        </button>
      </form>
    </div>
  );
}

/**
 * PreferencesToggle component - ShowPublicCollections toggle with immediate persistence.
 */
function PreferencesToggle(): JSX.Element {
  const { user, token, refreshProfile } = useAuth();
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');

  const handleToggle = async () => {
    if (!user) return;

    setError('');
    setIsLoading(true);

    try {
      const response = await fetch('/api/users/me/preferences', {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({
          showPublicCollections: !user.showPublicCollections,
        }),
      });

      if (!response.ok) {
        const errorData = await response.json();
        setError(errorData.message || 'Failed to update preferences');
        return;
      }

      await refreshProfile();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update preferences');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="settings-section">
      <h2>Preferences</h2>
      <div className="preferences-toggle">
        <label htmlFor="show-public-collections">
          <input
            id="show-public-collections"
            type="checkbox"
            checked={user?.showPublicCollections ?? true}
            onChange={handleToggle}
            disabled={isLoading}
            aria-describedby={error ? 'preferences-error' : undefined}
          />
          Show public collections on homepage
        </label>
        {error && (
          <div id="preferences-error" className="error-message" role="alert">
            {error}
          </div>
        )}
      </div>
    </div>
  );
}

/**
 * UserConfigurationPage component - main settings page with all sub-components.
 * Redirects unauthenticated users to /login.
 */
export function UserConfigurationPage(): JSX.Element {
  const { token } = useAuth();
  const navigate = useNavigate();

  // Redirect unauthenticated users to /login
  if (!token) {
    navigate('/login');
    return <div>Redirecting to login...</div>;
  }

  return (
    <div className="user-configuration-page">
      <div className="settings-container">
        <h1>Settings</h1>

        <DisplayNameEditor />
        <AvatarUploader />
        <PasswordChangeForm />
        <PreferencesToggle />
      </div>
    </div>
  );
}

export default UserConfigurationPage;
