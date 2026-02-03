import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import CandidatesTab from './CandidatesTab';
import VacanciesTab from './VacanciesTab';
import ConfigurationTab from './ConfigurationTab';
import './RecruitingPages.css';

const RecruitmentPage: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const [activeTab, setActiveTab] = useState<'candidates' | 'vacancies' | 'configuration'>('candidates');

  useEffect(() => {
    if (location.pathname.includes('/requisitions')) {
      setActiveTab('vacancies');
    } else if (location.pathname.includes('/configuration')) {
      setActiveTab('configuration');
    } else {
      setActiveTab('candidates');
    }
  }, [location.pathname]);

  const handleTabChange = (tab: 'candidates' | 'vacancies' | 'configuration') => {
    setActiveTab(tab);
    if (tab === 'candidates') {
      navigate('/recruiting/pipeline');
    } else if (tab === 'vacancies') {
      navigate('/recruiting/requisitions');
    } else {
      navigate('/recruiting/configuration');
    }
  };

  return (
    <div className="recruitment-module-wrapper">
      {/* Top Navigation Bar - Inside Layout */}
      <div className="recruitment-top-nav">
        <div className="top-nav-left">
          <h1 className="module-title">Recruitment (ATS) / {activeTab === 'candidates' ? 'Candidates' : activeTab === 'vacancies' ? 'Vacancies' : 'Configuration'}</h1>
        </div>
        <div className="top-nav-center">
          <div className="nav-tabs">
            <button
              className={`nav-tab ${activeTab === 'candidates' ? 'active' : ''}`}
              onClick={() => handleTabChange('candidates')}
            >
              Candidates
            </button>
            <button
              className={`nav-tab ${activeTab === 'vacancies' ? 'active' : ''}`}
              onClick={() => handleTabChange('vacancies')}
            >
              Vacancies
            </button>
            <button
              className={`nav-tab ${activeTab === 'configuration' ? 'active' : ''}`}
              onClick={() => handleTabChange('configuration')}
            >
              Configuration
            </button>
          </div>
        </div>
        <div className="top-nav-right">
          <button className="icon-btn" title="Help">?</button>
          <button className="icon-btn" title="Share">📤</button>
        </div>
      </div>

      {/* Main Content Area */}
      <div className="recruitment-content">
        {activeTab === 'candidates' && <CandidatesTab />}
        {activeTab === 'vacancies' && <VacanciesTab />}
        {activeTab === 'configuration' && <ConfigurationTab />}
      </div>
    </div>
  );
};

export default RecruitmentPage;
