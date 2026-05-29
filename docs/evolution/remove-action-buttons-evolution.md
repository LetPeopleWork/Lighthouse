<!-- DES-ENFORCEMENT : exempt -->

# Evolution Archive — remove-action-buttons (Finalize)

**Feature ID**: `remove-action-buttons`
**Epic**: ADO #5077 (https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5077)
**Customer**: LetPeopleWork (UX-coherence + debt retirement; raised internally)
**Waves shipped**: DISCUSS -> DESIGN -> DISTILL -> DELIVER (all 2026-05-29)
**Planning baseline**: `f977e949`
**HEAD at finalize**: `2770d739` (`fix(e2e): drop retired forecast-filter takeeffect-hint assertion (#5077)`)
**Commit range**: `f977e949..2770d739` — ~30 commits on `main` (local, unpushed at finalize).
**Status**: Feature complete. All six slices live; full Vitest suite green; walking skeleton verified live; mutation above the 80% gate. Adoption KPIs (5-6) remain blocked on Epic 5015.

---

## Feature summary

A frontend-only React/TypeScript UX refinement: replace the explicit Save / Run buttons with
debounced auto-save / auto-run on valid input across **six surfaces** — general team settings, state
mappings, forecast filter (premium), portfolio settings, new-item forecast, and backtest. The feature
establishes one interaction law — "valid input is acted on immediately; no ceremonial Save/Run click" —
and retires the two stopgap "you must refresh" Alerts that existed only because this work was pending.

The mechanism is an opt-in `autoSave` capability inside the already-shared `useModifySettings` hook
(debounce 300ms; `idle → saving → saved | error` state machine; validity-gate on `formValid`; injected
`canSave` for RBAC parity; monotonic `requestSeq` stale-guard; opt-in `refreshOnSave` /
`reloadDependentData`; no-save-on-mount), plus two new small presentational components
(`SaveStateIndicator`, `ReloadDependentDataAction`). The forecast track reused the already-shipped
`TeamForecastView` auto-run orchestration. No API contract, DTO, or backend port was touched.

## Business context

Every settings and forecast surface in Lighthouse forced a ceremonial Save / Run click, even though the
How Many / When forecast had already proven a calmer auto-compute pattern (the proof-of-pattern, not
new work). Two stopgap Alerts ("After saving, a data reload is needed"; `forecast-filter-takeeffect-hint`)
papered over the gap by instructing users to refresh manually. The feature aligns all six surfaces with
the proven pattern and deletes both stopgaps. Honest framing: low ODI gaps — this is polish + debt
retirement, not a capability gap.

## What shipped

- **The linchpin mechanism** — opt-in `autoSave` on `useModifySettings` plus `SaveStateIndicator`. Shipped first on the simplest real surface (general team settings) and reviewed before the consumers built on it.
- **Six surfaces converted** — general team settings (Save button removed), state mappings (auto-save + silent metrics auto-refresh + one-click fallback), forecast filter (auto-save + one-click "Reload throughput now"), portfolio settings (`canUpdatePortfolioData` parity), new-item forecast auto-run, backtest auto-run.
- **Two retired stopgap Alerts** — `forecast-filter-takeeffect-hint` (`ForecastSettingsComponent.tsx`) and the StateMappings "must reload" Alert, both replaced by silent auto-save + (auto-refresh OR one-click reload-now), never a navigate-away instruction (D-RELOAD).
- **One walking skeleton** — a single Playwright spec proving the real-io loop on the team-settings surface, folded into the existing `ForecastFilter.spec.ts` to keep the suite lean (`AutoSaveTeamSettings.spec.ts` deleted).

## Key decisions

### Architectural (ADR-backed)

- **ADR-029 — Auto-save-on-valid mechanism placement + save-state machine + RBAC-by-injection.** Auto-save lives as an opt-in capability *inside* `useModifySettings` (not a wrapper hook, not per-component effects) — the hook is already the single owner of `settings`/`formValid`/`validationError`, so the stale-guard invariant gets one owner. RBAC permission is *injected* as `canSave`; the hook never calls `useRbac()` itself (conforms to ADR-001). `SaveStateIndicator` is a NEW passive *status* affordance, deliberately not an extension of the action-shaped `ValidationActions` (which keeps its ~6 other callers untouched).
- **ADR-030 — Dependent-data reload after auto-save: cost-based split.** State Mappings (cheap client metrics re-fetch) → silent auto-refresh with a one-click fallback on failure; Forecast Filter throughput (expensive backend recompute) → one-click "Reload throughput now" in-place. The expensive recompute *always requires the click* (OQ-2, user-locked) — a valid rule edit auto-saves silently, but the throughput recompute never auto-fires.

### Design / delivery invariants

- **D-LINCHPIN then reuse** — Slice 1 introduced the reusable mechanism AND shipped it on the simplest surface, reviewed before Slices 2-4 consumed it. This "build the mechanism on the easiest real surface, prove it, then roll it out" approach kept the blast radius bounded and gave the consumers a known-good seam.
- **D-REUSE-SHIPPED-PATTERN** — the forecast track (Slices 5-6) reused `TeamForecastView`'s `hasInteractedRef` / `requestSeqRef` / `DEBOUNCE_MS` rather than inventing a second debounce/stale-guard. No new abstraction.
- **D-RBAC-PARITY** — auto-save/auto-run is suppressed exactly where the Save/Run button was gated today; no new authorization surface.
- **OQ-1 (user-locked)** — the persisted-state copy is **"All changes saved"** (Google-Docs idiom), uniform across all four settings surfaces.

### Late-session decision (user slice-review feedback)

