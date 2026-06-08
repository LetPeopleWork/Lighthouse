# ADR-063: Named Cycle Time Definition Validity (D5) — Single Source of Truth as a Method on the Settings Aggregate, Projected into Every DTO, Mirrored by ONE Pure TS Predicate; and the US-04 Cumulative-Scope Read

**Status**: Accepted (2026-06-08 — Morgan; Fork 3 confirmed by user)
**Date**: 2026-06-08
**Feature**: multiple-cycle-times (Epic 5251)
**Decider**: Morgan (Solution Architect)
**Relationship to prior ADRs**: pairs with ADR-061 (computation), ADR-062 (read endpoint). Reuses `WorkTrackingSystemOptionsOwner.AllStates` + `GetRawStatesForCategory` (ADR-056). Resolves DISCUSS **D5** (invalid-on-removal) and the **D6b/US-04** cumulative-scope read. Addresses the DISCUSS HIGH integration risk: "the one definition must read identically everywhere (config list + both chart selectors)."

---

## Context

DISCUSS locked (D5): a saved definition referencing a removed/renamed boundary state becomes INVALID — warned in the config list AND disabled in BOTH chart selectors (scatter + cumulative scope), never a silent break or crash. DISCUSS flagged this as the **highest integration risk** in the shared-artifact registry: the validity verdict MUST read identically in three UI surfaces; any divergence is a defect.

The fork: where "is this definition's start/end still present in current `AllStates`?" lives — (i) a method on the settings aggregate, (ii) a separate domain service, (iii) computed ad hoc in each DTO projection / each FE selector.

Code reality (verified): `AllStates` (`WorkTrackingSystemOptionsOwner` L26–28) is the mapping-expanded ordered universe; `GetRawStatesForCategory` (L89–109) is the single resolver. The settings aggregate is the natural owner of both the saved `CycleTimeDefinitions` and the current `AllStates` they must validate against. The FE already derives state lists from the settings DTO (`StatesList`, `StateMappingsEditor` per ADR-056).

---

## Decision

### 1. Validity is a method on the settings aggregate (the SSOT)

`CycleTimeDefinition` validity is computed by ONE method on `WorkTrackingSystemOptionsOwner` (the aggregate that owns both the definitions and the live `AllStates`):

```csharp
// on WorkTrackingSystemOptionsOwner
public bool IsCycleTimeDefinitionValid(CycleTimeDefinition definition)
{
    var states = AllStates.ToHashSet(StringComparer.OrdinalIgnoreCase);
    var startRaw = GetRawStatesForCategory([definition.StartState]);
    var endRaw   = GetRawStatesForCategory([definition.EndState]);
    return startRaw.Count > 0 && endRaw.Count > 0
        && startRaw.All(states.Contains) && endRaw.All(states.Contains);
}
```

A definition is valid iff BOTH its start and end resolve (via the existing mapping resolver) to states still present in the current `AllStates`. This is the ONLY backend implementation of the predicate. Save-time end-after-start ordering (D4) is a separate validator at write time; THIS method is the read-time "still-references-present-states" check (D5).

### 2. Validity is PROJECTED into every read DTO — never recomputed downstream

- The settings DTO (`CycleTimeDefinitionDto`, carried on `SettingsOwnerDtoBase` next to the state lists — ADR-064) includes an `IsValid` boolean stamped by `IsCycleTimeDefinitionValid` at projection time. The config list reads `IsValid` directly.
- The named-series read endpoint (ADR-062) calls the SAME method server-side; an invalid definition yields the empty-series + invalid signal (no compute, no crash — D5).
- The cumulative-scope read (US-04, §4) calls the SAME method; an invalid definition is refused (unscoped fallback).

No surface recomputes validity from raw state lists. The verdict travels as a stamped boolean from the one method.

### 3. ONE pure TS predicate mirrors it for FE selector disabling

The two chart selectors (scatter + cumulative scope) consume the SAME `IsValid` flag from the settings DTO they already fetch — they do NOT re-derive validity. Where a selector must reason about validity before a read (e.g. greying an option), it uses ONE shared pure TS predicate `isCycleTimeDefinitionValid(definition, allStates, stateMappings)` (a thin twin of §1, the same mapping-resolution rule as the FE `getRawStatesForCategory` twin established for wait-states/cumulative, ADR-054/056), imported by BOTH selectors and the config list. ONE FE function, three call sites — no third, fourth, fifth copy. Preferred path: consume the server-stamped `IsValid` and use the TS predicate only for live, pre-save selector reasoning.

### 4. US-04 cumulative scope — extend the existing `cumulativeStateTime` endpoint with `definitionId`

The cumulative-time-per-state scope (D6b) is served by EXTENDING the existing `GET …/metrics/cumulativeStateTime?startDate&endDate[&itemIds]` with an optional `definitionId` (parallel to ADR-062, same additive-param → NO client version gate):

