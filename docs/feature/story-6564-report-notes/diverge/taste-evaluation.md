# Taste evaluation — story-6564-report-notes (DIVERGE, rev. 1)

Inputs: the six options in `options-raw.md` (rev. 1). Scoring began after that file was complete. rev. 1 replaces the
earlier six, applies one constraint rule to all of them, and re-scores.

## 1. Weights (locked before scoring)

Locked 2026-11-26, before any score in this file was written, from the default column of `nw-taste-evaluation`. They
equal the weights of Epic 5878 and Story 6535 (same report, same readers, same thin evidence), so they do not
depend on which option wins.

| Criterion | Weight | Rationale |
|---|---|---|
| DVF (average) | 30% | Default; two voices, one organisation, so desirability must not be swamped. |
| T1 Subtraction | 20% | Default. |
| T2 Concept count | 20% | Default; the reader does not know Lighthouse. |
| T3 Progressive disclosure | 15% | Default. |
| T4 Speed-as-trust | 15% | Default; a report is opened occasionally. |

Concept count is counted beyond what every option shares: the report and its live view.

## 2. Constraint check (one rule) and DVF filter

Constraints from the maintainer: (a) the author is shown when auth is on and no author when auth is off; (b) visible
in the live report; (c) kept out of print and PDF; (d) generic: the mechanism works for every report type and both
owner kinds (Team and Portfolio).

**Rule, applied to every option the same way**: Pass = the mechanism meets the constraint as described. Fail = it
cannot meet it without becoming a different option. N/A = the option produces nothing the constraint is about (for
(a): nobody writes anything). One Fail removes the option before DVF. Then the DVF total below 6 removes it.

| Option (mechanism) | (a) author | (b) live | (c) not in print | (d) generic | Result |
|---|---|---|---|---|---|
| 1 notes list kept by the report | Pass: author per note | Pass | Pass: list drawn in the live view only | Pass: keyed on `Report`, one model for every template | stays |
| 2 Team size value on the Team | N/A: a setting, nobody writes a note | Pass: header | Pass | **Fail**: the value sits on `Team.cs`; a Portfolio report has no Team size (no member count found in `Models/`) | eliminated |
| 3 event marks on the charts | Pass: author per event | Pass | Pass: marks drawn in the live view only | **Fail**: a comparison panel has no day axis to mark | eliminated |
| 4 context step in the wizard | Pass, at a cost: author is the creator; `Report.cs` has no creator field today | Pass | Pass | Pass: every template has a wizard | stays |
| 5 events kept once, listed by date | Pass: author per event | Pass | Pass | Pass: a Portfolio report lists organisation-level events (an assumption for DISCUSS) | stays |
| 6 generated line | N/A: nobody writes | Pass | Pass | Pass: counts and period exist for every report | stays |

DVF rubric of `nw-taste-evaluation`: Desirability "1 = no evidence of want, 5 = clear expressed need"; Feasibility
"1 = requires unavailable technology, 5 = straightforward to build"; Viability "1 = no path to revenue/retention,
5 = clear value capture". Eliminate if total < 6.

| Option | D | F | V | Total | Avg | Reasons |
|---|---|---|---|---|---|---|
| 1 | 4 | 5 | 4 | 13 | 4.33 | D: serves what happened (workshop case 2) and how large the Team is as text. F: a copy of the Deliveries seam keyed on `Report`. V: a pattern the product already ships. |
| 4 | 3 | 3 | 3 | 9 | 3.00 | D: serves how large the Team is; what happened is frozen at creation. F: a payload per template, a wizard step, a creator field. V: no pattern shipped for it. |
| 5 | 4 | 3 | 4 | 11 | 3.67 | D: strong for what happened; how large the Team is fits only as a dated event. F: a new entity and a date-overlap rule. V: fits an organisation-level context the product is moving towards (ADR-270 context map). |
| 6 | 1 | 1 | 3 | 5 | 1.67 | D: neither workshop case can be derived from data held (no member count or assignee found in `Models/`): no evidence of want. F: the data is unavailable in the models searched. V: no author, so no accountable reader context. |

**Eliminated before taste scoring: option 2 and option 3 on constraint (d); option 6 on DVF (5 < 6).**
Survivors: options 1, 4, 5.

## 3. Taste scores, with the rubric level quoted

| Option | T1 Subtraction | T2 Concept count | T3 Progressive disclosure | T4 Speed-as-trust |
|---|---|---|---|---|
| 1 | 4: "One minor element could be removed; core intact" (the edit and delete controls) | 4: "One new concept, well-anchored to something familiar" (a note, anchored to Deliveries notes) | 4: "First interaction = core flow; secondary features one step removed" (list shown, add control one step away) | 4: "Minor latency well-masked by progress indicators" (one extra request when the report opens) |
| 4 | 3: "Multiple removable elements, value unclear without them" (the step, fixed fields, the block, the edit path) | 3: "Two new concepts, introduced sequentially" (context fields at creation; frozen unless edited) | 3: "First interaction exposes 2-3 features; sequencing is logical" (the wizard grows a step) | 5: "Instant feedback; every action has immediate response" (stored in the report payload, no extra request) |
| 5 | 3: "Multiple removable elements, value unclear without them" (store, selection rule, management place) | 2: "Three or more concepts, some interdependent" (an event store, a date-overlap rule, a place to manage them) | 3: "First interaction exposes 2-3 features; sequencing is logical" (written in one place, read in another) | 4: "Minor latency well-masked by progress indicators" (one extra request) |

## 4. Weighted matrix

Weighted = 0.30 x DVF avg + 0.20 x T1 + 0.20 x T2 + 0.15 x T3 + 0.15 x T4.

| Rank | Option | name_origin | DVF | T1 | T2 | T3 | T4 | **Weighted** |
|---|---|---|---|---|---|---|---|---|
| 1 | 1 notes to reports | user-words ("notes to reports") | 4.33 | 4 | 4 | 4 | 4 | **4.10** |
| 2 | 4 context step in the wizard | coined | 3.00 | 3 | 3 | 3 | 5 | **3.30** |
| 3 | 5 events kept once, listed by date | coined | 3.67 | 3 | 2 | 3 | 4 | **3.15** |

Check. Option 1: 0.30 x 4.33 = 1.30; + 0.8 + 0.8 + 0.6 + 0.6 = 4.10. Option 4: 0.90 + 0.6 + 0.6 + 0.45 + 0.75 = 3.30.
Option 5: 1.10 + 0.6 + 0.4 + 0.45 + 0.6 = 3.15.

Margins: option 1 leads option 4 by 0.80 (DVF +0.40, T1 +0.20, T2 +0.20, T3 +0.15, T4 -0.15). Option 4 leads option 5
by 0.15.

Note on the basis of the scores: desk judgement against the rubrics, on thin evidence (two informants, one workshop).
The ranking shows which direction fits the rubrics, not a measured preference.
