import { describe, expect, it } from 'vitest';
import { releasesPath } from './releases';

describe('releasesPath', () => {
  it('omits filters that are not set', () => {
    expect(releasesPath({ page: 2 })).toBe('/api/v1/releases?page=2');
  });

  it('carries pagination and filters', () => {
    expect(releasesPath({ page: 1, pageSize: 10, component: 'Validation', type: 'Fix' })).toBe(
      '/api/v1/releases?page=1&pageSize=10&type=Fix&component=Validation',
    );
  });
});
