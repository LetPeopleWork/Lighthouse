# Slice 02 — Work Item Age percentiles over time

**Goal**: A flow coach clicks a **WIA** toggle on the same "Percentiles Over Time" widget and sees
work-item-age percentiles (50/70/85/95) trending day by day.

**Stories**: US-03 (value).

## IN scope
- WIA-percentile daily snapshot recorded through the **same** US-02 pipeline (no horizon dimension — age is as-of-today).
- WIA tab added to the combined widget → toggle becomes `[ WIA | CT-30 | CT-60 | CT-90 ]`.
- WIA series on the same HTTP endpoint family.

## OUT of scope
- PBC widget (slice 03/04). Any horizon concept for WIA (none exists).

## Learning hypothesis
**Disproves** "one recording pipeline serves multiple percentile families" **if** WIA needs a second
bespoke recorder/table rather than dropping into the slice-01 pipeline.
**Confirms** the pipeline generalizes to a horizon-less metric — cheap second family.

## Acceptance criteria
See US-03 AC1–AC3 in `feature-delta.md`.

## Dependencies
- Slice 01 (pipeline, widget shell, series endpoint).
- Existing WIA-percentile computation (`workItemAgePercentiles` widget backend).

## Effort / reference class
≤0.5 day (pure reuse of slice-01 pipeline + one widget tab). Reference class: slice 01.

## Dogfood moment
On the same real team, toggle WIA the day it ships; confirm the age trend renders alongside the CT tabs.
