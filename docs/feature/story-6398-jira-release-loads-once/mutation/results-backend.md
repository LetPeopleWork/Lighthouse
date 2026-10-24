# Mutation testing — 6398 backend (a Jira Release loads once)

Run 2026-10-24 against `main` @ `a0787b6d0`, with Stryker.NET 5.0.0. The gate is an 80 % kill rate.
The frontend run is recorded separately, by its own pass.

| run | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Full run (`stryker.6398.backend.json`) | **86.36 %** | 22 | 19 | 3 | 0 | 2 m 30 s |
| Kill round, `DeliverySourceResolver.cs` only | **100.00 %** | 7 | 7 | 0 | 0 | 1 m 57 s |
| Combined after the kill round | **100.00 %** | 22 | 22 | 0 | 0 | |

The run starts from `Lighthouse.Backend.Tests/`. Its test filter covers `JiraReleaseSweep*`
(the cost line and the sweep line through the connector), `JiraDeliverySourceProviderTest` and
`DeliverySourceResolverTest`. Every Integration category and the live connector categories are
left out, and so is every acceptance suite booted through the web application factory.

For the kill round, `mutate` was narrowed to the one file that still had survivors and the filter
to `DeliverySourceResolverTest`. `--with-baseline` was not used, because it tests nothing when only
the tests change.

## Per file

| file | tested | killed (full run) | survived (full run) | survived (after the kill round) |
| --- | --- | --- | --- | --- |
| JiraReleaseSweepCost.cs | 10 | 10 | 0 | 0 |
| JiraReleaseSweepTally.cs | 5 | 5 | 0 | 0 |
| DeliverySourceResolver.cs | 7 | 4 | 3 | 0 |

Stryker did not test some mutants:
- **Compile errors (12):** all 12 are in `DeliverySourceResolver.cs`. They are pattern-variable and
  `is not` mutations that do not compile, so Stryker cannot run them.
- **"Block already covered" (5):** one each in `WriteTo`, `CountVersionPage` and `OffersSource`,
  and two in `CountProjectRead` (its body and the skipped-project branch). Stryker skips a block-removal mutant when the statements inside the
  block are already mutated on their own.

## Closed by this pass

The three survivors were the null guards in `DeliverySourceResolver`. They are on
`ResolveForPortfolio(portfolio)`, `ResolveForPortfolio(sourceReferences)` and
`OffersSource(portfolio)`. Three new tests in `DeliverySourceResolverTest` now pin them. Each
asserts the exact exception and the name of the missing argument.

The name matters for the reference list. Without its guard, `Distinct()` still throws an
`ArgumentNullException`, but one naming `source`. That test also checks that the remote is never
asked.

## Accepted survivors

None.

## Not mutated

- **The sweep inside `JiraWorkTrackingConnector.cs`**: `SweepReleasesWithoutThrowing`, the
  plumbing that feeds the tally, and the early return when the remembered list is used. The file
  is about 3,670 lines, and Stryker.NET ignores line spans. Mutating the whole file would bury this
  Story's few dozen lines under untouched connector code. The behaviour is covered through the
  connector's entry points by `JiraReleaseSweepLineTest`, which pins the following:
  - every count and the elapsed time;
  - that a remembered list writes no second line;
  - the cases of an unreachable Jira and an address that cannot be used;
  - that a project refusing its versions makes the list incomplete;
  - that an unexpected failure still writes what was counted before it.

  The line's wording lives in `JiraReleaseSweepCost`, which is mutated in full.
- **The purpose arguments in `DeliveriesController`, `DeliverySourcesController` and
  `DeliverySourceSyncService`**: each is a single enum constant passed to the resolver. Each
  caller's own tests pin it, and only a line span could isolate it in files this size.
