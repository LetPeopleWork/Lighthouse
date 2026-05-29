# Feature Delta — remove-action-buttons (ADO #5077)

DISCUSS wave (Luna). Density: lean (Tier-1 [REF] only). Brownfield UX-coherence refinement
of flows that already work but force a ceremonial Save / Run click. Proof-of-pattern already
shipped (How Many / When auto-forecast in `TeamForecastView`).

---

## Wave: DISCUSS / [REF] Persona

**delivery-forecaster** (aliases: delivery-lead, RTE) wearing the team-admin / portfolio-admin
hat. Concrete actor: *Priya Raman, RTE for the Atlas train, who also admins the Atlas team's
settings*. Viewer foil: *Sam Lee, read-only*. No new persona introduced (the existing
`related_personas: [team-admin, portfolio-admin]` already model the admin hats).

---

## Wave: DISCUSS / [REF] JTBD one-liners

- **Job A — `job-commit-intent-no-button`** (importance 3 / satisfaction 2 / gap 1): commit a
  valid settings change the moment it is valid — persist + refresh — without hunting for a Save
  button, and trust the screen reflects the change.
- **Job B — `job-see-forecast-update-live`** (importance 3 / satisfaction 2 / gap 1): have a
  new-item / backtest forecast recompute itself as inputs become valid, the way the How Many /
  When forecast already does.

Both added to `docs/product/jobs.yaml` with full dimensions/forces/opportunity score. Honest
framing: low ODI gaps — this is polish + debt-retirement, not a capability gap.

---

## Wave: DISCUSS / [REF] Locked decisions

- **D-RELOAD (user-locked, threads through Slices 2 & 3):** when a settings change requires a
  data reload, the in-place alert MUST carry a one-click "Reload now" action — NEVER instruct
  the user to navigate elsewhere and refresh manually. Auto-refresh where cheap (State Mappings);
  one-click "Reload throughput now" where expensive (Forecast Filter throughput recompute). Both
  stopgap Alerts (StateMappings "must reload"; `forecast-filter-takeeffect-hint`, commit 53e6287e)
  are replaced by silent auto-save + (auto-refresh OR one-click Reload now).
- **D-RBAC-PARITY:** auto-save / auto-run is suppressed exactly where the Save / Run button is
  gated today (`disableSave` / `canUpdatePortfolioData` / `isTeamAdmin(teamId)`) via `useRbac()`
  through `IRbacAdministrationService`. No new authorization surface.
- **D-LINCHPIN:** Slice 1 introduces the reusable auto-save mechanism in `useModifySettings` AND
  ships it on the simplest real surface (general team settings). Reviewed before Slices 2-4.
- **D-REUSE-SHIPPED-PATTERN:** Slices 5-6 reuse `TeamForecastView`'s `hasInteractedRef` +
  `requestSeqRef` + `DEBOUNCE_MS` (300ms). No new abstraction.
- **Slice split (user-confirmed, locked — do NOT re-slice):** 6 slices. Settings track 1 → 3 →
  2 → 4; forecast track 5 → 6 runs in parallel, gated only on the shipped pattern (NOT on Slice 1).

---

## Wave: DISCUSS / [REF] User stories with elevator pitches + embedded AC

Full stories in `docs/feature/remove-action-buttons/discuss/user-stories.md`. Every story traces
to a `job_id`; every story has an Elevator Pitch with a real UI entry point and observable output;
AC embedded per story. All six are user-visible (no `@infrastructure`-only slice).

| Story | Slice | job_id | Elevator Pitch "After" |
|---|---|---|---|
| US-5077-01 | 1 (linchpin) | job-commit-intent-no-button | Edit a field on `/teams/42/settings` → persists with "All changes saved", survives refresh, no Save button |
| US-5077-03 | 3 (premium) | job-commit-intent-no-button | Add "Exclude Type = Bug" → auto-saves with "Saved" + in-place one-click "Reload throughput now"; stopgap Alert gone |
| US-5077-02 | 2 | job-commit-intent-no-button | Group "Doing"+"Review" → auto-saves and metrics refresh automatically; "must reload" Alert gone |
| US-5077-04 | 4 | job-commit-intent-no-button | Edit a valid portfolio field → auto-saves with "Saved", no button; suppressed where `canUpdatePortfolioData` false |
| US-5077-05 | 5 | job-see-forecast-update-live | Adjust new-item inputs → forecast recomputes live, no "Forecast" button, no run on load, stale discarded |
| US-5077-06 | 6 | job-see-forecast-update-live | Adjust backtest inputs → backtest recomputes live, no "Run Backtest" button, mode-toggle mid-edit fires no run |

---

## Wave: DISCUSS / [REF] Definition of Done (feature-level)

- All six slices' UAT scenarios pass green (DISTILL → DELIVER).
- 0 of 6 settings/forecast surfaces require an explicit Save/Run click for valid input.
- Both stopgap Alerts removed (grep + test confirm absence).
- RBAC parity verified: no auto-save/auto-run fires where the button was disabled today.
- No API contract / DTO change; Lighthouse-Clients untouched.
- Each slice demoable on a local instance per its dogfood moment.

---

## Wave: DISCUSS / [REF] Out of scope

