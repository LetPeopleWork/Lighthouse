# Slice — Enable NUnit fixture parallelization (CS-P)

**Goal (one sentence)**: Cut backend test wall-clock from ~6 min local / ~11 min CI to ~3 min local / ~6–7 min CI by adding `[assembly: Parallelizable(ParallelScope.Fixtures)]`, but first eliminate the one static-state collision (`AuthenticationMethodSchema`) that accounts for ~95 % of the failures the spike surfaced, then `[NonParallelizable]`-tag the 3–5 residual fixtures.

**Owner story**: US-02 (catalog candidate CS-P; spike report at `spike-be-parallelism-findings.md`).

**Estimated effort**: 1–1.5 days.
- Schema refactor: ½ day.
- Residual `[NonParallelizable]` annotations + per-test fresh mocks where viable: ½ day.
- Enable attribute, verify locally 3×, push, watch CI: ½ day.

**Learning hypothesis**:
- Confirms: After the schema refactor + targeted annotations, the full BE suite runs 3 times consecutively green under `ParallelScope.Fixtures` locally; local wall-clock drops to ≤ 3 min; CI test step drops to ≤ 7 min on the merge build.
- Disproves: If a second hidden static-state collision surfaces post-schema-fix, the failure count drops but does not reach zero. Mitigation: add a second targeted refactor or another `[NonParallelizable]` annotation; don't expand scope to a third concurrent refactor in this slice.

## IN scope

### 1. `AuthenticationMethodSchema` per-WAF isolation (production code)

The current shape (`Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/AuthenticationMethodSchema.cs`):

```csharp
public static class AuthenticationMethodSchema
{
    private static IReadOnlyList<string> extraOAuthKeysForTesting = Array.Empty<string>();
    // ... GetOAuthProviderKeys(), SetExtraOAuthKeysForTesting(), GetDisplayName(), etc.
}
```

Two production call-sites (4 references inside `Lighthouse.Backend/`) plus 19 references from the test project. The static `extraOAuthKeysForTesting` field is the collision: `Program.cs:404` writes it during WebApplicationFactory build; `Program.cs:427` reads it during the validator check. Parallel WAFs share the static; one WAF's stub registration trips the next WAF's validator.

Refactor approach (port-and-adapter style; keeps blast radius small):

- Promote `AuthenticationMethodSchema` to an instance type: `IAuthenticationMethodSchema` interface + `AuthenticationMethodSchema` class implementing it.
- Register as `Singleton` in DI (one per `WebApplication` / `WebApplicationFactory`). The "singleton-per-host" semantics matches today's static behaviour for production; in test, each WAF gets its own instance ⇒ no cross-fixture pollution.
- Existing static methods become instance methods. Migrate the 4 production + 19 test call-sites to resolve from DI.
- For `WorkTrackingSystemConnectionDto` (which currently uses the static methods inline in a property getter) — pass the schema via the DTO factory / mapper that constructs the DTO, so the DTO remains a POCO.
- Delete the `SetExtraOAuthKeysForTesting` static and its backing field.

TDD shape:

