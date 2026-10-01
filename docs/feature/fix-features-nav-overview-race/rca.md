# RCA — Bug #6112: "verifypostgres is occasionally flaky and runs longer than verifysqlite"

Investigation date: 2026-10-01. Method: Toyota 5 Whys, multi-causal. Read-only; no code changed.

Legend: **[V]** verified against a primary source (run log, trace, report JSON, file:line, git) · **[I]** inferred, consistent with the evidence but not directly observed.

## 0. Problem statement and scope

Two separate questions are bundled in the bug title, and the evidence says they are unrelated:

| Symptom | Scope | Finding |
|---|---|---|
| S1: `verifypostgres` sometimes goes red, and a re-run turns it green | E2E job in `ci_verifypostgres.yml`, last ~300 `ci.yml` runs (2026-09-14 … 2026-10-01) | It is not Postgres-specific. One spec, `ManualSortingSwitch.spec.ts`, has a **test-only race in the Page Object**. It loses the race in **about half of all verify jobs on both databases**. A job goes red only when all three Playwright attempts lose it. |
| S2: `verifypostgres` takes 7–8 min against 5–6 min for `verifysqlite` | Same jobs | About 30 s is Postgres-only setup, which is expected. The other ~2–2.5 min is a **uniform ~1.75× slowdown of every test** against a Postgres backend. It is not a single avoidable hotspot. |

A correction to the bug text: the failure did not receive "6 items where 1 was expected". The `- Expected - 1 / + Received + 6` header counts diff lines. The spec actually compared an **empty list (or three non-Feature names)** with the 4 real Features. **[V]** run 36558360835 attempt 1, job 109374845781, log lines 1009–1023 / 1063–1075.

---

## 1. Evidence summary

### 1.1 The failing run (36558360835, attempt 1, job 109374845781) [V]

- `Running 57 tests using 1 worker` → `1 failed, 56 passed (7.9m)`. Only `ManualSortingSwitch.spec.ts:15` failed, and it failed on all 3 attempts. The `verifysqlite` job in the same run was green, but its own log shows `1 flaky` for the **same spec**: job 109374845897 lost on the first attempt and passed on retry.
- Assertion `ManualSortingSwitch.spec.ts:33`. In each attempt the *Received* side (the read **after** the switch) is correct: `AP-001, AP-004, AP-005, AP-006`. The *Expected* side (`beforeTheSwitch`, read **before** the switch) is wrong:
  - try 0 and retry 1: `[]`
  - retry 2: `["Project Apollo", "Team Zenith", "Demo Data CSV Connector"]`
- Those three names are not Features. They are the demo **Portfolio** (`DemoDataService.cs:16`, `DemoDataFactory.cs:187`), the demo **Team** (`DemoDataFactory.cs:168`) and the demo **Connection** (`DemoDataFactory.cs:67`). Those are exactly the `name` cells of the three DataGrids on the **Overview** page (`OverviewDashboard.tsx:254`, `DataOverviewTable.tsx:101`).

### 1.2 Playwright trace of retry #1 (`playwright-report-postgres` artifact 11028708083, `data/3d49102c….zip`) [V]

Times are ms from test start:

| t | Action | Page state in the DOM snapshot |
|---|---|---|
| 2566→2928 | fixture `overviewPage`: click "Overview" | `/` |
| 2947→3056 | `goToFeatures("Features")` click returns | URL becomes `/features` at 3038 |
| 3060→3237 | `expect(.MuiDataGrid-row.first()).toBeVisible()` **passes** | URL `/features`, but the DOM still holds `Project Apollo`, `Team Zenith`, `Demo Data CSV Connector` and has **no** "Lighthouse forecasts …" heading. These are the Overview grids. |
| ~3250 | `FeaturesView-*.js` lazy chunk is requested (network trace) | still Overview |
| 3342→3452 | `readColumn`: `[data-field="name"].first()` visible **passes** | Overview name cell |
| 3457→3700 | `allInnerTexts()` returns **`[]`** | Overview gone, Features grid still loading (`/api/latest/features` in flight ≈3500–3545) |
| 3721 | — | Features view rendered ("Lighthouse forecasts", AP-001…) |
| 4369 | `FeatureOrdering` switch `isChecked()` → **true**, so no click | switch was already ON |
| 4444→4708 | second read → the 4 real Features | `/features` |

