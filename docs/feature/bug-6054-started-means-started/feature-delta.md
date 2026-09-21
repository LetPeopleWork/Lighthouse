# Bug #6054 — A finished or in-progress Feature can show a start date in the future

**Parent**: Epic 6033 "Sync Forecasted Start Dates with the Work Tracking System"
**Workspace of record for the Epic**: `docs/feature/epic-6033-forecasted-start-dates/`
**ADO**: Bug #6054 (Active)

This bug corrects behaviour Epic 6033 shipped in slices 01–04. It **reverses two acceptance criteria**
of that Epic — AC-2.3 and AC-3.4 — which is why it is a work item rather than a quiet correction.

---

## Wave: BUGFIX / Root Cause

One statement is missing in three places: **state decides whether work has started; the date only
fills in the value.** The code instead asks "is there a `StartedDate`?" and reads its absence as "has
not started", falling back to a forecast that contradicts what the work tracking system says.

### Branch A — an in-progress Feature without a recorded start date is given a forecast

`Feature.WhenWorkBegins` guards the observed branch conjunctively:

`Lighthouse.Backend/Lighthouse.Backend/Models/Feature.cs:122` — `StateCategory == Doing && StartedDate is { } startedOn`

A null date drops through to the forecast branch at `:127-137` rather than to `NotKnown`. Three
connectors legitimately report Doing with no start instant:

- CSV — an empty *Started Date* cell parses to null with no fallback, while the state still maps to
  Doing. `CsvWorkTrackingConnector.cs:211`, `:218`, `:236`; `ParseDateTime` at `:362-372`.
- Linear — a Project's `startDate` is nullable and user-entered, independent of its status.
  `LinearResponses.cs:93` → `LinearWorkTrackingConnector.cs:441`, state mapped separately at `:425-436`.
- Azure DevOps — the Doing branch has no fallback, unlike the Done branch which falls back to
  `ClosedDate`. A renamed or unmapped historical raw state yields null.
  `AzureDevOpsWorkTrackingConnector.cs:1031-1034` vs `:1036-1039`.

The design considered only two answers — the date, or the forecast — and chose the forecast. The
reasoning is recorded in the existing test's own documentation at `FeatureWhenWorkBeginsTest.cs:37-41`:
*"would otherwise report an observed start of nothing at all, which reads worse than the forecast it
replaced."* `NotKnown`, which the same property already returns at `Feature.cs:129` and `:136`, was
not considered as the third answer.

### Branch B — a completed Feature keeps a stale future forecast in the work tracking system

`ResolveStartValue` short-circuits on Done and returns null
(`WriteBackTriggerService.cs:305-308`). A null resolution is **dropped, not written as blank** — the
pipeline has no way to say "clear this field" (`:211`, policy stated at `:252-261` and `:323-325`).
So nothing ever overwrites the last future date written while the Feature was open.

AC-3.4 was derived by analogy with the completion side, and the code copies the shape one for one
(completion guard at `:289-292` ↔ start guard at `:305-308`). **The analogy does not hold.** For a
Done Feature a *completion* forecast is genuinely unanswerable, whereas a *start* is the single most
certainly known date the Feature has. The symmetry was copied at the level of code shape rather than
of the question each source answers.

### Branch C — a Done Feature can show a future start on Lighthouse's own surfaces

Not in the original write-up, and it disproves that write-up's claim that the Feature table is already
correct for a Done Feature. `WhenWorkBegins` has no Done branch at all; a Done Feature is correct only
when it happens to hold no `StartForecast` rows, and whether it holds rows depends on **remaining
work, never on state**:

- `ForecastService.cs:76` selects the Features to forecast by team membership only —
  `featureRepository.GetAll().Where(f => f.Teams.Any(teams.Contains))`. No state filter.
- `InitializeSimulationResults` filters on `RemainingWorkItems > 0` and never on `StateCategory`
  (`ForecastService.cs:326`).
- Remaining work is the count of child items whose category is not Done (`WorkItemService.cs:610`).
  Nothing forces a Done Feature's children to be Done.

A Feature closed with one open child therefore keeps its rows, the run records a start for it, and the
Feature table and the Delivery timeline both show it a future start. Correctness for the Done case was
emergent, resting on a coincidence between two subsystems nothing keeps in step.

