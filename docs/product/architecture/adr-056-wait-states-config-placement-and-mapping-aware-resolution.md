# ADR-056: Wait States Config — Mapping-Aware `WaitStates` Field + Sibling `WaitStatesEditor` Next to `StateMappingsEditor` (No `StateMappingsEditor` Relocation), Resolved via `GetRawStatesForCategory`

**Status**: Accepted (2026-06-05 — Morgan, interaction mode PROPOSE)
**Date**: 2026-06-05
**Feature**: wait-states-flow-efficiency (Story #5173)
**Decider**: Morgan (Solution Architect)
**Relationship to prior ADRs**: independent of the cumulative ADRs; consumes `WorkTrackingSystemOptionsOwner.GetRawStatesForCategory` (existing). Resolves DISCUSS-deferred decision **D12** (container shape) and pins the **D11** mapping-aware resolution in the model + UI.

---

## Context

DISCUSS locked (D1/D11/D12) that `WaitStates` is a NEW mapping-aware `List<string>` on `WorkTrackingSystemOptionsOwner`, edited in the **state-config cluster** next to `StateMappingsEditor`, decoupled from Blocked States (which is evolving independently — out of scope to touch). DISCUSS deferred to DESIGN the **container shape**:

> (a) a new "Advanced State config" wrapper that **relocates** the existing `StateMappingsEditor` into it, **vs** (b) a Wait States `InputGroup`/editor **sibling** next to `StateMappingsEditor` **without regrouping**. Relocating the existing mappings editor is a brownfield refactor whose scope DESIGN must size.

Code reality (verified):

- `WorkTrackingSystemOptionsOwner.cs`: has `DoingStates`, `BlockedStates` (L41), `StateMappings: List<StateMapping>` (L45), and `GetRawStatesForCategory(List<string>)` (L87) which expands a `StateMapping.Name` entry to its raw `States`, else treats the entry as a raw state. `MapStateToStateCategory` uses the same resolver.
- `ModifyTeamSettings.tsx` (~L153–196) and `ModifyProjectSettings.tsx` render three siblings in order: `StatesList` → `StateMappingsEditor` → `FlowMetricsConfigurationComponent` (which holds Blocked States — out of scope, D12). `StateMappingsEditor` is invoked with `stateMappings`, `doingStates`, an `onChange` that **reconciles `doingStates` via `reconcileDoingStates`** when mappings change, plus `validationErrors`, `refreshFailed`, `onReloadDependentData`. It is wired into BOTH the team and project settings forms with non-trivial reconciliation logic.

The container-shape choice is a blast-radius question: relocating `StateMappingsEditor` means re-threading its 6 props + the `reconcileDoingStates` coupling through a new wrapper in TWO call sites, with no behavioural change to mappings — pure structural churn on a shipped, tested component.

---

## Decision

### 1. Model: mapping-aware `WaitStates` resolved via the existing `GetRawStatesForCategory`

```csharp
public List<string> WaitStates { get; set; } = [];   // on WorkTrackingSystemOptionsOwner, next to BlockedStates / StateMappings
```

The efficiency `waitTime` and the chart highlight resolve wait states through the EXISTING `GetRawStatesForCategory(WaitStates)` — the SAME expansion the categories use (D11). No new resolver is written on the backend; the wait-time fold (ADR-054 §3) and any wait-membership check call `GetRawStatesForCategory(WaitStates)`. A `WaitStates` entry is "wait" iff it lands in that expanded set ∩ the Doing raw set (an entry resolving outside Doing contributes nothing to the denominator — US-01 edge case, by construction since the fold is over Doing-category per-state rows only).

EF persistence: `WaitStates` is a `List<string>` and MUST mirror EXACTLY how `BlockedStates` (the structurally identical sibling `List<string>` on the same owner) is persisted — same value-conversion / owned-collection mechanism, same provider coverage. The migration is generated via the `CreateMigration` PowerShell script (all providers per CLAUDE.md), NOT `dotnet ef migrations add`; mind the `--no-incremental` stale-migration-DLL rebuild trap (prior memory). **This is a DELIVER task, not run at DESIGN.**

### 2. UI container shape: Option (b) — sibling `WaitStatesEditor`, NO relocation of `StateMappingsEditor`

A NEW component `WaitStatesEditor.tsx` is added as a **sibling immediately after `StateMappingsEditor`** in both `ModifyTeamSettings.tsx` and `ModifyProjectSettings.tsx`. The existing `StateMappingsEditor` is **NOT moved, NOT re-parented, NOT re-propped**. The two together visually form the "Advanced State config" cluster (D12's preferred direction) through layout/heading proximity and a shared section affordance, **without** a wrapper component that owns and relocates the mappings editor.

`WaitStatesEditor` props:

```ts
interface WaitStatesEditorProps {
  waitStates: string[];
  doingStates: string[];                 // raw Doing-state suggestions
  stateMappingNames: string[];           // mapping-name suggestions (same derivation StatesList already uses)
  onChange: (next: string[]) => void;    // writes onSettingsChange("waitStates", next)
}
```

It is gated by a "Configure Wait States" toggle and uses the EXISTING `InputGroup` + the `ItemListManager`/autocomplete idiom (the same pattern `StatesList` and the Blocked States sub-section use). Its suggestion list is `doingStates ∪ stateMappingNames` (D11) — `stateMappingNames` derived exactly as `StatesList` already derives it (`stateMappings.filter(name != "").map(name)`, see `ModifyTeamSettings.tsx` L167–171). Helper text: "Wait states are Doing-states (or State Mappings) where work sits idle (waiting, queued) rather than being actively worked. Flow efficiency = active time / total Doing time."

**Rationale for (b) over (a)**: relocating `StateMappingsEditor` is pure structural churn on a shipped component with a non-trivial `reconcileDoingStates` coupling and 6 props threaded through two call sites — high blast radius, zero behavioural benefit, and it would touch (and risk regressing) the mappings editor that this feature explicitly does NOT change (out-of-scope: "does not change how mappings are created"). The DISCUSS hard requirements (state-cluster placement + decoupled-from-Blocked + mapping-aware selector) are ALL satisfied by a sibling without regrouping. The "Advanced State config" visual grouping is achieved by layout/heading, not by a relocating wrapper. If a future settings-IA refactor wants a true wrapper, it can adopt one for `StatesList` + `StateMappingsEditor` + `WaitStatesEditor` together as a deliberate IA change — out of scope here.

**Blast radius of (b)**: one new component file + one new `useItemListManager`-style handler pair (`handleAddWaitState`/`handleRemoveWaitState`) + one render-slot insertion in each of the two settings forms + the `waitStates` field added to the settings TS model/Zod schema and the BE settings DTO/validator. No change to `StateMappingsEditor`, `StatesList`, `FlowMetricsConfigurationComponent`, or `reconcileDoingStates`.

### 3. Validation: keep wait states within reach of Doing (advisory, non-blocking)

Suggestions only ever offer raw Doing-states + mapping names (so the happy path stays in-Doing). A free-typed entry outside the Doing set is ACCEPTED into the list but contributes nothing to the denominator (US-01 edge case — no hard validation error, matching the permissive `BlockedStates` editor behaviour). A `StateMappingValidator`-parallel check is NOT introduced (mappings are Doing-sourced; the fold is Doing-only by construction). This keeps the editor permissive like its `BlockedStates` sibling and avoids coupling to mapping validation.

---

## Alternatives Considered

**Option (b) (chosen): sibling `WaitStatesEditor`, no relocation.** See Decision §2. Satisfies all DISCUSS hard requirements with minimal blast radius; leaves the shipped `StateMappingsEditor` untouched.

**Option (a): "Advanced State config" wrapper that relocates `StateMappingsEditor`.**

- Pros: the most literal reading of D12's "grouping that houses State Mappings + Wait States together"; one explicit container component.
- Cons: relocating a shipped component with 6 props + the `reconcileDoingStates` coupling across two call sites is pure structural churn with zero behavioural benefit and a real regression risk on the mappings editor this feature is told NOT to touch. The visual grouping D12 wants is achievable by layout without the relocation. **Rejected** for blast radius; reserved as a future deliberate settings-IA refactor.

**Option (c): put Wait States inside `FlowMetricsConfigurationComponent` next to Blocked States (the original pre-revision framing).**

- Pros: structural twin of `BlockedStates`.
- Cons: explicitly SUPERSEDED by the 2026-06-05 DISCUSS revision (D12) — Blocked is evolving independently and Wait States must be decoupled from it and live in the state-config cluster. **Rejected** (re-litigation of a locked decision).

**Option (d): store `WaitStates` pre-expanded (raw states only, no mapping names).**

- Pros: no resolver call at read time.
- Cons: violates D11 — a mapped state must be markable in ONE click without enumerating its raw states, and must track the mapping if its raw set changes. Pre-expanding would freeze the raw set at config time and break "mark the mapping, both raw states count." **Rejected.**

---

## Consequences

**Positive**:

- `StateMappingsEditor` is untouched — zero regression risk on the shipped mappings editor and its `reconcileDoingStates` coupling.
- Mapping-aware resolution reuses the EXISTING `GetRawStatesForCategory` on both the BE fold and (via a pure TS twin, ADR-054) the FE — one resolution rule, no second implementation of the expansion semantics.
- `WaitStates` mirrors `BlockedStates` persistence exactly — lowest-surprise EF migration, same provider coverage, same value-conversion mechanism.
- All DISCUSS D11/D12 hard requirements satisfied (state-cluster placement, decoupled-from-Blocked, mapping-aware selector, one-click mapping).

**Negative**:

- The "Advanced State config" grouping is layout-level, not a wrapper component. If product later wants a true container, that is a separate IA refactor (documented as out-of-scope here).

**Neutral**:

- The editor is permissive (free-typed out-of-Doing entries accepted but inert), matching the `BlockedStates` sibling rather than the stricter `StateMappingsEditor`.

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| `WaitStates` is resolved via the EXISTING `GetRawStatesForCategory` (no second expansion impl on the BE) | NUnit: mapping-name fixture "Waiting" → ["Waiting for Review", "Blocked - External"] expands; ArchUnitNET/grep: no new BE resolver |
| `StateMappingsEditor` is NOT modified by this feature | Git-diff review gate; the feature's component-decomposition table marks it REUSE-AS-IS (no edit) |
| `WaitStatesEditor` suggestions = raw Doing-states ∪ mapping names | Vitest: focus the input ⇒ suggestions include both; mapping name selectable as a single wait state |
| `WaitStates` persists like `BlockedStates` (same mechanism); read-your-writes on reload | NUnit EF InMemory + integration read-your-writes test; migration via `CreateMigration` (DELIVER) |
| An out-of-Doing wait entry contributes nothing to the denominator | NUnit: "Closed" in `WaitStates` ⇒ no effect on efficiency |
| Wait States is decoupled from Blocked States (no edit to `FlowMetricsConfigurationComponent`'s Blocked sub-section) | Review gate; component-decomposition marks `FlowMetricsConfigurationComponent` NO-CHANGE |

---

## Cross-feature impact

- Blocked-time epic (#5074): UNCHANGED — `BlockedStates` and its editor are not touched; `WaitStates` is an independent overlay (D9).
- `state-time-cumulative-view`: consumes `GetRawStatesForCategory(WaitStates)` for the highlight (ADR-057); no change to the cumulative computation.
- Settings concurrency: `WaitStates` rides the existing tokened config aggregate (`IConcurrencyTokenEntity` on `WorkTrackingSystemOptionsOwner`) — no-silent-lost-update inherited from epic-5121 for free.
