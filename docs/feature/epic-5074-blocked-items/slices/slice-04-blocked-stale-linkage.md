# Slice 04 — Blocked → Stale linkage

**Job**: `job-flow-coach-stale-when-blocked-too-long` | **Persona**: flow-coach (Priya Nair)
**MoSCoW**: Should | **Est**: ~1 day | **Premium**: No

## Goal (one line)
A blocked item becomes Stale after a configured blocked-duration threshold — a DISTINCT trigger from
today's time-in-state staleness — OR'd into the existing stale signal with a distinct reason.

## Learning hypothesis
**Disproves "blocked-duration staleness is a meaningfully distinct signal from time-in-state
staleness" if**: in real data the items it flags are already all flagged by time-in-state staleness
(no net new signal), OR coaches can't tell the two reasons apart and find the combined signal
confusing.

## In scope
- New `blockedStalenessThresholdDays` setting per Team and Portfolio (0 = disabled; twin of
  `StalenessThresholdDays`; config-admin gated).
- New staleness trigger: item blocked longer than the threshold (from `blockedSince`, slice 02) is
  Stale.
- OR with existing time-in-state staleness into ONE stale state, with DISTINCT reasons reported
  ("stale: blocked 12 days" vs "stale: 11 days in Review").

## Out of scope
- Changing the existing time-in-state staleness logic. New stale notification/push (epic-5121 territory,
  out of this Epic).

## Production-data AC (drive via demo data + real connector)
- AC1: With `blockedStalenessThresholdDays=10`, an item blocked 12 days renders stale with reason
  "stale: blocked 12 days".
- AC2: An item that is both blocked-stale and state-stale renders stale ONCE, listing both reasons (not
  double-counted).
- AC3: With `blockedStalenessThresholdDays=0`, blocked-duration staleness is disabled; only
  time-in-state staleness applies.
- AC4: A blocked item under the threshold is not stale-by-blocked (boundary: exactly at threshold ⇒
  decision recorded in feature-delta as ≥, matching time-in-state's `>`).
- AC5 (@property): the blocked-duration that crosses the threshold derives from the same `blockedSince`
  capture (slice 02), which derives from the same `BlockedRuleSet` (slice 01).

## Dogfood moment
Set a low blocked-staleness threshold on a real team with an aged blocker; confirm it appears in the
stale signal with the blocked reason, distinct from any state-stale items.

## Cross-cutting
- **RBAC**: `blockedStalenessThresholdDays` write inherits the existing settings gate (config-admin via
  `IRbacAdministrationService`, UI via `useRbac()`). Read inherits metric read gating.
- **Clients**: changed settings contract (new threshold field) — version-gate; see feature-delta.
- **Website**: N/A (non-premium).

## Dependencies
Slice 01 (`IsBlocked`) and slice 02 (`blockedSince`). Ties into Epic #4144 staleness.
