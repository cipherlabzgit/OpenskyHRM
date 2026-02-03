import React, { useState, useEffect } from 'react';
import api from '../../services/api';
import './SettingsPages.css';

interface User {
  id: string;
  email: string;
  fullName: string;
  isActive: boolean;
  emailConfirmed: boolean;
  roles: Array<{ id: string; name: string }>;
  createdAtUtc: string;
}

interface Role {
  id: string;
  name: string;
  description?: string;
}

const UsersPage: React.FC = () => {
  const [users, setUsers] = useState<User[]>([]);
  const [roles, setRoles] = useState<Role[]>([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [editingUser, setEditingUser] = useState<User | null>(null);
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    fullName: '',
    roleIds: [] as string[]
  });

  useEffect(() => {
    loadUsers();
    loadRoles();
  }, []);

  const loadUsers = async () => {
    try {
      setLoading(true);
      const response = await api.tenant.get('/users');
      setUsers(response.data || []);
    } catch (error) {
      console.error('Error loading users:', error);
      alert('Failed to load users');
    } finally {
      setLoading(false);
    }
  };

  const loadRoles = async () => {
    try {
      const response = await api.tenant.get('/users/roles');
      setRoles(response.data || []);
    } catch (error) {
      console.error('Error loading roles:', error);
    }
  };

  const handleOpenModal = (user?: User) => {
    if (user) {
      setEditingUser(user);
      setFormData({
        email: user.email,
        password: '',
        fullName: user.fullName,
        roleIds: user.roles.map(r => r.id)
      });
    } else {
      setEditingUser(null);
      setFormData({
        email: '',
        password: '',
        fullName: '',
        roleIds: []
      });
    }
    setShowModal(true);
  };

  const handleCloseModal = () => {
    setShowModal(false);
    setEditingUser(null);
    setFormData({
      email: '',
      password: '',
      fullName: '',
      roleIds: []
    });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (editingUser) {
        const payload: any = {
          fullName: formData.fullName,
          roleIds: formData.roleIds.map(id => id)
        };
        if (formData.password) {
          payload.password = formData.password;
        }
        await api.tenant.put(`/users/${editingUser.id}`, payload);
      } else {
        await api.tenant.post('/users', {
          email: formData.email,
          password: formData.password,
          fullName: formData.fullName,
          roleIds: formData.roleIds.map(id => id)
        });
      }
      
      handleCloseModal();
      loadUsers();
    } catch (error: any) {
      console.error('Error saving user:', error);
      alert(error.response?.data?.error || 'Failed to save user');
    }
  };

  const handleDelete = async (id: string) => {
    if (!window.confirm('Are you sure you want to delete this user? This action cannot be undone.')) {
      return;
    }

    try {
      await api.tenant.delete(`/users/${id}`);
      loadUsers();
    } catch (error: any) {
      console.error('Error deleting user:', error);
      alert(error.response?.data?.error || 'Failed to delete user');
    }
  };

  if (loading) {
    return (
      <div className="settings-page">
        <div className="settings-container">
          <div className="loading">Loading users...</div>
        </div>
      </div>
    );
  }

  return (
    <div className="settings-page">
      <div className="settings-container">
        <div className="settings-header">
          <h1>User Management</h1>
          <p>Manage user accounts and their roles</p>
        </div>

        <div className="settings-section">
          <div className="section-header">
            <h2>All Users</h2>
            <button className="btn-primary" onClick={() => handleOpenModal()}>
              + Add User
            </button>
          </div>

          {users.length === 0 ? (
            <div className="empty-state">
              <p>No users found. Create your first user to get started.</p>
            </div>
          ) : (
            <div className="table-container">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Name</th>
                    <th>Email</th>
                    <th>Roles</th>
                    <th>Status</th>
                    <th>Created</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {users.map((user) => (
                    <tr key={user.id}>
                      <td>{user.fullName}</td>
                      <td>{user.email}</td>
                      <td>
                        {user.roles.length > 0 ? (
                          <div style={{ display: 'flex', gap: '4px', flexWrap: 'wrap' }}>
                            {user.roles.map((role) => (
                              <span
                                key={role.id}
                                style={{
                                  padding: '2px 8px',
                                  borderRadius: '4px',
                                  fontSize: '12px',
                                  background: '#e0e7ff',
                                  color: '#3730a3'
                                }}
                              >
                                {role.name}
                              </span>
                            ))}
                          </div>
                        ) : (
                          '-'
                        )}
                      </td>
                      <td>
                        <span
                          style={{
                            padding: '4px 8px',
                            borderRadius: '4px',
                            fontSize: '12px',
                            background: user.isActive ? '#d1fae5' : '#fee2e2',
                            color: user.isActive ? '#065f46' : '#991b1b'
                          }}
                        >
                          {user.isActive ? 'Active' : 'Inactive'}
                        </span>
                      </td>
                      <td>{new Date(user.createdAtUtc).toLocaleDateString()}</td>
                      <td>
                        <div className="action-buttons">
                          <button
                            className="btn-edit"
                            onClick={() => handleOpenModal(user)}
                          >
                            Edit
                          </button>
                          <button
                            className="btn-delete"
                            onClick={() => handleDelete(user.id)}
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
              <h2>{editingUser ? 'Edit User' : 'Create User'}</h2>
              <button className="modal-close" onClick={handleCloseModal}>×</button>
            </div>
            <form onSubmit={handleSubmit} className="form">
              {!editingUser && (
                <div className="form-group">
                  <label>Email *</label>
                  <input
                    type="email"
                    required
                    value={formData.email}
                    onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                    placeholder="user@example.com"
                  />
                </div>
              )}

              <div className="form-group">
                <label>Full Name *</label>
                <input
                  type="text"
                  required
                  value={formData.fullName}
                  onChange={(e) => setFormData({ ...formData, fullName: e.target.value })}
                  placeholder="John Doe"
                />
              </div>

              {editingUser && (
                <div className="form-group">
                  <label>New Password</label>
                  <input
                    type="password"
                    value={formData.password}
                    onChange={(e) => setFormData({ ...formData, password: e.target.value })}
                    placeholder="Leave blank to keep current password"
                  />
                  <small style={{ color: '#64748b', fontSize: '12px' }}>
                    Only enter if you want to change the password
                  </small>
                </div>
              )}

              {!editingUser && (
                <div className="form-group">
                  <label>Password *</label>
                  <input
                    type="password"
                    required
                    value={formData.password}
                    onChange={(e) => setFormData({ ...formData, password: e.target.value })}
                    placeholder="Enter password"
                  />
                </div>
              )}

              <div className="form-group">
                <label>Roles</label>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', maxHeight: '200px', overflowY: 'auto', border: '1px solid #e5e7eb', borderRadius: '8px', padding: '12px' }}>
                  {roles.map((role) => (
                    <label key={role.id} style={{ display: 'flex', alignItems: 'center', gap: '8px', cursor: 'pointer' }}>
                      <input
                        type="checkbox"
                        checked={formData.roleIds.includes(role.id)}
                        onChange={(e) => {
                          if (e.target.checked) {
                            setFormData({ ...formData, roleIds: [...formData.roleIds, role.id] });
                          } else {
                            setFormData({ ...formData, roleIds: formData.roleIds.filter(id => id !== role.id) });
                          }
                        }}
                      />
                      <div>
                        <div style={{ fontWeight: 500 }}>{role.name}</div>
                        {role.description && (
                          <div style={{ fontSize: '12px', color: '#64748b' }}>{role.description}</div>
                        )}
                      </div>
                    </label>
                  ))}
                </div>
                <small style={{ color: '#64748b', fontSize: '12px', marginTop: '4px' }}>
                  Select roles to assign to this user. DepartmentManager and HiringManager can approve requisitions.
                </small>
              </div>

              <div className="form-actions">
                <button type="button" className="btn-secondary" onClick={handleCloseModal}>
                  Cancel
                </button>
                <button type="submit" className="btn-primary">
                  {editingUser ? 'Update' : 'Create'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default UsersPage;
