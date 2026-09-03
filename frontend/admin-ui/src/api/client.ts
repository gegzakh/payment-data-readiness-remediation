import { keycloak } from '../auth/keycloak';

const baseUrl = import.meta.env.VITE_API_BASE_URL ?? '';

export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly detail: string,
  ) {
    super(detail);
    this.name = 'ApiError';
  }
}

async function request<T>(method: string, path: string, body?: unknown): Promise<T | undefined> {
  // Keycloak rotates access tokens well before expiry, so every call refreshes opportunistically.
  await keycloak.updateToken(30).catch(() => keycloak.login());

  const response = await fetch(`${baseUrl}${path}`, {
    method,
    headers: {
      Accept: 'application/json',
      Authorization: `Bearer ${keycloak.token}`,
      ...(body === undefined ? {} : { 'Content-Type': 'application/json' }),
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  });

  if (!response.ok) {
    throw new ApiError(response.status, await response.text());
  }

  if (response.status === 204) {
    return undefined;
  }

  return (await response.json()) as T;
}

export const apiGet = <T>(path: string) => request<T>('GET', path) as Promise<T>;
export const apiPost = <T>(path: string, body?: unknown) => request<T>('POST', path, body);
export const apiPut = <T>(path: string, body?: unknown) => request<T>('PUT', path, body);
export const apiDelete = (path: string) => request<void>('DELETE', path);
