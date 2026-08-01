# ServiceNow: knowing what to type in the query field — feature delta

**ADO**: User Story [#5610](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5610), parent Epic
[#5513](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5513) (ServiceNow Integration).
**Waves recorded here**: DISCUSS (2026-07-31), SPIKE (2026-08-01 — `spike/findings.md`,
`spike/wave-decisions.md`).

**Why this file and not the epic's.** Same reason 5611 got its own workspace: epic 5513's
`feature-delta.md` is the record for slices 01–05 and is still being appended to (5578 is open, 5577
finalized on 2026-07-31). 5610 is a post-DESIGN story filed out of the slice-02 dogfood, not one of
the epic's slices. Sibling workspace:
`docs/feature/servicenow-multi-table-work-item-types/` (#5611), which this feature depends on.

---

## Wave: DISCUSS / [REF] Persona and job

**Primary persona**: `flow-coach` (`docs/product/personas/flow-coach.yaml`) — the person creating the
Lighthouse team, who is the one staring at the blank query field.
**Secondary**: `config-admin` — owns the connection, and is the only persona who can reach the board
picker at all (see OC-4).

**JTBD one-liner** — new SSOT job `job-snow-team-creator-author-a-query`:

> When I am pointing a Lighthouse team at ServiceNow work and the query field is blank, I want the
> product to tell me what a ServiceNow query is and where to get one — or hand me one from a board I
> already maintain — so I can finish creating the team instead of abandoning it.

Sits in front of `job-snow-flow-coach-see-flow-metrics` (epic 5513) rather than refining it. That job
is delivered and works; this one is the gate a user has to get through before any of it happens.

**Where the need came from**: maintainer dogfood of slice 02, 2026-07-29, recorded in
`docs/feature/epic-5513-servicenow-integration/feature-delta.md` (Dogfood findings, "#5610" row). The
connection wizard worked, a wrong password was caught, state mapping worked by typing labels — and
then team creation stopped dead at the query field. Nothing in the product says what a ServiceNow
query is, which fields exist, or where to get one, and ruling **R-2** makes a missing query a blocking
verdict. R-2 argued that a warning is dishonest when no warning channel renders it. It did not argue
that blocking without guidance is acceptable. This story is the missing third option.

---

## Wave: DISCUSS / [REF] Locked decisions

D-numbers are local to this feature. Epic 5513's own D1–D11 and 5611's D1–D7 are referenced by name.

| ID | Decision | Rationale |
|---|---|---|
| **D1** | Split 5610 into two slices: **in-product query guidance** first, **board picker** second. | The ADO body already calls the guidance half "cheap and independent" and it is the only half that reaches a non-admin (OC-4). It also ships without the SPIKE and without 5611, so the dogfood pain is answered even if the picker is disproven. Maintainer, 2026-07-31. |
| **D2** | The **docs prose** (ServiceNow's right-click breadcrumb → *Copy query* walkthrough) lands on **#5578**, not here. 5610 ships the in-product surface only. | There is no ServiceNow page under `docs/` at all today — `docs/concepts/worktrackingsystems/` has azuredevops, jira, linear, csv and nothing else. 5578 owns "ServiceNow docs". Two stories creating and then rewriting one new page is worse than one story writing it once. Maintainer, 2026-07-31. |
| **D3** | The guidance is carried by the **data-retrieval schema** as a new nullable field, rendered on the field that already exists — **not** hardcoded per connector inside the component. | `GeneralSettingsComponent.tsx:160-182` renders one `TextField` for every connector, labelled from `schema.displayLabel`. Branching on `systemType === "ServiceNow"` inside a shared component is the twin-drift anti-pattern pointing the other way. The schema is where connector knowledge already lives. |
| **D4** | The new schema field changes in **both twins** — `DataRetrievalSchemaDto.cs` and `DataRetrievalSchemaDefaults.ts` — and the #5613 exhaustiveness guard must still pass. | `docs/evolution/2026-07-30-bug-5613-schema-twin-drift.md`: this table is duplicated knowledge that already drifted once and shipped unsaveable ServiceNow teams. Same constraint 5611 D6 accepted. |
| **D5** | **`wizardHint` is not the vehicle.** It stays what it is (a wizard id) and a separate field carries the help text. | Measured, not assumed: `wizardHint` is declared in `IDataRetrievalSchema`, set for four connectors in both twins, asserted in `DataRetrievalSchemaDtoTest`, and **consumed by nothing in the frontend**. Overloading a dead field with a second meaning would make it harder to either use or delete. That it is dead is itself a finding — recorded, not fixed here. |
| **D6** | The board pre-fill rides **5611 slice 01** (class filter): the board's **table** becomes a `sys_class_name` value in **Work Item Types**, the board's **filter** becomes the **query**. | The ADO body says "pre-fill the team's table and query", and there is no team table to fill. Originally because ADR-116 put `Work Item Table` at *connection* scope and `Team` has no option bag (5611 D7); **now because the option is being removed altogether** — see the amendment below. Either way `BoardInformation` already carries `DataRetrievalValue` **and** `WorkItemTypes`, so the pre-fill needs zero contract change. Maintainer, 2026-07-31. |
| **D7** | ServiceNow implements the **existing** `IBoardInformationProvider` and joins the existing `WizardsController` switch. No new port, no new endpoint, no new dialog. | `GET /api/latest/wizards/{connId}/boards` and `/boards/{boardId}` already exist and are already generic; `BoardWizard.tsx` already serves Jira, ADO and Linear from one component through `DataRetrievalWizardRegistry.ts`. A ServiceNow picker is one provider implementation, one `switch` arm and one registry row. |
| **D8** | ServiceNow's `inputKind` stays **`freetext`**. Only `wizardHint` is set. | `GeneralSettingsComponent.tsx:126` computes `isDataRetrievalReadOnly = schema?.inputKind !== "freetext"`, so Linear's `wizard-select` makes the field read-only. The ADO body is explicit that manual entry stays the primary path and the pre-filled query must remain fully editable. Copying Linear's shape would silently contradict that. |
| **D9** | A board read that fails must **not** offer a pre-fill. | `BoardWizard.tsx:71-82` catches a failed `getBoardInformation` and substitutes an all-empty `IBoardInformation`, which is truthy, which enables **Confirm**, which overwrites whatever the user typed with blanks. Given OC-1's live risk that `vtb_board` is 403 for a least-privilege account, this is the epic's signature failure — quietly wrong beating visibly missing — wired up and waiting. Fixing it in the shared component fixes it for Jira, ADO and Linear too. |
| **D10** | A board whose membership cannot be expressed as a query is **excluded or refused by name**, never silently pre-filled with a partial query. | Freeform boards hold hand-placed `vtb_card` rows that no filter describes. Syncing "the filtered part of a freeform board" is a wrong number that looks right, which is the one outcome this epic exists to prevent. Which of exclude-from-list vs list-and-refuse is a DESIGN call, gated on OC-2. **Settled 2026-08-01 — see D14.** |
| **D14** | **Exclude at query time.** The board list is read as `active=true^tableISNOTEMPTY^filterISNOTEMPTY`; a board missing either is never listed. Maintainer, 2026-08-01, settling D10's open half. | The SPIKE measured that a freeform board stores empty `table` **and** empty `filter`, and that **no board-type column exists** — no `type` field on the record, `sys_class_name` empty on every row, one table with no subclasses. So "only data-driven boards" is not directly expressible; emptiness is the whole discriminator, and it is exactly the right one. **Both** fields must be non-empty, not just `table`: a board with a table and no filter is a real configuration ("all incidents") whose pre-fill is an empty query that `ValidateTeamSettings` then blocks with `query_matches_whole_table` — cheaper to exclude than to render and refuse. Guided vs data-driven is not separable on the record and does not need to be: Lighthouse copies the **filter**, which is live, not the card set, which drifts. |
| **D11** | **SPIKE first**, against PDI `dev191338`, **reusing 5611's probe accounts**. | The maintainer's call was "one combined probe run with 5611's OC-1/2/3". **Superseded within the hour: 5611's SPIKE landed first (`1c3cbf58c`) and settled all three of its open calls.** The intent survives the change — 5610's two board questions run on the same instance with the same scaffolding, against the accounts 5611 already created: `lh_probe_none` (no roles), `lh_probe_snc_read` (`sn_incident/change/request_read`, deliberately no `sn_problem_read`), `lh_probe_itil`, all sharing the admin password in `$ServiceNowLighthouseIntegrationTestToken`. So this is now a small standalone probe rather than a combined one, and it is cheaper than when the call was made. |
| **D12** | **No DESIGN wave for 5610 starts until #5611 is delivered.** Not merely slice 02 — the whole feature waits. | Maintainer, 2026-07-31. D6 makes the picker's entire pre-fill model a consumer of 5611's class filter, so designing against it while it is in flight designs against a moving target. 5611's SPIKE has since confirmed the model holds (`IN` over `^OR`, class names not labels), which removes the re-scope risk but not the gate: the field the picker fills does not exist until 5611 ships it. Slice 01's *content* does not depend on 5611, but it is held by the same gate for sequencing simplicity; it is small enough that waiting costs little. |
| **D13** | A board pick must not silently hand a team a **class that yields no time-in-state**. | 5611's SPIKE measured that stock `change_request` has **no state-tracking metric definition at all** — its two definitions sit on `approval` and `type` — so a change-request board can never produce state spans, whatever Lighthouse does. (The related `metric_definition`-is-empty-for-`table=task` finding is 5611's to carry: its class-scoped definition read is the repair, and it is no longer a *choice* a user makes now that every read is task-rooted.) A picker is the one moment the user is choosing a class, so it is the moment to say the choice costs time-in-state. Where it surfaces is a DESIGN call (OC-6); that it must surface is not. |

---

## Wave: DISCUSS / [REF] Amendment — the connection-scope table is going away (2026-07-31)

Written hours after the decisions above, once #5611's delivery landed on local `main` and the
maintainer confirmed the direction. Recorded as an amendment rather than a rewrite, so the reasoning
that produced D6 stays legible.

**What changed.** `ServiceNowWorkTrackingOptionNames.WorkItemTable` — the connection-scope option
ADR-116 introduced — **is being removed**, and every ServiceNow read becomes `task`-rooted. Work in
progress on `main` at the time of writing; assume it lands.

Note the two commits on `main` read in the opposite order tell a confusing story, and only the later
one is current: `e466caaf8` cancels 5611's per-team-override slice *because* the table option was
removed, while `a84859b26` records that the removal was implemented and then **reverted before
commit**, killed by two PDI measurements — a `task`-rooted read has no `resolved_at` (the Table API
projects an extended record onto the addressed table's columns, and `sysparm_fields` does not recover
it), and `ORDERBYsys_created_on` is not a unique sort key, which lost a row across a page boundary.
**This amendment assumes those two are being solved on `main` as part of the removal.** They are not
5610's to solve, but if the removal stalls on them, D6 reverts to its original wording and nothing
else here moves.

**What it changes for 5610 — less than it looks:**

| | Effect |
|---|---|
| **D6** | Unaffected in substance, **strengthened** in rationale. With no table field anywhere, Work Item Types is not merely the *available* home for a board's table — it is the only one. |
| **OC-5** | **Dissolved and re-narrowed.** No configured table means no board-vs-connection mismatch. What survives is the smaller case of a board rooted outside the `task` hierarchy. |
| **D13 / OC-6** | **Re-aimed.** "The picker steers you into task-rooting" is no longer meaningful when everything is task-rooted. The residual is per-class and outside Lighthouse's reach: stock `change_request` has no state-tracking definition, so a change-request board yields no time-in-state however the read is scoped. OC-6 also got *harder*: 5611 withdrew the connection-validation advice about history rather than rewording it, so there is currently no channel to put this in — the R-2 lesson, again. |
| **Pre-requisite 1** | **Mostly satisfied already.** 5611 shipped `isWorkItemTypesRequired: true` unconditionally for a ServiceNow team (ADR-123 decision 6, amended 2026-07-31), asserted in both twins. The pre-fill target exists and is visible today. |
| **D12** | **Unchanged** — the maintainer's gate is on 5611 being *delivered*, not on any one mechanism, and the table removal is precisely the part still in flight. |
| Slices, stories, ACs, KPIs, WS strategy | **Unchanged.** No AC was written against the connection table. |

**What does not change and is worth saying plainly**: this makes the board picker *more* valuable, not
less. Removing the table option removes the last place a user could say "my work lives in
`change_request`" outside of Work Item Types — so the picker, which derives exactly that from a board
the user already maintains, becomes the shortest path to a correctly-scoped team rather than a
convenience on top of one.

---

## Wave: SPIKE / [REF] Amendment — what the board probe overturned (2026-08-01)

Evidence in `spike/findings.md`; promotion decision (**DISCARD**, per WS strategy C) in
`spike/wave-decisions.md`. Recorded as an amendment for the same reason as the one above: the
reasoning that produced D9, D10 and OC-1 should stay legible next to what replaced it.

**The gate lifted first.** #5611 is Closed, the connection-scope table is gone, `resolved_at` is
deliberately not read, and the paging guard now keys `sys_id` rather than `number`. The three things
the 2026-07-31 amendment said to *assume* all landed, so **D12 is discharged** and D6 stands as
strengthened.

| | What the probe measured | Effect |
|---|---|---|
| **OC-1** | **Settled — and the first answer was wrong.** `vtb_board` reads 200/0-rows for `itil`, `snc_read` and no-roles, which reads as the epic's third role wall. It is not one: the read ACL carries **no role** and runs `VTBBoardSecurity().canAccess(current)`. Adding `lh_probe_itil` to `vtb_board_member` turned 0 boards into the board, its 38 cards and its 6 lanes; deleting the row turned it back. Boards are **shared, not roled**. | **Slice 02 survives.** But the list is scoped to the connection's service account, so "no boards" must be worded as "this account is not a member of any board" — R-2's lesson on a new surface — and *share the board with the Lighthouse account* becomes an onboarding step for #5578 that Lighthouse cannot perform for itself. |
| **OC-2** | **Settled — no.** A board's cards are a snapshot behind its filter (`incident` 38/38, but `change_request` **7 cards against 13 matches**). Freeform boards store empty `table` **and** empty `filter`; there is **no board-type column** — no `type` field, `sys_class_name` empty on every row — so emptiness *is* the discriminator. | **D10 is settled by D14** (exclude at query time, both fields non-empty) **and AC-B6 gets corrected.** Refusal is decidable from the board row alone — no card-set inspection. AC-B6's "synced items are the board's items" was unsatisfiable and has been restated against the filter. |
| **OC-3** | **Settled, and the trap is sharper than assumed.** `filter` is a verbatim encoded query in **column** form (`correlation_id=LIGHTHOUSE_DEMO`). `readable_filter` is the label form and matches the **whole table** — 105/105 and 118/118. | Pre-fill `filter`, never `readable_filter`. The poisonous string is the one ServiceNow's own UI displays, so it is the one a careless implementation would reach for. Worth a named test. |
| **OC-7** | **Settled — yes.** Every 0-row board read returned `X-Total-Count: 2`. | ACL-blindness generalises beyond `incident` (ADR-124). The picker must never count boards from the header. |
| **OC-5** | A `cmdb_ci` board is creatable and returns nothing from a task-rooted read (`task?sys_class_name=cmdb_ci` → 0). | Refusal is required. **New constraint**: verifying task-descendance needs `sys_db_object`, which 5611 measured **403 below `itil`** — so the check that would make the refusal precise is unavailable to exactly the accounts it protects. Two candidate ways out in `findings.md`; DESIGN picks one. |
| **D9** | Unchanged, and **promoted from tidy to load-bearing.** Every failure mode the probe found — not a member, freeform, wrong hierarchy — arrives at `BoardWizard.tsx:71-82` as a truthy all-empty `IBoardInformation` that enables Confirm. | The empty-fallback fix is now the difference between a refusal and a blanked query, for all four connectors. |
| **OC-4, OC-6** | Untouched by a probe. | Both still open, unchanged. |

**The claim this probe was called to disprove is disproven.**
`IServiceNowWorkTrackingConnector.cs:3-5` has asserted since `4b55362be` that ServiceNow *"has no
board concept"*. It has two, on a stock PDI, carrying a table and a verbatim encoded query. The
comment's second half (`sys_db_object` discovery is unavailable below `itil`, ADR-116) remains true and
does not support the first. Slice 02 must amend that xmldoc **and** the matching claim at
`DataRetrievalSchemaDefaults.ts:64`, or review will read them as authority against the slice.

---

## Wave: DISCUSS / [REF] Out of scope

- **The ServiceNow docs page** — D2, it is #5578's.
- **A per-team table override** — 5611 Story A, since **cancelled** (`e466caaf8`) because the
  connection-scope table is going away. D6 never needed it.
- **Field or table discovery** (offering a list of tables/columns to build a query from). SPIKE Q8
  measured `sys_db_object` 403 and `sys_dictionary` 200/EMPTY below `itil`: a discovery UI would work
  for the maintainer and show an empty list to every customer. ADR-116 already declined it.
- **A query builder.** The board picker borrows ServiceNow's own filter UI by reading its output;
  building a second one inside Lighthouse is a different product.
- **Widening `/api/latest/wizards/*` beyond SystemAdmin** — OC-4, pre-existing for all four connectors.
- **Portfolios.** Slice 03 cancelled; the ServiceNow portfolio schema is `inputKind: "none"`.
- **Write-back of any kind** — epic D8, still read-only.

---

## Wave: DISCUSS / [REF] Pre-requisites

1. **#5611 slice 01 (Work Item Types as record classes) must land before 5610 slice 02.** D6 pre-fills
   Work Item Types with a class name. **Now largely satisfied**: 5611 shipped that field as
   `isWorkItemTypesRequired: true` unconditionally for a ServiceNow team (ADR-123 decision 6, amended
   2026-07-31, asserted in both twins), so the pre-fill target already exists and is already visible.
   What remains is the table-removal work in flight on main. Slice 01 of *this* feature has no such
   dependency.
2. **The SPIKE (D11) must close OC-1 and OC-2.** Slice 02 is not buildable until it does. 5611's SPIKE
   (`1c3cbf58c`) already left the probe accounts and scaffolding in place, so this is a short run.
3. **Story 5577 landed and pushed** — done, `0e2e78340`. Code against
   `ServiceNowWorkTrackingConnector` is safe to touch again.
4. **The Bug #5613 schema-twin guard is in place** — shipped `cb5f0efb0`. D4 depends on it.
5. **A PDI board that a least-privilege account can be tested against.** The dogfood board observed
   was Table = `Incident`, Board Filter = `Correlation ID = LIGHTHOUSE_DEMO` — note that filter names
   the *label*; the column is `correlation_id`, and the label form silently matched all 103 incidents
   (epic slice-04 dogfood). Whether `vtb_board` stores the label or the column is OC-3.

---

## Wave: DISCUSS / [REF] Driving ports

| Surface | Change |
|---|---|
| `GET /api/latest/wizards/{connId}/boards` | Existing. `WizardsController.GetBoardInformationProviderForWorkTrackingSystem` gains a `WorkTrackingSystems.ServiceNow` arm; today it is `_ => throw new NotImplementedException`. |
| `GET /api/latest/wizards/{connId}/boards/{boardId}` | Existing. Returns `BoardInformation` with `DataRetrievalValue` = the board filter and `WorkItemTypes` = the board's table as a class name. |
| `GET /api/.../dataretrievalschema` (`DataRetrievalSchemaDto`) | The new help field (D3/D4), both twins. ServiceNow team row also gains `wizardHint`. |
| Team settings screen + Create Team wizard | `GeneralSettingsComponent.tsx` renders the help on the existing `TextField`; the wizard button comes from `DataRetrievalWizardRegistry.ts` with no component change (D7). |
| ServiceNow Table API (outbound) | New read of the Visual Task Board table(s). Exact table and columns are OC-3. |

---

## Wave: DISCUSS / [REF] User stories

### Story A — the query field says what to put in it

**As** a flow coach creating a Lighthouse team against ServiceNow,
**I want** the query field to tell me what it wants and show me an example,
**so that** I can fill it in instead of guessing, hitting the blocking verdict, and giving up.

`job_id: job-snow-team-creator-author-a-query`

#### Elevator Pitch
Before: the ServiceNow query field is an empty four-line box labelled "ServiceNow Query (Encoded Query)", and a wrong or missing query is refused with `query_matches_whole_table` — so the first real user's first experience of the connector is a blocked save with no instruction.
After: open **Create Team** → pick the ServiceNow connection → the query field shows the placeholder `active=true^assignment_group=Service Desk` and help text naming ServiceNow's own *right-click a filter breadcrumb → Copy query* path → paste → Save succeeds.
Decision enabled: whether this team's boundary is expressible as a ServiceNow filter at all — which the coach can now answer in their own instance in under a minute, instead of concluding Lighthouse is broken.

**AC-A1** — For a ServiceNow **team**, the query field renders a placeholder showing a real encoded
query and help text that names where to get one. The text is legible when the field is empty *and*
when it is filled.
**AC-A2** — The help text is supplied by the data-retrieval **schema**, not by a component branching on
connector type (D3). A connector with no help text renders exactly what it renders today — no layout
shift, no empty helper row.
**AC-A3** — The new schema field is present and consistent in **both** twins
(`DataRetrievalSchemaDto.cs`, `DataRetrievalSchemaDefaults.ts`), asserted on both stacks, and the
#5613 enum-exhaustiveness guard still passes (D4).
**AC-A4** — The help names the two silent-failure modes the SPIKE measured, in the coach's language:
an unknown **field name** is dropped and the query returns the whole table (which `ValidateTeamSettings`
then blocks with `query_matches_whole_table`), and a bad **value** silently returns nothing. This is
the one place a user can be warned before they hit either.
**AC-A5** — Both surfaces carry it: `ModifyTeamSettings` and `CreateTeamWizard`, which render through
the same `GeneralSettingsComponent`.
**AC-A6** — ServiceNow **portfolio** settings are unchanged. `inputKind` is `"none"` there and the
field is not rendered at all; no help appears for a surface that does not exist.

---

### Story B — pick a board you already maintain

**As** a configuration administrator whose ServiceNow instance already has Visual Task Boards,
**I want** to pick a board and have Lighthouse fill in the query and the kind of work from it,
**so that** I do not have to re-author in Lighthouse a filter my team already agreed on in ServiceNow.

`job_id: job-snow-team-creator-author-a-query`

#### Elevator Pitch
Before: a ServiceNow shop's team boundary already exists as a Visual Task Board — table plus board filter — and Lighthouse makes you retype it as an encoded query you have to go find first.
After: open **Create Team** → **Select ServiceNow Board** → pick *Service Desk — Open Incidents* → the dialog shows the filter and the record class it found → **Confirm** → the query field and Work Item Types are filled in, both still editable.
Decision enabled: whether the board the team already runs its stand-up from is the same boundary Lighthouse will forecast — visible side by side before saving, rather than discovered later from a throughput that looks off.

**AC-B1** — A ServiceNow connection offers a board picker in team settings and in the create-team
wizard, sourced from the existing `DataRetrievalWizardRegistry` and served by the existing
`/wizards/{connId}/boards` endpoints (D7). Portfolio context does not offer it.
**AC-B2** — Selecting a board pre-fills the **query** from the board's filter and **Work Item Types**
from the board's table, expressed as a `sys_class_name` value (D6). Both remain editable after
Confirm; `inputKind` stays `freetext` and the field never becomes read-only (D8).
**AC-B3** — A board read that fails — 403, unreachable, or a shape the parser does not recognise —
shows the reason and **cannot be confirmed**. It never substitutes an empty pre-fill over a query the
user already typed (D9). A 403 specifically says the account cannot read the board table, naming it,
rather than "Failed to load boards. Please try again."
**AC-B4** — A board whose membership is not expressible as a query is **absent from the list**: the
read is scoped `active=true^tableISNOTEMPTY^filterISNOTEMPTY`, so a freeform board (both empty) and a
table-without-filter board (which would pre-fill an empty query) never reach the user (D10, D14). A
board that *is* listed but cannot be turned into a pre-fill — a table outside the `task` hierarchy — is
refused by name with the reason stated.
**AC-B5** — The picker does not change the manual path. A team created by typing a query, with no
board involved, behaves exactly as it does today — including `ValidateTeamSettings`' blocking verdicts.
**AC-B6** — The pre-filled configuration is verified end-to-end against a real instance, not only
fixtures: pick a board on the PDI, save the team, and confirm the synced set equals **the board's
filter run against the board's table** — *not* the board's card set, which drifts behind its own
filter (SPIKE 2026-08-01: 7 cards against 13 matches on `change_request`). The epic's standing rule —
164 tests did not find what one manual run did.

---

## Wave: DISCUSS / [REF] Definition of Done

1. Both slices' ACs green, backend and frontend.
2. The new schema help field asserted on **both** stacks; the #5613 exhaustiveness guard still passes.
3. Mutation testing ≥ 80 % on the changed backend and frontend surface (project standing gate).
4. No new SonarCloud issues; `dotnet build` and `pnpm build` warning-free; Biome clean.
5. Verified against the PDI, not only against fixtures — a board picked, a team saved, its items
   synced (AC-B6).
6. Docs: the in-product help text is reviewed against what #5578's page will say, so the two do not
   contradict each other. Screenshot only if the settings screen visibly changes — it does, so the
   `@screenshot` E2E for team settings is re-run and the PNG deleted first (the <0.5 % diff trap).
7. A dogfood moment on the same day each slice lands — by the person who hit the original wall.
8. Epic 5513's `feature-delta.md` gets a back-reference to this file once #5578 is not mid-append.
9. ADO #5610 transitioned; Release Notes tag decided with the maintainer.

---

## Wave: DISCUSS / [REF] Open calls

**Status after the SPIKE (2026-08-01)**: OC-1, OC-2, OC-3 and OC-7 are **settled** — see the SPIKE
amendment above and `spike/findings.md`. The rows below are left as written so the questions stay
legible next to their answers. **OC-4, OC-5 and OC-6 remain open**; OC-5 acquired a new constraint.

| ID | Question | Why it is open | Settle by |
|---|---|---|---|
| **OC-1** | Is the Visual Task Board table readable by a **least-privilege** account (`sn_incident_read` and siblings), or does it need `itil`/admin? | This epic has been bitten twice. `sys_choice` was measured admin-only and killed `ServiceNowChoiceLabelResolver` outright (R-4); `metric_instance` is 403 for every read-only role and cost slice 04 an `itil` escalation. A picker that works for the maintainer and 403s for every customer is the same failure a third time. Measure it the way `spike/findings.md` measured the rest: the same read as no-roles / `snc_read_only` / `sn_*_read` / `itil` / `admin`, and **treat 200/EMPTY as a denial**, not an empty instance. | SPIKE, before DESIGN. |
| **OC-2** | Can every board's membership be expressed as a query? | Freeform boards hold hand-placed `vtb_card` rows that no filter describes. Probe: create or find a freeform board and a filtered/guided board, and compare each board's card set against running its stored filter. If they diverge, D10 applies and board support covers filtered boards only — said out loud, not discovered from a wrong throughput. | SPIKE, before DESIGN. |
| **OC-3** | Which columns carry the table and the filter, and is the stored filter a verbatim encoded query? | The dogfood board showed Board Filter = `Correlation ID = LIGHTHOUSE_DEMO` — a **label**, and the slice-04 dogfood proved `sysparm_query` matches the stored value, not the label (`correlation_id` is the column, and the label form silently matched all 103 incidents). If boards store the label form, pre-filling it verbatim ships the exact query that slice 01's widening guard exists to catch. | SPIKE, before DESIGN. |
| **OC-4** | `WizardsController` is `[RbacGuard(RbacGuardRequirement.SystemAdmin)]`, while creating a team is `CanCreateTeam`. So a user who may create a team may not be able to use the picker. | Pre-existing and identical for Jira, ADO and Linear, so 5610 is not the place to change it — but it is the reason Story A is ordered first and is not optional: it is the only half that reaches the persona who actually hits the blank field. Decide whether to widen the guard, or to state the constraint in #5578's docs. | Maintainer, before slice 02 DESIGN. |
| **OC-5** | ~~Does the board's table always sit under the connection's configured table?~~ **Narrowed, 2026-07-31**: what happens to a board rooted **outside** the `task` hierarchy? | The original question dissolves with the connection-scope table (see amendment): every read is task-rooted, so any task-descendant class a board names is reachable and no mismatch is possible. What survives is the outside case — a board on `cmdb_ci`, `sys_user`, or an Agile 2.0 `rm_story`. `sys_class_name` cannot express it from a `task` root, so the picker must refuse by name rather than pre-fill a class that returns nothing. Still needs a stated behaviour, on a smaller surface. | DESIGN. |
| **OC-6** | Where does a user learn that the board they picked yields **no time-in-state**? | D13. Slice 04 built a connection-validation notice for history being unavailable (ADR-118 D-04-3), but 5611 **withdrew** the advice it used to give rather than rewording it (`e466caaf8`), and parked the replacement against #5612 as probably out of MVP scope. So today there is no channel at all, which makes this the same shape as ruling R-2: do not choose to warn without checking the reader exists. Either the picker says it at pick time, or nobody does. | DESIGN. |
| **OC-7** | Does the board's stored filter suffer the same ACL blindness as `X-Total-Count`? | 5611's SPIKE found `X-Total-Count` is **ACL-blind** — a no-roles account gets header=103 / body=0 on `incident` — which is both a pre-existing defect in `ValidateTeamSettings`' widening detector and, usefully, a denial detector. If the board list or a board's card count comes back through a similarly blind counter, the picker could show a board it cannot actually read. Worth one read during the SPIKE while the probe accounts are up. | SPIKE, with OC-1. |

---

## Wave: DISCUSS / [REF] Recorded, not fixed here

- **`wizardHint` is dead weight.** Declared in `IDataRetrievalSchema` and `DataRetrievalSchemaDto`,
  populated for four connectors across both twins, asserted in `DataRetrievalSchemaDtoTest` — and read
  by no frontend code. The wizard button is driven entirely by `DataRetrievalWizardRegistry.ts`
  matching on `WorkTrackingSystemType`. Slice 02 sets it for ServiceNow for consistency with the other
  four, but nothing depends on it. Candidate for deletion; belongs in a cleanup, not here (D5).
- **`BoardWizard`'s empty-fallback affects all four connectors**, not just ServiceNow. D9 fixes it in
  the shared component, so the fix lands for Jira, ADO and Linear at the same time — noted so the
  blast radius is not a surprise at review.

---

## Wave: DISCUSS / [REF] WS strategy

**C — no walking skeleton.** Brownfield, on a connector that has shipped four slices. Both driving
ports (`/wizards/*`, the settings screen) already exist end to end for three other connectors; there
is no unproven path to skeleton. The unproven parts are ServiceNow-instance facts, and those are
answered by the SPIKE (D11), which is the right instrument for them.

---

## Wave: DISCUSS / [REF] Scope Assessment: PASS

2 stories, 2 slices, 1 bounded context (work-tracking connectors), ~1 day each. No oversized signal
fires. Slice 01 ships with no dependency on the SPIKE or on 5611, so a failed SPIKE costs one slice.

---

## Wave: DISCUSS / [REF] Slices and prioritization

Briefs in `slices/`. Order per D1: guidance first.

| # | Slice | Ships | Learning hypothesis |
|---|---|---|---|
| 01 | Query-authoring guidance in the product | A coach facing the blank query field is told what to type and where to get it | Disproves "the wall was ignorance, not absence" if a guided coach still cannot author a query that passes `ValidateTeamSettings` — which would mean the encoded-query concept itself is the barrier and the picker is not optional but mandatory |
| 02 | Pick a Visual Task Board to pre-fill query + class | An admin turns an existing board into a configured team | Disproves D6/D7 if boards are unreadable below `itil` (OC-1) or if board membership is not a query (OC-2). Either outcome cancels the slice loudly and leaves slice 01 as the whole answer |

**Prioritization rationale.** Slice 01 first on three counts, not one: it is the only half that reaches
`CanCreateTeam` users (OC-4); its content depends on neither the SPIKE nor 5611, so it is the cheapest
thing to start with the moment D12's gate lifts; and if slice 02 is later cancelled by OC-1 or OC-2,
the dogfood finding is still answered. Slice 02 second because every open call rides on it and it is
hard-blocked twice over.

**Both slices sit behind D12**: nothing here enters DESIGN until #5611 is delivered. The SPIKE (D11) is
the only work that can run before that gate, and it should — it is a probe, not a design, and its
answers may cancel slice 02 outright.

---

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Target | How measured |
|---|---|---|
| Team creation reaches a saved team on the first attempt | The dogfood repeat of the original 2026-07-29 walkthrough completes without leaving the product to look anything up | Same maintainer, same instance, timed; recorded in the slice-01 dogfood note |
| `query_matches_whole_table` refusals seen during dogfood | 0 on the guided path | Slice-01 dogfood; a non-zero count means AC-A4's wording failed, not that the guard is wrong |
| Board pick → saved team, clicks | ≤ 4 from the create-team wizard (open picker, choose, confirm, save) | Counted during the slice-02 dogfood |
| Board pre-fill accuracy | The synced item set equals the board's card set on a filtered board | AC-B6, verified against the PDI |
| Least-privilege reach of the picker | Stated, not assumed — a measured verdict for `sn_*_read` in the SPIKE findings | OC-1; a 403 is a legitimate result that cancels slice 02, and the number that matters is that it was measured before build, not after |

---

## Wave: DISCUSS / [REF] DoR validation

| # | Item | Evidence |
|---|---|---|
| 1 | Business value stated | Job `job-snow-team-creator-author-a-query`; the epic's first real user was blocked within minutes. |
| 2 | Persona identified | `flow-coach` primary, `config-admin` secondary — split deliberately, per OC-4. |
| 3 | Job traceability | Both stories carry `job_id`. No `infrastructure-only` story in this feature. |
| 4 | Acceptance criteria testable | AC-A1..A6, AC-B1..B6; each names a surface and an observable outcome. |
| 5 | Elevator pitch per story | Both stories; both "After" lines name a real UI action and a visible result. |
| 6 | Effort honestly estimated | ≤1 day each — slice 02 only because the picker port already exists (D7). Contingent on the SPIKE having been paid for separately. |
| 7 | Dependencies named | #5611's delivery, the SPIKE, #5613's guard, #5578 for docs. |
| 8 | Open questions recorded rather than assumed | OC-1..OC-7, each with a named settle-by. |
| 9 | Out-of-scope explicit | Listed above, each with a reason. |

Requirements completeness: **0.96** — the residual is OC-3 and OC-5, both of which change slice 02's
DESIGN shape and neither of which changes what the slices are for.
