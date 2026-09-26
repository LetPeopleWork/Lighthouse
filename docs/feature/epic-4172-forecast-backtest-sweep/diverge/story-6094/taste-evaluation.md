# Taste Evaluation — Story #6094

**Wave**: DIVERGE, light pass · Phase 4 · 2026-09-26 · Options from `options-raw.md` (curated 6).

## 1. Weights — locked before scoring

The skill's **Default** column, unmodified. The Epic replaced the taste criteria with five domain criteria; this story does not, because its question is presentation, which is what T1-T4 were written for. The Epic's honesty concerns enter as an unweighted invariant check (§2), not as a weight.

| Criterion | Weight | Why this column |
|---|---|---|
| DVF (average of D, F, V) | 30% | Default |
| T1 Subtraction | 20% | Default — the ask stacks table + grading + colour + text + dialog; subtraction is the live risk |
| T2 Concept Count | 20% | Default |
| T3 Progressive Disclosure | 15% | Default |
| T4 Speed-as-Trust | 15% | Default, not Developer Tool (25%): every option renders one already-fetched response (median 540 ms), so speed barely separates them |

**Tie-break, also locked before scoring**: higher Desirability wins — it is the lens that carries the maintainer's stated need.

## 2. Invariant check (unweighted; locked Epic decisions the maintainer has not reopened)

| Invariant | Source |
|---|---|
| I-a No per-window score, rank or winner anywhere, including text the client derives | DES-2, E5 |
| I-b Nothing writes a Team setting; no Apply | D4, E1 (ArchUnitNET) |
| I-c An unevaluable check is never blank and never looks calm | ADR-194, D9 |
| I-d No hard-coded renameable term | E7 |

**Reopened by the maintainer's ask, to be recorded in DISCUSS**: slice 02's "no matrix" and D2's "64 marks is unreadable". The maintainer has asked for exactly a matrix. That is his call; DISCUSS must record the reversal and keep I-a to I-d, which were what the "no matrix" rule protected.

| Option | Result |
|---|---|
| 1 Full Monte table | Pass with condition: the text summary must not tally correct cells per window (I-a) |
| 2 Graded grid only | Pass with condition: colour-only cells fail WCAG 1.4.1 unless glyphs carry the grade; a grey unevaluable cell risks I-c |
| 3 Inline + dialog | Pass |
| 4 Stated vs observed | Pass (aggregates by level, never by window) |
| 5 Words, then table | Pass with the same condition as 1 |
| 6 Grade against the promise | Pass |

No option is removed by the check.

## 3. DVF filter

| # | Option | D | F | V | Total | Verdict |
|---|---|---|---|---|---|---|
| 1 | Full Monte table in a dialog | **5** | 4 | 4 | 13 | Survives |
| 2 | Graded grid, numbers on demand | 3 | 4 | 4 | 11 | Survives |
| 3 | Sentence inline, table in a dialog | 3 | **5** | 4 | 12 | Survives |
| 4 | Stated versus observed | 3 | 3 | 4 | 10 | Survives |
| 5 | Words first, table as evidence | 4 | 4 | 4 | 12 | Survives |
| 6 | Grade against the promise | 3 | 4 | 3 | 10 | Survives |

- **1 D=5**: the maintainer asked for it in these words, and it is Brown's own format.
- **2 D=3**: hides the forecasts and the actual, which the story's own title asks to see.
- **3 D=3**: keeps the verdict inline, where the maintainer said "inline is horrible". **F=5**: the sentence ships already; only the dialog is new.
- **4 D=3, F=3**: not asked for; a new chart over a rate axis (DES-6 forbade a shared axis only across horizons, a 0-100% rate is commensurable, so this is legal but new).
- **6 V=3**: gives up Brown's recognisable scale, which is the Community / marketing hook.
- **F=4 for 1, 2, 5, 6**: the response already carries every number (`cells[].forecast`, `actualCompleted`, dates); the grading rule and its edge cases are new.

## 4. Scoring matrix

| # | Option | DVF avg | T1 | T2 | T3 | T4 | **Weighted** | Rank |
|---|---|---|---|---|---|---|---|---|
| 5 | **Words first, table as evidence** | 4.00 | 3 | 4 | 5 | 5 | **4.10** | **1** (tie-break: D 4 > 3) |
| 3 | Sentence inline, table in a dialog | 4.00 | 3 | 4 | 5 | 5 | **4.10** | 2 |
| 2 | Graded grid, numbers on demand | 3.67 | 4 | 3 | 5 | 4 | **3.85** | 3 |
| 1 | Full Monte table in a dialog (maintainer) | 4.33 | 3 | 3 | 3 | 5 | **3.70** | 4 |
| 6 | Grade against the promise | 3.33 | 4 | 3 | 3 | 5 | **3.60** | 5 |
| 4 | Stated versus observed | 3.33 | 4 | 2 | 4 | 5 | **3.55** | 6 |

```
5 = .30(4.00) + .20(3) + .20(4) + .15(5) + .15(5) = 1.20 + .60 + .80 + .75 + .75 = 4.10
3 = .30(4.00) + .20(3) + .20(4) + .15(5) + .15(5) = 1.20 + .60 + .80 + .75 + .75 = 4.10
2 = .30(3.67) + .20(4) + .20(3) + .15(5) + .15(4) = 1.10 + .80 + .60 + .75 + .60 = 3.85
1 = .30(4.33) + .20(3) + .20(3) + .15(3) + .15(5) = 1.30 + .60 + .60 + .45 + .75 = 3.70
6 = .30(3.33) + .20(4) + .20(3) + .15(3) + .15(5) = 1.00 + .80 + .60 + .45 + .75 = 3.60
4 = .30(3.33) + .20(4) + .20(2) + .15(4) + .15(5) = 1.00 + .80 + .40 + .60 + .75 = 3.55
```

## 5. Why the cells read as they do

- **T1 = 3 for 1, 3, 5**: each carries two statements of the same finding (text and table; or inline sentence and table). **4 for 2, 4, 6**: one primary view, one supporting element.
- **T2**: 1 = 3 (two new concepts — margin bands and a correctness colour scale that clashes with the forecast colours' existing meaning, see `job-analysis.md` §0). 2 = 3 (a six-state colour code with no numbers to anchor it). 3, 5 = 4 (the sentence and "held" are known; the margin band is the one new concept). 4 = 2 (a reliability diagram is a new mental model for most forecasters). 6 = 3 (the grade shifts meaning by level).
- **T3**: 1 = 3 — on open, 64 graded cells (80 off the ladder) plus text arrive together. 2, 3, 5 = 5 — one answer first, the depth on demand. 4 = 4. 6 = 3 (layout-neutral; inherits 1's).
- **T4**: 5 everywhere except 2 (= 4: a click per number read).

## 6. Sensitivity

- The maintainer's direction (1) scores highest on DVF (4.33) and loses on T2/T3 only. Setting T3 to 0% and DVF to 45%: 1 = 3.90, 5 = 3.95. **Option 5 still leads**, because 5 *is* option 1 with the text result moved to the top — the one change that moves T3 from 3 to 5.
- Flip the tie-break to Feasibility: 3 wins over 5. The tie-break was locked on Desirability before scoring because 3 contradicts an explicit instruction.
- Option 6 is scored as a direction but is layout-neutral: its grading rule can be applied to any of 1, 2, 3 or 5. It is carried as a risk list, not a rival layout (`recommendation.md`).
