# Bug 6656 — named Cycle Time shows its own population

## Root cause

`TeamMetricsService` and `PortfolioMetricsService` build every named Cycle Time answer (scatterplot data,
percentiles, percentiles info, outliers) from the items **closed** in the date range. When the end state of
the measure lies before Done, every closed item has passed it, so every closed item gets a named value: the
same dots with new heights. Items that reached the end state but are not closed never appear, and the dots stay
on the closed date.

## Maintainer decisions (2026-11-24)

- **Population:** a named measure shows every item whose last forward crossing into its end state falls in the
  date range, closed or not. The dot sits on that crossing date.
- **Scope:** team and portfolio.
- **Skips stay counted:** an item that jumps straight to a later state (for example a Cancelled state that is a
  Done state) still counts as having reached the end state, as the default Cycle Time does. Not changed here.
- **Work Item Age chart:** out of scope (a later feature, per the reply to the reporter).
- **Design:** `cycleTimeData` takes an optional `definitionId`, like `cycleTimePercentiles`. Without it nothing
  changes, so the other widgets that read the list are untouched. With it, the answer is the named population,
  and each named value carries `endDate`. The chart fetches it when a named measure is selected.
- **UI (S2), approved sketch:** dots on the end-state date; x-axis label `Date reached <end state>` for a named
  measure; loading = previous dots dimmed under a small spinner (WidgetShell look); failure = WidgetShell
  could-not-load message, never the previous answer as current; dot dialog title
  `<Work Items> that reached <end state> on <date> with <Cycle Time> of N days`. Default measure unchanged.
- **Slices:** S1 backend, S2 scatterplot.
- **Rigor profile:** `thorough` for both slices: crafter, acceptance designer, architect on `opus`; reviewers
  and EXAMINE on `sonnet`.
- **DEVOPS / usage data:** N/A, because this is a bug fix of an existing view; no new feature to count.
- **Context map:** no new concept, context or edge (Flow Metrics only).

## Decisions taken during DELIVER (AFK)

- The named Cycle Time percentiles widget's previous-period comparison and a configured outlier baseline use
  the named population too, so a named measure reads the same in every period.
- The scatterplot's hover text on a dot with several items reads `N <Work Items> that reached <end state>` for
  a named measure (an extension of the approved dialog title; not in the sketch).
- The dot dialog shows the selected measure's Cycle Time column; the percentiles View Data lists the measure's
  own Work Items, and none while they load.
- The percentiles hook and the scatterplot do not share one query: one extra request per named selection.
- A cached named measure the reader returns to is shown undimmed while it refetches (it is that measure's own
  answer, not the previous one).
- **Maintainer decision (09:2x):** with a named measure selected, the percentiles widget's View Data title is
  `<Work Items> that reached <end state>`; the default keeps `Closed <Work Items>`.
