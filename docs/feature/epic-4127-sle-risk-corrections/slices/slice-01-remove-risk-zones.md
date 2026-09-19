# Slice 01 — Remove the SLE Risk background zones

**ADO**: User Story #6034 · **Story**: US-R2-01 · **Job**: `job-flow-coach-act-before-sle-breach`
**Estimate**: ≤1 day · **Reference class**: round 1's slice 03 built this in one day; a deletion of the same surface is smaller.

## Goal

The Work Item Aging chart's background control offers two honest modes again — Off and Pace percentiles — and every symbol behind the third one is gone.

## Learning hypothesis

**Disproved if** the deletion changes how a risk is coloured in the work item dialog or the In Progress widget. That would mean the defect was in the palette mapping rather than in the band ladder, and the ladder would deserve a repair instead of a removal (D19/D21).

**Confirmed if** the column and the widget render identically before and after, and the chart's only remaining background vocabulary is the pace one — where an unpainted region has exactly one meaning.

## IN scope

**Backend** — `SleRiskCalculator`: `Zones`, `WithUpperEdges`, `UpperEdgeOf`, `ZoneLevels`, `CertainRisk`, and the `SleRiskZone` record. `SleRiskZoneDto` (`SleRiskDto.cs:27`). `ITeamMetricsService.GetSleRiskZonesForTeam` (`:71`) + its `TeamMetricsService` implementation and cache entry (`:393-406`). The `TeamMetricsController` action (`:232-243`, route `[HttpGet("sleRisk/zones")]`). `Slice03SleRiskZonesScenarios.cs` in full, and the zone half of `SleRiskAcceptanceTest.cs`.

**Backend, found during DESIGN and missed by DISCUSS** — `SleRiskCalculatorTest.cs:183-333`, twelve `Zones_*` unit tests. This is a 21st file. DISCUSS's inventory grep keyed on zone *symbol names*, and not one of these test names contains one — they read `Zones_TheCertainBand_NeverStops` and so on. A symbol grep cannot see a file whose only tie to the deleted code is what it calls.

**Two corrections to that range, found during DISTILL:**

- **The range starts at 182, not 184.** Line 182 is the section comment `// --- Where the odds turn, for the chart's background zones ---`; 183 is blank and 184 is the first `[Test]`. Deleting from 184 leaves a heading over nothing. It compiles, and the AC-01.9 grep cannot see it either. The frontend block is **1705**-1990 in `WorkItemAgingChart.test.tsx` (1705 opens its comment, 1711 the `describe`). *(Corrected twice. DISCUSS said 184; the first correction said 183; the comment is at 182. An off-by-one fixed by eye acquires a new off-by-one — the fix is to open the file at the boundary and read it, which is what settled it.)*
- **`SleRiskCalculatorTest.cs:23` — `private static readonly int[] RiskLevels = [25, 50, 75, 100]`** — goes with them. Its only two readers are at `:193` and `:236`, both inside the deleted block. Leaving it is an **S1144 unused-private-field failure** on the mandatory analyzer sweep, which `TreatWarningsAsErrors` turns into a build error. This is the third instance of the exact rule class DESIGN pre-applied twice: a deletion's characteristic Sonar failure is the member whose last caller just left. `SixtyFinishedItems` in the same file stays — the surviving `For_*` tests still read it.

**Frontend** — the zone overlay and `computeSleRiskZoneRects` inside `WorkItemAgingChart.tsx`; the `SleRiskZone` model in `models/Metrics/SleRisk.ts`; the `useMetricsData` fetch; the `MetricsService` and `TeamMetricsService` calls; `categoryMetadata.ts`; `MockApiServiceProvider.ts`. `useAgingBackground.ts` drops `"risk"` from `AgingBackground` and from `storedBackground`, and its doc comments stop describing a third mode.

**E2E** — `WorkItemAgingChart` POM: `showSleRisk`, `countSleRiskZones`. `Screenshots.spec.ts` L987-1010 (the whole `testWithDemo` block).

**Docs** — `docs/metrics/flow-metrics.md`: delete the `## SLE Risk Zones on the Aging Chart` section (L120-135) and correct any surviving sentence describing three background choices. Delete `docs/assets/features/metrics/aging_sle_risk.png`. Add a dated Status note to ADR-192 correcting its one mention of chart background zones as a planned surface.

