# RESUME — story-6053 DELIVER

## WHERE THIS STANDS — 2026-09-24 (read this first; supersedes the sections below)

**HOLD for the user's live check.** 03-05, 04-01 and 04-02 are done. Nothing is pushed. Nothing after
04-02 has run: not 04-03 (docs), not the L1-L6 refactor, not the adversarial review, not Stryker (04-04).
Do not start any of them until the user has checked the behaviour and says so.

| Commit | What |
|---|---|
| `5bb43d95d` | 03-05, both closing Slice07 scenarios un-ignored, no production change |
| `50c94d39a` | U-44/U-45 recorded |
| `a331f82ed` | Both fidelity scenarios (Slice05 + Slice07) re-armed by the acceptance designer; self-enforcing Givens |
| `7d3d9962e` | 04-01, 90-day cap moved into the filler (counts worked-out days); reconciler hands over the whole gap |
| `33b93519e` | U-46/U-47 recorded |
| `db646c934` | 04-02, one empty-state sentence on both widgets; range-end rule removed |
| `86b8148c0` | E2E page objects/spec expect the new sentence; 8/8 over-time specs run green locally on :5269 |

Suites at this point: backend **7298 passed / 0 failed / 1 skipped** (standard filter); frontend
**5694 passed** (4 fewer than before: the range-end-rule tests were deleted with the rule).

The sentence: "Nothing to show for the selected range. Days the stored history covers can fill in on a
later visit; days it does not cover stay empty."

### Open, for after the live check
- **04-03 docs** must also correct ADR-207 D4 and `brief.md` on the fill order (U-46), and the two stale
  "forward-only placeholder" comments in `PbcOverTime.spec.ts:50` and `Screenshots.spec.ts:716`.
- **Adversarial review:** U-45 (ceiling-refused days re-asked on every read — do NOT memoise them naively),
  U-46 (predates-everything scenarios cannot catch a floor regression; arm with an item in progress since
  before the period), U-6/U-28 (cost observables).
- Analyzer sweep `dotnet format analyzers --severity info --verify-no-changes` was denied to a subagent;
  the user runs it before push.
- Delivery charts and Blocked Items Over Time still carry their own "builds forward from today" copy.
  Not this story's charts; check whether it is still true there.
- Live dogfood (all process-behaviour families at both scopes, fresh owner vs pre-floor period) is the
  user's check — `dogfood-phase-03.md` records it as not performed.
- `db646c934` lacks the Co-Authored-By/Claude-Session trailers (des-commit wrote only Step-Id/Task-Id).
  Cosmetic; not amended.

Written 2026-09-22 at the phase-01 boundary, before a deliberate session compaction. Everything needed
to pick this up cold. Nothing here is recoverable from the commits alone.

**PHASE 01 IS COMPLETE.** Resume at step 02-01.

## State

10 commits on `main`, **all unpushed** (user asked to hold before push for review).

| Commit | What |
|---|---|
| `129838d11` | DISCUSS |
| `90be4fd11` | SPIKE-01 |
| `b2b77c301` | DESIGN (ADR-207, ADR-208, three amendments) |
| `13c0a6e84` | DEVOPS |
| `a283a2790` | DISTILL — 40 ATs, 1 green / 41 pending |
| `e2e448a7c` | 01-01 percentile writer extracted |
| `0e6a89dc2` | 01-02 decorator + reconciler + filler |
| `0021bb488` | 01-03 per-day collision absorption |
| `b0bf87247` | AT gap closure (foreign writer takes a day) |
| `d9644f65b` | 01-04 maintenance-gate coupling, both directions |
| `7887bbbf0` | 01-05 floor, absence gate, memo |
| `060c331ac` | 01-06 forward recorder stops writing zero rows |
| `e60a54c89` | 01-07 ninety-day cap + enforced wall-clock budget |
| `0a22adcdd` | 01-08 fidelity + dogfood — **no production change needed** |

Suite at last green: **7268 passed, 0 failed**, 26 skipped (all in slices 06-08; Slice05 has none).

`Slice05ReconstructCycleTimeHistoryScenarios.cs`: **19 methods, all green, zero `[Ignore]`.** The
roadmap says 17 — it is wrong, the fixture holds 19.

