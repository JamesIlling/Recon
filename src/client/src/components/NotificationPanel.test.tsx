import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '../test/test-utils';
import { NotificationPanel, Notification } from './NotificationPanel';
import { mockDataGenerators } from '../test/mocks';

/**
 * Mock useAuth hook for testing
 */
vi.mock('../contexts/AuthContext', async () => {
  const actual = await vi.importActual('../contexts/AuthContext');
  return {
    ...actual,
    useAuth: vi.fn(),
  };
});

import { useAuth } from '../contexts/AuthContext';

const mockNotification = (overrides: Partial<Notification> = {}): Notification => ({
  id: 'notif-123',
  type: 'PendingEditSubmitted',
  message: 'A new edit has been submitted',
  relatedResourceId: 'location-123',
  isRead: false,
  createdAt: new Date().toISOString(),
  ...overrides,
});

const renderNotificationPanel = () => {
  return render(<NotificationPanel />);
};

describe('NotificationPanel', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    global.fetch = vi.fn();
  });

  afterEach(() => {
    vi.clearAllTimers();
  });

  describe('Trigger Button and Badge', () => {
    it('should render trigger button with notification icon', () => {
      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        token: 'test-token',
        user: mockDataGenerators.user(),
        isLoading: false,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshProfile: vi.fn(),
      });

      (global.fetch as any).mockResolvedValue({
        ok: true,
        json: vi.fn().mockResolvedValue([]),
      });

      renderNotificationPanel();

      const trigger = screen.getByLabelText(/Notifications/);
      expect(trigger).toBeInTheDocument();
      expect(trigger).toHaveAttribute('aria-haspopup', 'dialog');
    });

    it('should display unread count badge when there are unread notifications', async () => {
      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        token: 'test-token',
        user: mockDataGenerators.user(),
        isLoading: false,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshProfile: vi.fn(),
      });

      const notifications = [
        mockNotification({ id: 'notif-1', isRead: false }),
        mockNotification({ id: 'notif-2', isRead: false }),
        mockNotification({ id: 'notif-3', isRead: true }),
      ];

      (global.fetch as any).mockResolvedValue({
        ok: true,
        json: vi.fn().mockResolvedValue(notifications),
      });

      renderNotificationPanel();

      // Open panel to trigger fetch
      const trigger = screen.getByLabelText(/Notifications/);
      fireEvent.click(trigger);

      await waitFor(() => {
        const badge = screen.getByText('2');
        expect(badge).toBeInTheDocument();
        expect(badge).toHaveClass('notification-panel__badge');
      });
    });

    it('should not display badge when all notifications are read', async () => {
      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        token: 'test-token',
        user: mockDataGenerators.user(),
        isLoading: false,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshProfile: vi.fn(),
      });

      const notifications = [
        mockNotification({ id: 'notif-1', isRead: true }),
        mockNotification({ id: 'notif-2', isRead: true }),
      ];

      (global.fetch as any).mockResolvedValue({
        ok: true,
        json: vi.fn().mockResolvedValue(notifications),
      });

      renderNotificationPanel();

      const trigger = screen.getByLabelText(/Notifications/);
      fireEvent.click(trigger);

      await waitFor(() => {
        const badge = screen.queryByText('0');
        expect(badge).not.toBeInTheDocument();
      });
    });

    it('should have aria-live="polite" on badge for screen reader announcements', async () => {
      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        token: 'test-token',
        user: mockDataGenerators.user(),
        isLoading: false,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshProfile: vi.fn(),
      });

      const notifications = [
        mockNotification({ id: 'notif-1', isRead: false }),
      ];

      (global.fetch as any).mockResolvedValue({
        ok: true,
        json: vi.fn().mockResolvedValue(notifications),
      });

      renderNotificationPanel();

      const trigger = screen.getByLabelText(/Notifications/);
      fireEvent.click(trigger);

      await waitFor(() => {
        const badge = screen.getByText('1');
        expect(badge).toHaveAttribute('aria-live', 'polite');
        expect(badge).toHaveAttribute('aria-atomic', 'true');
      });
    });
  });

  describe('Panel Opening and Closing', () => {
    it('should toggle panel open/closed on button click', async () => {
      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        token: 'test-token',
        user: mockDataGenerators.user(),
        isLoading: false,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshProfile: vi.fn(),
      });

      (global.fetch as any).mockResolvedValue({
        ok: true,
        json: vi.fn().mockResolvedValue([]),
      });

      renderNotificationPanel();

      const trigger = screen.getByLabelText(/Notifications/);

      // Initially closed
      expect(trigger).toHaveAttribute('aria-expanded', 'false');

      // Open panel
      fireEvent.click(trigger);
      expect(trigger).toHaveAttribute('aria-expanded', 'true');

      // Close panel
      fireEvent.click(trigger);
      expect(trigger).toHaveAttribute('aria-expanded', 'false');
    });

    it('should close panel when Escape key is pressed', async () => {
      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        token: 'test-token',
        user: mockDataGenerators.user(),
        isLoading: false,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshProfile: vi.fn(),
      });

      (global.fetch as any).mockResolvedValue({
        ok: true,
        json: vi.fn().mockResolvedValue([]),
      });

      renderNotificationPanel();

      const trigger = screen.getByLabelText(/Notifications/);

      // Open panel
      fireEvent.click(trigger);
      expect(trigger).toHaveAttribute('aria-expanded', 'true');

      // Press Escape
      fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' });

      expect(trigger).toHaveAttribute('aria-expanded', 'false');
    });

    it('should close panel when close button is clicked', async () => {
      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        token: 'test-token',
        user: mockDataGenerators.user(),
        isLoading: false,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshProfile: vi.fn(),
      });

      (global.fetch as any).mockResolvedValue({
        ok: true,
        json: vi.fn().mockResolvedValue([]),
      });

      renderNotificationPanel();

      const trigger = screen.getByLabelText(/Notifications/);

      // Open panel
      fireEvent.click(trigger);

      // Click close button
      const closeButton = screen.getByLabelText('Close notifications panel');
      fireEvent.click(closeButton);

      expect(trigger).toHaveAttribute('aria-expanded', 'false');
    });
  });

  describe('Notification Fetching and Polling', () => {
    it('should fetch notifications when panel is opened', async () => {
      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        token: 'test-token',
        user: mockDataGenerators.user(),
        isLoading: false,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshProfile: vi.fn(),
      });

      const notifications = [mockNotification()];

      (global.fetch as any).mockResolvedValue({
        ok: true,
        json: vi.fn().mockResolvedValue(notifications),
      });

      renderNotificationPanel();

      const trigger = screen.getByLabelText(/Notifications/);
      fireEvent.click(trigger);

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith('/api/notifications', {
          headers: {
            Authorization: 'Bearer test-token',
          },
        });
      });
    });

    it('should display loading state while fetching', async () => {
      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        token: 'test-token',
        user: mockDataGenerators.user(),
        isLoading: false,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshProfile: vi.fn(),
      });

      (global.fetch as any).mockImplementation(
        () =>
          new Promise((resolve) =>
            setTimeout(
              () =>
                resolve({
                  ok: true,
                  json: () => Promise.resolve([]),
                }),
              100
            )
          )
      );

      renderNotificationPanel();

      const trigger = screen.getByLabelText(/Notifications/);
      fireEvent.click(trigger);

      // Loading state should be visible
      expect(screen.getByLabelText('Loading notifications')).toBeInTheDocument();
    });

    it('should display error message on fetch failure', async () => {
      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        token: 'test-token',
        user: mockDataGenerators.user(),
        isLoading: false,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshProfile: vi.fn(),
      });

      (global.fetch as any).mockResolvedValue({
        ok: false,
        status: 500,
      });

      renderNotificationPanel();

      const trigger = screen.getByLabelText(/Notifications/);
      fireEvent.click(trigger);

      await waitFor(() => {
        const errorElement = screen.getByRole('alert');
        expect(errorElement).toBeInTheDocument();
        expect(errorElement).toHaveTextContent('Failed to fetch notifications');
      });
    });

    it('should display empty state when no notifications', async () => {
      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        token: 'test-token',
        user: mockDataGenerators.user(),
        isLoading: false,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshProfile: vi.fn(),
      });

      (global.fetch as any).mockResolvedValue({
        ok: true,
        json: vi.fn().mockResolvedValue([]),
      });

      renderNotificationPanel();

      const trigger = screen.getByLabelText(/Notifications/);
      fireEvent.click(trigger);

      await waitFor(() => {
        expect(screen.getByText('No notifications')).toBeInTheDocument();
      });
    });
  });

  describe('Notification List Display', () => {
    it('should display list of notifications', async () => {
      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        token: 'test-token',
        user: mockDataGenerators.user(),
        isLoading: false,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshProfile: vi.fn(),
      });

      const notifications = [
        mockNotification({ id: 'notif-1', message: 'First notification' }),
        mockNotification({ id: 'notif-2', message: 'Second notification' }),
      ];

      (global.fetch as any).mockResolvedValue({
        ok: true,
        json: vi.fn().mockResolvedValue(notifications),
      });

      renderNotificationPanel();

      const trigger = screen.getByLabelText(/Notifications/);
      fireEvent.click(trigger);

      await waitFor(() => {
        expect(screen.getByText('First notification')).toBeInTheDocument();
        expect(screen.getByText('Second notification')).toBeInTheDocument();
      });
    });

    it('should display unread notifications with unread styling', async () => {
      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        token: 'test-token',
        user: mockDataGenerators.user(),
        isLoading: false,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshProfile: vi.fn(),
      });

      const notifications = [
        mockNotification({ id: 'notif-1', isRead: false }),
      ];

      (global.fetch as any).mockResolvedValue({
        ok: true,
        json: vi.fn().mockResolvedValue(notifications),
      });

      renderNotificationPanel();

      const trigger = screen.getByLabelText(/Notifications/);
      fireEvent.click(trigger);

      await waitFor(() => {
        const listItem = screen.getByRole('listitem');
        expect(listItem).toHaveClass('notification-panel__item--unread');
      });
    });

    it('should display notification timestamp', async () => {
      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        token: 'test-token',
        user: mockDataGenerators.user(),
        isLoading: false,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshProfile: vi.fn(),
      });

      const createdAt = '2024-01-15T10:30:00Z';
      const notifications = [
        mockNotification({ id: 'notif-1', createdAt }),
      ];

      (global.fetch as any).mockResolvedValue({
        ok: true,
        json: vi.fn().mockResolvedValue(notifications),
      });

      renderNotificationPanel();

      const trigger = screen.getByLabelText(/Notifications/);
      fireEvent.click(trigger);

      await waitFor(() => {
        const timeElement = screen.getByRole('listitem').querySelector('time');
        expect(timeElement).toHaveAttribute('dateTime', createdAt);
      });
    });
  });

  describe('Mark as Read Action', () => {
    it('should mark notification as read when button is clicked', async () => {
      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        token: 'test-token',
        user: mockDataGenerators.user(),
        isLoading: false,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshProfile: vi.fn(),
      });

      const notifications = [
        mockNotification({ id: 'notif-1', isRead: false }),
      ];

      (global.fetch as any)
        .mockResolvedValueOnce({
          ok: true,
          json: vi.fn().mockResolvedValue(notifications),
        })
        .mockResolvedValueOnce({
          ok: true,
        });

      renderNotificationPanel();

      const trigger = screen.getByLabelText(/Notifications/);
      fireEvent.click(trigger);

      await waitFor(() => {
        const markAsReadButton = screen.getByLabelText(/Mark.*as read/);
        expect(markAsReadButton).toBeInTheDocument();
      });

      const markAsReadButton = screen.getByLabelText(/Mark.*as read/);
      fireEvent.click(markAsReadButton);

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          '/api/notifications/notif-1/read',
          {
            method: 'PUT',
            headers: {
              Authorization: 'Bearer test-token',
            },
          }
        );
      });
    });

    it('should remove mark as read button after marking as read', async () => {
      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        token: 'test-token',
        user: mockDataGenerators.user(),
        isLoading: false,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshProfile: vi.fn(),
      });

      const notifications = [
        mockNotification({ id: 'notif-1', isRead: false }),
      ];

      (global.fetch as any)
        .mockResolvedValueOnce({
          ok: true,
          json: vi.fn().mockResolvedValue(notifications),
        })
        .mockResolvedValueOnce({
          ok: true,
        });

      renderNotificationPanel();

      const trigger = screen.getByLabelText(/Notifications/);
      fireEvent.click(trigger);

      await waitFor(() => {
        const markAsReadButton = screen.getByLabelText(/Mark.*as read/);
        fireEvent.click(markAsReadButton);
      });

      await waitFor(() => {
        expect(screen.queryByLabelText(/Mark.*as read/)).not.toBeInTheDocument();
      });
    });
  });

  describe('Delete Action', () => {
    it('should delete notification when delete button is clicked', async () => {
      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        token: 'test-token',
        user: mockDataGenerators.user(),
        isLoading: false,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshProfile: vi.fn(),
      });

      const notifications = [
        mockNotification({ id: 'notif-1', message: 'Test notification' }),
      ];

      (global.fetch as any)
        .mockResolvedValueOnce({
          ok: true,
          json: vi.fn().mockResolvedValue(notifications),
        })
        .mockResolvedValueOnce({
          ok: true,
        });

      renderNotificationPanel();

      const trigger = screen.getByLabelText(/Notifications/);
      fireEvent.click(trigger);

      await waitFor(() => {
        const deleteButton = screen.getByLabelText(/Delete.*Test notification/);
        expect(deleteButton).toBeInTheDocument();
      });

      const deleteButton = screen.getByLabelText(/Delete.*Test notification/);
      fireEvent.click(deleteButton);

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          '/api/notifications/notif-1',
          {
            method: 'DELETE',
            headers: {
              Authorization: 'Bearer test-token',
            },
          }
        );
      });
    });

    it('should remove notification from list after deletion', async () => {
      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        token: 'test-token',
        user: mockDataGenerators.user(),
        isLoading: false,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshProfile: vi.fn(),
      });

      const notifications = [
        mockNotification({ id: 'notif-1', message: 'Test notification' }),
      ];

      (global.fetch as any)
        .mockResolvedValueOnce({
          ok: true,
          json: vi.fn().mockResolvedValue(notifications),
        })
        .mockResolvedValueOnce({
          ok: true,
        });

      renderNotificationPanel();

      const trigger = screen.getByLabelText(/Notifications/);
      fireEvent.click(trigger);

      await waitFor(() => {
        expect(screen.getByText('Test notification')).toBeInTheDocument();
      });

      const deleteButton = screen.getByLabelText(/Delete.*Test notification/);
      fireEvent.click(deleteButton);

      await waitFor(() => {
        expect(screen.queryByText('Test notification')).not.toBeInTheDocument();
      });
    });
  });

  describe('Accessibility', () => {
    it('should have proper ARIA labels and roles', async () => {
      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        token: 'test-token',
        user: mockDataGenerators.user(),
        isLoading: false,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshProfile: vi.fn(),
      });

      (global.fetch as any).mockResolvedValue({
        ok: true,
        json: vi.fn().mockResolvedValue([]),
      });

      renderNotificationPanel();

      const trigger = screen.getByLabelText(/Notifications/);
      expect(trigger).toHaveAttribute('aria-haspopup', 'dialog');

      fireEvent.click(trigger);

      const dialog = screen.getByRole('dialog');
      expect(dialog).toHaveAttribute('aria-label', 'Notifications');
    });

    it('should have semantic HTML structure', async () => {
      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        token: 'test-token',
        user: mockDataGenerators.user(),
        isLoading: false,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshProfile: vi.fn(),
      });

      const notifications = [
        mockNotification({ id: 'notif-1' }),
      ];

      (global.fetch as any).mockResolvedValue({
        ok: true,
        json: vi.fn().mockResolvedValue(notifications),
      });

      renderNotificationPanel();

      const trigger = screen.getByLabelText(/Notifications/);
      fireEvent.click(trigger);

      await waitFor(() => {
        const list = screen.getByRole('list');
        expect(list).toBeInTheDocument();

        const listItems = screen.getAllByRole('listitem');
        expect(listItems.length).toBeGreaterThan(0);
      });
    });

    it('should be keyboard navigable', async () => {
      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        token: 'test-token',
        user: mockDataGenerators.user(),
        isLoading: false,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshProfile: vi.fn(),
      });

      (global.fetch as any).mockResolvedValue({
        ok: true,
        json: vi.fn().mockResolvedValue([]),
      });

      renderNotificationPanel();

      const trigger = screen.getByLabelText(/Notifications/);

      // Trigger button should be focusable
      trigger.focus();
      expect(document.activeElement).toBe(trigger);

      // Open panel with click
      fireEvent.click(trigger);

      // Close button should be focusable
      const closeButton = screen.getByLabelText('Close notifications panel');
      closeButton.focus();
      expect(document.activeElement).toBe(closeButton);
    });
  });

  describe('Integration', () => {
    it('should render complete notification panel with all elements', async () => {
      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        token: 'test-token',
        user: mockDataGenerators.user(),
        isLoading: false,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshProfile: vi.fn(),
      });

      const notifications = [
        mockNotification({ id: 'notif-1', message: 'Test notification', isRead: false }),
      ];

      (global.fetch as any).mockResolvedValue({
        ok: true,
        json: vi.fn().mockResolvedValue(notifications),
      });

      renderNotificationPanel();

      // Trigger button should be present
      const trigger = screen.getByLabelText(/Notifications/);
      expect(trigger).toBeInTheDocument();

      // Open panel to trigger fetch
      fireEvent.click(trigger);

      // Panel should be visible
      const dialog = screen.getByRole('dialog');
      expect(dialog).toBeInTheDocument();

      // Wait for badge to appear with unread count
      await waitFor(() => {
        expect(screen.getByText('1')).toBeInTheDocument();
      });

      // Notification should be displayed
      await waitFor(() => {
        expect(screen.getByText('Test notification')).toBeInTheDocument();
      });

      // Actions should be available
      expect(screen.getByLabelText(/Mark.*as read/)).toBeInTheDocument();
      expect(screen.getByLabelText(/Delete.*Test notification/)).toBeInTheDocument();
    });
  });
});