The sub-claim that `SetStartForecasts` clears rows when the run admits none **is** correct
(`ForecastService.cs:291`), as is the note that `GetFeaturesToExtrapolate` excludes Done
(`Portfolio.cs:47-50`), which is why a Done Feature with no children is safe. The gap is specifically
the Done Feature with an open child.

---

## Wave: BUGFIX / The Fix

Two sites. Site 1 alone does not fix Branch B, because the resolver returns before it ever reads the
property.

**Site 1 — `Feature.cs:122`**, inside `WhenWorkBegins`. State-first, and **stated positively**:

```csharp
if (StateCategory is StateCategories.Doing or StateCategories.Done)
{
    return StartedDate is { } startedOn ? FeatureStart.On(startedOn) : FeatureStart.NotKnown;
}
```

Lines `:127-137` are unchanged. The documentation comment at `:109-116` states the old rule and must
be rewritten with it.

> **Implementation guard.** Do not write this as `StartedDate is { } && StateCategory != ToDo` or any
> other formulation that keys off the date with state as an exclusion. Any such wording closes the
> accepted "moved back to ToDo" finding by accident. Name `Doing` and `Done` positively.

**Site 2 — `WriteBackTriggerService.cs:305-308`.** Delete the Done short-circuit in
`ResolveStartValue`. The switch at `:312-326` then handles every case as written: `Observed` →
`:318-319`, `Unknown` → the `_ => null` at `:325`. **The completion guard at `:289-292` stays** — that
one is still right, and is covered by `WriteBackTriggerServiceTest.cs:326-350`.

**Not in the forecast engine.** Excluding Done Features from `InitializeSimulationResults` would remove
rows from the run, free team capacity, and move every other Feature's start *and* completion dates on
every screen. The domain-rule fix has no such blast radius.

### What the rule evaluates to

| StateCategory | StartedDate | Today | After |
|---|---|---|---|
| ToDo | any | `Forecast`, or `Unknown` if not forecastable | unchanged |
| Doing | set | `Observed` | unchanged |
| Doing | **null** | `Forecast` ← the defect | **`Unknown`** |
| Done | set | `Forecast` if rows survive, else `Unknown` | **`Observed`** |
| Done | null | `Forecast` if rows survive, else `Unknown` | **`Unknown`** |
| Unknown | any | `Forecast` | unchanged, and now pinned by a test |

No wire, DTO, schema or migration change — `WhenWorkBegins` is `[NotMapped]` (`Feature.cs:117`), the
DTO already carries all three sources (`FeatureStartDto.cs:17-34`). **No frontend change**: every
client reads `source` and never re-derives it (`ForecastedStartCell.tsx:47`,
`deliveryTimelineModel.ts:118-119`, `Feature.ts:54-60`). Exactly two production callers of
`WhenWorkBegins`: `FeatureDto.cs:39` and `WriteBackTriggerService.cs:310`.

---

## Wave: BUGFIX / Decisions

- **D1 — A Done Feature shows its real start date** on the Feature table and the Delivery timeline, not
  an empty cell. **This reverses AC-2.3** of Epic 6033, which said a done Feature shows the same empty
  state the completion column uses. Consequence: a Done Feature becomes *placeable* on the timeline,
  drawing a bar from its real start to today, where today it is unplaceable. No test covers that
  either way at present; one is owed.
- **D2 — Ship the corrective write-back without staging.** The first round after deploy issues one
  write per already-closed Feature with a mapped `ForecastedStart*` field, replacing the stale future
  date with the real start day. It settles in a single round: `GetChangedFields` suppresses no-op
  rewrites (`WriteBackService.cs:170-202`) and `PersistWrittenValues` stores the accepted value
  (`:241-265`). The Azure DevOps backfill is near-total because `StartedAndClosedDateFrom` falls back to
  `ClosedDate` (`AzureDevOpsWorkTrackingConnector.cs:1036-1039`). Accepted risk: on a large Jira
  instance that cannot suppress notifications, every watcher of every affected Feature is mailed once.
- **D3 — The residual is documented, not fixed.** A Done or Doing Feature with **no** `StartedDate`
  resolves to null, and null is dropped, so a wrong value already sitting in the work tracking system
  is never corrected. The fix stops the wrong write; it cannot clear a wrong value. Closing that gap
  needs a "write blank" capability the pipeline deliberately does not have. Recorded here and on the
  work item, because the bug title promises more than the fix delivers.
  **Superseded in part by `4b911a644`** — see Finding 2 below. The creation-day fallback means there is
  nearly always a value to write, so the stale value does get overwritten; what survives of this decision
  is the Feature that has neither a start date nor a creation date.
