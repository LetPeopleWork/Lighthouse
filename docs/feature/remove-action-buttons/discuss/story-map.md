# Story Map: remove-action-buttons (ADO #5077)

## User: delivery-forecaster (wearing team-admin / portfolio-admin hat — "Priya Raman, RTE for the Atlas train")

## Goal: Express a valid change (settings or forecast inputs) and have Lighthouse act on it immediately — persist + refresh, or recompute — without a ceremonial Save / Run button, while staying MORE sure the change took.

## Backbone

| Activity A: Shape an input | Activity B: Have intent committed | Activity C: See dependent result current | Activity D: Recover when it's not valid/green |
|---|---|---|---|
| Edit general team setting (S1) | Auto-save on valid (S1 mechanism) | Save-state "Saved" indicator (S1) | Invalid edit held + inline error (S1) |
| Edit state mapping (S2) | Auto-save mapping (S2) | Auto-refresh metrics (S2, D-RELOAD cheap) | Failed save → Retry, edits kept (S1) |
| Edit forecast filter rule (S3) | Auto-save rule (S3, premium) | One-click "Reload throughput now" (S3, D-RELOAD expensive) | RBAC: non-admin read-only, no auto-save (S1/S3/S4) |
| Edit portfolio setting (S4) | Auto-save portfolio (S4) | Portfolio metrics current (S4) | License downgrade preserves rule set (S3) |
| Shape new-item forecast inputs (S5) | Auto-run on valid (S5, shipped pattern) | Live forecast numbers (S5) | No run on mount / incomplete inputs (S5/S6) |
| Shape backtest inputs (S6) | Auto-run on valid (S6, shipped pattern) | Live backtest numbers (S6) | Stale runs discarded; mode-toggle mid-edit no run (S6) |

---

## Walking Skeleton

**N/A — brownfield.** All four surface groups already work end-to-end with explicit
Save / Run buttons. There is no greenfield end-to-end flow to thread. The "thinnest
slice" concept is replaced by **Slice 1 as the mechanism-introducing linchpin**: it
ships the reusable auto-save mechanism on the simplest real surface, and Slices 2-4
consume it. Forecast track (5-6) reuses the already-shipped `TeamForecastView` auto-run
orchestration. (Locked in `wave-decisions.md`; confirmed by user.)

---

## Releases (sliced by outcome, not by feature grouping)

### Settings track (sequenced: 1 → 3 → 2 → 4)

- **Slice 1 — Auto-save general team settings (LINCHPIN).** Outcome: a valid general
  team-settings edit persists with a "Saved" indicator and no Save button. KPI: one fewer
  surface requiring an explicit Save click; mechanism validated.
- **Slice 3 — Auto-save + auto-refresh Forecast Filter (premium).** Outcome: filter rule
  edits auto-save and offer one-click "Reload throughput now"; the 53e6287e stopgap Alert
  is deleted. KPI: stopgap Alert #1 removed (highest felt-value-per-effort debt retirement).
- **Slice 2 — Auto-save + auto-refresh State Mappings.** Outcome: mapping edits auto-save
  and auto-refresh metrics; the "must reload" Alert is deleted. KPI: stopgap Alert #2 removed.
- **Slice 4 — Auto-save general portfolio settings + RBAC parity.** Outcome: the mechanism
  generalises to the portfolio form with `canUpdatePortfolioData` parity. KPI: last
  settings surface converted; mechanism proven reusable.

### Forecast track (parallel: 5 → 6; gated only on the shipped pattern, NOT on Slice 1)

- **Slice 5 — Auto-run new-item-creation forecast.** Outcome: new-item forecast recomputes
  live; "Forecast" button removed. KPI: one fewer Run button; on-page consistency with the
  How Many / When forecast.
- **Slice 6 — Auto-run backtesting.** Outcome: backtest recomputes live; "Run Backtest"
  button removed. KPI: last Run button removed; full interaction-consistency.

---

## Priority Rationale

Because each job's ODI gap is small (1 — honest polish/debt-retirement, not a capability
gap), slices are sequenced by **learning + debt-retirement leverage and dogfood cadence**,
NOT by raw opportunity score (per `jtbd-opportunity-scores.md`).

1. **Slice 1 first** — dependency forces it: introduces and validates the auto-save
   mechanism on the simplest surface; unblocks 2-4. Highest learning leverage and the
   riskiest slice (linchpin — review carefully before 2-4 build on it).
2. **Slice 3 next (before 2)** — retires the explicit 53e6287e stopgap debt the user
   called out; highest felt-value-per-effort once the mechanism exists. Builds the
   throughput-refresh (D-RELOAD one-click) wiring that Slice 2's pattern echoes.
3. **Slice 2** — retires the second "must reload" hint; completes team-settings convergence.
4. **Slice 4** — generalises the mechanism to the portfolio form; confirms reusability.
5. **Slices 5 & 6 (parallel track)** — independent of the settings track (different module,
   already-shipped pattern); a second contributor can run them in parallel. 5 before 6
   (simpler input set warms up the pattern-reuse before 6's richer rolling/date-range inputs).

Value x Urgency / Effort (1-5 scale):

| Slice | Value | Urgency | Effort | Score | Notes |
|---|---|---|---|---|---|
| 1 | 4 | 5 | 2 | 10.0 | Linchpin; unblocks 2-4. |
| 3 | 4 | 4 | 2 | 8.0 | Retires named stopgap debt (53e6287e). |
| 2 | 3 | 3 | 2 | 4.5 | Retires "must reload" Alert. |
| 4 | 3 | 2 | 2 | 3.0 | Generalisation; confirms reuse. |
| 5 | 3 | 2 | 1 | 6.0 | Reuses shipped pattern; low effort. |
| 6 | 3 | 2 | 2 | 3.0 | Richer inputs. |

Tie-break order applied: linchpin (S1) > riskiest-assumption (S1 mechanism) > highest
debt-retirement value (S3).
