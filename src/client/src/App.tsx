import { Routes, Route, Navigate } from 'react-router-dom';

/**
 * Main application component with React Router configuration.
 * Routes are defined here and will be populated as features are implemented.
 */
function App(): JSX.Element {
  return (
    <Routes>
      <Route path="/" element={<div>Welcome to Location Management</div>} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

export default App;
