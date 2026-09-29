# Slice 06 — Open an Initiative to see which Feature carries its date

**Feature**: epic-5806-initiatives · **Epic**: #5806 "Visualize Initiatives" · **Story**: US-06 ·
**ADO**: #6118 · **Estimate**: ~4h (≤1 day) · **Severable** · **Order**: 5 of 10

## Goal

When a row's date is later than expected, one click shows which of its Features carries it, without
leaving the page.

**Dates only, no status.** In Epic #5806 no Initiative has a target date: target, likelihood and status
are slice 05, which belongs to Epic #6119 (decision D27). This slice therefore shows no status and no
likelihood. Slice 05 later adds the "this one alone" likelihood column to this expander (AC-5.7).

## IN scope

- A row expander listing the Initiative's readable Features: Teams, state, own 85% date, name linking to
  the Feature page.
- A closing line repeating the row's joint date: "All {n} {features} likely by {date} (85%)".
- Features in Portfolios the reader cannot read are counted, never named (D22).
- Hand-built expandable row (the data grid in use is the MIT tier).

## OUT of scope

- Any status, likelihood or target (Epic #6119, slice 05).
- Editing, re-ordering or cutting Features from here.
- Any date made by summing, averaging or taking the latest of the Feature dates.

## Learning hypothesis

**Disproves, if it fails**: that showing each Feature's own date under a joint row reads as helpful
rather than contradictory. The joint date can be later than every Feature's own date; if readers ask
"why is the total later than every row?", the "All … likely by" label is not carrying the meaning here.

## Acceptance criteria

AC-6.1 to AC-6.5 in `feature-delta.md`.

## Dependencies

Slice 04.

## Effort

~4h.

## Dogfood / production data note

Expand "Deep Space Readiness" on the demo (spans Orion and Altobelli) and one real multi-team
Initiative on the dev instance. Can you name the Feature holding the date within five seconds?
