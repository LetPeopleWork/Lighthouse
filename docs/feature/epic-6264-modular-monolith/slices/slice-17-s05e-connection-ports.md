# Slice 17 — S05e Role-specific persistence ports for WorkTrackingSystemConnection

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size M**

## Goal

Replace `IRepository<WorkTrackingSystemConnection>` with intention-revealing readers and stores.

## IN

- Ports named per S01. Consumers include WizardsController, WorkTrackingSystemConnection(s)Controller, PortfoliosController, TeamsController, ConnectionSaveService and the demo gate.
- No `IQueryable` / `Expression` / `Func`. S05a lists shrink.

## OUT

- Connector capability split (S06).

## Hypothesis

As in S05c. **Disproved if** consumers need ad-hoc predicates.

## Acceptance criteria

- [ ] No production consumer of `IRepository<WorkTrackingSystemConnection>` outside the composition root.
- [ ] Ports free of `IQueryable`, `Expression` and `Func`. S05a lists shrink in the same PR.
- [ ] Secrets stay encrypted on save (`PreprocessDataBeforeSave` path unchanged). Behaviour-free, tests green.

## Dependencies

- Hard: S05a.
- Soft: S05c, S05d.

## Notes for DESIGN

Connection saves carry encrypted secrets and a concurrency token. The store must go through the same `SaveChangesAsync` path.

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN.
