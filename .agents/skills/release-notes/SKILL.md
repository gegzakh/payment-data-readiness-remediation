---
name: release-notes
description: How to record a user-visible change in the Release Notes service so it appears on the public release-notes page. Use whenever a feature, change, fix, security update or deprecation ships.
---

# Recording a release note

Releases are data, not markdown. A release is a draft until it is published; only published releases
appear on the public page, newest release date first, entries grouped by component.

## Through the API (gateway on :5100)

```bash
# 1. create a draft (needs releasenotes.write)
curl -X POST /api/v1/admin/releases -H 'Content-Type: application/json' -d '{
  "version": "1.3.0", "title": "Structured address validation", "releaseDate": "2026-04-01",
  "summary": "…",
  "entries": [{ "type": "Feature", "component": "Validation", "title": "…", "references": ["FRD-4.2"] }]
}'

# 2. add more entries (type: Feature | Change | Fix | Security | Deprecation | Erratum)
curl -X POST /api/v1/admin/releases/{id}/entries -d '{ "type": "Fix", "component": "Ingestion", "title": "…" }'

# 3. publish (needs releasenotes.publish; a release with no entries cannot be published)
curl -X POST /api/v1/admin/releases/{id}/publish
```

After publishing, corrections go in as errata (`POST /api/v1/admin/releases/{id}/errata`) — published
entries are not rewritten.

Or use admin-ui → Releases.

## Paging

Page size is runtime configuration, not code: `ReleaseNotes:Paging:DefaultPageSize`,
`ReleaseNotes:Paging:AllowedPageSizes`, `ReleaseNotes:Paging:MaxPageSize`, editable via
`/api/v1/settings/{key}` or admin-ui → Settings.
