# l8e Kubernetes Learning — Planning Stage (North Star)

> **What this is.** The one-time backbone (north star) for the Kubernetes initiative. It was authored
> for what was originally a single 19-story Epic #5189; that epic was **split into three sequenced
> epics on 2026-06-15 (see §0)**. This document remains the shared end-state all three converge on —
> it is designed once here and each story across the three epics is a slice toward it.
>
> **Posture.** This is a *living north star, not a frozen spec.* You can't fully design ArgoCD /
> ESO / observability before you've learned them — the architecture below is a hypothesis you
> revise as you go. nWave/Claude is **instructor, researcher, reviewer** here — not implementer.
> Per-story execution is the light loop: `objective → nw-research (cited reading) → you build it →
> nw-spike (throwaway experiments) → Claude reviews (Socratic)`. No DoR gate, no Gherkin, except
> the three full-nWave stories called out in D3.

---

## 0. Epic structure (split 2026-06-15)

The initiative started as one Epic #5189 with 19 stories. Once the learning bands were nearly done it
was reorganized into **three sequenced epics** — learning the platform, then making the *app* safe to
run on it, then packaging and hosting it:

| Epic | State | Holds | Loop |
|------|-------|-------|------|
| **#5189 — l8e Kubernetes Learning** | Active | Stories **00–08** (#5190–#5198). 00–07 Closed; **08 (#5198) is the only open story.** | light loop (learning) |
| **#5305 — Lighthouse k8s-readiness — production code changes** | New | The real Lighthouse C#/TS changes that must land before hosting: #5304 SignalR Redis backplane + single-instance background work · #5307 MCP HTTP inbound OAuth pass-through (Q5) · #5308 expand-only migrations + safe startup under N replicas · #5309 graceful shutdown (SIGTERM) + draining · #5310 health checks (live/ready/startup) · #5311 reverse-proxy forwarded-headers · #5312 /metrics + structured logging. | full nWave (product code) |
| **#5306 — l8e Kubernetes Productization (GitOps · dogfood · stage/prod)** | Planned | Stories **09–18** (#5199–#5208): the public Helm chart, enterprise docs, the private `lighthouse-gitops` repo (ArgoCD), wildcard DNS + per-tenant TLS, secrets, dogfooding LPW as tenant-zero across stage + prod, upgrades, observability, provisioning, backup/DR. | light loop + chart/ops |

