# ADR-064: `CycleTimeDefinitions` Storage — Owned Collection of a `CycleTimeDefinition` Entity on `WorkTrackingSystemOptionsOwner`, Carried Additively on the Settings DTO, Rides the Existing Tokened Settings Write

**Status**: Accepted (2026-06-08 — Morgan, interaction mode PROPOSE)
**Date**: 2026-06-08
**Feature**: multiple-cycle-times (Epic 5251)
**Decider**: Morgan (Solution Architect)
**Relationship to prior ADRs**: follows ADR-056 (`WaitStates` additive field on the same aggregate/DTO, settings-write rule) and ADR-060 (recurring-blackout-rule entity persistence pattern). Resolves DISCUSS **D8** (config write rides existing settings) persistence shape.

---

## Context

DISCUSS locked (D8): config writes for named cycle times ride the EXISTING team/portfolio settings endpoint — no new write contract — and `SettingsOwnerDtoBase` (which already carries the state lists) is the natural home for `CycleTimeDefinitions`. A definition is `{ name, startState, endState }` (D1), 0..N per owner, distinct from the scalar `List<string>` fields (`WaitStates`, `BlockedStates`) because it is a small structured record, not a flat string list.

Code reality: `WorkTrackingSystemOptionsOwner` already owns scalar `List<string>` config (`WaitStates` L43, `BlockedStates` L41) AND a structured owned collection `List<StateMapping> StateMappings` (L47) with its own DTO (`StateMappingDto`) projected on `SettingsOwnerDtoBase` (L37, L87). `StateMappings` is the precedent for a structured owned collection on this aggregate. The aggregate is `IConcurrencyTokenEntity` (epic-5121) — any added field inherits no-silent-lost-update for free. Migrations are generated via the `CreateMigration` PowerShell script across providers (Sqlite + Postgres), NOT `dotnet ef migrations add` (CLAUDE.md); InMemory tests miss persisted-model migrations.

---

## Decision

### 1. `CycleTimeDefinition` is a small entity in an owned collection (mirror `StateMapping`)

```csharp
public class CycleTimeDefinition   // mirrors StateMapping shape
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StartState { get; set; } = string.Empty;   // raw state OR mapping name (resolved via GetRawStatesForCategory)
    public string EndState { get; set; } = string.Empty;
}

// on WorkTrackingSystemOptionsOwner, next to StateMappings:
public List<CycleTimeDefinition> CycleTimeDefinitions { get; set; } = [];
```

EF persistence mirrors EXACTLY how `StateMappings` is persisted (the structurally identical structured owned collection on the same owner) — same owned-entity / value-collection mechanism, same provider coverage. **The migration for the new field is generated via the `CreateMigration` PowerShell script (all providers) at DELIVER, NOT `dotnet ef migrations add`; mind the `--no-incremental` stale-migration-DLL rebuild trap (prior memory). InMemory tests do NOT exercise the migration — an explicit read-your-writes integration test against a real provider is required (DELIVER).**

### 2. Carried additively on the settings DTO with a stamped `IsValid`

`SettingsOwnerDtoBase` gains `List<CycleTimeDefinitionDto> CycleTimeDefinitions` (mirroring the `StateMappings → StateMappingDto` projection at L37). Each `CycleTimeDefinitionDto` is `{ Id, Name, StartState, EndState, IsValid }` where `IsValid` is stamped by `IsCycleTimeDefinitionValid` (ADR-063 §2) at projection time. Additive field on the existing settings contract ⇒ **no client version gate** (D8; ADR-062 §3).

### 3. Validation at the existing settings write

The existing settings write path validates `CycleTimeDefinitions` on save: end-after-start in `AllStates` order (D4), name non-empty + unique per owner (D4), mapping resolution via `GetRawStatesForCategory` (D3). Invalid SAVE is rejected inline (US-02 error path); this is distinct from read-time presence validity (D5/ADR-063). Concurrency token enforced by the existing aggregate (epic-5121) — no new concurrency design.

---

## Alternatives Considered

**Option A (chosen): owned collection of a `CycleTimeDefinition` entity, mirroring `StateMappings`.**

- Pros: exact precedent (`StateMappings` is a structured owned collection on the same aggregate with a DTO projection); lowest-surprise EF mapping + migration; carries a stable `Id` for `definitionId`-keyed reads (ADR-062/063) and telemetry (KPI 2); rides the tokened settings write for free.
- Cons: a new tiny entity + DTO + migration. Minimal and idiomatic.

**Option B: serialize `CycleTimeDefinitions` as a JSON column (single string).**

- Pros: no owned-collection mapping; one column.
- Cons: no stable per-definition `Id` for `definitionId`-keyed reads (would need a synthetic key inside JSON); diverges from the `StateMappings` precedent on the SAME aggregate (inconsistent persistence idioms); JSON-column case-insensitivity foot-gun (prior memory) at the deserialize boundary. Rejected — `StateMappings` precedent is cleaner and id-stable.

**Option C: a separate top-level `CycleTimeDefinition` table keyed by ownerId (not owned).**

- Pros: independent lifecycle.
- Cons: definitions have NO lifecycle independent of their owner (they are deleted with the owner, validated against the owner's `AllStates`) — they are a textbook owned collection. A separate aggregate adds a join + a second write path, contradicting D8 "rides the existing settings write." Rejected.

---

## Consequences

**Positive**:

- Mirrors the shipped `StateMappings` owned-collection idiom — predictable to map, migrate, project, and test; id-stable for `definitionId` reads + telemetry.
- Additive DTO field ⇒ no client version gate; rides the tokened settings write ⇒ concurrency inherited.

**Negative**:

- One migration across providers via `CreateMigration` (DELIVER); InMemory tests miss it ⇒ explicit real-provider read-your-writes test required. Flagged.

**Neutral**:

- `IsValid` is a projected (not persisted) DTO field — recomputed at read from current `AllStates` (ADR-063), correctly reflecting later workflow edits.

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| `CycleTimeDefinitions` persists like `StateMappings` (same mechanism); read-your-writes on reload | NUnit EF InMemory + real-provider integration test; migration via `CreateMigration` (DELIVER) |
| Additive DTO field ⇒ existing settings write/read otherwise unchanged | Integration: pre-feature settings round-trip unaffected |
| Save-time validation: end-after-start (D4), name non-empty + unique (D4), mapping-resolved (D3) | NUnit on the settings-write validator; US-02 error-path integration test |
| `IsValid` stamped at projection (not persisted), reflects current `AllStates` | NUnit: remove a boundary state ⇒ DTO `IsValid == false` without re-save |
| Rides the tokened aggregate (no new concurrency design) | Inherited from epic-5121; stale-write integration test |

---

## Cross-feature impact

- Settings aggregate: a fourth structured config concern alongside `StateMappings`, `WaitStates`, `BlockedStates`; same write/concurrency path.
- Lighthouse-Clients: additive settings field ⇒ no gate (D8).
- Migration: one new field, all providers via `CreateMigration` — DELIVER task.