The URL changes **before** the content does. The Overview stays painted while the lazily-loaded `FeaturesView` chunk loads. Every wait in the spec and the POM is satisfied by the **Overview's** rows.

### 1.3 Frequency across CI [V]

**Last 55 `ci.yml` pushes to `main`** (2026-09-22 … 2026-10-01, 108 verify jobs, every attempt; source: `/actions/runs/{id}/attempts/{n}/jobs` and the job logs):

| Job | Jobs | ManualSortingSwitch lost ≥1 attempt (flaky or failed) | Red jobs | Mean duration |
|---|---|---|---|---|
| verifysqlite | 54 | **37 (69 %)** | 0 | 349 s |
| verifypostgres | 54 | **19 (35 %)** | 1 (36558360835) | 496 s (52/54 > 7 min) |

Other flakes in the same window were rare and are unrelated: `ado.spec` (real ADO), `SleRiskColumnReachable`, `WorkItemAgePercentilesStatus`, `DeliverySourcePicker`.

**Every red verify job across the last 300 `ci.yml` runs (all branches)**, grouped:

| Failing test(s) | DB | Runs | Classification |
|---|---|---|---|
| `ManualSortingSwitch` (same `[]` / Overview-names signature in every log) | pg | 36558360835, 35195326278, 35059418578 (PR) | this race |
| `ManualSortingSwitch` | sqlite | 35185700564 (PR) | this race, **SQLite too** |
| `WorkItemAgePercentilesStatus:46` | both | 35237088051, 35237192335 | real regression on both DBs (not this bug) |
| `AgingPacePercentiles:16` | both | 35216342964 | both DBs |
| `DeliveryJointLikelihood:100` + `MultiTeamForecast:15` | both | 35430764225 | both DBs |
| `servicenow.spec:11` | both | 34852823039 (a1+a2), 34809… ×6 PRs | external ServiceNow, both DBs |
| `DeliverySourcePicker:56` | pg ×4 attempts, sqlite ×1 | 34935358494 (dependabot branch) | branch-specific (real Jira) |
| 52 failed, 5.5 h | both | 34932612733, 34933733401, 34934235046 | broken dependabot frontend bump, both DBs |
| setup: `docker run postgres:17.2-alpine` → Docker Hub auth error, exit 125 | **pg only** | 36135362619 | infra, Postgres-only |
| setup: corepack registry fetch | pg | 35564650697 | infra, not DB-related |

**Answer to Q3:** no Playwright spec fails only on Postgres. The one Postgres-only failure class is the Docker Hub image pull, which happened once. `ManualSortingSwitch` occurs on both DBs, and occurs **more often on SQLite**.

### 1.4 Step timings, subject run [V]

| Step | verifysqlite (a1) | verifypostgres (a1) | verifypostgres (a2) |
|---|---|---|---|
| Install latest PostgreSQL client (apt) | — | 17 s | 11 s |
| Run Postgres (`docker run` + `pg_isready`) | — | 8 s | 7 s |
| Wait for Lighthouse to start | 4 s | 7 s | 6 s |
| **Run Playwright tests** | **4 m 20 s** | **7 m 58 s** | **7 m 35 s** |
| Whole job | 4 m 52 s | 9 m 03 s | 8 m 30 s |

Sum of per-test durations from the HTML report JSON: sqlite 255 s, pg 451 s (passing attempt). The difference is spread over **every** spec, typically +3–7 s each and about 1.75×. No spec dominates (top delta: `TimeInStateAndStaleness` +13 s). By step category (pg vs sqlite): fixture `testData` 40.5 s vs 16.6 s, fixture `overviewPage` 37.5 s vs 22.3 s, teardown `DELETE` 11.0 s vs 3.0 s, UI `Click` 51 s vs 35 s.

---

## 2. Five Whys

### Branch A — ManualSortingSwitch compares a stale "before" reading (the flake)

