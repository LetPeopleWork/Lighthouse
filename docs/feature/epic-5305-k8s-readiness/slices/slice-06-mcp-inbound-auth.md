# Slice 06: MCP HTTP server inbound authentication (OAuth pass-through)

**Feature**: epic-5305-k8s-readiness
**Story**: US-06 (ADO #5307) → job-mcp-caller-own-identity
**Estimate**: ~2–3 crafter days (primarily in the **lighthouse-clients** repo)
**Reference class**: version-gated client endpoint wrapping (see `work-item-age-percentiles` clients wrapper + `FEATURE_REQUIRES_SERVER_NEWER_THAN`); reuses Lighthouse's existing owner-resolved/scoped API-key model

## Goal
Stop the published `mcp-http` container being a confused deputy. Each caller authenticates with their OWN credential (preferred: MCP spec rev 2025-06-18 OAuth pass-through; interim: `X-Api-Key` pass-through) that the MCP server forwards — so every caller drives Lighthouse as themselves, with their own RBAC scope and audit, no shared baked key.

## IN scope
- **lighthouse-clients repo (primary)**: the MCP HTTP server forwards the caller's credential instead of injecting one baked `LIGHTHOUSE_API_KEY`.
  - Preferred: adopt the MCP Authorization framework (OAuth) — caller brings an OAuth token.
  - Interim fallback: `X-Api-Key` pass-through reusing Lighthouse's owner-resolved (`ApiKey.OwnerSubject` → `sub`) + permission-scoped (`ApiKeyPermission`) keys.
- **Version gate**: the wrapping client method pre-checks the Lighthouse server version (an old server returns an opaque 404) and fails with a clear "upgrade Lighthouse" error. Pin to **strictly newer than the last released Lighthouse version**; record the baseline in the clients' `FEATURE_REQUIRES_SERVER_NEWER_THAN` registry.
- **Lighthouse backend (likely minimal/none)**: confirm the existing `ApiKeyAuthenticationHandler` owner-resolution + scope already satisfies pass-through; add only what's missing (e.g. an OAuth-token acceptance path if OAuth is chosen).
- **Standalone gate**: the existing single-key / dev path stays available; no break for self-hosters.

## OUT scope
- Edge auth (oauth2-proxy) and ClusterIP-vs-edge exposure decisions → Productization #5306 (chart/SaaS boundary, planning Q5).
- The MCP container's k8s deployment manifest → #5306.

## Learning hypothesis
**Confirms if it succeeds**: two different callers, each with their own credential, drive the MCP server and each sees only their own RBAC-scoped data, with per-caller audit — no shared-key ambient authority.
**Disproves if it fails**: the MCP OAuth framework is too heavy / immature for our stack right now, so we ship the interim `X-Api-Key` pass-through and defer OAuth (recording the decision), rather than blocking the slice.

## Acceptance criteria
See US-06 in `../feature-delta.md`. Key: an integration/e2e test in lighthouse-clients shows a caller-supplied credential is forwarded and resolved to that caller's owner+scope (not a baked key); the version gate rejects an old server with a clear upgrade message; the legacy single-key dev path still works.

## Dependencies
Independent of the other slices (lives mostly in a different repo). The decision OAuth-vs-X-Api-Key is the open question — resolve in DESIGN.

## Production data requirement
**Required.** Exercise against a real Lighthouse backend with two distinct API-key owners and assert per-owner scoping; smoke the version gate against an older Lighthouse build.

## Dogfood moment
Operator exposes the dev MCP server and two team members call it with their own keys; each sees only their scoped teams/portfolios.

## Cross-cutting checklist (confirmed in feature-delta)
RBAC: **central** — this slice removes ambient authority and makes the MCP path honour per-caller `ApiKeyPermission` scope (flows through the existing handler, no new RBAC port). Clients: **primary surface** — change lands in lighthouse-clients; version-gate per CLAUDE.md. Website: N/A — security/packaging, not a marketed UI feature.

## Pre-slice spike candidates
- **SPIKE (required)**: assess MCP spec 2025-06-18 OAuth support in the client SDK we use vs. effort of `X-Api-Key` pass-through; pick the path. (~half day)
- Confirm `ApiKeyAuthenticationHandler` needs no change for X-Api-Key pass-through. (~1 hr)
- Confirm the last released Lighthouse version to set the `FEATURE_REQUIRES_SERVER_NEWER_THAN` baseline. (~15 min)
