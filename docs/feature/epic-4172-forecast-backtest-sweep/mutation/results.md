# Mutation testing — Story 6072 (Forecast reality check, backend)

Run 2026-09-26 against `main` @ `5e50547e5`, plus the tests this pass added. Gate is 80 % kill rate.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.0.0) | **97.53 %** | 81 | 79 | 2 | 0 | 3 m 21 s |
| Frontend | N/A | — | — | — | — | — |

Config: `stryker.6072.backend.json`. Runs from `Lighthouse.Backend.Tests/`.

A first backend run on `5e50547e5` scored **91.36 %** with 7 survivors (81 tested, 74 killed, 3 m 21 s).
Five of them were missing tests and are closed below; the two left are equivalent.

## Backend

| file | tested | survived | first run |
| --- | --- | --- | --- |
| RealityCheckVerdictPolicy.cs | 59 | 0 | 59 / 2 |
| ForecastRealityCheckService.cs | 21 | 2 | 21 / 4 |
| ThroughputFilterOverride.cs | 1 | 0 | 1 / 1 |

### Closed by this pass

- **`ThroughputFilterOverride.cs:10`** — removing the method body survived because the mapping had no
  test of its own, and no caller's test told the three answers apart.
  `ThroughputFilterOverrideTest.ToFilterMode_YesAppliesTheFilterNoSkipsItAndLeavingItOutKeepsTheTeamsSetting`
  pins all three: yes applies the filter, no skips it, left out keeps the Team's setting.
- **`ForecastRealityCheckService.cs:113` (`?? []` → `[]`) and `:118` (`== level` → `!= level`)** — no
  test read the held count per level, so dropping every outcome, or counting every *other* level's
  holds, went unnoticed. `ForecastRealityCheckServiceTest.Each_level_counts_only_the_checks_it_held_in_itself`
  gives each horizon an actual that clears one more level than the last, so the four levels hold 4, 8,
  12 and 16 times, and asserts exactly those counts.
- **`RealityCheckVerdictPolicy.cs:12` (`>= 0` → `> 0`, and `All` → `Any`)** — `HasAReadingAtEveryLevel`
  had no direct test; the service test only ever fed it a forecast with no reading anywhere.
  `RealityCheckVerdictPolicyTest.HasAReadingAtEveryLevel_ZeroItemsIsAReadingButMinusOneAtAnyLevelLeavesTheForecastUnreadable`
  pins two decisions: a forecast of 0 items at a level is a legitimate reading, not a degenerate one;
  and a forecast where only *some* levels carry −1 is still unreadable.

### Accepted survivors

- **`ForecastRealityCheckService.cs:55`, `Append` → `Prepend`** — equivalent. The Team's own window is
  added to the standard ladder and the result goes straight through `.Distinct().Order()`, so where it
  was added cannot change the list.
- **`ForecastRealityCheckService.cs:113`, `?? []` → `cell.LevelOutcomes`** — unreachable. The line is
  behind `.Where(cell => cell.Sufficiency.IsSufficient)`, and the only way a cell becomes sufficient is
  `CheckedWindow.Evaluated`, which always builds its level outcomes. Every unevaluable cell carries
  `null` there and is sufficient `false`. The `?? []` only guards the nullable type.

### Not mutated

- **`ForecastRealityCheckController.cs`** — covered by the HTTP acceptance tests under
  `API.Integration` (routes, RBAC, 404 / 400, filter status). The config's `test-case-filter` keeps
  them out: each boots a `WebApplicationFactory`, which costs minutes per mutant against about three
  minutes for the whole unit run.
- **`RealityCheckResultDto.cs`, `RealityCheckInputDto.cs`, `RealityCheckVerdictTypes.cs`,
  `IForecastRealityCheckService.cs`** — records, enums and an interface; no logic to mutate.

## Frontend

N/A for this gate. The reality check's UI is being replaced by Story #6094, and the maintainer scoped
this gate to the backend.

## Configuration notes

The `test-case-filter` names `ThroughputFilterOverride` as a substring, so the new
`ThroughputFilterOverrideTest` joins the mutant loop without a config change. It also takes in
`ForecastControllerTest` and `TeamMetricsControllerTest`, the two other callers of the filter
override, and excludes `API.Integration` and `Integration.Containers` for the cost reason above.
