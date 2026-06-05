# Slice 04 — Forecast write-back date + historical×forward compose guard

**Story:** US-04 · **Surface:** forecast write-back to Jira/ADO (`WriteBackTriggerService`) · **Direction:** days→date

## Goal
One sentence: write the blackout-shifted date back to the work-tracking tool so the tool of record matches Lighthouse, and lock in that the shipped historical stripping and the new forward shift never double-count a blackout day.

## IN scope
- `WriteBackTriggerService:219` `Today.AddDays(daysToCompletion)` → the shared projector (Slice 01).
- Compose guard tests: assert the shipped blackout-aware historical throughput (sample) and the forward shift (projection) operate on disjoint concerns — a blackout day is never both stripped from the sample AND added to the projection in a way that changes the *days* value (US-04 AC3).

## OUT scope
- New write-back mappings/config. Monte Carlo (D4).

## Learning hypothesis
**Disproves** "historical-strip and forward-shift compose without double-counting" **if** the days value moves when both a blackout-in-history and a blackout-in-future are present.
**Confirms** the feature is internally consistent end-to-end and safe to finalize.

## Acceptance criteria
US-04 AC1 (written date == US-01 shifted date), AC2 (no-blackout unchanged), AC3 (compose guard green). Production-data: real team with both a historical and a future blackout period; verify the written field via a live write-back run.

## Dependencies
Slices 01–03 merged. Shipped historical stripping (locked, D1).

## Effort / reference class
~0.5–1 day. Reference class: write-back trigger edits + cross-feature regression tests.

## D8 gate
Forecast-result write-back code touched → brief + manual review BEFORE commit. Finalize candidate after this slice.
