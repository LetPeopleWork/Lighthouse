# Slice 48 — T6 Speed tiers from the code's structure, and a fast local loop on both stacks

Epic #6506 · ADO #6512 · `@infrastructure` · job_id: infrastructure-only · **Size M** (four slices of at most a day) · DRAFT
infrastructure_rationale: one command runs every test, so a developer editing a rule waits behind hosted tests. Behaviour-free.

## Goal

A developer runs only the fast tier while editing and gets an answer in seconds, on both stacks. A test's tier follows from what
it does, and a misplaced backend test fails the build. The full suite and CI run exactly what they run today.

## Why

On CI run 41691382136 frontend logic tests have a median of 0.3 ms, component tests 74 ms, backend unit tests 0.9 ms, integration
tests 204 ms (ADO #6512). FE7 measured a node project at +2.0 % on a full run, so the value is the loop, not a faster full run. A
caution: `trx_to_csv.py` writes `Unit` unless it finds a literal `[Category("Integration")]`, so `API.Integration` reads as `Unit`;
backend medians from that column would compare live connectors with the rest. Slice D re-cuts them.

## Proposals

1. **Backend rule: what a class can reach decides, a category records it.** Reaches a host or real SQLite: `Hosted`. Reaches
   Postgres or Redis: `Container`. Existing connector categories: live. Namespace `Architecture`: its own tier. Anything else: fast.
   Why: a filter needs a category, and a base class alone cannot decide, since only 57 files derive from `IntegrationTestBase`,
   about 36 other abstract bases are not built on it, and many classes build a host themselves. `[Hosted]` and `[Container]` derive
   from NUnit's `CategoryAttribute`, sit on the base class where there is one, and are inherited (confirm at DESIGN). No file moves.
2. **Frontend: a command, no Vitest projects.** Unit is `.test.ts` (208 files), component is `.test.tsx` (405), by glob over `src/`.
   Why: same environment and setup means no speed gain; FE7 rejected the node project, and 26 `.test.ts` files import
   `@testing-library/react`. The reporter needs nothing: it leaves alone an entry whose file was not collected, and all 3 `slowTests`
   entries are `.tsx`. The timing CSV has `file`, so T9 reads the tier off the extension.
3. **Commands.** `pnpm test:fast` is `cross-env TZ=Europe/Zurich vitest run --exclude "**/*.test.tsx"` (a `.test.ts` filter would
   likely match `.tsx` too; confirm). `pnpm test:fast --changed` runs what an edit affects. `Test-Fast.ps1`, beside `Test-Coverage.ps1`,
   filters out `Hosted`, `Container`, live and `.Architecture.`. Neither is a gate: `pnpm test` and `dotnet test` stay done. Measure:
   median wall of 5 warm runs on a quiet machine at the default worker cap, with counts, backend timed apart from the build. No figure
   exists: FE7's 24.1 s for 125 `.test.ts` files was on a shared machine; fixed cost is 53 % of worker time, so 10 s may be missed (estimate).
4. **The guard.** `Architecture/TestTierGuardTest.cs` reads the compiled assembly through `IlDependencies.ReadModule`, because
   ArchUnitNET drops async edges in Release. It reuses the call walker of `TestHostPerFixtureGuardTest`, moved to `TestHostScanning`
   in its own `refactor` commit, with one-time set-up, constructors and base types added as roots. It fails both ways and names the
   type: unmarked but reaches a host or database; marked but no longer does. Planted nested fixtures prove it, the `PlantedFixtures`
   way, and a real planted class is shown red once, also under `-c Release`. The baseline is an inline list of full type names with
   reasons, never a count; a rule is never loosened. Limit: the walk stops at the test assembly, so T9's budget is the backstop.
5. **Full-suite proof.** Before the first commit keep sorted ids: Vitest `file :: full name` (FE7's method), backend
   `fully_qualified_name` from a full run's TRX. After the last commit the diff is empty, and fast ids plus the other tiers' ids
   equal the full list with no overlap. Real runs, because `--list-tests` with a filter is unconfirmed. Unstable names get listed.
6. **Slices (days are estimates), frontend first:** cheap, and it gives question 1 a real number. A (0.5): `test:fast`, ids proof, first
   timing. B (1): scan moved, guard, every unmarked slow class on the inline list, so it lands green. C (1): 200 to 300 one-line markers
   (estimate) empty the list; `Test-Fast.ps1`. D (0.5 to 1): tier in the `category` column, medians re-cut, backend proof, target, docs.
7. **CI: no change.** The guard is one more test in the run; new categories leave CI's filter alone; `check_budget.py` ignores `category`.

## Backend tier counts and what is ambiguous

| Tier | Test files | How counted (text search, file level; a file can hold several classes) |
|---|---|---|
| fast | about 510 | 731 files with a test-named class outside `API/Integration`, minus the rows below |
| Hosted, `API/Integration` | 338 | files with a concrete class there (350 declarations, a few of them helpers) |
| Hosted, elsewhere | 85 | a host-start word outside `API/Integration`, `TestHelpers`, `Architecture`, `Containers` |
| Hosted, SQLite, no host | 23 | `UseSqlite(` or `new SqliteConnection(`: 26 files, 3 already hosted; 12 of the 23 are `:memory:`, see question 3 |
| Container | 34 | `Integration/Containers`, 37 files minus 3 fixtures |
| live | 16 | files carrying `Integration` or a connector category |
| architecture | 64 | `Architecture`, 70 files minus 6 helpers |

- `API/Integration` is not all hosted: of 165 declarations with no base on the class line, some start no host. Slice B's scan decides.
- 5 classes where few tests start a host (`OnlyAFewTestsStartAHost`): the whole class is `Hosted`, as the budget's unit is the class.
- Fast: 3 files that only call `WebApplication.CreateBuilder()`, and EF InMemory (26 files). At least 128 files touch files, real
  waits or `HttpClient` (a floor; search stopped at 500 hits): no rule, T9's budget catches the slow ones.

## IN

The four slices above, including T9's input: the frontend tier from the extension, the backend tier in the `category` column.

## OUT

Budgets and ceilings per tier (T9 #6536), rewriting any test, moving test files, `.github/`, Vitest `projects`, production code.

## Dependencies

Hard: none (T4 #6462 is pushed). Soft: T9 #6536 builds on this; T5 and T12 #6539 touch some of the same test files. The Epic knowingly
waives the slice-composition gate for `@infrastructure` Stories (D-6264-5). T6 has no entry in the Epic's feature-delta yet.

## Acceptance criteria

- [ ] `pnpm test:fast` runs only `.test.ts` files and `Test-Fast.ps1` only fast classes; both meet the agreed target.
- [ ] A planted misplaced class and a stale marker each fail the guard under Debug and Release, and the failure names the class.
- [ ] Full id lists before and after are identical; the tiers add up to the full list with no overlap, on both stacks.
- [ ] The backend `category` column agrees with the fast run's TRX. No change in `.github/` or production code, so mutation is N/A.

## Cross-cutting

RBAC: N/A, because no authorization code changes. Lighthouse-Clients: N/A, because no route or DTO changes. Website: N/A, because
nothing user-visible changes. Usage-data event: N/A, because nobody uses test tooling in the product. UI sketch: N/A, because no screen
changes. Terminology: N/A, because tier names are not configurable terms. Context map: N/A, because no context, concept or event changes.

## Open for the maintainer

1. **What is "an answer in seconds"?** (a) Whole fast tier under 10 s per stack; may be out of reach. (b) Frontend `--changed` under 10 s
   for a one-file edit, whole fast tier under 60 s per stack, confirmed after slice A. (c) No number. **Recommended: (b)**, checkable.
2. **ADO says "frontend projects"; this proposes a command.** (a) Command only. (b) Two jsdom projects, for names alone. (c) A node
   project, measured for the fast loop. **Recommended: (a)**, and (c) only if slice A misses the target.
3. **Is `:memory:` SQLite fast?** (a) No: any real database engine is `Hosted`, one simple rule, 12 classes leave the loop. (b) Yes: the
   rule has to read connection strings. **Recommended: (a)**, revisited when T9 shows their times.
4. **ADO says "by test base class"; this proposes a category a scan checks.** (a) As proposed. (b) Real shared bases, which change
   how fixtures start their hosts. (c) Tier folders, moving hundreds of files. **Recommended: (a)**.

## Maintainer decisions (2026-11-07)

These override the proposals above where they differ.

- **Target:** frontend `--changed` under 10 s for a one-file edit; the whole fast tier under 60 s on each stack, confirmed
  after the first slice.
- **Frontend:** a command only, no Vitest project change. A node project is measured only if the first slice misses.
- **What is fast is decided by time, not by what a test touches.** No rule about SQLite or any other engine: a test is
  fast if it runs fast, and one that takes long is a test to rethink. T9's per-test budget is what enforces this.
- **Backend tier:** regular tests carry no marker and are the default. One extra category, set through a real shared base
  class with its attribute, marks the tests that boot the host, so they can be selected or left out. One category to deal
  with. Fixtures that build a host themselves move onto that base class, so sizing above needs re-reading at DESIGN.
- The category cannot be called `Integration`: that name already means the live-connector tests in the standard filter.
- **Correction, same day: the category IS `Integration`, replacing the line above.** The live-connector tests are
  integration tests too, so one category covers hosted and live tests, and its count and summed time are the number that
  shows how fast integration tests grow. `Scripts/test-timings/trx_to_csv.py` already has that column but reads the
  attribute from the class's own source, so it must learn the shared base class, or hosted tests read as Unit.
- **Consequence for the standard filter:** `TestCategory!=Integration` would then leave out every hosted test, so it can no
  longer mean "no real systems". The standard filter keeps only `JiraIntegration`, `LinearIntegration`, `AdoIntegration`,
  `ServiceNowIntegration`; the two bare live files (`GithubServiceTest`, `LighthouseReleaseServiceIntegrationTest`) gain a
  category of their own, and the filter text in `CLAUDE.md` and `Test-Coverage.ps1` changes with it. The fast loop is
  `TestCategory!=Integration`.
