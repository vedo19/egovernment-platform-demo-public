import client from './client';

export const notificationApi = {
  list: (params) => client.get('/api/notifications', { params }),
  unreadCount: () => client.get('/api/notifications/unread-count'),
  markRead: (id) => client.put(`/api/notifications/${id}/read`),
  markAllRead: () => client.put('/api/notifications/read-all'),
};
