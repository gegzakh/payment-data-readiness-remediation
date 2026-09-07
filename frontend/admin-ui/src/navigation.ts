import type { IconName } from './components/Icon';

export interface NavItem {
  path: string;
  label: string;
  icon: IconName;
}

export interface NavGroup {
  label: string;
  items: NavItem[];
}

/** Grouped to follow the delivery story: find the exposure, fix it, prove it, then govern it. */
export const navGroups: NavGroup[] = [
  {
    label: 'Assess',
    items: [
      { path: '/readiness', label: 'Readiness', icon: 'gauge' },
      { path: '/sources', label: 'Sources', icon: 'database' },
      { path: '/ingestion', label: 'Ingestion', icon: 'upload' },
    ],
  },
  {
    label: 'Remediate',
    items: [
      { path: '/remediation', label: 'Remediation', icon: 'wrench' },
      { path: '/writeback', label: 'Write-back', icon: 'repeat' },
    ],
  },
  {
    label: 'Prove',
    items: [
      { path: '/simulation', label: 'Simulation', icon: 'flask' },
      { path: '/testing', label: 'Testing', icon: 'checkCircle' },
      { path: '/cutover', label: 'Cutover', icon: 'rocket' },
    ],
  },
  {
    label: 'Govern',
    items: [
      { path: '/dashboards', label: 'Dashboards', icon: 'barChart' },
      { path: '/notifications', label: 'Notifications', icon: 'bell' },
      { path: '/rules', label: 'Rules', icon: 'scale' },
      { path: '/audit', label: 'Audit', icon: 'shield' },
    ],
  },
  {
    label: 'Platform',
    items: [
      { path: '/releases', label: 'Releases', icon: 'tag' },
      { path: '/settings', label: 'Settings', icon: 'settings' },
    ],
  },
];

export const findNavLocation = (pathname: string): { group: string; item: NavItem } | undefined => {
  for (const group of navGroups) {
    const item = group.items.find((candidate) => pathname.startsWith(candidate.path));
    if (item) {
      return { group: group.label, item };
    }
  }

  return undefined;
};
