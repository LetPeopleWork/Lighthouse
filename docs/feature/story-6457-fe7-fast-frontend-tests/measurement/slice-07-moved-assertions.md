# Slice 07 - Refinement view: universe, preserved set, delta

All timing figures in this slice are indicative (not quiet): the machine was shared with a parallel session (load
average about 3.4 on 4 cores during the runs). The rewrite slices start before the configuration spike's outcome is
applied (it adopted no configuration change), so the payoff has to come from the rewrite.

## Pre-slice commit

`15bf02cb23aa0259128e21a4b37ae8b087788078` (HEAD of main when this was declared). The close-time alternating
before/after timing pairs check this commit out for the "before" side.

## Universe

Nothing outside this list may change in the slice. This slice is behaviour-free: no production behaviour changes,
no assertion is weakened or dropped without a row in the table at the end.

### Search result: what is real logic inside the components

The roadmap assumed the paging and filter arithmetic sits in `RefinementView.tsx`. It does not. Read first:

- `RefinementView.tsx` (283 lines) holds no paging or filter arithmetic. It assembles the marking and passes the rows
  to `RefinementGrid`. Its only pure-looking inline rules are `describeHeading` / `describeReadyByVotes` (the
  heading's count, module-private) and `describeWorkItem`.
- `RefinementGrid.tsx` (106 lines) only wires columns, `useEnoughForLine` and `DataGridBase`. No arithmetic.
- Pages are made by `DataGridBase` (shared by every list in the app): `PAGE_SIZE = 100` in `RowRangeFooter.tsx`,
  the range text "1–100 of 143" is MUI's own footer, and the filter itself is MUI's. The one piece of our arithmetic
  there is `useSpacingInWholeList` (`index = page * pageSize + indexRelativeToCurrentPage`,
  `isLastShown = index === expandedRowCount - 1`), module-private in `DataGridBase.tsx`. That file is shared and is
  not in this slice's declared files; it is **left alone** (its page-offset rule stays proven by the one large
  render and by `DataGridBase.paging.test.tsx`).
- The rules the paging file's 7 placement cases, numbering case and filter case drive are in
  `EnoughForLine.tsx` and `enoughForPlacement.ts`:
  - Already pure and unit tested (`enoughForPlacement.test.ts`), **not extracted again**: `lineBeside(high, row)`
    (which row carries the line, which side, which sentence), `isNumbered(high, index)`, `describeEnoughFor`,
    `describeNeededNumber`. Its existing test sweeps `high` against a shown count of 6 and below, never against 143
    rows or a page boundary.
  - Module-private or inline today in `EnoughForLine.tsx` (this is what is really still inline):
    - in `EnoughForRow`: `isLastShown: props.index === shownCount - 1`, `shown: props.index + 1` (the number the
      "All N ... are needed" sentence counts) and the pairing of both with `isNotVisible`;
    - `spaceForTheLine(high)(row)` (which side reserves the 36 px), a closure over `lineBeside`;
    - in `NeededNumber`: `number = index + 1` and the `isNumbered` guard.
- `RefinementView.comments.test.tsx` (24 tests, 7.75 s) and `RefinementView.votes.test.tsx` (33 tests, 7.84 s)
  are named by the roadmap but are **not driven by paging or filter arithmetic**: they use the small default
  fixture (no test renders more than a handful of rows, none is over 0.94 s, neither has an allow-list entry, the
  only real wait is a 0 ms `setTimeout` in `comments.test.tsx` line 123). Their rules (name prompt, condition,
  comment length, refusal wording) already sit in tested pure modules (`voteWording.ts`, `sizingLogWording.ts`,
  `useVoteCasting.ts`, `useCommentAdding.ts` with their own tests). **They stay in the universe as preserved
  tests only**: their ids are in the before set and must be unchanged at close; 08-02 has nothing to change in them
  unless it finds a real wait while rewriting (none found now). If 08-02 changes either file it must say so in its
  close report. This differs from the roadmap's `files_to_modify`, and is the honest reading of the code.
