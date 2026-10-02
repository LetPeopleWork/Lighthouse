# RED classification — story-6131-chart-postgres-major-upgrade (DISTILL, 2026-10-02)

Pre-DELIVER check that each pending check fails because the behaviour is missing, not because the test is
broken. Run against the templates on `main` at `991edd7e9`, Helm v4.3.0, helm-unittest 1.1.1 (CI pins 1.1.2;
the `skip` field has existed since well before both).

## How it was run

- **Hand-off state (what CI sees):** `helm unittest -f 'tests/unit/*.yaml' ./chart` → 7 suites passed,
  75 tests passed, 12 skipped, 0 failed.
- **Unskipped:** a scratch copy of `chart/` with every `skip:` block removed from
  `tests/unit/postgres-upgrade_test.yaml`, then `helm unittest -f 'tests/unit/postgres-upgrade_test.yaml'`
  → 15 tests: 9 failed, 6 passed.
- **Kind harness:** each function in `chart/tests/upgrade-path/run.sh`, sourced without its `main` call so no
  cluster or kube context is touched, exits 1 with `✗ upgrade-path: SCAFFOLD: not yet implemented — <scenario>`.
  `shellcheck` (koalaman/shellcheck:stable) reports nothing; `bash -n` is clean.

## helm-unittest, per test (unskipped)

| Test | Result | Class | Why |
|---|---|---|---|
| the bundled database gets the two upgrade steps, old binaries first | FAIL | MISSING_FUNCTIONALITY | `unknown path spec.template.spec.initContainers[0].name` — no init containers yet |
| by default the upgrade source is Postgres 17 and the upgrade runs the Postgres 18 image | FAIL | MISSING_FUNCTIONALITY | no init containers; database image is `postgres:18`, expected `postgres:18-trixie` |
| a mirrored database image is the one the upgrade step runs | FAIL | MISSING_FUNCTIONALITY | `unknown path ...initContainers[1].image` |
| the upgrade-source image comes from its value when one is set | FAIL | MISSING_FUNCTIONALITY | `unknown path ...initContainers[0].image` |
| values from an older chart still render, falling back to the chart's own upgrade-source image | FAIL | MISSING_FUNCTIONALITY | `unknown path ...initContainers[0].image`; the render itself succeeds with `postgresql.upgrade: null` |
| the upgrade scripts render with the bundled database | FAIL | MISSING_FUNCTIONALITY | `template "lighthouse/templates/postgres-upgrade-configmap.yaml" not exists` — the template is the thing to build |
| an external database renders neither the database nor its upgrade scripts | FAIL | MISSING_FUNCTIONALITY | same missing template; the StatefulSet half already holds |
| the notes warn when the database image is a major behind the chart's default | FAIL | MISSING_FUNCTIONALITY | rendered NOTES has no "behind" line and no `--reset-then-reuse-values` |
| the notes warn for a Debian-suffixed tag of the older major too | FAIL | MISSING_FUNCTIONALITY | rendered NOTES has no "behind" line |
| the notes stay quiet when the database image is the chart's default major | pass (vacuous) | n/a | nothing prints the line yet; meaningful only beside the positive NOTES tests, so it stays skipped with them |
| the notes stay quiet when the image carries no major to compare | pass (vacuous) | n/a | as above |
| the notes stay quiet for an external database | pass (vacuous) | n/a | as above |
| the database keeps the volume claim and size of chart 0.1.17 | pass | regression guard, **not skipped** | holds today and must keep holding |
| the database container keeps its name and mount path | pass | regression guard, **not skipped** | as above |
| the database keeps the readiness and liveness checks of chart 0.1.17 | pass | regression guard, **not skipped** | as above |

No IMPORT_ERROR / FIXTURE_BROKEN / SETUP_FAILURE: the suite loads, the dummy encryption key and password let
every template render, and setting `postgresql.upgrade: null` is proven to remove a key (checked against
`postgresql.persistence: null`, which nil-pointers the StatefulSet render as expected).

## Kind scenarios (@real-io)

All 18 `@real-io` scenarios map to one harness function each and fail with the `SCAFFOLD:` line, so their RED
is a placeholder, not yet an assertion. Their genuine fail-for-the-right-reason check happens in DELIVER,
when a function body is written against today's chart: the expected first failure of the walking skeleton is
the 18 pod crash-looping on `database files are incompatible with server` — the defect the story removes — not
a harness error. DELIVER records that observation when it unskips the walking skeleton.

## Known edit DELIVER must make in an existing suite

`chart/tests/unit/render_test.yaml` asserts the default database image is `postgres:18`. DDD-11 moves the
default to `postgres:18-trixie`, so that assertion changes in the same commit as `values.yaml`.

## Walking skeleton, genuine RED (DELIVER step 01-01, 2026-10-02)

`upgrade_plain_helm_upgrade_keeps_every_row` run on a fresh kind cluster against the chart at `5b64dc43f`
(before any template edit), with the upgrade given `--set postgresql.image=postgres:18-trixie` because that
chart still defaults to `postgres:17`. Chart 0.1.17 installed, the marker row was seeded and the row counts
recorded; after the upgrade the Postgres 18 container crash-looped and the harness stopped on its third
restart with:

```
FATAL:  database files are incompatible with server
DETAIL:  The data directory was initialized by PostgreSQL version 17, which is not compatible with this version 18.6 (Debian 18.6-1.pgdg13+2).
✗ upgrade-path: the database pod in plain-upgrade is crash-looping
```

Class: MISSING_FUNCTIONALITY — the defect the story removes, not a harness, image-pull or cluster failure.

## Slice 03 (DISTILL, 2026-10-02, at `d1fb6e061`)

Hand-off state: `helm unittest -f 'tests/unit/*.yaml' ./chart` → 7 suites passed, 90 tests passed, 2 skipped,
0 failed. Unskipped on a scratch copy (`skip:` blocks stripped), `postgres-upgrade_test.yaml` → 20 tests, 2 failed,
18 passed.

| Test | Result | Class | Why |
|---|---|---|---|
| the upgrade scripts include the shared volume chain and the cleanup command | FAIL | MISSING_FUNCTIONALITY | `unknown path data["volume.sh"]`, `unknown path data["remove-old-copies.sh"]` — neither key is in the ConfigMap yet |
| the upgrade step, the old-programs step and the cleanup all read the volume through the same script | FAIL | MISSING_FUNCTIONALITY | `upgrade.sh` and `copy-old-binaries.sh` render but source no `volume.sh`; `remove-old-copies.sh` does not exist |
| the database container can reach the cleanup script the docs tell the operator to run | pass | regression guard, **not skipped** | the `postgres` container already mounts the scripts at `/lighthouse-postgres`; the docs command depends on it |

A trap found on the way: `isNotNullOrEmpty` on a path that does not exist **passes** in helm-unittest 1.1.1, so a
presence check written with it would have been green against the current templates. The key checks use
`matchRegex` on the shebang instead, which fails with `unknown path`.

Kind (`chain` group): each of the 17 functions exits 1 with
`✗ upgrade-path: SCAFFOLD: not yet implemented — <scenario title>`. This is a placeholder RED. The real one,
which DESIGN predicts, is fixture step 3 on today's scripts: the defaults upgrade is refused with "the database
runs on Postgres 17 in pgdata-17". DELIVER records that line here when it writes the fixture.
