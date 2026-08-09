# Mutation testing — Epic #5687 (Faster Updates), slice 01: the update log signal

Run 2026-08-09 against `main` @ `0fcde79c8`. Gate is 80 % kill rate.

| stack | score | tested | killed | survived | no coverage | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 4.16.0) | **63.28 %** | 289 | 193 | 96 | 16 | 0 | 10 m 30 s |
| Frontend (StrykerJS) | **N/A** | — | — | — | — | — | — |

Frontend is N/A because slice 01 changed **zero** frontend files: the whole slice is a backend log
signal plus three persisted columns. `git diff --name-only 7f371a278..HEAD` lists nothing under
`Lighthouse.Frontend/`. Recorded rather than omitted, so the next reader does not have to re-derive it.

Config: `stryker.5724.backend.json`, run from `Lighthouse.Backend.Tests/`.

Score denominator is 305, not 289: Stryker.NET counts the 16 `NoCoverage` mutants against the score
but reports "tested" as killed + survived. `193 / (193 + 96 + 16) = 63.28 %`.

**The 80 % gate is not met on the whole-file score, and this report does not argue that it should be
waived on the strength of the headline.** The number that describes slice 01 is the one below.

## The number that describes this slice

Cross-referencing every mutant's line against `git diff -U0 7f371a278` (production files only):

| surface | mutants | killed | survived | score |
| --- | --- | --- | --- | --- |
| Lines slice 01 changed | 10 | 10 | 0 | **100 %** |
| Lines slice 01 did not change | 295 | 183 | 96 | 62.0 % |

Every one of the 96 survivors is in code this slice did not write. That is not a claim that the
slice is well tested by virtue of being small — it is a claim that the two numbers measure different
things, and that mixing them is what produces 63.28 %.

Before triage the changed surface was 48 mutants, 8 killed, 40 survived. All 40 survivors were the
same shape: a `logger.LogDebug` line whose only change in this slice was the word `LogInformation`
becoming `LogDebug`. Stryker has no mutator for a log **level**; what it mutated was the
**pre-existing message text**, which entered new-code scope only because the line was edited. Those
40 are now annotated (below) and the changed surface reads 10 / 10.

## Backend, per file

| file | tested | killed | survived | no coverage | score |
| --- | --- | --- | --- | --- | --- |
| WorkItemService.cs | 213 | 143 | 70 | 9 | 64.4 % |
| UpdateServiceBase.cs | 34 | 19 | 15 | 0 | 55.9 % |
| PortfolioUpdater.cs | 25 | 18 | 7 | 3 | 64.3 % |
| TeamUpdater.cs | 15 | 12 | 3 | 4 | 63.2 % |
| TeamDataService.cs | 2 | 1 | 1 | 0 | 50.0 % |
| SyncOutcome.cs | 0 | — | — | — | n/a |

`Program.cs` appears in the report with 726 ignored and 16 compile-error mutants and no tested ones.
That is the known top-level-statements leak, not a scope failure — see the configuration note below.

### `SyncOutcome.cs` produces no mutants at all

The slice's one genuinely behavioural change is `return SyncOutcome.FullSync(recordsFromTracker.Count)`
at `WorkItemService.cs:113` and `:584`. Stryker generates exactly one mutant at each — `Count()` →
`Sum()` — and both are **CompileError**. `SyncOutcome.cs` itself is a record with two
expression-bodied members and generates **zero** mutants: Stryker.NET has no numeric-literal mutator,
so `FullSync(0)` is untouched, and an expression-bodied member has no block to remove.

That surface is therefore **unmutable, not untested**. What pins it is the acceptance suite:
`scanned=2 / fetched=2` on the team path, `scanned=3 / fetched=3` on the portfolio path, and the
persisted `RefreshLog` row asserted through a fresh read
(`ThenTheRecordedUpdateReportsAFullUpdateOf`). Mutation testing has nothing to say here, in either
direction, and a reader should not read the absence of mutants as an absence of coverage.

