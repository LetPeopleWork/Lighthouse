# Mutation testing — Epic 5585 slice 02

**Date**: 2026-08-02 · **Stack**: frontend (StrykerJS + vitest runner) · **Story**: ADO #5615

## Result

| | mutants | killed | survived | score |
|---|---|---|---|---|
| **All files** | 210 | 170 | 34 | **80.95%** (83.33% of covered) |
| `DeliveryMetricsHistory.ts` | 131 | 115 | 16 | 87.8% |
| `DeliveryEpicSizeChart.tsx` | 73 | 55 | 18 | 75.34% |

Passes the project's 80% floor (`CLAUDE.md`, per-feature strategy). Config and raw report archived beside
this file; reproduce as documented in `results.md` (slice 01).

## A reported survivor that is demonstrably killed — do not trust the 80.95% as a floor

Four of the sixteen `DeliveryMetricsHistory.ts` survivors are `ConditionalExpression → false` on the
null-guards at `:60`, `:63`, `:111`, `:114`. Those are load-bearing branches, so the report was checked
rather than believed. Applying the `:60` mutant by hand — `asNullableBoolean`'s
`if (value === null || value === undefined)` forced to `if (false)` — and running the suite:

```
× still reads an epic recorded before sizes were ever written
× still reads an epic that could not be forecast
× treats a missing estimate flag as unknown rather than as a guess
Tests  3 failed | 24 passed (27)
```

The mutant is killed three times over by tests that were in the run's `include` list, with
`coverageAnalysis: "off"` (so every mutant runs the whole included suite). StrykerJS reported it
Survived anyway. Cause not established; the practical consequence is that **the true kill rate is higher
than 80.95%**, and that a StrykerJS "Survived" verdict on this project deserves a hand-check before
anyone writes a test to chase it.

Added to the standing Stryker-config traps for this repo, alongside the .NET line-span and
`disableTypeChecks` ones.

## Survivors left alive on purpose

- **`DeliveryEpicSizeChart.tsx` (18)** — the same `sx` styling family slice 01 documented (Card and
  CardContent padding, border radius, margins) plus the new left-margin widening. Killing them means
  asserting emotion class names; they encode no behaviour.
- **`DeliveryMetricsHistory.ts` (12 of 16)** — `StringLiteral → ""` on the boundary-error *context*
  strings for the newly-parsed fields (`"featureBreakdown.totalItems"`,
  `"featureBreakdown.isUsingDefaultSize"`, and the existing siblings). The file's own convention tests
  the message text for a handful of fields only, and blanket-asserting every context string would be
  pure enumeration. Worth revisiting only if a wrong-field error message ever misleads someone.

## Note for slice 03

`DeliveryEpicSizeChart.tsx` gains the hatch renderer (`slots.bar`, the `<pattern>` def, the
`::actual`/`::estimated` series split) in slice 03 — the biggest single jump in that file's surface so
far. Re-run this config then; 75.34% on that file is not a settled number.
