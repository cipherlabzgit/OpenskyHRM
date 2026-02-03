import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './contexts/AuthContext';
import PrivateRoute from './components/PrivateRoute';
import Layout from './components/Layout';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import DashboardPage from './pages/DashboardPage';
import EmployeesPage from './pages/EmployeesPage';
import AttendancePage from './pages/AttendancePage';
import LeavePage from './pages/LeavePage';
import LeaveRequestsPage from './pages/leave/LeaveRequestsPage';
import ReportsPage from './pages/ReportsPage';
import OrgChartPage from './pages/OrgChartPage';
import SettingsPage from './pages/SettingsPage';
import ProfileSettingsPage from './pages/settings/ProfileSettingsPage';
import CompanySettingsPage from './pages/settings/CompanySettingsPage';
import DepartmentsPage from './pages/settings/DepartmentsPage';
import DesignationsPage from './pages/settings/DesignationsPage';
import UsersPage from './pages/settings/UsersPage';
import RecruitmentPage from './pages/recruiting/RecruitmentPage';
import RequisitionsPage from './pages/recruiting/RequisitionsPage';
import RequisitionDetailPage from './pages/recruiting/RequisitionDetailPage';
import ATSPipelinePage from './pages/recruiting/ATSPipelinePage';
import InterviewsPage from './pages/recruiting/InterviewsPage';
import OffersPage from './pages/recruiting/OffersPage';
import RecruitingReportsPage from './pages/recruiting/ReportsPage';
import CareerPortalPage from './pages/CareerPortalPage';
import './App.css';

// Placeholder components for other routes
const PlaceholderPage: React.FC<{ title: string }> = ({ title }) => (
  <div style={{ padding: '24px', background: 'white', borderRadius: '12px' }}>
    <h2>{title}</h2>
    <p style={{ color: '#64748b' }}>This feature is coming soon.</p>
  </div>
);