- `RefinementView.test.tsx` (14 tests, 3.37 s) is **in the universe** because of its one allow-listed test
  "handles three hundred Work Items, counting all of them and starting at the top of the backlog" (1.23 s here).
  The arithmetic behind it is not the paging rule: it asserts the heading counts `workItems.length` (300, more than
  a page) and that the first listed row is GR-1000. The count is `describeHeading` (inline, module-private in
  `RefinementView.tsx`); "starts at the top" is `DataGridBase` showing backlog order. The 300 rows cost the time,
  not the logic.

### Proposed extractions (files beside the component)

The exact function split is decided in 08-02 after reading the files closely; the file names are the commitment.

| Extraction | From | New file | Unit-test file |
|---|---|---|---|
| The row facts the line needs: `shownRowOf(index, shownCount, isOutOfSight)` (the `isLastShown` rule), `shownCountOf(index)` (the `index + 1` the sentence counts), `neededNumberAt(high, index)` (number or none), `spaceKeptFor(high, row)` (moved `spaceForTheLine`) | `EnoughForLine.tsx` | `src/pages/Teams/Detail/Refinement/refinementPaging.ts` | `src/pages/Teams/Detail/Refinement/refinementPaging.test.ts` |
| `describeHeading`, `describeReadyByVotes` (the heading count; `describeWorkItem` only if cheap) | `RefinementView.tsx` | `src/pages/Teams/Detail/Refinement/refinementHeading.ts` | `src/pages/Teams/Detail/Refinement/refinementHeading.test.ts` |

`refinementPaging.ts` composes the existing `lineBeside` and `isNumbered`; it does not copy them. The page split
itself (100 rows, "101–143 of 143") and the filter are MUI and `DataGridBase`, not functions of ours, and cannot be
asserted on a function: they stay asserted in the one large render. What gets asserted directly on functions is
every line placement and number the paging file checks now, with `shownCount` 143 and 64 and the page rows
`index` 0 to 99 and 100 to 142 (page boundary at index 99/100), the last-page content (`shown` = 143, `AllNeeded`)
and the filtered result (64 rows, `AllNeeded`, `shown` 64, high 120).

### Production files that may change

- `src/pages/Teams/Detail/Refinement/EnoughForLine.tsx` (calls `refinementPaging.ts`). The roadmap did not name it;
  it holds the inline arithmetic named above.
- `src/pages/Teams/Detail/Refinement/RefinementView.tsx` (calls `refinementHeading.ts`; the roadmap file)
- the two new production files above

Not changed: `DataGridBase.tsx`, `RowRangeFooter.tsx`, `enoughForPlacement.ts`, `RefinementGrid.tsx`,
`refinementColumns.tsx`, `useRefinement.ts`.

### Test files edited (existing)

- `src/pages/Teams/Detail/Refinement/RefinementView.paging.test.tsx`
- `src/pages/Teams/Detail/Refinement/RefinementView.test.tsx` (only the 300-row test)
- `src/tests/timeBudget/slowTests.ts` (delete the entries 08-02 fixes)

Preserved untouched (must stay green as they are; they hold wiring named below):
`RefinementView.comments.test.tsx`, `RefinementView.votes.test.tsx`, `RefinementView.enoughFor.test.tsx`,
`EnoughForLine.test.tsx`, `enoughForPlacement.test.ts`, `DataGridBase.paging.test.tsx`.

## The test that pages to GR-0131: every assertion it makes

`RefinementView.paging.test.tsx` :: Team Gravity's 143 Work Items in refinement can all be reached Jonas pages to
GR-0131, the 131st Work Item in refinement, and votes on it. 2.44 s in the before run (allow-list ceiling 5.0 s,
the slowest of the suite). Fixture: 143 rows (`workItemsInRefinement(143)`, the first 64 "Next", the rest
"Analysing"), a browser that voted before as "Jonas", `castVote` resolved with GR-0131 voted "Yes". Its steps,
with every assertion quoted:

```ts
await theRowOf(reference(1));                       // waits for GR-0001 to be listed

expect(screen.getByText("1–100 of 143")).toBeInTheDocument();
expect(theRowsOnThisPage().map((shown) => shown.referenceId)).toEqual(
	references(1, 100),
);

await goToNextPage(user, "101–143 of 143");         // click "Go to next page", then findByText("101–143 of 143")
expect(theRowsOnThisPage().map((shown) => shown.referenceId)).toEqual(
	references(101, 143),
);

await user.click(theButton(await theRowOf(reference(131)), "Yes"));

await waitFor(() =>
	expect(sizingLogService.castVote).toHaveBeenCalledWith(
		GRAVITY_TEAM_ID,
		reference(131),
		{ answer: "Yes", channel: "Web", voterName: "Jonas" },
		key,
	),
);
await waitFor(async () =>
	expect(
		theButton(await theRowOf(reference(131)), "Yes"),
	).toHaveAttribute("aria-pressed", "true"),
);
```

Five `expect(` calls plus two implicit waits (`theRowOf(reference(1))`, `findByText("101–143 of 143")`). None of
them is arithmetic of ours: they are the page split, the page-2 row set, and a vote cast on a page-2 row. This
test therefore cannot shrink without losing an assertion; the plan below keeps it whole and makes it the one large
render.

## Allow-list entries (`src/tests/timeBudget/slowTests.ts`) belonging to these files

