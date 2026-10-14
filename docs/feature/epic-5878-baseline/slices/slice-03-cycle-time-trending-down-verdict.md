# Slice 03 — The report's summary header

**Feature**: epic-5878-baseline · **Epic**: #5878 · **Story**: US-03 (ADO #6161) · **Estimate**: ~½d (was ~1d) · **Tier**: Community ·
`job_id: job-flow-coach-show-whether-flow-changed`

> Revised 2026-10-14 (D49, D52; maintainer, after the DISTILL sketch review): the beyond-limits count this slice
> carried is dropped on every panel (D49), and the slice becomes the report's summary header (D52, recorded first as
> DT14), plus the behaviour-preserving refactor already done (XmR limits and the cycle-time selection extracted). The
> previous scope is kept below, marked superseded.
>
> Re-cut 2026-10-03: was "'{Cycle Time} is trending down': the first verdict". The report gives no verdicts any more
> (D39); this slice then added the count of Now's points beyond Then's frozen limits (D40). The file name keeps its
> old slug so existing links still resolve.

## Goal

A reader sees, before any panel, how long each side's window is and how many {Work Items} closed in it, so a
difference that comes from windows of different length is not read as a change in flow.

## IN

- A summary header between the one-line breadcrumb and the first panel, two segments matching the panels' sides:
  THEN and NOW.
- Each side: its window as "start – end · N days" (calendar days, inclusive at both ends, instance time zone) and
  "N {Work Items} closed" ({Features} on a Portfolio, once slice 06 lands). Then's count never moves after creation;
  Now's is today's. A count of 0 shows as 0.
- Panel side headings read just THEN and NOW — no "· N {Work Items}", no "· N days".
- No "frozen on" or "always the last N days" line. Anything more in the header waits for a sketch the maintainer has
  seen (D52).
- The header follows the report's loading and failure looks (DT6).
- Already done, behaviour-preserving: XmR limits and the cycle-time selection extracted.

## OUT

Any count beyond Then's limits, on any panel (D49); "days since Then ended" (slice 07 — whether it joins this header
is open, sketch first); other panels (04, 05); the Now length choice (07).

## Learning hypothesis

**This disproves "the window lengths explain the counts"** if a dogfood reader of a report whose Then and Now differ
in length still asks why one side has so few {Work Items}. Then the header needs more than window and count —
sketched for the maintainer first.

## Data and dogfood moment

- Dogfood: the live report that prompted DT14 (a 90-day Then and a short Now, both ending 13 Oct) and the 31 Jul
  report from slice 02; show both to a reader who did not create them.

## Acceptance criteria

AC-3.1 … AC-3.6 in `feature-delta.md` (US-03).

## Dependencies

Slices 01, 02.

## Superseded 2026-10-14 by D49 — {Cycle Time}: Now's {Work Items} beyond Then's frozen limits

*Kept for history; not applied.*

### Goal (was)

The {Cycle Time} panel counts how many of Now's finished {Work Items} fall beyond the limits frozen from Then, split
above and below and coloured by {Cycle Time}'s direction of good (down). No verdict; the reader interprets.

### IN (was)

- Then's XmR average and limits for {Cycle Time}, frozen **unrounded** at creation (`XmRResult` rounds, S3); never the
  owner's PBC Baseline (S4), never Now's own data.
- Count of Now's {Work Items} beyond those limits, by side: "Work Items beyond Then's limits: 2 above · 0 below". Only
  points beyond the limits count, not runs or other special-cause rules (D40).
- Colour by direction of good, known only to the frontend panel: above red, below green, 0 neutral; the number is
  always shown.
- A lower limit at 0: below shows "—" with "Then's lower limit is 0" (Q8, confirmed 2026-10-03).
- Too few Then points for a process-behaviour chart: "—" with the reason the Metrics page gives (D41).
- Reports created before this slice show the count as "Not captured for this report" (D27).
- Demo data: check one demo Team gives a non-zero count; adjust Team Lightspeed's CSV only if none does (Checklist).

### OUT (was)

Other panels (04, 05), any chart (dropped with slice 11), verdict words of any kind (D39), the Now length (07).

### Learning hypothesis (was)

**This disproves "a count against frozen limits says something on real Teams"** if, on the dev instance, every Team
reads 0 above and "—" below over the 63 days since 31 Jul 2026. Then the count is mute on per-item data, and its
shape must change before panels 04 and 05 copy it.

### Data and dogfood moment (was)

- Dogfood: the 31 Jul report from slice 02 on every dev-instance Team; tally the counts and check two {Work Items}
  above the limit against the Metrics tab's {Cycle Time} PBC for the same dates.
