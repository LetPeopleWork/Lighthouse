# Recommendation — epic-5878-baseline (DIVERGE → DISCUSS)

**Agent**: Flux (`nw-diverger`) · **Date**: 2026-10-02 · Derived from `diverge/taste-evaluation.md`. The fixed
maintainer decisions D8-D16 (`wave-decisions.md`) are taken as given and not re-opened.

## Decision for DISCUSS

> **Proceed with Option 3, "Claims checklist" (4.35 / 5).** The selectable unit is a plain-language **claim**
> (for example "{Cycle Time} is trending down"). One registered definition fuses the metric source, the
> direction of good and the rule. Rules are process-behaviour-chart rules, judged against limits frozen from
> the "Then" window, and a percentage threshold is used only where no series exists. The v1 catalog has
> 6 claims, the template is called **"Then & Now"**, and Community holds **2 reports per Team/Portfolio**.
> This assumes sponsors accept "No change yet" as an honest answer when the numbers differ (risk R-A below).

## 1. Top 3

### Option 3 — Claims checklist — 4.35

- **Why it scores well**: the strongest on Subtraction (5) and Progressive disclosure (5). A claim carries
  metric, direction and rule in one sentence. The first view is a short list of verdicts with Then → Now values
  on the row, and the chart opens on expand. Its phrasing is the maintainer's own ("Cycle Time trending down =
  good"). Claims are chosen before the numbers are seen, which serves OS5.
- **Core trade-off**: one metric can appear in several claims (trending down, predictable, 85th percentile
  lower), so the catalog grows by claims, not by metrics. Every claim also needs a sentence that uses the
  Terminology tokens.
- **Key risk**: sponsors read "No change yet" as failure, or as the tool dodging the question (R-A).
- **Hire criteria**: a coach who must put a defensible, noise-resistant answer in front of a non-expert sponsor.

### Option 2 — Signal catalog — 4.00

- **Why it scores well**: it has the same rule engine and the same resistance to noise as Option 3, laid out as a table of
  metrics with verdicts. It is familiar to anyone who has seen a scorecard.
- **Core trade-off**: the grain is the metric, so "predictable" and "trending down" for one metric need two
  rows or a second column. The reader still scans a table.
- **Key risk**: as Option 3.
- **Hire criteria**: practitioner audiences who think in metrics, not sentences.

### Option 1 — Threshold scorecard — 3.90

- **Why it scores well**: zero new concepts (T2 5). Before | Now | Δ | % is what Nave and Google Analytics
  users already know. It is also the cheapest to build.
- **Core trade-off**: a fixed percentage judges a noisy metric as "better" or "worse" by chance (OS3). It is the
  market default, so it does less to set Lighthouse apart (I1, I2).
- **Key risk**: a sponsor acts on a 12% "improvement" that is routine variation, and the coach's credibility
  takes the hit when it reverses.
- **Hire criteria**: audiences that want the number and trust the coach to narrate the caveats.

## 2. Dissenting case

The scoring almost chose **Option 1, Threshold scorecard**. Option 2 is closer on points (4.00), but it shares
Option 3's engine, so choosing between 2 and 3 is a presentation choice DISCUSS can still revisit cheaply.
Option 1 is the genuinely different philosophy. It leads every option on concept count and cost, and it passes
Option 2 when concept count is weighted heavily (S2: 4.12 vs 4.00). Its case: management already reads
percentages, and "No change yet" next to a 15% drop will look evasive.

**Why it loses**: the job's under-served outcomes are OS2 and OS3 (`job-analysis.md` §6). A percentage
threshold leaves OS3 unserved, and that is precisely where Nave, the closest competitor, stops (I2).

**Pre-committed trigger**: if interview protocol Q5 ("did anyone doubt the numbers?") or the first sponsor
showings show that sponsors reject "No change yet", do not swap directions. Add the Then → Now percentage
to the claim row (it is already shown as values), and make `ThresholdChange` available to more claims. The
rule-kind seam makes this one class and a few registrations.

## 3. Answers to the open items

### Item 7 — Signal vs noise: the v1 catalog, its rules and the extension seam (shaped by D16)

**Verdict states** (the same for every claim): **Holds** · **Does not hold** · **No change yet** · **Not enough
data**. Colour is used only for Holds and Does not hold. "Not enough data" reuses the existing PBC `NotReady`
state and the minimum-data guards, and never shows a zero (C4).

