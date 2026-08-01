# RCA — Bug #5621: ServiceNow transition-span dates disagree with span presence, and the stable sort is not always total

- **Reported**: 2026-07-31, second review of US #5611 at `67637ce76`. Not a field report — found by code review.
- **Triaged with the maintainer**: 2026-08-01. Verdicts and the two new findings are on the work item (rev 6).
- **Method**: code trace with `file:line` evidence at every link; the definition-count behaviour is
  measured on PDI `dev191338`, not inferred.
- **Release status**: nothing ServiceNow has ever shipped. All defects are on `main`, none are in a
  customer's hands. This is the good version of the problem — the fix lands before first release.

---

## 1. The single root cause

**Three components ask "does this record have transition history?" and mean three different things.**

| Where | The question it actually asks | Filters to state spans? |
|---|---|---|
| `ServiceNowWorkTrackingConnector.cs:331,349` — `history.TryGetValue` | does this record have **any** span? | no |
| `ServiceNowStateSpanMapper.ToTransitions:24` | spans whose label the team **mapped** | yes, explicitly |
| `WhenWorkStarted:34` / `WhenWorkFinished:52` | is there a **Doing** / **Done** span? | incidentally — an unmapped label is `Unknown`, so it never matches |

Upstream of all three: `ServiceNowHistoryQuery.DefinitionQueryFor:43` filters metric definitions on
`table` and `type` only. There is **no `field` filter**, so definitions that do not measure state are
collected and their spans travel the whole pipeline.

Measured on the PDI: `table=incident^type=field_value_duration` returns **four** definitions —
`incident_state`, `assigned_to`, `assignment_group`, `active`. Three of the four do not measure state.

Everything below is that one disagreement surfacing in a different place.

---

## 2. Findings and verdicts

### F1 — BLOCKER — a record whose only spans are non-state spans loses both dates, silently

`TryGetValue` says "has spans" → takes the span branch → the mapper finds no Doing/Done span → `null`.
The `opened_at ?? sys_created_on` / `closed_at` fallback that `ServiceNowWorkItemMapper.cs:111-112`
already computes is never reached.

**Per-record, on a correctly configured instance.** `INC0012345` opened and closed in May, before the
state definition was activated — so no state spans exist for it. In July someone re-assigns it during a
queue cleanup; the `assignment_group` definition writes one span. The record is now *present* in the
dictionary with a single unrecognised label, so it takes the span branch and gets
`StartedDate = null`, `ClosedDate = null`. Had nobody touched that unrelated field, it would have been
absent, hit the fallback, and been correct. **Whether an item carries dates depends on whether an
unrelated field happened to change.**

**Whole-team.** A customer deactivates *Incident State Duration* and leaves the other three stock
definitions active. `ServiceNowHistoryVerdict.From(200, 3)` returns `Available` — the parameter is named
`stateSpanDefinitions` but counts every `field_value_duration` definition — so `ReportHistoryUnavailable`
never fires, `SupportsTransitionHistory` stays `true`, and `WorkItemService.cs:233` skips the synthetic
transition fallback. Every item on the team: no dates, no transitions, Throughput 0, Cycle Time and Work
Item Age empty, **no warning anywhere**.

ADR-118's "Also" consequence predicted this cause but assumed it would present as the
200-with-zero-definitions rung. It does not: the other three definitions keep the count above zero.

