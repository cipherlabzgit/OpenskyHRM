import React, { useState, useEffect } from 'react';
import api from '../../services/api';
import './RecruitingPages.css';

interface Offer {
  id: string;
  offerNumber: string;
  applicationId: string;
  candidateName: string;
  baseSalary: number;
  currency: string;
  joiningDate: string;
  status: string;
  createdAtUtc: string;
}

const OffersPage: React.FC = () => {
  const [offers, setOffers] = useState<Offer[]>([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [formData, setFormData] = useState({
    applicationId: '',
    designationId: '',
    departmentId: '',
    baseSalary: '',
    currency: 'USD',
    joiningDate: '',
    expiryDate: '',
    notes: ''
  });
  const [applications, setApplications] = useState<any[]>([]);
  const [designations, setDesignations] = useState<any[]>([]);
  const [departments, setDepartments] = useState<any[]>([]);

  useEffect(() => {
    loadOffers();
    loadApplications();
    loadDesignations();
    loadDepartments();
  }, []);

  const loadOffers = async () => {
    try {
      setLoading(true);
      const response = await api.tenant.get('/recruitment/offers');
      setOffers(response.data || []);
    } catch (error) {
      console.error('Error loading offers:', error);
    } finally {
      setLoading(false);
    }
  };

  const loadApplications = async () => {
    try {
      const response = await api.tenant.get('/recruitment/applications?stage=Interview');
      setApplications(response.data.data || []);
    } catch (error) {
      console.error('Error loading applications:', error);
    }
  };

  const loadDesignations = async () => {
    try {
      const response = await api.tenant.get('/designations');
      setDesignations(response.data || []);
    } catch (error) {
      console.error('Error loading designations:', error);
    }
  };

  const loadDepartments = async () => {
    try {
      const response = await api.tenant.get('/departments');
      setDepartments(response.data || []);
    } catch (error) {
      console.error('Error loading departments:', error);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await api.tenant.post('/recruitment/offers', {
        ...formData,
        baseSalary: parseFloat(formData.baseSalary),
        joiningDate: formData.joiningDate ? new Date(formData.joiningDate).toISOString() : null,
        expiryDate: formData.expiryDate ? new Date(formData.expiryDate).toISOString() : null,
        designationId: formData.designationId || null,
        departmentId: formData.departmentId || null
      });
      setShowModal(false);
      loadOffers();
    } catch (error) {
      console.error('Error creating offer:', error);
      alert('Failed to create offer');
    }
  };

  const handleSendOffer = async (id: string) => {
    try {
      await api.tenant.post(`/recruitment/offers/${id}/send`);
      loadOffers();
    } catch (error) {
      console.error('Error sending offer:', error);
      alert('Failed to send offer');
    }
  };

  const handleConvertToEmployee = async (id: string) => {
    if (!window.confirm('Convert this offer to an employee record?')) return;
    
    try {
      const response = await api.tenant.post(`/recruitment/offers/${id}/convert-to-employee`);
      alert(`Employee created successfully! Code: ${response.data.employeeCode}`);
      loadOffers();
    } catch (error) {
      console.error('Error converting to employee:', error);
      alert('Failed to convert to employee');
    }
  };

  const getStatusColor = (status: string) => {
    const colors: Record<string, string> = {
      Draft: '#9E9E9E',
      PendingApproval: '#FF9800',
      Approved: '#4CAF50',
      Sent: '#2196F3',
      Accepted: '#8BC34A',
      Rejected: '#F44336',
      Expired: '#9E9E9E',
      Converted: '#4CAF50'
    };
    return colors[status] || '#9E9E9E';
  };

  return (
    <div className="recruiting-page">
      <div className="page-header">
        <h1>Offer Management</h1>
        <button className="btn-primary" onClick={() => setShowModal(true)}>
          + Create Offer
        </button>
      </div>

      {loading ? (
        <div className="loading">Loading...</div>
      ) : (
        <div className="table-container">
          <table className="data-table">
            <thead>
              <tr>
                <th>Offer #</th>
                <th>Candidate</th>
                <th>Salary</th>
                <th>Joining Date</th>
                <th>Status</th>
                <th>Created</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {offers.map((offer) => (
                <tr key={offer.id}>
                  <td>{offer.offerNumber}</td>
                  <td>{offer.candidateName}</td>
                  <td>{offer.currency} {offer.baseSalary.toLocaleString()}</td>
                  <td>{offer.joiningDate ? new Date(offer.joiningDate).toLocaleDateString() : '-'}</td>
                  <td>
                    <span
                      className="status-badge"
                      style={{ backgroundColor: getStatusColor(offer.status) }}
                    >
                      {offer.status}
                    </span>
                  </td>
                  <td>{new Date(offer.createdAtUtc).toLocaleDateString()}</td>
                  <td>
                    <div className="action-buttons">
                      {offer.status === 'Draft' && (
                        <button
                          className="btn-sm btn-primary"
                          onClick={() => handleSendOffer(offer.id)}
                        >
                          Send
                        </button>
                      )}
                      {offer.status === 'Accepted' && (
                        <button
                          className="btn-sm btn-success"
                          onClick={() => handleConvertToEmployee(offer.id)}
                        >
                          Convert to Employee
                        </button>
                      )}
                      <button className="btn-sm btn-secondary">View</button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {showModal && (
        <div className="modal-overlay" onClick={() => setShowModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>Create Offer</h2>
              <button className="modal-close" onClick={() => setShowModal(false)}>×</button>
            </div>
            <form onSubmit={handleSubmit} className="form">
              <div className="form-group">
                <label>Application *</label>
                <select
                  required
                  value={formData.applicationId}
                  onChange={(e) => setFormData({ ...formData, applicationId: e.target.value })}
                >
                  <option value="">Select Application</option>
                  {applications.map((app) => (
                    <option key={app.id} value={app.id}>
                      {app.candidateName} - {app.jobTitle}
                    </option>
                  ))}
                </select>
              </div>

              <div className="form-row">
                <div className="form-group">
                  <label>Department</label>
                  <select
                    value={formData.departmentId}
                    onChange={(e) => setFormData({ ...formData, departmentId: e.target.value })}
                  >
                    <option value="">Select Department</option>
                    {departments.map((dept) => (
                      <option key={dept.id} value={dept.id}>
                        {dept.name}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="form-group">
                  <label>Designation</label>
                  <select
                    value={formData.designationId}
                    onChange={(e) => setFormData({ ...formData, designationId: e.target.value })}
                  >
                    <option value="">Select Designation</option>
                    {designations.map((des) => (
                      <option key={des.id} value={des.id}>
                        {des.name}
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              <div className="form-row">
                <div className="form-group">
                  <label>Base Salary *</label>
                  <input
                    type="number"
                    required
                    step="0.01"
                    value={formData.baseSalary}
                    onChange={(e) => setFormData({ ...formData, baseSalary: e.target.value })}
                  />
                </div>

                <div className="form-group">
                  <label>Currency</label>
                  <select
                    value={formData.currency}
                    onChange={(e) => setFormData({ ...formData, currency: e.target.value })}
                  >
                    <option value="USD">USD</option>
                    <option value="EUR">EUR</option>
                    <option value="GBP">GBP</option>
                  </select>
                </div>
              </div>

              <div className="form-row">
                <div className="form-group">
                  <label>Joining Date</label>
                  <input
                    type="date"
                    value={formData.joiningDate}
                    onChange={(e) => setFormData({ ...formData, joiningDate: e.target.value })}
                  />
                </div>

                <div className="form-group">
                  <label>Expiry Date</label>
                  <input
                    type="date"
                    value={formData.expiryDate}
                    onChange={(e) => setFormData({ ...formData, expiryDate: e.target.value })}
                  />
                </div>
              </div>

              <div className="form-group">
                <label>Notes</label>
                <textarea
                  rows={4}
                  value={formData.notes}
                  onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
                />
              </div>

              <div className="form-actions">
                <button type="button" className="btn-secondary" onClick={() => setShowModal(false)}>
                  Cancel
                </button>
                <button type="submit" className="btn-primary">
                  Create Offer
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default OffersPage;
