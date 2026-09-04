import { describe, expect, it, vi } from 'vitest';

const apiGet = vi.fn();
const apiPost = vi.fn();
vi.mock('./client', () => ({
  apiGet: (path: string) => apiGet(path),
  apiPost: (path: string, body?: unknown) => apiPost(path, body),
}));

const { getDeliveries, getNotifications, rotateSecret, setSubscriptionEnabled } = await import('./notifications');

describe('notifications api', () => {
  it('omits an unset delivery status so the API returns every attempt', async () => {
    await getDeliveries(2, '');
    expect(apiGet).toHaveBeenCalledWith('/api/v1/notifications/deliveries?page=2');

    await getDeliveries(1, 'DeadLettered');
    expect(apiGet).toHaveBeenCalledWith('/api/v1/notifications/deliveries?page=1&status=DeadLettered');
  });

  it('encodes event types so dotted patterns survive the query string', async () => {
    await getNotifications(1, 'remediation.case.approved');

    expect(apiGet).toHaveBeenCalledWith('/api/v1/notifications/events?page=1&eventType=remediation.case.approved');
  });

  it('sends the enable flag rather than a bare toggle', async () => {
    await setSubscriptionEnabled('OPS', false);

    expect(apiPost).toHaveBeenCalledWith('/api/v1/notifications/subscriptions/OPS/enabled', { enabled: false });
  });

  it('rotates a signing secret through the dedicated endpoint', async () => {
    await rotateSecret('OPS', 'new-secret');

    expect(apiPost).toHaveBeenCalledWith('/api/v1/notifications/subscriptions/OPS/secret', { secret: 'new-secret' });
  });
});
