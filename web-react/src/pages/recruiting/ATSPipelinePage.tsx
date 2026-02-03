import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../../services/api';
import './RecruitingPages.css';

interface Application {
  id: string;
  candidateName: string;
  candidateEmail: string;
  jobTitle: string;
  stage: string;
  status: string;
  appliedAt: string;
  interviewCount: number;
  offerCount: number;
}

const stages = [
  { id: 'Applied', label: 'Applied', color: '#2196F3' },
  { id: 'Screening', label: 'Screening', color: '#FF9800' },
  { id: 'Shortlisted', label: 'Shortlisted', color: '#9C27B0' },
  { id: 'Interview', label: 'Interview', color: '#00BCD4' },
  { id: 'Assessment', label: 'Assessment', color: '#FFC107' },
  { id: 'Offered', label: 'Offered', color: '#4CAF50' },
  { id: 'Hired', label: 'Hired', color: '#8BC34A' },
  { id: 'Rejected', label: 'Rejected', color: '#F44336' }
];

const ATSPipelinePage: React.FC = () => {
  const navigate = useNavigate();
  const [applications, setApplications] = useState<Application[]>([]);
  const [loading, setLoading] = useState(true);
  const [viewMode, setViewMode] = useState<'kanban' | 'list'>('kanban');
  const [selectedRequisition, setSelectedRequisition] = useState<string>('all');
  const [requisitions, setRequisitions] = useState<any[]>([]);

  useEffect(() => {
    loadApplications();
    loadRequisitions();
  }, [selectedRequisition]);

  const loadApplications = async () => {
    try {
      setLoading(true);
      const url = selectedRequisition !== 'all'
        ? `/recruitment/applications?requisitionId=${selectedRequisition}`
        : '/recruitment/applications';
      const response = await api.tenant.get(url);
      setApplications(response.data.data || []);
    } catch (error) {
      console.error('Error loading applications:', error);
    } finally {
      setLoading(false);
    }
  };

  const loadRequisitions = async () => {
    try {
      const response = await api.tenant.get('/recruitment/requisitions');
      setRequisitions(response.data.data || []);
    } catch (error) {
      console.error('Error loading requisitions:', error);
    }
  };

  const handleStageChange = async (applicationId: string, newStage: string) => {
    try {
      const statusMap: Record<string, string> = {
        Applied: 'New',
        Screening: 'InReview',
        Shortlisted: 'Shortlisted',
        Interview: 'Interviewing',
        Assessment: 'AssessmentPending',
        Offered: 'OfferExtended',
        Hired: 'Hired',
        Rejected: 'Rejected'
      };

      await api.tenant.put(`/recruitment/applications/${applicationId}/stage`, {
        stage: newStage,
        status: statusMap[newStage] || 'New'
      });
      loadApplications();
    } catch (error) {
      console.error('Error updating stage:', error);
      alert('Failed to update stage');
    }
  };

  const getApplicationsByStage = (stage: string) => {
    return applications.filter(app => app.stage === stage);
  };

  if (loading) {
    return <div className="loading">Loading...</div>;
  }

  return (
    <div className="recruiting-page">
      <div className="page-header">
        <h1>Applicant Tracking System</h1>
        <div className="header-actions">
          <select
            value={selectedRequisition}
            onChange={(e) => setSelectedRequisition(e.target.value)}
            className="filter-select"
          >
            <option value="all">All Requisitions</option>
            {requisitions.map((req) => (
              <option key={req.id} value={req.id}>
                {req.title}
              </option>
            ))}
          </select>
          <div className="view-toggle">
            <button
              className={viewMode === 'kanban' ? 'active' : ''}
              onClick={() => setViewMode('kanban')}
            >
              Kanban
            </button>
            <button
              className={viewMode === 'list' ? 'active' : ''}
              onClick={() => setViewMode('list')}
            >
              List
            </button>
          </div>
        </div>
      </div>

      {viewMode === 'kanban' ? (
        <div className="kanban-board">
          {stages.map((stage) => {
            const stageApplications = getApplicationsByStage(stage.id);
            return (
              <div key={stage.id} className="kanban-column">
                <div className="kanban-column-header" style={{ borderTopColor: stage.color }}>
                  <h3>{stage.label}</h3>
                  <span className="badge">{stageApplications.length}</span>
                </div>
                <div className="kanban-column-content">
                  {stageApplications.map((app) => (
                    <div
                      key={app.id}
                      className="kanban-card"
                      onClick={() => navigate(`/recruiting/applications/${app.id}`)}
                    >
                      <div className="card-header">
                        <strong>{app.candidateName}</strong>
                      </div>
                      <div className="card-body">
                        <p className="card-job">{app.jobTitle}</p>
                        <p className="card-email">{app.candidateEmail}</p>
                        <div className="card-meta">
                          <span>Applied: {new Date(app.appliedAt).toLocaleDateString()}</span>
                          {app.interviewCount > 0 && (
                            <span>Interviews: {app.interviewCount}</span>
                          )}
                        </div>
                      </div>
                      <div className="card-actions">
                        {stage.id !== 'Hired' && stage.id !== 'Rejected' && (
                          <select
                            value={app.stage}
                            onChange={(e) => {
                              e.stopPropagation();
                              handleStageChange(app.id, e.target.value);
                            }}
                            onClick={(e) => e.stopPropagation()}
                            className="stage-select"
                          >
                            {stages.map((s) => (
                              <option key={s.id} value={s.id}>
                                {s.label}
                              </option>
                            ))}
                          </select>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            );
          })}
        </div>
      ) : (
        <div className="table-container">
          <table className="data-table">
            <thead>
              <tr>
                <th>Candidate</th>
                <th>Email</th>
                <th>Job Title</th>
                <th>Stage</th>
                <th>Status</th>
                <th>Applied Date</th>
                <th>Interviews</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {applications.map((app) => (
                <tr key={app.id}>
                  <td>
                    <a
                      href="#"
                      onClick={(e) => {
                        e.preventDefault();
                        navigate(`/recruiting/applications/${app.id}`);
                      }}
                    >
                      {app.candidateName}
                    </a>
                  </td>
                  <td>{app.candidateEmail}</td>
                  <td>{app.jobTitle}</td>
                  <td>
                    <select
                      value={app.stage}
                      onChange={(e) => handleStageChange(app.id, e.target.value)}
                      className="stage-select"
                    >
                      {stages.map((s) => (
                        <option key={s.id} value={s.id}>
                          {s.label}
                        </option>
                      ))}
                    </select>
                  </td>
                  <td>
                    <span className="status-badge">{app.status}</span>
                  </td>
                  <td>{new Date(app.appliedAt).toLocaleDateString()}</td>
                  <td>{app.interviewCount}</td>
                  <td>
                    <button
                      className="btn-sm btn-primary"
                      onClick={() => navigate(`/recruiting/applications/${app.id}`)}
                    >
                      View
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};

export default ATSPipelinePage;
