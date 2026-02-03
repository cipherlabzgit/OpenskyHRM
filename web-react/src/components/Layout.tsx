import React, { useState, useEffect } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import './Layout.css';

interface MenuItem {
  path: string;
  label: string;
  icon: string;
  children?: MenuItem[];
}

const menuItems: MenuItem[] = [
  { path: '/dashboard', label: 'Dashboard', icon: '📊' },
  { path: '/employees', label: 'Employees', icon: '👥' },
  { path: '/attendance', label: 'Attendance', icon: '⏰' },
  { 
    path: '/leave', 
    label: 'Leave', 
    icon: '🏖️',
    children: [
      { path: '/leave/requests', label: 'Requests', icon: '📝' },
      { path: '/leave/calendar', label: 'Calendar', icon: '📅' },
      { path: '/leave/balances', label: 'Balances', icon: '💰' }
    ]
  },
  { 
    path: '/recruiting', 
    label: 'Recruiting', 
    icon: '🎯',
    children: [
      { path: '/recruiting/requisitions', label: 'Job Requisitions', icon: '📋' },
      { path: '/recruiting/pipeline', label: 'ATS Pipeline', icon: '📊' },
      { path: '/recruiting/applicants', label: 'Applicants', icon: '👤' },
      { path: '/recruiting/interviews', label: 'Interviews', icon: '🗣️' },
      { path: '/recruiting/offers', label: 'Offers', icon: '💼' },
      { path: '/recruiting/reports', label: 'Reports', icon: '📈' }
    ]
  },
  { 
    path: '/onboarding', 
    label: 'Onboarding', 
    icon: '🚀',
    children: [
      { path: '/onboarding/templates', label: 'Templates', icon: '📄' },
      { path: '/onboarding/tasks', label: 'Tasks', icon: '✅' },
      { path: '/onboarding/newhires', label: 'New Hires', icon: '🆕' }
    ]
  },
  { 
    path: '/performance', 
    label: 'Performance', 
    icon: '📈',
    children: [
      { path: '/performance/reviews', label: 'Reviews', icon: '⭐' },
      { path: '/performance/goals', label: 'Goals', icon: '🎯' },
      { path: '/performance/feedback', label: '360 Feedback', icon: '🔄' }
    ]
  },
  { 
    path: '/benefits', 
    label: 'Benefits', 
    icon: '🎁',
    children: [
      { path: '/benefits/plans', label: 'Plans', icon: '📋' },
      { path: '/benefits/enrollments', label: 'Enrollments', icon: '✍️' }
    ]
  },
  { 
    path: '/training', 
    label: 'Training', 
    icon: '🎓',
    children: [
      { path: '/training/catalog', label: 'Catalog', icon: '📚' },
      { path: '/training/assignments', label: 'Assignments', icon: '📝' }
    ]
  },
  { path: '/org-chart', label: 'Org Chart', icon: '🏢' },
  { path: '/reports', label: 'Reports', icon: '📊' },
      { 
        path: '/settings', 
        label: 'Settings', 
        icon: '⚙️',
        children: [
          { path: '/settings/profile', label: 'Profile', icon: '👤' },
          { 
            path: '/settings/company', 
            label: 'Company', 
            icon: '🏢',
            children: [
              { path: '/settings/company/departments', label: 'Departments', icon: '🏛️' },
              { path: '/settings/company/designations', label: 'Designations', icon: '💼' }
            ]
          },
          { path: '/settings/users', label: 'Users', icon: '👥' },
          { path: '/settings/security', label: 'Security', icon: '🔒' }
        ]
      }
];

interface LayoutProps {
  children: React.ReactNode;
}

