# Wave Decisions — remove-action-buttons (ADO #5077)

DISCUSS wave (Luna). FINAL. Phases 1 (JTBD), 1.5 (Scope Assessment), 2 (Journey),
2.5 (Story Map + slice briefs), 3 (Requirements/Stories/AC/KPIs/DoR) complete. The
user CONFIRMED the slice split; journey, stories, KPIs, and SSOT updates are written.
DoR PASSED 9/9. No per-wave peer review run (consolidated review fires at end of DISTILL).
Ready for handoff to DESIGN.

## Configuration (locked with user)

- Feature type: User-facing (UI/UX).
- Walking skeleton: No (brownfield refinement of existing flows).
- JTBD: Yes (2 new jobs discovered — see `jtbd-job-stories.md`; no existing job covered
  auto-save/auto-run-on-valid).
- UX research depth: Comprehensive.
- Scope: all four surface groups IN.

## Risk note — no DIVERGE artifacts

`docs/feature/remove-action-buttons/diverge/` does not exist. No `recommendation.md` /
`job-analysis.md` to ground against. JTBD was run fresh in Phase 1. Low risk: the story
is a well-specified brownfield refinement with a shipped proof-of-pattern, not a
greenfield direction choice.

---

## Codebase grounding (verified, real paths)

| Surface group | Where it lives today | How it "acts" today |
|---|---|---|
| 1. Team/Portfolio **general settings** | `ModifyTeamSettings.tsx`, `ModifyProjectSettings.tsx` → `useModifySettings.ts` (`handleSave`) → `ValidationActions.tsx` Save button | One explicit Save button at the bottom of the whole settings form. `validateForm` gates validity; `disableSave`/`canUpdatePortfolioData` is the RBAC gate. |
| 2. **State Mappings** | `StateMappingsEditor.tsx` (inside `ModifyTeamSettings`/`ModifyProjectSettings`) | NOT a separate save — it feeds the SAME form Save. Plus its own Alert "After saving, a data reload is needed" (line 109-111). Two-step: save form, then manually refresh data. |
| 3. **Forecast Filter** | `ForecastFilterEditor.tsx` + `ForecastSettingsComponent.tsx` (inside `ModifyTeamSettings`) | NOT a separate save — also feeds the SAME form Save. Stopgap Alert `forecast-filter-takeeffect-hint` (commit 53e6287e, line 100-109): "save settings, then refresh throughput data." Two-step. |
| 4a. New-item-creation forecast | `NewItemForecaster.tsx` (in `TeamForecastView.tsx`) | Owns local state; explicit "Forecast" `ActionButton` (line 197). |
| 4b. Backtesting | `BacktestForecaster.tsx` (in `TeamForecastView.tsx`) | Owns local state; explicit "Run Backtest" `ActionButton` (line 361). |
| **Reference pattern (already shipped)** | `TeamForecastView.tsx` + `ManualForecaster.tsx` | Parent-owned auto-run: `hasInteractedRef` (no run on mount) + `DEBOUNCE_MS` debounce + `requestSeqRef` stale-response guard + run-only-when-valid. `ManualForecaster` is a dumb controlled child. THIS is the abstraction to reuse. |

### Two architecture facts that drive the split

1. **Surfaces 1, 2, 3 share ONE Save button.** They are not three independent save
   buttons — general settings, state mappings, and the forecast filter all feed the
   single `handleSave` in `useModifySettings`. So "auto-save general settings" and
   "auto-save state mappings" and "auto-save forecast filter" are the SAME mechanism
   change (auto-fire `handleSave` on valid, debounced) applied once, then differentiated
   only by their *dependent-data-refresh* needs (none / state-mapping reload / throughput
   refresh) and the *stopgap-hint removal*.
2. **Surfaces 4a, 4b need the forecast auto-run mechanism** that already exists in
   `TeamForecastView` for the manual forecaster. The abstraction is ALREADY SHIPPED
   (the How Many / When pattern). Each forecast slice reuses it; we do NOT need a new
   enabling abstraction slice for the forecast side.

### Carpaccio "ship the abstraction first?" investigation — resolved

The taste test says: if every slice depends on a new abstraction, ship the abstraction
first. Findings:

- **Forecast side (Job B):** the abstraction is already shipped (`TeamForecastView`
  auto-run). No enabling slice needed — slices reuse it directly.
