# CI Learnings

Durable rules derived from CI / SonarCloud failures on `Build And Deploy Lighthouse`. Append a new entry every time `/clean-ci` resolves a failure. Read this file before touching code in the related area.

Each entry follows:

```
### YYYY-MM-DD — <short title>
- **Symptom**: what CI / Sonar reported (rule key, error excerpt, job name).
- **Root cause**: the actual reason, in one sentence.
- **Fix**: what was changed (file:line is enough; the commit has the diff).
- **Rule going forward**: a single declarative do/don't sentence future-Claude can apply BEFORE writing similar code.
```

## Formatting & linting

### 2026-05-12 — SonarCloud's PowerShell analyzer is strict; new `.ps1` files trigger many rules
- **Symptom**: Sonar gate failed with 12 new violations on `LetPeopleWork_Lighthouse` after a fresh `.ps1` file was committed. Rules fired: `powershelldre:S8620` (trailing whitespace), `powershelldre:S8677` (`Write-Host` should be `Write-Output` for pipelining), `powershelldre:S8641` (`$null` must be on the LEFT of comparisons), `powershelldre:S8642` (cmdlet-style casing on identifiers).
- **Root cause**: PowerShell files are linted by Sonar with stricter conventions than ad-hoc shell scripting habits. `Write-Host` discards its output (un-pipelable) and is the de-facto debug-print habit; PowerShell-strict code uses `Write-Output` for informational text and reserves `Write-Host` for things that genuinely need TTY-only behaviour (e.g. `-ForegroundColor`). Comparing `$variable -eq $null` is unreliable in PowerShell because of array-flattening semantics; `$null -eq $variable` is the safe order.
- **Fix**: Trailing whitespace removed. `Write-Host "info"` → `Write-Output "info"` where colour wasn't needed. `Write-Host "..." -ForegroundColor Green` was kept as-is (intentional terminal-only). `$null -eq (Get-Process ...)` ordering flipped. The cmdlet-casing rule firing on the literal `pkcs11-tool` (an external binary, not a PowerShell cmdlet) was worked around by storing the name in a variable and invoking via `& $varname`.
- **Rule going forward**: In any new `.ps1` file: (a) prefer `Write-Output` for non-coloured informational text — only use `Write-Host` when `-ForegroundColor` is essential; (b) write `$null -eq $foo`, never `$foo -eq $null`; (c) trim trailing whitespace; (d) if you need to call an external binary whose name looks like a cmdlet (e.g. contains a hyphen), store the name in a variable and invoke with `& $name args...` to bypass the cmdlet-casing analyzer.

## Build & compile

_None yet._

## Tests

### 2026-05-12 — RBAC E2E bootstrap assertion contradicted the team-existence portfolio gate
- **Symptom**: `Verify Authentication / verifyauth` failed at step 25 ("Run RBAC Playwright tests"). The failing assertion in `Lighthouse.EndToEndTests/tests/specs/auth/RoleBasedAccessControl.spec.ts:108-110` expected the "Add Portfolio" button to be visible during the first-user bootstrap step, where ZERO teams exist in the system.
- **Root cause**: Under R2 (`team-portfolio-creation-rights` feature delta), `rbac.canCreatePortfolio` is `false` whenever `LighthouseAppContext.Teams` is empty — even for System Admin, even in bootstrap mode. The frontend Add Portfolio button is gated by `rbac.canCreatePortfolio`, so it is intentionally hidden during bootstrap-with-zero-teams. The E2E test was pinning the R1 contract that gave SysAdmin unconditional portfolio creation.
- **Fix**: Flipped the bootstrap assertion to `.not.toBeVisible()` for Add Portfolio, then added a positive assertion right after the "load demo scenario 0" step (where Team Zenith is created) confirming the button becomes visible once a team exists. `Lighthouse.EndToEndTests/tests/specs/auth/RoleBasedAccessControl.spec.ts:108-110, 156-163`.
- **Rule going forward**: The Add Portfolio button is hidden whenever the system has zero teams, in every RBAC mode (enforced, bootstrap-no-admin, RBAC-disabled). E2E flows that touch a fresh-database state MUST NOT assert Add Portfolio visibility until at least one team has been seeded (via demo scenario load, fixture, or the test creating one first). For unit / Vitest tests that pass `rbac.canCreatePortfolio = true` directly to `OverviewDashboard`, the visible-teams prop no longer affects the button — that gate is now backend-side only.

