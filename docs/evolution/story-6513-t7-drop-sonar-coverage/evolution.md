# Story 6513 — Drop code coverage from CI

**Finished:** 2026-11-04 · ADO User Story #6513 · Epic #6506 "Fast, reliable test suites"

## The problem

CI ran every test with coverage on, only so SonarCloud could show a figure that no quality gate reads. Each push waited
longer for a number nobody used.

## What changed

- **Two commands per stack.** `pnpm test` and `dotnet test` are the normal commands, in CI and locally.
  `pnpm test:coverage` and `Lighthouse.Backend/Test-Coverage.ps1` collect coverage when there is a specific reason.
- **CI collects no coverage.** The two backend test steps became one, and Sonar no longer reads lcov or opencover. Its
  general analysis, test results and quality gate stay.
- **The time-budget guard asks whether it runs on CI**, not whether coverage is on. A CI run keeps exactly the limits
  it had: three times the local budget, never retiring an entry. The run kind `coverage` became `scaled`.

## What it achieved

One run so far, on a fast runner: frontend Vitest Duration 501.89 s, against 610-830 s for coverage runs on the same kind
of runner, and summed test seconds 27 % lower. The backend test step took 658 s against 836-912 s, as direction only. No
plain run has landed on a slow runner yet.

## Key decisions

- Sonar's gate was read before the push and has no coverage condition.
- DISTILL was skipped, and the five-run wait dropped, on the maintainer's word.
- The maintainer replaced a designed third run kind with one line, so CI and a local coverage run share one factor.
- The per-file budgets were left alone and are looser on a plain run until they are measured on one.

## Quality

- Mutation: 100 % on `timeBudgetOptions.ts`, 93 mutants. See `mutation-results.md`.
- Adversarial review: one minor finding, fixed. The roadmap and DEVOPS reviews each rejected a first version.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | `CLAUDE.md`, `docs/ci-learnings.md` and `/clean-ci` updated. Product docs N/A, because nothing a user sees changed. |
| Screenshots, demo data, website | N/A, because nothing a user sees changed. |
| Clients (CLI, MCP) | N/A, because no route or DTO changed. |
| Release notes | N/A; this Story has no user-visible change. |
| Context map, `ARCHITECTURE.md` | No change: CI tooling adds no concept, context, edge or domain event. |
| Usage-data event | N/A, because nobody uses CI tooling in the product. |

## Follow-ups

- T4 #6462 recalibrates `LIMIT_FACTOR` and the per-file budgets on plain CI runs. The slowest test is now 2.77 s against a
  CI limit of 6 s.
- `run_sonar` in `ci_backend.yml` is always true and could go.