```
WHY 1A  The "nothing moves" assertion fails: beforeTheSwitch is [] or [Project Apollo, Team Zenith,
        Demo Data CSV Connector], while the after-read lists the 4 real Features.
        [V] job 109374845781 log; same signature in all 9 logs that contain a ManualSortingSwitch failure,
            pg and sqlite.

 WHY 2A  The before-read ran while the Overview page was still on screen (or between the Overview
         unmounting and the Features grid getting its data), so it read the Overview's grids.
         [V] trace retry#1: toBeVisible passed at 3237 ms with the Overview DOM showing and URL /features;
             the FeaturesView chunk was only requested at ~3250 ms; allInnerTexts returned [] at 3700 ms.

  WHY 3A  Nothing in the navigation or read path proves that the Features view is the page being shown.
          - LighthousePage.goToFeatures clicks the nav link and returns at once
            (LighthousePage.ts:48-53). It does not wait for the URL, a heading or data.
          - FeaturesPage.featureRows is the page-wide `.MuiDataGrid-row` (FeaturesPage.ts:14-16), so
            it matches any grid on any page.
          - readColumn waits for `.MuiDataGrid-row [data-field="name"]` (FeaturesPage.ts:112-116). The
            Overview's portfolio, team and connection grids all have a `name` column, so the wait is
            satisfied by the wrong page.
          [V] file:line above. This also explains why ONLY this read flakes: the FeaturesView.spec reads
              of `position` / `startForecast` (FeaturesView.spec.ts:25,56) match no Overview cell, so
              their waits block until the real grid appears. ManualSortingMove reads `name` but arrives
              from Settings, which shows no grid rows. Neither appears in any flaky list.

   WHY 4A  The POM assumes that once the click returns, the next page is showing. That is not true for
           this app: the route component is lazy (App.tsx:53, `lazy(() => import("./pages/Features/FeaturesView"))`)
           and the previous page stays painted while the chunk loads. The URL changes first.
           [V] that the old UI stays painted after the URL changes (trace snapshots at 3038 and 3311).
           [I] the mechanism: React Router runs navigations inside React.startTransition, so React keeps
               the committed Overview tree instead of showing the Suspense fallback.

    WHY 5A  The earlier fix diagnosed the wrong race. ad640cc8e (2026-09-17, "wait for the column before
            reading it off the grid") assumed the rows were the Features rows and that only their cells
            were late. It added a first-cell wait, and that wait is satisfied by the Overview's `name`
            cells too. The retry-1 trace was not inspected, and the Expected values (Overview entity
            names) went unexamined. ManualSortingSwitch kept flaking in 4 later failing runs, and in 56
            of 108 green jobs since.
            [V] `git merge-base --is-ancestor ad640cc8e` is true for 35216342964, 35237192335,
                35430764225, 36558360835. Flaky counts in §1.3.

    → ROOT CAUSE A (test-only): the Features POM decides "the Features grid is ready" with page-wide
      selectors that another page satisfies, right after a navigation whose target is lazily loaded.
      No product defect: the ordering endpoint, the seed and the view return the right order whenever
      the read is taken from the right page (the after-read was correct in every failing attempt).
```

### Branch B — why it looks like a Postgres problem (perception)

```
WHY 1B  Red verify jobs cluster on verifypostgres (3 pg vs 1 sqlite for ManualSortingSwitch).
        [V] §1.3.
 WHY 2B  A job is red only if all 3 attempts lose (playwright.config.ts: retries = CI ? 2 : 0). Single
         losses are reported as "flaky" inside a green job and nobody reads them.
         [V] config; 56 flaky-but-green jobs out of 108.
  WHY 3B  The per-attempt loss rate is actually HIGHER on SQLite (69 % vs 35 % of jobs). The race needs
          the Overview grids to be populated at the moment Features is clicked. A faster backend
          (SQLite, in-process) fills them sooner, so the stale rows are there to be read.
          [V] the rates. [I] the mechanism (fits the observed inverse correlation; not instrumented).
   WHY 4B  The bug was filed from red/green job status and the "pg-only" re-run pattern, not from per-test
           flaky data. That data exists only inside each job log and HTML report. Nothing aggregates it.
           [V] no CI step summarises flaky tests (ci_verify*.yml).
    WHY 5B  There is no flaky-test signal in CI. retries=2 turns an intermittent test defect into a
            probabilistic job failure that looks environmental.
    → ROOT CAUSE B (detection): retries hide the flake rate, so a 50 %-per-job test defect surfaces only
      as rare, apparently DB-specific red builds.
```

### Branch C — test isolation: the switch is already on (contributing, not causal)

