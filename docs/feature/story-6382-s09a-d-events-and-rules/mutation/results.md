# Mutation testing — 6382 (S09a feature D, events and rules)

Run 2026-11-28 against `main` @ `65da2bf61` (code identical to `cc508785b`). Gate is 80 % kill rate on the backend;
the frontend is N/A because no frontend file changed.

| stack | score | tested | killed | survived | timeout | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.1.0) | **91.84 %** | 98 | 86 | 6 | 4 | 2 | 4 m 47 s |

Config: `stryker.6382.backend.json`. The filter keeps `ForecastUpdaterTest`, the D2 oracle
`ForecastRoundsKeepTheirRefreshLogEntryTest` (it drives the application-service port and starts no host) and the
`ForecastRoundFinished` tests; host and integration tests are left out.

## Backend

| file | killed | timeout | survived | no coverage | compile error | ignored |
| --- | --- | --- | --- | --- | --- | --- |
| `Modules/Forecasting/Adapters/Background/ForecastUpdater.cs` | 67 | 3 | 5 | 2 | 18 | 29 |
| `Modules/Forecasting/Contracts/ForecastRoundFinished.cs` | 6 | 0 | 1 | 0 | 0 | 2 |
| `Modules/WorkItems/Application/Policies/ForecastRoundFinishedRefreshLogHandler.cs` | 13 | 1 | 0 | 0 | 0 | 1 |

Stryker also listed `Program.cs` (754 ignored, 16 compile errors, none tested): it is not in `mutate`, and Stryker
reports it because the handler registration sits there.

### Closed by this pass

None needed. Every mutant of the new handler was killed. Swapping, dropping or inverting any outcome-to-flag mapping
fails the D2 oracle.

### Accepted survivors

- `ForecastUpdater.cs:218` `stopwatch.Stop();` removed: the elapsed time feeds only a log line, and reading a running
  stopwatch a moment later gives the same log. Equivalent for behaviour.
- `ForecastUpdater.cs:280` (statement and string) and `:373` (statement and string): the error and warning log calls of
  the write-back and forget-inputs failure paths. These are log content, and these paths are older than this Story.
- `ForecastRoundFinished.cs:24` `ArgumentNullException.ThrowIfNull(portfolio);` removed: a defensive guard. The only
  caller passes the portfolio it is updating, which is never null, so no public path can observe it.

### No coverage

- `ForecastUpdater.cs:146`, `:151`: the message text of the `NotSupportedException` thrown by the periodic-refresh
  members the forecast updater does not support. No caller reaches them. Older than this Story.

### Not mutated

The other files feature D changed only in `namespace` and `using` lines (the 15 event types and their handlers) or
are rule tests. A namespace move changes no logic, so they are not re-mutated (design D-15).
