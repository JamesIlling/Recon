import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { ThumbnailImage } from './ThumbnailImage';

/**
 * UserMenu component displayed in the top-right corner of authenticated pages.
 * Provides access to user profile, settings, and sign-out functionality.
 * 
 * Features:
 * - Displays user avatar (ThumbnailVariant) in circular crop
 * - Shows user's display name
 * - Provides link to settings page (/settings)
 * - Provides sign-out button that clears JWT and redirects to /login
 * - Handles loading and error states gracefully
 * - Keyboard accessible and semantically correct
 */
export const UserMenu: React.FC = () => {
  const { user, logout, isLoading } = useAuth();
  const navigate = useNavigate();
  const [isMenuOpen, setIsMenuOpen] = useState(false);

  if (isLoading) {
    return (
      <div className="user-menu user-menu--loading" aria-label="Loading user menu">
        <div className="user-menu__skeleton" />
      </div>
    );
  }

  if (!user) {
    return null;
  }

  const handleSignOut = () => {
    logout();
    navigate('/login');
  };

  const handleSettingsClick = () => {
    setIsMenuOpen(false);
    navigate('/settings');
  };

  const handleMenuToggle = () => {
    setIsMenuOpen(!isMenuOpen);
  };

  const handleMenuClose = () => {
    setIsMenuOpen(false);
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Escape') {
      handleMenuClose();
    }
  };

  // Build avatar URL from image ID if available
  const avatarUrl = user.avatarImageId
    ? `/api/images/${user.avatarImageId}/thumbnail`
    : undefined;

  return (
    <div className="user-menu" onKeyDown={handleKeyDown}>
      <button
        className="user-menu__trigger"
        onClick={handleMenuToggle}
        aria-expanded={isMenuOpen}
        aria-haspopup="menu"
        aria-label={`User menu for ${user.displayName}`}
        title={user.displayName}
      >
        <ThumbnailImage
          thumbnailUrl={avatarUrl}
          altText={`${user.displayName}'s avatar`}
          fallbackText="U"
          className="user-menu__avatar"
          width={40}
          height={40}
        />
        <span className="user-menu__display-name">{user.displayName}</span>
      </button>

      {isMenuOpen && (
        <nav
          className="user-menu__dropdown"
          role="menu"
          aria-label="User menu options"
        >
          <button
            className="user-menu__item"
            onClick={handleSettingsClick}
            role="menuitem"
            aria-label="Go to settings"
          >
            Settings
          </button>
          <hr className="user-menu__divider" />
          <button
            className="user-menu__item user-menu__item--danger"
            onClick={handleSignOut}
            role="menuitem"
            aria-label="Sign out"
          >
            Sign Out
          </button>
        </nav>
      )}
    </div>
  );
};

export default UserMenu;
