# Story 6374 (S05f): mutation run on the frozen candidate

Run on 2026-11-10, Stryker.NET 4.16.0 under the side-by-side x64 SDK
(`docs/feature/epic-5511-task-manager/mutation/run-backend-x64.ps1`), config [`stryker.s05f.json`](stryker.s05f.json),
code at `25d80411e` (both features delivered, after both refactors). 402 mutants tested, 12 min 19 s.

## The bar

From the DESIGN (D-9): no touched file lower than its baseline, measured with S02's filter family; the new read
methods at 80 % or more.

## Results

| File | Killed | Survived | No coverage | Score | Baseline (S02 slice 8) |
|---|---:|---:|---:|---:|---:|
| `Modules/FlowMetrics/Application/TeamMetricsService.cs` | 89 | 107 | 85 | 31.67 % | 30.58 % (100 / 112 / 115) |
| `Modules/FlowMetrics/Application/PortfolioMetricsService.cs` | 45 | 90 | 68 | 22.17 % | 27.60 % (69 / 113 / 68) |
| `Services/Implementation/Repositories/FeatureRepository.cs`, lines added by this Story | 37 | 1 | 0 | **97.37 %** | new |
| `Services/Implementation/Repositories/WorkItemRepository.cs`, lines added by this Story | 18 | 1 | 0 | **94.74 %** | new |
| `FeatureRepository.cs`, whole file | 49 | 1 | 19 | 71.01 % | not measured before |
| `WorkItemRepository.cs`, whole file | 20 | 1 | 2 | 86.96 % | not measured before |

The two state-transition repositories produce no mutant: their new reads are one `Where(...Contains...).ToList()`
expression, which Stryker leaves alone.

## Why the Portfolio service score dropped although nothing was lost

The service lost 24 killed and 23 surviving mutants and kept every no-coverage mutant (68 before and after). Those
47 mutants were the inline `GetAllByPredicate` lambdas, which this Story moved into `FeatureRepository`. There they
are now 37 killed and 1 survived, so the same predicates are guarded far better than before (51 % → 97 %). What is
left in the service is the code the unit tests always covered least; its ratio falls because better-covered code
moved out, not because a test stopped guarding anything. No survivor or no-coverage mutant was added to either
service. Read strictly, "no file lower than its baseline" is broken for this one file; the maintainer may want to
restate the bar for a move as "no survivor or no-coverage mutant added".

## Survivors on the new lines (recorded, not killed)

The new lines are above 80 %, so the gate stops here, as the run's instructions say. Both survivors are real:

- `WorkItemRepository.cs:37`, `CountToDoOrDoingItemsOfTeam`: `i.TeamId == teamId` → `!=` survives. The parity
  fixture's two teams have the same number of To Do and Doing items, so the other team's count looks the same.
  Kill: give one team a different number.
- `FeatureRepository.cs:114`, `DoneFeaturesOfPortfolio`: `Portfolios.Any` → `All` survives. No Done feature in the
  parity fixture belongs to two portfolios. Kill: put one there.

Both kills are fixture edits to the parity support, so they need the oracle re-approved; worth doing with the next
Story that touches these classes (S05g), not after the candidate review.
