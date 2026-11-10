# Mutation testing — 6577 (Viewer and Admin for all Teams and Portfolios, Everyone signed in)

Run 2026-11-10 against `main`. Gate is 80 % kill rate on both stacks, measured on the lines this feature changed.

| stack | first run | after kill round | tested (changed lines) | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 4.16.0, x64 SDK) | 71.69 % | **95.78 %** | 166 | 159 | 7 | 0 | 21 m 27 s + 37 m 13 s |
| Frontend (StrykerJS, vitest runner) | 66.24 % | **89.81 %** | 314 | 282 | 30 | 2 | 14 m 21 s + 13 m 18 s |

Configs: `stryker.6577.backend.json` (full run), `stryker.6577.backend.kill.json` (narrowed kill round),
`stryker.6577.frontend.json`, `vitest.stryker.6577.ts` (copy it to `Lighthouse.Frontend/` before a run, and reference
it by bare file name).

## Why the backend gate is on changed lines

Stryker.NET mutates whole files and ignores line ranges. Two of the nine files are mostly older code:
`RbacAdministrationService.cs` (1477 lines, 401 changed) and `AuthorizationController.cs` (574 lines, 98 changed).
Whole-file scores (67.81 % first run) bury the feature under endpoints it never touched, so each mutant was mapped
to the feature's diff (`git diff -U0 8cc82854c..HEAD`) and only those on changed lines are scored here. The
frontend config already mutates changed-line ranges only.

The narrowed backend kill round first left `RbacAdministrationServiceTest` out of its filter, which showed eight
mutants on six lines (EffectiveAccess 211, 212, 226; RbacAdministrationService 1174) as unkilled. The first run's
report shows all eight killed by unchanged tests on unchanged production code, so they count as killed; the kill
config now includes that class.

## Backend — changed lines per file

| File | first run (killed/survived/no cov) | after |
| --- | --- | --- |
| `API/AuthorizationController.cs` | 0/4/13 | 17/0/0 |
| `Models/Authorization/AppliesTo.cs` | 7/1/0 | 8/0/0 |
| `Models/Authorization/EffectiveAccess.cs` | 56/6/5 | 64/3/0 |
| `Models/Authorization/EveryoneSignedInSetting.cs` | 1/0/0 | 1/0/0 (not re-run) |
| `Models/Authorization/PermissionRule.cs` | 2/1/0 | 3/0/0 |
| `Models/Authorization/SystemWideRule.cs` | 2/2/0 | 3/1/0 |
| `Services/Implementation/Authorization/RbacAdministrationService.cs` | 47/11/2 | 59/1/0 |
| `Services/Implementation/Seeding/EveryoneSignedInSeeder.cs` | 4/2/0 | 4/2/0 (not re-run) |

### Closed by this pass

- The new controller endpoints, unit-tested with a mocked service: the audit lines for adding and removing a rule
  (the right line by id, and a twin the list does not show) and for turning the switch; Ok and 404 with its message
  for a Team's and a Portfolio's rules; Forbid with no log line for a caller who is not a System Admin; BadRequest
  with the service's message.
- `EffectiveAccess`: a role that does not belong to the unit type reads nothing; no access on a scope that is not a
  Team or Portfolio; the highest System row decides in either order; a System Admin's key keeps its rows; a key keeps
  only the unit rows its owner covers.
- `PermissionRule.HasSameScopeAs` and `SystemWideRule.TryReading`, each with its own small test class.
- At the port: saving a rule with the role it already has; the order of the rules list (group, scope, unit, role)
  and of a unit's rules (group, role); which twin is listed; that a unit exists only when stored; that the first
  stored switch row decides.

### Accepted survivors

| Where | Mutation | Why it cannot be killed meaningfully |
| --- | --- | --- |
| `EffectiveAccess.cs:127` | `>` to `>=` in `Higher` | A tie is the same role, or Team Admin against Portfolio Admin; the System role is only read through `IsSystemAdmin`, false for both. Equivalent. |
| `EffectiveAccess.cs:159` | `\|\|` to `&&` in `Reach.Administers` | `AppliesTo` has only the all-units value, so a reach covers Teams and Portfolios together. Equivalent. |
| `EffectiveAccess.cs:234` | `>` to `>=` in `HigherOn` | Tied roles give the same read and write on a unit; the difference would only show through `Covers`, see "Possible latent defect" below. |
| `SystemWideRule.cs:17` | always builds the rule | The out value is unspecified on false and the only caller reads it on true. Equivalent. |
| `RbacAdministrationService.cs:927` | `&&` to `\|\|` in the all-units pre-filter | `SystemWideRule.TryReading` drops every other row anyway; the filter only saves rows read. Equivalent. |
| `EveryoneSignedInSeeder.cs:23` (2) | statement and string of a log line | A diagnostic line at start-up, not an audit record. |

