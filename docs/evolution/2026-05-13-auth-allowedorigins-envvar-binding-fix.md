# Evolution: auth-allowedorigins-envvar-binding-fix

**Finalized**: 2026-05-13
**Wave path**: DISTILL → DELIVER (lean; bug-fix scope, DISCUSS/DESIGN/DEVOPS sections inlined into the feature delta)
**Outcome**: One production file modified, regression test pre-existed (DISTILL output), 6 RED scenarios → GREEN, 2 GREEN regression guards preserved

## Summary

Closed a configuration-binding asymmetry that made any auth-enabled Lighthouse container refuse to start when the operator followed the repo's own `render.yaml` deployment template. The S-1 CORS fail-closed validation (shipped one day earlier in `c389e80b`) threw `InvalidOperationException` if `AuthenticationConfiguration.AllowedOrigins` was empty. The `IReadOnlyList<string>` property binds from environment variables ONLY via indexed keys (`Authentication__AllowedOrigins__0`, `__1`, ...); a scalar `Authentication__AllowedOrigins=value` silently produces an empty list. The repo's `render.yaml:57` template prescribed the scalar form, so any operator using the template hit the bug at startup. The same trap had been encountered (and patched at the CI layer) by `21f6d066` a few hours after `c389e80b`, but the fix at that time was workflow-local — the binding contract remained asymmetric and the operator-facing deployment template stayed broken.

The fix adds a single binding-layer fallback: bind normally, and if the resulting `AllowedOrigins` list is empty, look up the scalar key and split on `,`/`;` with trim-and-drop-empties. Both call sites (`ConfigureServices` and `EnsureCorsFailsClosed`) now route through one helper.

## Business context

Reported by an operator who saw their previously-working container fail to start "since yesterday" — they had `Authentication__AllowedOrigins=https://localhost:48332` set as a single env var (the natural mental model, and what `render.yaml` recommended) and got the new fail-closed exception in production. The bug shipped on 2026-05-12; the operator hit it on 2026-05-13.

## Key decisions

| ID | Decision | Rationale |
|---|---|---|
| Option A (binding-layer fallback) over Option B (clearer error message only) | Make the natural form work, not just nag operators about the indexed form | The repo's own `render.yaml` uses the scalar form; making the error friendlier still leaves the template broken. Fix the contract, not the symptom. |
| Helper in `Program.cs`, not on the `AuthenticationConfiguration` record | The asymmetry is a configuration-binding concern, not a domain concern | Record stays a pure data carrier; binding policy stays at composition root. |
| Preserve fail-closed semantics for the empty-everywhere case | Security guarantee from S-1 must remain | Scenario 6 of the regression test pins this; fix only changes how non-empty operator inputs are interpreted, never weakens the empty-input gate. |
| No change to `render.yaml` | Template stays as-deployed; the binding catches up to it | Operator's existing config keeps working post-fix; no operator-side migration needed. |
| Document both forms in `configuration.md`, not deprecate one | Indexed form has muscle memory in CI and `appsettings.json`-style minds; scalar form has muscle memory for env-var-only deployments | Two paths to the same outcome, explicit "do not combine" guidance. |

## Steps completed (1 implementation pass, lean delivery)

| Step | What |
|---|---|
| Implementation | `Program.cs` — added `AllowedOriginsSeparators` static readonly field + `LoadAuthenticationConfiguration(WebApplicationBuilder)` helper. Both `ConfigureServices` and `EnsureCorsFailsClosed` rewired to consume it. ~30 production lines added. |
| Docs | `docs/Installation/configuration.md:204-205` — documented scalar (single / comma / semicolon) and indexed forms as two equivalent options. `docs/ci-learnings.md` — new `## Runtime / startup` section with the 2026-05-13 entry and the durable rule. |

DISTILL had already created `S1_AllowedOriginsEnvVarBindingTests.cs` (8 NUnit tests) and the feature delta. DELIVER did not modify either; the fix flipped 6 RED scenarios to GREEN without test surgery. No new components, no new files in `src/`.

## Quality gates summary

- **Build**: `dotnet build Lighthouse.sln` — 0 Warning(s), 0 Error(s).
- **Test density**: 8 / 8 in `S1_AllowedOriginsEnvVarBindingTests` GREEN; 4 / 4 backwards-compat regression guards in `S1_CorsFailClosedTests` GREEN; full backend suite 2384 / 2384 GREEN.
- **Adversarial review**: not run (lean delivery; user opted to skip).
- **Mutation testing**: deferred. CLAUDE.md specifies per-feature Stryker.NET ≥ 80%; appropriate for a follow-up since the change touches a single helper. Recommended command: `dotnet stryker --solution Lighthouse.Backend.sln`.
- **Wiring smoke**: the new helper has two production call sites (`ConfigureServices` and `EnsureCorsFailsClosed`); the new static field is referenced from the helper.
- **Scaffold removal**: none required — no scaffolds were created.
- **CLAUDE.md comment policy**: one non-obvious WHY comment in the helper explaining the .NET env-var binding quirk; no banned section banners, no provenance comments.

## Carry-forward / follow-ups

- **Generalise the rule for other list-typed config**: `Authentication__Scopes`, `Authentication__TrustedProxies`, `Authentication__TrustedNetworks` are all `IReadOnlyList<string>` on the same record and have the same operator-facing asymmetry. They have not bitten anyone yet because their fail-closed surface is smaller, but the same scalar-fallback pattern could be applied generically (e.g. an `IConfigurationBinder` extension) when a second consumer hits the trap. Worth tracking; not worth pre-abstracting.
- **Mutation testing run**: deferred per above. Single-helper surface area should mutate cleanly; recommend running before the next release.
- **Adversarial review**: deferred per above. The diff is small enough that the user opted out; `/ultrareview` or `@nw-software-crafter-reviewer` can be run later if desired.

## Cross-references

- Feature delta (single SSOT): `docs/feature/auth-allowedorigins-envvar-binding-fix/feature-delta.md`
- NUnit regression: `Lighthouse.Backend/Lighthouse.Backend.Tests/API/Security/S1_AllowedOriginsEnvVarBindingTests.cs`
- Originating fail-closed commit: `c389e80b` (2026-05-12) — `feat(security): CORS fail-closed, scoped connection list, rate limiting, API key scopes`
- CI-only precursor fix: `21f6d066` (2026-05-12) — `fix(ci): bind Authentication AllowedOrigins via indexed env var`
- CI learnings durable rule: `docs/ci-learnings.md` → `## Runtime / startup` → 2026-05-13 entry
