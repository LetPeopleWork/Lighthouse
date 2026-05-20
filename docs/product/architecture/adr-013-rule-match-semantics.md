# ADR-013: Rule-Match Semantics — `RuleSetSemantics` Enum Decided at the Caller, Not Embedded in `DeliveryRuleSet`

**Status**: Accepted (2026-05-20)
**Date**: 2026-05-20
**Feature**: filter-forecast-throughput (Epic 4896)
**Decider**: Morgan (Solution Architect), interaction mode PROPOSE

---

## Context

DISCUSS-wave decision D8 fixed a semantic divergence between two consumers of the same rule-engine:

- **Delivery rules**: a rule that MATCHES a `Feature` means the feature is INCLUDED in the delivery.
- **Forecast-throughput filter**: a rule that MATCHES a `WorkItem` means the work item is EXCLUDED from the throughput sample.

The rule-engine generalisation (ADR-012) puts both consumers behind the same `RuleEvaluator<T>` algorithm. The question this ADR settles is: **where does the include-vs-exclude semantics live**?

Two structural options exist:

- **(a) Embed the semantics in the persisted `DeliveryRuleSet` JSON** — add a new field, e.g. `"semantics": "Include" | "Exclude"`, defaulted to `"Include"` for backward-compat with existing delivery-rule blobs.
- **(b) Decide the semantics at the caller — pass a `RuleSetSemantics` value alongside the rule set at the application layer**. The persisted JSON shape stays identical to today (D7 + cross-cutting invariant #6).

Cross-cutting invariant #6 ("the JSON shape persisted for `forecastFilterRuleSet` is `DeliveryRuleSet`-compatible. A canary test deserialises a stored rule set with the existing `DeliveryRuleSet` JSON deserialiser to prove shape compatibility") is the load-bearing constraint here.

---

## Decision

**Option (b): semantics is a property of the CALLER, not of the stored rule set.**

- The persisted `DeliveryRuleSet` JSON shape is unchanged. No new field. The canary test (DDD-7) succeeds against the existing delivery-rules deserialiser without modification.
- A new `RuleSetSemantics` enum lives in `Lighthouse.Backend/Lighthouse.Backend/Models/DeliveryRules/RuleSetSemantics.cs`:

  ```
  enum RuleSetSemantics
  {
      Include,
      Exclude,
  }
  ```

  (Pure declarative type — no logic on the enum itself.)
- `IRuleEvaluator<T>.Match(ruleSet, items, fieldProvider)` returns the items that match the rule set, **regardless of semantics**. The evaluator is semantics-agnostic.
- The CALLER decides what to do with the matched items:
  - `DeliveryRuleService.GetMatchingFeaturesForRuleset(...)` returns the matched features (semantics `Include` — caller behaviour preserved verbatim).
  - `ForecastFilterRuleService.Filter(items, ruleSet)` calls `_ruleEvaluator.Match(...)` and then returns `items.Except(matched, identityComparer)` (semantics `Exclude` — implemented at this single call site).
- The `RuleSetSemantics` enum is exposed in the C# domain layer but is **not** wire-visible. The frontend never sees the value; the UI labelling ("Exclude items where…" vs "Include items where…") is hard-coded per page (delivery-rules editor stays as-is; the new `ForecastFilterEditor` passes `title="Exclude items where…"` to the reused `DeliveryRuleBuilder` per DDD-6).

---

## Alternatives Considered

### Option A — Embed `"semantics"` in the persisted `DeliveryRuleSet` JSON

Add a new field to `DeliveryRuleSet`:

```csharp
public class DeliveryRuleSet
{
    public int Version { get; set; } = SchemaVersion;
    public RuleSetSemantics Semantics { get; set; } = RuleSetSemantics.Include;
    public List<DeliveryRuleCondition> Conditions { get; set; } = [];
}
```

**Rejected for two reasons:**

1. **Breaks the canary contract (cross-cutting invariant #6).** The whole point of the canary test is to prove that the stored JSON blob on the new `Team.ForecastFilterRuleSetJson` column is deserialisable by the EXISTING delivery-rules deserialiser without modification. Adding a `Semantics` field with a default makes the canary technically green (the default kicks in on read) but introduces a silent failure mode: if a future developer changes the default, every existing delivery-rules blob silently flips semantics. The invariant becomes meaningless.
2. **The persisted blob shouldn't carry "purpose" metadata.** A rule set is "a list of conditions"; what we DO with them is a property of the system that consults them. Embedding semantics in storage couples the data shape to the use case, which is the exact pattern that the rule-engine generalisation (ADR-012) is trying to escape.

### Option B — Two separate types (`DeliveryInclusionRuleSet` and `ThroughputExclusionRuleSet`) sharing a base

Make `DeliveryRuleSet` an abstract base; introduce two concrete subtypes (with no extra state, just type identity) that encode the semantics. The evaluator would be typed on the subtype.

**Rejected.** This makes the JSON shape divergent (or requires `$type` polymorphism in the serialiser, which is a known C# JSON foot-gun and complicates the canary). The genericity payoff disappears (the evaluator would need to be polymorphic across rule-set types). All for what is structurally a one-bit decision.

### Option C — A `bool ExcludeMatched` parameter on `IRuleEvaluator<T>.Match`

Make `Match(ruleSet, items, fieldProvider, bool excludeMatched)` return either matched or non-matched items based on the bool.

**Rejected (mildly).** This puts the semantics decision inside the evaluator (one branch on a bool). It's almost what we chose — but the chosen option is cleaner because the evaluator stays a single-purpose function (return matched items) and the include/exclude decision is named (an enum, not a bool) at the call site where it has business meaning. The reader of `ForecastFilterRuleService.Filter` sees `items.Except(matched)` — a clear English-readable expression of D8 — instead of `Match(..., excludeMatched: true)` which obscures the intent. Cost is one extra LOC at the call site; benefit is named semantics and a purer evaluator.

---

## Consequences

### Positive

- **D7 invariant is exact, not approximate.** The persisted JSON shape between delivery rules and forecast-throughput rules is byte-identical; the canary test enforces this without exception.
- **`RuleEvaluator<T>` is single-purpose** — "return matched items, given a rule set, a collection, and a field provider." No semantic branching inside it.
- **UI labelling is page-local** — there's no risk that a future page picks the wrong label because the storage default flipped. Each page consciously declares its semantics.
- **Future rule-based surfaces can choose their own semantics** without changing storage or evaluator code. A hypothetical "predictability scope" rule might be `Include` semantics; a hypothetical "cycle-time exclusion" rule might be `Exclude`; both reuse the evaluator unchanged.

### Negative

- **`RuleSetSemantics` enum is not surfaced to the frontend** — the FE labelling lives in TS strings on each page. Risk: a future maintainer renames the FE label inconsistently across the five surfaces this feature touches (US-03 chip, editor title, forecast result narrative, etc.). Mitigation: chip and editor-title strings are constants in one TS file per page; a Vitest snapshot of each catches accidental renames.
- **Two consumers, two callers, two decisions** — there's no compile-time enforcement that says "this consumer always uses Exclude semantics." A bug where `ForecastFilterRuleService.Filter` accidentally returns matched (instead of `Except(matched)`) would be a behaviour regression caught by the integration test, not by the type system. Mitigation: explicit unit test on `Filter` asserting "rule matches Bug → Bug is NOT in the output" (D8 semantics) is one of the first GREEN tests in Slice 01's TDD cycle.

### Sensitivity / trade-off points

- **Sensitivity**: if a future rule-based surface needs **per-condition semantics** (e.g. "exclude if Type=Bug AND include if Tag=critical"), this design does not support it. It would require either Option B (per-condition semantics is a property of the condition) or a refactor of the rule set itself. Out of scope for this feature; the assumption is that an entire rule set is uniformly Include or uniformly Exclude.

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| Persisted `DeliveryRuleSet` JSON shape carries no semantics field | `RuleEngineReuseCanaryTests` (DDD-7) asserts that a stored `forecastFilterRuleSetJson` deserialises into a `DeliveryRuleSet` with `Conditions` populated and no other top-level fields. |
| `RuleEvaluator<T>.Match` does NOT take a semantics or exclude/include parameter | NUnit reflection test on the `IRuleEvaluator<T>` interface — fails if a `bool` or `RuleSetSemantics` parameter creeps in. |
| `ForecastFilterRuleService.Filter` semantics is exactly D8 (matched items are excluded) | Targeted unit test: rule set matches Bug → output contains no Bugs AND contains all non-Bugs. |
