# Bug #6068 — the calver build counter emitted `.0`

**Merged to `main` 2026-09-22, not yet released** · ADO Bug #6068 · workspace
`docs/feature/fix-build-version-always-zero/` · seven commits, `59be5857e` through `495779682`
(5 files, +394/−59, all under `.github/`)

## What changed

The version stamped on a build is `v<YY>.<M>.<D>.<build>`, where `<build>` counts how many builds ran
today. Intermittently it counted none, and a build that was the eleventh of the day shipped as
`v26.9.21.0`. The maintainer reported it as "something changed and build version is now always 0 …
this used to work and suddenly broke", and was right on both counts: the failure is recent and it was
not there before.

The counter now refuses to answer when it cannot see itself. It asserts the one invariant available —
the current run is itself a run of the workflow it is listing, so it must appear in its own listing —
retries until it does, and fails the job rather than emit a number it cannot justify.

## Two defects, one report

**1 — intermittent, on `push` to `main`, roughly 1 run in 7. This is the reported one.**
GitHub's `listWorkflowRuns` is not read-your-writes consistent. It intermittently serves a stale page
whose newest run already predates today's midnight, and the counting loop — which breaks on the first
run older than midnight — exited on its first iteration with a count of zero. The script could not
distinguish "no builds today" from "the page I was handed is weeks old". Captured live, three
identical consecutive queries returned `total_count` 982, then 1443 with the newest run dated
2026-09-05 while the actual date was 2026-09-22, then 2379 correct. Pagination did not stitch over it
either: page 1 ended at 2026-09-19 and page 2 resumed at 2026-08-03, dropping six weeks.

**2 — deterministic, `pull_request` only, never shipped.** `context.ref.replace('refs/heads/','')`
leaves the literal `refs/pull/<N>/merge` untouched, which matches no branch, so the API returned an
empty list. Real, separate, and invisible in production because pull-request builds are not published.

**The evidence that dated it.** A scan of the Version job's logs across 60 consecutive `main` runs
found four zeros in 2026-09-19 → 09-22 (Sept 21 at 19:59, 17:16 and 10:11; Sept 20 at 14:34) and
**none** across 2026-07-23 → 08-03. The zeros interleave with correct counts on the same day — Sept 21
emitted `0` at 19:59 while 12:51 had counted 11 and 20:19 counted 14. No code changed between those
runs.

## What shipped

| Commit | |
|---|---|
| `59be5857e` | Both defects pinned with failing tests, counting logic extracted verbatim first |
| `570116a09` | Count only when the current run is visible — the fix |
| `b2495d40a` | The workflow calls the extracted generator |
| `1a6879469` | A phantom retry delay and a dead day check removed |
| `c6d642953` | `actions/github-script` pinned to a commit SHA in the version workflow |
| `cb8274149` | The workflow-script job extracted into a reusable workflow |
| `495779682` | The current run detected anywhere on the page, not only where the count reaches |

**Files.**

- `.github/scripts/generate-version.mjs` — new. The counting logic, lifted out of inline workflow YAML
  so that it could be tested at all. Every dependency is injected (`listRuns`, `now`, `runId`,
  `sleep`); there is no Actions API inside it.
- `.github/scripts/generate-version.test.mjs` — new. Six `node:test` cases.
- `.github/workflows/ci_version.yml` — now a thin adapter, and its `actions/github-script` is
  SHA-pinned.
- `.github/workflows/ci.yml` — a `workflow-scripts` job added, `version` gained
  `needs: workflow-scripts`, and `.github/scripts/**` added to the paths filter so a change there
  triggers the gate.
- `.github/workflows/ci_workflow-scripts.yml` — new reusable workflow that runs the tests.

**The fix itself.** Retry up to five attempts with 2/4/8/16-second backoff until the current run
appears in the listing; throw if it never does. Freshness detection is deliberately kept separate from
the counting loop's midnight break, because a re-run keeps its original creation time and can
therefore sit below that line while still proving the page current — the last commit in the table is
exactly that correction, found by probe rather than by reasoning.

