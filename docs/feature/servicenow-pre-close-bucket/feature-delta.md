# ServiceNow: the pre-close bucket — feature delta

**ADO**: User Story [#5612](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5612), parent Epic
[#5513](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5513) (ServiceNow Integration).
**Waves recorded here**: DISCUSS (2026-08-01).

**Why this file and not the epic's.** Same reason 5610 and 5611 got their own workspaces: epic 5513's
`feature-delta.md` is the record for slices 01–05 and #5578 is still appending to it. Siblings:
`docs/feature/servicenow-multi-table-work-item-types/` (#5611, shipped),
`docs/feature/servicenow-board-picker-and-query-guidance/` (#5610, DESIGN complete on `main`).

**What this story is.** A bucket of small findings from dogfooding the connector, intended to be picked
up once the structural slices settled. This DISCUSS wave's main product is **not** a plan — it is a set
of **verdicts**. Seven items entered; **two ship here**, three moved to owners that already exist, one
is a decided no-op, one is recorded NOT-NOW with a named trigger.

---

## Wave: DISCUSS / [REF] The finding that outranks the bucket

**#5627 as scoped cannot fire on the case that created it**, and 5612's item 6 is the reason.

[#5627](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5627) ("tell a team when its
kinds of work yield no time-in-state", ADR-127, split out of #5610 at DESIGN on 2026-08-01) states in
its own body: *"5611 already computes the answer in `ServiceNowHistoryVerdict.From`."* It does not.

| Evidence | Where |
|---|---|
| The definition read filters on **table ∈ classes** and **`type = field_value_duration`**, nothing else | `ServiceNowHistoryQuery.cs:67-70`, `DefinitionQueryFor` |
| The code says why, in its own words: *"A `field_value_duration` definition does not have to be one on the state field … `DefinitionQueryFor` cannot exclude them, because the state field is named differently on each record class"* | `ServiceNowWorkTrackingConnector.cs:306-309` |
| `From` returns `Available` when every named class appears in `kindsOfWorkADefinitionCameBackFor` — regardless of which field the definition sits on | `ServiceNowHistoryVerdict.cs` |
| Stock `change_request` carries definitions on `approval` and `type` | 5611 SPIKE, 2026-07-31; re-confirmed on dev191338, 2026-08-01 |

So for the motivating team (incident + problem + change_request on a stock instance): `change_request`
lands in `kindsOfWorkADefinitionCameBackFor` → `everyKindIsMeasured` is true → `Available` →
`SupportsTransitionHistory` returns true → **no advisory, for the exact class #5627 exists to warn
about**. The whole-team span-read repair does not catch it either: it guards on
`spans.ByRecord.Count < 1` **across the team** (`ServiceNowWorkTrackingConnector.cs:260-266`), and the
incident spans survive.

Same family as Bug #5621 F1/F6, one level finer. F6 moved the verdict from an aggregate count to
per-class coverage; this is per-class coverage still being satisfied by the wrong field. A definition
row cannot say whether it measures state, so the evidence must be **F1's evidence** — which classes'
spans survived `ServiceNowStateSpanMapper.TheTeamRecognisesAsState` — evaluated **per class** instead
of per team. That evidence exists at `ReadSpans` time and is currently discarded.

**Verdict: folded into #5627 as a scope addition** (maintainer, 2026-08-01). One story delivers both
the correct verdict and the advisory that reports it, because they are one outcome. ADR-127 needs an
amendment recording that its "already computes the answer" premise was false.

---

## Wave: DISCUSS / [REF] Persona and jobs

**Primary persona**: `flow-coach` (`docs/product/personas/flow-coach.yaml`) — reads the charts, opens
the dialog, configures the team.
**Secondary**: `config-admin` — owns the connection; `lighthouse-maintainer` — owns the viability verdict.

**JTBD one-liner** — new SSOT job `job-snow-coach-recognise-and-reach-the-record`:

> When Lighthouse shows me a ServiceNow item that looks wrong, I want to recognise what kind of work
> it is and open the actual record, so I can find out why instead of copying an ID into a search box.

Sits behind `job-snow-flow-coach-see-flow-metrics` (epic 5513) rather than refining it. That job is
delivered; this one is what makes its output *actionable* rather than merely correct.

---

## Wave: DISCUSS / [REF] Locked decisions

D-numbers are local to this feature. Epic 5513's D1–D11, 5611's D1–D7 and 5610's D1–D13 are
referenced by name.

| ID | Decision | Rationale |
|---|---|---|
| **D1** | 5612 ships **items 1, 7a and 7b**. Every other item is a recorded verdict with a named owner, not a slice. *(Amended twice: 7b was added when the maintainer asked for label input, and DESIGN then merged it into the same slice.)* | Maintainer, 2026-08-01. The bucket's purpose was to hold findings until the picture settled. It has settled, and most items now belong to stories that already exist. A bucket that ships one thing and hands the rest to their real owners is finished; one that keeps everything is a backlog. |
| **D2** | Item 1 (deep link) is **cheap**, not "not quite free" as the ADO body assumes. | Measured, not estimated. `WorkItemBase.Url` exists (`Models/WorkItemBase.cs:34`), flows to `WorkItemDto.cs:70`, and `WorkItemsDialog.tsx:142-144` **already renders it as a `<Link target="_blank">` when non-null**. `sys_id` is already read (`ServiceNowWorkItemMapper.RecordIdField`) and `sys_class_name` is already mapped as `Type`. ServiceNow is the only connector of five that leaves `Url` unset. The work is one string built in `MapRecord` — no contract change, no new UI, no new request. |
| **D3** | The deep-link URL is built from **the record's own class**, never from a configured table. | `{instanceUrl}/{sys_class_name}.do?sys_id={sys_id}`. The `.do` path is class-specific, and since 5611 removed the connection-scope table there is no configured table to build from. `KindOfWork` already resolves the class per record, so a team reading incident + change_request gets correct links for both. |
| **D4** | `WorkItemBase.Type` **stays the class name**. The human label is carried as a **separate nullable field**. | Not cosmetic caution — `Type` is matched post-sync in two places. `TeamMetricsService.cs:243-252` filters synced items with `includedWorkItems.Contains(item.Type.ToLowerInvariant())` against the team's work-item-type list, so switching `Type` to `"Change Request"` makes the Created Items run chart return **zero** for every ServiceNow team. `WorkItemFieldProvider.cs:56` exposes the same string as the rule engine's type field. |
| **D5** | The label is **read from the instance**, never derived by transforming the class name. | `sc_task`'s ServiceNow label is *Catalog Task*, not "Sc Task". Any snake_case→Title Case transform is wrong for exactly the ITSM classes D4 (epic) optimises for. The correct label is already in the payload — 5611 SPIKE `findings.md:142` measured `sys_class_name: {"display_value": "Change Request", "value": "change_request"}`, present *"in every record of the connector-shaped read without adding `sysparm_fields`"* (`:146-147`). |
| **D6** | The new label field is **nullable**, and the DTO falls back to `Type` when it is absent. | Bounds the blast radius of a shared-contract change to one connector. ADO, Jira, Linear and Csv already carry human-readable types; none needs touching. Per the project's shared-contract rule, the test factory / builder is extended before the contract is. |
| **D7** | Item 7b (**type a label in team config**) is a **separate slice**, small and unblocked. | Maintainer asked for it on 2026-08-01: *"I also want to be able to set 'Change Request' as work item type in a team config, not having to write `change_request`."* Separate from slice 01 because it is a different outcome (config input vs. rendered output), not because it is risky — see D8. |
| **D8** | Label→class resolution is a **static map in source**. No runtime lookup, no SPIKE. | Maintainer, 2026-08-01, and it corrects this wave's own first reading. `sys_db_object` is measured unreadable for the accounts that matter (epic SPIKE `findings.md:103,123`; 5611's contradicting `200` came from a probe account that had *"acquired `cmdb_query_builder_read` and friends since"*, and 5611's verdict is verbatim **"Do not build on `sys_db_object`"**). That is a reason to have **no runtime lookup at all**, not a reason to go looking for a different one. **The precedent is already in this connector**: 5611 carries hierarchy knowledge as *"a static set in source — ADR-116 decision 4 / S3: the runtime alternative (`sys_db_object`) is `403` for the accounts that matter"*. Same table, same rights problem, same answer. A case-insensitive dictionary and one `Select` in `ServiceNowReadScope.For` — which today only trims (`ServiceNowReadScope.cs:45-52`) — is the whole mechanism. |
| **D8a** | The static map is an **accepted alias, never the only accepted form**, and **never a display source**. | Two consequences that keep it honest. (1) ServiceNow labels are per-instance renameable and localised, so a static map cannot be exhaustive — but class names keep working unchanged, so a customised or non-English instance loses nothing it has today and the map is pure upside. (2) Display stays on `sys_class_name.display_value` per record (D5), which is instance-correct by construction. Using the static map for display would show "Change Request" on an instance that renamed it. |
| **D8b** | Where an entry matches both a class name and a map key, **the class name wins**. | A one-line rule stated now so it is not discovered later. The stock ITSM labels and class names do not collide, so this is a guard, not a live case. |
| **D9** | Item 2 (**declared per-instance capability set**) is **NOT-NOW**, with a named trigger. | Maintainer, 2026-08-01. #5627 proves the motivating case is answerable through the advisory channel that already ships (`ConnectionValidationResult.Advisory`, ADR-118 D5; `ValidationAdvisory.tsx`), without generalising anything. Evidence for a *declared capability set* is still one connector: Linear's `DowngradeHistorySupport()` flips **the same bool**, so it is the same mechanism, not a second demand. Trigger to revisit: a **second** connector wanting it, **or** a demand for a **widget-level** (not settings-level) explanation. See "Recorded, not fixed here" for the caching defect this leaves standing. |
| **D10** | Item 3 (**label-is-not-column, "did you mean `correlation_id`?"**) is **answered by #5610**; the dynamic resolver is **measured infeasible**. | Checked against `main` on 2026-08-01 at the maintainer's request. The static half already ships: 5610 slice 01's `AC-A4` requires the help text to name *"an unknown **field name** is dropped and the query returns the whole table"*, citing the exact case (`Correlation ID` vs `correlation_id`, 103 rows vs 36). The common case disappears: 5610's SPIKE verdict is **WORKS**, and OC-3 settled that `filter` stores the **column** form and is safe to copy verbatim while `readable_filter` matches 105/105 and 118/118. The dynamic half needs `sys_dictionary`, measured `200/EMPTY` at every rung below the top (epic SPIKE `findings.md:104`) — it would work for the maintainer and return nothing for every customer. |
| **D11** | Item 4 (**ServiceNow drops unmapped states where the others keep them**) — **keep both. No change, no warning.** | Already decided in the ADO body, 2026-07-31; recorded here so nobody "fixes" it later without knowing why. An unmapped state is treated as not existing, which is deliberate — dummy states, states a team does not use, and `Canceled`, whose items should simply vanish. The two behaviours differ for a reason ServiceNow alone has: `metric_instance` mixes non-state measurements into the same table, so without `TheTeamRecognisesAsState` a stock incident yields transitions like `true → false`. Unifying would mean either changing Jira/ADO/Linear for every existing customer to serve a misconfiguration case, or re-admitting junk spans on ServiceNow. **Only revisit if a customer reports confusion about state times not summing to cycle time.** |
| **D12** | Item 5 (**time-in-state has no empty state**) is **owned by #5627**, not by 5612. | ADR-127 already designs option (a) in full: `ValidateTeamSettings` reports availability as an advisory on a **success**, `ITeamService.validateTeamSettings` returns the result instead of a bool, and `ModifyTeamSettings` / `CreateTeamWizard` render the existing `ValidationAdvisory`. Options (b) per-item empty state and (c) per-class strip stay unbuilt — both need availability persisted per team **and** per record class, which is D9's question and is NOT-NOW. |

---

## Wave: DISCUSS / [REF] Verdict per item

| # | Item | Verdict | Owner |
|---|---|---|---|
| 1 | No link back to the ServiceNow record | **SHIPS** — slice 01 | 5612 |
| 2 | Per-instance capability as a connector property | **NOT-NOW**, trigger named (D9) | recorded only |
| 3 | A field's label is not its column name | **ANSWERED** by #5610; dynamic resolver infeasible (D10) | #5610 / #5578 docs |
| 4 | Unmapped states handled differently than other connectors | **NO-OP by decision** (D11) | recorded only |
| 5 | Time-in-state has no empty state | **MOVED** (D12) | #5627 |
| 6 | Verdict counts any duration definition as state coverage | **MOVED**, and it blocks its new owner | #5627 |
| 7a | Work item types display as raw class names | **SHIPS** — slice 01 (D4, D5, D6) | 5612 |
| 7b | Type a label in team config, not a class name | **SHIPS** — slice 02, static map, no SPIKE (D7, D8) | 5612 |

---

## Wave: DISCUSS / [REF] Out of scope

- **Anything #5627 owns** — the availability advisory and the per-class verdict fix (D12, and the
  headline finding above).
- **A declared capability set across connectors** — D9, NOT-NOW.
- **Persisting per-class history availability** — the real cost of item 5 options (b) and (c). Blocked
  behind D9 by design, so ServiceNow does not invent the parallel mechanism item 2 exists to prevent.
- **Any data migration, backfill or re-sync for existing ServiceNow work items** — OC-6. Nothing
  ServiceNow has shipped, so every such team is disposable by explicit maintainer decision.
- **Restoring a connection-scope advisory about history** — ADR-123 D10 withdrew it for a measured
  reason that still holds (`metric_definition` has 0 rows for `table=task`).
- **Changing Jira / ADO / Linear unmapped-state handling** — D11.
- **Table or field discovery UIs, and any runtime label lookup** — `sys_db_object` and `sys_dictionary`
  are both unavailable to the accounts that matter (D8, D10). ADR-116 already declined this, and D8
  takes the same exit 5611 took: a static set in source.
- **An exhaustive or localised label map.** D8a — it is an alias for the stock ITSM set, not a
  translation table. Class names remain the universal form.
- **Write-back of any kind** — epic D8, still read-only.

---

## Wave: DISCUSS / [REF] Pre-requisites

1. **#5611 shipped** — done, `main` @ `fa3350e2d`. `sys_class_name` is mapped as `Type` (ADR-123 D8),
   which is what D3's URL and D5's label both read.
2. **#5577 shipped and pushed** — done, `0e2e78340`. `sys_id` is carried through the mapper because
   the `metric_instance` batch is keyed on it, so item 1 pays nothing for it.
3. ~~**Slice 02 only**: a SPIKE settling OC-1.~~ **Dropped 2026-08-01** — D8's static map has no
   instance dependency, so slice 02 is unblocked too.
4. **Coordination with #5610 before slice 02 lands** — its board picker pre-fills Work Item Types with
   a **class name**. If the field starts accepting labels, the picker's pre-fill needs a stated
   behaviour. #5610 is at DISTILL on `main`; this is a conversation, not a code dependency.

---

## Wave: DISCUSS / [REF] Driving ports

| Surface | Change |
|---|---|
| `GET /api/latest/teams/{id}/metrics/*` → `WorkItemDto` | Existing. `Url` becomes non-null for ServiceNow (no schema change — the field already ships). New nullable type-label field, falling back to `Type` (D6). |
| Work Items dialog (`WorkItemsDialog.tsx`) | **No change for item 1** — the `<Link>` at `:142-144` already fires on a non-null `url`. Type column at `:153` reads the label when present. |
| ServiceNow Table API (outbound) | **No new request.** Both `sys_id` and `sys_class_name`'s two forms already arrive on every row. |
| Team settings — Work Item Types field | **Slice 02 only.** Accepts a label as well as a class name (D7). |

---

## Wave: DISCUSS / [REF] User stories

### Story A — reach the record, and know what kind of work it is

**As** a flow coach reading Lighthouse charts over ServiceNow work,
**I want** the item's ID to open the record in ServiceNow and its type to read the way ServiceNow
names it,
**so that** I can act on a suspicious item instead of transcribing an ID into a ServiceNow search box.

`job_id: job-snow-coach-recognise-and-reach-the-record`

#### Elevator Pitch
Before: the Work Items in Progress dialog lists `INC0010003` as inert text and its type as `change_request` — every other connector's rows are clickable, and every other connector's types read like English.
After: open a team → **Work Items in Progress** → click `INC0010003` → the incident opens in ServiceNow in a new tab; the Type column reads **Change Request**, not `change_request`.
Decision enabled: whether the item that has been aging for 40 days is genuinely stuck or just mis-stated — answerable in one click in the system of record, instead of abandoned because the round trip costs more than the question is worth.

**AC-A1** — A synced ServiceNow work item carries a `Url` of the form
`{instanceUrl}/{sys_class_name}.do?sys_id={sys_id}`, built from **the record's own class** (D3). A team
reading two classes gets correct links for both.
**AC-A2** — The dialog renders it through the **existing** link path (`WorkItemsDialog.tsx:142-144`).
No new component, no branch on connector type.
**AC-A3** — A record whose `sys_class_name` or `sys_id` is missing gets **no** `Url` rather than a
malformed one. A broken link is worse than an absent one — the epic's standing rule.
**AC-A4** — The Type column shows ServiceNow's own label, read from `sys_class_name.display_value`
(D5). `sc_task` reads **Catalog Task**, which no string transform produces.
**AC-A5** — `WorkItemBase.Type` is **unchanged** and still carries the class name (D4). Proved, not
asserted: a test covers `GetCreatedItemsForTeam` for a ServiceNow team whose work-item types are class
names, and it still returns a non-zero run chart.
**AC-A6** — The label field is nullable and the DTO falls back to `Type` (D6). ADO, Jira, Linear and
Csv rows are byte-identical to today.
**AC-A7** — Verified against the PDI, not only fixtures: sync a team on `dev191338`, open the dialog,
click a link, land on the record. The epic's standing rule — 164 tests did not find what one manual
run did.

---

### Story B — name the work the way ServiceNow names it

**As** a flow coach configuring a ServiceNow team,
**I want** to type **Change Request** in Work Item Types rather than `change_request`,
**so that** I am not required to know a column value to describe work my instance already has a name for.

`job_id: job-snow-coach-recognise-and-reach-the-record`

#### Elevator Pitch
Before: Work Item Types is a required field on every ServiceNow team, and the only accepted value is a snake_case class name — a wrong one narrows the query to **zero rows, silently** (5611 SPIKE), which is the failure this epic exists to prevent.
After: open **Team Settings** → type `Change Request` in Work Item Types → **Save** → validation accepts it and the team syncs change requests.
Decision enabled: whether the coach's own vocabulary describes their work, without a round trip to a ServiceNow administrator to learn what the column value is called.

**AC-B1** — Work Item Types accepts a ServiceNow **label** and resolves it to the class name used to
build `sys_class_nameIN…`.
**AC-B2** — Class names keep working unchanged. A team configured today, or pre-filled by #5610's
board picker, behaves exactly as it does now (D7, pre-requisite 4).
**AC-B3** — Resolution makes **no request**. It is a static case-insensitive map in source (D8), so it
behaves identically for a no-roles account and for `admin` — which is the entire point of choosing a
map over a lookup.
**AC-B4** — An entry that is neither a known class name nor a map key is refused **by name** at
validation, not accepted into a query that silently returns nothing. A bogus `sys_class_name` narrows
to zero and never widens (5611 SPIKE `findings.md:235`), so this is the one place it can be caught.
**AC-B5** — An entry matching both a class name and a map key resolves to the **class name** (D8b).
**AC-B6** — The map is **input-only**. The Type column still renders `sys_class_name.display_value`
(D5, D8a), so an instance that renamed a class displays its own name and is unaffected by the map.

---

## Wave: DISCUSS / [REF] Definition of Done

1. Both slices' ACs green, backend and frontend.
2. `WorkItemBase.Type` provably unchanged in meaning — AC-A5's `GetCreatedItemsForTeam` test exists.
3. Test factory / builder extended **before** the shared contract, per the project rule.
4. Mutation testing ≥ 80 % on the changed backend and frontend surface.
5. No new SonarCloud issues; `dotnet build` and `pnpm build` warning-free; Biome clean.
6. Verified against the PDI (AC-A7), and slice 02 dogfooded by creating a team typing `Incident` and
   `Change Request` rather than the class names. No privilege dimension to test — D8's map makes the
   behaviour identical at every rung, which is why it was chosen.
7. **Docs — #5578's ServiceNow page.** Three things, and the third is the one that will be missed:
   (a) the deep link exists and what it addresses; (b) **either form is accepted** in Work Item Types;
   (c) **5611's standing guidance now contradicts the product.** Its SPIKE recommendation reads
   *"Docs: class **names** not labels"* and its test file still calls typing `Change Request` the
   canonical user error — both were correct for the design they were written against and are now
   wrong. Update that guidance rather than adding a second, contradicting sentence elsewhere.
   Worth stating alongside (b): a team configured with class names keeps showing `change_request`,
   because nothing rewrites it (ADR-128 amendment) — so the docs should tell a coach to type the label
   if they want to read the label. Screenshot only if the dialog visibly changes — it does, so the
   `@screenshot` E2E is re-run and the PNG **deleted first** (the <0.5 % diff trap).
8. A dogfood moment on the same day each slice lands.
9. **ADR-127 amended** to record that its "5611 already computes the answer" premise was false, and
   #5627 updated with the folded scope.
10. ADO #5612 transitioned; Release Notes tag decided with the maintainer.

---

## Wave: DISCUSS / [REF] Open calls

| ID | Question | Why it is open | Settle by |
|---|---|---|---|
| **OC-1** | ~~Where does a label→class mapping come from for an account that is not the maintainer's?~~ **DISSOLVED, 2026-08-01.** Which entries does the static map carry? | The original question assumed a runtime lookup and proposed a SPIKE for it. D8 removes the assumption: a static map behaves identically at every privilege level, so there is nothing to probe. What survives is a content question, not a feasibility one — which ITSM classes get an alias. Confirmed present on the PDI across the SPIKEs: `incident`, `problem`, `change_request`, `sc_task`, `task`. The rest of the stock task hierarchy (`sc_req_item`, `change_task`, `incident_task`, `problem_task`) is a DESIGN call, and being wrong about one costs nothing — an absent alias just means the class name still works. | DESIGN. No SPIKE. |
| **OC-2** | Does `.do` reach a record of **every** ITSM class the connector supports? | D3 assumes `{class}.do` is universal. Measured for `incident` in the dogfood; `sc_task`, `problem` and `change_request` are assumed. Cheap to check while the PDI is up. A wrong assumption produces a link that 404s, which AC-A3 exists to prevent but cannot detect. | Slice 01 DESIGN, one manual check. |
| **OC-3** | Does the instance URL need normalising before concatenation? | `ServiceNowWorkTrackingOptionNames.InstanceUrl` is user-entered. Jira's connector already does `.TrimEnd('/')` (`JiraWorkTrackingConnector.cs:1297`); ServiceNow has no equivalent on this path. | Slice 01 DESIGN. |
| **OC-4** | Does #5610's board picker pre-fill the **label** or the **class name** once slice 02 ships? | Pre-requisite 4. Not a code dependency in either direction, but two stories landing opposite answers is the twin-drift shape #5613 already cost a release. | Maintainer + #5610, before slice 02. |

---

## Wave: DISCUSS / [REF] Recorded, not fixed here

- **`observedAvailability` is shared across teams.** It is a private mutable field on a connector
  instance that `WorkTrackingConnectorFactory` caches per work-tracking-system for 2 minutes and hands
  to every connection and team, so one team's sync overwrites another's verdict. Never persisted, so
  no UI can read it. This is a real defect independent of D9's NOT-NOW, and it is the *actual* cost of
  item 5 options (b) and (c). It becomes load-bearing the moment #5627 reports per-class availability
  for more than one team — flagged to #5627 rather than fixed here.
- **The label map is backend-only, and that is a deferral rather than a solution.** ADR-128 rejects a
  frontend map because it would duplicate across stacks (the Bug #5613 shape). But the map now lives
  only in the backend, so the *first* frontend feature that needs it — #5610's picker offering labels,
  an autocomplete, anything — recreates that exact problem. Raised by the DESIGN reviewer, 2026-08-01.
  Not solved here because nothing needs it yet; the pre-agreed answer, if it ever does, is to serve the
  map from the existing schema endpoint rather than to copy it.
- **`ServiceNowHistoryVerdict.From`'s contract is misdescribed by its consumer.** Its own remark is
  honest — *"Whether a definition measures state is a question this cannot answer"* — but #5627's body
  reads it as authoritative. The docstring is right; the ADR that cites it is wrong. Fixed in #5627.
- **The unmapped-state divergence (D11) is undocumented in user-facing docs.** Deliberate — documenting
  it would invite the "fix" D11 refuses. Recorded here so the omission is a choice, not an oversight.

---

## Wave: DISCUSS / [REF] WS strategy

**C — no walking skeleton.** Brownfield, on a connector that has shipped four slices plus two
follow-on stories. Both driving surfaces already exist end to end for four other connectors; there is
no unproven path to skeleton. The unproven part is one ServiceNow-instance fact (OC-1), which the
SPIKE is the right instrument for.

---

## Wave: DISCUSS / [REF] Scope Assessment: PASS

2 stories, 2 slices, 1 bounded context (work-tracking connectors). No oversized signal fires. Slice 01
ships with no dependency on the SPIKE, on #5610 or on #5627, so a failed SPIKE costs one slice.

Worth naming: the bucket **entered** oversized — 7 items spanning polish, a cross-connector
architecture question, and a defect blocking another story. The scope assessment's real output is the
verdict table above, which moved five of the seven out.

---

## Wave: DISCUSS / [REF] Slices and prioritization

Briefs in `slices/`. Order: the one that ships without a SPIKE goes first.

> **Superseded at DESIGN.** The two slices below **merged into one** — the same map serves display and
> input, so there is no second increment and `slice-02-name-work-by-its-label.md` was deleted. The
> table is kept because its learning hypotheses are still the ones being tested. The live slice list
> is under *Wave: DESIGN / [REF] Revised slices*.

| # | Slice | Ships | Learning hypothesis |
|---|---|---|---|
| 01 | A ServiceNow row reads like a ServiceNow record | Clickable ID + ServiceNow's own type label | **Disproves** "the connector's output is finished, only its configuration is rough" **if** the deep link or the label turns out to need a contract change beyond one nullable field — which would mean `Type`'s double duty as both query key and display string is a defect in the shared model, not a ServiceNow quirk |
| 02 | Name work by its label | Work Item Types accepts `Change Request` | **Disproves** "the class-name requirement is a knowledge barrier, not a typing one" **if** a coach given the alias still cannot name their work — which would mean the problem was never the vocabulary but the field itself, and #5610's board picker is the only real answer |

**Prioritization rationale.** Slice 01 first because it is the half the maintainer hit while dogfooding
and its hypothesis is the cheaper of the two to disprove. Slice 02 second, not because it is riskier —
D8 removed its risk entirely — but because it is the smaller win and depends on OC-4 being agreed with
#5610.

**Correction, 2026-08-01.** This wave first scoped slice 02 behind a mandatory SPIKE on the grounds
that `sys_db_object` is unreadable below `itil`. The maintainer's objection was correct and is now D8:
unreadability is an argument for having **no** runtime lookup, not for probing for a different one, and
5611 had already taken exactly that exit for hierarchy knowledge (ADR-116 D4). The SPIKE is cancelled
and OC-1 dissolves into a content question. Recorded rather than quietly rewritten, because the
reasoning error — treating a measured "no" as a research question instead of a design constraint — is
the kind worth being able to find again.

---
---

# Wave: DESIGN (2026-08-01)

Scope: **application / components** — one connector's mapper, one pure helper, one save path. No new
bounded context, no infrastructure, no new driving port. Mode: propose.

## Wave: DESIGN / [REF] Changed assumptions (back-propagation)

DESIGN overturns four DISCUSS decisions. The maintainer's correction, verbatim: *"it should just be
stored in the bloody type and the connector should have a hardcoded map of all known types and map it
in either direction when needed. if a type is not known, just use what the user entered … beyond the
snow connector, nobody even knows we mapped this."*

| DISCUSS said | DESIGN says | Why the original was wrong |
|---|---|---|
| **D4** — `Type` stays the class name; the label is a separate nullable field | `Type` **carries the label**. No second field. | D4's premise was that changing `Type` breaks `TeamMetricsService`'s Created Items comparison. True only if `Type` changes while the *config vocabulary* does not. Change both and they match by construction. The mismatch I guarded against was one I was creating. |
| **D5** — the label is read from the instance (`sys_class_name.display_value`), never derived | The label comes from the **map**. `display_value` is deliberately **not read**, though it is free. | Reading it gives an unknown class a pretty label on the item while its config entry keeps the class name — the two then stop matching and the run chart silently returns zero. A free field that desynchronises is worse than no field. Map-in-both-directions is symmetric by construction; `display_value` is not. |
| **D6** — new nullable field, DTO falls back to `Type` | No new field, no DTO change, no EF migration, no `Update()` line. | Follows from D4 being wrong. |
| **D8a** — the map is input-only and never a display source | The map is **both** directions. That is the whole mechanism. | D8a existed to protect against a renamed stock class displaying the wrong label. Real, but small — a different English label, never a wrong number — and it is not worth a second field plus two migrations. |

**What survives unchanged**: D1, D2, D3 (deep link from the record's own class), D7 (label input is
wanted), D8 (static map in source, no runtime lookup), D8b (collision rule), D9–D12 (the verdicts).

**Consequence for the story**: slices 01 and 02 **merge**. The same map that renders `Change Request`
is the map that accepts it as input, so there is no second increment to ship. See the revised slice
table below.

## Wave: DESIGN / [REF] Decisions

| ID | Decision | Rationale |
|---|---|---|
| **DD-1** | `ServiceNowRecordClasses` — new pure class, `ClassFor(label)`, case-insensitive, **passthrough on miss**. (`LabelFor` shipped alongside it and was removed at the post-implementation review, once the DD-2 supersession left it with no production caller.) | ADR-128. Passthrough is the load-bearing part: an unknown class flows through unchanged in *both* directions, so config and data stay consistent even where Lighthouse adds no value. |
| **DD-2** | ~~Inbound: `KindOfWork` returns `LabelFor(sys_class_name)`.~~ **Superseded at DELIVER — it returns `scope.AsTyped(sys_class_name)`**, the words *this team* used. | Not a global label but a per-team one, which makes config and data agree by construction and deletes DD-4 entirely. Full reasoning in ADR-128's amendment. The `sys_class_name`-absent fallback is `ServiceNowReadScope.RootTable`, the only value the old `table` parameter could carry since #5611 made every read task-rooted — passed through `AsTyped` as well, since the post-implementation review found it was otherwise the one row shape that could still diverge from a label-configured team's config. |
| **DD-3** | Outbound: `ServiceNowReadScope.For` maps each entry through `ClassFor` before the `sys_class_nameIN…` clause is built. | `For` already normalises (`Where(not blank).Select(Trim)`, `ServiceNowReadScope.cs:45-52`). This is one more `Select` in a class that is already pure and fully unit-tested. |
| **DD-4** | ~~On save, a ServiceNow team's `WorkItemTypes` are **normalised to the label form**.~~ **DELETED at DELIVER** — DD-2's `AsTyped` removes the divergence at source, so there is nothing to normalise. It had no home (`SyncTeamWithTeamSettings` is connector-agnostic), was bypassable by the API/CLI/MCP, and mutated what the coach typed. | Without it, a coach who types `change_request` gets a working sync (passthrough → correct query) but a **broken Created Items forecast**: `SuggestionsController` offers `change_request` from config, `ForecastController` passes it to `GetCreatedItemsForTeam`, and it is compared against `Type` = `Change Request`. Zero rows, no error. This is the one place the two vocabularies could still diverge, and normalising on save is where they converge. |
| **DD-5** | The deep link is `{instanceUrl.TrimEnd('/')}/{sys_class_name}.do?sys_id={sys_id}`, built from the **raw class**, not from `Type`. | Settles **OC-3** — Jira already does exactly this trim at `JiraWorkTrackingConnector.cs:1297` and `InstanceUrl` is user-entered. And it is the one place the class name is still needed *after* mapping, so the mapper reads `sys_class_name` once and uses it for both the link and the kind of work. |
| **DD-6** | No frontend change. At all. | Consequence of DD-2. All five FE type sites (`WorkItemsDialog.tsx:153`, the two chart legends, `scatterMarkerUtils.tsx:94`, and the rule engine's `"workitem.type"` at `evaluateCondition.ts:23`) read `item.type` and now receive whatever the team typed. The BE rule engine's `WorkItemFieldProvider.cs:56` likewise. A user-authored rule matches on the same string they see and configure — which after DD-2's supersession is true by construction rather than by everyone agreeing on one label. |
| **DD-8** | `ServiceNowReadScope` keeps **both forms** of each entry: the record class for every query, and the string the coach actually typed for every message. | Found while tracing DD-3, and it is the one place this design could still speak the platform's vocabulary at the user. `FirstUnreadableKindOfWork` (`ServiceNowWorkTrackingConnector.cs:676-690`) iterates `scope.KindsOfWork` and `WhyThisKindOfWorkCannotBeRead` names `recordClass` in its refusal (ADR-124 D2). After DD-3 that is the *mapped* value, so a coach who typed `Change Request` and hits a rights failure reads a message about `change_request` — a string they never entered. `KindsOfWork` keeps returning classes so every query path is unchanged; a companion lookup returns the typed form for the message. |
| **DD-7** | The map covers the stock ITSM task hierarchy only. | Settles **OC-1**. Confirmed present on the PDI across the SPIKEs: `incident`, `problem`, `change_request`, `sc_task`, `task`. Adding `sc_req_item`, `change_task`, `incident_task`, `problem_task` is free and being wrong about one costs nothing — passthrough. |

## Wave: DESIGN / [REF] Component decomposition

| # | Component | Path | Change |
|---|---|---|---|
| 1 | `ServiceNowRecordClasses` | `…/WorkTrackingConnectors/ServiceNow/` | **CREATE NEW** |
| 2 | `ServiceNowWorkItemMapper` | same | **EXTEND** — `KindOfWork` maps; `MapRecord` sets `Url` |
| 3 | `ServiceNowReadScope` | same | **EXTEND** — `For` maps entries through `ClassFor`, and keeps the typed form for messages (DD-8) |
| 3b | `ServiceNowTeamQueryVerdict` | same | **EXTEND** — refusals name the typed form (DD-8) |
| 4 | ~~ServiceNow team save path~~ | — | **NOT TOUCHED** — DD-4 deleted |
| 5 | `WorkItemBase` / `WorkItemDto` / `IWorkItem` | Models, API/DTO, frontend | **UNCHANGED** |

## Wave: DESIGN / [REF] Reuse analysis

| Existing component | File | Overlap | Decision | Justification |
|---|---|---|---|---|
| `ServiceNowWorkItemMapper` | `…/ServiceNow/ServiceNowWorkItemMapper.cs` | Maps a record field to a Lighthouse value | **EXTEND** | `KindOfWork` already resolves the class; adding one lookup is ~2 LOC. |
| `ServiceNowReadScope` | `…/ServiceNow/ServiceNowReadScope.cs` | Normalises configured entries before query construction | **EXTEND** | `For` is already the normalisation choke point. One more `Select`. |
| `ServiceNowTeamQueryVerdict` | `…/ServiceNow/ServiceNowTeamQueryVerdict.cs` | Pure per-class refusal messages | **EXTEND** | DD-8 only: the same rungs, naming the typed form instead of the mapped class. No new rung, no new verdict. |
| `ServiceNowValidationVerdict` | `…/ServiceNow/` | Pure connection-scope verdicts | **REUSE UNCHANGED** | DD-4 normalises; it does not add a refusal. An entry that is neither a class nor a label still reaches the existing zero-rows guard. |
| `WorkItemBase.Url` | `Models/WorkItemBase.cs:34` | Carries a record's source URL | **REUSE UNCHANGED** | Ships and is rendered. ServiceNow simply starts populating it. |
| `WorkItemsDialog` link rendering | `WorkItemsDialog.tsx:142-144` | Renders a non-null `url` as a link | **REUSE UNCHANGED** | No new component. |
| `ServiceNowChoiceLabelResolver` | *deleted* | Resolved choice labels at runtime | **DO NOT RESURRECT** | Killed by R-4 when `sys_choice` measured admin-only. DD-1 is the static answer to the same shape of problem. |

## Wave: DESIGN / [REF] Ports

**Driving**: none new. No endpoint, no schema field, no UI surface.
**Driven**: ServiceNow Table API — **no new request and no new field**. `sys_id` and `sys_class_name`
already arrive on every row; `display_value` arrives too and is deliberately ignored (ADR-128).

## Wave: DESIGN / [REF] Open questions

| ID | Question | Status |
|---|---|---|
| **OC-1** | Which classes does the map carry? | **SETTLED** — DD-7. |
| **OC-2** | Is `{class}.do` universal across ITSM classes? | **SETTLED YES by the dogfood, 2026-08-01.** A team reading `Incident, Change Request, Problem, Catalog Task` was synced on `dev191338` and a record of each class opened from the Work Items dialog. No 404. |
| **OC-8** | Does `sysparm_query`'s `IN` match a value case-sensitively? | **SETTLED NO by the dogfood, 2026-08-01**, and it mattered. Two curls against the PDI returned identical rows for `sys_class_nameINChange_Request` and `sys_class_nameINchange_request`. A wrong-case class name therefore queried successfully and then diverged from `Type`, because `AsTyped` compares ordinally — the silent zero, reached by a typo. Closed by making `ClassFor` canonicalise class-name case; see ADR-128's amendment. Raised as review finding F2 and accepted pending exactly this measurement. |
| **OC-3** | Does `InstanceUrl` need trimming? | **SETTLED** — DD-5, yes, per Jira's precedent. |
| **OC-4** | Does #5610's picker pre-fill the label or the class name? | **SETTLED as the label, 2026-08-01, and delivered in #5610.** `ServiceNowClassLabels.LabelFor` was restored — the class→label direction this ADR removed for having no caller, at the moment it predicted — and `ServiceNowBoardMapper.ToBoardInformation` labels the board's `table`. The lanes needed nothing: `vtb_lane.name` is already the display label. ADR-124's ladder still probes the **record class**, because `sys_class_name` holds the class and probing for `Change Request` would refuse every change board on the instance; only its message names the label. Proven end to end against `dev191338`: the label the picker hands over resolves back through `ClassFor` and still selects the board's own 38 of 105 incidents. |
| **OC-5** | Does a user-authored **rule** matching `change_request` break? | **SETTLED as near-empty, 2026-08-01.** "Rule" means a Lighthouse rule-set condition, not anything in ServiceNow. `WorkItemFieldProvider.cs:19` publishes `workitem.type` as a matchable field and `:56` returns `item.Type` raw, so a stored condition `Type equals change_request` stops matching once `Type` holds `Change Request` — silently, since a rule that matches nothing is not an error. Three surfaces share that engine: blocked items (`IBlockedItemService`, the Bug #5613 surface), the forecast filter (`IForecastFilterRuleService`), and delivery rules (`IDeliveryRuleService`, over Features). **Blast radius is the empty set**: nothing ServiceNow has shipped, and OC-6 makes every existing ServiceNow team disposable. Self-corrects going forward — the value picker is fed by `SuggestionsController.GetWorkItemTypesForTeams` from `team.WorkItemTypes`, which after DD-4's deletion is exactly what the coach typed and exactly what `Type` holds, so a newly authored rule matches whichever form that is. |
| **OC-6** | ~~What happens to rows already synced before this ships?~~ | **SETTLED, maintainer, 2026-08-01: "we can throw every existing snow team away, I wouldn't mind."** The DEVOPS reviewer was right about the mechanism — a team synced today holds `change_request` in `WorkItemBase.Type`, its config normalises to `Change Request`, and `GetCreatedItemsForTeam` compares the two — but wrong about the cost. **Nothing ServiceNow has ever been released**, so every ServiceNow team that exists is a dogfood team on `dev191338` and is disposable. No backfill, no migration, no re-sync ceremony, no window to document. Delete and recreate the teams if a stale row is ever confusing. This is the one moment in the epic's life when that answer is available; it will not be after the first release. |
| **OC-7** | ~~Rollback: rows written as labels while live, then a revert?~~ | **SETTLED by the same call.** Same disposability, and every kind of work resolves to one class from either of its names anyway, so a re-sync heals a mixed table without help. |

## Wave: DESIGN / [REF] Revised slices

Slices 01 and 02 **merge** — the map serves both directions, so there is no second increment.

| # | Slice | Ships |
|---|---|---|
| 01 | A ServiceNow row reads like a ServiceNow record | Clickable ID, `Type` reads `Change Request` everywhere, and `Change Request` is accepted in team config |

Effort: ≤1 day, and smaller than either original slice — one new pure class, three ~2-line call sites,
and their tests. Learning hypothesis unchanged from slice 01's brief.

---
---

# Wave: DISTILL (2026-08-01)

**Vehicle: NUnit, not Gherkin.** The repo has no `.feature` file for any backend behaviour — the three
that exist are Playwright/Helm. Backend acceptance tests are NUnit classes named
`*AcceptanceTest`/`*Test` with prose comments carrying the AC reference. Project convention wins over
the skill's pytest-bdd examples.

## Wave: DISTILL / [REF] Reconciliation

**Passed — 0 unresolved contradictions.** DESIGN overturns DISCUSS D4/D5/D6/D8a, but they are recorded
in the "Changed assumptions" table with the maintainer's decision, so they are resolved, not
contradictory. Scenarios are written against the DESIGN vocabulary.

**One upstream conflict found, and it is with a *shipped* story, not with this feature's own waves.**
#5611 recorded typing the label as the canonical user error and shipped a refusal for it —
`ServiceNowRecordClassTest.cs` says so verbatim: *"What a flow coach typing the label 'Change Request'
instead of the system name reaches"* and *"the flow coach reads 'Change Request' on their own screen
and has to type change_request"*. ADR-128 inverts that intent. The **test still passes** — it uses
`not_a_real_class`, which passes through the map unchanged and reaches the same refusal — so nothing
breaks, but ADR-124 rung 1's rationale is now half-obsolete and the comment is stale. A note is added
in place rather than deleting the history.

## Wave: DISTILL / [REF] Test placement

| File | Change | Why here |
|---|---|---|
| `…/ServiceNow/ServiceNowClassLabels.cs` | **NEW scaffold** | Production. Both methods throw with `__SCAFFOLD__` in the message. |
| `…Tests/…/ServiceNow/ServiceNowClassLabelsTest.cs` | **NEW** | Layer 1 (pure). The one place the map can be enumerated cheaply. |
| `…Tests/…/ServiceNow/ServiceNowRecordClassTest.cs` | **EXTEND** | Layer 3 (real adapter, stubbed transport). Its private helpers (`AnInstanceHolding`, `CreateSubject`, `ATeamWorkingOn`, `QueriesAskedOf`) are exactly what these scenarios need; a new file would duplicate them. Its stated subject — *"which kinds of work reach a team, and what the team is told"* — is this story. |

**Naming**: the class is `ServiceNowClassLabels`, not `ServiceNowRecordClasses`, because the latter sits
one letter from the existing `ServiceNowRecordClassTest` and would read as its subject.

**No collision with `main`**: neither file is among main's five dirty ServiceNow test files.

## Wave: DISTILL / [REF] Scenarios

| Scenario | AC | Status |
|---|---|---|
| `ServiceNowClassLabelsTest` — 23 cases: both directions per known class, round-trip, case-insensitivity, passthrough, misspelling, empty | AC-D2, ADR-128 | **RED** |
| `ATeamThatNamesItsWorkTheWayServiceNowDoes_ReadsTheSameWorkAsOneNamingTheRecordClass` | AC-B1 | **RED** |
| `WorkItemsOfAKnownKindOfWork_ReportTheKindTheCoachNamedRatherThanTheColumnValue` | AC-D3 | **RED** |
| `ATeamNamingItsWorkByLabel_EndsUpWithConfigAndWorkItemsSpeakingTheSameVocabulary` | **AC-D1** | **RED** |
| `ATeamNamingItsWorkByRecordClass_…` (same method, other case) | AC-D1 | **green — regression guard** |
| `ATeamNamingItsWorkByLabel_StillLooksForStateHistoryOnTheRecordClasses` | AC-D5 | **RED** |
| `ATeamNamingAKindOfWorkItCannotSee_IsRefusedInTheWordsTheCoachTyped` | AC-D4 | **RED** |
| `ATeamNamingAKindOfWorkLighthouseDoesNotKnow_AsksForItAndReportsItUnchanged` | AC-D2 | **green — regression guard** |

## Wave: DISTILL / [REF] Fail-for-the-right-reason gate

**PASSED.** Build 0 errors; no BROKEN. Full classification in `distill/red-classification.md`.

Two findings the gate produced, both worth more than the tests:

1. **AC-D1's test was authored vacuous.** As a single `[Test]` it exercised only the class-name
   configuration, which holds trivially today — green, and blind to the whole feature. Split into two
   cases so the label half is genuinely RED.
2. **The deep-link ATs are deliberately NOT authored here.** `MapRecord(record, owner, table)` has no
   instance URL and DD-5 needs one; a test against the new signature does not compile, which makes the
   *entire* test project BROKEN rather than making one test RED. They land in DELIVER's RED phase with
   the signature change. Deviation from ADR-025 recorded, not skipped.

## Wave: DISTILL / [REF] Adapter coverage

One driven adapter: the ServiceNow Table API. Covered at layer 3 by the stubbed-transport instance in
`ServiceNowRecordClassTest`, which routes by table and honours the class filter — a connector that
emits no filter gets everything back rather than a passing test. Real-I/O coverage over loopback
already exists in `ServiceNowTeamSyncAcceptanceTest` and needs no new scenario: this story changes the
*vocabulary* of a query, not the transport. The live proof is the dogfood, which is where OC-2 is
settled anyway.

## Wave: DISTILL / [REF] Pre-requisites for DELIVER

1. `ServiceNowClassLabels` scaffold replaced with the real map — this greens 23 of the 29 RED cases on
   its own.
2. `ServiceNowReadScope.For` mapping + typed-form retention (DD-3, DD-8).
3. `ServiceNowWorkItemMapper.KindOfWork` mapping (DD-2).
4. Save-path normalisation (DD-4).
5. `ServiceNowTeamQueryVerdict` messages naming the typed form (DD-8).
6. **In DELIVER's RED phase**: the deep-link ATs plus `MapRecord`'s signature change (DD-5).
7. Zero `__SCAFFOLD__` markers left under `Lighthouse.Backend/`.