- `definitionId` absent ⇒ today's full-workflow behaviour, byte-identical (US-04 "switch off ⇒ exactly as today").
- `definitionId` present + valid ⇒ the per-state aggregation (`ComputeCumulativeStateTime` over `BuildCumulativeWorkflowStateOrder`) is restricted to the half-open `[enter start … enter end)` window (D10): only states occupied from entering `startState` up to (excluding) entering `endState` contribute; the end state contributes no in-window time and so has no bar. This reuses the SAME boundary resolution (`NamedCycleTimeDays` index logic, ADR-061) so the scatter duration and the cumulative scope cover the IDENTICAL span — the cross-surface-consistency guarantee is by construction, not by parallel code.
- `definitionId` present + invalid ⇒ refused; chart stays unscoped (D5/US-04 example 3).

Cache key gains the `_Def_{definitionId}` suffix (same idiom as ADR-062 / `SelectionCacheSuffix`).

---

## Alternatives Considered

**Option (i) (chosen): method on the settings aggregate, projected as a stamped `IsValid`, mirrored by one pure TS predicate.**

- Pros: the aggregate owns BOTH the definitions and the live `AllStates` — validity is a natural aggregate invariant; one BE implementation + one stamped boolean means every backend surface (config DTO, scatter read, cumulative read) is forced to agree; one FE predicate covers the three UI surfaces. Directly retires the HIGH cross-surface-consistency risk. Reuses `GetRawStatesForCategory` (no new resolver).
- Cons: a small amount of validity logic exists twice across the stack (C# method + TS predicate), inherent to a C#-backend/TS-frontend split. Mitigated by the preferred "consume the stamped `IsValid`" path and the single-TS-function rule.

**Option (ii): a dedicated `ICycleTimeDefinitionValidator` domain service.**

- Pros: separable, independently mockable.
- Cons: the validity check needs the owner's `AllStates` + `GetRawStatesForCategory` — i.e. it needs the aggregate anyway; a separate service would just wrap the aggregate, adding a DI seam for a one-line predicate. The cumulative/save-time validators are already aggregate-proximate. Over-engineered for the surface. Rejected (reserve for if validity grows complex rules).

**Option (iii): compute validity ad hoc in each DTO projection and each FE selector.**

- Pros: no shared method.
- Cons: this IS the failure mode the DISCUSS HIGH risk warns against — N independent reimplementations of "is the boundary still in AllStates" across config DTO, scatter selector, cumulative selector, each free to drift (e.g. one forgets mapping resolution). The exact silent-divergence defect D5 must prevent. Rejected outright.

---

## Consequences

**Positive**:

- ONE backend predicate + ONE stamped boolean ⇒ config list, scatter read, and cumulative read cannot disagree on validity; the HIGH cross-surface risk is structurally retired.
- ONE FE predicate (used sparingly behind the stamped flag) covers the three UI surfaces.
- US-04 scope reuses the SAME boundary resolution as the scatter ⇒ scatter duration and cumulative span are the identical window by construction (no separate inclusive/exclusive toggle — D10).
- Reuses `GetRawStatesForCategory`/`AllStates` — no new resolver; mapping-aware everywhere.

**Negative**:

- Validity logic mirrored C#↔TS (stack-inherent). Mitigated by stamped-flag-first + single-function rule + parity test.

**Neutral**:

- Save-time ordering (D4) and read-time presence (D5) are distinct checks; both live on/near the aggregate.

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| ONE backend validity method; surfaces consume the stamped `IsValid`, never recompute | ArchUnitNET/grep: only `IsCycleTimeDefinitionValid` resolves boundaries against `AllStates`; NUnit on the method |
| Config DTO, scatter read, cumulative read agree on validity for the same definition | Integration test across all three with a removed boundary state ⇒ all report invalid |
| ONE pure TS predicate (`isCycleTimeDefinitionValid`), three call sites | Vitest; grep: single definition imported by config list + both selectors |
| C#↔TS validity parity | Shared fixture (removed/renamed/mapping boundary) asserted equal both stacks |
| `cumulativeStateTime` `definitionId` absent ⇒ byte-identical; present+valid ⇒ half-open window, end-state no bar (D10) | Integration: golden unscoped equality; NUnit: "Done" contributes no bar when end=Done |
| Cumulative scope reuses the scatter boundary resolution (same span) | NUnit: scatter named duration window == cumulative scoped span for the same definition |
| `cumulativeStateTime` `definitionId` is additive ⇒ NO client version gate | Clients-repo handoff note |

---

## Cross-feature impact

- `state-time-cumulative-view`: `cumulativeStateTime` gains an optional `definitionId`; the unscoped contract/DTO is unchanged (additive). The per-state aggregation (`ComputeCumulativeStateTime`) is reused, restricted to the window.
- `wait-states-flow-efficiency`: the wait-bar highlight (ADR-057) layers on the cumulative bars; when scoped to a named window the highlight applies to the in-window bars unchanged (no new interaction designed in MVP — flagged as a verify-in-DELIVER note, not a contract).
- Lighthouse-Clients: `definitionId` additive on `cumulativeStateTime` ⇒ no gate (parallel to ADR-062).
