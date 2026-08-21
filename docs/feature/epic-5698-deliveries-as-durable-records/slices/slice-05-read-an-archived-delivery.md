# Slice 05 — Read an archived Delivery as the record it was

**Epic** #5698 · **Story** US-05 · **ADO** #5640 · **Estimate** ≤1 day

## Goal
An archived Delivery shows exactly what it looked like at closure, no matter what happened to its
Features since.

## IN scope
- The archived Delivery's Feature grid rendered from the pinned closure record, never from live
  Feature data.
- An archived marker with the archive date on the section.
- Export (Slice 01) working unchanged on an archived Delivery, producing the pinned numbers.
- Notes read-only on an archived Delivery: listed, but add / edit / delete refused by the API with a
  reason the UI can show.
- `POST /api/latest/deliveries/{deliveryId}/unarchive` — back to the active list, live recomputation
  and daily recording resume.
- Editing an archived Delivery's name, date, Features or rules refused.

## OUT of scope
- Actual finish date, calibration, "we said X it landed Y" (D2).
- Reusing an archived Delivery's data in another Delivery's forecast.
- Bulk archive or bulk export.

## Learning hypothesis
**Disproves if it fails**: the Epic's entire premise — that a Delivery can be a durable record at all.
The single assertion that carries it is AC-05.3: archive, then refresh the Portfolio in a way that
changes the underlying Features, then read again and get identical numbers. If that fails, the read
path is still touching live Features somewhere, and every other acceptance criterion in the Epic is
decoration.
**Confirms if it succeeds**: retiring a Delivery preserves evidence rather than destroying it, and the
later calibration Epic has a record to calibrate against.

## Acceptance criteria
AC-05.1 … AC-05.8 in `../feature-delta.md`.

## Dependencies
Slices 01, 02, 04.

## Reference class
`DeliveryMetricsHistoryDto.From` — the existing read-back of `FeatureBreakdownJson` into per-Feature
rows. The archived grid is the same deserialisation at a single point in time.

## Dogfood moment
Open the Delivery archived in Slice 04 after a full Portfolio refresh and confirm nothing moved.
