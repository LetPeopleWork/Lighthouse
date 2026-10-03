# The chart carries its bundled Postgres across a major — Story 6131

Delivered 2026-10-02 to 2026-10-03 in three slices. Chart only. Pushed to `main`; not yet in a chart release.

The released chart, 0.1.17, ships `postgres:17` for its bundled database, and every bundled install in the
wild runs on it. `main` had already moved the default to `postgres:18`. Postgres 18 cannot open a 17 data
folder, so the next chart release would have left every bundled install that upgraded on default values
crash-looping on `database files are incompatible with server`, with a dump and restore by hand as the only
way out. Now the chart does the move itself. On every start of the database pod, two init containers built
only from official `postgres` images decide what the volume needs: one hands over the previous major's
programs, the other runs `pg_upgrade --copy` into a new folder named after the new major. The chart moves the
data one major per chart release, from whichever copy the database runs on, and keeps the copy it read from,
so a `helm rollback` one chart back still opens the database as it was before the upgrade.

## What shipped

| Step | Commit | What it changed |
| --- | --- | --- |
| — | `ce5a0edc7`, `b89ce41be` | Before any upgrade code: the default went back to `postgres:17`, so no release could ship 18 without the upgrade. |
| 01-01 | `c36030b6d` | A plain `helm upgrade` from 0.1.17 with data comes back on Postgres 18 with every row. The 17 data stays where it was, in `pgdata/`; the 18 copy is built beside it and goes live by a rename. |
| 01-02 | `1cc0ff3e9` | A restart after the upgrade starts on the upgraded copy without upgrading again. CI proves the upgrade on every chart change. |
| 01-03 | `d3e1d1d3e` | After a rollback to 0.1.17, upgrading again redoes the copy from the 17 data. The old 18 copy is detected as out of date and never reused. |
| 01-04 | `f914e7b76` | Pinning the image back to 17 on the new chart starts on the kept copy, with one warning line saying what is missing. |
| 01-05 | `108d438a1` | `--reuse-values` keeps the database on 17, and the install notes say it is behind and how to move it. |
| 01-06 | `2705a0a3c` | 18 can only ship with the upgrade: the publish guard, the `validate` checks, a Renovate rule keeping the upgrade-source image to minor updates, and the chart version note. |
| 02-01 | `2a0e89226` | Too little room for a second copy is refused before anything is written. Once the volume grows, the same pod goes ahead by itself. |
| 02-02 | `2a384158f` | `docs/Installation/kubernetes.md` describes the automatic upgrade, rollback, the old copy, what it costs and how to remove it. |
| 02-03 | `95062fce5` | Data on the wrong major, either too old or newer than the image, is refused, naming both majors. A values fix after a refusal is followed by `kubectl delete pod`. |
| 02-04 | `cd831ad40` | An upgrade interrupted part-way is redone from the start, and the database never starts on a partial copy. |
| — | `b6ab2c99f` | The demo-data dogfood and the stuck-pod amendment, recorded. |
| — | `8a3608aeb` | Refactor: the upgrade step's start-up decision reads as one table. Checked against the old script on 1,200 synthetic volumes, with identical log lines, folders and exit codes. |
| — | `73e84727e`, `674a41ded`, `3ef775b21` | Review of slices 01–02. An unreadable old copy no longer gets the upgraded copy thrown away. The publish guard also refuses a skipped major or a mismatched upgrade source. The database container no longer advertises a data folder it may not run on. Values comments rewritten in plain words, README regenerated. |
| 03-01 | `3dfaf7b3d` | A volume already upgraded once (16 → 17 in the test) moves on to the next major from the copy it runs on, with every row. |
| 03-02 | `86429ad0b` | Every start follows the copy the database last ran on: a restart stays put, and one chart back or one major back starts on the kept copy with the warning. CI runs the chain. |
| 03-03 | `979cb77cc` | Once a second upgrade is in place, the copy before last is removed automatically. One rollback step is kept. |
| 03-04 | `8e7eee8cb` | A removal cut off part-way is finished by the next start. A removal that fails logs a warning and never stops the database. |
| 03-05 | `617ae0714` | Every refusal is measured against the copy the database runs on. The older programs are handed over by the same rule, so every refusal line comes from one container. |
| 03-06 | `b420324d2` | The operator's cleanup becomes `remove-old-copies.sh`, shipped in the scripts ConfigMap. It removes every copy older than the live one through the same removal code, and can be rerun. |
| 03-07 | `5854c2d63`, `fba762832` | The room refusal points to a new docs section for cleaning up while the database is stopped. The docs, values comments and ADR-213 say what a second upgrade keeps, removes and costs. |
| — | `439dc672c`, `e5b01e72f` | Refactor: shared volume rules live in one library; the harness states each repeated check once. |
| — | `1150878fc` | Harness checks that could never fail now fail the scenario (see Lessons). |
| — | `1a5866155`, `50b786693`, `7cc069952`, `11fa900dc` | Review revision: an unreadable control file on a copy that still counts is always refused; a newer copy that no longer counts is never set aside; readiness is checked over TCP. |
| — | `b3ae14fbf`, `06a89c061`, `95a12d412` | CI fixes: 30-minute legs rebalanced by running time, the shellcheck finding only CI's older version reports, and the standalone render gate's pipe race. |

