# RCA — ADO Bug #6068 "Build Version wrong" (calver build counter is 0)

Date: 2026-09-22 · Analyst: Rex (nw-troubleshooter) · Method: Toyota 5 Whys, multi-branch
Repo: `LetPeopleWork/Lighthouse` · Status: investigation only, no files changed

---

> **[SUPERSEDED 2026-09-22 — read this first]** Section 1's scope correction below is **wrong**,
> and Branch A is **not** the reported defect. Verified against the maintainer's counterexample and
> a 60-run log scan. The reported bug is an **intermittent stale-response defect that does affect
> `push` runs on `main`**, documented in section 0. Branch A (PR refs) is a real but separate,
> deterministic, non-shipping defect. Sections 2, 5, 6 and 7 reason from the refuted scope; treat
> their conclusions as applying to Branch A only. Branches B and C stand.

## 0. CORRECTED root cause (supersedes section 1)

### The refutation

Run `35648229410` is `event=push`, `head_branch=main`, created `2026-09-21T19:59:18Z`, and it logged
`Found 0 builds today` -> `v26.9.21.0`. It is the run pasted into the work item (filed 20:07:07Z,
8 minutes later). `main` is therefore **not** out of scope.

### The real pattern — intermittent, on `main`, and recent

Version-job logs for 60 consecutive `main` runs (`gh api .../actions/jobs/<id>/logs`):

| Window | Runs | `Found 0` |
|---|---|---|
| 2026-09-19 -> 2026-09-22 | 30 | **4** (Sept 21 at 19:59, 17:16, 10:11; Sept 20 at 14:34) |
| 2026-07-23 -> 2026-08-03 | 30 | **0** |

The zeros interleave with correct counts *on the same day*: Sept 21 emitted `0` at 19:59 while
12:51 had already counted 11 and 20:19 counted 14. Sept 20's `0` at 14:34 sits between `5` at 13:40
and `7` at 15:56. No code changed between those runs. Rate is roughly 1 in 7. The clean July-August
window confirms the maintainer's "this worked until a few days ago" — the defect is recent, not
8 months old.

### Mechanism: GitHub serves a stale page, and the loop trusts it

`github.rest.actions.listWorkflowRuns` intermittently returns a stale snapshot. Observed live, same
query repeated:

```
call 1       total_count=982
call 2       total_count=1443   workflow_runs[0].created_at = 2026-09-05T09:15:31Z   <-- 16 days stale
calls 3..27  total_count=2379   workflow_runs[0].created_at = 2026-09-22T05:42:30Z   <-- correct
```

Pagination does not stitch either: page 1 ended at 2026-09-19, page 2 resumed at 2026-08-03,
dropping six weeks.

`ci_version.yml:66-69` breaks on the first run older than midnight:

```js
if (runDate < todayStart) { hasMore = false; break; }
```

On a stale page the newest element already predates `todayStart`, so the loop exits on the first
iteration with `buildCount === 0`. The script cannot distinguish "no builds today" from "stale
snapshot" — which is exactly root cause B, here promoted from contributing factor to the **proximate
cause of the reported defect**.

### ROOT CAUSE (corrected)

The counter derives from an API listing that is **not read-your-writes consistent**, and the script
treats an empty-or-stale page as an authoritative zero. It never asserts the one invariant it can:
the current run is itself a `ci.yml` run created today, so it **must** appear in the result set.
`context.runId` appears zero times in the file.

### Consequence for section 10's blast radius

Section 10 concludes no released artifact can carry `.0` because publishing jobs are `main`-guarded.
The guards are real (`ci_docker.yml:16`, `ci_release.yml:14`, all three standalone packagers) — but
`main` is precisely what is failing, so they do not contain this defect. **A `.0` on `main` can reach
a release.** Severity is higher than section 10 states.

### Corrected fix

