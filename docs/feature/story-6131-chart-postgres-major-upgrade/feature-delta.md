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
After: run `helm upgrade l8e letpeoplework/lighthouse --reset-then-reuse-values` → sees the Postgres pod become
Ready on the new major (`SELECT version()` reports 18), and the same teams, portfolios and features in
the Lighthouse UI as before the upgrade.
Decision enabled: the operator takes chart upgrades as they come instead of holding back, or scheduling
a manual migration window, for every Postgres major move.

#### Acceptance Criteria
- **AC-1.1** An install on chart 0.1.17 (Postgres 17) with data is upgraded to the new chart (Postgres 18)
  using a plain `helm upgrade` or `helm upgrade --reset-then-reuse-values` and no other flags. The Postgres pod reaches Ready on 18, the API
  reaches Ready, and every row present before the upgrade is present after it. The check compares row
  counts of all Lighthouse tables before and after, plus one known marker row.
- **AC-1.2** Credentials, the database name and the user are unchanged. The API connects with the
  existing Secret, and no value needs to be added or changed.
- **AC-1.3** The previous major's data is still on the PVC after a successful upgrade, holding exactly the
  pre-upgrade rows (D2). It is the same data, not the same bytes: `pg_upgrade` starts and stops the old
  server once, which rewrites its control file.
- **AC-1.8** `helm upgrade --reuse-values` keeps the previous chart's image, so the database stays on 17.
  Nothing breaks, and `NOTES.txt` prints one line saying the bundled Postgres is behind the chart's
  default and how to move it.
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
  the old copy once the operator is satisfied. That command empties the old folder but leaves a
  placeholder file in it, so a later rollback to the previous chart fails loudly instead of starting an
  empty database.
- **AC-2.4** After the old copy has been removed with that command, a rollback to the previous chart
  does not start an empty database: Postgres refuses to start on the non-empty folder, and the new chart
  still starts on its upgraded data.

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
  directory is byte-for-byte unchanged, the pod is not Ready (it sits in init back-off and retries by
  itself), and the log names the free space, the needed space, and "grow the PVC" (with
  `persistence.size`) or "pin `postgresql.image` to your current major". The test needs a volume whose
  size is enforced; kind's default storage reports the node's disk, not the PVC size.
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
- Resolved in DESIGN: the Postgres 18 image refuses to start on an old volume rather than starting empty
  (read from its entrypoint, not run). Original note, outside this story: the compose example on `main` moved its volume mount to
  `/var/lib/postgresql`. Check whether an old volume under the new file starts an *empty* 18 database
  rather than refusing. If it does, raise a separate bug.

## Wave: DESIGN / [REF] Design Summary

Architect: Platform Architect, interaction mode PROPOSE. Three mechanisms were proposed. The maintainer picked
B on 2026-10-02: two init containers built only from official `postgres` images, running `pg_upgrade --copy`
into a sibling directory named after the new major. ADR:
[ADR-213](../../product/architecture/adr-213-bundled-postgres-crosses-a-major-in-an-init-container-from-official-images.md).

The maintainer also chose how to handle `--reuse-values`, which keeps the old image (see Changed Assumptions).
The acceptance criteria and docs use a plain `helm upgrade` or `--reset-then-reuse-values`, and `NOTES.txt`
prints a line when the image's major is behind the chart's default major.

What was run during DESIGN (Docker 29.8, Helm v4.3.0, a throwaway kind cluster, all removed afterwards):
binary transplant 17 → 18, a full `pg_upgrade --copy`, 18 on the copy, 17 on the original, the unclean
shutdown failure and its recovery, the checksum default, the missing `pg_hba` rule, the `pg_control` hash
change, init container start cost, and `--reuse-values` semantics. Not run: arm64, PVC size enforcement on
kind, image pull time on a fresh node.

## Wave: DESIGN / [REF] Decisions

