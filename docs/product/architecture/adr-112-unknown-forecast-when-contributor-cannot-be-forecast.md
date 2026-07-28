# ADR-112: When a contributing team cannot be forecast, the feature forecast is UNKNOWN — and must never fall through to 100 %

- **Status**: **Accepted** (2026-07-28, Story 5570 DISTILL) — carrier shape settled as **(a) nullable**
  on the evidence below; **pending maintainer ratification** of that call and of the delivery-rollup
  decision (D8), both taken while the maintainer was unavailable.
- **Date**: 2026-07-27
- **Feature**: epic-5459-multi-team-forecasts (ADO Epic 5459, Story 5570)
- **Deciders**: Benjamin Huser-Berta (maintainer)

## Context

`ForecastService` only simulates teams present in `throughputByTeam` (`:112`), but
`UpdateFeatureForecasts` (`:130-146`) still constructs a `WhenForecast` for every contributing team —
including one with no usable throughput. That forecast carries an empty histogram and
`TotalTrials == 0`.

Today this is harmless by accident: `MaxBy(GetProbability(85))` discards it, because `GetProbability`
returns `-1` on an empty histogram. [ADR-110](./adr-110-multi-team-forecast-joint-probability.md)
deletes that selection and filters such contributors out of the product — deliberately preserving
today's behaviour so Story 5569 is a pure maths change.

DISCUSS D3 decided the end state: if a team that *must* finish cannot be forecast, the feature has no
honest completion distribution, so it reports **unknown** rather than a partial forecast built from
the teams that could be forecast.

**The trap.** `ForecastBase.GetLikelihood` (`:70-95`) ends:

```csharp
if (trialCounter > 0) { return 100 / ((double)TotalTrials) * trialCounter; }
return 100;
```

An empty aggregate therefore reports **100 % likelihood of hitting the target date** — the exact
inverse of "unknown", on the surface leadership anchors on. Implementing D3 as "return an empty
forecast" and nothing else would ship the worst possible rendering of it.

## Decision

1. **Detection**: the feature forecast is unknown when **any** contributing team's `WhenForecast` has
   `TotalTrials == 0`. This replaces the ADR-110 filter rather than layering on top of it.
2. **The unknown state is carried explicitly** on the aggregate — a state the callers read — not
   inferred by callers from an empty histogram. Inference-from-silence is precisely what produces the
   100 % fall-through.
3. **The likelihood path is suppressed explicitly.** `Feature.GetLikelhoodForDate` and
   `DeliveryWithLikelihoodDto.CalculateFeatureLikelihoods` (`:151-161`) must not reach
   `GetLikelihood` for an unknown forecast. Reaching the `return 100` branch is a test failure, not a
   tolerated edge case (US-02 AC-02.2).
4. **Composes with, does not replace, `HasSufficientData`.** The existing AND-across-teams sufficiency
   signal ([ADR-039](./adr-039-forecast-data-sufficiency-backend-signal.md)) continues to report
   `false` in this case. Unknown-forecast and insufficient-data are related but distinct: the former
   says "no distribution exists", the latter says "the distribution rests on thin history".
5. **A completed feature is exempt.** No remaining work is a fact, not a forecast — it still reads
   100 % / Done, mirroring the `forecast-minimum-data-guard` D4 exemption.
6. **The message names the teams** that could not be forecast (US-02 AC-02.6), so the user knows which
   data gap to close.

### Open: the DTO carrier

Two candidate shapes, to be settled at the start of Story 5570:

- **(a) Nullable likelihood** — `FeatureLikelihoodDto.LikelihoodPercentage` becomes nullable; `null`
  means unknown. Cleanest domain modelling. An older client deserialising `null` into a
  non-nullable `double` gets `0`, which reads as "0 % likely" — wrong, but conservative rather than
  falsely confident.
- **(b) Companion flag** — keep the numeric field, add an explicit `CanBeForecast` boolean, and set
  the number to a defined value when unknown. Additive and safest for existing clients, at the cost of
  a field that must never be read without checking its companion — the same trap class this ADR exists
  to close, one level up.

Recommendation leans (a) for honest modelling with (b)'s client-compatibility risk quantified first;
DESIGN deliberately does not pre-empt the Story 5570 client check.

