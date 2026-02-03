import React, { useState, useEffect } from 'react';
import api from '../../services/api';
import './SettingsPages.css';

interface Designation {
  id: string;
  name: string;
  code?: string;
  level?: number;
  createdAtUtc: string;
}

const DesignationsPage: React.FC = () => {
  const [designations, setDesignations] = useState<Designation[]>([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [editingDesignation, setEditingDesignation] = useState<Designation | null>(null);
  const [formData, setFormData] = useState({
    name: '',
    code: '',
    level: ''
  });

  useEffect(() => {
    loadDesignations();
  }, []);

  const loadDesignations = async () => {
    try {
      setLoading(true);
      const response = await api.getDesignations();
      setDesignations(response || []);
    } catch (error) {
      console.error('Error loading designations:', error);
      alert('Failed to load designations');
    } finally {
      setLoading(false);
    }
  };

  const handleOpenModal = (designation?: Designation) => {
    if (designation) {
      setEditingDesignation(designation);
      setFormData({
        name: designation.name,
        code: designation.code || '',
        level: designation.level?.toString() || ''
      });
    } else {
      setEditingDesignation(null);
      setFormData({ name: '', code: '', level: '' });
    }
    setShowModal(true);
  };

  const handleCloseModal = () => {
    setShowModal(false);
    setEditingDesignation(null);
    setFormData({ name: '', code: '', level: '' });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const data: any = {
        name: formData.name,
        code: formData.code || undefined,
        level: formData.level ? parseInt(formData.level) : undefined
      };

      if (editingDesignation) {
        await api.updateDesignation(editingDesignation.id, data);
      } else {
        await api.createDesignation(data);
      }
      
      handleCloseModal();
      loadDesignations();
    } catch (error: any) {
      console.error('Error saving designation:', error);
      alert(error.response?.data?.error || 'Failed to save designation');
    }
  };

  const handleDelete = async (id: string) => {
    if (!window.confirm('Are you sure you want to delete this designation? This action cannot be undone.')) {
      return;
    }

    try {
      await api.deleteDesignation(id);
      loadDesignations();
    } catch (error: any) {
      console.error('Error deleting designation:', error);
      alert(error.response?.data?.error || 'Failed to delete designation');
    }
  };

  if (loading) {
    return (
      <div className="settings-page">
        <div className="settings-container">
          <div className="loading">Loading designations...</div>
        </div>
      </div>
    );
  }

  return (
    <div className="settings-page">
      <div className="settings-container">
        <div className="settings-header">
          <h1>Designations</h1>
          <p>Manage job titles and designations for your organization</p>
        </div>

        <div className="settings-section">
          <div className="section-header">
            <h2>All Designations</h2>
            <button className="btn-primary" onClick={() => handleOpenModal()}>
              + Add Designation
            </button>
          </div>

          {designations.length === 0 ? (
            <div className="empty-state">
              <p>No designations found. Create your first designation to get started.</p>
            </div>
          ) : (
            <div className="table-container">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Name</th>
                    <th>Code</th>
                    <th>Level</th>
                    <th>Created</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {designations.map((designation) => (
                    <tr key={designation.id}>
                      <td>{designation.name}</td>
                      <td>{designation.code || '-'}</td>
                      <td>{designation.level ?? '-'}</td>
                      <td>{new Date(designation.createdAtUtc).toLocaleDateString()}</td>
                      <td>
                        <div className="action-buttons">
                          <button
                            className="btn-edit"
                            onClick={() => handleOpenModal(designation)}
                          >
                            Edit
                          </button>
                          <button
                            className="btn-delete"
                            onClick={() => handleDelete(designation.id)}
                          >
                            Delete
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>

      {showModal && (
        <div className="modal-overlay" onClick={handleCloseModal}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>{editingDesignation ? 'Edit Designation' : 'Create Designation'}</h2>
              <button className="modal-close" onClick={handleCloseModal}>×</button>
            </div>
            <form onSubmit={handleSubmit} className="form">
              <div className="form-group">
                <label>Designation Name *</label>
                <input
                  type="text"
                  required
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  placeholder="e.g., Senior Software Engineer"
                />
              </div>

              <div className="form-group">
                <label>Designation Code</label>
                <input
                  type="text"
                  value={formData.code}
                  onChange={(e) => setFormData({ ...formData, code: e.target.value })}
                  placeholder="e.g., SSE"
                />
              </div>

              <div className="form-group">
                <label>Level</label>
                <input
                  type="number"
                  min="1"
                  value={formData.level}
                  onChange={(e) => setFormData({ ...formData, level: e.target.value })}
                  placeholder="e.g., 5 (optional)"
                />
                <small style={{ color: '#64748b', fontSize: '12px' }}>
                  Optional: Numeric level for hierarchy (e.g., 1-10)
                </small>
              </div>

              <div className="form-actions">
                <button type="button" className="btn-secondary" onClick={handleCloseModal}>
                  Cancel
                </button>
                <button type="submit" className="btn-primary">
                  {editingDesignation ? 'Update' : 'Create'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default DesignationsPage;
