# Slice 02 — Blocked-time history capture + per-item duration

**Job**: `job-flow-coach-see-how-long-blocked` | **Persona**: flow-coach (Priya Nair)
**MoSCoW**: Must | **Est**: ~1 day | **Premium**: No

## Goal (one line)
Capture when each item enters/leaves blocked (Lighthouse-side per-sync, README L1) via a new
`WorkItemBlockedTransition` entity off the existing `WorkItemBlocked` event, and surface "blocked
\<N>d" per item.

## Learning hypothesis
**Disproves "per-sync enter/leave capture yields a trustworthy blocked duration" if**: sync-cadence
resolution loses so many short spells that the surfaced duration misleads coaches, OR leave-detection
(no `WorkItemUnblocked` event today) proves unreliable against a real connector's sync delta.

## In scope
- New capture entity (e.g. `WorkItemBlockedTransition { WorkItemId, EnteredAt, LeftAt? }`), NOT reusing
  `WorkItemStateTransition` (L1, README data-foundation note).
- Enter capture off the existing edge-triggered `WorkItemBlocked` event
  (`!WasBlockedBeforeSync && IsBlocked`, WorkItemService ~line 121).
- NEW leave-detection: `WasBlocked && !IsBlocked` on a sync closes the open spell (`LeftAt`).
- Per-item "blocked \<N>d" badge (current spell, D6) on team/portfolio views, with the rule-based
  reason available; "—" for first-observation items; tooltip "Approximate — based on sync cadence".

## Out of scope
- Over-time count chart (slice 03). Blocked→stale (slice 04 — consumes `blockedSince` from here).
- Cumulative across past spells (badge shows CURRENT spell only, D6).

## Production-data AC (drive via demo data + real connector)
- AC1: An item that becomes blocked (rule match) on a sync gets a `WorkItemBlockedTransition` with
  `EnteredAt` = that sync; the badge shows a growing "blocked \<N>d" from that timestamp.
- AC2: When the item later no longer matches the blocked rule on a sync, the spell closes (`LeftAt`
  set) and the badge clears.
- AC3: An item already blocked at release (no prior capture) shows "—" until the next sync sets a
  baseline (first-observation behaviour, twin of time-in-state).
- AC4: The duration tooltip reads "Approximate — based on sync cadence".
- AC5 (@property): `blockedSince` derives only from items that are `IsBlocked` per the slice-01
  `BlockedRuleSet` — no second blocked definition.

## Dogfood moment
On a real-connector team, block an item, run a sync, see "blocked 0d"; next day confirm it reads the
elapsed days; unblock and confirm the badge clears.

## Cross-cutting
- **RBAC**: read surface — inherits existing metric/work-item read gating; no new write surface.
- **Clients**: per-item blocked duration is a new read field on the work-item DTO — version-gate if the
  clients expose work items; see feature-delta.
- **Website**: N/A (non-premium).

## Dependencies
Slice 01 (`IsBlocked` from `BlockedRuleSet`). Builds on epic-5121 `WorkItemBlocked` event.