```
WHY 1C  In the traced attempt the spec never actually "hands the order over": the switch reads checked
        and enableFeature returns early (SystemConfigurationPage.ts:35-37).
        [V] trace retry#1, Frame.isChecked → true at 4357 ms, no click.
 WHY 2C  The FeatureOrdering optional feature is instance-wide state. ManualSortingMove (which runs
         immediately before, alphabetically, 1 worker) turns it on, and nothing turns it off.
         [V] report order 11:01:25 Move → 11:01:30 Switch; clearConfiguration deletes only portfolios,
             teams and connections (LighthouseFixture.ts:42-64). The retry ran after a fresh demo load
             and teardown and still saw ON.
  WHY 3C  Optional-feature toggles are outside the fixture teardown contract. switchHistoryFill is the
          only helper that resets one, and it is per-spec (helpers/api/optionalFeatures.ts).
   WHY 4C  Each walking skeleton was written in isolation, assuming a shipped-default instance.
    WHY 5C  The fixtures do not reset instance-level settings between specs.
    → ROOT CAUSE C: leaked instance state. In CI, the Switch walking skeleton does not exercise the
      toggle path it is named for. This does not cause the failure (the bad read happens before
      Settings is opened), but it does mean a green Switch run proves less than its name claims.
```

### Branch D — why verifypostgres takes ~2.5 min longer (duration)

```
WHY 1D  verifypostgres takes 496 s on average vs 349 s (+147 s, 54 jobs each).                       [V]
 WHY 2D  ~30 s is Postgres-only setup: apt PostgreSQL client 11–17 s, docker run + pg_isready 7–8 s,
         a slower backend start +3 s. The rest (~2–3 min) is inside "Run Playwright tests".           [V] §1.4
  WHY 3D  Every test is about 1.75× slower. No single spec or step dominates. The largest absolute
          deltas are in fixtures: demo load + waitForBackgroundUpdates (whose drain loop polls in 1 s
          steps, demo.ts:3,85-90, so a slower background recompute costs a whole extra second), and
          teardown DELETEs at 3.7×.                                                                    [V]
   WHY 4D  Each API call on the Postgres provider goes over TCP to a Docker container through a published
           port, and EF/Npgsql round-trips cost more than in-process SQLite. Write-heavy paths
           (demo load, deletes) are slowest, which fits per-commit durability cost (fsync /
           synchronous_commit) on the runner's disk.
           [I] not measured. The 3.7× on DELETE against 1.6× on reads points to write latency.
    WHY 5D  The CI Postgres runs with production durability defaults (`docker run … postgres:17.2-alpine`,
            no `-c` flags, ci_verifypostgres.yml "Run Postgres"), although the database is thrown away
            after the job.                                                                            [V] config
    → ROOT CAUSE D: mostly expected cost of a real, out-of-process database. A part is plausibly
      avoidable (durability settings), but that is unproven. The PostgreSQL client install is NOT
      avoidable: the DatabaseManagement spec needs pg_dump / pg_restore / psql
      (PostgresDatabaseManagementProvider.cs:12-14).
```

---

## 3. Validation

**Backwards chains**
- A: page-wide selector + lazy target that keeps the old page → the first-row and first-name-cell waits are met by the Overview → the read returns Overview names, or `[]` if it lands between unmount and data. Both observed values are produced, and only by this mechanism. ✔
- B: per-attempt loss p, 3 attempts → the job is red with probability ≈ p³. SQLite's higher p and pg's higher red count are not in contradiction: the red counts (3 vs 1) are too small to separate, and both come from the same mechanism. ✔ (statistical, [I])
- C: does not produce the symptom. It is listed as contributing because it weakens what the spec proves. ✔
- D: independent of A–C. The flake is not caused by Postgres slowness. If anything, slowness makes losing the race *less* likely. ✔

**Alternatives considered and rejected**
- *Postgres row order without ORDER BY*: rejected. The after-read was correctly ordered in every failing attempt, and the before-read did not contain Features at all. [V]
- *Background refresh/update queue mutating Features between reads*: rejected. The diff is not a reorder or a count change among Features; it is the wrong page. `waitForBackgroundUpdates` had drained (status polls at 74–2093 ms) before the page opened. [V]
- *Debounced-autosave navigation race* (the known pattern): does not apply. The switch is a single POST, and `enableFeature` awaits its response (SystemConfigurationPage.ts:39-48). In the traced attempt no save happened at all (already ON). [V]

