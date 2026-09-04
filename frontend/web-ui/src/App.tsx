import { Link, Outlet } from 'react-router-dom';

export function App() {
  return (
    <div className="shell">
      <header className="shell__header">
        <span className="shell__brand">Payment Data Readiness</span>
        <nav>
          <Link to="/release-notes">Release notes</Link>
        </nav>
      </header>
      <main className="shell__main">
        <Outlet />
      </main>
    </div>
  );
}