- **Settings side (Job A):** there is NO shipped "auto-save-on-valid" hook for forms.
  The mechanism (debounced auto-fire of `handleSave` when `formValid`, with a
  save-state indicator and inline-error-as-primary-feedback) lives in `useModifySettings`
  and would be touched by all three settings surfaces. **Recommendation: make the FIRST
  settings slice the one that introduces the auto-save mechanism in `useModifySettings`
  AND delivers it end-to-end on the simplest real surface (general settings), rather
  than a pure non-shippable "abstraction-only" slice.** This honors "ship the
  abstraction first" while keeping every slice independently shippable and dogfoodable
  (no orphan infrastructure slice). Subsequent settings slices then consume the now-shipped
  mechanism and only add their dependent-data-refresh + hint-removal.

---

## Scope Assessment (Elephant Carpaccio Gate)

### Oversized heuristics (2+ signals = oversized)

| Signal | Threshold | This feature | Hit? |
|---|---|---|---|
| User stories | >10 | ~6-8 estimated across 4 surface groups | borderline |
| Bounded contexts / modules | >3 | settings (team + portfolio) module + forecast module = 2 frontend modules, but 4 distinct UI surface groups | partial |
| Walking skeleton integration points | >5 | N/A — no walking skeleton (brownfield) | no |
| Estimated effort | >2 weeks | ~4-6 days across all four groups | no |
| Multiple independent shippable outcomes | yes | **YES — 4 surface groups each ship & demo independently** | **HIT** |

### Verdict: **OVERSIZED for a single deliverable → SPLIT.**

The decisive signal is **multiple independent shippable outcomes**: each of the four
surface groups delivers a verifiable working behavior on its own and can ship, demo,
and dogfood separately. Even though total effort (~4-6 days) and story count (~6-8) are
only borderline, bundling four independent user-visible outcomes into one deliverable
violates the carpaccio principle (prefer many tiny deliverables over one big one) and
delays the felt value of the debt-retiring convergence surfaces. Splitting is correct.

---

## Proposed Slice Split (FOR USER CONFIRMATION)

Each slice is thin, end-to-end, independently shippable, ≤1 day, with a real
(non-synthetic) surface, a learning hypothesis, and a dogfood moment. Ordering balances
*learning leverage* (introduce the auto-save mechanism on the simplest surface first),
*dependency* (mechanism before its dependents), and *dogfood cadence* (retire visible
stopgap hints as early as possible after the mechanism exists).

> Persona for all slices: `delivery-forecaster` wearing the team-admin/portfolio-admin
> hat (e.g. "Priya Raman, RTE for the Atlas train, who admins the Atlas team settings").

### Slice 1 — Auto-save general team settings on valid input (mechanism-introducing)
- **Goal:** Editing a Team's general settings (name, throughput history, etc.) persists
  automatically the moment the form is valid; the Save button is gone; a calm save-state
  indicator ("Saving… / Saved") and inline errors replace the click ritual. Introduces
  the reusable auto-save behavior in `useModifySettings`.
- **Learning hypothesis:** A debounced auto-save gated on `formValid`, with a save-state
  indicator and inline-error-as-primary-feedback, fully replaces the Save button without
  the user feeling unsure "did it save?" — validated by dogfooding the real Team settings
  form. Confirms the mechanism design before three more surfaces depend on it.
- **Real surface / dogfood:** Edit the Atlas team's name + throughput history on a local
  instance; confirm it persists across refresh with no button.
- **IN:** auto-save mechanism in `useModifySettings`; general-settings fields; save-state
  indicator; RBAC gate parity (no auto-save where Save was disabled); invalid-input held +
  inline error. **OUT:** state mappings refresh, forecast filter, portfolio, forecasts.
- **Estimate:** ~1 day (mechanism + simplest surface).

