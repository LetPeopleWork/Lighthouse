# ADR-012: Rule-Engine Generalisation — Hybrid (Shared Value-Objects + Extracted Generic Evaluator + Preserved Public Surfaces)

**Status**: Accepted (2026-05-20 — Option C locked in by Morgan after weighing trade-offs; user has not requested a different option)
**Date**: 2026-05-20
**Feature**: filter-forecast-throughput (Epic 4896)
**Decider**: Morgan (Solution Architect), interaction mode PROPOSE

---

## Context

DISCUSS-wave decision D7 locked the architectural direction: reuse and generalise the existing `DeliveryRuleSet` rule-engine for the forecast-throughput filter rather than building dedicated controls (work-item-type checkboxes, orphan bool). The question this ADR settles is the **shape** of that generalisation.

Today (pre-feature) the rule-engine is Feature-scoped:

- Value-objects (`DeliveryRuleSet`, `DeliveryRuleCondition`, `DeliveryRuleFieldDefinition`, `DeliveryRuleSchema`) live in `Lighthouse.Backend/Lighthouse.Backend/Models/DeliveryRules/`.
- A single concrete service `DeliveryRuleService : IDeliveryRuleService` evaluates a `DeliveryRuleSet` against an `IEnumerable<Feature>` using private static methods (`FeatureMatchesAllConditions`, `EvaluateCondition`, `EvaluateTagsCondition`, `GetFieldValue`, `RuleSetHasError`).
- Field keys are hard-coded `feature.type`, `feature.state`, `feature.name`, `feature.referenceid`, `feature.parentreferenceid`, `feature.tags` plus `additionalField.{id}`.
- Schema is built per `Portfolio` (the portfolio's connection contributes the additional-field definitions).
- The persisted JSON shape (`Delivery.RuleDefinitionJson`) is the format the new feature must reuse verbatim (D7 + cross-cutting invariant #6).

The new forecast-throughput filter needs the same algorithmic core but applied to `WorkItem`, with a Team-scoped schema (D9). Future rule-based surfaces (cycle-time exclusion, predictability scope, …) are likely.

CLAUDE.md fixes the paradigm: OOP, ports-and-adapters, immutability default, TDD non-negotiable, max 2 levels of nesting, options object when >3 params. The refactor must respect these.

---

## Decision

**Option C — Hybrid generalisation**:

1. **Value-objects reused verbatim** (no change to `DeliveryRuleSet`, `DeliveryRuleCondition`, `DeliveryRuleFieldDefinition`, `DeliveryRuleSchema`). The persisted JSON shape is byte-identical between delivery rules and forecast-throughput filters. The canary test (DDD-7) makes this a CI gate.

2. **Two new ports extracted**:
   - `IRuleEvaluator<T>` with two methods: `IEnumerable<T> Match(DeliveryRuleSet ruleSet, IEnumerable<T> items, IRuleFieldProvider<T> fieldProvider)` and `bool IsValid(DeliveryRuleSet ruleSet, DeliveryRuleSchema schema)`.
   - `IRuleFieldProvider<T>` with three methods: `string GetFieldValue(T item, string fieldKey)`, `IReadOnlyList<string> GetTagsForField(T item, string fieldKey)`, `IReadOnlyList<DeliveryRuleFieldDefinition> GetFixedFields()`.

3. **One adapter per typed evaluation**: `FeatureFieldProvider` (existing field-key constants extracted from `DeliveryRuleService`) and `WorkItemFieldProvider` (new, D9 WorkItem field keys).

4. **Existing `DeliveryRuleService` keeps its public interface unchanged**. Internals now delegate:
   - `GetRuleSchema(Portfolio portfolio)` → composes `FeatureFieldProvider.GetFixedFields()` + portfolio-derived additional fields.
   - `GetMatchingFeaturesForRuleset(DeliveryRuleSet, IEnumerable<Feature>)` → calls `_ruleEvaluator.Match(ruleSet, features, _featureFieldProvider)` (after `IsValid` check that preserves today's "invalid → empty result" behaviour).
   - `RecomputeRuleBasedDeliveries(Portfolio, IEnumerable<Delivery>)` → same loop, delegates to the new `Match` for each delivery's rule set.

5. **New `IForecastFilterRuleService` is the WorkItem-scoped delegator** with four methods:
   - `DeliveryRuleSchema GetSchema(Team team)` — composes `WorkItemFieldProvider.GetFixedFields()` + team-connection additional fields.
   - `DeliveryRuleSet? GetEffectiveRuleSet(Team team)` — returns `null` on free tenant / null JSON / zero conditions (DDD-8 + DDD-9 normalisation).
   - `IEnumerable<WorkItem> Filter(IEnumerable<WorkItem> items, DeliveryRuleSet ruleSet)` — calls `_ruleEvaluator.Match(...)` then **excludes** matched items (D8 semantics applied here; the evaluator stays semantics-agnostic per ADR-013).
   - `bool ValidateRuleSet(DeliveryRuleSet ruleSet, Team team)` — calls `_ruleEvaluator.IsValid(ruleSet, GetSchema(team))`.

6. **Refactor commit is separate from feature commit** (CLAUDE.md TDD discipline). The refactor commit:
   - introduces the new ports and adapters;
   - rewires `DeliveryRuleService` to delegate;
   - is provable-green by the existing rule-based-deliveries test suite running unchanged AND by the new canary test (DDD-7) running for the first time against an empty corpus.
   The feature commit adds `ForecastFilterRuleService`, the persistence column, the schema endpoint, the controller validation, and the filter integration into `TeamMetricsService`.

---

## Alternatives Considered

### Option A — Pure generic extraction (`IRuleEvaluator<T>` + new public surface on `DeliveryRuleService`)

Make `DeliveryRuleService` itself generic on a type parameter, or replace its public methods with new generic ones, and have `IDeliveryRuleService` carry typed methods like `GetMatchingItemsForRuleset<T>(...)`.

**Rejected.** This mutates the proven public surface of `IDeliveryRuleService` for callers that don't need genericity. The rule-based-deliveries code path is the only consumer and it's working fine; touching its public interface invites regression and provides zero benefit on the delivery-rules side. The cost (refactor blast radius across callers) is real; the benefit (uniform genericity at the public surface) is cosmetic.

### Option B — Parallel `WorkItemFilterRuleService` duplicating the evaluator logic

Reuse only the value-objects (`DeliveryRuleSet`, `DeliveryRuleCondition`, …). Copy the evaluator's private static methods (`EvaluateCondition`, `EvaluateTagsCondition`, `GetFieldValue`, `RuleSetHasError`) into a new `WorkItemFilterRuleService` and adapt them to operate on `WorkItem`.

**Rejected.** Two reasons:

1. **Duplication of knowledge** — operator semantics (`equals`, `notEquals`, `contains`, case-insensitive comparison, tag handling), validation rules (max 20 conditions, max 500-char value, valid field-key format), and additional-field key parsing become two copies that must evolve in lockstep. The next time someone adds an operator or fixes a comparison bug they must remember to do it twice. The cross-cutting invariant #6 canary protects the JSON shape but does NOT protect operator parity.
2. **The next rule-based surface** (cycle-time exclusion, predictability scope, …) would add a third copy, then a fourth. The architectural debt compounds linearly with each new surface.

The "faster" claim of Option B does not survive the second consumer; even on the first consumer the time saved (≈half a day) is less than the time it takes to write a single review comment explaining the duplication on the PR.

### Option C — Hybrid (chosen)

Share the value-objects AND the evaluator algorithm; keep two thin service classes that compose them with their own field provider and additional-field schema source. Public surface of `IDeliveryRuleService` is preserved.

**Selected.** Captures Option A's "single evaluator code path" benefit without Option A's "mutate the proven public surface" cost. Captures Option B's "two scoped services, each with its own DTO and field set" symmetry without Option B's duplication tax.

---

## Consequences

### Positive

- **One algorithmic code path** — operator semantics, value-length cap, rule-count cap, field-key format validation live in `RuleEvaluator<T>` (a pure function) and `DeliveryRuleSet` (the value-object constants). Mutation tests target one file; bug fixes land once.
- **Public surface of `IDeliveryRuleService` is preserved** — zero risk to the rule-based-deliveries code path. The refactor commit is provable-green by re-running the existing test suite untouched.
- **Symmetric extension point for future rule-based surfaces** — adding a new `IRuleFieldProvider<TNewEntity>` + a new thin service class is ~half a day of work and zero changes to the evaluator.
- **The canary test (DDD-7) is a real CI gate** — the persisted JSON shape between delivery rules and forecast-throughput rules cannot drift without breaking the canary.
- **Premium gate is centralised** (`ForecastFilterRuleService.GetEffectiveRuleSet` is the single point — DDD-9) — preserves the "license downgrade is non-destructive" invariant (US-07 / invariant #7) cleanly.

### Negative

- **Slight indirection on the delivery-rules side** — `DeliveryRuleService.GetMatchingFeaturesForRuleset` now does one extra `Match` call into `RuleEvaluator<Feature>` instead of inlining the loop. Performance impact is negligible (the evaluator is a pure function over typically ≤20 conditions and ≤a few thousand items); cognitive impact is one extra hop when tracing code. Mitigated by the test suite continuing to assert behaviour, not implementation.
- **Two field providers** (`FeatureFieldProvider`, `WorkItemFieldProvider`) live near the evaluator — slightly more classes than today. Justified by the symmetry payoff.
- **`RuleEvaluator<T>` is a pure function with two methods** — small surface area to test, but the constructor-purity contract (no I/O dependencies) must be enforced by ArchUnitNET (otherwise someone could inject a logger or repository and the purity invariant rots).

### Sensitivity / trade-off points

- **Trade-off**: the refactor commit increases Slice 01's size beyond the ≤1-day carpaccio taste test (~1.5 days). This is explicit in the slice brief and was accepted at DISCUSS-time as the architectural payback price. If Slice 01 grows further than 1.5 days, the fallback is to defer the schema endpoint and the FE wiring to Slice 1B — never to defer the refactor commit itself, because every additional consumer of the rule engine multiplies the cost of deferring.
- **Sensitivity**: if a future consumer needs a different operator (e.g. `greaterThan` on a numeric custom field), `RuleEvaluator<T>` must learn the new operator AND every existing field provider must declare which fields support it. The current design treats operators as a flat list on the schema; the consequence is a small refactor of `DeliveryRuleSchema` if/when that need arises. Out of scope for this feature.

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| `IRuleEvaluator<T>` implementations are pure (no I/O constructor dependencies) | NUnit constructor-inspection test on `RuleEvaluator<T>` — fails the build if a forbidden dependency (`IRepository<>`, `DbContext`, `HttpClient`, `ILogger`, `IClock`) is injected. |
| `DeliveryRuleService` public API surface unchanged through the refactor | NUnit reflection test asserts `GetRuleSchema(Portfolio)`, `GetMatchingFeaturesForRuleset(DeliveryRuleSet, IEnumerable<Feature>)`, `RecomputeRuleBasedDeliveries(Portfolio, IEnumerable<Delivery>)` still exist with their original signatures. |
| `DeliveryRuleSet` JSON shape is reused verbatim across both consumers | `RuleEngineReuseCanaryTests` parameterised over representative rule sets (see DDD-7). |

These three enforcement points are the architectural net for ADR-012. Removing any one weakens the invariant the ADR exists to protect.

> **Addendum (2026-05-23):** Value-object types renamed `DeliveryRule*` → `WorkItemRule*` (and their `Models.DeliveryRules` / `Services.{Interfaces,Implementation}.DeliveryRules` namespaces to `…WorkItemRules`) to reflect that they describe rules matching work items regardless of consumer. Same JSON wire format (property-name serialization, no type-name discriminator); same enforcement tests; `IDeliveryRuleService` / `DeliveryRuleService` / `DeliveryRulesController` / `DeliveryRulesDto` kept (use-case-specific to the delivery-rules feature). See the rename commit.
