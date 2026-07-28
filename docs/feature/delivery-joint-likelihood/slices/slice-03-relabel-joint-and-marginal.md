# Slice 03 — Both surfaces say which probability they are showing

**Story**: US-03 · **ADO**: #5587 · **Job**: `job-delivery-likelihood-covers-every-feature` · **Effort**: ≤ 1 day
**Blocked by**: slice 01 (the copy must describe the shipped number; relabelling the old number would
be actively false). **Release-bound to**: slices 01, 02, 04 (D9).

## Goal

Close the row-vs-header mismatch at a glance. The header states it covers **all** features; the
breakdown column states it ignores the others.

## The copy (D1, locked by the maintainer)

| Surface | Label | Tooltip |
|---|---|---|
| Delivery header chip (`DeliverySection.tsx`) | `All {featuresTerm} by {delivery date}: NN%` | `P(ALL of these land by the date)` |
| Breakdown grid Likelihood column | "each on its own" framing | `P(this one lands), ignoring the others` |

**Rejected alternatives** (do not revisit): showing both joint and marginal numbers on the badge — two
competing numbers; tooltip-only with no label change — leaves the mismatch unexplained at a glance.

## Two hard constraints

- **Constraint A — terminology.** All new copy routes through `useTerminology()` /
  `getTerm(TERMINOLOGY_KEYS.DELIVERY | FEATURE | FEATURES)`. "Delivery" and "Feature" are user-renamable
  (`src/models/TerminologyKeys.ts:8,9,25`). Never hardcode. `DeliverySection` already destructures
  `featureTerm` / `featuresTerm` / `deliveryTerm` — extend, do not add literals. Verify with the
  renamed-vocabulary scenario, not by inspection.
- **Constraint B — no false promise.** The copy must **not** claim the header is always lower than
  every row. Equality is legitimate: in the three-way fixture the delivery is 0.720 and F1's row is
  0.72. Team *t*'s min ≤ any row in bucket(*t*) and every other team's term ≤ 1, so `delivery ≤ every
  row` — with equality when one feature governs entirely and the other teams carry slack.

## IN scope

- `DeliverySection.tsx` header chip label + info affordance and tooltip, numeric state only.
- The Likelihood column definition in the same file (header text + header tooltip).
- The date inside the header label uses `delivery.getFormattedDate()` — the same formatter as the
  "Delivery Date:" text beside it, so the two can never disagree.

## OUT of scope

- The non-numeric header states. `CANNOT_FORECAST_SHORT` keeps its label and its
  `cannotForecastReason(teamsWithoutForecast)` tooltip; `INSUFFICIENT_FORECAST_DATA_SHORT` keeps its
  label. The "All … by …" framing applies to the numeric state only.
- `FeatureLikelihoodChip`'s own conditional tooltip — it must keep working alongside the new column
  tooltip, not be replaced by it.
- Chip position, size, `ForecastLevel` colour scale — unchanged, so the arc has no jarring transition.
- Any other surface (Team page feature grids, portfolio feature columns) — those are feature-level and
  already say what they mean post-5459.

## Learning hypothesis

**Confirms** that the header-vs-row mismatch is explainable by labels alone.

**Disproves** it if the label cannot fit the chip without truncation at common viewport widths once a
long renamed term ("Programme Increment Epics") is substituted — in which case this is a layout
problem, not a copy problem, and DESIGN owns a chip/label restructure rather than a string change.
Test with a deliberately long terminology override before writing the final copy.

## Acceptance criteria

Full text in `feature-delta.md` US-03. Summary: AC-03.1 header label + tooltip · 03.2 column framing +
tooltip · 03.3 no new hardcoded domain literal, proven by the renamed-vocabulary scenario · 03.4 no
"always lower" claim · 03.5 non-numeric states unaffected · 03.6 the row chip's tooltip is not
clobbered · 03.7 position/size/colour unchanged · 03.8 one date formatter.

## Gates before commit

1. `pnpm test` green; `pnpm build` zero errors and zero warnings; Biome clean on `./src`.
   Remove any stray `src/docs` symlink first — Biome `--write` will otherwise reformat the whole docs
   tree.
2. Mutation ≥ 80 % frontend on the changed surface. Epic 5459's lesson applies directly: the mutants
   that **deleted the whole branch** and **renamed the label** both survived on the Unknown level
   because nothing tested it. Assert the label text, not just that a chip renders.
3. Playwright run locally before commit; if a `@screenshot` test covers the delivery header, `rm` the
   old PNG first — a diff under 0.5 % keeps the stale image.
