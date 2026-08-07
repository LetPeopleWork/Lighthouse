# Slice 01 — manual verification log

**Feature**: epic-5375-manual-sorting · **ADO**: Story #5688 (Resolved, board column *Verification*)
**Session**: 2026-08-07 · **Status**: incomplete — AC-1.7 in progress, three ACs unchecked

Verified against the dogfood dev instance, not demo data: Postgres `localhost:1886`, 82 Features in one
Portfolio, ranks mirroring the real ADO backlog. Frontend built into `wwwroot`, backend on
`http://localhost:5169` (`--launch-profile http`).

## Results

| AC | Result | Evidence |
|----|--------|----------|
| AC-1.1 | **Findings F1, F2** | Nav entry present, routes to `/features`, label follows Settings → Terminology |
| AC-1.2 | **Not checked** | `Authentication:Enabled` and `Authorization:Enabled` are both `false` in `appsettings.Development.json` — no user to restrict. Covered by `Slice01FeaturesViewScenarios.cs` |
| AC-1.3 | **Pass (inspection)** | `FeaturesController.cs:16` carries `[Authorize]` only — no `LicenseGuard`; `Header.tsx:61-66` nav array is unconditional; `App.tsx:225` route has no license wrapper. Instance is licensed, so the non-premium path was not clicked through |
| AC-1.4 | **Not checked** | One Portfolio, no shared Feature. Needs demo scenario 15 "Shared Features", and `DemoDataService.LoadScenarios` calls `ClearExistingData()` first — loading it destroys the dogfood DB |
| AC-1.5 | **Pass** | List opens at `6`, not `1` — positions 1–5 are hidden Done Features still holding their place (DDD-5). Gaps between adjacent visible rows: `20 → 71` (Multiple Datasources? → Manual Signal Tracking) and `79 → 81`. `#` column present on the Portfolio Features tab with the same values (D10/S16) |
| AC-1.6 | **Pass** | Sorting by Name and by State leaves every position unchanged |
| AC-1.7 | **In progress** | Default-hidden half proven by AC-1.5's opening at `6`. The toggle-off half — 82 rows from `1`, previously visible rows keeping their numbers — was not completed |
| AC-1.8 | **Not checked** | Needs a Feature whose source `Order` is blank. The DTO does not expose `Order`, so the target has to come from the DB |
| AC-1.9 | **Not checked** | 500 Features not reachable on this instance. Backend half pinned by a scenario test |

## Findings

### F1 — the nav entry is in the wrong place

It sits third, **after** System Settings; it belongs directly after Overview.

Not a one-line reorder of `Header.tsx:61-66`. `LighthousePage.goToOverview` matches the concatenated
nav text `"LighthouseOverviewSystem"`, so inserting between the two breaks every existing E2E spec.
The locator is the brittle thing — tighten it first, then move the entry.

### F2 — the help text needs rewording

`FeaturesView.tsx:62`:

```
Lighthouse forecasts ${featuresTerm} in this order — the top of the list gets your teams' throughput first.
```

Final copy is still open. Separately, and regardless of the wording chosen: `featuresTerm` resolves
through `getTerm`, but **teams is a literal**. `TERMINOLOGY_KEYS.TEAMS` exists
(`TerminologyKeys.ts:22`), so an instance that renames Team reads its own word everywhere except this
sentence. If the final wording keeps a reference to teams, route it through `getTerm`.

## Resuming on another machine

```bash
cd Lighthouse.Frontend && pnpm build          # builds into ../Lighthouse.Backend/.../wwwroot
dotnet run --project Lighthouse.Backend/Lighthouse.Backend --launch-profile http
```

Then http://localhost:5169. Positions shift as the instance syncs — take the numbers from
`GET /api/latest/features` at the moment of checking rather than from this document.

Next open item is **AC-1.7**: flip *hide completed* off, confirm 82 rows from `1` and that rows
already visible keep their numbers.

For the three unchecked ACs, each needs an instance this one cannot be:

- **AC-1.2** — auth + authz on, plus a second user holding `PortfolioRead` on a subset.
- **AC-1.4** — demo scenario 15, on a throwaway DB (it wipes what is there).
- **AC-1.8** — a Feature with a blank source `Order`; check the DB for one before hunting in the UI.

## Not done: the dogfood moment

The slice brief's premise check (`slices/slice-01-features-view-and-position-column.md`) was not run —
neither the `Order` blank/tie query nor the two dogfood questions ("is anything important sitting below
something unimportant?", and whether the worst 85% date is explained by its position). The learning
hypothesis is therefore still open, as is the D7 clock the brief says to start here.
