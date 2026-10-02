# Feature Delta — story-6131-chart-postgres-major-upgrade

ADO: User Story #6131 "Chart: upgrade the bundled Postgres across a major version automatically" (Active).
Raised while taking the Postgres 17 → 18 Renovate update (PR #1848).

Density: lean + ask-intelligent. Feature type: infrastructure (Helm chart). Walking skeleton: brownfield,
the chart and its kind-based acceptance suite exist. UX research depth: lightweight. JTBD: yes. The persona
is the platform-operator, and an operator running `helm upgrade` is a user-visible surface.

## Wave: DISCUSS / [REF] Persona IDs

- `platform-operator`, self-hoster flavour: runs the published chart with the bundled Postgres
  (`postgresql.enabled=true`, the default) and upgrades with `helm upgrade` or a GitOps sync.
- `lighthouse-maintainer`: cuts the chart release that moves the default image (secondary actor).

## Wave: DISCUSS / [REF] JTBD One-Liners

- **job-operator-upgrade-bundled-database-across-major** (new): When a chart release moves the bundled
  Postgres to a new major, I want `helm upgrade` to carry my existing data across by itself, so my
  Lighthouse comes back with all its history and I never have to learn `pg_dump`/`pg_restore` or pin an
  image forever to stay safe.
- Supports the existing `job-saas-operator-upgrade-all-tenants-safely` (a version bump rolls every tenant
  without hand work) and `job-operator-configure-via-values` (no template surgery).

## Wave: DISCUSS / [REF] Pre-requisites

- Chart 0.1.17 is the latest release and ships `postgresql.image: postgres:17`. Every bundled install in
  the wild runs 17, unless its operator pinned something else. The chart has never defaulted to any other
  major (verified from the history of `chart/values.yaml`).
- `main` already defaults to `postgres:18` (c6b6bd49e), **unreleased**. The next chart release breaks
  every bundled install that upgrades on default values, unless this story ships first (see D5).
- The chart sets `PGDATA=/var/lib/postgresql/data/pgdata` itself, on a PVC from the StatefulSet's
  `volumeClaimTemplates` (`data-<release>-lighthouse-postgres-0`).
- The manual path is documented today in `docs/Installation/kubernetes.md` § Upgrading the bundled
  PostgreSQL. This story replaces that section.
- Kind-based chart acceptance runs in `.github/workflows/ci_chart.yml`, with features under
  `chart/tests/acceptance/` and helm-unittest suites under `chart/tests/unit/`.

## Wave: DISCUSS / [REF] Locked Decisions

- **D1 — Automatic by default.** No value to switch it on. An upgrade that moves the image just works,
  which is the point of the story. Fresh installs and same-major restarts behave exactly as today.
- **D2 — The previous major's data is kept, untouched, so `helm rollback` works** (maintainer's choice,
  2026-10-02). After a rollback to the previous chart, Postgres starts on exactly the pre-upgrade
  database. Anything written after the upgrade is not in it, and the docs say so. The cost is about 2× the
  data on the PVC until the operator removes the old copy.
- **D3 — Refuse rather than half-upgrade.** If the volume lacks room for the second copy, or the data's
  major cannot be upgraded automatically, nothing on the volume is touched. The Postgres pod stays
  not-ready, and its log names the reason and the way out. A crash-loop that says only
  `database files are incompatible with server` is the failure this story removes.
- **D4 — Automatic source major = the previous major (N-1).** Today that means 17 → 18, the only
  transition any released chart can produce. Data two or more majors behind is refused under D3, with a
  pointer to the manual path. DESIGN may widen the range if the mechanism it picks carries that range at
  no extra cost. Widening needs no new DISCUSS decision.
