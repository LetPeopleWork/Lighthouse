# RED classification — slice 04 (Story #5577)

Pre-DELIVER fail-for-the-right-reason gate. Every failing test is classified; anything not
`MISSING_FUNCTIONALITY` blocks handoff.

**Status: PARTIAL — the pure core is written and RED. The reader, the capability verdict, the
connector wiring and the frontend are not yet written.** See "Still to write" below.

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

## Still to write (DISTILL is NOT complete)

| Component | Tests owed | Carries |
|---|---|---|
| `ServiceNowHistoryReader` | batching, 200-id chunking, definition filter, grouping spans per record | ADR-118 D2, D4 |
| `ServiceNowHistoryVerdict` | three-way capability verdict (available · no rights · no metric) | ADR-118 D5 |
| `ServiceNowWorkTrackingConnector` | `SupportsTransitionHistory` per-instance; runtime downgrade; `StartedDate` switch | AC1, AC4, ADR-118 D7 |
| `ConnectionValidationResult` | advisory channel surviving `IsValid = true` | ADR-118 D5 — **shared contract, extend the factory first** |
| `ServiceNowWorkItemMapper` | carry `sys_id` (the batch key) | ADR-118 D4 |
| Acceptance (`ServiceNowTeamSyncAcceptanceTest`) | AC3 end to end — state-time widgets on a ServiceNow team | AC3 |
| Frontend | advisory rendered in wizard + settings, and **absent from the metrics UI** | ADR-118 D5 |
| ArchUnit | span mapper purity fixture | ADR-114 shape |

Three existing tests currently **assert the opposite** and must flip when `SupportsTransitionHistory`
stops returning a constant: `ServiceNowTeamSyncTest.cs:211`,
`ServiceNowWorkTrackingConnectorTest.cs:218`, `ServiceNowTeamSyncAcceptanceTest.cs:143`.
