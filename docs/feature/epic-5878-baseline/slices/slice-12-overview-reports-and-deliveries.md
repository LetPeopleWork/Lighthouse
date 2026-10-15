# Slice 12 — Reports and {Deliveries} on the Overview

**Feature**: epic-5878-baseline · **Epic**: #5878 · **Story**: US-12 (ADO #6305) · **Estimate**: ~½d · **Tier**: Community

## Goal

The Overview's Teams and Portfolios tables show each owner's reports as chips, two at most, then "+N more" to the
Reports tab; the {Deliveries} column follows the same rule, and both cells say when they are loading or could not load.

## IN

- Reports column on both tables (D64), two by name then "+N more" (D65), "No Reports".
- {Deliveries} column capped the same way (D66).
- Loading and could-not-load looks for both (D67); `DeliveriesChips` on a keyed query.

## OUT

A summary endpoint; sorting by the column; report chips elsewhere.

## Acceptance criteria

AC-12.1 … AC-12.8 in `feature-delta.md` (Story #6305).

## Dependencies

Slices 06 (Portfolio reports) and 08 (the owner list key the delete dialog reads).
