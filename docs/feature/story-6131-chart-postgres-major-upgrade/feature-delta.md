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
- **AC-2.5** On the new chart, pinning `postgresql.image` back to the previous major after an upgrade
  starts Postgres on the kept old copy, just as a rollback does, and the database log carries one warning
  line. The line says the newer copy exists, that anything written on it is not in the database now
  running, and that removing the pin redoes the upgrade from this copy, so those writes do not come
  back (the same rule as AC-2.2: once the old major has run, it is the source of truth). A rollback to chart 0.1.17 itself cannot print
  this line, because that chart has no upgrade step; the docs say so (maintainer's choice, 2026-10-02).

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
- **AC-3.5** (Amended in DELIVER, 2026-10-02: a StatefulSet with the default pod policy never replaces a pod that is not
  Ready, and that policy cannot be changed on an existing install. So a fix made by changing chart values, such as
  pinning the image, takes effect only once the stuck pod is deleted, and every refusal line that suggests such a fix
  says so. Growing the volume needs no delete, because the same pod retries by itself.)
  After the operator fixes the cause (grows the PVC, or pins the image and deletes the stuck pod), the next pod start
  proceeds with no other manual step.

### US-04: A volume this chart has already upgraded moves on to the next major the same way
`job_id: job-operator-upgrade-bundled-database-across-major`

(Added 2026-10-02 as slice 03, at the maintainer's request. Retention is the maintainer's choice of the same
day: after a second upgrade, the copy before last is dropped.) As a self-hoster whose bundled database was
already carried from one major to the next by this chart, I want the next chart release that moves the
default image again to carry it across the same way, so the automatic upgrade is not a one-time trick that
leaves me stuck at the second major move.

#### Elevator Pitch
Before: after one automatic upgrade the database runs on `pgdata-18/`, but the upgrade step only ever reads
`pgdata/`. A chart that moves the default to 19 refuses every such volume and tells the operator to set the
image back to 18 and move the data by hand.
After: run `helm upgrade l8e letpeoplework/lighthouse --reset-then-reuse-values` → the `pg-upgrade` log shows
three lines:
- `upgrading the Postgres 18 data in pgdata-18 to Postgres 19 in pgdata-19; pgdata-18 is kept as it is`
- `upgrade finished`
- a line saying the Postgres 17 copy in `pgdata/` was removed, and that a rollback two charts back is no
  longer possible.

The Postgres pod is Ready on the new major, with every row written on either earlier major.
Decision enabled: the operator takes every chart release that moves the Postgres major the same way,
however many have come before. One step of rollback is always kept, and the volume never needs more than
about twice the data once an upgrade has finished.

No image of Postgres 19 exists yet, so each AC is proved one major lower: a Postgres 16 volume is upgraded
to 17 by the chart with `postgresql.image=postgres:17-trixie` and `postgresql.upgrade.image=postgres:16-trixie`,
then to 18 by the chart's defaults. In the ACs, N is the major the database runs on after the first upgrade
(17 in the test), and N+1 is the next chart's default (18 in the test).

#### Acceptance Criteria
- **AC-4.1** A volume holding the kept Postgres N−1 data in `pgdata/` and the live Postgres N copy in
  `pgdata-N/` is upgraded by a plain `helm upgrade` (or `--reset-then-reuse-values`) to a chart defaulting
  to N+1. The upgrade reads `pgdata-N/`, the copy the database actually ran on, and builds `pgdata-(N+1)/`.
  Every row present before the upgrade is there afterwards, including rows written on N after the first
  upgrade. The log names the source and target folders.
- **AC-4.2** Once the new copy is in place, the step removes every copy older than the one it upgraded
  from, so the volume keeps the source and the new copy, about twice the data.
  - `pgdata/` is emptied down to the placeholder file `UPGRADED-TO-<N+1>-see-kubernetes-docs`, written before
    anything is deleted. Any older `pgdata-K/` is removed whole.
  - `pgdata-N/` still opens on N with its rows.
  - The removal starts only after the new copy has been renamed into place and synced to disk. An upgrade
    that fails, is refused or is interrupted removes nothing.
  - The log carries one line naming each removed folder and its major, and saying that a rollback to the
    chart before last (the one defaulting to N−1) is no longer possible, while a rollback to the chart
    defaulting to N still is.
  - The room check is unchanged: it needs room for one new copy of the source, because the older copy is
    still on the volume while the copy is made. When it refuses and older copies exist, the line also names
    the cleanup command as a way out.
- **AC-4.3** After AC-4.1, rollbacks behave as follows:
  - **One chart back:** `helm rollback` to the revision of the chart that defaulted to N starts Postgres N
    on `pgdata-N/`, without what was written on N+1. That chart has the upgrade step, so the pod logs the
    same one-line warning as AC-2.5, naming `pgdata-(N+1)`.
  - **Two charts back, to chart 0.1.17 or any chart without the upgrade step:** Postgres does not start,
    because its `initdb` refuses the non-empty `pgdata/` that holds only the placeholder. It never starts an
    empty database.
  - **Two charts back, to a chart that has the step:** that chart's image major has no copy left, so it is
    refused with the AC-3.3 line: "refusing to start Postgres N−1: the data is Postgres N+1, in
    pgdata-(N+1), which is newer than this image, and no Postgres N−1 copy of it is left to start on; set
    postgresql.image back to Postgres N+1 or remove the pin on it, then run kubectl delete pod …". Nothing
    is changed.
- **AC-4.4** Upgrading again after a one-chart-back rollback redoes the copy from `pgdata-N/`. It never
  reuses the out-of-date `pgdata-(N+1)/`, and a row written on N after the rollback is present on N+1.
  Nothing older than `pgdata-N/` is left, so nothing more is removed.
- **AC-4.5** On the chart defaulting to N+1, pinning `postgresql.image` back to N starts Postgres on
  `pgdata-N/` with the AC-2.5 warning, exactly as after the first upgrade. Pinning back to N−1 is refused
  as in the last bullet of AC-4.3.
- **AC-4.6** A removal cut off part-way (the pod deleted during it) is never mistaken for a database. On the
  next start, the step serves the new copy and finishes the removal. The documented cleanup command uses the
  same removal and the same order: placeholder first, then `PG_VERSION` first in each copy, oldest copy
  first. It removes every copy older than the live one, and can be rerun after being cut off.
- **AC-4.7** A cleaned-up volume (the placeholder plus `pgdata-N/` only) moves on to N+1 by a plain
  `helm upgrade`, the same as AC-4.1. Nothing older than the source exists, so nothing is removed.
- **AC-4.8** The refusals stay correct when the database runs on a `pgdata-K/` copy, and each still leaves
  the volume byte-identical:
  - A live copy two or more majors behind the image is refused, naming that copy's folder and major (in
    the test, a Postgres 16 copy in `pgdata-16/` made from 15, under the 18 chart).
  - A live copy newer than the image, with no copy of the image's major left, is refused naming both
    majors.
  - Data whose newer copies are out of date (the older major ran after they were made) is treated as data
    of the older major. The line never offers an out-of-date copy as the way back.
- **AC-4.9** Restarting the pod after AC-4.1 does not upgrade again, and adds under 5 s to the start (K3),
  the same as after a first upgrade.
- **AC-4.10** `docs/Installation/kubernetes.md`:
  - **Rollback section:** after a second upgrade only one chart back is possible, and what a rollback two
    back does.
  - **Cleanup section:** the automatic removal on a second upgrade, with its log line. The manual command is
    still how to remove the one remaining older copy earlier.
  - **"What it costs":** about twice the data after any upgrade, and briefly room for one more copy while
    an upgrade runs.
  - **Skipped releases:** the automatic upgrade moves one major per chart release. An operator who skipped a
    release that moved the major upgrades to that release first, then to the next. The refusal line for a
    gap of two majors says the same.