The wave documents (DISCUSS, DESIGN, DEVOPS, DISTILL for slices 01–02 and again for slice 03, and the
roadmaps) are `4184af3fa`, `fdd27ce28`, `2773e56d4`, `21170e762`, `ce9d8bb4b`, `d6183f842`, `5feb51de2`,
`23bc6cda4`, `67d853f53`, `f5d85185a` and `03f57dc53`.

No application code, no migration, no API change. External-database installs (`postgresql.enabled=false`)
render exactly as before.

## Decisions worth keeping

- **The old copy is kept, so a rollback works.** After an upgrade the volume holds the copy the upgrade read
  from and the new one, about twice the data. A `helm rollback` one chart back starts on the old copy. Anything
  written after the upgrade is not in it, and the docs say so.
- **Chart only.** The docker-compose example keeps its documented manual path.
- **One major per chart release.** An install that skipped a release that moved the major upgrades to that
  release first. A gap of two majors is refused with a line that says so, and the publish guard keeps every
  release to exactly one major above the last published chart.
- **Retention.** The chart keeps the copy the live one was made from and, once a newer upgrade is in place,
  removes the one before it. The operator's `remove-old-copies.sh` removes every older copy, including the
  one-chart-back rollback, and only ever on request. Every removal writes the placeholder file
  `UPGRADED-TO-<major>-see-kubernetes-docs` first, so a rollback too far back fails loudly instead of starting
  an empty database.
- **An unreadable `pg_control` on a copy that still has `PG_VERSION` is always damage.** The start and the
  cleanup both refuse with "put that file back from a backup", before writing anything. No removal the chart
  ships can leave that state, because every removal deletes `PG_VERSION` first. This reverses a rule decided
  autonomously during slice 03, which read such a copy as a removal that had already started and let the
  cleanup finish it. The reviewer showed that after a rollback that copy holds the newest rows, so
  "finishing" would delete them. It was reproduced on kind before the fix.
- **Readiness checks over TCP, liveness stays on the socket.** The readiness probe runs
  `pg_isready -h 127.0.0.1`, as Lighthouse itself connects. This is a change to the pod template, not only
  to the scripts.
- **A values change after a refusal needs `kubectl delete pod`.** The StatefulSet's default pod policy never
  replaces a pod that is not Ready, and the policy cannot be changed on an existing install. Every refusal
  line that suggests a values fix ends with the exact command. Growing the volume needs no delete, because
  the same pod retries by itself.
- **Slices 01–03 ship together.** So "one chart back" always reaches a chart that has the full upgrade step.
- **Planner statistics are a documented step, not an automatic one.** `pg_upgrade` does not carry all of
  them across. The docs give a `vacuumdb --analyze-in-stages` command to run after an upgrade.
- **`--reuse-values` keeps the old image.** Helm carries over the previous chart's `postgresql.image`, so
  nothing moves and nothing breaks. The install notes print a line saying the database is behind the chart's
  default and how to move it.

## Quality

- **Kind upgrade-path suite:** 46 scenarios in five CI legs (happy, refusals, chain, chain-refusals,
  chain-cleanup), each on its own kind cluster. Every refusal scenario takes a fingerprint of the volume
  before and after and asserts it unchanged. The job gates the chart's `publish` step.
- **helm-unittest:** 92 tests, including the external-database render.
- **shellcheck** on every upgrade script, the harness and the publish guard.
- **Mutation testing:** Stryker does not apply; this story is shell and Helm only. The substitute decided in
  DEVOPS stands: the fingerprinted refusal scenarios, shellcheck and helm-unittest. A sampled run of
  hand-made mutations was started and stopped by the maintainer as not worth the time.