Retry `listWorkflowRuns` with backoff until the page contains `context.runId`; count only then; if it
never appears, `core.setFailed` rather than emit a wrong version. Fix 1 (branch fallback) is still
required — without it PR runs would never observe their own run and would fail the new check. Fix 2's
`buildCount < 1` guard is subsumed by the run-visible check. Fix 3 (root cause C, duplicate versions —
confirmed again here: `.3` at 05:42:03 and 05:42:08, `.7` at 05:49:44 and 05:50:54) stays a separate
work item.

---


## 1. Scoped problem statement

On **`pull_request` runs of `.github/workflows/ci.yml`**, the version job in
`.github/workflows/ci_version.yml` logs `Found 0 builds today` and emits
`version=v<YY>.<M>.<D>.0` / `fileversion=<YY>.<M>.<D>.0`. Every PR run on a given day emits the
identical string.

Explicitly **out of scope / not affected**: `push` runs on `main` and on `features/**`, and
`workflow_dispatch`. Those count correctly and never emit `.0`.

### Scope correction to the reported symptom

The bug text says the counter "is now **always** 0". It is not. Evidence, all from 2026-09-21:

| Trigger | Runs that day | Counter emitted |
|---|---|---|
| `pull_request` | 12 | **0 in 12 / 12 runs** |
| `push` (main) | 14 | 1, 3, 3, 4, 5, 7, 7, 8, 9, … — **never 0** |

Per-run evidence (`gh api repos/LetPeopleWork/Lighthouse/actions/jobs/<jobId>/logs`):

```
PR run 35566860031 -> Found 0 builds today     (12/12 PR runs identical)
PR run 35565713309 -> Found 0 builds today
...
push run 35650376781  2026-09-21T20:19  -> Found 14 builds today / v26.9.21.14
push run 35565500823  2026-09-21T05:41  -> Found  1 builds today / v26.9.21.1
push run 35691318875  2026-09-22T05:35  -> Found  1 builds today / v26.9.22.1
```

The pasted log in the work item (`26.9.21`, `Found 0 builds today`, `v26.9.21.0`) is therefore a
**pull_request** run — it matches run `35566860031` (2026-09-21T06:04:16Z) line for line.

---

## 2. Five Whys — Branch A (the reported defect)

**WHY 1A — The version job emits `Found 0 builds today` on PR runs.**
Evidence: job log of run `35566860031`, 2026-09-21T06:04:17Z:
`Found 0 builds today` → `Generated version: v26.9.21.0`. Reproduced in 12/12 PR runs that day.

**WHY 2A — Because `listWorkflowRuns` returns an empty page on the first iteration, so the loop
breaks at `.github/workflows/ci_version.yml:53-56` with `buildCount` still 0.**
Evidence: the script's only other exit that yields 0 (`runDate < todayStart` on the first item,
line 60) is impossible here — the same API with `branch=main` returns runs created that same day.
Direct API probe:

```
GET /repos/LetPeopleWork/Lighthouse/actions/workflows/ci.yml/runs?branch=refs/pull/1777/merge
  -> total_count: 0
GET .../runs?branch=main                        -> total_count: 2379
GET .../runs?branch=dependabot/npm_and_yarn/... -> total_count: 2      (PR head branch — matches!)
GET .../runs (no branch filter)                 -> total_count: 3894
```

**WHY 3A — Because the `branch` argument passed to the API is the literal string
`refs/pull/1777/merge`, which is not a branch name and matches nothing.**
Evidence: `.github/workflows/ci_version.yml:40`

```js
const branch = context.ref.replace('refs/heads/', '');
```

On a `pull_request` event `context.ref` is `refs/pull/<n>/merge`, so the `replace` is a no-op.
Confirmed *directly* from the same run's log, where a sibling job echoes the ref verbatim:

```
Version / version   Uses: .../ci_version.yml@refs/pull/1777/merge (4deecd9cc…)
Detect Changes      if [ "refs/pull/1777/merge" == "refs/heads/main" ]; then
```

