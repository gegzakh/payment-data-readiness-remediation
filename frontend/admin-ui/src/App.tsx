import { Link, Outlet } from 'react-router-dom';
import { keycloak } from './auth/keycloak';

export function App() {
  return (
    <div className="shell">
      <header className="shell__header">
        <span className="shell__brand">PDR Admin</span>
        <nav>
          <Link to="/releases">Releases</Link> <Link to="/settings">Settings</Link>
        </nav>
        <span>
          {keycloak.tokenParsed?.preferred_username}{' '}
          <button onClick={() => keycloak.logout({ redirectUri: window.location.origin })} type="button">
            Sign out
          </button>
        </span>
      </header>
      <main className="shell__main">
        <Outlet />
      </main>
    </div>
  );
}
