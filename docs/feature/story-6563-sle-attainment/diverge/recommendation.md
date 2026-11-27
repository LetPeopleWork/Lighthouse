# Recommendation — story-6563-sle-attainment (DIVERGE, rev. 1)

Inputs: `job-analysis.md`, `competitive-research.md`, `options-raw.md`, `taste-evaluation.md`. Scores below are from
`taste-evaluation.md` (locked weights). The six options were scored singly. The maintainer's mix was scored after, as its own
two rows.

## Decision statement for DISCUSS

Proceed with showing the share [new] of closed Work Items within the SLE, next to its target and with its count, as
visible text in the Cycle Time Percentiles widget on the Flow Overview (for example "72.7% of Work Items closed within
10 days or less, 8 of 11. SLE target: 70%"), with "or less" in the status tooltips, and with the SLE line on the Work Item
Aging chart drawn by default. No new widget and no new chart is built in this step; the widget text and one chart default
change. Assumption: the first complaint (cannot find it, cannot compare it) is a pain worth fixing on its own. The over-time
half is a separate scope decision for the maintainer (see "Scope choices"). Score 4.35.

## Recommended: make the SLE more prominent (4.35)

The widget that already holds the SLE says the comparison in words. The sentence is visible without a hover. The count sits
beside the share [new]. The share is already computed for the status tooltip (`calculateSLEStats`); the work is layout,
copy, the aging chart default and the docs. Per `docs/metrics/flow-metrics.md`, the scatterplot already draws its SLE line
by default.

Why it ranks first: it leads the runner-up by 0.55 under the locked weights. It scores highest on Subtraction (5) and
Speed-as-trust (5), and 4 on Concept count: the share is already in the status tooltip, so the reader meets an old
number in a new place. It scores lower on Desirability (3 against 4 for the widget of its own and for the series) because it
does not answer the half the informant called key: how the share develops over time. The order holds in every sensitivity
row, including Desirability of this option cut to 2 and the "gut feeling" counted as a full expressed need
(`taste-evaluation.md` section 5).

**Critical weakness, stated plainly.** This option serves the first and third request. It does not serve the second one,
"tracking this over time is key". The weights favour simplicity (Subtraction and Concept count are 40% together), and that is
why a words-and-placement option leads.

**Answer to the maintainer's question, "so no new widget in the flow overview?"** In this step, no: the Cycle Time
Percentiles widget is already on the Flow Overview and carries the sentence. A widget of its own scores 3.80, and is the
dissent below.

## Dissent: one new dedicated SLE widget in the overview (3.80)

This is the runner-up by score, and it is the maintainer's "gut feeling". The margin to the recommended option is 0.55.

- **What it is**: a new widget on the Flow Overview for the SLE only. It shows the share as a big number, the count and the
  target under it, and the change against the preceding period of the same length.
- **Why it scores well**: it gives the SLE a place of its own and one step of "better or worse" (Desirability 4). It
  follows the SLE Risk tile and the existing trend indicator. The maintainer plans flow metric reports ("not an issue" that
  reports do not reuse widgets today), so a widget could later be placed in a report. That is a stated plan, a direction,
  not a capability today: the Then & Now report has its own panels and no dashboard widget.
- **Core trade-off**: a new widget that repeats what a sentence in the existing widget says (Subtraction 3). One comparison
  is not a series.
- **Key risk**: one preceding period over few Work Items swings by 9.1 points for each Work Item at 11 items.
- **Use when**: the maintainer decides that a place of its own on the Flow Overview matters more than the simplicity of
  the words-only step. With Desirability or Viability counted as 5 it reaches 3.90, and still trails the recommended
  option by 0.45.

## Scope alternatives (not the dissent)

These options are scored singly. Together with the mix, they are the choices the maintainer has on scope.

- **The share per closed week (Team) or month (Portfolio), one column each, with its count and the SLE target as a
  labelled line [new], 3.75.** It reads "kept in 5 of the last 6 weeks". It is computed from the stored Work Items on read;
  nothing is stored. A column under the minimum count is hatched grey, "too few Work Items", with no verdict. The open
  period is not drawn. It is the best over-time option: 0.60 under the recommended option and 0.05 under the dissent. Its
  page is open: the Flow Metrics tab, or the Predictability tab where the maintainer pointed ("perhaps SLE over time in
  predictability").
- **A panel for the SLE in a Then & Now report [new], 3.50.** It shows the share in the Then window beside the Now window,
  with the target and the change. It is 0.85 under the recommended option and 0.25 under the columns. It shows only where a
  report was made, and it sits where the maintainer plans flow metric reports.
- **A persisted record written each day, drawn as one line per day [new], 3.00.** A line over a trailing 30, 60 or 90
  days, written daily like Percentiles Over Time. It is the only way to keep the SLE as it stood on each day. It costs a new
  table, a migration for every database provider, a writer, a backfill, a query, two controllers and a chart. On one
  informant that cost is not justified yet. With Viability counted as 5 (planned flow metric reports) it reaches 3.20.

The two series compute against today's SLE for past days. Lighthouse holds the SLE as two current numbers on the Team or
Portfolio; no history of the target was found in what was read. If the target changed during the period, the past restates.
DISCUSS must say whether that is acceptable, or whether the target change is marked on the chart.

## The maintainer's mix, scored: the widget plus the SLE over time in Predictability

The maintainer asked for "my mix" to be scored. It has two rows, because the over-time part can be built two ways. The mix is
a composite of the widget and one series, so it is not in the diversity test.

- **The widget plus the columns, placed in Predictability: 3.45.** It is the only row with Desirability 5, because it
  serves the most stated needs (a rule recorded in `taste-evaluation.md` section 3b). It is 0.90 under the recommended
  option, 0.35 under the dissent and 0.30 under the columns alone. It ranks fifth. It has two concepts for the reader and
  two builds, and the widget's change repeats the last periods of the chart (Subtraction 3, Concept count 3,
  Speed-as-trust 3).
- **The widget plus the daily line, placed in Predictability: 2.70.** It carries the cost of the daily record (a table, a
  migration, a backfill) and three concepts (Subtraction 2, Concept count 2). It ranks last of the eight rows.

The matrix rewards simplicity (Subtraction and Concept count are 40%), so a mix of two good parts scores below each part.
The mix does not lead under any case tested. In the most favourable row (DVF at 50% and Viability counted as 5, because the
maintainer plans flow metric reports) the widget plus columns reaches 3.77 and the recommended option stays at 4.25. Under
a constraint "shows a series over time" the mix is second, behind the columns alone (3.75). The widget alone and the series
alone are each deliverable, so the mix can still be built as two steps.

## The quick settings: tweaking the quick settings to also give us information (3.00)

The maintainer's second direction: the header bar reports an actual next to each set value, such as the SLE and how well it
is kept, and the System WIP Limit next to the number of items in progress. The maintainer said it risks growing into "a quick
info bar that is freely configurable", which "for me is too much for now".

It scores 3.00, tied with the persisted daily record, and is not recommended. Its Viability is 2 because of the maintainer's
own words. The score is for the SLE readout only. What the evidence adds:

- **Scope for this Story**: the SLE readout only. The System WIP Limit with the number in progress, throughput, Feature WIP
  and any choice of readouts belong to a later Story and a product decision of its own.
- **Fit with ADR-271**: ADR-271 gives the unit page tabs as the contexts' views (Metrics shows flow metrics) and splits
  settings by who decides. It does not mention the header or Quick Settings. A Quick Setting is a set value that opens an
  edit dialog; an actual is a flow metric. Showing both in one control mixes two contexts. Read in the spirit of the ADR, the
  actual belongs on the Metrics tab and the header would be a new, fourth kind of surface.
- **Viewers**: Quick Settings are hidden from Viewers (RBAC decision Q12). A readout for a retro audience has to show to
  Viewers, so it must split from the edit controls. Then it is no longer a tweak of Quick Settings.
- **Range**: the header stays across tabs, but the share depends on a date range chosen on the Metrics tab. The header
  needs its own range, which can disagree with the chart beside it.
- **Visualization skill**: a figure needs its target and its good direction, and a view answers one question. A small
  header chip carries the number and the target in text and has little room for either. It must work at phone width.

## How a small number of closed Work Items is shown

The informant's example is 8 of 11. One Work Item moves an 11-item share by 9.1 points. If the 8 of 11 became 7 of 11, the
share would read 63.6%, 6.4 points under the 70% target, and the status indicator would turn amber on one Work Item. With 3
Work Items one item moves the share by 33.3 points. A Portfolio closes fewer items than a Team, so it is worse there.

What the evidence says: Lighthouse already decided that a percentage over one to four items is noise, and dropped it from
the Work Item Age status rule. Jira's control chart sets a minimum of 5 issues in its rolling window. The Google SRE
Workbook warns that a single failure can be a large share for a customer with few requests, and gives no threshold.

What every option does, in the same way:

1. **The count is always shown beside the share** ("8 of 11"). The reader sees how thin the base is.
2. **Under a minimum count, no verdict**: the share is still written, with how few Work Items closed, and a chart shows the
   period or day as "too few Work Items" in a distinct grey state. It does not draw a point or a column that reads as a
   measured pass or fail. This is a state of its own, not an empty chart (visualization skill, section 6).
3. **The minimum count is a DISCUSS value**. The two numbers found are 5 (Jira) and "one to four" (Lighthouse). No
   evidence was found for a better one.
4. **Open for DISCUSS**: the status indicator of the Cycle Time Percentiles widget today turns red or green on any non-zero
   count. It may need to go neutral under the minimum. That is a behaviour change to an existing status, not part of this
   option.

## Answers to the three open questions from discovery

- **Where the comparison lives**: in the widget that already holds the SLE, as text. It stays in the Team and Portfolio metrics
  tabs (ADR-271). Nothing moves to Settings.
- **How "over time" is computed**: if the maintainer keeps it in scope, one value for each closed period, computed on read.
  A persisted daily record is the dearer choice, justified only if the target as it stood on each day must be shown. Not
  recommended on this evidence.
- **How a small number is shown**: see the section above.

## Open points for DISCUSS

1. Is "over time" part of this Story, or a second Story? The recommended step is deliverable alone.
2. The minimum count, and whether the status indicator goes neutral under it.
3. The share is computed twice (`calculateSLEStats` for the percentiles status; the scatterplot status computes the other
   side with its own thresholds). One knowledge, one place: a DESIGN question.
4. A named Cycle Time makes the Cycle Time Percentiles status neutral, because the SLE targets the default one. The sentence
   must follow: with a named Cycle Time selected it says nothing about the SLE.
5. Terminology: copy uses the configured words for SLE and Work Items; the docs use the seeded defaults.
6. For DEVOPS: the riskiest assumption is that readers use the trend, not only today's number. A name-only usage event on
   the over-time view, if one is built, would show it.
7. Widgets in reports: the maintainer says "we plan on having flow metric reports so not an issue". It is a stated plan, a
   direction and not a capability. DISCUSS confirms what it needs from a widget.

## For the requester

**What we looked at.** Six ways to show whether a Team or Portfolio keeps its SLE. All six passed the checks. We scored all six one at a time, and then your mix. Two of the six are the two directions you named.

**Recommended.** Show the share [new] of Work Items within the SLE as visible text in the Cycle Time Percentiles widget, with the count and the target next to it, for example "72.7% of Work Items within 10 days or less, 8 of 11. SLE target: 70%". The status tooltips also say "or less". The SLE line on the Work Item Aging chart is drawn without a toggle. No new widget and no new chart is built. The widget text and one chart default change. It scores 4.35.

**What it does not do.** It does not show the share over time. The informant called that key. This option fixes the first and the third request only.

**Dissent.** One new dedicated SLE widget in the overview. It shows the share as a big number, with the count, the target and the change against the preceding period. It is your "gut feeling". It scores 3.80, which is 0.55 lower. It wins if you decide that the SLE needs a place of its own on the Flow Overview more than it needs a simple first step.

**Your question, "so no new widget in the flow overview?"** In the recommended step, no. The widget that is already there says the comparison.

**The quick settings.** Tweaking the quick settings to also give us information scores 3.00. You said the freely configurable bar is "too much for now". We do not recommend it on this evidence: Quick Settings are hidden from Viewers, and the header would need its own date range.

**A small number of closed Work Items.** The count is always shown ("8 of 11"). One Work Item moves an 11-item share by 9.1 points. Under a minimum count, we show how few Work Items closed and give no pass or fail. You choose the minimum in DISCUSS.

**What we are unsure about.** One person at one workshop asked for this. The scores are desk judgement, not measured. The SLE is held as two current numbers, so any chart over time uses today's SLE for past days. Reports hold their own panels today, not dashboard widgets. You say "we plan on having flow metric reports". That is a plan, not a capability.

**Scope choices for you.**
- One number now only: the share as text in the widget that is there. Score 4.35.
- A widget of its own on the Flow Overview: share, count, target and change against the last period. Score 3.80.
- A real trend: the share for each closed week or month, one column each, with its count and the target. Score 3.75. A line per day from a record kept each day scores 3.00.
- A panel in a Then & Now report: the share in the Then window beside the Now window. Score 3.50.
- Your mix ("one new dedicated SLE widget in the overview, perhaps SLE over time in predictability"). With the column chart: score 3.45, which is 0.90 lower than the recommended option. With the daily line: score 2.70.
