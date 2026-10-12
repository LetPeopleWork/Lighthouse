# Slice 01: Every row of a long list is reachable

**Goal:** any `DataGridBase` list with more than 100 rows shows its range and total with page arrows, so
every row can be reached; lists of 100 rows or fewer stay exactly as they are.

## Sketch (approved by the maintainer, 2026-10-08)

```text
| GR-0098  Export settings to file       In Refinement   Yes, but...   |
| GR-0099  Rename Delivery from list     In Refinement   No votes      |
| GR-0100  Show SLE on Feature card      In Refinement   Ready         |
+----------------------------------------------------------------------+
|                                            1–100 of 143     ‹   ›    |
+----------------------------------------------------------------------+
```

Same list with 38 rows: no footer line at all, as today.

## IN scope

- `DataGridBase`: footer shown only above 100 rows (D1), fixed 100-row pages, range + arrows, no
  rows-per-page picker, nothing persisted (D2).
- All eight consumers covered by the one change (D4): `DataOverviewTable`, `FeatureGrid`,
  `FeatureListDataGrid`, `WorkItemsDialog`, `OverviewDashboard`, `ArchivedFeatureGrid`, `RefinementGrid`,
  `ThirdPartyPackagesSection`.
- `ThirdPartyPackagesSection` drops `hidePagination={false}` (D3).
- A list that shrinks to 100 rows or fewer shows all its rows, with no footer (AC-1.6).
- Export/Copy stay whole-list (D6) — pinned by a test.
- Release-notes line on #6202.

## OUT of scope

- Virtualised scroll without pages; a rows-per-page picker; remembering the page.
- Server-side paging; any change to which rows, columns, sorts or filters a list has.
- Export honouring an active column filter (open question 4).

## Learning hypothesis

- **Disproves** "a conditional 100-row footer is enough" if, after release, a reader still reports a
  missing Work Item or Feature in a long list, or the maintainer finds a page arrow is missed on the
  Refinement tab (votes still never reach Work Items past row 100).
- **Disproves** "short lists are untouched" if any @screenshot of a list with 100 rows or fewer changes.

## Acceptance criteria

AC-1.1 … AC-1.9 in `../feature-delta.md` (US-01).

## Dependencies

None blocking. Story #6248 edits `DataGridBase.tsx` too (unpushed at the time of writing): merge with it,
do not overwrite it. DESIGN answers the open questions first (page on sort/filter, `hidePagination` fate).

## Effort

About 1 day including tests. Reference class: Story #6248 slice 01a (a `DataGridBase`-wide change with
consumer spot checks).

## Production-data check

- **Dev instance (`:5169`, real history):** Settings → System → Third-party packages (well over 100
  packages from both SBOMs) and a Work Items dialog behind a Throughput chart over a long date range
  (more than 100 closed Work Items) — both must show the footer and reach the last row.
- **Demo data:** demo Teams and Portfolios sit under 100 rows per list (to be confirmed in DISTILL), so
  they prove the "unchanged" side: Ocean Explorer's Feature list shows no footer.
- No demo Team has more than 100 Work Items in refinement; the Refinement case is covered by component
  tests with 143 rows, not by E2E (E2E stays a thin sanity check).

## Dogfood moment

The maintainer opens Third-party packages on the dev instance, pages to the last package, searches for
one name and sees the footer vanish; then opens a short Team list and sees nothing new.
