# Slice 07 — ServiceNow reads state spans only for records that moved

**Feature**: epic-5687-faster-updates · **ADO**: Epic #5687 → Story #5730 · **Story**: US-07 · **Estimate**: ~5h
**Reference class**: slice 02's contract; `ServiceNowWorkTrackingConnector.ReadEveryPage` already carries
a stable ordering clause (`InAStableOrder`) and a repeated-record guard, which is most of what a safe
sweep needs.

**Subject to the D7 checkpoint.**

## Goal

A ServiceNow refresh reads per-record state spans and history only for records whose `sys_updated_on`
moved, instead of for every record in the result set.

## IN scope

- Identity sweep over the same `sysparm_query`, with `sysparm_fields` narrowed to `sys_id` +
  `sys_updated_on`, keeping `InAStableOrder` so the existing paging guard still holds.
- `LastChangedRemote` populated from `sys_updated_on`.
- `ReadHistory` / `ReadSpans` restricted to the changed record set.
- Removal semantics unchanged (D2) — the sweep still enumerates the full record set.
- The existing `paging_repeated_records` guard still fires on a genuinely repeated `sys_id`.

## OUT of scope

- Linear (slice 08).
- Anything about which record classes carry state definitions — that is settled behaviour
  (`project_servicenow_metric_definitions_per_class`) and this slice does not touch it.
- The board picker, query guidance, or class filter surfaces.
- Any UI.

## Learning hypothesis

**Disproves "narrowing `sysparm_fields` leaves the paging guard intact."** The connector's paging is the
most defensive of the four — it counts rows, follows Link headers, orders results, and guards against
repeated `sys_id`s because the PDI demonstrably returns them
(`project_servicenow_paging_guard_keys_number`). If narrowing the field list changes how the instance
sizes or orders the result set, the sweep either loses records — which would delete live items — or
trips its own guard on every cycle.

Secondary: **disproves "the span read is the cost."** ServiceNow deployments in the field are smaller
than the Jira DC instance that motivated the epic. If the record read dominates and the span read is
marginal, this slice's value is low and the D7 checkpoint should send it to a follow-on feature.

## Acceptance criteria

AC-7.1, AC-7.2, AC-7.4, AC-7.5 from `feature-delta.md` (US-07).

## Dependencies

- Slices 02 and 05.
- The D7 checkpoint verdict.
- The ServiceNow PDI already used by epic-5513, with its seeded demo records.

## Effort

~5h. Sweep + field narrowing ~1.5h, restricting span/history reads ~1.5h, paging-guard tests ~2h.

## Production data / dogfood moment

The PDI, with the epic-5513 demo seeder's records — the same data that produced the real
`paging_repeated_records` collision, so the guard is exercised for real rather than in a fixture. One
full cycle, one delta cycle, same day, with the record counts compared.

## Pre-slice SPIKE

Not needed. The paging question is answerable inside the slice against the PDI.

## Verdict

_(recorded at slice close)_
