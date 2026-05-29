# Journey (Visual) — remove-action-buttons (ADO #5077)

Persona: **delivery-forecaster** wearing the team-admin / portfolio-admin hat
(concrete actor: *Priya Raman, RTE for the Atlas train, who also admins the Atlas
team's settings*).

Two jobs, two flow families:

- **Job A — Commit my intent without a ceremonial button** (settings surfaces:
  general team settings, State Mappings, Forecast Filter, portfolio settings).
- **Job B — See my forecast update as I shape its inputs** (forecast surfaces:
  new-item-creation forecast, backtesting).

> This is a brownfield refinement. The flows already work; the journey removes the
> ceremonial Save / Run click and replaces its reassurance with auto-feedback.

---

## Emotional arc (one-liner)

**From low-grade doubt about a vanished ritual ("did that actually save? where did
my error go without a Save button?") to quiet, continuous confidence — the calm
save-state indicator and always-visible inline error carry the reassurance the Save
click used to, and the forecast numbers track the user's thinking instead of waiting
for a Run click.**

| Phase | Job A (settings) | Job B (forecast) | Design lever |
|---|---|---|---|
| Entry | Trained habit: "I'll change it, then click Save." Mild unease at no button. | "I tweak, I click Run, I read." | Sibling auto-running How Many/When forecast already on-page models the new behaviour (habit easing). |
| Middle | Edits a field → pauses → sees "Saving…" then "Saved". The doubt resolves. | Adjusts window/dates → numbers recompute live, debounced. | Save-state indicator (Nielsen #1 visibility of status); debounce + stale-guard. |
| Error | Field goes invalid → no save fires, inline error is primary feedback, last valid value stays persisted. | Inputs incomplete → no run, the why is shown; no flicker. | Inline-error-as-primary-channel; run-only-when-valid gate. |
| Exit | "I changed it, it took, the screen is current — and the obsolete 'you must refresh' hint is gone." | "The numbers followed my thinking; stale runs never overwrote fresh." | Auto-refresh / one-click Reload now (D-RELOAD); requestSeqRef stale-guard. |

---

## Flow A — Commit intent on a settings surface (general settings shown; mappings/filter add a refresh step)

```
[Edit a field]      [Stop typing]        [Valid?]                 [Persisted]
 name / throughput   debounce window      ┌── yes ─► auto-save ──► "Saved" indicator
 history / mapping /  (≈300ms)            │         (handleSave)    survives refresh
 filter rule                              │                         + dependent data
   Feels: habitual    Feels: "will it?"   └── no ──► HOLD, no save  refreshed (mappings/
   Sees: value edits  Sees: "Saving…"              inline error    filter) OR one-click
                                                   stays visible    "Reload now" (D-RELOAD)
                                                   last valid kept   Feels: confident
                                                   Feels: guided     Sees: current screen
```

### TUI / DOM mockup — general settings (Slice 1)

```
+-- Team Settings: Atlas ---------------------------------------------+
|  Name           [ Atlas Delivery Team            ]                  |
|  Throughput hist[ 90 ] days                                         |
|  ...                                                                |
|                                                                     |
|                                  ${saveState}  <- "All changes saved"|
|                                  (was: [ Save ] button)             |
+--------------------------------------------------------------------+
   ${saveState} ∈ { "Saving…", "All changes saved", "Couldn't save — Retry" }
```

### DOM mockup — invalid mid-edit (error path a)

```
+-- Team Settings: Atlas ---------------------------------------------+
|  Name           [                                ]  <- emptied      |
|  ⚠ Name is required.   <- inline error = PRIMARY feedback           |
|  ...                                                                |
|                                  ${saveState} = "All changes saved" |
|                                  (last valid name still persisted)  |
+--------------------------------------------------------------------+
   No save fired. The form does NOT silently lose the prior valid value.
```

### DOM mockup — save failure (error path b)

```
+-- Team Settings: Atlas ---------------------------------------------+
|  Throughput hist[ 60 ] days   <- valid edit                        |
|                                                                     |
|  ⚠ Couldn't save your changes (server error). [ Retry ]            |
|     Your edits are not lost.                                        |
|                                  ${saveState} = "Couldn't save"     |
+--------------------------------------------------------------------+
   Edits retained in the form; one-click Retry re-fires handleSave.
```

### DOM mockup — State Mappings (Slice 2) and Forecast Filter (Slice 3), D-RELOAD

```
+-- State Mappings -------------------------------------------------+
|  Combine multiple Doing states into one named group...           |
|  [ Doing+Review  → "In Progress" ]   (auto-saved)                |
|                                                                  |
|  ✓ Saved. Reloading metrics…    <- auto-refresh (cheap)          |
|  --- OR, where recompute is expensive: ---                       |
|  ✓ Saved. [ Reload now ] to apply to metrics.  <- D-RELOAD       |
|  (REMOVED: "After saving, a data reload is needed…" Alert)       |
+------------------------------------------------------------------+

+-- Forecast Filter (Premium) -------------------------------------+
|  Exclude items where  [ Type ][ equals ][ Bug ]  (auto-saved)    |
|                                                                  |
|  ✓ Saved. [ Reload throughput now ]  <- D-RELOAD (expensive      |
|     recompute → one-click, not silent auto)                      |
|  (REMOVED: forecast-filter-takeeffect-hint Alert, commit 53e6287e)|
+------------------------------------------------------------------+
```

### DOM mockup — non-admin (error path d, RBAC parity)

```
+-- Team Settings: Atlas (viewer) ----------------------------------+
|  Name           [ Atlas Delivery Team            ] (read-only)    |
|  ...                                                              |
|  No save-state indicator; no auto-save fires.                    |
|  (parity with today's disabled Save button)                      |
+------------------------------------------------------------------+
```

---

## Flow B — Recompute a forecast on valid inputs (new-item / backtest)

```
[Page loads]      [User changes an input]   [All inputs valid?]      [Result]
 NO run on mount   from/to/target + ≥1 type  ┌── yes ─► debounce ──► forecast
 (hasInteractedRef) (new-item)               │         (≈300ms) +    recomputes;
   Feels: neutral   start/end + window        │         stale-guard   stale runs
   Sees: empty      (backtest)                │         (requestSeq)  discarded
                      Feels: exploratory       └── no ──► no run,      Feels: in flow
                      Sees: clears prior result          shows why     Sees: live numbers
```

### TUI mockup — new-item forecast (Slice 5)

```
+-- New Work Items Creation Forecast -------------------------------+
|  From [2026-04-01]  To [2026-05-01]  Target [2026-07-01]          |
|  Work item types: [x] Story [ ] Bug                               |
|                                                                  |
|  (was: [ Forecast ] button — REMOVED)                            |
|  ► P85: 42 items by target   <- recomputes live on input change  |
+------------------------------------------------------------------+
   No run until the user touches an input (hasInteractedRef).
   Stale responses discarded (requestSeqRef).
```

### TUI mockup — backtest (Slice 6)

```
+-- Forecast Backtesting -------------------------------------------+
|  Mode: ( ) Rolling  (•) Date range                               |
|  Start [2026-01-01]  End [2026-04-01]  Hist window [90d]          |
|                                                                  |
|  (was: [ Run Backtest ] button — REMOVED)                        |
|  ► MAPE 12% over window   <- recomputes live; no mid-edit flash  |
+------------------------------------------------------------------+
   Mode toggle mid-edit does NOT fire a half-configured run.
```

---

## Shared artifacts (every ${variable} has one source — see registry)

| Artifact | Source of truth | Consumers |
|---|---|---|
| `formValid` / `inputsValid` | `useModifySettings` (`validateForm`) | auto-save gate, save-state indicator |
| `${saveState}` (idle/saving/saved/failed) | NEW save-state machine in `useModifySettings` | settings save indicator (replaces `ValidationActions` icons) |
| `validationError` / `validationTechnicalDetails` | `useModifySettings.handleSave` | inline error (primary feedback channel) |
| `disableSave` / `canUpdatePortfolioData` / `isTeamAdmin(teamId)` | `useRbac()` via `IRbacAdministrationService` | auto-save suppression (RBAC parity), read-only rendering |
| `hasInteractedRef` | `TeamForecastView` (reference pattern) | on-mount run suppression (Job B) |
| `requestSeqRef` + `DEBOUNCE_MS` | `TeamForecastView` (reference pattern) | stale-response guard + debounce (Job B) |
| reload cost (cheap → auto / expensive → one-click) | per-surface design decision (D-RELOAD) | mappings auto-refresh; filter one-click "Reload throughput now" |

---

## Integration checkpoints

1. **Slice 1 is the linchpin.** The save-state machine + auto-save gate it introduces
   in `useModifySettings` must be reviewed before Slices 2-4 consume it.
2. **RBAC parity is invariant across 1, 2, 3, 4.** Auto-save fires only where the old
   Save was enabled (`!disableSave` / `canUpdatePortfolioData` / `isTeamAdmin`).
3. **D-RELOAD per-surface decision** (Slices 2, 3): mappings reload is cheap →
   auto-refresh; filter throughput recompute is expensive → one-click "Reload now".
   Neither may instruct the user to navigate away and refresh manually.
4. **Forecast pattern is already shipped** (Slices 5, 6 reuse `TeamForecastView`
   orchestration; no new abstraction).
