import React, { useState, useEffect } from 'react';
import api from '../../services/api';
import './RecruitingPages.css';

interface Interview {
  id: string;
  applicationId: string;
  candidateName: string;
  interviewType: string;
  interviewRound: string;
  scheduledAt: string;
  durationMinutes: number;
  location: string;
  status: string;
  overallRating: number;
}

const InterviewsPage: React.FC = () => {
  const [interviews, setInterviews] = useState<Interview[]>([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [formData, setFormData] = useState({
    applicationId: '',
    interviewType: 'Phone',
    interviewRound: 'Round 1',
    scheduledAt: '',
    durationMinutes: 60,
    location: '',
    meetingLink: '',
    agenda: ''
  });
  const [applications, setApplications] = useState<any[]>([]);

  useEffect(() => {
    loadInterviews();
    loadApplications();
  }, []);

  const loadInterviews = async () => {
    try {
      setLoading(true);
      const response = await api.tenant.get('/recruitment/interviews');
      setInterviews(response.data || []);
    } catch (error) {
      console.error('Error loading interviews:', error);
    } finally {
      setLoading(false);
    }
  };

  const loadApplications = async () => {
    try {
      const response = await api.tenant.get('/recruitment/applications');
      setApplications(response.data.data || []);
    } catch (error) {
      console.error('Error loading applications:', error);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await api.tenant.post('/recruitment/interviews', {
        ...formData,
        scheduledAt: new Date(formData.scheduledAt).toISOString()
      });
      setShowModal(false);
      setFormData({
        applicationId: '',
        interviewType: 'Phone',
        interviewRound: 'Round 1',
        scheduledAt: '',
        durationMinutes: 60,
        location: '',
        meetingLink: '',
        agenda: ''
      });
      loadInterviews();
    } catch (error) {
      console.error('Error creating interview:', error);
      alert('Failed to schedule interview');
    }
  };

  const getStatusColor = (status: string) => {
    const colors: Record<string, string> = {
      Scheduled: '#2196F3',
      Confirmed: '#4CAF50',
      InProgress: '#FF9800',
      Completed: '#8BC34A',
      Cancelled: '#F44336',
      NoShow: '#9E9E9E'
    };
    return colors[status] || '#9E9E9E';
  };

  return (
    <div className="recruiting-page">
      <div className="page-header">
        <h1>Interview Management</h1>
        <button className="btn-primary" onClick={() => setShowModal(true)}>
          + Schedule Interview
        </button>
      </div>

      {loading ? (
        <div className="loading">Loading...</div>
      ) : (
        <div className="table-container">
          <table className="data-table">
            <thead>
              <tr>
                <th>Candidate</th>
                <th>Type</th>
                <th>Round</th>
                <th>Scheduled At</th>
                <th>Duration</th>
                <th>Location</th>
                <th>Status</th>
                <th>Rating</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {interviews.map((interview) => (
                <tr key={interview.id}>
                  <td>{interview.candidateName}</td>
                  <td>{interview.interviewType}</td>
                  <td>{interview.interviewRound}</td>
                  <td>{new Date(interview.scheduledAt).toLocaleString()}</td>
                  <td>{interview.durationMinutes} min</td>
                  <td>{interview.location || '-'}</td>
                  <td>
                    <span
                      className="status-badge"
                      style={{ backgroundColor: getStatusColor(interview.status) }}
                    >
                      {interview.status}
                    </span>
                  </td>
                  <td>{interview.overallRating || '-'}</td>
                  <td>
                    <button className="btn-sm btn-primary">View</button>
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
              <h2>Schedule Interview</h2>
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
                  <label>Interview Type *</label>
                  <select
                    required
                    value={formData.interviewType}
                    onChange={(e) => setFormData({ ...formData, interviewType: e.target.value })}
                  >
                    <option value="Phone">Phone</option>
                    <option value="Video">Video</option>
                    <option value="InPerson">In Person</option>
                    <option value="Technical">Technical</option>
                    <option value="HR">HR</option>
                    <option value="Panel">Panel</option>
                  </select>
                </div>

                <div className="form-group">
                  <label>Round</label>
                  <input
                    type="text"
                    value={formData.interviewRound}
                    onChange={(e) => setFormData({ ...formData, interviewRound: e.target.value })}
                  />
                </div>
              </div>

              <div className="form-row">
                <div className="form-group">
                  <label>Scheduled At *</label>
                  <input
                    type="datetime-local"
                    required
                    value={formData.scheduledAt}
                    onChange={(e) => setFormData({ ...formData, scheduledAt: e.target.value })}
                  />
                </div>

                <div className="form-group">
                  <label>Duration (minutes)</label>
                  <input
                    type="number"
                    min="15"
                    step="15"
                    value={formData.durationMinutes}
                    onChange={(e) => setFormData({ ...formData, durationMinutes: parseInt(e.target.value) || 60 })}
                  />
                </div>
              </div>

              <div className="form-group">
                <label>Location</label>
                <input
                  type="text"
                  value={formData.location}
                  onChange={(e) => setFormData({ ...formData, location: e.target.value })}
                />
              </div>

              <div className="form-group">
                <label>Meeting Link</label>
                <input
                  type="url"
                  value={formData.meetingLink}
                  onChange={(e) => setFormData({ ...formData, meetingLink: e.target.value })}
                  placeholder="https://..."
                />
              </div>

              <div className="form-group">
                <label>Agenda</label>
                <textarea
                  rows={4}
                  value={formData.agenda}
                  onChange={(e) => setFormData({ ...formData, agenda: e.target.value })}
                />
              </div>

              <div className="form-actions">
                <button type="button" className="btn-secondary" onClick={() => setShowModal(false)}>
                  Cancel
                </button>
                <button type="submit" className="btn-primary">
                  Schedule Interview
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default InterviewsPage;
