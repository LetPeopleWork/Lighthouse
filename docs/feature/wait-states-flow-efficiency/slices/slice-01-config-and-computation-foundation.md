# Slice 01 — Config + flow-efficiency computation foundation

**Story**: US-01 · **Job**: `job-config-admin-define-wait-states` · **Persona**: config-admin (Carlos Mendez)

## Goal (one line)

Mapping-aware `WaitStates` field editable in the state-config cluster (next to State Mappings), and the
backend computes flow efficiency = active-Doing-time / total-Doing-time over the in-scope item set, with
`waitTime` taken over `GetRawStatesForCategory(WaitStates)`.

## Learning hypothesis

Teams with idle queues WILL define wait states when the control sits with their other state config and lets
them mark a whole State Mapping in one click, producing a non-100% efficiency value that matches their reality.

## In scope

- New `WaitStates` (`List<string>`, default `[]`) on `WorkTrackingSystemOptionsOwner` (alongside `BlockedStates`
  line 41 and `StateMappings` line 45) — **mapping-aware**: entries are raw Doing-states OR `StateMapping.Name`,
  resolved via the existing `GetRawStatesForCategory` (D11). EF migration via `CreateMigration` script (mind
  `--no-incremental` stale-DLL trap).
- "Wait States" config in the **state-config cluster** next to `StateMappingsEditor` (D12), decoupled from the
  Blocked States sub-section (which is evolving independently — out of scope to touch). Gated by a "Configure
  Wait States" toggle; `ItemListManager` whose suggestions include BOTH raw Doing-states AND `StateMappings`
  names; `handleAddWaitState`/`handleRemoveWaitState` writing `onSettingsChange("waitStates", …)`. Container
  shape (wrapper-vs-sibling) is a DESIGN choice; relocating the existing `StateMappingsEditor` is a brownfield
  refactor DESIGN must size.
- Helper text: "Wait states are Doing-states (or State Mappings) where work sits idle (waiting, queued) rather
  than being actively worked. Flow efficiency = active time / total Doing time."
- Backend efficiency computation: active/total Doing-time over the D12-included set, full-duration (D5),
  Doing-only (D19); `waitTime` = time spent in any raw state in `GetRawStatesForCategory(WaitStates)` (D2/D11).
  Inspectable/testable output (no UI surface yet beyond config).

## Out of scope (later slices)

Chart number (02), overview tile (03), wait-bar highlight (04).

## Done when

- US-01 ACs pass; settings persist + read-your-writes; efficiency math verified against a fixture
  (540d total / 356d wait → 34%); a wait state defined as a MAPPING NAME counts all its underlying raw states'
  time as wait (fixture "Waiting" → ["Waiting for Review", "Blocked - External"]); a wait entry outside the
  Doing set contributes nothing; no regression to throughput/forecast/cycle-time/aging (D9). `dotnet
  build`/`pnpm build` clean; ≥80% mutation on new code.

## Dependencies

None intra-feature (this is the foundation). Concurrency inherited from epic-5121 tokened config aggregate.
