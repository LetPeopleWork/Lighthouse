# Slice 16 — S05d Role-specific persistence ports for Portfolio

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size M**

## Goal

Replace `IRepository<Portfolio>` (~35 injections, plus the existing `IPortfolioRepository`) with intention-revealing readers and stores.

## IN

- Ports such as `IPortfolioReader.ForConnection(id)` and `IPortfolioStore.Save(portfolio)`, named per S01.
- No `IQueryable` / `Expression` / `Func` in the signatures.
- Remove the files that switch over from both S05a baselines.

## OUT

- Feature and Delivery repositories. ADR-163's archived-delivery narrowed port already exists and is the model to follow.

## Hypothesis

As in S05c. **Disproved if** consumers need ad-hoc predicates.

## Acceptance criteria

- [ ] No production consumer of `IRepository<Portfolio>` outside the composition root.
- [ ] Ports free of `IQueryable`, `Expression` and `Func`. S05a lists shrink in the same PR.
- [ ] Behaviour-free, tests green, mutation score held.

## Dependencies

- Hard: S05a.
- Soft: S05c (same pattern), S01.

## Notes for DESIGN

Portfolio is concurrency-tokened. Keep token handling intact.

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN.
