import { Navigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

interface ProtectedRouteProps {
  children: React.ReactNode;
}

/**
 * ProtectedRoute component that redirects unauthenticated users to /login.
 * Used to protect routes that require authentication.
 */
export function ProtectedRoute({ children }: ProtectedRouteProps): JSX.Element {
  const { token, isLoading } = useAuth();

  if (isLoading) {
    return <div>Loading...</div>;
  }

  if (!token) {
    return <Navigate to="/login" replace />;
  }

  return <>{children}</>;
}

interface AdminRouteProps {
  children: React.ReactNode;
}

/**
 * AdminRoute component that redirects non-admin users to /.
 * Used to protect routes that require admin privileges.
 */
export function AdminRoute({ children }: AdminRouteProps): JSX.Element {
  const { token, user, isLoading } = useAuth();

  if (isLoading) {
    return <div>Loading...</div>;
  }

  if (!token || !user || user.role !== 'Admin') {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
}
