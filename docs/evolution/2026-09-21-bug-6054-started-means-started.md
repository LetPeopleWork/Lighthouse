# Bug #6054 — a started Feature showed a start date in the future

**Merged to `main` 2026-09-21, not yet released** · ADO Bug #6054, child of Epic #6033 · workspace
`docs/feature/bug-6054-started-means-started/` · commits `feec2309e..28fea5b40`

## What users get

A Feature that has demonstrably started no longer claims it will start next month. Epic 6033 gave every
Feature a forecast start date; the code that decided whether to show the forecast or the real date asked
"is there a start date recorded?" and read the absence of one as "has not started". So a Feature in
progress without a recorded start day, and a Feature already closed, could both be shown — and written
into the user's work tracking system — starting on a day still in the future.

Three surfaces change:

- **The Feature table** shows a started or finished Feature's real start day, not a forecast.
- **The Delivery timeline** places a finished Feature, and ends its bar on the day it closed.
- **The work tracking system** receives the real start day for a closed Feature, overwriting the stale
  future date left behind while it was open. No configuration change is needed; the first write-back
  round after deploy corrects it.

The rule is now stated positively: **state decides whether work has started; the date only fills in the
value.**

| StateCategory | StartedDate | Before | After |
|---|---|---|---|
| To Do | any | forecast, or unknown if not forecastable | unchanged |
| Doing | set | observed | unchanged |
| Doing | **null** | forecast ← the defect | **observed — the day the Feature was created** |
| Done | set | forecast if rows survived, else unknown | **observed** |
| Done | null | forecast if rows survived, else unknown | **observed — creation day, or nothing if it has none** |
| Unknown (unmapped) | any | forecast | unchanged, and now pinned by a test |

