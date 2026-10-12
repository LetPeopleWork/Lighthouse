# Mutation testing — 6202 (Lists beyond 100 rows)

Run 2026-10-12 on the story branch, rebased on `origin/main` @ `f0d37f3c5`. Gate is 80 % kill rate on each
stack the story touched.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend | N/A — the story changes no backend file | | | | | |
| Frontend (StrykerJS, vitest runner, `perTest`) | **89.61 %** | 77 | 69 | 8 | 0 | 8 m 08 s |

Configs: `stryker.6202.frontend.json`, `vitest.stryker.mutation.ts` (copied to
`Lighthouse.Frontend/vitest.stryker.6202.ts` for the run).

The first pass scored 84.81 % (79 mutants, 12 survivors). Two survivors were real gaps and now have tests;
two more sat on `getRowHeight={() => "auto"}`, a line inside the first range that this story never
changed, so the range was narrowed around it.

## Scope

Only the lines this story changed, by range:

| file | lines | tested | survived | score |
| --- | --- | --- | --- | --- |
| `DataGrid/DataGridBase.tsx` | 110-148, 186, 268, 317-320, 369-371, 373-374 | 32 | 5 | 84.38 % |
| `DataGrid/RowRangeFooter.tsx` | whole file | 17 | 1 | 94.12 % |
| `DataGrid/DataGridToolbar.tsx` | 49-64 | 8 | 0 | 100 % |
| `Settings/SystemInfo/ThirdPartyPackagesSection.tsx` | 48-65, 166-168 | 7 | 2 | 71.43 % |
| `Teams/Detail/Refinement/EnoughForLine.tsx` | 57-66, 104-111 | 13 | 0 | 100 % |

`types.ts` only narrows a type and has nothing to mutate.

## Closed by this pass

Both in `DataGridBase.paging.test.tsx`, "A list follows what its page hands it on every render":

- `useSpacingInWholeList` deps `[apiRef, getRowSpacing]` → `[]`. The grid would keep the first spacing rule
  it was given. The Refinement tab hands it a new one whenever the high end of the need changes, so the
  space for the enough-for line would stay where it first was. Now pinned: after a re-render with a new
  rule, the first row carries the new 36 px.
- `gridSlots` deps `[slots]` → `[]`. The grid would keep the first slots it was given and ignore new ones.
  Now pinned: a new loading overlay replaces the first one.

Each test was run against its mutant applied by hand and failed, then passed on the real code.

## Accepted survivors

| where | mutant | why it cannot be killed meaningfully |
| --- | --- | --- |
| `DataGridBase.tsx:120` | `apiRef.current?.subscribeEvent` → `.subscribeEvent` | The effect runs after the grid has mounted, when the grid has always filled in `apiRef.current`. |
| `DataGridBase.tsx:122` | `page > lastPage` → `page >= lastPage` | On the last page, `setPage(lastPage)` moves nowhere. Equivalent. |
| `DataGridBase.tsx:123` | `apiRef.current?.setPage` → `.setPage` | Called from a grid event, which only fires while the grid is mounted. |
| `DataGridBase.tsx:125` | deps `[apiRef]` → `[]` | `useGridApiRef` returns the same ref on every render. Equivalent. |
| `DataGridBase.tsx:369` | `pageSizeOptions={[PAGE_SIZE]}` → `[]` | The page size is still 100 and the footer still offers no choice, both pinned. The only difference is a development-console warning from MUI. |
| `RowRangeFooter.tsx:32` | `rootElementRef.current?.scrollIntoView` → `.scrollIntoView` | The footer is a part of the grid's root element, so the root is there whenever a footer arrow can be clicked. |
| `ThirdPartyPackagesSection.tsx:59` | `if (searchedFor.current === searchText) return` → `if (false)` | The effect re-runs only when the search text changes, and on mount, when the list is already on its first page, `setPage(0)` moves nowhere. The guard only matters if the toolbar remounts while a later page is open, which nothing does today. |
| `ThirdPartyPackagesSection.tsx:61` | `apiRef.current?.setPage` → `.setPage` | Rendered inside the grid's own toolbar, so the grid context and its api are always present. |
