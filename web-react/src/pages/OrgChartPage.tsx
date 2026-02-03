import React from 'react';

const OrgChartPage: React.FC = () => (
  <div style={{ padding: '24px', background: 'white', borderRadius: '12px', textAlign: 'center' }}>
    <h2>Organization Chart</h2>
    <p style={{ color: '#64748b', marginBottom: '32px' }}>View your company's organizational structure.</p>
    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '24px' }}>
      <div style={{ padding: '16px 32px', background: 'linear-gradient(90deg, #dc2626, #1d4ed8)', color: 'white', borderRadius: '12px', fontWeight: '600' }}>
        CEO
      </div>
      <div style={{ width: '2px', height: '24px', background: '#e5e7eb' }}></div>
      <div style={{ display: 'flex', gap: '48px' }}>
        {['CTO', 'CFO', 'COO'].map((role, i) => (
          <div key={i} style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '16px' }}>
            <div style={{ padding: '12px 24px', background: '#3b82f6', color: 'white', borderRadius: '10px', fontWeight: '500' }}>{role}</div>
            <div style={{ display: 'flex', gap: '16px' }}>
              <div style={{ padding: '10px 16px', background: '#f1f5f9', borderRadius: '8px', fontSize: '14px' }}>Manager</div>
              <div style={{ padding: '10px 16px', background: '#f1f5f9', borderRadius: '8px', fontSize: '14px' }}>Manager</div>
            </div>
          </div>
        ))}
      </div>
    </div>
  </div>
);

export default OrgChartPage;
