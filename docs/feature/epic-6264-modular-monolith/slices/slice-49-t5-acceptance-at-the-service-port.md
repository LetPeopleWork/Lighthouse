# Slice 49 — T5 New acceptance scenarios drive the application-service port, with a thin HTTP skeleton per flow

Epic #6506 · ADO #6469 · `@infrastructure` · job_id: infrastructure-only · **Size M** (estimate: four slices of at most one day, cut at DESIGN)

- infrastructure_rationale: test-infrastructure and convention work, no user-visible slice (Epic D-6264-2, D-6264-5). Most backend acceptance tests boot the whole app and call it over in-memory HTTP, even when the scenario checks business behaviour. A 2026-11-02 run counted 2,089 host builds, 282 s summed (ADO #6469; T2 `d1-summary.txt`).

## Goal

A convention, written down and enforced: per flow, one or two walking-skeleton tests go through the host and prove the HTTP and DI wiring; every other new scenario drives the application-service port. The worst existing suites move once, each with a check of what HTTP behaviour stays covered.

## Decided by the maintainer (2026-11-07)

1. The convention applies to all new scenarios. 2. Existing suites move only for the worst by measured cost; the rest is cleaned up as we go. 3. It is written into CLAUDE.md; we check back once the Epic is in validation. 4. A ratchet guard: an inline list of today's host-using files that only shrinks; a new host test outside the skeleton list fails the build and names the file.

## Proposals (each with its why)

- **a. Flow and skeleton.** A *flow* is one user outcome reached through one entry route, e.g. "a team refresh runs and its result shows in the task list". A *skeleton* is a test that sends a real request through `TestWebApplicationFactory` and asserts a business outcome that needs the route, the DI wiring or middleware to appear. Violation test for a reviewer: a host test whose every assertion could be made after calling the service directly, and that is not named in the skeleton list. Why: it gives the guard a list to check and the reviewer a yes/no question.
- **a2. Dependencies at the port.** Build the service over an SQLite context from the existing schema template (`TestDatabaseTemplate.CopyTo` plus a `LighthouseAppContext`), with the service's own collaborators from a small `ServiceCollection` that registers only what the port needs. Direct construction with `new` stays fine for services with two or three collaborators. Why: `TestDatabaseTemplate` already gives a cheap real database; the repo has ad-hoc small containers (`BuildServiceProvider` in `EmbedStartControllerTest`, `DataProtectionKeyRingTest`) but no shared helper, so DESIGN decides whether to add one.
- **b. Worst suites.** The data is not good enough to rank by remaining cost. D1 (T2, `d1-by-listed-owner.csv`) counts host builds per owner before T2 moved fixtures to one host each, so it ranks the old per-test cost. Top by that count, host builds being about the test count: `UsageDataCollectorObservationTest` 14 fixtures, 279 builds; `ConnectionDefaultsAcceptanceTest` 8, 98; `FasterUpdatesAcceptanceTest` 7, 94; `TaskManagerAcceptanceTest` 17, 90; `ForecastRealityCheckAcceptanceTest` 3, 81. 25 of the 126 listed owners had no timing row, and T2 has since cut the allow-list from 115 to 53 entries. **DESIGN measurement:** after the other timing run ends, one `LIGHTHOUSE_FIXTURE_TIMING=1` run per candidate harness family, recording set-up plus body seconds per fixture today, and per test whether it makes an HTTP call. Rank by seconds that a move removes (set-up the seeders do per test, T2 follow-up note of about 210 ms, plus body), not by host builds. Why: T2's own `ranking, not a forecast` warning, and its saving figures are ceilings.
- **c. Per-suite HTTP-coverage check.** Before a suite moves, DESIGN lists each test's HTTP-visible assertions: route and verb, status code, JSON property names, headers, auth outcome (401/403). Then it names, for each, the test that keeps pinning it (skeleton, controller unit test or existing contract test). A test that really pins routing, JSON shape, status codes or authorization **stays at the host** and goes on the skeleton list with its flow name, even if that makes a flow have more than two. The check is a table in the slice's DESIGN note, reviewed before the move. Why: moving a test must never remove the only check of a wire contract.
- **d. Guard.** A new rule, `TestHostUseGuardTest`, in `Architecture/`, beside `TestHostPerFixtureGuardTest`. It reuses the same Cecil scan (`TestHostScanning`) but looks at every host start reachable from set-up, one-time set-up or a test, not only per-test ones. Two inline lists: today's host-using types, which only shrink (exact ceiling constant, like `MostTypesTheListMayHold`), and the skeleton list, each entry with its flow. A planted fixture (the guard already plants six) that starts a host from `[OneTimeSetUp]` and is on neither list must fail with the type name. The old guard stays: it says *how often* a host starts, the new one says *whether a test may use one*. Why: two questions, two failure messages; neither rule is loosened.
- **e. Where agents are told.** A short bullet in the project CLAUDE.md (like the "no scenario tags" bullet), read by every wave: new backend acceptance scenarios go to the port, a host test needs a skeleton-list entry. The DISTILL dispatch also carries it. Left for T11: the test-writing guidance with worked examples and the checklist. Why: CLAUDE.md is what the maintainer chose; T11 builds the longer skill on a settled rule.
- **f. Sizing and order.** Estimate M, four slices of at most one day each: (1) port helper and one pilot suite, to prove the shape; (2) the guard with today's list and the skeleton list, in the same commit as the pilot's list change; (3) CLAUDE.md bullet and the DISTILL dispatch text; (4) the worst suites, one per day-slice at DESIGN, as many as the measurement justifies. Slice 4 may be several slices. Order by value: the guard before the moves, so nothing new lands while old suites are cleaned. T5 against T10 (#6537, rules pinned by unit tests): T5 moves a test only from host to port; T10 pins rules at unit level from there. A suite T5 touches is closed to T10 until T5 finishes it, and T10 starts from the files T5 leaves. Assumption: I could not read T10's brief; the maintainer confirms the line.
- **g. Behaviour-free.** The Epic rule (routes, DTOs and responses unchanged, existing API tests green unmodified) holds for production code: **no production file changes in T5.** For tests it becomes "no behaviour lost": every business assertion survives at the port, and every HTTP assertion is either kept or covered by a named test (c). Test ids change when a test moves, so D11's equal-id-list proof is replaced by a before/after table per suite mapping each old id to its new id or to "stays". If a scenario has no port (logic sits in a controller), the test stays at the host, is recorded as such, and the gap is an input for Epic #6508 (thin controllers); T5 does not add a port.

## IN

- The convention, helper decision, guard, CLAUDE.md bullet and DISTILL text above.
- The per-suite HTTP-coverage tables and the moves of the worst suites, with a mutation-free proof of equal business assertions (deliberate break of one production line per moved suite shows the port test fails).

## OUT

- Production code, routes, DTOs and UI. Existing suites outside the measured worst. Postgres container and live-connector classes. Frontend tests. The T11 guidance and T10's unit-level rule tests.

## Dependencies

Hard: T2 #6460 (done 2026-11-02: guard, `TestWebApplicationFactory` host-build timing, `FixtureSetupTimer`). Soft: T11 #6538 and T10 #6537 wait on this Story. Slice-composition gate waived for `@infrastructure` Stories (D-6264-5).

## Cross-cutting (explicit)

- RBAC: N/A, because no authorization logic changes; any test pinning 401/403 stays at the host (c).
- Lighthouse-Clients (CLI/MCP): N/A, because no API contract or version changes.
- Website: N/A, because nothing user-facing changes.
- Usage-data event: N/A, because no feature is used by a user; this is test structure.
- UI sketch: N/A, because no UI is added or changed.
- Terminology: N/A, because no user-visible word is added; the brief uses seeded default terms.
- Context map (ADR-270): N/A, because no context, concept, term or event is added or changed; tests only.
- Mutation testing: N/A for test code (T2 D12: Stryker mutates the product assembly); the deliberate-break proof stands in.

## Acceptance criteria (CI- or developer-observable; estimates marked)

- [ ] Given a planted fixture that starts a host and is on neither list, when the guard runs, then the build fails naming the file; a type removed from the list lowers the ceiling in the same commit.
- [ ] The CLAUDE.md bullet and the DISTILL dispatch text exist; a new scenario written to them lands at the port.
- [ ] For each moved suite: a coverage table (c), the before/after id table, and a deliberate-break proof, all in the slice's workspace.
- [ ] Host builds in a standard-filter run fall from 2,089 (2026-11-02); the target is set at DESIGN from the measurement, not now.

## Open for the maintainer

1. **Does the skeleton list count against "only shrinks"?** (A) Skeleton list may grow, one named entry per flow, reviewed in the PR; (B) fixed cap of two per flow in the guard. Recommend A with the per-flow count printed in the failure; B would force a rule loosening the first time a flow pins several wire contracts.
2. **A test that pins a wire contract: stay, or become a separate contract test class?** (A) stays where it is, on the skeleton list; (B) moves to a `Contracts/` namespace the guard treats as always allowed. Recommend A for now: no extra category, no test moves twice.
3. **Is the line to T10 right?** I could not read #6537. (A) T5 first, T10 starts from what T5 leaves; (B) T10 takes the rule tests of suites T5 will not touch in parallel. Recommend A.
4. **How many worst suites in this Story?** (A) Pilot plus the top two by the DESIGN measurement; (B) all suites above a time threshold DESIGN proposes. Recommend A, then re-decide with numbers.

## Maintainer decisions (2026-11-07)

These override the proposals above where they differ.

- **Skeleton list:** may grow, one named entry per flow; a third skeleton for a flow is caught in review. Today's host-user
  list only shrinks.
- **Worst suites:** a pilot plus the top two by DESIGN's measurement, then decide again with the numbers.
- **T10 #6537:** comes after T5 and starts from the files T5 leaves, as its ADO text says, so no test moves twice.
- **Wire-contract tests:** a separate `Contracts` namespace, which overrides option A above. The guard allows it only for tests
  deriving from a small contract base class, so a move there is a deliberate step visible in the diff. The namespace and
  base class land with T5, with the wire-contract tests of the pilot and the top two suites; the rest move when someone
  touches them and stay on the shrinking list until then. Check against T6 whether `Contracts` is a slow-tier marker.
