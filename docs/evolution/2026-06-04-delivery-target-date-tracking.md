# Evolution: delivery-target-date-tracking

- **Date finalized**: 2026-06-04
- **ADO**: Epic 3993 (Delivery Metrics) follow-up — Stories #5174 (US-01), #5175 (US-02); #5176 (US-03 burnup) **Removed** at DESIGN.
- **Status**: Shipped to `main`, CI-green (feature run 26964812182, `fd77d09`). Mutation committed (`a80230d2`).
- **Workspace (history)**: `docs/feature/delivery-target-date-tracking/`
- **Builds on**: [delivery-metrics](./../feature/delivery-metrics/feature-delta.md) (Epic 3993 — the snapshot store, forward recorder, metrics-history endpoint, and the burnup/predictability/fever charts this feature extends).

## What shipped

The delivery predictability charts are now honest when a delivery's **target date moves**. Every
target-relative metric (likelihood, chance-of-being-late, the burnup marker) is computed against
`Delivery.Date`, but the snapshot stored only the computed value — so a target move silently
re-referenced the whole recorded history (a +2-week replan made the recorded likelihood line *step
up*, reading as progress when it was goalpost-moving).

Two thin slices fixed it, forward-only:

- **US-01 (#5174)** — a nullable `TargetDateAtSnapshot` column on `DeliveryMetricSnapshot`, captured
  by the daily recorder (`snapshot.TargetDateAtSnapshot = delivery.Date`) and surfaced as an additive
  nullable field on the existing metrics-history response. The predictability **When?** view plots the
  target as a `curve:"stepAfter"` series that holds at the earlier target and steps where it moved,
  falling back to a single flat reference line when no per-day target was recorded. The dead off-axis
  `ChartsReferenceLine` was removed from the burnup.
- **US-02 (#5175)** — the **How Likely?** view marks each target change with an emphasized dot on the
  likelihood line (none when the target is constant or unrecorded); the dot's hover reveals the neutral
  date-pair (`Target moved: old → new`). The When? view carries no duplicate marker — its step line is
  the signal.

The forecaster can now say "the likelihood rose in week 4 because we moved the date (the dot, and the
When? step) — against the old target the forecast had been slipping" instead of presenting a replan as
progress.

## Key decisions (user-locked)

Full decision log with verbatim framing lives in the workspace `feature-delta.md`. Load-bearing ones:

- **D1 — Option 2 "annotate + dual reference"** (not annotate-only, not re-baseline/scenario).
- **D2 — forward-only `TargetDateAtSnapshot`**; one EF migration, past rows null, no reconstruction.
- **D3 — daily cadence**, recorder captures `delivery.Date`; no immediate snapshot on a target edit.
- **D4 — neutral date-pair** marker copy ("Target moved: old → new"), not editorial "replan, not progress".
- **DESIGN scope cut (3 slices → 2)** — the **burnup target marker was dropped entirely** (user: the
  delivery date is not wanted on the burnup; it rendered off-axis and invisible anyway). The "when vs
  target" story lives solely on the predictability charts. The fever annotation was also dropped (no
  clean time axis). US-03/#5176 Removed.
- **DESIGN rendering (D7/D8, ADR-052)** — each predictability view marks the replan in the idiom that
  fits its axes: a stepped line where the y-axis is a date (When?), a dot where it is a percentage
  (How Likely?). No duplicate marker.

## Architecture (ADR-051, ADR-052)

A thin, reuse-heavy extension — no new endpoint, RBAC, dependency, or chart.

- **ADR-051** — per-snapshot target capture: one nullable column + one recorder assignment + one
  additive DTO field, forward-only. Reuses the wide-nullable-column + daily-recorder pattern
  (ADR-048/049/050); re-affirms ADR-050's single metrics-history contract (additive nullable field →
  no clients version-gate).
- **ADR-052** — moving-target predictability rendering: the When? `stepAfter` target series (flat
  fallback when all-null), the How Likely? marks-only change-dot overlay, and derivation kept in a pure
  FE helper `deliveryTargetHistory.ts` (`steppedTargetData` / `targetChanges`) out of the components
  for testability (the UI-1 lesson). Supersedes the dropped burnup treatment.

Cross-cutting: RBAC N/A (no new permission/endpoint; inherits the premium + portfolio-read gate);
clients N/A (additive nullable field); website N/A (honesty refinement to a marketed feature).

## The EF migration trap (navigated, ledger-documented)

The new column needed a migration (`Create-Migration.ps1`, both providers). Even after generating it,
the app still threw `PendingModelChangesWarning` on startup — the migrations assembly held a **stale
compiled `ModelSnapshot`**. Fixed with `dotnet build <MigrationsProject> --no-incremental` before
re-checking (the documented ci-learnings fix). InMemory unit tests never catch this — verified by
running the app against a throwaway sqlite db to "Application started". (Same family as the
forecast-minimum-data-guard regression; the ledger rule held.)

## Quality

- **Mutation testing** — Backend **86.84%** (recorder + DTO; every feature-introduced mutation killed;
  the 5 survivors are pre-existing best-effort logging + a pre-existing DTO branch). Frontend core
  logic `deliveryTargetHistory.ts` **100%**; whole-file aggregate 75.27% is **presentational-bound**
  (MUI labels/configs + boundary error-strings on largely pre-existing chart/parser files) — accepted
  with justification (user-confirmed), matching the `state-time-cumulative-view` chart-feature
  precedent. Report: `docs/feature/delivery-target-date-tracking/deliver/mutation/mutation-report.md`.
- **Tests** — Backend 2981 / 0 warnings; frontend 3333; both builds clean; Biome clean; SonarCloud
  new-violations gate green.
- **End-to-end** — the Playwright walking skeleton renders the `target` step line live against demo
  data (the demo delivery now seeds a mid-series target replan).
- A Biome `noAccumulatingSpread` vs the project no-`push` immutability rule conflict was resolved by
  writing `targetChanges` as map→filter→map (pure, no accumulator spread).

## Permanent artifacts

- **ADR-051** — `docs/product/architecture/adr-051-per-snapshot-target-date-capture.md`
- **ADR-052** — `docs/product/architecture/adr-052-moving-target-predictability-rendering.md`
- **Application architecture delta** — `docs/product/architecture/brief.md` (`## Application Architecture — delivery-target-date-tracking`)
- **Journey** — `docs/product/journeys/delivery-target-date-tracking.yaml`
- **Job** — `job-honest-delivery-trend-when-target-moves` in `docs/product/jobs.yaml`
- **Mutation report** — `docs/feature/delivery-target-date-tracking/deliver/mutation/mutation-report.md`

## Commits

`c4fad7d6` docs(plan) · `90f92ef9` feat backend S1 (column+recorder+DTO+demo+migration) ·
`da723dee` feat FE S1 (When? step line + burnup cleanup) · `f769c71d` feat S2 (How Likely? dots) ·
`fd77d090` test(E2E) · `a80230d2` chore(mutation config + report).
