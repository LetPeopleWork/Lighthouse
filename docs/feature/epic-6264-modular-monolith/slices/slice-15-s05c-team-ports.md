# Slice 15 — S05c Role-specific persistence ports for Team

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size M**

## Goal

Replace `IRepository<Team>` (~37 injections) with intention-revealing readers and stores, and keep EF semantics in the adapter.

## IN

- Ports such as `ITeamReader.ForConnection(id)` and `ITeamStore.Save(team)`. Final names come from the S01 glossary and naming conventions.
- No `IQueryable` / `Expression` / `Func` in the port signatures.
- Remove the files that switch over from both S05a baselines, e.g. ConnectionSaveService and WorkTrackingSystemConnectionUsage (Team half), plus the controllers that injected `IRepository<Team>`.

## OUT

- Portfolio (S05d), Connection (S05e).

## Hypothesis

A handful of named queries cover every Team consumer. **Disproved if** consumers need ad-hoc predicates. That signals a missing read model, which is recorded for S12.

## Acceptance criteria

- [ ] `grep -rn "IRepository<Team>"` finds no production consumer outside the composition root.
- [ ] Ports carry no `IQueryable`, `Expression` or `Func`.
- [ ] S05a lists shrink in the same PR.
- [ ] Behaviour-free, tests green, mutation score held.

## Dependencies

- Hard: S05a.
- Soft: S01, S07c (Team aggregate shape).

## Notes for DESIGN

- Team is a concurrency-tokened aggregate (ARCHITECTURE.md §7). Stores must keep the token handling and `SaveWithRetry` scoping intact.
- Team ids collide with Feature ids, so never key a cache by bare id.

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN (e.g. one consumer group per slice).