**The seam is the point.** The module owns the decision — what counts, when to retry, when to refuse —
and the workflow adapter owns the I/O: Octokit, the real timer, Actions outputs, failure signalling.
That split is the whole reason any of this is testable; while the logic lived in workflow YAML,
nothing about it could be exercised outside a real runner.

## Consequences, recorded honestly

**A build can now go red where it previously emitted a wrong version.** If the listing stays stale past
roughly thirty seconds of retries, the Version job fails and the build stops. Deliberate, and approved
by the maintainer: a wrong version can overwrite an already-pushed, cosign-signed `ghcr.io` tag, and a
red build is far cheaper than that.

**The fix cannot be verified locally.** The six tests prove the module's logic against injected stubs;
they do not and cannot reproduce real GitHub staleness, which shows up at about one run in seven on a
real runner. This ships and is then observed — the first evidence is the next few `main` runs.

**Nothing lints `.github/scripts/`.** Backend Sonar analysis excludes `**/.github/**`, frontend Sonar
is scoped to `Lighthouse.Frontend`, and Biome covers only each project's `./src`. The new `node --test`
job is the sole gate over that directory.

## The lessons worth carrying: two approvals, both wrong

**The first root-cause analysis reached the wrong conclusion and was peer-reviewed as approved while
wrong.** It concluded the defect was `pull_request`-only and that `main` was unaffected, generalising
from a single day's log sample. The maintainer's counterexample — a `push` run on `main` that emitted
`.0`, pasted into the work item eight minutes after it ran — refuted it outright, and the real root
cause was found only afterwards. The analysis document carries a superseded banner and a corrected
opening section rather than a rewrite, so that the refuted reasoning stays visible next to what
replaced it.

**The adversarial code review also approved the fix while wrong on its highest-value question.** It
argued that a false-positive failure "cannot occur" because the API returns runs newest-first. That
holds for a fresh run and fails for a re-run, which keeps its original creation time and can therefore
sit below the midnight break. An executable probe found the false positive the review had reasoned
away. For an intermittent, ordering-dependent defect, execute the scenario; do not argue about it.

**A small log sample reads as clean when the failures interleave.** Four zeros in thirty runs,
bracketed by correct values on the same day, is invisible to a spot check. It only appeared under a
systematic scan of consecutive runs.

## Deliberately out of scope

Each of these was examined and left, for a stated reason. None is a silent skip.

- **Concurrent `main` runs collide on the same version.** `v26.9.21.3` was emitted at both 05:42:03 and
  05:42:08, and `.7` twice. The counter reads how many runs exist rather than reserving a number, so a
  `concurrency:` group does not fix it. **The maintainer has explicitly decided to leave this.**
- **`todayStart` is built with the local-timezone `Date` constructor** rather than a UTC one. A no-op
  on UTC hosted runners; latent only for a non-UTC self-hosted runner. Not approved for this change.
- **Reverting `bd8a2db60`** — the maintainer's opening instinct of "undo whatever changed this" — was
  investigated and priced. That commit also rewrote `ci_changes.yml`, rewired 72 lines of `ci.yml` and
  moved release tagging, with eight months of CI built on top, and its pre-change behaviour was a bare
  `github.run_number`, not calver at all. It is also not the cause. A forward fix was chosen.
- **Mutation testing — not applicable, with a reason.** The project's per-feature mutation strategy
  names Stryker.NET for the backend and StrykerJS for the frontend. Neither is configured for a
  standalone `.mjs` module, and wiring a third runner for roughly sixty lines of CI glue is
  disproportionate to the risk it would retire.
- **Two other floating action references remain** — `actions/github-script@v9` in `ci_changes.yml` and
  `actions/download-artifact@v8` in `ci_sonar_gates.yml`. The same category as the pin that was fixed
  here, and left alone as outside this change.
- **Documentation, screenshots, demo data and website assets — not applicable.** This change has no
  user-facing surface. It alters CI version generation only: no UI, no API, no configurable
  terminology.

## Artifacts

- `docs/feature/fix-build-version-always-zero/rca.md` — the root-cause analysis, carrying its
  superseded banner, the corrected root cause, the live staleness capture and the 60-run log scan
- `docs/feature/fix-build-version-always-zero/deliver/roadmap.json` — four steps in one phase
