# ADR-126: A wizard refusal keeps its name — the board-picker error channel, the empty list, and who can open a picker at all

- **Status**: **Proposed** (2026-08-01, Story 5610 DESIGN) — pending maintainer ratification.
- **Date**: 2026-08-01
- **Feature**: servicenow-board-picker-and-query-guidance (ADO Story 5610, parent Epic 5513)
- **Deciders**: Benjamin Huser-Berta (maintainer)
- **Builds on**: [ADR-125](./adr-125-servicenow-visual-task-board-picker.md),
  [ADR-114](./adr-114-servicenow-connection-validation-verdict-ladder.md),
  [ADR-006](./adr-006-connection-list-payload-shape.md)
- **Blast radius**: every decision here lands in shared components and changes behaviour for **Jira,
  Azure DevOps and Linear** as well as ServiceNow. Named up front so it is not a review surprise.

## Context

Three things about the board wizard are wrong for all four connectors today, and ServiceNow is the
first connector whose failure modes make them matter.

**1. A failed board read is a silent no-op, not a data loss.** `BoardWizard.tsx:71-82` catches a
failed `getBoardInformation` and substitutes an all-empty `IBoardInformation`, which is truthy, which
enables **Confirm**.

DISCUSS D9 recorded that this *"overwrites whatever the user typed with blanks"*. **Measured against
the code, that is false.** `GeneralSettingsComponent.tsx:59-95` guards every assignment — it writes
`dataRetrievalValue` only when non-empty and each list only when non-empty — so an all-empty
`BoardInformation` writes nothing at all. The real defect is narrower and still real: the user clicks
Confirm on a dialog that says it succeeded, and nothing happens. Quietly wrong beating visibly
missing, in its cheapest form. The fix is the same; the justification has to be honest, or a reviewer
who tests "does it blank my query?" will find that it does not and conclude the fix was unnecessary.

**2. The reason is thrown away.** Every failure the SPIKE found — not a member, wrong hierarchy,
credential rejected — arrives at the same `catch` and is rendered as `"Failed to load boards. Please
try again."`. Retrying fixes none of them. Meanwhile the backend already knows exactly what happened:
`ServiceNowValidationVerdict` and `ServiceNowTeamQueryVerdict` produce `ConnectionValidationResult`s
whose messages name the table, the role to grant, and the correction to make.

**3. The picker is unreachable by the persona who needs it, and the UI does not say so.**
`WizardsController` is `[RbacGuard(RbacGuardRequirement.SystemAdmin)]` while creating a team is
`CanCreateTeam`. `getWizardsForSystem` consults no permission, so a `CanCreateTeam` user sees the
"Select Jira Board" button, clicks it, and gets a 403 rendered as "Failed to load boards. Please try
again." — a lie, today, for three shipped connectors.

The pipeline for doing this properly already exists end to end and is simply not connected:
`BaseApiService.parseApiErrorPayload` turns any 4xx body carrying `{message, technicalDetails,
fieldName}` into an `ApiError` with those fields, and `ConnectionValidationResult` serialises to
exactly that shape.

## Decision

### 1. A board read that fails throws a refusal, and the controller turns it into a 400 that carries the verdict.

A new abstract `WorkTrackingReadException` lives beside the board contracts and carries a
`ConnectionValidationResult`. `ServiceNowReadException` — which already wraps a verdict and already
exposes its `Code` — derives from it. `WizardsController` catches the base type and answers
`BadRequest(verdict)`.

It is created rather than reused for a layering reason: the controller sits on the driving side of
`IBoardInformationProvider` and must not name a ServiceNow type to catch it. Twelve lines buy the
port its own refusal vocabulary, which Jira, ADO and Linear can adopt later without another
controller change.

The alternative — widening `IBoardInformationProvider` to return a result object — is a contract
change across four connectors and their tests, for a case that is an exception in every sense.

### 2. `BoardWizard` renders the reason, and a failed read cannot be confirmed.

The empty-`IBoardInformation` fallback is deleted. On failure the wizard holds `boardInformation` at
`null` — Confirm is already `disabled={!boardInformation}` — and shows `error.message` from the
`ApiError` rather than the canned retry string. A `403` therefore says which table the account cannot
read, by name, which is AC-B3.

### 3. An empty board list is not a verdict. It is an empty list with both of its causes named.

ADR-114's `no_records_visible` rung fires on `200`-with-zero-rows and is a **Failure**. Applying it to
`vtb_board` would refuse the whole picker for a customer whose only mistake is not having shared a
board yet — an action they can take, reported as a fault they cannot.

So the board list intercepts that one rung and reports an empty list instead. Every other rung of
`FromResponse` applies unchanged:

| `vtb_board` answers | Outcome |
|---|---|
| `401` | `authentication_failed` — 400 + verdict |
| `400` | `unknown_table`, naming `vtb_board` — 400 + verdict |
| `403` | `insufficient_permissions`, naming `vtb_board` — 400 + verdict |
| `200`, body not JSON or no `result` array | `unexpected_response` — 400 + verdict |
| **`200`, zero rows** | **empty list, `200 OK`** — the copy below |
| `200`, rows | the list |

