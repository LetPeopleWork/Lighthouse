# Taste evaluation — Story 6502 "Hide Outliers"

Scores are the author's judgement against the rubrics in the taste-evaluation skill, written before the recommendation.
They have not been checked with the maintainer or with users.

## 1. DVF filter (eliminate if total < 6)

Lens meanings here: **Desirability** = answers the job and the maintainer's stated wishes (indicator, numbers on click,
still in View Data, nothing silently changed). **Feasibility** = buildable on MUI X Charts with the existing helpers and
custom scatter marker, without a new backend concept. **Viability** = fits Lighthouse's chart rules (explainable in a
minute, every axis labelled, reads in dark mode and phone width) and can be reused by the next chart without a rewrite.

| # | Option | D | F | V | Total | Outcome |
|---|---|---|---|---|---|---|
| 1 | Frame break (edge markers) | 4 | 3 | 4 | 11 | survives. F3: a second custom mark in a scatter whose marker is already custom; overlap of several arrows at the edge needs a rule. |
| 2 | Chip + popover | 5 | 4 | 4 | 13 | survives. This is the maintainer's own idea, and the legend row, `LegendChip` and the info popover exist. |
| 3 | Overview + detail | 3 | 2 | 3 | 8 | survives, weakly. F2: a second chart, linking, and the card already carries two legend rows; phone width. |
| 4 | "Show outliers" switch | 4 | 4 | 4 | 12 | survives. Chip pattern exists; the stored per-reader state is the extra (precedent: `lighthouse:deliveryTimeline:showTeams`). |
| 5 | Re-scale (log / symlog / break) | 2 | 3 | 2 | 7 | survives, weakly. D2: the maintainer asked to hide and disclose, not to re-scale; V2: a non-linear axis fails "someone who has never seen it can say what it shows within a minute". |
| 6 | Reader-flagged outliers | 2 | 1 | 2 | 5 | **eliminated** (fails two lenses). F1: a per-Work-Item flag needs a stored field, migration, RBAC, tracker-sync story and a way to read it back; D2: the story wants the system to point at outliers, not the reader to curate. |

Survivors: 1, 2, 3, 4, 5.

## 2. Weights (locked before scoring)

| Criterion | Weight | Default | Reason for any change |
|---|---|---|---|
| DVF (average of D, F, V) | 30% | 30% | unchanged |
| T1 Subtraction | 20% | 20% | unchanged |
| T2 Concept count | **25%** | 20% | Raised: the Lighthouse bar is a chart someone can explain to a stakeholder in a minute (`visualization` skill, opening paragraph), so what a first-time reader must learn is the dominant cost here. |
| T3 Progressive disclosure | 15% | 15% | unchanged |
| T4 Speed-as-Trust | **10%** | 15% | Lowered: every option is a client-side redraw of data already loaded, so latency barely separates them. |

Sum = 100%. Sensitivity at the end shows the ranking does not depend on this choice.

## 3. Taste scores (1-5, rubric from the skill)

| Option | T1 Subtraction | T2 Concept count | T3 Progressive disclosure | T4 Speed |
|---|---|---|---|---|
| 1 Frame break | 3. Arrows, value labels and a click target all sit in the plot; several off-scale items crowd the edge and need a stacking rule. | 3. Two ideas: "arrow at the edge = further up", and the cut itself. | 3. Every outlier is on show at once; detail is a second click. | 5. Instant, in the plot. |
| 2 Chip + popover | 4. One chip and one list. The "x the median" column could go, the rest is the point. | 4. One new idea, "outlier", anchored to an info-popover pattern users already know; the rule is one sentence in it. | 5. First sight: a plot that reads well and a small chip. The numbers only on request. | 5. Instant, local. |
| 3 Overview + detail | 2. A second chart to carry what a chip carries. | 2. Two views, a marked region, and the cut. | 3. Both views visible from the start. | 4. Redraws two charts; layout shift. |
| 4 Switch | 3. The chip of option 2 plus a stored preference plus a default per chart. | 3. "Outlier" plus "this chart remembers my choice and defaults differ between charts". | 4. Default state is simple; the switch is one step away. | 4. Axis jump on toggle. |
| 5 Re-scale | 4. Adds no element, only a different scale. | 1. A non-linear axis is a new mental model for the reader and for whoever they present to. | 3. Everything is visible, with no staging. | 5. Instant. |

## 4. Weighted ranking

DVF average = total / 3. Final = 0.30 x DVF + 0.20 x T1 + 0.25 x T2 + 0.15 x T3 + 0.10 x T4.

| Rank | Option | DVF | T1 | T2 | T3 | T4 | **Weighted total** |
|---|---|---|---|---|---|---|---|
| 1 | **2 Chip + popover** | 4.33 | 4 | 4 | 5 | 5 | **4.35** |
| 2 | 4 "Show outliers" switch | 4.00 | 3 | 3 | 4 | 4 | **3.55** |
| 3 | 1 Frame break | 3.67 | 3 | 3 | 3 | 5 | **3.40** |
| 4 | 5 Re-scale | 2.33 | 4 | 1 | 3 | 5 | **2.70** |
| 5 | 3 Overview + detail | 2.67 | 2 | 2 | 3 | 4 | **2.55** |

Arithmetic, option 2: 1.30 + 0.80 + 1.00 + 0.75 + 0.50 = 4.35. Option 4: 1.20 + 0.60 + 0.75 + 0.60 + 0.40 = 3.55.
Option 1: 1.10 + 0.60 + 0.75 + 0.45 + 0.50 = 3.40.

## 5. Sensitivity

| Weights | 1st | 2nd | 3rd |
|---|---|---|---|
| Locked (30/20/25/15/10) | 2 (4.35) | 4 (3.55) | 1 (3.40) |
| Skill default (30/20/20/15/15) | 2 (4.40) | 4 (3.60) | 1 (3.50) |
| Equal (20 each; DVF counted once) | 2 (4.47) | 4 (3.60) | 1 (3.53) |

Option 2 leads under all three. The 1st place does not depend on the weight adjustment. The 2nd/3rd gap is small
(0.15 to 0.07), so those two are close to a tie and should not be read as a ranking.

## 6. The three questions every direction must answer (compact)

| | Hidden items in View Data | Percentile lines and statistics | PDF / report |
|---|---|---|---|
| 1 Frame break | yes, unchanged | unchanged; lines at true values | edge glyphs and values print |
| 2 Chip + popover | yes, unchanged | unchanged | popover does not print: needs a one-line text stand-in if a chart ever enters a report |
| 3 Overview + detail | yes | unchanged | both views print, more space |
| 4 Switch | yes | unchanged | prints in the screen's state; the state must be labelled |
| 5 Re-scale | yes | unchanged values, but line spacing changes | prints as drawn |

Why "yes, unchanged" holds everywhere: the View Data payload for the scatterplot is built in `buildViewData` from the full
`cycleTimeData` list, independent of the chart, and the percentile values come from the backend over all closed items
(`ctx.percentileValues`). Every direction here changes only what one chart draws. The same code facts answer the
reports question today: the Report PDF export (epic 6298) and bulk reports (epic 6350) render report panels (Then / Now
numbers), and the `Reports` pages import no chart component, so no scatterplot is in a PDF at present.

## Gate G4

DVF applied; weights locked and justified; all five survivors scored on all four criteria; weighted ranking and
sensitivity done; recommendation follows the matrix (see `recommendation.md`).
