# Slice 02 — The level above Feature carries our own word

**Feature**: epic-5806-initiatives · **Epic**: #5806 "Visualize Initiatives" · **Story**: US-02 · **ADO**: #6114 · **Estimate**: ~3h (≤1 day)

## Goal

The Feature tables name the level above Feature with a configurable term, seeded "Initiative", so
every later surface of this Epic is born with the organisation's word instead of a hardcoded one.

## IN scope

- New Terminology entries, singular and plural, seeded "Initiative" / "Initiatives", added on upgrade
  without touching other terms.
- The Feature table column on Portfolio and Team pages heads with the singular term; empty cells read
  "No {initiative}".
- Docs: `docs/settings/configuration.md` Terminology list; `docs/portfolios/detail.md` column name.

## OUT of scope

- The Team work-distribution chart's "No Parent", which names a work item's Feature (decision D15).
- Any new page (slice 03).
- Sorting or filtering the column.

## Learning hypothesis

**Disproves, if it fails**: that one seeded word, renamable, is enough for organisations that mix
parent types across Portfolios. If a demo audience says "but this Portfolio calls them Objectives",
the term would have to become per-Portfolio, which is a different design. Record the reaction.

## Acceptance criteria

AC-2.1 to AC-2.5 in `feature-delta.md`.

## Dependencies

None. Slice 01 makes the result visible on demo data but is not needed to build it.

## Effort

~3h.

## Dogfood / production data note

On the dev instance (real history, no names in the demo sense), rename the term to "Objective", reload a
Portfolio and a Team page, and check no screen still says "Parent" for this relationship.
