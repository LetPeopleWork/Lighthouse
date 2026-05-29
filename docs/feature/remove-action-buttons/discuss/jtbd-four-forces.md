# Four Forces — remove-action-buttons (ADO #5077)

The anxiety force is the rich one here. When you take away a Save/Run button you take
away the user's confirmation ritual. The whole feature lives or dies on replacing that
ritual's reassurance with something better (auto-feedback), not just deleting it.

---

## Job A — Commit my intent without a ceremonial button

### Push (the frustration driving the user here)

- After editing a team/portfolio setting, the user must scroll/hunt to the bottom of a
  long settings form and click one Save button (`ValidationActions` in
  `ModifyTeamSettings.tsx` / `ModifyProjectSettings.tsx`). Easy to forget; easy to
  navigate away and silently lose the edit.
- State Mappings and the Forecast Filter make it WORSE: a two-step ceremony. The
  StateMappings editor literally shows an Alert "After saving, a data reload is needed
  for these changes to take effect" (`StateMappingsEditor.tsx` line 109-111). The
  Forecast Filter shows the stopgap Alert "Filter changes only take effect after you
  **save these settings** and then **refresh throughput data**"
  (`ForecastSettingsComponent.tsx` line 100-109, the `forecast-filter-takeeffect-hint`
  added in commit 53e6287e). The user has to remember a second, manual refresh step.

### Pull (the outcome the user is hoping for)

- Change it, it sticks, the screen is current — no button, no second step. The proof
  already exists elsewhere in the product (How Many / When auto-forecasts), so the pull
  is "make the rest of the app behave like the part that already feels good."

### Anxiety (what could go wrong with the new solution) — the crux

- **"Did it actually save?"** — Removing the Save button removes the click-confirmation.
  Need an explicit, calm save-state signal (saving… / saved) so the user is MORE sure,
  not less.
- **"What if it auto-saves something half-typed?"** — e.g. a half-renamed team, a state
  mapping with an empty name, a throughput-history of `0`. The existing validators
  (`validateForm` in `useModifySettings.ts`, `validateStateMappings`, the PUT 400
  paths) MUST gate the auto-save: only fire on a fully-valid form, debounced so
  mid-typing states don't persist.
- **"Where did the error go without a Save button to click?"** — Today an invalid form
  just leaves Save disabled / surfaces `validationError`. Without a Save button the
  user has no obvious "try again" affordance, so the inline error must become the
  primary, always-visible feedback channel.
- **"Did my auto-save clobber a colleague's concurrent edit?"** — settings are
  team-admin-scoped and low-concurrency, but worth acknowledging as a red card for
  DESIGN.

### Habit (the existing workflow that resists adoption)

- Users are trained by every form on the web to "make changes, then click Save." The
  absence of the button can momentarily read as "this form is broken / read-only."
  Mitigation: the save-state indicator and (for the data-refresh surfaces) the
  disappearance of the now-obsolete "you must refresh" Alerts.
- Team admins expect "set it once, click save, walk away" — auto-save must not feel
  like it's nagging or firing constantly while they think.

---

## Job B — See my forecast update as I shape its inputs

### Push

- `NewItemForecaster` and `BacktestForecaster` each force a tweak → click "Forecast" /
  "Run Backtest" → read → tweak → click again loop (`NewItemForecaster.tsx` line 197,
  `BacktestForecaster.tsx` line 361). The How Many / When forecast right above them on
  the same page already auto-runs (`TeamForecastView.tsx`), so the inconsistency is
  jarring within a single screen.

### Pull

- The numbers track the user's thinking; scenario exploration becomes continuous. The
  exact, already-shipped, already-trusted feel of the manual forecaster.

### Anxiety

- **"Will it fire while I'm still picking dates / types and burn requests or flash
  half-results?"** — Mitigated by the proven pattern: `DEBOUNCE_MS` debounce +
  `requestSeqRef` stale-response discard + only-run-when-all-inputs-valid guard
  (new-item needs from/to/target + ≥1 work-item type; backtest needs start/end +
  valid historical window).
- **"Will a forecast run on page load before I've touched anything?"** — Mitigated by
  the `hasInteractedRef` on-mount suppression already in the reference pattern. No
  result appears until the user engages.
- **"Will auto-run make it harder to tell a stale result from a fresh one?"** —
  Existing `onClearForecastResult` / `onClearBacktestResult` clearing-on-input-change
  is already wired; auto-run replaces the button, not the clearing.

### Habit

- Same "I expect to click Run" muscle memory as Job A's Save habit. Eased because the
  sibling forecaster on the same page already behaves this way, so the user has a
  live, on-screen model of the new behavior.

---

## Forces-derived scenario seeds (for DISTILL, Phase 6)

| Force | Scenario seed |
|---|---|
| A/Anxiety "did it save?" | Given a valid edit, When the user stops typing, Then a "Saved" state appears without any click |
| A/Anxiety "half-typed" | Given an edit that makes the form invalid (empty team name), When the user pauses, Then nothing is persisted and the specific field error is shown |
| A/Anxiety "error went where?" | Given an invalid value, When auto-save would have fired, Then the inline error is the visible, sufficient feedback (no reliance on a Save button) |
| A/Push "two-step gone" | Given a state-mapping / forecast-filter change saved automatically, Then the dependent data refreshes without a manual refresh step and the obsolete "you must refresh" hint is gone |
| A/RBAC | Given a viewer (non-team-admin), When they open settings, Then no auto-save fires and inputs are read-only (parity with today's disabled Save) |
| B/Anxiety "fires too soon" | Given partial backtest inputs, When the user is mid-edit, Then no run fires until all inputs are valid and stable for the debounce window |
| B/Anxiety "runs on load" | Given the forecast page just loaded, Then no new-item / backtest run fires until the user changes an input |
| B/Pull | Given valid new-item inputs, When the user widens the historical window, Then the forecast recomputes automatically and stale results never overwrite fresh ones |
