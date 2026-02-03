import React from 'react';
import { Navigate } from 'react-router-dom';

const SettingsPage: React.FC = () => {
  return <Navigate to="/settings/profile" replace />;
};

export default SettingsPage;
