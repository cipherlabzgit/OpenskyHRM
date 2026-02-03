import React, { useState, useEffect } from 'react';
import api from '../../services/api';
import './SettingsPages.css';

interface Department {
  id: string;
  name: string;
  code?: string;
  parentId?: string;
  createdAtUtc: string;
}

const CompanySettingsPage: React.FC = () => {
  const [departments, setDepartments] = useState<Department[]>([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [editingDepartment, setEditingDepartment] = useState<Department | null>(null);
  const [formData, setFormData] = useState({
    name: '',
    code: '',
    parentId: ''
  });

  useEffect(() => {
    loadDepartments();
  }, []);

  const loadDepartments = async () => {
    try {
      setLoading(true);
      const response = await api.getDepartments();
      setDepartments(response || []);
    } catch (error) {
      console.error('Error loading departments:', error);
      alert('Failed to load departments');
    } finally {
      setLoading(false);
    }
  };

  const handleOpenModal = (dept?: Department) => {
    if (dept) {
      setEditingDepartment(dept);
      setFormData({
        name: dept.name,
        code: dept.code || '',
        parentId: dept.parentId || ''
      });
    } else {
      setEditingDepartment(null);
      setFormData({ name: '', code: '', parentId: '' });
    }
    setShowModal(true);
  };

  const handleCloseModal = () => {
    setShowModal(false);
    setEditingDepartment(null);
    setFormData({ name: '', code: '', parentId: '' });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const data: any = {
        name: formData.name,
        code: formData.code || undefined,
        parentId: formData.parentId || undefined
      };

      if (editingDepartment) {
        await api.updateDepartment(editingDepartment.id, data);
      } else {
        await api.createDepartment(data);
      }
      
      handleCloseModal();
      loadDepartments();
    } catch (error: any) {
      console.error('Error saving department:', error);
      alert(error.response?.data?.error || 'Failed to save department');
    }
  };

  const handleDelete = async (id: string) => {
    if (!window.confirm('Are you sure you want to delete this department? This action cannot be undone.')) {
      return;
    }

    try {
      await api.deleteDepartment(id);
      loadDepartments();
    } catch (error: any) {
      console.error('Error deleting department:', error);
      alert(error.response?.data?.error || 'Failed to delete department');
    }
  };

  const getDepartmentName = (parentId?: string) => {
    if (!parentId) return null;
    const dept = departments.find(d => d.id === parentId);
    return dept?.name || 'Unknown';
  };

  if (loading) {
    return (
      <div className="settings-page">
        <div className="settings-container">
          <div className="loading">Loading departments...</div>
        </div>
      </div>
    );
  }

  return (
    <div className="settings-page">
      <div className="settings-container">
        <div className="settings-header">
          <h1>Company Settings</h1>
          <p>Manage your company's organizational structure</p>
        </div>

        <div className="settings-section">
          <div className="section-header">
            <h2>Departments</h2>
            <button className="btn-primary" onClick={() => handleOpenModal()}>
              + Add Department
            </button>
          </div>

          {departments.length === 0 ? (
            <div className="empty-state">
              <p>No departments found. Create your first department to get started.</p>
            </div>
          ) : (
            <div className="table-container">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Name</th>
                    <th>Code</th>
                    <th>Parent Department</th>
                    <th>Created</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {departments.map((dept) => (
                    <tr key={dept.id}>
                      <td>{dept.name}</td>
                      <td>{dept.code || '-'}</td>
                      <td>{getDepartmentName(dept.parentId) || '-'}</td>
                      <td>{new Date(dept.createdAtUtc).toLocaleDateString()}</td>
                      <td>
                        <div className="action-buttons">
                          <button
                            className="btn-edit"
                            onClick={() => handleOpenModal(dept)}
                          >
                            Edit
                          </button>
                          <button
                            className="btn-delete"
                            onClick={() => handleDelete(dept.id)}
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
              <h2>{editingDepartment ? 'Edit Department' : 'Create Department'}</h2>
              <button className="modal-close" onClick={handleCloseModal}>×</button>
            </div>
            <form onSubmit={handleSubmit} className="form">
              <div className="form-group">
                <label>Department Name *</label>
                <input
                  type="text"
                  required
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  placeholder="e.g., Human Resources"
                />
              </div>

              <div className="form-group">
                <label>Department Code</label>
                <input
                  type="text"
                  value={formData.code}
                  onChange={(e) => setFormData({ ...formData, code: e.target.value })}
                  placeholder="e.g., HR"
                />
              </div>

              <div className="form-group">
                <label>Parent Department</label>
                <select
                  value={formData.parentId}
                  onChange={(e) => setFormData({ ...formData, parentId: e.target.value })}
                >
                  <option value="">None (Top Level)</option>
                  {departments
                    .filter(d => !d.parentId || d.id !== editingDepartment?.id)
                    .map((dept) => (
                      <option key={dept.id} value={dept.id}>
                        {dept.name}
                      </option>
                    ))}
                </select>
              </div>

              <div className="form-actions">
                <button type="button" className="btn-secondary" onClick={handleCloseModal}>
                  Cancel
                </button>
                <button type="submit" className="btn-primary">
                  {editingDepartment ? 'Update' : 'Create'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default CompanySettingsPage;
