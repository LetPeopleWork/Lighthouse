# Mutation testing — Bug #5621 (ServiceNow span-to-date and stable sort)

Run 2026-08-01 against `main` @ `0fd948fca`. Gate is 80 % kill rate on both stacks.

| stack | score | tested | killed | survived | timeout | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | **88.11 %** | 377 | 336 | 36 | 5 | 10 | 13 m 41 s |
| Frontend (StrykerJS) | **N/A** | — | — | — | — | — | — |

**Frontend is N/A because the fix changed zero frontend files.** All five findings live in connector
internals — span-to-date derivation, the paging query, the availability verdict — and none crossed an
API boundary or a rendered surface. (5611 needed a frontend run because it changed
`DataRetrievalSchemaDefaults.ts`; this does not.)

Config: `stryker.5621.backend.json`. Run from `Lighthouse.Backend.Tests/`. See `/mutation-testing`.

A first run scored **86.05 %** (328 killed, 43 survived). Six of those survivors were in the logic this
series added rather than in code that merely shares a file with it; the tests written off that triage
closed all six and took the score to 88.11 %.

## Backend

| file | tested | not killed |
| --- | --- | --- |
| ServiceNowWorkTrackingConnector.cs | 264 | 25 |
| IssueFactory.cs | 64 | 22 |
| ServiceNowStateSpanMapper.cs | 17 | 1 |
| WorkItemCategoryCrossing.cs | 15 | 1 |
| ServiceNowHistoryVerdict.cs | 15 | 0 |
| ServiceNowHistoryQuery.cs | 12 | 2 |

### Closed by this pass

- **`ServiceNowWorkTrackingConnector.cs:399`, NoCoverage** — the block that drops the start date when
  work was pushed back to the queue was never executed by any test. The rule existed only in the pure
  mapper's tests; no sync ever walked a record through it.
  `WorkPushedBackToTheQueueAfterStarting_DoesNotKeepTheStartItHad` now takes a record Doing → New →
  Resolved and asserts the start collapses onto the finish.
- **`:406`, null-coalescing swap** — `startedDate ?? closedDate` mutated to `closedDate ?? startedDate`
  and nothing noticed, because no assertion had both dates non-null *and different* on one record.
  `WhenHistoryIsAvailable_WorkFinishedWhenItReachedDone` now asserts both.
- **`:391`, conditional forced true** — the `StateCategory == Done` gate on the finish date could be
  removed. `WorkThatWasReopened_CarriesNoFinishDateWhileItIsBeingWorkedAgain` supplies the case the
  fixtures lacked: a record whose spans hold a Done arrival but which is In Progress again, so the
  spans alone would close an item that is demonstrably open.
- **`:248`, block removal** — deleting the "no definitions means no span read" guard failed nothing,
  though without it `SpanQueryFor` builds an empty `definitionIN` that matches every span on the
  instance. `AnInstanceMeasuringNothing_AsksForNoSpansAtAll` asserts no `metric_instance` request.
- **`ServiceNowHistoryVerdict.cs:76` (3 mutants)** — `FromAnUnreadableAnswer` had no test at all, so a
  refusal reporting "activate a metric definition" instead of "grant a role" would not have failed
  anything. Both rungs now pinned.
- **`:61`, `Count > 0` → `Count >= 0`** — a team that named no kinds of work satisfies
  `everyKindIsMeasured` vacuously. `ATeamThatNamedNoKindsOfWork_ReportsNoStateMetric` closes it.

### Accepted survivors — in code this fix wrote

- **`ServiceNowWorkTrackingConnector.cs:398` (2)** — `&&` → `||` and `>` → `>=` on the queue-return
  guard. Both are equivalent: C#'s lifted `>` already yields false when either operand is null, so the
  `HasValue` checks are belt-and-braces, and two spans cannot begin at the same instant in different
  categories. Kept in that shape deliberately — it mirrors `IssueFactory.GetStartedAndClosedDate`
  line for line, and this series exists because ServiceNow diverged from that rule once already.
- **`WorkItemCategoryCrossing.cs:47`** — `>` → `>=` in the running maximum. Picks a different
  transition carrying the same instant; the returned value is identical.
- **`ServiceNowStateSpanMapper.cs:98`** — the empty `FromState` on the earliest arrival mutated to a
  literal. Any unmapped string reads as outside every category, which is precisely the property being
  relied on, so no observable behaviour changes.

### Not killed, and not this fix's code

The remaining 30 sit in parts of files this change only partly touched, and were survivors before it:
Link-header parsing (`:833`–`:922`, 11), the paging-termination guard (`:462`–`:473`, 5 — mostly
timeouts, which Stryker counts as killed), `GuardAgainstRepeatedRecords`'s identity fallback (`:522`,
2), two log-message strings, three NoCoverage blocks in unrelated methods, and Jira's rank-field
parsing and logging in `IssueFactory` (22). `ServiceNowHistoryQuery.cs:82-83` are string mutations in
`SpanQueryFor`, which this fix did not change.

### Not mutated

`AzureDevOpsWorkTrackingConnector.cs` is excluded from `mutate`. 27 of its 1111 lines changed, and
Stryker.NET cannot scope to a line range — including it would have measured ~800 mutants of untouched
code and buried this change's score. The ADO change is a pure delegation to
`WorkItemCategoryCrossing.LastEntryInto`, which **is** mutated (15 tested, 1 equivalent survivor) and
directly tested, and its behaviour-preservation was verified against the original loop during review.