- **D4 — `StateCategories.Unknown` keeps getting a forecast**, the same as ToDo, and gains a test so it
  is a decision rather than a leftover. It is the default (`WorkItemBase.cs:33`), is returned for any
  unmapped state (`WorkTrackingSystemOptionsOwner.cs:78-96`), and is set on Linear initiative parents
  (`LinearWorkTrackingConnector.cs:189`).

### Acceptance criteria reversed in Epic 6033

Both are amended in `docs/feature/epic-6033-forecasted-start-dates/feature-delta.md` as part of this
fix, so the Epic's own record does not keep asserting behaviour the product no longer has.

- **AC-2.3** (`feature-delta.md:403-404`) — "A Feature with no start forecast (no throughput, or done)
  shows the same empty state" → a done Feature with a known start shows that start.
- **AC-3.4** (`feature-delta.md:439-440`) — "A Feature that is Done writes nothing for a start source"
  → a Done Feature writes its observed start date, and writes nothing only when it has none.

### Accepted findings preserved, deliberately

Both are described in the Epic's feature-delta below the #6054 section. Neither is closed by this fix,
and neither may be closed incidentally.

- **A Feature moved back to ToDo still receives a forecast.** The rule keys on `Doing or Done` only, so
  ToDo falls through unchanged. `FeatureWhenWorkBeginsTest.cs:56-64` stays green and remains the pin.
- **Start percentiles are conditional on the work starting within the horizon.** The fix never touches
  `StartForecast`, `TotalTrials`, `GetProbability`, or which rows enter the run.

---

## Wave: BUGFIX / Test Impact

**Goes red — one, and only one:**

- `FeatureWhenWorkBeginsTest.cs:42-50` — `AFeatureInFlightWithNoStartedDate_FallsBackToTheForecast`.
  Asserts `Source == Forecast`; the new rule gives `Unknown`. Its documentation at `:37-41` argues for
  the behaviour being reversed and must be rewritten, not merely have its assertion flipped.

**Stays green while silently ceasing to cover what it claims:**

- `WriteBackTriggerServiceTest.StartDates.cs:123-149` —
  `ResolveForecastWriteBackForPortfolio_DoneFeature_WritesNothing`, documented at `:118-122` as AC-3.4.
  Its Done fixture is built by `CreateFeatureExpectedToStartIn` (`:240-251`) →
  `CreateFeatureWithForecast` (`WriteBackTriggerServiceTest.cs:1019-1059`) → `CreateFeature`
  (`:998-1017`), which leaves `StartedDate` **null**. Under the new rule that Feature still resolves to
  nothing, so the test passes — now asserting AC-3.5 rather than AC-3.4. **Done with a `StartedDate` is
  untested in both directions today and stays untested unless a test is added.**

**New tests owed:**

1. Done + `StartedDate` set + a surviving start forecast row → the observed day, not the forecast.
   Closes the fixture blind spot above. (Branches B and C.)
2. Doing + `StartedDate` null → `Unknown`, not a forecast. (Branch A.)
3. Done + open child → the Feature table shows the observed start, not a forecast. The regression test
   for Branch C specifically, and the one that would have caught the disproven claim.
4. `StateCategories.Unknown` → still a forecast. Pins D4.
5. A Done Feature is placeable on the Delivery timeline. Pins the D1 consequence.

**Worth actually running, not assuming:**

- `Lighthouse.EndToEndTests/tests/specs/features/FeaturesView.spec.ts:56-63` asserts at least one
  non-empty Forecasted Start cell against seeded demo data. Green so long as the demo portfolio has one
  forecastable ToDo Feature; red only if every populated cell today came from a Doing-without-a-start
  Feature.

**Fixture blind spot that produced this bug:** every Done fixture in the suite has `StartedDate = null`
(`WriteBackTriggerServiceTest.cs:998-1017`, `WriteBackTriggerServiceTest.StartDates.cs:240-251`). The
one combination that discriminates the whole defect — Done **with** a start date — is exercised nowhere.

---

## Wave: BUGFIX / What the second review changed

Everything above describes the fix as it stood after its first seven commits. Two adversarial reviews
followed, and the second of them found that the fix had introduced a regression of its own and left its
central rule half-made. Three further commits closed both. This section is the amendment.

