# Slice 03 — Risk zones as a third background mode on the Work Item Aging chart

**Feature**: `epic-4127-sle-risk` | **Stories**: US-03 | **Estimate**: ~1 day

**Gated by `OUT-4127-risk-stability`** — same gate as slice 02. Zone boundaries that move day to day are worse than no zones, because the chart's whole claim is that its geometry means something.

## Goal

A flow coach reading the aging chart sees where the odds turn against an item, not only where the deadline is — and gets that by switching one control, without the chart carrying two background schemes at once.

## Learning hypothesis

**The risk gradient tells a coach something the SLE line alone does not.**

Confirms if it succeeds: coaches switch to risk mode and keep it there, because the zones answer "which of these still-safe items is about to stop being safe" — a question the single deadline line cannot answer, since everything below it looks equally fine.

Disproves if it fails: that the chart is the right medium for this metric at all. Because the risk is end-to-end, it depends only on a dot's height — so the zones are horizontal, which is information the y-axis already carries. If users leave the control on `Pace percentiles` or `Off`, the honest reading is that the per-state pace bands are the more useful background and the risk belongs in the list surfaces only. Measured by `OUT-4127-risk-mode-used` against the 15% bar `aging-pace-percentiles` set for its own overlay.

## Production data

The same production-restored teams as slice 01. Two cases matter on real data:

- a team whose risk crosses 25/50/75% at ages that are **not** evenly spaced, which is the normal case and the one that shows the zones carry real distribution shape rather than decoration;
- a team with a thin history, where adjacent thresholds may land on the same age or collapse entirely — the zones must degrade legibly rather than paint zero-height bands.

A team with no SLE is needed to confirm the mode is unavailable rather than empty (AC-03.4).

## Dogfood moment

Same day: on the dogfood instance, switch the control through all three modes and confirm the 100% zone's lower edge lands exactly on the existing SLE reference line (AC-03.3). That agreement is the slice's central correctness claim — if the zone and the line disagree, the chart is asserting two different deadlines.

## IN scope

- Migrating `useShowPaceBands` from a boolean to a tri-state background mode: `Off` / `Pace percentiles` / `${sle} Risk` (D12). The existing `workItemAgingPaceBandsEnabled` localStorage value maps `"true" → pace`, `"false" → off`, so no user's chart changes appearance on upgrade (AC-03.6).
- Full-width horizontal zones at the ages where risk crosses 25%, 50%, 75% and 100%, derived from the same computed values slice 01 produces — not a second implementation of the rule (D5).
- The 100% zone's lower edge at exactly the SLE range in days, coincident with the existing `sleVisible` reference line, which stays and supplies the label the boundary would otherwise need (D13).
- Colours from `PACE_BAND_COLORS_LOW_TO_HIGH` (D14), so a colour means the same thing in the chart background, the dialog column and the card chip.
- `${sle} Risk` mode unavailable — not merely empty — when the owner has no SLE (D3, AC-03.4), and on portfolio pages (D4).
- Dot colouring untouched: blocked state (`WorkItemAgingChart.tsx:303`) and group (`:614`) keep the dot colour channel (D15).
- Vitest coverage: mode migration from each legacy boolean value, mutual exclusivity of the two background schemes, zone boundary equal to the SLE line, mode unavailable without an SLE, zones degrading on a thin history, and dot colours unchanged across all three modes.

## OUT scope

- Any per-dot risk annotation, tooltip line or label. `aging-pace-percentiles` cut exactly that (its `step-confirm-via-tooltip` / old US-03) and the cut stands — the background carries the signal, the dialog carries the number.
- Recolouring the dots by risk (D15).
- Removing or restyling the SLE reference line, which the zones complement rather than replace (D13).
- Any change to the per-state pace-band geometry or its computation.
- Portfolio scope (D4).
- The Cycle Time Scatterplot, which carries its own SLE line and is a history surface, not an in-flight one.
- Docs and screenshots — feature finalization.

## Acceptance criteria

Every AC listed under US-03 in `feature-delta.md` (AC-03.1 … AC-03.7).

## Dependencies

Slice 01, for the computed values behind the zone boundaries. Hard-gated on `OUT-4127-risk-stability`.

## Reference class

`aging-pace-percentiles` slice 01 — the same chart, the same palette, the same background-zone rendering, the same localStorage-backed toggle. The one genuinely new thing is the boolean-to-tri-state migration, which has to leave every existing user's chart looking exactly as it did.

## Pre-slice SPIKE

Not needed. Both the rendering technique and the data are already in the component; the risk is in the migration, which is covered by explicit tests from each legacy value rather than by investigation.
