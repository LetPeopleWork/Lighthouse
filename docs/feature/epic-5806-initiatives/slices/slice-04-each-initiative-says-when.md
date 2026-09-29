# Slice 04 — Each Initiative says when all its Features are likely done

**Feature**: epic-5806-initiatives · **Epic**: #5806 "Visualize Initiatives" · **Story**: US-04 · **ADO**: #6116 · **Estimate**: ~5h (≤1 day)

## Goal

Every row on the Initiatives page reads "Likely by {date} (85%)", computed from all its remaining
Features together, or says plainly why it cannot be forecast.

## IN scope

- Per row, the 85% date of "all remaining Features done", with 70% and 95% on hover, from the existing
  Delivery composition (`DeliveryCompletionForecast`), blackout days included.
- "Cannot forecast: {team} has no throughput" when any contributing Feature cannot be forecast.
- "Forecasts updated {time}" from the oldest forecast among the rows.
- An identity test: same Features, same dates as a Delivery.
- One added assertion on the slice 03 walking skeleton.

## OUT of scope

- Target date and status (slice 05). Per-Feature breakdown (slice 06).
- Any new simulation, or a simulation triggered by opening the page.
- A percentile selector.

## Learning hypothesis

**Disproves, if it fails**: that composing stored forecasts is fast and correct enough for a page of
every Initiative. If the request for 50 Initiatives / 300 Features exceeds 1 second, or a row's date
differs from the equivalent Delivery's, the "snappy, no new maths" premise is wrong.

## Acceptance criteria

AC-4.1 to AC-4.6 in `feature-delta.md`.

## Dependencies

Slice 03.

## Effort

~5h.

## Dogfood / production data note

On the dev instance, pick one Initiative, build a rule-based Delivery on its parent by hand, and compare
the two 85% dates. Then time how long it takes to answer "when will this land?" with the page against
the old way; that is the first KPI reading.
