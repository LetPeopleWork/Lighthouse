# Slice 03 — `lh` reports what the web reports, for the same actions

**Story** #6193 / US-04 · **Job** `job-maintainer-know-if-a-shipped-feature-landed` · **Repo**
`lighthouse-clients` (`client`, `cli`) · **Estimate** ~5h

## Goal

With usage data on, the remaining eight mapped `lh` commands (nine events, D8) report their web events after success,
with `source: Cli`, and nothing else is reported.

## IN scope

- `lh team create|delete|refresh`, `lh portfolio create|delete|refresh`.
- `lh refinement vote` → `TeamSizingVoteCast` (+ `TeamSizingReadinessReached` on `madeReady`), moment by D9.
- `lh refinement get` → `TeamRefinementDayVerdictShown` on a Refinement day with ≥ 1 Work Item (D8 #15).
- The web's moment and verdict rules restated in `client` with parity tests.

## OUT of scope

- Every N/A row in D8; `comment`, `take-back`, `backtest`, `team update`.

## Learning hypothesis

**Disproves that the web's event rules restate cleanly from a client's facts** — if the vote answer or the
Refinement read lacks what `sizingMomentOf` / `refinementDayVerdict` need, the vote and verdict events cannot
be mirrored and D8 rows 13–15 move to N/A.

## Acceptance criteria

AC-04.1 … AC-04.6. Risk carrier: **AC-04.6**, a real Refinement-day `lh refinement get` on the dev instance.

## Dependencies

Slice 02 (store and reporter).

## Reference class

Epic #5733 slice 04 / #5980 — the web's event call sites.

## Pre-slice SPIKE

None.