1. RED: Write a unit test asserting two `AuthenticationMethodSchema` instances can hold disjoint `extraOAuthKeys` simultaneously (proves the isolation contract; this fails today because there's no instance type).
2. GREEN: Introduce the interface + class + DI registration; migrate call-sites mechanically.
3. REFACTOR: Tidy any per-site smells (often inlining static-helper-style code into the schema instance).

### 2. Tag residual fixtures `[NonParallelizable]` (test code)

Per the spike findings, after the schema refactor the remaining failures fall into 2 clusters. Mark their fixtures `[NonParallelizable]`:

- `BackgroundServices.ExecuteAsync_*` tests (5 known) — Moq shared-mock state. Fixtures involved (confirm during slice): the `BackgroundServices.Update.*Test` fixtures that share a `Mock<IRefreshService>` or similar across method-level setups. Either annotate the fixture `[NonParallelizable]` OR move mock construction into `[SetUp]` (per-test) — pick the cheaper option per fixture; if 2+ tests already use a `[OneTimeSetUp]` to wire shared mocks, `[NonParallelizable]` is faster.
- `TerminologySeederTests` — shared in-memory DB context. Annotate `[NonParallelizable]`.

Scope this defensively: if more fixtures surface as flaky after the parallel attribute is added, annotate them in the same PR. The goal is "no flakes across 3 consecutive local runs", not "every fixture is parallel-safe forever".

### 3. Enable the assembly attribute (test code)

Add to `Lighthouse.Backend.Tests/GlobalUsings.cs`:

```csharp
global using NUnit.Framework;

[assembly: Parallelizable(ParallelScope.Fixtures)]
```

The pre-existing `[NonParallelizable]` annotations on security-bench fixtures (`S1_*`, `S5_*`, `S6_*`, etc.) continue to work as fixture-level opt-outs.

### 4. Verify and measure

- Local: 3 consecutive runs with `dotnet test ... --filter "Category!=Integration"`. All green. Record mean wall-clock + per-run variance.
- Open a PR branch and watch the next `Build And Deploy Lighthouse` run. Record the new BE test step wall-clock. Append to the multi-run baseline table in `feature-delta.md` under "Wave-decisions summary".
- Update `alternatives.md` "Ranking" table: mark CS-P shipped with the observed numbers.

## OUT scope

- `ParallelScope.Children` / `ParallelScope.All` (parallelize within fixtures) — separate spike if 2× isn't enough.
- Refactoring every fixture to be parallel-safe right now — `[NonParallelizable]` is the legitimate opt-out and is in use elsewhere already.
- Sharing `WebApplicationFactory` across fixtures (collection fixtures) — bigger architectural change; revisit after CS-P + CS-H land.
- Splitting the test project into multiple smaller projects — bigger architectural change; revisit if the suite grows another 50 %.
- Replacing in-memory EF with SQLite-in-file — separate consideration about test fidelity, unrelated to parallelism.
- Mutation testing the schema refactor — Stryker per-feature configs auto-pick up assembly attributes; no change needed.
- E2E / Playwright — out per D6.

## Acceptance criteria

- AC-P.1: `AuthenticationMethodSchema` is no longer a `public static class`. An `IAuthenticationMethodSchema` interface + class exist; both are registered in DI (Singleton per host). `Program.cs:404` and `Program.cs:427` resolve from DI, not from a static.
- AC-P.2: `extraOAuthKeysForTesting` static field is deleted. The per-fixture stub-key registration goes through DI's per-host singleton.
- AC-P.3: At least one TDD-style unit test proves two schema instances can hold disjoint `extra` state without bleed.
- AC-P.4: `[NonParallelizable]` is applied to the 3-5 fixtures named in the spike report (or the equivalent per-test refactor — choose per fixture).
- AC-P.5: `[assembly: Parallelizable(ParallelScope.Fixtures)]` lives in `Lighthouse.Backend.Tests/GlobalUsings.cs`.
- AC-P.6: Local `dotnet test ... --filter "Category!=Integration"` runs 3 times consecutively, all green, with mean wall-clock ≤ 3 min 30 s (target ≤ 3 min, ceiling allows for noise).
- AC-P.7: PR branch CI `Verify Backend` test step wall-clock ≤ 7 min (target 6 min, ceiling 7 min).
- AC-P.8: Stryker.NET against `Cache<,>` (existing per-feature config) stays ≥ 80 % kill rate — sanity check that the assembly attribute doesn't break mutation testing.
- AC-P.9: `docs/ci-learnings.md` gets a new entry under "Test selection / parallelization rules": when to annotate `[NonParallelizable]`, the per-host singleton pattern for shared state, the `AuthenticationMethodSchema` precedent.
- AC-P.10: ADO #5020 has a comment summarising the slice, the wall-clock before/after, and the residual fixtures opt-out list.

## Dependencies

- Spike findings (`spike-be-parallelism-findings.md`) merged — done (`a4550e0b`).
- slice-pre (`b404eb07`) and slice-03A / CS-G (`e2589815`) shipped — done. These give a clean baseline; without them the spike numbers would be muddier.

## Reference class

Hybrid: production-code refactor (`AuthenticationMethodSchema` static → instance, port-and-adapter shape) + test-infra annotation + one assembly attribute. Most similar in shape to the 2026-05-17 `VssConnection`/`ClientCache` isolation fix referenced in `docs/ci-learnings.md` — same problem class (shared static keyed by a generic identity), same fix class (carry the discriminator through the type, not the static).

## Pre-slice SPIKE

Done — `spike-be-parallelism-findings.md`. No further spike needed.

## Taste tests

- Ship 4+ new components? **Borderline** — the schema interface + class + DI registration + the per-fixture annotations + the assembly attribute are one coherent system. Acceptable.
- Depends on a new abstraction? **Yes — `IAuthenticationMethodSchema`.** Single-responsibility, single-implementation; the abstraction *is* the slice's product, not a speculative future-flexibility play.
- Disproves something? **Yes** — that BE test serial cost is intrinsic.
- Synthetic data only? **No** — verified against real `dotnet test` and real CI runs.
- Identical-except-for-scale duplicate of another slice? **No.**

All taste tests pass.

## Risk and mitigations

- **A second hidden static-state collision surfaces post-schema-fix.** Mitigation: same as the first — find it, isolate per-host or annotate `[NonParallelizable]`. Bounded: spike data shows ~95 % of failures share one root, so a second root would surprise me.
- **CI runner core count is lower than dev box.** CI achieves smaller speedup than local. Stated explicitly in AC-P.7 (≤ 7 min CI vs ≤ 3 min local).
- **Future tests added without parallel-safety awareness re-introduce flakes.** Mitigation: the `docs/ci-learnings.md` entry (AC-P.9) documents the pattern; PR review catches the rest.
- **`AuthenticationMethodSchema`-as-instance changes a heavily-referenced API.** 4 production + 19 test references. Each is mechanical (`AuthenticationMethodSchema.Foo()` → `_schema.Foo()` after DI injection). Risk is mostly typo-class, caught by the compiler + the test run.
- **Mutation kill rate slips.** AC-P.8 names this; remediate immediately if it does.
