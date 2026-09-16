# Slice 02 — At-risk count on the Items In Progress card

**Feature**: `epic-4127-sle-risk` | **Stories**: US-02 | **Estimate**: ~0.5 day

**Gated by `OUT-4127-risk-stability`** — do not start until slice 01's stability measurement has been read. A count chip built on an unstable number is a count that changes overnight for no visible reason.

## Goal

A flow coach landing on the team page sees, without opening anything, how many in-flight items are more likely than not to breach the SLE — and therefore whether the dialog is worth opening at all.

## Learning hypothesis

**The count is what draws the click.**

Confirms if it succeeds: a summary count on the card is enough to decide whether a triage conversation is needed, and clicking through from the chip becomes the normal route into the dialog rather than an accident of exploration.

Disproves if it fails: that the card is a useful surface for this signal. If coaches reach the dialog by other routes and never by the chip, the chip is decoration in a 44–80px box that already has a tenant — and the honest response is to remove it rather than let it compete with `Goal: N` forever. Measured by `OUT-4127-column-used` narrowed to entries whose dialog was opened from a row carrying a chip.

## Production data

The same production-restored teams as slice 01. Three cases must appear on real data rather than in fixtures:

- a row with at-risk items **and** an `idealWip` set, so both chips compete for the one slot and the count column's alignment is tested where it actually breaks (AC-02.5);
- a row with zero at-risk items, confirming absence rather than a `0 at risk` chip;
- a team with no SLE, confirming the row renders exactly as today.

## Dogfood moment

Same day: open the dogfood instance's team page, read the chip, click the row, and confirm the chip's count equals the number of dialog rows at or above 50% (AC-02.4). The two surfaces are one click apart, so a disagreement is visible immediately — which is the point of checking them together.

## IN scope

- A chip on each Items In Progress row counting that entry's items at ≥ 50% risk (D11), coloured by severity from `PACE_BAND_COLORS_LOW_TO_HIGH` so it speaks the same colour language as the dialog column and the chart.
- `Beyond history` items counted as at-risk (AC-02.6) — an item older than every completed item is not a safe item.
- Chip absent at a count of zero and absent when no SLE is set; the row renders exactly as today in both cases (AC-02.2).
- Layout work so the fixed-width chip box at `ItemsInProgress.tsx:148-172` holds the risk chip alongside or in place of `Goal: N` without the big count number losing its alignment across rows (AC-02.5).
- The chip reads the value slice 01 already computed — no second calculation anywhere (D5).
- Vitest coverage: chip present / absent / zero-count, both chips together, no-SLE row, colour by severity, and count parity with the dialog's rows at or above the threshold.

## OUT scope

- Any threshold other than 50%, and any setting to change it (D11). If the number proves wrong, that is evidence for a follow-up, not a knob to ship now.
- The chart background mode (slice 03) and write-back (slice 04).
- Portfolio scope (D4) — the card renders on portfolio pages and must there behave identically to the no-SLE case.
- Changing what the row's click already does, or the dialog it opens beyond the column slice 01 added.
- Any notification, badge or alert. Everything here is pull.
- Docs and screenshots — feature finalization.

## Acceptance criteria

Every AC listed under US-02 in `feature-delta.md` (AC-02.1 … AC-02.6).

## Dependencies

Slice 01, for the computed value. Hard-gated on `OUT-4127-risk-stability` having been read, not merely on slice 01 having merged.

## Reference class

The existing `Goal: N` chip in the same box (`ItemsInProgress.tsx:158-171`) — same component, same constraint, already solved once for a single chip. The new work is entirely in making the box hold two.

## Pre-slice SPIKE

Not needed. The only unknown is layout at narrow widths, which is cheaper to see in the browser than to investigate separately.
