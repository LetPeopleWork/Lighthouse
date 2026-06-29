# Epic 5306 — Helm chart manual end-to-end test notes

Date: 2026-06-28 · Tester: Claude + Benjamin · Chart 0.1.1
Test image: `dev-latest` (latest-and-greatest; production docs keep pinned released tags).

Lab: local `kind` cluster `l8e-test` + ingress-nginx. Hostnames via `/etc/hosts`:
`lighthouse.local`, `keycloak.local`.

Legend: ✅ works · ⚠️ works-with-caveat · ❌ broken · 🔧 fix-now · 🕒 fix-later

---

## Scenario 1 — simple: bundled Postgres + embedded backend, no auth

Values: `postgresql.enabled=true`, `frontend.mode=embedded`, `oidc.enabled=false`,
`mcp.enabled=false`, `replicaCount=1`, `image.tag=dev-latest`.

Findings:
- ✅ Install succeeds. `l8e-lighthouse-api` + `l8e-lighthouse-postgres-0` both `Running 1/1`.
- ✅ `/health/ready` = 200, `/health/live` = 200, `GET /` serves SPA (`<title>Lighthouse</title>`).
- ⚠️ **API crash-loops 3× at boot** (`err exit=1`) before Postgres accepts connections, then stabilises.
  No init-container / DB-wait; relies on pod restart backoff. Cosmetic on kind (settles <1min) but
  produces scary `RESTARTS 3` + `CrashLoopBackOff` flashes on slow clusters. 🕒 fix-later candidate
  (init-container `pg_isready` wait, or app-level connection retry on startup).
- ℹ️ Tested with image `26.6.28.1` (prior session), not `dev-latest`. Both are "latest-ish"; 26.6.28.1
  is a concrete dated build. No behavioural concern.

---

## Scenario 2a — OIDC via in-cluster Keycloak

Lab plumbing (prior session, verified this run):
- Keycloak deployed in ns `keycloak`, ingress `keycloak.local`, `KC_HOSTNAME=http://keycloak.local`,
  `--import-realm`. Realm `lighthouse`, confidential client `lighthouse`/`lighthouse-secret`,
  redirect `http://lighthouse.local/*`, test user `testuser`/`testpass`.
- **In-cluster issuer agreement solved via CoreDNS `hosts` block**: `keycloak.local`+`lighthouse.local`
  → ingress-nginx ClusterIP `10.96.160.158`. So the API pod resolves the SAME issuer string
  (`http://keycloak.local/realms/lighthouse`) as the browser. In-cluster discovery verified = 200.
- Host `/etc/hosts` still needs `127.0.0.1 keycloak.local lighthouse.local` for the **browser** login
  (kind maps ingress to localhost:80). NOT yet added — needed before manual browser test.

Findings:
- ❌🔧 **FIX-NOW (chart bug): enabling OIDC crashes the API.** With `oidc.enabled=true` the pod
  `FATAL: Authentication is enabled but Authentication:AllowedOrigins is empty ... Refusing to start
  with a wildcard CORS policy` (Program.cs `EnsureCorsFailsClosed`, epic-5305 security hardening).
  The chart's configmap never set `Authentication__AllowedOrigins`. **Fixed in chart**: configmap now
  derives `Authentication__AllowedOrigins__0` from the ingress access URL (scheme+host) when
  `oidc.enabled`, plus new optional override `oidc.allowedOrigins` (list). Backend accepts indexed /
  comma / semicolon forms (S1_AllowedOriginsEnvVarBindingTests). Needs a helm-unittest + docs note.
- ❌🔧 **FIX-NOW (chart bug): OIDC over HTTP issuer crashes every request.** After the AllowedOrigins
  fix the pod boots but every auth request throws `The MetadataAddress or Authority must use HTTPS
  unless disabled ... RequireHttpsMetadata=false`, and this also fails the readiness probe. Backend
  exposes `Authentication:RequireHttpsMetadata` (default true). **Fixed in chart**: new value
  `oidc.requireHttpsMetadata` (default **true** — production HTTPS issuers unaffected) → configmap
  `Authentication__RequireHttpsMetadata`. Local HTTP Keycloak sets it false. Needs helm-unittest + docs.
- ✅ With both fixes + `oidc.requireHttpsMetadata=false`, API boots `1/1`, and the OIDC chain works:
  `GET /api/latest/auth/login` → `302` to
  `http://keycloak.local/realms/lighthouse/protocol/openid-connect/auth?client_id=lighthouse&request_uri=urn:ietf:...`
  The `request_uri` (PAR) proves the API's **back-channel to the issuer succeeded over HTTP** and the
  confidential client authenticated. CoreDNS issuer-agreement design validated end to end.
