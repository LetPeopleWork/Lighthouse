# Story 6536 — A tight limit for fast tests, and ceilings for the slow tier that only go down

**Finished:** 2026-11-08 · ADO User Story #6536 · Epic #6506 "Fast, reliable test suites"

## The problem

Once T6 gave every test a tier, nothing kept the tiers apart in time. A fast test could grow to seconds under the 2 s
limit every test had, and the integration tier could grow slower or larger with no one told. A limit written as a bare
number in a file also gave no reason for itself and could be raised by any edit.

## What changed

- **A frontend fast limit.** The Vitest reporter gives every file that is not `.test.tsx` its own, lower limit; `.test.tsx`
  keeps 2 s.
- **Backend tier judges in `check_budget.py`.** A fast test over its limit fails the build by name. The `Integration`
  tier has two ceilings: mean seconds per test, which only goes down, and total seconds, which may rise only by a
  reviewed entry with a reason. Connector classes are found from the source and left out of both.
- **Limits are ledgers** in `test-time-budget.json`: ordered entries, each with its seconds, CI run ids and reason. An
  entry with 5 distinct runs is enforced and never changes; one with fewer is provisional and only warns, saying "n of 5".
- **Each run prints its tiers** to the log and the job summary. `check_budget.py --pyramid` prints the fast, integration
  and E2E shares across both stacks and Playwright. No gate reads the shares.
- No workflow changed. ADR-237 was amended on 2026-11-08, and `docs/ci-learnings.md` holds the derivation recipe.

## What it achieved

- **The frontend fast limit enforces:** 0.7 s, 2.1 s on CI, from the largest logic test in five shuffled CI runs, 0.890 s
  in run 42372874167 (step 01-02). A planted 1.21 s logic test failed `pnpm test:fast` and was named.
- **The backend limits are provisional, "2 of 5", and warn only:** fast test limit 20.0 s, `Integration` mean 0.81 s,
  `Integration` total 2,780 s at 3,353 tests, derived from runs 42468070284 and 42476850140 (step 03-01). Run
  42489988507, the first after they landed, printed all three warnings and failed nothing.
- **The shares, once** (run 42468070284, `--pyramid`): 58.5 % fast, 40.9 % integration, 0.6 % E2E, against the direction
  of about 90 %, 9.5 % and 0.5 %.

## Key decisions

- No `ci*.yml` edit: everything lives in `check_budget.py`, its JSON and the Vitest reporter.
- Connector classes are left out of the count, not whole runs, so every run gets a verdict.
- The headroom is the measured spread with a floor of 1.13; a spread over 1.25 is never capped, it stays provisional
  and goes to the maintainer. Both backend ceilings' spreads are over 1.25 today.
- `LIGHTHOUSE_BUDGET_WARN_ONLY` softens a breach, never a broken ledger or a stale entry.
- Maintainer, 2026-11-07: no frontend total, as its Vitest Duration does not compare across runs.
- Maintainer, 2026-11-08: the enforcing count starts after T5's last move, so T5's slice E box runs first.
- Maintainer, 2026-11-08: T9 closes with its backend limits provisional, against its own rule that it could not. Step
  03-02, the enforcing push, and criterion 7, the first connector or force-full run, go to T10 #6537.

## Quality

- Reviews: DESIGN and DISTILL each conditionally approved, 2 high findings each; all fixed but one low
  DISTILL finding, left on purpose. The adversarial slice B review
  found 1 high, 3 medium and 3 low; each fixed test-first, the suite going from 230 to 238 tests.
- Mutation: N/A, because no product file changed and Stryker mutates the product assembly. The work was Python tooling,
  data, and a small change to the Vitest time-budget reporter under `src/tests/`. A planted breach of each limit stood in.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | ADR-237 amendment and `docs/ci-learnings.md` updated. Product docs N/A, because nothing a user sees changed. |
| Screenshots, demo data, website | N/A, because no screen, data shape or website asset changed. |
| Clients (CLI, MCP) | N/A, because no route or DTO changed. |
| Release notes | N/A; release notes are written at /release, not at finalize. |
| Context map, `ARCHITECTURE.md` | Context map: no change, as CI tooling adds no context, concept, edge or domain event. `ARCHITECTURE.md` §15 points to ADR-237 for the tier limits, in this finalize commit. |
| Usage-data event | N/A, because nobody using the product reaches a CI budget check. |

## Follow-ups

- T10 #6537 makes the enforcing commit once 5 eligible runs exist after T5's last move push, with the recipe and the
  calibration table in this Story's closing record, and reads criterion 7.
- `--previous-budget` refuses an edit to an enforced entry, but no workflow passes it, so until it is wired an edit is
  caught by review only. For whoever edits `ci_backend.yml` next.
