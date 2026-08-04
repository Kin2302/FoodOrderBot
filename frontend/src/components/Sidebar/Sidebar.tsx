import { NavLink, useNavigate } from 'react-router-dom';
import { useAuthStore } from '../../store/authStore';
import './Sidebar.css';

// ─── SVG Icons ──────────────────────────────────────────────────────────────
function IconDashboard() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="3" width="7" height="7" rx="1.5" />
      <rect x="14" y="3" width="7" height="7" rx="1.5" />
      <rect x="3" y="14" width="7" height="7" rx="1.5" />
      <rect x="14" y="14" width="7" height="7" rx="1.5" />
    </svg>
  );
}

function IconAnalytics() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <polyline points="22 12 18 12 15 21 9 3 6 12 2 12" />
    </svg>
  );
}

function IconComplaints() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z" />
      <line x1="12" y1="9" x2="12" y2="13" />
      <line x1="12" y1="17" x2="12.01" y2="17" />
    </svg>
  );
}

function IconMenu() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <path d="M3 2h18l-2 7H5L3 2z" />
      <path d="M5 9l1 13h12l1-13" />
      <path d="M9 9v4" />
      <path d="M15 9v4" />
    </svg>
  );
}

function IconAI() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <rect x="2" y="3" width="20" height="14" rx="2.5" />
      <path d="M8 21h8" />
      <path d="M12 17v4" />
      <circle cx="9" cy="10" r="1" fill="currentColor" stroke="none" />
      <circle cx="15" cy="10" r="1" fill="currentColor" stroke="none" />
      <path d="M9 13s1 1.5 3 1.5 3-1.5 3-1.5" />
    </svg>
  );
}

function IconLogout() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4" />
      <polyline points="16 17 21 12 16 7" />
      <line x1="21" y1="12" x2="9" y2="12" />
    </svg>
  );
}

function IconBrand() {
  return (
    <svg width="28" height="28" viewBox="0 0 32 32" fill="none">
      <rect width="32" height="32" rx="9" fill="#10b981" />
      <path d="M9 11h14M9 16h10M9 21h7" stroke="#fff" strokeWidth="2.2" strokeLinecap="round" />
      <circle cx="24" cy="21" r="4" fill="#fff" fillOpacity="0.9" />
      <path d="M22.5 21l1 1 2.5-2.5" stroke="#10b981" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

// ─── Nav Config ──────────────────────────────────────────────────────────────
const NAV_ITEMS = [
  { to: '/dashboard',  Icon: IconDashboard,  label: 'Dashboard' },
  { to: '/analytics',  Icon: IconAnalytics,  label: 'Thống kê' },
  { to: '/complaints', Icon: IconComplaints, label: 'Khiếu nại' },
  { to: '/menu',       Icon: IconMenu,       label: 'Thực đơn' },
  { to: '/ai-test',    Icon: IconAI,         label: 'AI Console' },
];

export default function Sidebar() {
  const { email, logout } = useAuthStore();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const initial = email?.charAt(0).toUpperCase() ?? 'A';

  return (
    <aside className="sidebar">
      <div className="sidebar__logo">
        <IconBrand />
        <span className="sidebar__logo-text">FoodBot</span>
      </div>

      <nav className="sidebar__nav">
        {NAV_ITEMS.map(({ to, Icon, label }) => (
          <NavLink
            key={to}
            to={to}
            className={({ isActive }) =>
              `sidebar__nav-item ${isActive ? 'sidebar__nav-item--active' : ''}`
            }
          >
            <span className="sidebar__nav-icon">
              <Icon />
            </span>
            <span className="sidebar__nav-label">{label}</span>
          </NavLink>
        ))}
      </nav>

      <div className="sidebar__footer">
        <div className="sidebar__user">
          <div className="sidebar__avatar" aria-label={`Avatar ${email}`}>
            {initial}
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
          <IconLogout />
        </button>
      </div>
    </aside>
  );
}
