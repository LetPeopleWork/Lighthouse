<!-- markdownlint-disable MD024 -->
# User Stories — remove-action-buttons (ADO #5077)

Persona for all stories: **delivery-forecaster** wearing the team-admin / portfolio-admin
hat. Concrete actor: *Priya Raman, RTE for the Atlas train, who also admins the Atlas team's
settings*. Viewer foil: *Sam Lee, read-only*.

## System Constraints (cross-cutting)

- **No API contract / DTO change.** Every slice is a frontend interaction change reusing
  existing endpoints (`PUT /api/teams/{id}`, portfolio PUT, the forecast run / item-prediction /
  backtest POSTs). Lighthouse-Clients (CLI + MCP) unaffected; no version-gate needed.
- **RBAC parity, no new authz surface.** Auto-save / auto-run is suppressed exactly where the
  Save / Run button is disabled today, via `useRbac()` (`disableSave`, `canUpdatePortfolioData`,
  `isTeamAdmin(teamId)`) flowing through `IRbacAdministrationService`. No component fetches
  `/authorization/my-summary` directly.
- **Validity gate.** Auto-save fires only when `formValid === true` (`validateForm` in
  `useModifySettings`). Auto-run fires only when all forecast inputs are valid. Half-typed /
  invalid state is never persisted.
- **Save-state machine is the single reassurance source.** All settings surfaces (Slices 1-4)
  read one `saveState` (idle | saving | saved | failed) from `useModifySettings`. Inline error
  (`validationError`) is the PRIMARY failure feedback channel once the Save button is gone.
- **D-RELOAD (Slices 2 & 3).** When a change needs a data reload, the in-place alert carries a
  one-click "Reload now" action; never instruct the user to navigate elsewhere and refresh.
  Auto-refresh where cheap (State Mappings); one-click where expensive (Forecast Filter throughput).
- **Forecast auto-run reuses the shipped pattern.** Slices 5-6 reuse `TeamForecastView`'s
  `hasInteractedRef` (no run on mount) + `requestSeqRef` (stale guard) + `DEBOUNCE_MS` (300ms).
- **Slice composition gate.** Every slice contains ≥1 user-visible value story (all six stories
  below are user-facing — no `@infrastructure`-only slice).

---

## US-5077-01: Auto-save general team settings (linchpin)

`job_id: job-commit-intent-no-button` | Slice 1 | Must Have

### Elevator Pitch

- **Before:** Priya edits a Team setting, scrolls to the bottom of a long form, and clicks Save — and can navigate away and silently lose the edit.
- **After:** Priya edits a field on `/teams/42/settings` (e.g. throughput history → 60) → the field persists with a calm "All changes saved" indicator and survives a page refresh, with no Save button.
- **Decision enabled:** Priya can keep her attention on the decision she is actually making ("is 60 days the right throughput window?") instead of managing the tool's save ceremony, and trusts the screen reflects what she changed.

### Problem

Priya Raman is a team-admin who, after editing the Atlas team's settings, must hunt to the
bottom of a long form and click one Save button. It is easy to forget and easy to navigate
away and lose the edit. The Save click is a ceremony, not a decision.

### Who

- Delivery-forecaster / team-admin | editing team settings on a long form | wants to stay in
  the flow of the configuration decision, not manage save mechanics.

### Solution

Introduce a reusable auto-save mechanism in `useModifySettings`: debounced auto-fire of
`handleSave` when `formValid`, a save-state machine (idle/saving/saved/failed), and
inline-error-as-primary-feedback. Remove the Save button from general team settings. Suppress
auto-save where `disableSave` is true (RBAC parity). This is the linchpin — Slices 2-4 consume it.

### Domain Examples

#### 1: Happy Path — Priya changes throughput history from 90 to 60
Priya, team-admin on `/teams/42/settings`, changes throughput history to 60 and stops typing;
after ~300ms the indicator shows "Saving…" then "All changes saved"; 60 persists across refresh.

