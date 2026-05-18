# Slice 03B — CS-H: Path-scoped integration test selection

**Goal (one sentence)**: Skip per-connector integration tests on PR builds when the corresponding connector folder hasn't changed — reusing the existing `ci_changes.yml` path-detection mechanism — so the ~70 % of PRs that don't touch a connector get a much shorter Backend CI step, while `main` and release-tag builds always run everything.

**Owner story**: US-02 (catalog candidate CS-H).

**Estimated effort**: 1 day. The per-connector breakdown is mechanical, `ci_changes.yml` already does path detection, and the dynamic `--filter` string is a small bash block in `ci_backend.yml`.

**Learning hypothesis**:
- Confirms: After this slice, PRs that don't touch any `WorkTrackingConnectors/{Jira,AzureDevOps,Linear}` folder run zero per-connector integration tests; their `Verify Backend` step drops by ~200 s (the wall-clock-equivalent of the ~357 s saved test-sum, given CS-P's 1.8× parallel speedup). FE (~7 min) likely becomes the new CI bottleneck on those PRs, redirecting the next round of optimization effort.
- Disproves: If the resolver's "shared" whitelist is too narrow, a regression to `WorkTrackingConnectors/Base/` or the auth subsystem reaches `main` un-tested and breaks the post-merge build. Recovery: extend the whitelist and re-deploy. Bounded one-merge delay; not a coverage regression in absolute terms.

## Today's picture (as of `9a50b16f`)

Integration-tagged files and their CI medians:

| File | Category-tag today | Median (n=11 BE runs) | Real-API target |
|---|---|---|---|
| `WorkTrackingConnectors/Jira/JiraWriteBackTest.cs` | `Integration` (per-method) | ~140 s | Atlassian Cloud |
| `WorkTrackingConnectors/Jira/JiraWorkTrackingConnectorTest.cs` | `Integration` (class-level) | ~135 s | Atlassian Cloud |
| `WorkTrackingConnectors/Jira/JiraScopedTokenIntegrationTest.cs` | `Integration` (class-level) | ~75 s | Atlassian Cloud |
| `WorkTrackingConnectors/AzureDevOps/AzureDevOpsWriteBackTest.cs` | `Integration` (per-method) | ~8 s | dev.azure.com |
| `WorkTrackingConnectors/AzureDevOps/AzureDevOpsWorkTrackingConnectorTest.cs` | `Integration` (class-level, added in slice-pre) | ~30 s | dev.azure.com |
| `WorkTrackingConnectors/Linear/LinearWorkTrackingConnectorTest.cs` | `Integration` (class-level, added in slice-pre) | ~9 s | api.linear.app |
| `Services/Implementation/GithubServiceTest.cs` | `Integration` (class-level) | ~1 s | api.github.com |
| `Services/Implementation/LighthouseReleaseServiceIntegrationTest.cs` | `Integration` + `NonParallelizable` (added in CS-P fix) | ~1 s | api.github.com (cached static) |

Totals: Jira ~350 s, ADO ~38 s, Linear ~9 s, GitHub ~2 s, **~400 s real-API time per BE run**.

## Mechanism

### 1. Extend `ci_changes.yml` with per-connector outputs

`.github/workflows/ci_changes.yml` today emits three outputs (`backend`, `frontend`, `e2e`) computed from `git diff --name-only $base_ref HEAD`. Add four more:

| New output | Truthy when |
|---|---|
| `jira_connector` | Any path matches `^Lighthouse\.Backend/.*WorkTrackingConnectors/Jira/` (production or test) |
| `ado_connector` | Any path matches `^Lighthouse\.Backend/.*WorkTrackingConnectors/AzureDevOps/` (production or test) |
| `linear_connector` | Any path matches `^Lighthouse\.Backend/.*WorkTrackingConnectors/Linear/` (production or test) |
| `connector_shared` | Any path matches the **must-run-all-integrations whitelist**: `^Lighthouse\.Backend/.*WorkTrackingConnectors/(Base/|Auth/|OAuth/)` OR `IWorkTrackingConnector` OR `Models/WorkTrackingSystemConnection` OR `Models/AdditionalFieldDefinition` OR `Models/WriteBackFieldUpdate` OR `Models/OAuth/` OR `Lighthouse.sln` OR `Lighthouse.Backend.csproj` OR `Lighthouse.Backend.Tests.csproj` OR any `.github/workflows/ci.*\.yml` OR `Lighthouse.Backend/Program.cs` |

