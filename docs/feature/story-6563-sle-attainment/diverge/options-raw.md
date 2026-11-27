# Options (raw) — story-6563-sle-attainment (DIVERGE, rev. 1)

This file describes options and lists the probabilities stated while generating them. It holds no scoring and no
judgement of options. The diversity test, the head-clustering read and the reasons for dropping candidates are in
`taste-evaluation.md` section 0. rev. 1 re-draws the six after review and after the maintainer's inputs in `request.md`:
options 2 and 5 are the two directions the maintainer named, and option 6 is new. The placement of every option is stated.
Copy in examples uses the seeded terms "SLE" and "Work Items" and the figures of the informant's example (8 of 11 is
72.7%; target 70%, 10 days).

## HMW

How might we let a coach see whether a Team or Portfolio keeps the service level it published, and which way that is
moving?

## SCAMPER, with a probability per lens (each lens sums to 1; stated estimates, not measurements)

| Lens | Candidates (probability within the lens) |
|---|---|
| S Substitute | visible text in place of the tooltip 0.55; one word (kept or not kept) in place of numbers 0.25; the share counted only above the day value 0.20 |
| C Combine | the share joined with the SLE Risk count in one SLE widget 0.20; the share inside the Cycle Time Percentiles widget 0.50; readouts of actuals in the header bar, joined with the Quick Settings icons 0.30 |
| A Adapt | an SLO-style daily series of the share against an objective line, recorded each day 0.20; the Businessmap-style highlight of breaching items, on the scatterplot 0.30; error budget remaining (misses allowed, misses used) 0.50 |
| M Magnify | a tile on the Flow Overview with the number, the target and the change against the preceding period 0.35; a larger header chip 0.30; a rolling series computed on read 0.35 |
| P Put to other use | a panel in the Then & Now report, the share in the Then window beside the Now window 0.50; the stored percentile snapshots read against the SLE day value 0.20; the figure on the Team settings page 0.30 |
| E Eliminate | build nothing new, change wording and placement only 0.35; count breaches only 0.25; store nothing, derive on read 0.40 |
| R Reverse | judge each closed period once, in sequence 0.35; a prompt when the share crosses the target 0.40; the reader picks a past date and sees the SLE as it then was 0.25 |

Crazy 8s supplements (sum 1): a sentence in the header, "kept in 5 of the last 6 weeks", written by the system 0.40; the
share split per Work Item type 0.35; a colour band on the scatterplot date axis for periods under target 0.25.

## Deriving the curated-six probabilities

Each curated option takes the stated probability of the lens candidate it came from. The six raw values are divided by
their sum (2.05) so they form one distribution over the six. Stated estimates, never used to rank.

| Option | Source lens candidate | Raw | Curated (raw / 2.05) |
|---|---|---|---|
| 1 Make the SLE more prominent | E, 0.35 (S 0.55 and C 0.50 merged into it) | 0.35 | 0.171 |
| 2 One new dedicated SLE widget in the overview | M, 0.35 | 0.35 | 0.171 |
| 3 A daily record of the share | A, 0.20 | 0.20 | 0.098 |
| 4 A column for each closed week or month | R, 0.35 (E "derive on read, 0.40" merged into it) | 0.35 | 0.171 |
| 5 Tweaking the quick settings to also give us information | C, 0.30 (M "a larger header chip, 0.30" merged into it) | 0.30 | 0.146 |
| 6 A Then & Now panel for the SLE | P, 0.50 | 0.50 | 0.244 |

Tail candidate by the lens rule (probability at most 0.2 in its own lens): option 3 (0.20).

## The six

### Option 1: Make the SLE more prominent

**Core idea**: The Cycle Time Percentiles widget on the Flow Overview says in words what it already knows: "72.7% of Work Items closed within 10 days or less, 8 of 11. SLE target: 70%." The sentence is visible without a hover. The status tooltips say "10 days or less". The Work Item Aging chart on the Flow Metrics tab draws the SLE line without a toggle.
**Key mechanism**: Move the share that `calculateSLEStats` already computes from the status tooltip into the widget body, next to the SLE already shown at its top right. Add the count.
**Key assumption**: The first complaint (cannot find it, cannot compare it) is the pain; a figure for the selected range answers it.
**Placement**: Flow Overview (widget text); Flow Metrics tab (aging chart default).
**SCAMPER origin**: Eliminate (build nothing new); Substitute and Combine candidates merged into it
**Closest competitor**: none found; Scrum.org states the SLE in the same "or less" form
**name_origin**: user-words ("Make the SLE more prominent")
**stated_probability**: 0.171 (curated; 0.35 in lens E)
**Small n**: the count always sits beside the share. Below a minimum count (a DISCUSS value) the sentence adds how few Work Items closed.
**Loading and failure**: the sentence is part of the widget and follows the widget's existing loading and could-not-load looks; it never shows an earlier range's figure.

