# Slice 03: Large Features lists open sooner, with the same dependencies (US-03)

**Goal:** the Portfolio and Team Features lists stop reading every Feature of the instance with its forecasts for
their dependency and in-progress columns, so a 920-Feature list's first row comes at least 40% sooner, with every
verdict unchanged.

**Job:** `job-reach-and-read-what-i-came-for-on-a-large-instance`. **Moves:** K2; K3 in part.

## IN scope

- Dependency verdicts for `features/ids` and `features/references` built from a light projection of the **whole**
  graph (ids, references, Portfolios), not the full Feature table with histograms (C2; maintainer, DIVERGE Q8).
- "In progress" answered from one query per Team, for the list and for a Team update's WIP step.
- Response bodies unchanged (D20); RBAC readability unchanged (C1). One exception (maintainer decision 13,
  2026-10-24): where two stored Features share one reference id, which today answers with a server error, the first in
  forecast order answers, counted once, with one warning per refresh naming the id.

## OUT of scope

- A new endpoint or payload (slice 04).
- Answering dependencies from the requested Features only (dropped in DISCOVER).
- Move permissions and the global order (cheap, X1, X2).

## Learning hypothesis

- **Disproves** "the instance-wide reads dominate the list" (A11) if the first row moves less than 40% idle at 920.
- **Confirms** it if the list's server time no longer depends on instance size (~1,000 vs ~3,000 Features, ≤ 15%).

## Acceptance criteria

US-03 AC-3.1 to AC-3.6 in `feature-delta.md`.

## Production data (D25)

Dev DB backup: every dependency verdict, withheld blocker, position and in-progress Team identical before and after;
the response bodies of `features/ids`, `features/references` and `featuresInProgress` compared byte for byte.

## Looks and sketch

- Loading / failure look: **N/A, because** the screen's requests and response shapes are unchanged.
- UI sketch at DISTILL: **N/A, because** nothing a user sees changes.

## Dependencies

Task 01a. Swaps freely with 02 and 05. Slice 04 follows it. `FeaturesController` and `TeamMetricsService` S05a
entries keep or shrink (D28).

## Effort

About 1.5 days. Reference class: `FeaturePositionMap` (a three-column projection) and Story 6354's narrowed Feature
read.

## Dogfood

Scaled instance and dev backup: open Northwind Platform and Team Borealis's Features tabs before and after; compare
the "Waits on" column and the in-progress Teams.
