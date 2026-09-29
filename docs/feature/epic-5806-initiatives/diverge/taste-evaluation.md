# Taste Evaluation — Epic 5806 "Visualize Initiatives"

Wave: DIVERGE, phase 4 · 2026-09-29 · scores the five directions in `options-raw.md`.

## 1. DVF filter (eliminate below 6)

| Option | Desirability | Feasibility | Viability | Total | Note |
|---|---|---|---|---|---|
| A Initiatives page | 4 | 4 | 5 | 13 | Dedicated Premium surface, clear demo moment |
| B Auto-Deliveries | 3 | 3 | 4 | 10 | Automatic lifecycle (create, rename, archive) is new work |
| C Features page grouping | 3 | 5 | 2 | 10 | Premium mode on a free page; grouping was free until 2025-11 |
| D Portfolio tab + strip | 3 | 4 | 4 | 11 | |
| E Heatmap | 4 | 3 | 5 | 12 | New chart component |

All pass. Desirability is capped at 4: no customer interview exists (DISCOVER gate G1 failed).

## 2. Weights (locked before scoring)

The epic-specific criteria replace the generic Apple/Jobs set, which they cover: snappy = speed-as-trust,
thin slice = subtraction and progressive disclosure, concept count kept as its own criterion.

**Documented adjustment (2026-09-29, maintainer input):** Lighthouse must fit many organisation
models, not only SAFe. Criterion F was added at 10%, taken from snappy (15 → 10) and demo-ability
(15 → 10). Weights were re-locked, then every option was re-scored on every criterion.

| Criterion | Weight | Why this weight |
|---|---|---|
| Q: answers both questions (when, on track) | 20% | The maintainer's scope |
| S: snappy, one glance across Portfolios | 10% | Stated requirement |
| R: reuses forecast / Delivery machinery | 15% | Cost and correctness |
| M: demo-ability for enterprise sales | 10% | The stated Premium motion |
| P: probabilistic roll-up correct (no summed percentiles) | 10% | Trust in front of executives |
| T: thin first slice | 10% | Unvalidated demand (DISCOVER) argues for small bets |
| C: concept count (new ideas the user must learn) | 10% | Executive-facing screens must need no explanation |
| B: Premium boundary clarity | 5% | Keep the free Parent column free |
| F: flexibility across org models (SAFe Epic, Initiative, OKR, Theme; mixed parent types) | 10% | Maintainer: Lighthouse stays wide, never SAFe-only |

F rubric: 5 = assumes nothing beyond "a Feature may have a parent", of any type, under any name.
3 = one structural assumption that fits some models poorly. 1 = hardcodes SAFe's (or any single
methodology's) vocabulary or structure.

## 3. Scores (1-5)

| Option | Q | S | R | M | P | T | C | B | F | **Weighted** |
|---|---|---|---|---|---|---|---|---|---|---|
| A Initiatives page | 4 | 4 | 4 | 4 | 5 | 4 | 4 | 5 | 5 | **4.25** |
| C Features page grouping | 3 | 3 | 5 | 3 | 5 | 5 | 5 | 2 | 5 | **4.05** |
| E Heatmap | 4 | 5 | 3 | 5 | 4 | 2 | 3 | 5 | 4 | **3.80** |
| D Portfolio tab + strip | 4 | 2 | 4 | 3 | 5 | 4 | 4 | 4 | 4 | **3.80** |
| B Auto-Deliveries | 5 | 2 | 5 | 3 | 4 | 3 | 2 | 5 | 3 | **3.70** |

Arithmetic, A: 4×.20 + 4×.10 + 4×.15 + 4×.10 + 5×.10 + 4×.10 + 4×.10 + 5×.05 + 5×.10 = 4.25.
D: 4×.20 + 2×.10 + 4×.15 + 3×.10 + 5×.10 + 4×.10 + 4×.10 + 4×.05 + 4×.10 = 3.80.

Naming variants on F (they apply to every option alike): N1 configurable term with neutral default
= 5; N3 neutral fixed word = 3; N2 tracker's own word = 3 (mixed types give mixed words and no page
title); N4 fixed "Epic" or "Initiative" = 1, rejected. **All options above are scored assuming N1.**

## 4. Score rationale (the non-obvious ones)

- **A, Q=4**: "When" comes free from stored forecasts. "On track" needs a target date that parents do
  not carry today, so one new stored fact (or a tracker read) is required.
- **A, P=5**: rows use the same joint composition as Deliveries. Multi-Portfolio Initiatives are
  correct too: Feature forecasts belong to Features and Teams, not Portfolios.
- **A and C, F=5**: the unit is "whatever each Feature's parent is". A SAFe Epic, an Objective and a
  Theme can sit side by side, from different Portfolios and trackers.
- **B, Q=5**: target date, likelihood, snapshots and so a trend over time, all inherited.
- **B, S=2**: Deliveries live per Portfolio; seeing all Initiatives needs a new list anyway.
- **B, P=4, C=2, F=3**: a Delivery belongs to one Portfolio, so a parent spanning two either splits
  or breaks. "Delivery" would mean both "a release" and "a strategic item". Every strategic item
  becomes a dated deliverable: fits a SAFe Epic, fits an OKR or a Theme poorly.
- **C, B=2**: the Features page is free; a Premium mode inside it is a blurry boundary, and
  re-gating a grouping that used to be free risks goodwill (DISCOVER A10).
- **C, M=3 and S=3**: a toggle on a long page is a weak demo moment; groups interleave with the
  unparented Features.
- **D, S=2, F=4**: one Portfolio at a time. It assumes the level nests inside a Portfolio (the
  SAFe-portfolio shape); a Theme cutting across Portfolios becomes a second-class case.
- **E, T=2, F=4**: new chart, colour scale, legend and accessibility make a large first slice; a
  fixed month grid presumes a planning horizon. **P=4**: correct maths, but cumulative shading is
  easily misread as "this month only".

## 5. Top 3

| Rank | Why it scores well | Core trade-off | Key risk | Hire when |
|---|---|---|---|---|
| 1 A page, 4.25 | 4 or 5 everywhere; one new concept; correct roll-up for free; model-agnostic | Less striking in a demo than a heatmap | Customers' data lacks the parent level (DISCOVER A3) | Every strategic item's date and status on one screen, now |
| 2 C grouping, 4.05 | Thinnest; no new place; equally flexible | Blurry Premium boundary, weak demo | Reads as the 2025-11 grouping coming back, gated | Validation is the goal, not sales |
| 3 E heatmap, 3.80 (ties D; ahead on demo-ability) | Best one-glance and demo device | Most build; cells misread | A striking chart on unvalidated demand | Shown on screen in a steering meeting |

## 6. Sensitivity

C only ties A (4.20 each) even if Premium boundary drops to 0% and its 5% moves to thin slice. E
overtakes A only if thin slice is dropped and its 10% moves to demo-ability (E 4.10, A 4.05). Moving
10% from snappy to "answers both questions" keeps A ahead of B (4.25 vs 4.00). None of these shifts
is supported by the maintainer's answers, so the weights stand.