## Closed by this pass

No tests were written. Two mutants were nonetheless converted from unmeasured to killed, as a
second-order effect of the annotations:

- **`WorkItemService.cs:414`** — block removal of `if (portfolio.OwningTeam != null) { … }`
- **`WorkItemService.cs:496`** — block removal of `if (historicalFeatureSize.Any()) { … }`

Both were `Ignored` in the first run with the reason *"Removed by block already covered filter"*:
Stryker suppresses a block-removal mutant when every statement inside the block already carries its
own mutant. Disabling the log statement's mutants broke that redundancy, so each block-removal mutant
became live — and the existing suite killed both. Removing the owning-team assignment and removing
the percentile computation are both real behaviour changes, and both were previously invisible to the
score. Net effect of the triage on kills: **+2** (191 → 193), which is the reason the totals do not
simply drop by 40.

This is worth carrying forward: **annotating a diagnostic line can unmask a behavioural mutant that
the "block already covered" filter was hiding.** The filter is a de-duplication heuristic, not a
judgement about behaviour.

## Accepted survivors

### On lines slice 01 changed — 40, all annotated in code

Each is a `// Stryker disable once all:` on the line above, carrying its own reason. The shared
premise is stated once here and not repeated 19 times: the mutation targets a **pre-existing message
template** on a line whose only change was its log level, and Stryker cannot mutate a level. The
per-line reasons say what specifically makes *that* line's text non-load-bearing.

| file:line (post-annotation) | what the reason records |
| --- | --- |
| `WorkItemService.cs:45` | completion is now said by the `Update completed` summary line, whose text *is* pinned |
| `WorkItemService.cs:62` | the team half of the same trace |
| `WorkItemService.cs:70` | one of the three copies of the announcement AC-1.5 caps at one; counted by level, not wording |
| `WorkItemService.cs:331`, `:341` | entry / exit trace of the remaining-work pass; the pass's own statements are mutated separately and killed |
| `WorkItemService.cs:388`, `:393`, `:401` | the per-record narration AC-1.6 is about — see below |
| `WorkItemService.cs:416`, `:426` | traces of the owning-team and feature-owner branches; both guards are mutated on their own and killed |
| `WorkItemService.cs:431`, `:493` | includes the `string.Join` separator: the joined string is built for the message and read nowhere else |
| `WorkItemService.cs:453` | narrates `AddOrUpdateWorkForTeam`, whose removal is killed |
| `WorkItemService.cs:479`, `:500` | the percentile trace; the guard above and the computed value are pinned elsewhere |
| `PortfolioUpdater.cs:35`, `TeamUpdater.cs:88` | AC-1.4's skip trace — pinned by level (`Slice01SkippedEntityLogTest`), never by wording |
| `TeamDataService.cs:20`, `:28` | entry / exit trace; what the pass reports is the `SyncOutcome` it returns |

Block form (`// Stryker disable all` … `// Stryker restore all`) was considered and rejected: **no
two of these lines are contiguous.** Every candidate span encloses real behaviour — `:394`
(`IsUsingDefaultFeatureSize = true`), `:428` (`featureOwners = …`), the statements between the
remaining-work entry and exit traces. A block would have silenced those too, which is the difference
between accepting a survivor and hiding one. Nineteen single-line disables is the honest form here.

Two reason strings (`PortfolioUpdater.cs:34`, `TeamUpdater.cs:87` — the comment lines themselves) were reworded after the run — the
first draft credited `Slice01SkippedEntityLogTest` with pinning the portfolio half, which it does not:
it drives `TeamUpdater` only. Reason text is metadata; the mutant set and the score are unaffected.

### The AC-1.6 positive control is correctly scoped — verdict, with the evidence

`ThenThePerRecordDetailIsStillAvailableToWhoeverAsksForIt` matches
`PerRecordNarration = ["Extrapolating", "Items to Feature"]` with `.Any(…)`. Because it is an OR
across fragments, emptying any single template leaves the others, so `:388`, `:393` and `:401` all
survive. **This is not a weakness worth closing**, for three reasons:

