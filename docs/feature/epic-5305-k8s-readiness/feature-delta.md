# Feature Delta: epic-5305-k8s-readiness

<!-- markdownlint-disable MD024 MD041 -->

Wave: DISCUSS | Date: 2026-06-16 | Density: lean (per ~/.nwave/global-config.json) | Epic: ADO #5305

**Feature goal**: make the Lighthouse application itself safe to run on Kubernetes — multiple
replicas, rolling updates, behind an Ingress/reverse-proxy — through seven production C#/TS
changes, WITHOUT changing the sacrosanct single-container standalone product. This epic runs
BEFORE the k8s Productization epic (#5306): the app must be cluster-safe before it is packaged
and hosted. It was split out of the learning epic #5189 on 2026-06-15.

This DISCUSS covers all 7 child stories: #5311 forwarded headers, #5310 health checks, #5309
graceful shutdown, #5308 expand-only migrations + safe startup, #5312 observability, #5307 MCP
inbound auth, #5304 horizontal scalability. The north-star they slice toward (D1–D5, Q1–Q5, §4
architecture) lives in `docs/feature/l8e-kubernetes-learning/planning-stage.md` and is inherited,
not re-litigated.

**Prior-wave consultation** (READING ENFORCEMENT):
- ✓ `docs/product/jobs.yaml` · ✓ `docs/product/personas/{config-admin,lighthouse-maintainer}.yaml`
- ✓ `docs/product/journeys/multiple-cycle-times.yaml` (schema) · ✓ `docs/product/kpi-contracts.yaml`
- ✓ `docs/feature/l8e-kubernetes-learning/planning-stage.md` (north-star backbone)
- ✓ ADO #5305 (epic) + #5304, #5307–#5312 (child descriptions)
- ⊘ `docs/feature/epic-5305-k8s-readiness/{discover,diverge}/` (none — planning-stage is the upstream evidence)

No DISCUSS decision below contradicts the north-star; each inherits it.

---

## Wave: DISCUSS / [REF] Persona

