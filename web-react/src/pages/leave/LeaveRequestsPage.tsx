import React, { useState } from 'react';
import './LeavePages.css';

const LeaveRequestsPage: React.FC = () => {
  const [showModal, setShowModal] = useState(false);
  
  const leaveRequests = [
    { id: 1, employee: 'John Smith', type: 'Annual Leave', startDate: '2024-02-15', endDate: '2024-02-20', days: 5, status: 'Pending' },
    { id: 2, employee: 'Sarah Johnson', type: 'Sick Leave', startDate: '2024-02-10', endDate: '2024-02-11', days: 2, status: 'Approved' },
    { id: 3, employee: 'Mike Brown', type: 'Personal Leave', startDate: '2024-02-25', endDate: '2024-02-25', days: 1, status: 'Pending' },
  ];

  return (
    <div className="leave-page">
      <div className="page-actions">
        <button className="add-btn" onClick={() => setShowModal(true)}>
          + New Leave Request
        </button>
      </div>

      <div className="leave-table-container">
        <table className="leave-table">
          <thead>
            <tr>
              <th>Employee</th>
              <th>Leave Type</th>
              <th>Start Date</th>
              <th>End Date</th>
              <th>Days</th>
              <th>Status</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {leaveRequests.map((request) => (
              <tr key={request.id}>
                <td>{request.employee}</td>
                <td>{request.type}</td>
                <td>{request.startDate}</td>
                <td>{request.endDate}</td>
                <td>{request.days}</td>
                <td>
                  <span className={`status-badge ${request.status.toLowerCase()}`}>
                    {request.status}
                  </span>
                </td>
                <td>
                  <div className="action-buttons">
                    {request.status === 'Pending' && (
                      <>
                        <button className="action-btn approve">Approve</button>
                        <button className="action-btn reject">Reject</button>
                      </>
                    )}
                    <button className="action-btn view">View</button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {showModal && (
        <div className="modal-overlay" onClick={() => setShowModal(false)}>
          <div className="modal" onClick={e => e.stopPropagation()}>
            <div className="modal-header">
              <h2>New Leave Request</h2>
              <button className="close-btn" onClick={() => setShowModal(false)}>×</button>
            </div>
            <form className="modal-form">
              <div className="form-group">
                <label>Leave Type</label>
                <select>
                  <option value="">Select leave type</option>
                  <option value="annual">Annual Leave</option>
                  <option value="sick">Sick Leave</option>
                  <option value="personal">Personal Leave</option>
                </select>
              </div>
              <div className="form-row">
                <div className="form-group">
                  <label>Start Date</label>
                  <input type="date" />
                </div>
                <div className="form-group">
                  <label>End Date</label>
                  <input type="date" />
                </div>
              </div>
              <div className="form-group">
                <label>Reason</label>
                <textarea rows={3} placeholder="Enter reason for leave"></textarea>
              </div>
              <div className="modal-actions">
                <button type="button" className="cancel-btn" onClick={() => setShowModal(false)}>Cancel</button>
                <button type="submit" className="submit-btn">Submit Request</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default LeaveRequestsPage;
