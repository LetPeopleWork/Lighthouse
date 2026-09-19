# Mutation testing — slice 04 (ADO Story #6036)

Frontend only. Nothing backend changed in this slice.

| Target | First run | After closing the gaps | Gate |
|---|---|---|---|
| `MetricsView/ragRules.ts` — `computeSleRiskRag` | 34/39 | **39/39** | — |
| `MetricsView/SleRiskWidget.tsx` | 8/22 whole-file | **7/7** scoped | — |
| `utils/charts/sleRisk.ts` — the at-risk summary | 15/15 | **15/15** | — |
| **Overall** | **75.00%** | **100.00%** (61/61) | ≥ 80% ✓ |

All three files appear in the per-file table on the final run, which is the check that matters:
StrykerJS takes **one span per entry** and a mis-typed span drops the file silently, leaving a clean
score describing nothing. The widget needs two entries for that reason — its one behavioural line
near the top, and its rendered value near the bottom.

Configs: `stryker.slice04.frontend.json` + `vitest.stryker.slice04.ts` here; the working copies in
`Lighthouse.Frontend/` are gitignored as local tooling.

## The four real gaps, all of them unasserted user-visible text

The first run's 25% shortfall was one implementation detail and three sentences nobody had pinned.

### 1. The empty board's status tip was never read

`wipCount === 0` returning green survived being deleted. Removing the guard does **not** divide by
zero into an error — `0 / 0` is `NaN`, and `NaN >= 0.15` is false, as is `NaN >= 1` — so the function
falls through to the final green and the *status* is identical. Only the wording differs: *"No Work
Items in progress, so none are at risk"* versus *"No Work Items are at risk of missing the SLE"*.

An empty board and a board with nothing at risk are both green and are not the same thing. The
status alone cannot say whether the guard is still there, so the tip is now asserted exactly.

### 2 and 3. The red and all-clear tips were never read either

Both could be emptied without a test noticing. The amber tip was covered only because the allowance
assertion happened to read it. Now all three are asserted as whole strings.

This matters more here than it usually would: the tip is where the **derived allowance** appears, and
keeping it there rather than beside the fixed threshold is the one rule the widget's whole
information design rests on. A tip nothing reads is a rule nothing enforces.

### 4. The widget's em-dash was asserted as "not a zero"

`"—" → ""` survived, because an empty string also satisfies *not a zero*. A team with no target would
have rendered an empty card and the test would still have passed. Now the dash itself is asserted —
an empty cell is a widget that looks broken, which is a different thing from one that says it has
nothing to measure.

## What was NOT counted, and why the widget is scoped

The whole-file widget run scored 8/22, and **13 of the 14 survivors were MUI `sx` object literals and
their strings** — `{ borderRadius: 2 }`, `"center"`, `"column"`, `"bold"`. This is the ledger's
documented dominant equivalent class in this frontend, and the standing rule is not to write tests
for margins.

Scoping the entry to the widget's two behavioural lines is what makes the number mean something: 7
mutants, all in the value the widget actually renders and the colour it renders it in, all killed.
The alternative — leaving it whole-file — would have reported 36% for a component whose logic is
fully covered, and the honest response to that number would have been to explain it away every time.

## The one thing mutation could not check, and it was checked by hand

`sleRiskAtRiskSummary`'s threshold moved from 50 to 70, and every test of the summary moved with it.
Mutation confirms the tests pin the new line; it cannot confirm the new line is the right one. That
was verified against real demo data instead — see the slice's DELIVER section.
