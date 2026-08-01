# Slice 02 — pick a Visual Task Board to pre-fill the query and the kind of work

**Story**: B (`../feature-delta.md`) · **ADO**: #5610 · **Effort**: ~1 day, no new port
**Order**: second (D1). Was hard-blocked twice — by the SPIKE (D11) and by #5611's delivery (D6, D12).
**Both blocks are now clear** (2026-08-01): #5611 is Closed, and the SPIKE ran and confirmed the slice.
See `../spike/findings.md`.

## Goal

A configuration administrator picks a Visual Task Board the team already runs its stand-up from, and
Lighthouse fills in the query and the kind of work from it — both still editable.

## Learning hypothesis

**Confirms** D6/D7 — that a ServiceNow board carries exactly the structure Lighthouse needs (table +
filter), that the existing generic board port is the right home for it, and that turning query
authoring into a picker is a one-provider change rather than a new mechanism.
**Disproves** it if OC-1 (the board table is 403 below `itil`) or OC-2 (board membership is not
expressible as a query) fails against the PDI. Either result **cancels this slice loudly**, the way
SPIKE Q5 cancelled slice 03 — recorded as "the mechanism exists and is unusable for a real customer",
not quietly deferred. Slice 01 then stands as the whole answer to the dogfood finding, which is exactly
why it ships first.

**Result (SPIKE, 2026-08-01): confirmed, slice not cancelled.** OC-1's 200/EMPTY turned out to be a
*sharing* model, not a role wall — board access is granted by `vtb_board_member`, not by `itil`. What
the slice must absorb instead: the list is scoped to the connection's service account, `filter` is the
column-form query to copy while `readable_filter` matches the whole table, and a board's card set
drifts behind its own filter so the filter — not the cards — is the thing being pre-filled.

## IN scope

- `ServiceNowWorkTrackingConnector` implements the existing `IBoardInformationProvider`
  (`GetBoards`, `GetBoardInformation`).
- `WizardsController.GetBoardInformationProviderForWorkTrackingSystem` gains a
  `WorkTrackingSystems.ServiceNow` arm — today ServiceNow falls through to
  `_ => throw new NotImplementedException`.
- One row in `DataRetrievalWizardRegistry.ts` (`servicenow.board`, `applicableSettingsContexts:
  ["team"]`, component `BoardWizard`). ServiceNow's `wizardHint` set for consistency with the other
  four, though nothing reads it (D5).
- Mapping the board to `BoardInformation`: filter → `DataRetrievalValue`, table → a `sys_class_name`
  value in `WorkItemTypes` (D6). Both fields already exist on the contract.
- **Listing only boards that can become a query**, scoped server-side:
  `active=true^tableISNOTEMPTY^filterISNOTEMPTY` (D14). Excludes freeform boards (both empty) and
  table-without-filter boards, whose pre-fill would be an empty query. There is no board-type column to
  ask for instead — no `type` field, `sys_class_name` empty on every row.
- **D9's fix in the shared `BoardWizard`**: a failed board read cannot be confirmed and never
  substitutes an empty pre-fill over a typed query. This changes behaviour for Jira, ADO and Linear
  too — named here so the blast radius is not a review surprise.
- The 403 message says the account cannot read the board table, naming it (AC-B3).

## OUT of scope

- Changing `inputKind` to `wizard-select` (D8) — that would make the query read-only, contradicting
  "manual entry stays primary".
- A per-team table override (5611 Story A) — cancelled on main, and the connection-scope table it
  would have overridden is being removed too. D6 never needed it.
- Portfolio context — ServiceNow portfolios are `inputKind: "none"`.
- Deleting the dead `wizardHint` field. Recorded in the delta, belongs in a cleanup.
- Building a query builder. This slice reads ServiceNow's filter UI output; it does not reimplement it.

## Acceptance criteria

AC-B1..AC-B6 in `../feature-delta.md`.

## Dependencies