## OUT of scope

- **`sleRiskColorFor` and everything else in `utils/charts/sleRisk.ts`** — the column and the widget still use it (D21).
- **The SLE reference line on the chart** — untouched; with the zones gone it is once again the only deadline the chart asserts (D13, now moot).
- **`docs/settings/worktrackingsystems.md`** — verified to contain no zone content; its one SLE-risk caveat describes the minimum-sample guard, which is slice 02's (D25).
- **ADR-192's *Architectural Enforcement* table** — the `Beyond history is null, never 0, 100` row is reversed by slice 02, not here (D24).
- **The round-1 archive** at `docs/evolution/2026-09-17-epic-4127-sle-risk.md` (D28).
- **The storage key's name and its legacy `"true"` translation** (D23).
- **The Epic's Release Notes copy** — rewritten once, at slice 04 (D29).

## Acceptance criteria

See `feature-delta.md` → *User stories* → US-R2-01, AC-01.1 through AC-01.9. In short: two options on the control; a stored `"risk"` resolves to Off and a stored `"true"` still resolves to Pace; the zones route 404s; the column and widget colours are unchanged; the docs section and the asset are gone; ADR-192 carries a Status note; the symbol grep comes back empty.

## Dependencies

**None upstream. One downstream, and it is the reason this slice is first.**

`Zones()` is built on `For()` returning null — `SleRiskCalculator.cs:101-119` breaks the age walk the moment it does. Slice 02 deletes the minimum-sample guard and makes past-the-target return `100`, which makes null unreachable and silently rewrites every band's geometry. Running slice 02 first would force a rewrite of code this slice deletes.

`CertainRisk = 100` lives in the zones half of the file and goes with them. Slice 02 reintroduces the concept where `For` can reach it — that is slice 02's work, not a gap left by this one.

## Pre-slice SPIKE

**Not needed.** The two questions that could have blocked this were answered during DISCUSS:

- **Is the asset live on the marketing site?** No — `aging_sle_risk.png` appears nowhere in `/storage/repos/website` under a full-repo grep. The site's only `features/metrics/` hot-links are `metricsoverview.png` and `portfoliometricsoverview.png`, both plain page screenshots at the default background mode (D27).
- **Does any client wrap the zones route?** No — zero hits for `sleRisk` / `sle-risk` / `sle_risk` across `/storage/repos/lighthouse-clients`.

## Pre-push checklist

*Added at the final review gate. Both items are documentation-only guards for risks nothing in CI can catch.*

**Commit order is a human discipline here, and CI cannot see it.** A push fires one CI run at the tip, so an out-of-order push ships a bundle calling a deleted route to `main` with a green build and no signal. Before pushing:

```bash
git log origin/main..HEAD --reverse --format="%h %s"   # oldest FIRST: E2E, frontend, backend, docs
```

Use `--reverse`. A plain `git log` prints newest first, and reading that listing as chronological inverts the whole order — an adversarial reviewer did exactly that here and filed the correct order as a blocker.

Push all four as one push, or in ascending order. Never land the backend deletion before the frontend one.

**The dogfood browsers need one sentence.** Anyone holding the SLE Risk background mode sees the aging chart's background as **Off** after this deploys, silently. That is AC-01.2 behaving exactly as specified, and it is precisely the shape of thing reported as a regression by someone who did not read the slice. Put this in the dogfood note:

> The aging chart's SLE Risk background mode has been retired. A browser that had it selected now shows the background as Off. Nothing is lost and no action is needed — the per-item risk column and the In Progress count are unchanged.

## Watch-outs

- **`@screenshot` runs need a premium licence and the fixture is gitignored.** Deleting the screenshot test means one fewer shot, not a re-run — but if a re-run happens for another reason, `rm` the PNG first: a regeneration keeps the old file when the pixel diff is under 0.5%.
- **The `data-testid` the POM drives.** Round 1 burned a CI cycle on a deleted test id that a Playwright POM still referenced. Grep `Lighthouse.EndToEndTests/` for every test id removed here before pushing — the ledger rule exists because of this exact feature.
- **`docs/ci-learnings.md`** — run its machine-readable greps over every file touched, before the first push.
- **Mutation testing** covers the surviving `sleRisk.ts` and the chart, not the removed code. A deletion cannot raise a kill rate; the run is there to prove the survivors are still covered.
