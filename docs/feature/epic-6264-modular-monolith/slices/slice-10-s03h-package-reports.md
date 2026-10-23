# Slice 10 — S03h Package Reports

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size S**

## Goal

Move Reports, the ninth context and already port-shaped (`IReportQueries` / `IReportCommands` / `IReportReader` / `IReportWriter`), into its own folder.

## IN

- `Services/*/Reports/**`, `Models/Reports/**`, `ReportRepository` if S01 assigns it to the owning context.
- Namespace rule: depends down only on Metrics, RBAC/licence and Platform. Nothing but API and the composition root depends on it.

## OUT

- New report types or behaviour.

## Hypothesis

Like Refinement, Reports packages with namespace changes only. **Disproved if** it needs edits.

## Acceptance criteria

- [ ] No Reports file remains in `Services/Interfaces` or `Services/Implementation`.
- [ ] Down-only rule green by namespace. "Only the composition root names `ReportRepository`" preserved.
- [ ] ARCHITECTURE.md §4 Reports row updated. Behaviour-free, tests green.

## Dependencies

- Hard: S01, S02 gate.
- Soft: S02 (Reports conforms to Metrics).

## Notes for DESIGN

The `Reports` table's JSON payload and schema versioning are untouched.

## Slicing

Carpaccio slicing happens at this Story's DESIGN. It is likely a single slice.