---

## 4. Proposed fix (test-only, minimal)

### Fix A (primary, root cause A) — goToFeatures returns only once the Features view is the one shown

`Lighthouse.EndToEndTests/tests/models/app/LighthousePage.ts`

```ts
import { expect, type Locator, type Page } from "@playwright/test";
...
	async goToFeatures(navigationLabel: string): Promise<FeaturesPage> {
		await this.mainNavigation
			.getByRole("link", { name: navigationLabel })
			.click();
		const featuresPage = new FeaturesPage(this.page);
		// The page is loaded on demand and the previous one stays on screen while it loads, so the
		// address bar changes before the content does; only the view's own heading says it has arrived.
		await expect(featuresPage.helpText).toBeVisible();
		return featuresPage;
	}
```

`helpText` (`/Lighthouse forecasts .* in this order/`, FeaturesPage.ts:18-20) is rendered only by `FeaturesView.tsx`. Once it is visible, the Overview or Settings tree has been replaced. From that point `featureRows.first()` can only match Features rows. Those rows appear in one render (`setFeatures` + `setIsLoading(false)` are batched, FeaturesView.tsx:29-33), and `readColumn`'s existing first-cell wait still covers the rows-before-cells case from ad640cc8e.

Why not wait on the `/api/latest/features` response: ManualSortingMove calls `goToFeatures` while **already on** `/features` (ManualSortingMove.spec.ts:35). The same-route click does not remount the view or refetch, so a response wait would hang. The heading wait is already met in that case, so behaviour there is unchanged.

### Fix A' (defence in depth; ship together with A in one commit) — the grid can only be the Features grid

Fix A alone protects only callers that go through `goToFeatures`. A' makes the locator itself unable to match a foreign grid, so the two together close the race for any future caller.

`Lighthouse.EndToEndTests/tests/models/features/FeaturesPage.ts`. Scope `featureRows` to the grid that carries the position column (`showPosition` is passed only by the Features view, FeaturesView.tsx:73, and the Portfolio detail Feature list, PortfolioFeatureList.tsx:143. No Overview or Settings grid has it):

```ts
	private get grid(): Locator {
		return this.page.locator(".MuiDataGrid-root").filter({
			has: this.page.locator('.MuiDataGrid-columnHeader[data-field="position"]'),
		});
	}

	get featureRows(): Locator {
		return this.grid.locator(".MuiDataGrid-row");
	}
```

With this, no other page's grid can satisfy any Features wait, even if a future spec skips `goToFeatures`. (A `data-testid` on `FeatureListDataGrid` would be cleaner, but it is a production edit. Prefer it only if the team wants one.)

### Fix C (isolation, optional, same PR or follow-up)

Make both ManualSorting specs start from, and leave, the shipped default. Export the existing private `switchOptionalFeature` (`helpers/api/optionalFeatures.ts:11-33`, today used only by `switchHistoryFill`) or wrap it as an exported `switchFeatureOrdering(request, enabled)`. Call it with `false` at the start of `ManualSortingSwitch` (so the toggle click is really exercised) and in teardown for both specs. Check the shipped default first (`OptionalFeature` seeding; key `FeatureOrdering`).

### Fix B (detection, prevention)

Add a step after "Run Playwright tests" in `ci_verifysqlite.yml` / `ci_verifypostgres.yml` (and `ci_e2e.yml`) that greps the list-reporter output for `N flaky` and the following test names, and writes them to `$GITHUB_STEP_SUMMARY`. That makes the flake rate visible without failing the build. Keep `retries: 2` as it is. Raising it would only lower the red-job probability (≈p³ → p⁴) while hiding the defect further, and it exists for the real-IO connector specs. A blanket `failOnFlakyTests` would red-flag the real-IO connector specs (ADO/Jira/ServiceNow), which are legitimately retry-dependent, so it is not recommended.

### Fix D (duration, optional experiment, not a bug fix)

