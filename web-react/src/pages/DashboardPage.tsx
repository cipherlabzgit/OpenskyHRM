import React from 'react';
import './DashboardPage.css';

const DashboardPage: React.FC = () => {
  const stats = [
    { label: 'Total Employees', value: '156', icon: '👥', color: '#3b82f6' },
    { label: 'Present Today', value: '142', icon: '✅', color: '#22c55e' },
    { label: 'On Leave', value: '8', icon: '🏖️', color: '#f59e0b' },
    { label: 'Open Positions', value: '12', icon: '💼', color: '#8b5cf6' }
  ];

  const recentActivities = [
    { type: 'leave', message: 'John Smith requested annual leave', time: '2 hours ago' },
    { type: 'employee', message: 'New employee Sarah Johnson onboarded', time: '5 hours ago' },
    { type: 'attendance', message: 'Mike Brown clocked in late', time: 'Yesterday' },
    { type: 'performance', message: 'Q1 performance reviews completed', time: '2 days ago' }
  ];

  return (
    <div className="dashboard-page">
      <div className="stats-grid">
        {stats.map((stat, index) => (
          <div key={index} className="stat-card">
            <div className="stat-icon" style={{ background: `${stat.color}15`, color: stat.color }}>
              {stat.icon}
            </div>
            <div className="stat-info">
              <span className="stat-value">{stat.value}</span>
              <span className="stat-label">{stat.label}</span>
            </div>
          </div>
        ))}
      </div>

      <div className="dashboard-grid">
        <div className="dashboard-card">
          <h3>Recent Activity</h3>
          <div className="activity-list">
            {recentActivities.map((activity, index) => (
              <div key={index} className="activity-item">
                <div className="activity-dot"></div>
                <div className="activity-content">
                  <p className="activity-message">{activity.message}</p>
                  <span className="activity-time">{activity.time}</span>
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="dashboard-card">
          <h3>Quick Actions</h3>
          <div className="quick-actions">
            <button className="quick-action-btn">
              <span className="action-icon">👤</span>
              <span>Add Employee</span>
            </button>
            <button className="quick-action-btn">
              <span className="action-icon">📋</span>
              <span>Post Job</span>
            </button>
            <button className="quick-action-btn">
              <span className="action-icon">📊</span>
              <span>Run Report</span>
            </button>
            <button className="quick-action-btn">
              <span className="action-icon">⚙️</span>
              <span>Settings</span>
            </button>
          </div>
        </div>

        <div className="dashboard-card">
          <h3>Upcoming Events</h3>
          <div className="events-list">
            <div className="event-item">
              <div className="event-date">
                <span className="event-day">15</span>
                <span className="event-month">Feb</span>
              </div>
              <div className="event-info">
                <h4>Team Meeting</h4>
                <p>Quarterly planning session</p>
              </div>
            </div>
            <div className="event-item">
              <div className="event-date">
                <span className="event-day">20</span>
                <span className="event-month">Feb</span>
              </div>
              <div className="event-info">
                <h4>Training Workshop</h4>
                <p>Leadership development</p>
              </div>
            </div>
          </div>
        </div>

        <div className="dashboard-card">
          <h3>Attendance Overview</h3>
          <div className="attendance-summary">
            <div className="attendance-bar">
              <div className="bar-fill present" style={{ width: '91%' }}></div>
            </div>
            <div className="attendance-legend">
              <span className="legend-item">
                <span className="legend-dot present"></span>
                Present: 91%
              </span>
              <span className="legend-item">
                <span className="legend-dot absent"></span>
                Absent: 5%
              </span>
              <span className="legend-item">
                <span className="legend-dot leave"></span>
                On Leave: 4%
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default DashboardPage;