### Slice 2 — Auto-save + auto-refresh State Mappings (retire the "must reload" hint)
- **Goal:** Editing state mappings auto-saves (reusing Slice 1's mechanism) AND triggers
  the dependent data reload automatically; the "After saving, a data reload is needed"
  Alert is removed.
- **Learning hypothesis:** Auto-save can be coupled to an automatic dependent-data refresh
  so the two-step ceremony collapses to zero steps, and users trust the data is current
  without the now-deleted reminder Alert.
- **Real surface / dogfood:** Regroup two Doing states into a mapping on the Atlas team;
  confirm the metrics/data reflect it without a manual refresh and without the hint.
- **IN:** state-mappings auto-save via Slice-1 mechanism; auto data-refresh wiring; remove
  `StateMappingsEditor` reload Alert. **OUT:** forecast filter, portfolio, forecasts.
- **Estimate:** ~1 day. **Depends on Slice 1.**

### Slice 3 — Auto-save + auto-refresh Forecast Filter (retire the 53e6287e stopgap)
- **Goal:** Editing the forecast filter rules auto-saves AND auto-refreshes throughput
  data; the `forecast-filter-takeeffect-hint` Alert (commit 53e6287e) is removed —
  closing the convergence loop Benj called out.
- **Learning hypothesis:** The exact convergence the convergence-note predicted: once
  auto-save+auto-refresh exists, the stopgap inline hint is pure deletion, and the
  filtered forecast/throughput surfaces update without the two-step dance.
- **Real surface / dogfood:** Add an "exclude Type = Bug" rule on the Atlas team
  (premium-seeded local instance); confirm throughput chart + forecasts reflect it with
  no save/refresh clicks and no hint.
- **IN:** forecast-filter auto-save via Slice-1 mechanism; auto throughput refresh; remove
  the stopgap Alert. **OUT:** portfolio, forecasts. **Premium-gated surface** — reuse
  existing license gating.
- **Estimate:** ~1 day. **Depends on Slice 1** (and conceptually on Slice 2's refresh
  wiring; can reuse it).

### Slice 4 — Auto-save general Portfolio settings on valid input
- **Goal:** The portfolio (`ModifyProjectSettings`) settings form gets the same auto-save
  behavior as Slice 1, respecting `canUpdatePortfolioData` RBAC gating.
- **Learning hypothesis:** The Slice-1 mechanism generalizes cleanly to the portfolio
  form (parallel `useModifySettings` consumer) with only the RBAC-gate wiring differing —
  confirming the mechanism is genuinely reusable, not team-specific.
- **Real surface / dogfood:** Edit a portfolio's settings locally; confirm auto-save +
  RBAC parity.
- **IN:** portfolio general settings auto-save; portfolio state-mappings auto-save+refresh
  (portfolio also embeds `StateMappingsEditor`). **OUT:** forecasts.
- **Estimate:** ~1 day. **Depends on Slice 1** (and Slice 2 for the portfolio state-mapping
  refresh half).

### Slice 5 — Auto-run the New-Item-Creation forecast on valid input
- **Goal:** `NewItemForecaster` recomputes automatically when its inputs (from/to/target
  dates + ≥1 work-item type) are valid; the "Forecast" button is gone. Reuses the shipped
  `TeamForecastView` auto-run pattern.
- **Learning hypothesis:** Lifting `NewItemForecaster`'s inputs into the parent's
  existing auto-run orchestration (debounce + `requestSeqRef` + `hasInteractedRef`) gives
  the same fluid feel as the How Many / When forecast, with no on-mount run and no
  stale-result flicker.
- **Real surface / dogfood:** On the Atlas team forecast page (demo data), widen the
  historical window and watch the new-item forecast recompute live.
- **IN:** new-item auto-run via parent pattern; remove "Forecast" button; on-mount
  suppression; stale-guard. **OUT:** backtest.
- **Estimate:** ~1 day. Independent of the settings slices (different module); depends only
  on the already-shipped forecast pattern.

### Slice 6 — Auto-run Backtesting on valid input
- **Goal:** `BacktestForecaster` runs automatically when start/end + historical window are
  valid; the "Run Backtest" button is gone. Reuses the shipped auto-run pattern.
- **Learning hypothesis:** The backtest's richer input set (rolling vs date-range modes,
  historical window) can be driven by the same debounced valid-input auto-run without
  firing on every keystroke or on mode-toggle mid-edit.
- **Real surface / dogfood:** On the Atlas team (demo data), switch rolling↔date-range and
  adjust the window; backtest recomputes live, stale runs never overwrite fresh.
- **IN:** backtest auto-run via parent pattern; remove "Run Backtest" button; debounce +
  stale-guard tuned for the larger input set. **OUT:** none.
- **Estimate:** ~1 day. Independent of settings slices; depends only on the shipped pattern.

### Out of scope (explicit)
- The How Many / When manual forecast (already shipped — it is the proof-pattern, not work).
- Any API-contract / DTO change (preliminary read: none needed; confirm in Phase 3).
- Portfolio-level forecast surfaces beyond settings (no new-item/backtest at portfolio scope).

---

## Slice Taste Tests

