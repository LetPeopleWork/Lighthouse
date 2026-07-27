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

# Dates in the API

This part of the contract is easy to get wrong, so it is stated explicitly.

**A bare `YYYY-MM-DD` names a calendar day in the instance time zone.** Every metrics and forecast endpoint that takes or returns a date without a time component — `startDate` and `endDate` on the metrics endpoints, `date` on the drill-through endpoints, a delivery's target date, a forecast's projected date — means *that day as the Lighthouse instance reckons days*. It does not mean "midnight UTC", and it does not mean "midnight wherever the client happens to be".

Which zone that is, is an instance setting: see [Instance Time Zone](../Installation/configuration.html#instance-time-zone). Unless the operator configures one, it is the host zone of the instance — UTC for the official Docker image.

Two consequences for client authors:

- **Build a `YYYY-MM-DD` from local year/month/day parts, not from `toISOString()`.** `new Date(...).toISOString().split("T")[0]` converts to UTC first, so for any client east or west of the instance zone it silently sends the neighbouring day for part of every day. Ask the user for a day, send that day.
- **Read a `YYYY-MM-DD` back as a plain day, not as an instant.** Parsing it into a timestamp and re-rendering it in the viewer's zone can shift it by one day in either direction.

Fields that carry a **time component** are a different thing entirely and follow the opposite rule: they are **instants in UTC** (ISO-8601 with a `Z`) — creation and update stamps, sync bookkeeping, token expiry, blocked-transition timestamps. An instant has no time zone of its own and can safely be rendered in the viewer's local zone. A calendar day is *defined* by a time zone and must not be.
