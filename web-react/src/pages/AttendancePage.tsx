import React from 'react';
import './AttendancePage.css';

const AttendancePage: React.FC = () => {
  const todayRecords = [
    { name: 'John Smith', checkIn: '09:00 AM', checkOut: '06:15 PM', status: 'Present' },
    { name: 'Sarah Johnson', checkIn: '08:45 AM', checkOut: '05:30 PM', status: 'Present' },
    { name: 'Mike Brown', checkIn: '09:30 AM', checkOut: '-', status: 'Present' },
    { name: 'Emily Davis', checkIn: '-', checkOut: '-', status: 'Absent' },
  ];

  return (
    <div className="attendance-page">
      <div className="attendance-header">
        <div className="clock-card">
          <h3>Current Time</h3>
          <div className="current-time">{new Date().toLocaleTimeString()}</div>
          <div className="current-date">{new Date().toLocaleDateString('en-US', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}</div>
          <div className="clock-actions">
            <button className="clock-btn clock-in">🕐 Clock In</button>
            <button className="clock-btn clock-out">🕐 Clock Out</button>
          </div>
        </div>
        <div className="stats-cards">
          <div className="mini-stat">
            <span className="stat-number">142</span>
            <span className="stat-text">Present Today</span>
          </div>
          <div className="mini-stat">
            <span className="stat-number">8</span>
            <span className="stat-text">Absent</span>
          </div>
          <div className="mini-stat">
            <span className="stat-number">6</span>
            <span className="stat-text">On Leave</span>
          </div>
        </div>
      </div>

      <div className="attendance-table-container">
        <h3>Today's Attendance</h3>
        <table className="attendance-table">
          <thead>
            <tr>
              <th>Employee</th>
              <th>Check In</th>
              <th>Check Out</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {todayRecords.map((record, index) => (
              <tr key={index}>
                <td>{record.name}</td>
                <td>{record.checkIn}</td>
                <td>{record.checkOut}</td>
                <td>
                  <span className={`status-badge ${record.status.toLowerCase()}`}>
                    {record.status}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default AttendancePage;
