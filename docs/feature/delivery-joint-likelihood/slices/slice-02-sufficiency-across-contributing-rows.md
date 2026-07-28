# Slice 02 — The "not enough data" warning covers every contributing feature

**Story**: US-02 · **ADO**: #5587 · **Job**: `job-forecast-only-with-enough-data` · **Effort**: ≤ 1 day
**Blocked by**: slice 01 (same file, same grain; do 01 first so the AND lands on a delivery number that
is already row-grained). **Release-bound to**: slices 01, 03, 04 (D9).

## Goal

Retire the last "one representative feature stands for the delivery" signal. `HasSufficientData`
becomes an AND across the delivery's contributing features, with the no-remaining-work exemption.

## The change (D6)

`DeliveryWithLikelihoodDto.FromDelivery` currently:

```csharp
HasSufficientData = leastLikelyFeature?.HasSufficientData ?? featureLikelihoods.All(fl => fl.HasSufficientData),
```

After: the AND over the features **that have remaining work**; empty set ⇒ `true`.
`GetLeastLikelyFeature` is deleted — `ctx_search` finds exactly two sites, its own definition and this
one call.

## Why AND

1. After slice 01 the delivery number rests on every `(team, feature)` row; a sufficiency flag read off
   one representative is the same defect the feature exists to remove.
2. ADR-039's rule already says AND, and `AggregatedWhenForecast.HasSufficientData =
   materialized.All(…)` ANDs across a feature's team rows today. Least-likely-feature is the odd one
   out, not the precedent.
3. `FeatureLikelihoodDto.HasSufficientData` is already the All-across-teams aggregate, so
   All-across-features ≡ All across every `(team, feature)` row — the exact row set the likelihood uses.
4. AND can only flip `true → false`. It never newly hides a warning.

## The landmine this slice exists to avoid

A feature with no remaining work carries the whole-feature `{0: 0}` sentinel, whose `Team` is null, so
`CreateWhenForecastForSimulationResult` never copies the sufficiency flag and the `bool` stays at its
`false` default. `AggregatedWhenForecast` then ANDs it to `false`.

**A plain `All(…)` therefore makes every delivery containing a completed feature report "not enough
data".** Today `GetLeastLikelyFeature` masks this by accident — a finished feature sorts to likelihood
100 and is never selected unless it is the only one. The exemption keys off remaining work, which is
the same rule as ADR-112's completed-feature exemption and the same rule as slice 01's trap 4.

## IN scope

- `DeliveryWithLikelihoodDto.HasSufficientData` — AND with the remaining-work exemption.
- Delete `GetLeastLikelyFeature`.
- Reuse the existing `INSUFFICIENT_FORECAST_DATA_SHORT` rendering on `DeliverySection` — no new
  indicator, no new colour.

## OUT of scope

- The sufficiency **threshold** itself (`forecast-minimum-data-guard`) — untouched.
- ADR-112's unknown state — composes with this, does not replace it (ADR-112 D4).
- Per-feature sufficiency — the row already ANDs across its teams and is correct.

## Visible behaviour delta — must reach the release notes

A delivery whose least-likely feature has sufficient data but where **another** feature rests on thin
history reports `hasSufficientData: true` today and `false` after. The indicator appears where it never
did. That is the right direction — the joint number genuinely rests on that thin history now — but
users will see it, so slice 04 carries it as its own bullet (AC-02.4 / AC-04.2).

## Learning hypothesis

**Confirms** that sufficiency is a pure rollup rule and the exemption is the only subtlety.

**Disproves** it if the remaining-work signal cannot be reached from the DTO assembly without a new
field — in which case the exemption is not a one-line predicate and DESIGN must decide whether
`FeatureLikelihoodDto` gains a row-level signal or the AND is evaluated against `delivery.Features`.
Either is acceptable; the point of the hypothesis is that it is settled before code, not during.

## Acceptance criteria

Full text in `feature-delta.md` US-02. Summary: AC-02.1 AND over features with remaining work, empty ⇒
true · 02.2 completed-feature exemption with the regression fixture · 02.3 `GetLeastLikelyFeature`
deleted · 02.4 the visible delta is intentional and documented · 02.5 composes with the unknown state ·
02.6 existing indicator reused.

## Gates before commit

1. `dotnet build` zero warnings, `dotnet test` green; `pnpm test` / `pnpm build` / Biome clean.
2. Mutation ≥ 80 % on the changed surface. The Epic 5459 survivor list showed the delivery sufficiency
   fallback could not tell `All` from `Any` because every fixture had a single feature — this slice's
   fixtures need **two** features with differing sufficiency, plus one completed feature.
3. `docs/ci-learnings.md` pre-applied.