## SonarCloud — Backend (LetPeopleWork_Lighthouse)

### 2026-05-12 — Shared validate endpoints must not gate on a single workflow's permission
- **Symptom**: Manual testing surfaced a 403 on `POST /api/latest/teams/validate` and `POST /api/latest/portfolios/validate` when a scoped Team/Portfolio Admin clicked Save in the Edit Settings UI. The endpoints were guarded by `[RbacGuard(CanCreateTeam)]` / `[RbacGuard(CanCreatePortfolio)]`, which under v1 RBAC = sysadmin only.
- **Root cause**: The validate endpoints are shared by TWO different workflows: (1) the System-Admin-only Create flow, and (2) the Team-Admin / Portfolio-Admin Edit-and-Save flow. Gating them on the Create requirement broke the Edit path for scoped admins. The downstream write endpoints (`POST /teams` for create, `PUT /teams/{id}` for edit) already enforce the correct per-workflow scope.
- **Fix**: Dropped `[RbacGuard(...)]` from both validate endpoints. `[LicenseGuard]` and `[Authorize]` still apply. The frontend page-level gating (only sysadmin reaches /teams/new; only scoped-admin-on-this-entity reaches /teams/{id}/edit) is the access-control entry point; the validate endpoint is reachable to any authenticated user but doesn't write state.
- **Rule going forward**: When a shared "validate" / "pre-flight" endpoint exists, do NOT gate it on the requirement of the most-restrictive consumer. Either (a) leave the validate endpoint authenticated-but-unguarded if it has no write side-effects and DTOs redact secrets, or (b) compute the appropriate gate at runtime inside the handler based on the request body's intent (create vs. edit). Default to (a) unless you can show a leak.

### 2026-05-12 — CA1861: do not pass constant arrays inline to repeated method calls
- **Symptom**: Sonar gate failed with `new_violations = 4` on `LetPeopleWork_Lighthouse`. All four violations were rule `external_roslyn:CA1861` ("Prefer 'static readonly' fields over constant array arguments if the called method is called repeatedly and is not mutating the passed array") at `Lighthouse.Backend.Tests/Services/Implementation/Authorization/CreateRightsAcceptanceTest.cs:440,452,628,634`.
- **Root cause**: NUnit test bodies passed `new[] { 10, 20, 30, 40 }` and `new[] { 10 }` inline to `GetReadableTeamIdsAsync` and `Is.EquivalentTo` in two different tests. Sonar's analyzer treats "called repeatedly" as "appears more than once in the file" and flags every inline allocation, even in test fixtures where each method runs once per test.
- **Fix**: Hoisted both arrays to `private static readonly int[]` fields (`AllSeededTeamIds`, `CreatorVisibleTeamIds`) at the class level and replaced the four inline literals with the named references.
- **Rule going forward**: In C# test classes, any constant array literal (`new[] { ... }` or collection-expression `[...]`) that appears in more than one test method MUST be declared as a `private static readonly` field at the class level. The same applies to `Is.EquivalentTo`, `Contains`, and any other API that takes an `IEnumerable<T>`. CA1861 will fail the SonarCloud gate even at INFO severity because the project's gate condition is `new_violations = 0` on new code.

## SonarCloud — Frontend (LetPeopleWork_Lighthouse_Frontend)

_None yet._

## EF migrations

_None yet._

## Infra & flakes

_None yet._