| Test | Result | Reason |
|---|---|---|
| **Independently shippable** | PASS | Each slice ships and demos alone. Settings slices 2-4 depend on slice 1's mechanism but each still delivers a distinct user-visible behavior on its own surface once 1 is in. |
| **≤1 day each** | PASS | All six are ~1 day. Slice 1 carries the mechanism but on the simplest surface, keeping it within budget. |
| **Thin & end-to-end** | PASS | Each slice goes UI → persistence/run → user-visible result on a real surface. No horizontal "backend only" or "hook only" slice. |
| **Real (non-synthetic) data** | PASS | Every slice dogfoods a real Lighthouse surface (Atlas team/portfolio, demo data, premium-seeded for slice 3). No invented surfaces. |
| **Named learning hypothesis** | PASS | Each slice states one. Slice 1's is the load-bearing one (mechanism viability). |
| **Dogfood moment** | PASS | Each slice has a concrete local-instance dogfood action. |
| **Abstraction-first respected** | PASS | Settings auto-save mechanism is introduced in slice 1 (first dependent) rather than as an orphan slice; forecast auto-run abstraction already shipped, reused by slices 5-6. |
| **No infrastructure-only slice** | PASS | No slice is `@infrastructure`-only; each is user-visible. (Slice 1 bundles mechanism WITH a visible surface precisely to avoid an orphan-infra slice.) |

One caveat to flag, not a failure: **Slice 1 is the riskiest and the linchpin** — three
settings slices depend on its mechanism design. That is intentional (validate the
mechanism on the simplest surface first), but it means slice 1 should be reviewed
carefully before 2-4 build on it.

---

## Recommended Execution Order

1. **Slice 1** (general team settings + mechanism) — FIRST: introduces and validates the
   auto-save abstraction on the simplest real surface; unblocks 2, 3, 4. Highest learning
   leverage.
2. **Slice 3** (forecast filter) — EARLY: retires the explicit 53e6287e stopgap debt Benj
   called out; highest felt-value-per-effort once the mechanism exists. (Can run before
   slice 2 if the throughput-refresh wiring is built here; otherwise build refresh wiring
   in 2 and reuse.)
3. **Slice 2** (state mappings) — retires the second "must reload" hint; completes the
   team-settings convergence.
4. **Slice 4** (portfolio settings) — generalizes the mechanism to the portfolio form;
   confirms reusability.
5. **Slice 5** (new-item forecast) and **6** (backtest) — independent of the settings
   track (different module, already-shipped pattern); can be done in parallel with the
   settings slices by a second contributor, or after. Sequenced 5 then 6 (5 is the simpler
   input set, warms up the pattern-reuse before 6's richer inputs).

Rationale: dependency forces slice 1 first; debt-retirement + dogfood cadence pulls the
convergence slices (3, then 2) immediately after; the forecast slices are an
independent parallel track gated only on the already-shipped pattern.

---

## Scope Assessment: CONFIRMED SPLIT — 6 slices, 2 frontend modules (settings + forecast), estimated ~4-6 days

User confirmed the split. Settings track sequenced 1 → 3 → 2 → 4; forecast track 5 → 6
runs in parallel (gated only on the already-shipped How Many / When auto-run pattern,
NOT on Slice 1).

---

## Phase 2-3 decisions (added 2026-05-29)

- **D-RELOAD (user-locked):** reload-needed alerts carry a one-click "Reload now" — never
  "navigate away and refresh". Mappings (cheap) → auto-refresh; Forecast Filter throughput
  (expensive) → one-click "Reload throughput now". Both stopgap Alerts removed.
- **D-RBAC-PARITY:** auto-save/auto-run gated exactly where the button is gated today
  (`disableSave` / `canUpdatePortfolioData` / `isTeamAdmin`). No new authz surface.
- **D-LINCHPIN:** Slice 1 introduces the reusable auto-save mechanism on the simplest surface;
  reviewed before Slices 2-4 build on it.
- **Persona:** reused `delivery-forecaster` (team/portfolio-admin hat); both new jobs added to
  its `primary_jobs`. No new persona.
- **Jobs:** `job-commit-intent-no-button` (Job A) + `job-see-forecast-update-live` (Job B) added
  to `docs/product/jobs.yaml` with full dimensions/forces/opportunity score (both gap 1 — honest polish).
- **SSOT:** new journey `docs/product/journeys/remove-action-buttons.yaml`; back-propagated the
  one changed step (`step-configure-filter-rules`) into `filter-forecast-throughput.yaml` via a
  `## Changed Assumptions` block (DISCOVER steps NOT mutated).

## DoR: PASSED 9/9 (evidence in feature-delta.md)

## Cross-cutting (DoR Item 7): RBAC parity (no new surface) | Clients unaffected (no API change) | Website N/A (UX polish).

## Next: handoff to DESIGN (solution-architect). Do NOT proceed into DESIGN in this dispatch.
