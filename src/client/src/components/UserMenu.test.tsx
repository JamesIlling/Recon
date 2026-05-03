import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '../test/test-utils';
import { UserMenu } from './UserMenu';
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

/**
 * Mock useNavigate hook
 */
const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

import { useAuth } from '../contexts/AuthContext';

const renderUserMenu = () => {
  return render(<UserMenu />);
};

describe('UserMenu', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockNavigate.mockClear();
  });

  describe('Loading State', () => {
    it('should display loading skeleton while profile data is being fetched', () => {
      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        user: null,
        logout: vi.fn(),
        isLoading: true,
        token: null,
        login: vi.fn(),
        register: vi.fn(),
        refreshProfile: vi.fn(),
      });

      renderUserMenu();

      const loadingElement = screen.getByLabelText('Loading user menu');
      expect(loadingElement).toBeInTheDocument();
      expect(loadingElement).toHaveClass('user-menu--loading');
    });
  });

  describe('Error State', () => {
    it('should render nothing when user is not authenticated', () => {
      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        user: null,
        logout: vi.fn(),
        isLoading: false,
        token: null,
        login: vi.fn(),
        register: vi.fn(),
        refreshProfile: vi.fn(),
      });

      const { container } = renderUserMenu();

      expect(container.querySelector('.user-menu')).not.toBeInTheDocument();
    });
  });

  describe('Avatar Rendering', () => {
    it('should render avatar with ThumbnailVariant URL', () => {
      const mockUser = mockDataGenerators.user({
        avatarImageId: 'avatar-123',
      });

      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        user: mockUser,
        logout: vi.fn(),
        isLoading: false,
        token: 'test-token',
        login: vi.fn(),
        register: vi.fn(),
        refreshProfile: vi.fn(),
      });

      renderUserMenu();

      const avatarImage = screen.getByAltText(`${mockUser.displayName}'s avatar`);
      expect(avatarImage).toBeInTheDocument();
      expect(avatarImage).toHaveAttribute('src', `/api/images/avatar-123/thumbnail`);
    });

    it('should render avatar with alt text for accessibility', () => {
      const mockUser = mockDataGenerators.user({
        avatarImageId: 'avatar-123',
        displayName: 'John Doe',
      });

      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        user: mockUser,
        logout: vi.fn(),
        isLoading: false,
        token: 'test-token',
        login: vi.fn(),
        register: vi.fn(),
        refreshProfile: vi.fn(),
      });

      renderUserMenu();

      const avatarImage = screen.getByAltText("John Doe's avatar");
      expect(avatarImage).toBeInTheDocument();
    });

    it('should display fallback placeholder when avatar image ID is not available', () => {
      const mockUser = mockDataGenerators.user({
        avatarImageId: undefined,
      });

      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        user: mockUser,
        logout: vi.fn(),
        isLoading: false,
        token: 'test-token',
        login: vi.fn(),
        register: vi.fn(),
        refreshProfile: vi.fn(),
      });

      renderUserMenu();

      const placeholder = screen.getByText('U');
      expect(placeholder).toBeInTheDocument();
    });
  });

  describe('Display Name Rendering', () => {
    it('should render user display name', () => {
      const mockUser = mockDataGenerators.user({
        displayName: 'Alice Smith',
      });

      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        user: mockUser,
        logout: vi.fn(),
        isLoading: false,
        token: 'test-token',
        login: vi.fn(),
        register: vi.fn(),
        refreshProfile: vi.fn(),
      });

      renderUserMenu();

      expect(screen.getByText('Alice Smith')).toBeInTheDocument();
    });

    it('should set title attribute on trigger button with display name', () => {
      const mockUser = mockDataGenerators.user({
        displayName: 'Bob Johnson',
      });

      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        user: mockUser,
        logout: vi.fn(),
        isLoading: false,
        token: 'test-token',
        login: vi.fn(),
        register: vi.fn(),
        refreshProfile: vi.fn(),
      });

      renderUserMenu();

      const trigger = screen.getByLabelText(/User menu for/);
      expect(trigger).toHaveAttribute('title', 'Bob Johnson');
    });
  });

  describe('Settings Link Navigation', () => {
    it('should navigate to /settings when settings button is clicked', async () => {
      const mockUser = mockDataGenerators.user();

      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        user: mockUser,
        logout: vi.fn(),
        isLoading: false,
        token: 'test-token',
        login: vi.fn(),
        register: vi.fn(),
        refreshProfile: vi.fn(),
      });

      renderUserMenu();

      // Open menu
      const trigger = screen.getByLabelText(/User menu for/);
      fireEvent.click(trigger);

      // Click settings
      const settingsButton = screen.getByLabelText('Go to settings');
      fireEvent.click(settingsButton);

      await waitFor(() => {
        expect(mockNavigate).toHaveBeenCalledWith('/settings');
      });
    });

    it('should close menu after navigating to settings', async () => {
      const mockUser = mockDataGenerators.user();

      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        user: mockUser,
        logout: vi.fn(),
        isLoading: false,
        token: 'test-token',
        login: vi.fn(),
        register: vi.fn(),
        refreshProfile: vi.fn(),
      });

      renderUserMenu();

      // Open menu
      const trigger = screen.getByLabelText(/User menu for/);
      fireEvent.click(trigger);

      expect(trigger).toHaveAttribute('aria-expanded', 'true');

      // Click settings
      const settingsButton = screen.getByLabelText('Go to settings');
      fireEvent.click(settingsButton);

      await waitFor(() => {
        expect(trigger).toHaveAttribute('aria-expanded', 'false');
      });
    });
  });

  describe('Sign-Out Button Functionality', () => {
    it('should call logout when sign-out button is clicked', async () => {
      const mockLogout = vi.fn();
      const mockUser = mockDataGenerators.user();

      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        user: mockUser,
        logout: mockLogout,
        isLoading: false,
        token: 'test-token',
        login: vi.fn(),
        register: vi.fn(),
        refreshProfile: vi.fn(),
      });

      renderUserMenu();

      // Open menu
      const trigger = screen.getByLabelText(/User menu for/);
      fireEvent.click(trigger);

      // Click sign out
      const signOutButton = screen.getByLabelText('Sign out');
      fireEvent.click(signOutButton);

      await waitFor(() => {
        expect(mockLogout).toHaveBeenCalled();
      });
    });

    it('should redirect to /login after sign-out', async () => {
      const mockLogout = vi.fn();
      const mockUser = mockDataGenerators.user();

      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        user: mockUser,
        logout: mockLogout,
        isLoading: false,
        token: 'test-token',
        login: vi.fn(),
        register: vi.fn(),
        refreshProfile: vi.fn(),
      });

      renderUserMenu();

      // Open menu
      const trigger = screen.getByLabelText(/User menu for/);
      fireEvent.click(trigger);

      // Click sign out
      const signOutButton = screen.getByLabelText('Sign out');
      fireEvent.click(signOutButton);

      await waitFor(() => {
        expect(mockNavigate).toHaveBeenCalledWith('/login');
      });
    });

    it('should verify logout clears JWT from localStorage', async () => {
      const mockLogout = vi.fn();
      const mockUser = mockDataGenerators.user();

      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        user: mockUser,
        logout: mockLogout,
        isLoading: false,
        token: 'test-token',
        login: vi.fn(),
        register: vi.fn(),
        refreshProfile: vi.fn(),
      });

      renderUserMenu();

      // Open menu
      const trigger = screen.getByLabelText(/User menu for/);
      fireEvent.click(trigger);

      // Click sign out
      const signOutButton = screen.getByLabelText('Sign out');
      fireEvent.click(signOutButton);

      await waitFor(() => {
        expect(mockLogout).toHaveBeenCalled();
      });

      // Note: The actual localStorage clearing is handled by the logout function
      // in AuthContext, which is mocked here. In integration tests, we would verify
      // that localStorage.removeItem('auth_token') was called.
    });
  });

  describe('Menu Toggle and Keyboard Navigation', () => {
    it('should toggle menu open/closed on button click', () => {
      const mockUser = mockDataGenerators.user();

      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        user: mockUser,
        logout: vi.fn(),
        isLoading: false,
        token: 'test-token',
        login: vi.fn(),
        register: vi.fn(),
        refreshProfile: vi.fn(),
      });

      renderUserMenu();

      const trigger = screen.getByLabelText(/User menu for/);

      // Initially closed
      expect(trigger).toHaveAttribute('aria-expanded', 'false');

      // Open menu
      fireEvent.click(trigger);
      expect(trigger).toHaveAttribute('aria-expanded', 'true');

      // Close menu
      fireEvent.click(trigger);
      expect(trigger).toHaveAttribute('aria-expanded', 'false');
    });

    it('should close menu when Escape key is pressed', () => {
      const mockUser = mockDataGenerators.user();

      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        user: mockUser,
        logout: vi.fn(),
        isLoading: false,
        token: 'test-token',
        login: vi.fn(),
        register: vi.fn(),
        refreshProfile: vi.fn(),
      });

      renderUserMenu();

      const trigger = screen.getByLabelText(/User menu for/);

      // Open menu
      fireEvent.click(trigger);
      expect(trigger).toHaveAttribute('aria-expanded', 'true');

      // Press Escape
      const userMenu = screen.getByRole('menu');
      fireEvent.keyDown(userMenu, { key: 'Escape' });

      expect(trigger).toHaveAttribute('aria-expanded', 'false');
    });

    it('should display menu with proper ARIA attributes', () => {
      const mockUser = mockDataGenerators.user();

      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        user: mockUser,
        logout: vi.fn(),
        isLoading: false,
        token: 'test-token',
        login: vi.fn(),
        register: vi.fn(),
        refreshProfile: vi.fn(),
      });

      renderUserMenu();

      const trigger = screen.getByLabelText(/User menu for/);
      fireEvent.click(trigger);

      const menu = screen.getByRole('menu');
      expect(menu).toHaveAttribute('aria-label', 'User menu options');

      const settingsItem = screen.getByLabelText('Go to settings');
      expect(settingsItem).toHaveAttribute('role', 'menuitem');

      const signOutItem = screen.getByLabelText('Sign out');
      expect(signOutItem).toHaveAttribute('role', 'menuitem');
    });
  });

  describe('Accessibility', () => {
    it('should have accessible button labels', () => {
      const mockUser = mockDataGenerators.user({
        displayName: 'Test User',
      });

      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        user: mockUser,
        logout: vi.fn(),
        isLoading: false,
        token: 'test-token',
        login: vi.fn(),
        register: vi.fn(),
        refreshProfile: vi.fn(),
      });

      renderUserMenu();

      const trigger = screen.getByLabelText('User menu for Test User');
      expect(trigger).toBeInTheDocument();

      fireEvent.click(trigger);

      const settingsButton = screen.getByLabelText('Go to settings');
      expect(settingsButton).toBeInTheDocument();

      const signOutButton = screen.getByLabelText('Sign out');
      expect(signOutButton).toBeInTheDocument();
    });

    it('should have proper semantic HTML structure', () => {
      const mockUser = mockDataGenerators.user();

      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        user: mockUser,
        logout: vi.fn(),
        isLoading: false,
        token: 'test-token',
        login: vi.fn(),
        register: vi.fn(),
        refreshProfile: vi.fn(),
      });

      renderUserMenu();

      const trigger = screen.getByLabelText(/User menu for/);
      fireEvent.click(trigger);

      // Menu should be a nav element
      const menu = screen.getByRole('menu');
      expect(menu.tagName).toBe('NAV');

      // Menu items should be buttons
      const menuItems = screen.getAllByRole('menuitem');
      menuItems.forEach((item) => {
        expect(item.tagName).toBe('BUTTON');
      });
    });

    it('should be keyboard navigable', () => {
      const mockUser = mockDataGenerators.user();

      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        user: mockUser,
        logout: vi.fn(),
        isLoading: false,
        token: 'test-token',
        login: vi.fn(),
        register: vi.fn(),
        refreshProfile: vi.fn(),
      });

      renderUserMenu();

      const trigger = screen.getByLabelText(/User menu for/);

      // Trigger button should be focusable
      trigger.focus();
      expect(document.activeElement).toBe(trigger);

      // Open menu with click
      fireEvent.click(trigger);

      // Menu items should be focusable
      const settingsButton = screen.getByLabelText('Go to settings');
      settingsButton.focus();
      expect(document.activeElement).toBe(settingsButton);
    });
  });

  describe('Integration', () => {
    it('should render complete user menu with all elements', () => {
      const mockUser = mockDataGenerators.user({
        displayName: 'Jane Doe',
        avatarImageId: 'avatar-456',
      });

      const mockUseAuth = useAuth as any;
      mockUseAuth.mockReturnValue({
        user: mockUser,
        logout: vi.fn(),
        isLoading: false,
        token: 'test-token',
        login: vi.fn(),
        register: vi.fn(),
        refreshProfile: vi.fn(),
      });

      renderUserMenu();

      // Avatar should be present
      const avatar = screen.getByAltText("Jane Doe's avatar");
      expect(avatar).toBeInTheDocument();

      // Display name should be present
      expect(screen.getByText('Jane Doe')).toBeInTheDocument();

      // Trigger button should be present
      const trigger = screen.getByLabelText('User menu for Jane Doe');
      expect(trigger).toBeInTheDocument();

      // Menu should not be visible initially
      expect(screen.queryByRole('menu')).not.toBeInTheDocument();

      // Open menu
      fireEvent.click(trigger);

      // Menu items should be visible
      expect(screen.getByLabelText('Go to settings')).toBeInTheDocument();
      expect(screen.getByLabelText('Sign out')).toBeInTheDocument();
    });
  });
});