- **AC-4.11** The first upgrade on a volume (0.1.17's 17 in `pgdata/` → 18 in `pgdata-18/`, slices 01–02) is
  unchanged. Nothing is older than its source `pgdata/`, so nothing is removed, and a rollback to 0.1.17
  still starts on the pre-upgrade database.

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
| `slice-03-next-major-from-an-upgraded-volume` | US-04 | "The upgrade step can take its source from a copy it made itself, not only from `pgdata/`, and every rollback and refusal still holds". If it fails on the 16 → 17 → 18 chain, the chart must not move its default past 18 until the volume model is rethought. |

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
| 4 | Testable ACs | 17 ACs, each observable via kubectl, helm or the API on kind |
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
| D = M, no newer `pgdata-N` | Nothing to do | `pgdata` |
| D = M, a newer `pgdata-N` present (image pinned back after an upgrade) | Start on the kept copy and log one warning: the newer copy exists, its writes are not in this database, removing the pin redoes the upgrade from this copy (AC-2.5). The newer copy is left in place, so its hash check later marks it stale | `pgdata` |
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

## Wave: DISTILL / [REF] Reconciliation

Reconciliation passed — 0 contradictions. DISCUSS D1–D7, DESIGN DDD-1..16 with its four Changed Assumptions
(already folded into AC-1.1, AC-1.3, AC-1.8, AC-2.3, AC-3.1), and DEVOPS (environments, `upgrade-path` job,
release gate, K1–K3 instruments) agree. Differences that are refinements, not contradictions:

- AC-3.1 names `persistence.size`; DESIGN's refusal line names the full key `postgresql.persistence.size`. The
  scenario asserts the full key.
- DEVOPS puts the unclean-shutdown case in the `refusals` leg, but its behaviour (DDD-7) is part of AC-1.1 and
  slice 01: a 0.1.17 pod usually ends unclean, so the walking skeleton cannot pass without it. Tagged
  `@slice-01`, run in the `refusals` leg as DEVOPS placed it.
- DISCUSS WS Strategy says "no new walking skeleton"; the first real-I/O upgrade scenario plays that role and
  carries `@walking_skeleton @driving_port`.

## Wave: DISTILL / [REF] Scenario List with Tags

SSOT: `chart/tests/acceptance/upgrade-bundled-postgres.feature`. 24 scenarios (two outlines: 2 and 4 rows,
plus the 2-row render outline). Every scenario also carries `@contract-shape:<…>`. All `@pending` except the
render guard marked ✓.

| # | Scenario | Tags | Harness / suite |
|---|---|---|---|
| 1 | Operator upgrades a Lighthouse with data and finds every row on Postgres 18 | `@walking_skeleton @driving_port @US-01 @AC-1.1 @AC-1.2 @kpi:K2 @real-io @env:kind-0.1.17-with-data @slice-01` | `happy` |
| 2 | The other ways of applying the new chart carry the data across the same way (outline: `--reset-then-reuse-values`; rendered manifests `@AC-1.7 @env:gitops-rendered`) | `@US-01 @AC-1.1 @kpi:K2 @real-io @slice-01` | `happy` |
| 3 | The previous major's data stays on the volume with exactly the pre-upgrade rows | `@US-01 @AC-1.3 @real-io @env:kind-0.1.17-with-data @slice-01` | `happy` |
| 4 | An old database that was stopped abruptly is still carried across | `@US-01 @AC-1.1 @edge @real-io @env:kind-0.1.17-sigkilled @slice-01` | `refusals` |
| 5 | Reusing the previous values keeps the database on 17 and says how to move it | `@US-01 @AC-1.8 @edge @real-io @env:reuse-values @slice-01` | `happy` |
| 6 | A fresh install starts an empty Postgres 18 database with no upgrade step | `@US-01 @AC-1.4 @real-io @env:kind-clean-fresh @slice-01` | `happy` |
| 7 | Restarting the database after an upgrade does not upgrade again | `@US-01 @AC-1.5 @kpi:K3 @edge @real-io @env:kind-0.1.17-with-data @slice-01` | `happy` |
| 8 | The upgrade steps exist only where the chart runs the database itself (outline) | `@US-01 @AC-1.6 @in-memory @env:external-db @slice-01` | helm-unittest |
| 9 | An install with an external database renders exactly as before this story | `@US-01 @AC-1.6 @in-memory @env:external-db @slice-01` | one-off diff (see Open checks) |
| 10 | Values from an older chart still render a working upgrade step | `@US-01 @AC-1.8 @edge @in-memory @env:reuse-values @slice-01` | helm-unittest |
| 11 | The install notes warn only when the database image is behind the chart's default (outline, 4 rows) | `@US-01 @AC-1.8 @in-memory @env:reuse-values @slice-01` | helm-unittest |
| 12 ✓ | The database volume, where it is mounted and the health checks stay as they were | `@US-01 @AC-1.6 @AC-2.1 @in-memory @env:kind-0.1.17-with-data @slice-01` | helm-unittest, green now |
| 13 | Rolling back after an upgrade starts Postgres 17 on the pre-upgrade data | `@US-02 @AC-2.1 @kpi:K2 @real-io @env:kind-after-rollback @slice-01` | `happy` |
| 14 | Upgrading again after a rollback starts afresh from the Postgres 17 data | `@US-02 @AC-2.2 @kpi:K2 @edge @real-io @env:kind-after-rollback @slice-01` | `happy` |
| 15 | Rolling back between two releases of the new chart leaves the database as it is | `@US-02 @rollback-contract @edge @real-io @env:kind-0.1.17-with-data @slice-01` | `happy` |
| 16 | The Kubernetes docs say where the old copy is, what it costs and how to remove it | `@US-02 @AC-2.3 @in-memory @slice-02` | docs read (no env: it is prose) |
| 17 | After the old copy is removed, a rollback refuses to start an empty database | `@US-02 @AC-2.4 @AC-2.3 @error @real-io @env:kind-newer-data-no-kept-copy @slice-02` | `refusals` |
| 18 | Too little room for a second copy stops the upgrade before it writes anything | `@US-03 @AC-3.1 @error @real-io @env:kind-size-limited-pv @slice-02` | `refusals` |
| 19 | Once the volume has grown, the refused upgrade goes ahead by itself | `@US-03 @AC-3.5 @AC-3.1 @real-io @env:kind-size-limited-pv @slice-02` | `refusals` |
| 20 | Data two majors behind is refused, naming both majors and the manual path | `@US-03 @AC-3.2 @error @real-io @env:kind-16-data @slice-02` | `refusals` |
| 21 | Pinning the image back to the data's major lets the database start with no other step | `@US-03 @AC-3.5 @AC-3.2 @real-io @env:kind-16-data @slice-02` | `refusals` |
| 22 | Data newer than the image, with no kept copy of the image's major, is refused | `@US-03 @AC-3.3 @error @real-io @env:kind-newer-data-no-kept-copy @slice-02` | `refusals` |
| 23 | An upgrade interrupted part-way is redone from the start and never serves a partial copy | `@US-03 @AC-3.4 @error @real-io @env:kind-interrupted-upgrade @slice-02` | `refusals` |
| 24 | An upgrade-source image whose programs cannot run beside the database image is refused | `@US-03 @D3 @error @real-io @env:kind-0.1.17-with-data @slice-02` | `refusals` |

Error/edge: 12 of 24 (50 %). Every AC of US-01..03 has at least one scenario; AC-1.1 has four. Scenario 24 has no
AC of its own: it is the fourth refusal DDD-6 declares, the one a mirrored or Alpine upgrade-source image hits.
Scenario 15 is the DEVOPS rollback contract's point 3. Every refusal asserts the volume's file fingerprint is
unchanged, which DEVOPS uses as the mutation-testing substitute.

## Wave: DISTILL / [REF] WS Strategy

One walking skeleton, scenario 1, `@walking_skeleton @driving_port`: 0.1.17 with data → plain `helm upgrade` →
same rows on 18 and Lighthouse ready. It runs the real chart, real images and a real PVC on kind; nothing is
faked. Litmus: an operator reading it says "yes, that is the upgrade I want to just work". It cannot be green
at hand-off, because the behaviour it proves does not exist yet; the hand-off commit is still green because
nothing executes `.feature` files and the harness is not wired into CI until slice 01 ships with it.

## Wave: DISTILL / [REF] Test Placement

- Scenarios: `chart/tests/acceptance/upgrade-bundled-postgres.feature`, beside the three existing chart
  features (epic-5306 precedent: Gherkin as SSOT, executed by helm-unittest and shell steps).
- Render checks: `chart/tests/unit/postgres-upgrade_test.yaml`, picked up by the existing
  `helm unittest -f 'tests/unit/*.yaml'` glob. Pending tests use helm-unittest's own `skip: { reason }`
  (verified on 1.1.1; the field predates 1.1); a skipped test may name a template that does not exist yet.
- Kind harness: `chart/tests/upgrade-path/run.sh CLUSTER GROUP [SCENARIO...]`, the path DESIGN's component
  table names. One function per `@real-io` scenario, grouped as DEVOPS's `happy` / `refusals` matrix.

## Wave: DISTILL / [REF] Driving-port Coverage

| Driving port (DESIGN) | Scenarios |
|---|---|
| `helm upgrade` (plain) | 1, 4, 7, 14, 17, 18, 20, 23, 24 |
| `helm upgrade --reset-then-reuse-values` | 2 (row 1) |
| `helm upgrade --reuse-values` | 5 |
| `helm rollback` | 13, 15, 17 |
| `helm template \| kubectl apply` (GitOps) | 2 (row 2) |
| `helm install` (fresh) | 6 |
| Pod restart (deletion, rollout, back-off retry) | 7, 19, 21, 23 |
| Rendered manifests / `NOTES.txt` | 5, 8, 10, 11, 12 |
| `kubectl logs --all-containers` / `describe pod` (observable surface) | 6, 7, 15, 17, 18, 20, 22, 23, 24 |

No uncovered entry point.

## Wave: DISTILL / [REF] Adapter Coverage

| Driven adapter | `@real-io` scenario | Covered by |
|---|---|---|
| Data volume, local-path PVC | YES | 1–7, 13–17, 20–24 |
| Data volume, size-enforced (tmpfs static PV) | YES | 18, 19 |
| Old-major binaries (emptyDir from the upgrade-source image) | YES | every upgrade; 24 for a foreign image |
| Decision hand-off (emptyDir file → wrapper) | YES | 1 (serves 18 from the new copy), 13 (17 from the old), 7 (no re-upgrade) |
| Operator feedback (log + termination message) | YES | 18 asserts both; 17, 20, 22, 24 the log |
| Official image entrypoint, unchanged | YES | 6 (fresh install initialises as today) |
| Image registry pull (`postgres:16`, `17-trixie`, `18-trixie`, API image) | YES | pre-pulled on the runner, `kind load docker-image`; a cold-node pull is outside the 5 s budget by design |
| Helm CLI / kubectl | YES | every `@real-io` scenario |

No "NO — MISSING" rows.

## Wave: DISTILL / [REF] Scaffolds

- `chart/tests/upgrade-path/run.sh` — `# SCAFFOLD: true`; 19 scenario functions, each `fail`s with
  `SCAFFOLD: not yet implemented — <scenario title>`. Shellcheck-clean, `[[ ]]` throughout (Sonar
  `shelldre:S7688`). Not referenced by `ci_chart.yml`.
- `chart/tests/unit/postgres-upgrade_test.yaml` — 12 tests skipped with a `pending story 6131 slice 0N: …`
  reason, 3 regression guards running.
- No production template, value or CI job was touched. Language/policy notes: this is a Helm/bash
  deliverable, so the Python-pilot artifacts (state-delta port, PBT, Mandate-12 domain types) do not apply, as
  `docs/architecture/atdd-infrastructure-policy.md` already records for this repo. Two rows were appended to
  that policy (Helm CLI driving port; bundled Postgres volume as a real driven port).

## Wave: DISTILL / [REF] Pre-requisites

- DESIGN driving ports and decision table as above; ADR-213.
- DEVOPS `environments.yaml`: every `@env:` tag names one of its environments. The docs scenario (16) has none,
  because it reads prose.
- `docs/charts/lighthouse-0.1.17.tgz` present and byte-identical; images pullable; Helm v4.3.0;
  helm-unittest v1.1.2 in CI.

## Wave: DISTILL / [REF] Completeness Audit

13 of 15 → COMPLETE. Fails: C1b (the space threshold is not pinned to the byte — a tmpfs inside a kind node
cannot be sized that finely; the major partitions 16/17/18 and newer-than-image are covered), C6c (no scenario
asserts that only the four declared refusal lines can appear; each refusal asserts the volume untouched, which
is the property that matters). C7c is N/A (single-replica StatefulSet, no concurrency claim). No
SPECIFICATION_AMBIGUITY blocker; the one open behavioural question is non-blocking (Upstream findings, 1).

## Wave: DISTILL / [REF] Upstream Findings

1. **Pinning the image back after an upgrade silently drops post-upgrade writes.** The decision table's
   "D = M (also covers an image pinned back to the old major)" means a new-chart install with
   `postgresql.image=postgres:17-trixie`, before cleanup, starts on the kept 17 copy with no log line, and
   everything written on 18 disappears — a rollback the operator did not ask for. No AC covers it. Question for
   the maintainer (DESIGN): log one line naming the newer copy it is ignoring, refuse, or accept and document?
   Not blocking: no scenario depends on the answer.
2. **AC-1.5 / K3 as written would flake.** Both `readinessProbe`s run every 10 s with no initial delay, so
   time-to-Ready is quantised to the probe period and a 5 s difference is noise. Scenario 7 measures the time
   from pod creation to the `postgres` container starting (`containerStatuses[].state.running.startedAt` minus
   `metadata.creationTimestamp`), which is exactly the cost this story adds, and then asserts Ready separately.
3. **AC-1.5 cost source.** `pg-old-binaries` copies about 49 MB on every start while `pgdata/` holds 17 data,
   which is every restart after an upgrade until the operator cleans up. Expected to stay inside 5 s; scenario 7
   is where it shows if not.
4. **AC-3.4 depends on catching the copy in flight.** The harness must prove the delete landed mid-copy, not
   after it: wait for the "copy started" line, delete, then require the next start's log to report a discarded
   partial copy. Without that proof the scenario can pass on a completed upgrade. 300 MB gives roughly 30 s of
   window at DESIGN's measured rate.
5. **AC-1.6 "renders exactly as before" has no permanent golden.** Every render carries
   `helm.sh/chart: lighthouse-<version>`, so a stored snapshot breaks on each release bump. The permanent guard
   is scenario 8 (no upgrade machinery when disabled) plus the existing suites; scenario 9 is a one-off diff in
   DELIVER (Open checks).
6. **AC-2.3 is prose.** It becomes executable through scenario 17, which runs the cleanup command copied
   verbatim from the docs. DELIVER needs the command in the docs in a form the harness can extract.
7. **K1 is manual** (issues and Slack, 30 and 60 days after release) and has no scenario.
8. **Not tested, by decision:** arm64 (runners are amd64); `ALTER SYSTEM` settings (the chart sets none —
   documented, not asserted, per DESIGN's open question); GitOps rollback by Git revert (only `helm rollback` is
   exercised; the rendered path shares the same pod behaviour).

## Wave: DISTILL / [REF] Open Checks for DELIVER

- Wire `chart/tests/upgrade-path/run.sh` into the `upgrade-path` job with the slice-01 code, never before, so
  `main` is never red; add it to `publish.needs`.
- Remove each `@pending` tag and each helm-unittest `skip` in the commit that makes it green; unskip the three
  "notes stay quiet" tests together with the two positive NOTES tests — alone they pass vacuously.
- Update `chart/tests/unit/render_test.yaml`'s `postgres:18` assertion to `postgres:18-trixie` with `values.yaml`.
- NOTES wording is DELIVER's, but the tests require the word "behind" and `--reset-then-reuse-values` in it.
- Scenario 9: once, diff `helm template` with `postgresql.enabled=false` at `991edd7e9` against the slice-01
  commit (excluding the `helm.sh/chart` label) and record the empty diff here.
  - **Result (DELIVER step 01-06, 2026-10-02): empty diff.** The chart at `991edd7e9` (extracted with
    `git archive 991edd7e9 chart`) and the chart at the step 01-06 commit, each rendered with
    `helm template l8e <chart> --set postgresql.enabled=false --set externalDatabase.host=my-pg.example
    --set externalDatabase.database=lh --set externalDatabase.user=lh --set externalDatabase.password=extpass
    --set encryption.key=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=`, give 226 lines each, and `diff` exits 0
    with no output. Not even the `helm.sh/chart` label differs, because `Chart.yaml`'s `version` is still
    0.1.17 (the bump is the release's job). One-off check, not a permanent golden.
- Confirm the tmpfs static PV works under `helm/kind-action` first in slice 02; if not, the free-space override
  fallback needs a guard that stops it shipping.
- First walking-skeleton run against today's chart should fail on `database files are incompatible with
  server`; record that as its genuine RED (see `red-classification.md`).
- Shell in the new ConfigMap and harness: `[[ ]]` only; run shellcheck locally
  (`docker run --rm -v "$PWD:/mnt:ro" koalaman/shellcheck:stable …`) before pushing.
- Answer Upstream finding 1 before slice 02 closes, since it decides whether a further refusal or log line exists.

## Wave: DISTILL / [REF] Final Wave Review Gate

| Reviewer | Wave | Verdict | Disposition |
|---|---|---|---|
| Product owner | DISCUSS | approved, 0 findings | The DoR table said 15 ACs; corrected to 17 |
| Solution architect | DESIGN | approved, 0 findings | Its claim that pinning the image back is loss-free is wrong; see the decision for the maintainer below |
| Platform architect | DEVOPS | rejected, 3 blockers | All three are implementation not written yet (the `upgrade-path` job, `publish.needs`, the version-guard check). By the reviewer's own account they belong to slice 01, so they are DELIVER action items, not design defects |
| Acceptance designer | DISTILL | approved, 0 findings | AC coverage and the interruption scenario were spot-checked by hand, and both hold |

DELIVER action items carried from the review: wire `upgrade-path` into `ci_chart.yml` and `publish.needs`; add the
Postgres-major check to `version-guard.sh`; unskip every pending test in `postgres-upgrade_test.yaml`; check K2's
`gh run` query against the first real run; add the Renovate rule that keeps the upgrade-source image to minor updates.

Decisions for the maintainer before DELIVER:

1. **Pinning the image back on the new chart.** DECIDED 2026-10-02: start on the 17 copy with one warning line
   (AC-2.5, decision table). Original question: After an upgrade, `postgresql.image: postgres:17-trixie` on the new
   chart starts Postgres on the kept 17 copy, the same as a rollback, and everything written on 18 is not in it. No
   log line says so today. The choices are to start on the 17 copy with one warning line, to refuse while a current
   18 copy exists (the operator then removes `pgdata-18` deliberately), or to document it only.
2. **The release gate before slice 01 lands.** The version-guard check that enforces "Postgres 18 ships only with
   the upgrade" arrives with slice 01. Until then nothing stops a chart release cut from `main` defaulting to 18. It
   can land on its own now, because it only runs when a version is published and leaves `main` green, or the next
   `/release` re-pins `postgres:17` by hand.

## Wave: DELIVER / [REF] Demo Evidence

Dogfood on a throwaway kind cluster, 2026-10-02, driven by the harness helpers in
`chart/tests/upgrade-path/run.sh`, with the Lighthouse demo data rather than seeded rows. Chart 0.1.17 was
installed from `docs/charts/lighthouse-0.1.17.tgz`, and all nine demo scenarios were loaded through
`POST /api/latest/demo/scenarios/{id}/load` and left to refresh for five minutes. That gave 44 tables and
20,467 rows. The API was scaled to 0 around every comparison, so nothing wrote between two readings.

| Step | Result |
|---|---|
| Plain `helm upgrade` to the new chart | Ready on Postgres 18; every table's row count identical; teams and portfolios served unchanged by `/api/latest/teams` and `/api/latest/portfolios` |
| A row written on 18, then `helm rollback` to the 0.1.17 revision | Ready on Postgres 17 with the pre-upgrade row counts; the row written on 18 is gone, as the docs say |
| Plain `helm upgrade` again | Ready on 18; log: `lighthouse-postgres: pgdata-18 is out of date: Postgres 17 has run on pgdata since that copy was made, so it is discarded and the upgrade redone from pgdata`, then `upgrade finished: Postgres 18 starts on pgdata-18, and the Postgres 17 data stays in pgdata` |

## Wave: DELIVER / [WHY] Upstream Issues

- AC-3.5 was amended during step 02-03. A StatefulSet with the default `OrderedReady` pod policy never
  replaces a pod that is not Ready, and the policy cannot be changed on an existing install. So a fix made by
  changing chart values after a refusal reaches the database only once the stuck pod is deleted. Every refusal
  line that suggests such a fix now ends with the exact `kubectl delete pod -n <namespace> <pod>` command, and
  the docs say it once under "When an upgrade is refused". Growing the volume needs no delete. Recreating the
  StatefulSet with the `Parallel` policy was rejected, because it would itself be a disruptive migration of
  every install. This was decided autonomously while the maintainer was away; it is flagged for review.
- After the documented cleanup command, the volume holds only the placeholder and `pgdata-18`. Step 02-02
  found that this state had no row of its own and was refused on the next restart; step 02-03 added the row.

## Wave: DESIGN / [REF] Slice 03 — next major from an upgraded volume

Platform Architect, PROPOSE. Scope: `chart/files/postgres-upgrade/*`, the docs rollback and cleanup
sections, the harness, and ADR-213's known-limitation section. No template, value or CI gate changes
shape. Retention is the maintainer's choice (2026-10-02): after a successful upgrade, every copy older than
the source is removed.

### Today's gap, as the code stands at `93b7efe7c`

- `upgrade.sh` takes the data major from `pgdata/PG_VERSION` only (`data_major`), and hashes only
  `pgdata/global/pg_control` (`pg_control_hash`).
- On a volume holding `pgdata/` (16) and a live `pgdata-17/`, a chart defaulting to 18 lands in
  `data_out_of_reach`. `refuse_other_major` then refuses and says to set the image back to 17.
- `copy-old-binaries.sh` hands over its programs only when `pgdata/PG_VERSION` is its own major, so the 17
  programs would never be handed over for that volume either.

### Volume model: the live copy

The volume is read as a chain. Every copy except the first was made from the one just below it by an
upgrade.

- **Copies.** `pgdata/` and every directory named exactly `pgdata-<K>`. A `.partial` or `.stale` directory is
  never a copy.
  - A copy counts only if its `PG_VERSION` is present (for `pgdata-K`, it must read `K`).
  - Every removal deletes `PG_VERSION` first, so a copy that is half-removed already does not count.
- **Base.** `pgdata/` if it counts. Otherwise, when `pgdata/` holds the placeholder, the lowest-major
  `pgdata-K` that counts. A volume with copies but neither a base nor a placeholder is refused, as today's
  catch-all row does.
- **Chain step.** From copy C (major c), the next link is `pgdata-(c+1)`, provided:
  - it counts;
  - it has `.lighthouse-upgrade`, and the note's `source_major` is `c`;
  - the note's `source_pg_control_sha256` equals the hash of `C/global/pg_control`, computed and non-empty.

  When `pgdata-(c+1)` counts but `C`'s `pg_control` cannot be read, the volume is refused. This generalises
  today's `refuse_unreadable_old_data`: whether the newer copy is current cannot be told.
- **Live copy L** is the last link of the chain. It is the copy the database last ran on: any start of an
  older link rewrites that link's `pg_control`, which cuts the chain there.
- **Out-of-date copies** are copies that count but are not on the chain. They are never started, never
  offered as a way back, and are replaced when an upgrade rebuilds that major.

The note format does not change. The source folder is the copy holding `source_major`, and each major lives
in at most one folder, because `pgdata-K` is only ever made by an upgrade into K. So every note written by
slices 01–02, and every 0.1.17 volume (a chain of one), reads correctly with no migration.

The **source hash** in a new note is the hash of the `pg_control` of the copy the upgrade read from (`L`),
taken after `pg_upgrade` has finished with it. `write_upgrade_note` hashes `L` instead of `pgdata/`.

### Retention: the copy before last goes once the new copy is in place

**Rule.** After a successful upgrade from `L` (major `Lm`) into `pgdata-M`, every copy older than `L` is
removed. "Successful" means after the rename of `pgdata-M.partial` and the `sync` that follows it. What
stays is the source and the new copy, about twice the data.

**The first upgrade is unchanged.** On a slice 01–02 volume, or 0.1.17's 17 in `pgdata/` going to 18, the
source is `pgdata/` and nothing is older than it. So nothing is removed, and a rollback to 0.1.17 still
starts on the pre-upgrade database (AC-4.11).

**One implementation.** The removal is `remove_copies_older_than <major>` in `volume.sh`. It is the same
function the documented cleanup (`remove-old-copies.sh`) calls with the live major. The order is fixed:
1. When `pgdata/` is among the copies to remove and holds no placeholder yet, write the placeholder
   `UPGRADED-TO-<live major>-see-kubernetes-docs` and sync it.
2. Then, oldest copy first: delete `PG_VERSION`, sync, delete the rest. `pgdata/` is emptied down to the
   placeholder; a `pgdata-K` folder goes entirely.
3. Then one log line, for example: `lighthouse-postgres: removed pgdata (Postgres 16), the copy before the
   one this upgrade read from; a rollback to a chart on Postgres 16 is no longer possible, a rollback to
   the chart on Postgres 17 still is`.

**Cut-off removal.** Every intermediate state is one the decision table handles:
- The placeholder is written but `pgdata/PG_VERSION` is still there: `pgdata/` still counts. The chain runs
  16 → 17 → 18, the live copy is 18, the pod starts on `pgdata-18`, and the finishing step (below) removes
  `pgdata/`.
- `PG_VERSION` is gone and other files remain: `pgdata/` no longer counts. The base is the lowest counting
  `pgdata-K`, and the finishing step clears the rest.
- An older `pgdata-K` is half-removed: it does not count, and the finishing step removes it.

**Finishing step.** In the "nothing to do, `Lm` = M" row, and only when `pgdata/` holds a placeholder, the
step calls `remove_copies_older_than (Lm − 1)` before starting. It removes leftovers below the copy the live
one was made from: `pgdata/` contents other than the placeholder, and any `pgdata-K` folder (counting or
not) with K below `Lm − 1`.
- On every other volume it finds nothing.
- A first-upgrade volume has no placeholder, so it never runs there and its `pgdata/` is never touched.
- It never runs on a refusal or pin-back row, so a refusal still changes nothing.

**Room check: remove after the copy, not before (chosen).**
- The check stays as slice 01 built it: free space must hold one new copy of the source (`du` of `L` ×
  1.1 + 64 MiB). The older copy is already on the volume, so nothing more is needed.
- At the moment the copy finishes the volume holds three copies, briefly, and then drops back to two.
- Removing the oldest copy first would cap that peak at two copies. But if the upgrade then failed, was
  refused by `pg_upgrade`, or was interrupted, the operator would have lost the older rollback for an
  upgrade that never happened. It would also break the promise that a refused upgrade changes nothing.
- An operator whose volume cannot hold the third copy for that moment gets the room refusal. When older
  copies exist, that line also names `remove-old-copies.sh`, so freeing the space early is the operator's
  explicit choice.

### Generalised decision table (`pg-upgrade`)

M is the image's `PG_MAJOR`. L is the live copy (folder `Ld`, major `Lm`). The base is computed as above.

| Volume state | Action | `PGDATA` |
|---|---|---|
| No copy, no placeholder, no `pgdata-*` | Fresh install | `pgdata` |
| Placeholder and no copy counts | Refuse, naming what was found (unchanged) | none |
| Copies but no base | Refuse (unchanged catch-all) | none |
| A link of the chain cannot be hashed while a newer copy counts | Refuse: removal cut off, or a damaged copy; points to the cut-off section | none |
| `Lm` = M, no copy newer than M | Nothing to do; finishing step when a placeholder exists | `Ld` |
| `Lm` = M, a copy newer than M exists (out of date after a pin or rollback) | Start with the AC-2.5 warning naming the newest such copy; finishing step when a placeholder exists | `Ld` |
| `Lm` > M, the chain holds a copy of M (pinned back or rolled back one chart, first start) | Start on the chain's M copy with the AC-2.5 warning naming `Ld`. Nothing is removed. That start cuts the chain, so the next start falls in the row above | the M copy |
| `Lm` = M−1, `pgdata-M` absent | Upgrade from `Ld` into `pgdata-M`, then `remove_copies_older_than Lm` | `pgdata-M` |
| `Lm` = M−1, `pgdata-M` counts (out of date, since it is off the chain) | Set it aside as `.stale`, upgrade from `Ld`, remove the stale copy once the new one is in place (unchanged mechanism), then `remove_copies_older_than Lm` | `pgdata-M` |
| `Lm` ≤ M−2 | Refuse, naming `Ld` and `Lm`. If out-of-date newer copies exist, say they are out of date because Postgres `Lm` ran after they were made. Point to "one major per chart release" and the manual path | none |
| `Lm` > M, no copy of M on the chain (two charts back after a second upgrade, or pinned to a removed major) | Refuse with the AC-3.3 line, naming `Lm` in `Ld` and M | none |

Rows that no longer exist on their own: "already upgraded" is now the first of the `Lm` = M rows; "old copy
removed" is now base selection.

`settle_set_aside_copy` is unchanged and runs first. It concerns only `pgdata-M.stale`, and a `.stale` or
`.partial` of another major is ignored until an image of that major runs.

Everything `upgrade()` does to the source is done to `Ld`: the room check (`du` of `Ld`), recovery and
settings read, `pg_upgrade -d Ld`, the copy of `pg_hba.conf` and `pg_ident.conf`, and the note's hash and
system identifier. Log lines name the folders, for example
`upgrading the Postgres 17 data in pgdata-17 to Postgres 18 in pgdata-18; pgdata-17 is kept as it is`.

**Rollback two charts back** after a second upgrade (volume: placeholder, `pgdata-N`, `pgdata-(N+1)`):
- To chart 0.1.17, or any chart without the step: the image's entrypoint runs `initdb` on `pgdata/`, which
  refuses a non-empty folder. The pod crash-loops loudly and never starts an empty database.
- To a chart with the step: the last table row applies, so it is refused with the AC-3.3 line and nothing
  is changed.

### One implementation of the chain, used by three scripts

`chart/files/postgres-upgrade/volume.sh` (CREATE NEW, sourced, no side effects of its own) holds `copies`,
`base`, `live_copy`, `copy_for_major` and `remove_copies_older_than`. Three scripts use it:

- `upgrade.sh` (EXTEND): decides, upgrades, and does the post-commit removal and the finishing step.
- `copy-old-binaries.sh` (EXTEND): hands over its programs when the **live copy's** major is its own
  `PG_MAJOR`. Today's rule looks only at `pgdata/`, which misses `pgdata-17` and also copies about 49 MB on
  every restart while the kept 17 copy is in `pgdata/`. Under the new rule an already-upgraded volume copies
  nothing on restart. It runs as the `postgres` user in the upgrade-source image, which can read every copy
  and its `pg_control`. It sees the volume before `settle_set_aside_copy` runs, and a `.stale` is never a
  copy, so the binaries decision comes out the same.
- `remove-old-copies.sh` (CREATE NEW): the documented cleanup.
  - It finds the live copy and calls `remove_copies_older_than <live major>`.
  - It refuses (exit 1, nothing removed) when the live copy is `pgdata/`, or when `postmaster.pid` exists in
    a copy it would remove.
  - The docs command becomes
    `kubectl exec l8e-lighthouse-postgres-0 -c postgres -- bash /lighthouse-postgres/remove-old-copies.sh`.
  - The cut-off fallback pod also mounts the scripts ConfigMap.

  After a second upgrade the automatic removal has already left one older copy (the source), so the manual
  command only matters for removing that one early, or after a first upgrade.

### `start.sh`, `PGDATA` and the templates

- `start.sh`, the StatefulSet, the values, the schema, `NOTES.txt` and `_helpers.tpl` are unchanged.
  `PGDATA` can now be any `pgdata-K`, which the wrapper already handles.
- The ConfigMap gains two keys (`volume.sh`, `remove-old-copies.sh`). The existing shellcheck step covers
  them.
- The docs' `SHOW data_directory` advice already covers a live `pgdata-K`.

### Publish guard and the known limitation

- `version-guard.sh` rule 4 is unchanged and still right. A release moves the default by exactly one major
  above the last published chart, and the upgrade-source default is the last published major. That is the
  only step the upgrade makes, now from any live copy.
- ADR-213's known-limitation section is rewritten as "resolved once slice 03 is delivered". It also records
  the retention rule.
- What remains is D4's rule, not a limitation of the volume model: one major per chart release. An operator
  who skipped a release that moved the major steps through it, and the gap refusal says so.

### Docs to change in DELIVER (`docs/Installation/kubernetes.md`)

- **"Where the old copy is and what it costs":** about twice the data after any upgrade, briefly room for
  one more copy while an upgrade runs.
- **"Rolling back":** one chart back always works. Two charts back after a second upgrade fails loudly (on
  0.1.17) or is refused (on a chart with the step), with both lines quoted.
- **"Removing the old copy":** the automatic removal on a second upgrade and its log line, and the
  `remove-old-copies.sh` command.
- **"When removing the old copy was cut off":** the next start finishes it.
- **"When an upgrade is refused":** the gap line now says to step through each chart release that moved the
  major.

### Test plan (kind, `upgrade-path` harness)

**Chain fixture.** These are real clusters and real images, using today's scripts for the first step:
1. `helm install` chart 0.1.17 (`docs/charts/lighthouse-0.1.17.tgz`) with `postgresql.image=postgres:16-trixie`.
   Seed the marker row and record the row counts.
2. `helm upgrade ./chart --set postgresql.image=postgres:17-trixie --set postgresql.upgrade.image=postgres:16-trixie`.
   The current step upgrades 16 in `pgdata/` into `pgdata-17/`. Nothing is removed (first upgrade,
   AC-4.11). Write row R17.
3. `helm upgrade ./chart --reset-values` with the chart's defaults:
   - **RED today:** refused with "the database runs on Postgres 17 in pgdata-17". This is the genuine RED
     for `red-classification.md`.
   - **GREEN after slice 03:** upgrade from `pgdata-17` into `pgdata-18`. Marker, counts and R17 are present.
     `pgdata/` holds only `UPGRADED-TO-18-see-kubernetes-docs`, `pgdata-17` still opens on 17, and the
     removal log line is present (AC-4.1, 4.2).

**Scenarios on the fixture:**
- Restart: no second upgrade, no removal, `pg-old-binaries` copies nothing, start-time difference under 5 s
  (AC-4.9).
- `helm rollback` to the revision from step 2: Postgres 17 on `pgdata-17` with the warning naming
  `pgdata-18`, and the row written on 18 is absent. Write R17b, then upgrade again: redone from `pgdata-17`,
  R17b present, nothing removed (AC-4.3 first bullet, AC-4.4).
- Rollback to the revision from step 1 (0.1.17 on 16): the pod does not become Ready, its log shows
  `initdb` refusing the non-empty `pgdata`, and no database is created (AC-4.3 second bullet).
- Pin `postgresql.image=postgres:16-trixie` on the chart: refused with the newer-than-image line naming 18
  and 16, volume byte-identical (AC-4.3 third bullet, AC-4.5).
- Pin `postgresql.image=postgres:17-trixie` (upgrade source 16-trixie): starts on `pgdata-17` with the
  warning (AC-4.5).
- **Interruption during the removal:**
  - Seed `pgdata/` large enough (about 300 MB), and delete the pod once the removal has started (the
    harness waits for the placeholder file to appear).
  - The next start serves `pgdata-18` and its log reports the finishing removal. End state: placeholder,
    `pgdata-17`, `pgdata-18` (AC-4.6).
- **Interruption during the copy** of the second upgrade: nothing is removed, and the next start redoes it
  (AC-4.2, removal only after commit).
- `remove-old-copies.sh` via the command extracted verbatim from the docs: only the placeholder and
  `pgdata-18` remain. Then a plain upgrade path from a cleaned-up first-upgrade volume (step 2 plus cleanup,
  then the defaults) removes nothing (AC-4.7).

**Gap from a copy (AC-4.8, first bullet):**
- 0.1.17 with `postgres:15-bookworm`, then the chart with `postgres:16-bookworm` and upgrade source
  `15-bookworm`, then the chart's defaults.
- Refused naming `pgdata-16` and 16, volume byte-identical. The bookworm pair is needed only because
  `postgres:15-trixie` does not exist. The refusal happens before the operating-system check.

**Out-of-date newer copy (AC-4.8, third bullet):**
- Use a first-upgrade chain (step 2), roll back to 0.1.17 so that 16 runs, then upgrade to the defaults.
- Refused as two majors behind, naming `pgdata` and 16, with the line saying `pgdata-17` is out of date;
  volume byte-identical.

**The override does not trip any check.** I checked this by reading the code; nothing was run.
- The "Postgres defaults agree" step in `ci_chart.yml` reads `values.yaml` and `_helpers.tpl` as files.
  It never renders, so `--set` overrides cannot reach it.
- No template `fail`s on the image major.
- `version-guard.sh` only runs in `publish`.
- The one visible effect is the `NOTES.txt` "behind" line for 17 < 18, which is correct.

**New harness parts:** a `chain` group, and three environments for DEVOPS to add to `environments.yaml`:
`kind-chain-16-17`, `kind-chain-cleaned`, `kind-gap-from-copy`. helm-unittest: the ConfigMap carries
`volume.sh` and `remove-old-copies.sh`.

### Changed Assumptions (slice 03)

**S3-1. The upgrade source is the live copy, not `pgdata/`.**
ADR-213, Known limitation (before this wave): "The upgrade only ever reads the data in `pgdata/`, the
folder the volume was first set up with." DESIGN, Decision Table: "M is the main image's `PG_MAJOR`. D is
the data major in `pgdata/PG_VERSION`."
New assumption: the source is the live copy, the end of the chain of notes. `pgdata/` is only the chain's
base.

**S3-2. The note's source hash belongs to the copy the upgrade read from.**
DESIGN, On-Volume Layout: "`.lighthouse-upgrade`              source major, system identifier, sha256 of
pgdata/global/pg_control".
New assumption: the hash is of `<source copy>/global/pg_control`, which is `pgdata/` only for a first
upgrade. The format is unchanged, so existing notes stay valid.

**S3-3. Old copies are removed automatically, but only the ones older than the source.**
DISCUSS, Out of Scope: "Removing the previous major's copy automatically. The operator removes it with the
documented command." ADR-213 Consequences: "The volume holds about twice the data until the operator
removes the old copy."
New assumption (maintainer, 2026-10-02):
- The previous major's copy, the one a one-chart-back rollback needs, is still never removed
  automatically.
- What is removed automatically is everything older than it, after a successful second (or later)
  upgrade. The volume therefore holds about twice the data after any upgrade.
- A rollback two charts back is no longer possible after a second upgrade; it fails loudly or is refused.
- The first upgrade removes nothing.

**S3-4. The cleanup removes every copy older than the live one, through the same function.**
AC-2.3: "That command empties the old folder but leaves a placeholder file in it". DESIGN DDD-10: "The
documented cleanup empties `pgdata/` and leaves one placeholder file in it".
New assumption: `remove-old-copies.sh` and the automatic removal both call `remove_copies_older_than`. The
order is the placeholder first, then `PG_VERSION` first in each copy, oldest copy first. On a
first-upgrade volume the result is exactly today's.

**S3-5. Pinning back starts on the copy of the pinned major, wherever it is.**
AC-2.5: "pinning `postgresql.image` back to the previous major after an upgrade starts Postgres on the kept
old copy, just as a rollback does".
New assumption: on the copy of the pinned major on the chain, `pgdata/` or `pgdata-K`. When that copy was
removed by a second upgrade, the pin is refused.

**S3-6. Old programs are handed over only when the live copy needs them.**
DISTILL upstream finding 3: "`pg-old-binaries` copies about 49 MB on every start while `pgdata/` holds 17
data, which is every restart after an upgrade until the operator cleans up."
New assumption: it copies only when the live copy is the upgrade source's major.

**S3-7. Room is measured on the copy being upgraded; the peak is briefly three copies.**
DESIGN DDD-9: "Space needed = `du` of `pgdata` × 1.1 + 64 MiB". Docs: "the volume holds roughly twice the
data until you remove it".
New assumption:
- `du` of the live copy.
- During a second upgrade the volume briefly holds three copies, because the oldest goes only after the
  new copy is committed; it then drops back to two.
- The room refusal names the cleanup as a way out when older copies exist.

### Open questions

- Whether `pg_upgrade` from an already-upgraded cluster behaves the same is not measured. It should: the
  source is an ordinary cluster. The chain fixture is the proof.
- The finishing step only runs when a placeholder exists. A manual cleanup cut off after the placeholder,
  on a first-upgrade volume, is therefore left for the operator to rerun, as the docs already say.
- Run time: the chain fixture costs about three installs' worth of work; DEVOPS decides whether it is a
  third matrix leg.

## Wave: DEVOPS / [REF] Slice 03 — next major from an upgraded volume

Platform Architect (Apex). The fixed decisions are unchanged: GitHub Actions, trunk-based on `main`, no new
observability, usage data N/A. The DEVOPS answers above hold for slice 03 as written. This section covers
only what slice 03 adds. Nothing in it contradicts the slice 03 DESIGN.

### A third matrix leg: `chain`

`upgrade-path` becomes `matrix.group: [happy, refusals, chain]`. Each leg gets its own kind cluster, as today
(`upgrade-path-${{ matrix.group }}`), and `run.sh` gains `chain` as a third `GROUP`, with its own `CHAIN=(…)`
list and `namespaces_of` rows.

Why a leg of its own rather than more scenarios in `refusals`:
- `refusals` already ran 591 s locally after the review fixes. A GitHub-hosted runner is usually slower
  than a developer machine, so that leg alone may already sit near its 20-minute timeout on CI.
- The chain fixture repeats three installs' worth of work (0.1.17 on 16, upgrade to 17, upgrade to 18). On
  top of that come two rollbacks, a re-upgrade, an interrupted removal, a cleanup, and the 15 → 16 gap
  fixture.
- Added to either existing leg, that would push it past the budget.
- A leg of its own runs in parallel, so the job's wall-clock time grows only to the slowest leg, and a red
  chain names itself.

**Scenarios in `chain`**, in the order they build on each other:
1. Chain fixture: 0.1.17 on `postgres:16-trixie`, then the chart with `postgres:17-trixie` and upgrade
   source `16-trixie`, then the chart's defaults.
2. AC-4.1/4.2: rows, removal line, placeholder, `pgdata-17` kept.
3. AC-4.9: restart, against a fresh-install baseline in the same cluster.
4. AC-4.3 one chart back, then AC-4.4 re-upgrade.
5. AC-4.3 two charts back, to 0.1.17: fails loudly.
6. AC-4.3 and AC-4.5 pins to 16 (refused) and 17 (warns).
7. AC-4.6 interrupted removal.
8. AC-4.2 interrupted copy removes nothing.
9. AC-4.6/4.7 cleanup and a cleaned-up volume moving on.
10. AC-4.8 out-of-date newer copies, then the gap from a copy (15-bookworm → 16-bookworm, then the
    defaults).

**Budget:** `timeout-minutes: 20` per leg, unchanged. Target for `chain`: at most 12 minutes locally, to leave
the same margin `happy` has. Measure the first CI run of each leg in DELIVER. If `refusals` or `chain` comes
within 3 minutes of the timeout on CI, split it again (for example `chain` and `chain-refusals`) rather than
raising the timeout. A longer timeout only hides a slower suite.

**Images.** `preload_images` gains `postgres:16-trixie`, `postgres:15-bookworm` and `postgres:16-bookworm`.
- Today every leg preloads the whole list, so each new image adds a pull and a node import (about 150 MB
  each) to all three legs.
- DELIVER makes the list depend on the group, so `happy` and `refusals` do not pay for `chain`'s images and
  `chain` does not pay for `postgres:17-alpine` or `busybox`.
- A preload that cannot pull still falls back to the node pulling the image, as today. The chain leg adds
  three anonymous Docker Hub pulls per run, well inside the limit.

**Diagnostics:** the existing failure step already loops over every non-system namespace, so it covers
`chain` with no change.

### Gates: no change needed

Checked by reading `ci_chart.yml`, `version-guard.sh` and the templates at `bf3c441aa`:
- **Defaults agree:** the `validate` step reads `values.yaml` and `_helpers.tpl` as files, so the chain
  fixture's `--set postgresql.image=…` overrides never reach it. Slice 03 changes neither default.
- **`version-guard.sh` rule 4:** the release moves the default by exactly one major above the last
  published chart, with the upgrade-source default equal to that published major. That is still the only
  step the upgrade makes. It only runs in `publish`, so the harness's overrides never meet it.
- **shellcheck:** the step globs `chart/files/postgres-upgrade/*.sh` and `run.sh`, so `volume.sh` and
  `remove-old-copies.sh` are covered with no edit.
- **No-`lookup` and render-determinism gates:** unaffected; slice 03 changes scripts, not templates.
- **helm-docs drift:** unaffected; no value changes.
- **Renovate:** unaffected. `postgres:15-bookworm` and `16-bookworm` appear only in the harness, which no
  Renovate manager parses. Like `postgres:16` and `17-alpine` today, they are test fixtures pinned by tag.
- **`publish.needs`:** already lists `upgrade-path`. A matrix job is green only when every leg is, so
  `chain` gates the release with no edit.

### Measurement impact

- **K2** (`OUT-6131-upgrade-scenarios-green`): the target now names three legs (happy, refusals, chain).
  The `gh run view` query counts `upgrade-path` legs by conclusion, so it picks up `chain` with no change.
- **K3** (`OUT-6131-restart-overhead`): unchanged target and method. `chain` adds a second measured restart,
  after a chained upgrade (AC-4.9), against its own fresh-install baseline in the `chain` cluster.
  - Slice 03 stops `pg-old-binaries` copying about 49 MB on every restart of an upgraded volume. So both
    measurements should drop slightly against the slice 01 baseline.
  - A rise would point at the chain walk, which only hashes a few small control files.
- **K1:** unchanged.

### Usage data

Still N/A, for the same reasons as above. The removal and the chained upgrade happen in the database pod
before the API runs, and the chart has no event pipe.

## Wave: DISTILL / [REF] Slice 03 — next major from an upgraded volume

### Reconciliation

Reconciliation passed — 0 contradictions between slice 03's DISCUSS (US-04, AC-4.1..4.11), DESIGN (live-copy
model, retention, generalised decision table, S3-1..S3-7) and DEVOPS (`chain` leg, images, gates unchanged),
or between those and slices 01–02 as delivered. S3-1..S3-7 are deliberate and AC-4.x is written against them.
Two wording points, neither a contradiction:
- DISCUSS Out of Scope still lists "Removing the previous major's copy automatically". After S3-3 that stays
  true: the previous major's copy (the one-chart-back rollback) is still never removed. The copy *before* it
  is. Worth one clarifying clause in that list.
- AC-4.3's third bullet speaks of a rollback "two charts back, to a chart that has the step". The chain fixture
  has no such revision: its revision two back is 0.1.17. A pin to Postgres 16 on the new chart lands on the same
  decision-table row, so the pin scenario covers both bullets.

**Slice 01–02 scenarios whose expected outcome changes: none.** I checked each against S3-1..S3-7:
- Slice 01–02 scenarios only ever reach a first upgrade, where the source is `pgdata/` and nothing is removed
  (AC-4.11). The refusal lines they assert keep the parts they check.
- The room refusal names the cleanup only when older copies exist, and on those volumes none do.
- "An interrupted removal of the old copy never costs the upgraded copy" builds its state by hand: `pg_control`
  is gone, `PG_VERSION` is present and there is no placeholder. The new removal order cannot produce that state.
  It is still the "link cannot be hashed while a newer copy counts" row, so it is still refused, and the line
  still points to the cut-off docs anchor. The only thing at risk is the docs section that anchor names
  (Upstream findings, 2).
- The file header was updated for slice 03; no scenario was touched.

### Scenario list

18 scenarios, all `@US-04 @slice-03 @pending`, all with `@contract-shape:`. Error or edge: 11 of 18 (61 %).
Harness function = `run.sh` `chain` group.

| # | Scenario | AC / tags | Env | Harness function |
|---|---|---|---|---|
| 1 | The first upgrade on a volume removes nothing | `@AC-4.11` | kind-chain-16-17 | `chain_first_upgrade_removes_nothing` |
| 2 | A volume already upgraded once moves on to the next major with every row | `@AC-4.1 @kpi:K2` | kind-chain-16-17 | `chain_second_upgrade_keeps_every_row` |
| 3 | Once the new copy is in place, the copy before last is removed and one rollback step is kept | `@AC-4.2` | kind-chain-16-17 | `chain_second_upgrade_removes_copy_before_last` |
| 4 | Restarting the database after a second upgrade does not upgrade or remove anything | `@AC-4.9 @kpi:K3 @edge` | kind-chain-16-17 | `chain_restart_after_second_upgrade_does_nothing` |
| 5 | Rolling back one chart after a second upgrade starts the previous major and says what is missing | `@AC-4.3` | kind-chain-16-17 | `chain_rollback_one_chart_starts_previous_major_and_warns` |
| 6 | Upgrading again after a one-chart rollback starts afresh from the Postgres 17 copy | `@AC-4.4 @edge` | kind-chain-16-17 | `chain_upgrade_again_after_one_chart_rollback_starts_afresh` |
| 7 | Pinning the image back one major after a second upgrade starts on the kept copy with the warning | `@AC-4.5 @edge` | kind-chain-16-17 | `chain_pin_back_one_major_starts_kept_copy_and_warns` |
| 8 | Pinning the image back two majors after a second upgrade is refused, naming both majors | `@AC-4.5 @AC-4.3 @AC-4.8 @error` | kind-chain-16-17 | `chain_pin_back_two_majors_refuses_and_touches_nothing` |
| 9 | Rolling back two charts after a second upgrade fails loudly and never starts an empty database | `@AC-4.3 @error` | kind-chain-16-17 | `chain_rollback_two_charts_fails_loudly` |
| 10 | The documented cleanup removes every copy older than the live one, and can be run again | `@AC-4.6` | kind-chain-16-17 | `chain_cleanup_removes_every_older_copy_and_reruns` |
| 11 | A cleanup cut off part-way is finished by running it again | `@AC-4.6 @error` | kind-chain-16-17 | `chain_cut_off_cleanup_is_finished_by_running_again` |
| 12 | A removal of the copy before last cut off part-way is finished by the next start | `@AC-4.6 @AC-4.2 @error` | kind-chain-16-17 | `chain_interrupted_removal_is_finished_by_next_start` |
| 13 | A second upgrade interrupted during the copy removes nothing and is redone | `@AC-4.2 @error` | kind-chain-16-17 | `chain_interrupted_second_upgrade_removes_nothing` |
| 14 | Too little room for the next copy refuses, naming the cleanup as a way out | `@AC-4.2 @error` | kind-chain-16-17 (+ tmpfs PV) | `chain_too_little_room_names_cleanup_and_touches_nothing` |
| 15 | A cleaned-up volume moves on to the next major and removes nothing | `@AC-4.7` | kind-chain-cleaned | `chain_cleaned_volume_moves_on_and_removes_nothing` |
| 16 | A newer copy made out of date by a rollback counts as the older major and is refused as a gap | `@AC-4.8 @error` | kind-chain-16-17 | `chain_out_of_date_copy_counts_as_older_major_and_refuses` |
| 17 | A live copy two majors behind the image is refused, naming that copy and one major per release | `@AC-4.8 @AC-4.10 @error` | kind-gap-from-copy | `chain_gap_from_a_copy_refuses_and_touches_nothing` |
| 18 | The Kubernetes docs say what a second upgrade keeps, removes and costs | `@AC-4.10 @in-memory` | none (prose) | docs read |

Every AC-4.1..4.11 has a scenario. Scenarios 1–17 are `@real-io`. Every refusal (8, 14, 16, 17) asserts the
volume fingerprint unchanged, and scenario 9 asserts the two remaining copies unchanged.

### Test placement

- Scenarios: appended to `chart/tests/acceptance/upgrade-bundled-postgres.feature` under a slice-03 banner.
- Kind: `chart/tests/upgrade-path/run.sh`, new `chain` group. It has a `CHAIN` array, `namespaces_of` rows
  (shared namespace `chain-16-17` for the chained scenarios, one namespace each for the rest) and `chain` in
  `main()`. `HAPPY` and `REFUSALS` are untouched, and neither lists a scaffold.
- Render: `chart/tests/unit/postgres-upgrade_test.yaml`, two skipped tests plus one guard that runs now.

### Adapter coverage

| Driven adapter | `@real-io` scenario |
|---|---|
| Data volume holding a chain of copies (local-path PVC) | 1–13, 15–17 |
| Size-enforced volume (tmpfs static PV) under a chain | 14 |
| Old-major programs, handed over only when the live copy needs them (S3-6) | 2 (handed over), 4 (not handed over on restart) |
| Shared removal (`volume.sh` through `upgrade.sh` and `remove-old-copies.sh`) | 3, 10, 11, 12; render test for the shared sourcing |
| Operator feedback (log and termination message) | 2, 3, 5, 7, 8, 14, 16, 17 |
| Bookworm image pair (`postgres:15-bookworm`, `16-bookworm`) | 17 |
| Helm CLI: upgrade, rollback, pin by `--set` | all; rollback in 5, 9, 16 |

### Scaffolds

- `run.sh`: a `scaffold()` helper and 17 one-line `chain_*` functions, each `fail`ing with
  `SCAFFOLD: not yet implemented — <scenario title>`. `bash -n` and shellcheck (koalaman/shellcheck:stable)
  are clean. CI is not wired; DELIVER adds `chain` to the matrix.
- `postgres-upgrade_test.yaml`:
  - skipped with `pending story 6131 slice 03: …`: the ConfigMap carries `volume.sh` and
    `remove-old-copies.sh`; `upgrade.sh`, `copy-old-binaries.sh` and `remove-old-copies.sh` all source
    `volume.sh`;
  - running now: the `postgres` container mounts the scripts at `/lighthouse-postgres`, which the docs
    cleanup command needs.
- `red-classification.md`: slice 03 section appended.

### Upstream findings

1. **A cut between the commit and the placeholder leaves three copies for good.** The removal writes the
   placeholder first, and the finishing step runs only when a placeholder exists. So a pod killed after the
   rename and sync of the new copy, but before the placeholder is written, leaves `pgdata/` (N−1) counting,
   with the chain N−1 → N → N+1. No later start removes it, and AC-4.2's "about twice the data" is silently
   not met. It is recoverable with `remove-old-copies.sh`.
   - Possible fix (DESIGN's call): let the finishing step run without a placeholder. Its bound,
     `remove_copies_older_than (Lm − 1)`, already leaves a first-upgrade volume's `pgdata/` alone, because
     that copy is `Lm − 1`.
   - The window is milliseconds, so no kind scenario can hit it reliably. It is not covered, and not blocking.
2. **The cut-off docs section has two audiences.** DESIGN rewrites "When removing the old copy was cut off"
   as "the next start finishes it". That holds for the automatic removal and for a cleanup cut after
   `PG_VERSION` went. It does not hold in two cases, and the section must still give the rerun instruction for
   both:
   - a cleanup cut between the placeholder and `PG_VERSION` on a first-upgrade volume (DESIGN's own open
     question);
   - the damaged-`pg_control` state the slice-02 refusal points at.
3. **Scenario 14 needs an environment DEVOPS did not list.** It is the room refusal on a chain volume: the
   tmpfs static PV from `kind-size-limited-pv` under the `kind-chain-16-17` fixture. Sizing must fit two
   copies but not three, and the first upgrade must still fit. DELIVER composes the two. If the budget is
   tight, this is the first scenario to move to a `chain-refusals` leg.
4. **Scenario 11 builds its cut-off state by hand.** It removes `PG_VERSION` of `pgdata-17` after the
   placeholder exists, because cutting `kubectl exec` mid-cleanup does not reliably stop the server-side
   process. This is the same compromise the slice-02 interrupted-cleanup scenario made.
   - Per DESIGN, the next start does **not** finish this one. `pgdata-17` is not below `Lm − 1 = 17`, so the
     rerun of the command is what clears it. The scenario asserts exactly that.
5. **A presence assertion that cannot fail.** helm-unittest's `isNotNullOrEmpty` passes on a missing path, so
   the new key checks use `matchRegex` on the shebang (see `red-classification.md`).

### Open checks for DELIVER

- Wire `chain` into the `upgrade-path` matrix and make `preload_images` depend on the group, with the slice 03
  code. Remove `@pending` and the helm-unittest `skip` in the commit that greens each one.
- Record the genuine RED of fixture step 3 on today's scripts ("runs on Postgres 17 in pgdata-17") in
  `red-classification.md`.
- Scenario 12 waits for the placeholder to appear before deleting the pod. Scenario 13 waits for the "copy
  started" line, and must prove the delete landed mid-copy (the next start reports a discarded `.partial`).
- Scenario 4 reuses the K3 measurement from slice 01: pod creation to the `postgres` container start, against
  a fresh-install baseline restart in the same cluster. It also asserts that `pg-old-binaries` handed nothing
  over (S3-6).
- Measure the `chain` leg against the 12-minute local target. If it is close, split out scenarios 14, 16 and
  17 first.
- Answer Upstream finding 1 before slice 03 closes.

### Slice 03 DISTILL findings — dispositions (orchestrator, AFK, 2026-10-02)

- **Three copies kept for good if the pod dies between the rename and the placeholder.** Decided: the finishing
  step runs whenever the chain holds copies older than the live copy's source, with or without a placeholder. Its
  bound (only copies below live major − 1) already leaves a first-upgrade volume alone. DELIVER implements it in
  the shared removal function; no extra scenario, since the window is milliseconds.
- **Cut-off cleanup docs.** Both instructions stay: the next start finishes an automatic removal, and rerunning the
  documented command finishes a manual cleanup cut off on a first-upgrade volume or a damaged control file.
- **The chain room refusal needs the size-limited volume under the chain fixture.** DELIVER combines the two in the
  `chain` leg; it is the first scenario to split out if that leg runs long.
