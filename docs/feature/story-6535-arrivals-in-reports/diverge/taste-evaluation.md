# Taste Evaluation — story-6535-arrivals-in-reports (DIVERGE, rev. 1)

Inputs: the 6 options (7 rows, B scored in two directions) in `options-raw.md`. Scoring started after that file
was complete. rev. 1 adds the rubrics, a reason for every cell, and the B-zero row.

## 1. Weights (locked before scoring)

Same as Epic 5878's Then & Now DIVERGE: the same report, the same reader (a coach, and a sponsor who does not know
flow metrics), and the same thin evidence (n=1).

| Criterion | Weight | Rationale |
|---|---|---|
| DVF (average) | 30% | Default; thin evidence, so desirability must not be swamped by taste. |
| T1 Subtraction | 20% | Default. |
| T2 Concept count | 20% | Default; the second reader does not know flow metrics. |
| T3 Progressive disclosure | 15% | Default. |
| T4 Speed-as-trust | 15% | Default; a report is opened occasionally and computed once. |

Concept count is measured beyond the frame every option shares: Then & Now report, metric checkbox, a panel
with Then, Now and change, per-day comparison.

## 2. Rubrics (from `nw-taste-evaluation`)

- **DVF**, each 1-5: Desirability (does it serve the job and the requester's words), Feasibility (fit with the
  existing seam), Viability (fits the product's direction and the report's decisions). Eliminate if total < 6.
- **T1 Subtraction**: 5 nothing removable · 4 one minor element removable · 3 several removable · 2 bloated ·
  1 accumulation.
- **T2 Concept count**: 5 zero new concepts · 4 one, well anchored · 3 two · 2 three or more · 1 new mental model.
- **T3 Progressive disclosure**: 5 first interaction = one action · 4 core flow, extras one step away ·
  3 exposes 2-3 features · 2 must choose between paths · 1 everything at once.
- **T4 Speed-as-trust**: 5 instant · 4 minor latency, masked · 3 noticeable latency, justified · 2 several
  waits · 1 blocking.

## 3. DVF filter

| Row | D | F | V | Total | Avg | Reasons |
|---|---|---|---|---|---|---|
| A | 3 | 5 | 4 | 12 | 4.00 | D: shows arrivals, but the balance the requester named (E2) is mental arithmetic across two panels. F: a copy of `ThroughputReportMetric` on daily arrivals; a neutral mark is a small addition. V: fits the catalog seam Epic 5878 left for it. |
| B-lower | 5 | 4 | 5 | 14 | 4.67 | D: E2's sentence and E3's Then-vs-Now in one panel. F: as A plus a gap line with signed wording; existing direction kinds. V: the parked "arrivals balanced with Throughput" item of Epic 5878. |
| B-zero | 4 | 3 | 4 | 11 | 3.67 | D: matches E3 but colours a slowing drain as better, against E2. F: a third direction kind through the comparison and the header change summary. V: fits the catalog, but "closer to zero" is a new rule kind the report never had. |
| C | 4 | 3 | 4 | 11 | 3.67 | D: answers the balance in one number. F: undefined when nothing finished (dash rules), hides volume. V: one product shows it (research); sponsors know ratios. |
| D | 4 | 3 | 4 | 11 | 3.67 | D: the balance is visible. F: the registry's "a panel draws only from its own props" breaks. V: changes an existing panel's meaning per report. |
| E | 2 | 4 | 3 | 9 | 3.00 | D: not what the requester asked; hides the cause. F: WIP history exists. V: overlaps the WIP panel. |
| F | 2 | 5 | 3 | 10 | 3.33 | D: a PDF or sponsor reader cannot follow a link into Lighthouse (Epic 6298's reader). F: two links. V: the report would stop being self-contained. |

None eliminated.

## 4. Taste scores, with reasons

| Row | T1 | T2 | T3 | T4 |
|---|---|---|---|---|
| A | 4: the total is context and could go | 4: one new idea, "a change with no good direction" | 5: one checkbox | 5: frozen at creation like every panel; no extra wait on open |
| B-lower | 4: {Throughput} per day repeats when both are ticked | 4: one new idea, the gap, built from two known rates | 5: one checkbox | 5: same as A |
| B-zero | 4: same as B-lower | 3: the gap plus "towards zero is good", which no other panel uses | 5: one checkbox | 5: same as A |
| C | 5: one number per side | 3: a unitless ratio plus "1.0 means balance" | 5: one checkbox | 5: same as A |
| D | 3: arrivals and gap lines appear inside another metric's panel | 4: one new idea, the gap | 3: one checkbox silently changes a second panel | 5: same as A |
| E | 4: start and end WIP could collapse to the change | 3: net flow and Little's law | 5: one checkbox | 5: same as A |
| F | 5: the report adds nothing | 5: the existing charts | 3: the reader leaves the report and reads two charts | 3: each open loads two chart pages; justified only by richer charts |

## 5. Weighted matrix

Weighted = 0.30 × DVF avg + 0.20 × T1 + 0.20 × T2 + 0.15 × T3 + 0.15 × T4.

| Rank | Row | Option | name_origin | DVF | T1 | T2 | T3 | T4 | **Weighted** |
|---|---|---|---|---|---|---|---|---|---|
| 1 | B-lower | Arrivals vs Throughput, lower gap is better | user-words | 4.67 | 4 | 4 | 5 | 5 | **4.50** |
| 2 | A | Arrivals panel | user-words | 4.00 | 4 | 4 | 5 | 5 | **4.30** |
| 3 | C | Arrivals per finished Work Item | coined | 3.67 | 5 | 3 | 5 | 5 | **4.20** |
| 4 | B-zero | Arrivals vs Throughput, gap towards zero is better | user-words | 3.67 | 4 | 3 | 5 | 5 | **4.00** |
| 5 | F | Open on Metrics | coined | 3.33 | 5 | 5 | 3 | 3 | **3.90** |
| 6 | E | WIP grew by | coined | 3.00 | 4 | 3 | 5 | 5 | **3.80** |
| 7 | D | Throughput with arrivals | coined | 3.67 | 3 | 4 | 3 | 5 | **3.70** |

## 6. Top 3

### B-lower — Arrivals vs Throughput panel — 4.50 — name_origin: user-words
- **Pro**: the requester's sentence and the maintainer's Then-vs-Now point in one panel; per day avoids the
  window-length trap; one checkbox; existing direction kinds.
- **Con**: {Throughput} per day appears twice when both metrics are ticked.
- **Risk**: the maintainer meant "towards zero" (B-zero, 4.00). That is a product decision for DISCUSS.
- **Hire when**: the reader wants to know whether demand outruns delivery and whether that improved.

### A — Arrivals panel — 4.30 — name_origin: user-words
- **Pro**: the simplest; mirrors the Throughput panel.
- **Con**: the balance is left to mental arithmetic across two panels.
- **Risk**: readers read arrivals alone as good or bad.
- **Hire when**: arrivals alone matter (e.g. a demand-shaping intervention).

### C — Arrivals per finished Work Item — 4.20 — name_origin: coined
- **Pro**: one scale-free number.
- **Con**: a dash when nothing finished; a new unitless idea for the sponsor; hides volume.
- **Hire when**: comparing owners of very different size.

## 7. Dissent (second place)

A could win if DISCUSS decides no value may repeat across panels (T1 for B would then fall) or that the
balance belongs to a later Signals-style comparison. A is a subset of B, so it can ship first and grow
without rework.
