# Feature: story-6502-hide-outliers

ADO Story 6502 "Hide Outliers". Wave state: **DIVERGE, DISCUSS and DESIGN done in AFK mode (DESIGN and the slices revised after review round 1), DISTILL next.** No DISCOVER artifact exists.

> Written without the maintainer. Everything marked **A** is an assumption I made so the work could finish; everything
> marked **Q** is a decision that needs the maintainer before DISCUSS closes.

## Wave: DIVERGE / [REF] Summary

- **Job:** keep the picture of normal work legible while exceptional items stay known, reachable and explainable, with no
  number changing (`diverge/job-analysis.md`, 6 outcome statements).
- **Inventory:** of about 25 widgets, one is HIDE (Cycle Time scatterplot), one is SHOW by the maintainer's decision
  (Work Item Aging), two are OPT for a later story (Estimation vs Cycle Time, Feature Size), the six Process Behaviour
  Charts are never hidden, the rest are N/A or SHOW (`diverge/chart-inventory.md`).
- **Rule:** an outlier is a closed Work Item whose Cycle Time is more than twice the 95th percentile of the chart's items;
  median multiples, Tukey fences, a fixed percentile and SLE multiples were weighed and rejected as the cut
  (`diverge/outlier-definition.md`).
- **Directions (6 generated, 1 eliminated by DVF):** frame break with edge markers; chip + popover; overview + inset;
  "Show outliers" switch; non-linear / broken axis; reader-flagged items (eliminated)
  (`diverge/options-raw.md`, `diverge/taste-evaluation.md`).
- **Recommendation:** chip + popover (4.35), then the switch (3.55) and the frame break (3.40)
  (`diverge/recommendation.md`).
- **Unchanged by every direction:** View Data, the percentile lines, the SLE share and RAG, the tiles, the forecast
  inputs. The Report PDF export and bulk reports contain no charts today.

## Wave: DIVERGE / [REF] Assumptions (A)

| # | Assumption | Basis | Reverse if |
|---|---|---|---|
| A1 | The cut is 2 x P95 and never below the highest drawn reference line (percentiles, SLE). | Computed on an illustrative lognormal: flags about 1% of items; P95 ignores a single extreme item because of `PercentileCalculator`'s index. Not measured on Lighthouse data. | demo-data check in DESIGN shows too few or too many chips |
| A2 | The rule is off under 10 closed items. | P95 of a handful of items means nothing. | the maintainer prefers 20 or none |
| A3 | The cut is computed from the percentile values for the data shown (the named percentiles when a named cycle time is selected), not from which lines are ticked visible. | The cut should not move when the reader unticks "95%". | |
| A4 | Teams and Portfolios get the same behaviour because `BaseMetricsView` renders one `CycleTimeScatterPlotChart` for both. | `buildWidgetNodes`. | Portfolio Features need another cut |
| A5 | Percentile lines, SLE, RAG, tiles and View Data are untouched. | The story says outliers stay in View Data; changing statistics silently is what the research says other tools do and Lighthouse must not. | |
| A6 | No report or PDF contains the scatterplot today. | `Reports` pages import no chart component; `EnlargeableChart` is used only by the Delivery metrics tab. | a report type adds a chart |
| A7 | Importance/satisfaction numbers in the job analysis are estimates, not user data. | No DISCOVER evidence. | |
| A8 | Competitive findings come from web search summaries plus two fetched articles (PolicyViz, Desbarats); no `nw-researcher` dispatch and no peer review (`nw-diverger-reviewer`) was run. | Run was unattended and a Haiku review adds little certainty. | DISCUSS wants a reviewed DIVERGE |
| A9 | `docs/product/jobs.yaml` and wave-decisions at the SSOT level were not edited (shared docs). Proposed job text: the strategic statement in `job-analysis.md`. | Instruction for this run. | |

## Wave: DIVERGE / [REF] Open questions for the maintainer (Q)

1. **The rule.** Options: (a) more than 2 x the 95th percentile; (b) more than N x the median; (c) 1.5 or 3 x the 95th
   percentile. *Recommend (a).* Your "500% of the median" flags about 5% of items on a typical team (it is the P95), so
   the chip would show on most charts. Keep the "x the median" figure in the popover as the description.
2. **Minimum data.** Options: no rule under 10 closed items; under 20; none. *Recommend 10.*
3. **Small numbers.** A team with a P95 of 3 days has a cut of 6 days, so a 7-day item would be an "outlier" although
   the chart is not squeezed. Options: (a) accept it; (b) a floor on the cut (for example 14 days); (c) clip only when
   the largest item exceeds 1.5 x the cut. *Recommend (a) for now and look at the demo data in DESIGN; (b) and (c) add a
   second sentence to the help text.*
4. **Hidden types.** When type chips hide some types, should the chip count (a) only the visible types, or (b) all
   outliers? *Recommend (a): the chip describes what you see.*
5. **Seeing outliers in place.** Options: (a) no, the popover list and View Data are enough; (b) a "Show on chart"
   action inside the popover that stretches the axis until the reader leaves, with no stored state; (c) a persistent
   "Show outliers" switch (the dissenting option 4). *Recommend (a)* for the first release; take (b) if a reader
   wants the outlier next to the others. This is the question that would change the recommended direction.
6. **Scope of this story.** Options: (a) the Cycle Time scatterplot only (Teams and Portfolios), with the cut and chip
   built reusable; (b) also Estimation vs Cycle Time now. *Recommend (a).* Estimation vs Cycle Time is a sensible
   follow-on story; Feature Size stays "show" because a very large Feature is what its chart is for. Per the project
   rule that each Epic ships on its own, this is also where a split would fall, if you split.
7. **"How much bigger".** Options: "52 x the median", "5,200% of the median", both. *Recommend "x the median" in plain
   words; a percent over 1,000 reads badly.*
8. **The word.** Confirm "outlier" as the fixed UI word (it is not a renameable Terminology term), or choose another
   ("exceptionally long"). *Recommend "outlier"*, matching your story and the docs. DISCUSS will add it to the ADR-270
   glossary.
9. **View Data.** Keep unchanged (all items, as now), or add an "x median" column or an outlier marker there?
   *Recommend unchanged;* the popover already carries the numbers.
10. **Inventory.** Confirm the verdicts you did not decide yourself: the six Process Behaviour Charts never hide
    (a point beyond the limits is their purpose); Throughput / Arrivals bars never clip (bars start at zero); Percentiles
    over Time is a statistic, so no clipping. *Recommend confirming as listed.*

## Wave: DIVERGE / [REF] Artifacts

All in `docs/feature/story-6502-hide-outliers/diverge/`: `job-analysis.md`, `chart-inventory.md`,
`outlier-definition.md`, `competitive-research.md`, `options-raw.md`, `taste-evaluation.md`, `recommendation.md`.
Not produced: `review.yaml` (no peer review run), `docs/product/jobs.yaml` update, SSOT `wave-decisions.md`.

## DIVERGE peer review (2026-11-02, Haiku): NEEDS_REVISION

- Blocker: "> 2 × P95" masks extremes at small N (N=20: 18 × 5 days, 60, 100 → P95 = 60, cut 120, the 100-day
  item is not flagged). Resolved by the maintainer's choice of a median rule below.
- Major: the rule text ("never below the SLE line") and the popover sentence disagree. Moot with the new rule, but
  the help sentence must state the rule exactly.
- Major: ODI outcome statements O1-O6 name the chart and are compound; rewrite them chart-free in DISCUSS.
- Minor: option 4 is a variant of option 2; competitive sources have no URLs or dates; the taste rubric is not quoted;
  the dissent margin is not stated; some chart-inventory claims (PBC y-axis, Work Item Aging and Feature Size using
  `getMaxYAxisHeight`) are unverified.
- Confirmed by the reviewer: `getMaxYAxisHeight` = 1.1 × max (`chartAxisUtils.ts:26-43`); `buildViewData` uses the
  full list; Reports pages import no chart; one scatterplot serves Teams and Portfolios via `BaseMetricsView`.

## Maintainer decisions (2026-11-02, after DIVERGE)

Binding for DISCUSS.

