import React, { createContext, useContext, useState, useCallback, useEffect } from 'react';

/**
 * User profile information returned from the API.
 */
export interface UserProfile {
  id: string;
  username: string;
  displayName: string;
  email: string;
  role: 'Standard' | 'Admin';
  avatarImageId?: string;
  showPublicCollections: boolean;
}

/**
 * Authentication context providing JWT token management and user profile state.
 */
interface AuthContextType {
  token: string | null;
  user: UserProfile | null;
  isLoading: boolean;
  login: (username: string, password: string) => Promise<void>;
  register: (username: string, displayName: string, email: string, password: string) => Promise<void>;
  logout: () => void;
  refreshProfile: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

const STORAGE_KEY = 'auth_token';

/**
 * AuthProvider component that wraps the application and provides authentication context.
 * Manages JWT token storage in localStorage and user profile state.
 */
export function AuthProvider({ children }: { children: React.ReactNode }): JSX.Element {
  const [token, setToken] = useState<string | null>(() => {
    return localStorage.getItem(STORAGE_KEY);
  });
  const [user, setUser] = useState<UserProfile | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  // Fetch user profile when token changes
  useEffect(() => {
    if (token) {
      refreshProfile();
    } else {
      setUser(null);
    }
  }, [token]);

  const refreshProfile = useCallback(async () => {
    if (!token) {
      setUser(null);
      return;
    }

    try {
      setIsLoading(true);
      const response = await fetch('/api/users/me', {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      if (response.ok) {
        const profile = await response.json();
        setUser(profile);
      } else if (response.status === 401) {
        // Token is invalid, clear it
        setToken(null);
        localStorage.removeItem(STORAGE_KEY);
        setUser(null);
      }
    } catch (error) {
      console.error('Failed to refresh profile:', error);
    } finally {
      setIsLoading(false);
    }
  }, [token]);

  const login = useCallback(async (username: string, password: string) => {
    try {
      setIsLoading(true);
      const response = await fetch('/api/auth/login', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ username, password }),
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message || 'Login failed');
      }

      const data = await response.json();
      setToken(data.token);
      localStorage.setItem(STORAGE_KEY, data.token);
    } finally {
      setIsLoading(false);
    }
  }, []);

  const register = useCallback(async (username: string, displayName: string, email: string, password: string) => {
    try {
      setIsLoading(true);
      const response = await fetch('/api/auth/register', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ username, displayName, email, password }),
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message || 'Registration failed');
      }

      // After successful registration, log in automatically
      await login(username, password);
    } finally {
      setIsLoading(false);
    }
  }, [login]);

  const logout = useCallback(() => {
    setToken(null);
    setUser(null);
    localStorage.removeItem(STORAGE_KEY);
  }, []);

  const value: AuthContextType = {
    token,
    user,
    isLoading,
    login,
    register,
    logout,
    refreshProfile,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

/**
 * Hook to access the authentication context.
 * Must be used within an AuthProvider.
 */
export function useAuth(): AuthContextType {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
