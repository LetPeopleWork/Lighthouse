# Taste evaluation — story-6563-sle-attainment (DIVERGE, rev. 1)

Inputs: the six options in `options-raw.md` (rev. 1). Scoring began after that file was complete. rev. 1 re-draws the
six after review and after the maintainer's inputs in `request.md` (the question "so no new widget in the flow overview?" and the "gut
feeling" with two directions, and later the mix to be scored).

## 0. Diversity check and head-clustering read (moved here from options-raw.md)

### Mechanism, assumption and cost of each option

| Option | Mechanism | Assumption about the reader | Cost (ordinal) |
|---|---|---|---|
| 1 Make the SLE more prominent | words in an existing Flow Overview widget, plus a chart default | the pain is findability | 1 very low |
| 2 One new dedicated SLE widget in the overview | a new Flow Overview tile with one change against the preceding period | one comparison says "better or worse"; a place of its own matters | 2 low to medium |
| 3 A daily record of the share | a persisted daily series on the Predictability tab | recorded days and the target as it then was matter | 5 high |
| 4 A column for each closed week or month | a per-period series derived on read, on the Flow Metrics tab | the retro cadence is the unit | 3 medium |
| 5 Tweaking the quick settings to also give us information | readouts in the header bar, beside the settings icons | the header, seen on every tab, is where the answer belongs, with the set value and the actual together | 4 medium to high |
| 6 A Then & Now panel for the SLE | a two-window panel in a report | the coach builds a Then & Now report for the retro | 3 medium |

### The pairwise 3-point test, all 15 pairs

Y = the pair differs on that point. A pair that shares all three points would be merged.

| Pair | Mechanism | Assumption | Cost | Note |
|---|---|---|---|---|
| 1-2 | Y (text, new tile) | Y | Y | the weakest pair: both sit on the Flow Overview |
| 1-3 | Y | Y | Y | |
| 1-4 | Y | Y | Y | |
| 1-5 | Y | Y | Y | |
| 1-6 | Y | Y | Y | |
| 2-3 | Y | Y | Y | |
| 2-4 | Y | Y | Y | |
| 2-5 | Y | Y | Y | |
| 2-6 | Y (the container; see note) | Y | Y | both compare two windows of the same share; the mechanism differs in the container (a widget over the selected range and its preceding period, against a panel in a report over two frozen windows made when the report was made). The pair passes on assumption (one comparison is enough, against a coach who builds a report) and on cost (low to medium, against medium) |
| 3-4 | Y (persist, derive) | Y | Y | |
| 3-5 | Y | Y | Y | |
| 3-6 | Y | Y | Y | |
| 4-5 | Y | Y | Y | |
| 4-6 | Y | Y | N | the second weakest: both cost medium |
| 5-6 | Y | Y | Y | |

No pair shares all three points. Fourteen of fifteen differ on all three.

The requester-named package (section 3b) is a composite of option 2 and option 3 or 4. It is excluded from this test and from
the head-clustering read below: its mechanism is the mix of its parts, so a pair of the package and a part would share
mechanism and assumption by construction. The curated six and their 15 pairs stay as drawn.

### Head-clustering read

Restated over the six (`options-raw.md` derivation): 1 0.171; 2 0.171; 3 0.098; 4 0.171; 5 0.146; 6 0.244 (sum 1.000).
Stated estimates, not measurements.

| Mechanism group | Options | Mass |
|---|---|---|
| a figure for the selected range, in words or a tile or the header | 1, 2, 5 | 0.488 |
| a Then & Now comparison of two windows | 6 | 0.244 |
| a per-period series derived on read | 4 | 0.171 |
| a persisted daily series | 3 | 0.098 |

No group holds more than half of the mass. The largest, 0.488, is close to the line, and the first draft had 0.727 in
this group. The read is sensitive: raising any of the three raw values by 0.05 makes the group reach 0.50. The over-time
mechanisms (3, 4, 6) hold 0.512. Tail candidate by the lens rule (at most 0.2 in its own lens): option 3, whose mechanism
differs from every head's.

### Why candidates were dropped or merged (moved here from options-raw.md)

- Visible text in place of the tooltip, and the share inside the Cycle Time Percentiles widget: the same mechanism as
  option 1.
- Store nothing, derive on read: the same mechanism as option 4. A rolling series computed on read: the same picture as
  option 3; the difference (a table, but a past that restates when the SLE changes) belongs to DESIGN.
