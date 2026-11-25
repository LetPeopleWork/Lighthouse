# Slice 10 — S03h Package Reports

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size S**

## Goal

Move Reports, the ninth context and already port-shaped (`IReportQueries` / `IReportCommands` / `IReportReader` / `IReportWriter`), into its own folder.

## IN

- `Services/*/Reports/**`. *Amended at DESIGN (Story 6366):* `Models/Reports` (7 types) stays in the kernel, and `ReportRepository` stays with the platform repositories until S05.
- Namespace rule: depends down only on Flow metrics, Licensing, Organisation set-up data, the kernel and the platform infrastructure, along listed edges; never on Access beyond the API guards. Nothing but API, the composition root and `ReportRepository` depends on it.

## OUT

- New report types or behaviour.

## Hypothesis

Like Refinement, Reports packages with namespace changes only. **Disproved if** it needs edits.

## Acceptance criteria

- [x] No Reports file remains in `Services/Interfaces` or `Services/Implementation` other than `ReportRepository`; `Models/Reports` stays in the kernel (amended at DESIGN, Story 6366).
- [x] Down-only rule green by namespace (`ReportsModuleBoundariesArchUnitTest`, R-RP1 to R-RP6). "Only the composition root names `ReportRepository`" preserved.
- [x] ARCHITECTURE.md §4 Reports row updated. Behaviour-free, tests green.

## Dependencies

- Hard: S01, S02 gate.
- Soft: S02 (Reports conforms to Metrics).

## Notes for DESIGN

The `Reports` table's JSON payload and schema versioning are untouched.

## Done

Story 6366, 2026-11-25: three nwdes features (rules first, the move, edges and rules). The hypothesis held: every moved file changed only in its namespace and `using` lines (31 of 31).

## Slicing

Carpaccio slicing happens at this Story's DESIGN. It is likely a single slice.
