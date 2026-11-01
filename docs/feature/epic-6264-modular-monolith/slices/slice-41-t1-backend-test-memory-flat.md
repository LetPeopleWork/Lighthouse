# Slice 41 — T1 Backend test run keeps its memory flat: find and fix the test-host leak

Epic #6264 · ADO #6459 · `@infrastructure` · job_id: infrastructure-only · **Size M**

## Goal

One backend test run keeps its memory flat instead of growing until the machine runs out. Find what the test
host holds on to, release it, and add a check that keeps it released.

## Why

On 2026-10-31 a standard-filter backend run (`dotnet test` with the live-connector categories excluded, started by
another session in the main checkout) was sampled every 15 s from outside, read-only:

```
19:54  2.0 GB   open test DBs: 6
19:56  6.1 GB   open test DBs: 10
19:58  7.9 GB   open test DBs: 6
20:00 14.3 GB   open test DBs: 0
20:01 18.3 GB   open test DBs: 0   → machine down to 1.8 GB available (of 31 GB)
```

It rises steadily and never levels off, while only 0–10 test databases are open at any moment. Concurrency would
plateau; this is retention. Two sessions each running the backend suite exhaust the development machine and
kill test runs or the Claude session itself.

## IN

- **Analyse first.** Take heap snapshots (`dotnet-gcdump collect -p <testhost pid>`) of a filtered run, early
  and late, and name the retaining roots. Measure; do not assume. Suspects, in order:
  1. Entity Framework's internal service-provider cache. It is static and never evicts, and it holds one entry
     per distinct options set. Tests that add a new interceptor instance per test (`RealityCheckQueryCountTest`,
     `TeamFeatureSizeAcceptanceTest`, `Slice09TheFillShipsOptInSpecifications`,
     `Slice01ForecastRoundsRunOnTheirOwnThreadsScenarios`, `SecretCustodyServiceTests`, the repository tests)
     each add an entry. EF logs `ManyServiceProvidersCreatedWarning` when this happens.
  2. Undisposed hosts: about 98 `= new TestWebApplicationFactory` calls outside a `using`, plus derived hosts
     from `WithWebHostBuilder` / `WithTestAuthentication`.
  3. Static state in product or test code that captures a host's service provider.
- **Fix** the roots found.
- **Prevent it.** An automated check: for example a CI step that reports the test host's peak working set
  against a ceiling, or a test that fails when EF's internal provider count passes a bound. Add a
  `docs/ci-learnings.md` rule only if no tool can enforce it.

## OUT

- Making the suite faster (T2, FE7). Fewer hosts will leak less, but the leak must be named first.
- Worker caps (T3).

## Hypothesis

The growth comes from one or two roots that every host adds to, most likely the EF cache. **Disproved if** the
snapshots show no dominant root. In that case DESIGN re-scopes towards T2 (fewer hosts) plus a periodic forced
collection, and records why.

## Acceptance criteria

- [ ] DESIGN records the retaining roots, with snapshot evidence, before any fix.
- [ ] A full standard-filter run levels off, target peak under 3 GB, with before and after peaks measured on the
  same machine.
- [ ] A check fails when the leak returns.
- [ ] Every test stays green; no test is deleted, skipped or weakened.

## Note

The diagnosis needs test runs. Agree a quiet slot with the maintainer first, because sessions run in parallel on
the same machine.
