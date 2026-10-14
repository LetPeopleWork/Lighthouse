# Slice 03 — The next six slow frontend files

**Goal**: `BaseMetricsView.loading`, `ModifyTeamSettings.refinementNeed`, `ModifyTeamSettings.readiness`, `RefinementView.comments`, `DataGridBase.paging` and `RefinementView.votes` drop from ~236 s to ≤70 s summed on CI, and the frontend job median returns to ≤12 min.

**Owner story**: US-03. **Effort**: 1 day. **Reference class**: test-performance refactor.

**Learning hypothesis**
- Disproves "one pattern explains the slow new files" if slice 02's fix does not carry over and each file needs its own cause.
- Confirms it if the same change applies; the frontend KPI then shows whether anything else remains.

## IN scope
1. Apply slice 02's pattern; profile only where it does not fit.
2. D7 for each file.
3. Measure `Verify Frontend / frontend` over ≥5 `main` runs after landing.

## OUT scope
- Older files that did not grow.
- Vitest pool / worker / coverage config.

## Acceptance criteria
AC-03.1..AC-03.3 in `feature-delta.md`. If AC-03.3 misses, name the remaining cause in a follow-up ADO item.

## Dependencies
Slice 02.

## Taste tests
Six files but one pattern; not identical to slice 02 in scale only, because it tests whether the pattern generalises.
