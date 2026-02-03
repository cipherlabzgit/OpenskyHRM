import React from 'react';

const ConfigurationTab: React.FC = () => {
  return (
    <div className="configuration-tab">
      <div className="settings-container">
        <div className="settings-header">
          <h1>Recruitment Configuration</h1>
          <p>Configure recruitment settings, workflows, and templates</p>
        </div>

        <div className="settings-section">
          <h2>Workflow Settings</h2>
          <p>Configure approval workflows and stages</p>
        </div>

        <div className="settings-section">
          <h2>Email Templates</h2>
          <p>Manage email templates for candidate communications</p>
        </div>

        <div className="settings-section">
          <h2>Assessment Settings</h2>
          <p>Configure assessment types and scoring</p>
        </div>
      </div>
    </div>
  );
};

export default ConfigurationTab;
