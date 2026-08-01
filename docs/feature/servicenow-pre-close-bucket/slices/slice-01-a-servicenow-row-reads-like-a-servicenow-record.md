# Slice 01 — A ServiceNow row reads like a ServiceNow record

**Goal**: A flow coach opens the record in one click, reads its type as *Change Request*, and can type
*Change Request* into the team's Work Item Types.

**Story**: Story A + Story B (both value). **Revised at DESIGN, 2026-08-01** — the original slice 02
merged into this one: the same map serves display and input, so there is no second increment.

## IN scope
- `ServiceNowRecordClasses` — new pure class, `LabelFor(class)` / `ClassFor(label)`, case-insensitive,
  **passthrough on miss** (ADR-128, DD-1). Covers the stock ITSM task hierarchy (DD-7).
- `ServiceNowWorkItemMapper.KindOfWork` returns `LabelFor(sys_class_name)` (DD-2).
- `ServiceNowWorkItemMapper.MapRecord` sets `Url` to
  `{instanceUrl.TrimEnd('/')}/{sys_class_name}.do?sys_id={sys_id}`, from the **raw class**, not from
  the mapped `Type` (DD-5). No `Url` at all when either part is missing.
- `ServiceNowReadScope.For` maps each configured entry through `ClassFor` before the
  `sys_class_nameIN…` clause (DD-3).
- A ServiceNow team's `WorkItemTypes` normalised to the label form on save (DD-4).

## OUT of scope
- **Any change outside the ServiceNow connector.** No `WorkItemBase` field, no DTO, no EF migration,
  no `Update()` line, no frontend. If a change is needed outside `…/WorkTrackingConnectors/ServiceNow/`
  plus the team save path, the design is wrong — stop and re-open ADR-128.
- Reading `sys_class_name.display_value`. Free, and deliberately ignored — see ADR-128.
- Any runtime label lookup (`sys_db_object`, `sys_dictionary`).
- Changing what #5610's picker pre-fills — OC-4, a conversation, not code here.

## Learning hypothesis
**Disproves** "the connector's output is finished, only its configuration is rough" **if** the map has
to leak past the connector boundary to work — a DTO field, a frontend format call, a second
vocabulary anywhere. That would mean `Type`'s double duty as query key and display string is a defect
in the shared model rather than a ServiceNow quirk, and the fix belongs at the model.
**Confirms** that a connector can speak its instance's vocabulary entirely on its own side of the port.

## Acceptance criteria
Story A **AC-A1–AC-A3, AC-A7** and Story B **AC-B1, AC-B2, AC-B4** in `feature-delta.md` stand as
written. Revised at DESIGN:

- **AC-A4** — the Type column shows `Change Request`, sourced from the **map** (ADR-128), not from
  `display_value`.
- **AC-A5/AC-A6** — superseded. There is no second field and `Type`'s value *does* change. The
  regression they guarded is now covered by **AC-D1** below.
- **AC-B3** — resolution makes no request and behaves identically at every privilege level. Unchanged
  in substance.
- **AC-B5/AC-B6** — superseded by ADR-128: one map, both directions, so there is no class-name-vs-map
  precedence question and no separate display source.

New, from DESIGN:

- **AC-D1** — `GetCreatedItemsForTeam` returns a non-zero run chart for a ServiceNow team, for a team
  configured with **labels** *and* for one configured with **class names**. This is DD-4's guard and
  the single most important test in the slice — it is the silent-zero the whole design exists to avoid.
- **AC-D2** — an **unknown** class round-trips unchanged in both directions: a record of class
  `u_maintenance_task` stores `Type = u_maintenance_task`, and a team configured with
  `u_maintenance_task` reads it. Passthrough is the load-bearing behaviour, not the edge case.
- **AC-D3** — every consumer of `Type` sees the label with no code change: Type column, both chart
  legends, marker colours, and both rule engines (`WorkItemFieldProvider.cs:56`,
  `evaluateCondition.ts:23`). Verified by reading, not by editing.

## Dependencies
- **None blocking.** #5611 and #5577 shipped; nothing here needs the PDI to be answered first.
- **OC-4** — agree the picker pre-fill with #5610 before it finishes DELIVER on `main`.
- **OC-5** — confirm with the maintainer that no ServiceNow rule authored against a class name exists
  in the wild. Nothing ServiceNow has ever been released, so the expected answer is none.

## Effort / reference class
≤1 day, and **smaller than either original slice**: one new pure class plus three call sites of ~2
lines each, all inside classes that are already pure and fully unit-tested. Closest reference class is
5611's static hierarchy set (hours).

## Pre-slice SPIKE
**None.** Every instance fact this needs was already measured. OC-2 (`{class}.do` universality) is the
only unverified assumption and it self-resolves at the dogfood below.

## Dogfood moment
Same day, by the person who filed the finding, on `dev191338`:

1. Create a team typing **`Incident`** and **`Change Request`** — never the class names. Save, sync.
2. Open Work Items in Progress. Click through to a record of **each** class — this is what settles
   OC-2, and a single-class check would not.
3. Read the Type column and both chart legends. All should say *Incident* / *Change Request*.
4. Re-open team settings and confirm the stored values round-trip without surprising the coach.
5. Run a Created Items forecast for one of those types and confirm it is not empty (AC-D1, live).
