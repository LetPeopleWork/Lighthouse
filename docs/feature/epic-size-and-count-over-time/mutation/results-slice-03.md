# Mutation testing — Epic 5585 slice 03

**Date**: 2026-08-02 · **Stack**: frontend (StrykerJS) · **Story**: ADO #5616 · **File**: `DeliveryEpicSizeChart.tsx`

## Result

| run | mutants | killed | survived | score |
|---|---|---|---|---|
| first | 108 | 86 | 20 | 79.63% |
| **after two added assertions** | 108 | **90** | 16 | **83.33%** (84.91% of covered) |

Over the project's 80% floor.

## What the first run found — two real gaps, six mutants

Neither was cosmetic, and both were assertions DISTILL should have written.

**1. The items axis was never asserted absent.** `hasSizes = epics.length > 0` could be forced to `true`,
and the `hasSizes && <ChartsYAxis axisId="items" />` guard inverted, without a single scenario noticing —
five mutants across `:201` and `:306`. Scenario 13 checked that a legacy-only history draws no *bars* but
said nothing about the *axis*, even though "declare the items axis only when a bar series exists" is
DISTILL's own resolved open question from slice 01. Now asserted on both the container props and the
rendered axis element.

**2. "Solid" was only asserted as "not hatched".** `fill={hatch ? url(...) : (props.fill ?? props.color)}`
could become `props.fill && props.color` — yielding `undefined`, an invisible bar — and still pass
`expect(fill).not.toMatch(/^url\(#/)`. The scenario now calls the slot with an explicit
`color: "#123456"` and asserts the fill *equals* it. A negative assertion about an attribute is worth
little when dropping the attribute satisfies it.

## The 16 left alive

- **`sx` / style object literals** (`:117`, `:166`, `:169`, `:190`, `:299`) — 10 mutants. Card padding,
  border radius, chart margins, the pattern's stroke geometry. Killing them means asserting emotion class
  names or SVG geometry attributes; they encode no behaviour. Same family documented for slices 01-02.
- **`:38`, `:185` string literals** — the data-key prefix and the `useId()` sanitiser's replacement
  string. Both are internal identifiers; a test pinning them would assert the implementation's private
  naming.
- **`:69` `if (metric.totalItems !== null)` → `true`** — equivalent in effect. `collectSizedEpics` applies
  the same guard when deciding which series exist, so a null slipping into the per-day size map still
  reaches the dataset as `null` via `sizes.get(id) ?? null`.
- **`:205` optional chaining on `estimatedByDay[dataIndex]?.has`** — removing `?.` only differs for a
  `dataIndex` outside the recorded range, which MUI never produces.
- **`:274` `ArrayDeclaration`** — the hatch defs array; covered indirectly by the AC-3.6 pattern-count
  scenario, which asserts two distinct ids rather than the array's construction.

## Standing caveat

Per `results-slice-02.md`, StrykerJS on this repo reports **false survivors** — four null-guard mutants
in `DeliveryMetricsHistory.ts` were reported Survived while a hand-applied mutation killed three
scenarios. Treat any score here as a lower bound, and hand-check a survivor on a load-bearing branch
before writing a test to chase it. The two gaps fixed above were both hand-verified before assertions
were added.
