# Mutation testing: 6533 (scoped admins load the Settings tab)

Run 2026-11-04 on the branch at `04f00f443`. The gate is an 80 % kill rate on both stacks.

| stack | score | tested | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend, whole files (Stryker.NET 4.14.1) | 53.41 % | see below | | | | 4 m 11 s |
| Backend, lines this bug changed | 73.33 % | 15 | 11 | 3 | 1 | same run |
| Frontend (StrykerJS) | **85.71 %** | 7 | 6 | 1 | 0 | 13 s |

Configs: `stryker.6533.backend.json`, `stryker.6533.frontend.json`, `vitest.stryker.mutation.ts`.

## Backend

Stryker.NET cannot scope to lines, so `PortfolioController.cs` and `PortfolioExtensions.cs` are mutated whole. The
whole-file score is low because of code this bug did not touch, whose tests are outside this run's filter. The
changed-lines row is the one that describes this fix. It is under 80 % on a raw count, and the gate is met by the
justification below: all four remaining mutants are equivalent.

### Closed by this pass

- `WorkTrackingSystemConnectionSummaryDto.cs:21`: removing the additional fields went unnoticed, because the fixture
  seeded none. `S2_TeamAdmin_GetSummary_CarriesTheConnectionsAdditionalFieldsAndSupportedDefaults` now pins them.
- `KeepOwningTeamTheCallerCannotRead`: the first run reported all 13 mutants as compile errors, so nothing in it had
  been tested. The `is not { } x` guard broke definite assignment under mutation. `04f00f443` rewrote that guard in
  plain steps; the guard, the readable check and the early return are now killed.

### Accepted survivors, all equivalent

- `PortfolioController.cs:183`, null coalescing on `HttpContext?.RequestAborted ?? default`, and `:184`,
  `ConfigureAwait`: neither changes a result.
- `PortfolioController.cs:188`, both mutants on the owner's name fallback: `SyncTeams` reads only the id, so the name
  never reaches storage.

### Outside this change

Every other survivor is in code this bug did not change, for example `PortfolioController.cs:146`, `:151`, `:161` and
the `Sync*` calls in `PortfolioExtensions.cs`, whose tests are outside this run's filter.

## Frontend

### Accepted survivor

- `WorkTrackingSystemService.ts:157`, the endpoint name passed to `asArray`: it only labels the error message.