1. **AC-1.6's subject is a category, not a sentence.** The criterion is "per-record chatter is demoted,
   never dropped". Pinning each of the three messages verbatim would convert a control into a prose
   assertion — precisely what the specification file's own header disclaims ("the prose around them is
   free to improve without redding a scenario") and what this repo's
   `// Stryker disable … diagnostic log text is not behaviour` convention exists to reject.
2. **The control demonstrably observes real per-record output, not the pass-entry line.** The concern
   worth checking was that `"Extrapolating"` also matches `:388`, which fires unconditionally *outside*
   `foreach (var feature in portfolio.GetFeaturesToExtrapolate())` — so the control could in principle
   be satisfied with zero records narrated. The mutation data settles it: the `:388` template mutant
   **survived**, meaning scenario 6 still found narration at Debug with that line emptied. Only the
   loop-interior lines can have supplied it. The control is exercising the loop.
3. **Contrast with the AC-1.4 control, which is single-fragment and does kill.**
   `ThenTheCheckIsStillRecordedAtDebug` asserts one fragment, `"Checking last update"`, emitted by one
   line once — so both its mutants die and `UpdateServiceBase.cs:122` needed no annotation. That is the
   right shape *there*, because AC-1.4 is a promise about that specific line. The shapes differ because
   the criteria differ, not because one control was written less carefully.

If a future run wants one of the three killed, changing `.Any(…)` to require every fragment would kill
`:401` only — `"Items to Feature"` is unique to it, while `"Extrapolating"` appears on two lines that
cover for each other. That buys one mutant in exchange for a scenario that reds on a reword. Recorded
as an option, not recommended; and in any case it is `nw-acceptance-designer`'s call, not the
crafter's.

### On lines slice 01 did not change — 96, not annotated, not tested

Deliberately left standing. They exist because Stryker.NET takes **whole-file** `mutate` globs and
silently ignores line ranges (`Foo.cs{72..94}` matches nothing and warns about nothing), so scoping to
a changed region is impossible on this stack. Writing tests for them would be scope creep into code
five prior epics wrote; annotating them would be manufacturing a score.

| file | survivors not on changed lines |
| --- | --- |
| WorkItemService.cs | 70 |
| UpdateServiceBase.cs | 15 |
| PortfolioUpdater.cs | 7 |
| TeamUpdater.cs | 3 |
| TeamDataService.cs | 1 |

Four are worth naming, because they are real gaps rather than log text, and a later slice in this epic
will be working next to all four:

- **`TeamDataService.cs:23`** — deleting `await teamMetricsService.UpdateTeamMetrics(team);` survives.
  A team refresh that never recomputes its metrics is indistinguishable to the suite.
- **`WorkItemService.cs:582`** — deleting `await SweepDepartedFeatureSpells(portfolio, features);`
  survives. Blocked-spell sweeping on departed features has no test that fails without it.
- **`PortfolioUpdater.cs:37` / `TeamUpdater.cs:90`** — `minutesSinceLastUpdate >= RefreshAfter` mutated
  to `>` survives on both halves. The "due exactly now" boundary is untested, on the very comparison
  slice 01 demoted the log line of.
- **`UpdateServiceBase.cs`** — 15 survivors across the untouched parts of the base class, the largest
  single concentration outside `WorkItemService`.

None of these is slice 01's to fix. They are recorded here so that the slice which next touches
`UpdateServiceBase` or `TeamDataService` — slice 02 changes both — inherits the list rather than
rediscovering it.

## Not mutated

**Six production files slice 01 changed are absent from `mutate`, each deliberately.**

