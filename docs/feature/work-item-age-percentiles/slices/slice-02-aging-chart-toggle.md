# Slice 02 — Aging-chart CT ↔ WIA toggle (Team)

**Type:** vertical | **Est:** ~1 day | **Stories:** US-02

## Learning hypothesis

Coaches want to contrast their live-WIP age distribution against historical cycle-time on the **same** aging chart. Disproved if the toggle is never flipped or if users conflate the CT and WIA line meanings.

## What ships

- A switch on `WorkItemAgingChart.tsx` that **swaps** the horizontal reference lines between Cycle Time percentiles (default — current behaviour) and Work Item Age percentiles (50/70/85/95 of current WIP age). Mutually exclusive (D2).
- Reuses the WIA percentile values from Slice 01; the chart consumes the same source as the card (single source of truth).
- Clear axis/legend labelling so CT lines and WIA lines are never confused.

## IN scope

- Team scope only.
- Toggle round-trips client-side, no page reload.
- Pace-band overlay chip left untouched and orthogonal (D2).
- Empty-WIP → WIA position shows no lines, no crash (D6).

## OUT of scope

- Portfolio (Slice 03), the overview card (Slice 01), per-state WIA.

## Production-data AC

- Given the Team aging chart in default (CT) position, when the coach flips the switch to WIA, then the reference lines redraw at the 50/70/85/95 of current in-progress ages and the CT lines disappear.
- Given the switch in WIA position, when flipped back, then the CT lines return — without a page reload.
- Given zero in-progress items, when flipped to WIA, then no reference lines render and the chart does not crash.

## Taste tests

- Not 4+ new components: one toggle control + line-source switch on an existing chart. PASS.
- No new abstraction first: reuses Slice 01's WIA percentile values. PASS.
- Disproves a pre-commitment (coaches want the on-chart contrast). PASS.
- Production/demo data. PASS.
- Value-bearing. PASS.
