# Mutation testing — 6711 (Search on the feature list)

Run 2026-11-27 on the story's frozen code, rebased onto `origin/main`. Gate is 80 % kill rate on each stack the story
touched.

| stack | score | tested | killed | survived | compile error | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Frontend (StrykerJS 10.1.1), first run | 76.76 % | 143 | 109 | 33 | 1 | 14 m 18 s |
| Frontend, after one kill round (incremental) | **90.14 %** | 143 | 128 | 14 | 1 | 5 m 39 s |
| Backend | N/A | | | | | |

Backend N/A: the story's only backend change is one appended enum value, `UsageDataEventName.FeatureListSearched = 33`,
with no logic; `UsageDataDisclosureTest` and the event shape tests pin it against `docs/settings/usagedata.md`.

Configs: `stryker.6711.frontend.json` (line ranges of the changed code, `perTest` coverage, incremental) and
`vitest.stryker.mutation.ts` (copied to `Lighthouse.Frontend/vitest.stryker.6711.ts` for the run; that name is
gitignored).

## Frontend per file (after the kill round)

| file | killed | survived | score |
| --- | --- | --- | --- |
| `DataGrid/quickSearch.ts` | 44 | 5 | 89.80 % |
| `DataGrid/DataGridBase.tsx` (search lines) | 27 | 4 | 87.10 % |
| `DataGrid/GridSearchBox.tsx` | 11 | 2 | 84.62 % |
| `DataGrid/DataGridToolbar.tsx` (search line) | 3 | 0 | 100 % |
| `FeatureListDataGrid/FeatureListDataGrid.tsx` | 15 | 0 | 100 % |
| `FeatureListDataGrid/columns.tsx` (name column) | 6 | 2 | 75.00 % |
| `pages/Features/FeaturesView.tsx` (Portfolios column) | 1 | 1 | 50.00 % |
| `DeliveryGrid/deliveryFeatureRow.tsx` | 20 | 0 | 100 % |
| `pages/Teams/Detail/TeamFeatureList.tsx` | 1 | 0 | 100 % |

### Closed by the kill round

Six new test files, none of the approved oracle files touched:

- `quickSearch.columns.test.ts`: a column that brings its own quick filter is kept as it is; boolean, actions and
  custom columns are left alone; a plain column gains a search that matches its shown text in any letter case; blank
  text builds no search.
- `GridSearchBox.test.tsx`: no clear button while the box is empty; one once text is typed, and it empties the box.
- `DataGridBase.searchOwner.test.tsx`: the owner passed on a re-render hears the typing, not the earlier one.
- `FeatureListDataGrid.blankSearch.test.tsx`: typing only spaces reports no use and says nothing about matches.
- `FeaturesView.portfolioSearch.test.tsx`: a Feature is found by the name of any Portfolio it belongs to.
- `deliveryFeatureRow.teamText.test.tsx`: the Team column reads "Unassigned" when no team carries work, names the one
  team that does, and joins two with ", ".

### Accepted survivors

- `quickSearch.ts` 41 (the three names and the empty set) and 50 (the `?? ""` fallback): with the skip list emptied,
  a boolean, actions or custom column is wrapped too, but the shown-text matcher only ever matches strings and numbers,
  so the wrapped column still never matches. No user-visible change.
- `DataGridBase.tsx` 303, 307 (the "has a search" guard) and 308 (`apiRef.current?.`): without a search box the effect
  sets the quick filter to `[]`, which is MUI's own empty state, so dropping the guard changes nothing; `apiRef.current`
  is always set inside the effect.
- `DataGridBase.tsx` 763 (`.trim()` dropped from the no-match condition): **not proven equivalent.** The new
  blank-search test types spaces while a column filter leaves no Feature and expects no "No Features match" text, and
  it still passes under this mutant, so MUI apparently does not show the custom label in that state; why was not
  traced. Worst case, if it were reachable, someone who typed only spaces would read `No Features match "   ".` instead
  of the grid's plain no-results text. Left as it is because the gate is met and the effect is cosmetic.
- `GridSearchBox.tsx` 39: the `sx` margin and minimum width are layout only; jsdom does not lay out.
- `columns.tsx` 63, 64 (`rowOf(first)?.name`): MUI sorts only rows it holds, so the row lookup never returns null.
- `FeaturesView.tsx` 51 (`renderCell` → undefined): MUI then renders the column's formatted value, the same joined
  Portfolio names, so the cell reads the same.

The one compile error is a mutant TypeScript rejects; it never ran.
