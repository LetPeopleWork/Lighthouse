# Mutation testing — new Portfolios start with default Feature sizing off

Run 2026-11-25 on frozen code after the whole-candidate review. Gate: 80 % kill rate per stack.

| stack | score | tested | killed | survived | wall clock |
| --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET), run 1 | 79.41 % | 34 | 27 | 7 | 2 m 07 s |
| Backend, run 2 (after one kill test) | **82.35 %** | 34 | 28 | 6 | about 2 m |
| Frontend | N/A | — | — | — | — |

Config: `stryker.backend.json` (whole-file mutate of `Models/Portfolio.cs` and `API/PortfoliosController.cs`).
Run from `Lighthouse.Backend/Lighthouse.Backend.Tests/`:
`dotnet stryker --config-file ../../docs/feature/new-portfolio-default-sizing-off/mutation/stryker.backend.json`.

**Frontend N/A, because** the only frontend production change deletes one property from the wizard's settings
object (`infersDefaultFeatureSize`); a deleted line carries no mutant.

## The feature's own lines

Every mutant on a line this feature changed was killed: `Portfolio.StartOnConnection` (the connection assignment
and `SizeEstimateFieldForPortfolios.HasValue`), the `InfersDefaultFeatureSize` default, and the
`StartOnConnection` call in `PortfoliosController.CreatePortfolio`.

## Kill round

`PortfoliosControllerTest.CreatePortfolio_MakesTheCreatorAnAdminOfTheNewPortfolio` kills the statement removal of
`EnsureCreatorPortfolioAdminAsync` on create (a pre-existing line in a mutated file).

## Remaining survivors (pre-existing lines, recorded, not killed)

| file:line | mutation | why it survives |
| --- | --- | --- |
| `PortfoliosController.cs:46`, `:51` | `HttpContext?.RequestAborted ?? default` → `default` | the unit tests run without an `HttpContext`, so both sides are `default`; equivalent there |
| `PortfoliosController.cs:47`, `:52` | `ConfigureAwait(false)` → `true` | no observable difference in a test; equivalent |
| `PortfoliosController.cs:143` | `SyncWithPortfolioSettings` removed in the validate endpoint | the validate tests check the connection outcome, not the settings it read; outside this feature |
| `Portfolio.cs:9` | `WorkItemTypes = ["Epic"]` → `[""]` | no test reads the initial work item types of a bare Portfolio; outside this feature |