| # | Decision | Reason |
|---|---|---|
| DDD-1 | Two init containers: `pg-old-binaries` (upgrade-source image) copies 17's `bin`/`lib`/`share` into an emptyDir; `pg-upgrade` (the main image) decides and runs `pg_upgrade --copy` | Only official images are used, and the new cluster is created by the exact binaries that will serve it, so the C library and collation rules match. An init container has no probes, so a long upgrade is never killed by liveness |
| DDD-2 | The old cluster stays in `pgdata/`; the new one is built in `pgdata-<M>.partial/` and goes live by `rename` to `pgdata-<M>/` | 0.1.17's `PGDATA` is fixed at `.../pgdata`, so a rollback finds the old data where it always was. The rename is atomic, so Postgres can never start on a half-built copy |
| DDD-3 | `pgdata-<M>/.lighthouse-upgrade` records source major, system identifier and SHA-256 of the source `global/pg_control` taken after `pg_upgrade` | Any later run of the old major rewrites `pg_control` (measured), so a mismatch proves the copy is out of date and must be redone from the old data |
| DDD-4 | Majors are read at run time: data major from `PG_VERSION`, image majors from `PG_MAJOR` | A mirrored, renamed or digest-pinned image behaves exactly like the default one; no value can lie about the data |
| DDD-5 | `pg-upgrade` writes the chosen `PGDATA` to a file in an emptyDir; the `postgres` container starts through a wrapper that reads it and runs the image's own `docker-entrypoint.sh postgres` | Kubernetes cannot set an environment variable from an init container. The official entrypoint stays untouched, so fresh installs initialise exactly as today |
| DDD-6 | All refusal checks (space, major gap, newer data with no kept copy, old binaries that will not run) happen before any write. A refusal logs one line, writes it to the termination message, and exits non-zero | The volume stays byte-identical. The kubelet retries with back-off, so once the operator grows the PVC or pins the image, the next attempt proceeds by itself |
| DDD-7 | If the old cluster was not shut down cleanly, `pg-upgrade` starts it with the 17 binaries on a Unix socket only (`listen_addresses=''`) and stops it with a fast shutdown, before `pg_upgrade` | `pg_upgrade` refuses an unclean source (measured). The 0.1.17 pod usually ends unclean: SIGTERM means a smart shutdown, which waits for the API's pooled connections until the kubelet kills it |
| DDD-8 | The new cluster copies checksums, encoding, locale and locale provider of `template1`, and the bootstrap superuser (role OID 10) from the old one; `pg_hba.conf` and `pg_ident.conf` are copied across | `pg_upgrade` refuses mismatches, and 18 turns checksums on by default while 17 had them off (measured). A plain `initdb` has no TCP rule for the API (measured) |
| DDD-9 | Space needed = `du` of `pgdata` × 1.1 + 64 MiB; free = `df` on the mount + the size of any stale `pgdata-<M>` or `.partial` that will be removed | The copy came out smaller than the source (60 MB from 76 MB, because the source carries WAL), so the margin is conservative without being wasteful |
| DDD-10 | The documented cleanup empties `pgdata/` and leaves one placeholder file in it | A deleted `pgdata` would make a rollback to 0.1.17 initialise an empty database. With a file present, its `initdb` fails loudly instead |
| DDD-11 | Defaults `postgresql.image: postgres:18-trixie` and new `postgresql.upgrade.image: postgres:17-trixie` | Both images must sit on the same Debian release for the copied binaries to run. The floating major tags could move to a new Debian release at different times |
| DDD-12 | The chart's default major (`18`) and the upgrade-source default are template constants in `_helpers.tpl`, checked against `values.yaml` in CI | Under `--reuse-values` the templates see the previous chart's values, so neither can be read from `.Values` (measured) |
| DDD-13 | `NOTES.txt` prints one line when the numeric major in the `postgresql.image` tag is lower than the default-major constant. If the tag has no number (for example digest only), nothing is printed | Tells an operator who used `--reuse-values` that the database was not moved. It only compares render-time strings and never queries the cluster, so the no-`lookup` gate stays green |
| DDD-14 | Renovate: the upgrade-source image gets minor and digest updates but never a major one; a CI step fails if its major is not exactly one below the main image's | The two move together only in the hand-merged Postgres pull request, where the maintainer moves both |
| DDD-15 | `pg-upgrade` runs as root only to create directories and set ownership, then drops to the `postgres` user with `gosu` for `initdb` and `pg_upgrade`; `pg-old-binaries` runs as the `postgres` user | Same pattern as the official entrypoint, which the chart already runs. Postgres tools refuse to run as root |
| DDD-16 | Everything sits inside `{{ if .Values.postgresql.enabled }}` in the existing StatefulSet template and a new ConfigMap gated the same way | External-database installs render byte-identically to today |

## Wave: DESIGN / [REF] Component Decomposition

