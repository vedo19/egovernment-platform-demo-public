import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

const sidebarItems = {
  Citizen: [
    { label: 'Profile', to: '/citizen', tab: 'profile' },
    { label: 'Service Requests', to: '/citizen', tab: 'requests' },
    { label: 'Document Requests', to: '/citizen', tab: 'documents' },
  ],
  Admin: [
    { label: 'Service Requests', to: '/admin', tab: 'requests' },
    { label: 'Document Requests', to: '/admin', tab: 'documents' },
    { label: 'Users', to: '/admin', tab: 'users' },
  ],
  Officer: [
    { label: 'My Requests', to: '/officer', tab: 'requests' },
    { label: 'My Documents', to: '/officer', tab: 'documents' },
  ],
};

export default function Sidebar() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const currentTab = new URLSearchParams(location.search).get('tab');
  const items = sidebarItems[user?.role] || [];

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <aside className="sidebar">
      <div className="sidebar-brand">E-Government Platform</div>

      <nav className="sidebar-nav">
        {items.map((item) => {
          const isActive = location.pathname === item.to && currentTab === item.tab;

          return (
            <Link
              key={item.tab}
              to={`${item.to}?tab=${item.tab}`}
              className={isActive ? 'active' : ''}
            >
              {item.label}
            </Link>
          );
        })}
      </nav>

      <div className="sidebar-user">
        <span>{user?.fullName}</span>
        <small>{user?.role}</small>
        <button onClick={handleLogout} className="btn btn-logout">
          Logout
        </button>
      </div>
    </aside>
  );
}
