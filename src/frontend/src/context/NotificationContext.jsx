import { createContext, useCallback, useContext, useEffect, useRef, useState } from 'react';
import { HubConnectionBuilder, HttpTransportType, LogLevel } from '@microsoft/signalr';
import { notificationApi } from '../api/notifications';
import { useAuth } from './AuthContext';

const RENDER_GATEWAY_FALLBACK = 'https://api-gateway-xi3u.onrender.com';

const isLocalHost =
  window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1';

const API_BASE =
  import.meta.env.VITE_API_URL || (isLocalHost ? 'http://localhost:5050' : RENDER_GATEWAY_FALLBACK);

const NotificationContext = createContext(null);

export function NotificationProvider({ children }) {
  const { user } = useAuth();
  const [notifications, setNotifications] = useState([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const connectionRef = useRef(null);

  const refresh = useCallback(async () => {
    if (!user) {
      setNotifications([]);
      setUnreadCount(0);
      return;
    }
    try {
      const [listRes, countRes] = await Promise.all([
        notificationApi.list({ take: 20, skip: 0 }),
        notificationApi.unreadCount(),
      ]);
      setNotifications(listRes.data || []);
      setUnreadCount(countRes.data?.count ?? 0);
    } catch {
      // Silently degrade — bell will simply show no notifications.
    }
  }, [user]);

  const markRead = useCallback(
    async (id) => {
      try {
        await notificationApi.markRead(id);
        setNotifications((prev) =>
          prev.map((n) => (n.id === id && !n.isRead ? { ...n, isRead: true } : n))
        );
        setUnreadCount((c) => Math.max(0, c - 1));
      } catch {
        /* ignore */
      }
    },
    []
  );

  const markAllRead = useCallback(async () => {
    try {
      await notificationApi.markAllRead();
      setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })));
      setUnreadCount(0);
    } catch {
      /* ignore */
    }
  }, []);

  useEffect(() => {
    if (!user) {
      if (connectionRef.current) {
        connectionRef.current.stop().catch(() => {});
        connectionRef.current = null;
      }
      setNotifications([]);
      setUnreadCount(0);
      return undefined;
    }

    refresh();

    const token = localStorage.getItem('token');
    if (!token) return undefined;

    const connection = new HubConnectionBuilder()
      .withUrl(`${API_BASE}/hubs/notifications`, {
        accessTokenFactory: () => localStorage.getItem('token') || '',
        transport: HttpTransportType.WebSockets | HttpTransportType.LongPolling,
        skipNegotiation: false,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on('ReceiveNotification', (notification) => {
      setNotifications((prev) => [notification, ...prev].slice(0, 50));
      if (!notification.isRead) {
        setUnreadCount((c) => c + 1);
      }
    });

    connection
      .start()
      .catch(() => {
        // Connection failed — REST polling on refresh() still works.
      });

    connectionRef.current = connection;

    return () => {
      connection.stop().catch(() => {});
      if (connectionRef.current === connection) {
        connectionRef.current = null;
      }
    };
  }, [user, refresh]);

  return (
    <NotificationContext.Provider
      value={{ notifications, unreadCount, markRead, markAllRead, refresh }}
    >
      {children}
    </NotificationContext.Provider>
  );
}

export const useNotifications = () => {
  const ctx = useContext(NotificationContext);
  if (!ctx) {
    return { notifications: [], unreadCount: 0, markRead: () => {}, markAllRead: () => {}, refresh: () => {} };
  }
  return ctx;
};
