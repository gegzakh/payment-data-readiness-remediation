import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { App } from './App';
import { AuditPage } from './pages/AuditPage';
import { IngestionPage } from './pages/IngestionPage';
import { ReadinessPage } from './pages/ReadinessPage';
import { SourcesPage } from './pages/SourcesPage';
import { ReleasesAdminPage } from './pages/ReleasesAdminPage';
import { RulesAdminPage } from './pages/RulesAdminPage';
import { SettingsPage } from './pages/SettingsPage';
import { initKeycloak } from './auth/keycloak';
import './styles.css';

const queryClient = new QueryClient({ defaultOptions: { queries: { refetchOnWindowFocus: false } } });

const root = createRoot(document.getElementById('root')!);

initKeycloak()
  .then(() =>
    root.render(
      <StrictMode>
        <QueryClientProvider client={queryClient}>
          <BrowserRouter>
            <Routes>
              <Route element={<App />} path="/">
                <Route element={<Navigate replace to="/readiness" />} index />
                <Route element={<ReadinessPage />} path="readiness" />
                <Route element={<SourcesPage />} path="sources" />
                <Route element={<IngestionPage />} path="ingestion" />
                <Route element={<RulesAdminPage />} path="rules" />
                <Route element={<AuditPage />} path="audit" />
                <Route element={<ReleasesAdminPage />} path="releases" />
                <Route element={<SettingsPage />} path="settings" />
              </Route>
            </Routes>
          </BrowserRouter>
        </QueryClientProvider>
      </StrictMode>,
    ),
  )
  .catch(() => root.render(<p className="error">Unable to reach Keycloak. Is the dev stack running?</p>));
