// k6 smoke test: enough traffic to catch a service that only works for one request at a time
// (connection-pool exhaustion, per-request DbContext misuse, a snapshot cache that thrashes),
// not a capacity benchmark.
//
//   k6 run -e TOKEN="$(scripts/token.sh pdr-admin)" deploy/load/smoke.js
//
// Without TOKEN only the anonymous surface (health, public release notes) is exercised.

import http from 'k6/http';
import { check, group } from 'k6';

const gateway = __ENV.GATEWAY || 'http://localhost:5100';
const token = __ENV.TOKEN || '';

export const options = {
  scenarios: {
    steady: { executor: 'constant-vus', vus: Number(__ENV.VUS || 10), duration: __ENV.DURATION || '30s' },
  },
  thresholds: {
    // The rate limiter permits 600 requests/minute per caller, so a smoke run must stay inside it;
    // a 429 here means the limiter is misconfigured, not that the service is slow.
    http_req_failed: ['rate<0.01'],
    'http_req_duration{kind:read}': ['p(95)<800'],
    'http_req_duration{kind:dashboard}': ['p(95)<2000'],
  },
};

const authorized = token ? { headers: { Authorization: `Bearer ${token}` } } : null;

function get(path, kind) {
  const params = { tags: { kind }, ...(authorized || {}) };
  return http.get(`${gateway}${path}`, params);
}

export default function () {
  group('anonymous', () => {
    check(get('/health/ready', 'read'), { 'gateway ready': (r) => r.status === 200 });
    check(get('/api/v1/releases?page=1&pageSize=20', 'read'), {
      'release notes served': (r) => r.status === 200,
    });
  });

  if (!authorized) {
    return;
  }

  group('authorized', () => {
    check(get('/api/v1/validation/readiness', 'read'), { readiness: (r) => r.status === 200 });
    check(get('/api/v1/remediation/cases?page=1', 'read'), { cases: (r) => r.status === 200 });
    check(get('/api/v1/simulation/scenarios', 'read'), { scenarios: (r) => r.status === 200 });

    // Dashboards compose three upstream services, so they are the first thing to degrade;
    // the second call should be served from the snapshot cache within the freshness window.
    check(get('/api/v1/reporting/dashboards/executive', 'dashboard'), {
      dashboard: (r) => r.status === 200,
    });
    check(get('/api/v1/reporting/dashboards/executive', 'dashboard'), {
      'dashboard cached': (r) => r.status === 200,
    });
  });
}
