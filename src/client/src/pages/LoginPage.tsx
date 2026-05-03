import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import './LoginPage.css';

type FormMode = 'login' | 'register';

/**
 * Password complexity validation rules.
 */
function validatePasswordComplexity(password: string): { valid: boolean; errors: string[] } {
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

  return {
    valid: errors.length === 0,
    errors,
  };
}

/**
 * LoginForm component for user authentication.
 */
function LoginForm(): JSX.Element {
  const { login, isLoading } = useAuth();
  const navigate = useNavigate();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    try {
      await login(username, password);
      navigate('/');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Login failed');
    }
  };

  return (
    <form onSubmit={handleSubmit} className="auth-form">
      <h2>Login</h2>

      {error && <div className="error-message" role="alert">{error}</div>}

      <div className="form-group">
        <label htmlFor="login-username">Username</label>
        <input
          id="login-username"
          type="text"
          value={username}
          onChange={(e) => setUsername(e.target.value)}
          required
          disabled={isLoading}
        />
      </div>

      <div className="form-group">
        <label htmlFor="login-password">Password</label>
        <input
          id="login-password"
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
          disabled={isLoading}
        />
      </div>

      <button type="submit" disabled={isLoading}>
        {isLoading ? 'Logging in...' : 'Login'}
      </button>

      <div className="forgot-password">
        <a href="/forgot-password">Forgot password?</a>
      </div>
    </form>
  );
}

/**
 * RegisterForm component for user registration.
 */
function RegisterForm(): JSX.Element {
  const { register, isLoading } = useAuth();
  const navigate = useNavigate();
  const [username, setUsername] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState('');
  const [passwordErrors, setPasswordErrors] = useState<string[]>([]);

  const handlePasswordChange = (value: string) => {
    setPassword(value);
    const validation = validatePasswordComplexity(value);
    setPasswordErrors(validation.errors);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    // Validate password complexity
    const validation = validatePasswordComplexity(password);
    if (!validation.valid) {
      setPasswordErrors(validation.errors);
      return;
    }

    // Validate passwords match
    if (password !== confirmPassword) {
      setError('Passwords do not match');
      return;
    }

    try {
      await register(username, displayName, email, password);
      navigate('/');
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Registration failed';
      // Check if it's a duplicate error
      if (errorMessage.includes('username') || errorMessage.includes('Username')) {
        setError('Username already exists');
      } else if (errorMessage.includes('displayName') || errorMessage.includes('Display name')) {
        setError('Display name already exists');
      } else if (errorMessage.includes('email') || errorMessage.includes('Email')) {
        setError('Email already exists');
      } else {
        setError(errorMessage);
      }
    }
  };

  const isPasswordValid = validatePasswordComplexity(password).valid;

  return (
    <form onSubmit={handleSubmit} className="auth-form">
      <h2>Register</h2>

      {error && <div className="error-message" role="alert">{error}</div>}

      <div className="form-group">
        <label htmlFor="register-username">Username</label>
        <input
          id="register-username"
          type="text"
          value={username}
          onChange={(e) => setUsername(e.target.value)}
          required
          disabled={isLoading}
        />
      </div>

      <div className="form-group">
        <label htmlFor="register-displayname">Display Name</label>
        <input
          id="register-displayname"
          type="text"
          value={displayName}
          onChange={(e) => setDisplayName(e.target.value)}
          required
          disabled={isLoading}
        />
      </div>

      <div className="form-group">
        <label htmlFor="register-email">Email</label>
        <input
          id="register-email"
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
          disabled={isLoading}
        />
      </div>

      <div className="form-group">
        <label htmlFor="register-password">Password</label>
        <input
          id="register-password"
          type="password"
          value={password}
          onChange={(e) => handlePasswordChange(e.target.value)}
          required
          disabled={isLoading}
          aria-describedby="password-requirements"
        />
        {passwordErrors.length > 0 && (
          <div id="password-requirements" className="password-errors" role="alert">
            <p>Password must:</p>
            <ul>
              {passwordErrors.map((error, idx) => (
                <li key={idx}>{error}</li>
              ))}
            </ul>
          </div>
        )}
      </div>

      <div className="form-group">
        <label htmlFor="register-confirm-password">Confirm Password</label>
        <input
          id="register-confirm-password"
          type="password"
          value={confirmPassword}
          onChange={(e) => setConfirmPassword(e.target.value)}
          required
          disabled={isLoading}
        />
        {password && confirmPassword && password !== confirmPassword && (
          <div className="error-message" role="alert">Passwords do not match</div>
        )}
      </div>

      <button type="submit" disabled={isLoading || !isPasswordValid || password !== confirmPassword}>
        {isLoading ? 'Registering...' : 'Register'}
      </button>
    </form>
  );
}

/**
 * LoginPage component with tabs for login and registration.
 * Redirects authenticated users to homepage.
 */
export function LoginPage(): JSX.Element {
  const { token } = useAuth();
  const navigate = useNavigate();
  const [mode, setMode] = useState<FormMode>('login');

  // Redirect authenticated users to homepage
  if (token) {
    navigate('/');
    return <div>Redirecting...</div>;
  }

  return (
    <div className="login-page">
      <div className="login-container">
        <div className="form-tabs">
          <button
            className={`tab-button ${mode === 'login' ? 'active' : ''}`}
            onClick={() => setMode('login')}
            aria-selected={mode === 'login'}
          >
            Login
          </button>
          <button
            className={`tab-button ${mode === 'register' ? 'active' : ''}`}
            onClick={() => setMode('register')}
            aria-selected={mode === 'register'}
          >
            Register
          </button>
        </div>

        <div className="form-content">
          {mode === 'login' ? <LoginForm /> : <RegisterForm />}
        </div>
      </div>
    </div>
  );
}