- **D5 — Release gate** (maintainer's choice, 2026-10-02). If #6131 has not shipped when the next chart
  release is cut, that release puts `postgresql.image` back to `postgres:17`. Postgres 18 becomes the
  chart default only in the same release as the automatic upgrade.
- **D6 — Chart only** (maintainer's choice, 2026-10-02). The docker-compose example
  (`examples/postgres/docker-compose.yml`) keeps its documented manual path.
- **D7 — The same behaviour for `helm upgrade` and for a GitOps tool that renders the chart.** The upgrade
  must not depend on a lifecycle step only the Helm CLI runs. The LPW tenants are deployed by ArgoCD.

## Wave: DISCUSS / [REF] User Stories

### US-01: An upgrade that moves the bundled Postgres to a new major keeps every piece of data
`job_id: job-operator-upgrade-bundled-database-across-major`

As a self-hoster running the bundled Postgres, I want the chart release that moves the default image to
a new major to bring my existing database across by itself, so Lighthouse comes back after `helm upgrade`
with its teams, portfolios, history and forecasts intact.

#### Elevator Pitch
Before: `helm upgrade` to a chart whose Postgres default moved leaves the database pod crash-looping on
`database files are incompatible with server`, and the only ways out are pinning the old image or a
hand-run dump and restore.
After: run `helm upgrade l8e letpeoplework/lighthouse --reuse-values` → sees the Postgres pod become
Ready on the new major (`SELECT version()` reports 18), and the same teams, portfolios and features in
the Lighthouse UI as before the upgrade.
Decision enabled: the operator takes chart upgrades as they come instead of holding back, or scheduling
a manual migration window, for every Postgres major move.

#### Acceptance Criteria
- **AC-1.1** An install on chart 0.1.17 (Postgres 17) with data is upgraded to the new chart (Postgres 18)
  using `helm upgrade --reuse-values` and no other flags. The Postgres pod reaches Ready on 18, the API
  reaches Ready, and every row present before the upgrade is present after it. The check compares row
  counts of all Lighthouse tables before and after, plus one known marker row.
- **AC-1.2** Credentials, the database name and the user are unchanged. The API connects with the
  existing Secret, and no value needs to be added or changed.
- **AC-1.3** The previous major's data is still on the PVC, unchanged, after a successful upgrade (D2).
- **AC-1.4** A fresh install on the new chart initialises an empty 18 database with no upgrade step, as
  today.
- **AC-1.5** Restarting the Postgres pod after a successful upgrade does not run the upgrade again. The
  pod comes up in about the same time as before this story.
- **AC-1.6** `postgresql.enabled=false` (external database) renders exactly as before this story. The
  existing standalone-gate and render unit suites stay green, and no upgrade machinery appears in the
  manifests.
- **AC-1.7** The same upgrade succeeds when the chart is applied by rendering it (`helm template` piped to
  `kubectl apply`, as a GitOps tool does) rather than by `helm upgrade` (D7).

### US-02: Rolling back after an automatic upgrade brings the old database back
`job_id: job-operator-upgrade-bundled-database-across-major`

As a self-hoster whose upgraded Lighthouse misbehaves, I want `helm rollback` to return me to the
database I had before the upgrade, so trying the new release is never a one-way door.

#### Elevator Pitch
Before: once the data is on the new major, the previous image can never open it again, so a rollback
crash-loops and the operator restores from a backup by hand.
After: run `helm rollback l8e <previous revision>` → sees the Postgres pod Ready on 17 and Lighthouse
showing the data exactly as it was before the upgrade.
Decision enabled: the operator decides whether to stay on the new release or go back, knowing either
choice is one command.

#### Acceptance Criteria
- **AC-2.1** After AC-1.1, `helm rollback` to the 0.1.17 revision brings Postgres up Ready on 17 with the
  pre-upgrade data. The marker row is present, and a row written after the upgrade is absent, as the docs
  state.
- **AC-2.2** Upgrading again after that rollback runs the upgrade afresh from the 17 data. It never
  reuses a stale copy of 18 data from the first attempt. A row written on 17 after the rollback is
  present on 18.
- **AC-2.3** `docs/Installation/kubernetes.md` says where the previous major's data sits, how much room
  it takes, that a rollback discards what was written since the upgrade, and the one command that removes
  the old copy once the operator is satisfied.

### US-03: An upgrade that cannot be done safely says why, and touches nothing
`job_id: job-operator-upgrade-bundled-database-across-major`

As a self-hoster, I want an upgrade the chart cannot carry out safely to stop before it changes anything
and tell me what to do, so I am never left with a half-migrated volume or a log that only says the files
are incompatible.

#### Elevator Pitch
Before: every failure looks the same, a crash-loop on `database files are incompatible with server`,
with no hint of the cause or the fix.
After: run `kubectl logs l8e-lighthouse-postgres-0 --all-containers` → sees one line that names the
reason and the way out. For a full volume it gives the free and needed sizes and says to grow the PVC
or free space. For a major gap it names the data's major and the image's major and links the manual
path.
Decision enabled: the operator decides between growing the volume, pinning the old image, or doing the
documented manual move, without opening a support ticket.

#### Acceptance Criteria
- **AC-3.1** When the PVC lacks room for a second copy of the data, the upgrade does not start. The data
  directory is byte-for-byte unchanged, the pod is not Ready, and the log names the free space, the needed
  space, and "grow the PVC" (with `persistence.size`) or "pin `postgresql.image` to your current major".
- **AC-3.2** Data two or more majors behind the image (for example, 16 data with an 18 image) is refused
  in the same way. The log names both majors and points to the manual section of the Kubernetes docs (D4).
- **AC-3.3** Data on a newer major than the image, with no kept copy of the image's major, is refused
  with a log naming both majors. That happens, for example, when an image is pinned back after the old
  copy was removed.
- **AC-3.4** An upgrade interrupted part-way (pod deleted mid-copy) leaves the previous major's data
  intact. The next start either finishes the upgrade from the beginning or refuses under AC-3.1/3.2, and
  never starts Postgres on a partial copy.
- **AC-3.5** After the operator fixes the cause (grows the PVC or pins the image), the next pod start
  proceeds with no other manual step.

## Wave: DISCUSS / [REF] Out of Scope

- The docker-compose example and the binary/standalone server (D6). Their manual paths stay documented in
  `docs/Installation/configuration.md`.
- External databases (`postgresql.enabled=false`). The operator upgrades those the way their provider
  documents.
- Removing the previous major's copy automatically. The operator removes it with the documented command.
- Growing the PVC automatically.
- Downgrading data across majors. Only rollback to a kept copy is supported.
- Upgrades two or more majors apart (D4), unless DESIGN gets them for free.
- Backups, PITR or HA for the bundled Postgres. The bundled database remains a convenience, not a
  managed database.

## Wave: DISCUSS / [REF] Cross-cutting Impact (DISCUSS checklist, no silent N/A)

- **RBAC:** N/A, because no API surface, permission or UI gate changes.
- **Lighthouse-Clients CLI/MCP:** N/A, because no API, DTO or MCP tool changes.
- **Website marketing surface:** N/A, because this is an operational chart fix that no marketing page
  claims or shows.
- **Docs:** `docs/Installation/kubernetes.md` § Upgrading the bundled PostgreSQL is rewritten. It covers
  the automatic upgrade, rollback, removing the old copy, the refusal messages and the manual path for
  out-of-range majors. Other docs to update: the `postgresql.image` comment in `chart/values.yaml`, the
  regenerated `chart/README.md` and `values.schema.json`, if a value is added.
- **Chart version:** a minor note on the next `chart/Chart.yaml` version. The behaviour change is
  additive, and no value is required.
- **Usage data:** answered in DEVOPS. The expected answer is N/A, because the upgrade runs inside the
  database pod, before the API that forwards usage data is up, and the chart has no event pipe.
- **Release notes:** yes. Tag `Release Notes` on #6131. Self-hosters on the bundled Postgres care, and
  the note must say the old copy takes extra room until it is removed.
- **Terminology:** no configurable terms are involved.

## Wave: DISCUSS / [REF] WS Strategy

Brownfield with no new walking skeleton. The existing kind acceptance suite (`@env:ci-kind-clean`) is
the end-to-end harness. Slice 01 adds the first real-I/O scenario: install 0.1.17, seed data, upgrade,
assert.

## Wave: DISCUSS / [REF] Driving Ports

- `helm upgrade` / `helm rollback` against the published chart (CLI).
- Rendered manifests applied by a GitOps tool (`helm template` | `kubectl apply`) (D7).
- `kubectl logs` / pod readiness of the Postgres StatefulSet as the observable surface.
- The Lighthouse API's `/health/ready`, and its data, as the proof the data survived.

## Wave: DISCUSS / [REF] Scope Assessment: PASS

There are 3 stories, 1 bounded context (the chart's bundled-Postgres StatefulSet), 2 technologies (Helm
and Postgres) and 2 integration points (the PVC and the API's connection). The estimate is 1–2 days
across two slices. None of the oversized signals fires.

## Wave: DISCUSS / [REF] Story Map and Slices

Backbone: *detect the data's major* → *decide (no-op / upgrade / refuse)* → *carry data across, keeping
the old copy* → *start on the new major* → *roll back if wanted* → *clean up when satisfied*.

| Slice | Stories | Learning hypothesis (disproves if it fails) |
|---|---|---|
| `slice-01-upgrade-17-to-18-keeping-the-old-copy` | US-01, US-02 | "An upgrade inside the chart can carry a real Lighthouse database 17 → 18 and still let a rollback open the old copy". If it fails, the automatic route is wrong and D5 pins 17 for good. |
| `slice-02-refuse-safely-and-document` | US-03, docs, chart version | "Every unsafe case can be detected before anything is written". If it fails, the guard rails need a design rethink before release. |

Order: slice 01 first, because it carries the highest uncertainty (mechanism, rollback layout, GitOps
parity). Slice 02 builds on its detection step. Each slice ends green on the kind suite and is
dogfooded on a kind cluster seeded from a real Lighthouse backup (`Restore-DbBackup.ps1` data), not
synthetic rows.

Taste tests: each slice adds one capability. Neither is identical to the other except in scale. Both use
production-shaped data, and each disproves a named pre-commitment. Slice 02 carries US-03, which is
user-visible (the refusal line), so it is not infrastructure-only.

## Wave: DISCUSS / [REF] Outcome KPIs

- **K1:** 0 crash-loops on `database files are incompatible with server` reported against the chart
  release that moves the default to 18. Measured from GitHub issues and Slack for 60 days after release.
- **K2:** 100 % of the kind upgrade scenarios (AC-1.1, AC-1.7, AC-2.1, AC-2.2) green in `ci_chart.yml` on
  every chart change, so the guarantee is re-proved, not assumed.
- **K3:** Restart overhead under 5 s for an already-upgraded pod (AC-1.5), measured in the kind suite.

## Wave: DISCUSS / [REF] Definition of Done

1. All ACs of US-01..03 are green in the kind acceptance suite and in the helm-unittest suites.
2. The existing chart unit and acceptance suites are green, including the standalone gate.
3. Dogfood: a kind cluster with a restored real backup on chart 0.1.17 is upgraded and then rolled back,
   and the data is checked in the UI.
4. `docs/Installation/kubernetes.md` section rewritten, and the values comment and chart README updated.
5. The `chart/Chart.yaml` version comment is written.
6. D5 is honoured. The release that defaults to 18 contains this story, or that release pins 17.
7. `ci_chart.yml` is green on `main`.
8. #6131 is tagged `Release Notes`, with a line drafted.
9. No new SonarCloud issues, and `docs/ci-learnings.md` rules are pre-applied.

## Wave: DISCUSS / [REF] DoR Validation

| # | Item | Evidence |
|---|---|---|
| 1 | Problem stated in user terms | Elevator pitches, the ADO description and the crash-loop signature |
| 2 | User/persona identified | `platform-operator`, self-hoster flavour |
| 3 | Job traced | `job-operator-upgrade-bundled-database-across-major`, added to `jobs.yaml` |
| 4 | Testable ACs | 15 ACs, each observable via kubectl, helm or the API on kind |
| 5 | Right-sized | Scope assessment PASS; 2 slices of ≤1 day each |
| 6 | Dependencies known | Pre-requisites; the release gate is D5 |
| 7 | Out of scope explicit | Listed above |
| 8 | Outcome KPIs | K1–K3, each with a target and a measurement method |
| 9 | Decisions the maintainer owns are taken | D2, D5 and D6 were answered 2026-10-02 |

Requirements completeness: 0.96. The remaining open item is D4's range, which is deliberately left to
DESIGN.

## Wave: DISCUSS / [REF] Wave Decisions Summary

- Primary job: chart upgrades that move the Postgres major preserve data with no operator action, and a
  rollback still works.
- Feature type: infrastructure (Helm chart), operator-visible.
- Constraints: old copy kept (D2); refuse before touching data (D3); N-1 automatically (D4); 17 is
  pinned if this story misses the release (D5); chart only (D6); GitOps parity (D7).
- Upstream changes: none. No DISCOVER or DIVERGE artifacts exist for this story.

## Wave: DISCUSS / [REF] Open checks for DESIGN

- The mechanism: an init container with both majors' binaries running `pg_upgrade` in copy mode (for
  example, a `tianon/postgres-upgrade`- or `pgautoupgrade`-style image), versus a dump/restore inside the
  pod. The choice must satisfy D2, D3 and D7, and give no extra start cost once upgraded (AC-1.5).
- The on-volume layout that lets the 0.1.17 chart's fixed `PGDATA=/var/lib/postgresql/data/pgdata` find
  the untouched 17 copy on rollback (AC-2.1), and keeps a stale 18 copy from being reused (AC-2.2).
- Where the new-major image comes from. It must be official or reproducible, pinned and multi-arch. Find
  out whether Renovate can keep the pair of majors in step.
- An unverified side note, outside this story: the compose example on `main` moved its volume mount to
  `/var/lib/postgresql`. Check whether an old volume under the new file starts an *empty* 18 database
  rather than refusing. If it does, raise a separate bug.
