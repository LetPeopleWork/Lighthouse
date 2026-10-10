# RED classification — story-6248-work-items-dialog-context-columns

Produced by the pre-DELIVER fail-for-the-right-reason gate on 2026-10-10, and **re-run after review rounds 1 and 2**.
Every pending specification was un-skipped and run against unmodified production code plus the DISTILL scaffolds,
then classified. On the frontend that meant throw-away copies of the seven files with `it.skip` changed to `it`. On
the backend the `[Ignore(Pending)]` attributes were removed in place and restored afterwards. The copies are deleted
and the attributes are back. DELIVER reads this at PREPARE to confirm the RED it starts from is genuine.

**Verdict: PASS.** No `IMPORT_ERROR`, `FIXTURE_BROKEN`, `SETUP_FAILURE` or `OBSERVABLE_NOT_AT_PORT`. Every
pending specification that can run is `MISSING_FUNCTIONALITY`. The one E2E walking skeleton is `BLOCKED` until
slice 01a gives Team Zenith its Story Points field.

## Frontend — 153 cases: 146 pending failed, 7 pins passed

Every suite loaded. No failure came from an import, the dialog test kit, the held-answer harness or a stand-in.
On the first round-1 run, 13 cases passed without the feature (vacuous):

- the per-context "Estimate neither shown nor offered" checks (12), which only asserted an absence;
- the Delivery timeline's hide-and-reopen check, which today's single shared layout already satisfies.

Both were strengthened. The first now pins the exact columns shown and offered. The second also turns a catalogue
column on. On the final run none of them passed.

| Failure shape | Cases | Classification | Why it is the right reason |
|---|---|---|---|
| `AssertionError: expected … to deeply equal …`: columns shown, sort, row order, cells, Manage columns list, remembered layout | 61 | `MISSING_FUNCTIONALITY` | The dialog takes no `context`, the grid ignores `defaultColumnVisibilityModel`, and no catalogue or Estimate column exists. Each case reaches its assertion after real renders, real Manage-columns clicks and real held answers. |
| `AssertionError: Manage columns does not offer <column>` (the kit's `turnColumn`) | 12 | `MISSING_FUNCTIONALITY` | There is no catalogue yet. The panel opens; the column is not in it. |
| `AssertionError: No <column> column / cell …` (the kit's readers) | 9 | `MISSING_FUNCTIONALITY` | The column the context should show is absent. |
| `Error: Not yet implemented -- RED scaffold` (the map's invariants, through the scaffold's Proxy) | 28 (4 test blocks) | `MISSING_FUNCTIONALITY` | The defaults map does not exist. |
| `TestingLibraryElementError` for the loading overlay (`progressbar`) or the could-not-load copy | 8 | `MISSING_FUNCTIONALITY` | There is no `status`, no loading look and no failure look. The dialog itself is present. |
| `AssertionError: a lookup of the parents' names` / `the Work Items dialog is open` | 6 | `MISSING_FUNCTIONALITY` | No parent-name query exists. A Cumulative Time per State bar opens no dialog until its items arrive. |
| Other assertion counts (the remainder of the 146) | — | `MISSING_FUNCTIONALITY` | Same causes, reported through `expected … to be null` / `toHaveTextContent`. |

**The child items, from all three lists.** The Team's Feature list, the Portfolio's Feature list and a Delivery's
Features each open the real dialog through the real progress cell. All 15 cases fail on the missing loading look,
columns or failure copy, never on finding the row or the Team button.

**Cumulative Time per State.** This now renders the real `WorkItemsDialog`. Its 6 cases fail because the dialog
does not open at once, the loading look is missing, or Days Contributed is missing.

**Unhandled rejections: 7.** These are child items (2 per list) and Cumulative Time per State (1). Neither request
has a `catch` today, so their failure specs red the run through an unhandled rejection as well as through the
assertion. That is the missing error state itself, not a harness fault. It disappears when the keyed query with
its error look lands.

**Pins (7), green before and after:**

- A grid without defaults shows every column, remembers a hidden one, and Reset shows them all (3).
- Feature rows keep Owned by.
- A dialog without a context keeps today's columns.
- A dialog at its defaults with nothing to list says "No items to display".
- No parent lookup happens while Parent is hidden.

## Backend — 66 un-ignored, 66 failed; 1 pin passed

| File | Cases | Failure shape | Classification |
|---|---|---|---|
| `EstimateNormalizerEstimateOfTest.cs` | 28 | `InvalidOperationException: Not yet implemented -- RED scaffold written by DISTILL: the estimate(s) of …` | `MISSING_FUNCTIONALITY` |
| `WorkItemEstimateDtoTest.cs` | 8 | The same scaffold exception, from `WorkItemEstimateDto.For` | `MISSING_FUNCTIONALITY` |
| `WorkItemEstimateApiIntegrationTest.cs` | 27 | "… carries no estimate field" (`Expected: not null / But was: null`), `Expected: Object / "Story Points" / Null`, all on the real JSON body. The seeded rows and chart points are found: the fixture's "must show up" / "No row" / "has no row" guards failed 0 times. | `MISSING_FUNCTIONALITY` |
| `EstimateNormalizationSeamArchUnitTest.cs` | 3 | `BaseMetricsService.BuildEstimationVsCycleTimeResponse` and `BuildFeatureSizeEstimationResponse` call `NormalizeBatch` directly. `WorkItemEstimateDto.For` does not call `EstimateOf`. | `MISSING_FUNCTIONALITY` |

The seam test now decodes the IL instruction by instruction, using the `OpCodes` table and operand sizes. It no
longer matches call bytes anywhere in the method body. Its positive control, `TheScanner_SeesACallThatIsThere`,
runs today and stays green: it checks that `NormalizeBatch` calls `Normalize` and that the reverse is false. A
scanner that stopped seeing calls would fail that control rather than silently passing the seam tests.

In every integration case the host started, the database seeded and every endpoint answered 200. No failure came
from setup.

## E2E — 1 skipped, BLOCKED until slice 01a

`WorkItemsDialogContextColumns.spec.ts` is the walking skeleton on demo data. It opens Team Zenith's Estimation vs.
Cycle Time, clicks a bubble and reads the Estimate column by its header; it configures nothing. It compiles, is
Biome-clean and is listed by Playwright. It runs once the demo data gives Zenith its Story Points field.

The page-object members it uses were exercised live on 2026-10-10 against today's aging dialog: `columnHeader`,
`cellIn`, `cellsIn` (the skeleton checks every row's Estimate cell through it) and `openManageColumns`. Each run
used a throwaway instance on port 5199 with demo scenario 0. The temporary probe specs passed and were then
deleted.

After round 2 the four changed frontend files were un-skipped again. All 118 pending cases in them failed for
missing functionality, the 4 pins in them passed, and no case failed on a type or reference error.