function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          {/* Public routes */}
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/careers" element={<CareerPortalPage />} />

          {/* Protected routes */}
          <Route path="/" element={
            <PrivateRoute>
              <Layout>
                <Navigate to="/dashboard" replace />
              </Layout>
            </PrivateRoute>
          } />
          
          <Route path="/dashboard" element={
            <PrivateRoute>
              <Layout>
                <DashboardPage />
              </Layout>
            </PrivateRoute>
          } />

          <Route path="/employees" element={
            <PrivateRoute>
              <Layout>
                <EmployeesPage />
              </Layout>
            </PrivateRoute>
          } />

          <Route path="/attendance" element={
            <PrivateRoute>
              <Layout>
                <AttendancePage />
              </Layout>
            </PrivateRoute>
          } />

          {/* Leave routes */}
          <Route path="/leave" element={
            <PrivateRoute>
              <Layout>
                <LeavePage />
              </Layout>
            </PrivateRoute>
          } />
          <Route path="/leave/requests" element={
            <PrivateRoute>
              <Layout>
                <LeaveRequestsPage />
              </Layout>
            </PrivateRoute>
          } />
          <Route path="/leave/calendar" element={
            <PrivateRoute>
              <Layout>
                <PlaceholderPage title="Leave Calendar" />
              </Layout>
            </PrivateRoute>
          } />
          <Route path="/leave/balances" element={
            <PrivateRoute>
              <Layout>
                <PlaceholderPage title="Leave Balances" />
              </Layout>
            </PrivateRoute>
          } />

          {/* Recruiting routes */}
          <Route path="/recruiting" element={
            <PrivateRoute>
              <Layout>
                <Navigate to="/recruiting/pipeline" replace />
              </Layout>
            </PrivateRoute>
          } />
          <Route path="/recruiting/pipeline" element={
            <PrivateRoute>
              <Layout>
                <RecruitmentPage />
              </Layout>
            </PrivateRoute>
          } />
          <Route path="/recruiting/requisitions" element={
            <PrivateRoute>
              <Layout>
                <RecruitmentPage />
              </Layout>
            </PrivateRoute>
          } />
          <Route path="/recruiting/requisitions/:id" element={
            <PrivateRoute>
              <Layout>
                <RequisitionDetailPage />
              </Layout>
            </PrivateRoute>
          } />
          <Route path="/recruiting/configuration" element={
            <PrivateRoute>
              <Layout>
                <RecruitmentPage />
              </Layout>
            </PrivateRoute>
          } />
          <Route path="/recruiting/interviews" element={
            <PrivateRoute>
              <Layout>
                <InterviewsPage />
              </Layout>
            </PrivateRoute>
          } />
          <Route path="/recruiting/offers" element={
            <PrivateRoute>
              <Layout>
                <OffersPage />
              </Layout>
            </PrivateRoute>
          } />
          <Route path="/recruiting/reports" element={
            <PrivateRoute>
              <Layout>
                <RecruitingReportsPage />
              </Layout>
            </PrivateRoute>
          } />

          {/* Onboarding routes */}
          <Route path="/onboarding" element={
            <PrivateRoute>
              <Layout>
                <Navigate to="/onboarding/templates" replace />
              </Layout>
            </PrivateRoute>
          } />
          <Route path="/onboarding/templates" element={
            <PrivateRoute>
              <Layout>
                <PlaceholderPage title="Onboarding Templates" />
              </Layout>
            </PrivateRoute>
          } />
          <Route path="/onboarding/tasks" element={
            <PrivateRoute>
              <Layout>
                <PlaceholderPage title="Onboarding Tasks" />
              </Layout>
            </PrivateRoute>
          } />
          <Route path="/onboarding/newhires" element={
            <PrivateRoute>
              <Layout>
                <PlaceholderPage title="New Hires" />
              </Layout>
            </PrivateRoute>
          } />

          {/* Performance routes */}
          <Route path="/performance" element={
            <PrivateRoute>
              <Layout>
                <Navigate to="/performance/reviews" replace />
              </Layout>
            </PrivateRoute>
          } />
          <Route path="/performance/reviews" element={
            <PrivateRoute>
              <Layout>
                <PlaceholderPage title="Performance Reviews" />
              </Layout>
            </PrivateRoute>
          } />
          <Route path="/performance/goals" element={
            <PrivateRoute>
              <Layout>
                <PlaceholderPage title="Goals" />
              </Layout>
            </PrivateRoute>
          } />
          <Route path="/performance/feedback" element={
            <PrivateRoute>
              <Layout>
                <PlaceholderPage title="360 Feedback" />
              </Layout>
            </PrivateRoute>
          } />

          {/* Benefits routes */}
          <Route path="/benefits" element={
            <PrivateRoute>
              <Layout>
                <Navigate to="/benefits/plans" replace />
              </Layout>
            </PrivateRoute>
          } />
          <Route path="/benefits/plans" element={
            <PrivateRoute>
              <Layout>
                <PlaceholderPage title="Benefit Plans" />
              </Layout>
            </PrivateRoute>
          } />
          <Route path="/benefits/enrollments" element={
            <PrivateRoute>
              <Layout>
                <PlaceholderPage title="Enrollments" />
              </Layout>
            </PrivateRoute>
          } />

          {/* Training routes */}
          <Route path="/training" element={
            <PrivateRoute>
              <Layout>
                <Navigate to="/training/catalog" replace />
              </Layout>
            </PrivateRoute>
          } />
          <Route path="/training/catalog" element={
            <PrivateRoute>
              <Layout>
                <PlaceholderPage title="Training Catalog" />
              </Layout>
            </PrivateRoute>
          } />
          <Route path="/training/assignments" element={
            <PrivateRoute>
              <Layout>
                <PlaceholderPage title="Training Assignments" />
              </Layout>
            </PrivateRoute>
          } />

          <Route path="/org-chart" element={
            <PrivateRoute>
              <Layout>
                <OrgChartPage />
              </Layout>
            </PrivateRoute>
          } />

          <Route path="/reports" element={
            <PrivateRoute>
              <Layout>
                <ReportsPage />
              </Layout>
            </PrivateRoute>
          } />

          {/* Settings routes */}
          <Route path="/settings" element={
            <PrivateRoute>
              <Layout>
                <SettingsPage />
              </Layout>
            </PrivateRoute>
          } />
          <Route path="/settings/profile" element={
            <PrivateRoute>
              <Layout>
                <ProfileSettingsPage />
              </Layout>
            </PrivateRoute>
          } />
          <Route path="/settings/company" element={
            <PrivateRoute>
              <Layout>
                <Navigate to="/settings/company/departments" replace />
              </Layout>
            </PrivateRoute>
          } />
          <Route path="/settings/company/departments" element={
            <PrivateRoute>
              <Layout>
                <DepartmentsPage />
              </Layout>
            </PrivateRoute>
          } />
          <Route path="/settings/company/designations" element={
            <PrivateRoute>
              <Layout>
                <DesignationsPage />
              </Layout>
            </PrivateRoute>
          } />
          <Route path="/settings/users" element={
            <PrivateRoute>
              <Layout>
                <UsersPage />
              </Layout>
            </PrivateRoute>
          } />
          <Route path="/settings/security" element={
            <PrivateRoute>
              <Layout>
                <PlaceholderPage title="Security Settings" />
              </Layout>
            </PrivateRoute>
          } />

          {/* Catch all */}
          <Route path="*" element={<Navigate to="/login" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}

export default App;
