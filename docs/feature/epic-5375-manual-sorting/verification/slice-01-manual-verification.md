# Slice 01 — manual verification log

**Feature**: epic-5375-manual-sorting · **ADO**: Story #5688
**Sessions**: 2026-08-06, 2026-08-07 · **Status**: verified, with two findings fixed

Verified against a dogfood dev instance on `http://localhost:5169` (`--launch-profile http`), frontend
built into `wwwroot`. The 2026-08-07 instance is **multi-connector** — 97 Features across three
Portfolios (Lighthouse/ADO 81, Jira 4, Linear 7) plus 4 orphans — which is a stronger test of the
position column than the single-Portfolio instance the 2026-08-06 pass used.

## Results

| AC | Result | Evidence |
|----|--------|----------|
| AC-1.1 | **Pass** (was F1) | Nav entry routes to `/features`, label follows Settings → Terminology. Placement fixed 2026-08-07 — see F1 |
| AC-1.2 | **Not checked on an instance** | `Authentication:Enabled` and `Authorization:Enabled` are both `false` in `appsettings.Development.json` — no second user to restrict. Covered by `Slice01FeaturesViewScenarios.cs` |
| AC-1.3 | **Pass (inspection)** | `FeaturesController.cs:16` carries `[Authorize]` only — no `LicenseGuard`; the `Header.tsx` nav array is unconditional; `App.tsx:225` route has no license wrapper. Instance is licensed, so the non-premium path was not clicked through |
| AC-1.4 | **Pass** | A Feature placed in a second Portfolio renders once, listing `Lighthouse, Jira Portfolio`, holding a single position (13) |
| AC-1.5 | **Pass** | Positions are instance-wide: the visible list opens at `1` and runs `…7 → 13`, with a `26 → 77` gap between adjacent visible rows. `#` column present on the Portfolio Features tab with the same values (D10/S16) |
| AC-1.6 | **Pass** | Sorting by Name and by State leaves every position unchanged |
| AC-1.7 | **Pass** | Toggle off reveals 97 rows numbered exactly `1..97`, and **not one** previously-visible row changed its number |
| AC-1.8 | **Pass** | A Feature with an empty source `Order` renders position `89` — a number, no blank cell, no `NaN`. Empty sorts after every integer and ahead of the non-integer ranks, which is what `FeatureComparer`'s ladder specifies |
| AC-1.9 | **Not measured** | 500 Features not reachable on a real instance. Backend half pinned by a scenario test; the UI half remains an open measurement |

AC-1.4 and AC-1.8 were checked by seeding the two conditions directly in the dev database (a second
Portfolio membership, and one blanked `Order`) and reverting both afterwards. The instance is back to
its pre-check state.

## Premise check (slice brief, "Verify the premise first")

Run against the same instance. **Hypothesis 1 does not fire here**:

- **0 of 97** Features have a null or empty `Order`. Blanks do not dominate.
- 3 duplicate `Order` pairs out of 97. Ties are marginal.
- Shape mix: 81 ADO StackRanks (`1999xxxxxx`), 7 Linear negative integers (`-45661`), 4 Linear doubles
  (`33.74`, `-952.83`), 5 Jira LexoRanks (`0|i0007c:`). The live bucket layout is integers at 1–88,
  doubles at 89–92, LexoRank strings at 93–96.

That mix is also the case that makes `FeatureComparer`'s intransitivity reachable rather than
theoretical — see the epic's open decisions.

Hypothesis 2 (**is a flat ranked list usable at real size?**) stays **inconclusive**: 97 Features on a
dev instance is not a customer's backlog, and the D7 clock — do newly-arrived Features get found and
placed, or pile up unsorted at the tail? — starts from this dogfood, not from slice 01's ship date.

## Findings

### F1 — the nav entry was in the wrong place · **fixed 2026-08-07**

It sat third, after System Settings; it belongs directly after Overview.

The blocker was never the ordering: `LighthousePage.goToOverview` matched the concatenated nav text
`"LighthouseOverviewSystem"`, so inserting an entry between the two broke every spec that navigates
home. `OverviewPage.toolbar` had the same shape of problem.

Fixed by making the locators robust first — the desktop nav carries a `main-navigation` testid that
both navigations scope to, and `toolbar` resolves the app bar by its banner role. The banner box was
measured against what the old text locator matched (`0,0,1600,64` in both cases), so the licensing
screenshot clips the same region it always did.

### F2 — the help text mentioned teams as a literal · **fixed 2026-08-07**

`featuresTerm` resolved through `getTerm`, but *teams* was hard-coded, so an instance that renames Team
read its own word everywhere except that sentence. Rather than routing a second term, the sentence was
shortened to `Lighthouse forecasts {Features} in this order.`

## Still open after this pass

- **AC-1.2** needs auth + authz enabled and a second user holding `PortfolioRead` on a subset.
- **AC-1.9**'s UI half needs an instance with 500 Features.
- **Screenshots** are not regenerated. The nav reorder changes the app bar in all 13 full-page docs
  screenshots, so they are stale as of 2026-08-07 and owe a regeneration pass — remember to delete the
  PNGs first, or a sub-0.5% diff silently keeps the old image.