**`platform-operator`** (NEW — `docs/product/personas/platform-operator.yaml`) — the person who *runs*
a Lighthouse instance, not the one who reads metrics inside it. Two flavours of one persona: the
**self-hoster** running a single container today (the sacrosanct standalone product) and the **LPW
SaaS operator** running many replicas across tenants tomorrow. Cares about the pod/process lifecycle,
rollouts and proxying — the operational envelope around the app. Distinct from `config-admin` (edits
in-app config) and the end-user product personas (flow-coach, forecaster). The MCP-caller story (#5307)
has a secondary actor — the **MCP/CLI caller** authenticating as themselves — but the persona who
deploys and secures the MCP server is `platform-operator`.

---

## Wave: DISCUSS / [REF] JTBD one-liners

Six jobs (added to `docs/product/jobs.yaml`), all `persona: platform-operator`. Opportunity-scored:

| Job ID | One-liner | Imp | Sat | Gap | Stories |
|---|---|---|---|---|---|
| `job-operator-survive-multiple-replicas` | Run >1 replica without N× syncs, lost notifications, or migration races | 5 | 1 | **4** | #5304, #5308 (lock) |
| `job-operator-zero-downtime-rollout` | Upgrade with no dropped requests and no data loss | 4 | 1 | **3** | #5308, #5309 |
| `job-operator-correct-behind-proxy` | Correct HTTPS / cookies / OIDC / SignalR behind a reverse proxy | 4 | 1 | **3** | #5311 |
| `job-mcp-caller-own-identity` | Each MCP caller drives Lighthouse as themselves, not a shared baked key | 4 | 1 | **3** | #5307 |
| `job-operator-trust-pod-health` | k8s routes to only-ready pods, restarts only dead ones | 4 | 2 | 2 | #5310 |
| `job-operator-observe-in-cluster` | Per-instance metrics / structured logs / traces in my stack | 3 | 2 | 1 | #5312 |

**Highest opportunity** = `survive-multiple-replicas` (gap 4) and the three gap-3 jobs. This drives
prioritization (below): the high-uncertainty multi-replica job ships last but SPIKEs early; the gap-3
"login behind proxy" job ships first because it is small and unblocks all cluster auth testing.

---

## Wave: DISCUSS / [REF] Scope assessment (Elephant-Carpaccio early gate)

Oversized signals present: **>3 bounded contexts/technologies** (EF/Postgres, SignalR/Redis, ASP.NET
health + lifecycle, OAuth/MCP, OpenTelemetry, reverse-proxy middleware) and **>2 weeks effort**. → This
is correctly an **EPIC**, already split from #5189 and decomposed on ADO into 7 independently-shippable
stories. Each story is one thin vertical slice that ships end-to-end and auto-degrades to standalone.
**Verdict: PASS — already split; user confirmed full-epic DISCUSS of all 7.** No further split needed;
the only slice needing internal care is #5304 (gated behind a required SPIKE, see slice-07).

---

## Wave: DISCUSS / [REF] Locked decisions

Inherited from the north-star (planning-stage §3) and applied as this epic's hard gates:

- **[D1 · EPIC GATE — standalone is sacrosanct]** Every story MUST preserve the single-container
  standalone + regular server deployment unchanged, auto-degrading to the single-instance path: no Redis
  ⇒ in-memory backplane; one replica works; SQLite stays default; frontend stays embedded. Verified per
  story as an acceptance criterion. (planning §0 epic gate + §D4)
- **[D2 · full nWave for product code]** These are real C#/TS changes → full `DISCUSS→…→DELIVER` + the
  CLAUDE.md RBAC / Lighthouse-Clients / Website checklist, not the learning light-loop. (planning §D3)
- **[D3 · sequence]** Learning #5189 → **#5305 (this)** → Productization #5306. The app must be
  cluster-safe before it is packaged/hosted. Cluster-side stacks (Prometheus/Grafana/Loki, oauth2-proxy,
  Ingress manifests, Helm chart) are #5306, NOT here — this epic is in-app code only.
- **[D4 · expand-only migrations]** Additive-only per release; destructive cleanup is a *later* release
  (expand now, contract later) because rolling updates run new+old pods against one shared Postgres.
  (memory: expand-only/non-destructive; planning #5308)
- **[D5 · #5304 architecture is OPEN — do not pre-pick]** The cluster-aware unit is the *update queue
  itself*, not a timer leader (both the periodic loop AND inline manual-refresh paths must be covered).
  Leader election is necessary-not-sufficient. DESIGN/SPIKE decides between distributed-single-consumer
  queue vs. cluster-wide per-entity lock + shared status store. (ADO #5304 architectural note 2026-06-14)
- **[D6 · MCP auth = clients-repo work, version-gated]** #5307 lands primarily in `lighthouse-clients`;
  preferred path is MCP OAuth pass-through, interim is X-Api-Key pass-through reusing the existing
  owner-resolved/scoped keys. Version-gate the endpoint (strictly newer than last released Lighthouse;
  `FEATURE_REQUIRES_SERVER_NEWER_THAN`). (planning §6 Q5)

---

## Wave: DISCUSS / [REF] Cross-cutting impact checklist (mandatory per CLAUDE.md DISCUSS)

Recorded explicitly — "N/A, because…" where no change is needed; these extend DoR Item 7.

| Story | RBAC | Lighthouse-Clients (CLI + MCP) | Website |
|---|---|---|---|
| #5311 forwarded headers | N/A — derives scheme/host only; no authorization surface. (But it *fixes* OIDC behind a proxy, so auth *works* correctly.) | N/A — no API contract change. | N/A — operational, not marketed. |
| #5310 health checks | N/A — unauthenticated operational endpoints carrying no business data. | N/A. | N/A. |
| #5309 graceful shutdown | N/A — server lifecycle only. | N/A — callers just reconnect. | N/A. |
| #5308 migrations + startup | N/A — provider/startup mechanics; confirm provider selection touches no RBAC-gated admin surface (it does not). | Possibly a CLI **connection hint** for Postgres — confirm in DESIGN; otherwise N/A. | N/A. |
| #5312 observability | **Decide in DESIGN**: `/metrics` can leak request paths; default cluster-internal/unauthenticated, but exposure must be a conscious call (Sonar/security-hotspot). | N/A. | N/A. |
| #5307 MCP inbound auth | **Central** — removes ambient authority; the MCP path honours per-caller `ApiKeyPermission` scope via the existing `ApiKeyAuthenticationHandler` (no new RBAC port, flows through the established handler). | **Primary surface** — change lands in `lighthouse-clients`; **version-gate** per CLAUDE.md. | N/A — security/packaging, not a marketed UI feature. |
| #5304 horizontal scalability | N/A — no authorization surface. | Likely N/A — internal infra, no API contract change; confirm in DESIGN. | N/A. |

---

## Wave: DISCUSS / [REF] User stories

Seven stories, one per ADO child, one per slice. US-NN ↔ slice-NN (prioritized order). Every story is
operator-visible (none is `@infrastructure`-only → no slice-composition gate violation: every slice
ships one value story). Each inherits **D1 (standalone gate)** as an embedded AC.

### US-01 — Login works behind a TLS-terminating reverse proxy (ADO #5311)
As a **platform-operator**, I put Lighthouse behind Traefik/nginx/an Ingress and want OIDC login + secure
cookies + SignalR to use the real public HTTPS host, so users log in first try with no redirect loop.
`job_id: job-operator-correct-behind-proxy`

#### Elevator Pitch
Before: behind a TLS-terminating proxy, OIDC redirects to `http://`, the callback loops, and secure cookies are dropped — login is broken.
After: declare the proxy as trusted + enable forwarded headers → hit `https://<public-host>` → the OIDC redirect/callback are `https://<public-host>/...`, the secure cookie persists, login succeeds.
Decision enabled: the operator can safely front Lighthouse with any reverse proxy and trust that auth works.

#### Acceptance criteria
- AC1: With trust ON and `X-Forwarded-Proto: https` + `X-Forwarded-Host: <public>` from a **declared known proxy**, the generated OIDC redirect/callback URL is `https://<public>/...` (integration test).
- AC2: Forwarded headers from an **undeclared** source are ignored — no scheme/host spoof.
- AC3 (D1): With no proxy declared, direct/standalone access is byte-identical to today; forwarded-header trust is OFF by default.

### US-02 — Kubernetes trusts the pod's real health (ADO #5310)
As a **platform-operator**, I want readiness gated on real serving capacity and liveness shallow, so k8s
routes traffic only to serving pods and restarts only genuinely-dead ones.
`job_id: job-operator-trust-pod-health`

#### Elevator Pitch
Before: there are no real probes; k8s can route to a not-yet-ready pod (cold 500s) or restart-loop a healthy-but-slow pod.
After: configure probes → `GET /health/ready` is 503 until DB-reachable + migrations-applied, `GET /health/live` stays 200 through a slow dependency, `GET /health/startup` covers slow boot.
Decision enabled: the operator trusts rollout/health status and can set probe configs with confidence.

#### Acceptance criteria
- AC1: readiness returns unhealthy when the DB is unreachable, while liveness stays healthy (no restart storm).
- AC2: readiness returns healthy only when DB reachable AND migrations applied.
- AC3 (D1): endpoints return 200 / are harmless in single-container mode with no orchestrator.

### US-03 — Rolling updates drop no requests (ADO #5309)
As a **platform-operator**, I want a terminating pod to drain in-flight HTTP + SignalR + the update queue
on SIGTERM, so I can roll out updates during the day with zero dropped requests.
`job_id: job-operator-zero-downtime-rollout`

#### Elevator Pitch
Before: a rolling update kills pods mid-request — in-flight HTTP/SignalR/queued updates are lost.
After: `kubectl rollout restart` (or any SIGTERM) → the pod stops intake, drains in-flight work within `terminationGracePeriodSeconds`, then exits → a load test + live SignalR client sees zero failed requests and a clean reconnect.
Decision enabled: the operator ships updates without a maintenance window.

#### Acceptance criteria
- AC1: on SIGTERM/`StopAsync`, an in-flight HTTP request and a queued update complete (or the update is safely re-enqueued) before the host reports stopped.
- AC2: readiness flips to NotReady on `ApplicationStopping` so the LB stops routing before drain.
- AC3 (D1): a single-container Ctrl-C behaves exactly as today.

### US-04 — Concurrent replicas migrate safely and additively (ADO #5308)
As a **platform-operator**, I want each release's migrations additive-only and exactly one replica to
apply them on concurrent startup, so old+new pods coexist on one Postgres without breakage or races.
`job_id: job-operator-zero-downtime-rollout` (+ `job-operator-survive-multiple-replicas`)

#### Elevator Pitch
Before: every pod races `Database.Migrate()` on boot, and a destructive migration can break the old pods still serving during a rollover.
After: scale a fresh deploy to 3 replicas against one Postgres → the logs show migrations applied **once** (one applies, two wait); a destructive migration is **rejected by CI** before merge.
Decision enabled: the operator rolls out schema changes during the working day without a downtime window.

#### Acceptance criteria
- AC1: N hosts started against one DB apply migrations exactly once (concurrency test asserting single application).
- AC2: a CI check rejects a destructive migration (drop/rename column/table) in a release; expand→contract two-release pattern documented.
- AC3 (D1): single SQLite or Postgres instance auto-migrates on boot exactly as today (lock degrades to a no-op).

### US-05 — Lighthouse is observable in my cluster (ADO #5312)
As a **platform-operator**, I want a Prometheus `/metrics` endpoint, structured JSON logs and OTel traces,
so Lighthouse appears on my existing dashboards like any first-class service.
`job_id: job-operator-observe-in-cluster`

#### Elevator Pitch
Before: no `/metrics` and unstructured text logs — Lighthouse is a black box in the cluster.
After: scrape `GET /metrics` → request/error/latency render in Grafana; logs ship as queryable JSON to Loki; a slow request is traceable.
Decision enabled: the operator monitors and alerts on Lighthouse from the same stack as everything else.

#### Acceptance criteria
- AC1: `GET /metrics` returns Prometheus-format output including HTTP server metrics.
- AC2: logs are emitted as structured JSON to stdout with the expected fields.
- AC3 (D1): with telemetry disabled, no exporter runs and there is no behaviour or performance change for the single container (low-overhead/off-by-default).

### US-06 — Each MCP caller authenticates as themselves (ADO #5307)
As a **platform-operator** exposing the MCP HTTP server, I want each caller to authenticate with their own
credential (passed through), so every caller drives Lighthouse with their own RBAC scope and audit — no
shared baked key. `job_id: job-mcp-caller-own-identity`

#### Elevator Pitch
Before: the `mcp-http` container holds one baked `LIGHTHOUSE_API_KEY` — a confused deputy; every caller acts as that owner/scope with no per-user audit, and an unauth'd `/mcp` is an open hole.
After: a caller sends their OWN OAuth token (or `X-Api-Key`) to `/mcp` → the server passes it through → Lighthouse owner-resolves it (`ApiKey.OwnerSubject → sub`) and applies that caller's `ApiKeyPermission` scope.
Decision enabled: the operator exposes MCP beyond ClusterIP without distributing/rotating a shared secret, and security review gets a clean "no ambient authority" answer.

#### Acceptance criteria
- AC1: two callers with distinct credentials each see only their own RBAC-scoped data; the credential is forwarded, not a baked key.
- AC2: the wrapping client method version-gates — an old Lighthouse server fails with a clear "upgrade Lighthouse" error, not an opaque 404.
- AC3 (D1): the existing single-key / dev path stays available; no break for self-hosters.

### US-07 — Lighthouse runs safely with N replicas (ADO #5304)
As a **platform-operator**, I want syncs to run once across the fleet, every notification to reach all
pods' clients, and update status consistent across pods, so I scale Lighthouse like a normal web app.
`job_id: job-operator-survive-multiple-replicas`

#### Elevator Pitch
Before: Lighthouse is a stateful singleton — a second replica means N× external syncs racing Postgres, notifications that reach only one pod's clients, and a per-pod status cache that disagrees.
After: configure Redis + scale to 3 → a manual refresh served by pod B notifies a client on pod A; the external system is synced **once** per cycle; `GetUpdateStatus` agrees across pods.
Decision enabled: the operator sets a replica count for HA/scale and trusts Lighthouse stays correct through a node failure.

#### Acceptance criteria
- AC1: with Redis + N hosts, a single sync per entity occurs under concurrent timer + manual-refresh load (no N× duplication, no racing writes).
- AC2: a notification raised on any pod reaches clients connected to any other pod (Redis backplane).
- AC3: `GetUpdateStatus` returns a consistent answer across pods (shared/distributed status store).
- AC4 (D1): with no Redis / one host, behaviour AND code path are identical to today.

---

## Wave: DISCUSS / [REF] Story map

```
Backbone (operator activities):  CONFIGURE ──▶ DEPLOY ──▶ ROLL OUT ──▶ SCALE ──▶ OPERATE
                                     │           │           │            │          │
US-01 forwarded headers ────────────┘           │           │            │          │
US-02 health checks ────────────────────────────┘           │            │          │
US-03 graceful shutdown ────────────────────────────────────┤           │          │
US-04 expand-only migrations + startup lock ────────────────┘           │          │
US-06 MCP inbound auth (parallel, clients repo) ────────────────────────┤          │
US-07 horizontal scalability (SPIKE-gated, last) ───────────────────────┘          │
US-05 observability (lands any time after deploy) ─────────────────────────────────┘
```

**Walking skeleton**: none — brownfield hardening; US-01 (smallest, config-gated) is the thin first slice
that proves the standalone-gate + production-data discipline for the rest.

---

## Wave: DISCUSS / [REF] Prioritization

Order by (a) learning leverage / uncertainty, (b) dependency, (c) dogfood cadence:

1. **US-01 forwarded headers** — smallest; unblocks all cluster auth testing; near-zero risk. First.
2. **US-02 health checks** — prerequisite for any safe rollout; foundational for verifying US-03/US-04.
3. **US-03 graceful shutdown** — pairs with US-02 for zero-downtime; drains the *current* queue.
4. **US-04 expand-only migrations + startup lock** — precedes real multi-replica; feeds US-02's "migrations applied".
5. **US-05 observability** — independent; bring forward if operating blind during US-07 hurts.
6. **US-06 MCP inbound auth** — mostly clients repo, parallelizable; gated by an OAuth-vs-X-Api-Key SPIKE.
7. **US-07 horizontal scalability** — highest uncertainty, largest, depends on US-03/US-04; ship LAST but
   run its **required SPIKE early** (learning leverage: disprove "leader election is enough" cheaply).

---

## Wave: DISCUSS / [REF] WS strategy

**Strategy D — Configurable / env-switching** per Mandate 5. Every story is config-gated and auto-degrades
(no Redis ⇒ in-memory; no proxy declared ⇒ no forwarded-header trust; telemetry off by default; migration
lock no-op at 1 instance). This is the D1 standalone gate expressed as the WS mechanism: one codebase serves
both the single-container self-hoster and the multi-replica SaaS, selected by configuration. (Trigger:
WS=D fires the `alternatives-considered` expansion suggestion — see wave-end menu.)

---

## Wave: DISCUSS / [REF] Driving ports (inbound surfaces)

- **HTTP** — `/health/ready`, `/health/live`, `/health/startup` (US-02); `/metrics` (US-05); existing OIDC
  redirect/callback + SignalR `/hub` negotiation now proxy-aware (US-01); `/mcp` inbound auth (US-06).
- **Process signals** — SIGTERM / `IHostApplicationLifetime` (US-03).
- **Config** — env vars / appsettings: trusted-proxy set (US-01), Redis connection (US-07), telemetry
  exporter (US-05), shutdown timeout (US-03).
- **CLI/MCP client** — `lighthouse-clients` MCP server credential pass-through + version gate (US-06).
- **No new in-app UI surface.** (Operator surfaces are HTTP/CLI/kubectl, not the React app.)

---

## Wave: DISCUSS / [REF] Pre-requisites

- Learning epic #5189 stories 00–07 (k8s fundamentals + the story-07 scaling spike) inform US-07; story 08
  (#5198) is the only open learning story and is not a blocker.
- A real Postgres + Redis on k3s for US-04/US-07 production-data acceptance (InMemory cannot reproduce the
  races — recurring lesson).
- The `CreateMigration` PowerShell script for US-04 migration generation (per CLAUDE.md).
- `lighthouse-clients` repo access + the last-released Lighthouse version for the US-06 version-gate baseline.

---

## Wave: DISCUSS / [REF] Outcome KPIs

Lighthouse is self-hosted — no central telemetry (memory: self-hosted-telemetry-gap). All KPIs are
`per_instance` (operator-observable via logs/metrics) or `vendor_demo_only` (LPW stage/prod). Append to
`docs/product/kpi-contracts.yaml` in DEVOPS.

| KPI | Target | Measurement | Scope |
|---|---|---|---|
| Dropped requests during a rolling update (US-03) | 0 | load-gen error count across a rollout on stage | vendor_demo_only |
| Duplicate external syncs per cycle at N replicas (US-07) | 1 (exactly once) | connector request log / structured-log sync events | vendor_demo_only |
| Concurrent-startup migration applications (US-04) | 1 | migration-history + structured startup logs | per_instance |
| OIDC login success behind proxy (US-01) | 100% first-try | manual + stage smoke | per_instance |
| Pod restart-on-slow-dependency events (US-02) | 0 | liveness restart count vs. DB-latency events | vendor_demo_only |
| MCP calls using a shared baked key after US-06 (US-06) | 0 | per-caller audit / structured auth logs | per_instance |
| Lighthouse `/metrics` scrape success (US-05) | 100% | Prometheus `up` for the Lighthouse target | per_instance |

---

## Wave: DISCUSS / [REF] DoR validation (9 items, evidence)

1. **Business value clear** — ✓ each story maps to an opportunity-scored job (gap 1–4); value = operability of the hosted/self-hosted product.
2. **User/persona identified** — ✓ `platform-operator` (new persona file); secondary MCP-caller actor on US-06.
3. **Acceptance criteria testable** — ✓ each US has 3–4 ACs verifying the Elevator-Pitch "After" end-to-end, incl. the D1 standalone-gate AC.
4. **Dependencies known** — ✓ sequence + soft deps mapped (US-07 ⟵ US-03/US-04; US-02 feeds from US-04); pre-requisites listed.
5. **Story sized / sliced** — ✓ 7 thin slices, each its own brief at `slices/slice-0N-*.md`, ≤~6 crafter days except US-07 which is SPIKE-gated.
6. **No blocking unknowns** — ✓ the one real unknown (US-07 cluster-aware-queue design) is explicitly OPEN (D5) and quarantined behind a required SPIKE; not pre-picked.
7. **Technical notes / constraints + cross-cutting** — ✓ RBAC/Clients/Website checklist recorded per story (above); D1–D6 locked decisions.
8. **Outcome KPIs defined** — ✓ 7 KPIs with numeric targets + measurement + scope.
9. **Definition of Done agreed** — ✓ below.

**Requirements completeness**: 0.96 (>0.95). The one soft gap: US-07's solution shape is intentionally
deferred to SPIKE/DESIGN — that is recorded as a decision (D5), not a missing requirement.

---

## Wave: DISCUSS / [REF] Definition of Done (9-item)

1. All ACs green (incl. the D1 standalone-gate AC) for the story. 2. `dotnet build` zero warnings;
`pnpm build` + Biome clean (for any TS). 3. `dotnet test` / `pnpm test` green. 4. SonarCloud
`new_violations = 0`. 5. Mutation kill ≥ 80% on the story's real surface (per CLAUDE.md per-feature). 6.
Cross-cutting checklist answered for the story (RBAC/Clients/Website). 7. Production-data acceptance run
(real Postgres/Redis/proxy/OIDC as the slice requires) — not synthetic-only. 8. Docs/screenshots updated
if any user-visible surface changed (most stories: N/A operational — record it). 9. ADO story
Active→Resolved after CI green; push paused for review (ado-sync ritual).

---

## Wave: DISCUSS / [REF] Out-of-scope

- Cluster-side stacks: Prometheus/Grafana/Loki deployment, oauth2-proxy, Ingress/Traefik manifests, the
  Helm chart, ArgoCD/GitOps, wildcard DNS, secrets operators → **Productization epic #5306**.
- HPA / `sessionAffinity` / load-test manifests → the **learning** story 07 (#5197), throwaway scratch.
- Per-tenant isolation / namespace-per-tenant model → #5306.
- Destructive (contract) migrations for any expand done here → a **later** release (D4).
- Edge-vs-ClusterIP MCP exposure + oauth2-proxy decisions → #5306 (planning Q5).
- Any change to the standalone single-container product behaviour (forbidden by D1).

---

## Wave: DISCUSS / [REF] Wave decisions summary

- **Feature type**: cross-cutting (backend C#, clients TS, operational surface) — NOT infrastructure-only
  (US-01/US-06 are operator/user-visible), so JTBD traceability applies and the escape valve was rejected.
- **Persona**: new `platform-operator` (user-chosen over extending `lighthouse-maintainer`).
- **Scope**: full epic — all 7 stories DISCUSSed in one pass (user-chosen).
- **ADO**: #5304 re-parented under Epic #5305 (was orphaned); all 7 children now under #5305.
- **Walking skeleton**: none (brownfield); US-01 is the thin proving slice.
- **Primary needs**: run Lighthouse multi-replica + behind a proxy + rolling-update-safe + observable, all
  WITHOUT touching the sacrosanct standalone (D1).
- **Constraints established**: D1–D6 (above). D5 keeps US-07's architecture OPEN behind a SPIKE.
- **Upstream changes**: none — DISCUSS inherits the planning-stage north-star; no DISCOVER assumption changed.

**Handoff** → DESIGN (`nw-solution-architect`, full artifacts; #5304's cluster-aware-queue SPIKE is the
first DESIGN concern) + DEVOPS (`nw-platform-architect`, `outcome-kpis` only). DESIGN + DEVOPS parallel.
```

---

## Wave: DISCUSS / [WHY] Alternatives considered

Rendered on request (triggers: cross-context complexity, WS=D). Decision rationale for the choices that
are deferred to SPIKE/DESIGN or locked above — what was weighed and why. These are inputs for DESIGN, not
re-openings of D1–D6.

### A1 · US-07 — what becomes the cluster-aware unit (OPEN, the SPIKE question)
The breakage is that `UpdateQueueService` is `AddSingleton` but singleton-*per-process*: each replica has
its own Channel queue, consumer, awaiters, and the `updateStatuses` dedup dict — and updates fire from two
paths (the timer loop AND inline manual refresh on whatever replica serves the request).

- **(rejected as sufficient) Leader election for the timer only.** Elect one replica to run
  Team/Portfolio/ForecastUpdater. *Why not:* does nothing for a manual refresh handled by a follower, and
  the per-process dedup is invisible across replicas — the same entity can still be updated concurrently and
  race the same Postgres rows. Necessary-not-sufficient; the research doc §1 is explicit. Keep leader
  election only as a *component* of a fuller design, not the design.
- **(candidate, preferred-leaning) Distributed queue with a single consumer.** Replace the in-process
  Channel with a shared queue (Redis stream / Postgres-backed) drained by exactly one consumer across the
  fleet; manual refresh enqueues to the shared queue and awaits completion via a shared status store.
  *Pro:* makes the *queue itself* cluster-aware (covers both trigger paths), dedup + awaited-completion +
  `GetUpdateStatus` all consistent. *Con:* most moving parts; introduces a queue technology.
- **(candidate) Cluster-wide per-entity lock + shared status store.** Keep per-process queues but guard each
  Team/Portfolio update with a distributed per-entity lock (e.g. Postgres advisory lock / Redis lock); back
  `GetUpdateStatus` with a shared store so dedup and reads agree. *Pro:* smaller change, no new queue. *Con:*
  lock-contention + liveness edge cases; awaited-completion across replicas still needs the shared store.
- **Decision:** OPEN (D5). The SPIKE (slice-07) prototypes both candidates against real Postgres+Redis with
  3 hosts driving timer + manual-refresh concurrently; the one that disproves double-work *and* keeps
  awaited-completion consistent wins. Do NOT pre-pick in DISCUSS.

### A2 · US-07 — SignalR fan-out backplane
- **Redis backplane (chosen, config-gated).** Matches the north-star (§4 "API N replicas + Redis"), local
  MinIO/Redis already in the rehearsal stack, no managed-service lock-in. No Redis ⇒ in-memory (D1).
- **(rejected) Azure SignalR Service.** Offloads fan-out fully but is a managed Azure dependency — couples
  the self-hostable product to a cloud service, violating the vendor-neutral, runs-anywhere posture.
- **(rejected) Sticky sessions only (`sessionAffinity: ClientIP`).** Pins a client to one pod so in-memory
  fan-out "works" — but it was the *learning* spike (story 07), doesn't deliver cross-pod notifications for
  server-raised events, and breaks on rebalancing. Not a product answer.

### A3 · US-04 — concurrent-startup migration coordination
- **In-process lock (advisory lock / history sentinel), chosen for this epic.** One replica applies, others
  wait; degrades to a no-op at one instance (D1). Keeps "migrate on boot" — the self-hoster's current model.
- **(deferred, not rejected) Dedicated pre-deploy migration Job / ArgoCD sync-wave.** Cleaner separation
  (migrate→deploy) but it is a *cluster/GitOps* mechanism → belongs to Productization #5306, and it would
  break the single-container "auto-migrate on boot" the self-hoster relies on. The slice-04 hypothesis
  explicitly allows falling back to this *if* the in-process lock proves fragile, recording the decision.
- **(rejected) Do nothing / let pods race.** `Database.Migrate()` under concurrent start is undefined.

### A4 · US-06 — MCP inbound auth model
- **MCP OAuth pass-through (preferred).** Each caller brings their own OAuth token; no shared secret to bake,
  seal, distribute, rotate; per-user RBAC + audit for free; an unauth'd `/mcp` is no longer an open hole.
  *Risk:* MCP-spec (2025-06-18) OAuth maturity in our client SDK — the slice-06 SPIKE assesses this.
- **X-Api-Key pass-through (interim, accepted fallback).** Caller sends its own Lighthouse API key; the MCP
  server forwards it; reuses the existing owner-resolved (`ApiKey.OwnerSubject`) + scoped (`ApiKeyPermission`)
  model with near-zero backend change. *Cost:* N user-held keys instead of one Secret. Ships if OAuth proves
  too heavy now — recorded, not blocking.
- **(rejected) Keep the single baked key + restrict to ClusterIP.** That is the confused-deputy status quo;
  the moment MCP is exposed beyond ClusterIP it is an ambient-authority hole. Exposure topology is a #5306
  concern, but the auth model must change regardless.

### A5 · US-05 — metrics library
- **OpenTelemetry .NET + Prometheus exporter (leaning).** One instrumentation surface for metrics+traces,
  vendor-neutral OTLP, future-proof. *Con:* heavier setup; overhead must be measured (slice-05 SPIKE) to set
  the off-by-default posture for the single container.
- **(alternative) `prometheus-net` for metrics only.** Lighter for just `/metrics`, but a second mechanism
  for traces — DESIGN picks one to avoid two telemetry stacks. Decision deferred to DESIGN/SPIKE.

### A6 · Frontend topology (epic-wide, Q4 — already locked upstream, restated)
- **Embedded (chosen for this epic and Bands A–C).** API serves the SPA; mirrors the standalone exactly
  (D1). The `frontend.mode: split` nginx path is a Productization #5306 / Band-D optimization, built then,
  defaulted off. Out of scope here — restated so DESIGN does not reopen it.
```

---

## Wave: DESIGN / [APP] Application layer — epic-5305-k8s-readiness

Wave: DESIGN | Layer scope: **application / component + ports/adapters + ADR authoring** | Date: 2026-06-18
Architect: Morgan (Solution Architect), interaction mode = **PROPOSE** — THIRD and final architect (Titan / system → Hera / DDD → **here**).
Inputs (inherited, NOT re-litigated): `brief.md` → "## System Architecture — epic-5305-k8s-readiness" (Decisions 1–7, Reuse Analysis, the solution-architect handoff bullet) + the DDD subsection (INV-1..4, the aggregate verdict, the no-outbox/idempotency confirmation); `c4-diagrams.md` → "C4 Architecture Diagrams — epic-5305-k8s-readiness"; D1–D6; A1–A6. Code grounded at HEAD: `UpdateQueueService.cs`, `UpdateNotificationHub.cs`, `ApiKeyAuthenticationHandler.cs`, `DatabaseConfigurator.cs`, `Program.cs`.

I own the **application detail behind the fixed system constraints**: health/lifecycle/middleware wiring, the `IUpdateStatusStore` + ADR-076-substrate ports/adapters, the forwarded-headers ordering + OIDC redirect-uri construction, the MCP inbound-auth split. I do **not** pick ADR-076 Option A vs B (SPIKE-gated, D5), redefine any invariant, or touch production code.

### [APP] Reuse-Analysis confirmation

I inherit and honour the system layer's Reuse verdicts verbatim — no new CREATE beyond what it already justified. EXTEND dominates: every cluster-aware change lands behind a seam that exists (`IUpdateQueueService`, the shared `updateStatuses` singleton, `UseForwardedHeaders`/`ConfigureForwardedHeaders`, `ApiKeyAuthenticationHandler`, the Serilog pipeline, the `app.Lifetime` hooks, the `DatabaseMaintenanceGate` mutual-exclusion pattern). The two genuine CREATEs (`/health/{live,ready,startup}` via the ASP.NET `HealthChecks` framework; OTel `/metrics` off-by-default) are standard-library-backed, each mapped to a US/AC with no seam to extend. The one *new abstraction* I introduce — `IUpdateStatusStore` — wraps the existing `ConcurrentDictionary<UpdateKey,UpdateStatus>` field; minimal new surface, in line with the system layer's "EXTEND → extract port" verdict.

### [APP] US-02 + US-03 — health / lifecycle / shutdown wiring (Decisions 4, 5)

**Health endpoints (US-02, Decision 5 — CREATE via `Microsoft.Extensions.Diagnostics.HealthChecks`).** Register `AddHealthChecks()` with named, tagged checks; map three endpoints with distinct predicates so each gets its own depth:

- `AddHealthChecks().AddDbContextCheck<LighthouseAppContext>("db", tags: ["ready"])` (DbContext reachability) + a custom `MigrationsAppliedHealthCheck` (`tags: ["ready","startup"]`) that asserts `context.Database.GetPendingMigrations()` is empty.
- A drain-aware flag check (`tags: ["ready"]`) that reports Unhealthy once `ApplicationStopping` has fired (US-03 AC2 — see below). A small singleton `IReadinessState` holds the flag; the readiness check reads it.
- `MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false })` — **shallow**: no checks run, returns 200 while the process is up (never gated on a slow dependency → no restart storm, US-02 AC1).
- `MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") })` — **deep**: 503 until DB reachable AND migrations applied (US-02 AC2), and 503 once draining (US-03 AC2).
- `MapHealthChecks("/health/startup", new HealthCheckOptions { Predicate = c => c.Tags.Contains("startup") })` — covers slow boot (migrations applied / warm-up).
- All three are **unauthenticated** (`[AllowAnonymous]` / mapped outside the fallback auth policy) operational endpoints carrying no business data (RBAC N/A, confirmed). Mapped in `ConfigureApp` **after** `UseRouting` and alongside `MapControllers`/`MapHub`. The Earned-Trust startup probes for ADR-076/077 substrates surface their refusal as a `health.startup.refused` event that, while distinct from these endpoints, can drive `/health/startup` to stay Unhealthy when a substrate lie is detected.

**Graceful-shutdown drain (US-03, Decision 4 — EXTEND `app.Lifetime`).** Register an `ApplicationStopping` callback (alongside the existing `ApplicationStarted` registration at `Program.cs:116`) with this **ordering**:

1. **Flip readiness NotReady first** — set `IReadinessState.IsDraining = true` so `/health/ready` returns 503 and the LB/Ingress stops routing **before** drain begins (US-03 AC2). This must run first so no new request is routed mid-drain.
2. **Stop queue intake, drain in-flight** — the existing single-consumer loop (`UpdateQueueService.StartProcessingQueue`, `:181-197`) reads from an unbounded `Channel`. On stopping, complete the writer (`queue.Writer.Complete()`) so `ReadAllAsync` finishes the in-flight items then exits; the consumer's in-flight item completes (or `EnqueueAndAwaitAsync` work is allowed to finish / is safely re-enqueued for the next pod via the ADR-076 substrate at N>1). This rides the consumer's existing completion path — no new mechanism (US-03 AC1).
3. **Let in-flight HTTP complete** — the host's normal `ShutdownTimeout` window drains in-flight HTTP; SignalR clients disconnect and reconnect to another pod via the ADR-075 backplane.

Bound the whole drain with `HostOptions.ShutdownTimeout` set from `Shutdown:TimeoutSeconds` (`builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = ...)`), which in the cluster must be ≤ `terminationGracePeriodSeconds` (a #5306 deploy value). Ordering relative to middleware: ASP.NET stops accepting new connections on SIGTERM before `ApplicationStopping` fires for in-flight work; auth/SignalR middleware are unaffected (the drain is a lifecycle hook, not a middleware reorder).

**Standalone degradation (D1)**: Ctrl-C raises the same `ApplicationStopping` → same ordered drain → exits exactly as today; the `WaitForShutdownAsync` path (`Program.cs:136`) is unchanged, the drain is additive and harmless at N=1 (US-02 AC3, US-03 AC3).

### [APP] US-07 — ports / adapters (Decisions 1, 2; DDD INV-1..4)

**`IUpdateStatusStore` (extract from the shared `updateStatuses` singleton).** Today the `ConcurrentDictionary<UpdateKey,UpdateStatus>` is created at `Program.cs:932` and injected into BOTH `UpdateQueueService` (`:14`/`:22`) AND `UpdateNotificationHub` (`:10`/`:14`, read by `GetUpdateStatus` `:50-60`). Extract a port:

```
public interface IUpdateStatusStore
{
    bool TryAdmit(UpdateKey key, UpdateStatus initial);        // INV-4: at-most-one active lifecycle per key, atomic cluster-wide
    void Advance(UpdateKey key, UpdateProgress to);            // INV-1: compare-and-set on the UpdateProgress ordinal; write wins iff it advances
    bool TryGet(UpdateKey key, out UpdateStatus status);       // INV-2: bounded-stale read tolerated
    void Remove(UpdateKey key);                                // terminal cleanup (INV-3); a fresh lifecycle re-admits cleanly
}
```

- **In-process adapter (degrade path, D1)** — `InProcessUpdateStatusStore` wraps the existing `ConcurrentDictionary`: `TryAdmit` = `TryAdd` (exactly today's dedup, `:46`/`:72`); `Advance` = the in-place `updateStatus.Status =` mutation, which is monotone by construction because the single consumer owns the ordered sequence; `TryGet` = `TryGetValue`; `Remove` = `TryRemove`. **Byte-identical behaviour and code path at N=1** (DDD §standalone-degradation).
- **Shared adapter (multi-replica)** — a Redis-hash or Postgres-row adapter keyed by `UpdateKey`. It MUST implement `Advance` as a **monotonic-progress compare-and-set on the `UpdateProgress` ordinal** (write wins iff `to > current`), NOT blind LWW (INV-1, rejected by the DDD-architect). `TryAdmit` is the cluster-wide atomic admission (INV-4) — its atomicity is provided by the ADR-076 substrate's claim/lock, so the store and the queue substrate are co-designed: admission and status-store-write are the same transactional boundary. Reads are bounded-stale/eventually-consistent (INV-2) — no synchronous distributed read on the `GetUpdateStatus` hot path; the SignalR backplane push + next re-sync are the convergence mechanism (ADR-027 D2 parity).
- **Injection points (unchanged callers)**: `UpdateQueueService` and `UpdateNotificationHub.GetUpdateStatus` take `IUpdateStatusStore` instead of the raw dictionary; the singleton registration at `Program.cs:932-934` selects the in-process vs shared adapter on the same `ConnectionStrings:Redis` gate as ADR-075/076. No controller, no hub method signature, no `[Authorize]` changes.

**ADR-076 substrate seam (behind `IUpdateQueueService`).** I describe the seam, NOT the pick. Both options swap the impl behind the existing `IUpdateQueueService` port (`EnqueueUpdate` + `EnqueueAndAwaitAsync`, signatures unchanged); the config gate is `ConnectionStrings:Redis` (Option A's Redis Stream / Option B's Redis lock) and/or the Postgres connection (Option B's `pg_advisory_lock`). Absent the gate ⇒ the in-process `Channel` + `InProcessUpdateStatusStore` verbatim (US-07 AC4). The cross-pod `EnqueueAndAwaitAsync` completion signal rides the monotone terminal-status advance + the ADR-075 backplane push, NOT a durable queue (DDD Confirmation 3: no outbox; wire awaiter release off the store). **The Earned-Trust startup `probe()` for the chosen substrate belongs to the DELIVER slice (slice-07)** — referenced here, designed in ADR-076, not designed away: wire → probe → use, refuse on a substrate lie with `health.startup.refused`.

**SignalR Redis backplane (ADR-075).** `.AddStackExchangeRedis(conn)` appended to the existing `AddSignalR()` registration (`Program.cs:269`) only when `ConnectionStrings:Redis` is present; hub, groups, fan-out (`:199-204`), `[Authorize]` all unchanged. Sticky-session is a #5306 deploy concern, not in-app.

### [APP] US-01 — forwarded-headers ordering + OIDC redirect-uri (Decision 7)

US-01 is **mostly already implemented** (Reuse Analysis). `UseForwardedHeaders()` is at `Program.cs:170` and `ConfigureForwardedHeaders` (`:538-566`) already sets `XForwardedFor|Proto|Host` and populates `KnownProxies`/`KnownIPNetworks` from `authConfig.TrustedProxies`/`TrustedNetworks` (default empty ⇒ OFF). My application detail:

- **Middleware ordering (verify + lock)**: `app.UseForwardedHeaders()` (`:170`) runs **before** `UseRouting` (`:196`), `UseAuthentication` (`:207`), `UseAuthorization` (`:208`), and `MapHub` (`:212`). This is correct and load-bearing: forwarded headers must be applied to `HttpContext.Request.Scheme`/`Host` **before** auth so that the OIDC middleware, the cookie `SecurePolicy`, and the SignalR negotiation all observe the real public scheme/host. The fix is to confirm this ordering and guard it (a test), not to move anything.
- **OIDC redirect/callback URI construction (AC1)**: the OpenIdConnect handler builds the `redirect_uri` from the current request's scheme + host. Because `UseForwardedHeaders` runs first and rewrites `Request.Scheme`→`https` and `Request.Host`→`<public>` (only from a declared known proxy), the generated redirect/callback is `https://<public>/<CallbackPath>` (`CallbackPath` from `authConfig.CallbackPath`, `:667`). The cookie `SecurePolicy = Always` (`:628`) then persists because the request is seen as HTTPS. No code change to the OIDC options is required beyond confirming forwarded headers precede auth; the redirect-uri correctness is a *consequence* of the ordering.
- **Spoof rejection (AC2)**: forwarded headers from an **undeclared** source are ignored because `KnownProxies`/`KnownIPNetworks` are empty / do not contain the source — `UseForwardedHeaders` only honours declared proxies. This is the existing behaviour; the test proves it.
- **Tests that prove AC1/AC2/AC3** (integration, `WebApplicationFactory`): (AC1) with `TrustedProxies` set + `X-Forwarded-Proto: https` + `X-Forwarded-Host: <public>` from the declared proxy → assert the generated OIDC redirect/challenge `Location` is `https://<public>/...` and the session cookie is `Secure`. (AC2) the same headers from an undeclared source → assert scheme/host are NOT rewritten (no spoof). (AC3) no proxy declared → assert direct/standalone access is byte-identical (forwarded-header trust OFF by default). No new RBAC surface (derives scheme/host only).

### [APP] US-06 — MCP inbound-auth application detail (A4, D6)

Primarily a **`lighthouse-clients`** change, **version-gated** per CLAUDE.md: the wrapping client method pre-checks the server version and fails with a clear "upgrade Lighthouse" error (not an opaque 404), pinned **strictly newer than the last released Lighthouse version** in `FEATURE_REQUIRES_SERVER_NEWER_THAN` (US-06 AC2). The credential is **passed through** per caller, not a baked `LIGHTHOUSE_API_KEY`.

**Backend = REUSE `ApiKeyAuthenticationHandler` (no new RBAC port).** The handler (`ApiKeyAuthenticationHandler.cs`) already: reads `X-Api-Key` (`:14`/`:31`), validates via `apiKeyService.ValidateApiKeyWithOwnerAsync` (`:43`), **owner-resolves** via `ApiKey.OwnerSubject` → emits a `sub` claim (`:63-66`), and is wired into the `LighthouseSmartAuth` policy scheme that forwards `X-Api-Key` requests to the API-key scheme (`Program.cs:608-624`). A caller's forwarded `X-Api-Key` therefore drives Lighthouse with **that caller's** identity and `ApiKeyPermission` scope (US-06 AC1) — the confused-deputy ambient-authority hole is closed with zero new backend auth code; the MCP path honours per-caller scope through the established handler (cross-cutting RBAC checklist).

**The OAuth-pass-through-preferred / X-Api-Key-interim split (A4):**
- **Preferred — MCP OAuth pass-through**: each caller brings their own OAuth token; no shared secret to bake/seal/distribute/rotate; per-user RBAC + audit for free. *Risk*: MCP-spec (2025-06-18) OAuth maturity in the client SDK.
- **Interim/fallback (accepted) — X-Api-Key pass-through**: caller sends its own Lighthouse API key; the MCP server forwards it; reuses the owner-resolved/scoped model above with near-zero backend change.
- **The slice-06 SPIKE decides** which path ships first (assesses client-SDK OAuth maturity). If OAuth pass-through requires the backend to validate a third-party bearer token at `/mcp`, that is a small additional scheme in the `LighthouseSmartAuth` forwarding selector — but it is SPIKE-gated and not pre-designed here. **Standalone (D1)**: the existing single-key / dev path stays available; no break for self-hosters (US-06 AC3).

**External integration annotation (for platform-architect / DEVOPS)**: the OIDC provider (US-01) and the MCP OAuth path (US-06, if chosen) are external auth integrations — **contract tests recommended** (consumer-driven, e.g. PactNet) for the OIDC token/redirect contract and any MCP OAuth token-exchange, to detect breaking changes before production. These are the highest-risk boundary in the epic.

### [APP] Handoff to DISTILL (acceptance-designer)

Now fixed for acceptance test design (all behavioural, no implementation coupling):
1. **US-02**: three endpoints with distinct depth — `/health/live` 200 through a slow dependency; `/health/ready` 503 until DB-reachable AND migrations-applied (and during drain); `/health/startup` covers boot. Standalone: harmless at N=1.
2. **US-03**: on SIGTERM, readiness flips NotReady **before** drain; in-flight HTTP + queued update complete (or re-enqueue) within `Shutdown:TimeoutSeconds`; Ctrl-C standalone behaves as today.
3. **US-07**: `IUpdateStatusStore` enforces monotonic-progress (no reader ever observes a regressed `UpdateProgress` — the INV-1 race test the DDD-architect specified); single sync per entity at N=3; cross-pod notification delivery; standalone byte-identical.
4. **US-01**: declared-proxy forwarded headers → `https://<public>` OIDC redirect + Secure cookie; undeclared source ignored; no-proxy standalone byte-identical.
5. **US-06**: two callers with distinct credentials each see only their own RBAC-scoped data (credential forwarded, not baked); client version-gate gives a clear upgrade error; single-key dev path preserved.

Substrate Earned-Trust probes (ADR-076/077) and the ADR-076 Option A/B pick are **DELIVER/SPIKE concerns** — the acceptance tests assert the *behaviour* (single sync, monotonic status, exactly-once migration) substrate-agnostically, so they hold whichever option the SPIKE picks. ADRs: ADR-075 (ACCEPTED), ADR-076 (PROPOSED/OPEN — SPIKE-gated), ADR-077 (ACCEPTED), ADR-078 (PROPOSED — overhead SPIKE-measured), all under `docs/product/architecture/`.