| Path | Change | What |
|---|---|---|
| `chart/templates/postgres-statefulset.yaml` | EXTEND | Two `initContainers`, two emptyDirs (old binaries, decision), script ConfigMap volume, `command` on the `postgres` container pointing at the wrapper. PVC, mount path, Service and probes unchanged |
| `chart/templates/postgres-upgrade-configmap.yaml` | CREATE NEW | `copy-old-binaries.sh`, `upgrade.sh` (decision table, checks, recovery, `pg_upgrade`, marker, rename), `start.sh` (wrapper) |
| `chart/templates/_helpers.tpl` | EXTEND | `lighthouse.postgres.defaultMajor`, `lighthouse.postgres.upgradeImage` (value or constant fallback, nil-safe through `dig`), `lighthouse.postgres.imageMajor` (numeric major from the tag, or empty) |
| `chart/templates/NOTES.txt` | EXTEND | The "image major behind the chart default" line |
| `chart/values.yaml` | EXTEND | `postgresql.image: postgres:18-trixie`, `postgresql.upgrade.image: postgres:17-trixie`, rewritten image comment |
| `chart/values.schema.json` | EXTEND | `postgresql.upgrade.image` (string, minLength 1) |
| `chart/README.md` | EXTEND | Regenerated by helm-docs |
| `chart/tests/unit/postgres-upgrade_test.yaml` | CREATE NEW | Render shape, gating, fallbacks under missing keys, NOTES line |
| `chart/tests/acceptance/upgrade-bundled-postgres.feature` | CREATE NEW | Kind scenarios (see Test Plan) |
| `chart/tests/upgrade-path/*.sh` | CREATE NEW | Kind harness scripts driven by the CI job |
| `.github/workflows/ci_chart.yml` | EXTEND | `upgrade-path` job; shellcheck on the ConfigMap scripts; template-constant and major-pairing checks in `validate` |
| `renovate.json` | EXTEND | Package rule blocking major updates of the upgrade-source image |
| `docs/Installation/kubernetes.md` | EXTEND | Section rewritten (slice 02) |

## Wave: DESIGN / [REF] Driving Ports

- `helm upgrade` / `helm upgrade --reset-then-reuse-values` / `helm rollback` against the published chart.
- Rendered manifests applied by a GitOps tool (`helm template | kubectl apply`).
- Pod (re)start of `<release>-lighthouse-postgres-0`, by rollout, deletion or node move. The upgrade runs on
  every start and is a no-op unless one is due.

## Wave: DESIGN / [REF] Driven Ports and Adapters

| Port | Adapter |
|---|---|
| Data volume | The existing PVC `data-<release>-lighthouse-postgres-0`, mounted at `/var/lib/postgresql/data` |
| Old-major binaries | emptyDir filled by `pg-old-binaries`, mounted at `/usr/lib/postgresql/<old>` and `/usr/share/postgresql/<old>` in `pg-upgrade` |
| Decision hand-off | emptyDir file holding the chosen `PGDATA`, read by `start.sh` |
| Operator feedback | Container log plus `terminationMessagePath`, so `kubectl describe pod` shows the refusal line too |
| Database server | The official image's `docker-entrypoint.sh`, unchanged |

## Wave: DESIGN / [REF] Technology Choices

- Images: `postgres:18-trixie` (main) and `postgres:17-trixie` (upgrade source). Both are official, amd64
  and arm64, tracked by Renovate's docker datasource in the existing `Postgres` group. A digest pin is
  optional and not introduced here, to match how the chart pins today.
- Upgrade tool: `pg_upgrade --copy` from the main image. Copy mode, because link and swap modes make the old
  cluster unusable.
- Scripts: POSIX `sh`/`bash` already in the official image (`du`, `df`, `sha256sum`, `stat`, `gosu`, `ldd`).
  No extra image.
- Helm v4.3.0 and helm-unittest v1.1.2, as pinned in `ci_chart.yml`. kind via `helm/kind-action` v1.15.0.
- shellcheck for the ConfigMap scripts in the `validate` job.

## Wave: DESIGN / [REF] On-Volume Layout

```
/var/lib/postgresql/data/            PVC root (mount path unchanged)
  pgdata/                            the major the volume was first initialised with (17 for every
                                     existing install); fresh installs still initialise here
  pgdata-18.partial/                 work in progress; never started; removed at the start of every attempt
  pgdata-18/                         live after an upgrade; created only by renaming the .partial directory
    .lighthouse-upgrade              source major, system identifier, sha256 of pgdata/global/pg_control
```

After the documented cleanup, `pgdata/` holds only `UPGRADED-TO-18-see-kubernetes-docs`.

## Wave: DESIGN / [REF] Decision Table (`pg-upgrade`)

M is the main image's `PG_MAJOR`. D is the data major in `pgdata/PG_VERSION`.

