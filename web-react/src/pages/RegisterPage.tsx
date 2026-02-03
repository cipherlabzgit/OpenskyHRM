import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import api from '../services/api';
import './RegisterPage.css';

const RegisterPage: React.FC = () => {
  const [formData, setFormData] = useState({
    companyName: '',
    legalName: '',
    country: '',
    timeZone: 'UTC',
    currency: 'USD',
    adminEmail: '',
    adminPassword: '',
    confirmPassword: '',
    adminFullName: ''
  });
  const [error, setError] = useState('');
  const [success, setSuccess] = useState<{ tenantCode: string; message: string } | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const navigate = useNavigate();

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    setFormData(prev => ({
      ...prev,
      [e.target.name]: e.target.value
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (formData.adminPassword !== formData.confirmPassword) {
      setError('Passwords do not match');
      return;
    }

    if (formData.adminPassword.length < 8) {
      setError('Password must be at least 8 characters');
      return;
    }

    setIsLoading(true);

    try {
      const response = await api.registerTenant({
        companyName: formData.companyName,
        legalName: formData.legalName || formData.companyName,
        country: formData.country,
        timeZone: formData.timeZone,
        currency: formData.currency,
        adminEmail: formData.adminEmail,
        adminPassword: formData.adminPassword,
        adminFullName: formData.adminFullName
      });

      setSuccess({
        tenantCode: response.tenantCode,
        message: response.message || 'Registration successful!'
      });
    } catch (err: any) {
      console.error('Registration error:', err);
      setError(err.response?.data?.error || 'Registration failed. Please try again.');
    } finally {
      setIsLoading(false);
    }
  };

  if (success) {
    return (
      <div className="register-page">
        <div className="register-container success-container">
          <div className="success-icon">✅</div>
          <h1>Registration Successful!</h1>
          <p>{success.message}</p>
          <div className="tenant-code-box">
            <label>Your Tenant Code:</label>
            <div className="tenant-code">{success.tenantCode}</div>
            <p className="tenant-note">Save this code! You'll need it to log in.</p>
          </div>
          <button 
            className="login-redirect-btn"
            onClick={() => navigate(`/login?tenant=${success.tenantCode}`)}
          >
            Go to Login
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="register-page">
      <div className="register-container">
        <div className="register-header">
          <div className="logo">
            <span className="logo-icon">🏢</span>
            <span className="logo-text">OPENSKY HRM</span>
          </div>
          <h1>Register Your Company</h1>
          <p>Create your HR management account</p>
        </div>

        <form onSubmit={handleSubmit} className="register-form">
          {error && <div className="error-message">{error}</div>}
          
          <div className="form-section">
            <h3>Company Information</h3>
            <div className="form-group">
              <label htmlFor="companyName">Company Name *</label>
              <input
                type="text"
                id="companyName"
                name="companyName"
                value={formData.companyName}
                onChange={handleChange}
                required
              />
            </div>
            <div className="form-group">
              <label htmlFor="legalName">Legal Name</label>
              <input
                type="text"
                id="legalName"
                name="legalName"
                value={formData.legalName}
                onChange={handleChange}
              />
            </div>
            <div className="form-row">
              <div className="form-group">
                <label htmlFor="country">Country</label>
                <input
                  type="text"
                  id="country"
                  name="country"
                  value={formData.country}
                  onChange={handleChange}
                />
              </div>
              <div className="form-group">
                <label htmlFor="currency">Currency</label>
                <select
                  id="currency"
                  name="currency"
                  value={formData.currency}
                  onChange={handleChange}
                >
                  <option value="USD">USD</option>
                  <option value="EUR">EUR</option>
                  <option value="GBP">GBP</option>
                  <option value="LKR">LKR</option>
                </select>
              </div>
            </div>
          </div>

          <div className="form-section">
            <h3>Admin Account</h3>
            <div className="form-group">
              <label htmlFor="adminFullName">Full Name</label>
              <input
                type="text"
                id="adminFullName"
                name="adminFullName"
                value={formData.adminFullName}
                onChange={handleChange}
              />
            </div>
            <div className="form-group">
              <label htmlFor="adminEmail">Email *</label>
              <input
                type="email"
                id="adminEmail"
                name="adminEmail"
                value={formData.adminEmail}
                onChange={handleChange}
                required
              />
            </div>
            <div className="form-group">
              <label htmlFor="adminPassword">Password *</label>
              <input
                type="password"
                id="adminPassword"
                name="adminPassword"
                value={formData.adminPassword}
                onChange={handleChange}
                required
                minLength={8}
              />
            </div>
            <div className="form-group">
              <label htmlFor="confirmPassword">Confirm Password *</label>
              <input
                type="password"
                id="confirmPassword"
                name="confirmPassword"
                value={formData.confirmPassword}
                onChange={handleChange}
                required
              />
            </div>
          </div>

          <button type="submit" className="register-btn" disabled={isLoading}>
            {isLoading ? 'Registering...' : 'Register Company'}
          </button>
        </form>

        <div className="register-footer">
          <p>Already have an account? <Link to="/login">Sign in</Link></p>
        </div>
      </div>
    </div>
  );
};

export default RegisterPage;