- **Backtest starts with an empty rolling window.** Originally Slice 6 / step 06-03 framed the rule as *"No backtest run on page load"* (DISTILL scenario #24) — a guard suppressing a run despite *complete* default inputs. During slice review the user observed this felt inconsistent with the new-item/manual surfaces, which start *incomplete* and wait for input. The fix: initialize the backtest with an empty rolling window so on load its inputs are genuinely incomplete — uniform with new-item/manual. The no-run-on-mount guard stays correct and unchanged; the same observable outcome (no run on load) is now achieved by honest incompleteness rather than (only) the interaction guard. Recorded as a Changed Assumption in `feature-delta.md`.

## Steps completed

The DELIVER wave executed **32 DES-monitored steps** (3-phase canon; integrity 32/32), all EXECUTED
PASS in `docs/feature/remove-action-buttons/deliver/execution-log.json` (schema 3.0). Execution order
followed the locked slice plan: settings track 1 → 3 → 2 → 4, forecast track 5 → 6 in parallel, with a
07-* reorg sub-track folding the walking skeleton into the team-settings E2E spec.

- **Slice 01 — linchpin: auto-save on general team settings** (`useModifySettings` autoSave + `SaveStateIndicator`; Save button removed). Reviewed before consumers built on it.
- **Slice 03 — forecast filter (premium)**: auto-save + one-click "Reload throughput now"; `forecast-filter-takeeffect-hint` Alert deleted.
- **Slice 02 — state mappings**: auto-save + silent metrics auto-refresh; "must reload" Alert deleted; one-click fallback on refresh failure.
- **Slice 04 — portfolio settings**: auto-save with `canUpdatePortfolioData` parity.
- **Slice 05 — new-item forecast auto-run** (reuses shipped `TeamForecastView` orchestration).
- **Slice 06 — backtest auto-run** (reuses shipped orchestration; empty rolling window on load per review).
- **Sub-track 07 — E2E consolidation**: walking skeleton folded into `ForecastFilter.spec.ts`; `AutoSaveTeamSettings.spec.ts` deleted; retired-Alert assertions reconciled.

## Quality outcomes

- `pnpm test` (Vitest): GREEN — **3090 passing**.
- `pnpm build`: GREEN — Biome zero-warnings on `./src`, `tsc -b` strict clean, `vite build` succeeded.
- DES integrity: 32/32 steps EXECUTED PASS.
- Adversarial review: APPROVED — 0 findings, testing-theater pass.
- Walking skeleton: verified live against demo scenario 0 (Team Zenith).
- Mutation (Stryker, frontend): **81.82% overall** (above the 80% project minimum). See `docs/feature/remove-action-buttons/deliver/mutation-baseline.md`.

## Outcome KPIs (GA baseline, committed)

- **KPI 1 (North Star):** surfaces requiring an explicit Save/Run click for valid input → **0 of 6** (baseline 6/6).
- **KPI 2:** stopgap "you must refresh" Alerts → **0** (baseline 2).
- **KPI 3:** surfaces exhibiting valid-input-acts-immediately → **6 of 6** (baseline 1/6).
- **KPIs 5-6 (adoption):** lost-edit incidents −50%, scenario recomputes/session +30% — **BLOCKED on Epic 5015** (self-hosted telemetry gap; no cross-instance instrumentation). Recorded as aspirational, not abandoned.

## Lessons learned

- **The linchpin-then-reuse approach paid off.** Building the auto-save mechanism on the simplest real surface (general team settings) and getting it reviewed *before* the four consumers built on it meant every downstream slice consumed a known-good seam. Putting the mechanism in the already-shared hook (one owner of the stale-guard) avoided five divergent debounce implementations.
- **Update ALL test layers when retiring a UI element.** The 07-03 E2E merge surfaced a pre-existing RED: a Vitest spec (`ForecastSettingsComponent.test.tsx`) still asserted the *presence* of the `forecast-filter-takeeffect-hint` testid that production no longer rendered, while another asserted its *absence* — a contradiction caught only at E2E-merge time. The retired Alert had been removed from production and one test layer but not reconciled across all layers (Vitest assertion, POM getter, E2E assertion). Lesson: when you delete a UI element, grep every layer (production, component test, POM, E2E spec) in the same change — a clean `pnpm build` does not catch a stale `.not.toBeInTheDocument()` vs a stale POM getter expecting the element.
- **Honest mechanism beats a clever guard.** The backtest "no run on mount" was originally a guard suppressing complete default inputs. User review exposed the inconsistency; the better answer was to make the inputs genuinely incomplete (empty rolling window) so all three forecast surfaces behave uniformly. The guard stayed correct, but the mechanism became more honest — a reminder that "the test still passes" is not the same as "the behaviour is right."
- **Telemetry-blocked KPIs are recorded, not hidden.** KPIs 5-6 (true adoption outcomes) cannot be measured cross-instance until Epic 5015. They are explicitly carried as aspirational with their blocker named, rather than silently dropped or faked with proxies that don't measure the real thing.

## Links to migrated / permanent artifacts

- ADRs: `docs/product/architecture/adr-029-autosave-on-valid-mechanism-placement-and-save-state-machine.md`, `docs/product/architecture/adr-030-dependent-data-reload-after-autosave-cost-based-split.md`.
- Architecture delta: `docs/product/architecture/brief.md` → `## Application Architecture — remove-action-buttons` (SHIPPED).
- KPI contracts: `docs/product/kpi-contracts.yaml` (GA baseline).
- Mutation baseline: `docs/feature/remove-action-buttons/deliver/mutation-baseline.md`.
- Feature workspace (history): `docs/feature/remove-action-buttons/` (preserved for the wave matrix).
