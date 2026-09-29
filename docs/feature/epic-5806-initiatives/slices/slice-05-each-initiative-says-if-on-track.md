# Slice 05 — Each Initiative says whether it is on track for its target date

**Feature**: epic-5806-initiatives · **Epic**: #6119 "Initiative access and on-track status" (moved
from #5806) · **Story**: US-05 · **ADO**: #6117 · **Estimate**: ~7h (≤1 day)

## Goal

The RTE sets a target date on an Initiative's row and immediately reads how likely it is that all its
Features are done by then, as a percentage and a status word.

## IN scope

- Set, change and clear a target date inline on the row; stored by Lighthouse per Initiative reference,
  kept across refreshes and restarts (expand-only migration via `CreateMigration`).
- Likelihood that all remaining Features are done by the target, the same number a Delivery would show.
- Status word next to colour, read from the whole-percent likelihood (decision D16): On track at 70% or
  more (exactly 70 is On track), At risk from 50% to below 70% (exactly 50 is At risk), Off track below
  50% or when the target has passed with Features left, No target, and Unknown when the row cannot
  forecast. Each band includes its lower bound, as Deliveries do; the forecast colour bands elsewhere
  are not changed.
- The status comes from the likelihood alone. It does not mean "the 85% date is on or before the
  target": a row can be On track at 78% while its 85% date is after the target (AC-5.6).
- Write right: System Admin or the Initiative's Admin grant from slice 09 (D21); a Portfolio Admin has
  none from that alone. Backend through `IRbacAdministrationService`, control hidden through
  `useRbac()`; Premium gate on the write.
- The row expander from slice 06 (Epic #5806) gains each Feature's own likelihood by the target,
  labelled "this one alone", and the joint likelihood on its closing line (AC-5.7).
- One added assertion on the walking skeleton (set a date, read a status).

## OUT of scope

- Reading a target date from the tracker. Notes, history or trend of the target.
- Alerts when the status changes (Signals Epic).

## Learning hypothesis

**Disproves, if it fails**: that a single entered date and three bands are enough to answer "is it on
track?" in a way an executive accepts without a caveat speech. If the demo audience asks "on track
compared to what?", the target needs more context than a date.

## Acceptance criteria

AC-5.1 to AC-5.7 in `feature-delta.md`.

## Dependencies

Slices 04 and 06 (Epic #5806), and **slice 09** (the Initiative Admin grant is the write right).
Delivered last in Epic #6119, after slices 07-10. Thresholds (D16), rights (D21, D24) and behaviour
without RBAC (D26) are all settled.

## Effort

~7h: storage and write ~3h, status and control ~3h, E2E ~1h.

## Dogfood / production data note

Set real target dates on two or three dev-instance Initiatives the maintainer knows well. Does the status
match their own gut call? A mismatch is either a finding about the forecast or about the thresholds;
write down which.
