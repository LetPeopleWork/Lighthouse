# Slice 03: A Feature's child items show their progress at a glance

**Goal:** the child-items dialog (Delivery section, Team and Portfolio feature lists) shows Started, Closed,
Age / Cycle Time and Estimate; the Delivery timeline keeps Warnings and gains the catalogue.

## IN scope
- Defaults-map entries for the child-items context and the Delivery timeline context (D4).
- Estimate on child work items wherever the owning Team or Portfolio has estimation configured (D6).

## OUT of scope
- The feature list grids themselves; only the dialog they open.

## Learning hypothesis
- **Disproves** "child items carry enough to explain progress" if the delivery lead still opens the tracker to
  see what is moving.
- **Confirms** it if the not-started large item is visible from the dialog alone.

## Acceptance criteria
AC-3.1 to AC-3.3 in `../feature-delta.md` (US-03).

## Dependencies
Slice 01.

## Effort
Half a day to a day.

## Dogfood
Demo data, Ocean Explorer Portfolio, Deliveries: a Feature's progress opens its child items with dates and age.
