# ADR-029: Auto-save-on-valid Mechanism Placement and Save-state Machine

**Status**: Accepted
**Date**: 2026-05-29
**Feature**: remove-action-buttons (ADO #5077)
**Decider**: Morgan (Solution Architect), interaction mode = PROPOSE

---

## Context

`remove-action-buttons` retires the explicit Save button on four settings surfaces (general team
settings, State Mappings, Forecast Filter, portfolio settings). All four already share a single
state-management hook: `Lighthouse.Frontend/src/hooks/useModifySettings.ts`. The hook today exposes
`settings`, `formValid` (recomputed by an effect from `validateForm`), `validationError` /
`validationTechnicalDetails`, and a manual `handleSave` that the consuming components wire to a
`<ValidationActions>` Save button (`ModifyTeamSettings.tsx:250`, `ModifyProjectSettings.tsx:273`).

The change must persist a fully-valid form the moment it is valid, without a button, while keeping
the user MORE certain their change took. The dominating quality attribute is **correctness of the
save-state machine** (no half-typed persistence, no lost edits, no stale write overwriting a fresh
one), then **consistency** (one identical mechanism across all four surfaces — journey
`integration_validation.saveState` requires it), then **testability** (debounce + stale-guard must
be unit-testable in isolation), then **no-regression** (RBAC parity — D-RBAC-PARITY).

D-LINCHPIN locks that Slice 1 introduces the mechanism here and ships it on the simplest surface
before Slices 2-4 consume it.

A critical grounding fact: the RBAC parity signal (`disableSave`) is **not** known inside
`useModifySettings` today. It is computed in the parent page (`EditTeam.tsx:35` `canSave`,
`TeamDetail.tsx:543` `!canUpdateTeamData`, `ModifyProjectSettings.tsx:276` `!canUpdatePortfolioData`)
from `useRbac()` / `useRbacGate` and passed as a prop to `ModifyTeamSettings`/`ModifyProjectSettings`,
which forward it to `<ValidationActions>`. Auto-save must therefore receive that same prop — the hook
must be *told* whether saving is permitted; it must never re-derive authorization.

---

## Decision

**Extend `useModifySettings` with an opt-in `autoSave` capability and an explicit save-state machine,
both living in the hook. RBAC permission is injected as an input (`canSave: boolean`), never derived
inside the hook.**

Concretely:

- `useModifySettings` accepts a new option object field `autoSave?: { enabled: boolean; canSave: boolean; debounceMs?: number }`.
  When `autoSave.enabled` is false (default), behaviour is unchanged — existing manual `handleSave`
  callers are untouched.
- The hook owns a **save-state machine** with states: `idle | savingDebounced | saving | saved | error`.
  Transitions (the contract crafter implements; internal structure is theirs):
  - `idle → savingDebounced` when `formValid && canSave && settings changed by the user` (a dirty flag,
    not initial load — analogous to `hasInteractedRef` in `TeamForecastView`).
  - `savingDebounced → saving` after `debounceMs` (default 300, matching the shipped `DEBOUNCE_MS`)
    elapses with no further change.
  - `saving → saved` on a successful `saveSettings` whose sequence is the latest (stale-response guard,
    a monotonic `requestSeqRef` analogous to `TeamForecastView.requestSeqRef`).
  - `saving → error` on a failed `saveSettings`; the in-flight edit is retained in `settings`; a
    `retry()` re-enters `saving` for the same payload.
  - any state `→ idle` (no save) when `!formValid`; `validationError`/inline error becomes the primary
    (and only) failure affordance.
  - any state suppressed entirely when `!canSave` (RBAC parity) — no transition out of `idle`,
    no indicator rendered.
- The hook exposes `saveState: SaveState`, and (for the indicator) a derived label. It also continues to
  expose `handleSave` so non-auto-save callers and the explicit "Reload now" wiring (ADR-030) are
  unaffected.
- A **new small presentational component `SaveStateIndicator`** renders the machine's state
  ("Saving…" / "All changes saved" / "Couldn't save — Retry"). It replaces the `<ValidationActions>`
  Save button + pending/success/failed icons on the four settings surfaces. It is created new (not an
  extension of `ValidationActions`) because `ValidationActions` is an *action* affordance (buttons +
  validate handshake) whereas the new need is a passive *status* affordance; conflating them would
  force `ValidationActions` to carry a second, contradictory responsibility. `ValidationActions`
  itself is left intact for its other callers (e.g. connection settings, edit-team wizard).

### Enforcement (principle 11 / Earned Trust principle 12)

- **Subtype/structural check**: a Vitest unit suite on `useModifySettings` exercises the full state
  machine including the three substrate "lies" this mechanism must survive: (a) a save that rejects
  (network/500) must land in `error` with the edit retained; (b) two rapid edits must result in
  exactly one persisted write of the *latest* valid state (stale-guard); (c) `canSave=false` must
  produce zero `saveSettings` calls regardless of validity. These are the fault-injection scenarios —
  not optional.
- **Consistency probe**: a single shared `SaveStateIndicator` + single hook means the
  `integration_validation.saveState` "must_match_across" all four surfaces is structurally guaranteed,
  not convention. An ArchUnit-style frontend guard (Biome lint rule or a Vitest test grepping for any
  bespoke save indicator in the four settings components) catches a surface that invents its own.

---

## Alternatives Considered

### Option A: Extend `useModifySettings` with opt-in `autoSave` (selected)

**Accepted because**:
- Single source of truth for `settings` + `formValid` + `validationError` already lives here; the
  save-state machine belongs next to the validity it gates on. No prop-drilling of validity to a
  wrapper.
- All four surfaces already consume this exact hook, so consistency (the top-after-correctness driver)
  is achieved by construction.
- Opt-in flag keeps the ~6 other `useModifySettings`-adjacent / `ValidationActions` callers untouched
  (connection settings, new-team wizard) — bounded blast radius.

### Option B: New `useAutoSaveOnValid` wrapper hook composing `useModifySettings` (rejected)

A new hook wraps the existing one, observing `formValid` and calling `handleSave`.

**Rejected because**:
- The save-state machine needs to read and reset `validationError` and coordinate with the same
  `requestSeqRef` discipline the save path uses; splitting it across two hooks creates two owners of
  one invariant (the "no stale write" guard), which is exactly the correctness risk we are trying to
  eliminate. The shared-artifacts registry already flags `formValid`→auto-save as HIGH integration
  risk.
- A wrapper still cannot see `canSave` unless it is passed through — no advantage over Option A on the
  RBAC-parity axis, and one more layer to test.

### Option C: Per-component effects in each settings page (rejected)

Each of the four components grows its own `useEffect` debounce + save-state.

**Rejected because**:
- Four copies of the stale-guard / debounce / RBAC-suppression logic guarantees drift — the precise
  failure the journey's `integration_validation` block forbids. Violates DRY-of-knowledge and the
  consistency driver. Highest test cost (four times the fault-injection matrix).

---

## Consequences

**Positive**:
- One mechanism, one state machine, one indicator → consistency and a single fault-injection test
  matrix.
- Manual-save callers and `ValidationActions` are untouched (opt-in).
- RBAC parity is explicit and testable: `canSave` is an input; the viewer-parity scenario is a pure
  unit test.

**Negative**:
- `useModifySettings` grows in responsibility. Mitigated by keeping the machine internally cohesive and
  the indicator as a separate component; if the hook later strains, the machine can be extracted as a
  pure reducer without changing the public surface.
- A `dirty`/`hasInteracted` flag must be introduced so initial load does not trigger a save — a known,
  small risk already solved by the shipped `hasInteractedRef` pattern (referenced, not reinvented).

**Quality attribute impact**:
- Correctness: improved — single owner of the stale-guard + validity-gate.
- Consistency: improved — structurally identical across four surfaces.
- Testability: improved — debounce/stale/RBAC are unit-testable on the hook with injected fakes.
- No-regression (RBAC): preserved — `canSave` injected, never re-derived; conforms to ADR-001's
  `useRbac()`-only gating invariant.