Note the `branch` API filter matches on **`head_branch`**, which for a PR run is the PR's *source*
branch (`dependabot/npm_and_yarn/…`, `total_count: 2` above) — never the merge ref.

**WHY 4A — Because the script was written on the assumption that `context.ref` is always
`refs/heads/<branch>`, i.e. that the workflow only ever runs on a push.**
Evidence: that assumption is false for this workflow —
`.github/workflows/ci.yml:21-22` declares `pull_request: branches: [main]`, and that trigger
predates the version script (present already at `bd8a2db60^`, traceable back to `f74bc4b5e`). No
code path in `ci_version.yml` branches on `github.event_name`; the whole file (85 lines) contains
no `event_name`, no `pull_request`, and no fallback.

**WHY 5A — ROOT CAUSE A: commit `bd8a2db60` ("Only tag when releasing", 2026-01-09) replaced
`ci_tag.yml` with `ci_version.yml` and dropped the explicit non-`main` code path that the old
workflow had.**
Evidence — `git show bd8a2db60^:.github/workflows/ci_tag.yml`:

```yaml
- name: Set version based on branch
  run: |
    if [ "${{ github.ref }}" == "refs/heads/main" ]; then
      echo "create_tag=true" >> $GITHUB_ENV
    else
      echo "Not on main branch, setting version to current build number"
      echo "version=${{ github.run_number }}" >> $GITHUB_ENV   # <-- never 0, never absent
      echo "create_tag=false" >> $GITHUB_ENV
    fi
```

The predecessor handled "not on main" deliberately and gave it a non-zero, always-defined number.
`ci_version.yml` has no equivalent. `git log --diff-filter=A -- .github/workflows/ci_version.yml`
returns exactly `bd8a2db60`, and `git log -S"context.ref.replace" -- .github/workflows/ci_version.yml`
also returns exactly `bd8a2db60` — the faulty line has been there since the file was born.

---

## 3. Five Whys — Branch B (contributing: the failure is silent)

**WHY 1B — Nothing in CI noticed for eight months.** Evidence: the bug was filed 2026-09-21 against
a line introduced 2026-01-09; 12 PR runs a day sail through green.

**WHY 2B — Because a `buildCount` of 0 is indistinguishable from a legitimate result.** Evidence:
`ci_version.yml:76-84` formats and `core.setOutput`s whatever `buildCount` holds. There is no
validation, no `core.warning`, no `core.setFailed`, no non-zero assertion.

**WHY 3B — Because the script never checks the one invariant it can always assert: the run
executing the script is itself a `ci.yml` run created today, so the count can never be < 1.**
Evidence: `context.runId` is available in `github-script` and is never referenced in the file
(85 lines, zero occurrences). The loop at `ci_version.yml:57-68` counts by date only.

**WHY 4B — Because the design treats an empty API page as "end of data" rather than as a
possible malformed query.** Evidence: `ci_version.yml:53-56` — `length === 0 → hasMore = false;
break`. That is the correct pagination terminator *and* the silent-zero path; the two cases are
conflated at the same line.

**WHY 5B — ROOT CAUSE B: the version logic is untestable inline JavaScript with no assertion
layer.** Evidence: `find .github -name "*.js" -o -name "*.mjs" -o -name "package.json"` returns
**nothing**. Two workflows (`ci_version.yml`, `ci_changes.yml`) carry inline `github-script` blocks;
neither has a harness, and there is no `actionlint`/`action-validator` anywhere in `.github/`.
A logic error here can only ever be discovered by reading a CI log by eye.

---

## 4. Five Whys — Branch C (contributing: the counter is not unique even where it works)

**WHY 1C — Two different `main` runs on the same day were assigned the same version.**
Evidence, 2026-09-21:

```
push run 35565527231 created=05:42:03Z -> Generated version: v26.9.21.3
push run 35565532201 created=05:42:08Z -> Generated version: v26.9.21.3   <-- duplicate
push run 35566012631 created=05:49:44Z -> Generated version: v26.9.21.7
push run 35566085776 created=05:50:54Z -> Generated version: v26.9.21.7   <-- duplicate
```

The day's sequence is `1, 3, 3, 4, 5, 7, 7, 8, 9` — neither unique nor gap-free.

**WHY 2C — Because the "counter" is the number of sibling runs that *exist* at query time, a
value every concurrent run reads identically.** Evidence: `ci_version.yml:57-67` counts
`run.created_at >= todayStart` over *all* runs on the branch, including runs created after the
current one. Two runs created 5 s apart both observe the same three runs and both compute 3.

**WHY 3C — Because reading a shared count is not the same as reserving a number, and nothing
serializes or claims.** Evidence: `grep -n concurrency .github/workflows/ci_version.yml
.github/workflows/ci.yml` → no matches. The predecessor `ci_tag.yml` had
`concurrency: group: tag-${{ github.workflow }}-${{ github.ref }}, cancel-in-progress: false`
and obtained its number by *creating a git tag* (`fregante/daily-version-action`) — a reservation,
not a read.

**WHY 4C — Because the redesign swapped a claim-based counter (git tag high-water mark under a
concurrency lock) for a read-based counter without recognising they are not equivalent.**
Evidence: the `concurrency:` block and the `daily-version-action` step both exist at
`bd8a2db60^:.github/workflows/ci_tag.yml` and neither survives into `ci_version.yml`.

**WHY 5C — ROOT CAUSE C: same commit `bd8a2db60`. The version identifier lost its uniqueness
guarantee at the same moment it lost its non-`main` code path.**
Latent, not yet realised, because `ci_release.yml` sits behind the `Release` deployment
environment gate and only one run per day is ever approved — evidence: `gh release list` shows one
release per release-day (`v26.9.19.10`, `v26.9.9.9`, `v26.9.1.6`), and the remote carries no
duplicate tags. The first day two runs are both approved, `git tag -a v26.9.21.3` in
`ci_release.yml:34` fails on the second.

---

## 5. Branch D — hypotheses tested and REFUTED

These were the leading suspects. All are cleared, with evidence.

**D1. The dependabot bumps (`actions/github-script` 7→8 `a5f867f6f` 2026-01-12, 8→9 `9dda5e4dd`
2026-04-14; `actions/checkout` bumps) broke the API call. — REFUTED.**
Decisive: run `35650376781` (push to main, 2026-09-21T20:19) executes the *same file, same
`actions/github-script@v9` (SHA `3a2844b7e9c422d3c10d287c895573f7108da1b3`), same
`actions/checkout@3d3c42e5*` and logs `Found 14 builds today / v26.9.21.14`. A broken action
cannot produce 14 on one trigger and 0 on another four hours earlier. The bumps are innocent.
`git show` of both bump commits shows a one-line `uses:` change and nothing else.

*Second, independent refutation — the bug predates every bump.* Build artifacts outlive job logs,
so the version string is still readable from PR runs going back to January
(`gh api repos/.../actions/runs/<id>/artifacts`):

```
PR run 20909510543  2026-01-12T05:56:43Z -> Lighthouse-win-x64-v26.1.12.0
PR run 20909515085  2026-01-12T05:56:59Z -> Lighthouse-win-x64-v26.1.12.0
PR run 20909520010  2026-01-12T05:57:15Z -> Lighthouse-win-x64-v26.1.12.0
  ^ all three ran BEFORE commit a5f867f6f (github-script 7->8) landed at 2026-01-12T06:00:51Z,
    i.e. on actions/github-script@v7 — and already emitted .0
PR run 21579013844  2026-02-02 -> v26.2.2.0      PR run 22563017013  2026-03-02 -> v26.3.2.0
PR run 23861501040  2026-04-01 -> v26.4.1.0      PR run 28148869847  2026-06-25 -> v26.6.25.0
PR run 28544789280  2026-07-01 -> v26.7.1.0      PR run 35566860031  2026-09-21 -> v26.9.21.0
```

