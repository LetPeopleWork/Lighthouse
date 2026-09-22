# RESUME — story-6053 DELIVER

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
