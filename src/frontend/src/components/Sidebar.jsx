import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { USER_ROLES } from '../domain/roles';

const sidebarItems = {
  [USER_ROLES.CITIZEN]: [
    { label: 'Overview', to: '/citizen', tab: 'overview' },
    { label: 'Profile', to: '/citizen', tab: 'profile' },
    { label: 'Service Requests', to: '/citizen', tab: 'requests' },
    { label: 'Document Requests', to: '/citizen', tab: 'documents' },
  ],
  [USER_ROLES.ADMIN]: [
    { label: 'Overview', to: '/admin', tab: 'overview' },
    { label: 'Service Requests', to: '/admin', tab: 'requests' },
    { label: 'Document Requests', to: '/admin', tab: 'documents' },
    { label: 'Users', to: '/admin', tab: 'users' },
  ],
  [USER_ROLES.OFFICER]: [
    { label: 'Overview', to: '/officer', tab: 'overview' },
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
          const isActive =
            location.pathname === item.to &&
            (currentTab === item.tab || (!currentTab && item.tab === 'overview'));

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
