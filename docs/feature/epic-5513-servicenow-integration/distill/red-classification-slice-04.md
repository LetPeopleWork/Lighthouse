# RED classification — slice 04 (Story #5577)

Pre-DELIVER fail-for-the-right-reason gate. Every failing test is classified; anything not
`MISSING_FUNCTIONALITY` blocks handoff.

**Status: COMPLETE. 43 tests RED (38 backend + 5 frontend), both stacks building with 0 warnings.**

Whole-area check: `--filter ServiceNow` → **Failed 38 / Passed 176**; frontend
`ConnectionValidationResult.test.ts` → 5 failed / 1 passed. Every failure is `MISSING_FUNCTIONALITY`
at an assertion site. Nothing is `IMPORT_ERROR`, `FIXTURE_BROKEN`, `SETUP_FAILURE` or
`OBSERVABLE_NOT_AT_PORT`, so the DELIVER handoff is not blocked.

Whole-area check after the shared-contract change: `dotnet build` 0 warnings, and
`--filter ServiceNow|WorkTrackingSystemConnectionsController|TeamsController|PortfoliosController|Validation`
→ **Failed 31 / Passed 263**. The 31 are exactly the new RED tests; extending
`ConnectionValidationResult` regressed nothing.

## C# adaptation of the RED-scaffold rule (inherited from slice 01)

The production assembly cannot reference NUnit, so scaffolds **return deliberate wrong values**
rather than throwing: `__scaffold__` sentinels for strings, `DateTime.UnixEpoch` for instants. The
failure then lands at the assertion site and the expected/actual diff reads as the specification,
which is a stronger signal than an escaping exception. Keep this pattern.

## Written and RED — `ServiceNowStateSpanMapperTest` (10 / 10)

`dotnet build` 0 warnings. `dotnet test --filter ServiceNowStateSpanMapperTest` → **Failed: 10,
Passed: 0**. Every one `MISSING_FUNCTIONALITY`, every one failing at its assertion.

| Test | Carries | Classification |
|---|---|---|
| `ConsecutiveSpans_BecomeTheMovesBetweenThem` | AC2 · ADR-118 D1 | MISSING_FUNCTIONALITY |
| `TheEarliestSpan_IsAnArrivalNobodyWitnessed_AndIsNotReportedAsAMove` | DISTILL Q2 answered | MISSING_FUNCTIONALITY |
| `SpansArrivingOutOfOrder_ArePairedByWhenTheyStarted` | ADR-118 D1 | MISSING_FUNCTIONALITY |
| `AReopenedRecord_ReportsTheJourneyBackOutOfDone` | DISTILL Q1 answered | MISSING_FUNCTIONALITY |
| `TwoLabelsTheTeamTreatsAsOneState_ProduceNoMoveBetweenThem` | AC2 (shared mapper) | MISSING_FUNCTIONALITY |
| `ARecordWithNoHistory_ReportsNoMoves` | edge | MISSING_FUNCTIONALITY |
| `WorkStarted_WhenTheRecordFirstReachedAStateTheTeamCallsDoing` | ADR-118 D7 | MISSING_FUNCTIONALITY |
| `WorkThatReturnedToDoing_StartedTheFirstTimeItGotThere` | ADR-118 D7 (rework) | MISSING_FUNCTIONALITY |
| `WorkThatNeverLeftTheQueue_HasNotStarted` | ADR-118 D7 (null) | MISSING_FUNCTIONALITY |
| `NoSpansAtAll_ReportNoStart` | edge | MISSING_FUNCTIONALITY |

**Zero accidental greens.** The two "reports nothing" tests are red against the scaffold because the
scaffold returns a bogus transition and `UnixEpoch` rather than empty and null — deliberate, so that
no test can pass before the behaviour exists.

## Two DISTILL questions answered by these tests

**Q1 — reopened records.** A later span carrying an already-held label pairs into a
`Resolved → In Progress` transition. **Correct, and kept** — it is precisely what a flow coach
investigating rework needs to see. Pinned by `AReopenedRecord_ReportsTheJourneyBackOutOfDone`.

**Q2 — partial history / leading synthetic transition.** **No synthetic transition.** Spans begin
only when the metric definition was activated, so a record older than that has a first observed label
that is not necessarily the state it was created in. Manufacturing a "created → first label" move
would assert a state the record may never have held, dated to a moment nothing measured — the
invented-data failure this epic exists to prevent. Pinned by
`TheEarliestSpan_IsAnArrivalNobodyWitnessed_AndIsNotReportedAsAMove`.

## Design expressed in the type, not in a rule

`ServiceNowStateSpan` carries `RecordId`, `Label`, `Start` — and deliberately **no `End` and no
`Duration`**. ADR-118 decision 6 says those are never read; leaving them off the type makes that
structural instead of something a future contributor has to remember. It also means the Glide
epoch-offset parsing trap cannot be reintroduced by accident.

## Written and RED — `ServiceNowHistoryVerdictTest` (9 / 11)

ADR-118 decision 5, the three-way capability verdict.

