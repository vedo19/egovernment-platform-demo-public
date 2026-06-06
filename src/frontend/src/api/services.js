import client from './client';

export const authApi = {
  register: (data) => client.post('/auth/register', data),
  login: (data) => client.post('/auth/login', data),
  me: () => client.get('/auth/me'),
  users: () => client.get('/auth/users'),
  updateRole: (id, role) => client.put(`/auth/users/${id}/role`, { role }),
};

export const citizenApi = {
  createProfile: (data) => client.post('/citizens/profile', data),
  getProfile: () => client.get('/citizens/profile'),
  updateProfile: (data) => client.put('/citizens/profile', data),
  getAll: () => client.get('/citizens'),
  getByUserId: (userId) => client.get(`/citizens/by-user/${userId}`),
};

export const serviceRequestApi = {
  create: (data) => client.post('/servicerequests', data),
  getMyRequests: () => client.get('/servicerequests/my-requests'),
  getMyAssignments: () => client.get('/servicerequests/my-assignments'),
  getAssignedToMe: () => client.get('/servicerequests/assigned-to-me'),
  getAllRequests: (status) =>
    client.get('/servicerequests/all', { params: status ? { status } : {} }),
  getAll: (status) => client.get('/servicerequests', { params: status ? { status } : {} }),
  getById: (id) => client.get(`/servicerequests/${id}`),
  updateStatus: (id, data) => client.put(`/servicerequests/${id}/status`, data),
  assignOfficer: (id, officerId) => client.put(`/servicerequests/${id}/assign`, { officerId }),
  assignOfficerV2: (id, officerId) =>
    client.put(`/servicerequests/${id}/assign-officer`, { officerId }),
  requestDocuments: (id, officerNote) =>
    client.put(`/servicerequests/${id}/request-documents`, { officerNote }),
  approve: (id) => client.put(`/servicerequests/${id}/approve`),
  rejectDocuments: (id, reason) =>
    client.put(`/servicerequests/${id}/reject-documents`, { reason }),
  reject: (id, reason) => client.put(`/servicerequests/${id}/reject`, { reason }),
  uploadDocument: (id, file) => {
    const formData = new FormData();
    formData.append('file', file);
    return client.post(`/servicerequests/${id}/upload-document`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },
};

export const documentApi = {
  create: (data) => client.post('/documents', data),
  getMyDocuments: () => client.get('/documents/my-documents'),
  getMyAssignments: () => client.get('/documents/my-assignments'),
  getAssignedToMe: () => client.get('/documents/assigned-to-me'),
  getAll: (params) => client.get('/documents', { params }),
  getById: (id) => client.get(`/documents/${id}`),
  updateStatus: (id, data) => client.put(`/documents/${id}/status`, data),
  startReview: (id) => client.put(`/documents/${id}/start-review`),
  approve: (id) => client.put(`/documents/${id}/approve`),
  reject: (id, reason) => client.put(`/documents/${id}/reject`, { reason }),
  assignOfficer: (id, officerId) => client.put(`/documents/${id}/assign`, { officerId }),
  getSupportingFileBlob: (id) =>
    client.get(`/documents/supporting-files/${id}/download`, { responseType: 'blob' }),
  openSupportingFile: async (id) => {
    const response = await client.get(`/documents/supporting-files/${id}/download`, {
      responseType: 'blob',
    });
    const blob = new Blob([response.data], {
      type: response.headers['content-type'] || 'application/pdf',
    });
    const url = window.URL.createObjectURL(blob);
    window.open(url, '_blank', 'noopener,noreferrer');
    window.setTimeout(() => window.URL.revokeObjectURL(url), 30000);
  },
  downloadSupportingFile: async (id, fileName = 'supporting-document.pdf') => {
    const response = await client.get(`/documents/supporting-files/${id}/download`, {
      responseType: 'blob',
    });
    const blob = new Blob([response.data], {
      type: response.headers['content-type'] || 'application/pdf',
    });
    const url = window.URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    window.URL.revokeObjectURL(url);
  },
  download: (id) => client.get(`/documents/${id}/download`, { responseType: 'blob' }),
  preview: (id) => client.get(`/documents/${id}/preview`, { responseType: 'blob' }),
};
