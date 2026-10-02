# Taste Evaluation — epic-5878-baseline (DIVERGE, Phase 4)

Inputs: the curated 6 in `options-raw.md`. Scoring started only after `options-raw.md` was complete.

## 1. Weights (locked before scoring)

| Criterion | Weight | Rationale |
|---|---|---|
| DVF (average) | 30% | Default. The evidence base is thin (DISCOVER G1-G3 fail), so desirability must not be swamped by taste. |
| T1 Subtraction | 20% | Default. The ValueFlow field set invites accumulation. |
| T2 Concept count | 20% | Default. The second reader is a sponsor who does not know flow metrics (`job-analysis.md` §3). |
| T3 Progressive disclosure | 15% | Default. |
| T4 Speed-as-trust | 15% | Default, not the developer-tool 25%: a report is opened occasionally, and creation computes once. |

Concept count is measured **beyond** the fixed frame (Reports, Template, selectable items with rules, frozen
"before", rolling "now"), which every option shares.

## 2. DVF filter

| # | Option | D | F | V | Total | Avg | Notes |
|---|---|---|---|---|---|---|---|
| 1 | Threshold scorecard | 3 | 5 | 3 | 11 | 3.67 | D: serves OS1/OS4, fails OS3 (a noisy metric crosses 10% by chance); same as Nave's model, so it does less to set Lighthouse apart (I2). F: arithmetic on existing values. |
| 2 | Signal catalog | 4 | 4 | 4 | 12 | 4.00 | D: OS2/OS3; the maintainer leans to signals. F: all PBC series exist with `asOf`; new work: classify Now points against limits frozen from the Then window. V: the differentiator (I1) that has to reach management. |
| 3 | Claims checklist | 4 | 4 | 4 | 12 | 4.00 | D: OS3 + OS4 + OS5 (claims are agreed before the numbers are seen); its phrasing is the maintainer's own ("{Cycle Time} trending down = good"). F: same engine as 2 plus a claim text per definition. V: as 2. |
| 4 | Rule rows | 3 | 2 | 3 | 8 | 2.67 | D: tuning has no evidence of demand; it also re-opens OS5 (rules tuned after looking). F: a parameter store, an editor and validation for every rule kind. |
| 5 | Judged charts | 3 | 3 | 3 | 9 | 3.00 | D: suits coaches more than sponsors. F: frozen series per item and charts across two windows; PDF later needs server-side charts (Spike 6052 shows this works, at a cost). |
| 6 | Expectation first | 2 | 4 | 3 | 9 | 3.00 | D: retroactive baselines (D12) make the "expectation" post-hoc, which defeats its purpose; no evidence that coaches state directions per metric. |

No option totals below 6. **None eliminated.**

## 3. Taste scoring

| # | T1 Sub | T2 Concept | T3 Prog | T4 Speed | Justification |
|---|---|---|---|---|---|
| 1 | 3 | 5 | 4 | 4 | T1: ~10 metrics plus delta plus % — several removable parts. T2: zero new concepts. T3: one table. T4: values computed once. |
| 2 | 4 | 4 | 4 | 4 | T1: verdict + values; the "table of metrics" frame could still go. T2: one concept ("no change detected" vs a real shift), anchored to the PBC that Lighthouse users already have. T3: the table opens on verdicts. T4: Now series computed live against small frozen limits. |
| 3 | 5 | 4 | 5 | 4 | T1: a claim fuses metric, direction and rule; nothing left to remove. T2: one concept, the four verdict states. T3: first view is a short list of sentences with a verdict; values sit on the row and the chart on expand. T4: as 2. |
| 4 | 2 | 2 | 2 | 3 | T1: parameters per row per report. T2: rule kinds, parameters and overrides. T3: assemble before seeing anything. T4: recompute on each tuning. |
| 5 | 3 | 3 | 3 | 3 | T1: one chart per item is a lot to show a sponsor. T2: the sponsor has to read limits and runs. T3: all charts up front. T4: heavier payload and rendering. |
| 6 | 3 | 3 | 2 | 4 | T1: an expectation per item. T2: expectation plus verdict. T3: a creation step before the first view. T4: compute as 2. |

## 4. Weighted matrix

Final = 0.30·DVF + 0.20·T1 + 0.20·T2 + 0.15·T3 + 0.15·T4

| Rank | Option | DVF | T1 | T2 | T3 | T4 | **Weighted** |
|---|---|---|---|---|---|---|---|
| 1 | **3 Claims checklist** | 4.00 | 5 | 4 | 5 | 4 | **4.35** |
| 2 | 2 Signal catalog | 4.00 | 4 | 4 | 4 | 4 | **4.00** |
| 3 | 1 Threshold scorecard | 3.67 | 3 | 5 | 4 | 4 | **3.90** |
| 4= | 5 Judged charts | 3.00 | 3 | 3 | 3 | 3 | 3.00 |
| 4= | 6 Expectation first | 3.00 | 3 | 3 | 2 | 4 | 3.00 |
| 6 | 4 Rule rows | 2.67 | 2 | 2 | 2 | 3 | 2.35 |

