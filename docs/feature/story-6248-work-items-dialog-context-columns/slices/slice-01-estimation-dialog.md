# Slice 01: The Estimation dialog shows each item's estimate

**Goal:** from Estimation vs. Cycle Time, the Work Items dialog shows Estimate, Cycle Time and Closed per row,
with the rest of the catalogue one click away, and its layout remembered for that context alone.

## IN scope
- The backend adds the normalised estimate (display value) to `WorkItemDto` and `FeatureDto`, normalised by the
  same code the chart uses; absent where estimation is not configured (D2, D6).
- `IWorkItem` gains the optional estimate; the Feature zod schema keeps it.
- The dialog's column catalogue (D1, D2) and the one declared defaults map (D4), with the Estimation context as
  its first entry and every other context mapped to "today's columns".
- Hidden-by-default columns in `DataGridBase`, reachable through *Manage columns*.
- A per-context layout key (D3); *Reset layout* returns to the context's defaults.

## OUT of scope
- Every other context's new defaults (slices 02, 03). They look exactly as today (AC-1.6).
- Clients (D5), tags.

## Learning hypothesis
- **Disproves** "one defaults map can drive every context" if the Estimation context needs logic the map can't
  express (e.g. a value only the chart knows), forcing per-call-site code again.
- **Confirms** it if the Estimation context is one map entry plus the generic catalogue.

## Acceptance criteria
AC-1.1 to AC-1.6 in `../feature-delta.md` (US-01). Value on its own: Steve's request, answered.

## Dependencies
None.

## Effort
About a day. Reference class: story 5884 (age band column added to the same dialog).

## Dogfood
Dev instance (`:5169`, real history) with an estimation field set on a Team: click a bubble, see Estimate per
row; hide Closed, open the Cycle Time scatter dialog, and Closed (when that context gains it) is unaffected.
