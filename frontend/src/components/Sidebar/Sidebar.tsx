import { NavLink, useNavigate } from 'react-router-dom';
import { useAuthStore } from '../../store/authStore';
import './Sidebar.css';

const NAV_ITEMS = [
  { to: '/dashboard', icon: '📊', label: 'Dashboard' },
  { to: '/menu', icon: '🍜', label: 'Thực đơn' },
  { to: '/ai-test', icon: '🤖', label: 'AI Console' },
];

export default function Sidebar() {
  const { email, logout } = useAuthStore();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <aside className="sidebar">
      <div className="sidebar__logo">
        <span className="sidebar__logo-icon">🤖</span>
        <span className="sidebar__logo-text">FoodBot</span>
      </div>

      <nav className="sidebar__nav">
        {NAV_ITEMS.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            className={({ isActive }) =>
              `sidebar__nav-item ${isActive ? 'sidebar__nav-item--active' : ''}`
            }
          >
            <span className="sidebar__nav-icon">{item.icon}</span>
            <span className="sidebar__nav-label">{item.label}</span>
          </NavLink>
        ))}
      </nav>

      <div className="sidebar__footer">
        <div className="sidebar__user">
          <div className="sidebar__avatar">
            {email?.charAt(0).toUpperCase() ?? 'A'}
          </div>
          <div className="sidebar__user-info">
            <span className="sidebar__user-email">{email}</span>
            <span className="sidebar__user-role">Admin</span>
          </div>
        </div>
        <button
          id="logout-btn"
          className="sidebar__logout"
          onClick={handleLogout}
          title="Đăng xuất"
        >
          ⏻
        </button>
      </div>
    </aside>
  );
}
