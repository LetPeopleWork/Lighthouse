# Shared Artifacts Registry — remove-action-buttons (ADO #5077)

Every `${variable}` displayed across the journey has a single documented source. This
is a brownfield change: most artifacts already exist; one is NEW (`saveState`).

```yaml
shared_artifacts:
  formValid:
    source_of_truth: "Lighthouse.Frontend/src/hooks/useModifySettings.ts (validateForm → formValid)"
    consumers: ["auto-save trigger (NEW gate)", "save-state indicator", "ValidationActions.inputsValid (today)"]
    owner: "useModifySettings"
    integration_risk: "HIGH — the auto-save gate; if it diverges from today's validity check, half-typed states could persist"
    validation: "Auto-save fires only when formValid === true; covered by Slice 1 AC (invalid-edit-held scenario)"

  saveState:
    source_of_truth: "NEW save-state machine in useModifySettings.ts (idle | saving | saved | failed)"
    consumers: ["settings save indicator (replaces ValidationActions pending/success/failed icons)", "all four settings surfaces (Slices 1-4)"]
    owner: "useModifySettings"
    integration_risk: "HIGH — this is the reassurance that replaces the Save click; must be identical across all settings surfaces"
    validation: "Slices 2-4 consume the same machine; integration_validation.saveState in journey YAML"

  validationError / validationTechnicalDetails:
    source_of_truth: "useModifySettings.handleSave (setValidationError / setValidationTechnicalDetails)"
    consumers: ["inline error (PRIMARY feedback channel once the Save button is gone)"]
    owner: "useModifySettings"
    integration_risk: "HIGH — without a Save button this becomes the only failure affordance; must always be visible"
    validation: "Slice 1 AC (invalid-edit + failed-save scenarios)"

  disableSave:
    source_of_truth: "useRbac() via IRbacAdministrationService (ModifyTeamSettings disableSave prop, line 253)"
    consumers: ["auto-save suppression (Slice 1)", "ValidationActions today"]
    owner: "RBAC layer"
    integration_risk: "HIGH — auto-save must be suppressed exactly where Save is disabled; no new authz surface"
    validation: "Slice 1 AC (viewer RBAC-parity scenario)"

  canUpdatePortfolioData:
    source_of_truth: "useRbac() (ModifyProjectSettings disableSave={!canUpdatePortfolioData}, line 276)"
    consumers: ["portfolio auto-save suppression (Slice 4)"]
    owner: "RBAC layer"
    integration_risk: "HIGH — portfolio-scope RBAC parity"
    validation: "Slice 4 AC (RBAC-parity scenario)"

  isTeamAdmin(teamId):
    source_of_truth: "useRbac() (ForecastFilterEditor readOnly = !isTeamAdmin(teamId), line 70)"
    consumers: ["forecast-filter editor read-only + auto-save suppression (Slice 3)"]
    owner: "RBAC layer"
    integration_risk: "HIGH — filter-editor RBAC parity (err-non-team-admin-attempts-edit)"
    validation: "Slice 3 AC (non-team-admin read-only scenario)"

  licenseStatus.isPremium:
    source_of_truth: "useLicense() hook (ForecastSettingsComponent isPremium gate, line 91)"
    consumers: ["forecast-filter rendering + auto-save (Slice 3)"]
    owner: "license layer"
    integration_risk: "MEDIUM — premium gating reused unchanged; downgrade preserves persisted rule set (err-license-downgrade)"
    validation: "Reuse existing license gating; no new gate"

  hasInteractedRef:
    source_of_truth: "TeamForecastView.tsx (line 70, shipped reference pattern)"
    consumers: ["new-item auto-run (Slice 5)", "backtest auto-run (Slice 6)"]
    owner: "TeamForecastView orchestration"
    integration_risk: "MEDIUM — on-mount run suppression; reused, not reinvented"
    validation: "Slices 5/6 AC (no-run-on-load scenarios)"

  requestSeqRef + DEBOUNCE_MS:
    source_of_truth: "TeamForecastView.tsx (lines 71-72, 28; DEBOUNCE_MS = 300)"
    consumers: ["new-item auto-run (Slice 5)", "backtest auto-run (Slice 6)"]
    owner: "TeamForecastView orchestration"
    integration_risk: "MEDIUM — stale-response guard + debounce; reused, not reinvented"
    validation: "Slices 5/6 AC (stale-runs-never-overwrite scenarios)"

  reloadCost (D-RELOAD decision):
    source_of_truth: "per-surface design decision (this DISCUSS wave): mappings = cheap → auto-refresh; forecast filter throughput = expensive → one-click 'Reload throughput now'"
    consumers: ["State Mappings dependent data (Slice 2)", "Forecast Filter throughput + forecasts (Slice 3)"]
    owner: "DESIGN wave (per-surface confirmation)"
    integration_risk: "HIGH — D-RELOAD invariant: NEVER instruct the user to navigate away and refresh manually"
    validation: "Slices 2/3 AC (auto-refresh / one-click Reload now scenarios)"
```

## Retired artifacts (deletions, verifiable by grep + test)

| Artifact removed | Source location | Removed by |
|---|---|---|
| `forecast-filter-takeeffect-hint` Alert | `ForecastSettingsComponent.tsx` lines 100-109 (commit 53e6287e) | Slice 3 |
| "After saving, a data reload is needed" Alert | `StateMappingsEditor.tsx` lines 109-111 | Slice 2 |
| Save button (`ValidationActions` in settings) | `ValidationActions.tsx` (settings usage) | Slices 1, 4 |
| "Forecast" `ActionButton` | `NewItemForecaster.tsx` (~line 197) | Slice 5 |
| "Run Backtest" `ActionButton` | `BacktestForecaster.tsx` (~line 361) | Slice 6 |

## Consistency checks

- All four settings surfaces (Slices 1-4) display the SAME `saveState` from ONE source
  (`useModifySettings`). No surface invents its own save indicator.
- Both forecast surfaces (Slices 5-6) reuse the SAME `hasInteractedRef` / `requestSeqRef`
  / `DEBOUNCE_MS` orchestration from `TeamForecastView`. No divergent debounce.
- RBAC gates are read from `useRbac()` only; no component fetches `/authorization/my-summary`
  directly (per Architecture). Auto-save inherits the exact gate of today's Save button.