| Volume state | Action | `PGDATA` |
|---|---|---|
| No `pgdata/PG_VERSION`, no placeholder, no `pgdata-*` | Fresh install, nothing to do | `pgdata` |
| D = M | Nothing to do (also covers an image pinned back to the old major) | `pgdata` |
| D = M−1, `pgdata-M` present, hash matches | Already upgraded, nothing to do | `pgdata-M` |
| D = M−1, `pgdata-M` present, hash differs | Old major has run since: delete `pgdata-M`, then upgrade | `pgdata-M` |
| D = M−1, no `pgdata-M` | Checks, then recovery if unclean, `initdb` into `.partial` with the old cluster's settings, `pg_upgrade --copy`, copy `pg_hba`/`pg_ident`, write marker, `fsync`, rename | `pgdata-M` |
| D ≤ M−2 | Refuse, naming both majors and the manual section of the docs | none |
| D > M | Refuse, naming both majors | none |
| Placeholder, `pgdata-M` present | Old copy removed, nothing to do | `pgdata-M` |
| Placeholder, only `pgdata-N` with N ≠ M | Refuse, naming both majors | none |

Refusal line format, one line, for example:
`lighthouse-postgres: refusing upgrade 17->18: needs 812 MiB, 640 MiB free; grow the PVC (postgresql.persistence.size) or pin postgresql.image to postgres:17-trixie`.

## Wave: DESIGN / [REF] Reuse Analysis

| Existing piece | Decision | Why |
|---|---|---|
| Postgres StatefulSet template | EXTEND | The upgrade belongs to this pod; same PVC, same gating |
| Official image entrypoint | REUSE unchanged | Fresh-install behaviour stays identical |
| `_helpers.tpl` | EXTEND | Home of the chart's other render-time constants and guards |
| `NOTES.txt` | EXTEND | Already prints the Postgres image |
| `ci_chart.yml` kind setup | EXTEND | A new job reuses the same Helm and kind actions; existing gates (no-`lookup`, render determinism, helm-docs drift) cover the new templates unchanged |
| `renovate.json` Postgres group | EXTEND | One added rule |
| Upgrade logic | CREATE NEW (ConfigMap) | Nothing in the chart moves data today |
| Third-party upgrade images | REJECTED | Not official (ADR-080 rejected that supply risk); one deletes the old cluster |
| Helm hooks / Jobs | REJECTED | A GitOps sync never runs them |

## Wave: DESIGN / [REF] Test Plan (CI `upgrade-path` job)

A new job in `.github/workflows/ci_chart.yml`, parallel to `install-smoke`, and listed in `publish.needs`.
The "before" chart is `docs/charts/lighthouse-0.1.17.tgz`, already in the repo, so no network fetch is needed.

1. Install 0.1.17. Seed a marker row through `psql` and record row counts of every Lighthouse table.
2. `helm upgrade ./chart` (plain). Assert: `server_version_num` is 18, row counts and the marker match, the API
   reaches `/health/ready`, and `pgdata/PG_VERSION` still reads 17.
3. A second release does the same through `helm template | kubectl apply`.
4. Delete the pod and assert the time to Ready grows by less than 5 s over a pod with no upgrade due.
5. `helm rollback` to 0.1.17. Assert 17, marker present, a row written on 18 absent. Write a row on 17, then
   `helm upgrade` again and assert that row is present on 18.
6. `helm upgrade --reuse-values` from 0.1.17: assert the database stays on 17 and `NOTES.txt` prints the
   behind-the-default line.
7. Refusals, each asserting a byte-identical volume (a sorted `sha256sum` listing taken from a debug pod
   before and after) and the expected line in `kubectl logs --all-containers`:
   - too little space: a static PV backed by a size-limited tmpfs inside the kind node, with `claimRef` set to
     the StatefulSet's claim name. Then enlarge the tmpfs and assert the next retry upgrades;
   - two majors behind: 0.1.17 installed with `postgresql.image=postgres:16`;
   - newer data with no kept copy: the placeholder plus an image pinned to 17.
8. Interruption: seed about 300 MB, delete the pod once the `pg-upgrade` log reports the copy has started, and
   assert the next start finishes the upgrade and the counts match.
9. helm-unittest (`postgres-upgrade_test.yaml`): init containers and ConfigMap render only with
   `postgresql.enabled`; fallbacks render when `postgresql.upgrade` is missing; the NOTES line appears for a
   `postgres:17` image and not for `postgres:18-trixie` or a digest-only reference. The existing standalone-gate
   and render suites stay green unchanged.
10. `validate` job: shellcheck on the ConfigMap scripts; the template constants equal the `values.yaml`
    defaults; the upgrade-source major is the main major minus one.

Dogfood (per slice briefs): the same sequence on a local kind cluster with a restored real backup.

## Wave: DESIGN / [REF] C4 Container View