2026-01-12 carries the earliest PR run that exists after `bd8a2db60` merged on 2026-01-09 (no PR
ran on the 9th, 10th or 11th — `gh api …&event=pull_request&created=2026-01-09|10|11` is empty).
So the defect has been present on **every PR run since the file was created**, unbroken, across
github-script v7, v8 and v9 and four `actions/checkout` bumps. Nothing "suddenly" changed.

**D2. `todayStart` uses the local-time `Date` constructor
(`ci_version.yml:33`) and mis-computes midnight. — REFUTED (confirmed, not assumed).**
Every run log prints `Checking runs since: 2026-09-2?T00:00:00.000Z` — exact UTC midnight, correct
for the date in the same log. `ubuntu-latest` runners are UTC (`Image: ubuntu-24.04`, run log).
*Latent* only: the line would break on a non-UTC self-hosted runner. Low priority, worth fixing
opportunistically to `Date.UTC(...)`, but it is not this bug.

**D3. `workflow_id: 'ci.yml'` (filename) resolves differently from the numeric id. — REFUTED.**
`GET /actions/workflows` reports `{id: 103960909, path: ".github/workflows/ci.yml", state: "active"}`,
and the filename query returns `total_count: 3894`. Both forms resolve.

**D4. `permissions: actions: read` is insufficient / the token is scoped wrong. — REFUTED.**
The run log's `GITHUB_TOKEN Permissions` group shows `Actions: read, Contents: read, Metadata: read`
in *both* the working push run and the failing PR run — identical. A permission failure would throw
(HTTP 403), not return an empty list; no error appears in the log.

**D5. API eventual-consistency race — the current run not yet indexed when queried. — REFUTED.**
The first `main` run of three separate days each counted exactly 1 (`35565500823` → 1,
`35691318875` → 1, `35502172828` → 1), i.e. the current run is always already visible ~10 s in.

**D6. The failed run `35650376781` is the same bug. — REFUTED.** Its only failed job is
`sonar-gates / sonar-gates`; its Version job succeeded with `v26.9.21.14`.

---

## 6. Cross-validation

**Backwards chain, A:** if `context.ref` on a PR is `refs/pull/N/merge` and the API filters by
`head_branch`, then the query matches zero runs → first page empty → loop breaks at line 55 →
`buildCount` 0 → `v<date>.0`, identically for every PR that day. That is exactly the observed
12/12. PASS

**Backwards chain, B:** if nothing asserts `buildCount >= 1`, a zero propagates to
`core.setOutput` and onward to 20 consuming job inputs with no gate. Observed: PR artifacts named
`Lighthouse-win-x64-v26.9.21.0`, `Lighthouse-SBOM-v26.9.21.0` on runs `35566860031` and
`35565713309`. PASS

**Backwards chain, C:** if the count is a shared read, runs created within one polling window get
equal values and the sequence skips. Observed: `3, 3` and `7, 7`; `2` and `6` missing. PASS

**Consistency:** A, B and C are the same commit (`bd8a2db60`) and do not contradict. A explains the
reported symptom; B explains the eight-month latency; C explains a related, still-latent defect on
the path A does not touch. D1-D6 are refuted without weakening A-C.

**Completeness:** all three ci.yml triggers accounted for — `pull_request` (broken, A),
`push` (works but non-unique, C), `workflow_dispatch` (ref is `refs/heads/<branch>`, behaves as
push). No unexplained symptom remains.

---

## 7. Answer to "pls undo whatever changed this"

The instinct is right — a commit did change this — but it is **not** a dependabot bump and it is
**not** recent. It is `bd8a2db60`, 2026-01-09, "Only tag when releasing", which deleted
`ci_tag.yml` and introduced `ci_version.yml`.

