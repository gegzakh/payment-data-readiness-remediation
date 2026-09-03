import Keycloak from 'keycloak-js';

export const keycloak = new Keycloak({
  url: import.meta.env.VITE_KEYCLOAK_URL ?? 'http://localhost:8080',
  realm: import.meta.env.VITE_KEYCLOAK_REALM ?? 'pdr',
  clientId: import.meta.env.VITE_KEYCLOAK_CLIENT_ID ?? 'pdr-web',
});

/** Roles the API maps onto permissions; the UI hides actions the user cannot perform. */
export const hasPermission = (permission: string): boolean =>
  keycloak.hasResourceRole(permission, 'pdr-api') || keycloak.hasRealmRole(permission);

export const initKeycloak = () =>
  keycloak.init({ onLoad: 'login-required', pkceMethod: 'S256', checkLoginIframe: false });