In `ci_verifypostgres.yml` "Run Postgres", append `-c fsync=off -c synchronous_commit=off -c full_page_writes=off` to the `docker run` (safe for a throw-away CI DB). Compare the "Run Playwright tests" step across ~5 runs. Expected gain is unproven ([I]). Trade-off: with durability off, this job can no longer show a write-latency regression on Postgres. It never measured that deliberately, so nothing is lost that was being relied on, but the choice should be made knowingly. If it does not move the number, accept the ~1.75× as the cost of a real out-of-process database. Separately, an `actions/cache` or mirror for `postgres:17.2-alpine` would remove the one Postgres-only setup failure (Docker Hub pull, run 36135362619). Low priority.

## 5. Files affected

| File | Change | Fix |
|---|---|---|
| `Lighthouse.EndToEndTests/tests/models/app/LighthousePage.ts` | `goToFeatures` waits for `helpText`; import `expect` | A |
| `Lighthouse.EndToEndTests/tests/models/features/FeaturesPage.ts` | scope `featureRows` to the position-column grid | A' |
| `Lighthouse.EndToEndTests/tests/helpers/api/optionalFeatures.ts` | export a FeatureOrdering switch | C |
| `Lighthouse.EndToEndTests/tests/specs/features/ManualSortingSwitch.spec.ts`, `ManualSortingMove.spec.ts` | reset FeatureOrdering before/after | C |
| `.github/workflows/ci_verifysqlite.yml`, `ci_verifypostgres.yml` (`ci_e2e.yml`) | flaky summary step; optional pg `-c` flags | B, D |

No production code changes. No backend changes.

## 6. Risk assessment

| Fix | Risk | Notes |
|---|---|---|
| A | Low | `goToFeatures` callers: ManualSortingSwitch, ManualSortingMove, FeaturesView.spec (×2), FeatureDependencies.spec, Screenshots.spec:316. All expect to land on the Features view, and FeaturesView.spec already asserts `helpText`. With a renamed Features term, the regex `.*` still matches. With an empty instance the heading still renders. |
| A' | Low–medium | Couples the POM to the position column. If that column is ever made optional on the Features view, every Features read breaks loudly (it times out rather than reading wrong data). |
| C | Low | Changes the Switch spec from "no-op when already on" to a real toggle, which may surface a real defect in the toggle path. That is the point. Confirm the shipped default before resetting to `false`. |
| B | None | Summary only. |
| D | Low | CI-only DB, so durability flags cannot lose real data. Could mask a real write-latency regression; acceptable for a functional E2E job. |

## 7. Deterministic reproduction

Locally (`LIGHTHOUSEURL` pointing at a packaged build with demo scenario 0, SQLite is fine because the race is DB-agnostic), hold the lazy chunk so that the Overview stays painted after the click:

```ts
await page.route("**/assets/FeaturesView-*.js", async (route) => {
	await new Promise((r) => setTimeout(r, 2000));
	await route.continue();
});
```

Put this before `goToFeatures` in ManualSortingSwitch, after confirming the Overview grids show rows. With the current POM, `getListedFeatureNames()` returns `["Project Apollo", "Team Zenith", "Demo Data CSV Connector"]` every time. To get the `[]` variant, delay `**/api/latest/features` by ~2 s instead of the chunk. With Fix A the spec passes under both delays. [I] predicted, not executed (read-only investigation). Per the E2E-minimalism guideline this is a **one-off verification**, not a new permanent spec. Optionally, run `--repeat-each=30` on ManualSortingSwitch before and after the fix as a statistical check (current loss rate ≈ 35–69 % per job).

## 8. Verified vs inferred — summary

- **Verified:**
  - The failure signature in all 9 logs.
  - The trace timeline: rows matched on Overview while the URL was `/features`; chunk requested after the waits passed; `[]` read.
  - The non-Feature names are the Overview grid names.
  - POM / spec / fixture code at the cited lines.
  - Lazy route.
  - Flake rates (37/54 sqlite, 19/54 pg).
  - Full failure classification over 300 runs; no pg-only spec.
  - The prior fix ad640cc8e predates 4 failures.
  - The switch was already ON (state leak).
  - Step timings and per-test deltas.
  - pg client needed for pg_dump.
- **Inferred:**
  - React Router's startTransition as the reason the old page stays painted.
  - That SQLite's higher loss rate comes from the Overview data arriving sooner.
  - fsync/synchronous_commit and the docker port path as the cause of the 1.75× slowdown, and the size of Fix D's gain.
  - The reproduction recipe's exact outcome.
