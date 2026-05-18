# Slice PRE — Integration test labelling fix

**Goal (one sentence)**: Tag the hidden real-API tests in `AzureDevOpsWorkTrackingConnectorTest.cs` and `LinearWorkTrackingConnectorTest.cs` with `[Category("Integration")]` so the existing taxonomy is truthful — a prerequisite for CS-H path-scoped selection and for `dotnet test --filter "Category!=Integration"` to actually skip what its name promises.

**Owner story**: US-02 (this is a back-prop fix discovered while validating the alternatives memo against multi-run data).

**Estimated effort**: ≤ ½ day. Attribute insertion + local + CI verification.

**Learning hypothesis**:
- Confirms: With both files tagged, `dotnet test --filter "Category!=Integration"` runs only the true unit/architectural suite locally (no `NotSupportedException` thrown by env-var-missing tests). The next CI run's `test-timings-backend.csv` shows ~38 s of mass moving from Unit/Other into Integration cluster, matching the empirical numbers measured during Slice-02 resume.
- Disproves: If post-fix CI numbers don't show the expected ~38 s shift, something else is going on (e.g. tests were already environment-gated and silently no-ops on CI — would require investigation).

## IN scope

- Add `[Category("Integration")]` at the class level (or per-method if class has a mix) on:
  - `Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsWorkTrackingConnectorTest.cs` (85 methods, mean 29.5 s).
  - `Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/Linear/LinearWorkTrackingConnectorTest.cs` (19 methods, mean 8.9 s).
- Audit every other file under `Services/Implementation/WorkTrackingConnectors/**` for the same pattern: any test that reads an `Environment.GetEnvironmentVariable("*Token"|"*ApiKey")` AND throws / falls back to a fake value when missing is a real-API test and gets the attribute.
- Verify locally: `dotnet test --filter "Category!=Integration"` finishes without throwing `NotSupportedException`.
- Verify in CI: the next `test-timings-backend.csv` shows the previously-mislabelled methods in the Integration cluster.

## OUT scope

- Renaming files (e.g. `AzureDevOpsWorkTrackingConnectorTest` → `*IntegrationTest`) — defer to a non-feature cleanup PR. Attribute is enough for filtering.
- Introducing per-connector sub-categories (`[Category("JiraIntegration")]` etc.) — that's CS-H's slice, not this one's.
- Changing what the tests assert.
- Removing the `?? throw new NotSupportedException(...)` fallbacks — handled in CS-H when the resolver default-skips them.

## Acceptance criteria

- AC-PRE.1: `git grep -l 'GetEnvironmentVariable.*\(Token\|ApiKey\)' Lighthouse.Backend.Tests/` returns N files. Of those, 100 % declare `[Category("Integration")]` at class or method level for every method that reads the env var.
- AC-PRE.2: `dotnet test --filter "Category!=Integration"` on a clean checkout (no API tokens in environment) completes with 0 throws and 0 NotSupportedException.
- AC-PRE.3: The next `test-timings-backend.csv` on a PR build shows `AzureDevOpsWorkTrackingConnectorTest.*` and `LinearWorkTrackingConnectorTest.*` methods in the Integration cluster (visible by re-running `Scripts/test-timings/summarise.sh` against the artifact).
- AC-PRE.4: ADO #5020 has a comment confirming the labelling fix landed.

## Dependencies

- None. Cleanly orthogonal — runs before CS-H opens.

## Reference class

Attribute-only refactor. Comparable to a `[Obsolete]` or `[Test]→[TestCase]` sweep.

## Pre-slice SPIKE

Not required. The mislabelled files were identified by reading their `Environment.GetEnvironmentVariable` calls (see Slice-02 resume analysis).

## Taste tests

- Ship 4+ new components? **No** — attribute additions on 2 files.
- Depends on a new abstraction? **No**.
- Disproves something? **Yes** — that the existing `[Category("Integration")]` taxonomy was truthful.
- Synthetic data only? **No** — verified against the next CI build's CSV.
- Identical-except-for-scale duplicate of another slice? **No**.

All taste tests pass.

## Risk

Low. The only behavioural change is that `dotnet test` with the default (no-filter) settings will now still run these tests — same as today. The change is that `--filter "Category!=Integration"` now correctly excludes them.
