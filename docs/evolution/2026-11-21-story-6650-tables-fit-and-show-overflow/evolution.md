# Tables fit their width and show when columns are off screen (ADO #6650)

Delivered 2026-11-21. Workspace: `docs/feature/story-6650-tables-fit-and-show-overflow/` (`brief.md`,
`architecture.md`, `feature-delta.md`, `acceptance.md`, `runs/`, `mutation/`).

## What was delivered

Every shared table now fits its width. Long text (names, titles, dependency lists) is cut with an ellipsis, and the
full text shows in a tooltip on hover and on keyboard focus. Numbers, dates and states are never cut. The actions
column is pinned as the last column of every shared table that has one, so Delete and the row actions stay in reach.
When columns are out of sight, a soft shadow appears on that edge and screen readers hear "This table scrolls sideways.
More columns to the right." On a phone the table scrolls inside itself, not the page. On the Overview tables, a phone
folds Details, Edit, Clone and Delete into one pinned "⋮" menu. A width the person set is kept, and Reset layout returns
to the fitted layout. Frontend only: no API, no migration, no setting.

Tables covered: report list, Overview (Teams and Portfolios), Feature list, Work Items, Connections, Refinement.

## Slices and key commits

Commits are found with `git log --oneline --grep="(tables)"`.

| Slice | What it delivers | Key commits |
|---|---|---|
| S1 | The shared fit and the pinned actions column, shown on the report list | `4619027a8`, `390f5022b` |
| S2 | The overflow shadow per edge and the screen-reader text | `86c4ddd39`, `6b59da436`, `a62999c44` |
| S3 | The Overview tables fit, pin Delete and show the cue; phone menu (decision 7) | `1d76ae4ce`, `4ed4c003d`, `40aac6e68` |
| S4 | The Feature list, Work Item, Connections and Refinement tables; long dependency lists | `c6cea34c6`, `9d686fbc2` |
| Close | Overview actions cell as its own component; killed surviving mutants; mutation record | `c1af7db8c`, `b31b9088f`, `a8cf8b0bd` |

## Maintainer decisions

1. Both: columns fit better (shrink, truncate with tooltip) and a cue shows when columns still do not fit.
2. The actions column is pinned to the right edge on every shared table that has one.
3. Phone width: the table scrolls inside its box with the cue; no card-per-row layout.
4. Sketch approved: Name at least 160 px, other text columns at least 120 px; numbers, dates and states never cut; a
   width the person set is kept, Reset returns to the fitted layout.
5. Cue style: a soft inner shadow only, no text hint; screen readers still get the overflow text.
6. Rigor: S1 `thorough`; S2 to S4 `standard`.
7. Overview at phone width: the four action icons collapse into one pinned "⋮" menu (2026-11-21).
8. Known limitation accepted (2026-11-21), see below.

## Known limitation and follow-up

At 375 px, opening the "⋮" menu on the second Overview table can make the browser scroll that table to its end (a native
scroll when a menu item takes focus; cause not found after three craft passes). Focus return and the menu work as built.
**Follow-up: a Bug is to be created with the maintainer.** Not created here.

## EXAMINE waived

The source-blind EXAMINE was not run: `nwdes` refuses on Windows (it needs no-follow file opens, available on
Linux and macOS only). Recorded as waived, pending the maintainer. The whole-candidate review was accepted
(`runs/review.candidate.json`).

## Mutation

Per slice: S2 83.8 %, S3 100 %, S4 100 %. Final run, after code changed following the slice runs: 86.32 % overall;
`DataGridBase.tsx` 78.79 % with the survivors recorded in `mutation/final-frontend.md`. Backend: N/A.

## Open, non-blocking review findings

- The overflow test duplicates the shared kit's grid mock.
- One Overview E2E test is long.
- An earlier Sonar-rule batch (`1d97f2f45`) changed refetch, flatMap and `.at` behaviour.
- `ColumnOrderDialog` versus the pinned column: how they interact is not pinned.
- Feature list test hygiene: an unrestored `getBoundingClientRect` spy, and a bound that checks the right edge only.
- Report list spec: a few more one-shot reads could be retrying assertions, and one grid locator is page-wide.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | Done. Short notes on cut text, the pinned actions column and the overflow shadow in `docs/features/features.md`, `docs/reports/reports.md`, `docs/teams/teams.md` and `docs/portfolios/portfolios.md` (the last two also name the phone "⋮" menu). Written with the default terms. No page names Connections, Refinement or Work Item table layout, so none changed. No page describes column widths or Reset layout, so none changed. |
| Screenshots | Deferred: needs a clean backend (none was running on the laptop); run with `/update-docs`. Tests showing these tables, in `Lighthouse.EndToEndTests/tests/specs/screenshots/Screenshots.spec.ts`: "Take @screenshot of the populated overview page", "Take @screenshot of the Features page", "Take @screenshot of the Reports page with reports picked", "Take @screenshot of the Refinement tab and a Work Item's votes and comments", "Take @screenshot of the connections settings tab". Run each with `--grep`, not the whole suite. |
| Demo data | N/A, because the feature changes how tables draw, not what data exists; `DemoDataFactoryTest` is unaffected. |
| Website assets | N/A, because no asset under `docs/assets/` was changed in this commit; the screenshot re-run above may change some, and `/update-docs` then checks which the website hot-links. |
| Clients (CLI, MCP) | N/A, because no API route or DTO changed. |
| Context map, `ARCHITECTURE.md` | N/A, because the feature adds no concept, context or edge; ADR-270 and `ARCHITECTURE.md` do not name the shared table's behaviour (grep for the data grid found nothing). |
| Usage-data event | N/A, because DEVOPS decided so (maintainer, 2026-11-17): the existing report and delete events already show use. |
| Mutation | Done. Frontend 86.32 % overall (slices 83.8 / 100 / 100); `DataGridBase.tsx` at 78.79 % with recorded survivors. Backend N/A, no backend change. |
| Release notes | N/A; written at /release. |