## 5. Sensitivity

| Test | Change | 3 | 2 | 1 | Leader |
|---|---|---|---|---|---|
| S1 Developer-tool weights | 25/15/20/15/25 | 4.30 | 4.00 | 3.97 | 3 |
| S2 Concept-heavy | 25/15/35/10/15 | 4.25 | 4.00 | 4.12 | 3 |
| S3 Feasibility of 3 overrated | F3 → 3 | 4.25 | 4.00 | 3.90 | 3 |
| S4 Sponsors reject "No change yet" | D of 2 and 3 → 3 | 4.25 | 3.90 | 3.90 | 3 |
| S5 Claim verdict states count as two concepts | T2 of 3 → 3 | 4.15 | 4.00 | 3.90 | 3 |

Option 3 leads under every test. Option 1 overtakes Option 2 only under S2, and does not reach Option 3.

## 6. Pool scoring — Community/Premium line (item 9)

Separate rubric, locked before scoring: **L** the management-facing result reaches management intact on
Community (40%) · **C** conversion pressure at a moment of shown value (25%) · **E** enforceable, with
precedent (20%) · **S** simple to explain (15%).

| # | Lever | L | C | E | S | Weighted | Verdict |
|---|---|---|---|---|---|---|---|
| P1 | Per-owner cap (2 per Team/Portfolio) | 5 | 4 | 5 | 5 | **4.75** | **Adopt** — `AdditionalFieldsHelper` precedent; the first engagement and its hand-over fit in Community |
| P2 | Per-instance cap (3 total) | 4 | 4 | 5 | 4 | 4.20 | Runner-up; hits a coach covering many Teams before value is shown |
| P4 | Export gate (PDF / email) | 4 | 4 | 4 | 5 | 4.15 | **Decide when export is built**; see the dissent in `recommendation.md` |
| P5 | Template gate (later templates one by one) | 5 | 2 | 4 | 4 | 3.90 | Adopt as policy, no effect in v1 |
| P7 | Item gate (extended catalog Premium) | 2 | 3 | 4 | 2 | 2.65 | Reject: splits the rule list a sponsor reads |
| P3 | Rule gate (PBC rules Premium) | 1 | 3 | 4 | 3 | 2.40 | Reject: the Community result would be the noisy one |
| P6 | Retention gate | 1 | 3 | 3 | 3 | 2.20 | Reject: breaks the frozen promise (D10) |

## 7. Pool scoring — template name (item 10)

Rubric: no collision with "Set Baseline for Process Behaviour Chart" (C3, 30%) · plain to a sponsor (25%) ·
neutral when things got worse (20%) · fits the 4-week assessment, where there is no real "after" (15%) · a
naming pattern later templates can follow (10%).

| # | Name | Coll. | Plain | Neutral | 4-week | Pattern | Weighted |
|---|---|---|---|---|---|---|---|
| N3 | **Then & Now** | 5 | 4 | 5 | 5 | 3 | **4.55** |
| N2 | Before & After | 5 | 5 | 5 | 3 | 3 | 4.50 |
| N5 | Starting Point Comparison | 5 | 4 | 5 | 4 | 3 | 4.40 |
| N4 | Flow Check | 5 | 3 | 5 | 4 | 4 | 4.25 |
| N6 | Change Review | 5 | 4 | 3 | 3 | 4 | 3.95 |
| N1 | Baseline Comparison | 1 | 4 | 5 | 4 | 4 | 3.30 |

## 8. Evidence found after scoring (recorded, not re-scored)

`XmRCalculator.Calculate(baselineValues, displayValues)` (`Services/Implementation/XmRCalculator.cs:15`)
already derives limits from one series and classifies the points of another. That is exactly the "judge Now
against limits frozen from Then" step that Options 2, 3, 5 and 6 need. Their Feasibility would be 5, not 4
(+0.10 weighted each). Option 3 still leads and every gap widens or holds, so the matrix stands as
scored. Two limits come with it: the lower limit and the lower sigma lines are clamped at zero, and a rule
that depends on a clamped line is disabled. So for low-count daily series (Throughput, WIP) a downward signal
can only come from the run rule (8 in a row below the average), never from a point below the lower limit.
Classifications carry no direction either: the rule has to read direction from the point's side of the
average.

## Gate G4

- [x] DVF filter applied (no eliminations; reasons recorded).
- [x] Weights locked and justified before scoring.
- [x] All 6 options scored on all criteria; matrix and breakdown complete.
- [x] Sensitivity run; the leader is stable.
- [x] Premium and naming pools scored with their own rubrics, locked before scoring.

**G4: PASS.**
