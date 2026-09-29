# Slice 01 — The demo Portfolios carry named Initiatives of different kinds

**Feature**: epic-5806-initiatives · **Epic**: #5806 "Visualize Initiatives" · **Story**: US-01 · **ADO**: #6113 · **Estimate**: ~5h (≤1 day)

## Goal

Someone opening a demo Portfolio sees, in the existing Feature table column, which Initiative each
Feature belongs to, by name, with different kinds of Initiative in different Portfolios. A CSV user who
lists Initiatives as rows in the file gets the same.

## IN scope

- CSV connector: a row whose ID another row names as its parent, and whose type is not one of the
  Portfolio's work item types, resolves as that parent (name, URL, type). It stays out of the
  Portfolio's Features, which already happens today for non-Feature types.
- Demo CSVs gain parent rows and fill the Parent column. Proposed set (maintainer may rename):

| Portfolio | Type | Initiative | Features |
|---|---|---|---|
| Ocean Explorer | Initiative | Healthy Oceans 2027 | OE-002, 003, 005, 007, 012 |
| Ocean Explorer | Initiative | Blue Planet Knowledge | OE-001, 004, 008, 009 |
| Apollo | Objective | Return to the Moon | AP-001, 002, 003 (all Done) |
| Apollo | Objective | Humans on Mars | AP-004, 005, 006 |
| Orion + Altobelli | Theme | Deep Space Readiness | ORI-104, 105, ALT-104, 105 (spans) |
| Orion | Theme | Crew Safety | ORI-112, 118, 120 |
| Altobelli | Theme | Mars Surface Operations | ALT-115, 119 |
| NeuroLink City | Goal | Carbon-Neutral City Services | NL-006, 007, 011 |
| NeuroLink City | Goal | Safer Streets | NL-001, 003, 010 |

  OE-006, 010, 011, 013 and the rest keep no Initiative, so the "no Initiative" count is non-zero.
- If a demo Team already has no throughput, one Initiative includes a Feature of it, so slice 04 can
  show "cannot forecast" on the demo. If none exists, record that; do not invent one here.
- Docs: `docs/concepts/worktrackingsystems/csv.md` explains parent rows.

## OUT of scope

- Any new page, term or column change (slices 02-03).
- Renaming, re-typing, re-ordering or re-stating any existing demo Feature.
- Parent rows for other connectors (they already resolve names).

## Learning hypothesis

**Disproves, if it fails**: that parents can be shown by name through the existing refresh path with a
connector-local change, and that demo data can carry the level without disturbing what the E2E suite
already pins.

## Acceptance criteria

AC-1.1 to AC-1.7 in `feature-delta.md`.

## Dependencies

None.

## Effort

~5h: connector ~2h with tests, CSVs ~1h, full Playwright run and fixes ~2h.

## Dogfood / production data note

Load the demo, open each of the five Portfolios, and read the column aloud: does every name look like
a strategic item a sponsor would recognise, and is none of them confusable with a Feature name? Demo
data changes have broken unrelated E2E through substring locators and moved windows before
(`docs/ci-learnings.md` 2026-07-11, 2026-06-14, 2026-08-21). Run the whole suite, not the touched spec.