- ⚠️📄 **DOC GAP (decision): OIDC login requires a valid Premium license.** `GET /api/latest/auth/mode`
  returns `{"mode":"Blocked"}` — `AuthModeResolver` line 65-68: auth is enabled + configured correctly
  but `licenseService.CanUsePremiumFeatures()` is false → **Blocked** (users can't actually log in).
  Not a chart bug; it's a product gate. The chart's OIDC section MUST state that login needs Premium,
  else self-hosters hit a silent wall. Verified by importing the dev license → mode flips to `Enabled`.
- 🕒 **FIX-LATER (chart polish): config-only changes don't roll the Deployment.** Editing values that
  only change the ConfigMap (e.g. AllowedOrigins) leaves running pods on stale env — no
  `checksum/config` pod annotation. Had to `rollout restart` manually. Add a
  `checksum/config: {{ sha256sum ... }}` template annotation so `helm upgrade` rolls pods on config
  change. Low risk, standard helm pattern.
- ⚠️📄 **DOC GAP (ordering trap): import the Premium license BEFORE enabling OIDC.** `LicenseController`
  is `[Authorize]` + `RbacGuard(SystemAdminOrBootstrap)`. With OIDC enabled and premium not-yet-valid
  (Blocked), the `[Authorize]` gate returns **401 before** the bootstrap branch runs — so you cannot
  import the license that would unblock auth (chicken-and-egg). Correct flow: install (no auth) →
  import license while `mode=Disabled` (UI or API, bootstrap-open) → THEN `oidc.enabled=true`.
  Verified: imported `valid_premium_license.json` with auth off (`canUsePremiumFeatures:true`,
  Benjamin/LetPeopleWork, exp 2029), re-enabled OIDC → `mode=Enabled`, login `302`→Keycloak PAR.
- ✅ **Scenario 2a chart-level COMPLETE.** `mode=Enabled`; `/api/latest/auth/login` → 302 to
  `keycloak.local/.../auth?client_id=lighthouse&request_uri=urn:ietf:...` (PAR back-channel OK).
  REMAINING (manual, needs host `/etc/hosts` + browser): full browser round-trip login as
  `testuser`/`testpass` landing back authenticated. Chart plumbing & IdP integration validated.

## Scenario 2b — OIDC via external Entra

Setup: same chart + same `oidc.*` keys as 2a, different values. TLS required (Entra rejects non-HTTPS
redirect URIs for real hostnames) → `ingress.tls=true` + self-signed `lighthouse-tls` secret;
`oidc.requireHttpsMetadata=true` (default, production-correct). Redirect URI registered in Azure:
`https://lighthouse.local/api/auth/callback` (Web platform).

Findings:
- ✅ **Back-channel WORKS.** API rolls out clean (no crash — HTTPS issuer satisfies RequireHttpsMetadata).
  `mode=Enabled` (premium license persisted from 2a). `GET /api/latest/auth/login` → `302` to
  `https://login.microsoftonline.com/<tenant>/oauth2/v2.0/authorize?client_id=<id>&redirect_uri=
  https%3A%2F%2Flighthouse.local%2Fapi%2Fauth%2Fcallback&response_type=code&scope=openid profile email&
  code_challenge=...&code_challenge_method=S256&response_mode=form_post`. Entra HTTPS discovery loaded
  (egress OK), PKCE on, redirect_uri matches registration.
- ✅ **"Provide values once" confirmed:** one `oidc.*` block drives BOTH Keycloak (HTTP, requireHttps=false)
  and Entra (HTTPS, requireHttps=true). No provider-specific chart code. Same applies to MCP (scenario 4).
- REMAINING (manual, user): browser round-trip — add hosts entry, accept self-signed cert, complete the
  Entra login form_post callback, land authenticated.

---

## Scenario 3 — scaling: Redis backplane, replicaCount>1

Decision recall: `frontend.mode=split` is NOT implemented (ADR-081, fails loud). SPA is embedded in
the API workload, so scaling `replicaCount` scales the SPA-serving workload too — there is no separate
"frontend" to scale. So "multiple frontends" = N/A by design.

Setup: builds on 2b (Entra OIDC + TLS stay on). Deployed minimal Redis (`redis-master.redis.svc:6379`,
no chart bundle — vendor-neutral per ADR). `replicaCount=2` + `redis.connectionString`.

Findings:
- ✅ **Scaling works.** Zero-downtime rolling update; 2 API pods `1/1 Running`, both serve, auth still
  `Enabled`. `replicaCount>1` without Redis is correctly rejected at install (assertScaling guard).
- ✅ **Single-instance background work is correct.** The lock is a **per-entity Postgres advisory lock**
  ("single-active-lifecycle-per-UpdateKey", epic-5305 Option B) held only DURING an update — so 0 locks
  at idle is expected, not a leak. Redis = SignalR backplane + admission dedup. No double-sync risk.
- ✅ **"Multiple frontends" = N/A by design.** `frontend.mode=split` not implemented (ADR-081). SPA is
  embedded in the API; `replicaCount` scales the SPA-serving workload too. No separate frontend to scale.
- ❌🔧 **CROSS-EPIC FINDING (epic-5305 backend, NOT the chart): `cluster-substrate` readiness probe is
  destructive + ERROR-spams every 10s on every healthy scaled pod.** `ClusterSubstrateHealthCheck` is
  tagged `ReadyAndStartupTags` → runs on `/health/ready` (periodSeconds 10). Each run executes
  `pg_terminate_backend` (reclaim-on-death test) + opens ~4 PG conns + Redis HSETNX. Its `Lazy<>` cache
  is per-instance but health checks get a fresh instance per probe, so it re-runs forever. Returns
  Healthy (scaling fine), but floods `57P01: terminating connection due to administrator command` +
  `Failed executing DbCommand SELECT 1` at ERROR every 10s × every pod. For a *productization* epic this
  reads as a broken DB to operators (false alarms, log-volume). The probe validates **deployment-constant
  invariants** (pooler mode, HSETNX atomicity, reclaim) — intent is startup-only (msg = `health.startup.
  refused`). **Recommended backend fix:** make cluster-substrate **StartupTags-only** (drop from Ready),
  or cache the verdict process-wide (true singleton). DECISION: fix in epic-5305 now or log as follow-up.

---

## Scenario 4 — MCP with OIDC (oauth) auth

`mcp.auth.mode=oauth` reuses `oidc.issuer` (provided once). Template forwards `LIGHTHOUSE_OAUTH_ISSUER`
only (caller-Bearer pass-through, ADR-079). No-auth / apikey is NOT acceptable per requirement.
Builds on 3 (scaled + Entra auth + TLS). MCP image: `mcp-http:1.2.1` (no `dev-latest` tag — that's
backend-only; mcp-http publishes versioned + `latest`).

Findings:
- ❌🔧 **FIX-NOW (chart bug): MCP server binds 127.0.0.1 → unreachable via Service/Ingress (502).**
  mcp-http logs `running at http://127.0.0.1:3000`; kube-proxy targets the pod IP, so the Service
  refused connections and the Ingress `/mcp` returned 502. The server honours a `HOST` env. **Fixed in
  chart**: mcp.yaml now sets `HOST=0.0.0.0`. Verified chart-driven → `POST /mcp initialize` = 200.
- ✅ **"Provide values once" holds for MCP.** `LIGHTHOUSE_OAUTH_ISSUER` = the same `oidc.issuer` (Entra
  here; Keycloak by just swapping oidc.issuer). One auth config drives API + MCP, Keycloak + Entra.
- ✅ **OAuth enforced (no-auth genuinely not an option).** API-backed tool `lighthouse_team_list` with
  NO Bearer → `teams: unauthorized (401)`. The MCP server forwards the (absent) caller credential to the
  API, which rejects it. Static `tools/list` returns 200 unauth (expected — no backend call).
- ⚠️🔎 **INVESTIGATE (cross-repo, lighthouse-clients — NOT chart): no OAuth protected-resource metadata.**
  `/.well-known/oauth-protected-resource` (+ `/oauth-authorization-server`) → `{"error":"Not found"}` in
  mcp-http 1.2.1. ADR-079 expected the oauth mode to *advertise* protected-resource metadata (RFC 9728)
  so MCP clients can auto-discover the IdP. Today a client gets a bare 401 with no discovery hint —
  enforcement works, discovery doesn't. May need a different mcp-http env or a newer image. Decide:
  follow-up in lighthouse-clients.
- ℹ️ Chart default `mcp.image: :latest` is mutable; values-enterprise already says "pin a real tag".

---

## Fix-now vs fix-later ledger

| # | Finding | Decision | Status |
|---|---------|----------|--------|
| 1 | OIDC enable crashes API — `Authentication:AllowedOrigins` empty (fail-closed CORS) | FIX-NOW | ✅ Fixed in chart (configmap derives origin from accessURL + `oidc.allowedOrigins` override). Needs helm-unittest + docs. |
| 2 | OIDC over HTTP issuer crashes every request — `RequireHttpsMetadata` | FIX-NOW | ✅ Fixed in chart (`oidc.requireHttpsMetadata`, default true). Needs helm-unittest + docs. |
| 3 | OIDC login requires Premium license (mode=Blocked otherwise) | DOC | ⏳ Add to kubernetes.md OIDC section. |
| 4 | License must be imported BEFORE enabling OIDC (`[Authorize]` 401 chicken-and-egg) | DOC | ⏳ Add ordering note + demo-walkthrough step order. |
| 5 | Scenario-1 API crash-loops 3× at boot before Postgres ready (no DB-wait) | FIX-LATER | ⏳ init-container `pg_isready` or app startup retry. |
| 6 | Config-only value changes don't roll the Deployment (no `checksum/config`) | FIX-LATER | ⏳ Add checksum annotation. |
| 7 | `cluster-substrate` readiness probe destructive + ERROR-spams every 10s on healthy scaled pods (epic-5305 BACKEND, not chart) | FIX-NOW | ✅ Fixed (Program.cs `StartupOnlyTags` — substrate gates startup only, off readiness) + regression test `ClusterSubstrateProbeRegistrationTest`. Build + 111 health/security tests green. |
| 8 | MCP server binds 127.0.0.1 → Service/Ingress 502 | FIX-NOW | ✅ Fixed in chart (`HOST=0.0.0.0` in mcp.yaml). helm-unittest + docs added. |
| 9 | MCP oauth doesn't advertise protected-resource metadata (RFC 9728) | FIX-NOW (chart) + RELEASE (clients) | ✅ Root cause: (a) chart never set `LIGHTHOUSE_OAUTH_RESOURCE` / `Authentication:Audience`; (b) published mcp-http 1.2.1 predates the feature. Feature IS implemented in lighthouse-clients HEAD (commit c2efcf1, ADR-079, pending changeset release). **Chart fix**: new `oidc.audience` → `Authentication__Audience` + MCP `LIGHTHOUSE_OAUTH_RESOURCE`, required when `mcp.auth.mode=oauth` (mcp-http needs BOTH issuer+resource or it refuses to start). Pin `mcp.image` to the release that includes c2efcf1 once published. |
| 10 | OIDC login 502s out-of-the-box behind ingress-nginx — large callback Set-Cookie overflows the default 4k proxy buffer | FIX-NOW | ✅ Found during live browser login (Keycloak): `upstream sent too big header ... POST /api/auth/callback`. Added `ingress.annotations` passthrough; set `nginx.ingress.kubernetes.io/proxy-buffer-size: 16k`. Verified live — login round-trip completes. helm-unittest + docs added. |

| 11 | OIDC login breaks at replicaCount>1 — Data Protection keys not shared across pods (redirect loop) | FIX-NOW (backend) | ⏳ Program.cs `PersistKeysToFileSystem` → per-pod local dir. At scale, the OIDC cookie set by pod A is undecryptable on pod B → "too many redirects". epic-5305 shipped the Redis backplane + advisory lock but missed DP key sharing. Fix: `PersistKeysToStackExchangeRedis` + `SetApplicationName` when Redis is configured (same trigger as the backplane). Also covers the per-pod OAuth state secret. Live-repro on kind at replicaCount=2 + Entra. |

| 12 | MCP OAuth discovery broken through the public ingress (mcp-http 1.3.0) | FIX-NOW (clients + chart) — ✅ VERIFIED | ✅ Fixed + verified end-to-end 2026-06-29 by driving `mcp-remote` against the live chart (scenario 4, Entra). **Two-step fix.** (1) `96da0a7`/1.3.1 (Option a) corrected scheme via `X-Forwarded-Proto` but mislocated metadata under `/mcp/.well-known/...`. Live `mcp-remote` revealed spec-compliant clients ignore the WWW-Authenticate hint and build the **RFC 9728 root-anchored** path `/.well-known/oauth-protected-resource/mcp`, which the chart's `/`-catch-all routed to the API → redirect loop. (2) **`87a73e9`/1.3.2** serves metadata at the root-anchored path (bare + `/mcp` suffix) and advertises it; **chart ingress** now routes `/.well-known/oauth-protected-resource` (Prefix) → MCP service (+ helm-unittest). `mcp-remote` confirmed: fetches metadata through the ingress (HTTP 200, https) and resolves the IdP. Token round-trip is then an Entra-only limit (metadata under tenant path + no DCR), out of #5362 scope — Keycloak-class IdPs complete it. ADO #5362. Original root cause: `bin.ts:227` hardcoded `http://${req.headers.host}/.well-known/...`. |

### Browser round-trip (manual, on kind-l8e-test via /etc/hosts → node bridge IP 172.23.0.2)
- **Scenario 1** ✅ (port-forward, no auth) — landing page, no login.
- **Scenario 2a (Keycloak)** ✅ **full browser login confirmed** — lighthouse.local → Keycloak login →
  testuser/testpass → callback (after the buffer fix) → authenticated. Exercised AllowedOrigins +
  requireHttpsMetadata=false + licence-before-OIDC ordering + the new proxy-buffer annotation.
- Lab note: this box's docker loopback DNAT (127.0.0.1:80) is broken; browser reaches the ingress via
  the kind node bridge IP (172.23.0.2), not 127.0.0.1. Not a chart issue.
