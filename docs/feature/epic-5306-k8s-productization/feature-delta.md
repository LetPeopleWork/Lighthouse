<!-- markdownlint-disable MD024 -->
# Feature Delta — epic-5306-k8s-productization (DISCUSS wave)

> **Scope (HARD):** exactly two stories of Epic #5306 — **#5199** (publishable Helm chart) and
> **#5200** (enterprise setup & docs). The other 9 children (#5201–#5208, #5320) stay LIGHT-LOOP
> per the planning-stage north star and are **out of scope** here. See *Out of Scope*.
>
> **Density:** lean mode. No expansion-menu trigger fired (see *Wave: DISCUSS / Expansion Triggers*).
>
> **Feature type:** Infrastructure, customer-facing → **JTBD = YES** (real user jobs, not the
> infrastructure-only escape valve). Every story carries an Elevator Pitch and a `job_id`.

---

## Wave: DISCUSS / [REF] Prior Wave Consultation

| Source | Status | Use |
|--------|--------|-----|
| `docs/product/personas/platform-operator.yaml` | ✓ read | CONSUMER persona (self-hoster flavour); standalone-gate / auto-degrade / expand-only vocabulary |
| `docs/product/personas/lighthouse-maintainer.yaml` | ✓ read | PUBLISHER persona; low-ceremony-automation, finalization-gate, drift vocabulary |
| `docs/product/jobs.yaml` (6 operator jobs, ~L1919–2091) | ✓ read | Continuity + `job-operator-*` / `job-maintainer-*` naming convention; the 6 existing jobs are all RUNTIME, none cover install/package/configure/document |
| `docs/product/journeys/epic-5305-k8s-readiness.yaml` | ✓ read | Sibling journey style matched (lightweight, operational, kubectl/helm "screen") |
| `docs/feature/l8e-kubernetes-learning/planning-stage.md` | ✓ read | North star: §0 split, §1 Band C, §3 D1/D4, §4 architecture, §6 Q1–Q5. Treated as DISCOVER/DIVERGE evidence; not contradicted |
| `docs/product/kpi-contracts.yaml` | ✓ read | `OUT-*` naming + `measurement_scope` convention (self-hosted, no central telemetry) |
| `docs/product/vision.md` | ⊘ absent | noted |
| `docs/project-brief.md` | ⊘ absent | noted |

---

## Wave: DISCUSS / [REF] Personas

| Persona ID | Role in this feature | Flavour |
|------------|----------------------|---------|
| `platform-operator` | CONSUMER of the chart + docs — runs `helm install`, configures via values, reads docs to self-host and evaluate | **self-hoster** (aliases self-hoster / k8s-operator / devops-engineer) |
| `lighthouse-maintainer` | PUBLISHER — packages, versions and publishes the chart + README to the LPW GitHub org Helm repo | publisher / release-finalizer |

No new personas invented (SSOT reuse, as mandated).

---

## Wave: DISCUSS / [REF] JTBD — Job Stories, Forces, Opportunity Scores

Five new jobs created in `docs/product/jobs.yaml` (full dimensions/forces/scores there). One-liners:

| job-id | Persona | One-line | Opp. score (I/S/gap) |
|--------|---------|----------|----------------------|
| `job-operator-install-whole-stack-one-command` | platform-operator | Install the whole stack with one `helm install` instead of hand-assembling manifests | 5/1/**4** |
| `job-operator-configure-via-values` | platform-operator | Configure image/replicas/host/OIDC/DB/MCP/frontend.mode by editing values, not templates | 4/1/**3** |
| `job-operator-trust-clean-first-run` | platform-operator | Trust a clean first run — NOTES.txt guidance + values-enterprise.yaml reference + fail-fast on missing values | 3/1/**2** |
| `job-operator-self-host-from-docs` | platform-operator | Self-host AND evaluate from the docs alone (diagram, quick-start, config ref, demo walkthrough) | 4/1/**3** |
| `job-maintainer-publish-versioned-chart` | lighthouse-maintainer | Publish + version the chart with near-zero per-release ceremony | 3/1/**2** |

**Four-forces summary (highest-gap job, install-whole-stack):** Push = a day of fragile interdependent
YAML; Pull = one command brings it all up; Anxiety = "will the chart hide so much I can't fix it / force
a topology I don't want?"; Habit = operators expect a Helm chart for non-trivial k8s apps. The anxiety
is answered by the standalone gate (embedded default = the simple shape) and vendor-neutrality.

**Gap fills (grounding):** the 6 existing operator jobs are all RUNTIME (survive-replicas,
zero-downtime, pod-health, behind-proxy, observe, mcp-identity). None cover INSTALL / PACKAGE /
CONFIGURE / DOCUMENT / PUBLISH — that is the new job space these five occupy.

---

## Wave: DISCUSS / [REF] Locked Decisions

1. **Distribution = PUBLIC OSS Helm chart** on a GitHub Pages Helm repo (LPW GitHub org). `helm install`
   for any external self-hoster. (Resolves the open question on card #5199.)
2. **Primary persona = self-hoster flavour of `platform-operator`** (consumer); `lighthouse-maintainer` = publisher.
3. **Feature type = Infrastructure, customer-facing → JTBD = YES.**
4. **Research depth = LIGHTWEIGHT** — happy-path operational journeys (helm/kubectl/browser), matching epic-5305.
5. **Walking Skeleton = minimal publishable `helm install l8e ./chart -f values-enterprise.yaml`** that brings the whole stack up end to end (slice 01 is the thinnest cut of it).
6. **THE STANDALONE GATE (planning-doc D4):** the single-container standalone/server deployment is
   sacrosanct, unchanged. The chart serves both topologies from one source via `frontend.mode:
   embedded|split` (default **embedded**). The chart is **additive**.
7. **Vendor-neutral** where the planning doc leaves it open (Q1 substrate, Q2 DB isolation, Q3 identity
   home). The PUBLIC chart must not hard-code LPW-internal SaaS choices; those live in the private
   gitops overlay (out of scope).

---

## Wave: DISCUSS / [REF] Pre-requisites (epic-5305 shipped work — do NOT redesign)

The chart **consumes** these already-shipped k8s-readiness capabilities as configuration surface:

| Capability | Story | Chart consumes it as |
|------------|-------|----------------------|
| Health probes (live/ready/startup) | #5310 | probe wiring on the API Deployment (gates rollout) |
| Forwarded headers (behind proxy) | #5311 | config-gated UseForwardedHeaders → correct OIDC/cookies behind ingress |
| Graceful shutdown / drain (SIGTERM) | #5309 | terminationGracePeriodSeconds + drain on rolling update |
| Expand-only migrations + startup lock | #5308 | safe concurrent-pod startup; sync-wave-free additive migrations |
| SignalR Redis backplane + single-instance bg work | #5304 | optional Redis values enable replicaCount > 1 without double-sync |
| /metrics + structured logging | #5312 | scrape/observe values (off-by-default for the self-hoster) |

These are **Pre-requisites, already shipped.** This feature does not change them.

---

## Wave: DISCUSS / [REF] Driving Ports (entry points)

| Port | Type | Owned by |
|------|------|----------|
| `helm install l8e ./chart -f values-enterprise.yaml` | CLI command | self-hoster |
| `helm repo add letpeoplework <gh-pages-url>` / `helm search repo lighthouse` | CLI command | self-hoster |
| Published Helm repo URL (GitHub Pages, LPW org) | HTTP(S) artifact | maintainer publishes / self-hoster consumes |
| NOTES.txt post-install output | stdout text | self-hoster reads |
| Published docs site pages (architecture / quick-start / config ref / demo walkthrough) | rendered web pages | self-hoster + prospect |
| `helm package` + version bump + index publish | CLI / CI step | maintainer |

---

## Wave: DISCUSS / [REF] Lightweight Journey

Three operational journeys authored in `docs/product/journeys/epic-5306-k8s-productization.yaml`
(sibling style to epic-5305). Summary:

1. **self-host-the-whole-stack-with-one-command** (#5199) — Daunted → Configuring → Relieved.
2. **evaluate-and-self-host-from-the-docs** (#5200) — Skeptical → Following → Convinced.
3. **publish-and-version-the-chart-with-near-zero-ceremony** (#5199/#5200, maintainer) — Wary → Automating → Confident.

Shared-artifact consistency tracked in the journey YAML: `chart-version`, `values-enterprise-keys`,
`ingress-hostname`, `helm-repo-url` (each with single source + consumers + failure message).

---

## Wave: DISCUSS / [REF] User Stories

> Combined-file heading convention applied (`## Wave` is the document spine; stories use `###`).

### US-01 — One-command install of the whole stack (#5199)

`job_id: job-operator-install-whole-stack-one-command`

**Problem.** A self-hoster (e.g. Dana Okafor, SRE at a 40-person consultancy) wants Lighthouse on her
k3s cluster. Today she must hand-write Deployment, Service, Ingress, Postgres, Secrets and ConfigMaps
and wire them together — a day of YAML with a dozen subtle failure modes.

**Who.** platform-operator (self-hoster) | runs a conformant k8s + ingress controller | wants Lighthouse as a unit, not a science project.

**Solution.** A publishable Helm chart that renders the full stack and brings it up with one command,
embedded single-container shape as the default (standalone gate).

#### Elevator Pitch
- **Before:** Dana hand-writes and wires six interdependent manifests.
- **After:** she runs `helm install l8e ./chart -f values-enterprise.yaml` and `kubectl get pods` shows `l8e-api 1/1 Running` (+ Postgres, + MCP if enabled); NOTES.txt prints her access URL.
- **Decision enabled:** "Lighthouse is installable as a unit — I can adopt it for my org."

#### Domain Examples
1. **Happy path** — Dana on fresh k3s runs `helm install l8e ./chart -f values-enterprise.yaml` (image `v26.6.x`, host `lh.acme.internal`, Postgres + OIDC set); all pods Ready; NOTES.txt prints `https://lh.acme.internal`.
2. **Edge (simple shape)** — Marco Silva, a solo self-hoster, installs with defaults only; `frontend.mode` stays `embedded`; he gets the one-container experience unchanged (standalone gate).
3. **Error (missing required value)** — Priya Nair omits the Postgres password; `helm install` fails fast naming `postgres.password`, no half-broken release.

#### UAT Scenarios (BDD)
```gherkin
Scenario: Self-hoster brings the whole stack up with one command
  Given Dana has a clean k3s cluster and a values-enterprise.yaml with image, host, Postgres and OIDC set
  When she runs `helm install l8e ./chart -f values-enterprise.yaml`
  Then all chart workloads reach Ready and Helm exits 0
  And NOTES.txt prints the access URL https://lh.acme.internal and the next step

Scenario: Default install preserves the single-container shape
  Given Marco installs the chart with default values only
  When the chart renders
  Then frontend.mode is embedded and exactly one API workload serves the SPA in-process

Scenario: Missing required value fails fast
  Given Priya omits the Postgres password from her values
  When she runs helm install
  Then rendering fails with a message naming the missing postgres.password key
  And no partial release is created

Scenario: Configured topology comes up healthy behind ingress
  Given Dana set the ingress hostname and enabled MCP
  When the install completes
  Then the app responds on the configured hostname and an MCP workload is Ready
```

#### Acceptance Criteria
- [ ] `helm install l8e ./chart -f values-enterprise.yaml` of the real published image brings all workloads to Ready.
- [ ] Default values render `frontend.mode: embedded`, one API workload (standalone gate).
- [ ] Omitting a required value fails fast naming the key; no partial release.
- [ ] NOTES.txt prints the resolved access URL + next step.
- [ ] `mcp.enabled` and `replicaCount` (with Redis pre-req) behave as configured.

#### Outcome KPIs
- **Who**: external self-hosters attempting a k8s install
- **Does what**: complete a clean first install from the chart without hand-writing manifests
- **By how much**: ≥ 90% of attempted `helm install` runs reach all-pods-Ready first try (vendor-demo + per-instance scope)
- **Measured by**: install-walkthrough E2E (vendor demo) + structured post-install log event
- **Baseline**: 0% (no chart exists today)

#### Technical Notes
- Pre-reqs: epic-5305 #5304/#5308/#5309/#5310/#5311/#5312 (shipped). Chart consumes, does not redesign.
- Vendor-neutral: no hard-coded LPW substrate/DB/identity (Q1/Q2/Q3 are the operator's values).
- `frontend.mode: split` path is a toggle stub here; full split wiring deferred (Band D, out of scope).

---

### US-02 — Enterprise setup docs + demo walkthrough (#5200)

`job_id: job-operator-self-host-from-docs`

**Problem.** A prospect / self-hoster (e.g. Tomás Reyes, platform lead evaluating Lighthouse) can't tell
whether he can run it himself or whether it's real — and he won't book a sales call to find out. The
production self-host story is tribal knowledge.

**Who.** platform-operator (self-hoster reads to deploy) + prospect (reads to evaluate) | lands on the published docs | needs self-serve.

**Solution.** Published docs: architecture diagram, prerequisites, quick-start, full config reference, and
a runnable demo walkthrough (install → auth → MCP → scaling).

#### Elevator Pitch
- **Before:** the self-host story is undocumented; a prospect must book a call.
- **After:** Tomás opens the published docs page and sees the architecture diagram (Ingress → oauth2-proxy → API + MCP + Postgres), a copy-paste quick-start that gets him to a running instance, a full config reference, and a demo walkthrough he can run verbatim.
- **Decision enabled:** "I can deploy this myself AND I'd pitch it internally."

#### Domain Examples
1. **Happy path** — Tomás follows the quick-start verbatim on his conformant cluster and reaches a responding instance in under 15 minutes.
2. **Edge (evaluation)** — Aisha Bello, a prospect, reads the architecture diagram + demo walkthrough without installing and concludes it's pitch-worthy.
3. **Error (drift)** — a values key was renamed in the chart; the finalization drift check flags the stale config-reference entry before publish.

#### UAT Scenarios (BDD)
```gherkin
Scenario: Self-hoster reaches a running instance from the quick-start
  Given Tomás is on a conformant k8s cluster with an ingress controller
  When he follows the published quick-start verbatim
  Then he reaches a responding Lighthouse instance using the published chart

Scenario: Prospect evaluates from the architecture and demo docs
  Given Aisha is evaluating Lighthouse without installing
  When she reads the architecture diagram and the demo walkthrough
  Then she sees Ingress -> oauth2-proxy -> API + MCP + Postgres and a runnable install -> auth -> MCP -> scaling sequence

Scenario: Demo walkthrough runs end to end
  Given a reader runs the demo walkthrough against the real image
  When they execute install, auth, MCP and scaling stages in order
  Then each stage produces the documented observable output (pods Running, OIDC login, an MCP call, replicas scaled)

Scenario: Config-reference drift is caught at finalization
  Given a chart values key has been renamed
  When the finalization drift check runs
  Then the stale config-reference entry is flagged before publish
```

#### Acceptance Criteria
- [ ] Published docs include a rendered architecture diagram (Ingress → oauth2-proxy → API + MCP + Postgres).
- [ ] Quick-start, followed verbatim, reaches a responding instance using the published chart.
- [ ] Every config-reference option maps to a real chart value (no documented-but-nonexistent knobs).
- [ ] The demo walkthrough's four stages each produce their documented observable output.
- [ ] Docs + README published to the LPW GitHub org; drift check guards config-reference ↔ values.yaml.

#### Outcome KPIs
- **Who**: self-hosters/prospects landing on the enterprise docs
- **Does what**: self-serve to a running instance (or an evaluation decision) without a support ticket or sales call
- **By how much**: quick-start completes to a responding instance in ≤ 15 min in the vendor-demo walkthrough; 0 documented-but-nonexistent config keys
- **Measured by**: docs-walkthrough E2E (vendor demo) + config-reference-vs-values drift check
- **Baseline**: 0 (no enterprise self-host docs today)

#### Technical Notes
- Config reference is generated from / cross-checked against values.yaml comments (single source of truth).
- Vendor-neutral prerequisites (a conformant k8s + ingress controller); no LPW-specific substrate assumption.
- Depends on US-01 (the chart must exist to be documented).

---

## Wave: DISCUSS / [REF] Story Map

**User:** platform-operator (self-hoster). **Goal:** self-host the whole Lighthouse stack and trust it.

| Discover/Add chart | Configure | Install full stack | Publish | Document/Evaluate |
|--------------------|-----------|--------------------|---------|-------------------|
| `helm repo add` / inspect values (S04/S01) | set image/replicas/host (S02) | one-command full stack: API+PG+MCP+OIDC (S03) | package+version+publish (S04) | arch diagram + quick-start (S05) |
| minimal chart skeleton (S01) | values-enterprise.yaml reference (S02→S03) | NOTES.txt + fail-fast (S01/S03) | refuse-silent-overwrite (S04) | config ref + demo walkthrough (S05) |

### Walking Skeleton
**Slice 01** — `helm install l8e ./chart` of the real image → API pod Ready + NOTES.txt access URL.
Thinnest end-to-end cut of the locked WS (one command brings the stack up); one task across the
install activity, embedded default.

### Release slices
- **WS:** S01 (one-command install, API up).
- **R1 — configurable + full stack:** S02 (values: image/replicas/host) → S03 (Postgres + MCP + OIDC + frontend.mode).
- **R2 — publicly installable + pitch-ready:** S04 (publish to public Helm repo) → S05 (enterprise docs + demo).

### Priority Rationale
Outcome impact + dependency order: S01 (WS) validates the riskiest assumption (one command can bring
the stack up) and unblocks all else. S02→S03 deliver the highest-gap job (`install-whole-stack`, gap 4)
fully. S04 turns it from "installable from source" into "installable by any external user" (the locked
distribution model). S05 (`self-host-from-docs`, gap 3) makes it adoptable + pitch-ready and depends on
the chart existing. Every release slice contains ≥1 user-visible value story (slice-composition gate
satisfied; no @infrastructure-only slice).

---

## Wave: DISCUSS / [REF] Outcome KPIs

### Objective
External self-hosters can stand up and evaluate a production-grade Lighthouse on Kubernetes from a
public chart + docs, with zero bespoke YAML and no sales call.

| # | Who | Does What | By How Much | Baseline | Measured By | Type | Scope |
|---|-----|-----------|-------------|----------|-------------|------|-------|
| 1 | external self-hosters | clean first `helm install` to all-pods-Ready | ≥ 90% first-try success | 0% (no chart) | install-walkthrough E2E + post-install log event | Leading | vendor_demo_only + per_instance |
| 2 | self-hosters/prospects | reach a running instance (or eval decision) from docs alone | quick-start ≤ 15 min; 0 phantom config keys | 0 (no docs) | docs-walkthrough E2E + drift check | Leading | vendor_demo_only |
| 3 | lighthouse-maintainer | publish a new chart version at finalization | published index always == Chart.yaml; 0 silent overwrites | N/A (no pipeline) | finalization-gate check | Guardrail | per_instance (CI) |

- **North Star:** first-try one-command install success rate (KPI 1).
- **Guardrails:** the standalone single-container path keeps working unchanged (must NOT degrade); chart-version consistency across Chart.yaml / index / NOTES.txt / README.
- **Measurement scope note:** Lighthouse is self-hosted with no central telemetry; KPI 1/2 are
  primarily `vendor_demo_only` (the install/docs E2E walkthroughs are the observable population) plus
  optional `per_instance` log events. Matches `kpi-contracts.yaml` convention.

> Append to `docs/product/kpi-contracts.yaml` as `OUT-helm-install-first-try-success`,
> `OUT-enterprise-docs-self-serve`, `OUT-chart-publish-consistency` during the DEVOPS wave (not done
> here — convention noted for handoff).

---

## Wave: DISCUSS / [REF] Definition of Done

- All UAT scenarios green (Outside-In TDD); standalone-gate guard test passes (default values → embedded, one workload).
- A real `helm install` of the real image brings the full stack up Ready on a clean cluster (dogfood).
- Chart published to the LPW GitHub org Helm repo; `helm repo add` + install works from a no-source machine.
- Docs (architecture/quick-start/config ref/demo walkthrough) published; config-reference ↔ values.yaml drift check green.
- Chart version consistent across Chart.yaml / index / NOTES.txt / README.
- CLAUDE.md DISCUSS/DELIVER checklists answered (no silent N/A); per-feature, not batched.

---

## Wave: DISCUSS / [REF] Out of Scope (explicit)

These 9 children of #5306 stay **LIGHT-LOOP** (planning-stage §0/§7) — **NOT** in this DISCUSS:
`#5201–#5208` (GitOps/ArgoCD, wildcard DNS + per-tenant TLS, secrets/ESO, dogfood LPW as tenant-zero,
upgrades, observability ops, provisioning, backup/DR) and `#5320` (OpenTofu substrate). Also out of
scope: the private `lighthouse-gitops` repo and overlay; LPW-internal substrate/DB/identity choices
(Q1/Q2/Q3 — kept vendor-neutral in the public chart); the `frontend.mode: split` full wiring (toggle
stub only; pays off in Band D); any change to epic-5305 runtime code (pre-reqs, already shipped); any
change to the in-app product UX.

---

## Wave: DISCUSS / [REF] Definition of Ready — Validation (9/9)

### US-01 — One-command install (#5199)

| DoR Item | Status | Evidence |
|----------|--------|----------|
| 1 Problem statement (domain language) | PASS | Dana hand-writes 6 interdependent manifests; a day of fragile YAML |
| 2 Persona (specific) | PASS | platform-operator, self-hoster flavour, conformant k8s + ingress |
| 3 ≥3 domain examples (real data) | PASS | Dana/Marco/Priya with real values (host, image tag, missing key) |
| 4 UAT in G/W/T (3–7) | PASS | 4 scenarios |
| 5 AC derived from UAT | PASS | 5 AC trace to scenarios |
| 6 Right-sized (1–3 days, 3–7 scenarios) | PASS | delivered as slices 01–03, ≤1 day each, 4 scenarios |
| 7 Technical notes (constraints) | PASS | epic-5305 pre-reqs, vendor-neutrality, split-deferred |
| 8 Dependencies tracked | PASS | epic-5305 #5304/#5308–#5312 shipped (pre-reqs) |
| 9 Outcome KPIs (measurable) | PASS | KPI 1 (≥90% first-try install) |

### US-02 — Enterprise docs (#5200)

| DoR Item | Status | Evidence |
|----------|--------|----------|
| 1 Problem statement | PASS | Tomás can't tell if he can self-host / if it's real; won't book a call |
| 2 Persona | PASS | self-hoster (deploy) + prospect (evaluate) |
| 3 ≥3 domain examples | PASS | Tomás/Aisha/drift example with real specifics |
| 4 UAT in G/W/T | PASS | 4 scenarios |
| 5 AC from UAT | PASS | 5 AC trace to scenarios |
| 6 Right-sized | PASS | slice 05, ≤1 day, 4 scenarios |
| 7 Technical notes | PASS | config-ref single-source, vendor-neutral prereqs, depends on US-01 |
| 8 Dependencies tracked | PASS | depends on US-01 (chart must exist) |
| 9 Outcome KPIs | PASS | KPI 2 (quick-start ≤15 min; 0 phantom keys) |

**DoR Status: PASSED (9/9 both stories).**

---

## Wave: DISCUSS / [REF] Wave Decisions

- **Scope Assessment (Elephant Carpaccio Gate): PASS** — 2 stories, 5 carpaccio slices, 1 bounded
  context (the chart + its docs), estimated ~5 days. Right-sized for the scoped DISCUSS; the broader
  epic is deliberately NOT widened (9 children stay light-loop).
- **DIVERGE artifacts:** absent as `recommendation.md`/`job-analysis.md`, BUT `planning-stage.md` is the
  authoritative DISCOVER/DIVERGE evidence (north star). Risk of missing formal DIVERGE: LOW — the
  direction, topology and distribution model are all locked there.
- **Slice composition gate:** every release slice has ≥1 user-visible value story; no @infrastructure-only slice.
- **JTBD traceability:** both stories carry a real `job_id` (not infrastructure-only).
- **Standalone gate** threaded through every story/AC (embedded default; additive chart).
- **Vendor-neutrality** threaded through (Q1/Q2/Q3 stay the operator's values).

---

## Wave: DISCUSS / Expansion Triggers

Strict lean mode held. Trigger evaluation:
- Oversized feature (>10 stories / >3 contexts)? **No** (scope is 2 stories, 1 context).
- Conflicting/ambiguous locked decisions? **No** (all settled by the user + planning-stage).
- Missing persona / undefined emotional arc? **No** (both personas SSOT; arcs defined in journey YAML).
- Unresolved red-card blocker? **No.**

**No expansion-menu trigger fired — no menu emitted.**

---

## Wave: DISCUSS / Changed Assumptions

- **Card #5199 open question ("which Helm repo / distribution?") is now RESOLVED** to PUBLIC OSS chart on
  a GitHub Pages Helm repo under the LPW GitHub org. Recorded here per instruction (epic-5305 / DISCOVER
  docs not modified directly).
- **`frontend.mode: split`** is scoped here as a *toggle stub only*; full split wiring is deferred to
  Band D (out of scope), consistent with planning-stage Q4 ("build the toggle, default it off").
- No other planning-stage assumption changed.

---

# Feature Delta — epic-5306-k8s-productization (DESIGN wave)

> **Scope (DESIGN):** system/infrastructure only — a public Helm chart (`chart/`) + enterprise docs. No new backend code, no new domain model (the chart packages already-shipped epic-5305 capabilities). Single architect (System Designer), PROPOSE mode. Density: **lean** — Tier-1 [REF] only; no expansion menu fired. SSOT-integrated: see `docs/product/architecture/brief.md` → `## System Architecture — epic-5306-k8s-productization` and ADR-080..085.

## Wave: DESIGN / [REF] Design Decisions (DDD list)

| # | Decision | Verdict / one-line rationale | ADR |
|---|----------|------------------------------|-----|
| D1 | Database posture | **Postgres-only** chart; bundled in-chart StatefulSet (official image) OR BYO `externalDatabase.*`; **no SQLite**; Bitnami rejected (Broadcom 2025 supply risk) | ADR-080 |
| D2 | Frontend topology | `frontend.mode: embedded` default = the scalable shape (scale via `replicaCount`+Redis); `split` = loud `fail` stub (Band D) — split gives no API-scaling benefit | ADR-081 |
| D3 | Required-value validation | fail-fast naming the key — `values.schema.json` (structure/types/enums/unconditional) + `{{ required }}` (conditional, e.g. DB password); explicit password (no auto-gen) | ADR-082 |
| D4 | Publish mechanism | `docs/charts/` on the **existing artifact-based Pages**, in the existing release stage; **no gh-pages / no chart-releaser** (one Pages source per repo); Chart.yaml single-source + no-overwrite version guard | ADR-083 |
| D5 | Config-reference drift | helm-docs generates the config table from `values.yaml` comments; `git diff` drift gate; narrative docs hand-authored | ADR-084 |
| D6 | MCP server | optional `mcp.enabled` workload (Deployment+Service), orthogonal to `frontend.mode`; inbound-auth per ADR-079 (X-Api-Key / IdP JWT Bearer); not behind oauth2-proxy | ADR-085 |
| D7 | Chart packaging | single chart, `apiVersion: v2`, **no third-party subchart dependency**; one-chart-config-selected-branches (no fork), mirroring ADR-027/epic-5305 | ADR-080..085 |

## Wave: DESIGN / [REF] Component Decomposition (chart-rendered workloads)

| Workload/object | When | Kind / image | Change type |
|---|---|---|---|
| API Deployment + Service | always | product image, `Deployment` (replicaCount) | CREATE template |
| Ingress | `ingress.enabled` (default on) | `Ingress` | CREATE template |
| Postgres StatefulSet + Service + PVC + Secret | `postgresql.enabled` (default on) | official `postgres` image | CREATE template |
| MCP Deployment + Service | `mcp.enabled` | clients `mcp-http` image | CREATE template |
| ConfigMap / Secret(s) | always | `ConfigMap`/`Secret` | CREATE template |
| NOTES.txt | always | Helm notes (URL + MCP/replica summary + kubectl watch line) | CREATE template |
| `values.yaml` + `values.schema.json` | always | chart inputs | CREATE |
| Redis | never (operator-provided) | external | REUSE (config only) |
| nginx split frontend | never (Band D) | `fail` stub | reserved |

## Wave: DESIGN / [REF] Driving Ports

| Port | Type | Owner |
|---|---|---|
| `helm install l8e ./chart -f values-enterprise.yaml` | CLI | self-hoster |
| `helm repo add letpeoplework https://<pages-domain>/charts` / `helm search repo` / `helm install l8e letpeoplework/lighthouse` | CLI | self-hoster |
| NOTES.txt post-install output | stdout | self-hoster |
| Published enterprise docs pages | rendered web | self-hoster + prospect |
| `helm package` + `helm repo index --merge` + commit (existing release stage) | CI step | maintainer |

## Wave: DESIGN / [REF] Driven Ports + Adapters

| Driven dependency | Adapter / mechanism | Gated by |
|---|---|---|
| Postgres (bundled/external) | EF Core Npgsql (epic-5305 `DatabaseConfigurator`) | always |
| OIDC issuer | ASP.NET OpenIdConnect handler (existing) | `oidc.*` |
| Redis (backplane + status store) | SignalR StackExchangeRedis (#5304) | `redis.connectionString` (replicaCount>1) |
| Lighthouse API (from MCP) | `mcp-http` forwards caller credential (ADR-079) | `mcp.enabled` |
| GitHub Pages Helm index | static `docs/charts/index.yaml` via `pages.yml` | publish step |

## Wave: DESIGN / [REF] Technology Choices (pinned)

- Helm 3.x · Chart `apiVersion: v2` · `values.schema.json` (JSON Schema)
- Bundled DB: official `postgres` image StatefulSet (no Bitnami, no subchart dependency)
- `helm-docs` (config-reference generation + drift gate)
- `chart-testing` (`ct`) for lint + template render in CI
- GitHub Pages (existing artifact-based deploy, `docs/charts/`), LPW org
- Publish: `helm package` + `helm repo index --merge` in the existing release workflow (no chart-releaser)

## Wave: DESIGN / [REF] Decisions Table

| ID | Locked decision |
|---|---|
| DDD-1 | Postgres-only chart, bundled-or-BYO, no SQLite (ADR-080) |
| DDD-2 | embedded default scales via replicaCount; split = fail stub (ADR-081) |
| DDD-3 | fail-fast schema + required validation, explicit DB password (ADR-082) |
| DDD-4 | publish via docs/charts on existing Pages, existing release stage, no-overwrite guard (ADR-083) |
| DDD-5 | helm-docs single-source config reference + drift gate (ADR-084) |
| DDD-6 | optional mcp.enabled workload, ADR-079 auth (ADR-085) |
| DDD-7 | single chart, no subchart dependency, no-fork (ADR-080..085) |

## Wave: DESIGN / [REF] Reuse Analysis

| Component | Verdict | Justification |
|---|---|---|
| `chart/` templates + `values.schema.json` + NOTES.txt | CREATE (justified) | no chart exists; standard Helm, no bespoke mechanism |
| In-chart Postgres StatefulSet+Service+PVC+Secret | CREATE (justified) | no bundled-DB template; Bitnami rejected (ADR-080); ~4 small official-image templates |
| epic-5305 runtime capabilities (probes/headers/drain/migration-lock/backplane/telemetry/MCP-auth) | REUSE (config surface) | all shipped + config-gated; chart sets values, no code change |
| `.github/workflows/pages.yml` | EXTEND | already publishes `docs/**`; Helm index under `docs/charts/`, no new Pages source/workflow |
| existing release workflow | EXTEND | add package+index+guard step (CI-consolidation rule) |
| per-feature docs/screenshot discipline | REUSE | narrative docs via existing discipline; only config table generated |
| helm-docs config-reference + drift gate | CREATE (justified) | no values↔docs single-source today; 0 phantom keys by construction (ADR-084) |

Zero unjustified CREATE NEW. Dominant pattern: REUSE/EXTEND of deployment, CI, and docs surfaces; CREATE limited to the chart itself + bundled DB + config-reference generator.

## Wave: DESIGN / [REF] Outcome Collision Check

`nwave-ai outcomes check-delta` **unavailable** (tool broken — `ModuleNotFoundError: jsonschema`, not a real exit-1 collision). The 3 candidate outcomes (`OUT-helm-install-first-try-success`, `OUT-enterprise-docs-self-serve`, `OUT-chart-publish-consistency`) occupy a net-new install/package/publish namespace with no existing `OUT-*` overlap; the DISCUSS KPI section already defers appending them to DEVOPS. No genuine collision; proceeding per contract.

## Wave: DESIGN / [REF] Open Questions (deferred)

- **Live MCP OAuth dogfood** — the ADR-079 readiness checklist (IdP audience/scope, RFC 8707 resource indicators, server version gate `> v26.6.16.14`) needs the real environment; it is an enterprise-docs *prerequisite*, not chart code. Carried to DELIVER/dogfood, out of DESIGN scope.
- **`frontend.mode: split` full wiring** — Band D, out of scope (stub only here).
- **Helm repo public URL / CNAME path** — the concrete `https://<pages-domain>/charts` value is a DEVOPS detail (depends on the repo's Pages domain/CNAME).

## Wave: DESIGN / Changed Assumptions

1. **Slice-01 walking skeleton now brings up API + bundled Postgres** (was: API-only, Postgres deferred to slice-03). Cause: the chart is Postgres-only with no SQLite path (ADR-080), so the first cut needs a database. Still one command. Recorded in `design/upstream-changes.md` for product-owner review of the slice docs.
   > Original (slice-01, *Out of scope*): *"Postgres / MCP / OIDC values (slice 03)."*
   > New: slice-01 additionally renders the bundled Postgres StatefulSet (default `postgresql.enabled: on`) so the API has a database; OIDC/MCP/full enterprise values stay in slice-03.

2. **Publish mechanism refined**: DISCUSS said "GitHub Pages Helm repo (LPW org)". Refined to **`docs/charts/` on the existing artifact-based Pages deploy, in the existing release stage — not a `gh-pages` branch and not chart-releaser-action** (the repo already uses a single artifact-based Pages source; a `gh-pages` branch would conflict). Same outcome (a public GitHub Pages Helm repo), different mechanism (ADR-083). No story/AC change — `helm repo add` UX is preserved.

3. **"Single-container shape" clarified**: the standalone gate / "single-container shape" means **`frontend.mode: embedded` (one *app* workload serving the SPA)**, not "no database workload". The chart always renders a Postgres workload (bundled) or wires an external one. The standalone *image* is unchanged (keeps SQLite); the chart's "simple shape" is embedded frontend + bundled Postgres + MCP off. No contradiction with DISCUSS — clarifies the term.