The existing `backend` output stays — it's the umbrella signal. The four new ones are sub-discriminators consumed only by `ci_backend.yml`.

Refactor the bash block in `ci_changes.yml:103-150` so the four new outputs follow the same shape (`grep -Eq <regex>` against the diff list). The "github workflow changes ⇒ true" override that exists today for `backend` carries forward to all four sub-outputs (a workflow YAML change should re-run everything, to avoid silent regressions).

### 2. Sub-category attributes on each integration test class

Add a second `[Category(...)]` to each integration-tagged class so the umbrella `[Category("Integration")]` stays for backward compat (existing `Category!=Integration` filters still work):

| File | New category to add |
|---|---|
| Three `Jira/Jira*.cs` test files | `[Category("JiraIntegration")]` |
| Two `AzureDevOps/AzureDevOps*.cs` test files | `[Category("AdoIntegration")]` |
| One `Linear/LinearWorkTrackingConnectorTest.cs` | `[Category("LinearIntegration")]` |
| `GithubServiceTest.cs` + `LighthouseReleaseServiceIntegrationTest.cs` | `[Category("GithubIntegration")]` |

`JiraWriteBackTest.cs` and `AzureDevOpsWriteBackTest.cs` have per-method `[Category("Integration")]` rather than class-level. For those, add the sub-category at class level once — NUnit composes class + method categories, so all tests in the file inherit the new sub-tag.

### 3. Plumb the new outputs through `ci.yml` to `ci_backend.yml`

`ci.yml` already calls `ci_changes.yml` and passes `backend: ${{ needs.changes.outputs.backend }}` to `ci_backend.yml`. Extend the same wiring to pass the four new outputs.

`ci_backend.yml` gains four new `inputs:` declarations (`jira_connector`, `ado_connector`, `linear_connector`, `connector_shared`) of type `string` to mirror the existing `backend` input.

### 4. Build the `--filter` string dynamically in `ci_backend.yml`

Replace the current `Test Backend` step's `dotnet test ./Lighthouse.Backend ...` line with a two-step block:

```yaml
- name: Compute test filter
  id: filter
  run: |
    # Always run everything that isn't an integration test (unit + WAF + arch tests)
    parts=("Category!=Integration")

    # Force-full when not a PR (push to main, release tag, manual dispatch)
    force_full="false"
    if [ "${{ github.event_name }}" != "pull_request" ]; then
      force_full="true"
    fi
    if [ "${{ inputs.connector_shared }}" == "true" ]; then
      force_full="true"
    fi

    if [ "$force_full" == "true" ]; then
      parts+=("Category=Integration")
    else
      [ "${{ inputs.jira_connector }}"   == "true" ] && parts+=("Category=JiraIntegration")
      [ "${{ inputs.ado_connector }}"    == "true" ] && parts+=("Category=AdoIntegration")
      [ "${{ inputs.linear_connector }}" == "true" ] && parts+=("Category=LinearIntegration")
      # GitHub integration tests are tiny (~2 s) and always run alongside non-Integration tests.
      parts+=("Category=GithubIntegration")
    fi

    # Join the alternatives with VSTest's | (logical OR)
    IFS='|' filter="${parts[*]}"
    echo "filter=$filter"
    echo "filter=$filter" >> $GITHUB_OUTPUT

- name: Test Backend
  run: |
    dotnet test ./Lighthouse.Backend \
      --filter "${{ steps.filter.outputs.filter }}" \
      --logger trx --collect:"XPlat Code Coverage" \
      -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover \
      RunConfiguration.MaxCpuCount=0
```