**Sequence: #5189 → #5305 → #5306.** The app must be cluster-safe (#5305) before it is packaged and
hosted (#5306). #5306 consumes the public Helm chart and depends on #5305.

> **Scope note (2026-06-21).** Two #5306 stories were elevated from light loop to a **scoped full
> DISCUSS** because they are public, customer-facing, repo-bound deliverables: **#5199 (Helm chart,
> publishable)** and **#5200 (enterprise docs)**. The distribution question on card #5199 is **resolved
> to a PUBLIC OSS chart** (GitHub Pages Helm repo, LPW org). The other nine #5306 children
> (#5201–#5208, #5320) **stay light loop** per D3/§7. DISCUSS artifacts:
> `docs/feature/epic-5306-k8s-productization/`. This refines, not contradicts, the D3 rule — the
> public *self-hostable product* surface earns DISCUSS rigor; internal GitOps/ops stays light loop.

**#5305 epic gate (every child story):** the change MUST keep the existing single-container standalone
and regular server deployment working unchanged — it auto-degrades to the single-instance path (no
Redis → in-memory backplane; single replica works; SQLite stays the default; frontend stays embedded).
No breaking change for self-hosters; verified per story. This is the D4 "standalone is sacrosanct" rule
applied as a hard acceptance gate.

This §0 supersedes the earlier "all 19 stories under one epic" framing; the bands (§1) and the
architecture/decisions (§3–§6) are unchanged by the split — band boundaries map onto the epics: **A–B →
#5189**, the product-code work pulled out of B/C → **#5305**, **C–E → #5306**.

---

## 1. The learning arc — five bands

The 19 stories cluster into five competency bands. The "definition of learned" is stated at the
**band** level (the per-story exit criteria already live on the ADO cards).

| Band | Stories | Theme | Definition of learned (the gate) |
|------|---------|-------|----------------------------------|
| **A — k8s fundamentals on a real app** | 00–04 | cluster → run → persist → expose → self-heal | *I can take any containerized app and run it on k8s reliably: deploy it, give it durable storage, expose it on a real hostname with TLS, and make it self-heal and roll out safely — without looking up commands.* |
| **B — Lighthouse-specific hardening** | 05–07 | edge auth, second service, scaling | *I can run Lighthouse as a multi-service, authenticated app that scales horizontally, and I understand exactly what breaks when a stateful (SignalR) app meets multiple replicas.* |
| **C — Packaging & productization** | 08–10 | multi-env, Helm, docs | *Anyone can self-host the whole stack with one `helm install` against a published chart, configured by values, documented well enough to pitch.* |
| **D — SaaS platform foundation (GitOps)** | 11–13 | ArgoCD, wildcard DNS, secrets | *Tenants are provisioned from a Git repo with zero manual `kubectl`; each gets a working TLS subdomain automatically; no plaintext secret exists anywhere in Git.* |
| **E — Operate the SaaS** | 14–18 | dogfood, upgrades, observability, provisioning, DR | *I run a real multi-tenant SaaS: dogfooded by LPW, upgraded by merging a PR, observable per-tenant, onboarded in <5 min, and recoverable within a known RTO/RPO.* |

The arc is deliberately end-to-end early (Band A already runs the *real* app, not toy nginx beyond
00) and gets more "platform" as it goes. Bands A→B→C are the **self-hostable product**; Bands D→E
are the **hosted SaaS** built on top of that product.

---

## 2. Sequencing & dependencies

Stories are numbered to be done in order, but the *real* constraint graph is looser than a straight
line. Hard dependencies (must precede) vs. the rest:

```
00 ─▶ 01 ─▶ 02 ─▶ 03 ─▶ 04 ─▶ 05 ─▶ 06 ─▶ 07 ─▶ 08 ─▶ 09 ═══▶ 10
                  │                                          │
                  └── persistence is assumed by everything   │
                      downstream (02 is foundational)        │
                                                              ▼
   09 (Helm) is a REFACTOR-AND-RECONVERGE point ──────────▶ 11 ═══▶ 12 ─▶ 13 ─▶ 14 ─▶ 15
   everything after it is expressed as chart values         (ArgoCD flips the deploy model)
                                                              ▼                         │
                                                             16 (observability) ◀───────┤ can land any time after 11
                                                             17 (provisioning) ◀─────────┤ needs 11+12+13
                                                             18 (backup/DR)   ◀──────────┘ needs 02; hardened after 14
```

Two **inflection points** worth naming, because they change *how* you work, not just *what* you build:

- **Story 09 (Helm)** — up to here you write raw manifests. From here, everything is chart
  templates + values. Treat 09 as the moment you go back and fold 01–08's manifests into the
  chart. After 09, "deploy" means "render the chart."
- **Story 11 (ArgoCD)** — up to here you `helm install` / `kubectl apply` by hand. From here, Git
  is the only way anything changes the cluster. This is the product→SaaS boundary and it
  establishes the private `lighthouse-gitops` repo (see D1).

Soft ordering (could move): **16 (observability)** is valuable as early as right after 11 — bring
it forward if operating blind during 14's dogfood hurts. **18 (backup/DR)** only hard-depends on 02
(Postgres exists); do a minimal `pg_dump` CronJob early and harden it after 14.

---

## 3. Cross-cutting decisions (the DISCUSS-light decisions)

These are the few choices every story inherits. Locked here so each story doesn't re-litigate them.

### D1 — Repo split: public product vs. private ops

| Repo | Visibility | Holds | Established by |
|------|-----------|-------|----------------|
| `lighthouse` (this repo) | public | product code (C#/TS) **+ the Helm chart** (`chart/`), published to a Helm repo via GitHub Pages | story 09 |
| `lighthouse-gitops` | **private** | ArgoCD `Application`/`ApplicationSet`, `tenants/<name>/values.yaml`, `tenants/_template/`, SealedSecrets / ExternalSecret manifests | story 11 |

**Boundary rule:** the *chart* (how to deploy) is public and reusable by any self-hoster; the
*tenant configuration and secrets* (who is deployed, with what) are private. A self-hoster consumes
the public chart and writes their own values; LPW's hosted SaaS drives the same chart from the
private repo.

### D2 — Local-test stack mirrors prod (free, on your machine)

The whole point: learn against a stack that maps 1:1 to the hosted setup, at zero cost.

| Concern | Local (free) | Hosted (prod) | Introduced in |
|---------|-------------|---------------|---------------|
| Cluster | k3s | k3s-on-VM **or** managed k8s *(open decision Q1)* | 00 |
| DNS | `/etc/hosts` entries | wildcard `*.lighthouse.letpeoplework.com` | 03 / 12 |
| TLS | mkcert (self-signed, trusted locally) | cert-manager DNS-01 wildcard | 03 / 12 |
| Identity | Keycloak in-cluster | Entra ID (or existing OIDC) *(open decision Q3)* | 05 / 14 |
| Secrets | Sealed Secrets + `kubeseal` | External Secrets Operator + Azure Key Vault | 13 |
| Object storage | MinIO | Hetzner/Azure Blob | 18 |

The local stack is not a throwaway — it's the rehearsal environment for every prod change.

### D3 — The full-nWave exception (product code = epic #5305)

> **Post-split (§0):** the product-code work below is now the dedicated epic **#5305** rather than a
> handful of flagged stories inside the learning epic. The learning stories that *touched* product
> code (02, 06) shipped as light-loop learning slices in #5189; their genuine productization (e.g.
> 06 → MCP OAuth #5307) is full-nWave work under #5305. The rule of thumb below is unchanged — it
> just maps onto epic boundaries now.

Most stories are infra/ops YAML with no testable app code and no external customer → **light loop**.
Three stories change real Lighthouse C#/TS and are testable + customer-facing → full
`DISCUSS→…→DELIVER` + the CLAUDE.md **RBAC / Lighthouse-Clients / Website** checklist:

- **02 — SQLite→Postgres.** *Lighter than its label.* `Lighthouse.Migrations.Postgres` already
  exists and `pg_dump` is already in the image — Postgres is a supported backend today. The slice
  is mostly *config* (connection string, provider selection, default-in-container) + verifying
  migrations run clean in-cluster. Still: run the checklist (does provider selection touch any
  RBAC-gated admin surface? do the CLI/MCP clients need a connection hint? website N/A).
- **06 — MCP HTTP server in-cluster.** New driving adapter (HTTP-exposed MCP) → directly implicates
  **Lighthouse-Clients (CLI + MCP)** in the checklist, and likely version-gating per CLAUDE.md.
- **07 — SignalR scaling.** *Re-scoped to a light loop (Option 3 decision, 2026-06-14).* The learning
  story does the **k8s-layer spike only** — scale to N, reproduce the breakage, `sessionAffinity:
  ClientIP`, HPA on CPU, load-test — all throwaway scratch against the *current* in-memory app. The
  **production code** (SignalR Redis backplane **plus** the work that makes Lighthouse actually
  multi-replica-safe — single-instance background updaters + a distributed status cache) is deferred
  to a dedicated full-nWave story **#5304**, because those pieces are coupled (the backplane alone
  doesn't yield a scalable app) and nothing needs real multi-replica until the SaaS bands. So story 07
  is now light-loop, not a D3 exception; #5304 is the D3 exception. See
  `stories/story-07-research.md` §1 for the (A)/(B)/(C) reframe.

**Rule of thumb:** infra/learning = light loop; Lighthouse product code = full nWave.

### D4 — Topology: the standalone stays single-image; the k8s version may split (Q4)

Two constraints, deliberately kept apart:

- **The standalone / server deployment is sacrosanct.** Today the image builds the React frontend
  into the backend's `wwwroot` and the API serves it — one process, one container. That *is* the
  "simple, good-enough-for-many" product and it **does not change**. Self-hosters keep getting the
  one-container experience.
- **The k8s version is free to diverge** if it earns its keep. Splitting the SPA into its own tiny
  static-serving Deployment (nginx) behind path-based Ingress (`/` → frontend; `/api`, `/hub`
  SignalR, `/mcp` → backend) is fair game. The mechanism: a Helm values toggle
  `frontend.mode: embedded | split`, so the chart serves both the simple and the split topology
  from one source. This is the recommended shape — see **Q4** for the recommendation and rationale.

Either way, the MCP server (06) is a genuine *second* workload; story 01 stands up the API
(serving the SPA in `embedded` mode) as the single app workload.

### D5 — The gate is "definition of learned," not DoR

A band is "done" when you can demonstrate its band-level competency (section 1) on demand — not when
a checklist is ticked. The artifact left behind (a manifest, a chart, a runbook) is evidence, not
the goal.

---

## 4. North-star architecture (DESIGN + DEVOPS, guide mode)

The hosted end-state every story slices toward. **A hypothesis — revise as you learn (section 5).**

```
                         Internet
                            │
              *.lighthouse.letpeoplework.com  (wildcard DNS → server IP)
                            │
                    ┌───────▼────────┐
                    │ Traefik Ingress │  TLS via cert-manager DNS-01 (one wildcard cert)
                    └───────┬────────┘
                            │  host = {tenant}.lighthouse.letpeoplework.com
                    ┌───────▼────────┐
                    │  oauth2-proxy   │  edge auth enforcement (OIDC → Entra ID)
                    └───────┬────────┘
                            │   frontend.mode=split (Q4): a SHARED nginx SPA sits here,
                            │   serving every subdomain; /api,/hub,/mcp route per-tenant.
                            │   mode=embedded: SPA lives in the API pod (standalone parity).
     ╔══════════════════════▼═══════════════════════════╗   namespace per tenant
     ║  namespace: tenant-<name>                          ║   (NetworkPolicy isolates them)
     ║                                                    ║
     ║   ┌──────────────┐   ┌──────────────┐             ║
     ║   │ Lighthouse   │   │ Lighthouse   │             ║
     ║   │ API          │   │ MCP HTTP svc │             ║
     ║   │ N replicas   │   │              │             ║
     ║   │ + Redis      │   └──────┬───────┘             ║
     ║   │   backplane  │          │                     ║
     ║   └──────┬───────┘          │                     ║
     ║          │                  │                     ║
     ║   ┌──────▼──────────────────▼───────┐            ║
     ║   │ Postgres  (PVC)                  │  ← per-tenant DB (open decision Q2)
     ║   └──────┬──────────────────────────┘            ║
     ║          │ pg_dump CronJob → object storage      ║
     ╚══════════╪════════════════════════════════════════╝
                │
   ┌────────────▼─────────────┐     ┌────────────────────────────┐
   │ Object storage (backups) │     │ Platform namespaces:        │
   │ Hetzner/Azure Blob       │     │  ArgoCD · cert-manager · ESO │
   └──────────────────────────┘     │  kube-prometheus · Loki      │
                                     └────────────────────────────┘

   Control plane (out-of-cluster):
     lighthouse-gitops repo  ──▶  ArgoCD ApplicationSet (directory generator on tenants/)
     Renovate PRs (image/chart bumps)  ──▶  merge  ──▶  ArgoCD sync (sync-waves: migrate→deploy)
     Azure Key Vault  ──▶  External Secrets Operator  ──▶  per-tenant Secrets
```

**Deployment / ops strategy in one paragraph:** A tenant is a folder under `tenants/` in the private
repo. An ArgoCD `ApplicationSet` (directory generator) turns each folder into an `Application` that
renders the public Helm chart with that tenant's `values.yaml`. Secrets never sit in Git: locally
SealedSecrets, in prod ESO pulls from Key Vault. Wildcard DNS + a DNS-01 wildcard cert mean a new
tenant gets a valid HTTPS subdomain with **zero** DNS work. Renovate watches the image and chart
deps and opens PRs; merging one rolls the new version to all tenants, with ArgoCD sync-waves
ensuring Postgres migrations run before the API starts. Prometheus/Grafana/Loki give per-tenant
health and usage; per-tenant `pg_dump` CronJobs to object storage give a tested restore path.

---

## 5. Revisable hypotheses (north-star caveats)

Design bets that learning will confirm or overturn. Revisit at each band boundary:

1. **k3s in prod is enough.** Single-node (or small) k3s-on-a-VM may suffice for tenant-zero +
   demo, deferring managed-k8s cost/complexity. Revisit if HA or node-scaling pressure appears (Q1).
2. **Postgres-per-tenant.** Strongest isolation and trivially per-tenant backups, at higher
   resource cost. A shared Postgres with a DB-per-tenant is the cheaper alternative (Q2). Story 18's
   "per-tenant `pg_dump`" works under either — decide before 14.
3. **oauth2-proxy at the edge is sufficient** alongside app-level OIDC. May be redundant for some
   surfaces; the MCP endpoint (06) likely wants token auth *instead of* browser OIDC — see Q5 for the
   inbound-auth model that decision turns on.
   > **Update 2026-06-20 (ADR-079):** for the **MCP surface this is settled — `oauth2-proxy` is NOT
   > used.** The MCP client drives OAuth itself and sends a Bearer that Lighthouse validates
   > directly; a proxy would be a redundant edge gate (and a trusted-header variant would break the
   > standalone D1 gate). oauth2-proxy may still earn its place in front of *browser* surfaces, but
   > not for `/mcp`.
4. **ESO + Azure Key Vault** assumes LPW stays on the Azure/Entra side for identity+secrets even if
   compute is elsewhere (e.g. Hetzner). Confirm the identity/secrets home (Q3) before 13.
5. **Single cluster, namespace-per-tenant** is the multi-tenancy model. Fine for early scale;
   cluster-per-tenant only if a customer demands hard isolation.
6. **Embedded frontend is the default; split is a SaaS-scale optimization** (Q4). The standalone
   product never changes; the split only pays off in the hosted SaaS, where one shared
   tenant-agnostic static frontend could serve every subdomain while backends stay per-tenant.
7. **Ingress is the routing API.** Band A learns Ingress (story 03) because it's ubiquitous, simplest,
   and what Helm charts use — but the k8s project has **frozen** the Ingress API (GA + stable, no
   removal, but no new features) and recommends the **Gateway API** (GatewayClass / Gateway /
   HTTPRoute) for new work. Concepts map 1:1 and cert-manager supports both, so Ingress is the on-ramp,
   not a dead end. **Revisit at the chart/SaaS boundary**: the public Helm chart (story 09) and
   per-tenant routing + wildcard TLS (stories 11–12) are where Gateway API's expressiveness and
   multi-tenant routing model may earn a switch (or a chart toggle). Traefik on k3s backs both.

---

## 6. Open decisions for you (guide-mode — your calls)

These aren't blockers for Band A, but they shape Bands D–E. Decide by the band that needs them:

- **Q1 (by Band D/E):** Prod substrate — **k3s-on-a-VM** (cheap, mirrors local, more ops by hand)
  vs **managed k8s** (Hetzner/Azure, less ops, more cost/abstraction). Learning argument favors
  k3s-on-VM; durability argument favors managed.
- **Q2 (by story 14):** Tenant DB isolation — **Postgres-per-tenant** (isolation, simple backups)
  vs **shared Postgres, DB-per-tenant** (cheaper). Affects 02, 17, 18.
- **Q3 (by story 13):** Identity + secrets home — **Entra ID + Azure Key Vault** (assumed) vs
  something else. Affects 05, 13, 14.
- **Q4 (by story 01, revisitable at 09 + Band D):** k8s frontend topology — **`embedded`**
  (API serves the SPA, mirrors standalone) vs **`split`** (separate nginx Deployment). The
  standalone stays embedded regardless. *Recommendation:* default **embedded** through Bands A–C —
  the SPA is small, static, and cacheable at the Ingress, so a split buys little for self-hosters
  while doubling images/versioning. Add the `frontend.mode: split` path in the chart **when Band D
  starts**, where it actually pays off: one shared static frontend serving every tenant subdomain
  (it derives its API base from its own hostname) while backends stay per-tenant. So: build the
  toggle, default it off, flip it on for the hosted SaaS. Affects 01, 03, 08, 09.
- **Q5 (by Band C/D productization — chart 09 / SaaS 11–13):** MCP HTTP server inbound-auth model.
  Story 06 deploys today's published `mcp-http` container, which holds **one** baked-in
  `LIGHTHOUSE_API_KEY` (a Secret) and authenticates *downstream* to Lighthouse — but does **not**
  authenticate the *inbound* caller. That makes it a **confused deputy**: every caller drives
  Lighthouse as that single key's owner/scope, which is exactly why story 06 forces a ClusterIP-only
  vs edge-auth exposure decision. Lighthouse's backend already supports the better model — API keys
  are **owner-resolved** (`ApiKey.OwnerSubject` → `sub` claim in `ApiKeyAuthenticationHandler`) and
  **permission-scoped** (`ApiKeyPermission`: role + scope) — so a caller's own credential already
  maps to *their* identity and rights. Two productization options, decision deferred:
  - **(b, preferred) Official MCP OAuth pass-through** — adopt the MCP spec (rev 2025-06-18)
    Authorization framework so each caller authenticates with their own OAuth token; no shared key to
    bake, seal, distribute, or rotate. Removes the ambient authority entirely → an unauth'd `/mcp` is
    no longer an open hole, and per-user RBAC + audit come for free.
  - **(a, interim) `X-Api-Key` pass-through** — the client sends its own Lighthouse API key, the MCP
    server forwards it. Simpler than OAuth, reuses the existing owner-resolved/scoped key model, same
    no-ambient-authority benefit; costs N user-held keys instead of one Secret.
  Either way the change is primarily in the **lighthouse-clients** repo (the MCP server); the
  Lighthouse backend likely needs little/nothing. **Solve this when we productize the MCP server**
  (chart/SaaS boundary), not in the throwaway story-06 scaffold. Affects 06, 09, 11–13; CLAUDE.md
  Lighthouse-Clients cross-cutting checklist applies.

  > **RESOLVED 2026-06-20 (ADR-079).** Both auth models shipped, and the earlier "backend needs
  > little/nothing" guess was wrong — the preferred (OAuth) model needs a real backend capability:
  > - **Interim X-Api-Key pass-through** — shipped (epic-5305 #5307 / clients): `mcp-http`
  >   forwards the caller's own `X-Api-Key` (or Bearer) instead of a baked key. The model for
  >   **CLI + stdio + standalone**.
  > - **Hosted MCP OAuth (preferred)** — shipped end-to-end across two repos: the **MCP client**
  >   does the browser OAuth flow against the **same OIDC provider as Lighthouse** and sends its
  >   own `Authorization: Bearer`; `mcp-http` **advertises** RFC 9728 protected-resource metadata +
  >   `401` challenge (clients #5330) and forwards the token; the **Lighthouse backend validates
  >   the IdP JWT** (a new `JwtBearer` scheme — backend #5329) and maps it through the existing
  >   RBAC. **No API key.** **`oauth2-proxy` is NOT needed** (the MCP client drives OAuth itself; a
  >   proxy would be a redundant edge gate) — this overturns hypothesis #3 for the MCP surface.
  > - **Deferred to this epic (#5306): the live end-to-end dogfood**, which needs the real
  >   environment #5306 builds. Readiness checklist:
  >   1. IdP (Entra/Keycloak, Q3) **exposes a Lighthouse API audience/scope** so access tokens
  >      carry `aud = <Lighthouse API>` (the backend rejects wrong-audience tokens).
  >   2. The MCP client requests a token **scoped to Lighthouse via RFC 8707 resource indicators**
  >      — the SPIKE risk: confirm the client + IdP actually support this; if not, fall back to the
  >      shipped X-Api-Key path.
  >   3. Set `Authentication:Audience` on Lighthouse + `LIGHTHOUSE_OAUTH_ISSUER` /
  >      `LIGHTHOUSE_OAUTH_RESOURCE` on `mcp-http` (a startup version gate refuses OAuth against a
  >      Lighthouse ≤ v26.6.16.14).
  >   4. Run the browser flow with a real MCP client (Claude Desktop / MCP Inspector) → two users
  >      each see only their own RBAC-scoped data; an unauth'd `/mcp` is challenged, not open.

---

## 7. How to run each story (the light loop)

**Every story doc opens with a 🗂 Workspace banner** stating whether the work is throwaway scratch
or repo-bound (and which repo), so it's never ambiguous where files belong. The map:

| Stories | Workspace | Where files live |
|---------|-----------|------------------|
| 00–08 | **scratch** | personal dir e.g. `~/learn-k8s/story-NN/` — throwaway, never committed |
| 09 (Helm) | **repo (public)** | this `lighthouse` repo, `chart/` |
| 10 (docs) | **repo (public)** | this `lighthouse` repo, `docs/` |
| 11–18 (GitOps/ops) | **repo (private)** | `lighthouse-gitops` |
| 02, 06 (C#/TS) | **repo (public, product code)** | this `lighthouse` repo — full nWave |
| #5304 (horizontal-scaling product code, deferred — split out of 07) | **repo (public, product code)** | this `lighthouse` repo — full nWave, scheduled after the learning stories |

For every non-exception story:

1. **Objective** — already on the ADO card ("Deliver" + "What you'll learn").
2. **`nw-research`** — get curated, cited reading for that story's concepts **plus copy/paste-ready
   commands/manifests** (a "Hands-on" section). The reading explains; the commands are a backstop
   and reference. This is personal learning — the goal is understanding, not withholding answers.
3. **You build it** — you hold the keyboard. Try from memory first, lean on the commands when you
   want them.
4. **`nw-spike`** — throwaway experiments to test understanding (e.g. break a liveness probe, watch
   the restart; scale to 3 replicas, watch SignalR drop).
5. **Claude reviews** — Socratic review of what *you* wrote: what's missing, what would break in
   prod, what you can't yet explain.

For the D3 exception stories (02, 06, and the deferred scaling story #5304): full
`DISCUSS→…→DELIVER` + the CLAUDE.md checklist. (Story 07 was re-scoped to a light loop — see §D3.)

**Suggested start:** story **00 — Local Cluster & kubectl Basics** (#5190). Smallest possible loop,
establishes the k3s rehearsal environment everything else runs on.