Age: the `StartedDate` half predates this story (slice 04, #5577); #5611 extended the pattern to
`ClosedDate`. Not covered by any fixture — none has `StateCategory == Done` + spans + no Done span.

**Verdict: FIX.** Filter to team-recognised state spans **once**, where spans enter the by-record
dictionary (`ReadSpans` / `KeepAgainstItsRecord`), so a record with no state span is simply *absent* and
the existing fallback is reached without touching it. Label-recognition rather than a `field=` filter on
the definition query: the state field name differs per table (`incident_state` vs `state`) and
`ServiceNowWorkItemMapper.StateField` is hardcoded to `state`, so a naive field filter risks dropping the
stock incident definition.

**Constraint (maintainer, D7 review).** The fix must keep *no state evidence at all* distinguishable from
*state evidence present, and it shows the record never reached Doing*. Only the first falls back to
`opened_at`; the second is handled by F2's `started = closed` rule. Collapsing them would resurrect start
dates for genuinely unstarted work, which ADR-118 D7 exists to prevent.

### F2 — HIGH — dates are derived from individual spans, not from category crossings

Filed as "`WhenWorkFinished` uses `FindLast`". The triage established the defect is broader: **ServiceNow
derives dates from individual spans where Jira and ADO derive them from category crossings.**

The rule, as the codebase already writes it twice — `IssueFactory.cs:106-116` and
`AzureDevOpsWorkTrackingConnector.cs:756-763`:

```csharp
targetStates.IsItemInList(result.state)          // arriving into the category
 && !targetStates.IsItemInList(previousState)    // FROM OUTSIDE it — in-category moves never re-date
 && !statesToIgnore.IsItemInList(previousState)
```

taking the **latest** such crossing.

Three divergences follow:

1. **`WhenWorkFinished` uses `FindLast` over spans.** A desk mapping both `Resolved` and `Closed` to Done
   — the mapping `AServiceDesk()` and `ATeam()` fixtures use — resolves on 07-10 and the stock close-job
   closes on 07-17. Spans `In Progress`, `Resolved@07-10`, `Closed@07-17` → returns 07-17. Cycle Time
   inflated by the whole close-out window, item lands in Throughput a week late, **for every incident on
   the instance**. Nothing was undone between Resolved and Closed; a reopen has a non-Done span between.
   Lighthouse's out-of-box ITSM mapping files `Resolved` under Doing (ADR-117), so this bites only teams
   who map both — which the fixtures suggest is expected. New in #5611.
2. **`WhenWorkStarted` uses `Find`** — the earliest Doing span ever. Jira/ADO take the latest crossing
   into Doing from a non-Done state. On `ToDo → Doing(d1) → ToDo(d3) → Doing(d5) → Done`, ServiceNow says
   d1 and Jira/ADO say d5. **Not previously filed.**
3. **Two rules ServiceNow lacks entirely**: the `lastToDoEntryDate > startedDate ⇒ startedDate = null`
   guard, and the `startedDate == null && closedDate != null ⇒ startedDate = closedDate` fallback. The
   second matters for F1's blast radius — a Done ServiceNow record with no Doing span gets a null
   StartedDate and **drops out of Cycle Time entirely**, where Jira and ADO keep it with a zero-length
   cycle. **Not previously filed.**

The tell that this is a defect rather than a preference: the category-boundary rule passes every existing
ServiceNow test unchanged. `FindLast` was under-constrained, not chosen.

**Verdict: FIX**, adopting the Jira/ADO rule, with `started = closed` when the record is Done and no Doing
crossing was observed (maintainer's call).

**DRY, accepted by the maintainer.** The rule is the same *knowledge* written twice already and the
divergence found here is the evidence. Extract it to one place all three connectors call rather than
writing a third copy. Input shapes converge: Jira/ADO walk revision pairs, and `PairConsecutive` already
turns spans into the same from/to pairing.

### F3 — MEDIUM-HIGH — the `sys_id` tie-breaker is skipped when the team wrote its own ORDERBY

`InAStableOrder` (`ServiceNowWorkTrackingConnector.cs:518-523`) early-returns the query unchanged when it
already contains `ORDERBY`, so `95e8a9d39`'s tie-breaker never reaches those teams. Offset paging over a
non-total sort overlaps pages; `GuardAgainstRepeatedRecords` throws `paging_repeated_records` and the
**whole team's sync** aborts.

`active=true^ORDERBYDESCopened_at` — a shape the SPIKE exercised — sorts on a one-second-resolution field
that bulk imports tie heavily on. Under ~100 records it never pages, so it appears only as a team grows:
worked for months, then stopped.

**Why the population is larger than it looks.** "Teams should not write an ORDERBY" is not enforceable at
the source: **ServiceNow's own *Copy query* emits one**, because the list's current sort is part of the
query state and stock list views are sorted by default. That is the path #5610 D2 places on **#5578**
("the right-click breadcrumb → *Copy query* walkthrough") and that AC-A1's help text will name. The
affected population is therefore not "a coach who unusually hand-writes ORDERBY" but "everyone who follows
our own documentation" — ~nobody today, structural once #5578 lands.

**Verdict: FIX**, one line — delete the early return. ServiceNow chains ORDERBY terms, so
`active=true^ORDERBYDESCopened_at^ORDERBYsys_id` keeps the coach's sort primary *and* makes it total. No
parsing, no stripping, and no validation rule rejecting ORDERBY (which would break the Copy query path).
`ServiceNowReadScope.cs:55` already documents the append as unconditional; the code catches up to the doc.

### F4 — a 200 carrying no record set throws instead of downgrading

`ReadEveryPage:387` tests the status code alone, so a 200 with a non-record body reaches `RecordsFrom:424`
and throws. Both history reads pass `WhenRefused.Downgrade` precisely so a degraded instance degrades the
sync rather than failing it (ADR-118 D5) — and neither gets that protection when the failure arrives as a
200 with an SSO login page, which is the case ADR-114 exists for.

**Verdict: FIX** (promoted out of "parked" by the maintainer). Downgrade should trigger on *not a usable
answer*, not on *not a 200*. Same path as F1, opposite direction — it makes F1's fix safer.

### F5 — the paging slack is frozen from page 1 — PARKED

`PagesAllowed:465` computes `(totalCount / rowsInFirstPage) + 2` once, on page 1. The slack is a constant
while exposure scales with table size and sync duration, and on the `Link`-header path the count cannot be
re-checked at all.

**Verdict: PARK, confirmed.** The failure is loud (`PagingDidNotTerminate`), the limit is documented, and
the honest next step is a live probe — does a real instance emit `rel=next` past its own reported count?
Guessing at a larger constant is not a fix.

### F6 — the availability verdict is an aggregate count across all of a team's classes — NEW

`ReadStateSpanDefinitions:297` queries `tableIN{classes}^type=field_value_duration` and
`ServiceNowHistoryVerdict.From(statusCode, definitionIds.Count)` asks only whether that total exceeds
zero. A team covering `incident` and `change_request` where only `incident` has a state definition
activated reports **Available**, logs no warning, and every change_request silently loses its dates and
transitions.

Distinct from F1's whole-team scenario in that it needs **no misconfiguration at all** — only an instance
where one class was set up and another was not. Definitions attach to concrete classes and never to a base
table (ADR-123 D9, measured: `table=task` returns 0 against 6 for `tableINincident,change_request`), so
every class a team names needs its own definition.

**Verdict: FIX**, rolled into F1's second half — same read, same verdict, one fix.

---

## 3. Correction to the filed analysis

The description's claim that the paging guard keys on `number` rather than `sys_id` is **wrong and already
fixed**. `GuardAgainstRepeatedRecords:450` calls `ServiceNowWorkItemMapper.ReadRecordId`, which reads
`sys_id`; the comment above it cites the PDI measurement (change_request: 118 rows, 113 distinct numbers).
Landed in `95e8a9d39`. No work required.

---

## 4. Fix order

F3 and F4 are independent one-liners and land first. The shared category-boundary rule is extracted and
proven against Jira/ADO's existing tests before ServiceNow is pointed at it. F1 lands last because its
fallback behaviour is defined in terms of F2's `started = closed` rule.

| Step | Finding | Change |
|---|---|---|
| 01-01 | F3 | Delete the `InAStableOrder` early return |
| 01-02 | F4 | Downgrade on an unusable answer, not on the status code alone |
| 02-01 | F2 | Extract the category-boundary rule; Jira and ADO call it (refactor, behaviour unchanged) |
| 02-02 | F2 | ServiceNow adopts the shared rule; `started = closed` when Done with no Doing crossing |
| 03-01 | F1 | Filter to team-recognised state spans where spans enter the dictionary |
| 03-02 | F1 / F6 | Availability decided per record class, not by an aggregate count |
