import { apiGet, apiPost } from './client';
import type { PagedResult } from './releases';

export type DeliveryChannel = 'InApp' | 'Email' | 'Webhook' | 'ItsmTask';
export type DeliveryStatus = 'Pending' | 'Delivered' | 'Retrying' | 'DeadLettered';
export type ScheduleFrequency = 'Daily' | 'Weekly' | 'Monthly';
export type NotificationSeverity = 'Info' | 'Warning' | 'Critical';

export const deliveryChannels: DeliveryChannel[] = ['InApp', 'Email', 'Webhook', 'ItsmTask'];
export const deliveryStatuses: DeliveryStatus[] = ['Pending', 'Delivered', 'Retrying', 'DeadLettered'];
export const severities: NotificationSeverity[] = ['Info', 'Warning', 'Critical'];
export const frequencies: ScheduleFrequency[] = ['Daily', 'Weekly', 'Monthly'];

export interface SubscriptionDto {
  id: string;
  code: string;
  name: string;
  eventPattern: string;
  channel: DeliveryChannel;
  target: string;
  schemeCodes?: string | null;
  sourceCodes?: string | null;
  minimumSeverity: NotificationSeverity;
  owner: string;
  isEnabled: boolean;
  hasSigningSecret: boolean;
  consecutiveFailures: number;
  lastDeliveredAtUtc?: string | null;
}

export interface DeliveryDto {
  id: string;
  notificationId: string;
  subscriptionCode: string;
  channel: DeliveryChannel;
  target: string;
  status: DeliveryStatus;
  attemptCount: number;
  queuedAtUtc: string;
  nextAttemptAtUtc?: string | null;
  deliveredAtUtc?: string | null;
  responseStatusCode?: number | null;
  lastError?: string | null;
}

export interface NotificationDto {
  id: string;
  idempotencyKey: string;
  eventType: string;
  severity: NotificationSeverity;
  subject: string;
  payload: string;
  schemeCode?: string | null;
  sourceCode?: string | null;
  publishedBy: string;
  occurredAtUtc: string;
  deliveries: DeliveryDto[];
}

export interface ScheduledReportDto {
  id: string;
  code: string;
  name: string;
  audience: string;
  schemeCodes?: string | null;
  sourceCodes?: string | null;
  frequency: ScheduleFrequency;
  hourUtc: number;
  dayOfWeek: number;
  dayOfMonth: number;
  recipients: string;
  owner: string;
  isEnabled: boolean;
  runCount: number;
  lastRunAtUtc?: string | null;
  nextRunAtUtc?: string | null;
}

export interface DispatchSummaryDto {
  attempted: number;
  delivered: number;
  retrying: number;
  deadLettered: number;
}

export const getSubscriptions = () => apiGet<SubscriptionDto[]>('/api/v1/notifications/subscriptions');

export const createSubscription = (request: {
  code: string;
  name: string;
  eventPattern: string;
  channel: DeliveryChannel;
  target: string;
  schemeCodes?: string | null;
  sourceCodes?: string | null;
  minimumSeverity: NotificationSeverity;
  signingSecret?: string | null;
}) => apiPost<SubscriptionDto>('/api/v1/notifications/subscriptions', request);

export const setSubscriptionEnabled = (code: string, enabled: boolean) =>
  apiPost<SubscriptionDto>(`/api/v1/notifications/subscriptions/${code}/enabled`, { enabled });

export const rotateSecret = (code: string, secret: string) =>
  apiPost<SubscriptionDto>(`/api/v1/notifications/subscriptions/${code}/secret`, { secret });

export const getNotifications = (page: number, eventType?: string) =>
  apiGet<PagedResult<NotificationDto>>(
    `/api/v1/notifications/events?page=${page}${eventType ? `&eventType=${encodeURIComponent(eventType)}` : ''}`,
  );

export const getDeliveries = (page: number, status?: DeliveryStatus | '') =>
  apiGet<PagedResult<DeliveryDto>>(
    `/api/v1/notifications/deliveries?page=${page}${status ? `&status=${status}` : ''}`,
  );

export const replayDelivery = (id: string) =>
  apiPost<DeliveryDto>(`/api/v1/notifications/deliveries/${id}/replay`);

export const dispatchDeliveries = () =>
  apiPost<DispatchSummaryDto>('/api/v1/notifications/deliveries/dispatch');

export const getScheduledReports = () =>
  apiGet<ScheduledReportDto[]>('/api/v1/notifications/scheduled-reports');

export const createScheduledReport = (request: {
  code: string;
  name: string;
  audience: string;
  schemeCodes?: string | null;
  sourceCodes?: string | null;
  frequency: ScheduleFrequency;
  hourUtc: number;
  dayOfWeek: number;
  dayOfMonth: number;
  recipients: string;
}) => apiPost<ScheduledReportDto>('/api/v1/notifications/scheduled-reports', request);

export const setScheduledReportEnabled = (code: string, enabled: boolean) =>
  apiPost<ScheduledReportDto>(`/api/v1/notifications/scheduled-reports/${code}/enabled`, { enabled });

export const runScheduledReport = (code: string) =>
  apiPost<ScheduledReportDto>(`/api/v1/notifications/scheduled-reports/${code}/run`);