- The How Many / When manual forecast (already shipped — it is the proof-pattern, not work).
- Any API-contract / DTO change (confirmed none needed — frontend interaction change only).
- Portfolio-level forecast surfaces beyond settings (no new-item/backtest at portfolio scope).
- Cross-instance adoption telemetry (KPIs 5-6) — BLOCKED on Epic 5015.

---

## Wave: DISCUSS / [REF] Walking-skeleton strategy

**N/A — brownfield.** All four surface groups already work end-to-end with explicit buttons.
Replaced by **Slice 1 as the mechanism-introducing linchpin** (ships the reusable auto-save on
the simplest surface; Slices 2-4 consume it). Forecast track reuses the shipped auto-run.

---

## Wave: DISCUSS / [REF] Driving ports (UI actions / endpoints)

| Surface | Driving UI action | Endpoint (unchanged) | Real file |
|---|---|---|---|
| General team settings | Edit field on `/teams/{id}/settings` | `PUT /api/teams/{id}` | `useModifySettings.handleSave`, `ModifyTeamSettings.tsx` |
| State Mappings | Edit mapping (same form) | `PUT /api/teams/{id}` | `StateMappingsEditor.tsx` |
| Forecast Filter | Edit rule (same form, premium) | `PUT /api/teams/{id}` | `ForecastFilterEditor.tsx`, `ForecastSettingsComponent.tsx` |
| Portfolio settings | Edit field on portfolio settings | portfolio PUT | `ModifyProjectSettings.tsx` |
| New-item forecast | Adjust inputs | `runItemPrediction` POST | `NewItemForecaster.tsx`, `TeamForecastView.tsx` |
| Backtest | Adjust inputs | `runBacktest` POST | `BacktestForecaster.tsx`, `TeamForecastView.tsx` |

---

## Wave: DISCUSS / [REF] Pre-requisites

- Slice 1 mechanism reviewed and shipped before Slices 2-4.
- Premium license seeding for Slice 3 dogfood (see `reference_premium_license_dev_seed`).
- Forecast track (5-6) only needs the already-shipped `TeamForecastView` orchestration.

---

## Wave: DISCUSS / [REF] Outcome KPIs

Full table in `docs/feature/remove-action-buttons/discuss/outcome-kpis.md`. Committed (repo/test/
walkthrough verifiable) because customer instances do not phone home:

- **KPI 1 (North Star, committed):** surfaces still requiring an explicit Save/Run click for valid
  input → **0 of 6** (baseline 6/6). Measured by per-surface test + grep.
- **KPI 2 (committed):** stopgap "you must refresh" Alerts → **0** (baseline 2). Measured by grep + test.
- **KPI 3 (committed):** surfaces exhibiting valid-input-acts-immediately → **6 of 6** (baseline 1/6).
- **KPI 4 (committed, usability):** ≥90% of moderated-walkthrough participants notice an invalid
  field within 5s with no Save button. Measured by walkthrough on Slice 1.
- **KPI 5-6 (aspirational, BLOCKED on Epic 5015):** lost-edit incidents −50%; scenario recomputes/session +30%.
- **Guardrails (must NOT degrade):** no half-typed/invalid state persisted; RBAC parity; no run on
  mount; stale runs never overwrite fresh; no API/DTO change.

---

## Wave: DISCUSS / [REF] DoR validation (9-item hard gate)

Scope: feature-level, applied across all six stories. Each story individually satisfies items 1-9
(see `user-stories.md`).

| DoR Item | Status | Evidence |
|---|---|---|
| 1. Problem statement clear, domain language | PASS | Each story opens with Priya Raman's pain in domain terms (save ceremony, two-step refresh, tweak-click-read). No "implement X". |
| 2. User/persona with specific characteristics | PASS | delivery-forecaster + team/portfolio-admin hat; concrete Priya Raman (RTE, Atlas) + Sam Lee (viewer). |
| 3. 3+ domain examples with real data | PASS | Each story has 3 examples (happy/edge/error) with real names + data (Atlas team, throughput 90→60, "Exclude Type = Bug", Doing+Review→In Progress). |
| 4. UAT in Given/When/Then (3-7 scenarios) | PASS | US-01: 4, US-02: 3, US-03: 4, US-04: 3, US-05: 3, US-06: 3 scenarios. All business-outcome titles, no implementation. |
| 5. AC derived from UAT | PASS | Each story's AC checklist maps 1:1 to its scenarios. |
| 6. Right-sized (1-3 days, 3-7 scenarios) | PASS | All six ~1 day, 3-4 scenarios each. Oversized feature was split (carpaccio gate). |
| 7. Technical notes: constraints/dependencies + cross-cutting | PASS | Each story states RBAC / Clients / Website (see checklist below); dependencies noted (Slice 1 linchpin, shipped forecast pattern). |
| 8. Dependencies resolved or tracked | PASS | Settings track 1→3→2→4 dependencies explicit; forecast track 5→6 independent. Linchpin flagged. |
| 9. Outcome KPIs defined with measurable targets | PASS | 4 committed numeric KPIs + 2 aspirational (gated on Epic 5015) with baselines + measurement methods. |

### DoR Status: PASSED (9/9)

---

## Wave: DISCUSS / [REF] Cross-cutting checklist (DoR Item 7 hard gate)

