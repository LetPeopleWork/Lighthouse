# Slice-06 cross-repo — MCP inbound auth in `lighthouse-clients` (#36–#37)

**Story**: US-06 (ADO #5307) · scenarios #36–#37 (`@cross-repo`) · **Status**: interim X-Api-Key /
Bearer pass-through (#37 interim) **IMPLEMENTED** in `/storage/repos/lighthouse-clients` (unpushed,
paused for review); OAuth-bearer-accept backend capability + version gate (#36 / #37 preferred)
deferred — see below.

## Implemented (lighthouse-clients, unpushed)

`packages/mcp-http/src/bin.ts` — `resolveRequestAuth(headers, fallbackApiKey)` derives auth **per
inbound request**: caller `X-Api-Key` → `{api-key}`; else `Authorization: Bearer` → `{bearer-token}`;
else the container's baked key as legacy fallback; else `none`. Wired into the per-request
`createClient` so each `/mcp` call drives Lighthouse as the *caller*, not the baked key.
Tests: `bin.passthrough.test.ts` (7 policy unit tests) + `bin.passthrough.e2e.test.ts` (3 — a
header-capturing fake upstream + a real MCP tools/call proves caller-one/caller-two/bearer are
forwarded distinctly and a no-credential caller falls back to the baked key). Full suite 242 green,
`tsc -b` clean. Changeset: `mcp-http-caller-credential-passthrough.md` (minor). Biome could not run
in-sandbox (native binary OOM) — runs in CI.

## What changes where

| Surface | Auth model | Change |
|---|---|---|
| **CLI** | caller's own Lighthouse **API key** (`X-Api-Key`) | **No change** — keeps the API-key option (user direction 2026-06-20). |
| **stdio MCP server** | caller's own **API key** | **No change** — single local user; API-key stays the supported option (user direction). |
| **HTTP MCP server** (`mcp-http` container) | today: ONE baked `LIGHTHOUSE_API_KEY` (confused deputy) | **Stop baking one key.** Forward the *caller's own* credential so every caller drives Lighthouse as themselves. |

Only the **HTTP** MCP server is the confused-deputy hole. The CLI and stdio MCP are per-user
processes and legitimately carry the user's own API key — the API-key path is retained for them.

## Backend reality after this slice (grounds the client design)

- The Lighthouse backend already owner-resolves + per-key-scopes API keys
  (`ApiKeyAuthenticationHandler` → `sub` + `api_key_id` claims → `RbacAdministrationService`
  intersects owner permissions ∩ `ApiKeyPermission` rows). Slice-06 added the first HTTP-level
  proof of this (`McpInboundAuthIntegrationTest`) and **no production change**.
- The smart-auth scheme accepts **`X-Api-Key`** for non-browser callers; it does **not** accept a
  raw OAuth bearer token from an API caller (non-`X-Api-Key`, non-cookie requests fall through to
  the cookie/OIDC challenge). So **OAuth pass-through (#37) needs a future backend capability** (an
  API bearer-accept scheme) — it is *not* available today.

## #37 — credential pass-through

Two paths, **both shipped/forwarded by `resolveRequestAuth`** (caller X-Api-Key OR Authorization
Bearer → forwarded to Lighthouse):

1. **X-Api-Key pass-through (shipped, the baseline).** The HTTP MCP server forwards the caller's
   `X-Api-Key` to Lighthouse instead of injecting a baked key. No backend change, no version gate.
   Reuses the owner-resolved + scoped model proven by `McpInboundAuthIntegrationTest`. This is the
   model for CLI + stdio + standalone (no OAuth infra).
2. **Hosted MCP OAuth (future, separate stories — see ADR-079).** Decision 2026-06-20: for the
   hosted k8s scenario the user wants **no API key at all** — the MCP client does the OAuth browser
   flow against the **same OIDC provider as Lighthouse**, stores the token, and sends it as
   `Authorization: Bearer` to mcp, which forwards it (already wired) to Lighthouse. **oauth2-proxy is
   NOT needed** (the MCP client drives OAuth itself; a proxy would be a redundant edge gate). This
   requires a **new backend capability — Lighthouse validating the IdP JWT bearer on its API**
   (`AddJwtBearer`, same authority/JWKS, audience = Lighthouse API, claims → existing
   `CurrentUserProfileService` + RBAC). That is **not** "an OAuth server in Lighthouse" — it is the
   API learning to accept the IdP's access token directly, the same way the browser path already
   trusts that IdP (today via code-flow → its own cookie). Tracked as ADR-079 + a backend story
   (JWT bearer) + a clients story (mcp advertises MCP OAuth protected-resource metadata).

## #36 — version gate

Only relevant once the hosted-OAuth path (#37.2) lands and depends on the backend JWT-bearer
capability: pin the wrapping client method in `FEATURE_REQUIRES_SERVER_NEWER_THAN` to **strictly
newer than the last released Lighthouse version** (baseline **v26.6.16.14**; bump to the then-latest
release at wrap time) so an old server fails with a clear "upgrade Lighthouse" message, not an opaque
401/404. The X-Api-Key path (#37.1) needs no gate (it reuses the long-existing endpoint).

## Acceptance (in `lighthouse-clients`)

- An integration/e2e test: two callers with distinct credentials each see only their own RBAC-scoped
  data through the HTTP MCP server (mirrors backend #32/#33), the credential is forwarded (not a
  baked key), and the legacy single-key dev path still works for self-hosters (#35 / D1).
- The version-gate test (#36) applies once the OAuth path is wrapped.

## Dogfood

Operator exposes the dev HTTP MCP server; two team members call it with their own keys; each sees
only their scoped teams/portfolios. Smoke the version gate against an older Lighthouse build (OAuth
path only).
