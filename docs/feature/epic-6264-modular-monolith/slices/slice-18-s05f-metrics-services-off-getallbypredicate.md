# Slice 18 — S05f Move the two metrics services off `GetAllByPredicate`

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size M** · **May be absorbed by S02. S02's DESIGN decides and records it.**

## Goal

`TeamMetricsService` (14 calls) and `PortfolioMetricsService` (13 calls) read through named, provider-translatable read-port methods instead of chaining `.Where/.Any/.Select` on `GetAllByPredicate`.

## IN

- Named read methods for the query shapes in use: closed in range, started, WIP at date, blocked-eligible, transitions for item ids, and so on.
- Both files leave the S05a baseline.

## OUT

- `BaseMetricsService` composition (S07b). Controller logic (S12a).

## Hypothesis

The 27 calls collapse to around 10 named queries. **Disproved if** most calls are unique shapes. Then the read side wants a query object per capability (S12a pattern) rather than repository methods.

## Acceptance criteria

- [ ] Both metrics services absent from the S05a list.
- [ ] Each read method proven translatable on a provider-backed test (`.Any()` per Feature included).
- [ ] Metrics responses unchanged. Mutation score held.

## Dependencies

- Hard: S05a.
- Soft: S02 (may absorb this), S05b.

## Notes for DESIGN

EF InMemory translates nothing, so a predicate SQLite or Postgres cannot translate still passes there. That is the reason this Story exists.

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN (Team first, then Portfolio).
