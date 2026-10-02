# ADR-213: The bundled Postgres crosses a major version in an init container, with `pg_upgrade --copy` into a sibling directory and binaries taken only from official images

**Status**: Accepted (2026-10-02, option chosen by the maintainer during the DESIGN wave, interaction mode PROPOSE)

**Feature**: `story-6131-chart-postgres-major-upgrade` (ADO User Story #6131),
`docs/feature/story-6131-chart-postgres-major-upgrade/feature-delta.md`

**Decider**: Benjamin (maintainer), Platform Architect (PROPOSE)

**Relationship to prior work**: ADR-080 gave the chart a bundled Postgres StatefulSet built from the official
`postgres` image, and rejected a third-party catalog because a public chart must not depend on one. This ADR
keeps that rule and adds the step that carries the data across when the chart's default image moves to a new
major. ADR-082 (render-time refusal) and ADR-083 (the published chart repo) are unchanged.

---

## Context

A Postgres major cannot open the data files of the previous one. Chart 0.1.17 ships `postgres:17` with
`PGDATA=/var/lib/postgresql/data/pgdata` on the StatefulSet's PVC, and `main` already defaults to
`postgres:18`. Without an upgrade step, the next chart release leaves every bundled install crash-looping on
`database files are incompatible with server`.

Four constraints narrow the choice:

1. A rollback to 0.1.17 must start on the pre-upgrade database. 0.1.17's `PGDATA` path is fixed and cannot be
   changed after the fact, so the old data has to stay exactly where it is and stay usable.
2. A tenant deployed by ArgoCD gets Helm hooks only through Argo's own partial mapping of them, and a chart
   applied as rendered manifests gets none, so the mechanism can only use plain pod spec.
3. An upgrade that cannot be done safely must stop before it writes anything, and say why in one log line.
4. The images must be official or reproducible, multi-arch (amd64 and arm64) and kept current by Renovate.

What was measured during DESIGN (Docker 29.8, Helm v4.3.0, a throwaway kind cluster):

- `postgres:17` and `postgres:18` are both built on Debian 13 (trixie) with glibc 2.41. 17's binaries,
  libraries and share files (about 49 MB) copied into the official 18 container have no missing libraries,
  and 18's `pg_upgrade --copy` upgraded a seeded 17 database with every row intact. 18 started on the copy,
  and 17 started on the untouched original afterwards.
- `pg_upgrade` refuses a source cluster that was not shut down cleanly. One start and fast stop with the old
  binaries fixes that. The 0.1.17 pod is usually not shut down cleanly: Postgres answers SIGTERM with a smart
  shutdown that waits for the API's pooled connections, and the kubelet kills it at the end of the grace
  period.
- 18's `initdb` turns data checksums on by default, and 17's left them off. `pg_upgrade` refuses a mismatch.
- A cluster created by `initdb` directly lacks the `host all all all scram-sha-256` rule that the image's
  entrypoint adds, so the API could not connect over TCP unless the old `pg_hba.conf` is carried over.
- `pg_upgrade` starts and stops the old cluster, which rewrites `global/pg_control`. Every later run of the
  old major rewrites it again.
- Each init container added about 1 s to the time from pod creation to Ready on kind.
- `helm upgrade --reuse-values` keeps the previous chart's default values, including the old image and the
  absence of any new key.

## Decision

1. **Two init containers in the bundled Postgres pod do the upgrade. Nothing runs outside the pod.**
   - `pg-old-binaries` runs the upgrade-source image (`postgresql.upgrade.image`, default
     `postgres:17-trixie`). When the data on the volume is its own major, it copies its `bin`, `lib` and
     `share` trees into an emptyDir and records its OS release. Otherwise it exits at once.
   - `pg-upgrade` runs the **main** Postgres image, so the new cluster is created by the same binaries that
     will serve it, with the same C library and collation rules. It decides what to do, and when an upgrade
     is due it runs `pg_upgrade --copy` against the copied binaries.
   - The `postgres` container starts through a short wrapper that takes `PGDATA` from that decision and then
     runs the image's own entrypoint, unchanged.
2. **The old data never moves.** The original cluster stays in `pgdata/`. The upgraded cluster is built in
   `pgdata-<major>.partial/` and becomes live only by being renamed to `pgdata-<major>/` after `pg_upgrade`
   succeeds. A rename on one filesystem is atomic, so an interrupted attempt leaves at most a `.partial`
   directory, which the next attempt deletes and redoes. Postgres is never started on a partial copy.
3. **The upgraded copy records which source it came from.** A file inside `pgdata-<major>/` holds the source
   major, its system identifier and the SHA-256 of the source's `global/pg_control`, taken after `pg_upgrade`
   finished. If the hash no longer matches, the old major has run since, for example after a rollback. The
   copy is then out of date, so the upgrade is redone from the old data. The out-of-date copy is set aside
   as `pgdata-<major>.stale` and deleted only once the new copy has been renamed into place; a `.stale`
   left by a stop in between is put back when nothing replaced it. A copy is only ever called out of date
   on a hash that was actually computed: when the old data's `global/pg_control` is missing or unreadable,
   as a removal of the old copy cut off part-way leaves it, the start is refused instead.
4. **Majors are detected at run time, never taken from values.** The data's major comes from `PG_VERSION` on
   the volume and each image's major from its `PG_MAJOR` environment variable. A mirrored or renamed image
   therefore behaves the same as the default one.
5. **Every refusal check runs before anything is written.** Free space, a gap of two or more majors, data
   newer than the image with no kept copy, and binaries that will not run in the main image are all checked
   before crash recovery, `initdb` or any copy. A refusal writes one line to the log and to the termination
   message, and exits non-zero. The pod stays in `Init` and the kubelet retries with back-off, so fixing the
   cause (growing the PVC or pinning the image) needs no other manual step.
6. **The new cluster takes its settings from the old one:** checksums, encoding, locale and locale provider
   of `template1`, and the bootstrap superuser. `pg_hba.conf` and `pg_ident.conf` are copied across.
7. **Removing the old copy leaves a placeholder.** The documented cleanup empties `pgdata/` and leaves one
   file in it. A rollback to 0.1.17 then fails loudly (its `initdb` refuses a non-empty directory) instead of
   starting an empty database. The new chart treats the placeholder as "old copy removed".
8. **The image pair is pinned to one Debian release.** The defaults become `postgres:18-trixie` and
   `postgres:17-trixie`, because the floating major tags could move to a new Debian release at different
   times. The upgrade source gets minor and digest updates from Renovate, but never a major one. A CI step
   checks that its major is exactly one below the main image's.
9. **The chart's default major is also a template constant.** `--reuse-values` hides the new chart's
   values, so the fallbacks for `postgresql.upgrade.image` and the default-major check in `NOTES.txt` live
   in `_helpers.tpl`, and a CI step checks that they agree with `values.yaml`.

## Alternatives Considered

- **A. A third-party dual-binary image** (`tianon/postgres-upgrade:17-to-18` pinned by digest, or
  `pgautoupgrade`). One init container instead of two, so about 1 s faster on restart, and the image name
  carries the pair of majors. Rejected:
  - Neither is an official image, which is the supply risk ADR-080 already declined for this chart.
  - The new cluster would be created by different binaries from the server that runs it. On an Alpine (musl)
    main image, that means indexes sorted under glibc would be read under musl.
  - `pgautoupgrade` upgrades in place with `--link` and deletes the old cluster, which makes a rollback
    impossible.
- **C. Dump and restore.** The old image runs `pg_dumpall` against a server on a local socket, and the main
  image restores into a fresh cluster. It needs no copied binaries and was slightly faster on a 20 MB
  database (5.6 s against 8.3 s). Rejected:
  - A restore has to tolerate some expected errors (the bootstrap role already exists), which makes real
    errors harder to tell apart.
  - Every index is rebuilt, so it slows down faster than `pg_upgrade` as the database grows.
  - It needs room for the dump as well as the new cluster.
  - It still needs two init containers, so it saves nothing on restart.
- **Upgrade inside the `postgres` container before starting the server.** One init container fewer.
  Rejected, because the liveness probe would kill the container during a long upgrade and turn it into a
  restart loop.
- **Kubernetes image volumes** to mount the old image's files without copying them. Not available by
  default on the cluster versions the chart supports. Worth revisiting when it is.
- **A Helm pre-upgrade hook Job.** Rejected, because a GitOps tool that renders the chart never runs it.

## Consequences

- A chart release can move the default Postgres major. Existing installs come up on the new major after a
  plain `helm upgrade` or a GitOps sync, and a rollback still opens the old data.
- The volume holds about twice the data until the operator removes the old copy. The docs give the size and
  the one command.
- Every start of the bundled Postgres pod runs two short init containers, about 2 s on kind. The first start
  on a node also pulls the upgrade-source image.
- `--reuse-values` from 0.1.17 keeps `postgres:17` and does no upgrade. `NOTES.txt` prints a line when the
  image's major is behind the chart's default, and the docs say to use a plain `helm upgrade` or
  `--reset-then-reuse-values`.
- Each new Postgres major needs a matching upgrade-source image, moved by hand in the same pull request as
  the main image. The CI check fails if the two drift apart.
- Settings changed with `ALTER SYSTEM` (`postgresql.auto.conf`) are not carried across. The chart never sets
  any.
- External databases (`postgresql.enabled=false`) render exactly as before. The whole mechanism sits inside
  the bundled StatefulSet template.
- Every start pulls the upgrade-source image, even with nothing to upgrade, so an operator who mirrors images
  mirrors both. Both steps need an official `postgres` image that starts as root and drops to the `postgres`
  user, so a runtime that forbids root (OpenShift's `restricted` SCC, for one) cannot run the bundled
  database.

## Known limitation

The upgrade only ever reads the data in `pgdata/`, the folder the volume was first set up with. After one
upgrade the database runs on `pgdata-<major>/`, and `pgdata/` holds the data as it was before that upgrade.
A later chart that moves the default image on to the next major therefore cannot carry such a volume across
by itself: the step refuses, names `pgdata-<major>/` as where the database runs, and says to set the image
back to that major. It never suggests the older major in `pgdata/`, which would start the database without
everything written since the first upgrade.

Before the chart moves its default image to the next major, the step has to be extended to take the newest
`pgdata-<major>/` as the source of the upgrade. That was not done here because no image of the next major
exists to test it against. The publish guard does not catch it: it refuses a default major more than one
above the last published chart's, and an upgrade-source image that is not the last published major, but a
move from 18 to 19 passes both checks. Whoever moves the default on next extends the step first.
