import { NavLink, Outlet } from 'react-router-dom';
import { useTheme } from './theme';

export function App() {
  const { theme, toggleTheme } = useTheme();

  return (
    <div className="shell">
      <header className="shell__header">
        <span className="shell__brand">
          <span className="shell__mark">PDR</span>
          Payment Data Readiness
        </span>
        <nav className="shell__nav">
          <NavLink className={({ isActive }) => (isActive ? 'is-active' : undefined)} to="/release-notes">
            Release notes
          </NavLink>
        </nav>
        <span className="shell__spacer" />
        <button
          aria-label={theme === 'dark' ? 'Switch to light theme' : 'Switch to dark theme'}
          className="icon-button"
          onClick={toggleTheme}
          type="button"
        >
          {theme === 'dark' ? (
            <svg aria-hidden="true" fill="none" stroke="currentColor" strokeLinecap="round" viewBox="0 0 24 24">
              <circle cx="12" cy="12" r="4" />
              <path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M6.3 17.7l-1.4 1.4M19.1 4.9l-1.4 1.4" />
            </svg>
          ) : (
            <svg aria-hidden="true" fill="none" stroke="currentColor" strokeLinecap="round" viewBox="0 0 24 24">
              <path d="M12 3a6 6 0 0 0 9 9 9 9 0 1 1-9-9z" />
            </svg>
          )}
        </button>
      </header>
      <main className="shell__main">
        <Outlet />
      </main>
    </div>
  );
}
