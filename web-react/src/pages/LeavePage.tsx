import React from 'react';
import { Navigate } from 'react-router-dom';

const LeavePage: React.FC = () => {
  return <Navigate to="/leave/requests" replace />;
};

export default LeavePage;
