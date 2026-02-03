import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../../services/api';
import './RecruitingPages.css';

interface Candidate {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  designation?: string;
  appliedAt: string;
  stage: string;
  requisitionTitle?: string;
  profilePhotoUrl?: string;
}

const stages = [
  { id: 'all', label: 'All Candidates', count: 0 },
  { id: 'ApplicationReceived', label: 'Application Received', count: 0 },
  { id: 'Shortlisted', label: 'Shortlisted', count: 0 },
  { id: 'InProgress', label: 'In Progress', count: 0 },
  { id: 'JobOffer', label: 'Job Offer', count: 0 },
  { id: 'Preboarding', label: 'Preboarding', count: 0 },
  { id: 'Hired', label: 'Hired', count: 0 },
  { id: 'Rejected', label: 'Rejected', count: 0 }
];

const CandidatesTab: React.FC = () => {
  const navigate = useNavigate();
  const [candidates, setCandidates] = useState<Candidate[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedStage, setSelectedStage] = useState<string>('all');
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedVacancy, setSelectedVacancy] = useState<string>('all');
  const [vacancies, setVacancies] = useState<any[]>([]);
  const [stageCounts, setStageCounts] = useState<Record<string, number>>({});

  useEffect(() => {
    loadCandidates();
    loadVacancies();
  }, [selectedStage, selectedVacancy]);

  const loadCandidates = async () => {
    try {
      setLoading(true);
      let url = '/recruitment/applications';
      const params: any = {};
      
      if (selectedVacancy !== 'all') {
        params.requisitionId = selectedVacancy;
      }

      const response = await api.tenant.get(url, { params });
      const applications = response.data.data || [];

      // Transform applications to candidates
      const candidatesData: Candidate[] = await Promise.all(
        applications.map(async (app: any) => {
          // Fetch candidate details
          let candidateData: any = {};
          if (app.candidateId) {
            try {
              const candidateResponse = await api.tenant.get(`/recruitment/candidates/${app.candidateId}`);
              candidateData = candidateResponse.data;
            } catch (error) {
              console.error('Error loading candidate:', error);
            }
          }

          return {
            id: app.id,
            firstName: candidateData.firstName || app.candidateName?.split(' ')[0] || 'Unknown',
            lastName: candidateData.lastName || app.candidateName?.split(' ').slice(1).join(' ') || '',
            email: candidateData.email || app.candidateEmail || '',
            phone: candidateData.phone || '',
            designation: app.requisitionTitle || candidateData.currentTitle || '',
            appliedAt: app.appliedAt || app.createdAtUtc || '',
            stage: mapStageToDisplay(app.currentStage, app.status),
            requisitionTitle: app.requisitionTitle,
            profilePhotoUrl: candidateData.profilePhotoUrl
          };
        })
      );

      // Filter by stage
      let filtered = candidatesData;
      if (selectedStage !== 'all') {
        filtered = candidatesData.filter(c => c.stage === selectedStage);
      }

      // Filter by search query
      if (searchQuery) {
        filtered = filtered.filter(c =>
          `${c.firstName} ${c.lastName}`.toLowerCase().includes(searchQuery.toLowerCase()) ||
          c.email.toLowerCase().includes(searchQuery.toLowerCase()) ||
          c.phone.includes(searchQuery)
        );
      }

      setCandidates(filtered);

      // Calculate stage counts
      const counts: Record<string, number> = {};
      stages.forEach(stage => {
        if (stage.id === 'all') {
          counts[stage.id] = candidatesData.length;
        } else {
          counts[stage.id] = candidatesData.filter(c => c.stage === stage.id).length;
        }
      });
      setStageCounts(counts);
    } catch (error) {
      console.error('Error loading candidates:', error);
    } finally {
      setLoading(false);
    }
  };

  const loadVacancies = async () => {
    try {
      const response = await api.tenant.get('/recruitment/requisitions');
      setVacancies(response.data.data || []);
    } catch (error) {
      console.error('Error loading vacancies:', error);
    }
  };

  const mapStageToDisplay = (stage: number, status: string): string => {
    const stageMap: Record<number, string> = {
      0: 'ApplicationReceived',
      1: 'Shortlisted',
      2: 'InProgress',
      3: 'JobOffer',
      4: 'Preboarding',
      5: 'Hired',
      6: 'Rejected'
    };
    return stageMap[stage] || 'ApplicationReceived';
  };

  const handleStageChange = async (candidateId: string, newStage: string) => {
    try {
      const stageMap: Record<string, number> = {
        'ApplicationReceived': 0,
        'Shortlisted': 1,
        'InProgress': 2,
        'JobOffer': 3,
        'Preboarding': 4,
        'Hired': 5,
        'Rejected': 6
      };

      // Find the application for this candidate
      const application = candidates.find(c => c.id === candidateId);
      if (application) {
        await api.tenant.put(`/recruitment/applications/${candidateId}/stage`, {
          stage: stageMap[newStage] || 0,
          status: newStage
        });
        loadCandidates();
      }
    } catch (error) {
      console.error('Error updating stage:', error);
      alert('Failed to update stage');
    }
  };

  const getInitials = (firstName: string, lastName: string): string => {
    const first = firstName?.charAt(0)?.toUpperCase() || '';
    const last = lastName?.charAt(0)?.toUpperCase() || '';
    return first + last || '?';
  };

  const getAvatarColor = (name: string): string => {
    const colors = ['#10b981', '#3b82f6', '#8b5cf6', '#ec4899', '#f59e0b', '#ef4444'];
    const index = name.charCodeAt(0) % colors.length;
    return colors[index];
  };

  if (loading) {
    return <div className="loading">Loading candidates...</div>;
  }

  return (
    <div className="candidates-tab">
      <div className="candidates-layout">
        {/* Left Sidebar */}
        <div className="candidates-sidebar">
          <button
            className="btn-add-candidate"
            onClick={() => {
              // TODO: Open add candidate modal or navigate to form
              alert('Add Candidate feature - Coming soon');
            }}
          >
            + Add Candidate
          </button>

          <div className="sidebar-section">
            <div className="sidebar-header">
              <span className="sidebar-icon">👁️</span>
              <span>All Vacancies</span>
              <span className="dropdown-arrow">▼</span>
            </div>
          </div>

          <div className="sidebar-stages">
            {stages.map((stage) => (
              <div
                key={stage.id}
                className={`stage-item ${selectedStage === stage.id ? 'active' : ''}`}
                onClick={() => setSelectedStage(stage.id)}
              >
                <span className="stage-label">{stage.label}</span>
                <span className="stage-badge">{stageCounts[stage.id] || 0}</span>
              </div>
            ))}
          </div>
        </div>

        {/* Main Content */}
        <div className="candidates-main">
          <div className="candidates-header">
            <h2>({candidates.length}) Candidates Found</h2>
            <div className="header-toolbar">
              <div className="search-box">
                <span className="search-icon">🔍</span>
                <input
                  type="text"
                  placeholder="Search"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                />
                <button className="filter-icon" title="Filter">🔽</button>
              </div>
              <button className="btn-csv">CSV</button>
            </div>
          </div>

          <div className="table-container">
            <table className="candidates-table">
              <thead>
                <tr>
                  <th>
                    <input type="checkbox" />
                  </th>
                  <th></th>
                  <th>
                    Candidate <span className="sort-icon">⇅</span>
                  </th>
                  <th>
                    Email <span className="sort-icon">⇅</span>
                  </th>
                  <th>
                    Contact Number <span className="sort-icon">⇅</span>
                  </th>
                  <th>
                    Date Applied <span className="sort-icon">⇅</span>
                  </th>
                  <th>
                    Job Fit <span className="sort-icon">⇅</span>
                  </th>
                  <th>
                    Stage <span className="sort-icon">⇅</span>
                  </th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {candidates.length === 0 ? (
                  <tr>
                    <td colSpan={9} className="empty-state">
                      No candidates found
                    </td>
                  </tr>
                ) : (
                  candidates.map((candidate) => (
                    <tr key={candidate.id}>
                      <td>
                        <input type="checkbox" />
                      </td>
                      <td>
                        {candidate.profilePhotoUrl ? (
                          <img
                            src={candidate.profilePhotoUrl}
                            alt={`${candidate.firstName} ${candidate.lastName}`}
                            className="avatar-img"
                          />
                        ) : (
                          <div
                            className="avatar-placeholder"
                            style={{ backgroundColor: getAvatarColor(candidate.firstName) }}
                          >
                            {getInitials(candidate.firstName, candidate.lastName)}
                          </div>
                        )}
                      </td>
                      <td>
                        <div className="candidate-info">
                          <strong>{`${candidate.firstName} ${candidate.lastName}`}</strong>
                          <span className="candidate-role">{candidate.designation || 'N/A'}</span>
                        </div>
                      </td>
                      <td>{candidate.email}</td>
                      <td>{candidate.phone || 'N/A'}</td>
                      <td>{new Date(candidate.appliedAt).toLocaleDateString()}</td>
                      <td>--</td>
                      <td>
                        <select
                          value={candidate.stage}
                          onChange={(e) => handleStageChange(candidate.id, e.target.value)}
                          className="stage-select-inline"
                        >
                          {stages.filter(s => s.id !== 'all').map((stage) => (
                            <option key={stage.id} value={stage.id}>
                              {stage.label}
                            </option>
                          ))}
                        </select>
                      </td>
                      <td>
                        <button className="action-menu">⋯</button>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  );
};

export default CandidatesTab;