const Layout: React.FC<LayoutProps> = ({ children }) => {
  const { user, logout, tenantCode } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const [expandedMenus, setExpandedMenus] = useState<string[]>([]);
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);

  // Auto-expand parent menus when a child or grandchild route is active
  useEffect(() => {
    const activePaths: string[] = [];
    
    menuItems.forEach(item => {
      if (item.children) {
        // Check if any direct child is active
        const hasActiveChild = item.children.some(child => {
          if (location.pathname === child.path) {
            activePaths.push(item.path);
            return true;
          }
          // Check if any grandchild is active
          if (child.children) {
            const hasActiveGrandchild = child.children.some(grandchild => 
              location.pathname === grandchild.path
            );
            if (hasActiveGrandchild) {
              activePaths.push(item.path);
              activePaths.push(child.path);
              return true;
            }
          }
          return false;
        });
      }
    });
    
    setExpandedMenus(activePaths);
  }, [location.pathname]);

  const toggleMenu = (path: string) => {
    setExpandedMenus(prev => 
      prev.includes(path) 
        ? prev.filter(p => p !== path)
        : [...prev, path]
    );
  };

  const isChildActive = (path: string) => {
    const menuItem = menuItems.find(m => m.path === path);
    if (!menuItem?.children) return false;
    
    // Check if any child or grandchild is active
    return menuItem.children.some(child => {
      if (location.pathname === child.path) return true;
      if (child.children) {
        return child.children.some(grandchild => location.pathname === grandchild.path);
      }
      return false;
    });
  };

  const isActive = (path: string) => {
    return location.pathname === path || location.pathname.startsWith(path + '/');
  };

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <div className="layout">
      <aside className={`sidebar ${sidebarCollapsed ? 'collapsed' : ''}`}>
        <div className="sidebar-header">
          <div className="logo">
            <span className="logo-icon">🏢</span>
            {!sidebarCollapsed && <span className="logo-text">OPENSKY HRM</span>}
          </div>
          <button 
            className="collapse-btn"
            onClick={() => setSidebarCollapsed(!sidebarCollapsed)}
          >
            {sidebarCollapsed ? '→' : '←'}
          </button>
        </div>
        
        <nav className="sidebar-nav">
          {menuItems.map((item) => (
            <div key={item.path} className="nav-item-container">
              {item.children ? (
                <>
                  <button
                    className={`nav-item ${isActive(item.path) ? 'active' : ''}`}
                    onClick={() => toggleMenu(item.path)}
                  >
                    <span className="nav-icon">{item.icon}</span>
                    {!sidebarCollapsed && (
                      <>
                        <span className="nav-label">{item.label}</span>
                        <span className={`nav-arrow ${expandedMenus.includes(item.path) ? 'expanded' : ''}`}>
                          ▼
                        </span>
                      </>
                    )}
                  </button>
                  {!sidebarCollapsed && expandedMenus.includes(item.path) && (
                    <div className="nav-children">
                      {item.children.map((child) => (
                        <div key={child.path}>
                          {child.children ? (
                            <>
                              <button
                                className={`nav-child nav-child-parent ${isChildActive(child.path) ? 'active' : ''}`}
                                onClick={() => toggleMenu(child.path)}
                              >
                                <span className="nav-icon">{child.icon}</span>
                                <span className="nav-label">{child.label}</span>
                                <span className={`nav-arrow ${expandedMenus.includes(child.path) ? 'expanded' : ''}`}>
                                  ▶
                                </span>
                              </button>
                              {expandedMenus.includes(child.path) && (
                                <div className="nav-grandchildren">
                                  {child.children.map((grandchild) => (
                                    <Link
                                      key={grandchild.path}
                                      to={grandchild.path}
                                      className={`nav-grandchild ${location.pathname === grandchild.path ? 'active' : ''}`}
                                    >
                                      <span className="nav-icon">{grandchild.icon}</span>
                                      <span className="nav-label">{grandchild.label}</span>
                                    </Link>
                                  ))}
                                </div>
                              )}
                            </>
                          ) : (
                            <Link
                              to={child.path}
                              className={`nav-child ${location.pathname === child.path ? 'active' : ''}`}
                            >
                              <span className="nav-icon">{child.icon}</span>
                              <span className="nav-label">{child.label}</span>
                            </Link>
                          )}
                        </div>
                      ))}
                    </div>
                  )}
                </>
              ) : (
                <Link
                  to={item.path}
                  className={`nav-item ${isActive(item.path) ? 'active' : ''}`}
                >
                  <span className="nav-icon">{item.icon}</span>
                  {!sidebarCollapsed && <span className="nav-label">{item.label}</span>}
                </Link>
              )}
            </div>
          ))}
        </nav>
      </aside>

      <div className="main-container">
        <header className="header">
          <div className="header-left">
            <h1 className="page-title">
              {menuItems.find(m => isActive(m.path))?.label || 'Dashboard'}
            </h1>
            {tenantCode && (
              <span className="tenant-badge">{tenantCode}</span>
            )}
          </div>
          <div className="header-right">
            <div className="user-info">
              <span className="user-name">{user?.fullName || 'User'}</span>
              <span className="user-email">{user?.email}</span>
            </div>
            <button className="logout-btn" onClick={handleLogout}>
              Logout
            </button>
          </div>
        </header>

        <main className="main-content">
          {children}
        </main>
      </div>
    </div>
  );
};

export default Layout;
