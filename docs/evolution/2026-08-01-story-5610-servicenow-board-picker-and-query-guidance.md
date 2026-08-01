# Story 5610 — ServiceNow query guidance and the Visual Task Board picker

**ADO**: User Story [#5610](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5610),
parent Epic [#5513](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5513).
**Delivered**: 2026-08-01, two slices, plus a SPIKE.
**Workspace**: `docs/feature/servicenow-board-picker-and-query-guidance/` — retained; this document is
the summary, that directory is the history.

> **This story is archived with work still open.** See *Open at close* — the mutation numbers are
> stale and three ADRs are unratified. Screenshots, the epic back-reference and the ADO transition
> closed later the same day under #5578, and `main`'s backend failure was fixed by `5fad1b84b`; both
> rows are updated in place. Read that section before treating the story as done.

## What it was for

R-2's accepted cost, arriving on the first real user within minutes: a flow coach configuring a
ServiceNow team faces an empty query field with no idea what to type, and a missing query blocks the
save. Two separable halves.

**Slice 01 — say what to type.** The schema gains a placeholder and help text, on both stacks, and
shows them where the team is actually created.

**Slice 02 — do not make them type it.** A ServiceNow Visual Task Board already carries a table and a
stored filter. Pick the board you maintain and Lighthouse copies both.

## The shape of the answer

### Slice 01 — two nullable fields, and one test that was pointing at nothing

`DataRetrievalSchemaDto` gained `Placeholder` and `HelpText`, mirrored into the TypeScript twin so
#5613's exhaustiveness guard still passes. (Bug #5613's lesson holds: that schema table is duplicated,
and adding to it means editing both stacks.)

The dogfood caught what the gate did not. **AC-A5 asserted that team settings and the create-team
wizard "render through the same `GeneralSettingsComponent`". They do not** — the create wizard has its
own `TextField` in `CreateWizardShell`. So the test asserted against a component the wizard never
renders, passed, and the exact surface the slice exists to fix showed nothing. Tested but unwired, in
the worst possible place. AC-A4 was withdrawn outright: the field copy is now one actionable
instruction and no longer names the two silent-failure modes, which are handed to #5578's docs. **If
#5578 does not carry them they are lost.**

### Slice 02 — the picker

`IServiceNowWorkTrackingConnector` extends `IBoardInformationProvider`, `WizardsController` gains its
ServiceNow arm, `BoardInformation.DataRetrievalValue` ← the board's `filter`, `WorkItemTypes` ← its
`table`. No contract change; the mechanism already ran end to end for three other connectors.

Boards are listed as `active=true^tableISNOTEMPTY^filterISNOTEMPTY` (D14). Both fields, not just
`table`: a board with a table and no filter pre-fills an empty query.

## Decisions worth keeping

| | Decision | Why |
|---|---|---|
| ADR-125 | The board picker reuses ADR-124's readability ladder to refuse a non-task board | `sys_db_object` is the wrong instrument — an account that cannot read it cannot read any class either, so the ladder answers first |
| ADR-126 | A wizard refusal keeps its name; the empty list and the refusal are distinct rungs | "No boards found" is wrong when the truth is "nobody has shared a board with this account" |
| ADR-128 am. | The picker pre-fills the **label**, the ladder probes the **raw class** | Two forms of one value. Backwards refuses every change board on the instance — written down in both ADRs and at the call site |
| SPIKE OC-1 | Boards are **shared, not roled** | `vtb_board`'s read ACL carries no role and runs `VTBBoardSecurity().canAccess(current)`. Role inventories predict nothing; the list is per-service-account |
| SPIKE OC-3 | Copy `filter`, never `readable_filter` | `filter` is the encoded query in column form; `readable_filter` is the label form and matches the **whole table** (105/105, 118/118) — the exact widening `ValidateTeamSettings` blocks |
| SPIKE OC-2 | A board's card set is a snapshot **behind** its filter | 7 cards against 13 matches. So AC-B6 was amended: the synced set equals the board's filter run against its table, not its card set. A correct implementation fails the original wording |
| — | `ReadEveryPage` needed a per-table termination rule | `X-Total-Count` is ACL-blind on the board tables (header 2, body 0). Following it reads body 1 of header 4, requests `offset=1`, trips the repeated-record guard and refuses the **whole picker**. Resolved additively with a defaulted sizing parameter; three existing callers stay byte-identical |
| — | Lanes needed nothing | `vtb_lane.name` is already the display label, which is the form Lighthouse's state mapping is written in |

**ADRs**: ADR-125 and ADR-126 are new (both **Proposed**). ADR-124 amended and still **Proposed**.
ADR-128 amended to expose the class→label direction its own remarks had reserved for this moment.

## What the merge broke, and why no gate saw it

`f83cbbae6` is the one worth remembering. Merging `origin/main` brought #5612's deletion of the
validation advisory channel, which took `ConnectionValidationResult.SuccessWith` and the
`Advisory`/`AdvisoryCode` pair with it. **The board picker's empty-list rung was the one caller that
arrived afterwards, so the merge compiled cleanly on both sides and broke in the middle.**

The rung was rebuilt on `Code`/`Message` rather than by reviving the channel — the maintainer's ruling
three commits earlier was that zero callers means delete it. Nothing reads the message either way:
`GetBoards` asks only whether the verdict is valid, and `BoardWizard.tsx` owns the empty-list copy the
administrator actually sees.

**Consequence for #5627**: ADR-127 was accepted as design with delivery split out to that story, and
it builds on ADR-118 decision 5 — the advisory. That channel is now gone. **#5627 needs a new channel
before it can ship its design.**

## Mutation — measured, then invalidated

Run 2026-08-01 against `main` @ `23e23afc5`: backend **89.37 %** (381 tested), frontend **94.44 %**
(180 tested). Both over the 80 % gate. Every file the feature created scored 100 %.

The frontend pass is the finding worth keeping. **`hasEveryConfigInput` had 32 survivors and now has
zero.** It is the wizard's config gate, extracted in 02-05 to fix a defect the maintainer hit while
dogfooding — a board wizard returning no states dropped the user on *Name & Create* with nothing
mapped. Every clause could be mutated without a test noticing, because the suite's only failing case
was *nothing filled in at all*, under which no individual clause carries weight. One 6-row `it.each`
— each row valid everywhere but one clause — closed the cluster. The fix for a real, user-found bug
was effectively unpinned, and the score is what said so.

**These numbers no longer describe the shipped tree.** `f83cbbae6` and `cfda81ea1` changed code after
the run. The invalidation is not merely "two commits later": the backend record's own explanation of
the `ServiceNowBoardVerdict` survivors is written in terms of `SuccessWith` populating
`AdvisoryCode`/`Advisory` — **the API `f83cbbae6` deleted**. The pure core that scored 100 % was
rewritten underneath the measurement.

**The maintainer accepted this and chose not to re-run** (2026-08-01). Recorded so the next reader knows
the published scores are a measurement of `23e23afc5`, not of what shipped — not so that someone
re-derives the drift from scratch.

One honest gap carried forward: `ServiceNowWorkTrackingConnector.cs` L173, the lane read's
`CarriesRecords` ternary. Killing it means teaching `StubbedInstance` to page and then refuse page 2.
A truncated flow must map **no** states rather than an invented split, so it is recorded as unfinished
rather than dressed up as equivalent.

## Process notes

- **The SPIKE was promoted DISCARD, and that was the right call.** DISCUSS had recorded WS strategy C,
  no walking skeleton, on the grounds that both driving ports already run end to end for three
  connectors and the unproven parts were ServiceNow-instance facts. The probe answered exactly those
  facts, so the reason not to skeleton was *stronger* after the run than before it.
- **The probe overturned three written assumptions** (OC-1, OC-2, OC-3) and corrected a stale one of
  its own (`sys_db_object` 403-below-`itil`). Findings, not code, were the deliverable.
- **Both defects that mattered were found by dogfooding, not by a gate** — the unwired AC-A5 pin and
  the config-gate drop-through. Same pattern as slice 04's dogfood.

## Open at close

| Item | State |
|---|---|
| Mutation re-run on the shipped tree | **Open by decision** (maintainer, 2026-08-01). Measured numbers predate `f83cbbae6` + `cfda81ea1`, and the verdict core was rewritten under them |
| `main` backend test failure | **Red** at time of writing — run [30705170589](https://github.com/LetPeopleWork/Lighthouse/actions/runs/30705170589), step `Test Backend (with Coverage)`, filter `Category!=Integration\|Category=ServiceNowIntegration`. Fixed the same day by `5fad1b84b` (the release service raced the update process it should have waited for) |
| ADR-124, ADR-125, ADR-126 | **Proposed** — pending maintainer ratification |
| ADR-127 / #5627 | Needs a **new** channel; the one it was designed against was deleted by #5612 |
| DoD 6 — screenshots per theme | **Closed** by #5578 on 2026-08-01. `worktrackingsystem_ServiceNow.png` and `servicenow_wizard.png`; the board-picker shot is taken by the new ServiceNow E2E against the live PDI |
| DoD 8 — epic back-reference | **Closed** by #5578. Both waiting items are done, and the Board Wizard section was re-read against `ServiceNowBoardMapper` and corrected |
| DoD 9 — ADO transition + Release Notes tag | ADO **Closed**. The Release Notes tag sits on Epic 5513 rather than on this story |
| AC-A4's two silent-failure modes | Handed to #5578's docs. No in-product home |
