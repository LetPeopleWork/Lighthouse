# Options (raw) — epic-5878-baseline (DIVERGE, Phase 3)

Generation only. No option is scored or ranked in this file; evaluation is in `taste-evaluation.md`.

## Fixed frame (not re-opened by any option)

Maintainer decisions D8-D16 (`../wave-decisions.md`): Teams and Portfolios, flow metrics; many reports per
owner; values frozen at creation with the settings in force then; current side a selectable rolling window;
baseline window picked at creation and allowed to end in the past (inside `DoneItemsCutoffDays`); Reports tab →
Create Report → Template; live in-app first; editors create, readers view; **the user selects comparison items
from a list, each item has a rule that judges it good or not, the v1 list is limited and the catalog must make
new items and rules easy to add (D16).**

The options therefore vary on what is left open:

- **(a) catalog grain** — what one selectable item is (a metric, a claim, a metric + parameters);
- **(b) rule semantics** — what "trending down", "stable" and "no signals" mean;
- **(c) where rules live** — code registry, data rows, per-report settings;
- **(d) presentation** — table, claims list, charts;
- **(e) metric depth** of the v1 catalog.

The Community/Premium line (item 9) and the template name (item 10) are generated as separate pools at the end,
because either can sit on any direction.

## HMW

> How might we let someone who changed how a Team or Portfolio works say, from a "before" that cannot move and a
> definition of "better" agreed before looking, whether flow changed?

No solution embedded (no "report", "tab", "chart").

## SCAMPER

### S — Substitute: "Rule rows"

**Core idea**: The catalog is data. Each item is a stored row: metric source + rule kind + parameters
(threshold, direction, detection rules), seeded with defaults; an editor can tune a row's parameters per report.
**Key mechanism**: Generic rule kinds (threshold, shift, no-signal) parameterised by stored rows; adding an
item for an existing source and rule kind needs no code.
**Key assumption**: Coaches want to tune what "better" means per client.
**Closest competitor**: Nave (user-defined thresholds).

### C — Combine: "Judged charts"

**Core idea**: Each selected item renders as the existing process behaviour chart spanning before and now, with
the baseline's frozen limits extended across the current window and the rule's verdict as a badge on the chart.
**Key mechanism**: Reuse of the Metrics tab PBC widget with limits taken from the frozen baseline instead of the
current window; the rule reads the same classifications the chart draws.
**Key assumption**: Sponsors read a chart if the verdict sits on it.
**Closest competitor**: ActionableAgile Process Behavior Chart; Wheeler practice.

### A — Adapt: "Threshold scorecard"

**Core idea**: A table of selected metrics: Before | Now | change | % change, coloured by a fixed per-item
percentage threshold in the item's direction of good (e.g. 10%).
**Key mechanism**: Arithmetic delta against a code-defined threshold per catalog item.
**Key assumption**: Sponsors trust percentages; a fixed threshold is accepted as the meaning of "better".
**Closest competitor**: Nave Executive Report; Google Analytics period compare.

### M — Magnify: "Signal catalog"

**Core idea**: Each catalog item is a metric with one rule from a closed set of PBC rule kinds, judged against
XmR limits computed on the baseline window and frozen at creation. A table shows the verdict per metric, with
Before/Now values beside it.
**Key mechanism**: Code registry of item definitions (source series, direction of good, rule kind); new data
points in the current window classified against frozen limits with the existing special-cause classes.
**Key assumption**: Sponsors accept "no change detected" as an answer when the numbers differ.
**Closest competitor**: none found doing this on a frozen before (I1); method from Wheeler.

### P — Put to other use: "Frozen Metrics page"

**Core idea**: The report is the Metrics tab rendered twice — the baseline window frozen, the current window
live — for the widgets matching the selected items, with each widget's rule verdict in its header.
**Key mechanism**: The existing widget payloads stored at creation and replayed; the selection list is the
widget list.
**Key assumption**: Users want the widgets they already know rather than a new view.
**Closest competitor**: ValueFlow dashboard export.

### E — Eliminate: "Claims checklist"

