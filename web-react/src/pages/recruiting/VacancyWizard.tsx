import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../../services/api';
import './RecruitingPages.css';

interface WizardStep {
  id: string;
  label: string;
  icon: string;
}

const wizardSteps: WizardStep[] = [
  { id: 'info', label: 'Vacancy Info', icon: '📄' },
  { id: 'workflow', label: 'Workflow', icon: '🔗' },
  { id: 'application', label: 'Application Form', icon: '📝' },
  { id: 'screening', label: 'Smart Screening', icon: '✈️' },
  { id: 'posting', label: 'Job Posting', icon: '📋' }
];

interface VacancyWizardProps {
  requisitionId?: string;
  onComplete?: () => void;
}

const VacancyWizard: React.FC<VacancyWizardProps> = ({ requisitionId, onComplete }) => {
  const navigate = useNavigate();
  const [currentStep, setCurrentStep] = useState(0);
  const [formData, setFormData] = useState({
    // Step 1: Vacancy Info
    title: '',
    departmentId: '',
    designationId: '',
    branchId: '',
    location: '',
    employmentType: 'FullTime',
    openings: 1,
    hiringManagerId: '',
    requestConsent: false,
    resumeRequired: true,
    
    // Step 2: Workflow
    approvalWorkflow: 'default',
    approvers: [] as string[],
    
    // Step 3: Application Form
    applicationFields: [] as string[],
    customQuestions: [] as string[],
    
    // Step 4: Smart Screening
    screeningEnabled: false,
    screeningCriteria: '',
    
    // Step 5: Job Posting
    publishToCareerPortal: false,
    publishToJobBoards: false,
    postingStartDate: '',
    postingEndDate: ''
  });

  const [departments, setDepartments] = useState<any[]>([]);
  const [designations, setDesignations] = useState<any[]>([]);
  const [branches, setBranches] = useState<any[]>([]);
  const [users, setUsers] = useState<any[]>([]);
  const [loading, setLoading] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    loadReferenceData();
    if (requisitionId) {
      loadRequisition();
    }
  }, [requisitionId]);

  const loadReferenceData = async () => {
    try {
      const [deptsRes, desigsRes, usersRes] = await Promise.all([
        api.getDepartments(),
        api.getDesignations(),
        api.tenant.get('/users')
      ]);
      setDepartments(deptsRes.data || []);
      setDesignations(desigsRes.data || []);
      setUsers(usersRes.data || []);
    } catch (error) {
      console.error('Error loading reference data:', error);
    }
  };

  const loadRequisition = async () => {
    if (!requisitionId) return;
    try {
      const response = await api.getJobRequisition(requisitionId);
      const req = response.data;
      setFormData({
        ...formData,
        title: req.title || '',
        departmentId: req.departmentId || '',
        designationId: req.designationId || '',
        branchId: req.branchId || '',
        location: req.location || '',
        employmentType: req.employmentType || 'FullTime',
        openings: req.openings || 1,
        hiringManagerId: req.hiringManagerId || ''
      });
    } catch (error) {
      console.error('Error loading requisition:', error);
    }
  };

  const validateStep = (step: number): boolean => {
    const newErrors: Record<string, string> = {};

    if (step === 0) {
      // Validate Vacancy Info
      if (!formData.title.trim()) {
        newErrors.title = 'Vacancy title is required';
      }
      if (!formData.designationId) {
        newErrors.designationId = 'Job title is required';
      }
      if (!formData.location) {
        newErrors.location = 'Location is required';
      }
      if (!formData.hiringManagerId) {
        newErrors.hiringManagerId = 'Hiring manager is required';
      }
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleNext = () => {
    if (validateStep(currentStep)) {
      if (currentStep === 0 && !requisitionId) {
        // Save requisition on first step
        handleSave();
      } else {
        setCurrentStep(currentStep + 1);
      }
    }
  };

  const handlePrevious = () => {
    if (currentStep > 0) {
      setCurrentStep(currentStep - 1);
    }
  };

  const handleSave = async () => {
    try {
      setLoading(true);
      const payload: any = {
        title: formData.title,
        departmentId: formData.departmentId || null,
        designationId: formData.designationId || null,
        branchId: formData.branchId || null,
        location: formData.location,
        employmentType: formData.employmentType,
        openings: formData.openings,
        hiringManagerId: formData.hiringManagerId || null
      };

      if (requisitionId) {
        await api.updateJobRequisition(requisitionId, payload);
      } else {
        const response = await api.createJobRequisition(payload);
        // Update formData with the new requisition ID for subsequent steps
        if (response.data?.id) {
          // Store for next steps
        }
      }

      if (currentStep < wizardSteps.length - 1) {
        setCurrentStep(currentStep + 1);
      } else {
        // Wizard complete
        if (onComplete) {
          onComplete();
        } else {
          navigate('/recruiting/requisitions');
        }
      }
    } catch (error: any) {
      console.error('Error saving requisition:', error);
      alert(error.response?.data?.error || 'Failed to save requisition');
    } finally {
      setLoading(false);
    }
  };

  const handleFieldChange = (field: string, value: any) => {
    setFormData({ ...formData, [field]: value });
    // Clear error for this field
    if (errors[field]) {
      setErrors({ ...errors, [field]: '' });
    }
  };

  const renderStepContent = () => {
    switch (currentStep) {
      case 0:
        return (
          <div className="wizard-step-content">
            <div className="wizard-form-header">
              <h2>Vacancy Info</h2>
              <p>Use a saved template or import from template for vacancy details.</p>
              <button className="btn-import-template" type="button">
                <span>☁️</span> Import from Template
              </button>
            </div>

            <div className="wizard-form">
              <div className="form-row">
                <div className="form-group">
                  <label>
                    Vacancy <span className="required">*</span>
                  </label>
                  <input
                    type="text"
                    value={formData.title}
                    onChange={(e) => handleFieldChange('title', e.target.value)}
                    placeholder="Type here"
                    className={errors.title ? 'error' : ''}
                  />
                  {errors.title && <span className="error-message">{errors.title}</span>}
                </div>

                <div className="form-group">
                  <label>
                    Job Title <span className="required">*</span>
                  </label>
                  <select
                    value={formData.designationId}
                    onChange={(e) => handleFieldChange('designationId', e.target.value)}
                    className={errors.designationId ? 'error' : ''}
                  >
                    <option value="">Select</option>
                    {designations.map((d) => (
                      <option key={d.id} value={d.id}>
                        {d.name}
                      </option>
                    ))}
                  </select>
                  {errors.designationId && <span className="error-message">{errors.designationId}</span>}
                </div>
              </div>

              <div className="form-row">
                <div className="form-group">
                  <label>
                    Location <span className="required">*</span>
                  </label>
                  <select
                    value={formData.location}
                    onChange={(e) => handleFieldChange('location', e.target.value)}
                    className={errors.location ? 'error' : ''}
                  >
                    <option value="">Select</option>
                    <option value="Remote">Remote</option>
                    <option value="On-site">On-site</option>
                    <option value="Hybrid">Hybrid</option>
                  </select>
                  {errors.location && <span className="error-message">{errors.location}</span>}
                </div>

                <div className="form-group">
                  <label>Sub Unit</label>
                  <select
                    value={formData.departmentId}
                    onChange={(e) => handleFieldChange('departmentId', e.target.value)}
                  >
                    <option value="">Select</option>
                    {departments.map((d) => (
                      <option key={d.id} value={d.id}>
                        {d.name}
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              <div className="form-row">
                <div className="form-group">
                  <label>
                    Hiring Manager <span className="required">*</span>
                  </label>
                  <input
                    type="text"
                    value={formData.hiringManagerId}
                    onChange={(e) => handleFieldChange('hiringManagerId', e.target.value)}
                    placeholder="Type for hints..."
                    className={errors.hiringManagerId ? 'error' : ''}
                    list="users-list"
                  />
                  <datalist id="users-list">
                    {users.map((u) => (
                      <option key={u.id} value={u.id}>
                        {u.fullName} ({u.email})
                      </option>
                    ))}
                  </datalist>
                  {errors.hiringManagerId && <span className="error-message">{errors.hiringManagerId}</span>}
                </div>

                <div className="form-group">
                  <label>Number of Positions</label>
                  <input
                    type="number"
                    value={formData.openings}
                    onChange={(e) => handleFieldChange('openings', parseInt(e.target.value) || 1)}
                    placeholder="Type here"
                    min="1"
                  />
                </div>
              </div>

              <div className="form-checkboxes">
                <label className="checkbox-label">
                  <input
                    type="checkbox"
                    checked={formData.requestConsent}
                    onChange={(e) => handleFieldChange('requestConsent', e.target.checked)}
                  />
                  <span>Request consent to keep candidate data for later processing</span>
                </label>

                <label className="checkbox-label">
                  <input
                    type="checkbox"
                    checked={formData.resumeRequired}
                    onChange={(e) => handleFieldChange('resumeRequired', e.target.checked)}
                  />
                  <span>Resume required</span>
                </label>
              </div>
            </div>
          </div>
        );

      case 1:
        return (
          <div className="wizard-step-content">
            <h2>Workflow</h2>
            <p>Configure approval workflow for this vacancy.</p>
            <div className="wizard-form">
              <div className="form-group">
                <label>Approval Workflow</label>
                <select
                  value={formData.approvalWorkflow}
                  onChange={(e) => handleFieldChange('approvalWorkflow', e.target.value)}
                >
                  <option value="default">Default Workflow</option>
                  <option value="custom">Custom Workflow</option>
                </select>
              </div>
            </div>
          </div>
        );

      case 2:
        return (
          <div className="wizard-step-content">
            <h2>Application Form</h2>
            <p>Configure application form fields and questions.</p>
            <div className="wizard-form">
              <p>Application form configuration coming soon...</p>
            </div>
          </div>
        );

      case 3:
        return (
          <div className="wizard-step-content">
            <h2>Smart Screening</h2>
            <p>Configure automated screening criteria.</p>
            <div className="wizard-form">
              <div className="form-group">
                <label className="checkbox-label">
                  <input
                    type="checkbox"
                    checked={formData.screeningEnabled}
                    onChange={(e) => handleFieldChange('screeningEnabled', e.target.checked)}
                  />
                  <span>Enable smart screening</span>
                </label>
              </div>
            </div>
          </div>
        );

      case 4:
        return (
          <div className="wizard-step-content">
            <h2>Job Posting</h2>
            <p>Configure where and when to post this job.</p>
            <div className="wizard-form">
              <div className="form-group">
                <label className="checkbox-label">
                  <input
                    type="checkbox"
                    checked={formData.publishToCareerPortal}
                    onChange={(e) => handleFieldChange('publishToCareerPortal', e.target.checked)}
                  />
                  <span>Publish to Career Portal</span>
                </label>
              </div>
              <div className="form-group">
                <label className="checkbox-label">
                  <input
                    type="checkbox"
                    checked={formData.publishToJobBoards}
                    onChange={(e) => handleFieldChange('publishToJobBoards', e.target.checked)}
                  />
                  <span>Publish to Job Boards</span>
                </label>
              </div>
            </div>
          </div>
        );

      default:
        return null;
    }
  };

  return (
    <div className="vacancy-wizard">
      {/* Wizard Navigation Bar */}
      <div className="wizard-nav-bar">
        <div className="wizard-nav-left">
          <button className="wizard-nav-btn" onClick={() => navigate('/recruiting/requisitions')} title="Home">
            🏠
          </button>
          <button className="wizard-nav-btn" onClick={handlePrevious} disabled={currentStep === 0} title="Back">
            ←
          </button>
          <span className="wizard-nav-label">Vacancy Add</span>
        </div>

        <div className="wizard-nav-steps">
          {wizardSteps.map((step, index) => (
            <div
              key={step.id}
              className={`wizard-nav-step ${index === currentStep ? 'active' : ''} ${index < currentStep ? 'completed' : ''}`}
              onClick={() => {
                if (index <= currentStep || index === 0) {
                  setCurrentStep(index);
                }
              }}
            >
              <span className="wizard-step-icon">{step.icon}</span>
              <span className="wizard-step-label">{step.label}</span>
            </div>
          ))}
        </div>

        <div className="wizard-nav-right">
          <button className="wizard-nav-btn" title="Help">?</button>
          <button className="wizard-nav-btn" title="Share">📤</button>
        </div>
      </div>

      {/* Wizard Content */}
      <div className="wizard-content">
        <div className="wizard-card">
          {renderStepContent()}

          <div className="wizard-footer">
            <div className="wizard-footer-left">
              <span className="required-note">* Required</span>
            </div>
            <div className="wizard-footer-right">
              {currentStep > 0 && (
                <button className="btn-secondary" onClick={handlePrevious} disabled={loading}>
                  Previous
                </button>
              )}
              <button
                className="btn-save-continue"
                onClick={currentStep === wizardSteps.length - 1 ? handleSave : handleNext}
                disabled={loading}
              >
                {loading ? 'Saving...' : currentStep === wizardSteps.length - 1 ? 'Save and Complete' : 'Save and Continue'}
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default VacancyWizard;