## Issues and lessons

- **The chain leg outgrew the CI budget twice.** First the chain refusals moved to a leg of their own, then
  the cleanup scenarios. The legs were then rebalanced by measured running time and the job limit went
  from 20 to 30 minutes.
- **Some harness checks could never fail.** Bash drops `set -e` inside a command substitution, so a check
  printed through `echo "$(...)"` reported nothing when it failed. The same holds for a scenario run on the
  right of `||`. Both now assign first and print after, or run inside an `if`.
- **CI's shellcheck is older than the local one.** The runner's 0.9.0 reports SC2015 on two `[[ ]]` tests
  chained with `&&` before an `||`; the 0.11 image used locally does not.
- **The standalone render gate lost a pipe race.** It piped the render into `grep -q` under `pipefail`.
  `grep -q` exits at the first match, the writer dies of SIGPIPE, and the step failed on a render that was
  fine. It only showed once the ConfigMap carried the scripts and the render grew. A here-string has no
  writer to kill.
- **A fresh install could turn Ready too early.** The official entrypoint runs a temporary server on the
  Unix socket alone while it sets up an empty database. The readiness check asked over the socket, so the
  pod went Ready during that setup and Lighthouse, which connects over TCP, got "the database system is
  shutting down".
- **Freezing a process tree one process at a time is too slow on CI.** The interrupted-removal scenario
  stopped the upgrade step by sending SIGSTOP to each of its processes in turn, found by walking `/proc` on
  the kind node. The `rm -rf` child kept deleting meanwhile, and on CI's faster disk the removal finished
  before the freeze landed, so the scenario failed on its own precondition. The harness now freezes the
  container's cgroup with one write to `cgroup.freeze`, which stops every process in it at once.

## Finalize checklist

- **Docs prose:** done. `docs/Installation/kubernetes.md`, the `postgresql.image` and
  `postgresql.upgrade.image` comments in `chart/values.yaml`, `chart/README.md` regenerated with helm-docs,
  and ADR-213.
- **Per-feature screenshots:** N/A, because there is no UI change.
- **Demo data:** N/A, because the change is chart-only.
- **Website asset freshness:** N/A, because no file under `docs/assets` changed.
- **Lighthouse-Clients CLI/MCP:** N/A, because no API, DTO or MCP tool changed.
- **Usage-data event:** N/A, decided in DEVOPS. The upgrade runs in the database pod's init containers
  before the API is up, the API cannot see the volume, and the chart has no event pipe of its own. The
  outcome measures are reported crash-loops and CI results, which need no event.
- **RBAC:** N/A, because no permission, API surface or UI gate changed.
- **Configurable terminology:** N/A, because no Lighthouse term is involved.
- **Release Notes:** no tag on #6131, by the maintainer's decision. (DISCUSS had planned one.)
- **Dogfood with a restored real backup:** still the maintainer's. What was done on 2026-10-02 used the
  demo data, not a real backup, and covered the slice 01 path only: install 0.1.17, load all nine demo
  scenarios (44 tables, 20,467 rows), plain `helm upgrade` to 18, rollback to 0.1.17, upgrade again, with row
  counts identical at each step. The slice 02 refusals and the slice 03 chain were proved by the kind suite,
  not by a dogfood run.

## Still open

- The step that settles a set-aside copy runs before the refusals. It only touches a `.stale` folder, so it
  is harmless, but a refusal is then not strictly the first thing that runs.
- The older-programs container hands its programs over before the room, operating-system and
  mismatched-upgrade-image refusals are decided. It writes only to a scratch volume, so nothing on the data
  volume changes.
- A volume holding a Postgres 9.x database is described as "no database" in the log, because a version
  like `9.6` is not read as a major. Wording only.
- The next chart release ships `postgres:18` as the default. No re-pin to 17 is needed.
- Cutting the chart release, the real-backup dogfood, and reading the crash-loop count at 30 and 60 days
  after release.

## Links

- Workspace: `docs/feature/story-6131-chart-postgres-major-upgrade/`
- [ADR-213](../product/architecture/adr-213-bundled-postgres-crosses-a-major-in-an-init-container-from-official-images.md)
- [Kubernetes installation docs](../Installation/kubernetes.md)
- `chart/tests/acceptance/upgrade-bundled-postgres.feature`
- `chart/tests/upgrade-path/run.sh`
