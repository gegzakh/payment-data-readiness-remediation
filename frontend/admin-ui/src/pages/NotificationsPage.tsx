import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import {
  createScheduledReport,
  createSubscription,
  deliveryChannels,
  deliveryStatuses,
  dispatchDeliveries,
  frequencies,
  getDeliveries,
  getNotifications,
  getScheduledReports,
  getSubscriptions,
  replayDelivery,
  rotateSecret,
  runScheduledReport,
  setScheduledReportEnabled,
  setSubscriptionEnabled,
  severities,
  type DeliveryChannel,
  type DeliveryStatus,
  type NotificationSeverity,
  type ScheduleFrequency,
} from '../api/notifications';
import { dashboardAudiences } from '../api/reporting';
import { hasPermission } from '../auth/keycloak';

export function NotificationsPage() {
  const queryClient = useQueryClient();
  const canWrite = hasPermission('notifications.write');
  const canAdmin = hasPermission('notifications.admin');

  const [statusFilter, setStatusFilter] = useState<DeliveryStatus | ''>('');
  const [deliveryPage, setDeliveryPage] = useState(1);
  const [eventPage, setEventPage] = useState(1);
  const [secret, setSecret] = useState({ code: '', value: '' });
  const [subscription, setSubscription] = useState({
    code: '',
    name: '',
    eventPattern: '',
    channel: 'Webhook' as DeliveryChannel,
    target: '',
    schemeCodes: '',
    sourceCodes: '',
    minimumSeverity: 'Info' as NotificationSeverity,
    signingSecret: '',
  });
  const [report, setReport] = useState({
    code: '',
    name: '',
    audience: 'Executive',
    schemeCodes: '',
    sourceCodes: '',
    frequency: 'Daily' as ScheduleFrequency,
    hourUtc: 6,
    dayOfWeek: 1,
    dayOfMonth: 1,
    recipients: '',
  });

  const subscriptions = useQuery({ queryKey: ['subscriptions'], queryFn: getSubscriptions });
  const deliveries = useQuery({
    queryKey: ['deliveries', deliveryPage, statusFilter],
    queryFn: () => getDeliveries(deliveryPage, statusFilter),
  });
  const events = useQuery({ queryKey: ['notification-events', eventPage], queryFn: () => getNotifications(eventPage) });
  const reports = useQuery({ queryKey: ['scheduled-reports'], queryFn: getScheduledReports });

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ['subscriptions'] });
    void queryClient.invalidateQueries({ queryKey: ['deliveries'] });
    void queryClient.invalidateQueries({ queryKey: ['notification-events'] });
    void queryClient.invalidateQueries({ queryKey: ['scheduled-reports'] });
  };

  const addSubscription = useMutation({
    mutationFn: () =>
      createSubscription({
        code: subscription.code,
        name: subscription.name,
        eventPattern: subscription.eventPattern,
        channel: subscription.channel,
        target: subscription.target,
        schemeCodes: subscription.schemeCodes || null,
        sourceCodes: subscription.sourceCodes || null,
        minimumSeverity: subscription.minimumSeverity,
        signingSecret: subscription.signingSecret || null,
      }),
    onSuccess: () => {
      setSubscription({ ...subscription, code: '', name: '', target: '', signingSecret: '' });
      invalidate();
    },
  });
  const toggleSubscription = useMutation({
    mutationFn: (input: { code: string; enabled: boolean }) => setSubscriptionEnabled(input.code, input.enabled),
    onSuccess: invalidate,
  });
  const rotate = useMutation({
    mutationFn: () => rotateSecret(secret.code, secret.value),
    onSuccess: () => {
      setSecret({ code: '', value: '' });
      invalidate();
    },
  });
  const replay = useMutation({ mutationFn: replayDelivery, onSuccess: invalidate });
  const dispatch = useMutation({ mutationFn: dispatchDeliveries, onSuccess: invalidate });
  const addReport = useMutation({
    mutationFn: () =>
      createScheduledReport({
        code: report.code,
        name: report.name,
        audience: report.audience,
        schemeCodes: report.schemeCodes || null,
        sourceCodes: report.sourceCodes || null,
        frequency: report.frequency,
        hourUtc: report.hourUtc,
        dayOfWeek: report.dayOfWeek,
        dayOfMonth: report.dayOfMonth,
        recipients: report.recipients,
      }),
    onSuccess: () => {
      setReport({ ...report, code: '', name: '' });
      invalidate();
    },
  });
  const toggleReport = useMutation({
    mutationFn: (input: { code: string; enabled: boolean }) => setScheduledReportEnabled(input.code, input.enabled),
    onSuccess: invalidate,
  });
  const runReport = useMutation({ mutationFn: runScheduledReport, onSuccess: invalidate });

  return (
    <section>
      <h1>Notifications</h1>
      <p className="muted">
        Who gets told what, through which channel, and what happened to each attempt — signed webhooks and
        ITSM tasks included, with retries, dead letters and replay.
      </p>

      <h2>Subscriptions</h2>
      {subscriptions.isError && <p className="error">{subscriptions.error.message}</p>}
      <table className="table">
        <thead>
          <tr>
            <th>Code</th>
            <th>Name</th>
            <th>Pattern</th>
            <th>Channel</th>
            <th>Target</th>
            <th>Scope</th>
            <th>Min severity</th>
            <th>Signed</th>
            <th>Failures</th>
            <th>Enabled</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {subscriptions.data?.map((item) => (
            <tr key={item.id}>
              <td>{item.code}</td>
              <td>{item.name}</td>
              <td>{item.eventPattern}</td>
              <td>{item.channel}</td>
              <td>{item.target}</td>
              <td>{[item.schemeCodes, item.sourceCodes].filter(Boolean).join(' / ') || 'all'}</td>
              <td>{item.minimumSeverity}</td>
              <td>{item.hasSigningSecret ? 'yes' : 'no'}</td>
              <td className={item.consecutiveFailures > 0 ? 'error' : undefined}>{item.consecutiveFailures}</td>
              <td>{item.isEnabled ? 'yes' : 'no'}</td>
              <td>
                {canWrite && (
                  <button
                    onClick={() => toggleSubscription.mutate({ code: item.code, enabled: !item.isEnabled })}
                    type="button"
                  >
                    {item.isEnabled ? 'Disable' : 'Enable'}
                  </button>
                )}{' '}
                {canAdmin && (
                  <button onClick={() => setSecret({ ...secret, code: item.code })} type="button">
                    Rotate secret
                  </button>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      {canWrite && (
        <div className="card">
          <h3>New subscription</h3>
          <div className="filters">
            <label>
              Code
              <input
                onChange={(event) => setSubscription({ ...subscription, code: event.target.value.toUpperCase() })}
                value={subscription.code}
              />
            </label>
            <label>
              Name
              <input
                onChange={(event) => setSubscription({ ...subscription, name: event.target.value })}
                value={subscription.name}
              />
            </label>
            <label>
              Event pattern
              <input
                onChange={(event) => setSubscription({ ...subscription, eventPattern: event.target.value })}
                placeholder="remediation.* or *"
                value={subscription.eventPattern}
              />
            </label>
            <label>
              Channel
              <select
                onChange={(event) => setSubscription({ ...subscription, channel: event.target.value as DeliveryChannel })}
                value={subscription.channel}
              >
                {deliveryChannels.map((channel) => (
                  <option key={channel} value={channel}>
                    {channel}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Target
              <input
                onChange={(event) => setSubscription({ ...subscription, target: event.target.value })}
                placeholder="https://… or inbox"
                value={subscription.target}
              />
            </label>
            <label>
              Schemes
              <input
                onChange={(event) => setSubscription({ ...subscription, schemeCodes: event.target.value.toUpperCase() })}
                value={subscription.schemeCodes}
              />
            </label>
            <label>
              Sources
              <input
                onChange={(event) => setSubscription({ ...subscription, sourceCodes: event.target.value.toUpperCase() })}
                value={subscription.sourceCodes}
              />
            </label>
            <label>
              Min severity
              <select
                onChange={(event) =>
                  setSubscription({ ...subscription, minimumSeverity: event.target.value as NotificationSeverity })
                }
                value={subscription.minimumSeverity}
              >
                {severities.map((severity) => (
                  <option key={severity} value={severity}>
                    {severity}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Signing secret
              <input
                onChange={(event) => setSubscription({ ...subscription, signingSecret: event.target.value })}
                type="password"
                value={subscription.signingSecret}
              />
            </label>
            <button
              disabled={
                !subscription.code || !subscription.name || !subscription.eventPattern || !subscription.target ||
                addSubscription.isPending
              }
              onClick={() => addSubscription.mutate()}
              type="button"
            >
              Create
            </button>
          </div>
          {addSubscription.isError && <p className="error">{addSubscription.error.message}</p>}
        </div>
      )}

      {canAdmin && secret.code && (
        <div className="card">
          <h3>Rotate signing secret — {secret.code}</h3>
          <div className="filters">
            <label>
              New secret
              <input onChange={(event) => setSecret({ ...secret, value: event.target.value })} type="password" value={secret.value} />
            </label>
            <button disabled={!secret.value || rotate.isPending} onClick={() => rotate.mutate()} type="button">
              Rotate
            </button>
          </div>
          {rotate.isError && <p className="error">{rotate.error.message}</p>}
        </div>
      )}

      <h2>Deliveries</h2>
      <div className="filters">
        <label>
          Status
          <select
            onChange={(event) => {
              setDeliveryPage(1);
              setStatusFilter(event.target.value as DeliveryStatus | '');
            }}
            value={statusFilter}
          >
            <option value="">All</option>
            {deliveryStatuses.map((status) => (
              <option key={status} value={status}>
                {status}
              </option>
            ))}
          </select>
        </label>
        {canAdmin && (
          <button disabled={dispatch.isPending} onClick={() => dispatch.mutate()} type="button">
            Dispatch due now
          </button>
        )}
      </div>
      {dispatch.data && (
        <p className="muted">
          Attempted {dispatch.data.attempted}, delivered {dispatch.data.delivered}, retrying {dispatch.data.retrying},
          dead-lettered {dispatch.data.deadLettered}.
        </p>
      )}
      {deliveries.isError && <p className="error">{deliveries.error.message}</p>}
      <table className="table">
        <thead>
          <tr>
            <th>Queued</th>
            <th>Subscription</th>
            <th>Channel</th>
            <th>Target</th>
            <th>Status</th>
            <th>Attempts</th>
            <th>Next attempt</th>
            <th>Response</th>
            <th>Last error</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {deliveries.data?.items.map((item) => (
            <tr key={item.id}>
              <td>{new Date(item.queuedAtUtc).toLocaleString()}</td>
              <td>{item.subscriptionCode}</td>
              <td>{item.channel}</td>
              <td>{item.target}</td>
              <td className={item.status === 'DeadLettered' ? 'error' : undefined}>{item.status}</td>
              <td>{item.attemptCount}</td>
              <td>{item.nextAttemptAtUtc ? new Date(item.nextAttemptAtUtc).toLocaleString() : '—'}</td>
              <td>{item.responseStatusCode ?? '—'}</td>
              <td>{item.lastError ?? '—'}</td>
              <td>
                {canWrite && item.status !== 'Delivered' && (
                  <button onClick={() => replay.mutate(item.id)} type="button">
                    Replay
                  </button>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      <div className="pager">
        <button disabled={deliveryPage === 1} onClick={() => setDeliveryPage(deliveryPage - 1)} type="button">
          Previous
        </button>
        <span>Page {deliveryPage}</span>
        <button
          disabled={(deliveries.data?.items.length ?? 0) === 0}
          onClick={() => setDeliveryPage(deliveryPage + 1)}
          type="button"
        >
          Next
        </button>
      </div>

      <h2>Published events</h2>
      {events.isError && <p className="error">{events.error.message}</p>}
      <table className="table">
        <thead>
          <tr>
            <th>Occurred</th>
            <th>Event</th>
            <th>Severity</th>
            <th>Subject</th>
            <th>Scope</th>
            <th>Published by</th>
            <th>Deliveries</th>
          </tr>
        </thead>
        <tbody>
          {events.data?.items.map((item) => (
            <tr key={item.id}>
              <td>{new Date(item.occurredAtUtc).toLocaleString()}</td>
              <td>{item.eventType}</td>
              <td className={item.severity === 'Critical' ? 'error' : undefined}>{item.severity}</td>
              <td>{item.subject}</td>
              <td>{[item.schemeCode, item.sourceCode].filter(Boolean).join(' / ') || 'all'}</td>
              <td>{item.publishedBy}</td>
              <td>{item.deliveries.length}</td>
            </tr>
          ))}
        </tbody>
      </table>
      <div className="pager">
        <button disabled={eventPage === 1} onClick={() => setEventPage(eventPage - 1)} type="button">
          Previous
        </button>
        <span>Page {eventPage}</span>
        <button disabled={(events.data?.items.length ?? 0) === 0} onClick={() => setEventPage(eventPage + 1)} type="button">
          Next
        </button>
      </div>

      <h2>Scheduled reports</h2>
      {reports.isError && <p className="error">{reports.error.message}</p>}
      <table className="table">
        <thead>
          <tr>
            <th>Code</th>
            <th>Name</th>
            <th>Audience</th>
            <th>Frequency</th>
            <th>Hour (UTC)</th>
            <th>Recipients</th>
            <th>Last run</th>
            <th>Next run</th>
            <th>Enabled</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {reports.data?.map((item) => (
            <tr key={item.id}>
              <td>{item.code}</td>
              <td>{item.name}</td>
              <td>{item.audience}</td>
              <td>{item.frequency}</td>
              <td>{item.hourUtc}</td>
              <td>{item.recipients}</td>
              <td>{item.lastRunAtUtc ? new Date(item.lastRunAtUtc).toLocaleString() : '—'}</td>
              <td>{item.nextRunAtUtc ? new Date(item.nextRunAtUtc).toLocaleString() : '—'}</td>
              <td>{item.isEnabled ? 'yes' : 'no'}</td>
              <td>
                {canWrite && (
                  <>
                    <button onClick={() => toggleReport.mutate({ code: item.code, enabled: !item.isEnabled })} type="button">
                      {item.isEnabled ? 'Disable' : 'Enable'}
                    </button>{' '}
                    <button onClick={() => runReport.mutate(item.code)} type="button">
                      Run now
                    </button>
                  </>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      {canWrite && (
        <div className="card">
          <h3>New scheduled report</h3>
          <div className="filters">
            <label>
              Code
              <input onChange={(event) => setReport({ ...report, code: event.target.value.toUpperCase() })} value={report.code} />
            </label>
            <label>
              Name <input onChange={(event) => setReport({ ...report, name: event.target.value })} value={report.name} />
            </label>
            <label>
              Audience
              <select onChange={(event) => setReport({ ...report, audience: event.target.value })} value={report.audience}>
                {dashboardAudiences.map((value) => (
                  <option key={value} value={value}>
                    {value}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Frequency
              <select
                onChange={(event) => setReport({ ...report, frequency: event.target.value as ScheduleFrequency })}
                value={report.frequency}
              >
                {frequencies.map((value) => (
                  <option key={value} value={value}>
                    {value}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Hour (UTC)
              <input
                max={23}
                min={0}
                onChange={(event) => setReport({ ...report, hourUtc: Number(event.target.value) })}
                type="number"
                value={report.hourUtc}
              />
            </label>
            <label>
              Day of week
              <input
                max={6}
                min={0}
                onChange={(event) => setReport({ ...report, dayOfWeek: Number(event.target.value) })}
                type="number"
                value={report.dayOfWeek}
              />
            </label>
            <label>
              Day of month
              <input
                max={28}
                min={1}
                onChange={(event) => setReport({ ...report, dayOfMonth: Number(event.target.value) })}
                type="number"
                value={report.dayOfMonth}
              />
            </label>
            <label>
              Recipients
              <input
                onChange={(event) => setReport({ ...report, recipients: event.target.value })}
                placeholder="ops@bank.example"
                value={report.recipients}
              />
            </label>
            <button
              disabled={!report.code || !report.name || !report.recipients || addReport.isPending}
              onClick={() => addReport.mutate()}
              type="button"
            >
              Create
            </button>
          </div>
          {addReport.isError && <p className="error">{addReport.error.message}</p>}
        </div>
      )}
    </section>
  );
}
