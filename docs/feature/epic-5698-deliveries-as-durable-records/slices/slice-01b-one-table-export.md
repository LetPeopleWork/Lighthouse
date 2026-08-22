# Slice 01b — One table, Delivery as the first row

**Epic** #5698 · **Story** US-01 · **ADO** #4309 (reopened, Resolved on verification) · **Estimate** ≤1 day

Corrects what slice 01 shipped. Verification found the exported artifact unreadable: a two-column
header block sitting above a grid whose derived cells exported empty or as raw JSON.

## The defect behind every symptom

`DataGridToolbar.getGridData()` scrapes the MUI grid — `apiRef.getCellValue(rowId, field)` then
`JSON.stringify` for anything object-shaped. The export therefore sees raw row fields, never what the
cell renders:

| Symptom | Cause |
|---|---|
| Forecast exports as a JSON array | `field: "forecasts"` is the raw array, no `valueGetter` |
| Likelihood always empty | `field: "likelihood"` does not exist on `IFeature`; the chip reads `delivery.featureLikelihoods` |
| Dependencies exports `0` | `valueGetter` returns `.length`, which exists for sorting |
| Team, Progress empty | render-only columns, no backing field |
| Warnings exports `false` | raw boolean |

## Decision

The export is a **status-report document**, not a screenshot of the grid. The caller owns the whole
table; the toolbar stops scraping when it is given one. Same table for CSV and for the clipboard.

## IN scope

- `DataGridBaseProps.exportTable?: (orderedRowIds: GridRowId[]) => DataGridExportTable` and the same
  prop on `DataGridToolbarProps`. When present, CSV and clipboard both render it verbatim; when
  absent, today's scraping behaviour is unchanged for every other grid.
- The toolbar passes `apiRef.getSortedRowIds()`, so the reader's sort and any grid filter still decide
  row order. Column visibility and column order no longer affect the file — the column set is now
  canonical.
- `buildDeliveryExportTable(delivery, features, teams, terms)` — a pure module, no MUI.
- Delete `deliveryExportHeader.ts`, its test, `exportHeaderRows` and `DataGridExportHeaderRow`.
- Extract two pure helpers so the file and the screen cannot drift apart:
  - `featureLikelihoodLabel(featureLikelihood, hasRemainingWork): string` out of `FeatureLikelihoodChip`
  - `featureWarningSentences({isDoneWithRemainingWork, isUsingDefaultFeatureSize, dependencies}, terms): string[]` out of `WarningsIndicator`

## Table contract

Headers, in this order:

`Name`, `Team`, `Progress`, `Forecast 50%`, `Forecast 70%`, `Forecast 85%`, `Forecast 95%`,
`Likelihood`, `State`, `Dependencies`, `Warnings`

**Row 1 is the Delivery**, and it is export-only — nothing about the on-screen grid changes.

| Column | Delivery row | Feature row |
|---|---|---|
| Name | `<delivery.name> (<deliveryTerm>)` | `getWorkItemName(row.name, row.referenceId)` |
| Team | empty | names of teams with `getTotalWorkForTeam(id) > 0`, joined `"; "`; none → `Unassigned` |
| Progress | `<completed>/<total>` from `totalWork - remainingWork` | `<completed>/<total>` from `getTotalWorkForFeature()` / `getRemainingWorkForFeature()` |
| Forecast N% | `completionDates` entry with that probability, `formatLocalDate` (YYYY-MM-DD) | `row.forecasts` entry with that probability, same format |
| Likelihood | `Math.round(likelihoodPercentage)%` | `featureLikelihoodLabel(...)` for the matching `delivery.featureLikelihoods` entry |
| State | empty | `row.state` |
| Dependencies | empty | one entry per `dependsOn`, joined `"; "`; withheld → `withheldName(terms)`; named → `<referenceId>: <name>` |
| Warnings | empty | `Yes` when `featureWarningSentences(...)` is non-empty, `No` when it is empty |

Absent-value rule from slice 01 stands: a number nobody computed leaves as an empty cell, never
`null`, `undefined`, `NaN` or a fabricated `0`.

A Feature that cannot be forecast at all (`cannotBeForecast({teamsWithoutForecast})`) puts
`CANNOT_FORECAST_SHORT` in all four Forecast cells, mirroring what the screen says. A Delivery whose
`likelihoodPercentage` is null leaves Likelihood empty.

Warnings is a `Yes` or a `No` rather than the sentences behind it: the column exists to be filtered
on in a spreadsheet, and the reasons are on screen, in the tooltip. The Delivery row leaves it empty —
a Delivery has no warnings of its own, and an empty cell keeps it out of a Yes/No filter instead of
claiming it is clean. The verdict is read off `featureWarningSentences` being empty, never off a
second count of the same thing.

Terminology reaches the file through the Delivery-row suffix and through the dependency names, which
already read the tenant's words.

## OUT of scope

- Any other grid opting into `exportTable`.
- Any change to the premium gate, its tooltip or its disabled affordance.
- Any backend change. Frontend only.
- Rendering the Delivery as a row in the on-screen grid.

## Acceptance criteria replaced

AC-01.3, AC-01.5 and AC-01.8 in `../feature-delta.md` describe the deleted header block and the
visible-columns-only rule. They are rewritten against this contract.
