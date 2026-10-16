# Slice 05b — Story 6304 — A field a default points at cannot be deleted; an older client's round trip changes nothing

**ADO**: Story #6304 (Epic #6303) · **User story**: US-04 (second half) · **Estimate**: ~0.5 day · **Depends on**: slice 05a
**Release point**: no — #6304 releases after slice 06. Alone, this slice leaves `main` releasable (L8).
**Locked decisions**: D9, D17, D18 (amended by G-R1 (b), maintainer 2026-10-16), D22

## Goal
Deleting an Additional Field that a connection default or any Team or Portfolio points at is refused, naming who
points at it. A script that GETs a Team's settings and PUTs them back unchanged — including one that drops
properties it does not know — leaves every Team's mode and effective Parent Override Field as they were.

## Why
After 05a, two things can quietly undo a default. Deleting the field it names leaves followers reading nothing
with no warning (the upgrade already repaired values that pointed at nothing, 05a); and an older client that never
heard of the mode PUTs back an empty Parent Override, which under
pass 2's D18 (absent mode = *Own*) would have turned a following Team into a deliberate None (R1). The maintainer
chose G-R1 (b) instead. Both must be closed before #6313 adds four more settings that share the same contract.

## IN
- **D17 refusal**: removing an Additional Field referenced by a default or by any owner (any of C1–C5, own or
  default) is refused with a message naming the default(s) and the owners that use it as their own; nothing is
  saved (UI-6). Replaces the interim behaviour 05a left (P5).
- **G-R1 (b) on the Team PUT (AC-4.9)**: a PUT without the mode keeps the stored mode, unless the payload sends a
  Parent Override value that is **present and non-empty** and differs from the effective one — then the Team
  becomes *Own* with that value. No mode with an empty or absent value changes nothing.
- **Round-trip test (AC-4.10)** in two payload shapes: one that echoes every property, one that drops unknown
  properties. Neither changes any Team's mode or effective value; both must pass.
- CLI check (Lighthouse-Clients checklist, R1): `lh team update` with the payload `lh team get` returned changes
  nothing, in both shapes.

## OUT
- Portfolio PUT: lands with the first Portfolio setting (Story #6313, slice 08), reusing this slice's rule.
- Immediate update on a default change (slice 06).

## Learning hypothesis
Disproves "an older client's round trip is harmless" if any shape of GET-then-PUT changes a mode or effective value
under G-R1 (b).

## Production data / dogfood
Dev instance: script the GET-then-PUT across every Team on the Jira connection with a default set; compare modes
and effective values before and after. Try to delete the default's field; read the refusal.

## UI
Refusal copy approved by the maintainer (UI-6): "✕ "Caused by" cannot be removed. It is the Teams' Parent Override
Field default, and Ledger uses it as its own. Nothing was saved."

## Acceptance criteria
AC-4.8 – AC-4.10.

## Changed assumptions
- 2026-10-16 (DESIGN, maintainer UI-6): the refusal names the default and the owners that use the field as their own,
  not the default's followers.
