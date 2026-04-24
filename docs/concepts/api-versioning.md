---
title: API Versioning
layout: home
nav_order: 2
parent: Concepts
---

This page defines the REST API versioning contract used by external Lighthouse clients.

- TOC
{:toc}

# Stable External Contract
External clients must use `/api/v1`.

Examples:
- `/api/v1/version/updateSupported`
- `/api/v1/teams`
- `/api/v1/portfolios`

`/api/v1` is the compatibility boundary for reusable client packages, CLI workflows, and hosted MCP integrations.

# Transition Contract
`/api/latest` exists for first-party and transition scenarios.

Examples:
- `/api/latest/version/updateSupported`
- `/api/latest/teams`

`/api/latest` can change as Lighthouse evolves and is not the stable contract for external integrations.

# Legacy Unversioned Routes
Unversioned `/api/*` routes are not part of the supported API contract.

All API consumers should call `/api/v1/*` or `/api/latest/*`.