```mermaid
C4Container
  title Bundled Postgres pod with the in-chart major upgrade
  Person(op, "Platform operator", "helm upgrade, helm rollback or a GitOps sync")
  Container(api, "Lighthouse API", "Deployment", "Connects with the existing Secret, unchanged")
  Container_Boundary(pod, "lighthouse-postgres-0 (StatefulSet)") {
    Container(ob, "pg-old-binaries", "init, postgres:17-trixie", "Copies the 17 binaries into an emptyDir when the data is 17")
    Container(up, "pg-upgrade", "init, main image postgres:18-trixie", "Decides, checks, recovers, runs pg_upgrade --copy, writes the marker, renames")
    Container(pg, "postgres", "postgres:18-trixie", "Wrapper sets PGDATA from the decision, then the official entrypoint")
  }
  ContainerDb(pvc, "PVC data-*-postgres-0", "Kubernetes volume", "pgdata/ kept for rollback; pgdata-18/ live")
  Rel(op, pod, "Renders and applies")
  Rel(ob, up, "17 binaries", "emptyDir")
  Rel(up, pvc, "Reads pgdata, writes pgdata-18.partial, renames it")
  Rel(up, pg, "Chosen PGDATA", "emptyDir file")
  Rel(pg, pvc, "Serves the chosen PGDATA")
  Rel(api, pg, "SQL", "TCP 5432")
```

## Wave: DESIGN / [REF] Open Questions for DISTILL / DELIVER

- `helm upgrade --wait` (5 min default) or an ArgoCD sync timeout can expire during a large upgrade. The pod
  carries on regardless. If the operator's tooling rolls back on timeout, the pod is deleted mid-copy and the
  next start redoes it, which is safe but slow. The docs should mention a longer `--timeout` for big
  databases. No chart change is planned.
- Settings made with `ALTER SYSTEM` are not carried across. The chart never sets any; DISTILL decides whether
  to assert that or just document it.
- arm64 was not run. The images are built per architecture from the same Debian release, so the transplant
  is expected to behave the same; the CI runners are amd64 only.
- The tmpfs-backed static PV for the space refusal has not been tried on `helm/kind-action`. If the kind
  node refuses the mount, the fallback is a test-only value that overrides the measured free space. That
  value would need its own guard so that it never ships.
- The first start on a node pulls `postgres:17-trixie`, which can be slow on a cold node. It is a one-time
  cost per node, outside the 5 s restart budget.
- The compose side note was checked by reading 18's entrypoint, not by running it. An old volume under the
  new mount shows up as `/var/lib/postgresql/PG_VERSION`, which the entrypoint reports as old data and refuses
  to start on. No bug is expected; DELIVER can confirm with one `docker compose up`.

## Wave: DESIGN / [REF] Changed Assumptions

**1. `--reuse-values` does not move the image.**
DISCUSS, US-01 elevator pitch: "After: run `helm upgrade l8e letpeoplework/lighthouse --reuse-values` → sees
the Postgres pod become Ready on the new major (`SELECT version()` reports 18)".
DISCUSS, AC-1.1: "An install on chart 0.1.17 (Postgres 17) with data is upgraded to the new chart (Postgres 18)
using `helm upgrade --reuse-values` and no other flags."
New assumption: Helm 4.3 with `--reuse-values` keeps the previous chart's default values, so the image stays
`postgres:17` and new keys are unset (measured). No upgrade happens and nothing breaks. The upgrade is
triggered by a plain `helm upgrade` or by `--reset-then-reuse-values`, and both ACs and the docs use those.
`NOTES.txt` prints a line when the image's major is behind the chart's default. The templates must not
assume any new key exists.

**2. The old copy is unchanged in content, not in bytes.**
DISCUSS, AC-1.3: "The previous major's data is still on the PVC, unchanged, after a successful upgrade (D2)."
New assumption: `pg_upgrade` starts and stops the old cluster, and crash recovery replays its WAL first.
Both rewrite files such as `global/pg_control` (measured). The data is the same and 17 opens it, but the bytes
differ. AC-1.3 is checked by opening the old copy with 17 and comparing row counts and the marker, not by
hashing files. The byte-for-byte promise of AC-3.1 still holds, because every refusal happens before any
start.

**3. Removing the old copy has to leave a placeholder.**
DISCUSS, AC-2.3: "`docs/Installation/kubernetes.md` says where the previous major's data sits, how much room
it takes, that a rollback discards what was written since the upgrade, and the one command that removes the
old copy once the operator is satisfied."
New assumption: if the command deletes `pgdata/`, a later rollback to 0.1.17 initialises an empty database
there and Lighthouse starts with no data. The documented command empties `pgdata/` and leaves the file
`UPGRADED-TO-18-see-kubernetes-docs`. 0.1.17 then fails loudly, and the new chart reads the file as "old copy
removed". The docs must also say that a rollback to the previous chart is no longer possible after cleanup.

