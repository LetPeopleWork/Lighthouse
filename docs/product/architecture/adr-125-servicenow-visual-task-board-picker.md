# ADR-125: ServiceNow Visual Task Boards are boards — the existing wizard port, the live filter, and the shipped class ladder

- **Status**: **Accepted** (ratified 2026-08-01 by the maintainer, at Epic 5513's close).
- **Date**: 2026-08-01
- **Feature**: servicenow-board-picker-and-query-guidance (ADO Story 5610, parent Epic 5513)
- **Deciders**: Benjamin Huser-Berta (maintainer)
- **Builds on**: [ADR-123](./adr-123-servicenow-record-classes-as-work-item-types.md),
  [ADR-124](./adr-124-servicenow-record-class-readability-ladder.md),
  [ADR-114](./adr-114-servicenow-connection-validation-verdict-ladder.md)
- **Reverses a claim in**: [ADR-116](./adr-116-servicenow-table-at-connection-scope.md) — the assertion,
  carried in `IServiceNowWorkTrackingConnector.cs:3-5` and `DataRetrievalSchemaDefaults.ts:64` since
  `4b55362be`, that *"ServiceNow has no board concept"*. It has two on a stock PDI.

## Context

Epic 5513's first real user stopped at the ServiceNow query field. Nothing in the product said what an
encoded query is, and [ruling R-2](../../feature/epic-5513-servicenow-integration/feature-delta.md)
makes a missing query a blocking verdict — so the connector's first impression is a refusal with no
instruction. A ServiceNow shop's team boundary usually already exists, as a Visual Task Board, and a
board carries exactly the two things a Lighthouse team needs.

The SPIKE of 2026-08-01 (`docs/feature/servicenow-board-picker-and-query-guidance/spike/findings.md`,
PDI `dev191338`) measured five facts that bind this decision:

1. **`vtb_board` exists and carries `table` + `filter`.** On the dogfood board: `table='incident'`,
   `filter='correlation_id=LIGHTHOUSE_DEMO'`. The interface xmldoc's first claim is false.
2. **Boards are shared, not roled.** The read ACL on `vtb_board` carries **no role**; it runs
   `VTBBoardSecurity().canAccess(current)`. `lh_probe_itil` read 0 boards, was added to
   `vtb_board_member`, read the board plus 38 cards and 6 lanes, and read 0 again after the row was
   deleted — the same 46 roles throughout. The list is therefore scoped to *the connection's service
   account*, and `vtb_board_member` is admin-write, so Lighthouse cannot grant itself access.
3. **`readable_filter` is poison.** `filter` is a verbatim encoded query in **column** form and
   returns 38 of 105 incidents. `readable_filter` is the **label** form — the string ServiceNow's own
   UI displays, and therefore the one a careless implementation would prefer for being legible — and
   run as `sysparm_query` it matches **105 of 105** and **118 of 118**. That is precisely the widening
   `query_matches_whole_table` exists to block.
4. **Cards are a snapshot behind the filter** (`change_request`: 7 cards, 13 matches). The filter is
   live; the card set drifts. Freeform boards store empty `table` **and** empty `filter`, and there is
   **no board-type column** — no `type` field, `sys_class_name` empty on every row, one table with no
   subclasses. Emptiness is the whole discriminator.
5. **A non-task board is real and unreachable.** A `cmdb_ci` board is creatable
   (`table='cmdb_ci'`, `filter='operational_status=1'`) and `task?sys_class_name=cmdb_ci` returns 0.
   Pre-filling it produces a team that syncs nothing — the quiet wrong number this epic exists to
   prevent.

`X-Total-Count` is ACL-blind on `vtb_board`, `vtb_card` and `vtb_lane` alike (header 2, body 0),
confirming on a second surface the defect ADR-124 recorded on `incident`.

## Decision

### 1. ServiceNow joins the existing board port. No new port, no new endpoint, no new dialog.

`IServiceNowWorkTrackingConnector` extends `IBoardInformationProvider`;
`WizardsController.GetBoardInformationProviderForWorkTrackingSystem` gains its
`WorkTrackingSystems.ServiceNow` arm (today it falls through to `NotImplementedException`); and
`DataRetrievalWizardRegistry.ts` gains one row — `servicenow.board`, `applicableSettingsContexts:
["team"]`, component `BoardWizard`. The portfolio context is excluded because ServiceNow portfolios
are `inputKind: "none"` (ADR-116).

`Board` and `BoardInformation` are **unchanged**. `BoardInformation.DataRetrievalValue` already exists
for the query and `WorkItemTypes` already exists for the classes ADR-123 shipped, so the pre-fill
needs zero contract change across four connectors.

**Amended 2026-08-01 (#5612's OC-4).** `WorkItemTypes` is pre-filled with the board table's **label**
— `Change Request`, not `change_request` — through `ServiceNowClassLabels.LabelFor`. ADR-128 made a
team's work items report the kind of work in the words that team was configured with, and deleted the
save-time normalisation that would otherwise have caught a class name later; a class-name pre-fill
would therefore leave the team syncing correctly and forecasting nothing. Still zero contract change:
the field is the same field, carrying a different string. An unmapped class passes through unchanged.

`inputKind` stays **`freetext`**. Linear's `wizard-select` makes the field read-only
(`GeneralSettingsComponent.tsx:126`), and manual entry stays the primary path.

### 2. The board's `filter` is the query. `readable_filter` is never read, anywhere.

`DataRetrievalValue` ← `filter`, verbatim, no translation and no parsing.

`readable_filter` is not carried onto the contract at all — not as a description, not as a dialog
caption, not as a field the mapper reads and discards. The SPIKE showed it is both the tempting
string and the whole-table string; a field that holds it is a field a future change can pre-fill
from. The bug class is made **non-representable** rather than tested around.

### 3. The list is scoped by the board row, server-side, and is a promise about the account.

`vtb_board?sysparm_query=active=true^tableISNOTEMPTY^filterISNOTEMPTY`. Both fields, not just `table`:
a board with a table and no filter pre-fills an empty query, which `ValidateTeamSettings` then blocks —
cheaper to exclude than to render and refuse. A freeform board (both empty) never reaches the user.

Boards are **never counted from `X-Total-Count`** (fact 5 above); the list is the body.

`GetBoardInformation` **re-applies the same scoping** on its single-board read
(`sys_id={boardId}^active=true^tableISNOTEMPTY^filterISNOTEMPTY`) rather than trusting the list it
served a moment ago. A board that stopped qualifying in between is refused, not pre-filled with
blanks.

### 4. The board's `table` is validated by the ladder ADR-124 already shipped. No new mechanism, no static list.

A board's `table` is a candidate `sys_class_name`, so `GetBoardInformation` runs the **existing**
two-probe ladder (`WhyThisKindOfWorkCannotBeRead`, ADR-124 decision 2) against it:

| `task?sys_class_name={table}` | `/{table}` | verdict |
|---|---|---|
| header > 0, rows visible | *not asked* | accept — pre-fill |
| header > 0, no rows visible | *not asked* | `class_records_not_visible` |
| non-200 / no record set | — | ADR-114's rung, naming `task` |
| header = 0 | `400` | `unknown_table`, naming the board's table |
| header = 0 | `200`, header > 0 | **`class_is_not_a_kind_of_work`** — the `cmdb_ci` case |
| header = 0 | `200`, header = 0 | accept — a kind of work this instance has none of yet (ADR-124 OQ-8) |

The message `class_is_not_a_kind_of_work` already names `sys_user` and `cmdb_ci` explicitly. It was
written for a class a coach typed; it is exactly as true for a class a board named.

This costs one request for a correct board and two for a wrong one, at the one moment a human is
already waiting on a click — the budget ADR-124 already accepted for Save.

**Correction to the SPIKE record.** `findings.md` and `wave-decisions.md` both state that
`sys_db_object` is *"403 below `itil`"* and conclude that a precise hierarchy check is unavailable.
That is stale: 5611's own post-DESIGN addendum measured `sys_db_object` at **200 for three of the four
probe accounts** — 403 only for `lh_probe_none`, which can read nothing at all. The premise is wrong;
the conclusion survives for a different and better reason, recorded by 5611 as a standing ruling:
**do not build on `sys_db_object`**, because an account that cannot read it cannot read any class
either, so the first rung fires first and the ambiguity never reaches a user who could act on it.

## Alternatives Considered

**Refuse on evidence — run the board's filter scoped by `sys_class_name`, refuse on an empty result.**
(SPIKE candidate 1.) One request, cheap. Rejected: it cannot separate *wrong hierarchy* from
*legitimately empty board*, and refusing a real board because its filter matches nothing today is an
accusation about the one thing that is definitely not wrong — the same error `NoWorkSelected` was
written to avoid. ADR-124's second probe resolves exactly that ambiguity, at the same cost, and is
already shipped and already tested.

**A static list of known ITSM task descendants, unknown-but-warned as the fallback.** (SPIKE
candidate 2.) Rejected on two counts. It re-introduces `ServiceNowTableHierarchy`, a construct 5611
built and then **deleted** when the connection-scope table was withdrawn — nothing in the backend
holds a hierarchy set today, and adding one back means a new pair of twinned constants under the
#5613 guard for a question the instance can answer directly. And a static list cannot know a
customer's own `task` extension, which is the D3 failure mode 5611 closed rather than reopened.

**Read `sys_db_object` to verify descendance precisely.** Now known to be *available* to the accounts
that matter, contrary to what the SPIKE record says. Rejected anyway, per 5611's standing ruling: it
buys a distinction that never reaches an actionable user, and it makes the picker depend on a
platform table whose readability has drifted once already inside this epic.

**Pre-fill from the board's `vtb_card` set instead of its filter.** Rejected on measurement: cards
are a snapshot (7 against 13), so Lighthouse would inherit a set that is already stale at pick time
and drifts further. AC-B6 was amended upstream for the same reason.

**Show `readable_filter` in the dialog as a human-readable description.** Tempting and rejected — see
decision 2.

**Exclude non-task boards from the list rather than refusing them.** Rejected: exclusion by table
would need the ladder run per board at list time — N boards × up to 2 requests — and it hides the
one case where the user has a real board and deserves to be told why Lighthouse will not use it.
Emptiness is decidable from the row and is excluded (decision 3); hierarchy is not, and is refused by
name.

## Consequences

**Positive.**
- The picker is one provider implementation, one `switch` arm and one registry row. No contract
  change, no migration, no new endpoint.
- The board's boundary and Lighthouse's boundary are visibly the same object, which is the shortest
  path to a correctly-scoped ServiceNow team now that the connection-scope table is gone.
- The two failure modes the epic fears most — a widened query and a class that syncs nothing — are
  both refused by machinery that already exists and is already asserted.

**Negative, and named.**
- **The list is a promise about the service account, not about the instance.** A customer whose
  admins own every board sees an empty picker until somebody adds the Lighthouse account to
  `vtb_board_member`. Lighthouse cannot do this for itself, so it is an onboarding step for #5578
  and nothing else.
- **The empty list has two indistinguishable causes** — not a member of any board, and no board
  carries both a table and a filter — and `X-Total-Count` is ACL-blind, so nothing can separate them.
  The copy names both and asserts neither (see [ADR-126](./adr-126-board-picker-refusal-channel-and-wizard-reach.md)).
- **A board pick replaces `workItemTypes` outright**, it does not merge
  (`GeneralSettingsComponent.tsx:68-73`). That is existing shared behaviour and is correct for a
  picker, but it means picking a second board discards the first board's class.
- Two source comments become false the moment this ships and must be amended in the same change:
  `IServiceNowWorkTrackingConnector.cs:3-5` and `DataRetrievalSchemaDefaults.ts:64`.

**Standing substrate assertions** (Earned Trust — these are instance behaviour a vendor release can
change underneath us, and getting any of them wrong ships the bug the epic's validation exists to
catch). Added to `ServiceNowWorkTrackingConnectorIntegrationTest` alongside 5611's ladder:
`filter` run as `sysparm_query` selects a proper subset; `readable_filter` run the same way selects
the whole table; `X-Total-Count` on `vtb_board` reports rows a non-member cannot see; a non-member's
board read is `200`-with-zero-rows and never `403`.

## Related

- [ADR-114](./adr-114-servicenow-connection-validation-verdict-ladder.md) — the verdict ladder this
  reuses, and the one rung it deliberately does not (ADR-126 decision 3).
- [ADR-123](./adr-123-servicenow-record-classes-as-work-item-types.md) — the `task`-rooted read and
  `Team.WorkItemTypes` as record classes, which is what the board's `table` pre-fills.
- [ADR-128](./adr-128-servicenow-record-class-labels-are-connector-local.md) — the label map. Decision
  1's amendment above is that ADR's OC-4, answered: the picker pre-fills the label, while decision 4's
  ladder still probes the record class.
- [ADR-124](./adr-124-servicenow-record-class-readability-ladder.md) — the two-probe ladder decision 4
  reuses unchanged. Its applicability widens from "a class a coach typed" to "a class anything
  proposed"; no rung changes.
- [ADR-126](./adr-126-board-picker-refusal-channel-and-wizard-reach.md) — how a refusal reaches the
  user, and who can open the picker at all.
- [ADR-127](./adr-127-team-settings-advisory-channel.md) — where a user learns that the class they
  just picked yields no time-in-state.
