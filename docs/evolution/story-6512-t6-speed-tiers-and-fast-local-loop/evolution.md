# Story 6512 — Speed tiers from the code's structure, and a fast local loop

**Finished:** 2026-11-08 · ADO User Story #6512 · Epic #6506 "Fast, reliable test suites"

## The problem

There was no quick test run to work in. A backend test's tier was a hand-written `[Category]`, which most hosted tests
did not carry, so the timing CSV read every test as `Unit` and a plain CI run counted 0 integration tests. On the
frontend, every run rendered components, even when only a function had changed.

## What changed

- **The tier comes from the base class.** `IntegrationFixture` carries `[Category("Integration")]` and nothing else.
  Every test that starts a host or a container derives from it, about 170 one-line edits, each move its own commit.
- **`TestTierGuardTest`** fails the build for a host-building test off the base, or a base user with no reason. It reads
  compiled IL, holds 90 tests, and has 7 rules, the last added by the maintainer: no category name has a space at an edge.
- **One `Integration` category in every filter.** CI's always-on clause changed its text so it selects the same tests;
  the standard filter, `dev-test.*`, `Test-Coverage.ps1` and `TestSelectionClauseTest` follow it.
- **The fast loop.** `pnpm test:fast` runs the `.test.ts` logic tests, `pnpm test:fast --changed` only those an edit
  reaches. `Lighthouse.Backend/Test-Fast.ps1` builds, then runs every test that starts no host, no container and no live
  tracker. The `CLAUDE.md` DELIVER bullet makes it the red, green and refactor loop, with the full gates at slice end.
- **`trx_to_csv.py` follows base classes** in the source tree to fill the `category` column, and warns rather than
  reading an unplaced class as `Unit`.

## What it achieved

- **Frontend fast tier:** `pnpm test:fast` median 32.86 s against a 60 s target, 3,648 tests. `--changed` medians 1.35 s
  for a one-file edit and 3.53 s for a typical one against 10 s, and 15.25 s for the most shared file (5 runs each, step
  01-02).
- **Backend fast tier: 73.29 s median against 60 s, a miss** (`Test-Fast.ps1 -NoBuild`, 5 warm runs, 9,536 tests, step
  06-04). Four `Encryption` test classes hold 43.1 % of its summed time.
- **The category column is right:** on `L6`, 3,395 rows read `Integration` and 9,538 `Unit`, against 0 before. The first
  plain CI run after slice F (run 42407897220) read the same.
- **No test lost:** each local full list, `L0` to `L7`, held every id of the one before; pushes 7 and 8 lost 0 ids
  against the CI run before.

## Key decisions

- A category probe ran before anything moved, on Debug and Release, and the plan rested on it.
- Container tests sit on `IntegrationFixture`: in run 42345830021 they were 1 % of the tests and 17 % of the time.
- The fast loop also leaves out the five connector categories, so it cannot reach a real tracker.
- The planned CI "Check test selection" step was dropped at the DEVOPS review; local id lists before each push stand in.
- Maintainer, 2026-11-07: an `[Explicit]` test's `NotExecuted` row reads as not selected; CI keeps `UsageDataCanary`.
- Maintainer, 2026-11-07: about 15 s for `--changed` on a heavily shared frontend file is accepted.
- Maintainer, 2026-11-07: rule 7 of the guard, on a category name with a space at an edge.
- Maintainer, 2026-11-08: the backend fast tier's 73.29 s is accepted and stays recorded as a miss. T9's limit keeps it
  from rising; T10's moves lower it.

## Quality

- Reviews: DESIGN conditionally approved, all highs addressed; DEVOPS and DISTILL each came back needing revision and
  were revised. The reviews of slices A and B, C and D, and E and F found no high finding; their findings were fixed with
  a failure shown first.
- Two guarantees not met, and recorded: guarantee 13 (two force-full CI runs, not one) and guarantee 16 (the backend
  target).
- Mutation: N/A, because no product file changed and Stryker mutates the product assembly. Every commit was checked
  against an allow-list of paths. `Test-Fast.ps1` is shell; the Python scripts carry their own unit tests, 159 in all.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | `CLAUDE.md` and `.claude/commands/mutation-testing.md` updated. Product docs N/A, because nothing a user sees changed. |
| Screenshots, demo data, website | N/A, because no screen, data shape or website asset changed. |
| Clients (CLI, MCP) | N/A, because no route or DTO changed. |
| Release notes | N/A; release notes are written at /release, not at finalize. |
| Context map, `ARCHITECTURE.md` | Context map: no change, as test tooling adds no context, concept, edge or domain event. `ARCHITECTURE.md` §15's test stack line gains the two tiers, in this finalize commit. |
| Usage-data event | N/A, because nobody using the product reaches a test command. |

## Follow-ups

- T10 #6537 lowers the backend fast tier, starting from the class table of step 06-04.
- Open gaps from the slices E and F review: the method-level scan is raw-line; `trx_to_csv.py` does not skip `bin`, `obj`
  or `.stryker-tmp*`; `Test-Fast.ps1`'s claim to need no Docker was never run with Docker stopped.
- ADO #6512 comment: read the first force-full run after slice E once for any `[Explicit]` id that ran.
