# Slice 05 — S03c Package Forecasting

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size M**

## Goal

Move the Monte-Carlo forecasting context into its own folder.

## IN

- `Services/*/Forecast/**` (`ForecastService`, `ForecastRunPlan`, draw streams, reality check, data-sufficiency policy).
- The forecast-filter rule engine.
- `Models/Forecast/**` types that only Forecasting and its API use.
- Namespace ArchUnit rule for Forecasting.

## OUT

- `ForecastRunPlan`'s S107 (S07d). The `*Updater` pipelines (Portfolio/Delivery, S03d).

## Hypothesis

Forecasting is already nearly self-contained. **Disproved if** packaging needs edits beyond namespaces in more than a handful of files.

## Acceptance criteria

- [ ] No Forecasting file remains in `Services/Interfaces` or `Services/Implementation`.
- [ ] Namespace rule green. Forecast DTOs moved after S12c where needed.
- [ ] Moves separate from edits. ARCHITECTURE.md §4 updated.
- [ ] Behaviour-free (forecast results identical for seeded draws), tests green, mutation score held.

## Dependencies

- Hard: S01, S02 gate.
- Soft: S03b.

## Notes for DESIGN

Forecasting is a good second candidate after the pilot, being small and well-tested.

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN.