**4. kind's default storage cannot show a full volume.**
DISCUSS, AC-3.1: "When the PVC lacks room for a second copy of the data, the upgrade does not start. The data
directory is byte-for-byte unchanged, the pod is not Ready, and the log names the free space, the needed
space, and "grow the PVC" (with `persistence.size`) or "pin `postgresql.image` to your current major"."
DISCUSS, slice-02 dogfood: "kind cluster with a deliberately small PVC: upgrade, read the refusal, grow the
PVC, watch it proceed."
New assumption: kind's local-path provisioner does not enforce the requested size, and `df` reports the node's
disk, so a small PVC still looks spacious (inferred from how the provisioner works, not run). The scenario
uses a static PV backed by a size-limited tmpfs inside the kind node, bound to the StatefulSet's claim name.
"Grow the PVC" becomes "enlarge the tmpfs" in the test. On a real cluster it is a PVC resize, which needs a
StorageClass that allows volume expansion; the docs should say so.

## Wave: DEVOPS / [REF] Summary

Platform Architect (Apex). The deployment targets, orchestration, CI, observability posture, deployment
strategy and branching model are fixed by the project and were not re-asked. This wave decides how the
DESIGN is proven in CI, how the "18 only ships with the upgrade" rule is enforced, and how K1–K3 are
measured. Environments are in `environments.yaml` next to this file; K1–K3 are appended to
`docs/product/kpi-contracts.yaml`. Nothing in this wave contradicts DESIGN, so no Changed Assumptions entry
was added.

## Wave: DEVOPS / [REF] Environment Matrix

| Environment | Who runs it | How it is reached | Verified by |
|---|---|---|---|
| Self-hoster's own Kubernetes, bundled Postgres | Operators | `helm upgrade` / `--reset-then-reuse-values` / `helm rollback` | The kind scenarios below; dogfood on local kind with a restored real backup |
| LPW tenants | ArgoCD (rendered manifests) | Git revision bump of the chart | The `gitops-rendered` kind scenario. The platform is torn down, so there is no live tenant to check; the first respin after release is the first real run |
| External database installs | Operators | Any upgrade | Render unchanged (helm-unittest plus the existing standalone gate); nothing new to deploy |
| CI | GitHub Actions `ubuntu-latest` (amd64) + kind | `ci_chart.yml` | `upgrade-path` job |

Every environment, with its preconditions, is listed in `environments.yaml`.

## Wave: DEVOPS / [REF] CI/CD Pipeline

All changes go into the existing `.github/workflows/ci_chart.yml`. No new workflow file is added, because chart
concerns already live in one workflow.

