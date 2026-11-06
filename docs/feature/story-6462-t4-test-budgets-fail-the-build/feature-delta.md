# Feature: story-6462-t4-test-budgets-fail-the-build

Story #6462 · T4 Test-time budgets fail the build instead of warning · part of Epic #6506 "Fast, reliable test suites"
(a Continuous Improvement Epic). Catalogue: [Epic workspace](../epic-6264-modular-monolith/feature-delta.md) ·
Brief: [slice-44](../epic-6264-modular-monolith/slices/slice-44-t4-test-budgets-fail-the-build.md).

DISCUSS: covered at Epic level (see the Epic's Wave Decisions, D-6264-3 and D-6264-4); waves start at DESIGN.

Picked up 2026-11-06 by `/continuous-improvement`, run under `/afk`. Maintainer's options: slices wrap up with refactor
and review, mutation once at the end; nothing is pushed; rigor profile `standard`.

## Wave: DISCUSS / [REF] Inherited

Link, do not copy. From the catalogue's `feature-delta.md`:

- How to pick up a Story; Story Map (G0b); Priority Rationale point 6
- Stories → Test-infrastructure track → T4, including its hand-off from FE7
- Outcome KPIs + guardrails; Wave Decisions; Risks / Open questions
- Improvement log: T2 #6460 ("FE7 and T4")

Also: `docs/evolution/story-6457-fe7-fast-frontend-tests/evolution.md` (Follow-ups) and
`docs/evolution/story-6513-t7-drop-sonar-coverage/evolution.md`, with the T4 contract in
`docs/feature/story-6513-t7-drop-sonar-coverage/feature-delta.md`.

## Wave: DISCUSS / [REF] DoR re-run

| # | DoR item | Status | Evidence |
|---|---|---|---|
| 1 | Problem statement, domain language | PASS | The backend budget cannot go red on a breach; the limits were measured on coverage runs and are looser on today's plain run. |
| 2 | Persona | PASS | `lighthouse-maintainer` and the parallel sessions that wait for CI's answer. |
| 3 | ≥ 3 examples, real data | PASS | Backend budget 34 s with 61 listed classes; frontend budget 63 s with 9 ceilings; per-test factor 3. |
| 4 | UAT | PASS (catalogue) | Scenarios are written at DISTILL. |
| 5 | AC derived from UAT | PASS | Brief's three AC, narrowed by the scope change below. |
| 6 | Right-sized | PASS | Size S; carpaccio at DESIGN. |
| 7 | Technical notes + cross-cutting | PASS | RBAC, Clients, Website, usage data, UI sketch: N/A, because this changes CI and test tooling only and nobody uses it in the product. |
| 8 | Dependencies | PASS | Soft T2, FE7 done; T7 done, so runs since 2026-11-04 14:00 UTC are plain. |
| 9 | Outcome KPIs | PASS | CI fails on the commit that adds an over-budget unit. |

## Wave: DISCUSS / [REF] Baseline re-count

Read from the files on `main` at `87ac968e7`, 2026-11-06.

| Brief says | Today |
|---|---|
| Both CI steps pass `--warn-only` | **Frontend already enforces**: `ci_frontend.yml` passes `--warn-only` only when the repository variable `LIGHTHOUSE_BUDGET_WARN_ONLY` is `true`. **Backend still always passes it** (`ci_backend.yml:187`) and has no such switch. |
| Re-derive both budgets, the large-file limit and every ceiling | Frontend per-file budget 63 s, 9 ceilings. Backend per-class budget 34 s, 61 ceilings. Per-test `LIMIT_FACTOR` 3 (`vitest.config.ts:13`). All measured on coverage runs. |
| At least 5 main runs | 10 `main` runs since T7. Timing artifacts: 9 frontend, 8 backend. Frontend runs 42127753201 and 42127795017 stopped at the first failure, so their timings are partial; check every run for completeness before using it. |

Scope change from the FE7 hand-off: the frontend half of "fail the build" is done. T4 keeps the backend half and one
recalibration of the frontend limits on the plain CI run. The brief is corrected at close-out.

Consequence for AC2: "at least 5 main runs after the change are green" can only be read after a push. This run holds
locally, so AC2 stays open at the hold.

## Checklist

- [ ] Behaviour-free: routes, DTOs, responses, pixels and topologies unchanged; existing tests stay green unmodified
- [ ] Ratchet in the same change: the budget fails the build, and a limit only tightens
- [ ] Baselines are inline lists, so a failure names the unit
- [ ] Moves separate from edits
- [ ] Every number in the brief re-measured (see above)
- [ ] Mutation last, on frozen code; the touched area's score does not drop
- [ ] `ARCHITECTURE.md` and any ADR whose decision changed updated with the code
- [ ] One context at a time (N/A, because no module moves)
- [ ] Terminology (N/A, because no user-facing term)
- [ ] Cross-cutting re-check answered: RBAC, Lighthouse-Clients, Website, usage-data event, UI sketch
- [ ] No user-facing release notes
