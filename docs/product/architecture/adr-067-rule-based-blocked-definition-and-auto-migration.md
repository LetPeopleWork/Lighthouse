# ADR-067: Rule-Based Blocked Definition — `BlockedRuleSet` JSON Column on `WorkTrackingSystemOptionsOwner` (Third Include Consumer), Single `IsBlocked` via `RuleEvaluator<T>`, Application-Layer One-Time Auto-Migration

**Status**: Accepted (2026-06-12 — Morgan, interaction mode PROPOSE)
**Date**: 2026-06-12
**Feature**: epic-5074-blocked-items (Slice 01 — foundation / walking skeleton)
**Decider**: Morgan (Solution Architect)
**Relationship to prior ADRs**: third Include consumer of the rule engine after DeliveryRule (Include) and ForecastFilter (Exclude) — builds directly on ADR-012 (rule-engine generalisation: `IRuleEvaluator<T>` + `IRuleFieldProvider<T>`) and ADR-013 (semantics decided at the caller, persisted JSON carries no semantics). Storage idiom follows the EXISTING `ForecastFilterRuleSetJson` / `Delivery.RuleDefinitionJson` JSON-column precedent (NOT the owned-collection idiom of ADR-064, which applies to structured non-rule config). Resolves DISCUSS decisions D-ENGINE, D-MIGRATE, D1, D2, D3.

---

## Context

Today "blocked" is two hardcoded flat lists on the shared settings aggregate `WorkTrackingSystemOptionsOwner` (base of both `Team` and `Portfolio`):

```csharp
public List<string> BlockedStates { get; set; } = [];   // L41
public List<string> BlockedTags { get; set; } = [];     // L45
```

`IsBlocked` is computed inline on the work-item models:

- `WorkItem.IsBlocked` (L24): `Team != null && (Team.BlockedStates.IsItemInList(State) || Tags.Any(Team.BlockedTags.IsItemInList))` — case-insensitive via `IsItemInList`.
- `Feature.IsBlocked` (L64): `Portfolios.Any(p => p.BlockedStates.Contains(State) || p.BlockedTags.Any(Tags.Contains))` — case-**sensitive** (`Contains`). A latent inconsistency between the two surfaces.

The rule engine (ADR-012/013) already supports exactly the shape blocked needs: `WorkItemRuleSet { Mode, List<WorkItemRuleCondition> }`, `RuleEvaluator<T>.Match(ruleSet, items, fieldProvider)` (semantics-agnostic), and BOTH `WorkItemFieldProvider` (fixed keys `workitem.{type,state,name,referenceid,parentreferenceid,tags}` + `additionalField.{id}`) and `FeatureFieldProvider` (the `feature.*` equivalents) exist. `AdditionalFieldValues : Dictionary<int,string?>` is on `WorkItemBase`, so both `WorkItem` and `Feature` resolve `additionalField.{id}` through the same generic id-keyed path. The six `RuleOperators` (`equals/notequals/contains/notcontains/isempty/isnotempty`) suffice — a Jira flag custom field is `additionalField.{id} isnotempty`.

Existing rule sets persist as a **JSON string column**, evaluated caller-side: `Team.ForecastFilterRuleSetJson` (L19), `Delivery.RuleDefinitionJson` (L47). No EF owned-collection, no value converter — `JsonSerializer.Deserialize<WorkItemRuleSet>(...)` at the call site. `ForecastFilterRuleSetJson` is **Team-only**; blocked must live on BOTH Team and Portfolio (the base aggregate).

The journey's HIGH-risk shared artifact: there must be exactly ONE definition of blocked. A leftover `BlockedStates`/`BlockedTags` read path after migration = two contradictory definitions (forbidden). The `IsBlocked` read blast radius is small: `WorkItemDto.cs:28` (serialized to clients/FE), `WorkItemService.cs:121` (the edge-triggered `WorkItemBlocked` event + `WasBlocked` at L103).

---

## Decision

### 1. `BlockedRuleSet` is a JSON-column `WorkItemRuleSet` on the shared aggregate, with Include semantics

A new nullable string column `BlockedRuleSetJson` on `WorkTrackingSystemOptionsOwner` (so BOTH Team and Portfolio inherit it — unlike `ForecastFilterRuleSetJson` which is Team-only). It holds a serialized `WorkItemRuleSet` (the byte-identical shape delivery rules and forecast filters already persist; ADR-013 canary applies). `BlockedStates` and `BlockedTags` are **removed** from the aggregate, the settings DTO, and every read path in the same change (migration is destructive of the old columns by design — see §3).

