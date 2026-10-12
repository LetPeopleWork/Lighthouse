# RED classification — story-6202-lists-beyond-100-rows

Produced by the pre-DELIVER fail-for-the-right-reason gate on 2026-10-12. Every pending case was
un-skipped in throw-away copies of the three test files (`it.skip` → `it`, `it.skip.each` → `it.each`)
and run against unmodified production code plus the DISTILL scaffold. The copies are deleted. DELIVER
reads this at PREPARE to confirm the RED it starts from is genuine.

Command (on the copies): `pnpm vitest run --reporter=verbose <the three *.redgate.test.tsx copies>`
→ **31 failed, 9 passed (40)**. After review a fourth file, `WorkItemsDialog.paging`, added one pending case; all four were re-run the same way: **32 pending failed, 9 pins passed**. The 9 that passed are exactly the pins, which run green in the
committed files too.

**Verdict: PASS.** No `IMPORT_ERROR`, `FIXTURE_BROKEN`, `SETUP_FAILURE` or `OBSERVABLE_NOT_AT_PORT`.
Every suite loaded and rendered the real MUI grid; every pending case is `MISSING_FUNCTIONALITY`. No
pending case passed without the feature, so none is vacuous.

No pending case reaches the `RowRangeFooter` scaffold: the tests go through `DataGridBase`, the
Refinement tab and the Third-party packages section, which do not render it yet. Its throw is a marker
for DELIVER, not a RED source.

## Frontend — 41 cases: 32 pending failed, 9 pins passed

| Failure shape | Cases | Classification | Why it is the right reason |
|---|---|---|---|
| `Unable to find an element with the text: 1–100 of N` (N = 101, 143, 243; 143 on the Refinement tab; 243 in the Work Items dialog) | 7 | `MISSING_FUNCTIONALITY` | `hideFooter` is on for every grid but Third-party packages, so no range is shown. The rows render; only the footer is missing. |
| `Unable to find an accessible element with the role "button" and name "Go to next page"` | 15 | `MISSING_FUNCTIONALITY` | No pager exists, so the second page cannot be reached. Each of these cases first renders the real list (and, on the Refinement tab, the real need and line) before it asks for the arrow. |
| `expect(element).not.toBeInTheDocument()` on `Rows per page` / the footer bar (Third-party packages) | 3 | `MISSING_FUNCTIONALITY` | The section still passes `hidePagination={false}` and `DataGridBase` offers `[10, 25, 50, 100]`, so the always-on footer with its picker is there, including under 38 packages and under a 9-package search. |
| `Unable to find … 1–100 of 150` / `1–100 of 243` after a search (Third-party packages) | 2 | `MISSING_FUNCTIONALITY` | MUI keeps the open page when the rows change, so a search from page 2 opens `101–150 of 150`, and clearing the search lands on `101–200 of 243`. The reset to page 1 the maintainer chose is not built. |
| `expected [ 'GR-0100:below' ] to deeply equal []` (enough-for line, 101 / 120 / 143 / 150 needed) | 4 | `MISSING_FUNCTIONALITY` | Today's first 100 rows already show the defect the paging story makes visible: the space for the line is kept under GR-0100 because it is the last row of the page. |
| `expected [ <p …> ] to deeply equal []` (120 needed, numbering case) | 1 | `MISSING_FUNCTIONALITY` | Page 1 says "All 100 Work Items in Refinement are needed before the next Refinement." while 143 are in refinement. The numbers 1 to 100 and the `100 of 120 needed …` label pass before it, as they should. |

Per case:

| File · case | Classification |
|---|---|
| DataGridBase.paging · with 101 / 143 / 243 rows shows the first 100 above the range (3) | MISSING_FUNCTIONALITY (range text absent) |
| DataGridBase.paging · the 101st row alone on page 2 | MISSING_FUNCTIONALITY (no pager) |
| DataGridBase.paging · every row of 243 reachable, arrows lead back | MISSING_FUNCTIONALITY (no pager) |
| DataGridBase.paging · previous and next only, no rows-per-page choice | MISSING_FUNCTIONALITY (range text absent) |
| DataGridBase.paging · saved layout unchanged after paging | MISSING_FUNCTIONALITY (no pager) |
| DataGridBase.paging · sort on page 2 goes back to page 1 | MISSING_FUNCTIONALITY (no pager) |
| DataGridBase.paging · column filter on page 2 leaving 179 opens its page 1 | MISSING_FUNCTIONALITY (no pager) |
| DataGridBase.paging · column filter on page 2 leaving 64 shows all, no footer | MISSING_FUNCTIONALITY (no pager) |
| DataGridBase.paging · reload to 64 on page 2 shows all, no footer | MISSING_FUNCTIONALITY (no pager) |
| DataGridBase.paging · reload keeping > 100 keeps the page | MISSING_FUNCTIONALITY (no pager) |
| DataGridBase.paging · reload removing the open page lands on the last page left | MISSING_FUNCTIONALITY (no pager) |
| DataGridBase.paging · no footer while reloading, range once loaded | MISSING_FUNCTIONALITY (range text absent after loading) |
| DataGridBase.paging · export from page 2 holds all 243 | MISSING_FUNCTIONALITY (no pager) |
| DataGridBase.paging · copy from page 2 holds all 243 | MISSING_FUNCTIONALITY (no pager) |
| ThirdPartyPackagesSection.paging · 243 packages: range, arrows, no picker | MISSING_FUNCTIONALITY (picker present) |
| ThirdPartyPackagesSection.paging · 38 packages: no footer | MISSING_FUNCTIONALITY (footer present) |
| ThirdPartyPackagesSection.paging · search from page 2 leaving 9: all show, no footer | MISSING_FUNCTIONALITY (footer present) |
| ThirdPartyPackagesSection.paging · search from page 2 leaving 150 opens its page 1 | MISSING_FUNCTIONALITY (page kept: `101–150 of 150`) |
| ThirdPartyPackagesSection.paging · clearing the search from page 2 opens page 1 of the whole list | MISSING_FUNCTIONALITY (page kept) |
| RefinementView.paging · Jonas pages to GR-0131 and votes | MISSING_FUNCTIONALITY (range text absent) |
| WorkItemsDialog.paging · page 2 of 243 filtered to 64 | MISSING_FUNCTIONALITY (range text absent). With the paging steps removed, its filter path was run separately and reaches 64 rows, so the tail of the case is sound. |
| RefinementView.paging · line placement, 0 / 20 / 100 needed (3) | MISSING_FUNCTIONALITY (page 1 correct, then no pager) |
| RefinementView.paging · line placement, 101 / 120 / 143 / 150 needed (4) | MISSING_FUNCTIONALITY (space kept under GR-0100 on page 1) |
| RefinementView.paging · 120 needed: numbering 1–100, then 101–120 | MISSING_FUNCTIONALITY ("All 100 … are needed" on page 1) |
| RefinementView.paging · State filter on page 2 leaving 64 | MISSING_FUNCTIONALITY (no pager) |

Pins (run in the committed files, green today and to stay green):

| File · case | Why it is a pin |
|---|---|
| DataGridBase.paging · 0 / 1 / 100 rows: all show, no footer (3) | Short lists must look as they do today |
| DataGridBase.paging · a sort orders the whole list | MUI already sorts the whole list; paging must not change that |
| DataGridBase.paging · first load with no rows: loading look, no footer | Today's loading look stays |
| DataGridBase.paging · export / copy from page 1 hold all 243 in sort order (2) | Export already reads every sorted row |
| ThirdPartyPackagesSection.paging · a failed load shows why, no footer | The section keeps its own failure look |
| RefinementView.paging · a Team with 38 Work Items in refinement sees them all, no footer | Short lists must look as they do today |

## E2E — 1 case, BLOCKED until DELIVER

`Lighthouse.EndToEndTests/tests/specs/settings/ThirdPartyPackagesPaging.spec.ts` is `test.skip`. It was
type-checked (`pnpm tsc --noEmit`), Biome-checked and listed by `playwright test --list` (1 test, 1 file);
it was **not** run against a live instance in DISTILL, so its POM members are unproven in a browser. DELIVER
runs it live before un-skipping (see the hand-off note in `feature-delta.md`).
