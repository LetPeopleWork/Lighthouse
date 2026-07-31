# Slice 02 — pick a Visual Task Board to pre-fill the query and the kind of work

**Story**: B (`../feature-delta.md`) · **ADO**: #5610 · **Effort**: ~1 day, no new port
**Order**: second (D1). Hard-blocked twice: by the SPIKE (D11) and by #5611 slice 01 (D6, D12).

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
- **D9's fix in the shared `BoardWizard`**: a failed board read cannot be confirmed and never
  substitutes an empty pre-fill over a typed query. This changes behaviour for Jira, ADO and Linear
  too — named here so the blast radius is not a review surprise.
- The 403 message says the account cannot read the board table, naming it (AC-B3).

## OUT of scope

- Changing `inputKind` to `wizard-select` (D8) — that would make the query read-only, contradicting
  "manual entry stays primary".
- A per-team table override (5611 Story A). D6 exists to avoid needing it.
- Portfolio context — ServiceNow portfolios are `inputKind: "none"`.
- Deleting the dead `wizardHint` field. Recorded in the delta, belongs in a cleanup.
- Building a query builder. This slice reads ServiceNow's filter UI output; it does not reimplement it.

## Acceptance criteria

AC-B1..AC-B6 in `../feature-delta.md`.

## Dependencies

- **#5611 slice 01 delivered** — D6 pre-fills Work Item Types, which is hidden for a ServiceNow team
  until 5611 makes `isWorkItemTypesRequired` conditional. Also D12's gate.
- **OC-1, OC-2 and OC-3 closed against the PDI.** Not buildable until they are.
- **OC-5 answered in DESIGN**: what happens when the board's table is not under the connection's
  configured table. No forgiving fallback exists — 5611's SPIKE measured `sys_class_name=task` as an
  exact match, not hierarchy-inclusive.
- **OC-6 answered in DESIGN**: where the user learns that the board they picked yields no time-in-state
  (D13). `metric_definition` has zero rows for `table=task`, and stock `change_request` has no
  state-tracking definition at all — so a change-request board can never produce spans, whatever
  Lighthouse does. The picker is the moment the user is choosing, and therefore the moment to say so.

## Reference class

- `LinearWorkTrackingConnector.GetBoards` / `GetBoardInformation` (`:817-880`) — the closest shape:
  list entities as boards, then read one entity's detail into `BoardInformation` including a state
  split. ServiceNow's version is simpler (no state classification to infer) and the paging helper
  already exists on the connector.
- `WizardsController` — the whole file is ~60 lines; the change is one `switch` arm.
- `DataRetrievalWizardRegistry.ts` — four rows, all pointing at one component.

## Pre-slice SPIKE

**Mandatory**, against PDI `dev191338`. The maintainer's call was to combine it with #5611's
OC-1/OC-2/OC-3, but 5611's SPIKE landed first (`1c3cbf58c`) and settled all three, so this is now a
short standalone run on scaffolding that is already up: probe accounts `lh_probe_none`,
`lh_probe_snc_read` (`sn_incident/change/request_read`, deliberately no `sn_problem_read`) and
`lh_probe_itil`, sharing the admin password in `$ServiceNowLighthouseIntegrationTestToken`.

Four things to measure, all one-request experiments:

1. **Readability, as a role matrix** — read the Visual Task Board table as no-roles / `snc_read_only` /
   `sn_*_read` / `itil` / `admin`, exactly as `spike/findings.md` did for `sys_choice` and
   `metric_instance`. **Treat `200/EMPTY` as a denial, not an empty instance** — that trap is already
   documented and already cost this epic once.
2. **Board membership vs stored filter** — for a filtered/guided board and a freeform board, compare
   the board's card set against the result of running its stored filter. Divergence on the freeform
   board is the expected answer and triggers D10; divergence on the *filtered* board would sink the
   whole slice.
3. **What the filter column actually stores** — the label form (`Correlation ID=…`) or the column form
   (`correlation_id=…`). If it is the label form, pre-filling it verbatim ships the exact query slice
   01's widening guard exists to catch, and the provider has to translate before it hands anything over.
4. **Whether the board list is ACL-blind** (OC-7). 5611's SPIKE found `X-Total-Count` reports 103 rows
   to an account whose body comes back empty. One read as `lh_probe_none` says whether the picker can
   list a board it cannot actually read — the same denial-wearing-a-success-costume trap the epic has
   now hit at three different layers.

## Dogfood moment

Same day: on the PDI, pick the demo board, save the team, and confirm the synced items are the board's
items — not the whole table, not a subset. Then repeat the pick as a least-privilege account and
confirm the failure is legible (AC-B3), because that is the account a customer will actually use.