Two acceptance criteria of Epic 6033 are reversed by this — AC-2.3 ("a done Feature shows the same empty
state") and AC-3.4 ("a Feature that is Done writes nothing for a start source") — which is why this was a
work item rather than a quiet correction. Both, plus AC-2.2 and AC-3.3, are amended in the Epic's own
workspace so its record does not keep asserting behaviour the product no longer has.

## Three branches of one missing statement

- **Branch A** — `Feature.WhenWorkBegins` guarded the observed branch conjunctively: `Doing && StartedDate
  is { }`. A null date fell through to the forecast. Three connectors legitimately report Doing with no
  start instant: CSV (an empty cell parses to null), Linear (a Project's `startDate` is nullable and
  user-entered, independent of status), and Azure DevOps (the Doing branch has no `ClosedDate`-style
  fallback, so a renamed or unmapped historical state yields null).
- **Branch B** — `WriteBackTriggerService.ResolveStartValue` short-circuited on Done and returned null,
  and a null resolution is *dropped*, not written as blank. Nothing ever overwrote the last future date
  written while the Feature was open. The guard had been derived by analogy with the completion side and
  the code copied its shape one for one. **The analogy does not hold**: for a finished Feature a
  *completion* forecast is genuinely unanswerable, whereas a *start* is the most certainly known date it
  has.
- **Branch C** — found while writing this up, and it disproved the original write-up's claim that the
  Feature table was already correct for a finished Feature. `WhenWorkBegins` had no Done branch at all; a
  finished Feature was correct only when it happened to hold no start-forecast rows, and whether it holds
  rows depends on **remaining work, never on state** (`ForecastService` selects by team membership and
  filters on `RemainingWorkItems > 0`). A Feature closed with one open child kept its rows and was shown
  a future start. Correctness rested on a coincidence between two subsystems that nothing keeps in step.

## What shipped

| Commit | |
|---|---|
| `77e56f1e1`, `9d73e5a1f` | The state-first rule in `Feature.WhenWorkBegins`, red test first |
| `ad87ed803`, `3028f5eff` | The Done short-circuit deleted from `ResolveStartValue`, red test first |
| `6c32ab13c` | The DTO seam pinned for a closed Feature with open work — the Branch C regression test |
| `0e7740a5d` | A finished Feature pinned as placeable on the Delivery timeline |
| `cf1fccce8`, `cc5a2ea22` | The two reversed criteria amended in Epic 6033's record; a comment rewritten to say its reason rather than cite a criterion |
| `451e3dab3` | A finished Feature's bar ends on the day it finished — review finding 1 |
| `4b911a644` | A started Feature with no recorded start day stands in its creation day — review finding 2 |
| `15e8cf94c`, `0707afd38`, `28fea5b40` | The record corrected for what the reviews found, and the mutation run |

Six insertions and ten deletions of production C# across two methods, plus thirteen lines of TypeScript in
the timeline model. No wire, DTO, schema or migration change: `WhenWorkBegins` is `[NotMapped]`, the DTO
already carried all three provenance values, and no client re-derives the source.

**Deliberately not in the forecast engine.** Excluding finished Features from `InitializeSimulationResults`
would remove rows from the run, free team capacity, and move every other Feature's start *and* completion
date on every screen. The domain-rule fix has no such blast radius.

## Key decisions

**D1 — a finished Feature shows its real start date**, and is therefore *placeable* on the Delivery
timeline, where before it was not. This is the decision that reverses AC-2.3.

**D2 — ship the corrective write-back without staging.** The first round after deploy issues one write per
already-closed Feature with a mapped start field. It settles in a single round: no-op rewrites are
suppressed and accepted values are stored. Accepted risk, taken knowingly: on a large Jira instance that
cannot suppress notifications, every watcher of every affected Feature is mailed once.

**D3 — the residual is documented, not fixed.** A Feature that resolves to nothing still cannot clear a
wrong value already sitting in the work tracking system, because the pipeline deliberately has no "write
blank". Largely superseded by the creation-day fallback, which means there is nearly always a value to
write; what survives is the Feature that has neither a start date nor a creation date.

**D4 — the unmapped `Unknown` state keeps getting a forecast**, the same as To Do, and now has a test, so
it is a decision rather than a leftover.

**Two accepted findings preserved deliberately**, neither closed by this fix and neither closeable by
accident: a Feature moved back to To Do still receives a forecast (which is why the rule names `Doing` and
`Done` positively, rather than keying off the date with state as an exclusion), and start percentiles
remain conditional on the work starting within the horizon.

## The lesson worth carrying: a clean review is evidence of nothing until its citations are checked

The first adversarial review returned **APPROVED with no findings**, and three of the claims it rested that
verdict on were not true of the code:

- it cited the write-back pipeline's null filter at a line number where nothing of the kind exists;
- it said a test exercises the behaviour through real controllers, when that test constructs a DTO directly
  with a fake clock;
- it said a documentation comment explained a point the comment does not mention at all.

A second review, run after being told exactly what the first had fabricated, found **two real regressions
inside the same code** — both introduced by this fix:

**Finding 1 — the bar ended on the wrong day.** D1 makes a finished Feature placeable on the timeline and
says so; nobody worked out where such a bar would *end*. The timeline model took every bar's end from the
completion forecast and read neither `closedDate` nor `stateCategory`, so a Feature closed in April drew a
bar running to today, and one closed with an open child drew a bar running into the *future*. On a Gantt
both read as work still in flight and badly overdue — the same class of untruth this bug was raised to
remove from the start end of the bar, reintroduced at the other end by the fix for it. The clamp sits
*before* the refusal to draw a bar with no end, so a finished Feature whose forecast rows were already
cleared is still drawn.

**Finding 2 — `NotKnown` was the right third answer, taken too far.** The rule as first written returned
nothing for a started Feature with no recorded start day. That dropped those Features off the timeline
under the heading "No forecast for when work on this begins", which is untrue of a Feature demonstrably in
progress; it is reachable in bulk, because a work tracking system with an empty started-date column maps
*every* in-progress item to exactly that shape; and it made Lighthouse contradict itself within one
write-back round, since work item age already falls back to the creation date, so the same Feature could
read "in progress 40 days" on one surface while leaving "starts 14 October" standing in the tracker. The
rule is now `StartedDate ?? CreatedDate`, reported as observed, resolving to nothing only when both are
null — which is not a new idea here: `WorkItemBase` already uses that fallback three times over.

Both reviews read the same diff. The difference was that the second one was told the first had invented its
evidence.

## Mutation testing

| Stack | Score | Tested | Killed | Survived | Wall clock |
|---|---|---|---|---|---|
| Backend (Stryker.NET) | **90.38 %** | 156 | 141 | 11 | 3 m 15 s |
| Frontend (StrykerJS) | **98.48 %** | 66 | 65 | 1 | 30 s |

The headline is not the interesting number. Stryker.NET ignores line ranges — a `mutate` entry always
widens to the whole file — so two large files were mutated entire and the score is diluted by code this bug
never touched. **Every mutant inside the two changed methods was killed**, and every survivor and
no-coverage mutant sits outside their line spans. The one frontend survivor is provably equivalent (both
arms of an `ArrayDeclaration` mutation resolve to `undefined` through the same `find`); it is recorded so it
is not re-litigated on a later run. Per-survivor reasoning:
`docs/feature/bug-6054-started-means-started/mutation/results.md`.

## Verified before hand-off

- `dotnet test` — 7218 passed, live-connector categories excluded
- `pnpm test` — 5472 passed; `tsc -b` and `vite build` clean; Biome clean on `./src`
- `des-verify-integrity` — exit 0 on all ten steps across the four phases

## Architecture record

`docs/product/architecture/brief.md` **needed no change.** Its Epic 6033 section states that one rule
answers "when did this Feature start", that the rule lives on `Feature` carrying its own provenance, and
that both the DTO and the write-back resolver read it. All three remain true — this bug changed the rule's
*content*, not its location, its shape or its consumers, and it introduced no component.

[ADR-201](../product/architecture/adr-201-observed-start-supersedes-the-forecast-in-the-domain.md) **did**
need one, and carries a dated amendment: its decision §1 named `Doing` as the sole predicate and §4 endorsed
the write-back resolver's refusal to write anything for a finished Feature. Both are what this bug reverses.

## Still open

- **No user documentation says a completed Feature is now drawn on the Delivery timeline, or where its bar
  ends.** That is new prose for a docs pass, not a correction to existing text.
- **The timeline's axis window is a known, accepted edge.** The axis spans the minimum and maximum over all
  bars drawn, and a portfolio keeps its finished Features for a year by default, so a Feature closed eleven
  months ago still widens the window and coarsens the axis for bars that are actually live. Clamping the bar
  to the close day narrows this but cannot remove it, because the bar's *start* is still eleven months back.
  The obvious answer is a hide-completed toggle; the user declined it. Recorded so it is not rediscovered as
  a defect.
- **Three review findings were deliberately not actioned, and deliberately not filed as work items.**
  Recorded here so they are not re-reported as new:
  - the per-team start rows the DTO carries still hold unfixed forecast dates — the Feature-level value was
    corrected, the per-team ones were not. Nothing renders those rows yet, which is the only reason this is
    invisible; whatever first renders them will need the same correction;
  - the Feature table and the Delivery timeline apply the observed-start guard in opposite orders, so for one
    Feature they can disagree about which date is shown. Pre-existing rather than introduced here, though
    this fix widens the set of Features for which the two orders can diverge;
  - the demo Delivery screenshot may change the next time it is regenerated, because finished Features are
    now drawn on the timeline. Not regenerated as part of this fix.
- **The fixture blind spot that produced the bug is worth naming.** Every Done fixture in the backend suite
  had `StartedDate = null`, so the one combination that discriminates the whole defect — Done *with* a start
  date — was exercised nowhere. It is now.

## Artifacts

- `docs/feature/bug-6054-started-means-started/feature-delta.md` — root cause per branch, the fix per site,
  decisions D1–D4, test impact, and the amendment recording what the second review changed
- `docs/feature/bug-6054-started-means-started/deliver/roadmap.json` — ten steps across four phases
- `docs/feature/bug-6054-started-means-started/mutation/` — `results.md` and the three Stryker configs
- `docs/feature/epic-6033-forecasted-start-dates/feature-delta.md` — the parent Epic's record, amended for
  the four criteria this fix changed