- A larger header chip: a degree of option 5. Count breaches only: the same fact as options 1 and 2, written the other way.
- Error budget remaining (misses allowed, misses used): adds a new concept (an allowance) to the same figure.
- The share joined with the SLE Risk count; a prompt when the share crosses the target: both belong to the job
  `job-flow-coach-act-before-sle-breach`, which looks at items in progress, before a breach.
- One word, kept or not kept: the status indicator already shows it, and the informant asked for numbers.
- A label with the share on the scatterplot SLE line (option 5 of the first draft, score 4.05 there): not carried into rev. 1
  to make room for the two directions the maintainer named. It is a degree of option 1 on another chart. It stays open
  as a later addition to option 1.
- A line at the SLE day value on Percentiles Over Time (the sixth option of the first draft, not the Then & Now panel): it cannot state the share against its
  target. The SLE probability is a whole number from 50 to 95 (`SleQuickSetting.tsx`); only 50, 70, 85 and 95 are recorded.
- The figure on the Team settings page: ADR-271 puts Settings by audience (set-up); the readers of the share are in the
  metrics tabs.
- The reader picks a past date: a form of options 3 and 4.

## 1. Weights (locked before scoring)

Locked 2026-11-27, before any score in this file was written: the default column of `nw-taste-evaluation`, the same
weights as Story 6564 and Story 6535. They do not depend on which option wins.

| Criterion | Weight | Rationale |
|---|---|---|
| DVF (average) | 30% | Default; one informant, so desirability must not be swamped. |
| T1 Subtraction | 20% | Default. |
| T2 Concept count | 20% | Default; the reader is often new to flow ("or less"). |
| T3 Progressive disclosure | 15% | Default. |
| T4 Speed-as-trust | 15% | Default; the metrics tabs load several widgets at once. |

Concept count is counted beyond what every option shares: the SLE, the closed Work Items and the date range.

## 2. Constraint check (one rule) and DVF filter

Constraints, with their source:

- (a) Works for Teams and for Portfolios (job statement).
- (b) The figure shows by default, with no toggle between the reader and the figure, on the page or tab the option names (a
  metrics tab, the header bar, or the Reports tab). Source: `request.md`, first bullet: "instead of hiding it behind chart
  toggles". Round 1 added the words "without set-up"; the requester did not say them, so they are dropped. **One page rule
  for all six options and both packages**: pass = the figure is on screen when that page opens, with no toggle in the way.
  Whether the reader has something to open (a report that was made, a role that sees the header controls) is not a toggle;
  it is counted in Desirability, Feasibility and the notes below.
- (c) States the share and its target as numbers. Source: `request.md`, "for example 72.7% of items within 10 days against
  a target of 70%". Set by the requester through the word "for example"; the host reads it as a hard rule.
- (d) Can meet the visualization skill (labelled axes, a labelled target, a too-little-data state, a loading look and a
  failure look). The looks are stated in `options-raw.md`; layout checks are for DESIGN. Passes below mean "can meet it as
  described", not "is designed".
- (e) Uses the default Cycle Time only (the SLE targets it).

**Rule, applied to every option the same way**: Pass = the mechanism meets the constraint as described. Fail = it cannot
meet it without becoming a different option. One Fail removes the option before DVF. Then a DVF total below 6 removes it.

| Option (page) | (a) Team and Portfolio | (b) default view, no toggle | (c) numbers | (d) | (e) | Result |
|---|---|---|---|---|---|---|
| 1 (Flow Overview; Flow Metrics) | Pass | Pass: the sentence and the aging line show on opening | Pass | Pass | Pass | stays |
| 2 (Flow Overview) | Pass | Pass: the widget shows on opening | Pass | Pass | Pass | stays |
| 3 (Predictability) | Pass: the snapshot has an owner type | Pass: the chart shows on opening at the default 30-day horizon; the horizon selector picks a window and does not switch the SLE on | Pass | Pass | Pass | stays |
| 4 (Flow Metrics) | Pass | Pass: the chart shows on opening | Pass | Pass | Pass | stays |
| 5 (header bar of the unit page) | Pass, at a cost: the Quick Settings icons exist on Team and Portfolio pages (Portfolio SLE icon not checked) | Pass: the readout shows on opening. A cost goes to Feasibility: Quick Settings are hidden from Viewers (`docs/feature/rbac-enhancements/feature-delta.md`, Q12), so the readout must show to Viewers while the edit controls stay Admin-only | Pass | Pass | Pass | stays |
| 6 (Reports tab, inside a Then & Now report) | Pass | Pass: the panel shows when the report opens, with no toggle. A cost goes to Desirability and Feasibility: a Team with no report shows nothing, and a report is made in a wizard | Pass | Pass | Pass | stays |
| Packages (Flow Overview, and Predictability) | Pass | Pass: both parts show on opening | Pass | Pass | Pass | stays |