The copy names both causes and asserts neither — the house style `no_records_visible` and
`class_records_not_visible` already established:

> No ServiceNow boards are available to this connection. Either the account this connection signs in
> with is not a member of any Visual Task Board, or none of its boards has both a table and a filter
> set — Lighthouse can only use a board that has both. Share a board with that account in ServiceNow,
> or set a filter on one, and try again.

`X-Total-Count` is ACL-blind on `vtb_board` (SPIKE, 2026-08-01: header 2, body 0), so it cannot
separate the two causes and is not consulted.

The interception lives in a new pure `ServiceNowBoardVerdict`, beside the two verdict cores ADR-114
established, and gets the same ArchUnitNET purity fixture. It is a pure function of four scalars;
`FromResponse` is called through, never copied.

### 4. The `/wizards/*` guard stays `SystemAdmin`. The button that leads to it stops lying.

Widening the guard is explicitly **out of scope** for this story, and it should be: it changes who may
enumerate any connection's boards and read back its configured query, for all four connectors, which
is a security decision that deserves its own story rather than a rider on a ServiceNow picker.

What ships instead: `GeneralSettingsComponent` renders wizard buttons only when
`useRbac().isSystemAdmin`. Per the project's standing rule, all UI gating derives from `useRbac()`;
the hook already exposes `isSystemAdmin`, so this is a predicate, not a new fetch. A `CanCreateTeam`
user no longer sees a button that 403s — for ServiceNow **or** for the three connectors where it has
been lying since the wizard shipped.

The constraint itself — a board picker needs a system administrator — goes into #5578's docs, and
Story A (in-product query guidance) remains the half that reaches the `flow-coach` persona. That
ordering was already D1's reason and this decision does not change it.

## Alternatives Considered

**Widen `WizardsController` to `RbacGuardRequirement.AnyScopedAdmin`.** There is real precedent:
`GET /worktrackingsystemconnections/summary` is already `AnyScopedAdmin` for exactly this reason —
populating a dropdown in a wizard a `CanCreateTeam` user is walking through. Rejected here, not
forever: connection *summaries* are `{id, name, type}`, while `/wizards/{id}/boards/{boardId}`
returns a connection's board names and its configured query, to any holder of any scoped admin grant,
for a connection they may have no relationship to. That is a different disclosure and it should be
argued on its own. Recorded as the live option if the maintainer wants the picker to reach
`CanCreateTeam`.

**Add a read-only wizard permission (`CanUseDataRetrievalWizard` or similar).** The most precise
answer, and the most expensive: a new `RbacGuardRequirement` member, a `RbacAdministrationService`
arm, an API-key scope mapping, a claims-driven test double, and a migration of whatever grants it.
Disproportionate to a picker.

**Leave the guard and the button both as they are, and document the constraint in #5578.** The
literal DISCUSS position, and the cheapest. Rejected because it is not actually zero-cost: it leaves
three shipped connectors rendering a button whose only outcome for a `CanCreateTeam` user is a
mistranslated 403. Documenting a lie does not stop it being one.

**Return the refusal inside `BoardInformation` as a nullable reason field.** Avoids the exception
type. Rejected: it puts a failure channel on a success type, which is exactly the shape that produced
the truthy-all-empty bug being fixed here, and it changes a contract four connectors share.

**Keep the empty-`IBoardInformation` fallback and disable Confirm separately.** Rejected: the
fallback's only purpose was to enable Confirm. Two mechanisms where one suffices, and the wrong one
survives the next refactor.

## Consequences

**Positive.**
- Every refusal the SPIKE catalogued reaches the user with the words the backend already wrote for
  it. No new copy for `401`, `400`, `403` or a rewritten error envelope.
- Jira, ADO and Linear get the same repair: a failed board read stops being a confirmable no-op, and
  their wizard buttons stop appearing to users who cannot use them.
- The empty-list case stays a `200`, so a customer with no shared board is not told the connection is
  broken.

**Negative, and named.**
- **A `CanCreateTeam` user loses a button they could see before.** For three connectors that button
  never worked for them; the change makes an existing constraint visible rather than imposing a new
  one. It will still read as a regression to anyone who did not know the 403 was there, so it belongs
  in release notes.
- **Frontend tests for three connectors' wizard buttons now need an RBAC-aware render.** The mock
  surface exists (`useRbac` is already mocked across the suite); the cost is breadth, not depth.
- `WorkTrackingReadException` is a new type that only one connector throws on day one. Justified by
  the layering rule, but it is one more thing to keep honest — a probe that no adapter's board method
  leaks a connector-specific exception past the port belongs in the ArchUnitNET fixture.

## Related

- [ADR-125](./adr-125-servicenow-visual-task-board-picker.md) — what the picker reads and what it
  refuses. This ADR is how those refusals arrive.
- [ADR-114](./adr-114-servicenow-connection-validation-verdict-ladder.md) — the ladder decision 3
  reuses, and the single rung it intercepts.
- [ADR-006](./adr-006-connection-list-payload-shape.md) — the `AnyScopedAdmin` connection-summary
  precedent decision 4 weighs and declines to follow.
