# Feature: story-6539-t12-frontend-tests-order-and-load

Story #6539 · T12 Frontend tests that do not depend on order or a quiet machine · part of Epic #6506 "Fast, reliable test
suites". Catalogue: [Epic workspace](../epic-6264-modular-monolith/feature-delta.md) ·
Brief: [slice-47](../epic-6264-modular-monolith/slices/slice-47-t12-frontend-tests-order-and-load.md).

DISCUSS: the brief, settled with the maintainer on 2026-11-07. Its final section, "Maintainer decisions", overrides the text
above it. Waves start at DESIGN.

## Wave: DISCUSS / [REF] Inherited

- The behaviour-free contract: `docs/evolution/story-6457-fe7-fast-frontend-tests/evolution.md`. Epic decision D-6264-5.
- job_id: infrastructure-only. Contexts touched: none. RBAC, Lighthouse-Clients, website, usage-data, UI sketch, Terminology,
  context map: N/A, because only test files and one CI line change, as the brief answers item by item.

## Baseline re-count

To be read at DESIGN: the failing seeds and files today, against the brief's two tests and the Reports load failures.

## Checklist

- [ ] Behaviour-free: production code unchanged
- [ ] No longer timeouts or retries as the fix
- [ ] Ratchet in the same change: shuffle in the CI frontend run
- [ ] Docs in the same change where a rule changes (`docs/ci-learnings.md`)
- [ ] Mutation N/A while only test files change, accepted by the maintainer
