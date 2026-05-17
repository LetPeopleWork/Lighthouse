# Forecast-update portfolio RBAC scope fix (ADO bug 5021)

Author date: 2026-05-17 | ADO: https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5021 | Symptom: HTTP 500 when setting Team Feature WIP from a Portfolio quick-setting. Team-direct setting works.

## Wave: DISCUSS / [REF] Bug summary

| Field | Value |
|---|---|
| Reported | 2026-05-17 by Benj Huser (ADO 5021) |
| Severity / Priority | 3 - Medium / 2 |
| Reproduction | Open a portfolio detail page, click the Team Feature WIP quick-setting, set a value, save. Backend returns 500. The team WIP value DOES persist on the team because the team-update API call succeeded; only the followup forecast-refresh call 500'd. |
| Environment | `Authentication:Enabled=true`, `Authorization:Enabled=false` ("Auth on, RBAC off"). Bug is config-independent — see Root cause. |
| Tags | `Release Notes` |

## Wave: DISCUSS / [REF] Root cause

`Lighthouse.Backend/API/ForecastController.cs:25-32` declares:

```csharp
[HttpPost("update/{id:int}")]
[RbacGuard(RbacGuardRequirement.TeamWrite, ScopeIdRouteKey = "teamId")]
public ActionResult UpdateForecastForProject(int id)
```

Two independent defects on this attribute:

1. **Route-key mismatch.** The route declares `{id}`; `ScopeIdRouteKey = "teamId"` looks for a route value that does not exist. `RbacGuardAttribute.TryResolveScopeId` (`Lighthouse.Backend/Services/Implementation/Authorization/RbacGuardAttribute.cs:78-105`) returns false → `OnAuthorizationAsync` returns `StatusCodeResult(500)` at line 44. This is the 500 the user observes.

2. **Wrong scope.** `id` is the **portfolio** id, not a team id (`ForecastUpdater.Update` does `portfolioRepo.GetById(id)` at `Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/ForecastUpdater.cs:31`). `TeamWrite` is therefore semantically wrong even if the route key were corrected — the guard would authorize against team permissions on a portfolio id.

The team-direct path works because the team-detail page calls the OTHER endpoint `POST /forecast/update-portfolios-for-team/{teamId}` (`ForecastController.cs:34-47`), whose `ScopeIdRouteKey = "teamId"` correctly matches its `{teamId}` route segment.

The 500 fires **before** any RBAC enforcement, so the "Auth on, RBAC off" config does not save the call — `RbacGuardAttribute` always runs scope resolution when `IRbacAdministrationService` is registered (which is unconditional in `Program.cs:902`).

The user-visible symptom is misleading: the team WIP update API call (`PUT /api/latest/teams/{id}`) succeeds and persists; the immediate followup `POST /api/latest/forecast/update/{portfolioId}` from `PortfolioDetail.updateTeamSettingsFromPortfolio` (`Lighthouse.Frontend/src/pages/Portfolios/Detail/PortfolioDetail.tsx:268-282`) is what 500s.

## Wave: DISCUSS / [REF] Fix-direction decision

| Option | Decision | Reasoning |
|---|---|---|
| A. Fix the guard: `ScopeIdRouteKey = "id"`, `Requirement = RbacGuardRequirement.PortfolioWrite` | **CHOSEN** | Matches what the route + handler actually operate on. Single attribute edit. |
| B. Rename the route segment to `{teamId}` and keep `TeamWrite` | Rejected | The handler operates on portfolios; renaming the route still leaves the semantic guard wrong. Also breaks the existing frontend call site. |
| C. Remove the guard entirely on this endpoint | Rejected | Forecast refresh is a write operation that mutates persisted forecasts and triggers writebacks (`ForecastUpdater.Update:38-41`). Must remain authorization-gated. |
| D. Catch the 500 in `RbacGuardAttribute.TryResolveScopeId` and translate to 400/403 | Rejected | Treats the symptom (500 vs 4xx) not the cause (misconfigured attribute). The other guards in the codebase rely on the existing fail-fast behaviour for their own correctness. |

## Wave: DISCUSS / [REF] Post-fix contract

When `Authentication:Enabled=true`:

1. `POST /api/latest/forecast/update/{portfolioId}` against an existing portfolio → 200 OK, `forecastUpdater.TriggerUpdate(portfolioId)` enqueues the refresh. (Fixes the bug.)
2. `POST /api/latest/forecast/update/{portfolioId}` from a caller WITHOUT `PortfolioWrite` on that portfolio → `403 Forbid` (was 500). Behaviour matches every other `PortfolioWrite`-guarded portfolio endpoint.
3. `POST /api/latest/forecast/update-portfolios-for-team/{teamId}` — unchanged. Still `TeamWrite` scoped on `{teamId}`. Backwards-compat guard.
4. Setting team Feature WIP from the team-detail page — unchanged. Already worked.
5. Setting team Feature WIP from the portfolio-detail page — now succeeds end-to-end (team update + followup forecast refresh).

## Wave: DISTILL / [REF] Inherited commitments

| Origin | Commitment | DDD | Impact |
|--------|------------|-----|--------|
| `CLAUDE.md` (RBAC architecture rule) | All RBAC business logic flows through `IRbacAdministrationService` | n/a | Fix MUST keep the guard in place and scoped via `IRbacAdministrationService`; only the attribute parameters change. |
| `ForecastController.cs:36` (UpdateForecastsForTeamPortfolios) | The team→portfolios fan-out endpoint stays `TeamWrite + ScopeIdRouteKey = "teamId"` | n/a | Don't unify the two endpoints; they have legitimately different scopes (team vs portfolio). |
| `RbacGuardAttribute.cs:42-46` | Scope-resolution failure returns 500 fail-fast | n/a | Keep this behaviour. The right fix is to make scope resolution succeed for this endpoint, not to soften the failure mode. |