### Possible latent defect (older than this feature)

`EffectiveAccess.Covers` compares the owner's and the key's roles by their general priority, where unit reads use
the priority on that unit. A key limited to Viewer on a Team would pass for an owner whose only row on that Team is
Portfolio Admin (or System Admin stored on a Team). Every write path refuses such a row, so it takes one written by
hand. Not fixed here; raised for the maintainer.

### Not mutated

`EveryoneSignedInRequest.cs` (a request record), `UserRole.cs` and `PermissionScopeType.cs` (one enum value each),
`UsageDataEventName.cs` (an enum value), `IRbacAdministrationService.cs` (interface members),
`LighthouseAppContext.cs` (two `DbSet`s and their mapping), `Program.cs` (one registration line) and the migrations:
no logic to mutate.

## Frontend — per file (changed lines only)

| File | first run (killed/survived/no cov) | after |
| --- | --- | --- |
| `components/Common/Authorization/ScopedGroupMappingManager.tsx` | 17/5/0 | 19/3/0 |
| `components/Common/Authorization/SystemWideRulesNotice.tsx` | 28/5/0 | 28/5/0 |
| `models/Authorization/RbacModels.ts` | 15/0/0 | 15/0/0 |
| `pages/Portfolios/Detail/PortfolioDetail.tsx` | 7/3/0 | 9/1/0 |
| `pages/Settings/Rbac/EveryoneSignedInSection.tsx` | 41/21/1 | 54/9/0 |
| `pages/Settings/Rbac/RbacSettings.tsx` | 93/38/18 | 136/11/2 |
| `pages/Teams/Detail/TeamDetail.tsx` | 7/3/0 | 9/1/0 |
| `services/Api/RbacService.ts` | 0/0/12 | 12/0/0 |

The changed-line ranges were merged across small unchanged gaps (for example `RbacSettings.tsx:28-136`), which
widens the scope slightly and makes the score conservative.

### Closed by this pass

- `RbacService`: the four new methods (a Team's and a Portfolio's rules, reading and setting the switch), with URL
  and payload.
- Settings → Access: no user rows while the status is loading; the load failure message; user search by name or
  e-mail ignoring case and surrounding spaces; revoking the last System Admin; each failed save's message; the group
  value field starting empty, the required message, and the field clearing after an add; a double Confirm deleting
  once.
- Everyone signed in: Escape closes the question without saving; the On copy; the switch disabled while it saves;
  a failure that is not a conflict shows its own message and clears after a later save.
- The rules list leaves out unit-scoped rows and System Admin groups; no spinner or alert once loaded.
- The unit Access tabs offer a System Admin exactly the unit's admin role and Viewer; the editable list's headers.

### Accepted survivors

| Where | Why |
| --- | --- |
| MUI `sx` and layout objects (EveryoneSignedInSection 87, 149, 150, 154; RbacSettings 421, 439, 457; ScopedGroupMappingManager 122; SystemWideRulesNotice 55) | jsdom computes no styles; a test would assert the style object itself, a copy of the source. |
| TanStack query keys (EveryoneSignedInSection 45; SystemWideRulesNotice 30) | The key stays unique and nothing else reads it. |
| React list key template (SystemWideRulesNotice 76) | No observable change. |
| Initial `groupMappings` state and `setGroupMappings([])` without a System Admin (RbacSettings 95, 131) | The table renders only after the rules load, and the section is hidden without a System Admin. Equivalent. |
| `useCallback` dependency lists (RbacSettings 115, 136) | The service from context is stable. Equivalent. |
| Fallback text for a user with neither name nor e-mail (RbacSettings 237, 238) | Only a search for the placeholder text itself would see it. |
| `ScopedGroupMappingManager.tsx:185` (`true`) | Survives although the read-only test pins the headers; not explained. Low risk, left for a later look. |