**New job `upgrade-path`.** It uses the same triggers as the workflow: push to `main` on `chart/**` and the
workflow file, `pull_request` on those paths (in practice Renovate's chart PRs), and `workflow_dispatch`. It
runs on pull requests too, because a Renovate PR that moves either Postgres image is exactly the change it
guards.

- `runs-on: ubuntu-latest`, with the same pinned `checkout`, `setup-helm` (v4.3.0) and `kind-action`
  (v1.15.0) steps as `install-smoke`. It has no `needs`, so it runs in parallel with `validate` and
  `install-smoke`.
- `strategy.matrix.group: [happy, refusals]`. Each group gets its own kind cluster, so the two run in parallel
  and a failure names its group.
  - `happy` (in order): install 0.1.17 and seed, upgrade, check counts, restart timing, rollback, write on 17,
    re-upgrade, `--reuse-values` stays on 17 with the NOTES line. Then a second release in another
    namespace goes through `helm template | kubectl apply` with the same assertions.
  - `refusals`: no room and then grown, 16 data, newer data with the placeholder, interruption mid-copy,
    unclean shutdown before the upgrade.
- **The "before" chart** is `docs/charts/lighthouse-0.1.17.tgz`, installed by path. It is committed, and the
  publish job never rewrites published packages, so the job needs no network access to the Helm repo and
  cannot drift if the Pages site is down.
- **The space-refusal volume.** The job runs
  `docker exec <cluster>-control-plane sh -c 'mkdir -p /mnt/small && mount -t tmpfs -o size=<N>m tmpfs /mnt/small'`,
  then applies a static `PersistentVolume`:
  - `hostPath: /mnt/small`, `storageClassName: lh-small`;
  - `claimRef` set to `<namespace>/data-l8e-lighthouse-postgres-0`;
  - capacity equal to `postgresql.persistence.size`.

  It then installs 0.1.17 with `postgresql.persistence.storageClass=lh-small`. N is chosen at run time from the
  seeded size, so the old cluster fits and a second copy does not. `mount -o remount,size=<2N>m` "grows the
  PVC", and the job waits for the next back-off retry to upgrade.
  - The kind node container is privileged, so the mount is expected to work. If it does not, DELIVER falls
    back to the guarded test-only free-space override named in DESIGN's open questions.
- **Images.** Both kind clusters pull `postgres:16`, `postgres:17-trixie`, `postgres:18-trixie` and the 0.1.17
  API image from their registries. The job pre-pulls them on the runner and runs `kind load docker-image`,
  so each image is fetched once per job rather than once per node, which keeps Docker Hub's anonymous pull
  limit out of reach.
- **Budget:** 15 minutes per matrix leg (`timeout-minutes: 20`). `install-smoke` takes about 5 minutes today,
  and the `happy` leg does about three installs' worth of work.
- **Failure diagnostics** follow `install-smoke`: pods, `describe`, and `logs --all-containers`, which shows
  the init containers' lines.

**`validate` job additions** (seconds each):
- `shellcheck` on the scripts inside the new ConfigMap, extracted with `helm template`.
- The template constants in `_helpers.tpl` equal the `values.yaml` defaults.
- The upgrade-source image's major is exactly one below the main image's.

**Release gate (only Postgres 18 ships with the upgrade).** One more check in
`chart/scripts/version-guard.sh`, which already runs in `publish` before anything is packaged:
- Read the default `postgresql.image` major from `values.yaml` and from the latest package in `docs/charts/`
  (`helm show values`).
- If the major moved and the StatefulSet template carries no `pg-upgrade` init container, refuse with
  "default Postgres major moved from 17 to 18 without the in-chart upgrade; pin postgresql.image to postgres:17
  or ship story 6131 first".

This is the cheapest reliable form. It costs a few lines in a script that already reads the same files, runs
exactly when a version is about to be published, and needs no checklist to be remembered. Running it in
`validate` instead would turn every `main` push red until this story lands, because `main` already defaults to
18. `upgrade-path` is added to `publish.needs`, so a release cannot ship when the upgrade itself is red.

**Local parity.** The repo has no chart pre-commit hook, and none is added. DELIVER runs `helm unittest`,
`shellcheck` and the same `chart/tests/upgrade-path/*.sh` scripts against a local kind cluster before pushing.
The scripts take the cluster name as an argument so they run unchanged locally and in CI.

## Wave: DEVOPS / [REF] Monitoring Contracts

| KPI | Measured by | When | Gate |
|---|---|---|---|
| K1: 0 crash-loops reported on the release that moves to 18 | `gh issue list -R LetPeopleWork/Lighthouse --search "incompatible with server" --state all`, plus a manual search of the community Slack for the same phrase | 60 days after the release, checked at 30 and 60 | None automated; the maintainer reads it |
| K2: every upgrade scenario green on every chart change | `gh run list -R LetPeopleWork/Lighthouse --workflow ci_chart.yml --json conclusion,jobs` over the window, counting `upgrade-path` legs that are not `success` on `main` | Continuous; read at DELIVER finalize and at the 60-day K1 check | `upgrade-path` is in `publish.needs`, so a red leg blocks the release |
| K3: under 5 s added to a restart once upgraded | In the `happy` leg: Ready time of the already-upgraded pod after `kubectl delete pod`, against the Ready time of a fresh install with no upgrade due in the same cluster | Every chart change | Hard assertion in the leg (difference under 5000 ms) |

## Wave: DEVOPS / [REF] Deployment Strategy and Rollback Contract

- **Postgres StatefulSet**: one replica, replaced in place by the default `RollingUpdate`. The database is
  down from the moment the old pod stops until the new one is Ready. On an upgrade that includes the copy,
  which scales with the data size (8 s for 76 MB on disk in DESIGN's measurement). Bundled Postgres has never
  been zero-downtime, and this keeps it that way.
- **API Deployment**: unchanged rolling update. Pods lose the database during the window and recover on
  their own when it returns. Nothing in the API changes.
- **Timeouts**: `helm upgrade --wait` defaults to 5 minutes. On a large database it can time out while the
  pod is still copying. The pod finishes regardless, and the docs advise `--timeout 15m` for databases
  above a few GB. ArgoCD keeps reporting `Progressing` until the pod is Ready.
- **Rollback contract**, written first:
  1. `helm rollback <release> <revision on 0.1.17>`, or for ArgoCD, revert the chart revision in Git. Postgres
     starts on 17 from the kept `pgdata/`. Anything written after the upgrade is not there.
  2. Upgrading again redoes the copy from the 17 data. The old 18 copy is detected as out of date and
     replaced.
  3. A rollback between two revisions of the new chart changes nothing on the volume.
  4. After the operator runs the cleanup command, a rollback to 0.1.17 is no longer possible and fails
     loudly. The docs say so next to the command.
  5. Proven by the `happy` leg on every chart change. That is what makes it a tested rollback rather than a
     documented one.

## Wave: DEVOPS / [REF] Mutation Testing Strategy

Stryker per feature does not apply: this story adds no C# and no TypeScript, only templates, shell scripts
and YAML. Nothing equivalent exists for Helm templates or shell. The substitute, stated so it is not a
silent N/A:
- `shellcheck` on every script, in `validate`.
- The refusal scenarios act as the mutation check for the upgrade logic. Each one takes the volume's sorted
  `sha256sum` listing before and after, and asserts it is identical. So a script that wrote anything before
  deciding to refuse fails the job, which is the fault a mutant would plant.
- The interruption and stale-copy scenarios do the same for the two failure modes the marker and the rename
  exist to stop.
- helm-unittest covers the render-time branches: gating, nil-safe fallbacks, and the NOTES line in both
  directions.

## Wave: DEVOPS / [REF] Observability

No new stack. The bundled Postgres has no metrics and no alerting, and this story adds none.
- The only signal is the container log. `pg-upgrade` writes one `lighthouse-postgres:` line per step
  (decision, checks passed, recovery, copy started, copy finished, renamed). A refusal is one line naming the
  reason and the fix.
- The refusal line is also written to the termination message, so `kubectl describe pod` and ArgoCD's
  resource view show it without reading logs.
- `kubectl logs <pod> --all-containers` is the documented way to read the lines.
- Self-hosters who scrape container logs get these lines with no extra setup.

## Wave: DEVOPS / [REF] Branching Strategy

Trunk-based on `main`, as the project already works: focused commits pushed straight to `origin main`, with
no feature branch and no PR. `ci_chart.yml` runs on the push. Renovate's PRs are the only pull requests that
touch the chart, and `upgrade-path` runs on them too. A chart release is the version-bump commit, and it
publishes through the existing `Release`-gated `publish` job.

## Wave: DEVOPS / [REF] Coexistence Matrix

| Existing gate or tool | Effect of this story |
|---|---|
| `helm lint` (default + enterprise values) | Unchanged; must stay green with the new templates |
| `helm unittest` | One new suite; existing suites, including the standalone gate, unchanged |
| Standalone gate step (one Deployment, provider postgres) | Unchanged; the init containers are on the StatefulSet, not a Deployment |
| No-key-generation / no-`lookup` gate | Stays green by construction; the NOTES line compares strings only. The gate scans `templates/*.yaml` and `*.tpl`, so it also covers the new ConfigMap |
| Render determinism gate | Stays green; nothing random or time-based is rendered |
| helm-docs drift gate | The new value needs `chart/README.md` regenerated in the same commit |
| `install-smoke` | Unchanged; a fresh install still runs the init containers' no-op path, so it now proves that path too |
| `publish` + `version-guard.sh` | `needs` gains `upgrade-path`; the guard gains the release-gate check |
| Renovate Postgres group | Gains one rule: the upgrade-source image never gets a major update. Chart value changes stay hand-merged |
| `ci_verifypostgres`, Testcontainers fixture, local dev Postgres | Unaffected; they do not use the chart |
| docker-compose example | Unaffected; it keeps its manual path |

## Wave: DEVOPS / [REF] Usage-Data Event

**N/A**, for three reasons:
- **The API cannot observe the upgrade.** It runs in the database pod's init containers before the
  database, let alone the API, is up. The API cannot read the PVC, so it cannot tell whether its database was
  upgraded, freshly created, or external.
- **No consent path exists there.** The chart has no event pipe of its own, and usage data is opt-in per
  browser and forwarded by the backend. Nothing reaches it from the cluster.
- **The KPIs don't need it.** K1 counts reported crash-loops, and K2–K3 are measured in CI.

The existing deployment-mode property already says an instance runs on `Kubernetes`. Nothing is added to
`UsageDataEventName` or `docs/settings/usagedata.md`.

## Wave: DEVOPS / [REF] Pre-requisites

- `docs/charts/lighthouse-0.1.17.tgz` is in the repo (checked) and stays byte-identical.
- The 0.1.17 API image (`26.9.24.6`) and `postgres:16`, `postgres:17-trixie`, `postgres:18-trixie` are
  pullable. The amd64 and arm64 manifests of the two trixie tags were checked in DESIGN.
- The DESIGN components exist before the `upgrade-path` job can go green. Until then the job is added
  together with the slice-01 code, so it is never red on `main`.
- The release gate in `version-guard.sh` lands in slice 01. It protects any chart release cut before slice
  02 finishes.
- A real Lighthouse backup for the dogfood, restored with `Restore-DbBackup.ps1` into a 0.1.17 kind install.
