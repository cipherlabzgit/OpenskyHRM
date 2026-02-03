import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../../services/api';
import VacancyWizard from './VacancyWizard';
import './RecruitingPages.css';

interface Requisition {
  id: string;
  requisitionNumber: string;
  title: string;
  department: string;
  designation: string;
  location: string;
  employmentType: string;
  openings: number;
  status: string;
  applicationCount: number;
  createdAtUtc: string;
}

const RequisitionsPage: React.FC = () => {
  const navigate = useNavigate();
  const [requisitions, setRequisitions] = useState<Requisition[]>([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState<string>('all');
  const [showWizard, setShowWizard] = useState(false);
  const [editingRequisitionId, setEditingRequisitionId] = useState<string | undefined>();
  const [formData, setFormData] = useState({
    title: '',
    departmentId: '',
    designationId: '',
    location: '',
    employmentType: 'FullTime',
    openings: 1,
    budgetMin: '',
    budgetMax: '',
    currency: 'USD',
    description: '',
    requirements: '',
    responsibilities: '',
    requiredSkills: '',
    minExperienceYears: '',
    maxExperienceYears: ''
  });
  const [departments, setDepartments] = useState<any[]>([]);
  const [designations, setDesignations] = useState<any[]>([]);

  useEffect(() => {
    loadRequisitions();
    loadDepartments();
    loadDesignations();
  }, [filter]);

  const loadRequisitions = async () => {
    try {
      setLoading(true);
      const status = filter !== 'all' ? filter : undefined;
      const response = await api.tenant.get(`/recruitment/requisitions?status=${status || ''}`);
      setRequisitions(response.data.data || []);
    } catch (error) {
      console.error('Error loading requisitions:', error);
    } finally {
      setLoading(false);
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

  const loadDesignations = async () => {
    try {
      const response = await api.tenant.get('/designations');
      setDesignations(response.data || []);
    } catch (error) {
      console.error('Error loading designations:', error);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    // Validate required fields
    if (!formData.title.trim()) {
      alert('Title is required');
      return;
    }
    
    try {
      // Convert empty strings to null for optional fields
      const payload: any = {
        title: formData.title.trim(),
        location: formData.location.trim() || null,
        employmentType: formData.employmentType,
        openings: parseInt(formData.openings.toString()) || 1,
        budgetMin: formData.budgetMin ? parseFloat(formData.budgetMin) : null,
        budgetMax: formData.budgetMax ? parseFloat(formData.budgetMax) : null,
        currency: formData.currency,
        description: formData.description.trim() || null,
        requirements: formData.requirements.trim() || null,
        responsibilities: formData.responsibilities.trim() || null,
        requiredSkills: formData.requiredSkills.trim() || null,
        minExperienceYears: formData.minExperienceYears ? parseInt(formData.minExperienceYears) : null,
        maxExperienceYears: formData.maxExperienceYears ? parseInt(formData.maxExperienceYears) : null,
        departmentId: formData.departmentId && formData.departmentId.trim() ? formData.departmentId : null,
        designationId: formData.designationId && formData.designationId.trim() ? formData.designationId : null
      };
      
      console.log('Submitting requisition with payload:', JSON.stringify(payload, null, 2));
      
      const response = await api.tenant.post('/recruitment/requisitions', payload);
      console.log('Requisition created successfully:', response.data);
      setShowModal(false);
      setFormData({
        title: '',
        departmentId: '',
        designationId: '',
        location: '',
        employmentType: 'FullTime',
        openings: 1,
        budgetMin: '',
        budgetMax: '',
        currency: 'USD',
        description: '',
        requirements: '',
        responsibilities: '',
        requiredSkills: '',
        minExperienceYears: '',
        maxExperienceYears: ''
      });
      loadRequisitions();
    } catch (error: any) {
      console.error('Error creating requisition:', error);
      console.error('Error response:', error.response);
      console.error('Error response data:', error.response?.data);
      
      let errorMessage = 'Failed to create requisition';
      
      if (error.response?.data) {
        if (typeof error.response.data === 'string') {
          errorMessage = error.response.data;
        } else if (error.response.data.error) {
          errorMessage = error.response.data.error;
        } else if (error.response.data.message) {
          errorMessage = error.response.data.message;
        } else {
          errorMessage = JSON.stringify(error.response.data);
        }
      } else if (error.message) {
        errorMessage = error.message;
      }
      
      alert(`Error: ${errorMessage}\n\nStatus: ${error.response?.status || 'Unknown'}\n\nCheck browser console for details.`);
    }
  };

  const handleSubmitForApproval = async (id: string) => {
    try {
      await api.tenant.post(`/recruitment/requisitions/${id}/submit`);
      loadRequisitions();
    } catch (error) {
      console.error('Error submitting requisition:', error);
      alert('Failed to submit requisition');
    }
  };

  const handlePublish = async (id: string) => {
    try {
      await api.tenant.post(`/recruitment/requisitions/${id}/publish`);
      loadRequisitions();
    } catch (error) {
      console.error('Error publishing requisition:', error);
      alert('Failed to publish requisition');
    }
  };

  const getStatusColor = (status: string) => {
    const colors: Record<string, string> = {
      Draft: '#9E9E9E',
      PendingApproval: '#FF9800',
      Approved: '#4CAF50',
      Published: '#2196F3',
      Closed: '#F44336',
      OnHold: '#FFC107',
      Cancelled: '#9E9E9E'
    };
    return colors[status] || '#9E9E9E';
  };

  return (
    <div className="recruiting-page">
      <div className="page-header">
        <h1>Job Requisitions</h1>
        <button className="btn-primary" onClick={() => setShowWizard(true)}>
          + Create Requisition
        </button>
      </div>

      <div className="filters">
        <button
          className={filter === 'all' ? 'filter-active' : ''}
          onClick={() => setFilter('all')}
        >
          All
        </button>
        <button
          className={filter === 'Draft' ? 'filter-active' : ''}
          onClick={() => setFilter('Draft')}
        >
          Draft
        </button>
        <button
          className={filter === 'PendingApproval' ? 'filter-active' : ''}
          onClick={() => setFilter('PendingApproval')}
        >
          Pending Approval
        </button>
        <button
          className={filter === 'Published' ? 'filter-active' : ''}
          onClick={() => setFilter('Published')}
        >
          Published
        </button>
        <button
          className={filter === 'Closed' ? 'filter-active' : ''}
          onClick={() => setFilter('Closed')}
        >
          Closed
        </button>
      </div>

      {loading ? (
        <div className="loading">Loading...</div>
      ) : (
        <div className="table-container">
          <table className="data-table">
            <thead>
              <tr>
                <th>Requisition #</th>
                <th>Title</th>
                <th>Department</th>
                <th>Location</th>
                <th>Openings</th>
                <th>Applications</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {requisitions.map((req) => (
                <tr key={req.id}>
                  <td>{req.requisitionNumber}</td>
                  <td>
                    <a
                      href="#"
                      onClick={(e) => {
                        e.preventDefault();
                        navigate(`/recruiting/requisitions/${req.id}`);
                      }}
                    >
                      {req.title}
                    </a>
                  </td>
                  <td>{req.department}</td>
                  <td>{req.location}</td>
                  <td>{req.openings}</td>
                  <td>{req.applicationCount}</td>
                  <td>
                    <span
                      className="status-badge"
                      style={{ backgroundColor: getStatusColor(req.status) }}
                    >
                      {req.status}
                    </span>
                  </td>
                  <td>
                    <div className="action-buttons">
                      {req.status === 'Draft' && (
                        <button
                          className="btn-sm btn-secondary"
                          onClick={() => handleSubmitForApproval(req.id)}
                        >
                          Submit
                        </button>
                      )}
                      {req.status === 'Approved' && (
                        <button
                          className="btn-sm btn-primary"
                          onClick={() => handlePublish(req.id)}
                        >
                          Publish
                        </button>
                      )}
                      <button
                        className="btn-sm btn-secondary"
                        onClick={() => navigate(`/recruiting/requisitions/${req.id}`)}
                      >
                        View
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {false && (
        <div className="modal-overlay" onClick={() => setShowWizard(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>Create Job Requisition</h2>
              <button className="modal-close" onClick={() => setShowWizard(false)}>×</button>
            </div>
            <form onSubmit={handleSubmit} className="form">
              <div className="form-group">
                <label>Title *</label>
                <input
                  type="text"
                  required
                  value={formData.title}
                  onChange={(e) => setFormData({ ...formData, title: e.target.value })}
                />
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
                  <label>Location</label>
                  <input
                    type="text"
                    value={formData.location}
                    onChange={(e) => setFormData({ ...formData, location: e.target.value })}
                  />
                </div>

                <div className="form-group">
                  <label>Employment Type</label>
                  <select
                    value={formData.employmentType}
                    onChange={(e) => setFormData({ ...formData, employmentType: e.target.value })}
                  >
                    <option value="FullTime">Full Time</option>
                    <option value="PartTime">Part Time</option>
                    <option value="Contract">Contract</option>
                  </select>
                </div>

                <div className="form-group">
                  <label>Openings</label>
                  <input
                    type="number"
                    min="1"
                    value={formData.openings}
                    onChange={(e) => setFormData({ ...formData, openings: parseInt(e.target.value) || 1 })}
                  />
                </div>
              </div>

              <div className="form-row">
                <div className="form-group">
                  <label>Budget Min</label>
                  <input
                    type="number"
                    value={formData.budgetMin}
                    onChange={(e) => setFormData({ ...formData, budgetMin: e.target.value })}
                  />
                </div>

                <div className="form-group">
                  <label>Budget Max</label>
                  <input
                    type="number"
                    value={formData.budgetMax}
                    onChange={(e) => setFormData({ ...formData, budgetMax: e.target.value })}
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

              <div className="form-group">
                <label>Description</label>
                <textarea
                  rows={4}
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                />
              </div>

              <div className="form-group">
                <label>Requirements</label>
                <textarea
                  rows={4}
                  value={formData.requirements}
                  onChange={(e) => setFormData({ ...formData, requirements: e.target.value })}
                />
              </div>

              <div className="form-group">
                <label>Required Skills</label>
                <input
                  type="text"
                  placeholder="Comma-separated skills"
                  value={formData.requiredSkills}
                  onChange={(e) => setFormData({ ...formData, requiredSkills: e.target.value })}
                />
              </div>

              <div className="form-row">
                <div className="form-group">
                  <label>Min Experience (Years)</label>
                  <input
                    type="number"
                    min="0"
                    value={formData.minExperienceYears}
                    onChange={(e) => setFormData({ ...formData, minExperienceYears: e.target.value })}
                  />
                </div>

                <div className="form-group">
                  <label>Max Experience (Years)</label>
                  <input
                    type="number"
                    min="0"
                    value={formData.maxExperienceYears}
                    onChange={(e) => setFormData({ ...formData, maxExperienceYears: e.target.value })}
                  />
                </div>
              </div>

              <div className="form-actions">
                <button type="button" className="btn-secondary" onClick={() => setShowModal(false)}>
                  Cancel
                </button>
                <button type="submit" className="btn-primary">
                  Create Requisition
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default RequisitionsPage;
