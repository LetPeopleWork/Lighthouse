# Story 6374 — S05f: the two metrics services read through named reads

**Finished:** 2026-11-10 · ADO User Story #6374 (Closed) · Epic #6507 "Narrow persistence access: repositories by role"

## The problem

`TeamMetricsService` (14 calls) and `PortfolioMetricsService` (13 calls) read work items, Features and their
transitions by handing a lambda to `GetAllByPredicate`. The unit tests run on EF's in-memory provider, which evaluates
any lambda, so a predicate that SQLite or Postgres cannot translate would still pass every test. The two services were
also 2 of the 15 files on the S05a allow-list that S05g has to empty.

## What changed

Behaviour-free, as two nwdes features on one branch, each a prefactoring move followed by one value slice:

- **Feature A, Team** (`docs/feature/story-6374-s05f-a-team-reads/`): seven reads added to `IWorkItemRepository`,
  `IWorkItemStateTransitionRepository` and `IFeatureRepository` (`89849ecea`), then `TeamMetricsService` switched to
  them (`b9320773b`). Allow-list 15 → 14.
- **Feature B, Portfolio** (`docs/feature/story-6374-s05f-b-portfolio-reads/`): six reads added to `IFeatureRepository`
  and `IFeatureStateTransitionRepository` (`2941085f4`), then `PortfolioMetricsService` switched to them and to
  `IFeatureRepository` (`701e90608`). Allow-list 14 → 13.
- Each read keeps today's predicate word for word, one read per state-category set, so the SQL, the loaded data, the
  tracking and the row order stay the same. Work-item reads share one helper holding `Include(Team)`; Feature reads
  share one helper that loads, orders and tracks a Feature exactly as `GetAllByPredicate` did (`b30b56e92` routed
  feature A's Feature read through it too).
- Tests: the metrics test set-ups answer each named read with the predicate it replaces, through
  `WorkItemRepositoryDoubles` and `FeatureRepositoryDoubles`; no assertion changed except one `Verify(..., Times.Never)`
  retargeted with the same meaning. Two parity classes in `Integration/Containers` compare every named read with its
  predicate on SQLite and Postgres.

## Key commits

| Commit | What |
|---|---|
| `a86e3f8b4`, `ebf0e6746` | Pick-up and DESIGN |
| `89849ecea`, `2941085f4` | The two prefactoring moves: reads added, no caller |
| `d6786c80e`, `f74341671` | Oracles: allow-list edits and the parity classes |
| `b9320773b`, `701e90608` | The two services switched |
| `6535d6aa5`, `b30b56e92` | Refactors |
| `2e36ca203` | Mutation run |

The branch was rebased on `main` before the push, so the nwdes run records and the feature workspaces cite the
pre-rebase hashes (for example `d36cfa20e` for `b9320773b`); match them by commit subject.

## Quality

| Gate | Result |
|---|---|
| Oracle reviews | A and B accepted; A re-approved after B's edit of the shared allow-list file |
| Slice reviews | A and B accepted, no findings |
| Whole-candidate reviews | A accepted with 2 low findings (duplicated parity support; `WorkItemRepository.GetAllByPredicate` repeats `Include(Team)`); B accepted, no findings |
| EXAMINE (WSL) | A accepted, E1–E3 met. B indeterminate: E1, E2 met; E3 (answers and query count unchanged) an observation gap — `metrics-answers-compare` needs a `DB_Backup` database present on neither machine, and no query-count test covers Portfolio. Draft kept in the Story workspace under `examine/` |
| Full filtered suite | 13,304 passed, 0 failed on `701e90608`; 13,333 passed after the rebase with the known Defender file lock in `ServiceProviderValidationTest` as the one failure |
| CI | Run 42884870319 green; only the Standalone release gates waiting |
| Mutation | New reads 97.4 % and 94.7 %; services: see the checklist below |

## Decisions taken without the maintainer, open to revisit

- Pushed with feature B's EXAMINE indeterminate on E3 (the maintainer gave the go at the push decision).
- Two surviving mutants on the new lines left unkilled: the new lines were above 80 %, and the kills are fixture
  edits that would have reopened the reviewed oracles.
- The mutation bar was read as "no survivor or no-coverage mutant added" for `PortfolioMetricsService`, whose score
  fell only because its well-covered predicates moved out.

## Knowingly left open (for S05g)

- A Portfolio statement-count test.
- The two surviving mutants (`mutation/results.md`).
- One shared helper for the two parity classes; `WorkItemRepository.GetAllByPredicate` using the `Include(Team)` helper.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | N/A, because nothing a user sees changed: no route, screen, setting or answer. |
| Screenshots | N/A, because no screen changed. |
| Demo data | N/A, because no data shape or seeded value changed. |
| Website assets | N/A, because no asset under `docs/assets/` changed. |
| Clients (CLI, MCP) | N/A, because no route, DTO or enum changed; no client follows. |
| Context map, `ARCHITECTURE.md` | Done: `ARCHITECTURE.md` §6 (reads through the owner's named reads, the allow-list at 13); `docs/product/architecture/brief.md` (new section); ADR-270 needs no change (no edge, layout or ownership changed); the Epic catalogue's Improvement log. |
| Usage-data event | N/A, because no feature became usable or used differently. |
| Mutation | Done: `docs/feature/story-6374-s05f-metrics-services-off-getallbypredicate/mutation/results.md`. New reads 97.4 % / 94.7 %; `TeamMetricsService` 31.67 % (baseline 30.58 %); `PortfolioMetricsService` 22.17 % (baseline 27.60 %, no survivor or no-coverage mutant added). Frontend: N/A, no frontend file changed. |
| Release notes | N/A; written at /release. |