**Rule kinds** (v1, closed set, one class each):

| Rule kind | Judged against | Holds | Does not hold | No change yet |
|---|---|---|---|---|
| `ShiftInDirection(dir)`, "trending {dir}" | Now-window points classified with XmR limits **frozen from Then** (`XmRCalculator.Calculate(thenValues, nowValues)`) | ≥1 special cause on the good side of the Then average and none on the bad side | any special cause on the bad side | no special cause |
| `NoShiftAgainst(dir)`, "stable or {dir}" | as above | no special cause on the bad side (a shift in the good direction also holds) | any special cause on the bad side | n/a |
| `NoSignalsInWindow`, "predictable" | the Now window on **its own** limits (what the Metrics tab shows today) | no special cause | any special cause | n/a. Then's own signal state is frozen and shown for reference. |
| `ThresholdChange(dir, pct)`, used only where no series exists | Then value vs Now value | moved ≥ pct in dir | moved ≥ pct against dir | within ± pct |

"Special cause" means the four existing classes: a point beyond a natural process limit, 2 of 3 beyond 2σ, the
moderate-shift rule, and 8 in a row on one side of the average (`XmRCalculator`). **"Trending" means a
detected shift, not a regression slope.** A slope needs no limits and would treat noise as a trend, which is
exactly what OS3 rejects. Lower lines clamp at zero (`XmRCalculator.cs:46`), so on low-count daily series a
downward claim can only hold through the run rule. The claim's detail text must say so rather than imply
symmetry. If both sides fire, the verdict is Does not hold and the detail lists both.

**v1 catalog (6 claims, Team and Portfolio):**

| # | Claim (Terminology tokens in braces) | Source | Rule | Then → Now values shown | Preselected |
|---|---|---|---|---|---|
| C1 | {Cycle Time} is trending down | Cycle Time PBC (per finished {Work Item}) | `ShiftInDirection(down)` | 50th / 70th / 85th percentile | yes |
| C2 | {Throughput} is trending up | daily {Throughput} PBC | `ShiftInDirection(up)` | weekly median, total | yes |
| C3 | {WIP} is stable or down | daily {WIP} PBC | `NoShiftAgainst(down)` | average, range (min-max) | yes |
| C4 | Total {Work Item Age} is stable or down | daily Total {Work Item Age} PBC | `NoShiftAgainst(down)` | average daily total, average {Work Item Age} | yes |
| C5 | {Cycle Time} is predictable | Cycle Time PBC, Now window | `NoSignalsInWindow` | Then and Now signal counts | no |
| C6 | 85th percentile {Cycle Time} is lower | percentile over each window | `ThresholdChange(down, 10%)` | 85th percentile, with the {SLE} as reference if set | no |

C6 is ValueFlow's "SLE 85th percentile". ValueFlow shows 90 and 30 days as separate fields; here the Then and
Now window lengths do that job. C6 is the one threshold claim, kept because sponsors know that number. Days
since the Then window ended is shown as **context in the header**, not as a claim.

**Nice-to-have (one registration each, later):** {Throughput} is predictable · fewer {SLE} breaches (needs an
{SLE}; absent otherwise) · arrivals balanced with {Throughput} · {WIP} streaks · {Feature} size stable
(Portfolio only) · **Forecast lens (Team only)**, "more {Work Items} likely in the next 30 days at 85%", from
the existing `HowMany` run on the Then and Now {Throughput}. The Then result is frozen at creation.

**Extension seam.** It is one registered **`ComparisonItemDefinition`** per claim: key, owner kinds (Team /
Portfolio), source (series or summary provider), direction of good, rule kind plus parameters, the values to
show, claim text with Terminology tokens, and a minimum-data guard. Rule kinds implement one interface.
Adding a claim is one registration. Adding a rule kind is one class. A **Template** is a key, a default
selection of claim keys and default window lengths. The catalog does not depend on Reports, so a later
signal or alert surface (the D6 counter-signal) can reuse the same claims.

**Freezing (from D10).** At creation, freeze **every** catalog claim that applies to the owner, not only the
selected ones: Then values, frozen limits, Then signal state, and readiness. The selection can then change
later without recomputing. A claim added to the catalog after a report was created shows "Not captured
for this report" and is never recomputed silently (C5). Store a fingerprint of the settings in force
(cycle-time definition, state mapping, blocked rules, blackout) and show a notice when the Now side runs under
different settings. DISCUSS decides the wording.