11 entries of 23 (the file's whole list). Ceiling in seconds on a quiet machine; "before" is the duration in the
indicative (not quiet) run of this step. 08-02 deletes the ones it fixes. Note: the paging file has 10 entries, not
11 (the 38-row test has none); with the `RefinementView.test.tsx` one that makes 11.

| # | File | Test (name after the file's describe) | Ceiling (s) | Before (s, not quiet) |
|---|---|---|---|---|
| 1 | `RefinementView.paging.test.tsx` | Jonas pages to GR-0131, the 131st Work Item in refinement, and votes on it | 5.0 | 2.44 |
| 2 | same | with 0 needed, the line sits where it falls in the whole list (page 1: { side: 'above', by: 'GR-0001', …(1) }, page 2: null) | 2.5 | 1.07 |
| 3 | same | with 20 needed, ... (page 1: { side: 'below', by: 'GR-0020', …(1) }, page 2: null) | 2.5 | 0.94 |
| 4 | same | with 100 needed, ... (page 1: { side: 'below', by: 'GR-0100', …(1) }, page 2: null) | 2.5 | 0.96 |
| 5 | same | with 101 needed, ... (page 1: null, page 2: { side: 'below', by: 'GR-0101', …(1) }) | 2.5 | 0.94 |
| 6 | same | with 120 needed, ... (page 1: null, page 2: { side: 'below', by: 'GR-0120', …(1) }) | 2.5 | 0.96 |
| 7 | same | with 143 needed, ... (page 1: null, page 2: { side: 'below', by: 'GR-0143', …(1) }) | 2.5 | 0.91 |
| 8 | same | with 150 needed, ... (page 1: null, page 2: { side: 'below', by: 'GR-0143', …(1) }) | 2.5 | 0.82 |
| 9 | same | with 120 needed, numbers the first page 1 to 100 and continues at 101 on the second, up to 120 | 2.5 | 0.93 |
| 10 | same | with 120 needed, a State filter on the second page that leaves 64 shows them all, the line under the last | 4.5 | 1.79 |
| 11 | `RefinementView.test.tsx` | The Refinement tab lists the Work Items in refinement handles three hundred Work Items, counting all of them and starting at the top of the backlog | 3.0 | 1.23 |

(Full names are in `slowTests.ts`; the placement ones read "Team Gravity's 143 Work Items in refinement can all be
reached with N needed, the line sits where it falls in the whole list (page 1: %o, page 2: %o)".) The 38-row test
(0.47 s) has no entry and stays unchanged.

## Preserved set (before)

`measurement/slice-07-ids-before.txt`: 82 ids, one per line, sorted, as `file :: fullName`, taken from a run of the
four test files (82 passed, 0 skipped, 0 failed; file paths relative to `Lighthouse.Frontend/`). Per file:
`RefinementView.paging.test.tsx` 11, `RefinementView.test.tsx` 14, `RefinementView.comments.test.tsx` 24,
`RefinementView.votes.test.tsx` 33. Surviving ids must be unchanged at close. A test that moves keeps its file and
name, or is listed in the Moved assertions table below.

Command (in `Lighthouse.Frontend`):

```
LIGHTHOUSE_TIME_BUDGET=warn TZ=Europe/Zurich pnpm exec vitest run \
  src/pages/Teams/Detail/Refinement/RefinementView.paging.test.tsx \
  src/pages/Teams/Detail/Refinement/RefinementView.comments.test.tsx \
  src/pages/Teams/Detail/Refinement/RefinementView.votes.test.tsx \
  src/pages/Teams/Detail/Refinement/RefinementView.test.tsx \
  --reporter=default --reporter=json --outputFile.json=<scratch>/before.json
```

The config pins the json report to the gitignored `Lighthouse.Frontend/test-results.json`, so
`--outputFile.json` was not honoured; the ids and seconds were read from that file (copied to scratch). Nothing in
the repo changed (`git status` shows only the three untracked epic-6033 files that were there at the start).

## Before figures (indicative, not quiet)

Summed per-test durations of the touched files (the figure the close-time pairs compare), one run:

| File | Tests | Summed seconds |
|---|---|---|
| `RefinementView.paging.test.tsx` | 11 | 12.22 |
| `RefinementView.test.tsx` | 14 | 3.37 |
| `RefinementView.comments.test.tsx` | 24 | 7.75 |
| `RefinementView.votes.test.tsx` | 33 | 7.84 |
| Sum | 82 | 31.19 |

Wall clock of the four files together (vitest "Duration"): 14.97 s. The paging file alone is 39 % of the summed
seconds and holds 10 of the 11 allow-listed tests; the slowest tests of the other three are 1.23 s (the 300-row
test), 0.94 s (comments) and 0.66 s (votes).

## Coverage (before)

`src/pages/Teams/Detail/Refinement/**`, from a run of the whole directory (24 test files, 356 tests, all passed;
a directory's coverage needs all of its tests, since several other files drive the same sources):

```
LIGHTHOUSE_TIME_BUDGET=off TZ=Europe/Zurich pnpm exec vitest run src/pages/Teams/Detail/Refinement \
  --coverage.enabled --coverage.include='src/pages/Teams/Detail/Refinement/**' \
  --coverage.reporter=text --coverage.reporter=json-summary
```

| Measure | Before |
|---|---|
| Lines | 100 % (551 of 551) |
| Statements | 100 % (563 of 563) |
| Functions | 100 % (177 of 177) |
| Branches | 99.05 % (316 of 319) |

The three uncovered branches: `VoteControl.tsx` line 35, `sizingLogWording.ts` line 51, `useRefinement.ts` line 11 -
none in the code this slice touches. Coverage must not fall; new files must be covered by their own unit tests, and
the branch denominator will grow with them.

## Wiring to keep

For each page-level test 08-02 would delete or shrink, the test that survives and must fail when the component's call
site of the extracted function breaks. 08-02 proves each row by breaking the call site temporarily and reverting,
as in slices 5 and 6; none was mutated in this declaration step, so "must fail" is the claim to verify, not a
finding. A reason string asserted is not button state asserted; a test name must stay true after shrinking (so a
test whose name says "143" or "page 2" either keeps its large fixture or is removed with a row, never shrunk
under the same name).

| Page test 08-02 would delete | Behaviour it proves today | Surviving test that must fail if the call site breaks |
|---|---|---|
| The 7 placement cases (0, 20, 100, 101, 120, 143, 150 needed) | The line sits beside the right row on the right page, with the 36 px space kept there and nowhere else | **On the page-1 side and on the shown-count side:** `RefinementView.enoughFor.test.tsx` :: "numbers the first rows up to the high end and draws the line after them", "draws the line after the last row when exactly as many are listed as needed", "says all of them are needed, after the last row, when fewer are in refinement than needed", "numbers nothing and draws the line before the first row when nothing is needed", and the two "reserves the space the line takes ..." tests (breaking `shownRowOf` or `spaceKeptFor` at the `EnoughForLine.tsx` call site fails these). **On the page-2 side (the row's place in the whole list, page offset):** only a 143-row render proves it, so the GR-0131 test, the one large render, must grow the page-2 line and margin check (`expectOnThisPage` with the line under the expected row, e.g. high 120 under GR-0120 on page 2 and nothing on page 1). If it cannot without changing its fixture's need, 08-02 reports it as a gap instead of removing the cases |
| Numbering case (120 needed: 1–100, then 101–120, then blank) | `#` cell counts across pages; the progress `img` label "100 of 120 needed before the next Refinement" | `RefinementView.enoughFor.test.tsx` :: "numbers the first rows up to the high end and draws the line after them" and "tells a screen reader what each number counts instead of reading the line" (small list, call site of `neededNumberAt`); across pages, again the GR-0131 test extended with the `#` cells of page 2 |
| State filter on page 2 leaving 64 (all needed, line under the last, no footer) | A filter changes the shown count the sentence and `isLastShown` use; the footer vanishes at 100 or fewer | `RefinementView.enoughFor.test.tsx` :: "says all the shown ones are needed when a filter leaves fewer than needed" and "draws the line after the last shown row when a filter leaves exactly as many as needed" (these use the real column-menu filter on a small list, so they fail if the shown-count read in `EnoughForRow` breaks). The footer disappearing is already `RefinementView.paging.test.tsx` :: "a Team with 38 Work Items in refinement sees all of them, with no footer" (stays, 0.47 s) and `DataGridBase.paging.test.tsx` |
| The 300-row test (`RefinementView.test.tsx`) | The heading counts all 300, more than a page; the first row is GR-1000 | Heading: `RefinementView.test.tsx` :: "asks for this Team's refinement and counts what it holds in the heading above a grid" (fails if the `describeHeading` call site breaks); first row: "shows the Work Items in the order they came, which is backlog order". The beyond-a-page count moves to `refinementHeading.test.ts` (300 and 1); if 08-02 would rather keep a 300-row render it keeps the test and its allow-list entry |

The GR-0131 test, the 38-row test and every test in `RefinementView.comments.test.tsx` and
`RefinementView.votes.test.tsx` stay as they are, ids unchanged. The 38-row test stays because its name states a
property of the footer that only a render shows.

## Goal for 08-02 to plan against

1. Extraction commit first: `refinementPaging.ts` and `refinementHeading.ts` (names above), called from
   `EnoughForLine.tsx` and `RefinementView.tsx`, no test touched; the unmodified tests of the four files, plus
   `EnoughForLine.test.tsx`, `enoughForPlacement.test.ts` and `RefinementView.enoughFor.test.tsx`, pass.
2. Test commit: page boundaries (index 99 vs 100, `shownCount` 143), last-page content (`shown` 143, "all needed"),
   numbering across the boundary, the filtered result (64 rows) and the heading count (300) asserted directly on the
   functions; exactly ONE test renders the large 143-row list (the GR-0131 test, extended as the wiring table says);
   the nine other large-fixture tests (rows 2 to 10 above) are removed with a row each, since their names state
   "143", "page 1/2" or "64" and would be untrue on a small fixture; the 300-row test is removed or kept per the
   last row of the wiring table.
3. Delete the allow-list entries for removed tests (2 to 10, and 11 if removed). Entry 1 stays unless the
   extended test is measured under budget on a quiet machine, then its ceiling is lowered or the entry deleted.
4. Close report with the 5 alternating pairs on the four test files (labelled indicative (not quiet)), allow-list
   length (23 now), tests added and removed.

## Delta and moved assertions

Filled in by 08-02: commits, test counts before and after, tests added and removed, preserved ids missing,
coverage, allow-list length, `expect(` counts per file against the extraction commit. Before: 82 tests in the four
files, allow-list length 23.

### Moved assertions

| Old test (file :: test) | Assertion text | New location (file) | New test (name) |
|---|---|---|---|