**A merge commit sits in the history** (`5ecf6353b`, 2026-09-22 12:20), bringing two SLE-risk commits
from `origin/main` into this branch. They are the user's own work, already pushed, and touch no file
this story edits. It was not deliberate on my part; it appeared during a crafter's git activity. It
was NOT rebased away, and must not be without the user saying so.

## What phase 01 still owes (none of it blocks phase 02)

1. **AT gap, open (U-6)**: the memo's convergence claim has no test. `Looking_at_the_same_period_twice`
   converges via `daysAlreadyHeld`, not the memo — delete the memo and everything still passes. Needs
   `nw-acceptance-designer` to author "a second visit over a stretch the absence gate refused enqueues
   nothing". It is a **cost** observation (enqueue count), not a state observation.
2. **U-4, for the adversarial review**: the maintenance gate's dependency on the filler is optional
   (`IOverTimeHistoryFillActivity? historyFill = null`). Behaviour is right (registered at
   `Program.cs:1360`) but deleting that line silently reopens the bug with nothing failing.
3. **Demo synthesiser still declares its own horizon lists** (`DemoPercentilesBackfillHandler.cs:33,35`).
   Contained by an ArchUnit allowlist pinned to two files; folding it in needs that file in scope.

## How to dispatch a step — the recipe that works

Template source: `~/.claude/skills/nw-execute/SKILL.md` lines 43-190. Use it verbatim; the DES hook
validates the prompt **before** the agent starts and rejects abbreviated ones.

Required header, all four lines:

```
<!-- DES-VALIDATION : required -->
<!-- DES-PROJECT-ID : story-6053-reconstruct-over-time-history -->
<!-- DES-PROJECT-ROOT : /storage/repos/Lighthouse -->
<!-- DES-STEP-ID : NN-NN -->
```

Agent is `nw-software-crafter` (project CLAUDE.md declares object-oriented). Step context comes from
`/tmp/.../scratchpad/step.py NN-NN` — or just read `deliver/roadmap.json` directly.

For a **non-step** dispatch that happens to mention step ids (a review, an AT-authoring task), the hook
will block it unless the prompt carries `<!-- DES-ENFORCEMENT : exempt -->`.

## Gotchas that cost time if rediscovered

- **`roadmap.json` `files_to_modify` paths are short by one directory level.** They say
  `Lighthouse.Backend/Services/…`; the truth is `Lighthouse.Backend/Lighthouse.Backend/Services/…`.
  Every file exists; an agent trusting the literal path gets "not found". Correct them in the dispatch.
- **`des-commit --owned-paths` nargs trap**: pass every path to ONE flag, space-separated. Repeating the
  flag silently keeps only the last path and commits one file.
- **`python3 -c` and heredoc-to-python3 are both blocked.** Write a script file and run it.
- **`execution-log.json` cannot be read with native Read or Bash** (hook + deny rule). Use `ctx_read`.
  Its schema uses short keys: `sid`, `p`, `s`, `d`, `t` — not `step_id`/`phase`.
- **Test filter**, always: `--filter "TestCategory!=Integration&TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration"`.
  Unfiltered runs hit real Jira/ADO/Linear/ServiceNow and rate-limit CI.
- Occasional single failures in `DeletePortfolio_*` / `TeamInProject_*` are **contention** — they pass
  when re-run alone. Not regressions.
- `Program.cs` edits force the full backend Integration suite in CI. Expected, recurring, not a problem.

## Traps in the code that fail SILENTLY — do not "simplify" these

1. **The filler must consult `IsMaintenanceOperationActive`, never `IsBlocked`.** After 01-04 `IsBlocked`
   includes "a history fill is in flight", so a filler reading it sees *itself*, decides maintenance is
   active, and abandons every pass on its first day — with every test still green.
2. **The per-day maintenance/budget check `break`s, never `return`s**, so the `finally` still runs
   `InvalidateReadCache()` for days already written.
3. **A day abandoned by the budget must NOT enter the memo** as "already worked out" — it was never
   attempted, and memoising it makes it permanently unfillable. Faults are handled the same way: the
   memo write sits inside the `try`, after `SaveFilledDay`.
4. **Refusal granularity is per (day, horizon)**, not per day. Assertions read horizon 30, so a
   horizon-90-only row is invisible to them.
5. **`ArchUnit` pins `"void FillDayIfAbsent("` as source text.** Changing that signature reds a test that
   looks unrelated. It is why the memo stores "days worked out" rather than "days refused".

