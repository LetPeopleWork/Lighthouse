# Slice 01: Pages keep answering while Lighthouse forecasts (US-01)

**Goal:** while a forecast round runs over a 920-Feature Portfolio's 8 Teams, Settings opens in under 1 s, a large
Features list opens as fast as when nothing runs, and the log carries no heartbeat warning. The precursor Task 01a
first records the baseline every slice of this Epic is judged against.

**Job:** `job-operator-keep-lighthouse-usable-while-it-forecasts` (Task 01a: `infrastructure-only`, D17).
**Moves:** **K3** (exit criterion, D19); K1 and K2 during a round.

## Part A — Task 01a, precursor (`@infrastructure`, lands first, not released alone)

IN:
- **Step 01a-1, first commit (DELIVER)**: bring `Lighthouse.EndToEndTests/perf/` into the E2E `tsconfig.json`
  `include` (with `playwright.perf.config.ts`) and the Biome path in `prebuild`, add `/perf/out/` to
  `Lighthouse.EndToEndTests/.gitignore` (the one place the harness writes), and add the `perf` script; `pnpm build` in
  `Lighthouse.EndToEndTests/` clean. Nothing of it runs in CI.
- A dev-only generator for the scaled set (D29): 100 / 400 / 920 Features per Portfolio, 8 Teams, ~3,000 Features in
  the instance, child Work Items, dependency links, forecasts run. Written through the CSV connector path the demo
  data uses. Not reachable in a release build.
- The harness: the DISCOVER Measurement plan's readings 1–7 (requests per open, server time, payload, rows read,
  client split, user-visible marks, the round's gather / simulation / save split with thread-pool and GC counters,
  heartbeat warnings), plus Q_r, S_q and the falsifier. Median of 5 after one warm-up; SQLite and PostgreSQL; idle and
  during a computed and a skipped round; `main` and the v26.10.19.10 tag.
- Calibration by ratio: a computed round lasts minutes and is several times the idle list time, else fix the set.
- The baseline table and the D27 re-score (G2, Dissent A/B, order of 02, 03, 05) written into `feature-delta.md`.
- **Hold point (maintainer decision 16, 2026-10-24)**: if the readings classify the stalls as GC pauses or CPU-quota
  throttling (DESIGN DDD-3), DELIVER stops after Part A and brings the readings to the maintainer before Part B starts.

OUT: shipping the generator, a demo scenario, CI budgets (maintainer).

## Part B — the user-visible change

IN:
- The forecast round (gather, simulate, save) runs off the request thread pool, with bounded parallelism; where and
  how is DESIGN's (DV-10).
- Epic 6311's half-the-cores bound, Stop and Cancel all keep working.
- The closed size bucket on `PortfolioTabOpened` / `TeamTabOpened` (D24; bands `UpTo20`, `From21To50`,
  `From51To100`, `From101To399`, `From400`, `Unknown` per maintainer decision 2), listed in
  `docs/settings/usagedata.md`.

OUT: async conversion of request paths (Dissent B); any change to what a round computes (C12); any screen change.

## Learning hypothesis

- **Disproves** "the round's own threads starve the requests" if, after Part B, during-a-round readings stay above
  1.2 × idle or heartbeat warnings remain in 5 + 5 rounds. Then read S_q: above 0.3 → Dissent B; otherwise GC or the
  database.
- **Confirms** it if K3 holds on the scaled set and the reporter's next log shows no warning during rounds.
- Part A **disproves** the list claim if the idle 920-Feature list loads in under ~10 s (falsifier; D18 moves 04).

## Acceptance criteria

US-01 AC-1.0 to AC-1.7 in `feature-delta.md`. In short: Settings < 1 s during a round; list ≤ 1.2 × idle during a
round; 0 heartbeat warnings across 5 computed + 5 skipped rounds; Cancel all unchanged; outputs and fingerprint
identical on both providers and the dev backup; size bucket emitted.

## Production data (D25)

Dev DB backup: forecasts identical before and after. The reporter's first log after the release: heartbeat warnings
during rounds counted and recorded (not a gate).

## Looks and sketch

- Loading / failure look: **N/A, because** no screen's fetch or response changes.
- UI sketch at DISTILL: **N/A, because** nothing a user sees changes.

## Dependencies

None before it. Premium licence fixture in the worktree (P2); a PostgreSQL instance (P5); the dev backup (P4).

## Effort

About 3 days: Part A ~1.5 d (reference class: Epic 6311's measurement plus a seeding script), Part B ~1.5 d
(reference class: Epic 6311's bounded simulation). Over the 1-day carpaccio norm because the slice-composition gate
puts the precursor inside this Story.

## Dogfood

On the scaled instance: start Northwind Platform's update, open System Settings and the Features tab while Task
Manager shows the Forecasting row, then read the round's log for heartbeat warnings.
