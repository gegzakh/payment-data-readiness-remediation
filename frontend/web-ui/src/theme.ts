import { useCallback, useEffect, useState } from 'react';

export type Theme = 'light' | 'dark';

const storageKey = 'pdr-theme';

const preferredTheme = (): Theme => {
  const stored = localStorage.getItem(storageKey);
  if (stored === 'light' || stored === 'dark') {
    return stored;
  }

  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
};

/** Theme lives on the document element so the CSS custom properties switch in one place. */
export function useTheme() {
  const [theme, setTheme] = useState<Theme>(preferredTheme);

  useEffect(() => {
    document.documentElement.dataset.theme = theme;
    localStorage.setItem(storageKey, theme);
  }, [theme]);

  const toggleTheme = useCallback(() => setTheme((current) => (current === 'dark' ? 'light' : 'dark')), []);

  return { theme, toggleTheme };
}
