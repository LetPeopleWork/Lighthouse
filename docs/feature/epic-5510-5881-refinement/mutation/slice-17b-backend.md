# Mutation testing — 6156, slice 17b backend (votes from the clients)

Run 2026-10-06 against `main` @ `8cfcdf03c`, with the production code frozen at that commit. Gate is
80 % kill rate. The clients half of this slice is recorded in `clients-slice-09.md`.

| stack | score | tested | killed | survived | no coverage | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.0.0), lines this slice wrote | **100.00 %** | 78 | 78 | 0 | 0 | 0 | 8 m 32 s |
| Backend, every mutated file whole | 97.50 % | 80 | 78 | 0 | 2 | 0 | (same run) |

Config: `stryker.6156.backend.json` (copied from `stryker.6153.backend.json`, scoped to this slice's three
production files). Command, from `Lighthouse.Backend/Lighthouse.Backend.Tests/`:

```bash
rm -rf StrykerOutput
dotnet stryker --config-file ../../docs/feature/epic-5510-5881-refinement/mutation/stryker.6156.backend.json
```

Scope = the production files changed in `95c26a101..8cfcdf03c`. Scope sanity check from the log:
`78 total mutants will be tested`, all of them in the three files below (confirmed per file from
`reports/mutation-report.json`, not from the headline). The `test-case-filter` takes the unit namespaces
only — `Tests.Services.Implementation.Refinement`, `RefinementVotesControllerTest`, `SizingVoteDtoTest` —
and excludes `API.Integration`, `Integration.Containers` and `ArchUnit`, so the acceptance scenarios
(`Slice17ClientVotes*`, `SizingVotesAcceptanceTest`) do not count toward the score.

## Per file

| file | killed | survived | no coverage | score, first run | score, after this pass |
| --- | --- | --- | --- | --- | --- |
| `API/DTO/SizingVoteDto.cs` | 10 | 0 | 0 | 100.00 % | 100.00 % (no change needed) |
| `Services/Implementation/Refinement/VoterIdentityResolver.cs` | 35 | 0 | 0 | 100.00 % | 100.00 % (no change needed) |
| `API/RefinementVotesController.cs` | 33 | 0 | 2 | 94.29 % | 94.29 % (no change needed) |

## Closed by this pass

None. Every mutant on a line this slice wrote was killed on the first run, so no kill tests were written
and no re-run was needed.

## Not chased — survivors on lines this slice did not touch

Both are `String mutation → $""` on the message of an `UnreachableException` in a `switch`'s discard
arm, and both lines predate this slice (`git blame`: `4ef168d97` and `7abb3c9dd`).

| line | code | why it is left |
| --- | --- | --- |
| 157 | `_ => throw new UnreachableException($"No such sizing outcome: {outcome}")` in `Answered` | Every defined `SizingOutcome` has its own arm; the message is diagnostic text for a value the enum does not have. |
| 171 | `_ => throw new UnreachableException($"No such voter refusal: {refusal}")` in `RefusedWithoutAVoter` | Unreachable through the public API: the refusal comes from the sealed `VoterIdentityResolver`, which only ever sets one of the four defined values when it returns no voter. |

## Equivalent mutants

None among the in-slice mutants.