DVF rubric of `nw-taste-evaluation`: Desirability "1 = no evidence of want, 5 = clear expressed need"; Feasibility "1 =
requires unavailable technology, 5 = straightforward to build"; Viability "1 = no path to revenue/retention, 5 = clear
value capture". Eliminate if total < 6.

| Option | D | F | V | Total | Avg | Reasons |
|---|---|---|---|---|---|---|
| 1 | 3 | 5 | 4 | 12 | 4.00 | D: serves two of the three bullets (prominence, wording) and O2, O4; the third, over time, which the informant called key, is not served. Part of O1 is already fixed (the scatterplot line is drawn by default, per `docs/metrics/flow-metrics.md`). F: the share is already computed; the work is layout, copy and docs. V: no new concept to maintain. |
| 2 | 4 | 4 | 4 | 12 | 4.00 | D: prominence, a place of its own (the maintainer's "gut feeling", `request.md`) and one step of "better or worse"; not a series. F: a new widget, a second range read like the trend indicator, help text and docs. V: a tile pattern the product ships. |
| 3 | 4 | 2 | 3 | 9 | 3.00 | D: the closest to "tracking this over time" as a daily line. F: a new table and a migration for every provider, a writer, a filler, a query, two controllers, a chart; the pipeline pattern exists, which keeps F above 1. V: the cost is large for one informant. |
| 4 | 4 | 3 | 4 | 11 | 3.67 | D: over time as a verdict per period, which matches the social line "five of the last six weeks"; no figure for the selected range unless placed with option 1. F: a grouping query or a wider read, a chart, a small-n state; no migration. V: reads the retro cadence. |
| 5 | 3 | 2 | 2 | 7 | 2.33 | D: answers "a small header indicator" (`request.md`, first block) and shows on every tab; no over time. F: a new read of closed Work Items on every unit page load, a range that may differ from the Metrics tab, a readout for Viewers split from the Admin-only edit controls, and room in the header on a phone. V: the maintainer named the risk: it grows into "a quick info bar that is freely configurable" (items at risk and more), which "for me is too much for now" (`request.md`, the block right after the "gut feeling" block). It also mixes a set value with an actual in one control. V is 2 for that reason. |
| 6 | 3 | 3 | 4 | 10 | 3.33 | D: a retro or stakeholder comparison of a frozen Then with Now, with the change; two windows, not a series, and found only where a report was made (the informant looked on a metrics tab). F: a new panel for the report template, computed from the stored Work Items for two windows like the existing panels; the docs mention values "not captured for this report", so how an SLE panel stores its values was not checked. V: it sits where the maintainer plans flow metric reports (a stated plan, `request.md`). |

**Eliminated before taste scoring: none.** All six options and both packages pass (a) to (e) under the one page rule, and no
DVF total is below 6 (option 5 is lowest at 7). Round 1 eliminated option 6 on "without set-up"; with those words dropped it
stays and is scored.

## 3. Taste scores, with the rubric level quoted

| Option | T1 Subtraction | T2 Concept count | T3 Progressive disclosure | T4 Speed-as-Trust |
|---|---|---|---|---|
| 1 | 5: "Nothing can be removed without breaking the core value" (it adds a sentence and removes a dependency on hover) | 4: "One new concept, well-anchored to something familiar" (the share as a visible figure; anchored to the status tooltip that already shows it and to the SLE) | 4: "First interaction = core flow; secondary features one step removed" (the sentence is visible; the status detail stays in the tooltip) | 5: "Instant feedback; every action has immediate response" (the value is already computed in the browser) |
| 2 | 3: "Multiple removable elements, value unclear without them" (a widget that repeats what option 1 says in an existing widget) | 4: "One new concept, well-anchored to something familiar" (the change against the preceding period, anchored to the trend indicator) | 4: "First interaction = core flow; secondary features one step removed" (number first; the comparison under it) | 4: "Minor latency well-masked by progress indicators" (a second range read) |
| 3 | 3: "Multiple removable elements, value unclear without them" (three horizons, the backfill, the horizon toggle) | 3: "Two new concepts, introduced sequentially" (a trailing-window share; recorded days) | 3: "First interaction exposes 2-3 features; sequencing is logical" (a line, a target, a horizon toggle) | 3: "Noticeable latency but justified by clear payoff" (a past range fills in the background and shows on the next visit, as Percentiles Over Time does) |
| 4 | 4: "One minor element could be removed; core intact" (the hatched too-few state is the one removable element) | 4: "One new concept, well-anchored to something familiar" (a period is kept or not kept, anchored to the retro) | 4: "First interaction = core flow; secondary features one step removed" (the last periods first; the period length is one step away) | 3: "Noticeable latency but justified by clear payoff" (a grouped read over a wide range on every open) |
| 5 (the SLE-only readout, as scored) | 4: "One minor element could be removed; core intact" (the one removable element is the split that shows the readout to Viewers while the edit controls stay Admin-only) | 3: "Two new concepts, introduced sequentially" (a settings control that also reports an actual; a header range that may differ from the Metrics tab) | 3: "First interaction exposes 2-3 features; sequencing is logical" (the SLE readout beside the icons already in the bar) | 3: "Noticeable latency but justified by clear payoff" (extra reads on every unit page, including tabs that do not show metrics) |
| 6 | 4: "One minor element could be removed; core intact" (the CHANGE line, which the existing panels carry too) | 4: "One new concept, well-anchored to something familiar" (the share, in the THEN, NOW and CHANGE layout the reader knows) | 2: "First interaction requires choosing between multiple paths" (the reader first makes a report: Type, Then, Now, metrics) | 4: "Minor latency well-masked by progress indicators" (the report's own loading look) |

Note on option 5: later readouts (the System WIP Limit beside the number in progress, "etc.") are not part of the option as
scored. They would add rows to the bar and would lower T1 and T3 at that time. The first draft of this table scored T1 and T3
from those later readouts; that was a scope error and is corrected here.

## 3b. The requester-named package (scored as its own rows)

The maintainer asked for "my mix" to be scored (`request.md`, last requester block): "one new dedicated SLE widget in the
overview, perhaps SLE over time in predictability". That span is its name (name_origin: user-words). The package is a
composite of option 2 and one over-time mechanism, so it is not a seventh generated option and is **excluded from the diversity
test of section 0** (a pair of the package and one of its parts would share two mechanisms by construction). It is scored with
the same locked weights and the same rubric.

The over-time part can be built two ways, and both are plausible, so both are scored:

- **Variant with columns**: option 2 plus option 4's column chart, placed on the Predictability tab (the placement is open
  for option 4; the maintainer pointed at Predictability). No new table.
- **Variant with the daily record**: option 2 plus option 3's daily record, which already sits on the Predictability tab,
  beside Percentiles Over Time, and follows its pattern.

Constraints (a) to (e): both variants pass as their parts do, under the one page rule of section 2.

**Rule for the Desirability of a composite.** Desirability scores how many of the requester's stated needs a row serves. The
needs are: be more prominent, show over time, say "or less" (first block), and a place of its own on the Flow Overview (later
blocks). A composite serves the union of its parts' needs, so it can score above each part: D 5 means it serves all of them.
This is a judgement and no rubric line states it. The sensitivity row "both packages lowered to 4" shows the effect: they
stay below option 1 and option 2 either way.

| Variant | D | F | V | Total | Avg | Reasons |
|---|---|---|---|---|---|---|
| columns | 5 | 3 | 4 | 12 | 4.00 | D: serves all the stated needs. F: two builds to ship: a widget with a second range read, and a grouped query with a chart and a small-n state; no migration. Each part ships alone. V: the widget can later sit in a flow metric report (the maintainer's stated plan, a direction); two deliveries on one informant. |
| daily record | 5 | 2 | 3 | 10 | 3.33 | D: as above. F: the widget plus a new table, a migration for every provider, a writer, a backfill, a query, two controllers and a chart; the cost of option 3 dominates. V: the cost of option 3 on one informant. |

Whole-package looks, for both variants:

- **Concept count**: the widget brings one new concept (the change against the preceding period). The columns bring a second
  (a period is kept or not kept): "Two new concepts, introduced sequentially", T2 3. The daily record brings a trailing-window
  share and recorded days: with the widget that is three, T2 2.
- **Cost**: the widget costs low to medium and the series costs medium (columns) or high (daily record). The costs add up and
  do not overlap, because the two parts share no code.
- **Loading and failure**: the two parts sit on two pages, so each keeps its own look: the widget the widget shell's status
  (spinner, could-not-load, a dash with no SLE); the chart its own looks. A failure of one never blanks the other.
- **Small count**: the same rule in both parts, with one minimum count. The widget and every column or day show their count.
  Under the minimum there is no verdict and the chart shows "too few Work Items" in a distinct grey state. The widget's change
  is drawn only when both periods hold the minimum count.
- **Overlap**: the widget's change against the preceding period and the chart's last two periods answer the same question
  once. That overlap is the reason T1 is 3 for the columns and 2 for the daily record.

Taste scores (rubric levels quoted):

| Variant | T1 Subtraction | T2 Concept count | T3 Progressive disclosure | T4 Speed-as-Trust |
|---|---|---|---|---|
| columns | 3: "Multiple removable elements, value unclear without them" (the widget's change repeats the chart's last periods) | 3: "Two new concepts, introduced sequentially" | 4: "First interaction = core flow; secondary features one step removed" (the number on the Flow Overview; the series one tab away) | 3: "Noticeable latency but justified by clear payoff" (the series page groups a wide range on open; scored by its slowest part) |
| daily record | 2: "Clearly bloated; several non-essential parts" (three horizons, the backfill, a horizon toggle, and a widget that repeats the line's last values) | 2: "Three or more concepts, some interdependent" | 3: "First interaction exposes 2-3 features; sequencing is logical" (a line, a target and a horizon toggle on the series page) | 3: "Noticeable latency but justified by clear payoff" (a past range fills in the background, as Percentiles Over Time does) |

## 4. Weighted matrix

Weighted = 0.30 x DVF avg + 0.20 x T1 + 0.20 x T2 + 0.15 x T3 + 0.15 x T4.

| Rank | Option | name_origin | DVF | T1 | T2 | T3 | T4 | **Weighted** |
|---|---|---|---|---|---|---|---|---|
| 1 | 1 Make the SLE more prominent | user-words ("Make the SLE more prominent") | 4.00 | 5 | 4 | 4 | 5 | **4.35** |
| 2 | 2 One new dedicated SLE widget in the overview | user-words ("One new dedicated SLE widget in the overview") | 4.00 | 3 | 4 | 4 | 4 | **3.80** |
| 3 | 4 A column for each closed week or month | coined | 3.67 | 4 | 4 | 4 | 3 | **3.75** |
| 4 | 6 A Then & Now panel for the SLE | coined | 3.33 | 4 | 4 | 2 | 4 | **3.50** |
| 5 | package, variant with columns | user-words ("one new dedicated SLE widget in the overview, perhaps SLE over time in predictability") | 4.00 | 3 | 3 | 4 | 3 | **3.45** |
| 6= | 3 A daily record of the share | coined | 3.00 | 3 | 3 | 3 | 3 | **3.00** |
| 6= | 5 Tweaking the quick settings to also give us information | user-words ("Tweaking the quick settings to also give us information") | 2.33 | 4 | 3 | 3 | 3 | **3.00** |
| 8 | package, variant with the daily record | user-words (same span) | 3.33 | 2 | 2 | 3 | 3 | **2.70** |

Check. Option 1: 1.20 + 1.00 + 0.80 + 0.60 + 0.75 = 4.35. Option 2: 1.20 + 0.60 + 0.80 + 0.60 + 0.60 = 3.80. Option 4:
1.10 + 0.80 + 0.80 + 0.60 + 0.45 = 3.75. Option 6: 1.00 + 0.80 + 0.80 + 0.30 + 0.60 = 3.50. Package with columns: 1.20 + 0.60 +
0.60 + 0.60 + 0.45 = 3.45. Option 3: 0.90 + 0.60 + 0.60 + 0.45 + 0.45 = 3.00. Option 5: 0.70 + 0.80 + 0.60 + 0.45 + 0.45 =
3.00. Package with the daily record: 1.00 + 0.40 + 0.40 + 0.45 + 0.45 = 2.70.

Margins: option 1 leads option 2 (the runner-up) by 0.55. Option 2 leads option 4 by 0.05; option 4 leads option 6 by 0.25;
option 6 leads the package with columns by 0.05; that package leads options 3 and 5 by 0.45; options 3 and 5 lead the package
with the daily record by 0.30. The package with columns is 0.90 under option 1 and 0.35 under option 2. Option 6 is 0.85
under option 1. Option 4 is the best over-time option on its own (0.60 under option 1).

**Why a package scores below its best part.** The weights give Subtraction and Concept count 40% together. A package adds a
second element and a second concept, and part of it repeats the other (the widget's change and the chart's last periods).
The package is the only row with Desirability 5, because it serves the most stated needs. The matrix rewards simplicity
more than coverage. That is a trade the maintainer should see, and section 5 shows how far coverage would have to count for
the package to lead.

## 5. Does the order hold? (sensitivity; the locked weights stay the basis)

PC = package with columns. PD = package with the daily record.

| Test | Opt 1 | Opt 2 | Opt 4 | Opt 6 | PC | Opt 3 | Opt 5 | PD |
|---|---|---|---|---|---|---|---|---|
| Locked weights | 4.35 | 3.80 | 3.75 | 3.50 | 3.45 | 3.00 | 3.00 | 2.70 |
| DVF raised to 50%, taste weights scaled down in proportion | 4.25 | 3.86 | 3.73 | 3.45 | 3.61 | 3.00 | 2.81 | 2.88 |
| Desirability of option 1 lowered to 2 (no over time at all) | 4.25 | 3.80 | 3.75 | 3.50 | 3.45 | 3.00 | 3.00 | 2.70 |
| Desirability of options 3 and 4 raised to 5 (over time counted as the whole need; the reviewer's stress) | 4.35 | 3.80 | 3.85 | 3.50 | 3.45 | 3.10 | 3.00 | 2.70 |
| Desirability of options 2 and 5 raised to 5 (the "gut feeling" counted as a full expressed need) | 4.35 | 3.90 | 3.75 | 3.50 | 3.45 | 3.00 | 3.20 | 2.70 |
| Desirability of both packages lowered to 4 (the mix is a gut feeling, not user evidence) | 4.35 | 3.80 | 3.75 | 3.50 | 3.35 | 3.00 | 3.00 | 2.60 |
| Viability of options 2, 3 and 6 and of both packages raised to 5 (flow metric reports are planned, the maintainer's stated plan) | 4.35 | 3.90 | 3.75 | 3.60 | 3.55 | 3.20 | 3.00 | 2.90 |
| Both at once: DVF at 50%, and Viability of options 2, 3 and 6 and of both packages at 5 | 4.25 | 4.02 | 3.73 | 3.62 | 3.77 | 3.33 | 2.81 | 3.21 |

Option 1 leads in every row. Options 2 and 4 are within 0.29 of each other in every row and change places in one
(Desirability of 3 and 4 at 5: 3.85 against 3.80). The last row shows how close the widget can get: with DVF at 50% and
Viability counted as 5 for the parts that reports could reuse, option 1 (4.25) leads option 2 (4.02) by 0.23, and the package
with columns (3.77) passes option 4 (3.73). Option 6 is fourth in six rows and fifth in the two rows with DVF at 50%. The
package with columns is fifth in six rows, fourth with DVF at 50%, and third in the last row. The package with the daily
record is last in six rows and seventh in the two DVF rows. Option 5 ties option 3 at 3.00 in three rows, is above it in one,
below it in four, and is last only in the two rows with DVF at 50%. The matrix cannot be tipped to a package by weights or
Desirability alone. It tips only if a constraint is added: "shows a series over time" would remove options 1, 2, 5 and 6, and
then option 4 (3.75) leads the package with columns (3.45) and option 3 (3.00). Under that constraint the package with columns
would be the second choice. That is a scope decision for the maintainer, not a weight.

## 6. Note on the basis of the scores

Desk judgement against the rubrics, on thin evidence (one informant, one workshop; the maintainer's directions and the mix
are a "gut feeling" in the maintainer's words, not user evidence). The ranking shows which direction fits the rubrics, not a
measured preference. The weights favour simplicity (T1 and T2 together 40%), which is the reason a words-and-placement option
leads and a package trails. That is a trade the reader should see.

**Widgets and reports.** What the repo shows today (checked 2026-11-27): the Then & Now report has its own panel for each
metric (Cycle Time, Throughput, WIP, Total Work Item Age, Arrivals), each with THEN, NOW and CHANGE
(`docs/reports/thenandnow.md`). `docs/reports/` names no dashboard widget, and the report views reuse only the widget shell
component (`WidgetBody`), not the dashboard widgets. Reports today have one Type. The maintainer's answer to this: "we plan
on having flow metric reports so not an issue" (`request.md`). It is recorded as the requester's stated plan: a direction,
not a capability. It is read as a positive signal for the widget, the series and the report panel, and it is used only in the
Viability rows of section 5. It does not change the order.
