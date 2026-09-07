import { useState } from 'react';
import { NavLink, Outlet, useLocation } from 'react-router-dom';
import { keycloak } from './auth/keycloak';
import { Icon } from './components/Icon';
import { findNavLocation, navGroups } from './navigation';
import { useTheme } from './theme';

const initials = (name: string) =>
  name
    .split(/[.\-_\s]/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0])
    .join('');

export function App() {
  const [collapsed, setCollapsed] = useState(false);
  const { theme, toggleTheme } = useTheme();
  const { pathname } = useLocation();
  const current = findNavLocation(pathname);
  const username = keycloak.tokenParsed?.preferred_username ?? 'signed in';

  return (
    <div className="shell" data-collapsed={collapsed}>
      <aside className="sidebar">
        <div className="sidebar__brand">
          <span className="sidebar__mark">PDR</span>
          <span className="sidebar__wordmark">
            <strong>Payment Data</strong>
            <span>Readiness</span>
          </span>
        </div>

        <div className="sidebar__scroll">
          {navGroups.map((group) => (
            <nav aria-label={group.label} key={group.label}>
              <p className="sidenav__label">{group.label}</p>
              <ul className="sidenav__list">
                {group.items.map((item) => (
                  <li key={item.path}>
                    <NavLink
                      className={({ isActive }) => (isActive ? 'sidenav__link is-active' : 'sidenav__link')}
                      title={item.label}
                      to={item.path}
                    >
                      <Icon className="sidenav__icon" name={item.icon} />
                      <span>{item.label}</span>
                    </NavLink>
                  </li>
                ))}
              </ul>
            </nav>
          ))}
        </div>
      </aside>

      <div className="shell__body">
        <header className="topbar">
          <button
            aria-label={collapsed ? 'Expand navigation' : 'Collapse navigation'}
            className="icon-button"
            onClick={() => setCollapsed((value) => !value)}
            type="button"
          >
            <Icon name="panelLeft" />
          </button>

          <div className="topbar__crumbs">
            {current && (
              <>
                <span>{current.group}</span>
                <span aria-hidden="true">/</span>
                <strong>{current.item.label}</strong>
              </>
            )}
          </div>

          <div className="topbar__spacer" />

          <div className="topbar__actions">
            <button
              aria-label={theme === 'dark' ? 'Switch to light theme' : 'Switch to dark theme'}
              className="icon-button"
              onClick={toggleTheme}
              type="button"
            >
              <Icon name={theme === 'dark' ? 'sun' : 'moon'} />
            </button>

            <span className="user-chip">
              <span className="user-chip__name">{username}</span>
              <span className="user-chip__avatar">{initials(username)}</span>
            </span>

            <button
              aria-label="Sign out"
              className="icon-button"
              onClick={() => keycloak.logout({ redirectUri: window.location.origin })}
              title="Sign out"
              type="button"
            >
              <Icon name="logOut" />
            </button>
          </div>
        </header>

        <main className="shell__main">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
