# ADR-127: The advisory channel reaches team settings — where a user learns their kind of work yields no time-in-state

- **Status**: **Accepted as design, delivery split out** (maintainer, 2026-08-01). The decisions below
  stand; they ship under their own story **[#5627](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5627)**, not inside #5610. Decision 3 is what
  forces the split: once the advisory belongs to team settings rather than the picker, it shares no
  code with the board picker, so bundling it would only enlarge #5610's slice 02 and muddy its
  same-day dogfood. **One objection is open against decision 1** — see Consequences. Until #5627
  ships, #5578's docs carry the caveat, which is the fallback named in Alternatives.
- **Date**: 2026-08-01
- **Feature**: delivered by ADO Story 5627; designed during Story 5610's DESIGN wave
  (`servicenow-board-picker-and-query-guidance`), parent Epic 5513
- **Deciders**: Benjamin Huser-Berta (maintainer)
- **Builds on**: [ADR-118](./adr-118-servicenow-transition-history-from-metric-instance-spans.md)
  decision 5 (the advisory), [ADR-123](./adr-123-servicenow-record-classes-as-work-item-types.md)
  decision 10 (why the connection stopped answering this)
- **Answers** the open item recorded at the end of the 5611 section of `brief.md`: *"whether
  `ValidateTeamSettings` should report history availability now that a hierarchy-rooted connection
  deliberately says nothing about it, which would otherwise leave a `task`-rooted administrator with
  no screen that answers 'will I get time-in-state?'."*

## Context

Stock `change_request` has **no state-tracking metric definition at all** — its two definitions sit on
`approval` and `type` (5611 SPIKE, 2026-07-31). A change-request team therefore can never produce
state spans, whatever Lighthouse does. A board picker is the moment a user chooses a record class in
one click, which is the moment that cost becomes real and invisible.

DISCUSS recorded that **there is no channel at all** for saying so, because 5611 withdrew the
connection-scope advice about history rather than rewording it, and that ruling R-2 forbids choosing
to warn without first checking a reader exists.

**Measured against the code, "no channel" is wrong, and the correction is what makes this decision
cheap.** Three of the four parts are already built and shipped:

| Part | State |
|---|---|
| `ConnectionValidationResult.Advisory` + `AdvisoryCode` | **Exists**, serialised, documented as ADR-118 D5 |
| `ValidationAdvisory.tsx` | **Exists**, tested, renders or returns null |
| Connection surfaces render it | **Yes** — `ModifyConnectionSettings`, `CreateConnectionWizard` |
| Team surfaces render it | **No** — and this is the whole gap |

The gap is one line: `TeamService.validateTeamSettings` posts to `/teams/validate`, receives the full
`ConnectionValidationResult`, and returns `response.data.isValid === true`. Everything else on a
*successful* validation — advisory, advisory code — is discarded in the service layer.

Failure verdicts do reach the user: `POST /teams/validate` answers `BadRequest(validationResult)` on a
refusal, `BaseApiService` parses `{message, technicalDetails}` into an `ApiError`, and
`useModifySettings.handleSave` renders it as `validationError`. So `query_matches_whole_table` and
`class_is_not_a_kind_of_work` are read today. Only the advisory that rides a **success** is dropped —
which is exactly the shape a capability limitation has.

5611 also already computes the answer. `ServiceNowHistoryVerdict.From` decides availability as
*coverage over the kinds of work the team named*, and returns `NoRights` for a `403` on the metric
tables and `NoStateMetric` where a named class has no definition.

## Decision

### 1. `ValidateTeamSettings` reports history availability as an advisory on a success.

After the existing probes pass, the connector reads state-span definitions for the team's named kinds
of work — the read `ServiceNowHistoryQuery` already builds — and maps the outcome:

| `ServiceNowHistoryAvailability` | Result |
|---|---|
| `Available` | `Success()`, no advisory |
| `NoStateMetric` | `SuccessWith("history_no_state_metric", …)` naming the classes with no definition, and what to activate |
| `NoRights` | `SuccessWith("history_requires_itil", …)` naming the role to grant |

It stays an **advisory on a valid result**, never a refusal. A team without time-in-state is a
legitimate configuration — age, WIP, throughput and forecasting all work — so blocking the save would
be a false gate on the majority ITSM case.

One extra request at Save, on a path where a human is already waiting, and never on a refresh.

### 2. The team settings path carries the whole verdict, not a boolean.

`ITeamService.validateTeamSettings` returns the `ConnectionValidationResult` rather than `boolean`.
`useModifySettings` and `useCreateWizard` expose `validationAdvisory` beside the `validationError`
they already expose, and `ModifyTeamSettings` and `CreateTeamWizard` render the existing
`ValidationAdvisory` component.

This is a **shared-contract change** — `validateSettings` is a `useModifySettings`/`useCreateWizard`
option that `ModifyProjectSettings` and `CreatePortfolioWizard` also supply. Per the project's
standing rule, the mock surface (`MockApiServiceProvider`, `createMockTeamService`) is extended before
the contract is edited, so the blast radius is bounded by the compiler rather than discovered by a
failing suite.

### 3. The picker does not warn. The screen the pick lands on does.

The board wizard says nothing about time-in-state. Three reasons, in order of weight:

- **It would reach the wrong people.** Per ADR-126 decision 4, the picker is `SystemAdmin`-only. The
  `flow-coach` who types a class by hand — the primary persona and the majority path — would never
  see it.
- **It would need a contract change.** `BoardInformation` has no advisory field, and adding one puts a
  capability caveat on a pre-fill payload shared by four connectors, three of which have no such
  concept.
- **It would be evaluated on the wrong input.** The advisory is a fact about the team's *final* class
  list, which the user may edit after Confirm. Asserted at pick time it can be stale before Save.

The advisory therefore fires wherever the class list is settled — picker path and manual path alike,
and for the CLI and MCP server, which get the JSON and never read the schema.

## Alternatives Considered

**The picker says it at pick time.** The DISCUSS default reading of OC-6. Rejected on all three
counts in decision 3.

**Nobody says it; #5578's docs carry it.** Zero code, and the honest cheap answer — it makes no false
statement. Rejected as the *primary* answer because the one metric a ServiceNow shop most wants is
time-in-state, and "your change-request team will never have it" is a configuration fact discovered
otherwise only from a chart that is quietly empty. That is the epic's signature failure.
**This remains the live fallback**: if the maintainer wants slice 02 to stay at one day, decline
decision 1 and 2, ship the picker, and let #5578 carry it. Nothing else in the design moves —
ADR-125 and ADR-126 do not depend on this.

**Make `NoStateMetric` a blocking verdict.** Rejected: a team with no time-in-state still gets
throughput, WIP, age and forecasts. Refusing the save would gate the majority ITSM configuration on a
capability it does not need.

**Restore a connection-scope advisory about history.** Rejected because ADR-123 decision 10 withdrew
it for a measured reason that still holds: `metric_definition` holds **0 rows for `table=task`**, so a
connection-scope read would report a missing state metric and tell the administrator to activate one
on `task` — advice that cannot be followed and that contradicts what their teams will get. The
question genuinely has no answer at connection scope; it has one at team scope.

**Surface it as a chart annotation instead.** Rejected for the reason ADR-118 D5 already gave: a
caveat pinned to every chart is noise, while a configuration fact belongs where the configuration is
made, and re-validating clears it.

## Consequences

**Positive.**
- Closes the open item 5611 left in `brief.md`, on the surface where the question has an answer.
- Reuses `ConnectionValidationResult.SuccessWith`, `ValidationAdvisory.tsx` and
  `ServiceNowHistoryVerdict.From` unchanged. No new component, no new copy mechanism, no new endpoint.
- The advisory channel becomes available to every connector's `ValidateTeamSettings`, not just
  ServiceNow's — Jira's and ADO's capability caveats have somewhere to go for the first time.
- Satisfies R-2 properly: the warning ships **with** its reader, in the same change.

**Negative, and named.**
- It touches a shared frontend contract used by team *and* portfolio settings — service, two hooks,
  four components, plus mocks. This was #5610's only scope addition and the reason slice 02 might not
  fit in a day; **resolved 2026-08-01 by splitting delivery into #5627** rather than by declining the
  decision.
- One additional ServiceNow request per team Save.
- An advisory that is correct and repeated is still noise: it renders on every Save of an unchanged
  team. This was defended as acceptable because re-validating is also how granting the role clears it,
  which is ADR-118 D5's own argument. **Objection, maintainer 2026-08-01 — open, settle before #5627
  ships**: that defence holds for `history_requires_itil` and **not** for `history_no_state_metric`.
  On stock `change_request` there is no role to grant — the customer must create a metric definition
  inside ServiceNow — so until they do, every Save shows an advisory they cannot act on from
  Lighthouse. That is nagging, not informing. Resolve by firing only when the class list changed, or
  by wording the copy as a statement of fact rather than a call to action.
- `history_requires_itil` as an advisory code is already asserted in a frontend model test against the
  *connection* path. Reusing the string on the team path is deliberate — the remedy is identical — but
  it means the code no longer identifies the surface, only the cause.

## Related

- [ADR-118](./adr-118-servicenow-transition-history-from-metric-instance-spans.md) decision 5 — the
  advisory concept and the argument for configuration-time over chart-time.
- [ADR-123](./adr-123-servicenow-record-classes-as-work-item-types.md) decision 10 — why connection
  scope cannot answer this, and therefore why team scope must.
- [ADR-125](./adr-125-servicenow-visual-task-board-picker.md) — the pick this advisory follows.
- [ADR-126](./adr-126-board-picker-refusal-channel-and-wizard-reach.md) decision 4 — why the picker's
  audience is too narrow to be the only reader.
