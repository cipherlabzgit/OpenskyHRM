import React from 'react';
import './SettingsPages.css';

const ProfileSettingsPage: React.FC = () => (
  <div className="settings-page">
    <div className="settings-card">
      <h2>Profile Settings</h2>
      <form className="settings-form">
        <div className="form-group">
          <label>Full Name</label>
          <input type="text" defaultValue="Admin User" />
        </div>
        <div className="form-group">
          <label>Email</label>
          <input type="email" defaultValue="admin@company.com" disabled />
        </div>
        <div className="form-group">
          <label>Phone</label>
          <input type="tel" placeholder="Enter phone number" />
        </div>
        <button type="submit" className="save-btn">Save Changes</button>
      </form>
    </div>
  </div>
);

export default ProfileSettingsPage;
