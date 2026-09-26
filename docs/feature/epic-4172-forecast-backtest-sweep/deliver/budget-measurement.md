# The reality check against its time budget

## The budget, and where it comes from

The reality check is one click that answers synchronously, inside the request. The budget is a
**median of at most 5,000 ms and a maximum of at most 10,000 ms over twelve samples**. It is a
statement about how long someone waits at a button before deciding it is broken, not a throughput
figure, so no measurement chose it. What the measurement answers is whether the sweep the product
actually ships fits inside it.

## Verdict

**Met, with room.** On a Team the size of a real one in this product's development database, the
sixteen-cell sweep has a cold median of **540.5 ms** and a maximum of **604 ms**; the twenty-cell sweep
has a cold median of **657 ms** and a maximum of **754 ms**. That is between seven and nine times
inside the median budget and thirteen to sixteen times inside the maximum. Even the 20,000-item row, at a **1,730.5 ms**
median and **1,815 ms** maximum, stays about three times inside.

The query counts are exactly the ones the design predicted: **20** on a cold cache for a Team on the
standard ladder, **24** for a Team whose own window adds a fifth, and **0** on a warm one. They do not
grow with the size of the Team.

## What was measured, and on what

| | |
|---|---|
| Machine | `Linux cachy-desktop 7.2.7-1-cachyos #1 SMP PREEMPT_DYNAMIC Thu, 24 Sep 2026 10:44:37 +0000 x86_64 GNU/Linux` |
| CPU | AMD Ryzen 5 5600X 6-Core Processor (12 threads), 31 GB RAM |
| Runtime | .NET SDK 10.0.112, Debug build, under the NUnit test host |
| Commit | `c7dd98bd39a6601f239da56aa3213d0b7ab74713`, plus the probe change committed with this record |
| Trial count | **10,000** per simulated run — `ForecastSimulationLimits.Default`, the value the product registers |
| Code measured | the production `ForecastRealityCheckService`, over the real `TeamMetricsService`, `WorkItemRepository` and `ForecastService`. No copy of the sweep. |
| Horizons | 7, 14, 28 and 56 days — the service's own |
| Sampling windows | 14, 30, 60 and 90 days, plus the Team's own window when it is off that ladder |
| Database | real SQLite **file**, schema from the SQLite migrations, a command interceptor counting every query |
| Seeded history | one Team; N finished Work Items with closed dates drawn uniformly across the 365 days before today (fixed seed) |
| Probe | `Lighthouse.Backend.Tests/API/Integration/ForecastRealityCheck/RealityCheckWallClockProbe.cs`, `[Explicit]`, run with `dotnet test --filter FullyQualifiedName~RealityCheckWallClockProbe` |

**Method.** Each case seeds a fresh database once. It then takes one unrecorded sample, so the first
recorded one does not carry the cost of the process compiling the code, followed by **twelve recorded
samples**. Every sample builds fresh services, so its first run meets an empty metrics cache — the
state a first click lands in — and is recorded as **cold**; a second run on the same services, meeting
the cache the first one filled, is recorded as **warm**. Median of twelve is the mean of the middle two.
Nothing else from this session ran while measuring. The desktop itself was not idle — an IDE with its
language servers and a local Kubernetes node were running, with a load average near 10 when the probe
started — so these numbers include that background noise rather than being a best case.

**Baseline.** One shipped backtest exactly as `ForecastController.RunBacktest` performs it — a read of
the sampling window, one `HowMany`, a read of what was delivered — on the same 615-item Team, scoring
the last 28 days from the 30 before them, twelve samples, same method.

## The numbers

Wall clock in milliseconds.

