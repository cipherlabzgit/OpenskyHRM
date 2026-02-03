import React, { useState } from 'react';
import './EmployeesPage.css';

interface Employee {
  id: string;
  employeeCode: string;
  fullName: string;
  email: string;
  department: string;
  designation: string;
  joinDate: string;
  status: string;
}

const EmployeesPage: React.FC = () => {
  const [searchTerm, setSearchTerm] = useState('');
  const [showAddModal, setShowAddModal] = useState(false);
  
  // Mock data
  const [employees] = useState<Employee[]>([
    { id: '1', employeeCode: 'EMP001', fullName: 'John Smith', email: 'john@company.com', department: 'Engineering', designation: 'Senior Developer', joinDate: '2023-01-15', status: 'Active' },
    { id: '2', employeeCode: 'EMP002', fullName: 'Sarah Johnson', email: 'sarah@company.com', department: 'HR', designation: 'HR Manager', joinDate: '2022-06-01', status: 'Active' },
    { id: '3', employeeCode: 'EMP003', fullName: 'Mike Brown', email: 'mike@company.com', department: 'Sales', designation: 'Sales Lead', joinDate: '2023-03-20', status: 'Active' },
    { id: '4', employeeCode: 'EMP004', fullName: 'Emily Davis', email: 'emily@company.com', department: 'Marketing', designation: 'Marketing Specialist', joinDate: '2023-05-10', status: 'Active' },
    { id: '5', employeeCode: 'EMP005', fullName: 'James Wilson', email: 'james@company.com', department: 'Engineering', designation: 'Junior Developer', joinDate: '2024-01-02', status: 'Active' }
  ]);

  const filteredEmployees = employees.filter(emp =>
    emp.fullName.toLowerCase().includes(searchTerm.toLowerCase()) ||
    emp.email.toLowerCase().includes(searchTerm.toLowerCase()) ||
    emp.department.toLowerCase().includes(searchTerm.toLowerCase())
  );

  return (
    <div className="employees-page">
      <div className="page-header">
        <div className="header-actions">
          <div className="search-box">
            <span className="search-icon">🔍</span>
            <input
              type="text"
              placeholder="Search employees..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
            />
          </div>
          <button className="add-btn" onClick={() => setShowAddModal(true)}>
            + Add Employee
          </button>
        </div>
      </div>

      <div className="employees-table-container">
        <table className="employees-table">
          <thead>
            <tr>
              <th>Employee</th>
              <th>Department</th>
              <th>Designation</th>
              <th>Join Date</th>
              <th>Status</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {filteredEmployees.map((employee) => (
              <tr key={employee.id}>
                <td>
                  <div className="employee-info">
                    <div className="employee-avatar">
                      {employee.fullName.split(' ').map(n => n[0]).join('')}
                    </div>
                    <div className="employee-details">
                      <span className="employee-name">{employee.fullName}</span>
                      <span className="employee-email">{employee.email}</span>
                    </div>
                  </div>
                </td>
                <td>{employee.department}</td>
                <td>{employee.designation}</td>
                <td>{new Date(employee.joinDate).toLocaleDateString()}</td>
                <td>
                  <span className={`status-badge ${employee.status.toLowerCase()}`}>
                    {employee.status}
                  </span>
                </td>
                <td>
                  <div className="action-buttons">
                    <button className="action-btn view">View</button>
                    <button className="action-btn edit">Edit</button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {showAddModal && (
        <div className="modal-overlay" onClick={() => setShowAddModal(false)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>Add New Employee</h2>
              <button className="close-btn" onClick={() => setShowAddModal(false)}>×</button>
            </div>
            <form className="modal-form">
              <div className="form-group">
                <label>Full Name</label>
                <input type="text" placeholder="Enter full name" />
              </div>
              <div className="form-group">
                <label>Email</label>
                <input type="email" placeholder="Enter email" />
              </div>
              <div className="form-row">
                <div className="form-group">
                  <label>Department</label>
                  <select>
                    <option value="">Select department</option>
                    <option value="Engineering">Engineering</option>
                    <option value="HR">HR</option>
                    <option value="Sales">Sales</option>
                    <option value="Marketing">Marketing</option>
                  </select>
                </div>
                <div className="form-group">
                  <label>Designation</label>
                  <input type="text" placeholder="Enter designation" />
                </div>
              </div>
              <div className="form-group">
                <label>Join Date</label>
                <input type="date" />
              </div>
              <div className="modal-actions">
                <button type="button" className="cancel-btn" onClick={() => setShowAddModal(false)}>
                  Cancel
                </button>
                <button type="submit" className="submit-btn">
                  Add Employee
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default EmployeesPage;