- **Semantics**: Include (caller decides, ADR-013). A work item that MATCHES the blocked rule set IS blocked — the mirror of DeliveryRule's Include, the inverse of ForecastFilter's Exclude. The persisted JSON carries no semantics field.
- **Storage rationale**: JSON column, not owned-collection (ADR-064). A `WorkItemRuleSet` is the EXACT shape already JSON-persisted twice (`ForecastFilterRuleSetJson`, `RuleDefinitionJson`); reusing that idiom keeps one serialization contract and lets the ADR-013 canary protect shape parity. ADR-064's owned-collection applies to structured *non-rule* config (`CycleTimeDefinitions`, `StateMappings`) that needs a stable per-row `Id`; a rule set needs no per-condition DB identity.

### 2. One `IsBlocked`, computed via `RuleEvaluator<T>` over `BlockedRuleSet` — for BOTH WorkItem and Feature

Introduce a single application-layer service `IBlockedItemService` (mirroring `ForecastFilterRuleService`'s thin-delegator shape, ADR-012 §5) with two typed entry points sharing one algorithm:

```
bool IsBlocked(WorkItem item, Team owner)         // RuleEvaluator<WorkItem>.Match over owner.BlockedRuleSet, WorkItemFieldProvider
bool IsBlocked(Feature item, Portfolio owner)     // RuleEvaluator<Feature>.Match over owner.BlockedRuleSet, FeatureFieldProvider
```

Both delegate to the shared `RuleEvaluator<T>` (Include: matched ⇒ blocked). The inline `WorkItem.IsBlocked` / `Feature.IsBlocked` properties are removed; the model no longer reads `BlockedStates`/`BlockedTags` (those fields no longer exist). This unifies the case-sensitivity inconsistency (rule operators are case-insensitive per `RuleEvaluator`, so Feature-blocked becomes case-insensitive too — a correctness improvement, called out in Consequences). An empty rule set (no conditions) ⇒ `RuleEvaluator` returns no matches ⇒ nothing blocked (matches today's empty-lists behaviour).

The `WasBlocked` precedent computation in `WorkItemService` (L103) routes through the same `IBlockedItemService.IsBlocked` so enter/leave detection (slice 02) reads the single definition.

### 3. One-time auto-migration is application-layer + idempotent, NOT an EF data-migration

Each existing `BlockedStates` value → a `State equals <value>` condition; each `BlockedTags` value → a `Tags contains <value>` condition; OR-combined (`Mode = "or"`). Run **once per owner in the application layer** on first access after deploy, guarded by a natural condition (`BlockedRuleSetJson is null AND (BlockedStates or BlockedTags non-empty)`), NOT by a `=true` sentinel (forecast-minimum-data-guard non-idempotency lesson).

- **Mechanism**: a one-time migration step invoked from the existing settings read/sync path that, for each owner whose `BlockedRuleSetJson` is null, synthesizes the OR'd rule set from the legacy lists and persists it. Because the legacy columns are dropped in the SAME release, the EF schema migration (generated via the `CreateMigration` PowerShell script across all providers — NOT `dotnet ef migrations add`; mind the `--no-incremental` stale-DLL rebuild trap) must run a **data backfill step before dropping the columns**: read `BlockedStates`/`BlockedTags`, write the synthesized JSON into `BlockedRuleSetJson`, then drop the legacy columns. This is the loss-free transform.
- **Loss-free proof (the slice-01 learning hypothesis)**: an integration test on a real provider seeds owners with representative legacy config, runs the migration, and asserts (a) the synthesized rule set matches the SAME set of items as the legacy lists did for every item in a fixture corpus (no item changes blocked status), and (b) an empty legacy config yields an empty rule set (nothing blocked). Case-sensitivity note: Feature-blocked was case-sensitive (`Contains`); the migrated `Tags contains` is case-insensitive — the test asserts no REAL fixture item flips, and the change is documented as an intentional correctness fix.

### 4. Validation rides the existing settings write, mirroring the forecast-filter validator

`BlockedRuleSetJson` validates on the team/portfolio settings PUT exactly as `ForecastFilterRuleSetJson` does in `TeamController.ValidateForecastFilterRuleSet` (deserialize → null/empty is valid → else `IRuleEvaluator.IsValid(ruleSet, schema)`). This is added to BOTH `TeamController` and `PortfolioController` settings PUT (Portfolio currently validates no rule set — it gains the blocked validation). The schema is composed from the owner's connection additional-field definitions (the existing `GetRuleSchema` composition).

---

## Alternatives Considered

**Storage — Option A (chosen): JSON column `BlockedRuleSetJson` on the base aggregate.**
- Pros: exact reuse of the shipped rule-set persistence idiom (`ForecastFilterRuleSetJson`); one serialization contract; ADR-013 canary protects shape; no owned-collection mapping; trivially on the base class ⇒ Team + Portfolio for free.
- Cons: no per-condition DB identity (not needed for a rule set); JSON-column case-insensitivity deserialize foot-gun (mitigated — `WorkItemRuleSet` deserialization already proven across two consumers).

**Storage — Option B: owned EF collection of `BlockedRuleCondition` (ADR-064 idiom).**
- Pros: queryable per-condition; stable `Id`.
- Cons: diverges from the EXISTING rule-set persistence (delivery rules + forecast filter are JSON columns) — would make blocked the ONLY rule set stored differently, breaking the ADR-013 canary's shape-parity premise and forcing a bespoke mapping. Rejected — rule sets are opaque blobs evaluated wholesale, not row-queried.

**IsBlocked placement — Option C: keep `IsBlocked` inline on the model, reading the rule set.**
- Pros: no new service.
- Cons: the model would need a `RuleEvaluator` + field provider dependency injected into a property getter (impossible cleanly) or a static evaluator (untestable, hidden dependency). The thin-delegator service (ADR-012 §5 precedent: `ForecastFilterRuleService`) keeps evaluation testable and ports-and-adapters-clean. Rejected.

**Migration — Option D: EF-only data migration (no application-layer guard).**
- Pros: runs once at deploy, no runtime guard.
- Cons: InMemory tests miss EF migrations (recurring memory); a pure SQL data step can't reuse the C# rule-set synthesis logic (risking divergence between the migrated shape and what the validator/evaluator expects). The chosen hybrid (EF backfill step that materializes the SAME synthesized JSON the app would produce, proven by a real-provider test) is loss-free AND testable. Rejected as the sole mechanism.

---

## Consequences

**Positive**:
- ONE definition of blocked (`RuleEvaluator` over `BlockedRuleSet`) for every downstream signal — the journey's HIGH-risk invariant is true by construction once the legacy columns are dropped.
- Third Include consumer reuses the entire engine (evaluator, field providers, schema, six operators, MaxRules/MaxValueLength) — near-zero new evaluation code.
- Feature-blocked becomes case-insensitive (correctness fix; the old `Contains` was an inconsistency vs `WorkItem.IsItemInList`).
- Carlos can express "blocked = our Blocked state OR the Jira flag" — the elevator-pitch payoff.

**Negative**:
- Destructive migration: the legacy columns are dropped. Mitigated by the loss-free real-provider test (the slice-01 hard AC) and the EF backfill-before-drop ordering.
- A new thin service (`IBlockedItemService`) + its registration. Minimal, idiomatic (mirrors `ForecastFilterRuleService`).

**Neutral**:
- `WorkItemDto.IsBlocked` wire field is unchanged (still a computed bool on the DTO) — the FE and clients see no contract change for the blocked BOOLEAN (the config CONTRACT changes — see ADR-072).

---

## Earned Trust — probing the dependencies

- **Migration loss-free probe**: real-provider (SQLite + Postgres) integration test seeds legacy `BlockedStates`/`BlockedTags`, runs the backfill migration, asserts every fixture item's blocked status is identical pre/post and the synthesized rule set round-trips through the validator. A migration that changes any item's blocked status refuses the build.
- **Single-definition probe**: a test asserts the per-item badge, the overview widget count, and `WorkItemService.WasBlocked` all resolve through `IBlockedItemService.IsBlocked` — and a grep/ArchUnit test asserts NO production code references `BlockedStates`/`BlockedTags` after the slice (the columns/properties no longer exist).
- **Empty-config probe**: an owner with an empty rule set blocks nothing (matches the legacy empty-lists behaviour).

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| Exactly one `IsBlocked` evaluation path (`IBlockedItemService` over `BlockedRuleSet`) | ArchUnitNET: no production type other than `IBlockedItemService`/`RuleEvaluator` resolves blocked; grep test asserts `BlockedStates`/`BlockedTags` symbols are gone |
| `BlockedRuleSet` JSON shape is `WorkItemRuleSet`-identical | The existing ADR-013 `RuleEngineReuseCanaryTests` extended to deserialize a stored `BlockedRuleSetJson` |
| Migration is loss-free | Real-provider integration test (SQLite + Postgres), pre/post blocked-status equality on a fixture corpus; migration via `CreateMigration` |
| `BlockedRuleSet` validated on the settings write for BOTH Team and Portfolio | NUnit on the settings-write validator (mirrors `ValidateForecastFilterRuleSet`); Portfolio gains the validation |
| Include semantics (matched ⇒ blocked) decided at the caller, not in storage | Unit test: a rule matching an item ⇒ `IsBlocked == true`; ADR-013 canary asserts no semantics field in the JSON |

---

## Cross-feature impact

- ADR-012/013: third consumer; evaluator + field providers + canary reused unchanged.
- ADR-026 (staleness): consumes `IWorkItem.isBlocked` (now rule-derived) unchanged — the value's MEANING is richer, its TYPE is the same bool. See ADR-070 for the blocked→stale extension.
- Lighthouse-Clients: the settings CONTRACT changes (`blockedRuleSet` replaces `blockedStates`/`blockedTags`) — version-gated (ADR-072). The `IsBlocked` bool on `WorkItemDto` is unchanged.
- Slices 02–05 all derive from this single definition.
