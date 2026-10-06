# CLI sketches — every `lh` command, `--pretty`

Story #6218 *CLI: every --pretty command reads like the web, not a facts dump*. One page, every command,
for the maintainer to review **before DELIVER** (per `CLAUDE.md`, "Sketch any UI before building it, and
ask"). Written 2026-10-06 in DISCUSS, maintainer AFK. Nothing here is built yet.

## How to read this page

- **Data** is the seeded demo data (`Factories/DemoData/*.csv`): Teams Equinox, Lightspeed, Gravity,
  Meridian, Pulsar, Voyager, Zenith; Portfolios Apollo, Ocean Explorer, Orion, NeuroLink City,
  Altobelli. "Today" is **Tue 6 Oct 2026**, the date the refinement precedent was sketched on. Numbers
  are plausible, not read from an instance.
- **Words** are the seeded Terminology defaults (`Work Item`, `Feature`, `Team`, `Portfolio`, `Cycle
  Time`, `Throughput`, `WIP`, `Blocked`, `Service Level Expectation`, `Delivery`, `Work Tracking
  System`, `Work Item Age`). Every one of them is replaced by the instance's own word when it has been
  renamed; a blank value falls back to the seeded word, as the web and `lh refinement get` already do.
- **Mirrors** names the web view and file (under `Lighthouse.Frontend/src/`) whose words and columns the
  sketch copies.
- **Reuses** names the CLI control style already shipped in `lh refinement` that the sketch reuses, so no
  second table or sentence style appears:
  - **R-TABLE** — `toTableLines` in `cli/src/refinementOutput.ts`: a header row, columns padded to the
    widest cell, two spaces between columns, trailing blanks trimmed. No box-drawing borders.
  - **R-HEAD** — the refinement heading: `<name> · <fact> · <fact>` on one line, then the answer
    sentence on the next.
  - **R-LINE** — the refinement "enough for" divider, `── <what it says> ──`, placed between table rows.
  - **R-REC** — the vote confirmation, `Recorded: <what> on <where>. <where it stands>.` One line,
    past tense, names the thing and its id.
  - **R-LABEL** — the existing `Label: value` lines of `lh connection status` and `lh config`.
- **CHOSEN WORDING** marks a command where the web has no equivalent view or sentence and the wording on
  this page was chosen in DISCUSS. Those are the ones to read most carefully.
- **Ids** are `[id: n]` everywhere, as the generic view already prints them.
- **Dates** are `Fri 30 Oct 2026` (weekday, day, short month, year), the refinement format plus the
  year because forecasts cross years. A calendar day on the wire is never shifted by the reader's time
  zone (the refinement rule). A timestamp (`Last Updated`) is shown in the reader's local time as
  `Tue 6 Oct 2026, 07:14`.
- **`--json` and `--toon` are untouched** by every sketch on this page: byte-for-byte the facts they
  return today, and no extra calls to Lighthouse.
- **Errors are untouched**: `category: reason` on stderr, exit 1. A refusal Lighthouse words itself is
  shown as Lighthouse words it.
- **A shape the renderer does not recognise** (an older or newer Lighthouse) falls back to today's
  generic view, exit 0 — never a crash, never `undefined` in a cell.

---

## 1. `forecast` — slice 01

### `lh forecast manual --team-id 3 --remaining 25 --target-date 2026-10-30`

```text
Gravity · 25 Work Items · target Fri 30 Oct 2026

When will 25 Work Items be done?
Chance  Level      Date
95%     Certain    Fri 13 Nov 2026
85%     Confident  Mon 9 Nov 2026
70%     Realistic  Wed 4 Nov 2026
50%     Risky      Fri 30 Oct 2026

How Many Work Items will you get done till Fri 30 Oct 2026?
Chance  Level      Work Items
95%     Certain    16
85%     Confident  19
70%     Realistic  22
50%     Risky      25

Likelihood to close 25 Work Items by Fri 30 Oct 2026: 48.20%
```

- **Mirrors** `pages/Teams/Detail/ManualForecaster.tsx` (both titles, verbatim, with the Work Items
  term), `components/Common/Forecasts/ForecastInfoList.tsx` (highest chance first),
  `ForecastInfo.tsx` / `ForecastLevel.ts` (the level the web shows as an icon and tooltip — `Certain`,
  `Confident`, `Realistic`, `Risky` at the 50/70/85 thresholds) and `ForecastLikelihood.tsx` (the
  closing sentence, `fixed2` precision).
- **Reuses** R-HEAD, R-TABLE.
- **Deviation from the web, deliberate**: the web's likelihood sentence hard-codes "Items"
  (`Likelihood to close {n} Items by …`); the CLI uses the Work Items term, per the style guide.

Variants, same command:

```text
# only --remaining 25: the "When" table, no "How Many", no likelihood sentence
# only --target-date:  the "How Many" table only

# likelihood above the confidence cap while work remains (formatLikelihood.ts, CERTAINTY_CAP_THRESHOLD)
Likelihood to close 6 Work Items by Fri 30 Oct 2026: >95%

# hasSufficientData = false (utils/forecast/insufficientForecastData.ts) — replaces the likelihood line
Not enough data yet — need at least 5 days with completed items to forecast.

# likelihood = null (utils/forecast/cannotForecast.ts)
Likelihood to close 25 Work Items by Fri 30 Oct 2026: Cannot forecast

# --filter filtered on a Team with a forecast filter (ManualForecaster.tsx toggle label)
Gravity · 25 Work Items · target Fri 30 Oct 2026 · Use filtered Throughput
```

### `lh forecast backtest --team-id 3 --start-date 2026-09-01 --end-date 2026-09-30 --hist-start-date 2026-07-01 --hist-end-date 2026-08-31`

```text
Gravity · Backtest Results
Period: Tue 1 Sep 2026 to Wed 30 Sep 2026 (historical data: Wed 1 Jul 2026 to Mon 31 Aug 2026)

Forecast Percentiles
Chance  Work Items
50%     24
70%     21
── Actual Throughput: 21 Work Items ──
85%     18
95%     15
```

- **Mirrors** `pages/Teams/Detail/BacktestResultDisplay.tsx`: the header, the `Period:` line, the
  `Forecast Percentiles:` list in its fixed 50/70/85/95 order, and `Actual Throughput:` — all verbatim.
- **Reuses** R-HEAD, R-TABLE, R-LINE.
- **CHOSEN WORDING (layout)**: the web draws the actual as a dashed reference line across a bar chart.
  The CLI places it as an R-LINE between the two percentiles it falls between — the chart's line, in
  text. If the actual is above the 50% value it goes first; below the 95% value, last.
- **Not shown**: the web's `Average:` bar. It needs a second Throughput read and client-side arithmetic
  the CLI does not do today; `--json` is unchanged either way. (Decision D9.)

---

## 2. `metrics` headline — slice 02

### `lh metrics team --id 3` (no `--metrics`: everything)

```text
Gravity · Mon 7 Sep 2026 – Tue 6 Oct 2026 (30 days)

Work Items in Progress    9    System WIP Limit: 10 Work Items
Total Throughput          31   1.0 / day
Total Arrivals            28   0.9 / day
Blocked Work Items        2
Total Work Item Age       84 days across 9 Work Items
Predictability Score      63.4%

Percentile  Cycle Time  Work Item Age
95th        21 days     18 days
85th        12 days     11 days
70th        8 days      6 days
50th        5 days      3 days

Over time (one row per recorded day: lh metrics team --id 3 --metrics <name>)
Cycle Time 85th percentile    14 days on Mon 7 Sep → 12 days on Tue 6 Oct   29 days recorded
Throughput process limits     0 – 3.1 / day, average 1.0, on Tue 6 Oct      29 days recorded
Blocked Work Items            1 on Mon 7 Sep → 2 on Tue 6 Oct               30 days recorded

Time in State (cumulative)    see slice 04
```

- **Mirrors** the Team dashboard, `pages/Common/MetricsView/BaseMetricsView.tsx` and its widgets:
  `WipOverviewWidget.tsx` (`… in Progress`, `Limit: n`), `TotalThroughputWidget.tsx`
  (`Total Throughput`, `n / day`), `TotalArrivalsWidget.tsx`, `BlockedOverviewWidget.tsx`,
  `components/Common/Charts/PredictabilityScore.tsx` (one decimal), `CycleTimePercentiles.tsx`
  (`Cycle Time Percentiles`, `95th`, `21 days`, highest first),
  `components/Common/Charts/TotalWorkItemAgeRunChart.tsx`, and
  `components/Common/QuickSettings/SystemWipQuickSetting.tsx` for the limit's wording.
- **Reuses** R-HEAD, R-TABLE (twice: the headline block is a two-column R-TABLE without a header row).
- **Deviation from the web, deliberate**: the widgets hard-code `Total Throughput`, `Total Arrivals` and
  `Blocked`; the CLI says `Total {Throughput}` and `{Blocked} {Work Items}` in the instance's words
  (D6). The web's own inconsistency is noted, not fixed, in this story.
- **CHOSEN WORDING**: the `Over time` block. The web shows these as charts; a terminal cannot. Each
  over-time metric gets one line — first recorded day → last recorded day, and how many days were
  recorded — plus the hint that `--metrics <name>` prints every day (sketched in slice 03). (D7.)
- **Not shown**: `workDistribution`. It is a placeholder the CLI itself adds ("No dedicated backend
  endpoint is available for this metric"), not a Lighthouse answer. `--json` keeps it. (D8.)
- An individual metric Lighthouse refused prints as its own line, `Total Throughput  unexpected: <reason>`,
  and the rest of the view still renders — today's behaviour per section, kept.

### `lh metrics portfolio --id 2`

```text
Ocean Explorer · Wed 8 Jul 2026 – Tue 6 Oct 2026 (90 days)

Features in Progress      4    System WIP Limit: 5 Features
Total Throughput          7    0.1 / day
Total Arrivals            9    0.1 / day
Blocked Features          0
Total Work Item Age       212 days across 4 Features
Predictability Score      41.8%

Percentile  Cycle Time  Work Item Age
95th        68 days     61 days
85th        52 days     44 days
70th        39 days     30 days
50th        27 days     18 days
```

- **Mirrors** `pages/Portfolios/Detail/PortfolioMetricsView.tsx` over the same `BaseMetricsView.tsx`;
  on a Portfolio the counted thing is the Feature term, as the web titles it.
- **Reuses** as above.

---

## 3. `metrics --metrics <name>` — one metric, every day — slice 03

### `lh metrics team --id 3 --metrics throughput`

```text
Gravity · Mon 7 Sep 2026 – Tue 6 Oct 2026 (30 days)
Total Throughput: 31 Work Items, 1.0 / day

Date             Work Items closed
Mon 7 Sep 2026   2
Tue 8 Sep 2026   0
Wed 9 Sep 2026   1
…                (one row per day, 30 rows)
Tue 6 Oct 2026   3
```

`--metrics arrivals` is the same shape: `Total Arrivals: 28 Work Items, 0.9 / day`, column
`Work Items started`.

### `lh metrics team --id 3 --metrics wip`

```text
Gravity · as of Tue 6 Oct 2026
Work Items in Progress: 9 (System WIP Limit: 10 Work Items)

ID      Name                              State        Work Item Age  Blocked
GR-061  Export flow report as PDF         In Progress  14 days        since Thu 1 Oct 2026
GR-064  Retry failed Jira sync            Review       9 days
GR-066  Show SLE on the refinement tab    In Progress  6 days
…

Date             Work Items in Progress
Mon 7 Sep 2026   7
…                (one row per day)
Tue 6 Oct 2026   9
```

- **Mirrors** `components/Common/WorkItemsDialog/WorkItemsDialog.tsx` (`ID`, `Name`, `State`, the
  Work Item Age column, the blocked marker) opened from the `… in Progress` widget, and the
  `… In Progress Over Time` chart in `BaseMetricsView.tsx` for the daily table.

### `lh metrics team --id 3 --metrics cycleTime`

```text
Gravity · Mon 7 Sep 2026 – Tue 6 Oct 2026 (30 days)
Cycle Time Percentiles: 50th 5 days · 70th 8 days · 85th 12 days · 95th 21 days

ID      Name                              Closed           Cycle Time
GR-052  Rename "Sprint" to "Iteration"    Mon 7 Sep 2026   3 days
GR-055  Fix forecast tooltip overflow     Wed 9 Sep 2026   11 days
…       (one row per closed Work Item)
```

- **Mirrors** `CycleTimePercentiles.tsx` and the `Closed Work Items` dialog titled in
  `BaseMetricsView.tsx`. With `--definition-id 4` the sentence names the cycle time:
  `Lead Time Percentiles: …` (the named definition's name, as the web's scope selector shows it).

### `lh metrics team --id 3 --metrics workItemAge`

```text
Gravity · as of Tue 6 Oct 2026
Work Item Age Percentiles: 50th 3 days · 70th 6 days · 85th 11 days · 95th 18 days

Date             Oldest         Work Items
Mon 7 Sep 2026   GR-061 6 days  7
…
```

- **Mirrors** the Work Item Age percentiles widget (`workItemAgePercentilesTrend.ts`) and the aging
  chart's per-day items.
- **CHOSEN WORDING (layout)**: the per-day table shows the oldest item and the count — the web plots
  every item as a dot. Every item per day is in `--json`.

### `lh metrics team --id 3 --metrics totalWorkItemAge`

```text
Gravity · Mon 7 Sep 2026 – Tue 6 Oct 2026 (30 days)
Total Work Item Age: 84 days across 9 Work Items on Tue 6 Oct 2026

Date             Total Work Item Age  Work Items
Mon 7 Sep 2026   61 days              7
…
```

- **Mirrors** `TotalWorkItemAgeRunChart.tsx` ("Total Work Item Age Over Time").

### `lh metrics team --id 3 --metrics predictabilityScore`

```text
Gravity · Mon 7 Sep 2026 – Tue 6 Oct 2026 (30 days)
Predictability Score: 63.4%

The predictability score shows how "close" the 50% and 95% chance are. The closer they are, the more
predictable you are. 100% means they are exactly the same value. The higher number, the better.

Chance  Work Items in 30 days
50%     31
70%     27
85%     24
95%     20
```

- **Mirrors** `PredictabilityScore.tsx` — the score, its explanation sentence verbatim, and the four
  percentile marks the chart draws as reference lines.

### `lh metrics team --id 3 --metrics blocked`

```text
Gravity · Mon 7 Sep 2026 – Tue 6 Oct 2026 (30 days)
Blocked Work Items: 1 on Mon 7 Sep → 2 on Tue 6 Oct

Date             Blocked Work Items
Mon 7 Sep 2026   1
…
```

- **Mirrors** the `{Blocked} Over Time` chart in `BaseMetricsView.tsx` and `blockedTrend.ts`.

### `lh metrics team --id 3 --metrics percentilesOverTime`

```text
Gravity · Mon 7 Sep 2026 – Tue 6 Oct 2026 (30 days)
Cycle Time over the last 30 days, per recorded day

Date             50th    70th    85th     95th
Mon 7 Sep 2026   6 days  9 days  14 days  23 days
…
Tue 6 Oct 2026   5 days  8 days  12 days  21 days
```

- **Mirrors** `PercentilesOverTimeWidget.tsx` (its 30-day horizon tooltip, `Cycle Time over the last
  30 days`). The horizon is pinned at 30 by the CLI today and stays pinned.
- Empty history prints the web's sentence from `overTimeEmptyState.ts`, verbatim: `Nothing to show for
  the selected range. Days appear here as Lighthouse records them.`

### `lh metrics team --id 3 --metrics processBehaviorOverTime`

```text
Gravity · Mon 7 Sep 2026 – Tue 6 Oct 2026 (30 days)
Throughput natural process limits per recorded day

Date             Lower limit  Average  Upper limit
Mon 7 Sep 2026   0            1.1      3.4
…
```

- **Mirrors** `PbcOverTimeWidget.tsx` (its tooltip, `… natural process limits per recorded day`).
  Empty history: the same `overTimeEmptyState.ts` sentence.

---

## 4. `metrics --metrics cumulativeStateTime` — slice 04

### `lh metrics team --id 3 --metrics cumulativeStateTime`

```text
Gravity · Mon 7 Sep 2026 – Tue 6 Oct 2026 (30 days)
Time in State across 42 Work Items

State        Total days  Work Items  Completed  Ongoing  Mean      Median
To Do        96          18          12         6        5.3 days  4 days
In Progress  241         31          22         9        7.8 days  6 days
Review       88          27          21         6        3.3 days  2 days
Test         61          23          20         3        2.7 days  2 days
```

- **Mirrors** the cumulative state-time chart in `BaseMetricsView.tsx` (ADR-025's widget) — states in
  workflow order, the same per-state facts its tooltip shows.
- **CHOSEN WORDING**: the one-line heading `Time in State across 42 Work Items`. The web titles the chart
  and states no sentence.

### `… --metrics cumulativeStateTime --state Review`

```text
(the table above, then)

Work Items contributing to Review
ID      Name                              Type        State   Days Contributed
GR-064  Retry failed Jira sync            User Story  Review  6
GR-058  Throughput chart legend wraps     Bug         Done    4
…
```

- **Mirrors** the drill-down dialog in `BaseMetricsView.tsx` (`{Work Items} contributing to {state}`,
  `Days Contributed`), verbatim.

### `… --metrics cumulativeStateTime --item-ids 61,64`

Same table, heading `Time in State across 2 Work Items`; the item picker's subset, as the web narrows it.

---

## 5. `team` and `portfolio` — slice 05

### `lh team list`

```text
Teams
Name              Features    Tags              Last Updated
Equinox [id: 1]   3 Features  platform          Tue 6 Oct 2026, 07:12
Lightspeed [id: 2] 1 Feature                    Tue 6 Oct 2026, 07:12
Gravity [id: 3]   6 Features  mobile, payments  Tue 6 Oct 2026, 07:14
Meridian [id: 4]  4 Features                    Tue 6 Oct 2026, 07:13
…
```

(R-TABLE pads the Name column to its widest cell; the misalignment above is the sketch's, not the
renderer's.)

- **Mirrors** the Teams table on the Overview, `pages/Overview/OverviewDashboard.tsx` →
  `components/Common/DataOverviewTable/DataOverviewTable.tsx`: `Name`, the Features term as header with
  `1 Feature` / `n Features` in the cell, `Tags`, `Last Updated`. The title is `{Teams}`.
- **Reuses** R-TABLE (the web reuses `DataGridBase`; this is its CLI counterpart).
- **Not shown**: the `Actions` column (nothing to click).

### `lh portfolio list`

```text
Portfolios
Name                   Features     Tags   Last Updated
Apollo [id: 1]         5 Features          Tue 6 Oct 2026, 06:58
Ocean Explorer [id: 2] 8 Features   ocean  Tue 6 Oct 2026, 06:58
…

Deliveries per Portfolio: lh delivery list --portfolio-id <id>
```

- **Mirrors** the same `DataOverviewTable.tsx`, Portfolios title.
- **Deviation, deliberate**: the web's `Deliveries` column (`DeliveriesChips.tsx`) needs one extra read
  per Portfolio. The CLI points at `lh delivery list` instead of making N extra calls. (D10.)

### `lh team get --id 3`

```text
Gravity [id: 3]
Last Updated on Tue 6 Oct 2026, 07:14

Service Level Expectation: 85% of Work Items within 12 days or less
System WIP Limit: 10 Work Items
Feature WIP: 2 Features
Throughput: Mon 7 Sep 2026 to Tue 6 Oct 2026 (rolling)
Portfolios: Apollo [id: 1], Ocean Explorer [id: 2]
Features: 6
Tags: mobile, payments
Work Item Types: User Story, Bug
```

- **Mirrors** the Team page header `components/Common/FeatureOwnerHeader/FeatureOwnerHeader.tsx`
  (`Last Updated on …`) and the quick-settings bar beneath it: `SleQuickSetting.tsx`
  (`{SLE}: 85% of {Work Items} within 12 days or less`, `Not set` when unset),
  `SystemWipQuickSetting.tsx` (`System {WIP} Limit: n {Work Items}`), `FeatureWipQuickSetting.tsx`
  (`{Feature} {WIP}: n {Features}`), `ThroughputQuickSetting.tsx`.
- **Reuses** R-LABEL.
- **CHOSEN WORDING**: the Throughput line. The web's chip says `Throughput: Rolling 30 days` from the
  Team's settings; the Team read carries the resolved dates, not the setting, so the CLI states the
  dates and `(rolling)` / `(fixed dates)`. The last four lines (Portfolios, Features, Tags, Work Item
  Types) are facts the web shows elsewhere on the page, gathered here.

### `lh portfolio get --id 2`

```text
Ocean Explorer [id: 2]
Last Updated on Tue 6 Oct 2026, 06:58

Service Level Expectation: 85% of Features within 45 days or less
System WIP Limit: 5 Features
Feature WIP: 3 Teams
Teams: Gravity [id: 3], Voyager [id: 6], Zenith [id: 7]
Features: 8
Tags: ocean
```

- **Mirrors** the Portfolio page header (`pages/Portfolios/Detail/PortfolioDetail.tsx` with
  `FeatureOwnerHeader.tsx`) and `PortfolioFeatureWipQuickSetting.tsx` (`{Feature} {WIP}: 3 {Teams}`).
- **Reuses** R-LABEL.

---

## 6. Writes — slice 08

One line each, in the shape of the approved vote lines (R-REC): a past-tense verb, a colon, the thing
and its id. **CHOSEN WORDING** throughout — the web confirms these with snackbars and dialogs, not
sentences the CLI could copy. (D11.)

| Command | Today | New |
|---|---|---|
| `lh team create --payload-file lightspeed.json` | the created Team, dumped field by field | `Created: Team Lightspeed [id: 9].` |
| `lh team update --id 3 --payload-file gravity.json` | the updated Team, dumped | `Updated: Team Gravity [id: 3].` |
| `lh team delete --id 9` | `Team deleted: 9` | `Deleted: Team [id: 9].` |
| `lh team refresh --id 3` | `Team refreshed: 3` | `Refresh queued: Team [id: 3]. Lighthouse updates it in the background.` |
| `lh portfolio create --payload-file apollo.json` | dumped | `Created: Portfolio Apollo II [id: 6].` |
| `lh portfolio update --id 2 --payload-file oe.json` | dumped | `Updated: Portfolio Ocean Explorer [id: 2].` |
| `lh portfolio delete --id 6` | `Portfolio deleted: 6` | `Deleted: Portfolio [id: 6].` |
| `lh portfolio refresh --id 2` | `Portfolio refreshed: 2` | `Refresh queued: Portfolio [id: 2]. Lighthouse updates it in the background.` |
| `lh blackout create --payload-file focus-friday.json` | the rule, dumped | `Created: recurring blackout rule [id: 5] — Every 2 weeks on Friday, from Fri 9 Oct 2026 (Focus Friday).` |
| `lh blackout update --id 5 --payload-file …` | dumped | `Updated: recurring blackout rule [id: 5] — Every week on Friday, from Fri 9 Oct 2026 (Focus Friday).` |
| `lh blackout delete --id 5` | `Recurring blackout rule deleted: 5` | `Deleted: recurring blackout rule [id: 5].` |

- `Team` / `Portfolio` are the instance's words.
- **The refresh line corrects a wrong claim**: `Team refreshed: 3` says the refresh is done; Lighthouse
  has only queued it (the Task Manager shows it as `Refreshing Team 'Gravity'` while it runs — story
  #6055's wording).
- The blackout line's schedule is the `summary` Lighthouse sends with the rule, as the web's
  `BlackoutSettings.tsx` `Schedule` column shows it — not re-worded by the CLI.
- A delete names the id only: the delete answer carries no name, and reading the entity first only to
  name it is a second call for a line. (D12.)

---

## 7. `delivery` — slice 06

### `lh delivery list --portfolio-id 2`

```text
Ocean Explorer · Deliveries
Name                   Delivery Date    Features  Done                  Likelihood  Forecast 85%
Q4 Release [id: 11]    Tue 15 Dec 2026  5         34 of 55 Work Items   78%         Wed 16 Dec 2026
Pilot Launch [id: 12]  Fri 30 Oct 2026  2         18 of 20 Work Items   >95%        Tue 27 Oct 2026
Beta Drop [id: 14]     Fri 2 Oct 2026   3         9 of 14 Work Items    Overdue     Thu 15 Oct 2026
Spring Rollout [id: 15] Tue 9 Mar 2027  4         0 of 31 Work Items    Not enough data  —
```

- **Mirrors** each Delivery's header in
  `pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySection.tsx`: the name, `{Delivery} Date:`,
  the `Overdue` chip, the likelihood chip (`whatTheHeaderChipSays`: `Cannot forecast` beats `Not enough
  data` beats the number; `>95%` cap from `formatLikelihood.ts`), the `Forecast:` chips and the
  progress title (`5 Features (55 Work Items)`).
- **Reuses** R-HEAD, R-TABLE.
- **CHOSEN WORDING (layout)**: one row per Delivery instead of one card. The web's chip text
  `All Features by 15 Dec 2026: 78%` (`jointLikelihoodLabel.ts`) becomes a `Likelihood` column, because
  the sentence repeats the date already in the row; the column keeps the web's three exclusive answers.
  The `Forecast:` chips (50/70/85/95) shrink to the 85% date; all four are in `--json`. (D13.)

### `lh delivery metrics --delivery-id 11`

```text
Delivery [id: 11] · Delivery Date Tue 15 Dec 2026 · recorded since Tue 15 Sep 2026

Date             Done  Remaining  Total  Features  Likelihood
Tue 15 Sep 2026  12    43         55     5         41%
Wed 16 Sep 2026  13    42         55     5         43%
…
Tue 6 Oct 2026   34    21         55     5         78%
```

- **Mirrors** `DeliveryMetricsTab.tsx` (the trend charts, `{Features} over Time`).
- **CHOSEN WORDING**: the web charts these; the table is the CLI's. The heading cannot name the
  Delivery: the metrics read carries no name (out of scope to add — a Lighthouse change).

### `lh delivery metrics --delivery-id 11 --detail epics`

```text
(the table above, then the latest recorded day in detail)

On Tue 6 Oct 2026
Feature Name                       Done  Likelihood  Size
OE-001 Sonar mapping                100%  —           12
OE-002 Deep-sea camera stream       62%   81%         13
OE-007 Pressure alarms              40%   74%         10 (default size)
…

Chance  Done by
50%     Wed 9 Dec 2026
70%     Mon 14 Dec 2026
85%     Wed 16 Dec 2026
95%     Tue 22 Dec 2026
```

- **Mirrors** the Feature grid columns in `DeliverySection.tsx` (`{Feature} Name`, `Likelihood`) and
  `isUsingDefaultSize` as the web's "default size" marker.
- **CHOSEN WORDING (scope)**: the per-Feature detail of the **latest** recorded day only; every day's
  breakdown stays in `--json`. The flag keeps its name `epics` (renaming a flag breaks scripts); the
  output says Features. (D14.)

---

## 8. `feature` — slice 07

### `lh feature get --refs OE-001,OE-002,OE-007`

```text
Feature Name                  Progress              Forecasted Start  Forecasted Completion (85%)  State
OE-001 Sonar mapping          12 of 12 Work Items   —                 —                            Done
OE-002 Deep-sea camera stream 8 of 13 Work Items    Mon 28 Sep 2026   Fri 20 Nov 2026              In Progress
OE-007 Pressure alarms        4 of 10 Work Items    Mon 12 Oct 2026   Cannot forecast              Planned
```

- **Mirrors** `pages/Portfolios/Detail/PortfolioFeatureList.tsx` with
  `components/Common/FeatureListDataGrid/columns.tsx`: `{Feature} Name` (reference id then name, as
  `getWorkItemName` writes it), `Progress`, `Forecasted Start`, `Forecasted Completion`, `State`;
  `Cannot forecast` from `cannotForecast.ts`.
- **CHOSEN WORDING (layout)**: the web's completion cell lists all four chances; the CLI shows 85%
  and names it in the header. (D13.)
- **Not shown**: `Parent` (needs the parent read the web does through `useParentWorkItems`; out of
  scope), `Dependencies`, `Warnings`.

### `lh feature workitems --id 2`

```text
OE-002 Deep-sea camera stream · 13 Work Items

ID      Name                          Type        State        Owned by
GR-061  Export flow report as PDF     User Story  In Progress  Gravity
VO-112  Stream reconnect on drop      User Story  Done         Voyager
…
```

- **Mirrors** `components/Common/WorkItemsDialog/WorkItemsDialog.tsx` (`ID`, `Name`, `Type`, `State`,
  `Owned by`), opened from a Feature's progress bar.
- **CHOSEN WORDING**: the heading. The work-items read does not carry the Feature's name; the CLI reads
  it with the existing `getFeaturesByIds` (one extra call, `--pretty` only). If that read fails, the
  heading is `Feature [id: 2] · 13 Work Items`.

---

## 9. Housekeeping: `blackout list`, `worktracking`, `version`, `health` — slice 09

### `lh blackout list`

```text
Recurring blackout rules
Schedule                                       Description
[id: 5] Every 2 weeks on Friday, from 9 Oct 2026  Focus Friday
[id: 6] Every week on Monday and Tuesday, 1 Dec 2026 – 22 Dec 2026  Hackathon
```

- **Mirrors** `pages/Settings/System/BlackoutSettings.tsx`: `Schedule` (the server's `summary`,
  verbatim) and `Description`.
- **Reuses** R-TABLE. `No recurring blackout rules.` when there are none (**CHOSEN WORDING**).

### `lh worktracking list`

```text
Work Tracking Systems
Name                             Type
Letpeoplework Jira [id: 1]       Jira
Lighthouse ADO [id: 2]           AzureDevOps
Linear Demo [id: 3]              Linear
```

- **Mirrors** the Work Tracking Systems table on the Overview, `OverviewDashboard.tsx`
  (`connectionColumns`: `Name`, `Type`), titled with the `{Work Tracking Systems}` term.

### `lh worktracking get --id 1`

```text
Letpeoplework Jira [id: 1]
Type: Jira

Option      Value
Url         https://letpeoplework.atlassian.net
Username    benj@letpeoplework.com
Api Token   (secret, not shown)
```

- **Mirrors** the connection editor, `pages/Connections/Edit/EditConnection.tsx` (field names as the
  editor labels them).
- **CHOSEN WORDING**: `(secret, not shown)`. Lighthouse never sends a secret's value back; the web
  shows an empty password field, which has no text equivalent.

### `lh version get`

```text
Lighthouse v26.10.3.6
```

- **Mirrors** the footer version, `components/App/LetPeopleWork/LighthouseVersion.tsx`.

### `lh health check`

```text
Lighthouse at https://lighthouse.letpeoplework.com is reachable.
```

- **CHOSEN WORDING**: today it prints `success`. There is no web equivalent (the web is the thing being
  reached). The standalone form: `The standalone Lighthouse is reachable.` Failure is unchanged
  (`category: reason`, exit 1).

---

## 10. Unchanged on purpose

| Command | Why it stays as it is |
|---|---|
| `lh refinement get` | Already the web's words; it is the precedent this page follows (story #6147). |
| `lh refinement vote` / `comment` / `take-back` | Already R-REC lines (story #6156); they are the style the writes copy. |
| `lh connection connect` / `disconnect` / `status` | The wizard is a dialogue, and `status` is already R-LABEL lines (`Connected to: …`, `Auth: …`). Nothing is a facts dump. |
| `lh config output` / `output set` / `voter` / `voter set` | Already sentences (`Default output format: pretty`, `Voter name set to Ana Lima.`). |
| `lh help` and every group's help | Usage text, not a Lighthouse answer. |

Totals: **42 command forms sketched as changing** (counting each `--metrics` name and each write
separately), **12 named as unchanged** (refinement ×4, connection ×3, config ×4, help), and **14 marked
CHOSEN WORDING** in whole or in layout.