#### 2: Edge Case — Priya renames the team to a valid new name
Priya changes the name from "Atlas Delivery Team" to "Atlas Train"; the new name auto-saves and
is shown on the team page after refresh; no Save click.

#### 3: Error/Boundary — Sam Lee (viewer) opens the same settings
Sam Lee, a viewer, opens `/teams/42/settings`; fields are read-only; no save-state indicator;
no auto-save fires (parity with today's disabled Save).

### UAT Scenarios (BDD)

#### Scenario: General team settings persist the moment they are valid, with no Save button
Given Priya Raman is a team-admin editing the Atlas team settings on /teams/42/settings
And the form shows throughput history "90"
When Priya changes throughput history to "60" and stops typing
Then a save-state indicator shows "Saving…" then "All changes saved" within the debounce window
And the value "60" is still present after Priya refreshes the page
And there is no Save button on the form

#### Scenario: An invalid edit is held back and the inline error is the primary feedback
Given Priya is editing the Atlas team settings with a valid name "Atlas Delivery Team"
When Priya clears the name field so the form becomes invalid
Then no save is fired
And an inline error "Name is required" is shown next to the field
And the previously persisted name "Atlas Delivery Team" is unchanged after refresh

#### Scenario: A failed save keeps the edit and offers a one-click retry
Given Priya changes throughput history to "60" on a valid form
And the server returns an error when the change is saved
When the save attempt completes
Then the save-state indicator shows "Couldn't save" with a Retry action
And Priya's edit "60" is still present in the form
And clicking Retry re-attempts the save

#### Scenario: A viewer cannot trigger an auto-save (RBAC parity)
Given Sam Lee is a viewer (non-team-admin) on /teams/42/settings
When Sam opens the Atlas team settings
Then the fields are read-only
And no auto-save fires

### Acceptance Criteria

- [ ] A valid general-settings edit persists automatically (debounced) with no Save button.
- [ ] Save-state indicator transitions idle → "Saving…" → "All changes saved".
- [ ] An invalid edit fires no save; inline error is shown; last valid value stays persisted.
- [ ] A failed save shows "Couldn't save" + Retry; edits are not lost; Retry re-fires save.
- [ ] No auto-save fires where `disableSave` is true; fields read-only for non-admins.
- [ ] Rapid edits debounce; only the latest valid state persists (stale guard).

### Outcome KPIs

- **Who:** team-admins editing general team settings.
- **Does what:** persist a valid edit with no Save click.
- **By how much:** 1 of 6 surfaces converted (linchpin); +KPI 4 usability target (≥90% notice invalid field <5s).
- **Measured by:** acceptance test (no Save button + auto-save) + moderated walkthrough.
- **Baseline:** Save button present; lost-edit rate unknown.

### Technical Notes

- **RBAC:** Suppress auto-save where `disableSave` (ModifyTeamSettings line 253) is true, via
  `useRbac()` / `IRbacAdministrationService`. No new authz surface.
- **Clients:** N/A — no API change (reuses `PUT /api/teams/{id}`).
- **Website:** N/A — pure UX polish on an already-marketed flow.
- Mechanism lives in `useModifySettings.ts` (`handleSave`, `formValid`, `validationError`).
  Replaces `ValidationActions` pending/success/failed icons with the save-state indicator.
- **LINCHPIN — review carefully before Slices 2-4 build on this mechanism.**
- Dependencies: none (first slice).

---

## US-5077-03: Auto-save + auto-refresh Forecast Filter (retire the 53e6287e stopgap)

`job_id: job-commit-intent-no-button` | Slice 3 | Must Have | Premium

### Elevator Pitch

- **Before:** Priya edits a forecast filter rule, clicks Save, then reads an Alert telling her to go to the team page and refresh throughput data — a two-step ceremony, and the Alert is admitted debt (commit 53e6287e).
- **After:** Priya adds "Exclude items where Type equals Bug" on `/teams/42/settings` (premium) → the rule auto-saves with a "Saved" indicator and an in-place "Reload throughput now" button; one click recomputes the throughput chart and forecasts. The stopgap Alert is gone.
- **Decision enabled:** Priya can decide what counts as forecast "noise" and see its effect in one click, without leaving the page or remembering a manual refresh.

### Problem

Priya, a team-admin on a premium tenant, edits forecast filter rules and is told by a stopgap
Alert (`forecast-filter-takeeffect-hint`, commit 53e6287e) to save and then go elsewhere to
refresh throughput. The two-step dance is explicit debt the user wants retired.

### Who

- Delivery-forecaster / team-admin on a premium tenant | tuning forecast-throughput filter
  rules | wants the filtered numbers to reflect the rule without a multi-step refresh.

### Solution

Reuse Slice 1's auto-save mechanism for the forecast filter rule set. Because the throughput
recompute is expensive, apply D-RELOAD as a one-click "Reload throughput now" action embedded
in the in-place alert (NOT silent auto-refresh). Remove the `forecast-filter-takeeffect-hint`
Alert. Preserve existing premium gating and `isTeamAdmin` read-only gating.

### Domain Examples

#### 1: Happy Path — Priya excludes Bugs from forecast throughput
Priya adds "Exclude items where Type equals Bug"; the rule auto-saves; she clicks "Reload
throughput now"; the throughput chart and forecasts recompute against the filtered series.

#### 2: Edge Case — Priya adds an orphan-item rule (Parent Reference ID is empty)
Priya adds a second rule excluding orphan items; it auto-saves alongside the first; one reload
applies both.

#### 3: Error/Boundary — Sam Lee (viewer) opens the filter section
Sam Lee, a viewer on the premium tenant, sees the rule editor read-only; no auto-save fires.

### UAT Scenarios (BDD)

#### Scenario: A forecast-filter change auto-saves and offers a one-click throughput reload
Given Priya is a team-admin on a premium tenant editing the Forecast Filter on /teams/42/settings
When Priya adds the rule "Exclude items where Type equals Bug" and stops editing
Then the rule auto-saves with a "Saved" indicator and no Save button
And the alert offers a single-click "Reload throughput now" action in place
And the user is never instructed to navigate to another page and refresh manually
And the "forecast-filter-takeeffect-hint" alert is no longer shown

#### Scenario: Reloading throughput in one click reflects the new filter
Given Priya has just auto-saved an "Exclude Type = Bug" rule
When Priya clicks "Reload throughput now"
Then the throughput chart and forecasts recompute against the filtered throughput
And no separate save or navigation step was required

#### Scenario: A non-team-admin sees the filter read-only with no auto-save
Given Sam Lee is a viewer on a premium tenant on /teams/42/settings
When Sam opens the Forecast Filter section
Then the rule editor is read-only
And no auto-save fires

#### Scenario: An invalid rule is rejected without clobbering the existing rule set
Given Priya submits a rule with an unknown field key
When the change would auto-save
Then the change is rejected with an inline error
And the previously persisted rule set remains intact

### Acceptance Criteria

- [ ] A valid filter-rule edit auto-saves (debounced) with a "Saved" indicator, no Save button.
- [ ] The in-place alert carries a one-click "Reload throughput now"; no "go elsewhere" instruction.
- [ ] Clicking "Reload throughput now" recomputes throughput chart + forecasts against the filter.
- [ ] The `forecast-filter-takeeffect-hint` Alert is removed (grep + test confirm absence).
- [ ] Non-team-admin: editor read-only, no auto-save (RBAC parity, err-non-team-admin-attempts-edit).
- [ ] Invalid rule (unknown field key) rejected with inline error; prior rule set intact.

### Outcome KPIs

- **Who:** team-admins on premium tenants tuning forecast filters.
- **Does what:** auto-save a rule and reload throughput in one click.
- **By how much:** stopgap Alert #1 removed; 1 more surface converted (highest felt-value/effort).
- **Measured by:** grep + test for Alert absence; acceptance test for auto-save + one-click reload.
- **Baseline:** 2-step ceremony + stopgap Alert present.

### Technical Notes

- **RBAC:** `isTeamAdmin(teamId)` read-only gate (ForecastFilterEditor line 70); suppress
  auto-save identically. No new authz surface.
- **Clients:** N/A — no API change (reuses `PUT /api/teams/{id}` for the rule set field).
- **Website:** N/A — premium flow already marketed; interaction polish only.
- D-RELOAD per-surface decision: throughput recompute is expensive → one-click, not auto.
- License downgrade preserves persisted rule set (err-license-downgrade) — unchanged.
- Dependencies: Slice 1 (auto-save mechanism). Builds the throughput-reload wiring Slice 2 echoes.

---

## US-5077-02: Auto-save + auto-refresh State Mappings (retire the "must reload" hint)

`job_id: job-commit-intent-no-button` | Slice 2 | Must Have

### Elevator Pitch

- **Before:** Priya regroups Doing states, clicks Save, then reads an Alert ("After saving, a data reload is needed") and has to refresh manually for the change to take effect.
- **After:** Priya groups "Doing" + "Review" into "In Progress" on `/teams/42/settings` → the mapping auto-saves and the metrics refresh automatically; the "must reload" Alert is gone.
- **Decision enabled:** Priya can decide how her team's workflow states map to flow stages and immediately trust the metrics reflect that mapping, with no two-step refresh.

### Problem

Priya, a team-admin, edits state mappings and is told by an Alert that, after saving, a data
reload is needed for the change to take effect. The two-step ceremony adds friction and doubt.

### Who

- Delivery-forecaster / team-admin | combining workflow states into named flow groups |
  wants the metrics to reflect the mapping without a manual refresh.

### Solution

Reuse Slice 1's auto-save mechanism for state mappings. Because the mappings reload is cheap,
apply D-RELOAD as automatic auto-refresh of the dependent metrics (with a one-click "Reload now"
fallback if the auto-refresh fails). Remove the "After saving, a data reload is needed" Alert.

### Domain Examples

#### 1: Happy Path — Priya groups Doing + Review into "In Progress"
Priya creates the group; it auto-saves; the metrics refresh automatically to reflect the new
mapping; no manual refresh, no reminder Alert.

#### 2: Edge Case — Priya removes a previously created group
Priya removes the "In Progress" group, restoring the original states; the change auto-saves and
the metrics refresh automatically.

#### 3: Error/Boundary — Priya adds a group with an empty name
Priya adds a mapping group with an empty name; no save fires; an inline error states the group
name is required.

### UAT Scenarios (BDD)

#### Scenario: A state-mapping change auto-saves and the dependent data refreshes without a manual step
Given Priya is a team-admin editing State Mappings on /teams/42/settings
When Priya groups the "Doing" and "Review" states into "In Progress" and stops editing
Then the change auto-saves and the dependent metrics refresh automatically
And the data reflects the new mapping with no manual refresh step
And the "After saving, a data reload is needed" alert is no longer shown

#### Scenario: An invalid mapping is held back with an inline error
Given Priya is editing State Mappings
When Priya adds a mapping group with an empty name
Then no save is fired
And an inline error explains the group name is required

#### Scenario: A failed auto-refresh offers a one-click reload (D-RELOAD fallback)
Given Priya's state-mapping change has auto-saved
And the automatic metrics refresh fails
When the failure is detected
Then an in-place alert offers a single-click "Reload now" action
And the user is never instructed to navigate elsewhere and refresh manually

### Acceptance Criteria

- [ ] A valid state-mapping edit auto-saves (reusing Slice 1's mechanism), no Save button.
- [ ] Dependent metrics refresh automatically after a successful mapping save (D-RELOAD cheap → auto).
- [ ] The "After saving, a data reload is needed" Alert is removed (grep + test confirm absence).
- [ ] An invalid mapping (empty group name) fires no save; inline error shown.
- [ ] If auto-refresh fails, an in-place one-click "Reload now" is offered (never "go elsewhere").

### Outcome KPIs

- **Who:** team-admins editing state mappings.
- **Does what:** auto-save a mapping and see metrics refresh automatically.
- **By how much:** stopgap Alert #2 removed; 1 more surface converted.
- **Measured by:** grep + test for Alert absence; acceptance test for auto-save + auto-refresh.
- **Baseline:** 2-step ceremony + "must reload" Alert present.

### Technical Notes

- **RBAC:** mappings feed the same form Save → same `disableSave` gate as Slice 1. No new authz.
- **Clients:** N/A — no API change.
- **Website:** N/A — interaction polish.
- D-RELOAD per-surface decision: mappings reload is cheap → auto-refresh (one-click fallback only on failure).
- Dependencies: Slice 1 (auto-save mechanism); reuses Slice 3's reload wiring where applicable.

---

## US-5077-04: Auto-save general portfolio settings + RBAC parity

`job_id: job-commit-intent-no-button` | Slice 4 | Should Have

### Elevator Pitch

- **Before:** Priya edits a portfolio's settings, scrolls to the bottom, and clicks Save (disabled with a tooltip when she lacks the right or exceeds the free-tier limit).
- **After:** Priya edits a valid field on the Atlas Program portfolio settings → it auto-saves with a "Saved" indicator and no Save button, and auto-save is suppressed exactly where `canUpdatePortfolioData` is false.
- **Decision enabled:** Priya manages portfolio configuration with the same frictionless commit as team settings, and the RBAC/licensing boundary is preserved without a button to police it.

### Problem

Priya, a portfolio-admin, faces the same Save ceremony on the portfolio settings form
(`ModifyProjectSettings`) as on team settings — including a Save gated on `canUpdatePortfolioData`.

### Who

- Delivery-forecaster / portfolio-admin | editing portfolio settings (and portfolio state
  mappings) | wants the team-settings frictionless commit at portfolio scope.

### Solution

Apply Slice 1's auto-save mechanism to `ModifyProjectSettings`, suppressing auto-save where
`canUpdatePortfolioData` is false (parity with `disableSave={!canUpdatePortfolioData}`, line 276).
Portfolio also embeds `StateMappingsEditor` → reuse Slice 2's auto-save + auto-refresh.

### Domain Examples

#### 1: Happy Path — Priya edits a valid portfolio field
Priya changes a valid setting on the Atlas Program portfolio; it auto-saves with a "Saved"
indicator; no Save click.

#### 2: Edge Case — Priya edits portfolio state mappings
Priya regroups states on the portfolio's embedded State Mappings; the change auto-saves and the
portfolio metrics refresh automatically (Slice 2 reuse).

#### 3: Error/Boundary — free-tier limit exceeded
On a portfolio where `canUpdatePortfolioData` is false (free tier beyond the allowed count),
no auto-save fires; the boundary is preserved (parity with the disabled Save + tooltip).

### UAT Scenarios (BDD)

#### Scenario: Portfolio settings auto-save the moment they are valid
Given Priya is a portfolio-admin editing the Atlas Program settings
When Priya changes a valid field and stops typing
Then the change auto-saves with a "Saved" indicator and no Save button

#### Scenario: Auto-save is suppressed where portfolio data updates are not permitted
Given Priya is on a portfolio where canUpdatePortfolioData is false
When Priya attempts to edit a field
Then no auto-save fires (parity with the disabled Save button and its tooltip today)

#### Scenario: Portfolio state mappings auto-save and auto-refresh
Given Priya is editing the portfolio's State Mappings
When Priya groups two states and stops editing
Then the change auto-saves and the portfolio metrics refresh automatically

### Acceptance Criteria

- [ ] A valid portfolio-settings edit auto-saves (Slice 1 mechanism), no Save button.
- [ ] No auto-save fires where `canUpdatePortfolioData` is false (RBAC/licensing parity).
- [ ] Portfolio state mappings auto-save + auto-refresh (Slice 2 reuse).
- [ ] An invalid portfolio form fires no save; inline error shown.

### Outcome KPIs

- **Who:** portfolio-admins editing portfolio settings.
- **Does what:** auto-save valid portfolio edits with RBAC parity.
- **By how much:** last settings surface converted; mechanism proven reusable (4/4 settings surfaces).
- **Measured by:** acceptance test (auto-save + RBAC-parity suppression).
- **Baseline:** Save button present on portfolio form.

### Technical Notes

- **RBAC:** suppress auto-save where `canUpdatePortfolioData` is false (ModifyProjectSettings line 276). No new authz.
- **Clients:** N/A — no API change (reuses portfolio PUT).
- **Website:** N/A — interaction polish.
- Dependencies: Slice 1 (mechanism); Slice 2 (portfolio state-mapping auto-refresh half).

---

## US-5077-05: Auto-run the new-item-creation forecast on valid input

`job_id: job-see-forecast-update-live` | Slice 5 | Should Have

### Elevator Pitch

- **Before:** Priya tweaks the new-item forecast inputs, clicks "Forecast", reads, tweaks, clicks again — jarring next to the How Many/When forecast right above it that already auto-runs.
- **After:** Priya adjusts the new-item inputs (dates + ≥1 work-item type) on the Atlas team forecast page → the forecast recomputes live with no "Forecast" button, no run on page load, and stale runs discarded.
- **Decision enabled:** Priya can explore "what if the window were wider / the target later" as a continuous conversation with the numbers instead of a tweak-click-read loop.

### Problem

Priya, a delivery-forecaster, must click "Forecast" on the new-item forecaster after every
input tweak — inconsistent with the auto-running How Many/When forecast on the same page.

### Who

- Delivery-forecaster | sizing a new-item-creation forecast | wants the numbers to track her
  thinking as she shapes inputs.

### Solution

Lift `NewItemForecaster`'s inputs into `TeamForecastView`'s already-shipped auto-run orchestration
(`hasInteractedRef` no-run-on-mount + `requestSeqRef` stale guard + `DEBOUNCE_MS` debounce +
run-only-when-valid). Remove the "Forecast" button.

### Domain Examples

#### 1: Happy Path — Priya widens the historical window
With valid inputs, Priya widens the window; the new-item forecast recomputes automatically.

#### 2: Edge Case — page just loaded
Priya opens the forecast page; no new-item run fires until she changes an input.

#### 3: Error/Boundary — inputs incomplete
Priya clears all selected work-item types; no run fires; the prior result is cleared.

### UAT Scenarios (BDD)

#### Scenario: The new-item forecast recomputes live as inputs are shaped, with no Run button
Given Priya is on the Atlas team forecast page with valid new-item inputs
And the "Forecast" button has been removed
When Priya widens the historical window (a valid change)
Then the new-item forecast recomputes automatically after the debounce window
And stale results never overwrite a fresher run

#### Scenario: No forecast runs on page load until the user engages
Given Priya has just opened the Atlas team forecast page
When no input has been changed yet
Then no new-item forecast run fires
And no result is shown until Priya changes an input

#### Scenario: Incomplete inputs produce no run
Given Priya has cleared all selected work-item types
When the inputs are incomplete
Then no run fires and the prior result is cleared

### Acceptance Criteria

- [ ] Valid new-item input change auto-runs the forecast (debounced), no "Forecast" button.
- [ ] No run fires on mount (hasInteractedRef); no result until an input changes.
- [ ] Incomplete inputs fire no run; prior result cleared.
- [ ] Stale responses are discarded; only the latest run's result is shown (requestSeqRef).

### Outcome KPIs

- **Who:** forecasters using the new-item-creation forecast.
- **Does what:** recompute the forecast live without a Run button.
- **By how much:** 1 more Run button removed; on-page consistency with How Many/When forecast.
- **Measured by:** acceptance test (auto-run + no-run-on-mount + stale-guard).
- **Baseline:** "Forecast" button present.

### Technical Notes

- **RBAC:** N/A — forecast running has no write-permission gate today; parity = no new gate.
- **Clients:** N/A — reuses `runItemPrediction` POST unchanged.
- **Website:** N/A — interaction polish.
- Reuses shipped `TeamForecastView` orchestration; no new abstraction.
- Dependencies: none on the settings track; depends only on the already-shipped forecast pattern.

---

## US-5077-06: Auto-run backtesting on valid input

`job_id: job-see-forecast-update-live` | Slice 6 | Should Have

### Elevator Pitch

- **Before:** Priya configures a backtest (rolling vs date-range, historical window), clicks "Run Backtest", reads, adjusts, clicks again.
- **After:** Priya adjusts the backtest inputs on the Atlas team forecast page → the backtest recomputes live with no "Run Backtest" button, no half-configured run on a mid-edit mode toggle, and stale runs discarded.
- **Decision enabled:** Priya can validate her forecasting model across windows and modes as a continuous exploration ("watch what happens if I widen the window") instead of a click loop.

### Problem

Priya, a delivery-forecaster, must click "Run Backtest" after every input change on a richer
input set (rolling vs date-range, historical window) — the last Run button on the page.

### Who

- Delivery-forecaster | validating the forecast model via backtest | wants live recompute
  across the backtest's richer input set without firing half-configured runs.

### Solution

Drive `BacktestForecaster`'s inputs through the same shipped auto-run orchestration, tuned for
the larger input set so a mode toggle mid-edit does not fire a half-configured run. Remove the
"Run Backtest" button.

### Domain Examples

#### 1: Happy Path — Priya adjusts the historical window
With a valid date-range backtest, Priya changes the historical window; the backtest recomputes
automatically.

#### 2: Edge Case — Priya switches rolling ↔ date-range mid-edit
Priya toggles mode while inputs are incomplete; no run fires until the chosen mode's inputs are valid.

#### 3: Error/Boundary — page just loaded
Priya opens the page; no backtest run fires until she changes an input.

### UAT Scenarios (BDD)

#### Scenario: The backtest recomputes live as inputs are shaped, with no Run button
Given Priya is on the Atlas team forecast page with a valid backtest configuration
And the "Run Backtest" button has been removed
When Priya adjusts the historical window to a new valid value
Then the backtest recomputes automatically after the debounce window
And stale runs never overwrite a fresher run

#### Scenario: Switching backtest mode mid-edit does not fire a half-configured run
Given Priya is configuring a backtest
When Priya switches from "Rolling" to "Date range" mode while inputs are incomplete
Then no run fires until all inputs for the chosen mode are valid and stable

#### Scenario: No backtest runs on page load until the user engages
Given Priya has just opened the Atlas team forecast page
When no backtest input has been changed yet
Then no backtest run fires

### Acceptance Criteria

- [ ] Valid backtest input change auto-runs (debounced), no "Run Backtest" button.
- [ ] Mode toggle mid-edit fires no run until the chosen mode's inputs are valid and stable.
- [ ] No run fires on mount (hasInteractedRef).
- [ ] Stale runs discarded; only the latest run's result is shown (requestSeqRef).

### Outcome KPIs

- **Who:** forecasters using backtesting.
- **Does what:** recompute the backtest live without a Run button.
- **By how much:** last Run button removed; full interaction-consistency (6/6 surfaces).
- **Measured by:** acceptance test (auto-run + mode-toggle guard + no-run-on-mount + stale-guard).
- **Baseline:** "Run Backtest" button present.

### Technical Notes

- **RBAC:** N/A — backtest running has no write-permission gate today; parity = no new gate.
- **Clients:** N/A — reuses `runBacktest` POST unchanged.
- **Website:** N/A — interaction polish.
- Reuses shipped `TeamForecastView` orchestration; debounce/stale-guard tuned for richer inputs.
- Dependencies: none on the settings track; depends only on the already-shipped forecast pattern.
