import React from 'react';

const ReportsPage: React.FC = () => (
  <div style={{ padding: '24px', background: 'white', borderRadius: '12px' }}>
    <h2>Reports & Analytics</h2>
    <p style={{ color: '#64748b' }}>Generate and view HR reports and analytics.</p>
    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '16px', marginTop: '24px' }}>
      {['Employee Report', 'Attendance Report', 'Leave Report', 'Payroll Report'].map((report, i) => (
        <button key={i} style={{
          padding: '20px',
          background: '#f8fafc',
          border: '2px solid #e5e7eb',
          borderRadius: '12px',
          cursor: 'pointer',
          textAlign: 'left'
        }}>
          <div style={{ fontSize: '24px', marginBottom: '8px' }}>📊</div>
          <div style={{ fontWeight: '600' }}>{report}</div>
        </button>
      ))}
    </div>
  </div>
);

export default ReportsPage;
