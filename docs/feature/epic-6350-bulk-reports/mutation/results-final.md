# Mutation testing: final gate for Epic 6350 (bulk reports)

Run 2026-10-31 against local `main` @ `54a6ab180` (production code frozen), plus the two kill-round tests below. Every
slice already had its own run (`results-64xx.md`). This run covers only the production lines changed after the last
slice's run, that is the final refactor, the CI fix and the final-review fixes:

```
git diff -U0 dda234574..HEAD -- Lighthouse.Backend/Lighthouse.Backend Lighthouse.Frontend/src ':!**/*.test.*' ':!**/tests/**'
```

Gate: 80 % kill rate per stack on those changed lines. Import-only, type-only and comment-only lines are left out.

## Backend (Stryker.NET 5.0.0)

Config `stryker.final.backend.json`, from `Lighthouse.Backend/Lighthouse.Backend.Tests/`, with `StrykerOutput` removed
first. Settings: `perTestInIsolation`, concurrency 4. Stryker.NET ignores line spans, so the four files are mutated
whole and the changed-line score is read from `mutation-report.json`. "Will be tested": **44**. The run took
**3 m 22 s** (3 m 35 s wall clock, build included).

Test filter: `ReportBulkCommandsTest`, `ReportOwnerLookupTest`, `API.ReportsControllerTest`, `Models.Reports`, and the
one acceptance class `CreateForSeveralTeamsScenarios` (53 tests). The rest of `API.Integration`,
`Integration.Containers`, `Dogfood` and the live-connector categories are out.

Changed lines:
- `ReportsController.cs`: the two ceiling checks and their messages (`:49`, `:51`, `:66`, `:68`), now naming
  `ReportBulkRun.MostPerRun`.
- `ReportBulkOutcomes.cs`: the `ReportBulkRun` constant class (`:5`–`:9`) and `OwnerName` (`:28`).
- `ReportBulkCommands.cs`: the owner looked up after the save and named on the outcome (`:55`–`:62`, `:65`,
  `:68`–`:69`, `:83`–`:84`).
- `ReportOwnerLookup.cs`: `Find` answering the owner's name (`:13`).

Not mutated: `IReportBulkCommands.cs` and `IReportOwnerLookup.cs` (interfaces).

| file | whole file | changed lines |
| --- | --- | --- |
| `API/ReportsController.cs` | 8 / 9 | 2 / 2 |
| `Services/Implementation/Reports/ReportBulkCommands.cs` | 27 / 27 | 3 / 3 |
| `Services/Implementation/Reports/ReportOwnerLookup.cs` | 8 / 8 | 3 / 3 |
| `Models/Reports/ReportBulkOutcomes.cs` | no mutant | no mutant |
| **total** | 43 / 44 = **97.73 %** | **8 / 8 = 100 %** |

### Mutants Stryker could not test, probed by hand

Stryker's safe mode rolled back every mutant inside `CreateForEach`, `RemoveEach` and the per-owner creation method
as `CompileError`. The pattern-matched `asked` and the `created` assigned in a `try` stop compiling once their guard
is mutated, and the rollback takes the whole method. So the changed conditions at `ReportsController.cs:49`, `:66`
and `ReportBulkCommands.cs:55` were never scored. They were applied by hand and run against the unit classes plus
`CreateForSeveralTeamsScenarios` and `RemoveReportsInBulkScenarios`:

| mutant | result |
| --- | --- |
| `ReportsController.cs:49` `Count > MostPerRun` to `>=` | killed (2 tests, among them `Exactly_50_owners_are_answered_owner_by_owner`) |
| `ReportsController.cs:66` `Count > MostPerRun` to `>=` | **survived**; killed by the kill-round test below |
| `ReportBulkCommands.cs:55` `==` to `!=` | killed (8 tests) |
| `ReportBulkCommands.cs:55` NotFound branch skipped (`&& owner.Id < 0`) | killed (3 tests) |

`if (false)` cannot be probed: CS0162 (unreachable code) fails the build under `TreatWarningsAsErrors`.

### Kill round (one test)

- `ReportsControllerTest.A_removal_of_exactly_the_ceiling_is_passed_on_whole`: 50 report ids reach the remover whole
  and the answer is OK. Red with `:66` turned into `>=`, green without. Only one over the ceiling had ever been sent
  for a removal, so the boundary was pinned from one side.

### Survivor left (not on a changed line)

- `ReportsController.cs:26`, `row.Owner.Kind == ReportOwnerKind.Team` turned into `!=`, in `ReadableOwners`. Untouched
  since its slice. The Teams' id list would then hold the Portfolios' ids instead; no test in this filter gives a Team and
  a Portfolio the same id under different read rights, so nothing tells the two lists apart.

## Frontend (StrykerJS 10.1.1, vitest runner)