| file | change | why not mutated |
| --- | --- | --- |
| `UpdateQueueService.cs` | 2 × `LogInformation` → `LogDebug` | would add whole-file mutants of untouched queueing code to buy 4 log-text mutants that would be annotated on arrival |
| `BaseMetricsService.cs` | 1 × log demotion | same, over a 950-line file |
| `DeliveryMetricSnapshotRecordingHandler.cs` | 1 × log demotion | same |
| `AzureDevOpsWorkTrackingConnector.cs` | 1 × log demotion | same |
| `JiraWorkTrackingConnector.cs` | 1 × log demotion | same |
| `ITeamDataService.cs`, `IWorkItemService.cs` | return type `Task` → `Task<SyncOutcome>` | interface declarations generate no mutants |

The log-level demotions are not unpinned by their absence: AC-1.7's "at most two operator-visible
lines" fails if any of them regresses to `Information`, and no Stryker mutator can change a log level
in the first place.

**`RefreshLog.cs` and `SyncMode.cs` are also absent, and are additionally outside the diff base.**
Both landed in `7f371a278` — the DISTILL commit that is the base for every changed-line figure in this
report — alongside the two `AddRefreshLogModeAndRecordCounts` migrations. So the three persisted
fields and the `SyncMode` enum are production code of this slice that the changed-surface analysis
does not see. They are auto-properties and an enum: zero mutants either way, and pinned by
`ThenTheRecordedUpdateReportsAFullUpdateOf` reading the row back. Recorded because the base commit is
not the clean pre-implementation line it looks like.

## Configuration: `WorkItemService.cs` stays in

`WorkItemService.cs` is 711 lines (696 before this pass), 24 of which slice 01 changed, and it contributed 252 of the first
run's 343 mutants — it dominates the score by an order of magnitude. Excluding it would lift the
headline to roughly 78 % without a single test being written. **Recommendation: do not exclude it.**

1. **It holds 15 of the 19 lines under triage.** Excluding the file to improve the score would remove
   from view the exact lines the triage is about. The report would then describe everything except the
   subject.
2. **Its 70 remaining survivors are information.** They are correctly labelled pre-existing above, and
   two of them (`:582`, and the `Ignored`-turned-`Killed` blocks) only became visible because the file
   was in scope.
3. **Slices 02–08 all change this file** — the entire epic is about how `recordsFromTracker` is
   fetched. A baseline established now is the thing later runs are read against; establishing it by
   omission would mean re-establishing it later.
4. **Excluding the dominant file to reach a threshold is the failure mode this document exists to
   avoid.** An honest 63 % with a written changed-surface figure of 100 % is worth more than a
   manufactured 78 %.

The config is committed unchanged at `stryker.5724.backend.json`.

## Configuration notes

- **`14119 total mutants are skipped` / `289 total mutants will be tested`** is the line that proves
  scope. The created-count printed earlier covers the whole project — Stryker.NET injects everywhere,
  *then* filters, *then* compiles — so `Program.cs`'s 726 ignored mutants and the compile-error
  warnings on unrelated files are normal output of a working run, not a broken filter. Confirm scope
  from `reports/mutation-report.json`, never from the created-count.
- **Line-span `mutate` entries do not work on Stryker.NET** and fail silently. Whole-file entries plus
  line-by-line triage of the report is the only reliable method here. (StrykerJS does honour spans;
  this run has no frontend half.)
- **`test-case-filter` is load-bearing.** Without it the initial run executes the whole suite under
  `perTestInIsolation` — roughly 11 minutes before the first mutant. The filter in this config admits
  the slice-01 fixtures plus the updater, `TeamDataService`, `WorkItemService`, `UpdateQueueService`
  and `QuietWriteBack` suites.

## Verification after the run

- `dotnet build Lighthouse.sln` — succeeded, **0 warnings**, 0 errors.
- `dotnet test Lighthouse.sln` — **4690 passed, 0 failed, 0 skipped**.
- `dotnet format analyzers Lighthouse.sln --severity info --verify-no-changes` — exit 2 with 35
  findings, **none in any file this pass touched**; all 35 are the known pre-existing CA1861 hits in
  generated EF migration files.
