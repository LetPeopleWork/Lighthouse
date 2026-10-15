# Mutation testing — Bug #6306 (backend)

Stryker.NET 5.0.0, config `stryker.6306.backend.json`, run 2026-10-15 on frozen code (after refactor,
adversarial review and its revision pass).

**Scope (maintainer's choice):** `JiraRestRoutes.cs` and `JiraReleaseVersionReader.cs`, whole-file.
`JiraWorkTrackingConnector.cs` was deliberately left out. **Tests:** the Jira unit test classes only.
Every live-connector category (`JiraIntegration`, `AdoIntegration`, `LinearIntegration`,
`ServiceNowIntegration`, `GithubIntegration`, `Integration`) and the `API.Integration` acceptance suite were
excluded, so no mutant ever reached a real Jira. Each run took about 3 minutes.

| Run | Score | `JiraReleaseVersionReader.cs` | `JiraRestRoutes.cs` |
|---|---|---|---|
| 1 | 93.07 % | 76 killed + 2 timeout / 82 = 95.1 % | 16 / 19 = 84.2 % |
| 2 (after `d0c2d1521`) | **94.06 %** | 95.1 % (unchanged) | **17 / 19 = 89.5 %** |

Gate: ≥ 80 % per file. **Passed.**

## Survivors

**Killed between runs.** `JiraRestRoutes.cs:88`: Cloud's `FallsBackToProjectListWhenSearchIsMissing`
mutated `false → true`. Nothing made a Cloud Jira answer 404 to its project search. Commit `d0c2d1521` adds
`A_Cloud_Jira_that_lost_its_project_search_offers_nothing_and_does_not_fall_back_to_the_project_list`.
Applying the mutant by hand fails that test, because a request to `/rest/api/2/project` appears.

**Equivalent:**

- `JiraRestRoutes.cs:90` survived: Cloud's `CanFilterVersionsByStatus` mutated `true → false`. The only
  effect is that Lighthouse filters by status a page Jira Cloud has already filtered by the same status, so
  the result cannot change.
- `JiraRestRoutes.cs:106`, no coverage: Data Center's `FallsBackToProjectListWhenSearchIsMissing` mutated
  `false → true`. It is never read, because Data Center's `PagedProjectSearch` is `null` and the project
  list is read without trying a search at all.

**Older code, not part of this change:** `JiraReleaseVersionReader.cs:106, 113, 132, 142`, all four in
`ReadRefusalMessage`. That method turns a refused publish into an operator message and dates from the
original publish work in August, so it is recorded here and not chased.

**Timeouts, counted as detected:** `JiraReleaseVersionReader.cs:171` (two mutants on the `isLast` check).
Both make a paged read never end, which the test harness detects as a timeout.