## Wave: DISTILL / [REF] Scenario list with tags

These regression tests are RED today against current code; they pin the post-fix contract.

| # | Scenario | Tags | Today |
|---|----------|------|-------|
| 1 | `POST /api/latest/forecast/update/{id}` does NOT return 500 (the user's reproduction) | `@regression @real-io @rbac-scope @adapter-integration @bug-5021` | RED — returns 500 |
| 2 | `POST /api/latest/forecast/update/{id}` returns 200 OK (no auth/RBAC configured in test factory; fix's happy path) | `@regression @real-io @rbac-scope @adapter-integration @bug-5021` | RED — returns 500 |
| 3 | `RbacGuardAttribute` on `ForecastController.UpdateForecastForProject` declares `Requirement = PortfolioWrite` and `ScopeIdRouteKey = "id"` (locks the corrected attribute contract) | `@regression @unit @attribute-shape @bug-5021` | RED — currently `TeamWrite` + `"teamId"` |
| 4 | `RbacGuardAttribute` on `ForecastController.UpdateForecastsForTeamPortfolios` STILL declares `TeamWrite` + `"teamId"` (guards against accidental over-correction) | `@regression @unit @attribute-shape @backwards-compat` | GREEN — existing test at `ForecastControllerTest.cs:35` |

Driving port for scenarios 1-2: the HTTP endpoint `POST /api/latest/forecast/update/{id}`. Scenarios 3-4 are attribute-shape contract tests at the type level — they protect against silent regressions of the guard parameters.

Why not also assert a 403 path under `WithTestAuthentication`? The bug is purely scope-resolution (pre-policy). The 500-vs-not-500 + 200-OK assertions in the default `IntegrationTestBase` already cover the failure mode end-to-end. A `WithTestAuthentication`-flavoured 403 path is in scope for a future RBAC-coverage sweep — out of scope here per port-to-port minimality.

## Wave: DISTILL / [REF] WS strategy

**Strategy C (real local).** Not a feature, so no walking skeleton scenario; this is a regression-only set. Real adapters all the way: `TestWebApplicationFactory<Program>` exercises the real ASP.NET Core authorization pipeline, the real `RbacGuardAttribute`, the real `RbacAdministrationService`, the real routing — exactly the layers where the bug lives. No InMemory doubles needed.

## Wave: DISTILL / [REF] Adapter coverage table

| Adapter / boundary | `@real-io` scenario | Covered by |
|---|---|---|
| ASP.NET Core routing (`{id:int}` constraint, route values) | YES | Scenarios 1, 2 (HTTP through `TestWebApplicationFactory.CreateClient()`) |
| `RbacGuardAttribute.OnAuthorizationAsync` (filter pipeline) | YES | Scenarios 1, 2 |
| `RbacGuardAttribute.TryResolveScopeId` | YES | Scenarios 1, 2 (resolution must succeed); Scenario 3 (attribute parameters) |
| `IRbacAdministrationService.CanSatisfyRequirementAsync` | TRANSITIVE | Scenario 2 (200 OK implies the policy decision was reached) |
| `IForecastUpdater.TriggerUpdate` | INDIRECT | Scenario 2 (200 OK means the guard let the handler run); unit test at `ForecastControllerTest.cs:69` already verifies the trigger |

No driven adapter is missing real-I/O coverage. The fix touches only the guard attribute parameters, so no production module needs scaffolding (Mandate 7 does not apply to bug fixes against existing production code).

## Wave: DISTILL / [REF] Scaffolds

None. Bug fix against existing production code in `ForecastController.cs:25-32`. Mandate 7 (RED-ready scaffolds for unimplemented production modules) does not apply.

## Wave: DISTILL / [REF] Test placement

- `Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/ForecastControllerAuthorizationTests.cs` — new file. Mirrors `ProjectsControllerAuthorizationTests.cs` / `TeamsControllerAuthorizationTests.cs` pattern in the same directory. Precedent for "controller-level HTTP-pipeline regression test in `API/Integration/`".
- `Lighthouse.Backend/Lighthouse.Backend.Tests/API/ForecastControllerTest.cs` — added one `[Test]` method `UpdateForecastForProject_HasPortfolioWriteRbacGuardAttributeScopedById`. Mirrors the existing attribute-shape test `UpdateForecastsForTeamPortfolios_HasTeamWriteRbacGuardAttribute` at line 35.

## Wave: DISTILL / [REF] Driving Adapter coverage

| Driving adapter | Scenario | Protocol |
|---|---|---|
| `POST /api/latest/forecast/update/{id}` (the buggy endpoint) | 1, 2 | HTTP through `HttpClient` against `TestWebApplicationFactory<Program>` |
| `[RbacGuard(...)]` attribute decoration on `UpdateForecastForProject` | 3 | Reflection on `MethodInfo.GetCustomAttributes` |

Every entry point that DESIGN would have identified for this fix has a regression scenario.

## Wave: DISTILL / [REF] Pre-requisites

- No DESIGN/DEVOPS prerequisites (bug fix, no infrastructure change).
- Test infrastructure: existing `IntegrationTestBase` + `TestWebApplicationFactory<Program>`. No new helpers.
- Database: SQLite via `IntegrationTestBase.Init` — clean DB per test, no seeding required (scenarios 1-2 use a non-existent portfolio id; the fire-and-forget `forecastUpdater.TriggerUpdate` returns 200 regardless of whether the portfolio exists, because the not-found branch is `return;` inside the background task at `ForecastUpdater.cs:32-35`).