**Reverting is not recommended.** `bd8a2db60` also rewrote `ci_changes.yml`, rewired `ci.yml`
(72 lines), and moved release tagging into `ci_release.yml`; eight months of CI work sits on top of
it. And the pre-`bd8a2db60` behaviour on a PR was `version = github.run_number` — a bare integer
like `4207`, not calver at all. A revert would trade a cosmetic PR-only wrong number for a large,
risky CI rewrite and a *different* version scheme. The forward fix is 4 lines.

---

## 8. Proposed fix

### Fix 1 (root cause A) — derive the branch from the event, not from `ref`

`.github/workflows/ci_version.yml:40`

```diff
-            const branch = context.ref.replace('refs/heads/', '');
+            // On a pull_request, context.ref is refs/pull/<n>/merge — not a branch, so the
+            // workflow-runs API matches nothing and every PR build would be numbered 0.
+            // The API filters on head_branch, which for a PR run is the source branch.
+            const branch = context.payload.pull_request?.head?.ref
+              ?? context.ref.replace('refs/heads/', '');
```

Effect on a PR: counts `ci.yml` runs on the PR's own head branch today, including the current one →
always >= 1, and it increments across re-runs/pushes to that PR, which is the intended semantics.

Equivalent alternative, if a payload-shape dependency is disliked: read the standard env var the
runner already sets for `pull_request` events —
`const branch = process.env.GITHUB_HEAD_REF || context.ref.replace('refs/heads/', '')`
(`GITHUB_HEAD_REF` is empty on push, set to the source branch on a PR). Pick one; do not add both.

### Fix 2 (root cause B) — make the silent zero impossible

after `.github/workflows/ci_version.yml:75` (the `while` loop), before the `Found …` log:

```diff
+            // The run executing this script is itself a ci.yml run created today, so it must be
+            // in the set we just counted. A zero means the query was wrong, not that no builds ran.
+            if (buildCount < 1) {
+              core.setFailed(`Counted ${buildCount} ci.yml runs today on branch '${branch}', but this run must be one of them. The branch filter matched nothing (context.ref='${context.ref}').`);
+              return;
+            }
```

**Trade-off, stated explicitly.** `core.setFailed` turns a wrong version into a red build. The
softer alternative is `core.warning(...)` plus `buildCount = 1`, which keeps CI green and never
emits `.0`. Recommendation: **`setFailed`**, because Fix 1 makes a zero genuinely impossible, so any
future zero is a new defect that should be loud — and a wrong version shipping silently is what
caused this bug. If the maintainer prefers never to red a build on a version glitch, the
warn-and-floor variant is a one-word change.

### Fix 3 (root cause C) — OPTIONAL, separate change, needs a decision

Uniqueness on `main` is a real defect but a different scope and a different risk profile. Do **not**
bundle it. Options, in ascending cost:
- (a) add `concurrency: { group: version-${{ github.ref }}, cancel-in-progress: false }` to the
  `version` job — **insufficient on its own**: serialising the *query* does not help when the value
  read is "runs that exist", which both runs see identically. Documented here so it is not tried.
- (b) append `-${{ github.run_id }}` / use `github.run_number` as the 4th segment — unique and
  monotone, but abandons "Nth build of the day", which is the maintainer's stated intent.
- (c) reserve the number instead of reading it (the old `daily-version-action` tag approach, or a
  lightweight `gh api` tag-create retry loop).

This is a redesign of the versioning scheme and must not be decided unilaterally. Raise it as a
separate work item.

### Fix 4 (opportunistic, D2) — `todayStart` in UTC

`.github/workflows/ci_version.yml:33`

```diff
-            const todayStart = new Date(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate());
+            const todayStart = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()));
```

No behaviour change on GitHub-hosted runners (verified UTC); removes a latent trap. Zero risk.

---

## 9. Can a regression test be written?