| Case | Cells | Cold median | Cold max | Warm median | Warm max | Cold queries | Warm queries | Unrecorded first sample |
|---|---|---|---|---|---|---|---|---|
| **Single backtest, 615 items** (baseline) | 1 | **46.5** | **52** | 36 | 41 | 2 | 0 | 51 |
| **615 items, window 30** | 16 | **540.5** | **604** | 491 | 555 | **20** | 0 | 867 |
| **615 items, window 45** | 20 | **657** | **754** | 608 | 689 | **24** | 0 | 647 |
| 5,000 items, window 30 | 16 | 775.5 | 897 | 481.5 | 549 | 20 | 0 | 772 |
| 20,000 items, window 30 | 16 | 1,730.5 | 1,815 | 501 | 545 | 20 | 0 | 1,684 |

Every cell was evaluated and simulated in every case — none was skipped as having too little data, so
none of these numbers is flattered by a cell that did no Monte Carlo.

The raw samples, in the order taken:

- Single backtest — cold 50, 51, 52, 47, 47, 46, 46, 46, 46, 46, 49, 46; warm 41, 39, 38, 38, 35, 35, 36, 36, 38, 36, 36, 36
- 615 / window 30 — cold 571, 536, 516, 517, 545, 591, 583, 590, 604, 536, 520, 524; warm 532, 472, 473, 470, 555, 507, 536, 545, 535, 471, 469, 475
- 615 / window 45 — cold 648, 657, 657, 651, 653, 739, 731, 729, 754, 671, 650, 646; warm 616, 597, 597, 597, 689, 678, 678, 655, 675, 599, 600, 590
- 5,000 / window 30 — cold 837, 791, 852, 746, 745, 840, 809, 760, 897, 741, 740, 744; warm 483, 549, 514, 484, 477, 542, 479, 476, 498, 480, 474, 474
- 20,000 / window 30 — cold 1,815, 1,771, 1,787, 1,725, 1,809, 1,643, 1,799, 1,652, 1,632, 1,736, 1,721, 1,650; warm 517, 527, 527, 545, 478, 475, 536, 479, 485, 537, 476, 478

## What the numbers say

- **The Monte Carlo is the cost, and it scales with cells, not with the Team.** The warm sweep is
  about 30 ms per cell at every volume (491 ms for sixteen cells, 608 ms for twenty), against 36 ms for
  the one cell of the single backtest. The sweep costs about what sixteen or twenty backtests cost,
  which is what the design reasoned.
- **Only the cold path grows with the Team**, because each of the twenty (or twenty-four) reads
  fetches every finished Work Item. Cold minus warm is about 50 ms at 615 items, about 300 ms at 5,000
  and about 1,230 ms at 20,000.
- **The twenty-cell case costs what four more cells should:** about 115 ms more than sixteen, cold and
  warm alike. Its 24 queries confirm what the design predicted for it — the Team's own window misses
  the cache once per horizon and adds no read of what was delivered.
- **These are lower than the earlier sixteen-cell figures** (701 ms cold median at 615 items). That
  earlier probe scored horizons of 14, 28, 42 and 56 days; the product scores 7, 14, 28 and 56, so
  each simulated run covers fewer days. It also took three samples, not twelve.

## What the numbers are NOT

- **Not a production measurement.** SQLite, in-process, a Debug build, under a test host. No Kestrel,
  no HTTP, no JSON serialisation, no authentication or RBAC, no concurrent load. A loaded production
  instance, or one on Postgres over a network, will be slower.
- **One machine.** A desktop CPU; a small cloud instance will be slower per simulated run, and the
  Monte Carlo is most of the cost.
- **Not the development instance.** The slice brief asked for the shipped endpoint on the development
  instance; this is the production service code over a seeded database sized like a Team on that
  instance. It measures the sweep's own cost, which is what the budget question is about.
- **The controller's filter-status read is not included** in either the sweep or the baseline. It is
  one read, the same for both, and does not change the comparison.
- **Seeded, not real, history.** Closed dates are spread uniformly; a real Team is lumpier. The cost of
  a run does not depend on what the history says, only on how much of it there is, so this does not
  bend the numbers — but it is not the same data.

The headroom on the 20,000-item row is real but smaller than it looks for the reasons above. If a Team
ever approaches that size, the recorded remedy — reading the finished Work Items once per check and
projecting them per window — collapses the only part of the cost that scales with the Team.
