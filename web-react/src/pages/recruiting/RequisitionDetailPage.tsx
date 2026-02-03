import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import api from '../../services/api';
import './RecruitingPages.css';

const { tenant } = api;

interface RequisitionDetail {
  id: string;
  requisitionNumber: string;
  title: string;
  departmentId?: string;
  designationId?: string;
  location?: string;
  employmentType: string;
  openings: number;
  budgetMin?: number;
  budgetMax?: number;
  currency: string;
  description?: string;
  requirements?: string;
  responsibilities?: string;
  requiredSkills?: string;
  preferredSkills?: string;
  minExperienceYears?: number;
  maxExperienceYears?: number;
  educationLevel?: string;
  status: string;
  requestedById?: string;
  hiringManagerId?: string;
  targetStartDate?: string;
  createdAtUtc: string;
  updatedAtUtc?: string;
  department?: { id: string; name: string };
  designation?: { id: string; name: string };
  applicationCount: number;
}

const RequisitionDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [requisition, setRequisition] = useState<RequisitionDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [isEditing, setIsEditing] = useState(false);
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
    preferredSkills: '',
    minExperienceYears: '',
    maxExperienceYears: '',
    educationLevel: ''
  });
  const [departments, setDepartments] = useState<any[]>([]);
  const [designations, setDesignations] = useState<any[]>([]);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (id) {
      loadRequisition();
      loadDepartments();
      loadDesignations();
    }
  }, [id]);

  const loadRequisition = async () => {
    try {
      setLoading(true);
      // API method already extracts response.data, so req is the requisition object directly
      const req = await api.getJobRequisition(id!);
      setRequisition(req);
      
      // Populate form data
      setFormData({
        title: req.title || '',
        departmentId: req.departmentId || '',
        designationId: req.designationId || '',
        location: req.location || '',
        employmentType: req.employmentType || 'FullTime',
        openings: req.openings || 1,
        budgetMin: req.budgetMin?.toString() || '',
        budgetMax: req.budgetMax?.toString() || '',
        currency: req.currency || 'USD',
        description: req.description || '',
        requirements: req.requirements || '',
        responsibilities: req.responsibilities || '',
        requiredSkills: req.requiredSkills || '',
        preferredSkills: req.preferredSkills || '',
        minExperienceYears: req.minExperienceYears?.toString() || '',
        maxExperienceYears: req.maxExperienceYears?.toString() || '',
        educationLevel: req.educationLevel || ''
      });
    } catch (error: any) {
      console.error('Error loading requisition:', error);
      console.error('Error response:', error.response);
      const errorMessage = error.response?.data?.error || error.message || 'Failed to load requisition';
      alert(errorMessage);
      navigate('/recruiting/requisitions');
    } finally {
      setLoading(false);
    }
  };

  const loadDepartments = async () => {
    try {
      const response = await tenant.get('/departments');
      setDepartments(response.data || []);
    } catch (error) {
      console.error('Error loading departments:', error);
    }
  };

  const loadDesignations = async () => {
    try {
      const response = await tenant.get('/designations');
      setDesignations(response.data || []);
    } catch (error) {
      console.error('Error loading designations:', error);
    }
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!id) return;

    try {
      setSaving(true);
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
        preferredSkills: formData.preferredSkills.trim() || null,
        minExperienceYears: formData.minExperienceYears ? parseInt(formData.minExperienceYears) : null,
        maxExperienceYears: formData.maxExperienceYears ? parseInt(formData.maxExperienceYears) : null,
        educationLevel: formData.educationLevel.trim() || null,
        departmentId: formData.departmentId && formData.departmentId.trim() ? formData.departmentId : null,
        designationId: formData.designationId && formData.designationId.trim() ? formData.designationId : null
      };

      await api.updateJobRequisition(id, payload);
      setIsEditing(false);
      loadRequisition();
      alert('Requisition updated successfully');
    } catch (error: any) {
      console.error('Error updating requisition:', error);
      const errorMessage = error.response?.data?.error || error.message || 'Failed to update requisition';
      alert(errorMessage);
    } finally {
      setSaving(false);
    }
  };

  const handleSubmitForApproval = async () => {
    if (!id) return;
    if (!window.confirm('Submit this requisition for approval?')) return;

    try {
      await tenant.post(`/recruitment/requisitions/${id}/submit`);
      loadRequisition();
      alert('Requisition submitted for approval');
    } catch (error: any) {
      console.error('Error submitting requisition:', error);
      alert(error.response?.data?.error || 'Failed to submit requisition');
    }
  };

  const handlePublish = async () => {
    if (!id) return;
    if (!window.confirm('Publish this requisition? It will be visible on the career portal.')) return;

    try {
      await tenant.post(`/recruitment/requisitions/${id}/publish`);
      loadRequisition();
      alert('Requisition published successfully');
    } catch (error: any) {
      console.error('Error publishing requisition:', error);
      alert(error.response?.data?.error || 'Failed to publish requisition');
    }
  };

  const canEdit = requisition?.status === 'Draft' || requisition?.status === 'PendingApproval';

  if (loading) {
    return <div className="loading">Loading requisition...</div>;
  }

  if (!requisition) {
    return <div className="error">Requisition not found</div>;
  }

  return (
    <div className="recruiting-page">
      <div className="page-header">
        <div>
          <button className="btn-secondary" onClick={() => navigate('/recruiting/requisitions')}>
            ← Back to Requisitions
          </button>
          <h1>{requisition.requisitionNumber} - {requisition.title}</h1>
        </div>
        <div className="action-buttons">
          {canEdit && !isEditing && (
            <button className="btn-primary" onClick={() => setIsEditing(true)}>
              Edit
            </button>
          )}
          {isEditing && (
            <>
              <button className="btn-secondary" onClick={() => setIsEditing(false)}>
                Cancel
              </button>
              <button className="btn-primary" onClick={handleSave} disabled={saving}>
                {saving ? 'Saving...' : 'Save'}
              </button>
            </>
          )}
          {!isEditing && requisition.status === 'Draft' && (
            <button className="btn-primary" onClick={handleSubmitForApproval}>
              Submit for Approval
            </button>
          )}
          {!isEditing && requisition.status === 'Approved' && (
            <button className="btn-primary" onClick={handlePublish}>
              Publish
            </button>
          )}
        </div>
      </div>

      <div className="requisition-detail">
        {isEditing ? (
          <form onSubmit={handleSave} className="form">
            <div className="form-section">
              <h2>Basic Information</h2>
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
            </div>

            <div className="form-section">
              <h2>Budget</h2>
              <div className="form-row">
                <div className="form-group">
                  <label>Min Budget</label>
                  <input
                    type="number"
                    step="0.01"
                    value={formData.budgetMin}
                    onChange={(e) => setFormData({ ...formData, budgetMin: e.target.value })}
                  />
                </div>
                <div className="form-group">
                  <label>Max Budget</label>
                  <input
                    type="number"
                    step="0.01"
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
            </div>

            <div className="form-section">
              <h2>Job Details</h2>
              <div className="form-group">
                <label>Description</label>
                <textarea
                  rows={5}
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                />
              </div>
              <div className="form-group">
                <label>Requirements</label>
                <textarea
                  rows={5}
                  value={formData.requirements}
                  onChange={(e) => setFormData({ ...formData, requirements: e.target.value })}
                />
              </div>
              <div className="form-group">
                <label>Responsibilities</label>
                <textarea
                  rows={5}
                  value={formData.responsibilities}
                  onChange={(e) => setFormData({ ...formData, responsibilities: e.target.value })}
                />
              </div>
              <div className="form-group">
                <label>Required Skills</label>
                <input
                  type="text"
                  value={formData.requiredSkills}
                  onChange={(e) => setFormData({ ...formData, requiredSkills: e.target.value })}
                  placeholder="Comma-separated skills"
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
            </div>
          </form>
        ) : (
          <div className="detail-view">
            <div className="detail-section">
              <h2>Basic Information</h2>
              <div className="detail-grid">
                <div className="detail-item">
                  <label>Requisition Number</label>
                  <span>{requisition.requisitionNumber}</span>
                </div>
                <div className="detail-item">
                  <label>Title</label>
                  <span>{requisition.title}</span>
                </div>
                <div className="detail-item">
                  <label>Department</label>
                  <span>{requisition.department?.name || '-'}</span>
                </div>
                <div className="detail-item">
                  <label>Designation</label>
                  <span>{requisition.designation?.name || '-'}</span>
                </div>
                <div className="detail-item">
                  <label>Location</label>
                  <span>{requisition.location || '-'}</span>
                </div>
                <div className="detail-item">
                  <label>Employment Type</label>
                  <span>{requisition.employmentType}</span>
                </div>
                <div className="detail-item">
                  <label>Openings</label>
                  <span>{requisition.openings}</span>
                </div>
                <div className="detail-item">
                  <label>Status</label>
                  <span className="status-badge" style={{ backgroundColor: getStatusColor(requisition.status) }}>
                    {requisition.status}
                  </span>
                </div>
              </div>
            </div>

            {requisition.description && (
              <div className="detail-section">
                <h2>Description</h2>
                <p>{requisition.description}</p>
              </div>
            )}

            {requisition.requirements && (
              <div className="detail-section">
                <h2>Requirements</h2>
                <p>{requisition.requirements}</p>
              </div>
            )}

            {requisition.responsibilities && (
              <div className="detail-section">
                <h2>Responsibilities</h2>
                <p>{requisition.responsibilities}</p>
              </div>
            )}

            <div className="detail-section">
              <h2>Statistics</h2>
              <div className="detail-grid">
                <div className="detail-item">
                  <label>Applications</label>
                  <span>{requisition.applicationCount || 0}</span>
                </div>
                <div className="detail-item">
                  <label>Created</label>
                  <span>{new Date(requisition.createdAtUtc).toLocaleString()}</span>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

function getStatusColor(status: string): string {
  const colors: { [key: string]: string } = {
    Draft: '#94a3b8',
    PendingApproval: '#f59e0b',
    Approved: '#10b981',
    Published: '#3b82f6',
    Closed: '#6b7280',
    Cancelled: '#ef4444'
  };
  return colors[status] || '#94a3b8';
}

export default RequisitionDetailPage;
