import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function Layout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <div className="app">
      <nav className="navbar">
        <div className="nav-brand">E-Government Platform</div>
        <div className="nav-links">
          {user?.role === 'Citizen' && <NavLink to="/citizen">Dashboard</NavLink>}
          {user?.role === 'Admin' && <NavLink to="/admin">Dashboard</NavLink>}
          {user?.role === 'Officer' && <NavLink to="/officer">Dashboard</NavLink>}
        </div>
        <div className="nav-user">
          <span>
            {user?.fullName} ({user?.role})
          </span>
          <button onClick={handleLogout} className="btn btn-logout">
            Logout
          </button>
        </div>
      </nav>
      <main className="main-content">
        <Outlet />
      </main>
    </div>
  );
}
