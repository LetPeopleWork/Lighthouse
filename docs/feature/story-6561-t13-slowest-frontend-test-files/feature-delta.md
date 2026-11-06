# Feature: story-6561-t13-slowest-frontend-test-files

Story #6561 · T13 The slowest frontend test files run faster, and the frontend budget comes down with them · part of
Epic #6506 "Fast, reliable test suites". Catalogue: [Epic workspace](../epic-6264-modular-monolith/feature-delta.md) ·
Brief: [slice-46](../epic-6264-modular-monolith/slices/slice-46-t13-slowest-frontend-test-files.md).

Created 2026-11-06 on the maintainer's word, as a separate Story after T4 #6462 pushed. DISCUSS is the brief plus this
file, following FE7's framing, because the Story applies an existing contract to more files. Waves start at DESIGN.

## Wave: DISCUSS / [REF] Inherited

Link, do not copy: `docs/evolution/story-6457-fe7-fast-frontend-tests/evolution.md` and
`docs/feature/story-6457-fe7-fast-frontend-tests/feature-delta.md` (the behaviour-free contract for test rewrites);
T4's derivation in `docs/feature/story-6462-t4-test-budgets-fail-the-build/feature-delta.md`.

- job_id: infrastructure-only. infrastructure_rationale: the budget cannot fall while seven files take 17 to 27 s.
- Tests change by design; production code, behaviour and pixels do not.
- Cross-cutting: RBAC, Lighthouse-Clients, Website, usage-data event, UI sketch, Terminology: N/A, because only test
  files and the budget file change. Context map: no change. Release notes: none.

## Baseline re-count

Measured 2026-11-06 on the 7 plain runs of T4 (see the brief's table). Frontend budget 56 s, empty allow-list.

## Checklist

- [ ] Behaviour-free: production code and pixels unchanged
- [ ] Every removed assertion has a row saying where it lives on
- [ ] Coverage not lower (`pnpm test:coverage`, opt-in for exactly this reason)
- [ ] Each call site proven by a deliberate break
- [ ] Ratchet: the budget falls in this Story
- [ ] Mutation last, on frozen code; the touched area's score does not drop
- [ ] `pnpm build` and `pnpm test` clean before review
