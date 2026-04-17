import axios from 'axios';

const RENDER_GATEWAY_FALLBACK = 'https://api-gateway-xi3u.onrender.com';

const isLocalHost =
  window.location.hostname === 'localhost' ||
  window.location.hostname === '127.0.0.1';

const API_BASE =
  import.meta.env.VITE_API_URL ||
  (isLocalHost ? 'http://localhost:5050' : RENDER_GATEWAY_FALLBACK);

const client = axios.create({
  baseURL: API_BASE,
  headers: { 'Content-Type': 'application/json' },
});

client.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

client.interceptors.response.use(
  (res) => res,
  (err) => {
    const requestUrl = err.config?.url || '';
    const isAuthRequest =
      requestUrl.includes('/api/auth/login') || requestUrl.includes('/api/auth/register');

    if (err.response?.status === 401 && !isAuthRequest) {
      localStorage.removeItem('token');
      localStorage.removeItem('user');

      if (window.location.pathname !== '/login' && window.location.pathname !== '/register') {
        window.location.href = '/login';
      }
    }
    return Promise.reject(err);
  }
);

export default client;