### Resolved (2026-07-28): (a) nullable — the client check found no risk to quantify

The compatibility worry that made (b) tempting does not apply to the actual clients:

- **CLI/MCP do not deserialise the payload.** `listDeliveries` is typed
  `Promise<LighthouseApiResult<readonly unknown[]>>` (`packages/client/src/index.ts:1311`) and
  `lighthouse_delivery_list` forwards it verbatim through `encodePayload`
  (`packages/mcp-core/src/index.ts:2213`). There is no non-nullable `double` to break and no
  human-rendered percentage to mis-render — the "older client reads null as 0 %" scenario has no
  instance in this codebase.
- **The frontend already carries a nullable likelihood in the same domain.**
  `DeliveryMetricsHistory.ts:21` declares `likelihoodPercentage: number | null`, parses it with
  `asNullableNumber`, and `DeliveryPredictabilityChart.tsx:85` already gaps the line on `null`. (a)
  follows an established precedent rather than introducing a new idiom.
- **(b) reproduces the defect this ADR exists to close**, one level up: a numeric field that is only
  meaningful once a companion boolean has been consulted is the same "infer from silence" trap as
  `return 100`, and every future consumer would have to re-learn it.

### D8 (new, 2026-07-28): the delivery-level likelihood is unknown when any of its features is

Not covered by the original ADR and surfaced while implementing: `DeliveryWithLikelihoodDto` carries
its **own** `LikelihoodPercentage`, derived by `GetLeastLikelyFeature` (`:109-124`), which filters
`fl.LikelihoodPercentage >= 0` and takes the minimum. An unknown feature would silently drop out of
that filter and the delivery would report the least likely of the features that *could* be forecast —
the worst-team defect of ADR-110, one level up, and the second "infer from silence" path in the
codebase (which is what US-02's learning hypothesis predicted would decide whether this is a local
change).

Decision: a delivery containing a feature that cannot be forecast **cannot itself be honestly
forecast**, so its `LikelihoodPercentage` is unknown too, and it names the same teams. The alternative
— show the minimum over the forecastable features — is rejected for the same reason the partial
feature forecast was rejected: it presents a number that quietly ignores work that must still happen.

## Alternatives considered

- **Exclude the un-forecastable team and show a partial forecast** (keep ADR-110's filter as the end
  state). Rejected at DISCUSS by the maintainer: a forecast that silently ignores a team that must
  finish is exactly the class of dishonesty this epic exists to remove — it is the same defect as the
  worst-team copy, one level down.
- **Reuse `HasSufficientData` as the unknown signal.** Rejected — it already means something else
  (thin history, forecast still shown), and overloading it would make "show a number with a warning"
  and "show no number" indistinguishable to every consumer.
- **Fix `GetLikelihood`'s `return 100` in place** to return 0 or throw. Tempting, and the branch is
  indefensible, but it is reachable from single-team paths too; changing it as a side effect of this
  epic would alter behaviour outside the epic's scope without its own tests. Left alone deliberately —
  the aggregate must not *reach* it. Worth a separate ticket.

## Consequences

- **Positive**: a feature that cannot be honestly forecast says so, on every surface, instead of
  presenting maximum confidence.
- **The frontend needs an unknown rendering** — `ForecastInfoList.tsx:25` already tolerates an empty
  `forecasts` array, so the date columns degrade safely; the likelihood cell does not and must be
  handled (US-02 AC-02.5, verify rather than assume).
- **Clients (CLI/MCP)** that render a likelihood to a human must honour the unknown state. Whether
  they do — versus emitting raw JSON — is a Story 5570 check, not a blocker for Story 5569.
- **Reuse verdict**: `AggregatedWhenForecast` → **EXTEND** (the ADR-110 filter becomes the detection
  rule); `Feature.GetLikelhoodForDate` → **EXTEND** (guard before the existing call). No new type.
- Cross-refs [ADR-110](./adr-110-multi-team-forecast-joint-probability.md) (the filter this replaces),
  [ADR-111](./adr-111-aggregate-forecast-field-provenance.md) (what the provenance fields read as when
  unknown), [ADR-039](./adr-039-forecast-data-sufficiency-backend-signal.md) (the sufficiency signal
  this composes with).
