# Recommendation — story-6535-arrivals-in-reports (DIVERGE)

Inputs: `job-analysis.md`, `competitive-research.md`, `options-raw.md`, `taste-evaluation.md`.

## Decision statement for DISCUSS

Build **one new Then & Now metric, "Arrivals", whose panel sets arrivals per day against {Throughput} per day
and compares the gap between them, Then against Now, with a lower gap counted as better** (row B-lower, 4.50).
DISCUSS confirms or overturns the direction of good (B-zero, 4.00) and settles the open items below.

## Recommended: Arrivals vs Throughput panel (row B-lower, 4.50)

Ticking **Arrivals** in the Then & Now wizard adds one panel. For Then and for Now it shows:

- Arrivals: total (context) and **per day**;
- {Throughput}: **per day**, repeated so the panel reads alone;
- **Gap per day** = arrivals per day − {Throughput} per day, worded with its sign ("0.8 more started than
  finished per day" / "0.3 fewer started than finished per day").

The change mark sits on the gap. Per day, because Then and Now can have different lengths (the same reason the
Throughput panel compares per day). An arrival is a {Work Item} **started**, as on the Metrics tab
(`docs/metrics/flow-metrics.md:260`); no new definition.

Why: it answers the requester's own sentence ("compare arrivals vs throughput; arrivals bigger is a bad sign") and
the maintainer's point (Then against Now of that balance) in one place, at the cost of one checkbox and one panel
on the existing seam (`reportPanelRegistry.ts`, a `WindowedReportMetric` beside `ThroughputReportMetric.cs`).
Research found no product that shows this balance as a before/after comparison; tools show one window, as a CFD
slope, created-vs-resolved bars or a ratio (`competitive-research.md`).

## Dissent: Arrivals panel alone (row A, 4.30)

Arrivals total and per day with a neutral change, the balance left to the reader. Wins if DISCUSS decides no value
may repeat across panels. It is a strict subset of row B, so it can ship first and grow without rework.

## Decisions for DISCUSS

1. **Direction of good for the gap**: B-lower "lower is better" (requester: arrivals above Throughput is bad; Jira
   colours created > resolved red, as the research found) or B-zero "closer to zero is better" (4.00) (maintainer: arrivals
   should match Throughput). B-zero needs a third direction kind and reads a slowing drain as better.
2. **Gap or ratio** as the balance number. Ratio (arrivals ÷ Throughput, one product shows it) is scale-free
   but undefined when nothing finished, and hides volume. Per-day gap is already window-length-independent.
3. **Max and median per day**: recommended out. Daily arrivals are lumpy, the median is often 0, and the other
   panels compare averages.
4. **Portfolio**: arrivals exist for Portfolios (Features started), so the panel applies to both owner kinds.
5. **Existing reports** show the panel as "not captured" for Then, as for any metric added later.