**Configurability (the maintainer's "possibly configurable what's shown").** Yes to selecting claims (D16).
No to tuning rules or thresholds per report in v1: that is Option 4, which scored 2.35, and it re-opens OS5
(rules tuned after looking).

### Item 8 — Core v1 metric set vs nice-to-have

**Core**: Cycle Time 50/70/85th, Throughput weekly median (plus total), WIP average and range, Total Work Item
Age average plus average Work Item Age, and days since the Then window ended (header context). These are the
values carried by C1-C6.
**Nice-to-have**: the separate 30-day SLE field (covered by window choice), SLE breach count, Throughput
stability as a field (it is claim C5's sibling), WIP streaks, Feature size, arrivals, and the Team-only
forecast comparison. Each is one catalog registration later and none needs a model change.

### Item 9 — Community / Premium line

- **Community**: the "Then & Now" template with **every** claim and rule, viewed live, **up to 2 reports per
  Team or Portfolio** (P1, 4.75). Two reports cover an engagement-start report plus a hand-over report, or two
  experiments.
- **Premium**: unlimited reports per owner.
- **Licence lapse**: reports over the cap stay viewable and deletable but cannot be created. Frozen data is
  never deleted (D10). This copies the `AdditionalFieldsHelper` pattern.
- **Never gate**: rules or claims (P3 2.40, P7 2.65). A Community result that is the noisy one would carry
  the wrong evidence to management and break land-and-expand.
- **Later templates**: decided one by one (P5).
- **Export (PDF / email)**: decide when it is built (P4 4.15). Dissent: gating the management-facing PDF works
  against the land-and-expand mechanism. Prefer a Community PDF and Premium scheduled email or branding.

### Item 10 — Naming

| Rank | Template name | Score |
|---|---|---|
| 1 | **Then & Now** (recommended) | 4.55 |
| 2 | Before & After | 4.50 |
| 3 | Starting Point Comparison | 4.40 |
| 4 | Flow Check | 4.25 |
| 5 | Change Review | 3.95 |
| 6 | Baseline Comparison (collides with "Set Baseline for Process Behaviour Chart", C3) | 3.30 |

- **Labels**: windows are labelled **"Then"** (frozen, for example "Then: 90 days to 1 Aug 2026 — frozen") and
  **"Now"** (for example "Now: last 30 days").
- **"Baseline"**: no user-facing copy uses the word. It can stay as the internal and ADO name.
- **Terminology**: every metric word renders through the Terminology tokens ({Team}, {Portfolio},
  {Work Item}, {Cycle Time}, {Throughput}, {WIP}, {Work Item Age}, {SLE}, {Feature}). Copy never says Epic,
  Story or Initiative. "Then", "Now" and "Report" are not Terminology keys.
- **Close call**: "Then & Now" and "Before & After" are 0.05 apart. "Before & After" reads more plainly, but it
  promises an "after" that a 4-week assessment (D14) does not have. Either choice is defensible, and DISCUSS
  may decide on copy testing.

## 4. Risks carried into DISCUSS

| # | Risk | Mitigation / owner |
|---|---|---|
| R-A | Sponsors read "No change yet" as evasive | Values always on the row; the dissent trigger above; interview Q5 |
| R-B | The Now side runs under different settings than the frozen Then | Settings fingerprint and notice (DISCUSS wording) |
| R-C | Then window too short for credible limits (BaselineValidationService minimum is 14 days) | Readiness guard → Not enough data; DISCUSS picks a minimum Then length for PBC claims |
| R-D | Asymmetric downward detection on zero-clamped series | Claim detail text; DESIGN confirms per series |
| R-E | Evidence base: DISCOVER G1-G3 failed (D2) | Interview protocol continues; this is a learning investment |

## 5. Notes for DISCUSS

- DEVOPS question (C8): a candidate name-only event when a report is created. Template key as a closed enum
  property only if a KPI needs it.
- The checkpoint-timeline idea (C8-2) was set aside because it would re-open D11. Several reports per owner (D9)
  cover repeated checkpoints.
- Re-point the journey file's proposed job ids to `job-flow-coach-show-whether-flow-changed` (both journeys
  serve it; the 4-week shape is covered by D14).
