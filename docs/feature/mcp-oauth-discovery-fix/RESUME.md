# RESUME — MCP OAuth discovery fix (ADO #5362)

> **This is the NEXT thing to do (step 1 of 2).** Flow: **bugfix** (`/nw-bugfix`), NOT full nWave waves.
> RCA is already done. Sibling of #5330. Child of Epic 5306.

## Status
- **RCA: DONE.** Root cause pinpointed during the epic-5306 manual k8s dogfood (finding #12).
- **Code: not started.** Spans two repos: `lighthouse-clients` (mcp-http) + the Lighthouse Helm chart.

## Root cause
`lighthouse-clients/packages/mcp-http/src/bin.ts:227`:
```js
const metadataUrl = `http://${req.headers.host}${PROTECTED_RESOURCE_METADATA_PATH}`;
```
Two defects make OAuth auto-discovery unreachable behind the chart's TLS ingress on the `/mcp` path:
1. **Hardcoded `http://`** — ignores `X-Forwarded-Proto`, so behind the TLS ingress it advertises an
   `http://` metadata URL.
2. **Root-path assumption** — `PROTECTED_RESOURCE_METADATA_PATH` is `/.well-known/oauth-protected-resource`
   at the host root. The chart routes only `/mcp` to the MCP server, so the root path hits the API
   (`/` catch-all → 302) and `/mcp/.well-known/...` is 404.

Net: an external MCP client (e.g. Claude Desktop) gets a `401` whose `resource_metadata` URL is wrong
scheme + unreachable path → cannot run the OAuth browser flow. Direct-to-service behaviour is correct.

## Design decision to make FIRST (~1 page, not a DESIGN wave)
- **Scheme:** honor `X-Forwarded-Proto` (trust-proxy) → emit `https` behind the TLS ingress. [clear]
- **Path/resource — pick one:**
  - (a) mcp-http serves the metadata under its `/mcp` mount (base-path aware) + `LIGHTHOUSE_OAUTH_RESOURCE`
    becomes `https://<host>/mcp`; chart ingress already routes `/mcp`. **(leading candidate)**
  - (b) chart ingress adds a route `/.well-known/oauth-protected-resource` → MCP service (resource stays
    host-root).
  - (c) dedicated MCP host (`mcp.<host>`) so the server is at the host root.

## Acceptance (regression test target)
- Behind a TLS ingress, the `WWW-Authenticate` `resource_metadata` URL uses **https** and is **reachable**
  through the ingress (served by mcp-http, not the API).
- An external MCP client: `401` → fetch metadata → IdP OAuth flow → `/mcp` with bearer → API accepts.
- Direct-to-service behaviour preserved. Verify with Claude Desktop against `https://lighthouse.local/mcp`.

## Validation lab (recreate as in the chart dogfood)
kind + ingress-nginx, `/etc/hosts → node bridge IP` (loopback DNAT broken on this box), Keycloak realm +
Entra app, premium licence imported before OIDC. See
`docs/feature/epic-5306-k8s-productization/deliver/manual-test-notes.md` for the full recipe + findings.

## After this: step 2 → combined DISCUSS/DESIGN
See `docs/feature/epic-5306-productization-platform/RESUME.md`.

---
_Scaffolded 2026-06-28 end of day. This is the first thing to pick up tomorrow._