## Two things that did NOT pass, recorded honestly

- **KPI "no row has all four percentiles zero" FAILS as literally worded.** 7 such rows exist on the
  dogfood instance, all belonging to deleted owner id 2, all written by the pre-story recorder.
  Reconstruction never rewrites, so nothing here removes them. The delta is clean: 36 -> 464 rows,
  none of the 428 added is all-zero. The KPI should have said "no NEW all-zero row". The 7 belong to
  the orphaned-snapshot bug logged at DISCUSS.
- **Fidelity across a CONFIGURATION CHANGE has no probe anywhere** and is still open. Step 01-08
  proves the mechanism only: same code path, clock moved back and forward, byte-equal. It says nothing
  about a reconstructed day matching a watched one when the state mapping or cycle-time definition has
  since changed. Do not let a green suite be recorded as closing this. It belongs in 04-03's docs.

## WHERE THIS STANDS — end of 2026-09-22 (read this first)

**Stopped after 03-04 at the user's request.** Resume at **03-05**.

Suite at last green: **7287 passed, 0 failed, 11 skipped, total 7298** (backend, standard filter).
Frontend: **5698 passed, 392 files**. Both are the baselines to beat.

Slice07 carries **2 remaining `[Ignore(Pending)]`** - the fidelity scenario and the last-observed
(ceiling) scenario, both belonging to 03-05.

### What phase 03 cost, and what it bought

03-04 took a crafter halt, two acceptance-designer passes and four commits before a line of production
code was written, because **its own designated acceptance cover could not fail**. The scenarios seeded
the product's default retention window, which is wide enough that today and the day being rebuilt reach
the same verdict. Implementing against that would have produced a green step, a satisfied criterion 3,
and a shipped bug.

That makes **nine** scenarios in this story found to assert something unfalsifiable: U-16, U-19, U-22,
U-26, U-28, U-33, U-39 and the pair at U-41. The first seven were caught by sabotaging production code;
the last two by modelling the fixture against `XmRCalculator` and noticing the arrangement made the
outcome inevitable. **The second route is cheaper and catches a class the first cannot** - sabotage
proves a test *can* fail, never that it fails *for the reason its name claims*.

### The defences that now exist, and must not be removed

1. **A self-enforcing arrangement.** The two pinned-stretch scenarios call a Given that reads both
   settings back off the owner and fails if they ever stop disagreeing. Put the default retention window
   back and they fail **in the Given**, before reaching the product. This is the first thing in the
   story that defends against the vacuous-assertion pattern structurally rather than by someone noticing.
2. **`AChartThatIsNotReady_ReportsNoCentreAndNoUpperLimit`** in `OverTimeReconstructionSeamArchUnitTest`.
   Pins the invariant the status gate's redundancy rests on, and pins the **count** of construction
   sites (6) as well as their content - a scan that silently finds nothing reports no offenders forever,
   the same failure shape as the defect this story fixes.
3. **Two seeders, both load-bearing.** The flat one (one item a day) is what
   `A_stretch_in_which_the_team_finished_nothing...` needs for its collapsed band; the varying one
   (quiet weekends, one more item each month) is what makes a pinned stretch and a rolling one give
   different answers. **Unifying them silently disarms the collapsed-band cover.** The note saying so
   sits where someone would make that change.

### The trap worth carrying to other stories

**A periodic fixture whose period divides the analysis window produces a constant statistic, and
therefore an assertion about change that cannot fail.** `3 + (day.DayNumber % 5)` over a 30-day window
holds six whole cycles, so the sum never moves. Caught by modelling before writing. A pure trend fails
differently: the moving range collapses and the band has no width at all.

### Open, not blocking 03-05

- **U-40(b) the chart cache key** - eleven process-behaviour keys are `$"...Chart_{start}_{end}"` with
  no anchor. 03-04 was told to fix it and pin it; confirm it did.
- **U-42** - the two assertions in `ThenTheLimitsHoldSteadyAcross` fail to two *different* edits.
  Re-anchoring the window does NOT move the limits: with a pin, the band comes from the pinned stretch
  alone. Both forms are in the scenario's docstring.
- **U-27 risk, live at 03-05**: `ThenTheLimitsStopOn` is used by a scenario seeding only finished items
  ending at the break - the identical vacuous shape U-26 disproved. **Check whether anything would be
  written past the ceiling at all before trusting it.**
