import { describe, expect, it } from 'vitest';
import { findNavLocation, navGroups } from './navigation';

describe('navigation', () => {
  it('exposes every routed page exactly once', () => {
    const paths = navGroups.flatMap((group) => group.items.map((item) => item.path));

    expect(paths).toHaveLength(14);
    expect(new Set(paths).size).toBe(paths.length);
  });

  it('resolves the group and item for a route', () => {
    expect(findNavLocation('/writeback')).toEqual({
      group: 'Remediate',
      item: { path: '/writeback', label: 'Write-back', icon: 'repeat' },
    });
  });

  it('resolves nested routes onto their parent item', () => {
    expect(findNavLocation('/audit/12345')?.item.label).toBe('Audit');
  });

  it('returns nothing for an unknown route', () => {
    expect(findNavLocation('/nowhere')).toBeUndefined();
  });
});
