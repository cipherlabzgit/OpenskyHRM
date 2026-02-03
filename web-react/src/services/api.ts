import axios from 'axios';

const PLATFORM_API_URL = 'http://localhost:5000/api/v1';
const TENANT_API_URL = 'http://localhost:5001/api/v1';

const platformApi = axios.create({
  baseURL: PLATFORM_API_URL,
  headers: { 'Content-Type': 'application/json' }
});

const tenantApi = axios.create({
  baseURL: TENANT_API_URL,
  headers: { 'Content-Type': 'application/json' }
});

// Add auth interceptor - skip for login endpoint (tenant code is in body)
tenantApi.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  const tenantCode = localStorage.getItem('tenantCode');
  
  // Skip adding stored tenant code for login requests
  const isLoginRequest = config.url?.includes('/auth/login');
  
  if (token && !isLoginRequest) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  if (tenantCode && !isLoginRequest) {
    config.headers['X-Tenant-Code'] = tenantCode;
  }
  return config;
});

const api = {
  // Platform APIs
  registerTenant: async (data: {
    companyName: string;
    legalName: string;
    country?: string;
    timeZone?: string;
    currency?: string;
    adminEmail: string;
    adminPassword: string;
    adminFullName?: string;
  }) => {
    const response = await platformApi.post('/tenant/register', data);
    return response.data;
  },

  // Lookup tenant by email
  lookupTenant: async (email: string): Promise<{ tenantCode: string; companyName: string } | null> => {
    try {
      const response = await platformApi.get('/tenant/lookup', {
        params: { email: email.toLowerCase() },
        timeout: 10000 // 10 second timeout
      });
      return response.data;
    } catch (error: any) {
      if (error.response?.status === 404) {
        return null;
      }
      // Re-throw with more context
      if (error.code === 'ECONNREFUSED' || error.message?.includes('Network Error')) {
        throw new Error('Network Error: Unable to connect to Platform API. Please ensure the API is running on http://localhost:5000');
      }
      throw error;
    }
  },

  // Auth APIs - now automatically looks up tenant by email
  login: async (email: string, password: string) => {
    try {
      // First lookup tenant by email
      const tenant = await api.lookupTenant(email);
      if (!tenant) {
        throw new Error('No account found for this email address. Please register first.');
      }
      
      const tenantCode = tenant.tenantCode;
      const response = await tenantApi.post('/auth/login', {
        tenantCode,
        email: email.toLowerCase(),
        password
      }, {
        headers: { 'X-Tenant-Code': tenantCode },
        timeout: 10000 // 10 second timeout
      });
      
      // Store tenant code for future API calls
      localStorage.setItem('tenantCode', tenantCode);
      
      return response.data;
    } catch (error: any) {
      // Provide more specific error messages
      if (error.code === 'ECONNREFUSED' || error.message?.includes('Network Error') || error.message?.includes('Failed to fetch')) {
        throw new Error('Network Error: Unable to connect to Tenant API. Please ensure the API is running on http://localhost:5001');
      }
      throw error;
    }
  },

  // Employee APIs
  getEmployees: async () => {
    const response = await tenantApi.get('/employees');
    return response.data;
  },

  createEmployee: async (data: any) => {
    const response = await tenantApi.post('/employees', data);
    return response.data;
  },

  // Leave APIs
  getLeaveRequests: async () => {
    const response = await tenantApi.get('/leave/requests');
    return response.data;
  },

  createLeaveRequest: async (data: any) => {
    const response = await tenantApi.post('/leave/requests', data);
    return response.data;
  },

  // Attendance APIs
  getAttendanceRecords: async () => {
    const response = await tenantApi.get('/attendance/records');
    return response.data;
  },

  clockIn: async () => {
    const response = await tenantApi.post('/attendance/clock-in');
    return response.data;
  },

  clockOut: async () => {
    const response = await tenantApi.post('/attendance/clock-out');
    return response.data;
  },

  // Recruitment APIs
  getJobRequisitions: async (params?: { status?: string }) => {
    const response = await tenantApi.get('/recruitment/requisitions', { params });
    return response.data;
  },

  createJobRequisition: async (data: any) => {
    const response = await tenantApi.post('/recruitment/requisitions', data);
    return response.data;
  },

  getJobRequisition: async (id: string) => {
    const response = await tenantApi.get(`/recruitment/requisitions/${id}`);
    return response.data;
  },

  updateJobRequisition: async (id: string, data: any) => {
    const response = await tenantApi.put(`/recruitment/requisitions/${id}`, data);
    return response.data;
  },

  updateJobRequisitionStatus: async (id: string, status: string) => {
    const response = await tenantApi.put(`/recruitment/requisitions/${id}/status`, { status });
    return response.data;
  },

  approveJobRequisition: async (id: string, data: any) => {
    const response = await tenantApi.post(`/recruitment/requisitions/${id}/approve`, data);
    return response.data;
  },

  getCandidates: async () => {
    const response = await tenantApi.get('/recruitment/candidates');
    return response.data;
  },

  createCandidate: async (data: any) => {
    const response = await tenantApi.post('/recruitment/candidates', data);
    return response.data;
  },

  updateApplicationStage: async (id: string, stage: string) => {
    const response = await tenantApi.put(`/recruitment/applications/${id}/stage`, { stage });
    return response.data;
  },

  getInterviews: async (params?: { applicationId?: string }) => {
    const response = await tenantApi.get('/recruitment/interviews', { params });
    return response.data;
  },

  createInterview: async (data: any) => {
    const response = await tenantApi.post('/recruitment/interviews', data);
    return response.data;
  },

  submitInterviewFeedback: async (id: string, data: any) => {
    const response = await tenantApi.put(`/recruitment/interviews/${id}/feedback`, data);
    return response.data;
  },

  getOffers: async (params?: { applicationId?: string }) => {
    const response = await tenantApi.get('/recruitment/offers', { params });
    return response.data;
  },

  createOffer: async (data: any) => {
    const response = await tenantApi.post('/recruitment/offers', data);
    return response.data;
  },

  updateOfferStatus: async (id: string, status: string) => {
    const response = await tenantApi.put(`/recruitment/offers/${id}/status`, { status });
    return response.data;
  },

  convertToEmployee: async (id: string) => {
    const response = await tenantApi.post(`/recruitment/offers/${id}/convert-to-employee`);
    return response.data;
  },

  // Department APIs
  getDepartments: async () => {
    const response = await tenantApi.get('/departments');
    return response.data;
  },

  createDepartment: async (data: { name: string; code?: string; parentId?: string }) => {
    const response = await tenantApi.post('/departments', data);
    return response.data;
  },

  updateDepartment: async (id: string, data: { name: string; code?: string; parentId?: string }) => {
    const response = await tenantApi.put(`/departments/${id}`, data);
    return response.data;
  },

  deleteDepartment: async (id: string) => {
    const response = await tenantApi.delete(`/departments/${id}`);
    return response.data;
  },

  // Designation APIs
  getDesignations: async () => {
    const response = await tenantApi.get('/designations');
    return response.data;
  },

  createDesignation: async (data: { name: string; code?: string; level?: number }) => {
    const response = await tenantApi.post('/designations', data);
    return response.data;
  },

  updateDesignation: async (id: string, data: { name: string; code?: string; level?: number }) => {
    const response = await tenantApi.put(`/designations/${id}`, data);
    return response.data;
  },

  deleteDesignation: async (id: string) => {
    const response = await tenantApi.delete(`/designations/${id}`);
    return response.data;
  },

  // Expose tenantApi for direct access (for public pages like career portal)
  tenant: tenantApi
};

export default api;
