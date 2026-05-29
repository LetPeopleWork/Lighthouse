# JTBD Job Stories — remove-action-buttons (ADO #5077)

Source story: "Remove Buttons to force Actions" (tag: UX Improvement). This is a
brownfield UX-coherence refinement: flows that already work, but make the user
perform a ceremonial action (click Save, click Forecast/Run) after they have
already expressed valid intent. The proof-of-pattern already shipped — the Team
Forecast "How Many / When" manual forecast auto-runs on valid input
(`TeamForecastView.tsx` parent-owned debounce + stale-guard pattern). This feature
generalises that proof to four remaining surface groups.

> **No existing job in `docs/product/jobs.yaml` covers auto-save / auto-run-on-valid.**
> The two jobs below are NEW. They are deliberately framed as a satisfaction/polish
> improvement (importance is honest — these flows are not broken, they are friction-y).

---

## Job A (unifying): Commit my intent without a ceremonial button

> **When** I have just finished expressing a valid change in Lighthouse — edited a
> team/portfolio setting, adjusted a state mapping, tuned a forecast filter, or
> entered the inputs for a forecast —
> **I want** Lighthouse to act on my intent the moment it is valid (persist it,
> and refresh whatever data depends on it), instead of making me hunt for and click
> a Save / Run button,
> **so I can** stay in the flow of the decision I am actually making (is this
> forecast date defensible? is this mapping right?) rather than managing the tool's
> save/refresh ceremony — and trust that what I see on screen reflects what I just
> changed.

### Three dimensions

- **Functional**: As soon as inputs are valid, the change is persisted (settings) or
  computed (forecasts) without a button press; if the dependent data view needs a
  refresh (state mappings, forecast filter), that refresh happens automatically too,
  so the two-step "save then manually refresh" ceremony disappears. When inputs are
  invalid, the change is held back and the specific problem is shown inline — the
  absence of a Save button must not hide the error.
- **Emotional**: Move from low-grade friction and doubt ("did that actually save?",
  "do I need to refresh now?", "where did my error go without a Save button to
  click?") to quiet confidence ("I changed it, it took, the screen is current").
- **Social**: Present Lighthouse to a team or to leadership as a tool that "just
  keeps up" with the user — no awkward "hang on, let me click save and refresh"
  mid-demo. The consistency across every settings/forecast surface reads as polish.

### JTBD → story bridge

Job A is the umbrella. Every slice in the Phase 1.5 split is a thin end-to-end
realisation of Job A on one concrete surface. The acceptance core repeats across
slices ("valid input → acted on, no button; invalid input → held + visible error")
and is the reusable scenario skeleton handed to DISTILL.

---

## Job B (forecast-run flavor): See my forecast update as I shape its inputs

> **When** I am sizing up a new-item-creation forecast or a backtest and I am
> adjusting the historical window, the target date, or the work-item-type selection,
> **I want** the forecast to recompute itself each time my inputs become valid — the
> same way the How Many / When forecast already does —
> **so I can** explore "what if the window were wider / the target later" as a
> continuous conversation with the numbers, instead of tweaking, clicking Run,
> reading, tweaking, clicking Run again.

### Three dimensions

- **Functional**: When the required inputs for new-item-creation (from/to/target
  dates + at least one work-item type) or backtest (start/end + historical window)
  are all valid, the run fires automatically on change (debounced, with stale-response
  guarding — exactly the `requestSeqRef` + `DEBOUNCE_MS` pattern already in
  `TeamForecastView`). Auto-run does NOT fire on first mount (no result until the user
  engages — preserve the `hasInteractedRef` gate). Invalid/incomplete inputs simply
  produce no run and show why.
- **Emotional**: Move from "stop-start" tweak-click-read rhythm to a fluid "the
  numbers track my thinking" feel — the same delight the How Many / When forecast
  already delivers.
- **Social**: In a planning session, exploring scenarios live ("watch what happens to
  the backtest if I widen the window") is more credible than narrating clicks.

### Why B is distinct from A

A is *commit-on-valid* (persist + refresh dependent data). B is *recompute-on-valid*
(no persistence — a forecast run is ephemeral, re-runnable, and has stale-response
and on-mount concerns A does not). They share the "no button, act on valid" spirit
but have genuinely different acceptance shapes (debounce + sequence guard + on-mount
suppression for B; persistence + dependent-data-refresh + RBAC write-gating for A).
Splitting them keeps each slice's scenarios honest.

### JTBD → story bridge

Job B maps to the two forecast surfaces that have NOT yet adopted the proof-pattern:
`NewItemForecaster` and `BacktestForecaster`. Both currently own local state +
explicit `ActionButton`. The bridge work is to lift their inputs into the
parent's auto-run orchestration (or replicate it), reusing the shipped
`ManualForecaster` reference exactly.

---

## Persona decision

**Reuse `delivery-forecaster`** (aliases: delivery-lead, RTE) as the primary actor,
with its already-documented `related_personas: [team-admin]` covering the settings-edit
hat. Justification:

- `delivery-forecaster.yaml` already states the actor "often the same person as
  team-admin" and owns both team-settings editing and forecast running — exactly the
  span of this feature (settings surfaces + forecast surfaces).
- Job B (forecast exploration) is squarely `delivery-forecaster`'s documented job.
- Job A's settings-edit surfaces are gated to team-admin / portfolio-admin (RBAC),
  which the persona's `related_personas` and the existing `team-admin` persona in
  `jobs.yaml` already model.

**Do NOT introduce a new `team-admin`-only persona.** A `team-admin` concept already
exists in `jobs.yaml` (`job-rbac-scoped-admin`, `job-team-admin-tune-staleness`) and
in the persona's related list. Inventing a fourth persona would fragment, not clarify.
We will name the concrete actor in domain examples as a real delivery
forecaster/team-admin (e.g. "Priya Raman, RTE for the Atlas train, who also admins
the Atlas team's settings").

---

## Cross-cutting impact (preliminary — full treatment in Phase 3)

- **RBAC**: No new authorization surface. Auto-save MUST respect exactly the same
  write-permission gate the current Save button enforces. Grounded: `ModifyTeamSettings`
  passes `disableSave`/`saveTooltip`; `ModifyProjectSettings` gates Save on
  `canUpdatePortfolioData` (line 276); the forecast filter editor is read-only for
  non-team-admins via `useRbac().isTeamAdmin(teamId)` (`ForecastFilterEditor.tsx` line 70).
  The auto-save trigger must be suppressed identically where Save is disabled, flowing
  through `IRbacAdministrationService` / `useRbac()` per Architecture. To confirm in DESIGN.
- **Lighthouse-Clients (CLI + MCP)**: Preliminary read — **unaffected**. This is a
  frontend interaction change (when a PUT/POST fires), not an API-contract change. The
  endpoints (`PUT /api/teams/{id}`, the forecast run/backtest/item-prediction POSTs)
  are unchanged in shape. To confirm no DTO change in Phase 3; if confirmed, no client
  version-gate needed.
- **Website**: Preliminary read — **N/A**. Pure interaction polish on existing,
  already-marketed flows; no new premium capability to surface. Confirm in Phase 3.