The filter is monotone: always `Category!=Integration | Category=GithubIntegration` baseline, plus 0–3 per-connector sub-categories. On `main` / release / `connector_shared=true`, it collapses to `Category!=Integration | Category=Integration` (i.e. every test).

### 5. Local UX — `Scripts/test-selection/dev-test.sh`

A small bash + ps1 wrapper that mirrors the CI logic: `git diff --name-only origin/main...HEAD` → emit the matching `--filter` and run `dotnet test`. Default behavior when run with no flags: behave like the CI PR path. `--full` flag overrides to `Category!=Integration | Category=Integration`. `--unit-only` overrides to `Category!=Integration` (today's fast-local mode).

Document in `docs/ci-learnings.md` as a new entry under "Tests" describing the per-connector tags + the resolver rules + the local override flags.

### 6. Stryker compatibility

Existing per-feature stryker configs (`stryker-config.bug-5016-cache-thread-safety.json`, etc.) already use `test-case-filter` to scope which tests Stryker runs against the mutation surface. They are **unaffected** by this slice — the assembly-level categories don't fight with Stryker's per-mutation filter. Verify after the slice lands by running the bug-5016 config once and confirming kill-rate is unchanged.

## Acceptance criteria

- AC-H.1: `ci_changes.yml` emits `jira_connector`, `ado_connector`, `linear_connector`, `connector_shared` outputs computed from the diff against `base_ref`. Truth-table-checked by a manual `gh workflow run` on a synthetic branch that touches only `Jira/` (expect `jira_connector=true`, others `false`), one that touches `WorkTrackingConnectors/Base/` (expect `connector_shared=true`), and one that touches only docs (all false except where `connector_shared` rules apply).
- AC-H.2: All six real-API connector test classes carry both the umbrella `[Category("Integration")]` AND a connector-specific sub-category. `GithubServiceTest.cs` + `LighthouseReleaseServiceIntegrationTest.cs` carry `[Category("GithubIntegration")]`.
- AC-H.3: `ci_backend.yml`'s `Test Backend` step uses a dynamically-built `--filter` and passes it to `dotnet test`. The filter string is echoed in the step's log for debuggability.
- AC-H.4: A PR that touches **only docs** has `Verify Backend` test step wall-clock drop by ≥ 150 s relative to the run immediately before this slice landed (proves Jira tests skipped; the slack budget allows for runner variance).
- AC-H.5: A PR that touches `WorkTrackingConnectors/Jira/JiraWorkTrackingConnector.cs` runs all Jira integration tests (`Category=JiraIntegration`) and **no** ADO or Linear integration tests. Verified by running the new local resolver script against `git diff` for that PR and confirming the emitted filter contains `JiraIntegration` but not `AdoIntegration` or `LinearIntegration`.
- AC-H.6: The first `push: main` build after this slice merges runs every integration category (forced-full). Verified by downloading `test-timings-backend.csv` and grepping for at least one `Jira`, one `AzureDevOps`, one `Linear` test row.
- AC-H.7: `Scripts/test-selection/dev-test.sh` and `dev-test.ps1` exist and work locally. Document in `docs/ci-learnings.md` with at least one worked example.
- AC-H.8: Stryker.NET kill-rate on `stryker-config.bug-5016-cache-thread-safety.json` is unchanged (≥ 80 %, same surviving mutant). Confirms the assembly-level categories don't fight per-feature Stryker filters.

## Dependencies

- slice-pre (✅ `b404eb07`) — ADO + Linear classes tagged `[Category("Integration")]`. Without it, this slice can't add the connector sub-tags consistently.
- CS-P / slice-be-parallel-enable (✅ `10903bb6` + `9a50b16f`) — parallel attribute shipped. The wall-clock savings projection assumes the 1.8× parallel speedup already in effect.
- `ci_changes.yml` (already in repo) — the path-detection script we're extending.

## Out of scope

- Frontend equivalent — Vitest already parallelizes by default; FE is currently the *new* bottleneck on PRs that benefit from this slice. Frontend optimization is a separate slice once this lands and shifts the constraint.
- Splitting `Verify Backend` into multiple parallel CI jobs — single job + filter is simpler; revisit only if the residual integration cluster on Jira-touching PRs is still too slow.
- Caching the resolved filter across commits on the same branch — every push runs `ci_changes.yml` fresh; no caching needed.
- E2E sharding — out per D6.
- Replacing the umbrella `[Category("Integration")]` with sub-categories only — keep both so existing `Category!=Integration` filters keep working (slice-pre's local-fast UX depends on this).
- Auto-detecting which `WorkTrackingConnectors/*` subfolders exist — keep the four sub-discriminators hard-coded; adding a new connector is rare and worth one PR's extra YAML.

## Reference class

Workflow/test-infra plumbing. Most similar in shape to the existing `ci_changes.yml` → `backend`/`frontend`/`e2e` fan-out: one bash regex per output, one filter string per consumer. No new third-party dependencies.

## Pre-slice SPIKE

Not required. The mechanism is mechanical and the timing assumptions are calibrated against this morning's CSVs.

## Taste tests

- Ship 4+ new components? **Borderline** — `ci_changes.yml` edit, `ci.yml` wiring, `ci_backend.yml` filter step, sub-tag sweep across 8 files, dev script (sh + ps1), ci-learnings entry. Coherent system, single goal — acceptable.
- Depends on a new abstraction? **No** — reuses `Category` attributes and the existing change-detection pattern.
- Disproves something? **Yes** — that integration tests must run on every PR.
- Synthetic data only? **No** — verified on real PR builds (AC-H.4, AC-H.5).
- Identical-except-for-scale duplicate of another slice? **No.**

All taste tests pass.

## Theory-of-constraints expectation

CI today (after `9a50b16f`): BE ~9–10 min, FE ~7 min. BE is the constraint.

CI after this slice, on a docs-only or non-connector PR (~70 % of PRs based on commit-pattern analysis from Slice-02 resume): BE ~3–4 min (the unit cluster + ~2 s GitHub integration + the parallel-speedup we already get from CS-P), FE ~7 min. **FE becomes the constraint.** This explicitly redirects the next optimization slice toward the frontend (`spike-fe-profile` etc.) rather than further BE work.

CI after this slice, on a Jira-touching PR (~10 % of PRs): BE ~6 min (unit + Jira integration), FE ~7 min. FE still the constraint.

Either way: BE stops being the bottleneck for the vast majority of PRs after this slice. That's the theory-of-constraints win.

## Risk and mitigations

- **Whitelist too narrow** — a shared-base change reaches `main` un-tested. *Mitigation*: forced-full on `push: main` catches it within one merge; rule documented; whitelist is extensible.
- **Filter string syntax error** — `dotnet test` rejects malformed filter. *Mitigation*: AC-H.3 requires echoing the filter; CI fails fast with a clear message; bash block is small enough to unit-test by eye.
- **Sub-category typo on a single class** ⇒ tests don't run for that connector on its own PRs. *Mitigation*: bug only surfaces when that connector is the only thing changed; minor and self-correcting on first such PR; not a coverage regression on `main`.
- **Concurrent dependabot PRs touching `.csproj`** — `connector_shared` forces full run, which is correct. No mitigation needed; this is by design.

## Definition of Done

1. All AC-H.* pass.
2. New `ci-learnings.md` entry under "Tests" documents the resolver rules and the local override flags.
3. `feature-delta.md` catalog row for CS-H is marked **shipped** with the commit hash.
4. `alternatives.md` ranking table marks slice-03B as shipped with the observed CI wall-clock savings.
5. ADO #5020 has a comment summarising the change (pause before `wit_update_work_item` per [[feedback-ado-workflow-rules]]).
