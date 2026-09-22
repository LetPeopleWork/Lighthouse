# Slice 01 — The trend my data already supports (CT-30, team scope)

**Story**: US-01 | **ADO**: 6053 | **Estimate**: ~1 day (incl. SPIKE-01) | **Reference class**: `DemoPercentilesBackfillHandler` (backdating idiom), `BlockedCountSnapshotRecordingHandler` (recording idiom)

## Goal

A flow coach opening Team → Metrics → Predictability → Percentiles Over Time sees a continuous CT-30 line across
the capped window instead of the four points the recorder happened to catch.

## Pre-slice SPIKE-01 (timeboxed, read-only, **run first**)

The one unknown that can invalidate a locked decision. Ground truth already located: the dev DB backup holds
**4 days the recorder genuinely wrote** (2026-09-05, 09-19, 09-20, 09-21) alongside a year of work items.

Probe, against a throwaway copy of `Lighthouse.Backend/Lighthouse.Backend/DB_Backup/LighthouseAppContext.db`
(holds live credentials — copy out, never print a decrypted value, delete after):

1. **Fidelity.** For each of those 4 days, call `GetCycleTimePercentilesForTeam(team1, D-30, D)` and diff
   P50/P70/P85/P95 against the persisted `PercentilesOverTimeSnapshot` row.
2. **Cost.** Time one day's reconstruction for the full team family set; extrapolate to 30 / 90 / 365 days.
3. **Absence.** Reconstruct into the thin end of the year (e.g. 2025-10) and record whether percentiles come
   back all-zero, and on how many days.

Write nothing. Persist findings to `docs/feature/story-6053-reconstruct-over-time-history/spike/findings.md`.

**Decision gates:**

- Fidelity diverges materially on any of the 4 days → **D6 is withdrawn**; stop and re-decide marking before
  building. This is the whole point of running it first.
- Cost extrapolation exceeds a few seconds per owner at 90 days → D4's cap is set below 90, and D3's background
  mechanism gets DESIGN attention rather than being assumed cheap.
- Zero-rows appear → D7's absence gate is mandatory, not precautionary (already assumed mandatory; this
  measures how often it fires).

## IN scope

- Cycle-time percentiles at **horizon 30 only**, **team scope only**.
- Gap detection over the requested range: leading and interior days with no row (D5).
- Walk-back terminating at the data floor, under a cap (D4).
- Floor at the owner's `UpdateTime` — no day after it is reconstructed (D5).
- Absence gate — never persist `P50=P70=P85=P95=0`, **on both the reconstruction path and the forward
  recorder** (D7 as answered by DDD-13). Gating only reconstruction would make D6 false: a day's row would
  depend on which path reached it first, and fill-if-absent makes the recorder's all-zero row permanent.
  **This half is a behaviour change to shipped code** — the daily recorder stops writing all-zero percentile
  rows — so it needs a release-notes line and a `docs/metrics/predictability.md` update. Rows already
  written stay; no repair migration.
- Idempotency — a day already carrying a row is never rewritten.
- Non-blocking: the series read enqueues and returns what exists now (D2 + D3).
- **The maintenance-gate coupling, both directions (DEVOPS-1, gate G-5).** `DatabaseMaintenanceGate`
  consults a pass-in-flight predicate owned by the filler alongside `statusStore.HasActiveWork()`, and the
  filler abandons its pass when a maintenance operation is already active. **Added 2026-09-22 after DEVOPS**:
  this is a safety property, not a feature, and slice 01 is the first slice that writes a row — deferring it
  would ship a window in which `RestoreBackup` can run mid-pass and replace the database under a filler
  holding an open context.

## OUT of scope

- CT-60, CT-90, work-item age, portfolio scope → slice 02.
- Every PBC family → slice 03.
- Empty-state copy and docs → slice 04.
- Marking reconstructed points (D6 says no — SPIKE-01 confirmed it for the mechanism, so this stays out).
- The ADR-108 / ADR-109 amendments themselves (written at finalization, not mid-slice).

## Learning hypothesis

**Disproves if it fails**: "a missing day is recomputable from stored history at the same value the recorder
would have written." If SPIKE-01's diff is non-zero, the premise of the whole story is wrong and D1/D6 both
fall — better to learn that in hours than after three slices.

**Confirms if it succeeds**: the reconstruction seam is a shifted-window call on an existing service, so slices
02 and 03 are parameter and family additions rather than new mechanisms.

## Acceptance criteria

1. SPIKE-01 findings recorded; D6 explicitly confirmed or withdrawn in `feature-delta.md`.
2. On the restored dev DB, team 1's CT-30 series grows from 4 dated points to the full capped window, including
   the 13-day interior gap between 2026-09-05 and 2026-09-19.
3. A day the recorder genuinely wrote is **not** rewritten by reconstruction (idempotency).
4. No row exists for any day after team 1's `UpdateTime`.
5. No row exists with all four percentiles zero.
6. The series request that discovers the gap returns within today's latency envelope (< 50 ms added).
7. Backend `dotnet build` zero warnings; `dotnet test` green with connector categories excluded.

## Dependencies

Epic 5427 shipped (`PercentilesOverTimeSnapshot`, `PercentilesOverTimeRecordingHandler`,
`IPercentilesOverTimeSeriesQuery`, `GET .../metrics/percentiles-over-time`). No migration. No RBAC change.

## Dogfood moment

Same day: restore the dev DB, start via `Start-DevServer.ps1`, open the team Predictability tab, and read the
CT-30 line across the window that previously held four points.
