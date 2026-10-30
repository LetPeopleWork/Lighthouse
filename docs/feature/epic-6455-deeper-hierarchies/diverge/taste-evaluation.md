# Taste Evaluation — Epic 6455 "Deeper hierarchies"

Wave: DIVERGE, phase 4 · 2026-10-30. Options from `options-raw.md`; job and outcomes from `job-analysis.md`;
evidence from `competitive-research.md` and its base (Epic 5806). Weights were locked before any option was
scored.

## 1. Weights (locked before scoring)

| Criterion | Weight | Default | Why it moved |
|---|---|---|---|
| DVF (average) | 30% | 30% | unchanged |
| T1 Subtraction | 15% | 20% | Lowered: every option adds one capability; the sharper test here is how many *concepts* it adds (T2) |
| T2 Concept count | 20% | 20% | unchanged: a config-admin who misunderstands the model misconfigures it, which is the failure in E1 |
| T3 Progressive disclosure | 20% | 15% | Raised: constraint C5 (two-level instances behave exactly as today) and OS3 (the admin sees what is unreached) are both disclosure questions; most users must see nothing new |
| T4 Speed-as-trust | 15% | 15% | unchanged: forecasts run in the background, so speed here means refresh cost and whether a page reads stored results |

Sensitivity (section 5) re-runs the matrix with the default and the developer-tool weights.

## 2. DVF filter (elimination threshold: total < 6)

Rubric anchors used:
- **D**: does it serve OS1/OS2 (the promise-level forecast), OS4 (a level above it), OS5 (named, visible levels,
  the maintainer's direction), E2 (Customer A wants Change "when", Change flow metrics and a Project/Program
  roll-up), and the structure table in `options-raw.md`? Judged against maintainer decision 4: an option's
  value from jumping a level counts for nothing.
- **F**: cost on today's code (`../feature-delta.md` inventory), across Jira, Azure DevOps, Linear and CSV.
- **V**: a sustainable Premium line, fit with the existing product concepts, no harm to the installed base.

| Option | D | F | V | Total | Avg | Verdict | Rationale |
|---|---|---|---|---|---|---|---|
| 1 Level stack above the Feature | 5 | 3 | 4 | 12 | 4.00 | pass | D: serves both shapes, every required setup, all three asks in E2, every level named and visible. F: N-level parent fetch per connector (CSV needs S7/S8 lifted), composition per upper item per round; the joint maths exists (ADR-113/155/156). V: Premium above the Feature matches the market norm (Jira Plans, Linear Enterprise) |
| 2 Stacked Portfolios | 4 | 3 | 4 | 11 | 3.67 | pass | D: both shapes, any depth, every level a full page; fits "Team → ART → Program" if those are groups. F: composition *across* Portfolios and their refresh order are new (today each Portfolio forecasts alone). V: Premium is inherent (free tier = one Portfolio, `docs/licensing/licensing.md:118`) |
| 3 Instance-wide Level Model | 5 | 2 | 4 | 11 | 3.67 | pass | D: the fullest form of the maintainer's direction. F: replaces the fixed Feature role across pages, metrics, Terminology, access scopes, clients and the API; high cost and wide blast radius. V: strong Premium story, but every instance migrates |
| 4 Look-through levels (relay) | 3 | 3 | 3 | 9 | 3.00 | pass | D: fixes shape (a)'s forecast, nothing for shape (b) or Project/Program; its distinguishing value (hiding the intermediate) is what decision 4 made unnecessary. F: intermediate fetch plus a rework of Team Feature WIP and of intermediate default sizing (OS6). V: free repair; no new value to sell |
| 5 Query-scoped forecast items | 3 | 2 | 3 | 8 | 2.67 | pass | D: fixes shape (a)'s forecast and handles links and many-to-many, but no visible levels, no Project roll-up. F: Jira needs a descendant function from the tracker or an app [I]; Linear and CSV cannot; one query per Feature per refresh. V: an expert setting, little to show |
| 6 Parent-grouped Deliveries | 3 | 4 | 2 | 9 | 3.00 | pass | D: Change "when" yes; Change flow metrics no (Deliveries have no tracker states); Project needs a new Delivery-of-Deliveries. F: one new source in the existing registry (ADR-166). V: floods the Delivery list with generated rows beside real releases and works against Deliveries as durable records (ADR-160, ADR-164); Epic 5806 kept Deliveries separate for this reason (D8) |

No option falls below 6; all six go to taste scoring.

## 3. Taste scores

### T1 Subtraction (could it reach its goal with one fewer part?)

| Option | Score | Why |
|---|---|---|
| 1 | 4 | Level = name + types + attach field. What a level answers is fixed (when, flow, progress), not configured, so no per-level "answers" switchboard. The per-level Terminology could fold into the level's name |
| 2 | 4 | One new part: "source = another Portfolio". Per-Portfolio Terminology rides along |
| 3 | 2 | Level registry, per-connection type mapping, per-level roles, tracker discovery, Portfolio level ranges |
| 4 | 4 | One setting (relay types), plus intermediate default size |
| 5 | 4 | One setting (the query template) |
| 6 | 4 | One source; the second tier (grouping Deliveries) is an extra part |

