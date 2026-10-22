# Slice 36 — FE4 Per-module API ports

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size L** (split per module at DESIGN) · Mirrors backend S05 and S07b

## Goal

Replace the 33-member `IApiServiceContext` with narrow per-module ports, so each consumer, and each test, sees only what it uses.

## IN

- `useContext(ApiServiceContext)` has **75** production consumers (verified 2026-10-22). They move to per-module port hooks or contexts.
- Test doubles per port. Today `tests/MockApiServiceProvider.ts` is 518 lines with 196 `vi.fn` and is imported by 133 test files, so every test mocks everything.
- Move `deserializeTeam/Portfolio/Features` out of `BaseApiService`.
- Split the generic `IMetricsService<T>` (~33 methods, `MetricsService.ts` 964 lines) and its `BaseMetricsService` inheritance into composition, mirroring S07b.

## OUT

- The **TanStack Query migration**, which is its own Epic and a behaviour change.

## Hypothesis

Consumers use one to three services each. **Disproved if** many need five or more. That signals oversized components, which goes to FE5.

## Acceptance criteria

- [ ] `grep -rln "useContext(ApiServiceContext)" src | grep -v test` count reaches 0 (per-module progress is visible).
- [ ] The print route's `Pick`-based ports keep their runtime key tests (§12).
- [ ] Metrics service split. Composition, not inheritance.
- [ ] Behaviour-free, tests green, Stryker score held.

## Dependencies

- Soft: FE1, FE3, S07b (shape to mirror).

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN (one module's port per slice).
