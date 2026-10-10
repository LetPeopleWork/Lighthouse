# Slice 02: Every chart's dialog brings the columns that explain its point

**Goal:** each chart click and *View Data* context opens with its locked defaults (D4), so the table explains why
its items are there.

## IN scope
- Defaults-map entries for every chart and *View Data* context in the D4 table except a Feature's child items,
  the Delivery timeline and Cumulative State Time.
- Feature Size (dot and *View Data*) sorts by Size.
- Existing judgement cells (SLE colouring, Age band, SLE risk, Time in State) keep their look.
- Cumulative State Time: Days Contributed only, no catalogue.

## OUT of scope
- A Feature's child items and the Delivery timeline (slice 03).

## Learning hypothesis
- **Disproves** "the locked table is right" if the dogfood shows a context whose defaults hide what the chart
  point is about; adjust the map entry, as D4 expects.
- **Confirms** it if each dialog answers "why is this item here?" without opening *Manage columns*.

## Acceptance criteria
AC-2.1 to AC-2.5 in `../feature-delta.md` (US-02).

## Dependencies
Slice 01 (catalogue, defaults map, per-context layout).

## Effort
About a day: mostly map entries and moving each call site from `highlightColumn` to a context key.

## Dogfood
Dev instance: Blocked over time bar shows Blocked since; Feature Size dot sorts by Size; Work Distribution slice
shows Parent.