**Not against the current code — the logic must be extracted first.** Evidence:
`find .github -name '*.js' -o -name '*.mjs' -o -name 'package.json'` returns nothing; the logic is
an inline YAML string, unreachable by any runner.

Recommended, minimal, and genuinely testable:

1. Extract the counting logic to `.github/scripts/generate-version.mjs`, exporting a pure function
   that takes an injected client and context:
   `export async function generateVersion({ listRuns, ref, pullRequestHeadRef, now, runId })`
   returning `{ version, fileversion, buildCount }`.
2. `ci_version.yml` keeps `actions/github-script` and becomes a thin adapter:
   ```yaml
   script: |
     const { generateVersion } = await import(`${process.env.GITHUB_WORKSPACE}/.github/scripts/generate-version.mjs`);
     const result = await generateVersion({ /* github.rest…, context, new Date() */ });
     core.setOutput('version', result.version);
     core.setOutput('fileversion', result.fileversion);
   ```
   (the job already does `actions/checkout` at line 19, so the file is on disk).
3. Tests with `node:test` + `node:assert` in `.github/scripts/generate-version.test.mjs`, run by a
   tiny job in `ci.yml` (or folded into `ci_frontend.yml`; `node` is already available).

**The test that must fail before the fix and pass after:**

> *given* `ref = 'refs/pull/1777/merge'` and `pullRequestHeadRef = 'dependabot/x'`, *and* a stub
> `listRuns` that returns runs only when asked for `branch === 'dependabot/x'` (an empty array for
> anything else — exactly what the real API does), *then* `buildCount >= 1` and `version` does not
> end in `.0`.

Against today's code the stub is queried with `'refs/pull/1777/merge'`, returns `[]`, and the
assertion fails. With Fix 1 it is queried with `'dependabot/x'` and passes. That is a true RED→GREEN
regression test, not a tautology.

Companion cases worth the same harness, all cheap: push-to-main counts the current run (→ 1 on the
first run of the day); an empty result set raises rather than returning 0 (Fix 2); the date boundary
is computed in UTC (Fix 4).

**Cost/benefit:** the extraction is ~60 lines of movement plus a ~40-line test file, and it also
opens the door for `ci_changes.yml`'s inline block later. Without it there is no regression test at
all and the next phase's gate cannot be met. Recommend doing it.

---

## 10. Risk assessment