- **My dogfood, still outstanding**: all four percentile tabs plus every process-behaviour family at
  both scopes on the restored dev database. Deferred to the live check.
- `MEMORY.md` compaction, hook-requested.

## A pull landed mid-step on 2026-09-22 — read this before trusting any commit around `f43eef492`

`origin/main` was pulled into this branch while step 03-03's crafter was running. The crafter reached
its commit step before the two conflicts were resolved, and `des-commit` committed the half-finished
merge: commit `6cf949717` recorded `origin/main` as merged while its tree carried **none** of the eight
incoming commits. Pushing it would have deleted twenty-four files from the remote with no warning,
because `git merge-base --is-ancestor` was satisfied.

It was reset away and the merge redone deliberately as **`f43eef492`**, keeping both sides of all three
append-versus-append conflicts (the feature list and the job registry in `docs/product/jobs.yaml`, and
one `## Application Architecture` section in `docs/product/architecture/brief.md`). Three tags mark the
reachable points and should be left in place until the story is pushed:

| Tag | Points at |
|---|---|
| `rescue/pre-pull-local` | `49dd9f3d5` — the local tip before the pull |
| `rescue/origin-tip` | `b07ce5d95` — everything the pull brought down |
| `rescue/bad-merge` | `6cf949717` — the merge that merged nothing |

**Step 03-03 is unstarted.** The crafter was stopped before reporting any of its verifications, so
nothing it found is trustworthy, and the two-line un-skip it had committed went with the reset.

The full account is U-37 in `feature-delta.md`. The operational rule it produced: **`TaskStop` every
running agent the moment a pull, merge or rebase is mentioned**, before looking at anything else. The
window is seconds wide and nothing tells the agent the repository moved. A merge that merged nothing
looks entirely normal in `git log`; the tell is `git diff --stat <sha>^1 <sha>` being implausibly small.

## STANDING INSTRUCTION — HOLD AFTER 04-02, and a separate stop after 03-04

**2026-09-22, end of day: "Stop once next step is done, we wrap up for today."** The session stopped
after 03-04. That is a stopping point, not the hold - the hold below is still the standing instruction
for when work resumes.

## STANDING INSTRUCTION — HOLD AFTER 04-02 (revised 2026-09-22)

The user asked to **stop after step 04-03** so they can check the behaviour in the live view before
anything else runs. Two separate holds, both still in force:

1. **HOLD BEFORE PUSH** — nothing is pushed until the user reviews. Unchanged since the start.
2. **HOLD BEFORE refactor / adversarial review / mutation.** Step **04-04 IS the mutation step**
   ("Freeze it, mutate it, and check nothing was left pending"), so the hold point is **after 04-03**,
   not after phase 04.

Run: 02-04 → 03-01…03-05 → 04-01 → 04-02, then **STOP**.
Do NOT run: **04-03 (docs)**, 04-04, the L1-L6 refactor pass, the adversarial review, Stryker.

**Revision (the user's words): "go on and run through everything including 04-02. then I wanna verify
(docs and gates after)."** The hold moved one step earlier: 04-03 is the documentation step, and docs
wait for the user's confirmation in their own environment - the standing rule, now applied to the whole
step rather than only to its screenshots. So 04-02 (the frontend widget test) is the last step before
the hold, and 04-03 runs afterwards together with the remaining gates.

When 04-03 does run, it still splits in two: the correctness prose can be written from the code, but the
Playwright screenshot regeneration needs a live instance with a premium licence - the very instance the
user is looking at during the hold. Regenerating them is a separate decision from writing the prose.

## Remaining plan

Phase 02 (4 steps) → phase 03 (5) → phase 04 steps 01-02, then **HOLD for the user's live check**.
Afterwards, and only on their word: **refactor (L1-L6) → adversarial review → mutation (Stryker, ≥80%,
step 04-04)**. Mutation runs **LAST on frozen code** — any edit afterwards shifts line ranges and
invalidates the score. Then DES integrity verification, then finalize **without pushing**.

Full per-step detail is in `deliver/roadmap.json`. Every finding so far is in `feature-delta.md` under
`## Wave: DELIVER / [WHY] Upstream issues found while implementing` (U-1…U-9) and
`## Wave: DELIVER / [REF] Roadmap gaps resolved before dispatch` (R-1…R-3).
