# Slice 02 — S02 Metrics pilot (go/no-go gate)

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size L**

## Goal

Package the Metrics context per the S01 ADR and use what that teaches to decide whether and how the other contexts follow.

## IN

- Move Metrics into its own folder and namespace: `*MetricsService`, `PercentileCalculator`, `PercentileFamilies`, `XmRCalculator`, `Baseline*`, `SleRiskCalculator`, the `*SeriesQuery` and `*SnapshotWriter` files, `MetricsCacheKey`, `BlockedCountSeriesBuilder`, and so on. S01 confirms the exact membership.
- Replace the Metrics type-set ArchUnit rules with namespace rules in the same PR.
- Move Metrics off `IRepository<T>` onto read ports. S05f overlaps here, and this Story's DESIGN decides whether S05f is absorbed and records it.
- Amend the ADR with lessons learned and an explicit **go / go-with-changes / stop** for S03*.

## OUT

- `BaseMetricsService` → composition (S07b). Metrics controllers (S12a). Moving Metrics DTOs out of `Models` unless S12c has already made them pure mapping.

## Hypothesis

The layout works for the most entangled context. **Disproved if** the move needs widespread edits beyond namespaces, or produces no clearer boundary. Then the gate says stop, or change the layout.

## Acceptance criteria

- [ ] Metrics files under the ADR's folder. Namespace-based ArchUnit rule in place, type-set rule removed.
- [ ] Read ports instead of `IRepository<T>` (or S05f explicitly keeps that).
- [ ] Metrics API responses unchanged, all tests green, Metrics mutation score not lower.
- [ ] ADR amended with lessons and the gate decision. ARCHITECTURE.md §4 Metrics row updated.

## Dependencies

- Hard: S01.
- Soft: S05a (the guard shows progress), S12c (before Metrics DTOs move).

## Notes for DESIGN

- Metrics has the most `GetAllByPredicate` calls (27 across the two services). Prove the read ports on a provider-backed test, because EF InMemory translates nothing.
- Keep moves and edits in separate commits. Stryker line-range configs go stale on moves, so run mutation last.

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN (e.g. move + rule first, read ports next, ADR amendment last).
