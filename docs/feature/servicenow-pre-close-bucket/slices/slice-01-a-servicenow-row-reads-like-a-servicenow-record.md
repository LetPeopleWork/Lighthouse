# Slice 01 — A ServiceNow row reads like a ServiceNow record

**Goal**: A flow coach can click a ServiceNow item's ID to open the record, and reads its type as
*Change Request* rather than `change_request`.

**Story**: Story A (value).

## IN scope
- `WorkItemBase.Url` populated for ServiceNow as `{instanceUrl}/{sys_class_name}.do?sys_id={sys_id}`,
  built from **the record's own class** (D3) so a two-class team gets correct links for both.
- A new **nullable** type-label field on `WorkItemBase` → `WorkItemDto` → `IWorkItem`, set from
  `sys_class_name.display_value`, with the DTO falling back to `Type` when absent (D5, D6).
- Test factory / builder extended **before** the contract, per the project's shared-contract rule.
- A test proving `Type` still carries the class name: `GetCreatedItemsForTeam` for a ServiceNow team
  whose work-item types are class names still returns a non-zero run chart (AC-A5).
- `@screenshot` E2E re-run for the Work Items dialog — **`rm` the old PNG first** (<0.5 % diff
  silently keeps the old image).

## OUT of scope
- Accepting a label in team config — slice 02, and it needs a SPIKE.
- Any change to `Type`'s value or to the other four connectors' rows (D4, D6).
- Anything #5627 owns.

## Learning hypothesis
**Disproves** "the connector's output is finished, only its configuration is rough" **if** either half
needs a contract change beyond one nullable field — which would mean `Type` doing double duty as query
key *and* display string is a defect in the shared model rather than a ServiceNow quirk, and the fix
belongs at the model, not at this connector.
**Confirms** that ServiceNow rows are now indistinguishable in affordance from Jira/ADO/Linear rows.

## Acceptance criteria
See Story A, AC-A1–AC-A7 in `feature-delta.md`.

## Dependencies
- **None blocking.** #5611 (`sys_class_name` → `Type`) and #5577 (`sys_id` through the mapper) both
  shipped, and they are what make this cheap.
- Settle **OC-2** (is `{class}.do` universal across ITSM classes?) and **OC-3** (does the instance URL
  need `.TrimEnd('/')` the way Jira's does at `JiraWorkTrackingConnector.cs:1297`?) during DESIGN.
  Both are one manual check each.

## Effort / reference class
≤1 day. Backend: one string built in `ServiceNowWorkItemMapper.MapRecord` plus one nullable field
carried through two DTOs. Frontend: **no new component** — `WorkItemsDialog.tsx:142-144` already
renders `url` as a link, and the Type column at `:153` reads one field. Closest reference class is the
`sys_class_name` → `Type` mapping in 5611, which was a half-day.

## Pre-slice SPIKE
**None.** Nothing here is an unmeasured instance fact. Both fields were already measured present in
every row of the connector-shaped read (5611 SPIKE `findings.md:142,146-147`), and no additional
request is made.

## Dogfood moment
Same day, by the person who filed the finding: open the Work Items in Progress dialog on a `dev191338`
team reading incident + change_request, click through to a record of **each** class, and read the Type
column. Both classes must link correctly — a single-class check would not exercise D3.