**Risk of the change itself: LOW.** Fix 1 is strictly wider — it only changes behaviour on the
`pull_request` path, which today produces a constant wrong value. The `push` path is bit-for-bit
untouched (`context.payload.pull_request` is `undefined` on a push, so the `??` falls through to
today's expression).

**What a wrong fix would break — mapped to actual consumers**
(`grep -rn "needs.version.outputs\|inputs.fileversion" .github/`):

| Consumer | File:line | Exposure |
|---|---|---|
| Release tag `git tag -a <version>` | `ci_release.yml:34-35` | **Highest.** A non-unique or regressed version makes the tag push fail, or worse, silently reuses a released tag. Guarded by `if: github.ref == 'refs/heads/main'` **and** the `Release` environment gate. |
| Docker image tag + `latest` + cosign | `ci_docker.yml:66-69`, `ci_release.yml:47-49,59-66` | **High.** A collision overwrites a pushed `ghcr.io/…:<fileversion>` image and re-signs a different digest under the same tag. Guarded by `if: github.ref == 'refs/heads/main' && github.actor != 'dependabot[bot]'` (`ci_docker.yml:16`, `ci_release.yml:14`). |
| Assembly version `-p:Version=<fileversion>` | `.github/actions/build-backend/action.yml:38,48-51` via `ci_packageapp.yml:31-35` | **Medium — and this is the only PR-reachable consumer.** `ci_packageapp.yml` has **no** `if:` guard (verified: the whole file, 35 lines, contains no `github.ref` condition), so every PR compiles the backend with `-p:Version=26.9.21.0`. `<M>.<D>.0` is a *valid* 4-part assembly version, so it compiles and is packaged silently — no build-time guard exists anywhere. |
| Standalone packages (Tauri win/linux/macos), `FULL_VERSION` | `ci_package-{win,linux,macos}-standalone.yml:92/109/109` | Low. Feeds installer metadata and the in-app "current version" the update check compares against the release feed — but all three are guarded: `if: github.ref == 'refs/heads/main' && github.actor != 'dependabot[bot]'` at `ci_package-linux-standalone.yml:16`, `ci_package-win-standalone.yml:17`, `ci_package-macos-standalone.yml:17`. They never see a `.0`. |
| Artifact names `Lighthouse-*-<version>` | `ci_release.yml:70-80`, package workflows | Low. Artifacts are scoped per run, so same-name artifacts in different runs do **not** collide. Verified: runs `35566860031` and `35565713309` both hold `Lighthouse-win-x64-v26.9.21.0` independently. |
| SBOM artifact / in-app SBOM page | `ci.yml:93`, `ci_sbom.yml`, `ci_docker.yml:33` | Low. `sbom-version` is already deliberately blanked off `main` (`ci.yml:93`) — the author already knew version is main-specific. |

**Blast radius of the bug as it stands, today: contained.** No released artifact can carry `.0`,
because every publishing job (`docker`, `release`) is gated on `refs/heads/main`, and `main` runs
always count >= 1. Confirmed: `gh release list` shows no `.0` release; remote tags carry none.
The damage is limited to PR-only build artifacts and to the maintainer's (correct) loss of trust in
the version number.

**Risk of doing nothing: LOW-MEDIUM and rising.** Root cause C is a live latent fault: the first day
two `main` runs are both approved through the `Release` gate, `ci_release.yml:34` fails on a
duplicate tag mid-release, after the Docker image has already been overwritten.

**Priority:** Fix 1 + Fix 2 = **P1** (root-cause fix for the reported defect, current sprint).
Fix 4 = P2, free rider. Fix 3 / root cause C = **P1 as a separate work item** — it is the one that
can actually corrupt a release.

---

## 11. Root cause → solution map

| Root cause | Solution | Type | Priority |
|---|---|---|---|
| **A** — `bd8a2db60` dropped the non-`main` branch path; `context.ref.replace` leaves a merge ref (`ci_version.yml:40`) | Fix 1 — derive branch from `context.payload.pull_request.head.ref` with the ref as fallback | Permanent fix | P1 |
| **B** — no assertion distinguishes "0 builds" from "filter matched nothing" (`ci_version.yml:53-56`, 76-84) | Fix 2 — assert `buildCount >= 1` and fail loudly, citing `context.ref` | Permanent fix + early detection | P1 |
| **B'** — inline JS is untestable; no harness exists under `.github/` | Extract to `.github/scripts/generate-version.mjs` + `node:test` suite wired into CI | Prevention | P1 (prerequisite for the regression test) |
| **C** — counter is a shared read, not a reservation; `concurrency` lock and tag-based claim lost in `bd8a2db60` | Separate work item; options (b)/(c) in §8 — needs a maintainer decision on the scheme | Permanent fix | P1, separate item |
| **D2** — local-time `Date` constructor for the UTC day boundary (`ci_version.yml:33`) | Fix 4 — `Date.UTC(...)` | Prevention | P2 |

---

## 12. Files affected

- `/storage/repos/Lighthouse/.github/workflows/ci_version.yml` — lines 33, 40, and an insert after 75 (**the fix**)
- `/storage/repos/Lighthouse/.github/scripts/generate-version.mjs` — **new**, extracted logic
- `/storage/repos/Lighthouse/.github/scripts/generate-version.test.mjs` — **new**, regression test
- `/storage/repos/Lighthouse/.github/workflows/ci.yml` — new job (or extend an existing one) to run the node test

Read-only during this investigation; nothing was modified.