- **RBAC — N/A new surface, parity required.** Auto-save / auto-run must be gated exactly where the
  Save / Run button is gated today: team-admin via `useRbac().isTeamAdmin(teamId)`; portfolio via
  `canUpdatePortfolioData` (`ModifyProjectSettings.tsx:276`, `disableSave={!canUpdatePortfolioData}`);
  forecast filter read-only via `!isTeamAdmin(teamId)` (`ForecastFilterEditor.tsx:70`); team settings
  via `disableSave` (`ModifyTeamSettings.tsx:253`). All flow through `IRbacAdministrationService` with
  UI gating from `useRbac()` per Architecture. **No new authorization surface** — auto-save inherits
  the exact gate of today's button. Forecast running (Slices 5-6) has no write-permission gate today,
  so parity = no new gate.
- **Lighthouse-Clients (CLI + MCP) — N/A, unaffected.** Every slice is a frontend interaction change
  (WHEN a PUT/POST fires) reusing existing endpoints (`PUT /api/teams/{id}`, portfolio PUT,
  `runItemPrediction` / `runBacktest` POSTs). No DTO / endpoint shape changes, so the clients follow
  nothing and **no version-gate is needed** (version-gating only applies to NEW endpoints).
- **Website — N/A.** Pure UX polish on already-marketed flows (team/portfolio settings, premium
  forecast filter, forecasts). No new premium capability to surface. The premium forecast filter is
  already marketed; this slice only changes its interaction.

---

## Wave: DISCUSS / [REF] wave-decisions summary

Scope assessment (Phase 1.5): OVERSIZED for a single deliverable (decisive signal: 4+ independent
shippable outcomes) → SPLIT into 6 thin end-to-end slices. User CONFIRMED the split. Settings track
1→3→2→4; forecast track 5→6 parallel. Full record in `discuss/wave-decisions.md`.

---

## Wave: DISCUSS / [REF] Requirements completeness

