import React, { useState, useEffect, useCallback, useRef } from 'react';
import { useAuth } from '../contexts/AuthContext';

/**
 * Notification data structure from the API.
 */
export interface Notification {
  id: string;
  type: string;
  message: string;
  relatedResourceId?: string;
  isRead: boolean;
  createdAt: string;
}

/**
 * NotificationPanel component that displays in-app notifications.
 * 
 * Features:
 * - Displays unread notification count badge in UserMenu
 * - Shows unread notifications in a dropdown panel
 * - Polls GET /api/notifications every 30 seconds when panel is open
 * - Mark as read / delete actions for each notification
 * - Proper ARIA labels and semantic HTML for accessibility
 * - aria-live="polite" for screen reader announcements
 */
export const NotificationPanel: React.FC = () => {
  const { token } = useAuth();
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [isOpen, setIsOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const pollIntervalRef = useRef<NodeJS.Timeout | null>(null);

  /**
   * Fetches notifications from the API.
   */
  const fetchNotifications = useCallback(async () => {
    if (!token) {
      return;
    }

    try {
      setIsLoading(true);
      setError(null);

      const response = await fetch('/api/notifications', {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      if (!response.ok) {
        if (response.status === 401) {
          // Token is invalid, stop polling
          setNotifications([]);
          return;
        }
        throw new Error('Failed to fetch notifications');
      }

      const data = await response.json();
      setNotifications(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
      console.error('Error fetching notifications:', err);
    } finally {
      setIsLoading(false);
    }
  }, [token]);

  /**
   * Sets up polling when panel is open.
   */
  useEffect(() => {
    if (isOpen && token) {
      // Fetch immediately
      fetchNotifications();

      // Set up polling every 30 seconds
      pollIntervalRef.current = setInterval(() => {
        fetchNotifications();
      }, 30000);

      return () => {
        if (pollIntervalRef.current) {
          clearInterval(pollIntervalRef.current);
          pollIntervalRef.current = null;
        }
      };
    }
  }, [isOpen, token, fetchNotifications]);

  /**
   * Marks a notification as read.
   */
  const handleMarkAsRead = useCallback(
    async (notificationId: string) => {
      if (!token) {
        return;
      }

      try {
        const response = await fetch(`/api/notifications/${notificationId}/read`, {
          method: 'PUT',
          headers: {
            Authorization: `Bearer ${token}`,
          },
        });

        if (response.ok) {
          // Update local state
          setNotifications((prev) =>
            prev.map((n) =>
              n.id === notificationId ? { ...n, isRead: true } : n
            )
          );
        }
      } catch (err) {
        console.error('Error marking notification as read:', err);
      }
    },
    [token]
  );

  /**
   * Deletes a notification.
   */
  const handleDeleteNotification = useCallback(
    async (notificationId: string) => {
      if (!token) {
        return;
      }

      try {
        const response = await fetch(`/api/notifications/${notificationId}`, {
          method: 'DELETE',
          headers: {
            Authorization: `Bearer ${token}`,
          },
        });

        if (response.ok) {
          // Update local state
          setNotifications((prev) =>
            prev.filter((n) => n.id !== notificationId)
          );
        }
      } catch (err) {
        console.error('Error deleting notification:', err);
      }
    },
    [token]
  );

  /**
   * Closes the panel when Escape key is pressed.
   */
  const handleKeyDown = useCallback((e: React.KeyboardEvent) => {
    if (e.key === 'Escape') {
      setIsOpen(false);
    }
  }, []);

  /**
   * Closes the panel when clicking outside.
   */
  const handlePanelClick = useCallback((e: React.MouseEvent) => {
    e.stopPropagation();
  }, []);

  const unreadCount = notifications.filter((n) => !n.isRead).length;

  return (
    <div className="notification-panel" onKeyDown={handleKeyDown}>
      <button
        className="notification-panel__trigger"
        onClick={() => setIsOpen(!isOpen)}
        aria-expanded={isOpen}
        aria-haspopup="dialog"
        aria-label={`Notifications, ${unreadCount} unread`}
        title={`${unreadCount} unread notifications`}
      >
        <span className="notification-panel__icon" aria-hidden="true">
          🔔
        </span>
        {unreadCount > 0 && (
          <span
            className="notification-panel__badge"
            aria-live="polite"
            aria-atomic="true"
          >
            {unreadCount}
          </span>
        )}
      </button>

      {isOpen && (
        <div
          className="notification-panel__dropdown"
          role="dialog"
          aria-label="Notifications"
          onClick={handlePanelClick}
        >
          <div className="notification-panel__header">
            <h2 className="notification-panel__title">Notifications</h2>
            <button
              className="notification-panel__close"
              onClick={() => setIsOpen(false)}
              aria-label="Close notifications panel"
              title="Close"
            >
              ✕
            </button>
          </div>

          <div className="notification-panel__content">
            {isLoading && (
              <div
                className="notification-panel__loading"
                aria-busy="true"
                aria-label="Loading notifications"
              >
                Loading...
              </div>
            )}

            {error && (
              <div
                className="notification-panel__error"
                role="alert"
                aria-live="assertive"
              >
                {error}
              </div>
            )}

            {!isLoading && !error && notifications.length === 0 && (
              <div className="notification-panel__empty">
                No notifications
              </div>
            )}

            {!isLoading && !error && notifications.length > 0 && (
              <ul className="notification-panel__list" role="list">
                {notifications.map((notification) => (
                  <li
                    key={notification.id}
                    className={`notification-panel__item ${
                      !notification.isRead
                        ? 'notification-panel__item--unread'
                        : ''
                    }`}
                    role="listitem"
                  >
                    <div className="notification-panel__item-content">
                      <p className="notification-panel__item-message">
                        {notification.message}
                      </p>
                      <time
                        className="notification-panel__item-time"
                        dateTime={notification.createdAt}
                      >
                        {new Date(notification.createdAt).toLocaleString()}
                      </time>
                    </div>

                    <div className="notification-panel__item-actions">
                      {!notification.isRead && (
                        <button
                          className="notification-panel__action-button"
                          onClick={() => handleMarkAsRead(notification.id)}
                          aria-label={`Mark "${notification.message}" as read`}
                          title="Mark as read"
                        >
                          ✓
                        </button>
                      )}
                      <button
                        className="notification-panel__action-button notification-panel__action-button--delete"
                        onClick={() => handleDeleteNotification(notification.id)}
                        aria-label={`Delete "${notification.message}"`}
                        title="Delete"
                      >
                        🗑
                      </button>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </div>
      )}
    </div>
  );
};

export default NotificationPanel;