**Core idea**: The selectable unit is a plain-language claim ("{Cycle Time} is trending down", "{WIP} is
stable or down", "{Throughput} is predictable"). The report is a list of the chosen claims, each marked Holds /
Does not hold / No change yet / Not enough data, with Before → Now values on the row; charts on expand.
**Key mechanism**: Catalog grain is the claim (metric + direction of good + rule fused into one registered
definition); one metric may appear in several claims. Rules are PBC-based where a series exists and a stated
threshold where it does not.
**Key assumption**: Sponsors read sentences faster than metric tables; coaches agree claims up front.
**Closest competitor**: none found; nearest is a consultant's slide of bullet claims.

### R — Reverse: "Expectation first"

**Core idea**: At creation the user picks items **and** states the expected direction for each (down, up,
hold); the report judges each item against the stated expectation, not a catalog default.
**Key mechanism**: Per-report direction of good stored with the frozen values; the rule kind stays fixed per item.
**Key assumption**: Direction of good depends on context (e.g. Throughput expected to drop while WIP is cut).
**Closest competitor**: experiment trackers (hypothesis → result); none in flow tools.

## Crazy 8s supplements

### C8-1: "Belt and braces"

**Core idea**: Every item shows two verdicts: the percentage change against a threshold, and the PBC signal; an
item counts as good only when both agree.
**Key mechanism**: Two rule kinds evaluated per item, combined with AND.
**Key assumption**: Sponsors want the familiar number and the statistical guard together.
**Closest competitor**: none.

### C8-2: "Checkpoint timeline"

**Core idea**: A report holds a sequence of frozen checkpoints (engagement start, hand-over, 3-month check) and
the live current window; the selected items are judged checkpoint to checkpoint.
**Key mechanism**: N frozen snapshots per report on a time axis.
**Key assumption**: Engagements are reviewed several times against the same start.
**Closest competitor**: BaselineX (multiple baselines per project).

### C8-3: "Forecast lens" (Team only)

**Core idea**: A catalog item comparing the Monte Carlo "how many Work Items in the next 30 days at 85%" computed
from the baseline Throughput against the same computed from current Throughput.
**Key mechanism**: Existing `HowMany` forecast run on two Throughput histories.
**Key assumption**: Sponsors think in "how much can we deliver", not in Throughput.
**Closest competitor**: Nave/ActionableAgile Monte Carlo (single window only).

## Curation to 6

| Raw | Outcome | Why |
|---|---|---|
| S Rule rows | **Kept → Option 4** | Distinct seam: catalog as data, parameters per report. |
| C Judged charts | **Kept → Option 5** (absorbs P) | Chart-led presentation. P is the same mechanism (existing widgets, frozen limits/payloads) with a wider payload; C is the representative. |
| A Threshold scorecard | **Kept → Option 1** | Percentage-threshold rule semantics. |
| M Signal catalog | **Kept → Option 2** (absorbs C8-1) | PBC rule semantics on a metric-grain catalog. C8-1 is M with a second rule kind ANDed in; recorded as a variant. |
| P Frozen Metrics page | Merged into Option 5 | See C. |
| E Claims checklist | **Kept → Option 3** | Claim grain: different unit of selection and presentation. |
| R Expectation first | **Kept → Option 6** | User-set direction of good. |
| C8-1 Belt and braces | Merged into Option 2 | Variant of M. |
| C8-2 Checkpoint timeline | Removed from the direction pool | Re-opens D11 (current side is a rolling window; several reports per owner already cover several checkpoints, D9). Kept as a note for DISCUSS. |
| C8-3 Forecast lens | Moved to the **catalog item pool** | It is an item, not a direction; any option can carry it. |

### Curated 6

| # | Option | (a) grain | (b) rule semantics | (c) rules live in | (d) presentation | (e) v1 depth |
|---|---|---|---|---|---|---|
| 1 | Threshold scorecard | metric | % change vs fixed threshold | code registry | table + colour | ValueFlow-wide (~10 metrics) |
| 2 | Signal catalog | metric | frozen-limit PBC rule kinds | code registry | verdict table | 4 PBC series |
| 3 | Claims checklist | claim (metric + direction + rule) | PBC rule kinds; threshold only where no series | code registry | claim list, values inline, chart on expand | 6 claims |
| 4 | Rule rows | metric + parameters | generic kinds, tunable parameters | stored rows, per-report overrides | table | user-assembled |
| 5 | Judged charts | metric | PBC rule kinds, read off the chart | code registry | one chart per item | 4 PBC series |
| 6 | Expectation first | metric + user direction | PBC rule kinds against stated direction | code registry + per-report direction | verdict table | 4-6 |

### Diversity test

| Pair at risk | Mechanism differs? | Assumption differs? | Cost profile differs? |
|---|---|---|---|
| 1 vs 2 | Arithmetic threshold vs limits classification | Sponsors trust % vs accept "no change detected" | Lowest vs medium (new: classify against foreign limits) |
| 2 vs 3 | Metric-grain table vs claim-grain list (one metric, several claims) | Sponsors read tables vs read sentences; claims agreed up front | Similar backend; 3 adds a claim text per definition with Terminology tokens |
| 2 vs 5 | Verdict computed and stated vs verdict on a chart the reader inspects | Reader needs the answer vs reader reads charts | 5 renders frozen series per item (larger payload, chart work) |
| 2 vs 6 | Direction from catalog vs from user per report | Good is universal vs context-dependent | 6 adds a creation step and stored direction |
| 4 vs all | Rules as data with parameters vs code | Coaches tune vs accept defaults | Highest: rule engine over stored parameters, editor UI |

Each pair differs on all three. **G3 gate**: 6 curated options, SCAMPER coverage 7/7 plus 3 Crazy 8s, diversity
test documented, no evaluation in the option text.

## Pool — v1 catalog item candidates (for every option)

| Key | Item | Source | Owner |
|---|---|---|---|
| K1 | Cycle Time (per finished Work Item series; 50/70/85th shown) | Cycle Time PBC | Team, Portfolio |
| K2 | 85th percentile Cycle Time (ValueFlow "SLE 85th") | percentile over window | Team, Portfolio |
| K3 | Throughput (daily series; weekly median shown) | Throughput PBC | Team, Portfolio |
| K4 | WIP (daily series; average and range shown) | WIP PBC | Team, Portfolio |
| K5 | Total Work Item Age (daily series; average daily total + average Work Item Age shown) | Total Work Item Age PBC | Team, Portfolio |
| K6 | Predictability of Cycle Time / Throughput (no signals on the current window's own limits) | PBC | Team, Portfolio |
| K7 | SLE breaches (count of finished Work Items over the SLE) | Cycle Time vs SLE | owners with an SLE |
| K8 | WIP streaks (days above/below average) | WIP series | Team, Portfolio |
| K9 | Arrivals vs Throughput balance | arrivals PBC | Team, Portfolio |
| K10 | Feature size | Feature size PBC | Portfolio |
| K11 | Forecast lens (C8-3) | `HowMany` | Team |

## Pool — Community/Premium line (item 9)

| # | Lever |
|---|---|
| P1 | Per-owner report cap on Community (e.g. 1 or 2 per Team/Portfolio); Premium unlimited |
| P2 | Per-instance report cap on Community (e.g. 3 in total) |
| P3 | Rule gate: percentage rules Community, PBC rules Premium |
| P4 | Export gate: live in-app Community; PDF / scheduled email Premium when built |
| P5 | Template gate: Before/after Community; later templates decided one by one |
| P6 | Retention gate: Community reports expire after N days |
| P7 | Item gate: core catalog Community; extended items (K7-K11) Premium |

## Pool — template names (item 10)

| # | Name | Window labels it implies |
|---|---|---|
| N1 | Baseline Comparison | Baseline / Current |
| N2 | Before & After | Before / Now |
| N3 | Then & Now | Then / Now |
| N4 | Flow Check | Starting point / Now |
| N5 | Starting Point Comparison | Starting point / Now |
| N6 | Change Review | Before / After |
