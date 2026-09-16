# Slice 04 — SLE Risk as a write-back value source

**Feature**: `epic-4127-sle-risk` | **Stories**: US-04 | **Estimate**: ~0.5 day

**Gated by `OUT-4127-risk-stability`** — and more strictly than slices 02 and 03. An unstable number written into a tracker is an unstable number in someone else's issue history, filters and automation rules, where Lighthouse cannot take it back.

## Goal

A team whose work lives in Jira or Azure DevOps gets each in-flight item's SLE risk written onto the item itself, so the signal reaches the people working the board rather than only the people already looking at Lighthouse.

## Learning hypothesis

**The risk is trusted enough that people want it outside Lighthouse.**

Confirms if it succeeds: configuring the mapping is a thing users actually do, and the value gets used on the board — a filter, a column, a dashboard, an automation rule. That is the strongest available evidence the number is believed, because it costs the user tracker noise to have it.

Disproves if it fails: that the metric has earned a place outside the tool. If nobody configures the mapping, or if configured mappings get switched off after a few weeks, the signal belongs in the read surfaces only — and the noise cost below was paid for nothing.

## Production data

The dev instance restored from a production backup, connected to a **real** Jira or Azure DevOps sandbox project with a real custom field as the write target. Three cases on real items:

- an in-flight item with a computable risk — the field is written with the integer (AC-04.3);
- an item with no computable risk (closed, or `Beyond history`) — **no write at all**, not an empty write (AC-04.4);
- a team with no SLE set — no writes for any of its items.

A synthetic connector would prove the `switch` arm and miss what actually matters: that the value lands in the shape the target field accepts, and that the no-write cases genuinely produce no tracker activity.

## Dogfood moment

Same day: configure the mapping on the dogfood instance's own connection against a scratch field, run a team update, and read the field on a real issue — then compare it against the dialog column for the same item and day. That comparison is the cross-system agreement check that D5 exists to guarantee, and it is the only place it can actually be observed.

## IN scope

- A new `WriteBackValueSource.SleRisk` enum member, its `WriteBackMappingValidator` entry, and its arm in `ResolveWorkItemValue` (`WriteBackTriggerService.cs:178-189`), returning the integer percentage as text — `86`, not `86%` and not `0.86` (AC-04.3).
- Team-scoped only; not offered for `Portfolio` mappings (D4, AC-04.2). The portfolio resolution paths at `:62-70` are untouched.
- The value computed by the same domain function the read surfaces use (D5), called with the team's configured history window and `clock.Today` rather than any UI range (D6, AC-04.5).
- No write for any item with no computable risk, following the existing `=> null` convention that already governs `WorkItemAgeCycleTime` (AC-04.4).
- Frontend: the new source appears in the write-back mapping editor's value-source list for team mappings, labelled through `TERMINOLOGY_KEYS.SLE` (D10).
- Backend tests: the new arm for each of computable / no-SLE / closed / `Beyond history`, portfolio mappings not offering the source, premium gating, and a resolution failure being swallowed and logged without cutting short the rest of the update (AC-04.6).
- Confirm before writing any code whether `WriteBackValueSource` is persisted by **name or by ordinal** in `WriteBackMappingDefinition`. Appending a member is safe either way; it is worth one look rather than one migration.

## OUT scope

- **Any noise mitigation** — no threshold-crossing writes, no change-magnitude filter, no bucketing (D16). Threshold-crossing writes were offered in DISCUSS and declined: write-back is opt-in per mapping, so the choice belongs to the user who enabled it. The cost is real and is recorded, not hidden: the value moves for every in-flight item on every refresh, and per `quiet-jira-writeback` D1 Jira can suppress watcher **email only** — issue history, the `Updated` timestamp, webhooks, listeners and automation rules fire on every deployment regardless. A future noise report is this decision surfacing, not a new defect.
- Portfolio and feature write-back (D4).
- Any new `WriteBackTargetValueType`. The percentage travels as `FormattedText`, which is what exists.
- Any change to when write-back fires. It rides the existing post-update trigger; no new event, no new schedule.
- Docs and screenshots — feature finalization, which for this slice must include the noise trade-off in the write-back documentation so a user enabling the mapping reads it before, not after.

## Acceptance criteria

Every AC listed under US-04 in `feature-delta.md` (AC-04.1 … AC-04.6).

## Dependencies

Slice 01, for the domain function. Hard-gated on `OUT-4127-risk-stability`.

## Reference class

`WorkItemAgeCycleTime` — the existing per-item, all-items, raw-number team write-back source. This slice is the same shape with a different value, and the plumbing it needs is already load-bearing in production.

## Pre-slice SPIKE

Not needed. The path is the one `WorkItemAgeCycleTime` already walks, and the single open question — name-vs-ordinal persistence of the enum — is a code read, not an experiment.