- **Functional:** PASS — all six surfaces' behaviors specified with G/W/T.
- **Non-functional:** PASS — debounce (300ms), no-run-on-mount, stale-guard, save-state visibility
  (<100ms feedback per Nielsen #1), inline-error-as-primary-feedback are specified as guardrails/AC.
- **Business rules:** PASS — RBAC parity, validity gate (only persist fully-valid forms), D-RELOAD
  (never "go elsewhere and refresh"), premium gating, license-downgrade rule-set preservation.

**Requirements completeness score: 95% (committed).** The 5% gap is the two aspirational KPIs
(5-6) that cannot be instrumented until Epic 5015 — explicitly recorded, not hidden.

---

## Wave: DISCUSS / [REF] Artifacts (absolute paths)

- `docs/feature/remove-action-buttons/discuss/journey-remove-action-buttons-visual.md`
- `docs/feature/remove-action-buttons/discuss/journey-remove-action-buttons.yaml`
- `docs/feature/remove-action-buttons/discuss/shared-artifacts-registry.md`
- `docs/feature/remove-action-buttons/discuss/story-map.md`
- `docs/feature/remove-action-buttons/discuss/outcome-kpis.md`
- `docs/feature/remove-action-buttons/discuss/user-stories.md`
- `docs/feature/remove-action-buttons/slices/slice-01..06-*.md`
- SSOT: `docs/product/jobs.yaml` (+Job A, +Job B), `docs/product/journeys/remove-action-buttons.yaml`,
  `docs/product/journeys/filter-forecast-throughput.yaml` (back-propagated step),
  `docs/product/personas/delivery-forecaster.yaml` (+2 primary_jobs).

---
---

# DESIGN wave (Morgan). Density: lean (Tier-1 [REF] only). Frontend-only; no API/DTO change.

Grounded in real code (paths + lines cited). No backend touch. The mechanism extends the
already-shared `useModifySettings` hook; the forecast track reuses the shipped `TeamForecastView`
auto-run. All decisions conform to ADR-001 (`useRbac()`-only UI gating) and introduce no new
authorization surface.

## Wave: DESIGN / [REF] Quality attributes (ranked)

1. **Correctness of the save-state machine** (dominant) — no half-typed/invalid persistence, no lost
   edits on failure, no stale write overwriting a fresh one. This is the guardrail set the whole
   feature is judged on.
2. **Consistency** — one identical save mechanism + one indicator across all four settings surfaces
   (journey `integration_validation.saveState`); one debounce/stale-guard across both forecast
   surfaces (`integration_validation` auto-run).
3. **Testability** — debounce, stale-guard, validity-gate, and RBAC suppression must be unit-testable
   on the hook in isolation with injected fakes.
4. **No-regression (RBAC parity)** — auto-save/auto-run suppressed exactly where the Save/Run button
   is gated today; D-RBAC-PARITY.
5. **Time-to-market** — high (6 thin slices) but explicitly subordinate to #1; the linchpin (Slice 1)
   is reviewed before consumers build on it.

## Wave: DESIGN / [REF] Constraints

- TreatWarningsAsErrors / TS strict mode; Biome zero-warning; no `any` (narrow `unknown`).
- Ports-and-adapters preserved; `useRbac()`-only gating (ADR-001) — no component fetches
  `/authorization/my-summary` directly.
- No new endpoint / DTO; reuse `PUT /api/teams/{id}`, portfolio PUT, `runItemPrediction`,
  `runBacktest`.
- Forms use **plain MUI controlled state**, not react-hook-form/zod. Validity is computed by
  `validateForm` inside `useModifySettings` and surfaced as `formValid`
  (`useModifySettings.ts:102-117`). Errors surface via `validationError` /
  `validationTechnicalDetails` rendered in an MUI `Alert` (`ModifyTeamSettings.tsx:230-244`). The
  "show the error without a Save button" design rides this existing channel — it becomes the PRIMARY
  failure affordance once the button is gone.

## Wave: DESIGN / [REF] Conway / team

N/A — single codebase, single team. No org-boundary impact on this frontend-only change.

## Wave: DESIGN / [REF] Paradigm

Locked: OOP project, React 18 + TS functional-leaning hooks. No CLAUDE.md change. The auto-save
machine is a hook-internal reducer-style state machine (composition, not inheritance) — consistent
with the existing hook style.

## Wave: DESIGN / [REF] Decisions table

| ID | Decision | Rationale | ADR |
|---|---|---|---|
| D-SA-PLACEMENT | Auto-save lives as an opt-in capability **inside `useModifySettings`** (not a wrapper hook, not per-component effects). | The hook is already the single source of `settings`/`formValid`/`validationError` shared by all four surfaces; the stale-guard invariant needs one owner. | ADR-029 |
| D-SA-MACHINE | Save-state machine `idle → savingDebounced → saving → saved \| error`; debounce 300ms (matches shipped `DEBOUNCE_MS`); monotonic `requestSeqRef` stale-guard; `dirty`/`hasInteracted` flag so initial load fires no save. | Mirrors the proven `TeamForecastView` orchestration applied to *save*. | ADR-029 |
| D-SA-RBAC-INPUT | RBAC permission is **injected** into the hook as `canSave: boolean`; the hook never re-derives authorization. | `disableSave` is computed in parent pages from `useRbac()`/`useRbacGate` (`EditTeam.tsx:35`, `TeamDetail.tsx:543`, `ModifyProjectSettings.tsx:276`) and passed down. Conforms to ADR-001. | ADR-029 |
| D-SA-INDICATOR | **New** small `SaveStateIndicator` presentational component (not an extension of `ValidationActions`). | `ValidationActions` is an *action* affordance; the new need is a passive *status* affordance. Conflating breaks single-responsibility and would alter the ~6 other `ValidationActions` callers. | ADR-029 |
| D-RELOAD-SPLIT | Cost-based: State Mappings (cheap) → **silent auto-refresh**; Forecast Filter throughput (expensive) → **one-click "Reload throughput now"** in-place. Both gated on the machine's `saved` state. | Honours D-RELOAD ("never navigate away") + performance (no silent repeated expensive recompute). | ADR-030 |
| D-RELOAD-ACTION | **New** small `ReloadDependentDataAction` inline component for the expensive case; cheap case uses a transient `SaveStateIndicator` "Reloading…" label. Both call each surface's **existing** refresh path. | The retired Alerts were instructional text; the replacement is an action tied to `saved`. No new endpoint/service. | ADR-030 |
| D-FC-REUSE | Forecast track (Slices 5-6) **reuses** `TeamForecastView`'s `hasInteractedRef` + `requestSeqRef` + `DEBOUNCE_MS` by lifting the per-input `useEffect`+debounce orchestration into `TeamForecastView` for the new-item and backtest runs (same shape as the already-shipped manual run, `TeamForecastView.tsx:181-196`). No new abstraction. | D-REUSE-SHIPPED-PATTERN. | — |

## Wave: DESIGN / [REF] Component decomposition

Default is **EXTEND**. Every CREATE NEW carries hard evidence.

| Component | File (real path) | Change | Summary |
|---|---|---|---|
| `useModifySettings` | `Lighthouse.Frontend/src/hooks/useModifySettings.ts` | EXTEND | Add opt-in `autoSave: { enabled; canSave; debounceMs? }`; add save-state machine + `dirty` flag + `requestSeqRef` stale-guard; expose `saveState` + `retry()`. Existing manual callers unchanged (opt-in default off). |
| `SaveStateIndicator` | `Lighthouse.Frontend/src/components/Common/ValidationActions/SaveStateIndicator.tsx` (new) | CREATE NEW | Passive status affordance: "Saving…" / "All changes saved" / "Couldn't save — Retry" / "Reloading…". Evidence: no existing passive status component; `ValidationActions` is action-shaped (buttons + validate handshake). |
| `ReloadDependentDataAction` | `Lighthouse.Frontend/src/components/Common/StateMappings/ReloadDependentDataAction.tsx` (new) | CREATE NEW | Inline one-click "Reload throughput now" tied to `saved`. Evidence: retired Alerts are text-first; reusing `Alert` re-introduces the retired shape. |
| `ValidationActions` | `Lighthouse.Frontend/src/components/Common/ValidationActions/ValidationActions.tsx` | NO CHANGE (settings usage removed) | Left intact for its other ~6 callers (connection settings, new-team wizard). Settings surfaces stop rendering it; they render `SaveStateIndicator`. |
| `ModifyTeamSettings` | `Lighthouse.Frontend/src/components/Common/Team/ModifyTeamSettings.tsx` | EXTEND | Opt into `autoSave` (pass through `disableSave`→`canSave`); replace `<ValidationActions onSave>` block (line 250-255) with `SaveStateIndicator`. Inline `validationError` Alert (230-244) retained as primary error channel. |
| `ModifyProjectSettings` | `Lighthouse.Frontend/src/components/Common/ProjectSettings/ModifyProjectSettings.tsx` | EXTEND | Same, with `canSave = canUpdatePortfolioData` (line 276). Reuses Slice-2 auto-refresh for its embedded State Mappings. |
| `StateMappingsEditor` | `Lighthouse.Frontend/src/components/Common/StateMappings/StateMappingsEditor.tsx` | EXTEND | Delete the "must reload" Alert (lines 109-111); on `saved`, auto-refresh dependent metrics; inline mapping validation errors stay. |
| `ForecastSettingsComponent` | `Lighthouse.Frontend/src/pages/Teams/Edit/ForecastSettingsComponent.tsx` | EXTEND | Delete `forecast-filter-takeeffect-hint` Alert (lines 100-109); render `ReloadDependentDataAction` ("Reload throughput now") on `saved`. Premium gate (`isPremium`, line 70) unchanged. |
| `ForecastFilterEditor` | `Lighthouse.Frontend/src/components/Teams/ForecastFilterEditor/ForecastFilterEditor.tsx` | NO CHANGE | `readOnly = !isTeamAdmin(teamId)` (line 70) already gates editing; auto-save inherits parity via `canSave`. |
| `TeamForecastView` | `Lighthouse.Frontend/src/pages/Teams/Detail/TeamForecastView.tsx` | EXTEND | Add debounced `useEffect` + `hasInteractedRef`/`requestSeqRef` orchestration for new-item (Slice 5) and backtest (Slice 6) runs, mirroring the shipped manual-run effect (181-196). Pass run triggers down; drop the child Run buttons. |
| `NewItemForecaster` | `Lighthouse.Frontend/src/pages/Teams/Detail/NewItemForecaster.tsx` | EXTEND | Remove "Forecast" `ActionButton` (197-206); lift input state up / notify `TeamForecastView` on every valid change so the parent's debounced effect runs; clear result on incomplete inputs (existing `onClearForecastResult`). |
| `BacktestForecaster` | `Lighthouse.Frontend/src/pages/Teams/Detail/BacktestForecaster.tsx` | EXTEND | Remove "Run Backtest" `ActionButton` (361-364); same lift-state-up; mode toggle mid-edit fires no half-config run (guard on full validity). |

## Wave: DESIGN / [REF] Driving ports (UI actions) — unchanged from DISCUSS

Per the DISCUSS driving-ports table; no endpoint shape changes. Auto-save changes only WHEN the
existing PUT/POST fires (on debounced validity), not WHAT it sends.

## Wave: DESIGN / [REF] Driven ports + adapters

All reused as-is (no new adapter): `teamService.updateTeam` / portfolio update (the `saveSettings`
callback already injected into `useModifySettings`), `forecastService.runItemPrediction`,
`forecastService.runBacktest`, `teamMetricsService` (dependent-data refresh). No backend port touched.

## Wave: DESIGN / [REF] Technology choices

- React 18 + TypeScript (strict). No new dependency.
- Validation: existing `validateForm` predicate inside `useModifySettings` (NOT react-hook-form/zod —
  confirmed by reading the hook). Inline error via MUI `Alert` (existing).
- State machine: hook-internal `useState`/`useRef` reducer-style (matches shipped `TeamForecastView`
  pattern); `crypto.randomUUID` already in use for keys (`StateMappingsEditor.tsx:97`).
- Debounce: `setTimeout` cleanup in `useEffect`, `DEBOUNCE_MS = 300` (shipped constant).
- Enforcement (principle 11): Vitest fault-injection suite on `useModifySettings` (reject / rapid-edit
  stale-guard / `canSave=false` → zero saves); a guard test asserting no settings surface renders a
  bespoke save indicator (consistency probe for `integration_validation.saveState`).

## Wave: DESIGN / [REF] Save-state machine (explicit)

```
            user edits a valid form (formValid && canSave && dirty)
   idle ───────────────────────────────────────────────► savingDebounced
    ▲  ▲                                                        │ debounceMs (300) elapses
    │  │ formValid===false  (no save; inline error is primary) │
    │  └──────────────────────◄──────────────────────────────┘ (further edit re-arms timer)
    │                                                            │
    │                                                            ▼
    │                            saveSettings(latest), seq=++requestSeqRef
    │                                                            │
    │                              ┌────────────success & seq current────────────┐
    │                              ▼                                              ▼
    │   error ◄──fail──── saving ──┘                                            saved
    │     │                                                                       │
    │     │ retry() (same payload)                                  cheap → auto-refresh
    │     └──────────────────────────────────────────────►saving   expensive → one-click reload
    └── canSave===false at any point ⇒ suppressed; no transition out of idle; no indicator
```

Invariants: (a) a save fires only from a fully-valid, dirty, permitted form; (b) only the latest
sequence's response is applied (stale-guard); (c) a failed save retains the edit and offers Retry;
(d) `canSave===false` ⇒ read-only, zero `saveSettings` calls, no indicator (RBAC parity).

## Wave: DESIGN / [REF] D-RELOAD per-surface split

| Surface | Cost | After `saved` | Retired stopgap |
|---|---|---|---|
| State Mappings (Slice 2) | cheap (client metrics re-fetch) | silent auto-refresh; fallback to one-click if refresh fails | "After saving, a data reload is needed" Alert (`StateMappingsEditor.tsx:109-111`) |
| Forecast Filter throughput (Slice 3) | expensive (backend recompute → all forecasts) | one-click "Reload throughput now" in-place | `forecast-filter-takeeffect-hint` Alert (`ForecastSettingsComponent.tsx:100-109`, commit 53e6287e) |

## Wave: DESIGN / [REF] Reuse Analysis (HARD GATE)

| Overlapping component | Verdict | Evidence |
|---|---|---|
| `useModifySettings` | **EXTEND** | Already the single shared hook for all four surfaces; add opt-in `autoSave` + state machine; manual callers untouched (default off). ADR-029. |
| Save-state indicator | **CREATE NEW** (`SaveStateIndicator`) | No existing passive status component; `ValidationActions` is action-shaped (buttons + validate handshake) — conflating breaks SRP and disturbs its ~6 other callers. ADR-029. |
| In-place "Reload now" action | **CREATE NEW** (`ReloadDependentDataAction`) for expensive case | Retired Alerts are instructional text; reusing MUI `Alert` re-introduces the retired text-first shape. Cheap case = existing `SaveStateIndicator` label only. ADR-030. |
| `TeamForecastView` debounce/stale-guard | **EXTEND/REUSE** (Slices 5-6) | Lift the shipped manual-run effect shape (`hasInteractedRef`/`requestSeqRef`/`DEBOUNCE_MS`, lines 70-72, 181-196) to drive new-item + backtest runs. D-REUSE-SHIPPED-PATTERN; no new abstraction. |
| `ModifyTeamSettings` / `ModifyProjectSettings` | **EXTEND** | Opt into `autoSave`, pass `disableSave`→`canSave`, swap `ValidationActions` block for `SaveStateIndicator`. |
| `StateMappingsEditor` | **EXTEND** | Delete reload Alert; auto-refresh on `saved`. |
| `ForecastSettingsComponent` | **EXTEND** | Delete hint Alert; render reload action; premium gate unchanged. |
| `ForecastFilterEditor` | **NO CHANGE** | `readOnly = !isTeamAdmin(teamId)` already correct; parity inherited via `canSave`. |
| `ValidationActions` | **NO CHANGE** | Retained for non-settings callers; settings surfaces stop using it. |
| `NewItemForecaster` / `BacktestForecaster` | **EXTEND** | Remove Run buttons; lift input state so parent's debounced effect runs. |

## Wave: DESIGN / [REF] Outcome collision check

**SKIPPED — documented reason.** The `nwave-ai outcomes check-delta` CLI is absent
(no `nwave-ai` binary on PATH; `docs/product/outcomes/registry.yaml` does not exist). Per D-6
gate-scoping, the outcomes registry tracks code-feature pipelines that introduce **new typed contract
surface** (endpoints/DTOs). This feature introduces **no new endpoint or DTO** (cross-cutting checklist
confirms; reuses `PUT /api/teams/{id}`, portfolio PUT, `runItemPrediction`, `runBacktest`). Pure
frontend-interaction change ⇒ no new outcome to register; skip is correct, not a gap.

## Wave: DESIGN / [REF] ADRs written

- `docs/product/architecture/adr-029-autosave-on-valid-mechanism-placement-and-save-state-machine.md`
- `docs/product/architecture/adr-030-dependent-data-reload-after-autosave-cost-based-split.md`

## Wave: DESIGN / [REF] Open questions

- **OQ-1 (RESOLVED 2026-05-29, user):** `SaveStateIndicator` persisted-state copy is **"All changes
  saved"** (Google-Docs idiom). Locked — use this string across all four settings surfaces.
- **OQ-2 (RESOLVED 2026-05-29, user):** Forecast Filter "Reload throughput now" **always requires the
  click** — a valid rule edit auto-saves silently, but the expensive throughput recompute never
  auto-fires. Locked per the "expensive ⇒ intentional" rationale (see ADR-030).
- **OQ-3 (crafter, GREEN-time):** exact `dirty` detection — per-field touched vs deep-equality vs
  reuse of a `hasInteracted` flag. Recommendation: `hasInteracted` flag set in the existing
  `updateSettings`/list handlers (mirrors `TeamForecastView`); leave structure to crafter.

## Wave: DESIGN / [REF] Back-propagation

No DESIGN decision changes a DISCUSS assumption, story, or AC. The cost-based D-RELOAD split,
RBAC-parity-by-injection, and forecast reuse are all consistent with the locked decisions
(D-RELOAD, D-RBAC-PARITY, D-LINCHPIN, D-REUSE-SHIPPED-PATTERN) and the journey's
`integration_validation`. No `## Changed Assumptions` block and no `design/upstream-changes.md`
required.

---
---

# DISTILL wave (Sentinel). Density: lean (Tier-1 [REF] only). Frontend-only; Vitest + RTL
acceptance specs at the hook / component driving port + ONE Playwright walking skeleton.

Reconciliation HARD GATE: PASSED — 0 contradictions across DISCUSS / DESIGN / DEVOPS (confirmed
upstream). C#/TS project per `docs/architecture/atdd-infrastructure-policy.md` — NOT the
Python/Hypothesis pilot, so no `.feature` files, no pytest-bdd, no `state_delta`/`__SCAFFOLD__`/
Hypothesis machinery. Tier B (state-machine PBT) is NOT emitted: these are config-shaped settings
interactions (Mandate 10 "skip Tier B for config-shaped features"). Layer per the policy: hook
`renderHook` + component RTL with faked driven-API client = in-memory acceptance (layer 2,
example-only); the WS = real-io E2E (layer 6). Specs authored RED via `describe.skip` /
`test.fixme` for one-at-a-time DELIVER un-skip (Outside-In TDD, matching the project's
green-before-push / skip-acceptance precedent).

## Wave: DISTILL / [REF] Scenario list with tags

| # | Scenario | Tags | Test file |
|---|----------|------|-----------|
| 1 | Valid team-settings edit persists with "All changes saved", no Save button (WS) | `@walking_skeleton @real-io @US-01 @kpi` | `AutoSaveTeamSettings.spec.ts` |
| 2 | Valid edit auto-saves after stop-typing | `@US-01 @in-memory @kpi` | `useModifySettings.autosave.test.ts` |
| 3 | Save-state: idle → saving → all-changes-saved | `@US-01 @in-memory` | same |
| 4 | Invalid edit held back; inline error is primary | `@US-01 @error @in-memory` | same |
| 5 | Failed save keeps edit + offers retry | `@US-01 @error @in-memory` | same |
| 6 | Viewer (no canSave) never auto-saves | `@US-01 @error @in-memory` | same |
| 7 | Rapid edits debounce; only latest persists (stale guard) | `@US-01 @error @in-memory` | same |
| 8 | No save fires on initial page load | `@US-01 @in-memory` | same |
| 9 | Mapping change auto-saves + dependent metrics auto-refresh | `@US-02 @in-memory @kpi` | same |
| 10 | Empty mapping group name fires no save | `@US-02 @error @in-memory` | same |
| 11 | Failed auto-refresh offers one-click reload | `@US-02 @error @in-memory` | same |
| 12 | Filter rule auto-saves; throughput recompute never auto-fires (OQ-2) | `@US-03 @in-memory @kpi` | same |
| 13 | Unknown filter field rejected; prior rule set intact | `@US-03 @error @in-memory` | same |
| 14 | Viewer sees filter read-only, no auto-save | `@US-03 @error @in-memory` | same |
| 15 | Valid portfolio field auto-saves | `@US-04 @in-memory` | same |
| 16 | Auto-save suppressed where canUpdatePortfolioData false | `@US-04 @error @in-memory` | same |
| 17 | Invalid portfolio form fires no save | `@US-04 @error @in-memory` | same |
| 18 | New-item forecast recomputes on valid input change | `@US-05 @in-memory @kpi` | `TeamForecastView.autorun.test.tsx` |
| 19 | No new-item run on page load | `@US-05 @error @in-memory` | same |
| 20 | Incomplete inputs → no new-item run | `@US-05 @error @in-memory` | same |
| 21 | Rapid new-item changes → only latest run (stale guard) | `@US-05 @error @in-memory` | same |
| 22 | Backtest recomputes on valid input change | `@US-06 @in-memory` | same |
| 23 | Mode toggle mid-edit fires no half-config run | `@US-06 @error @in-memory` | same |
| 24 | No backtest run on page load | `@US-06 @error @in-memory` | same |

24 scenarios. Error/edge ratio: 14 of 24 = **58%** (exceeds the 40% target). Every story US-01..06
covered; every `failure_modes` entry from the journey mapped (err-invalid-edit-held → #4/#10/#13/#17;
err-save-failed → #5; err-reload-needed → #11/#12; err-non-admin-no-autosave → #6/#14/#16;
err-concurrent-rapid-edits → #7/#21; err-no-run-on-mount → #8/#19/#24; err-incomplete-inputs →
#20/#23; err-stale-run → #7/#21).

## Wave: DISTILL / [REF] WS strategy

ONE Playwright walking skeleton (user-confirmed: one WS only) — `@walking_skeleton @real-io`,
demo-data driven (`loadDemoScenario(0)` + `waitForBackgroundUpdates`, Team Zenith), POM-based
(no inline `page.locator`). Surface: general team settings (the linchpin, Slice 1). Proves the
full real-io loop: edit Throughput History → "All changes saved" indicator → value persists after
reload → no Save button. The remaining 23 scenarios run at the hook/component driving port with a
faked + captured driven-API client (in-memory acceptance, layer 2) for fast deterministic feedback.

## Wave: DISTILL / [REF] Adapter coverage table

| Driven adapter | Treatment | @real-io scenario | Covered by |
|----------------|-----------|-------------------|------------|
| API client `saveSettings` (`PUT /api/teams/{id}` / portfolio PUT) | fake + capture (in-memory); real (WS) | YES | WS #1 (real PUT via UI) + hook specs capture `saveSettings` calls |
| API client `forecastService.runItemPrediction` (`runItemPrediction` POST) | fake + capture | via component port | #18-21 capture the faked call |
| API client `forecastService.runBacktest` (`runBacktest` POST) | fake + capture | via component port | #22-24 capture the faked call |
| Dependent-data refresh (`additionalFetch` / teamMetrics re-fetch) | fake + capture | via hook port | #9, #11 capture the faked refresh |

No NEW driven adapter introduced (all reused as-is per DESIGN "Driven ports + adapters"). No backend
adapter change — frontend-interaction-only feature. The single real-io proof is the WS; per Mandate 11
(layer 3+) and the project policy, the in-memory specs are example-based, not PBT-generated.

## Wave: DISTILL / [REF] Scaffolds

RED scaffolds created so skipped specs are RED-not-BROKEN (imports/types resolve; un-skipping fails
for MISSING_FUNCTIONALITY, verified by un-skip dry-run):

- `Lighthouse.Frontend/src/components/Common/ValidationActions/SaveStateIndicator.tsx` — new passive
  status component (ADR-029 D-SA-INDICATOR). Exports `SaveState` type + props; body throws
  "RED scaffold". DELIVER implements.
- `Lighthouse.Frontend/src/components/Common/StateMappings/ReloadDependentDataAction.tsx` — new
  one-click reload action for the expensive case (ADR-030 D-RELOAD-ACTION). Body throws "RED scaffold".
- `Lighthouse.Frontend/src/hooks/useModifySettings.ts` — typed-option scaffold: added exported
  `SaveState` + `AutoSaveOptions` types, opt-in `autoSave?` option, inert `saveState` (stays "idle")
  + no-op `retry()` return. Auto-save behaviour NOT implemented — DELIVER wires the machine. Existing
  manual callers + 29 existing hook tests stay green (opt-in default off).

No `.tsx`/`.ts` markers à la `__SCAFFOLD__` (that is the Python-pilot convention); RED-not-BROKEN is
proven by the un-skip dry-run classification instead, per the C#/TS policy.

## Wave: DISTILL / [REF] Test placement

- `Lighthouse.Frontend/src/hooks/useModifySettings.autosave.test.ts` — colocated next to the hook,
  mirrors the existing `useModifySettings.test.ts` (`renderHook` + faked callbacks). Slices 1-4
  (the shared auto-save mechanism is one hook driving port).
- `Lighthouse.Frontend/src/pages/Teams/Detail/TeamForecastView.autorun.test.tsx` — colocated next to
  the component, mirrors the existing `TeamForecastView.test.tsx` (`createMockApiServiceContext` +
  child mocks). Slices 5-6 (forecast auto-run orchestration lives in `TeamForecastView`).
- `Lighthouse.EndToEndTests/tests/specs/teams/AutoSaveTeamSettings.spec.ts` — alongside
  `ForecastFilter.spec.ts`; POM additions in `tests/models/teams/TeamEditPage.ts`.

## Wave: DISTILL / [REF] Driving Adapter coverage

| Driving port | Exercised by |
|--------------|--------------|
| `useModifySettings` hook (auto-save mechanism, Slices 1-4) | hook specs via `renderHook` + `updateSettings`/list handlers + fake timers |
| `TeamForecastView` component (auto-run orchestration, Slices 5-6) | component specs via RTL render + child input-change triggers + fake timers |
| Production React app (general team settings) | Playwright WS — real UI interaction `setThroughputHistory` → reload |

The mechanism's debounce (300ms) + stale-guard are exercised deterministically via Vitest
`vi.useFakeTimers()` + `vi.advanceTimersByTime(300)`. RBAC suppression is exercised by injecting
`autoSave.canSave = false` (parity with `disableSave` / `canUpdatePortfolioData` / `isTeamAdmin`).

## Wave: DISTILL / [REF] Pre-requisites

- DESIGN driving ports: `useModifySettings` (EXTEND, opt-in `autoSave`), `TeamForecastView` (EXTEND,
  lift child input state), the two new components (`SaveStateIndicator`, `ReloadDependentDataAction`).
- WS runs on demo scenario 0 (Team Zenith) via `loadDemoScenario` + `waitForBackgroundUpdates`
  (never live connector syncs).
- Slice 3 (premium forecast filter) hook spec runs at the hook port with no license dependency;
  the premium gate is a parent-page concern injected as `canSave` (a live premium-filter E2E, if
  added later, would seed a premium license per `reference_premium_license_dev_seed` — out of scope
  for this DISTILL's single WS).
- Claude RUNS the Playwright WS locally before DELIVER commits it (per project E2E precedent); the
  spec is authored `test.fixme` and un-run until the production behaviour exists.

## Wave: DISTILL / [REF] Outcomes registry

**SKIPPED — documented reason.** No new typed contract surface: frontend-interaction change reusing
existing endpoints (`PUT /api/teams/{id}`, portfolio PUT, `runItemPrediction`, `runBacktest`); no
new endpoint/DTO/rule module/CLI subcommand. The `nwave-ai` CLI is absent and
`docs/product/outcomes/registry.yaml` does not exist. Per D-6 gate-scoping (code-feature pipelines
with NEW typed contracts), the skip is correct, not a gap. KPI tag-links are recorded in
`docs/product/kpi-contracts.yaml` instead (OUT-remove-action-buttons-*).

## Wave: DISTILL / [REF] Build / RED verification

- `pnpm build` (frontend): GREEN — Biome clean on `./src`, `tsc -b` clean, `vite build` succeeded.
- E2E `tsc --noEmit`: clean; Biome clean on the WS spec + POM.
- Vitest collection: 23 in-memory scenarios collect + skip cleanly; existing 29 hook tests green.
- RED-not-BROKEN dry-run: un-skipping the US-01 + US-05 happy-path describes fails for
  MISSING_FUNCTIONALITY (`saveSettings`/`runItemPrediction` never called) — not import/fixture
  errors. The "no save on initial load" scenario passes against the inert scaffold (the no-op-on-mount
  guarantee the scaffold already satisfies; DELIVER keeps it green). Skip markers restored after the
  dry-run.
