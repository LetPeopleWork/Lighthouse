# Dogfood, phase 01 — reconstructing cycle-time history on the development database

Run on 2026-09-22 against the restored development database
(`Lighthouse.Backend/Lighthouse.Backend/DB_Backup` copied over the project-directory database),
started with `Lighthouse.Backend/Start-DevServer.ps1` on <http://localhost:5169>.

The instance holds one team, `New Team` (id 1). The chart read is the one the Predictability tab
issues: `GET /api/latest/teams/1/metrics/percentiles-over-time?metricType=CycleTime&horizon=30`.

## The CT-30 line, before and after

**Before** — four points across seventeen days, and the whole interior of the period is missing:

| Recorded on | p50 | p70 | p85 | p95 |
|---|---|---|---|---|
| 2026-09-05 | 1 | 1 | 1 | 2 |
| 2026-09-19 | 1 | 1 | 2 | 2 |
| 2026-09-20 | 1 | 1 | 2 | 2 |
| 2026-09-21 | 1 | 1 | 2 | 2 |

Between 2026-09-05 and 2026-09-19 sit **thirteen days with nothing on them** — 2026-09-06 through
2026-09-18 — the stretch this instance was switched off. A coach reading the tab saw a line jump
that gap in one straight segment, and nothing on the chart said the gap was there.

**After** — the same request over 2026-09-05 to 2026-09-22 answers with **eighteen points**, one per
day, the thirteen-day gap filled in. Widening the request to 2026-05-01 to 2026-09-22 settles at
**145 points**, which is every day in that window.

The filled days read p50 1, p70 1, p85 2, p95 2 — the same tuple the days either side of the gap were
recorded at, which is what a period with no configuration change should produce.

## Two counted KPIs

Counted on the dogfood database after the fills above.

**No row exists for any day after the team's `UpdateTime`.** Count: **0**, against an `UpdateTime` of
2026-09-22 12:12. The filling stops where the team's own observation stops; it does not invent days
the instance was never in a position to watch.

**Rows with all four percentiles zero: 7 — the same 7 as before the run, and this needs saying
plainly.** All seven belong to owner id 2, a team and portfolio that no longer exist on this
instance, and all seven are dated 2026-09-19. They were written by the recorder as it behaved
*before* this story, which wrote four zeros for a quiet day. Reconstruction never rewrites a day that
is already there, so nothing in this story removes them.

What the run does establish is the delta: the database went from 36 percentile rows to 464, and
**not one of the 428 rows added is all-zero**. The story stops new ones being written. It does not
clean up the ones already on disk, and no step in this phase claims to. An instance that wants those
seven gone needs a data fix that has not been written.

## Added latency

Measured on the restored database, same window for both, warmed first so neither sample pays for
start-up. Eight requests each.

| | samples (ms) | mean | median |
|---|---|---|---|
| Read that discovers a gap | 1.93, 2.28, 2.33, 2.39, 2.21, 2.20, 2.24, 2.18 | **2.22 ms** | 2.21 ms |
| Read that finds nothing missing | 2.51, 2.03, 2.72, 1.90, 1.94, 1.99, 1.83, 1.86 | **2.10 ms** | 1.96 ms |

**Added latency: +0.12 ms on the means, +0.25 ms on the medians.** The target was under fifty
milliseconds; the discovery is roughly two hundred times cheaper than that. The reason it is this
small is that discovering a gap only costs the request the discovery — the writing happens after the
answer has gone out, which is the property the chart-load scenario pins.

**This measurement is not covered by CI, and nothing should be filed as though it were.** A test that
asserts one wall-clock reading is smaller than another will fail on a loaded parallel runner for
reasons that have nothing to do with the code, so the CI scenario pins the property the budget stands
for — a read answers with what is there and writes nothing while the reader waits — rather than the
timing. If the budget is ever in question, it has to be measured again by hand, the way it was here.

## What this run does not show

Every percentile on this team is 1 or 2, and **no configuration changed in the period covered** — no
state mapping edited, no cycle-time definition changed, no item deleted or re-parented. So this run
shows the mechanism working on real history. It says nothing about whether a day worked out afterwards
still matches a day that was watched when the configuration it was watched under has since been
changed. That question has no probe here and none anywhere else yet; it is carried forward as an open
item.