| # | Decision |
|---|---|
| Direction | **Chip + popover** (the recommended direction). |
| Q1 rule | **An outlier is more than N × the median; N = 5 to start.** It must be easy to change (one constant). It is a **frontend calculation**, generic and reusable per chart, because an item may be normal in one chart and an outlier in another (it is computed on that chart's own value, e.g. Cycle Time on the scatterplot). |
| Q2 minimum data | **No minimum.** The rule is always on. |
| Q4 type chips | **The chip counts only the outliers among the visible Work Item types.** |
| Q5 in place | **No.** Outliers are seen only in the popover and in View Data; the chart stays zoomed to normal work. |
| Q6 scope | **Cycle Time scatterplot only**, for Teams and Portfolios. The rule and chip are built so other charts can reuse them later. |
| Q10 verdicts | **Confirmed:** process behaviour charts never hide; Throughput and Arrivals never clip; Percentiles over time is unaffected; Feature Size shows everything for now; Work Item Aging shows outliers. |
| Q3, Q7, Q8, Q9 | Not asked individually; the recommendations stand unless the maintainer says otherwise: "52× the median" wording (Q7); "outlier" is the fixed UI word and enters the ADR-270 glossary (Q8); View Data unchanged (Q9). Q3 (small-number floor) is moot with the median rule; DISCUSS checks whether a tiny median (e.g. 1 day) needs a floor and raises it if so. |

## Wave: DISCUSS / [REF] State and method (2026-11-02, PROPOSE mode)

Wave state: **DISCUSS proposed, awaiting maintainer review.** The DIVERGE rule (2 x P95, minimum of 10 items, a floor
at the highest line) is superseded everywhere below by the maintainer's decisions; assumptions A1-A3 are retired.
Every item marked **N** is a new open question. The sketch walk-through happens at the start of DISTILL.

Scope Assessment: PASS. One ADO Story, 3 user stories, 3 slices of 0.5 to 2 days (the first two DISCUSS slices merged by AFK-6), 1 bounded context (Flow metrics,
presentation only), 0 backend changes, 0 new integration points. Not an Epic; no split needed.

Findings that shaped this wave (verified in code):

- `CycleTimeScatterPlotChart` is the only consumer; `BaseMetricsView` renders it for Teams and Portfolios, and it
  receives the items already limited to the date range. Its axis is fixed from all data (stable while type chips
  are toggled) via `getMaxYAxisHeight` = 1.1 x the larger of dots, percentile values and SLE.
- On an illustrative right-skewed lognormal the 95th percentile is about 5.2 x the median, at or just above the cut
  of 5 x the median, which suggested that the 95% line would often lie above a clipped axis. **Computed on Team
  Zenith's real demo data it does not**: 50% = 6 days, 95% = 15 days (2.5 x), cut = 30 days, so no line lies above
  the cut there (table "Computed Team Zenith numbers", AFK-15). Edge markers are therefore rare on real-looking data;
  they stay as an honesty guard so a line is never silently lost, and they are proven on a fixture population.
- The backend percentile index (`floor(p/100 x N) - 1`, clamped; `PercentileCalculator.cs`) puts the 50% line one
  rank below the textbook median when p x N is not a whole number. By AFK-10 the rule's "median" is that same 50th
  percentile, computed in the frontend with the same convention, so the popover's median and the 50% line are always
  the same number (this replaces the earlier "true median" proposal).
- The legend row, including the type chips, is rendered only while at least one dot is drawn. Switching every type
  off therefore removes the chips that would switch them back on. Not this story's bug, but the "every visible item
  is an outlier" state must keep the legend row, and AFK-12 makes the "all types off" state keep it too.
- The `cycleScatter` entry of `widgetInfoMetadata.ts` is a fixed string ("cycle time", "items", "outliers"); other
  entries are built from `terms`. The new sentence must be built from `terms`.

## Wave: DISCUSS / [REF] Persona and JTBD

**Persona.** Maria Santos, flow coach for three teams (also the delivery lead or team lead who walks a stakeholder
through the chart). Tomas Reyes, delivery lead, is the second reader (Portfolio). Existing personas, no new file.

**JTBD one-liner.** When I read the picture of my closed work to judge how the team is doing, I want the picture of
normal work to stay legible while the exceptional items stay known and one step away, so that I can act on both and
explain the picture to someone else without being misled by either.

**job_id (hard DoR gate).** The nearest existing jobs were read and do not fit:
`job-flow-coach-spot-pace-outliers` (in-flight items against pace bands, Work Item Aging) and
`job-delivery-lead-see-time-over-custom-window` (named Cycle Time) concern other situations. A narrow search of
`docs/product/jobs.yaml` is limited (the file is over 512 KB and `ctx_search` skipped it on most patterns), so the
main session should confirm with `grep -n -i "outlier\|legib" docs/product/jobs.yaml` before adding the entry.
**Proposed new entry** (shared doc, not edited here):

```yaml
- id: job-flow-coach-read-typical-work-know-the-exceptions
  title: Read where typical closed work lands while knowing which items are exceptional
  persona: flow-coach
  feature_context: story-6502-hide-outliers
  job_story: >
    When I read the picture of my closed work in a flow review or in front of a stakeholder,
    I want normal work to stay legible while every exceptional item stays known, reachable and explained,
    so I can act on both and explain the picture without being asked "what is that dot?" or "what did you leave out?".
  dimensions:
    functional: read where most closed Work Items land, know which did not and how far out they are
    emotional: confident that nothing is hidden and nothing is drowned by one item
    social: put the picture in front of a stakeholder and answer both questions
  related: [job-flow-coach-spot-pace-outliers]   # counterpart for finished work
```

All three stories carry `job_id: job-flow-coach-read-typical-work-know-the-exceptions`.

### Outcome statements (rewritten: chart-free, one metric each; fixes the DIVERGE review's major finding)

Importance and satisfaction remain estimates (no user data; see A7).

| # | Outcome statement | Imp | Sat | Story |
|---|---|---|---|---|
| O1 | Minimise the time it takes to see where most closed Work Items fall relative to the percentile values | 5 | 2 | US-01 |
| O2 | Minimise the likelihood that an exceptionally long Work Item goes unnoticed | 5 | 3 | US-01 |
| O3 | Minimise the likelihood that a displayed statistic is taken to describe fewer items than it covers (guard rail) | 5 | 4 | US-01, US-02 |
| O4 | Minimise the time it takes to find which Work Items are exceptionally long | 4 | 2 | US-01 |
| O5 | Minimise the time it takes to learn how many times longer than typical an exceptional Work Item is | 4 | 2 | US-01 |
| O6 | Minimise the effort to explain to a stakeholder why a Work Item is not in the picture | 4 | 2 | US-01, US-03 |
| O7 | Minimise the likelihood that a Work Item disappears from a picture with no sign that it did | 5 | 3 | US-01, US-02, US-03 |

(The DIVERGE O4 was compound, "which" and "how far"; it is now O4 and O5. DIVERGE O2 and O6 overlapped; O2 is now
about noticing, O7 about silent disappearance.)

## Wave: DISCUSS / [REF] Locked decisions

| # | Decision | Source |
|---|---|---|
| D1 | Chip + popover on the Cycle Time scatterplot only; Teams and Portfolios share it. | Maintainer |
| D2 | Outlier = value > N x median; `OUTLIER_MEDIAN_MULTIPLE = 5`, one constant, one place. "More than": an item exactly at the cut is not an outlier. | Maintainer |
| D3 | A generic frontend calculation (pure function over a list and a value selector) that another chart can reuse on its own value. No backend change, no API change. | Maintainer |
| D4 | No minimum sample size. Guards instead of minimums: no items, or median <= 0, means no outliers. | Maintainer + guard |
| D5 | The chip counts only outliers among the visible Work Item types. | Maintainer |
| D6 | Outliers are never drawn; the popover and View Data show them. View Data is unchanged. | Maintainer |
| D7 | Chart verdicts as in `diverge/chart-inventory.md` (confirmed). | Maintainer |
| D8 | Percentiles, SLE share, RAG, tiles, forecasts and Spearman keep including outliers. | Carried |
| D9 | "outlier" is a fixed UI word, not a Terminology key; it enters the ADR-270 glossary. | Q8 |
| D10 | Median population: every closed Work Item of the date range for the Cycle Time shown, **independent of the type chips**, so the cut and the axis do not move when a type is toggled. "The median" is Lighthouse's own 50th percentile (AFK-10): sort ascending, index = floor(0.5 x N) - 1, clamped, exactly the backend's `PercentileCalculator.cs`. It is always one of the items' values (whole days) and equals the 50% line. N2 is answered by AFK-2. | AFK-2, AFK-10 |
| D11 | The axis is capped at the cut and lines above it are shown as labelled edge markers; both ship in one slice (AFK-6). | AFK-3, AFK-6 |
| D12 | The "x the median" figure: no percent; under 10 one decimal rounded **up** (so a flagged item never reads "5.0 x"), from 10 upward a whole number rounded half-up from the exact ratio (10.5 reads "11x", AFK-17); accessible name "9.7 times the median" (computed example: TZ-301, 58 days against the 50% line of 6). | Proposed (Q7) |

### The help text (exact)

One sentence, built from the reader's terms, N from the constant:

> An outlier is a closed {Work Item} whose {Cycle Time} is more than {5} times the median {Cycle Time} of all closed {Work Items} in this date range.

"The median" is the number behind the 50% line (AFK-10); the popover states it in days.

With a named Cycle Time selected, "{Cycle Time}" is the definition's name ("...whose Concept to Cash is more than 5
times the median Concept to Cash of..."). The ⓘ sentence is its short form: "Items beyond {5} times the median
{Cycle Time} are not drawn; they are counted in the chip next to the legend." (not "outliers" twice).

### Copy table (every line uses the configured term)

| Where | Text | Terms |
|---|---|---|
| Chip | `{n} outlier` / `{n} outliers` (icon + words) | none |
| Chip accessible name | `{n} outliers not drawn on the chart. Show details.` | none |
| Popover title | `{n} outliers` | none |
| Popover line 1 | `Not drawn. They are longer than {cut} days (5 times the median of {median} days).` The median is the 50% line's number (AFK-10) | none |
| Popover line 2 | the help sentence above | Work Item(s), Cycle Time |
| Popover line 3 | `The percentiles, the {SLE} share and the numbers include them. View Data lists them too.` | SLE |
| Popover line 4 (conditional) | `Above the axis: 95% at {p95} days, {SLE} at {sle} days.` (values come from the lines; for example `95% at 33 days, SLE at 35 days` on the illustrative fixture of US-02) | SLE |
| Column headers | `{Work Item}`, `Closed`, `{Cycle Time} (days)`, `Times the median` | Work Item, Cycle Time |
| Row | `TZ-301 Legacy data store migration`, `12 Mar 2026`, `58`, `9.7x` (screen reader: "9.7 times the median"); 58 days and 9.7x are computed for Team Zenith, last 30 days; the closed date is illustrative | |
| Overflow | `and {k} more` + `Open in a list` | |
| Dialog title (slice 03) | `{Work Items} with a {Cycle Time} more than 5 times the median` | Work Items, Cycle Time |
| All-outliers state | `Every visible {Work Item} is an outlier. Open the chip to see them.` | Work Item |
| All-types-off state (AFK-12) | `All {Work Item} types are hidden. Switch one on in the legend.` | Work Item |

"Days" and "Closed" are the unit and the field as the chart's axis and dialog already write them.

## Wave: DISCUSS / [REF] Behaviour on awkward data

| Case | What happens |
|---|---|
| Nothing above the cut | No chip, axis as today, chart unchanged. |
| One 58-day item (demo Team Zenith, TZ-301, last 30 days; computed), 50% line 6 | Cut 30. The item is not drawn; chip "1 outlier"; "9.7x the median" in the popover. |
| Item exactly 30 (5 x 6; illustrative) | Not an outlier. |
| Tiny median (1 day, cut 5 days; illustrative) | A 6-day item is an outlier: true to the rule, and the plot spans 1 to 5 days either way, so clipping buys little. Upper percentile lines usually fall above the cut and become edge markers (slice 02). A floor is **raised as N1**; the proposal is no floor now. |
| Median 0 or no items | No outliers; guard in the calculation. |
| 2 or 3 items (illustrative) | The rule runs (D4) with the 50th percentile (index floor(0.5 x N) - 1). `[1, 1, 100]`: median 1, cut 5, the 100 is an outlier. `[1, 100]`: median 1 (the lower of two), cut 5, the 100 is an outlier. Honest and cheap; the chip is a disclosure, not a verdict. |
| Everything visible is an outlier | Only reachable through the type chips (the median item is never above 5 x the median). Plot frame, axes, lines and the whole legend row stay; the plot says "Every visible {Work Item} is an outlier. Open the chip to see them." |
| All types switched off | The legend row stays, and the plot says "All {Work Item} types are hidden. Switch one on in the legend." Never "No data available" (AFK-12). |
| Several outliers on one day | The chart groups same-day, same-Cycle-Time items; the popover lists each item on its own row. |
| A reference line above the clipped axis | Edge marker (D11). |
| Loading / failure | The chip belongs to the widget: dimmed with it while loading, replaced by the could-not-load message on failure, never a count from the previous answer. Whenever the widget status leaves "ready" the chip is disabled and an open popover closes (AFK-11). |
| Date range changed | Median, cut, chip, list and axis recompute from the new range's items. The x-axis (closed date) keeps its domain from all items, outliers included (AFK-13): an outlier's closed date lies inside the range, so only the y-axis is distorted. |
| Named Cycle Time | The same calculation on the named value; popover names it; items outside the definition are not in the population (they are not on the chart either). |

### Reference lines when the axis is clipped

Percentile and SLE values are computed over all items by the backend and stay that way. When at least one outlier
exists, the axis top is `axisTop` (DESIGN): 1.1 x the larger of (a) the highest typical dot and (b) the highest
percentile / SLE value that is at or below the cut, **and never above the cut**. The lines are all the percentile
lines and the SLE of the data shown, ticked or not, as the axis does today. With no outlier the axis is today's.

- One slice (AFK-6): the cap at the cut and the edge markers ship together. A line above the axis top becomes an
  **edge marker**: an upward triangle at the top edge, right end of the plot, in the line's own colour, with the text
  label ("95%: 33 days" on the illustrative fixture of US-02; on Team Zenith no line lies above the cut, so the demo shows no marker, AFK-15). Shape and text carry the meaning, not colour. The popover names the same lines in one
  sentence.
- The earlier "slice 01 alone" variant (every line keeps the axis open, so some outliers sit below the axis top yet
  stay undrawn) is withdrawn: it is dishonest on the typical team.
- If edge markers cannot be built as designed, the DESIGN names a concrete fallback (the marker row moves into the
  legend row) and the spike pass/fail criteria that decide it; keeping the axis open for every line is not the
  fallback, because it undoes the point of the Story on most teams.

## Wave: DISCUSS / [REF] UI sketch (for the DISTILL walk-through)

Reuses: the legend row of `CycleTimeScatterPlotChart` (percentile chips left, type chips right), MUI `Chip` as
`LegendChip` uses it (not `LegendChip` itself: its `aria-pressed` toggle semantics are wrong for a button that opens
a popover), MUI `Popover` as `InfoPopoverButton` does, a small MUI `Table` for the list, and `WorkItemsDialog`
(`DataGridBase`, tracker links, sort, export) behind "Open in a list". Nothing new in the data layer. Per ADR-271 it
lives on the Metrics tab of a Team's and a Portfolio's page; no surface moves.

The loaded sketch (one outlier, the 95% line above the capped axis as an edge marker) and the open-popover sketch
are in the DESIGN sketches below, drawn with the Team Zenith demo item (AFK-14). The DISCUSS versions used an
invented Ocean Explorer item (a Portfolio, not a Team) and are removed. Popover facts that do not change: it opens
below the chip, its maximum width is the viewport minus 32 px, Esc closes it. More than 10 outliers: first 10 rows,
then "and 4 more" beside "Open in a list". The median in days is always shown (line 1 of the popover, AFK-10: it is
the number behind the 50% line). Phone width: the Closed column is hidden, the rest scrolls horizontally inside the
popover.

All visible items are outliers (type chips hide the rest; axis numbers illustrative):

```
 [50%] [70%] [85%] [95%] [SLE]                         [Feature] [Bug]  [(!) 2 outliers]
  Cycle Time (days)
  31 |                 Every visible Work Item is an outlier.
  15 |                 Open the chip to see them.
   1 +---------------------------------------------------- Date
```

Loading: the whole widget dimmed under a small spinner (`WidgetShell` `status`), chip dimmed with it, no stale
count. Failure: the widget's could-not-load message replaces plot and chip. Check by hand with the browser throttled.

## Wave: DISCUSS / [REF] Keyboard and screen-reader access

- The chip is a real button in the tab order, after the type chips, in the same order as it reads. Accessible name
  "{n} outliers not drawn on the chart. Show details." `aria-haspopup="dialog"`, `aria-expanded`.
- Enter or Space opens; Esc and a visible Close button close; focus returns to the chip. The popover is a labelled
  dialog (title = "{n} outliers"); focus moves into it.
- After "Open in a list" the popover closes and focus moves to the dialog. When the dialog closes, focus returns to
  the chip if it is still mounted, otherwise to the widget heading (the count may have fallen to 0, or the widget
  left "ready", in which case the chip is gone or disabled).
- The list is a real table with column headers and scoped row headers; the Work Item name is a link (tracker URL as
  in the dialog); "9.7x" has the accessible text "9.7 times the median".
- The chip is also the text alternative for what the SVG chart omits: the chart is not keyboard-explorable, so
  nothing else carries the outliers for a screen-reader user, and the popover must be complete (the first 10 rows
  plus "Open in a list").
- The count changes when the reader toggles a type chip; that toggle announces its own state, so no extra live
  region is added (to be confirmed in DESIGN with a screen reader).
- Not colour alone: the chip has an icon and words; edge markers have a shape and a text label. Contrast checked in
  the dark theme.
- **Testable screen-reader criteria (React Testing Library):** (1) `getByRole('button', { name: '2 outliers not drawn
  on the chart. Show details.' })` exists, with the singular name for 1; (2) after opening, `getByRole('dialog',
  { name: '2 outliers' })` exists and holds one `table` with four `columnheader`s and one `rowheader` per row; (3) the
  "times the median" cell has the accessible text "9.7 times the median" (a visually hidden span; the visible "9.7x" is
  `aria-hidden`); (4) the SVG edge-marker group is `aria-hidden="true"`, so the lines above the axis are exposed
  exactly once, in the popover's "Above the axis" sentence; (5) the chart region has no `aria-live` of its own. A
  manual pass with a real screen reader stays a DELIVER step.

## Wave: DISCUSS / [REF] Visualization checklist (`.claude/skills/visualization/SKILL.md` section 8)

- [x] **One question and its decision.** Question: "Where do my closed {Work Items} land against the percentile
  lines and the {SLE}, and which are far beyond?" Decision: is typical delivery within the {SLE}, and which
  exceptional items does the team investigate.
- [x] **Awkward data.** Table above: tiny, few items, outlier, none, all, partial period (unchanged), empty
  (unchanged "No data available"), beyond-the-horizon N/A (not a forecast). To repeat on the demo data and the
  restored dev history in DESIGN.
- [x] **Simplest form.** The scatter stays; one chip and one popover are added. No pie, gauge or 3D.
- [x] **Title and axes.** Title is the Cycle Time term, axis `{Cycle Time} (days)` and `Date`, unchanged; the new
  copy uses the table above.
- [x] **ⓘ popover.** One added sentence, from `terms`; Learn More stays `#cycle-time-scatterplot` (slice 03).
- [x] **Docs.** A "how to read it" passage in the scatterplot section of `metrics/flow-metrics.md` at finalize: the
  chip, the rule in one sentence, that no number and no View Data row changes, what an edge marker means.
- [x] **Grey plus one accent.** The chip is outlined and neutral, not the SLE or percentile hue. The only accent
  remains the SLE line. Edge markers reuse their line's colour, so a colour keeps its one meaning.
- [x] **No meaning by colour alone.** Icon + words on the chip, triangle + text on markers.
- [x] **Bars from zero; dual axis.** N/A, because a scatter with one value axis.
- [x] **Uncertainty.** N/A, because the chart shows actuals; no forecast on it.
- [x] **Loading, failure, too little data.** Loading and failure are `WidgetShell` states (above). "Too little
  data" is deliberately not a state here: D4 says no minimum, and the chip is a disclosure, not a verdict.
- [ ] **Phone width, dark mode, enlarged, PDF.** Phone: legend row wraps (`flexWrap`), popover width-limited and
  scrolling, Closed column dropped. Dark mode: verified in DELIVER. Owner of the phone, dark and PDF checks: DELIVER (the chart step checks 375 px width
  and both themes in a browser and logs the result). Enlarged: N/A, because `EnlargeableChart` is not
  used by this chart. PDF/bulk reports: N/A, because no report contains a chart today (rule for the future: a chart
  added to a report prints "2 items above 31 days are not shown: 312, 190").

## Wave: DISCUSS / [REF] Context map, glossary, UI surface (ADR-270, ADR-271)

- **Contexts touched:** Flow metrics (core) only, on its frontend presentation. Work Items, Forecasting, Signals,
  Reports and RBAC are not touched. No new context and no new edge.
- **Domain events:** none published, none reacted to. The calculation is pure and runs at render time on data the
  scatterplot already holds.
- **New glossary term (Flow metrics), proposed row for ADR-270, not edited here:**

  | Term | Meaning | Code name today | Drift |
  |---|---|---|---|
  | Outlier | A closed Work Item whose value on a chart is more than 5 times the median (the 50th percentile, with Lighthouse's own convention) of that chart's items; computed per chart on that chart's own value; not drawn on that chart, still counted in every number and listed in View Data | `findOutliers`, `OUTLIER_MEDIAN_MULTIPLE`, `fiftiethPercentile` (`utils/charts/outliers.ts`) | **Overloaded in prose**: "pace outlier" (Work Item Aging bands), a point beyond the limits of a Process Behaviour Chart, and this one are three different things. Qualify every use |

  Also proposed: `ARCHITECTURE.md` section 4 gets one line under Flow metrics ("charts may hide outliers by the
  shared median rule; the statistics never do"). Both land at the DESIGN or DELIVER wave of this feature.
- **UI surface (ADR-271):** a tab of a unit's page: the Metrics tab of a Team and of a Portfolio. New thing in its
  target surface; nothing moved, nothing to move one step closer here (the feature does not touch a settings page).
- **Terminology:** no new Terminology key. Everything renameable renders as the reader's word (copy table).

## Wave: DISCUSS / [REF] Lighthouse checklist

- **RBAC impact: N/A, because** no endpoint, DTO or permission changes; the chip derives from the Cycle Time data
  the scatterplot already receives. Nothing is gated; `useRbac()` is not involved and nothing reads
  `/authorization/my-summary`. The named Cycle Time selector stays premium-gated as today; the calculation runs on
  whatever the chart shows.
- **Lighthouse-Clients CLI/MCP versioning: N/A, because** the CLI and MCP return raw Cycle Times and percentiles;
  the outlier is a render-time view concern, no API change, no client version bump.
- **Website marketing surface: N/A for a new claim, because** this refines an existing chart and is not a headline
  capability; **but** the scatterplot screenshot (docs assets, hot-linked from the website) must be regenerated at
  finalize if the demo data now produce a chip. Release-note positioning is written at `/release`, not at finalize.
- **Docs:** passage in `metrics/flow-metrics.md` (slice 03, written at finalize; code ships first).
- **Usage data (DEVOPS asks):** candidate name-only event "outlier popover opened"; not decided here.
- **Demo data:** DESIGN checked: no seeded Team or Portfolio has a closed Work Item above 5 x its median. DELIVER
  adds TZ-301 to Team Zenith (AFK-14). Ocean Explorer is a Portfolio (its `OE-0xx` are Epics), not a Team, and has no
  outlier; the Portfolio path is covered by unit tests only (E2E stays thin).
- **Loading and failure looks:** designed above (DESIGN pins and verifies them).
- **Epic ships on its own:** N/A, because one Story, not an Epic split. Slices ship in order 01 (chip, cap and edge markers: the
  first two DISCUSS slices merged by AFK-6), 02, 03; each is releasable alone.
- **ADO:** untouched. The three user stories are slices of ADO Story 6502, no new work items proposed.

## Wave: DISCUSS / [REF] User stories (embedded acceptance criteria)

<!-- markdownlint-disable MD024 -->

### US-6502-01: Typical work stays readable while outliers stay one click away

`job_id: job-flow-coach-read-typical-work-know-the-exceptions`

**Problem.** Maria Santos is a flow coach who walks Team Zenith (the demo Team) through their Cycle Time chart every
sprint review. One long {Work Item}, TZ-301 "Legacy data store migration" (added to the demo data in DELIVER, AFK-14),
took 58 days against a 50% line of 6 days (computed, last 30 days, 29 closed items), so it stretches the axis to about
64 days (1.1 x 58) and the team's other 28 dots and their lines (95% at 15 days) are squeezed into the lower third of
the plot (the axis would end at 22 days without it). She now explains the chart by
hand ("ignore that one dot") and still gets asked what she left out.

**Elevator Pitch**
Before: Maria cannot read where typical {Work Items} land while one very long {Work Item} sets the scale of the whole chart.
After: open a Team's Metrics tab, look at the Cycle Time widget -> sees the plot scaled to normal work and a chip "1 outlier" in the legend row; click the chip -> sees TZ-301 Legacy data store migration, 58 days, 9.7x the median.
Decision enabled: Maria decides which exceptional {Work Item} to investigate in the review, and whether typical delivery meets the {SLE}.

**Domain examples**

Figures are labelled **computed** (Team Zenith, last 30 days, from the table "Computed Team Zenith numbers"; they
shift by about a day with the weekday the demo is loaded, so DELIVER pins them against Zenith's own 50th percentile)
or **illustrative** (fixture figures that exist only in a test).

1. Happy path (computed). Team Zenith after AFK-14: 29 closed items, 50% line 6 days, 70% 8, 85% 10, 95% 15, the
   SLE 85% within 7 days, the largest other item 20 days, so the cut is 30. TZ-301 at 58 days is the one outlier,
   9.7x the median. The plot ends at 22 days (1.1 x 20), below the cut, the chip reads "1 outlier", the popover
   lists TZ-301 with "9.7x". No line lies above the cut, so the demo shows no edge marker. The unit-test fixture
   version (illustrative): median 6, other items 3 to 20 days, TZ-301 at 58.
2. Edge (illustrative). A Team with a median of 6: an item at exactly 30 days is not an outlier; at 31 days it is
   "5.2x" (5.17 rounded up). Under 10 the figure shows one decimal rounded up, so "5.2x" and never "5.0x". Unit-level:
   no seeded item sits at the cut.
3. Nothing to hide (read from the demo data at DISCUSS, not recomputed). Tomas Reyes's Portfolio view: the demo
   Portfolios (Ocean Explorer is one) hold Features of 14 to 45 days with medians of 30 to 38, so the cut is 150 or
   more and no chip shows. The Portfolio chip is covered by unit
   tests only, because no seeded Portfolio has an outlier.
4. Terms (wording example). A Team that renamed Work Item to "Story" and Cycle Time to "Lead Time" reads: "An outlier is a closed
   Story whose Lead Time is more than 5 times the median Lead Time of all closed Stories in this date range."

**UAT scenarios (BDD)**

```gherkin
Scenario: The chart shows normal work and says what is left out
  Given a Team has 29 closed Work Items with a 50% line of 6 days, the longest of the others at 20 days, the 95% line at 15 days and the SLE at 7 days, and TZ-301 closed at 58 days (computed: Team Zenith, last 30 days)
  When Maria opens the Metrics tab
  Then the Cycle Time chart does not draw TZ-301 and its axis top is 1.1 x 20 = 22 days, below the cut of 30 days
  And a chip reading "1 outlier" appears in the legend row

Scenario: The popover tells her how far beyond normal each outlier is
  Given a median of 6 days and the chip reads "2 outliers" for TZ-301 (58 days, computed) and TZ-302 (40 days, illustrative: not in the demo data)
  When Maria opens the chip
  Then she sees TZ-301 Legacy data store migration, 58 days, "9.7x" the median, above TZ-302 at "6.7x"
  And the popover states that they are longer than 30 days (5 times the median of 6 days) and that the percentiles and View Data include them

Scenario: Nothing is hidden when nothing is far out
  Given a Team whose slowest Work Item is 25 days against a median of 6 days (illustrative)
  When Maria opens the Metrics tab
  Then no outlier chip appears and the chart is drawn as it is today

Scenario: The numbers are unchanged by hiding
  Given TZ-301 is an outlier
  When Maria compares the percentile lines, the SLE share and View Data with and without the feature
  Then they are identical and View Data still lists TZ-301

Scenario: The words are the team's own
  Given the Team renamed Work Item to "Story" and Cycle Time to "Lead Time"
  When Maria opens the chip
  Then the rule sentence and the column headers read "Story" and "Lead Time"

Scenario: The chip works without a mouse
  Given the chip reads "1 outlier"
  When Maria tabs to the chip and presses Enter
  Then the popover opens with focus inside it, and pressing Escape closes it and returns focus to the chip

Scenario: An item exactly at the cut is not an outlier
  Given a median of 6 days and a Work Item closed at exactly 30 days (illustrative)
  When the chart is drawn
  Then that Work Item is drawn and not counted by the chip
```

**Acceptance criteria**

- [ ] Work Items with Cycle Time > 5 x the median (the 50th percentile, Lighthouse's convention, AFK-10) are not drawn; the rest are drawn as today. The 5 is one constant.
- [ ] The chip shows `{n} outlier(s)` with icon and words; absent when n = 0.
- [ ] The popover shows the not-drawn line with the cut and the median in days, the rule sentence, and a table sorted by days descending
  with ID/name (tracker link), closed date, days, "x the median" per D12; first 10 rows then "and k more". The chip count equals the rows when n is 10 or fewer; above 10 it equals the 10 rows plus k.
- [ ] Percentiles, SLE share, tiles, forecast inputs and View Data are unchanged (View Data lists every item).
- [ ] All user-facing words come from the reader's Terminology; "outlier" is the fixed word.
- [ ] Chip and popover meet the keyboard and screen-reader rules above.
- [ ] Teams and Portfolios behave the same.
- [ ] Slice 03: "Open in a list" opens `WorkItemsDialog` with the same outliers; the ⓘ sentence carries the terms.

**Outcome KPIs.** See KPI section.

**Technical notes.** Pure frontend. The calculation is generic (list + value selector) and unit-tested on its own.
The median is the 50th percentile with the backend's convention over the range's items for the Cycle Time shown (D10, AFK-10). The axis is capped at the cut and lines above it become edge markers in the same slice (AFK-6), so the axis numbers in the examples hold from the first release. Size: about 2.5 days over
slices 01 and 03.

### US-6502-02: A percentile or SLE line above the clipped axis is still seen

`job_id: job-flow-coach-read-typical-work-know-the-exceptions`

**Problem.** Tomas Reyes is a delivery lead presenting a Team's chart to a steering group. **This story's figures are
an illustrative fixture, not Team Zenith:** on Zenith's computed numbers the 95% line (15 days) and the {SLE} (7 days)
lie below the cut of 30, so the demo shows no marker (AFK-15). In the fixture, several outliers pull the 95% line to
33 days and the {SLE} is 35 days, while the cut is 30. With the axis capped at the cut, those two lines would fall off
the top. If they vanish, the group reads a chart that seems to have no 95% line; if the axis reopens to hold them, the
squeeze returns.

**Elevator Pitch**
Before: Tomas cannot tell whether a line that is not on the plot is missing, zero or just above it.
After: open the Metrics tab of a Team whose 95% line lies above the cut (illustrative fixture) -> sees a triangle at the top edge labelled "95%: 33 days" and "SLE: 85% @ 35 days or less"; open the chip -> reads "Above the axis: 95% at 33 days, SLE at 35 days."
Decision enabled: Tomas judges the {SLE} and the 95% value against the typical dots without a stretched axis, and can tell the group where those lines are.

**Domain examples**

1. Happy path (illustrative fixture). Median 6, cut 30, highest typical dot 20: the axis ends at 22. The 85% line
   (10 days) is drawn; the 95% line (33 days) and the SLE (35 days) are edge markers.
2. Edge (illustrative). The reader switches the 95% chip off: its marker disappears; the SLE marker stays.
3. Boundary (computed for Zenith's last 30 days without TZ-301, and in every unit fixture without an outlier). No
   outlier exists: no clipping, so no markers; all lines drawn as today. With TZ-301 on Zenith the cut is 30 and the
   highest line is 15, so there is still no marker.

**UAT scenarios**

```gherkin
Scenario: A line above the axis becomes a labelled marker at the top edge
  Given (illustrative fixture) the 95% line is at 33 days and the axis ends at 22 days with the 85% line at 10
  When Tomas opens the Metrics tab
  Then the 85% line is drawn in place and the 95% value appears as a labelled marker at the top edge

Scenario: The popover names the lines above the axis
  Given the 95% line and the SLE are above the axis
  When Tomas opens the chip
  Then it says "Above the axis: 95% at 33 days, SLE at 35 days." (illustrative fixture)

Scenario: Switching a line off removes its marker
  Given the 95% marker is shown
  When Tomas switches the 95% chip off
  Then the marker disappears and the SLE marker remains

Scenario: With no outliers every line is drawn in place
  Given no Work Item is an outlier
  When Tomas opens the Metrics tab
  Then no markers appear and all lines are drawn as today
```

**Acceptance criteria**

- [ ] A line whose value is above the axis top has a marker with a text label (name and value); others do not.
- [ ] The marker's shape and text carry the meaning, not only colour.
- [ ] The percentile/SLE chip toggles show and hide the markers.
- [ ] The popover sentence appears only when at least one line is above the axis.
- [ ] Lines at or below the cut are drawn in place, as today.

**Technical notes.** Line values stay as computed by the backend. Ships in the same slice as the cap (AFK-6); N3 is answered by AFK-3. About 1 day of the merged slice 01. Tested on a fixture population and the humble-object marker spec, not on the demo and not in E2E (AFK-15); the demo E2E and the `@screenshot` show the chip and popover only.

### US-6502-03: The chip stays truthful when the reader changes the view

`job_id: job-flow-coach-read-typical-work-know-the-exceptions`

**Problem.** Maria hides the Bug type to talk about Features only, switches to the "Concept to Cash" Cycle Time, or
widens the date range to a quarter. If the chip kept its old count, or the plot went blank with "No data available"
because the only visible items are outliers, she could not trust it.

**Elevator Pitch**
Before: Maria cannot tell whether the outlier chip describes what is on screen after she changes types, Cycle Time or dates.
After: toggle the Bug type chip, change the Cycle Time selector or the date range -> sees the chip count and popover list change with exactly the visible types, selected Cycle Time and range.
Decision enabled: Maria decides whether an outlier belongs to the Features or Bugs conversation, and trusts the chip as she presents.

**Domain examples**

1. Happy path. TZ-301 is a User Story, TZ-302 a Bug (fixture). Both visible: "2 outliers". Hide Bug: "1 outlier"
   listing TZ-301. The axis does not move.
2. Edge (illustrative; the named Cycle Time is premium and not seeded). Concept to Cash selected: median 40 days, cut 200; the popover names "Concept to Cash".
3. Boundary. Only Bugs visible and both are outliers: the plot shows "Every visible Work Item is an outlier. Open the
   chip to see them." with the legend row and chip intact.

**UAT scenarios**

```gherkin
Scenario: The count follows the visible types
  Given TZ-301 is a User Story outlier and TZ-302 is a Bug outlier
  When Maria hides the Bug type
  Then the chip reads "1 outlier" and the popover lists only TZ-301
  And an untyped outlier is counted as visible, as it is drawn as visible today

Scenario: The axis does not jump when she toggles a type
  Given the axis top is 22 days
  When Maria hides and shows the Bug type
  Then the axis top is still 22 days

Scenario: A named Cycle Time uses its own median
  Given (illustrative) a "Concept to Cash" Cycle Time with a median of 40 days
  When Maria selects it
  Then outliers are those above 200 days and the popover rule sentence names "Concept to Cash"

Scenario: A new date range recomputes the chip
  Given the chip reads "2 outliers" for the last 90 days
  When Maria widens the range to 180 days
  Then the median, the chip and the list reflect the 180-day Work Items

Scenario: Everything visible is an outlier
  Given only the Bug type is visible and both visible Bugs are outliers
  When Maria views the chart
  Then it says "Every visible Work Item is an outlier. Open the chip to see them." and keeps the legend row and chip

Scenario: The chip never shows an old answer while loading
  Given the chip reads "2 outliers" and its popover is open
  When Maria changes the date range and the new data is loading
  Then the popover closes, the widget is dimmed with a spinner, the chip is disabled, and the old count is not presented as current

Scenario: Hiding every type says so
  Given Maria hides every Work Item type
  When she views the chart
  Then it says "All Work Item types are hidden. Switch one on in the legend." and keeps the legend row with its type chips
```

**Acceptance criteria**

- [ ] Count and rows cover only visible Work Item types (an untyped item counts as visible); median and axis ignore the type chips (D10).
- [ ] Named Cycle Time: the calculation runs on that value and the sentence names the definition.
- [ ] Date range change recomputes everything from the new range's items.
- [ ] All-outliers state and all-types-off state as worded (AFK-12); the legend row is not removed in either.
- [ ] Loading shows the widget's dimmed state; failure shows its could-not-load message; no stale count. Whenever the widget leaves "ready" the chip is disabled and an open popover closes (AFK-11). The chart's own "No data available" never shows while the widget is loading.

**Technical notes.** `useChartVisibility` supplies the visible types. The existing "all types off hides the legend"
trap is now in scope (AFK-12): the legend-row condition is rewritten for the all-outliers state anyway. About 1 day
(slice 02).

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Who | Does what | Target | Measured by | Baseline |
|---|---|---|---|---|---|
| K1 | A first-time reader of the demo Team's scatterplot | Says what the plot leaves out and where to find it | 4 of 5 readers within one minute, unprompted by the author | Observed walk-through, maintainer plus 4 colleagues, after slice 01 ships | Unknown; today the answer is "nothing is left out" and the plot is squeezed |
| K2 | Flow coach presenting the chart | Is not asked "what is that dot / what did you leave out" | 0 such questions in the maintainer's next 3 reviews | Maintainer's own reports | Asked regularly (story text; no count) |
| K3 | Readers of Teams with an outlier | Open the outlier popover | At least 1 in 3 Teams with a chip has the popover opened within 30 days | Opt-in usage-data event, if DEVOPS adds it | None |
| K4 | The product | Keep every number unchanged | 0 differences in percentiles, SLE share, tiles, View Data between before and after | Existing tests plus the wiring-point test on a mounted chart (DESIGN section 6) | n/a |

K3 depends on the DEVOPS usage-data decision; K1, K2 and K4 do not. All estimates remain estimates.

## Wave: DISCUSS / [REF] Definition of Ready (9 items)

| # | Item | Evidence |
|---|---|---|
| 1 | Problem in domain language | Three stories open with Maria or Tomas and a concrete squeezed chart |
| 2 | Persona | Maria Santos (flow coach), Tomas Reyes (delivery lead) |
| 3 | 3+ domain examples with real data | US-01 has four. Real data: Team Zenith with TZ-301, last 30 days, **computed** (50% line 6, cut 30, 95% line 15, SLE 85% within 7 days, 58 days = 9.7x; table "Computed Team Zenith numbers"). Not real data, labelled so: the exact-cut edge on a median of 6 (illustrative), the demo Portfolios (read at DISCUSS from the demo data, not recomputed), the renamed terms (wording example). **US-02 cannot use real data:** on Zenith no line lies above the cut, so its three examples are an illustrative fixture or a computed no-marker boundary (AFK-15). US-03 uses fixtures TZ-301 (computed) and TZ-302 (illustrative) plus the named "Concept to Cash" Cycle Time (premium; not seeded; illustrative). Every figure is labelled computed or illustrative |
| 4 | UAT in Given/When/Then, 3-7 | US-01: 7, US-02: 4, US-03: 7 |
| 5 | AC derived from UAT | Checklists above map one to one |
| 6 | Right-sized | US-01 about 2.5 days over slices 01 and 03, US-02 about 1 day inside slice 01, US-03 about 1 day (slice 02); 4 to 7 scenarios each |
| 7 | Technical notes | Per story; pure frontend; reuse list in the sketch |
| 8 | Dependencies resolved or tracked | N1, N2, N3 answered by AFK-1, AFK-2, AFK-3; the Zenith demo item (AFK-14) is a DELIVER step in slice 01; the spike (AFK-9) gates the edge-marker form only |
| 9 | Outcome KPIs | K1 to K4 |

Also: `job_id` set on every story (new job proposed, entry text above); Elevator Pitch present on every story.
Status: **re-checked in review round 3: DoR met, with item 3 qualified.** US-01 meets it on computed real data; US-02 meets "3+ examples" only with an illustrative fixture because the demo cannot show a marker (stated, not hidden; AFK-15); US-03 mixes computed and illustrative figures, all labelled. The invented Ocean Explorer Team item is gone; N1 to N3 are answered by AFK-1 to AFK-3. The sketches are still walked at the start of DISTILL (AFK-8).

## Wave: DISCUSS / [REF] Out of scope, WS strategy, driving ports, pre-requisites

- **Out of scope:** any chart other than the Cycle Time scatterplot (Estimation vs Cycle Time is the cheapest next
  adopter, its own story); changing View Data; a "Show on chart" action or stored switch (decision Q5); a
  user-tunable multiple; disclosing outliers hidden by the reader's type chips;
  charts in reports. (The all-types-off legend trap is in scope after all: AFK-12.)
- **WS strategy:** C-style, real I/O from the existing metrics endpoints; the walking skeleton is slice 01 (chip, cap and
  edge markers) on the real Team Metrics tab. No new external dependency.
- **Driving ports:** the Metrics tab of a Team and of a Portfolio (the Cycle Time widget: chip, popover, "Open in a
  list").
- **Pre-requisites:** an outlier in the demo data (TZ-301 on Team Zenith, added in DELIVER, AFK-14); the proposed jobs.yaml entry added; the DISTILL sketch
  walk-through; `visualization` skill rules apply in DESIGN and DISTILL.

## Wave: DISCUSS / [REF] Proposed shared-doc edits (not made)

1. `docs/product/jobs.yaml`: the new job entry above.
2. `docs/product/architecture/adr-270-...md`: the Outlier glossary row above (Flow metrics section).
3. `ARCHITECTURE.md` section 4: one line under Flow metrics.
4. `docs/.../metrics/flow-metrics.md`: the "how to read it" passage, at finalize.

## Wave: DISCUSS / [REF] New open questions

- **N1. A floor for a tiny median?** (Answered: AFK-1, no floor.) A median of 1 day gives a cut of 5 days, so a 6-day item is an outlier although
  the plot is not squeezed. Options: (a) no floor; (b) the cut is never below a fixed number of days (for example
  14); (c) clip only when the largest item is at least 1.5 x the cut. *Recommend (a) now,* check the demo data in
  DESIGN; (b) adds a second sentence and a magic number, (c) adds a second rule.
- **N2. Which items form the median?** (Answered: AFK-2.) Options: (a) every closed Work Item of the date range for the Cycle Time
  shown, regardless of type chips (D10); (b) only the visible types, so the cut and axis move when a type is toggled.
  *Recommend (a):* percentile lines are computed over all items too, and the axis stays still.
- **N3. Lines above the clipped axis.** (Answered: AFK-3, merged into one slice by AFK-6.) Options: (a) labelled edge markers (D11); (b) the axis stays open
  for every line (slice 01 rule kept), at the price of a taller axis on most teams because the 95% line usually sits
  at or above the cut. *Recommend (a);* settle with the sketch at DISTILL.

## AFK decisions (2026-11-02, orchestrator; the maintainer reviews them on return)

| # | Decision | Why |
|---|---|---|
| AFK-1 (N1) | **No floor** for a tiny median. | A floor is a magic number and a second rule; N is one constant and easy to change. DESIGN checks the demo data for a tiny-median team. |
| AFK-2 (N2) | **The median is computed over every closed Work Item of the date range for the Cycle Time shown, regardless of the type chips.** Only the chip count follows the visible types. | Same population as the percentile lines; the axis does not jump when a type is toggled. |
| AFK-3 (N3) | **Labelled edge markers** for lines above the clipped axis (slice 02); the final look is settled with the sketch at the start of DISTILL. | Keeping the axis open for the 95% line would undo the point of the Story on most teams (the 95% line sits at about 5.2× the median, at or just above the cut). |
| AFK-4 (JTBD) | **The new job `job-flow-coach-read-typical-work-know-the-exceptions` is accepted as proposed**, added to `jobs.yaml` in DELIVER. | `grep -n -i "outlier\|legib" docs/product/jobs.yaml` finds only `job-flow-coach-spot-pace-outliers` (Work Item Aging, a different job) and unrelated log-legibility notes. |
| AFK-5 (review) | **The DISCUSS peer review is folded into the DESIGN review** rather than run separately. | Time; the DIVERGE review's findings were addressed in this DISCUSS and the DESIGN reviewer is told to check them. |

## Wave: DESIGN / [REF] State, method, reuse (2026-11-02, PROPOSE mode, maintainer AFK)

Binding inputs: the maintainer decision tables and the AFK decisions above. Scope: application level, frontend only.
Style: unchanged (React 19 + MUI 9, `utils` pure modules, `components/Common/Charts`, page-level status in
`pages/Common/MetricsView`). Wave state: **DESIGN revised after review round 1 (NEEDS_REVISION: B1, M1 to M9 and m1 to m12 addressed; AFK-10 to AFK-14 folded in); round 2 pending.**
Draft ADR: `adr-282-draft-outlier-is-a-per-chart-median-multiple.md` (this folder).

Quality drivers, in order: (1) no number silently changes, (2) N changes in one place, (3) a second chart can adopt
the rule without touching the first, (4) a slow connection never shows a stale count or an empty-looking plot,
(5) testable without a browser. Team/Conway: one maintainer plus agents, nothing to flag.

Not verified in this worktree: it has **no `node_modules`**, so the installed `@mui/x-charts` (^9.16) source could
not be read. Everything said about how MUI X treats out-of-domain values and `ChartsContainer`'s `margin` is from
the library's documented behaviour and is marked **[spike]**: the first DELIVER step proves it in a browser, against
the pass and fail criteria in section 2. The only MUI X hooks this design relies on are `useDrawingArea`, `useXScale`
and `useYScale` from `@mui/x-charts/hooks`, which this repository already imports (`BlackoutOverlay.tsx:2,13`,
`PbcBlackoutOverlay.tsx:2`, `TimeBlackoutOverlay.tsx:2`) at the installed range `^9.16.0` (`package.json:33`); no
other hook or prop is assumed.

### Reuse analysis

| Existing piece | Verdict | Contract shape / universe / assertion |
|---|---|---|
| `getMaxYAxisHeight` (`utils/charts/chartAxisUtils.ts:19-43`) | **Reuse unchanged.** It returns 1.1 x the largest of the data values, every percentile and the SLE it is given (the scatterplot passes all of them, ticked or not, as today). `axisTop` delegates to it. Feature Size and Estimation callers are untouched. | pure-function; no universe; existing tests |
| `CycleTimeScatterPlotChart` | **Modify.** Two `useEffect`+`setState` derivations (axis, grouped dots) become `useMemo`, so chip count, axis and dots come from one render. | **bounded-change.** Universe: the component's own state only: `selectedDefinitionId`, the tagged named percentiles `{definitionId, fetcher, percentiles}`, the dialog state `{title, items}`, plus `visibleTypes` from `useChartVisibility`. Delta of this feature: the two derivation effects and their states are removed (derived with `useMemo`); the dialog state gains a title; the named-percentiles state gains its tag; it gains one boolean prop, `frameIsReady`, handed on to the chip (AFK-16). It never mutates its props (tests pass deep-frozen props); it hands the unpartitioned arrays to the dialog, the lines and the ⓘ, and the partition only to its own drawing and the chip. Assertion: RTL with the existing MUI mocks, a frozen-props test, the statistics wiring-point test and the 50%-line test (section 6) |
| `LegendChip` | **Not reused** (its `aria-pressed` and "visibility toggle" name are toggle semantics). Its look is the model; a new `OutlierChip` uses MUI `Chip` outlined. | n/a |
| `PercentileLegend`, `useChartVisibility` | **Reuse unchanged.** | |
| `WorkItemsDialog` + context `closedItems` | **Reuse unchanged** for "Open in a list". The chart's single dialog gets one state `{title, items}` instead of items-plus-derived-title. | **pure-function presentation.** For this feature it is handed `{title, items, context}` and renders them; the feature adds no state, effect or method to it and passes it only data. Universe: none (its own sort and export UI state exist today and are untouched). Assertion: existing tests, plus the mounted-chart check that the dialog opened from the chip or from a dot receives the effective copies unchanged |
| `InfoPopoverButton` | **Pattern reused** (MUI `Popover`, `role=dialog`, `aria-expanded`); not the component (its body is text only). | |
| `widgetInfoMetadata.getWidgetInfo` | **Modify.** `WidgetInfoTerms` gains `cycleTime`; the `cycleScatter` description is built from terms for both owner kinds (today only `team` gets term-built wording). | pure-function |
| `PercentileCalculator` (backend, `PercentileCalculator.cs:8-10`) | **Not changed, mirrored (AFK-10).** The rule's median is its 50th percentile. The frontend `fiftiethPercentile` reproduces its index (`floor(p/100 x N) - 1`, clamped) and a table test pins the two together. | pure-function; no universe; table test for N = 1 to 6, 10 and 11 |
| `utils/charts/paceBands.ts`, `sleRisk.ts` | **Precedent** for pure chart modules with term-injected wording. `outliers.ts` and `outlierWording.ts` follow it. | |
| `WidgetShell` / `WidgetBody` status | **Reuse unchanged.** The chart is a pure view of its props, so loading and failure need no new code. | |
| E2E `CycleTimeScatterPlotChart` page object | **Extend** with the chip and popover. | |
| `utils/charts/outliers.ts` (new: `fiftiethPercentile`, `findOutliers`, `axisTop`, `timesTheMedian`, `formatTimesTheMedian`) | **New**; no existing alternative (the frontend has no median or outlier code; `PercentileCalculator` is backend). | **pure-function.** Universe: none; arguments are read-only (deep-frozen in tests), results are new arrays. Assertion: fast-check properties (section 6) and Stryker 80% |
| `utils/charts/outlierWording.ts` | **New**, following `paceBands.ts` and `sleRisk.ts`. | **pure-function**; terms in, strings out; no universe. Assertion: unit tests and the constant-versus-copy test |
| `utils/charts/offScaleLines.ts` (`splitLinesByAxis`, `markerSpecs`) | **New**, the pure companion of the marker component. | **pure-function.** Assertion: properties: drawn + off-scale = all lines, order kept, a line is off scale iff its value is above the top |
| `OffScaleLineMarkers` | **New**, thin SVG component. | **bounded-change.** Universe: the chart's SVG subtree it is mounted in. Delta: one `<g aria-hidden="true">` with one `<text>` row per off-scale line in the reserved top margin, nothing when there is none; no state, no effect, no write outside its subtree; reads only `useDrawingArea`. Assertion: RTL on the spec output with the hook mocked (precedent `BlackoutOverlay.test.tsx:30`); the markers' placement is asserted on the spec output against the mocked drawing area, with no E2E geometry check (AFK-15) |
| `OutlierChip` | **New.** | **bounded-change.** Universe: its own open flag (one boolean) and its anchor element; nothing else. Delta: opens and closes its popover; focus returns to the chip on close; closes when `disabled` turns true (AFK-11). **No service access:** it imports no service, query, router or page code; everything it reaches outside arrives as props, declared **effect ports**: `onOpenInList`, `onClose` and the usage-event callback (DO-1, called once per closed-to-open change). Never mutates `rows`. Assertion: RTL: open and close, focus return, `disabled` closes, 10-row cap, each effect port called exactly when specified and nothing else imported (the layer rule, "Hexagonal view, enforcement, handoff") |
| `OutlierPopoverContent` | **New.** | **pure-function**: a pure view of props (rows, wording strings, callbacks) to markup; no state, no effect of its own, no service access. The callbacks it receives are **effect ports**, the only way it reaches outside and called by a user action only: `onOpenInList` and `onClose`. Universe: none. Assertion: RTL role queries (the DISCUSS screen-reader criteria) and a spy per effect port |
| Driving ports that only read | None added. The chip, popover and markers expose no write method; the only callbacks are `onOpenInList`, `onClose` and the usage-event callback, all handed in as props. | n/a |
| Unbounded-preservation (must return a Plan) | **None in this feature.** No dry-run, preview or validate function exists and nothing writes outside the component tree. Stated so the crafter does not invent one. | n/a |

## Wave: DESIGN / [REF] Decisions (options, recommendation, choice)

### 1. Where the calculation lives

| Option | For | Against |
|---|---|---|
| **A. Pure module `src/utils/charts/outliers.ts` (chosen)** | No React; reusable by any chart; trivial PBT; follows `paceBands.ts`; the layer rules already keep it clean | none material |
| B. `useOutliers` hook | Memoises | Nothing stateful to hide; ties PBT to React |
| C. Backend flag on the DTO | One place for all clients | Maintainer: frontend, per chart; API churn; client bump |
| D. `IOutlierPolicy` strategy | Extensible | One rule; the accessor already is the strategy (YAGNI) |

**Interface contract (no bodies).**

- `OUTLIER_MEDIAN_MULTIPLE = 5`, the only place the 5 lives.
- `fiftiethPercentile(values)` is **the one rule for "the median"** (AFK-10): sort a copy ascending, index =
  `floor(0.5 x N) - 1`, clamped to `0..N-1`, exactly `PercentileCalculator.CalculatePercentile(items, 50)`. It
  returns one of the input values, or `null` for an empty list. Its header comment says it mirrors the backend
  convention, so a reader who changes one finds the other; a table test pins the two. Contract shape:
  **pure-function**.
- `findOutliers(items, valueOf)` returns `{ median, cut, outliers, typical }`, with `median` the
  `fiftiethPercentile` of the finite values. `median` and `cut` are `null` when the rule does not apply (no items,
  or median not above 0, or no finite values). `outliers` are the items whose value is **strictly greater than**
  `cut`, largest first (ties keep input order). `typical` is every other item in input order (a non-finite value is
  typical: never an outlier, never in the median). Together they partition the input; the input is never mutated.
  Contract shape: **pure-function**. There is deliberately no `multiple` parameter: "N is one constant".
- `axisTop({ typicalValues, lineValues, cut, hasOutliers })` returns the y-axis maximum. **No outliers: exactly
  today's `getMaxYAxisHeight` over all values and all lines**, so a chart without an outlier is unchanged byte for
  byte. **With outliers:** `min(1.1 x max(typicalValues, and the lineValues that are <= cut), cut)`. `lineValues` are
  every percentile and the SLE of the data shown, ticked or not, as the axis does today. It delegates the `1.1 x max`
  to `getMaxYAxisHeight`, so `outliers.ts` imports that one sibling util and nothing else. Contract shape:
  **pure-function**. Why the cap: section 2.
- `timesTheMedian(value, median)` and `formatTimesTheMedian(value, median)` returning the short text (`"9.7x"`,
  `"11x"`) and the spoken text (`"9.7 times the median"`).
- `outlierWording.ts` (pure, terms passed in): the rule sentence, the ⓘ short form, the popover lines, the chip
  label and accessible name (with singular and plural), the dialog title. Every sentence that quotes N reads the
  constant. Added here because the sentence is used in three places (popover, ⓘ, dialog title): one piece of
  knowledge, one definition.

**Factor formatting (fixes D12's ambiguity).** Work in tenths: `tenths = ceil(value * 10 / median)`, one division,
no float arithmetic on a rounded factor (the median is one of the item values, so for whole days both operands are
integers and the division is exact; 51 against 10 reads 5.1 and never 5.2). If `tenths < 100`: one decimal,
rounded up, so a flagged item never reads "5.0x". Otherwise: a whole number rounded **half-up** from the exact ratio (AFK-17; in integers
`floor((2 x value + median) / (2 x median))`): 63 against 6 is 10.5 and reads "11x", 62 against 6 is 10.3 and reads
"10x", and `9.96` has `tenths = 100`, so it reads "10x", never "10.0x". Pinned in the property test. The spoken text is the same figure plus "times the median".
Property: for every outlier the figure read back is greater than N, and it never decreases as the value grows.

**Median** = `fiftiethPercentile` (AFK-10): an item value in whole days, shown as such, and the number behind the
"50%" line when the page shows the same population. There is no second definition of "median" in the product, so
the text never has to explain two.

### 2. How the scatterplot uses it

Order of computation inside one render (all `useMemo`, no new effects):

1. `population` = `effectiveDataPoints` (the named value when a definition is selected; items without it are not
   in the population, as today). **All types**, so the cut and the axis stay still when a type chip is toggled
   (AFK-2).
2. `analysis = findOutliers(population, item => item.cycleTime)`.
3. `lines` = one descriptor list built once from the visible percentiles and the SLE: `{key, value, label, color,
   style}`. The label text is defined once and used both for the in-range reference line and for its edge marker.
4. `yMax = axisTop({ typicalValues, lineValues, cut, hasOutliers })` (the pure function in the contract above). With
   no outlier, including `cut === null`, it is today's `getMaxYAxisHeight` over everything, byte for byte.
5. `lines` split by `value <= yMax`: those go to `ChartsReferenceLine` as today; the rest become edge markers.
6. Drawn dots = typical items of the **visible types**, grouped as today. Chip and popover = outliers of the
   visible types (D5); an item without a type counts as visible for the chip, exactly as the chart already treats it
   for the dots (`if (!itemType) return true`). Median, cut and axis ignore the type chips.
   **One filtered array (index alignment).** The grouped, visible, typical array is computed once with `useMemo` and
   is the only array behind the series `data`, the marker colours and the tooltip and click lookups, which all
   address a group by its array index. A second filter anywhere would misalign them; a test checks that the group
   under a hovered or clicked index is the group drawn there.
   **The x-axis keeps its domain from all items (AFK-13)**, outliers included: an outlier's closed date lies inside
   the selected range, so only the y-axis is distorted. The x-domain therefore reads `effectiveDataPoints`, not
   `typical`.

**Why the cap at the cut (a DISCUSS gap, see issues).** Without it the axis top is `1.1 x` the largest typical
value, which can exceed the cut, so an outlier of 32 days would sit below an axis ending at 33 and be "not drawn"
in empty space, while the popover says it is not drawn. With the cap every outlier is above the axis, and "not
drawn" is exactly "above the plot". Cost: a dot at exactly the cut touches the top edge when the largest typical
value is above `cut / 1.1`. Accepted. The cap is a stated property of `axisTop` (section 6): every typical value and
every drawn line is at or below the top, every outlier is above it, and the top never exceeds the cut.

**Filter, not clip.** Outliers never reach the chart library. Reasons: scatter markers and reference lines are not
clipped by the axis (**[spike]**), each marker is a focusable button and a tooltip target, so an off-screen marker
would be reachable by keyboard and by tooltip.

**Edge markers.** Not via `ChartsReferenceLine` (an out-of-domain `y` is drawn above the plot, over the legend
**[spike]**). A small custom SVG layer inside `ChartsContainer`, in the top margin above the plot:

- `OffScaleLineMarkers` = a thin component using `useDrawingArea` (from `@mui/x-charts/hooks`, already used by
  `BlackoutOverlay.tsx:2,13`; the name is confirmed in this repository) for the right edge, plus the **pure**
  `utils/charts/offScaleLines.ts`, which splits the lines (value at or below the top: drawn; the rest: off scale) and
  turns the off-scale ones into ordered marker specs (text, colour, order). The pure part is unit tested; the thin
  part by an RTL test with the hook mocked (precedent: `BlackoutOverlay.test.tsx` mocks the same module; the mock
  list in `CycleTimeScatterPlotChart.test.tsx` gets the one extra hook), on a fixture population that has a line above the cut (the demo has none, AFK-15); there is
  no E2E geometry check. The marker group is `aria-hidden="true"`: the lines are exposed once, by the popover sentence.
- One right-aligned `<text>` per row of marker, built from `<tspan>`s: a filled triangle glyph coloured as its line,
  then the text `95%: 33 days`, `SLE: 85% @ 35 days or less` (illustrative fixture figures). Flowing text means no width measurement and no
  overlap between markers. Shape (triangle) and words carry the meaning; colour only repeats the line's colour.
- Reserve the top margin only when any line is off scale for the data (type- and visibility-independent), so the
  plot does not jump when a percentile chip is unticked.
- Phone width fallback: drop the value and keep the label (`▲ 95%  ▲ SLE`); the popover carries the values. To be
  checked at the DISTILL sketch review.

**Spike (DELIVER step 0): pass and fail criteria, and the fallback.** Unverified: that a custom SVG layer can draw
in a reserved top margin of the chart container, in both themes and at phone width. The spike is a manual DELIVER step
on a throwaway fixture page (a population with a line above the cut, because the demo has none, AFK-15); its result goes
in the delivery log and no committed E2E test keeps the geometry. **Pass**, all in a real browser
at 1280 px and 375 px, light and dark: (1) with the container's top margin raised by the marker rows, a `<text>` row
placed above `useDrawingArea().top` is fully visible, its bounding box inside the `<svg>` box; (2) the plot's
height is the same with 0 and with 2 marker rows (apart from the reserved margin), and no dot or line moves when a
percentile chip is ticked; (3) no marker box intersects the plot or the legend row's box; (4) the longest label
(`SLE: 85% @ 35 days or less`, illustrative) stays inside the SVG at 375 px, or the phone fallback below truncates it to fit.
**Fail** = any of the four. **Fallback on fail (named, designed now): the marker row moves into the legend row** as
non-interactive text after the percentile chips (`▲ 95%: 33 days  ▲ SLE: 85% @ 35 days or less`, illustrative figures, in the line
colours, `aria-hidden`), wrapping with the row on a phone; no SVG layer, no margin, no hook. Cost: the marker is no
longer at the plot's top edge, so the popover sentence carries more. Only `OffScaleLineMarkers`' home changes; the
pure modules, the cap and the spec tests stay. Keeping the axis open for every line is **not** a fallback. ADR-282
records both.

**Stale named percentiles (found at DESIGN).** `namedPercentiles` keeps the previous definition's lines until the
new answer lands, and edge markers and the axis now read them. Fix in the same slice, small: store
`{definitionId, fetcher, percentiles}` and use the lines only when both the id and the fetch callback match the
current selection, otherwise none. A keyed TanStack query was weighed: it would be complete, but it needs the fetch
to move up out of the chart. The earlier rationale ("the chart cannot see the owner id or date range") was wrong:
`onFetchNamedCycleTimePercentiles` (`BaseMetricsView.tsx:1464-1473`) already closes over `entity.id`, `startDate`
and `endDate` and takes only `definitionId`. So a tag of the definition id alone is **not** complete (an answer for
definition 7 fetched for the previous date range would match a later selection of 7). The tag therefore also carries
the callback's identity, which changes exactly when its `useCallback` dependencies change, so it is as complete as a
key. **Deviation from the project's keyed-query norm**, recorded in ADR-282: chosen because scoped percentiles
already live in component state (`BaseMetricsView.tsx:1475-1480`, another widget) and moving the fetch is outside
this Story. A test covers a late answer for the old date range with the same definition id.

### 3. Chip and popover

| Piece | Home | Notes |
|---|---|---|
| `OutlierChip` (button + popover state) | `components/Common/Charts/OutlierChip.tsx` | MUI `Chip` `variant="outlined"`, icon + `{n} outlier(s)`; neutral colours, not the SLE or percentile hue; `aria-haspopup="dialog"`, `aria-expanded`, accessible name `{n} outlier(s) not drawn on the chart. Show details.` (contains the visible text, WCAG label-in-name); `data-testid="outlier-chip"`. Prop `disabled` (= not `frameIsReady`): true whenever the widget status is not "ready" (AFK-11); the chip then cannot be opened and an open popover closes (the chip's one effect). The page passes the flag down through the chart as a plain boolean (`frameIsReady`, AFK-16; see "Where `frameIsReady` comes from" below); the chart imports no page code |
| `OutlierPopoverContent` | same folder | title, three lines, conditional "above the axis" line, table, overflow row |
| rows | built by the chart from outliers of visible types | `{item, valueDays, closedDate, timesText, spokenText}`; first `OUTLIER_POPOVER_ROWS = 10`; then `and {k} more` beside `Open in a list` |
| list | the chart's existing `WorkItemsDialog` | one state `{title, items}` shared by dot clicks and the chip; `context="closedItems"`; items are the effective copies, as the dot click passes them, so a named Cycle Time shows its days; the popover closes first; focus moves to the dialog, and on its close returns to the chip if it is still mounted, else to the widget heading |

Mounted in the legend row, right group, **after the type chips**. Two changes to that row: the right group renders
when there are types **or** an outlier; and the row is shown whenever the chart holds any item (before the type and
outlier filters), not while a dot is drawn, so the all-outliers and the all-types-off states keep their chips
(AFK-12). The
plot area says `Every visible {Work Item} is an outlier. Open the chip to see them.` when visible items exist and
all are outliers, and `All {Work Item} types are hidden. Switch one on in the legend.` when every type is off,
instead of "No data available", which stays only for a chart with no items at all.

The chip is a presentational component with plain props (rows, wording strings, callbacks); it fetches nothing, so
it needs no capability beyond what it is handed. It is not generic beyond that on purpose: the calculation is the
reusable part; make the chip generic when a second chart needs it. Layer rules hold: `components` imports `utils`
and `models` only.

Popover copy changes proposed at DESIGN (settle at the DISTILL sketch walk-through): line 1 becomes
`Not drawn. They are longer than {cut} days (5 times the median of {median} days).` instead of "The axis ends at
{top} days", because the rule is about the cut and the axis top is now an implementation detail; the median shown is
the "50%" line's number (AFK-10), so the two can never disagree. Accepted provisionally (AFK-8), gated by the
DISTILL sketch walk.

### 4. Interactions

| With | Behaviour |
|---|---|
| Type chips | Chip count, popover rows and list: visible types only. Median, cut, axis: all types. Toggling a type changes only the count and the dots. No live region added; confirm with a screen reader in DELIVER |
| Date range | Items arrive already limited to the range; everything is derived in render, so it recomputes with the new items. No local copy to go stale |
| Named Cycle Time | Population, median, cut, rows and sentence all use the named days and the definition's name; items outside the definition are not in the population (not on the chart either). Percentile lines come from the id-tagged named answer |
| ⓘ | `cycleScatter` description gains the short sentence, built from terms for Teams and Portfolios; Learn More unchanged |
| View Data, percentiles, SLE share, RAG, tiles, forecasts | Untouched: they never receive the partition (D8). K4 is asserted by existing tests plus the **wiring-point test** (section 6): a mounted chart given the full population hands the page's percentile, SLE and View Data inputs on unchanged, and draws plus marks exactly the lines it received |
| Portfolio | Same component, same copy; the chart already says "Work Items" for Features, so DESIGN follows the chart's word rather than fixing that drift here; Portfolio coverage is by unit tests only (no seeded Portfolio has an outlier; E2E stays thin) |
| Widget status leaves "ready" | The page passes `frameIsReady=false`; the chip is disabled and an open popover closes (AFK-11). Test: render ready with the popover open, rerender not ready, popover gone |

**Where `frameIsReady` comes from (AFK-16).** `frameStateOf(widgetKey, fetchStates, isCommitPending)`
(`BaseMetricsView.tsx:1286-1295`) already computes the status the `WidgetShell` frame shows, and the render uses it for
every placed widget (`:1996`). `BaseMetricsView` evaluates it once more for the `cycleScatter` key,
`frameStateOf("cycleScatter", fetchStates, isCommitPending).status === "ready"`, and passes the boolean into
`buildWidgetNodes` through its `ctx` (the call at `:1732`; the node is built at `:1111-1121`), which hands it to
`CycleTimeScatterPlotChart` as `frameIsReady`. The chip and the frame therefore read one definition of "ready" and cannot
disagree. The prop is named `frameIsReady`, not `isCurrent`: `BaseMetricsView.tsx:1492` already declares a local
`isCurrent` (a stale-response guard), and two meanings of one name in one file would mislead. **Considered and
rejected: the `WidgetStatusReporterContext` channel** (`widgetStatus.ts:119-143`). It carries status from a chart up to
its frame (chart to frame); this need runs the other way (frame to chart), so using it would need a second context
running the opposite direction, plus a provider, for one boolean. A prop is the smaller, testable seam.

### 5. Demo data

Checked by reading `Lighthouse.Backend/Factories/DemoData/*.csv` (Team Zenith is **computed** by a one-off script, table "Computed Team Zenith numbers"; the other rows are hand estimates from the CSV, not run; a DELIVER test pins Zenith against its own 50th percentile):
**no seeded Team or Portfolio has a closed item above 5 times its median.**

| Source | Median (approx.) | Largest | Cut | Outlier? |
|---|---|---|---|---|
| Team Zenith (201 closed before TZ-301; computed, replaces the earlier hand estimate) | 6 (7 over the last 90 days) | 20 over the last 30 days | 30 (35 over the last 90 days) | no, before TZ-301 |
| Team Pulsar | about 10 | 24 | about 55 | no |
| Team Gravity | about 8 | 23 | about 40 | no |
| Team Voyager | about 5 | 14 | about 25 | no |
| Team Lightspeed (about 100 closed) | 1 | 5 (two items) | 5 | no: 5 is not more than 5. It is the tiny-median case (AFK-1): the plot spans 1 to 5 days either way |
| Team Equinox | 3 closed items | | | n/a |
| Portfolios (Epics 14 to 45 days) | 30 to 38 | 45 | 150+ | no |

**Demo-data change (AFK-14), a DELIVER step in slice 01.** Add one closed item to **Team Zenith** (the
"GoodThroughput" demo team: a healthy team with one exceptional item is the story), in
`Lighthouse.Backend/Lighthouse.Backend/Factories/DemoData/Team Zenith.csv`:

- **Id `TZ-301`.** `TZ-204` is taken (`Team Zenith.csv:206`, a Next-state item), and so are `TZ-205` and `TZ-206`;
  `TZ-300` sits at line 42. The file holds `TZ-001` to `TZ-206` plus `TZ-300` and nothing from `TZ-301` up (searched in
  the file and in the backend sources). `DemoDataReferenceIdUniquenessTest` guards it.
- **The row**, in the file's column order (`ID,Name,State,Type,Parent,StartedDate,ClosedDate,Tags,StateEnteredDate,
  Story Points`): `TZ-301,Legacy data store migration,Done,User Story,,{w-45},{w-4},,,5`. **No Parent**, so no
  Portfolio number moves (`TZ-038`, also Done, has none either).
- **Units and counting.** The `{w-N}` placeholders count **working days** before the seeding day
  (`DemoDataFactory.ReplaceDatePlaceholders`: weekends are skipped), so the item starts 45 and closes 4 working days
  before it. The Cycle Time the chart shows is **calendar days, inclusive of both ends: closed day minus started day
  plus 1** (`WorkItemBase.GetDateDifference`, weekends included). 41 working days are 57 or 59 calendar days apart
  (59 when the closing day is a Monday), so TZ-301 reads **58 or 60 days** depending on the weekday of seeding.
  Tests assert the chip, the id and "more than 5 times", never the exact days or ratio (58 days reads "9.7x" and
  60 days "10x" against the 50% line of 6).
- **Median and cut.** Zenith's 50% line is 6 days and its cut 30 over the last 30 days (computed; 7 and 35 over the last 90 days; by
  AFK-10 the median is the item at index `floor(0.5 x N) - 1` of the sorted Cycle Times). A DELIVER test pins that
  TZ-301 is above 5 times Zenith's own 50th percentile instead of pinning a number.
- **Accepted shift in Zenith's statistics** (so DELIVER can update the tests that pin them): the closed total +1;
  throughput +1 in the week of the closing day and arrivals +1 in the week of the start; each percentile line (50%,
  70%, 85%, 95%) can move up by at most one rank, because the new item is the largest and `floor(p/100 x N) - 1` for
  N+1 can pick the next-higher sorted value (the 95% line is the likeliest to move); the SLE share can move by at
  most one item in N; any estimation-versus-Cycle-Time correlation moves slightly (a 5-point item with a long Cycle
  Time). WIP and work item age do not move (the item is Done); no Portfolio, forecast or Parent roll-up moves.
  DELIVER greps the backend tests, the E2E tests and the demo screenshots for Zenith counts and percentiles before
  editing them.
- Then run `DemoDataFactoryTest`, `DemoDataReferenceIdUniquenessTest` and `DemoDataServiceTest`; confirm on the
  running demo that the default date range includes the closing day and that the chip reads "1 outlier". The
  screenshot used by the docs and the website is regenerated at finalize, because the Zenith scatterplot will now
  show a chip. Portfolios stay without a chip in the demo; the Portfolio path is covered by unit tests only (E2E
  stays thin).

### 6. Test strategy (high level; scenarios belong to DISTILL)

| Level | What | Notes |
|---|---|---|
| Unit + property (Vitest) | `outliers.ts`, `outlierWording.ts`, the marker-spec function, the lines split | **Properties of `findOutliers`:** partition (disjoint, complete, order kept); every outlier above and every typical item at or below `N x median`; the median is one of the input values and equals the backend convention (a pinned table of lists for N = 1 to 6, 10 and 11, by `floor(0.5 x N) - 1`); invariant under scaling by a whole `k > 0` and under permutation of the input; no outliers when the median is not above 0 or the list is empty; adding an item that was already an outlier keeps the others typical or outliers as before; the figure read for any outlier exceeds N and never decreases; from 10 up it is rounded half-up (AFK-17: 63 against 6 reads "11x", 62 against 6 reads "10x"); input deep-frozen and not mutated. **Properties of `axisTop`** (generated typical values, lines and cut): every typical value is at or below the top; every drawn line (value at or below the top) is at or below it, and every line above it has a value above the cut; every outlier is above the top; the top is at or below the cut whenever outliers exist; with no outliers the top equals `getMaxYAxisHeight` over all values and lines. **Property of the split** (`splitLinesByAxis`): drawn + off scale = all lines, order kept. `typical` and `outliers` reunite to the input as a multiset (a property of the function only). **Statistics are covered at the wiring point, not by a pure-function property (K4).** A property that runs `buildViewData` or the percentile reduction before and after `findOutliers` cannot fail, because both sides pass through the same reduction; it is dropped as K4 evidence. Instead one RTL test mounts `CycleTimeScatterPlotChart` (MUI mocked) with a fixture population that holds outliers, given in full: (1) the percentile values, the SLE and the items that feed the dialog and View Data, as they reach the chart's collaborators (percentile legend, reference lines, `WorkItemsDialog`), are deep-equal to what the test handed in; (2) the reference lines the mocks received plus the markers drawn (the marker specs) equal the lines received as props, none lost and none changed; (3) the dialog opened from a dot or from the chip receives the effective copies unchanged. Boundary test at exactly `N x median`, written in terms of the constant so changing N breaks nothing but the copy test that is meant to follow it. `fast-check` (MIT) is added as a frontend dev dependency (AFK-7; it is in neither `package.json` nor the lockfile today), a release older than 24 h for the pnpm min-release-age rule; a seeded generator loop is no longer the plan. Stryker 80% |
| Component (RTL) | `OutlierChip`: count, singular and plural names, opens on click, Enter and Space, Esc closes and focus returns, 10-row cap and overflow row, "Open in a list" calls back; chart integration with the existing mocks: type chip toggle changes the count only, all-outliers state keeps the legend row, named Cycle Time, series data excludes outliers (`data-series` is already exposed); the group at a hovered or clicked index is the group drawn there (one filtered array); an untyped outlier counts as visible; a late named-percentile answer for the old date range with the same definition id does not land; `disabled` closes an open popover (AFK-11); all-types-off shows its sentence and keeps the legend row (AFK-12); the screen-reader criteria listed in the DISCUSS access section; **page level (extend `BaseMetricsView.loading.test.tsx`): while the `cycleScatter` widget is loading or reloading with no items, the chart's own "No data available" is not in the accessible document** | The axis max and the geometry are **not** visible through the mock; do not pretend to assert them here |
| Component (RTL), the 50% line is the median (F5) | On a fixture population the mounted chart's 50th-percentile reference line has the value `fiftiethPercentile(effectiveDataPoints)`: the page-supplied 50% value in the fixture is a literal worked out once with the backend convention (`floor(0.5 x N) - 1`), so the test cross-checks two independent computations. Two cases: (a) the default Cycle Time, e.g. 29 items whose 50% line is 6; (b) a **named Cycle Time**: the answer tagged with the definition id carries its own 50% value, and it equals `fiftiethPercentile` of the named days (not of the default days). If the two ever differ, the popover's "median of {m} days" would contradict the 50% line, which is the one thing AFK-10 forbids | Fixture figures are illustrative (the literal is the worked-out value for the fixture list), except that case (a) can reuse Zenith's computed 6 |
| E2E (Playwright, POM) | One walking skeleton on Team Zenith: wait for `data-widget-status="ready"`, chip reads "1 outlier", open popover, row present, "Open in a list" opens the dialog; **no marker and no geometry assertion**, because Zenith has no line above the cut (AFK-15); one `@screenshot` per theme of the widget showing the chip and, in a second shot, the open popover (no markers). Marker text, order and placement are asserted in RTL and unit tests on a fixture population; their real-browser look is the manual spike, not a committed E2E | Extends the existing POM; thin, as the maintainer wants. DELIVER owns the phone-width (375 px), dark-theme and PDF checks |

## Wave: DESIGN / [REF] Slow connection: loading and failure looks

The chart is a pure view of props; the widget status is page-level (`categoryMetadata`: `cycleScatter` depends on
`cycleTimeData`, `cycleTimePercentiles`, `blackoutPeriods`), reported through `WidgetShell`. Nothing is added to the
chart for status. Behaviour that follows from reusing it:

| State | What the reader sees | Chip |
|---|---|---|
| Ready | Plot, legend row, chip | Live, count current |
| Reloading with something on screen (range changed) | The old plot dimmed to 0.4 under a small spinner, pointer events off | Dimmed with it and **disabled** (`frameIsReady=false`); an open popover closes, so a count from the previous answer is neither opened nor left open as if current (AFK-11) |
| First load, nothing yet | A lone spinner; the chart is mounted but hidden | Hidden |
| Failed | The widget's could-not-load message (with Retry where the page offers it) replaces plot and chip | Hidden |
| Named percentiles pending | Dots for the named value; lines absent until the tagged answer arrives; no markers from the old definition | Count is from the items, already current |

The popover is portalled, so it would outlive a status change; AFK-11 closes it whenever the widget leaves "ready"
(and keeps the chip disabled until it returns), so no stale open popover remains. It also closes when the outlier
count falls to 0. The chart's own "No data available" must never show while loading: pinned by a page-level test
(section 6). Check by hand with the browser throttled to a slow connection (DELIVER).

## Wave: DESIGN / [REF] Sketches

Visualization checklist items that changed since DISCUSS are answered in the section after the sketches. Figures
are **computed** for Team Zenith, last 30 days, with TZ-301 (table "Computed Team Zenith numbers"): 29 closed items,
50% line 6, 70% 8, 85% 10, 95% 15, SLE 85% within 7 days, cut 30, 28 other dots up to 20 days, one outlier of 58 days
(9.7x). Axis top 22 (1.1 x 20). On Zenith **no line lies above the cut, so the demo shows no edge marker** (AFK-15);
the marker sketch further down is an **illustrative fixture** and is not reachable on the demo.

**Loaded** (computed, Team Zenith), one outlier, every line inside the axis, reusing the legend row and `MuiChip`:

```
 Cycle Time  [Default v]                                                           (i)
 [50%] [70%] [85%] [95%] [SLE]                        [Task] [Bug]  [(!) 1 outlier]
 Cycle Time (days)
   22 |
   15 |- - 95% - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
      |        .   .                    .
   10 |- - 85% - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    8 |- - 70% - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    7 |- - SLE (85% within 7 days) - - - - - - - - - - - - - - - - - - - -
      |    . .:.:..:. .  .  .  .:  . .                  .
    6 |- - 50% - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    1 +--------------------------------------------------------------- Date
```

**Loaded with edge markers** (illustrative fixture: median 6, cut 30, highest typical dot 20, 95% line 33, SLE 35;
not reachable on the demo, proven by RTL and unit tests): the lines above the axis become labelled markers in the top
margin.

```
 Cycle Time  [Default v]                                                           (i)
 [50%] [70%] [85%] [95%] [SLE]                        [Task] [Bug]  [(!) 3 outliers]
                                          ▲ 95%: 33 days  ▲ SLE: 85% @ 35 days or less  (triangles in the line colours, text)
 Cycle Time (days)
   22 |
   10 |- - 85% - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    6 |- - 50% - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    1 +--------------------------------------------------------------- Date
```

**Loading**, a range change with the old plot on screen: everything dimmed (chip too), small spinner, nothing
clickable.

```
 Cycle Time  [Default v]                                                           (i)
 [50%] [70%] [85%] [95%] [SLE]  (dimmed)             [Task] [Bug] [(!) 1 outlier]  (dimmed)
 Cycle Time (days)                          (o)  <- small spinner centred on the plot
   22 |   ........ the old plot at 40% opacity, pointer events off ........
    1 +--------------------------------------------------------------- Date
```

**Loading, nothing on screen yet**: header only, a lone spinner, no plot, no chip, no "No data available".

**Failure**: the plot and chip are replaced; nothing numeric remains.

```
 Cycle Time                                                                        (i)
 +-----------------------------------------------------------------------------+
 |        (!)  The data could not be loaded.   [Retry]                          |
 +-----------------------------------------------------------------------------+
```

**Popover open** (below the chip, Esc or Close dismisses, focus returns to the chip; width limited to the viewport
minus 32 px; on a phone the Closed column is dropped and the table scrolls inside the popover):

```
 +--------------------------------------------------------------------------+
 | 1 outlier                                                           [x]  |
 | Not drawn. They are longer than 30 days (5 times the median of 6 days).  |
 | An outlier is a closed Work Item whose Cycle Time is more than 5 times   |
 | the median Cycle Time of all closed Work Items in this date range.       |
 | The percentiles, the SLE share and the numbers include it. View Data     |
 | lists it too.                                                            |
 | (no "Above the axis" line here: no line lies above the axis)             |
 |                                                                          |
 |  Work Item                       Closed        Cycle Time   Times the    |
 |                                                (days)       median       |
 |  TZ-301 Legacy data store mig.   12 Mar 2026   58           9.7x         |
 |                                                                          |
 |  [Open in a list]            (with 11+: "and 4 more" beside the button)  |
 +--------------------------------------------------------------------------+
```

**Every visible item is an outlier** (type chips hide the rest; axis top 22 is Zenith's computed value, which types
are outliers is illustrative): the plot frame, axes, lines and the whole legend row stay.

```
 [50%] [70%] [85%] [95%] [SLE]                      [Task] [Bug]  [(!) 2 outliers]
 Cycle Time (days)
   22 |            Every visible Work Item is an outlier.
   11 |            Open the chip to see them.
    1 +--------------------------------------------------------------- Date
```

### Visualization checklist (changes and answers at DESIGN)

- [x] **One question and decision**: as DISCUSS. Edge markers answer "where is the 95% line when it is off the plot".
- [x] **Awkward data**: Lightspeed (median 1, cut 5, nothing flagged, plot spans 1 to 5), the Zenith outlier (added
  in DELIVER), an all-outlier view, an empty view, a named Cycle Time. To repeat on the running demo.
- [x] **Axis labels and units**: unchanged; `Cycle Time (days)` and `Date`.
- [x] **Not colour alone**: chip icon + words; marker triangle + words.
- [x] **One accent**: chip neutral; markers reuse their line's colour.
- [x] **Loading, failure, too little data**: table above; too little data is not a state (D4).
- [ ] **Phone, dark mode**: marker fallback text and popover scrolling specified; verified in DELIVER on real
  screens; DELIVER owns these checks. Dark-mode contrast of the outlined chip checked then.
- [x] **Sketch reuses**: legend row, MUI `Chip`, MUI `Popover`, MUI `Table`, `WorkItemsDialog`, `WidgetShell` status.

## Wave: DESIGN / [REF] Hexagonal view, enforcement, handoff

Frontend-only; "ports" here are the layer boundaries of the frontend. Backend, API, DTOs, RBAC, migrations,
Lighthouse-Clients: **unchanged**. Domain events: none (render-time derivation).

```
 Team / Portfolio Metrics tab   (driving UI: BaseMetricsView, unchanged except widget-info terms)
        | cycleTimeData, percentiles, SLE   (hooks + API: unchanged)
        v
 CycleTimeScatterPlotChart [MODIFIED]  ---- useMemo ---->  utils/charts/outliers.ts        [NEW, pure]
        |  |  |                                            utils/charts/outlierWording.ts  [NEW, pure]
         |  |  |                                            utils/charts/offScaleLines.ts   [NEW, pure]
         |  |  |                                            (outliers.ts: fiftiethPercentile, findOutliers, axisTop)
        |  |  +--> OffScaleLineMarkers [NEW]              utils/charts/chartAxisUtils.ts   (unchanged, reused)
        |  +-----> OutlierChip + PopoverContent [NEW]
        +--------> WorkItemsDialog (unchanged)    WidgetShell status (unchanged)
 widgetInfoMetadata.ts [MODIFIED: cycleTime term]      Backend demo CSV: Team Zenith +1 item [DELIVER data]
```

**Effect isolation (functional core, thin shell).** All new logic is pure (`outliers`, wording, marker specs, line
split). Effects stay where they were: the named-percentile fetch (now tagged with the definition id and the callback) and the dialog/popover open state.
No preview or dry-run function exists here. The chip receives rows and callbacks, never services. Derived values are
`useMemo`; the old effect+state derivations are removed, which also removes a render in which chip and axis
disagree.

**Earned Trust (what does the environment lie about?)** (a) MUI X out-of-domain behaviour: probed by the manual DELIVER
spike on a throwaway fixture; there is no E2E geometry assertion because the demo has no line above the cut (AFK-15), and
the markers' own logic is covered by RTL and unit tests on a fixture. (b) Frontend median versus the backend's "50%" line: one convention (AFK-10),
pinned by a table test against `PercentileCalculator`'s index; nothing to probe at run time because the function is
pure. (c) Demo data: pinned by a backend test that Zenith's closed set contains an item above 5 times its own 50th
percentile (not a number). (d) Late answers: the tagged named percentiles are proven by a test with a stale date
range.

**Enforcement.** The fallow layer check (`layerBoundaries.enforcement.test.ts`) already forbids `utils` from
importing services, hooks, pages, and `components` from importing pages; `outliers.ts` imports only the pure `chartAxisUtils`. No new
rule needed. The constant-versus-copy test and Stryker cover the rest. Biome and `dotnet build` as per CLAUDE.md.

**Shared-doc edits owed in DELIVER** (not made here): move the ADR draft to `docs/product/architecture/` as
ADR-282; add the Outlier row to the ADR-270 Flow metrics glossary (row text in the DISCUSS context map section);
one line in `ARCHITECTURE.md` section 4; `jobs.yaml` job (AFK-4); the `metrics/flow-metrics.md` passage at
finalize; the `docs/settings/usagedata.md` line only if DEVOPS adds an event. `brief.md` gets one paragraph under
Application Architecture (frontend chart rule), also owed. DEVOPS: usage-data candidate "outlier popover opened"
(name-only) for K3; not decided here.

**External integrations:** none new. Contract tests: N/A, because no external service or API changes.

**Development paradigm for the crafter:** object-oriented project, functional-leaning React; the new module is
plain functions.

**Suggested build order (the roadmap belongs to DELIVER):** (0) spike: MUI X out-of-domain behaviour and margin in a
browser; (1) `outliers.ts` + PBT; (2) wording; (3) chart: memoised derivation, filter, cap, `OffScaleLineMarkers` (or its
fallback), tagged named percentiles; (4) `OutlierChip` + popover; (5) all-outliers and all-types-off states, legend
condition, `frameIsReady`; (6) "Open in a list" and the ⓘ sentence; (7) Zenith demo data + E2E + screenshots; (8) docs,
ADR move, glossary. **Slices:** 01 = steps 0 to 4 and the Zenith data of 7; 02 = step 5; 03 = step 6.

## DISCUSS issues found at DESIGN

1. **Slice 01 is not honest on the typical team.** The DISCUSS rule for slice 01 keeps every line on the plot; on
   a typical team the 95% line is near 5.2 times the median, above the cut, so the axis stays open above the cut
   and outliers between the cut and the axis top are "not drawn" in plain sight, while the popover says they are
   not drawn. The wording "a few outliers may sit below the axis top yet stay undrawn" admits it. **Recommend
   merging slices 01 and 02** (cap at the cut plus edge markers together); the design above is written for the
   merged rule. If the maintainer keeps them apart, slice 01 must not cap and must say so in the popover. **Resolved by AFK-6:** the slice
   files are rewritten (merged slice 01, the others renumbered 02 and 03). (The 5.2 times figure was an illustrative
   lognormal; computed on Zenith the 95% line is 2.5 times the 50% line, AFK-15. The merge stands as an honesty guard:
   a line above the cut is never silently lost, even if that is rare.)
2. **No Ocean Explorer team has the sketch's data.** The story text and sketch use "Ocean Explorer", `OE-123`,
   312 days, median 6. Ocean Explorer is a seeded Portfolio (its `OE-0xx` are Epics); Teams Equinox and Meridian
   carry three or no closed items. Nothing in the seeded data exceeds 5 times its median. The DISTILL sketch and
   scenarios use the Zenith item `TZ-301` added in DELIVER (AFK-14); the DISCUSS examples are rewritten with it.
3. **"Axis ends at {top} days" (popover line 1) is the wrong statement** once the axis is capped; the rule is about
   the cut. Replacement copy proposed above.
4. **Plural of the chip name.** The copy table has `{n} outliers not drawn...`; for 1 it must read "1 outlier".
   Singular and plural are specified in the wording module.
5. **D12 leaves the whole-number rounding open** ("from 10 upward a whole number"). Decided: rounded half-up from the exact
   ratio (AFK-17), and the switch happens on the rounded-up tenths (so 9.96 reads "10x", and 10.5 reads "11x").
6. **"Median" has two meanings on one screen**: the "50%" line (backend, lower index at small N) and the rule's true
   median. The two can differ by under one step. **Resolved by AFK-10:** the rule's median is the
   50th percentile with the backend's convention, so only one median is on the screen; the popover still states it
   in days (unconditionally).
7. **Status line says "ready for DESIGN once N2 is answered"**; AFK-2 answers it. Fixed: the DoR status line is re-marked.
8. **The Portfolio path says "Work Item(s)"** for Features (the chart already does so). Kept, as drift outside this
   story; the Terminology rule is met because the word is the configured one.
9. **DISCUSS never mentions the tooltip/focus problem of undrawn-but-present markers**; decision 2 (filter, not
   clip) closes it.

## Wave: DESIGN / [REF] Decisions made while the maintainer is AFK

| # | Decision | Reverse if |
|---|---|---|
| DES-1 | Pure module `utils/charts/outliers.ts`; no `multiple` parameter; strict `>` | a second rule appears |
| DES-2 | Median over the whole population, as the 50th percentile with the backend's convention (AFK-10); chip and list follow the visible types | |
| DES-3 | Axis top = the pure `axisTop` (with outliers: `min(1.1 x max(typical dots, lines at or below cut), cut)`; without: today's); outliers filtered from the series; x-domain from all items (AFK-13) | the spike shows MUI X clips safely and the cap looks bad |
| DES-4 | Edge markers as a custom SVG text layer in the top margin | the spike shows `ChartsReferenceLine` clips correctly |
| DES-5 | Merge slices 01 and 02 (issue 1; AFK-6, done) | maintainer wants two releases |
| DES-6 | Popover line 1 states the cut and the median, not the axis end | |
| DES-7 | Named percentiles tagged with the definition id and the fetch callback's identity, in the same slice (deviation from keyed queries, recorded in ADR-282) | the fetch moves up into a keyed query |
| DES-8 | Add `fast-check` (MIT) as a dev dependency (AFK-7), a release older than 24 h | |
| DES-9 | Demo data: `TZ-301` on Team Zenith, no Parent (AFK-14); the Gravity alternative is dropped | |
| DES-10 | `OutlierChip` is not generic yet; the calculation is | a second chart adopts the chip |
| DES-11 | Spike fallback: the marker row moves into the legend row; pass and fail criteria in section 2 | the spike passes |
| DES-12 | The page passes `frameIsReady` (the `frameStateOf` status of the `cycleScatter` widget is "ready", through the `buildWidgetNodes` ctx, AFK-16) to the chart; the chip disables and its popover closes otherwise (AFK-11). `WidgetStatusReporterContext` (chart to frame) rejected: wrong direction | the chart gets the status another way |
| DES-13 | All types off: sentence in the plot, legend row kept (AFK-12) | |
| DES-14 | K4 is evidenced by a wiring-point test on a mounted chart (percentile, SLE and View Data inputs unchanged; drawn lines plus markers equal the lines received), not by a same-reduction property; a second test pins the chart's 50% line to `fiftiethPercentile(effectiveDataPoints)`, default and named Cycle Time | |
| DES-15 | No marker or marker-geometry assertion in E2E and no marker in the `@screenshot`: Zenith has no line above the cut (AFK-15). Markers are proven by RTL/unit tests on a fixture and the manual spike | a seeded team gains a line above its cut |

## Wave: DESIGN / [REF] Architecture summary for the maintainer

Posted in chat at the end of the run (hexagonal shape, patterns weighed); this file holds the decisions above.

Delta after review round 1: (1) one median definition, `fiftiethPercentile`, mirrors the backend's 50th percentile
(AFK-10), so the popover and the 50% line agree; (2) `axisTop` and the line split are pure functions with stated
properties, and `OffScaleLineMarkers` has a spike with pass and fail criteria and a named fallback (marker row in
the legend row); (3) the page passes `frameIsReady`, so the chip disables and its popover closes whenever the widget
leaves "ready"; named percentiles are tagged with the definition id and the fetch callback. Patterns weighed and
rejected in this round: a keyed TanStack query (fetch would have to move up), a stored switch or backend flag
(maintainer decisions), a second `median` definition (confusing).

## AFK decisions after DESIGN (2026-11-02, orchestrator)

| # | Decision | Why |
|---|---|---|
| AFK-6 | **Slices 01 and 02 merge**: the axis caps at the cut and the edge markers for lines above it ship together. The slice files are updated to match after the DESIGN review. | Shipping the cap without markers hides the 95% / SLE lines silently; shipping without the cap shows "not drawn" outliers in plain sight. Neither half is honest alone. |
| AFK-7 | **`fast-check` is added as a frontend dev dependency** for the property tests of `outliers.ts` (checked: it is in neither `package.json` nor `pnpm-lock.yaml`). Pick a version older than 24 h (pnpm 12 min-release-age). | Dev-only, no runtime or bundle impact; the outlier rule is exactly the kind of pure numeric code property tests are for. |
| AFK-8 | **The DESIGN's AFK copy changes** (popover line 1 "Not drawn. They are longer than …", the singular "1 outlier", the factor rounding) **are accepted provisionally and gated by the DISTILL sketch walk.** | CLAUDE.md: no UI ships unseen. |
| AFK-9 | **The `[spike]` items** (MUI X behaviour for out-of-range values and the top margin) **are proven in the first DELIVER step**, as the DESIGN proposes. | No `node_modules` in this worktree; a browser spike is cheaper than guessing. |

## AFK decisions after the DESIGN review (2026-11-02, orchestrator)

| # | Decision | Why |
|---|---|---|
| AFK-10 (review M3) | **"The median" in the rule is Lighthouse's own 50th percentile**, computed in the frontend with exactly the backend's convention (`PercentileCalculator.cs`: sort, index = floor(p/100 × N) − 1, clamped). The popover's "median" and the chart's "50%" line are therefore always the same number. The backend is **not** changed. | Two different "medians" on one screen would confuse every reader. The backend convention sits one rank below the textbook median when p × N is not a whole number (e.g. N = 5 picks the 2nd value); changing it would move every percentile in the product, which is far outside this Story. **Flagged for the maintainer as an observation, not a decision:** whether that convention is intended is a separate question. |
| AFK-11 (review M4) | **The popover closes and the chip is disabled whenever the widget status leaves "ready"**, not only when the count reaches 0. | CLAUDE.md: never show a previous answer as current. |
| AFK-12 (review M8) | **All types switched off shows "All {Work Item} types are hidden. Switch one on in the legend." and keeps the legend row**, never "No data available". | The design rewrites that legend-row condition anyway; an empty look that reads as "no data" is forbidden. |
| AFK-13 (review m4) | **The x-axis (closed date) keeps its domain from all items**, outliers included. | An outlier's closed date lies inside the selected range, so it does not stretch the x-axis; only the y-axis is distorted by outliers. |
| AFK-14 (review M7c) | **Demo data: Team Zenith**, with an unused id (not TZ-204), the unit stated as working days, and the counting convention (inclusive, end − start + 1) written down. The accepted shift in Zenith's percentiles, throughput and closed counts is listed so DELIVER can update the affected tests. | One choice, and the reviewer's collisions fixed. |


## Wave: DEVOPS / [REF] Story 6502 Hide Outliers (2026-11-02, PROPOSE mode, maintainer AFK)

Wave state: **DEVOPS proposed.** Platform change: none (no workflow, image, chart or infrastructure edit). Everything
below was checked by reading the repository; nothing was run (no tests, no builds). Mutation-testing strategy is the
project's `per-feature`; the pipeline already carries every gate this story needs.

### 1. Usage-data event (decision)

How a frontend event reaches the catalog (read in code): the browser sends a name through `useUsageDataReporter()`
(`Lighthouse.Frontend/src/services/UsageData/usageDataReporter`) to the backend, which forwards it. The browser-side
list is `UsageDataEventName` in `Lighthouse.Frontend/src/services/Api/UsageDataService.ts` (string values), and it must
match the backend enum `Lighthouse.Backend/Lighthouse.Backend/Models/UsageData/UsageDataEventName.cs` (today it ends at
`ReportCombinedPdfExportStarted = 24`). The service comment says a name not on the list makes the server refuse the
whole message. So a frontend-triggered event is **not** frontend-only: it needs one backend enum member.

| Item | Design |
|---|---|
| Serves KPI | K3 (popover opened). Yes, so the event is built. |
| Name | `OutlierPopoverOpened`. Name-only: no property, so no `UsageDataEventShapes` entry is needed beyond the enum (the other name-only events, `AllReportsListed` and `ReportBulkRemovalStarted`, are registered the same way). Never the count of outliers, the ids, the Team, the chart or a day figure. |
| Integer | Next free on `main` when DELIVER starts. Today 25 is reserved by Story 6501, so **26**; DELIVER re-reads the enum and appends, never renumbers. Tests pin the name and "appended after the previous last", not the integer. |
| Emit point | In the slice that first makes the chip usable (slice 01, the chip step): the `OutlierChip` open handler calls `reportUsage({ name: UsageDataEventName.OutlierPopoverOpened })` once per closed-to-open change (click, Enter or Space). Not on close, not on the `frameIsReady=false` forced close, not on re-render. The chip receives the callback as a prop from the chart (it never imports the service), so the component stays testable. |
| Consent | `reportUsage` already no-ops without consent; no new gate. |
| Docs | One row in `docs/settings/usagedata.md` in the events table (next to "A report was exported as a PDF"): "The outlier list was opened | Somebody opened the list of outliers on the Cycle Time chart | Nothing. Not which Team or Portfolio, not how many outliers, not which items." Written by the slice that emits it. |
| Tests | Vitest: opening reports once; closing, `disabled` and re-render report nothing; no consent sends nothing. Backend: extend the existing usage-event enum pin test (pattern of `BulkReportsUsageEventTests`) with the new member. |
| Honest limit | Name-only cannot give K3's denominator ("Teams that have a chip"). K3 is therefore read as a floor: "installs that opened the outlier list at least once in 30 days". Adding a property for the denominator would break the name-only rule; not done. |

### 2. CI

| Concern | Finding and decision |
|---|---|
| Vitest/RTL | New specs sit beside the components under `Lighthouse.Frontend/src` and run in the default `pnpm test` (`ci_frontend.yml`). Nothing to add. |
| fast-check (AFK-7) | Added to `devDependencies` in `Lighthouse.Frontend/package.json`. pnpm 12 checks every lockfile entry for 24 h of age, **including its transitive `pure-rand`**, so at DELIVER step 1 pick the newest `fast-check` whose publish time (`pnpm view fast-check time`) and whose `pure-rand` resolution are both older than 24 h. `pnpm_config_minimum_release_age=0` is allowed for a local install only and never committed. Overrides, if `pure-rand` needs pinning, go in `Lighthouse.Frontend/pnpm-workspace.yaml` (not `package.json`). Property tests set `numRuns` explicitly (100) to keep the suite fast; the seed is random and fast-check prints it on failure. Renovate takes the dependency over afterwards (7-day wait). |
| E2E | One walking skeleton on Team Zenith plus one `@screenshot` per theme, through the existing `CycleTimeScatterPlotChart` page object (`Lighthouse.EndToEndTests/tests/models/metrics/CycleTimeScatterPlotChart.ts`, extended with chip and popover locators; no inline `page.locator`). The page object waits for `data-widget-status="ready"` before reading the chip. E2E runs in CI through `ci_e2e.yml` and again in `ci_verifysqlite` / `ci_verifypostgres`. |
| Screenshot run | `@screenshot` needs a premium licence locally (a DB wipe fails 6 premium tests otherwise) and keeps the old PNG when the pixel diff is under the threshold. The chip and the clipped axis are a large diff, but DELIVER checks `git status` shows the PNGs as modified. |
| Demo-data pins on Team Zenith | **Backend (read):** `DemoDataFactoryTest` names Zenith only in structural cases (no estimation-field rule, refinement states, cadence, stage rules, date anchoring): no count or percentile is pinned. `DemoDataStateEnteredSeamIntegrationTest` reads Zenith's in-progress items (TZ-301 is Done): unaffected. `DemoDataReferenceIdUniquenessTest` must still pass (TZ-301 is new). DELIVER adds the planned test "Zenith's closed set holds an item above 5 times its own 50th percentile". The other backend files that match "Zenith" (Refinement, Reports, TeamFeatureSize, Delivery*) build their own fixture Teams; DELIVER greps them for `TZ-` ids and closed totals once before editing the CSV. **E2E (read):** no Zenith spec asserts a count or percentile figure (assertions are structural: dots greater than 0, widget visible, dialog title, line counts of the Percentiles over Time chart). Specs that read the scatter or an adjacent Zenith chart and so need one run after the CSV edit: `NamedCycleTimePercentiles.spec.ts` (scatter with a named cycle time), `MultipleCycleTimes.spec.ts`, `WorkItemsDialogContextColumns.spec.ts` (Estimation vs Cycle Time bubble: that chart is not clipped, so TZ-301 stretches its axis), `TotalThroughputViewData.spec.ts` (throughput +1), `PercentilesOverTime.spec.ts`. **Screenshots:** every Zenith chart screenshot is regenerated at finalize (scatter, named cycle time, Estimation vs Cycle Time, percentiles). |
| Pipeline change | None. `ci.yml` path filters already route `Lighthouse.Frontend`, `Lighthouse.Backend` (CSV, enum) and E2E changes to the right jobs. |

### 3. Observability

The feature is a render-time derivation in the browser: no endpoint, job, queue or log line is added. No new SLO, alert
or dashboard. The only signal is the `OutlierPopoverOpened` usage event (section 1). The statistics themselves cannot
silently change: K4 rests on the wiring-point test that a mounted chart hands the percentile, SLE and View Data inputs on unchanged and draws plus marks exactly the lines it received. A
broken chart in the field has no telemetry in this product (no frontend error reporting was found); none is added here.

### 4. Release and rollback

| Question | Answer |
|---|---|
| API, DTO, migration, RBAC, clients | None. Lighthouse-Clients: unchanged (DELIVER greps the Clients repo once for the existing event names to confirm it carries no mirror of `UsageDataEventName`; if it does, the new name is a patch bump there). |
| Backend touched | Two additive items: one enum member and one CSV row compiled into the backend. No behaviour of any endpoint changes. |
| Rollback | Revert the commits. Nothing is persisted by the feature: no setting, no stored state, no schema. The enum member is append-only; an older frontend never sends it, an older backend would refuse it only if a newer frontend ran against it, which a single deployed unit cannot be. |
| Demo data | Applied **on demand**: Settings, Demo Data, "load scenario" (wipes existing Teams and Portfolios, per `docs/settings/demodata.md`). The CSV is read at load time, so **an existing install keeps its old Zenith (no TZ-301) until someone reloads the scenario**; there is no migration and no auto-reseed. Fresh installs and the E2E (scenario 0 is loaded per run) get it at once. The `updatedemoenv.yml` workflow reloads the public demo environments nightly (no demo environment is deployed while the platform is paused). So an existing user with real data sees the chip only when a Team really has an item above 5 times its median; that is intended. |
| Release note | Written at `/release`, not at finalize. |

### 5. Mutation testing (StrykerJS, break 80)

- **DELIVER output:** the committed copy lives in the feature folder, because Frontend gitignores `**/vitest.stryker*.ts` and `**/stryker-config*.json`: `docs/feature/story-6502-hide-outliers/mutation/vitest.stryker.6502.config.ts`, modelled on `docs/feature/story-6249-chart-loading-indicators/mutation/vitest.stryker.6249.config.ts`. `include` names the story's spec files one by one (a full sweep runs out of memory; a missing spec makes every mutant survive).
- **`mutate`:** `src/utils/charts/outliers.ts`, `src/utils/charts/offScaleLines.ts`, `src/utils/charts/outlierWording.ts`, the `OutlierChip` and popover-content components, and `OffScaleLineMarkers` (or its fallback). `CycleTimeScatterPlotChart.tsx` only by the story's line spans, one `mutate` entry per span (`N-N` for a single line; a bare line number is silently dropped). `thresholds: { break: 80 }`, `disableTypeChecks: false`, `concurrency: 2`, `coverageAnalysis: "off"` (as 6249), `vitest.related: false`.
- **When:** last, on frozen code, once per feature (slice 03 done); any later edit shifts the line spans.
- **Cost control:** `numRuns` stays at 100; survivors are killed by a targeted test rather than a re-run of everything.
- **Backend (Stryker.NET):** N/A, because the backend change is an enum member and a data row with no logic to mutate.

### 6. Docs and website at finalize

- **Page:** `docs/metrics/flow-metrics.md`, section "Cycle Time Scatterplot" (from line 16): add the chip, the popover, the rule (more than 5 times the median, computed on the plotted value), the markers for lines above the axis, and that View Data and every statistic are unchanged. Cross-check `docs/metrics/widgets.md` and `docs/metrics/portfolio-features.md` (the latter links to this section) for repeated wording. Use the configured terms (Work Item, Feature), the seeded defaults.
- **Screenshots:** `docs/assets/features/metrics/cycleScatter.png` and `cycleScatterNamedCycleTime.png` (the latter is taken in `Screenshots.spec.ts` on Zenith with "Analysis to Done"), plus the Estimation vs Cycle Time image if its figure shows Zenith. Regenerated at finalize; the scatter now shows a chip, and the dark theme shot per the E2E plan.
- **Website:** it hot-links `docs/assets` from `@main` through jsDelivr (ADR-073; the website repo's `lighthouseAsset()`), with no reference inside this repo. I did not find `cycleScatter` referenced in `/storage/repos/website/src`, so whether a live page shows it is unconfirmed; the rule is to **overwrite the PNGs in place, never rename or delete them**, so a page that does use one keeps working and picks up the new image once it is on `main` (jsDelivr may cache `@main` for some hours). At finalize, grep the website repo for `cycleScatter` and note the result.
- **Also owed at finalize (from DESIGN):** ADR-282 moved to `docs/product/architecture/`, ADR-270 glossary row, `ARCHITECTURE.md` section 4 line, `brief.md` paragraph, `jobs.yaml` entry, the `usagedata.md` row (section 1).

### Checklist answers (no silent N/A)

| Item | Answer |
|---|---|
| Usage-data event | Designed: `OutlierPopoverOpened` (section 1). |
| New infrastructure, IaC, container, workflow | N/A, because the existing frontend, backend and E2E jobs cover every change. |
| Security scanning | N/A, because no new surface; the only new dependency is dev-only (`fast-check`) and goes through the existing SBOM and Renovate path. |
| Deployment strategy | N/A, because there is no runtime component; ships with the normal release. Rollback is a revert. |
| SLOs and alerts | N/A, because no service behaviour changes (section 3). |

## AFK decisions (DEVOPS)

| # | Decision | Why |
|---|---|---|
| DO-1 | Build the usage event `OutlierPopoverOpened`, name-only, integer = next free on `main` at DELIVER (26 if 6501 lands first), emitted once per open from the chip, with the `usagedata.md` row. | It is the only way to measure K3; one enum member and one row; reversible by revert. |
| DO-2 | Accept that K3 is read as a floor ("opened at least once per install in 30 days"), not the ratio over Teams with a chip. | A denominator needs a property or a second event, which breaks the name-only rule. |
| DO-3 | The story is "frontend-only" except for one append-only backend enum member (a prerequisite of any frontend event) and the demo CSV row. | The browser-side list is refused by the server when it differs from the backend enum. |
| DO-4 | `fast-check`: newest version and `pure-rand` both older than 24 h at install; `numRuns` 100; random seed with fast-check's failure seed printed. | pnpm 12 min-release-age; keeps the default suite fast. |
| DO-5 | No Zenith test pins a count or percentile (read); three greps owed at DELIVER (backend fixtures for `TZ-` ids, Clients for the event mirror, website for `cycleScatter`), and five E2E specs run once after the CSV edit. | Names the exact set instead of a general "update tests". |
| DO-6 | Existing installs do not receive TZ-301 until the demo scenario is reloaded; no migration and no auto-reseed. | Demo data is applied on demand and wipes data; reseeding would destroy user data. |
| DO-7 | Mutation config committed at `docs/feature/story-6502-hide-outliers/mutation/vitest.stryker.6502.config.ts` (Frontend gitignores stryker files), spec files named in `include`, `disableTypeChecks: false`, `concurrency: 2`, one `mutate` entry per chart line span; break 80; run last on frozen code; Stryker.NET N/A. | Matches the project's strategy and the 6249 precedent. |
| DO-8 | Overwrite the scatter screenshots in place at finalize; never rename; website hot-link unconfirmed so grep at finalize. | The website serves `docs/assets` from `@main`. |
| DO-9 | No observability, SLO, alert or pipeline change. | Render-time derivation in the browser. |

## Computed Team Zenith numbers (2026-11-02, orchestrator; review round 2 F2)

Computed from `Factories/DemoData/Team Zenith.csv` with a one-off script (working-day offsets `{w-N}` resolved
against Monday 2026-11-02, weekends skipped; Cycle Time = inclusive calendar days, `end − start + 1`, as
`WorkItemBase.GetDateDifference`; percentiles with the backend convention `floor(p/100 × N) − 1`). The exact values
shift by ±1 day with the weekday the demo is loaded on; DELIVER pins them with a test against Zenith's own
50th percentile, not fixed numbers.

| Date range | N closed | 50% | 70% | 85% | 95% | Largest | Cut (5 × 50%) | Outliers |
|---|---|---|---|---|---|---|---|---|
| Last 30 days, today | 28 | 6 | 8 | 10 | 15 | 20 | 30 | none |
| Last 30 days, + TZ-301 | 29 | 6 | 8 | 10 | 15 | 58 | 30 | TZ-301 (58 days, 9.7×) |
| Last 90 days, + TZ-301 | 94 | 7 | 8 | 11 | 15 | 58 | 35 | TZ-301 (58 days, 8.3×) |
| All, + TZ-301 | 202 | 6 | 8 | 11 | 15 | 58 | 30 | TZ-301 |

Zenith's SLE is 85% within 7 days (`DemoDataFactory.cs:63-64`).

## AFK decisions after DESIGN review round 2 (2026-11-02, orchestrator)

| # | Decision | Why |
|---|---|---|
| AFK-15 (F2) | **All examples, the DoR, the Gherkin and the sketch numbers use the computed table above** (50% = 6, 95% = 15, cut = 30, TZ-301 = 58 days = 9.7×, SLE 7 days), each labelled "computed" or "illustrative". **On the Zenith demo no line lies above the cut, so no edge marker appears**: the demo E2E and the `@screenshot` show the chip and popover only; the edge markers are covered by RTL/unit tests on a fixture population (and the humble-object marker spec). No second demo item is added. | Real data, not estimates. Adding an artificial second extreme item only to make a marker appear would distort the demo for everyone. **Observation for the maintainer:** on real-looking data the 95% line sits at about 2.5 × the 50% line (15 vs 6), not 5.2× as the illustrative lognormal suggested, so edge markers will be rare in practice; they stay in slice 01 as a cheap honesty guard (AFK-6 unchanged). |
| AFK-16 (F4) | **The chart receives `frameIsReady`** (not `isCurrent`, which clashes with a local at `BaseMetricsView.tsx:1492`), sourced from `frameStateOf`'s status for the `cycleScatter` widget key, passed through the `buildWidgetNodes` ctx. The `WidgetStatusReporterContext` channel (`widgetStatus.ts:119-143`) is recorded as the considered alternative, rejected because it reports chart→frame, not frame→chart. | Names a real source; avoids the clash. |
| AFK-17 (F7) | **Factor rounding from 10 up is half-up** (10.5 → "11×"), pinned in the property test. | Matches what readers expect. |

## Maintainer decision on the percentile observation (2026-11-02, evening)

**Leave the backend percentile convention as it is** (`PercentileCalculator.cs`, index floor(p/100 × N) − 1). No Bug is
raised; this Story mirrors it (AFK-10).

## Maintainer decisions at the start of DELIVER (2026-11-14, sketch walk-through, AFK entry)

Binding for every slice; they replace the DESIGN sketches and the copy table where they differ.

| # | Decision |
|---|---|
| S-1 rigor | **`standard` for all three slices**: crafter and acceptance designer `sonnet`, reviewers and the examiner `haiku`. |
| S-2 AFK options | Slice wrap-up **refactor + review** (mutation once, at the end); **push each slice** with its gates green and watch CI in the background. |
| S-3 chip place | **The outlier chip lives in the widget header** (`WidgetShell`'s header row: info, View Data, trend, RAG, then the chip), not in the legend row. Same size and style as the RAG chip (small, 20 px), outlined, `(!)` icon plus `1 outlier` / `{n} outliers`. Tooltip on hover and focus: `{n} outliers not drawn on the chart. Show details.` (singular for 1). Click, Enter or Space opens the popover below the chip. Below 600 px the words drop to `(!) {n}`; the accessible name keeps the full sentence. Absent when there are no outliers. The legend row keeps only the percentile and type chips. |
| S-4 popover copy | **Shorter:** title `{n} outlier(s)`, line 1 `Not drawn: longer than {cut} days (5 times the median, {median} days).`, line 2 `Still counted in the percentiles, {SLE} and View Data.`, then the table and `Open in a list`. The full rule sentence moves to the widget's ⓘ help (slice 03) **in its long form** ("The help text (exact)"), not the short form. The conditional `Above the axis: 95% at {p95} days, {SLE} at {sle} days.` line stays (orchestrator: the markers are `aria-hidden`, so this line is how a screen-reader user learns of them). |
| S-5 edge markers | **Top margin of the plot**, triangle in the line's colour plus text, as designed; the fallback (a text line under the legend row) is taken if the spike fails. |
| S-6 empty copy | **Shorter:** every visible item an outlier: `All visible {Work Items} are outliers.`; all types off: `All types are hidden.` Legend row and axes stay; never "No data available". |
| S-7 no outliers | Confirmed: no chip, today's axis, no markers. |

**Design consequence of S-3 (orchestrator, engineering):** the count depends on the type chips (D5), whose state lives
in the chart, but the header is drawn by `WidgetShell`. The chart therefore hands the chip to the header through a
header slot that `WidgetShell` offers to its child (a small context, rendered by `WidgetHeader` after the RAG chip),
rather than lifting the type-chip state out of the chart. The crafter of slice 01 may pick a portal or a
context-held node; the oracle tests the result in the header, not the mechanism.

## DELIVER refresh of the DESIGN anchors (2026-11-14, orchestrator)

The metrics view was split after DESIGN (`refactor(metrics)` commits `6a32760c8`, `a63c924ec`, `a4e92f20d`,
`5ee48a83b`). Current places: `buildWidgetNodes` and the `cycleScatter` node are in
`pages/Common/MetricsView/widgetNodes.tsx` (the chart at about line 363); `frameStateOf` is in
`pages/Common/MetricsView/dashboardFrame.ts` and is spread into each `WidgetShell` in `BaseMetricsView.tsx` (about line
627). `BaseMetricsView.tsx:1492` (AFK-16) no longer names the clash. **AFK-16 is refined:** because the chip now lives
in the shell's header (S-3), the shell's header-slot context also carries whether the frame is ready, so the chart
reads `frameIsReady` from that context instead of a new `buildWidgetNodes` ctx field. The usage event takes the next
free integer, **27** (`SystemWideAccessGranted = 26`), still appended, never renumbered. ADR-282 is still free.
**The spike (AFK-9) runs inside slice 01:** the crafter builds the top-margin markers; the orchestrator checks them in
a real browser on a fixture before the slice commits, and takes the S-5 fallback if they fail.
| S-8 SLE default | **The Cycle Time scatterplot draws its SLE line by default** (maintainer, 2026-11-14, after the S1 crafter raised it); the reader can still switch it off. Other charts keep SLE off by default. |

## Maintainer guided review (2026-11-14, after finalize)

Checked by hand on the "When Will This Be Done?" demo (Team Zenith, TZ-301), build `dda497b87`.

- **Met:** the chip is in the header; the axis fits the rest; the SLE line is on at first load; Open in a list opens
  the Work Items dialog with focus back on the chip; the (i) help states the rule; "All types are hidden."; TZ-301
  still counts in View Data and the percentiles; the disabled "Outliers" chip while loading (Slow 3G); the short chip
  at phone width; the Outliers section of `docs/metrics/flow-metrics.md`.
- **Not run:** the top-edge markers, because Team Zenith puts no line above the axis; the oracle tests and the
  browser spike of slice 01 cover them.
- **Defects:** none. Two changed minds, built as slice S4 inside Story 6502 before it closes:

| # | Decision |
|---|---|
| D13 chip colour | The chip is **amber outlined**: MUI `warning` colour on border, icon and text, at every width and while loading. Replaces the plain outlined look of S-3. |
| D14 no popover | **The chip opens the outlier list dialog directly** (the Work Items dialog with title `{Work Items} with a {Cycle Time} more than 5 times the median`, every outlier of the visible types). No popover, and no extra lines in the dialog; the rule stays in the (i) help. Focus returns to the chip when the dialog closes. Replaces S-4 and the popover half of S3. |
| D15 event (orchestrator) | The usage event keeps integer **27** and is renamed `OutlierListOpened`, sent when the chip opens the dialog. Safe because no release has carried `OutlierPopoverOpened` yet (last tag `v26.11.8.1`, before Story 6502). |
| D16 screen readers (orchestrator) | The popover's "Above the axis: …" line was how a screen-reader user learnt of the top-edge markers (S-4). With the popover gone, the chip's accessible name carries it instead: `{n} outlier(s) not drawn on the chart. Above the axis: 95% at {p95} days. Show the list.`, the middle sentence only when a line is above the axis. |
| D17 no multiple (orchestrator, from D14) | The "times the median" multiple (TZ-301's "9.7x") lived only in the popover table and goes with it. The maintainer chose the dialog exactly as it is, with no extra lines (the "Dialog only, no lines" option, whose preview showed the dialog's own columns), so the dialog shows its usual columns, Cycle Time in days included, and no multiple. Slice S1's observation text ("9.7x the median") is history. |
