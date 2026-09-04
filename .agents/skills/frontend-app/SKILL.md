---
name: frontend-app
description: Conventions for the PDR React front ends (web-ui and admin-ui) — API access, auth, and the quality gate. Use when changing anything under frontend/.
---

# PDR front ends

Two independently deployable Vite + React + TypeScript apps:

- `frontend/web-ui` (port 5173) — public, anonymous, read-only (release notes today).
- `frontend/admin-ui` (port 5174) — Keycloak `login-required` with PKCE, authoring and settings.

## Rules

- Both apps only call the gateway. In dev, Vite proxies `/api` to `http://localhost:5100`; in production
  set `VITE_API_BASE_URL`. Never call a service port directly.
- Server state goes through TanStack Query (`useQuery`/`useMutation` + `invalidateQueries`); component
  state is `useState`. No global store.
- admin-ui attaches the Keycloak token in `src/api/client.ts` (`updateToken(30)` before every call) and
  hides actions with `hasPermission('<permission>')` — the API still enforces it.
- Keep API types in `src/api/*.ts` mirroring the service DTOs; page size options come from
  `/api/v1/releases/page-sizes` rather than being hard-coded.
- Quality gate before committing, in the app folder:
  `npm run lint && npm run typecheck && npm test && npm run build`.
