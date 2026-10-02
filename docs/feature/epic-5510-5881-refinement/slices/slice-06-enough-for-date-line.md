# Slice 06 — Highlight the Work Items needed before the next Refinement

**Feature**: epic-5510-5881-refinement · **Epic (proposed)**: E2 Refinement need (#5881) · **Story**: US-06 ·
**Estimate**: ~½d · **Tier**: Community

## Goal

The list marks the first N Work Items in backlog order, where N is the band's high end, and draws the line "enough for
Thu 8 Oct (85%)". Below the line the list reads "not needed before then" (DD-1, DD-4).

## IN

- Row highlighting that does not rely on colour alone, plus the line row. Both are rendered from the same facts as the
  verdict.
- An edge case: when fewer Work Items are in refinement than the high end, the line says so.
- No line when there is no number (no cadence, or too little data).

## OUT

Any gating of votes by position. Votes stay open everywhere (DD-1).

## Learning hypothesis

**This disproves "a line in the list is read as the answer"** if the dogfood coach, looking at the tab, still asks
"so how many do we refine?". In that case the verdict sentence must name the Work Items as well as the count.

## Data and dogfood moment

- Demo: Team Gravity with a band of 5–8; the line falls after GR-073 (#8).
- Dogfood: show the tab to the coach of the dev Team at their next Refinement, without explaining it.

## Acceptance criteria

AC-6.1 … AC-6.3 (US-06).

## Dependencies

Slice 05.
