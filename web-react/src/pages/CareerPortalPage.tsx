import React, { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import api from '../services/api';
import './CareerPortalPage.css';

interface JobRequisition {
  id: string;
  requisitionNumber: string;
  title: string;
  department: string;
  location: string;
  employmentType: string;
  description: string;
  requirements: string;
  budgetMin: number;
  budgetMax: number;
  currency: string;
  createdAtUtc: string;
}

const CareerPortalPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const tenantCode = searchParams.get('tenant') || '';
  const [jobs, setJobs] = useState<JobRequisition[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedJob, setSelectedJob] = useState<JobRequisition | null>(null);
  const [showApplyModal, setShowApplyModal] = useState(false);
  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    coverLetter: '',
    resume: null as File | null
  });

  useEffect(() => {
    if (tenantCode) {
      loadJobs();
    }
  }, [tenantCode]);

  const loadJobs = async () => {
    try {
      setLoading(true);
      // In production, this would be a public endpoint
      // For now, we'll use the authenticated endpoint with tenant code from URL
      if (!tenantCode) {
        console.error('Tenant code is required');
        return;
      }
      
      const response = await api.tenant.get('/recruitment/requisitions', {
        params: { status: 'Published' },
        headers: { 'X-Tenant-Code': tenantCode }
      });
      setJobs(response.data.data || []);
    } catch (error) {
      console.error('Error loading jobs:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleApply = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedJob) return;

    try {
      // In production, handle file upload separately
      await api.tenant.post('/recruitment/applications', {
        requisitionId: selectedJob.id,
        firstName: formData.firstName,
        lastName: formData.lastName,
        email: formData.email,
        phone: formData.phone,
        coverLetter: formData.coverLetter,
        source: 'Career Portal'
      });

      alert('Application submitted successfully!');
      setShowApplyModal(false);
      setFormData({
        firstName: '',
        lastName: '',
        email: '',
        phone: '',
        coverLetter: '',
        resume: null
      });
    } catch (error) {
      console.error('Error submitting application:', error);
      alert('Failed to submit application. Please try again.');
    }
  };

  if (loading) {
    return <div className="career-portal-loading">Loading job openings...</div>;
  }

  return (
    <div className="career-portal">
      <div className="career-header">
        <h1>Join Our Team</h1>
        <p>Explore exciting career opportunities</p>
      </div>

      <div className="jobs-grid">
        {jobs.map((job) => (
          <div key={job.id} className="job-card">
            <div className="job-card-header">
              <h3>{job.title}</h3>
              <span className="job-badge">{job.employmentType}</span>
            </div>
            <div className="job-card-body">
              <div className="job-meta">
                <span>📍 {job.location}</span>
                <span>🏢 {job.department}</span>
              </div>
              {job.budgetMin && job.budgetMax && (
                <div className="job-salary">
                  {job.currency} {job.budgetMin.toLocaleString()} - {job.budgetMax.toLocaleString()}
                </div>
              )}
              <p className="job-description">
                {job.description?.substring(0, 150)}...
              </p>
            </div>
            <div className="job-card-footer">
              <button
                className="btn-view"
                onClick={() => setSelectedJob(job)}
              >
                View Details
              </button>
              <button
                className="btn-apply"
                onClick={() => {
                  setSelectedJob(job);
                  setShowApplyModal(true);
                }}
              >
                Apply Now
              </button>
            </div>
          </div>
        ))}
      </div>

      {selectedJob && !showApplyModal && (
        <div className="modal-overlay" onClick={() => setSelectedJob(null)}>
          <div className="modal-content job-details" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>{selectedJob.title}</h2>
              <button className="modal-close" onClick={() => setSelectedJob(null)}>×</button>
            </div>
            <div className="job-details-content">
              <div className="job-details-meta">
                <div><strong>Location:</strong> {selectedJob.location}</div>
                <div><strong>Department:</strong> {selectedJob.department}</div>
                <div><strong>Employment Type:</strong> {selectedJob.employmentType}</div>
                {selectedJob.budgetMin && selectedJob.budgetMax && (
                  <div><strong>Salary Range:</strong> {selectedJob.currency} {selectedJob.budgetMin.toLocaleString()} - {selectedJob.budgetMax.toLocaleString()}</div>
                )}
              </div>
              <div className="job-details-section">
                <h3>Job Description</h3>
                <p>{selectedJob.description}</p>
              </div>
              {selectedJob.requirements && (
                <div className="job-details-section">
                  <h3>Requirements</h3>
                  <p>{selectedJob.requirements}</p>
                </div>
              )}
              <div className="job-details-actions">
                <button
                  className="btn-apply"
                  onClick={() => {
                    setShowApplyModal(true);
                  }}
                >
                  Apply for this Position
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {showApplyModal && selectedJob && (
        <div className="modal-overlay" onClick={() => setShowApplyModal(false)}>
          <div className="modal-content apply-form" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>Apply for {selectedJob.title}</h2>
              <button className="modal-close" onClick={() => setShowApplyModal(false)}>×</button>
            </div>
            <form onSubmit={handleApply} className="form">
              <div className="form-row">
                <div className="form-group">
                  <label>First Name *</label>
                  <input
                    type="text"
                    required
                    value={formData.firstName}
                    onChange={(e) => setFormData({ ...formData, firstName: e.target.value })}
                  />
                </div>
                <div className="form-group">
                  <label>Last Name *</label>
                  <input
                    type="text"
                    required
                    value={formData.lastName}
                    onChange={(e) => setFormData({ ...formData, lastName: e.target.value })}
                  />
                </div>
              </div>

              <div className="form-row">
                <div className="form-group">
                  <label>Email *</label>
                  <input
                    type="email"
                    required
                    value={formData.email}
                    onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                  />
                </div>
                <div className="form-group">
                  <label>Phone</label>
                  <input
                    type="tel"
                    value={formData.phone}
                    onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
                  />
                </div>
              </div>

              <div className="form-group">
                <label>Resume *</label>
                <input
                  type="file"
                  accept=".pdf,.doc,.docx"
                  required
                  onChange={(e) => setFormData({ ...formData, resume: e.target.files?.[0] || null })}
                />
              </div>

              <div className="form-group">
                <label>Cover Letter</label>
                <textarea
                  rows={6}
                  value={formData.coverLetter}
                  onChange={(e) => setFormData({ ...formData, coverLetter: e.target.value })}
                  placeholder="Tell us why you're interested in this position..."
                />
              </div>

              <div className="form-actions">
                <button type="button" className="btn-secondary" onClick={() => setShowApplyModal(false)}>
                  Cancel
                </button>
                <button type="submit" className="btn-primary">
                  Submit Application
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default CareerPortalPage;
