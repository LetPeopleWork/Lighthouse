# Story 6202 — every row of a long list can be reached

**Pushed to `main` 2026-10-12, not yet released** · ADO Tech Debt item #6202 (no parent Epic) · workspace
`docs/feature/story-6202-lists-beyond-100-rows/` (kept as delivery history) · Lighthouse commits
`f0d37f3c5..7aa54b9dd` (26 commits, `3704bdb57` … `7aa54b9dd`), then a CI fix (the 300-row Refinement test
timed out on CI and got the heavy-render timeout) and the finalize commits (release note, this record).

## Why

Every list in Lighthouse is a `DataGridBase`, and every one of them hid its footer. MUI's community grid pages
at 100 rows by default, so a list of 243 rows showed its first 100 and nothing said more existed. Two earlier
evolution records had already met the limit (the sizing-votes and refinement-need Epics): a Team with 143
Work Items in refinement could never reach, vote on, or see the enough-for line beside the last 43. Settings →
Third-party packages was the one list that showed a footer, always, with a rows-per-page picker no other list
had.

## What users get

- A list with more than 100 rows the reader can see (after filters) shows a footer under it: the range and total,
  for example `1–100 of 243`, and previous/next arrows. Pages hold 100 rows; there is no rows-per-page choice and
  nothing about the page is remembered.
- A list of 100 rows or fewer looks exactly as before: no footer, no empty bar.
- A sort or a column filter opens page 1. A reload keeps the open page while it still exists and otherwise moves
  to the last page that does, in the same render, so a lone last row never flashes on a page that is gone.
- Pressing an arrow brings the top of the list into view, so the new page does not open far above the fold.
- Export and Copy still take every row in sort order, whichever page is open.
- Third-party packages follows the same rule. Its search opens page 1 of the results, keeping the reader's sort
  and column filter.
- On the Refinement tab, the numbering and the enough-for line follow the row's place in the whole list: with 120
  needed out of 143, page 1 numbers its rows and page 2 carries the line under row 120.

## What shipped

One story, one slice, seven DELIVER steps (`deliver/roadmap.json`): the conditional footer and page policy, the
pins for sort/filter/reload and for export, the package search, the whole-list enough-for line, a two-branch
walking skeleton on Third-party packages, and a polish step for the scroll on a page turn that the live run
asked for. Then refactor, adversarial review with one revision round, and mutation testing.

## Key decisions

- **Footer only above 100 visible rows, fixed pages of 100, no picker** (maintainer, from an approved sketch).
  Virtualised scrolling without pages was rejected.
- **The threshold counts filtered rows, not `rows.length`**, because the Work Items dialog filters through MUI's
  column filter, which never changes `rows`; a `rows.length` rule would leave a dead footer under a filtered list.
- **A conditional footer slot**, merged over the consumer's slots, rather than `hideFooter={…}` or a custom
  pager: it holds for every grid and keeps MUI's copy and the arrows' accessible names.
- **`hidePagination` removed.** A flag to opt out would only let a list hide rows again.
- **A package search opens page 1** (maintainer, MD-1): a reader cannot tell a search from a filter.
- **The enough-for line sits where it falls in the whole list, nothing added** (maintainer, MD-2). The grid hands
  `getRowSpacing` the row's whole-list index, so the reserved space and the drawn line share one basis.
- **No DEVOPS wave and no usage-data event** (maintainer): a frontend fix that makes existing rows reachable is not
  a feature anyone chooses to use.

## Quality gates after DELIVER

- `des-verify-integrity`: all 7 steps have complete DES traces.
- `pnpm test` 459 files / 7204 tests green; `pnpm build` clean, Biome "No fixes applied". E2E: the Third-party
  packages spec passed live on a build carrying main's real SBOMs (233 packages) and on the placeholders.
- Adversarial review: findings fixed in one revision round (test-precision and naming fixes, the search-from-a-gone-
  page case, the dialog paging test after the #6248 merge).
- Mutation (StrykerJS, changed lines only): **89.61 %**, 69 of 77 killed. Two survivors were real gaps — the grid
  kept the first spacing rule and the first slots it was handed — and now have tests. The 8 left are equivalent;
  `mutation/results.md` explains each.

## Lessons learned

- **Only `main` builds serve real SBOMs.** PR builds serve one-component placeholders, so a spec that assumes
  "well over 100 packages" fails off main. The walking skeleton reads the package count from the build and runs
  the long branch only above 100. Consequence: PR CI's E2E proves only the short list; the long list was proven
  locally on a downloaded SBOM artifact.
- **MUI moves off a page that is gone one tick late,** and for that render shows the list's last row alone under
  a range that names the new page. Subscribing to `rowCountChange` and moving at once removes the frame.
- **The page arrows sit under the last row,** so without a scroll the new page opens off-screen. Nothing in jsdom
  shows that; the live run did.
- **A memo over props needs a test that changes the prop.** Mutation testing found that emptying two dependency
  lists broke nothing in the suite, while the Refinement tab really does hand the grid a new spacing rule when the
  need changes.
- **The DESIGN expectation that `settings/systeminfo.png` would change was wrong:** the shot is cropped to the
  viewport and never reaches the packages list. No screenshot changed.

## Known gaps

- Open question for the maintainer: should a page turn scroll high enough to show the toolbar and search as well,
  not only the top of the grid? Kept as is for now.
- The page size is MUI's default 100; `PAGE_SIZE` drives the footer threshold and `pageSizeOptions`, not the page
  size itself. Setting `initialState.pagination.paginationModel.pageSize` would make the constant the one source.
- Export ignores column filters (existing behaviour, out of scope; a separate item for the maintainer to raise).
- A list whose load failed can read as "no data" (recorded in DISTILL as a follow-up candidate, not in scope).

## Phase B — lasting artifacts

No design, ADR, walking-skeleton or UX-journey files to migrate: DESIGN lives in `feature-delta.md` and wrote no
ADR. The workspace stays as delivery history.

## Finalize checklist

- **Docs prose — none needed.** No page in `docs/` says a list shows all its rows or describes page controls;
  `docs/settings/systeminfo.md` does not mention the packages list's footer.
- **Per-feature screenshots — none changed.** Short lists render as before (pinned by tests), and
  `settings/systeminfo.png` does not reach the packages list.
- **Demo data — N/A,** the fix adds no data shape, and the tests and the live check build their own long lists.
- **Website — none found.** No copy or image of the packages list or its picker in the website repo.
- **Lighthouse-Clients — N/A,** frontend-only; no API or payload change.
- **Release note** — added to the vNext section of `docs/releasenotes/releasenotes.md`.

## Release note (draft)

**Every row of a long list can now be reached.** A list used to stop at its first 100 rows without saying there
were more, so a Team with 143 Work Items in refinement could not vote on the last 43. A list of more than 100
rows now says how many it holds, for example `1–100 of 143`, and its arrows page to the rest. Sorting,
filtering, the search in Third-party packages, and Export and Copy all cover the whole list. Lists of 100 rows
or fewer look as before.