- **#5611 delivered** — D12's gate. The pre-fill target itself already exists: 5611 shipped
  `isWorkItemTypesRequired: true` unconditionally for a ServiceNow team (ADR-123 decision 6, amended
  2026-07-31), so the field is visible and required today. What is still in flight on `main` is the
  removal of the connection-scope `Work Item Table` option.
- **OC-1, OC-2 and OC-3 closed against the PDI.** Not buildable until they are.
- **OC-5 answered in DESIGN** — now the narrower question: a board rooted **outside** the `task`
  hierarchy (`cmdb_ci`, `sys_user`, an Agile 2.0 `rm_story`). `sys_class_name` cannot express it from
  a `task` root, and 5611's SPIKE measured `sys_class_name=task` as an exact match rather than
  hierarchy-inclusive, so there is no forgiving fallback. Refuse by name.
- **OC-6 answered in DESIGN**: where the user learns that the board they picked yields no time-in-state
  (D13). Stock `change_request` has no state-tracking definition at all, so a change-request board can
  never produce spans whatever Lighthouse does — and 5611 withdrew the connection-validation advice
  about history rather than rewording it, so there is currently no channel to say it in. The picker is
  the moment the user is choosing, and may be the only place left to say so.

## Reference class

- `LinearWorkTrackingConnector.GetBoards` / `GetBoardInformation` (`:817-880`) — the closest shape:
  list entities as boards, then read one entity's detail into `BoardInformation` including a state
  split. ServiceNow's version is simpler (no state classification to infer) and the paging helper
  already exists on the connector.
- `WizardsController` — the whole file is ~60 lines; the change is one `switch` arm.
- `DataRetrievalWizardRegistry.ts` — four rows, all pointing at one component.

## Pre-slice SPIKE — **DONE** (2026-08-01)

Ran against PDI `dev191338` on 5611's probe accounts. Full evidence in `../spike/findings.md`; the
four questions and what they returned:

1. **Readability** — `vtb_board` is 200/0-rows for `itil`, `snc_read` and no-roles, but the read ACL
   carries **no role**: it runs `VTBBoardSecurity().canAccess(current)`. Adding `lh_probe_itil` to
   `vtb_board_member` produced the board plus its 38 cards and 6 lanes; deleting the row took them
   away again. **Sharing, not roles.** The 200/EMPTY trap held — it just wasn't a role wall this time.
2. **Cards vs filter** — a filtered board's cards are a snapshot *behind* its filter
   (`incident` 38/38, `change_request` **7 cards / 13 matches**). Copy the filter, never the cards.
   Freeform boards store empty `table` and empty `filter`, so D10's refusal needs no heuristic.
3. **What `filter` stores** — the **column** form (`correlation_id=LIGHTHOUSE_DEMO`), safe to copy
   verbatim. `readable_filter` holds the label form and matches the **whole table** (105/105, 118/118),
   which is exactly the widening slice 01's guard exists to catch. Never pre-fill it.
4. **ACL-blind list** — yes. Every 0-row board read returned `X-Total-Count: 2`. Never count boards
   from the header.

Two things this slice now owns that it did not before: the stale claims at
`IServiceNowWorkTrackingConnector.cs:3-5` and `DataRetrievalSchemaDefaults.ts:64` ("ServiceNow has no
board concept") must be amended here, and the **empty-list wording has two indistinguishable causes**
to cover — this account is a member of no board, *and* none of its boards carries both a table and a
filter (D14's scoping). `X-Total-Count` cannot tell them apart; it is ACL-blind. Say both, claim
neither.

## Dogfood moment

Same day: on the PDI, pick the demo board, save the team, and confirm the synced items are **the
board's filter run against the board's table** — not the whole table, and not the board's card set,
which the SPIKE measured drifting behind its own filter. Then repeat the pick as an account that is
not a member of the board and confirm the failure is legible (AC-B3) and names membership rather than
"no boards found", because that is the account a customer will actually use.
