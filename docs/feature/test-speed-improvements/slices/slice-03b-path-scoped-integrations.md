# Slice 03B — CS-H: Path-scoped integration test selection

**Goal (one sentence)**: Default `dotnet test` to skipping real-API integration tests; resolve which connector integration suites to run from `git diff` so PRs that don't touch a connector pay none of its cost locally or in CI — preserving end-to-end coverage via a forced-full run on `main`.

**Owner story**: US-02 (catalog candidate CS-H, added during Slice-02 resume).

**Estimated effort**: 2 days. Category sub-tags + resolver script + GHA workflow rewrite + Stryker config sweep + 2-3 PR validation cycles.

**Learning hypothesis**:
- Confirms: 88 % of recent commits (since 2026-04-01) don't touch any connector path; after this slice, those PR builds drop ~322 s = ~56 % of BE wall-clock on average. Local `dotnet test` (no args) finishes ~5 minutes faster than today on a clean checkout without API tokens.
- Disproves: If `main`-cadence catches more than one regression in the first month that the per-PR cadence would have caught, the resolver's "shared paths" whitelist is too narrow — back-prop to expand it (or fall back to CS-A cadence-split on Jira specifically).

## IN scope

- Per-connector category sub-tags:
  - `JiraScopedTokenIntegrationTest.cs`, `JiraWriteBackTest.cs`, `JiraWorkTrackingConnectorTest.cs` → `[Category("JiraIntegration"), Category("Integration")]`.
  - `AzureDevOpsWriteBackTest.cs`, `AzureDevOpsWorkTrackingConnectorTest.cs` → `[Category("AdoIntegration"), Category("Integration")]`.
  - `LinearWorkTrackingConnectorTest.cs` → `[Category("LinearIntegration"), Category("Integration")]`.
  - `LighthouseReleaseServiceIntegrationTest.cs`, `GithubServiceTest.cs` → `[Category("GithubIntegration"), Category("Integration")]`.
- `Scripts/test-selection/select-tests.sh` + `select-tests.ps1` — read `git diff --name-only origin/main...HEAD`; emit `dotnet test --filter "..."` arguments. Rules:
  - Touched `WorkTrackingConnectors/Jira/**` (any branch) → include `JiraIntegration`.
  - Touched `WorkTrackingConnectors/AzureDevOps/**` → include `AdoIntegration`.
  - Touched `WorkTrackingConnectors/Linear/**` → include `LinearIntegration`.
  - Touched `WorkTrackingConnectors/Base/**`, `IWorkTrackingConnector`, `Models/Connectors/**`, the auth subsystem, the cache subsystem, or `Lighthouse.sln` / project files → **include all** integration categories.
  - Touched only frontend / docs / unrelated backend → **exclude all** integration categories.
  - `--full` flag overrides: include every category.
- Default local UX: `dotnet test` (no args) ⇒ wrapper auto-detects "no diff vs main" or "no relevant diff" and resolves to `--filter "Category!=Integration"`. Explicit `dotnet test --filter "..."` always wins.
- GitHub Actions workflow:
  - On `pull_request`: run `select-tests.sh` to produce the filter; pass to `dotnet test`.
  - On `push: main`: always force `--full` (every integration test runs post-merge).
  - On `release` tag: always force `--full`.
- Stryker config sweep:
  - Each per-feature `stryker-config*.json` gains an optional `"test-case-filter"` matching the mutation target. Default: exclude all integrations unless the mutation target is under a connector folder.
  - `stryker-config.json` (root) keeps current behaviour (full run) so the per-feature configs stay opt-in.
- Document the resolver rules in `docs/ci-learnings.md` (a new section "Test selection rules").

## OUT scope

- Removing the `?? throw new NotSupportedException(...)` fallbacks — done in this slice as a clean-up *only* for the connector files we re-categorise; the resolver makes them dead code on default-skip paths, but they stay as safety nets for explicit-include paths.
- Frontend equivalent — out (Vitest runs all FE specs; sharding rejected per user feedback in Slice-02 resume).
- E2E selection — out per D6.
- Caching the resolved filter across commits in the same branch — out, regen on every push.
- Replacing the umbrella `[Category("Integration")]` with sub-categories only — keep both so existing `Category!=Integration` filters still work.

## Acceptance criteria

- AC-03B.1: All real-API connector test classes carry both the umbrella `[Category("Integration")]` and the connector-specific sub-category.
- AC-03B.2: `Scripts/test-selection/select-tests.sh` (and `.ps1`) emits the correct `--filter` string for the three canonical scenarios: touched-Jira-only, touched-base, touched-nothing-relevant. Verified by a self-contained test in `Scripts/test-selection/`.
- AC-03B.3: `dotnet test` (no args) on a clean checkout completes in ≤ 250 s with 0 throws (compare against the post-slice-pre baseline; tighten ceiling to ≤ 220 s after measuring 3 runs).
- AC-03B.4: The next 3 `main`-branch builds run all integration categories (forced `--full`) and stay green.
- AC-03B.5: The next 3 PR builds where no connector path was touched run zero integration tests AND save ≥ 200 s wall-clock vs the equivalent pre-slice baseline.
- AC-03B.6: Stryker.NET kill rate on every per-feature config remains ≥ 80 % under the new `test-case-filter`. Audit each `stryker-config*.json` before merge.
- AC-03B.7: `docs/ci-learnings.md` has a "Test selection rules" section that documents the resolver rules and how to opt in / out locally.
- AC-03B.8: ADO #5020 has a comment summarising the slice and the resolver rules.

## Dependencies

- slice-pre-integration-labelling **MUST** land first (the resolver assumes every real-API test carries `[Category("Integration")]`).
- slice-03A (CS-G) can land before, after, or in parallel — independent file.

## Reference class

Test infrastructure + GHA workflow change. Most similar in shape to the way `pnpm test` is wired in `ci_frontend.yml` — a wrapper-script layer between the developer's intent and the underlying test runner.

## Pre-slice SPIKE

Not required for the main mechanism (path filtering is a well-known pattern; `dorny/paths-filter` exists if we want a reference). The Stryker `test-case-filter` integration may need a brief verification — bundled into AC-03B.6.

## Taste tests

- Ship 4+ new components? Resolver script (1 pair), workflow edit, category sweep, Stryker sweep, docs update — **borderline**. The wrapper script + workflow + docs are a coherent system, not 4 unrelated artifacts; keep grouped.
- Depends on a new abstraction? **Yes — the resolver.** That abstraction is the slice's product; well-scoped, single responsibility.
- Disproves something? **Yes** — that integration tests must run on every PR.
- Synthetic data only? **No** — validated on real PR builds.
- Identical-except-for-scale duplicate of another slice? **No**.

Borderline on the component count; the slice is intentionally a single coherent system rather than 4 unrelated changes. Acceptable.

## Coverage invariant note (D5)

D5 ("at least every API call for each work-tracking system is verified") is preserved by the forced-full `main` cadence. The detection latency for a regression shifts from per-PR to per-merge — at most one merge of delay. Mitigation if too coarse: tighten to "include connector integration tests on any PR that has been open > 2 days without `main` rebase" (deferred follow-up; not in this slice).
