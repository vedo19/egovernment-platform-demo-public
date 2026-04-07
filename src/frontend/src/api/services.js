import client from './client';

export const authApi = {
  register: (data) => client.post('/api/auth/register', data),
  login: (data) => client.post('/api/auth/login', data),
  me: () => client.get('/api/auth/me'),
  users: () => client.get('/api/auth/users'),
  updateRole: (id, role) => client.put(`/api/auth/users/${id}/role`, { role }),
};

export const citizenApi = {
  createProfile: (data) => client.post('/api/citizens/profile', data),
  getProfile: () => client.get('/api/citizens/profile'),
  updateProfile: (data) => client.put('/api/citizens/profile', data),
  getAll: () => client.get('/api/citizens'),
  getByUserId: (userId) => client.get(`/api/citizens/by-user/${userId}`),
};

export const serviceRequestApi = {
  create: (data) => client.post('/api/servicerequests', data),
  getMyRequests: () => client.get('/api/servicerequests/my-requests'),
  getMyAssignments: () => client.get('/api/servicerequests/my-assignments'),
  getAssignedToMe: () => client.get('/api/servicerequests/assigned-to-me'),
  getAllRequests: (status) =>
    client.get('/api/servicerequests/all', { params: status ? { status } : {} }),
  getAll: (status) =>
    client.get('/api/servicerequests', { params: status ? { status } : {} }),
  getById: (id) => client.get(`/api/servicerequests/${id}`),
  updateStatus: (id, data) =>
    client.put(`/api/servicerequests/${id}/status`, data),
  assignOfficer: (id, officerId) =>
    client.put(`/api/servicerequests/${id}/assign`, { officerId }),
  assignOfficerV2: (id, officerId) =>
    client.put(`/api/servicerequests/${id}/assign-officer`, { officerId }),
  requestDocuments: (id, officerNote) =>
    client.put(`/api/servicerequests/${id}/request-documents`, { officerNote }),
  approve: (id) => client.put(`/api/servicerequests/${id}/approve`),
  rejectDocuments: (id, reason) => client.put(`/api/servicerequests/${id}/reject-documents`, { reason }),
  reject: (id, reason) => client.put(`/api/servicerequests/${id}/reject`, { reason }),
  uploadDocument: (id, file) => {
    const formData = new FormData();
    formData.append('file', file);
    return client.post(`/api/servicerequests/${id}/upload-document`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },
};

export const documentApi = {
  create: (data) => client.post('/api/documents', data),
  getMyDocuments: () => client.get('/api/documents/my-documents'),
  getMyAssignments: () => client.get('/api/documents/my-assignments'),
  getAssignedToMe: () => client.get('/api/documents/assigned-to-me'),
  getAll: (params) => client.get('/api/documents', { params }),
  getById: (id) => client.get(`/api/documents/${id}`),
  updateStatus: (id, data) => client.put(`/api/documents/${id}/status`, data),
  startReview: (id) => client.put(`/api/documents/${id}/start-review`),
  approve: (id) => client.put(`/api/documents/${id}/approve`),
  reject: (id, reason) => client.put(`/api/documents/${id}/reject`, { reason }),
  assignOfficer: (id, officerId) =>
    client.put(`/api/documents/${id}/assign`, { officerId }),
};