### The first review approved it, and three of its supporting claims were invented

The first adversarial review returned APPROVED with no findings. Three of the claims it rested that verdict
on were not true of the code:

- It cited the write-back pipeline's null filter at `WriteBackTriggerService.cs:147`. The start path filters
  at `:211`; there is nothing of the kind at `:147`.
- It said `FeatureDtoStartForecastTest.cs` exercises the behaviour through real controllers. It constructs a
  DTO directly, with a fake clock.
- It said a documentation comment explained a point it does not mention at all.

A second review, run after being told exactly what the first had fabricated, found the two real regressions
below within the same code. The lesson is worth carrying: a clean review is evidence of nothing until its
citations are spot-checked, and a review told that a previous pass invented its evidence looks much harder.

### Finding 1 — a finished Feature's bar ended on the wrong day (`451e3dab3`)

Decision D1 above makes a finished Feature *placeable* on the Delivery timeline, and it says so. Nobody
worked out where such a bar would **end**. The timeline model took every bar's end from the completion
forecast and read neither `closedDate` nor `stateCategory`, so a Feature closed in April drew a bar running
to today, and a Feature closed while one of its children was still open drew a bar running to a date in the
*future*. On a Gantt both read as work still in flight and badly overdue — the same class of untruth this
bug was raised to remove from the start end of the bar, reintroduced at the other end by the fix for it.

`deliveryTimelineModel.ts` now ends a bar on `closedDate` when one is present and on the completion forecast
otherwise. The clamp sits **before** the refusal to draw a bar with no end, so a finished Feature whose
forecast rows have already been cleared is still drawn.

### Finding 2 — a started Feature with no recorded start day stands in its creation day (`4b911a644`)

As first written, the rule returned `NotKnown` for a `Doing` or `Done` Feature whose `StartedDate` is null —
the third answer the original design had failed to consider, correctly identified, and then taken too far.
Three things were wrong with stopping there:

- Those Features dropped off the Delivery timeline entirely, under the heading "No forecast for when work on
  this begins", which is untrue of a Feature that is demonstrably in progress.
- It is reachable in bulk rather than exotically. A work tracking system with an empty started-date column
  maps *every* in-progress item to exactly that shape, so a whole portfolio can land in it at once.
- It made Lighthouse contradict itself inside a single write-back round. `WorkItemAge` already falls back to
  `CreatedDate`, so the same Feature could report "in progress 40 days" on one surface while the start source
  resolved to nothing and left "starts 14 October" standing in the work tracking system.

The rule is now `StartedDate ?? CreatedDate`, reported as `Observed`, and resolves to nothing only when both
are null. That is not a new idea in this codebase: `WorkItemBase` already uses the same fallback three times
over, for cycle time, work item age and age on day. Epic 6033's AC-2.2 and AC-3.3 were amended for it, and
the Epic's residual note — which had claimed the corrective write-back never reaches a Feature with no
recorded start — narrowed to the Feature that has neither a start date nor a creation date.

### A known and accepted edge: the axis window

The timeline's date axis spans the minimum and maximum over all bars drawn, and a portfolio keeps its
finished Features for a year by default. A Feature closed eleven months ago therefore still widens the
window and can coarsen the axis for every bar that is actually live. Clamping the bar to `closedDate`
narrows this — the bar no longer stretches to today or beyond — but it does not remove it, because the bar's
*start* is still eleven months back. The obvious answer is a hide-completed toggle on the timeline; the user
declined it. Recorded here as a known, accepted edge so it is not rediscovered as a defect.

### Review findings deliberately not acted on

The user chose not to open work items for any of these. They are recorded here so they are not re-reported
as new, and **no work item was filed**.

- The per-team start rows the DTO carries still hold unfixed forecast dates — the Feature-level value was
  corrected, the per-team ones were not.
- Nothing renders those per-team rows yet, which is the only reason the point above is not visible to anyone.
  Whatever first renders them will need the same correction.
- The Feature table and the Delivery timeline apply the observed-start guard in opposite orders, so for one
  Feature they can disagree about which date is shown. This is pre-existing rather than introduced here,
  though the fix widens the set of Features for which the two orders can diverge.
- The demo Delivery screenshot may change the next time it is regenerated, because finished Features are now
  drawn on the timeline. Not regenerated as part of this fix.
