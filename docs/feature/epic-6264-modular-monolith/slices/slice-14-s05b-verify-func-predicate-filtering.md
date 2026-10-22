# Slice 14 — S05b Verify in-memory filtering via `Func` predicates; fix if confirmed

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size S–M**

## Goal

Find out whether `GetByPredicate` / `Exists` (which take `Func<T,bool>`) load every row and filter in memory. Fix it if they do, with identical results.

## IN

- A provider-backed test (SQLite, and Postgres via the shared container) that captures the generated SQL or the row count for `GetByPredicate` and `Exists`. EF InMemory translates nothing, so it cannot answer this.
- If confirmed: switch the signatures to `Expression<Func<…>>`, or move hot callers to intention-revealing methods. Results stay identical.

## OUT

- A general port redesign (S05c–e).

## Hypothesis

The `Func` overloads filter in memory. **Disproved if** the SQL shows a `WHERE`. Then record "verified: translated" and close with no code change.

## Acceptance criteria

- [ ] Evidence recorded for both methods on SQLite and Postgres.
- [ ] If confirmed: same rows and order for every caller (existing tests plus a provider-backed one), and the change noted in ARCHITECTURE.md §8.
- [ ] If refuted: the brief's claim is closed in writing.

## Dependencies

- Soft: S05a.

## Notes for DESIGN

This is the one Story where a runtime effect (faster queries) is acceptable. Results must not change. Use the shared Postgres container with a fresh database per case.

## Slicing

Carpaccio slicing happens at this Story's DESIGN: verify first, fix second.