### T2 Concept count (new mental concepts for a first-time admin)

| Option | Score | Why |
|---|---|---|
| 1 | 4 | One concept, "a level above {feature}", anchored to the Parent column users already see |
| 2 | 3 | Two: a Portfolio fed by a Portfolio, and a Feature word per Portfolio |
| 3 | 2 | A new mental model: "Feature" becomes one level among N, Teams and Portfolios are placed on levels |
| 4 | 4 | One: "Work Items reach {features} through these types". Hidden semantics (Team Feature WIP counting a level the Team does not see) count against it only lightly |
| 5 | 3 | Two: a query template with a key placeholder, and "a Feature's Work Items are a query result" |
| 6 | 3 | Two: a generated Delivery, and a Delivery group for a second level |

### T3 Progressive disclosure (does a first use expose only what it needs?)

| Option | Score | Why |
|---|---|---|
| 1 | 5 | A two-level user sees nothing new. One action, "Add a level above {feature}", reveals the rest; a level page appears only once a level exists |
| 2 | 4 | Nothing new until a second Portfolio exists; then one source choice in the wizard |
| 3 | 3 | Seeded with two levels, but a Levels page sits first in setup for everyone |
| 4 | 4 | One optional setting on the Portfolio |
| 5 | 3 | One optional setting, but using it needs tracker query knowledge up front |
| 6 | 4 | One optional source on the Deliveries tab |

### T4 Speed-as-trust (latency, waits, stale reads)

| Option | Score | Why |
|---|---|---|
| 1 | 4 | Composed inside the same forecast round from the same trials; pages read stored results. One extra parent fetch per level, over distinct parents only |
| 2 | 3 | An upper Portfolio waits for the lower one's forecast; chained refreshes can show an upper date older than the lower one |
| 3 | 3 | Like 1 at run time, but every page and query widens to N levels |
| 4 | 3 | Fetches every intermediate item each refresh, including ones nobody sees |
| 5 | 2 | One tracker query per Feature per refresh (hundreds on a large Portfolio, cf. Epic 6400's 920-Feature lists) |
| 6 | 4 | Joint forecast per Delivery already stored; no extra fetch |

## 4. Weighted matrix

| Option | DVF avg | T1 | T2 | T3 | T4 | Weighted total | Rank |
|---|---|---|---|---|---|---|---|
| **1 Level stack above the Feature** | 4.00 | 4 | 4 | 5 | 4 | **4.20** | 1 |
| 2 Stacked Portfolios | 3.67 | 4 | 3 | 4 | 3 | 3.55 | 2 (tie; D 4 > 3) |
| 4 Look-through levels (relay) | 3.00 | 4 | 4 | 4 | 3 | 3.55 | 3 (tie) |
| 6 Parent-grouped Deliveries | 3.00 | 4 | 3 | 4 | 4 | 3.50 | 4 |
| 5 Query-scoped forecast items | 2.67 | 4 | 3 | 3 | 2 | 2.90 | 5 |
| 3 Instance-wide Level Model | 3.67 | 2 | 2 | 3 | 3 | 2.85 | 6 |

Arithmetic (0.30·DVF + 0.15·T1 + 0.20·T2 + 0.20·T3 + 0.15·T4):
1 = 1.20+0.60+0.80+1.00+0.60 · 2 = 1.10+0.60+0.60+0.80+0.45 · 4 = 0.90+0.60+0.80+0.80+0.45 ·
6 = 0.90+0.60+0.60+0.80+0.60 · 5 = 0.80+0.60+0.60+0.60+0.30 · 3 = 1.10+0.30+0.40+0.60+0.45.

Tie-break between 2 and 4: Desirability (2 serves shape (b) and visible levels; 4 does neither, and decision 4
removed its unique value).

## 5. Sensitivity

| Weights | 1 | 2 | 4 | 6 | 5 | 3 | Winner |
|---|---|---|---|---|---|---|---|
| Locked (30/15/20/20/15) | 4.20 | 3.55 | 3.55 | 3.50 | 2.90 | 2.85 | 1 |
| Skill default (30/20/20/15/15) | 4.15 | 3.55 | 3.55 | 3.50 | 2.95 | 2.80 | 1 |
| Developer tool (25/15/20/15/25) | 4.15 | 3.47 | 3.50 | 3.55 | 2.82 | 2.82 | 1 |
| Desirability alone in place of the DVF average (maintainer-preference test) | 4.50 | 3.65 | 3.55 | 3.50 | 3.00 | 3.25 | 1 |

Option 1 wins under every weighting tried; the places behind it trade among 2, 4 and 6 by a few hundredths,
which is inside scoring noise. Option 3 rises most when only desirability counts, which is the honest reading
of the maintainer's preference: the direction is wanted, the full re-model is what costs (see the dissent in
`../recommendation.md`).

**Gate G4 (inputs)**: DVF applied; weights locked and documented; all six options scored on all criteria;
ranking produced; sensitivity checked. Recommendation and dissent: `../recommendation.md`.
