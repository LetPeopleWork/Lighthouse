# What "outlier" means here

The rule has to (1) flag few items, (2) flag the ones that actually stretch the chart, (3) not move when the outlier
itself is in the data, (4) work with no SLE configured, and (5) fit in one sentence of help text.

## How Lighthouse computes percentiles today

`PercentileCalculator.CalculatePercentile`: sort, take index `floor(p/100 x N) - 1`, clamped. For N >= 3 the 95th
percentile is therefore never the maximum, so **one** extreme item does not set the P95 line. It takes about 5% of the
items being extreme (two items at N = 10..20) before the P95 itself rises. The median moves only when half the items
are extreme. The 50/70/85/95 lines and the SLE come from the backend over **all** closed items and must stay that way.

## Candidate definitions

Share flagged = the share of items a rule marks on a *typical* right-skewed cycle time (illustrative lognormal,
sigma = 1, i.e. P95 is about 5.2 x the median; computed by hand, not measured on Lighthouse data. A team with sigma 0.8
flags fewer, one with sigma 1.2 more).

| Rule | One-sentence help text | Share flagged at sigma = 1 | Works with no SLE | Robust to the outlier itself | Verdict |
|---|---|---|---|---|---|
| A. More than k x the **median** (the story's "500% of the median") | "Took more than 5 times the typical (median) cycle time." | k = 5: **5.4%**; k = 10: 1.1% | Yes | Yes (most robust) | k = 5 is just the P95 in disguise: it flags a twentieth of all items on an ordinary team, so a chip would show on almost every chart. k = 10 is rarer but is a magic number that does not scale with the team's own spread. Good as the **description** ("5.6 x the median"), poor as the **cut**. |
| B. More than k x the **95th percentile** | "Took more than twice as long as 95% of closed items." | k = 1.5: 2.0%; **k = 2: 1.0%**; k = 3: 0.3% | Yes | Mostly (one item cannot set P95; a cluster of 5% can) | **Best fit.** Scales with the team's own spread, uses a line already drawn on the chart, one sentence. |
| C. Tukey fences, Q3 + 1.5 x IQR | "Beyond 1.5 x the interquartile range above the third quartile." | **7.8%** | Yes | Yes | Rejected: made for symmetric data; on skewed data it over-flags the upper tail (the sources found say so), flags nearly one in twelve ordinary items, needs quartiles Lighthouse does not show, and the sentence needs a statistics course. The adjusted (medcouple) variant fixes the bias but is worse on explainability. |
| D. A fixed percentile cut (everything above P95) | "The slowest 5% of items." | 5% by definition | Yes | Yes | Rejected: there is always an outlier, so the chip is always on, and it says nothing about *distortion*. |
| E. A multiple of the **SLE** | "More than twice the SLE." | Depends on the SLE (4% at sigma = 1 with an 85% SLE) | **No** | Yes | Rejected as the primary rule: not every Team or Portfolio has an SLE. Fine as a floor for the axis (see below). |
| F. Geometry: "taller than the chart can show without squeezing the lines" | n/a | n/a | Yes | n/a | Not a definition but the *reason* for one; B with k = 2 approximates it. |

## Recommended definition (to confirm with the maintainer)

> **An outlier is a closed Work Item whose Cycle Time is more than twice the 95th percentile of the same chart's items.**

Rules that go with it (assumptions A1-A5 in `feature-delta.md`):

- The cut is `cut = 2 x P95`, never below the SLE line or the highest percentile line, so no drawn reference line is
  ever clipped. The axis then ends at about 1.1 x the larger of `cut` and the highest non-outlier dot.
- Nothing is clipped when no item exceeds the cut. The chip appears only when there is something to disclose.
- Fewer than 10 closed items: the rule is off and the chart behaves as today (a P95 of 4 items means nothing).
- The cut uses the percentile lines for the data being shown: with a named cycle time selected, the named percentiles.
- The cut is computed from the percentile values, not from which lines the reader has toggled visible, so it does not
  move when they untick "95%".
- The popover also reports each outlier as a multiple of the **median** ("5.6 x the median"), which answers the story's
  "how much bigger" without making the median the cut.

Open: k (2 is a judgement; 1.5 and 3 are the neighbours), a minimum size for the axis on very short cycle times
(open question 3), and whether the multiplier should be user-tunable (recommendation: no, YAGNI).

## What the rule must never touch

Percentile lines, the SLE share and RAG, the percentile tiles, the Percentiles over Time widget, the forecast inputs
and the Spearman figure keep including outliers. The rule changes what is *drawn on one axis*, never a number.
