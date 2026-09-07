import { beforeEach, describe, expect, it } from 'vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { App } from './App';

const renderAt = (path: string) =>
  render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route element={<App />} path="/">
          <Route element={<p>page</p>} path="remediation" />
          <Route element={<p>page</p>} path="readiness" />
        </Route>
      </Routes>
    </MemoryRouter>,
  );

describe('App shell', () => {
  beforeEach(() => {
    localStorage.clear();
    delete document.documentElement.dataset.theme;
  });

  it('groups the navigation and links every page', () => {
    renderAt('/readiness');

    for (const group of ['Assess', 'Remediate', 'Prove', 'Govern', 'Platform']) {
      expect(screen.getByRole('navigation', { name: group })).toBeTruthy();
    }

    expect(screen.getByRole('link', { name: 'Write-back' }).getAttribute('href')).toBe('/writeback');
  });

  it('marks the current page as active and shows it in the breadcrumb', () => {
    const { container } = renderAt('/remediation');

    expect(screen.getByRole('link', { name: 'Remediation' }).className).toContain('is-active');
    expect(screen.getByRole('link', { name: 'Readiness' }).className).not.toContain('is-active');
    expect(container.querySelector('.topbar__crumbs')?.textContent).toBe('Remediate/Remediation');
  });

  it('toggles the theme on the document element', () => {
    renderAt('/readiness');
    expect(document.documentElement.dataset.theme).toBe('light');

    fireEvent.click(screen.getByRole('button', { name: 'Switch to dark theme' }));

    expect(document.documentElement.dataset.theme).toBe('dark');
    expect(localStorage.getItem('pdr-theme')).toBe('dark');
  });

  it('collapses the sidebar to icons only', () => {
    const { container } = renderAt('/readiness');
    const shell = container.querySelector('.shell')!;
    expect(shell.getAttribute('data-collapsed')).toBe('false');

    fireEvent.click(screen.getByRole('button', { name: 'Collapse navigation' }));

    expect(shell.getAttribute('data-collapsed')).toBe('true');
    expect(screen.getByRole('button', { name: 'Expand navigation' })).toBeTruthy();
  });
});
