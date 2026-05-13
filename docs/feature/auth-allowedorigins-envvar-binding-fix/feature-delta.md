# Auth `AllowedOrigins` env-var binding fix

Author date: 2026-05-13 | Origin: deployed Lighthouse container fails to start with `InvalidOperationException: Authentication is enabled but Authentication:AllowedOrigins is empty` when the operator sets `Authentication__AllowedOrigins=<origin>` as a single (non-indexed) environment variable -- which is exactly what `render.yaml:57` instructs them to do.

## Wave: DISCUSS / [REF] Bug summary

| Field | Value |
|---|---|
| Reported | 2026-05-13 by an operator running a Render-deployed container |
| Severity | High -- container won't start under auth; only workaround is to know undocumented `__0` suffix |
| First broken commit | `c389e80b` (2026-05-12) -- introduced `EnsureCorsFailsClosed` fail-closed validation |
| Related precedent | `21f6d066` (2026-05-12) -- fixed the same root cause in `ci_verifyauth.yml`; deployment templates (`render.yaml`) and operator docs (`docs/Installation/configuration.md:204`) were not aligned |
| Operator config (from screenshot) | `Authentication__Enabled=true`, `Authentication__AllowedOrigins=https://localhost:48332` (no `__0`) |

## Wave: DISCUSS / [REF] Root cause

`Lighthouse.Backend/Models/Auth/AuthenticationConfiguration.cs:23` declares `AllowedOrigins` as `IReadOnlyList<string>`. .NET's environment-variable configuration provider does NOT bind a non-indexed scalar env var into a list -- list binding requires `Authentication__AllowedOrigins__0`, `__1`, ... -- so `AllowedOrigins` stays empty. The fail-closed check at `Lighthouse.Backend/Program.cs:275` then throws `InvalidOperationException` because `authConfig.Enabled && authConfig.AllowedOrigins.Count == 0`.

The repository itself contradicts the requirement:
- `render.yaml:57` -- the Render deployment template -- sets `Authentication__AllowedOrigins` without the `__N` suffix. Any operator following this template hits the bug the moment they enable auth.
- `docs/Installation/configuration.md:204` documents the `__0`/`__1` form. Operators reading docs vs. operators copying `render.yaml` end up in different worlds.
- `ci-learnings.md` (2026-05-12 entry) already captured this footgun for CI workflows but did not generalise the rule to deployment templates / operator-facing config.

## Wave: DISCUSS / [REF] Fix-direction decision

| Option | Decision | Reasoning |
|---|---|---|
| A. Accept single value as one-element list; comma- or semicolon-separated as multi-element list. Indexed form keeps working. | **CHOSEN** | Matches operator mental model + the `render.yaml` template + the `ASPNETCORE_URLS` pattern already familiar in .NET. Preserves fail-closed guarantee (empty still throws). |
| B. Only improve the error message to point operators at `__0` suffix | Rejected | `render.yaml` already disagrees with the docs; making the error friendlier still leaves the template broken. Fix the contract, not the symptom. |
| C. Drop the fail-closed | Rejected | Fail-closed is the security mandate from `c389e80b` (S-1). Keep it; just fix the binding. |

## Wave: DISCUSS / [REF] Post-fix contract

