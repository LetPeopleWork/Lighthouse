# SPIKE Decisions — servicenow-board-picker-and-query-guidance

**ADO**: [#5610](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5610) · **Run**: 2026-08-01,
PDI `dev191338`, ~35 min of a 1 h timebox.

## Assumption tested

Can Lighthouse turn a ServiceNow Visual Task Board into a configured team — is the board table
readable by the account a customer would connect with, is a board's membership expressible as a query,
and is the stored filter safe to copy verbatim? (OC-1, OC-2, OC-3, OC-7 from DISCUSS.)

## Probe verdict

**WORKS** — the mechanism is there and slice 02 is buildable, but three written assumptions are wrong.
Full evidence in `findings.md`.

- **OC-1 SETTLED**: not a role gate. `vtb_board`'s read ACL carries no role and runs
  `VTBBoardSecurity().canAccess(current)`. `lh_probe_itil` read 0 boards, was added to
  `vtb_board_member`, read the board plus 38 cards and 6 lanes, and read 0 again after the row was
  deleted — same 46 roles throughout. Boards are **shared, not roled**, so the picker's list is scoped
  to the connection's service account.
- **OC-7 SETTLED**: `X-Total-Count` is ACL-blind on `vtb_board`/`vtb_card`/`vtb_lane` (header 2, body 0),
  confirming 5611's `incident` finding generalises.
- **OC-3 SETTLED**: `filter` is a verbatim encoded query in **column** form and is the one to copy;
  `readable_filter` is the label form and matches the **whole table** (105/105, 118/118) — the exact
  widening `ValidateTeamSettings` blocks.
- **OC-2 SETTLED — no**: a filtered board's cards are a snapshot behind its filter (change_request:
  7 cards, 13 matches). Freeform boards store empty `table` and empty `filter`, so D10's refusal is
  decidable from the board row alone.
- **OC-5 (narrowed)**: a `cmdb_ci` board is creatable and yields nothing from a task-rooted read.
  Refusal is required; verifying task-descendance is blocked by `sys_db_object` 403 below `itil`.

## Promotion decision

**DISCARD** — maintainer, 2026-08-01.

The findings are the deliverable. DISCUSS already recorded WS strategy **C, no walking skeleton**, on
the grounds that both driving ports (`/wizards/*`, the settings screen) run end to end for three other
connectors and the unproven parts were ServiceNow-instance facts. The probe answered exactly those
facts, so the reason not to skeleton is stronger after the run than before it. Probe scripts deleted;
`findings.md` and this file are the artifacts DESIGN reads.

## Upstream correction applied

**AC-B6 was unsatisfiable as written** and has been amended in `feature-delta.md` (dated amendment,
2026-08-01) rather than left for DESIGN. It asked that "the synced item set equals the board's card
set"; the probe measured that a board's card set drifts behind its filter, so a *correct*
implementation fails that assertion. It now reads: the synced set equals the board's **filter** run
against the board's **table**. That is the invariant the probe verified on both boards.

## Design implications

1. `IServiceNowWorkTrackingConnector` extends `IBoardInformationProvider`; `WizardsController`'s switch
   gains its ServiceNow arm; `BoardInformation.DataRetrievalValue` ← `filter`, `WorkItemTypes` ←
   `table`. **No contract change.** D6 and D7 hold.
2. The list is per-service-account. Empty-list copy must say "this account is not a member of any
   board", not "no boards found" — R-2's lesson on a new surface.
3. Sharing the board with the Lighthouse account is an onboarding step Lighthouse cannot perform
   (`vtb_board_member` is admin-write). It belongs in #5578's docs.
4. **Settled as D14** (maintainer, 2026-08-01): exclude at query time — list boards as
   `active=true^tableISNOTEMPTY^filterISNOTEMPTY`. Both fields, not just `table`: a board with a table
   and no filter pre-fills an empty query. "Only data-driven boards" is not expressible — there is no
   board-type column — and does not need to be, since Lighthouse copies the live filter rather than the
   drifting card set. A board whose `table` is not a task descendant is still a *refusal*, not an
   exclusion, and needs a detection strategy that survives `sys_db_object` 403 — open for DESIGN, two
   candidates in `findings.md`.
5. D9's empty-fallback fix in `BoardWizard.tsx` is load-bearing: every failure mode found here
   currently arrives as a truthy all-empty `IBoardInformation` that enables Confirm.
6. The stale comments in `IServiceNowWorkTrackingConnector.cs:3-5` and
   `DataRetrievalSchemaDefaults.ts:64` assert ServiceNow has no board concept. Slice 02 must amend both.

## Constraints discovered

- Board access is script-guarded membership, not roles — role inventories predict nothing.
- `vtb_board_member` is admin-write; Lighthouse cannot grant itself access.
- `X-Total-Count` is ACL-blind on the board tables.
- `sys_class_name` is empty on every `vtb_board` row: one table, no subclasses, no type column.
- `sys_db_object` stays 403 below `itil` (5611), so task-descendance cannot be verified by the accounts
  that most need the refusal.

## Still open after this run

- **OC-4** (the `/wizards/*` SystemAdmin guard vs `CanCreateTeam`) — maintainer call, untouched by a
  probe.
- **OC-6** (where a user learns a picked class yields no time-in-state) — no channel exists; unchanged.