### Option 2: One new dedicated SLE widget in the overview

**Core idea**: A new widget on the Flow Overview, for the SLE only: the share as a big number, the count and the target under it, and the change against the preceding period of the same length: "72.7% / 8 of 11 within 10 days or less / SLE target 70% / 4.1 points up on the preceding 30 days". The maintainer's words add "perhaps SLE over time in predictability"; that part is a second piece (option 3 or 4), not part of this option's scoring.
**Key mechanism**: A new widget in the pattern of the SLE Risk tile; the preceding-period comparison follows the one the Cycle Time Percentiles trend indicator already makes. As a widget it could later be placed in a report (the maintainer's last signal; reports hold their own panels today, see `taste-evaluation.md` section 6).
**Key assumption**: One comparison with the last period is enough to say "better or worse"; a coach does not need the series. A visible place of its own on the Flow Overview matters.
**Placement**: Flow Overview.
**SCAMPER origin**: Magnify
**Closest competitor**: Grafana SLO dashboard (a stat panel for the SLI against its objective; an assumption from the page read)
**name_origin**: user-words ("One new dedicated SLE widget in the overview")
**stated_probability**: 0.171 (0.35 in lens M)
**Small n**: the widget shows the count under the number. The change is drawn only when both periods hold the minimum count. Otherwise it reads "Not enough Work Items to compare".
**Loading and failure**: the widget shell's status: dimmed under a spinner while loading, the could-not-load message on failure; a dash instead of zero when no SLE is set (as the SLE Risk tile does).

### Option 3: A daily record of the share

**Core idea**: A line chart on the Predictability tab: for each day, the share of Work Items closed in the trailing 30, 60 or 90 days that finished within the SLE, with the SLE target as a labelled horizontal line. Axes: "Date" and "Work Items within the SLE (%)".
**Key mechanism**: A new snapshot kind beside the percentile snapshots: one row per owner, day and horizon, written by a writer like `PercentileSnapshotWriter`, filled backwards by `OverTimeHistoryFiller`, read by a series query and the Team and Portfolio metrics controllers. The row can hold the SLE in force on that day.
**Key assumption**: A series recorded as the days happen is worth a table, a migration for every database provider and a backfill, and the target as it was on each day matters.
**Placement**: Predictability tab, beside Percentiles Over Time.
**SCAMPER origin**: Adapt (an SLO dashboard: a line of the share against an objective)
**Closest competitor**: Grafana SLO dashboard (SLI over time against the objective)
**name_origin**: coined (this wave minted it)
**stated_probability**: 0.098 (0.20 in lens A)
**Small n**: a day whose window holds fewer than the minimum count of Work Items draws no point, as Percentiles Over Time draws none for a day with nothing to measure. The chart says why ("needs at least N closed Work Items in the window"). A filled past day uses today's SLE, as the existing filler uses today's configuration.
**Loading and failure**: the chart's own loading and could-not-load looks; a past range fills in the background and shows on the next visit, as Percentiles Over Time does.

### Option 4: A column for each closed week or month

**Core idea**: A column chart on the Flow Metrics tab: the share per closed calendar period (a week for a Team, a month for a Portfolio; the length follows the selected range), the SLE target as a labelled line, the count on each column. It reads "kept in 5 of the last 6 weeks". Axes: "Week closed" (or "Month closed") and "Work Items within the SLE (%)". Columns start at zero.
**Key mechanism**: Group the Work Items already stored by the period they closed in and compute each share on read, with today's SLE. Nothing is stored.
**Key assumption**: Coaches judge a promise by calendar period (the retro cadence), and a share per period is stable enough to judge once the period is closed.
**Placement**: Flow Metrics tab, below the Cycle Time Scatterplot.
**SCAMPER origin**: Reverse (judge each closed period once, instead of a window that always ends today); the "derive on read" candidate of Eliminate merged into it
**Closest competitor**: Google SRE Workbook (a calendar-aligned window; period-against-period compliance reports)
**name_origin**: coined (this wave minted it)
**stated_probability**: 0.171 (0.35 in lens R)
**Small n**: every column carries its count. A column under the minimum count is drawn hatched grey with the words "too few Work Items" and gets no verdict. The open (current) period is not drawn.
**Loading and failure**: the chart's own loading and could-not-load looks; no column of a previous range stays on screen.

### Option 5: Tweaking the quick settings to also give us information

**Core idea**: The Quick Settings icons in the header bar also report an actual beside each set value. In this Story the SLE only: "SLE: 70% of Work Items within 10 days or less. Now: 72.7% (8 of 11)". The maintainer's examples go further: the System WIP Limit with the current number of items in progress, "etc."
**Key mechanism**: Today each Quick Setting is an icon button with a tooltip of the set value (for example "SLE: 70% of Work Items within 10 days or less") that opens a dialog to edit it (`SleQuickSetting.tsx`, `SystemWipQuickSetting.tsx`). Here the header reads the closed Work Items (and for the WIP example the items in progress) and shows the actual in the bar.
**Scope**: In this Story, the SLE readout. A later Story, and a product decision of its own: the System WIP Limit next to the items in progress, throughput and Feature WIP readouts, and any choice of which readouts to show. The maintainer names the end of that road, "a quick info bar that is freely configurable" (items at risk and more), as "too much for now".
**Key assumption**: The header, seen on every tab, is where the answer belongs, and the set value and the actual belong together in one control.
**Facts that bear on it**: Quick Settings are hidden from Viewers (`docs/feature/rbac-enhancements/feature-delta.md`, Q12). The header stays across tabs, while the share depends on a date range chosen on the Metrics tab. ADR-271 describes the unit page tabs and the settings areas and does not mention the header or Quick Settings.
**Placement**: Header bar of the Team and Portfolio page.
**SCAMPER origin**: Combine (a settings control joined with a readout); the "larger header chip" candidate of Magnify merged into it
**Closest competitor**: none found
**name_origin**: user-words ("Tweaking the quick settings to also give us information")
**stated_probability**: 0.146 (0.30 in lens C)
**Small n**: the readout always shows the count. Below the minimum count it shows the count and no percentage.
**Loading and failure**: the readout shows a dash while loading and "could not load" on failure; it never shows the previous range's figure.

### Option 6: A Then & Now panel for the SLE

**Core idea**: A Then & Now report gets a panel for the SLE: the share within the SLE in the Then window beside the Now window, the target, and the change. "Then 63.6% (7 of 11), Now 72.7% (8 of 11), target 70%."
**Key mechanism**: A new metric panel for the report template, with THEN, NOW and CHANGE, computed from the stored Work Items for the two windows, as the existing panels are (Cycle Time, Throughput, WIP, Total Work Item Age, Arrivals).
**Key assumption**: The retro or stakeholder moment is a comparison of a frozen "before" with today, and the coach has made, or will make, a report for it.
**Placement**: Reports tab of the Team or Portfolio, inside a Then & Now report.
**SCAMPER origin**: Put to other use (the Then & Now report, Epic 5878)
**Closest competitor**: Google SRE Workbook (a compliance report against the previous quarter)
**name_origin**: coined (this wave minted it)
**stated_probability**: 0.244 (0.50 in lens P)
**Small n**: each window shows its count. A window under the minimum count shows the count and no verdict, and the change is not drawn.
**Loading and failure**: the report's own panel looks.

## Requester-named package (not one of the six)

After the first revision the maintainer asked for "my mix" to be scored: one new dedicated SLE widget in the overview plus
the SLE over time in Predictability (`request.md`, last requester block). It is a composite of option 2 and one over-time
option. It is not a generated option, carries no stated probability, and is not part of the diversity test. Two variants:

- **Variant with columns**: option 2 on the Flow Overview, plus option 4's column chart placed on the Predictability tab.
- **Variant with the daily record**: option 2 on the Flow Overview, plus option 3's daily record on the Predictability tab.

**name_origin**: user-words ("one new dedicated SLE widget in the overview, perhaps SLE over time in predictability")
**Small n, loading and failure**: each part keeps its own look and uses one shared minimum count (see options 2, 3 and 4).
Scoring is in `taste-evaluation.md` section 3b.

## Candidates merged or not carried (sameness only; reasons are in `taste-evaluation.md` section 0)

- Visible text in place of the tooltip (S 0.55) and the share inside the Cycle Time Percentiles widget (C 0.50): the same
  mechanism as option 1; merged into it.
- Store nothing, derive on read (E 0.40): merged into option 4. A rolling series computed on read (M 0.35): the picture of
  option 3, derived instead of stored.
- A larger header chip (M 0.30): merged into option 5. Count breaches only (E 0.25, S 0.20): written forms of option 1.
- The highlight of breaching items with a label on the scatterplot line (A 0.30): not carried in rev. 1 (it was option 5
  of the first draft).
- Error budget remaining (A 0.50): a count form (misses allowed, misses used) of the figure in options 1 and 2.
- The share joined with the SLE Risk count (C 0.20); a prompt when the share crosses the target (R 0.40): they belong to
  `job-flow-coach-act-before-sle-breach`.
- One word, kept or not kept (S 0.25): the status indicator already shows it.
- The stored percentile snapshots read against the SLE day value (P 0.20): it was option 6 in the first draft.
- The figure on the Team settings page (P 0.30).
- The reader picks a past date (R 0.25): a form of options 3 and 4.
- Supplements: a system-written sentence "kept in 5 of 6 weeks" (a caption of option 4); the share split per Work Item
  type (a cut of any option); a colour band on the scatterplot date axis (a form of the scatterplot highlight).