| Test | Carries | Classification |
|---|---|---|
| `AnAccountRefusedTheMetricTables_LacksTheRights` | 403 rung | MISSING_FUNCTIONALITY |
| `ARefusedReadReturningNothing_IsAboutRightsRatherThanConfiguration` | the distinction itself | MISSING_FUNCTIONALITY |
| `AnInstanceThatCanSupplyHistory_CarriesNoAdvisory` | silence when nothing is wrong | MISSING_FUNCTIONALITY |
| `AMissingCapability_DoesNotFailTheConnection` | advisory rides a success | MISSING_FUNCTIONALITY |
| `TheAdvisoryForMissingRights_NamesTheRoleToGrant` | actionable remedy | MISSING_FUNCTIONALITY |
| `TheAdvisoryForAMissingMetric_NamesTheTableAndTheMetricKind` | actionable remedy | MISSING_FUNCTIONALITY |
| `WhateverTheCause_TheAdvisorySaysWhichNumberTheTeamWillGet` (×2) | ADR-117 honesty obligation | MISSING_FUNCTIONALITY |
| `AnInstanceMeasuringStateSpans_CanSupplyHistory` | happy path | MISSING_FUNCTIONALITY |

**Two accidental greens, both argued rather than hidden:**

- `AnInstanceMeasuringNothing_HasNoStateMetric` — the scaffold returns `NoStateMetric` unconditionally,
  so this one case matches. The constant was chosen deliberately: returning `Available` would be a
  scaffold that says history works whatever the instance answered, which is the exact success-costume
  defect slice 01 caught in its own `ValidateConnection` scaffold. Being wrong towards *unavailable*
  cannot fake a passing capability.
- `AnAnswerNobodyExpected_IsNotTreatedAsWorking` — a negative assertion (`Is.Not.EqualTo(Available)`)
  that any conservative constant satisfies. It is a regression guard for later, not a driver now.

## Written and RED — `ServiceNowHistoryQueryTest` (12 / 12)

ADR-118 decisions 2 and 4. Batching, query construction, row filtering.

| Test | Carries | Classification |
|---|---|---|
| `ATeamLargerThanOneBatch_IsSplit` | 200-id chunking | MISSING_FUNCTIONALITY |
| `ATeamThatFitsInOneBatch_IsNotSplit` | chunking | MISSING_FUNCTIONALITY |
| `SplittingATeam_KeepsEveryRecordExactlyOnce` | no loss, no duplication | MISSING_FUNCTIONALITY |
| `ATeamWithNoWork_AsksForNothing` | empty case | MISSING_FUNCTIONALITY |
| `AFullBatchsQuery_StaysUnderTheMeasuredUrlLimit` | the 8192-byte cliff | MISSING_FUNCTIONALITY |
| `TheDefinitionQuery_AsksForTheTablesStateSpanMeasurements` | ADR-118 D2 | MISSING_FUNCTIONALITY |
| `TheSpanQuery_RestrictsToBothTheRecordsAndTheDefinitions` | ADR-118 D2 | MISSING_FUNCTIONALITY |
| `ASpanRow_IsReadForItsLabelRatherThanItsChoiceNumber` | `value` not `field_value` | MISSING_FUNCTIONALITY |
| `RowsFromADefinitionThatDoesNotMeasureState_AreDiscarded` | ADR-118 D2 | MISSING_FUNCTIONALITY |
| `ASpansStart_IsReadInUniversalTime` | Bug #5567 ledger | MISSING_FUNCTIONALITY |
| `AnEmptyAnswer_ProducesNoSpans` | edge | MISSING_FUNCTIONALITY |
| `ARowWithNoReadableStart_IsNotASpan` | no fabricated instants | MISSING_FUNCTIONALITY |

**Zero accidental greens — and getting there needed the scaffold's wrong values chosen, not defaulted.**
The first attempt had five false passes, two of them load-bearing:

- `IntoBatches` echoing its input back made *"a team that fits in one batch is not split"* pass before
  batching existed. Now it returns one batch holding one id belonging to nobody — wrong for every case.
- `SpanQueryFor` returning a short sentinel satisfied the **URL-length guard**, which is the single
  test standing between a full batch and a 414 that fails the whole sync. Now it returns 8000
  characters, deliberately over budget.
- `SpansFrom` returning one span made the **definition filter's own test** pass while filtering
  nothing — it asserts exactly one span survives, so a scaffold that always answers with one was
  right by accident about the decision this slice turns on. Now it returns two.

This is the slice-01 lesson applied ahead of time rather than discovered in flight: *a scaffold whose
constant happens to match an assertion is a test that will never fail for the right reason.*

## Contract change landed with the tests

`ConnectionValidationResult` gains `Advisory` + `AdvisoryCode` and a `SuccessWith(...)` factory.
Purely additive — `Success()` and `Failure()` are untouched, so all 27 existing call sites keep
working, confirmed by the 263 passing tests in the same area.

## Carried into DELIVER (specified and RED, not yet wired)

| Item | State |
|---|---|
| The three pure cores | scaffolds returning chosen-wrong values |
| Connector history read | seam marked `// SCAFFOLD (DISTILL slice 04` in `GetWorkItemsForTeam` |
| Frontend advisory | `readConnectionValidation` RED; the service still collapses the response to a boolean, and the wizard has no success-path channel |
| Advisory absent from metrics UI | **structurally satisfied, not tested** — no metrics component receives a `ConnectionValidationResult`. Worth a reviewer's eye, not a test asserting an absence across the whole UI |
| Mutation ≥80 % both stacks | owed at the end; **no Stryker config is committed in this repo** |
| Dogfood | move records through states on the PDI as `lh_probe_itil`, confirm Cycle Time drops, then revoke the role and confirm the downgrade rather than an error |