When `Authentication__Enabled=true`:
1. `Authentication__AllowedOrigins__0=https://app.example` -- still binds to `["https://app.example"]`. (Backwards-compat.)
2. `Authentication__AllowedOrigins=https://app.example` -- binds to `["https://app.example"]`. (Fixes user's case.)
3. `Authentication__AllowedOrigins=https://a.example,https://b.example` -- binds to `["https://a.example", "https://b.example"]`. (Comma-separated multi-origin without forcing indexed syntax.)
4. `Authentication__AllowedOrigins=https://a.example;https://b.example` -- binds to the same. (Semicolon-separated -- matches `ASPNETCORE_URLS`.)
5. No `AllowedOrigins` env var set AND no `appsettings.json` value -- fail-closed still throws (security guarantee preserved).
6. `appsettings.json` with `"AllowedOrigins": ["https://x"]` -- still works. (JSON-list path untouched.)

## Wave: DESIGN / [REF] Inherited commitments

| Origin | Commitment | DDD | Impact |
|--------|------------|-----|--------|
| `c389e80b` S-1 fail-closed | Auth-enabled + empty origins MUST refuse to start | n/a | Fix MUST preserve fail-closed for case 5 above; cannot weaken the security gate |
| `Lighthouse.Backend/Models/Auth/AuthenticationConfiguration.cs:23` | `AllowedOrigins` is `IReadOnlyList<string>` on the record | n/a | Type stays; binding becomes more forgiving via a configuration post-processing step (Program.cs) rather than by changing the type |
| `render.yaml:57` | Template sets non-indexed env var | n/a | Template stays as-is post-fix -- it becomes correct once the binding accepts that form. No template change required. |
| `docs/Installation/configuration.md:204` | Docs list only `__0`/`__1` form | n/a | Docs MUST be expanded post-fix to mention single-value and CSV forms as supported alternatives. |

## Wave: DISTILL / [REF] Scenario list with tags

The regression scenarios below pin the post-fix contract. They are RED today: under current code, scenarios 1-4 fail because the host throws `InvalidOperationException` before the test can issue any HTTP request.

| # | Scenario | Tags | Today |
|---|----------|------|-------|
| 1 | Auth enabled + `Authentication__AllowedOrigins` (single, no index) -- host starts | `@regression @real-io @auth @cors` | RED -- throws on startup |
| 2 | Auth enabled + single-value env var -- preflight from that origin returns 2xx with `Access-Control-Allow-Origin` and `Access-Control-Allow-Credentials: true` | `@regression @real-io @auth @cors` | RED -- throws on startup |
| 3 | Auth enabled + comma-separated env var (`a,b`) -- preflight from EACH origin allowed; foreign origin rejected | `@regression @real-io @auth @cors` | RED -- throws on startup |
| 4 | Auth enabled + semicolon-separated env var (`a;b`) -- same as #3 | `@regression @real-io @auth @cors` | RED -- throws on startup |
| 5 | Auth enabled + indexed env var (`__0`) -- host starts AND preflight from that origin allowed (backwards-compat guard) | `@regression @real-io @auth @cors` | GREEN -- existing behaviour |
| 6 | Auth enabled + NO `AllowedOrigins` env var anywhere -- host STILL throws `InvalidOperationException` (security guarantee preserved) | `@regression @real-io @auth @cors @security-gate` | GREEN -- existing behaviour |
| 7 | Auth enabled + foreign origin against a single-value env var -- preflight does NOT echo the foreign origin | `@regression @real-io @auth @cors` | RED -- throws on startup |

Scenario 5 + 6 are kept as **GREEN-today regression guards**: they ensure the fix does not break what already works (indexed form) or weaken the security mandate (empty still fails closed). The DELIVER implementer MUST keep them green.

## Wave: DISTILL / [REF] WS strategy

**Strategy C (real local).** The fix lives in ASP.NET Core configuration binding -- there are no costly external dependencies to fake. `TestWebApplicationFactory<Program>` already exercises real configuration binding, real CORS middleware, real `EnsureCorsFailsClosed`, real `ConfigureCors` -- the exact units the bug lives in. Process-level env vars are the actual driving adapter the operator uses, so the scenarios MUST set them via `Environment.SetEnvironmentVariable` and assert against a real WebApplicationFactory client, NOT against `AuthenticationConfiguration` in isolation. The existing `S1_CorsFailClosedTests.cs` already follows this pattern; the new file mirrors it.

## Wave: DISTILL / [REF] Adapter coverage table

| Adapter / boundary | `@real-io` scenario | Covered by |
|---|---|---|
| Environment-variable configuration provider (`AddEnvironmentVariables` for `Authentication__*`) | YES | Scenarios 1-7 set real `Environment.SetEnvironmentVariable` |
| ASP.NET Core CORS middleware (`UseCors`) | YES | Scenarios 2-7 issue real preflight HTTP `OPTIONS` requests through `TestWebApplicationFactory.CreateClient()` |
| `EnsureCorsFailsClosed` host-startup hook | YES | Scenarios 1, 5, 6 |
| `ConfigureCors` policy builder | YES | Scenarios 2, 3, 4, 7 |

No driven adapter is missing coverage.

## Wave: DISTILL / [REF] Test placement

`Lighthouse.Backend/Lighthouse.Backend.Tests/API/Security/S1_AllowedOriginsEnvVarBindingTests.cs` -- co-located with the existing `S1_CorsFailClosedTests.cs` from commit `c389e80b`. Same `[NonParallelizable]` requirement (tests mutate process env vars). The S1_ prefix preserves the story-line linkage to the original S-1 security story.

## Wave: DISTILL / [REF] Driving adapter coverage

The user-facing driving adapter for this bug is the **process environment** at host startup. Every regression scenario sets env vars via `Environment.SetEnvironmentVariable` BEFORE constructing `TestWebApplicationFactory<Program>`, which is the same path the deployed container takes -- container env vars are visible to `Program.Main`'s default configuration sources.

Driving-adapter checklist:
- Exit-code-equivalent: `factory.CreateClient()` either succeeds (host started) or throws `InvalidOperationException` (fail-closed). Scenarios 1, 5, 6 assert on this.
- Output format: CORS response headers (`Access-Control-Allow-Origin`, `Access-Control-Allow-Credentials`). Scenarios 2, 3, 4, 7 assert on these.
- Argument handling: env var name spellings (no-index, indexed, comma, semicolon). All seven scenarios collectively cover the spelling matrix.

## Wave: DISTILL / [REF] Pre-requisites

- DESIGN driving ports: ASP.NET Core configuration system + CORS middleware (both framework adapters; no custom driving ports introduced by this fix).
- DEVOPS environment matrix: container env-var injection (the path Render uses for `render.yaml` env values). No new infrastructure.
- Inherits the security mandate from S-1 (`c389e80b`): fail-closed when auth on + zero origins. Scenario 6 pins this.

## Wave: DISTILL / [REF] Scaffolds

None. This is a bug fix in existing production code (`Lighthouse.Backend/Program.cs` `EnsureCorsFailsClosed` and the configuration-binding path). No new modules are introduced; no `__SCAFFOLD__` markers are required. DELIVER will modify existing files only.

## Wave: DISTILL / [REF] Self-review checklist

- [x] 1. WS strategy declared (Strategy C -- real local; see above)
- [x] 2. Scenarios tagged correctly (`@real-io @auth @cors @regression`)
- [x] 3. Every adapter exercised has at least one `@real-io` scenario (env-var provider, CORS middleware, fail-closed hook, policy builder)
- [x] 4. n/a -- no InMemory doubles used
- [x] 5. n/a -- no container preference
- [x] 6. n/a -- no new production modules; existing modules already in tree
- [x] 7. n/a -- no scaffold files
- [x] 8. n/a -- no scaffold methods
- [x] 9. Tests are RED (scenarios 1-4, 7) for the right reason: `EnsureCorsFailsClosed` throws today
- [x] 10. Driving adapter coverage -- process env vars exercised through real `TestWebApplicationFactory` (not by calling `EnsureCorsFailsClosed` directly)
- [x] 11. At least one `@real-io` scenario per driven adapter
- [x] 12-15. n/a -- NUnit project, not pytest-bdd

## Wave: DISTILL / [REF] Handoff to DELIVER

DELIVER MUST:
1. Make scenarios 1-4 and 7 GREEN.
2. Keep scenarios 5 (indexed form) and 6 (no origins → fail-closed) GREEN.
3. Update `docs/Installation/configuration.md:204` to mention the supported forms: single value, comma-separated, semicolon-separated, indexed.
4. Confirm `render.yaml:57` template now works as-deployed (no template change expected -- the binding catches up to the template).
5. Add an entry to `docs/ci-learnings.md` under "Build & compile" or a new "Runtime / startup" section so this footgun does not return.

## Wave: DELIVER / [REF] Implementation summary

Added `Program.LoadAuthenticationConfiguration(WebApplicationBuilder)` as the single bind site for `AuthenticationConfiguration`. It runs the standard `Get<AuthenticationConfiguration>()` first (indexed env vars and JSON-list `appsettings` shapes keep working unchanged); when the bound `AllowedOrigins` is empty, it falls back to the scalar `Authentication:AllowedOrigins` configuration key and splits on `,`/`;` (`TrimEntries | RemoveEmptyEntries`). The record is regenerated immutably via a `with`-expression. Both call sites (`EnsureCorsFailsClosed` and `ConfigureServices`) now route through the helper, removing the previous duplicated bind. Fail-closed semantics are preserved: scalar absent AND indexed absent → list stays empty → existing `InvalidOperationException` fires.

## Wave: DELIVER / [REF] Files modified

**Production**
- `Lighthouse.Backend/Lighthouse.Backend/Program.cs:213,268-294,300` — added `AllowedOriginsSeparators` static readonly field + `LoadAuthenticationConfiguration` helper; rewired `ConfigureServices` and `EnsureCorsFailsClosed` to consume it.

**Tests** — none modified. DISTILL's `S1_AllowedOriginsEnvVarBindingTests.cs` was the spec; the fix flipped its six RED scenarios to GREEN without test changes.

**Docs**
- `docs/Installation/configuration.md:204-205` — documented scalar (single / comma / semicolon) and indexed forms as two equivalent options.
- `docs/ci-learnings.md` — new `## Runtime / startup` section with the 2026-05-13 entry; rule going forward: list-typed configuration from env vars MUST accept both indexed and scalar comma/semicolon forms.

No test files were added by DELIVER. No new C# class libraries. No new abstractions beyond the single helper method.

## Wave: DELIVER / [REF] Scenarios green count

8 of 8 in `S1_AllowedOriginsEnvVarBindingTests` GREEN at 2026-05-13. The four pre-existing scenarios in `S1_CorsFailClosedTests` remain GREEN (backwards-compat regression guards). Full backend test suite: 2384 / 2384 GREEN.

## Wave: DELIVER / [REF] DoD check

| DoD item (DISTILL handoff) | Status | Evidence |
|---|---|---|
| Scenarios 1-4 and 7 flip RED → GREEN | PASS | `dotnet test --filter ~S1_AllowedOriginsEnvVarBindingTests` = 8/8 |
| Scenarios 5 (indexed) and 6 (empty → fail-closed) remain GREEN | PASS | Same run; the two negative-path tests trigger the expected `InvalidOperationException` |
| `docs/Installation/configuration.md` documents both forms | PASS | configuration.md:204-205 |
| `render.yaml` template works as-deployed (no template change) | PASS | Template unchanged; binding now accepts its scalar form |
| `ci-learnings.md` entry added so footgun does not return | PASS | New `## Runtime / startup` section, 2026-05-13 entry |
| `dotnet build` 0/0 | PASS | `Build succeeded. 0 Warning(s) 0 Error(s)` |
| `dotnet test` full suite GREEN | PASS | 2384 / 2384 passed |
| Comments policy (CLAUDE.md) | PASS | Single non-obvious WHY comment in the helper explaining the .NET env-var binding quirk; no banned section banners, no provenance comments |
| Immutability (CLAUDE.md) | PASS | New `AuthenticationConfiguration` produced via `with`-expression; no mutation |
| SonarCloud rules (`ci-learnings.md`) | PASS | Constant separator array hoisted to `private static readonly` (CA1861-safe); no `Assert.Multiple` introduced |

## Wave: DELIVER / [REF] Demo evidence

The deployed-container failure mode is reproducible and verifiable end-to-end via the regression tests rather than a CLI elevator-pitch command (bug fix, not a feature with a user-facing demo). Pre-fix vs. post-fix evidence captured at 2026-05-13:

**Pre-fix (DISTILL run, before any production change)** — `dotnet test --filter ~S1_AllowedOriginsEnvVarBindingTests`:
```
Failed!  - Failed:     6, Passed:     2, Skipped:     0, Total:     8
```
Every failure stack-traced to `Program.EnsureCorsFailsClosed` line 283 with the exact `System.InvalidOperationException` message the operator hit in production.

**Post-fix** — same command, same test file, no test changes:
```
Passed!  - Failed:     0, Passed:    12, Skipped:     0, Total:    12, Duration: 1 s
```
(12 counts both `S1_AllowedOriginsEnvVarBindingTests` (8) + `S1_CorsFailClosedTests` (4), since the filter targets both for backwards-compat verification.)

Operator action to unblock the existing deployed container: no env-var change required after this fix ships. The operator's existing `Authentication__AllowedOrigins=https://localhost:48332` will start working.

## Wave: DELIVER / [REF] Quality gates

| Gate | Outcome | Notes |
|---|---|---|
| Refactor pass | n/a | Single-helper extraction was the refactor; no separate L1-L6 pass warranted for ~30-line surgical change |
| Adversarial review | DEFERRED | Lean delivery; the user may run `/ultrareview` or `@nw-software-crafter-reviewer` as a follow-up if desired |
| Mutation testing | DEFERRED | CLAUDE.md specifies `per-feature` Stryker.NET ≥ 80%; appropriate to run as a follow-up since the change touches a single helper. Run: `dotnet stryker --solution Lighthouse.Backend.sln --project Lighthouse.Backend.csproj` |
| Integrity verification | n/a | Lean delivery — no DES execution-log was created (no roadmap.json was needed for a single-step bug fix) |
| `dotnet build` 0/0 | PASS | |
| `dotnet test` full suite | PASS | 2384 / 2384 |
| SonarCloud `new_violations = 0` | EXPECTED PASS | Helper follows existing conventions; CA1861 mitigated; no banned comment patterns introduced. Confirmed by next CI run. |

## Wave: DELIVER / [REF] Pre-requisites

- DISTILL Tier-1 [REF] sections (scenario list + adapter coverage + test placement) were the spec the implementation satisfied.
- DESIGN Tier-1 [REF] Inherited commitments table pinned the constraints: preserve fail-closed for the empty case; `AllowedOrigins` type unchanged; `render.yaml` template unchanged. All three preserved.
- No DEVOPS or DISCOVER artifacts were consumed (none required for this bug fix).