Config `stryker.final.frontend.json` with `vitest.stryker.final.mutation.ts`, `inPlace`, `perTest` coverage,
incremental, concurrency 2, one entry per changed span. The vitest include holds the Reports page suites
(`src/pages/Reports/*.test.tsx`, so the bulk wizard, the result and the removal dialog), `reportOwner.test.ts`,
`ReportsTab.test.tsx`, `reportRefusalCopy.test.ts` and the `ReportPrintPage*` suites. `ActionButton.test.tsx` and the
`*.enforcement.test.ts` suites stay out, as in 6435. The run took **12 m 40 s** for 67 mutants (21.9 tests per
mutant), while another worktree's vitest held the machine at a load of about 20.

Every file in the mutate list shows up in the per-file table. `thenAndNowDraft.ts` changed only an import, so it has no
entry. `reportOwner.ts` has no mutant on its `as const` query-key arrays (`:29`, `:33`, `:36`–`:39`); StrykerJS
generated none there.

| file | changed lines, first run | after kill round (hand-verified) |
| --- | --- | --- |
| `pages/Common/Reports/print/ReportPrintDocument.tsx` | 4 / 4 | 4 / 4 |
| `pages/Common/Reports/reportOwner.ts` | 3 / 3 | 3 / 3 |
| `pages/Common/Reports/wizard/longDay.ts` | 1 / 1 | 1 / 1 |
| `pages/Common/Reports/wizard/reportRefusalCopy.ts` | 6 / 6 | 6 / 6 |
| `pages/Reports/BulkReportWizard.tsx` | 6 / 7 | 6 / 7 (85.71 %) |
| `pages/Reports/BulkResult.tsx` | 5 / 7 | 5 / 7 (71.43 %) |
| `pages/Reports/OwnerStep.tsx` | 3 / 3 | 3 / 3 |
| `pages/Reports/RemoveSelectedDialog.tsx` | 3 / 3 | 3 / 3 |
| `pages/Reports/useBulkCreation.tsx` | 1 / 1 | 1 / 1 |
| `pages/Reports/useBulkReportCreation.ts` | 1 / 2 | 1 / 2 (50.00 %) |
| `pages/Reports/useBulkReportRemoval.ts` | 20 / 24 | **21 / 24 (87.50 %)** |
| `pages/Reports/useRowPicks.ts` | 6 / 6 | 6 / 6 |
| **total** | 59 / 67 = **88.06 %** | **60 / 67 = 89.55 %** |

The first run already met the gate. No Stryker re-run was made after the kill round: the kill was checked by applying
the mutant by hand (`sed`) and running the new test, which fails with the mutant and passes without it.

### Kill round (one test)

- `ReportsPage.removal.test.tsx`, "the report forgotten is the one removed, not one picked before it". Kills
  `useBulkReportRemoval.ts:35`, where `picked.id === outcome.reportId` became `!==`. The existing forget test removed
  the first two of three reports, and with `!==` each of those two finds the other, so the same two entries were
  forgotten. The new test keeps the first report and removes the other two, so the mutant forgets the wrong one.

### Survivors left (6), all judged equivalent or not worth a test

- `BulkResult.tsx:43` (two): the under-way row's `sx={{ alignItems: "center" }}` emptied, and its value blanked. MUI
  `sx` layout, the usual equivalent class here.
- `BulkReportWizard.tsx:310`: the `.filter` on picked owners removed, so `pickedNames` maps every offered owner, not only
  the picked ones. The map is read only by owner key for an outcome row, and the run answers only the owners that
  were picked, so the extra entries are never read.
- `useBulkReportCreation.ts:38` and `useBulkReportRemoval.ts:46`: `invalidateQueries({ queryKey: ["reports"] })` turned
  into `invalidateQueries({})`. Invalidating every query still re-reads every report list, so nothing a reader sees
  changes. Same reasoning as `useBulkReportCreation.ts:36` in 6429 and `useBulkReportRemoval.ts:28` in 6433.
- `useBulkReportRemoval.ts:36`, `report !== undefined` turned into `true`. The report is looked up among the ones sent,
  and the server answers only the ids it was asked about, so it is always found. The guard is defensive (same as
  `RemoveSelectedDialog.tsx:98` in 6433).
- `useBulkReportRemoval.ts:39`, `exact: true` turned into `false`. The report's key `["report", kind, ownerId,
  reportId]` ends with the report id and no cache entry nests beneath it, so a prefix match removes the same one entry.

No survivor revealed a bug.

## Commands

From `Lighthouse.Backend/Lighthouse.Backend.Tests/`:

```
rm -rf StrykerOutput
dotnet stryker --config-file ../../docs/feature/epic-6350-bulk-reports/mutation/stryker.final.backend.json
```

From `Lighthouse.Frontend/`, after
`cp ../docs/feature/epic-6350-bulk-reports/mutation/vitest.stryker.final.mutation.ts .`:

```
TZ=Europe/Zurich pnpm exec stryker run ../docs/feature/epic-6350-bulk-reports/mutation/stryker.final.frontend.json --incremental
```

The two stacks were run one after the other, never at the same time.
