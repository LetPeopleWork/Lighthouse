# Feature Delta — test-speed-improvements

ADO source: [User Story #5020 — Improve speed of Frontend and Backend Tests](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5020) (state: `Active` as of 2026-05-18)
Density: `lean` (resolved from `~/.nwave/global-config.json`) + one user-requested `[WHY] alternatives-considered` expansion
Waves complete: DISCUSS, DELIVER Slice 01 (baseline), DELIVER Slice 02 (alternatives memo — **RESUMED 2026-05-18**, multi-run validation n=11 BE / n=15 FE confirms ranking), DELIVER slice-pre (integration labelling fix, `eb6fe68d`), DELIVER Slice 03A (CS-G cache concurrency downscale, `e1cbb4a3`)
Next wave: DELIVER **spike-be-parallelism** (CS-P discovered 2026-05-18 — NUnit runs serially; this is now the biggest lever) → DELIVER **slice-be-parallel-enable** (gated by spike) → DELIVER Slice 03B (CS-H path-scoped integrations). Two spikes (`spike-fe-profile`, `spike-cs-b-setup-split`) gate the next-tier FE and BE slices. CS-I (Vitest sharding) rejected per user feedback (prior attempt: effort exceeded gain). See `alternatives.md` Measurement note + RESUMED banner for the full plan and the wall-clock vs test-sum correction.

---

## Wave: DISCUSS / [REF] Persona ID

`lighthouse-developer` — A maintainer or contributor (Benj + community) writing C# / TypeScript against Lighthouse. Runs `dotnet test` and `pnpm test` many times per day. Pushes a branch and waits on `Build And Deploy Lighthouse` to gate the merge. Currently treats the backend + frontend test wait as a context-switch tax.

---

## Wave: DISCUSS / [REF] JTBD one-liner

**When** I have a backend or frontend code change ready, **I want to** know within a minute (locally) and a few minutes (CI) whether it broke anything, **so I can** stay in flow and ship without batching multiple changes behind a slow signal.

Full job story added to `docs/product/jobs.yaml` as `job-dev-test-feedback-velocity`.

---

## Wave: DISCUSS / [REF] Locked decisions

| ID | Decision | Verdict | Rationale |
|---|---|---|---|
| D1 | Feature type | Cross-cutting (developer-experience) | Touches Backend NUnit suite, Frontend Vitest suite, and CI workflows. |
| D2 | Walking skeleton | No | Brownfield optimisation; no greenfield vertical to bootstrap. |
| D3 | UX research depth | Lightweight | Single persona (developer). |
| D4 | JTBD analysis | Yes | The developer IS the user. Story `job-dev-test-feedback-velocity` traces all stories. |
| D5 | API-coverage invariant | Hold | "At least every API call for each work-tracking system is verified" stays non-negotiable per ADO #5020. Any fix must preserve this; the **cadence** and **mechanism** of verification are negotiable. |
| D6 | E2E scope | **Out for this feature** | User-directed (2026-05-17). Playwright sharding and verify-* parallelism are a follow-up; this feature focuses on backend + frontend. |
| D7 | Measurement before action | Mandatory first slice | Per ADO #5020 author intent: "Start by analyzing what may take the longest." No optimisation slice opens until baseline data + alternatives memo are reviewed. Avoids narrowing on a guess. |
| D8 | Alternative-mechanism candidates | Stay open | User-directed (2026-05-17): "consider if there are other ways than splitting the CI jobs, rewriting the tests somehow." Cadence-split, test-rewrite, fixture-recording, session-sharing, parallelism-config — all stay candidates until baseline data exists. The `[WHY]` expansion below enumerates the catalog. |
| D9 | Mutation-testing impact | Re-validate per-feature kill rate ≥ 80 % after whatever fix lands | CLAUDE.md mandates Stryker.NET + Stryker ≥ 80 % per feature delivery. |

---

## Wave: DISCUSS / [REF] User stories with elevator pitches

This feature ships in two firm stories (US-01, US-02) followed by **candidate** stories that only enter Definition of Ready after the alternatives memo lands.

### US-01 — Per-test timing baseline visible in CI artifact

**As** a Lighthouse developer
**I want** every CI run to publish per-test timing CSVs for backend and frontend (durations, suite, category)
**So that** I have evidence — not anecdote — when I claim "test X is the slow one"
**Job traceability**: `job-dev-test-feedback-velocity`

#### Elevator Pitch
Before: A developer wanting to speed up tests has to scroll CI logs manually and eyeball durations; the data is there but unaggregated.
After: run `gh run download <runId> --name test-timings` → sees CSVs (backend + frontend) with one row per test, sorted by duration descending, with `suite,category,duration_ms` columns.
Decision enabled: Pick targeted offenders to address in US-02's alternatives memo — instead of guessing.

#### Acceptance criteria
- AC-01.1: `dotnet test ./Lighthouse.Backend` writes a TRX whose per-test timings are extracted into `test-timings-backend.csv` (columns: `fully_qualified_name,category,duration_ms,outcome`) — published as a CI artifact for every PR build.
- AC-01.2: `pnpm test --reporter=verbose` output is post-processed into `test-timings-frontend.csv` (columns: `file,test_name,duration_ms,outcome`) — published as a CI artifact for every PR build.
- AC-01.3: A `scripts/test-timings/summarise.{sh|ps1}` helper merges the CSVs and prints the top-20 slowest tests to stdout when run locally against a `TestResults/` directory.
- AC-01.4: Running the summary helper on the CI artifact from a single PR run produces a non-empty table within 5 seconds and exits 0.
- AC-01.5: The artifact distinguishes `[Category("Integration")]`-tagged tests (real-API hitters) from the unit suite, so the "is real-Jira/real-ADO traffic the bottleneck?" hypothesis can be tested directly from the CSV.

### US-02 — Alternatives memo grounded in the baseline

**As** a Lighthouse developer (and the user driving this story)
**I want** a short, evidence-based memo that scores each candidate speed-up approach against the actual baseline data — not a pre-committed plan
**So that** the slice we open next is the one the data supports, not the one we guessed at in DISCUSS
**Job traceability**: `job-dev-test-feedback-velocity` (emotional: "confidence the fix is the right one")

#### Elevator Pitch
Before: We have a hypothesis (real-Jira/real-ADO traffic dominates), but no proof and no comparison of fixes; jumping to "split CI jobs" might miss a cheaper win like fixture-sharing or test-rewriting.
After: read `docs/feature/test-speed-improvements/alternatives.md` → sees each candidate (A through F below) with a one-paragraph description, an expected wall-clock impact based on Slice-01 data, an estimated effort, a coverage-invariant impact assessment, and a recommendation.
Decision enabled: Pick one (or stack two) candidates to open as concrete slices. Reject the rest with reason. The remaining candidates stay in the memo as documented "not now" choices.

#### Acceptance criteria
- AC-02.1: `docs/feature/test-speed-improvements/alternatives.md` is produced AFTER Slice-01 lands and contains one section per candidate from the catalog below.
- AC-02.2: Each candidate section has: (a) hypothesis it disproves, (b) effort estimate, (c) coverage-invariant impact, (d) expected wall-clock gain calibrated to the Slice-01 numbers, (e) recommendation (open as slice | hold | reject).
- AC-02.3: The memo concludes with a ranked top-1 or top-2 picks. The picks are what becomes the next candidate slice(s).
- AC-02.4: No other slice opens before this memo is reviewed by the user (`/ado-sync` confirmation gate; analogous to the DoR review for fresh stories).

### Candidate stories (post-memo; NOT in DoR yet)

These are listed for context but do not enter DoR until US-02 has selected one or more.

- **CS-A** — Backend integration tests run on a different cadence (PR vs release-tag).
- **CS-B** — Backend integration tests restructured to share authenticated sessions / cached fixtures across multiple test methods (one real-API setup, many assertions).
- **CS-C** — Backend integration tests recorded once and replayed (VCR-style cassettes); real-API runs only on release-tag verification.
- **CS-D** — Frontend top-N slowest specs fixed in place (anti-pattern removal: real timers, oversized fixtures, redundant renders).
- **CS-E** — Frontend Vitest config tuning (worker count, isolation level, parallelism budget).
- **CS-F** — Backend test parallelism / isolation hardening (the 2026-05-17 CI learnings show the static-cache collisions are a known anti-pattern; fixing them may enable safe parallelism increases).

Each candidate has its own taste-test row in the alternatives memo; the memo's recommendation determines which get carpaccio-sliced.

---

## Wave: DISCUSS / [WHY] Alternatives considered (catalog)

User-requested expansion (2026-05-17). The ask-intelligent cross-context trigger had already flagged this; the user's "don't narrow your view just yet" instruction made it required.

Each row is a candidate evaluated AGAINST a hypothesis and scored AFTER Slice-01 data lands in US-02. The catalog is meant to be exhaustive enough that the memo can REJECT options with reason, not just pick favourites.

| ID | Mechanism | Hypothesis it tests | First-cut effort | First-cut risk to coverage invariant |
|---|---|---|---|---|
| CS-A | **Cadence split**: move `[Category("Integration")]` tests off per-PR; run on `main` + release tag. | "Real-API calls dominate the PR-CI wall-clock." | Low (1 day) — `.runsettings` filter + new workflow YAML. | Medium — release-tag cadence may catch drift later than per-PR. Mitigation: coverage-map gate (CS-A.1). |
| CS-B | **Fixture session sharing**: one real-API setup per test class instead of per test method (`OneTimeSetUp` / shared fixtures); many assertions reuse the authenticated `VssConnection` / Jira client. | "Most of the integration runtime is auth + connection setup, not the assertions." | Medium (2-3 days) — refactor connector test fixtures to share state safely (mind the 2026-05-17 cache-collision learnings). | Low — same calls, fewer of them; explicit invariant: every public connector method still exercised. |
| CS-C | **Recorded cassettes**: capture real API responses once (VCR-style with a tool like WireMock or `PollyCache`); replay on per-PR runs; re-record on release tag. | "We can verify our request shape and response handling without paying real-API latency on every PR." | High (4-5 days) — requires fixture-management discipline + a re-record CI job. | Medium — replays prove our code's shape; real upstream drift only caught on re-record cadence. Acceptable if re-record is automated and gated. |
| CS-D | **Per-spec fixes**: identify top-N slowest Vitest specs from Slice-01 and apply standard speed-ups (fake timers, smaller fixtures, fewer DOM mounts). | "A small handful of specs account for the FE bulk; standard refactor patterns win." | Low-medium (1-2 days, scoped per spec). | None — coverage unchanged when test names preserved. |
| CS-E | **Vitest config tuning**: increase worker count, switch isolation mode, prune cold-start cost. | "FE slowness is config, not test content." | Low (a few hours of experiments). | None directly — but config changes can mask flake; needs back-to-back green runs to validate. |
| CS-F | **Backend parallel/isolation hardening**: fix the static-cache collision class (per 2026-05-17 CI learning) so `MaxCpuCount=0` is genuinely safe, then raise the parallelism budget. | "Static state in connectors and shared cache keys is silently capping the parallelism we already configured." | Medium (2 days) — change the cache key shape, add credential fingerprinting (cf. ci-learnings 2026-05-17 entries). | None — coverage unchanged; isolation is strictly improved. |
| CS-G | **Cache concurrency test downscaling**: reduce thread counts and time budgets in `CacheTest` concurrency tests so they prove the invariant without spawning 64 tasks that serialise on constrained CI runners. Added 2026-05-17 from Slice-01 evidence — the catalog at DISCUSS time assumed real-API traffic and FE specs were the dominant cost; the data revealed a single test suite (`Cache.CacheTest.Concurrent*`) consuming 133.7 s = 25 % of BE wall-clock on CI. | "A small number of intentionally-thread-heavy unit tests scale poorly on CI's limited cores and dominate BE wall-clock." | Low (½ day) — reduce thread counts (32+32 → 8+8 or similar), shorten the time-bounded loop, keep the same correctness assertions. Could combine with property-based testing later. | None — same invariants asserted (no torn reads, no exceptions, all values retrievable). Stress dimension changes; correctness coverage does not. |
| CS-H | **Path-scoped integration test selection**: per-connector category sub-tags (`JiraIntegration`, `AdoIntegration`, `LinearIntegration`, `GithubIntegration`); a resolver script reads `git diff --name-only` and emits a `dotnet test --filter` string; default local UX is "skip all integrations", `--full` is the opt-in, and `main`-cadence forces full coverage post-merge. Added 2026-05-18 from Slice-02 multi-run analysis + user feedback. **The Integration cluster is 88 % Jira** (350 s of 397 s); empirical commit-pattern weighting across 585 recent commits shows 88 % of PRs touch neither connector folder nor shared base — those PRs would skip all integration tests entirely. | "Integration tests must run on every PR; we can't safely scope them by changed-files." | Medium (2 days) — sub-category sweep + resolver script + GHA workflow rewrite + Stryker config sweep + 2-3 PR validation cycles. | D5 preserved via forced-full `main` cadence (every merge exercises every connector). Per-PR detection latency relaxes for unchanged connectors — at most one merge of delay. |
| LBL-FIX | **Integration test labelling fix** (prerequisite for CS-H): `AzureDevOpsWorkTrackingConnectorTest.cs` (85 methods, mean 29.5 s) and `LinearWorkTrackingConnectorTest.cs` (19 methods, mean 8.9 s) both hit real APIs via env-var tokens but carry no `[Category("Integration")]` attribute. Slice-02 multi-run analysis found ~38 s of real-API traffic silently counted as Unit/Other. Discovered 2026-05-18 during memo resume. **Shipped `eb6fe68d`.** | "The existing `[Category('Integration')]` taxonomy is truthful." | Trivial (½ day) — attribute additions on 2 files + verification on next CI artifact. | None — same tests run, same assertions, only the taxonomy becomes truthful. Local `dotnet test --filter "Category!=Integration"` becomes accurate. |
| CS-P | **NUnit fixture parallelization**: add `[assembly: Parallelizable(ParallelScope.Fixtures)]` to `Lighthouse.Backend.Tests/GlobalUsings.cs` so test fixtures run in parallel. Discovered 2026-05-18 after slice-pre + CS-G: the suite is 100 % serial today (no assembly-level `Parallelizable`; CI's `MaxCpuCount=0` only parallelises *between* test assemblies, and Lighthouse has only one BE test assembly). CI test step wall-clock 11–12 min, local 6 min — both serial. | "BE test slowness is per-test cost we can only attack by making individual tests faster." | ½ d spike + 0–2 d fixes. Spike enables the attribute, runs the full suite, captures every test that fails or flakes; report drives the follow-up slice. | Coverage unchanged. Risk is **test isolation** (already known via 2026-05-17 `VssConnection` cache-collision learning — same class of issue). The codebase already carries `[NonParallelizable]` on a few security tests, suggesting prior awareness. Mitigations are documented in `docs/ci-learnings.md`. |

Combinations to consider (revised post-Slice-02 resume):
- **slice-pre → CS-G → CS-H** (current top recommendation) — labelling, then cheapest BE win, then biggest per-PR win. Cumulative −75 % BE on the average PR; no cadence change, no cassette debt, no coverage regression.
- **SPIKE-FE-profile → CS-D refined** (parallel FE pipeline) — local-first FE wins driven by profiling data, not pattern-matching.
- **SPIKE-CS-B → maybe CS-B** (post-BE-pipeline) — gate the 2-3 d investment behind a ½-d setup-vs-body measurement.
- **CS-A** (cadence-split all integrations) — held as fallback if CS-H proves too aggressive.
- **CS-C, CS-E, CS-F** — rejected (see `alternatives.md` for reasons).
- **CS-I** (Vitest sharding) — rejected up front per user feedback (prior attempt: effort > gain).

---

## Wave: DISCUSS / [REF] Acceptance criteria

Embedded per story above. Cross-cutting rule: no candidate slice opens, and no duration target is claimed, before the Slice-01 timing CSVs exist and the US-02 memo has ranked the options against them.

---

## Wave: DISCUSS / [REF] Definition of Done (this feature delta)

1. Slice-01 timing artifacts shipping on every PR build (backend + frontend).
2. US-02 alternatives memo published, reviewed, and one or more candidate slices opened from it.
3. Each selected candidate slice ships its own DoD (per-slice DoD in the slice brief produced later).
4. API-coverage invariant (D5) preserved end-to-end across whatever mechanism is selected.
5. Mutation kill rate ≥ 80 % on both BE (Stryker.NET) and FE (Stryker) for any file touched by the selected slices.
6. `docs/ci-learnings.md` updated per `/clean-ci` discipline if any CI failure surfaces during DELIVER.
7. No new SonarCloud issues introduced.
8. ADO #5020 reaches `Resolved`; no child Stories created (user-directed, 2026-05-17).

Concrete wall-clock targets (e.g. "BE < 60 s local") are set in the candidate slice briefs once the data is in. DISCUSS does not pre-commit those numbers.

---

## Wave: DISCUSS / [REF] Out-of-scope

- **End-to-end (Playwright) suite speed-ups**: explicitly out (D6). Verify-* job sharding is a separate feature when ready.
- **New test framework / framework migration**: NUnit + Vitest stay; this is operational + mechanical, not technological.
- **Telemetry to the vendor team**: Test timings are CI artifacts and local stdout; Lighthouse is self-hosted, no phone-home.
- **Cross-platform runners (`verify-windows`, `verify-macos`)**: Linux-runner numbers are canonical for this feature; cross-platform speed is follow-up.
- **Coverage expansion**: This feature does not add tests; it changes how (or how often) existing tests run.
- **Decision on which candidate wins**: That happens in US-02 memo, not here.

---

## Wave: DISCUSS / [REF] WS strategy

**Not applicable — D2 = "No walking skeleton"**. Brownfield optimisation; no vertical slice to bootstrap.

---

## Wave: DISCUSS / [REF] Driving ports

| Surface | Form | Owner story | Change type |
|---|---|---|---|
| `dotnet test` (CLI) | Local + CI | US-01, candidate slices | Reporter wiring; later: filter / fixture changes per chosen candidate |
| `pnpm test` (CLI) | Local + CI | US-01, candidate slices | Reporter wiring; later: per-spec / config changes per chosen candidate |
| `Build And Deploy Lighthouse` workflow | GitHub Actions | US-01 + selected candidates | Artifact upload (firm); later: cadence/sharding/fixture-record per chosen candidate |
| `scripts/test-timings/summarise.*` | New developer CLI | US-01 | New artifact |
| `scripts/coverage-map` (only if CS-A or CS-C selected) | New developer CLI | candidate stories | New artifact, scoped to the candidate |

No HTTP / UI / public-API surface changes.

---

## Wave: DISCUSS / [REF] Pre-requisites

- `docs/product/jobs.yaml` is back-propagated with `job-dev-test-feedback-velocity` (done; see SSOT section in this file's footer).
- ADO Story #5020 stays the single parent for this work; **no child Stories created** (user-directed 2026-05-17).
- CI learnings file (`docs/ci-learnings.md`) is the canonical record of test-isolation invariants the speedup MUST preserve. Five entries from 2026-05-12 to 2026-05-17 are directly relevant and constrain the candidate slices.

---

## Wave: DISCUSS / [REF] Scope assessment

| Heuristic | Threshold | Observed | Verdict |
|---|---|---|---|
| User stories (firm in DISCUSS) | > 10 = oversized | 2 firm + 6 candidate (post-memo only) | Pass — firm scope is intentionally narrow |
| Bounded contexts / modules | > 3 = oversized | 2 (BE tests, FE tests) | Pass |
| Estimated effort (firm slices) | > 2 weeks = oversized | Slice 01 ≤ 1 day + Slice 02 memo ≤ 1 day + selected candidates later (each ≤ 1 day) | Pass |
| Independent outcomes | If multiple → split | Single coherent outcome (dev velocity) | Pass |

**Verdict: right-sized**, with intentional under-commitment on candidate slices until baseline data exists.

---

## Wave: DISCUSS / [REF] Story map and slice plan

Backbone (left-to-right developer journey):

```
  measure  →  evaluate options  →  ship chosen fix(es)
   US-01       US-02 (memo)         candidate slice(s) — opened only after memo
```

Firm slices in DISCUSS (full briefs at `docs/feature/test-speed-improvements/slices/`):

1. **slice-01-baseline-instrumentation** — measure BE + FE per-test timings, publish CI artifacts, ship local summary helper. Disproves "we already know what's slow."
2. **slice-02-alternatives-memo** — read Slice-01 data, score each candidate (CS-A…CS-F + combinations) against actual numbers, recommend top-1 or top-2 for the next slice opening. Disproves "splitting CI jobs is obviously the right answer." **RESUMED 2026-05-18** with multi-run validation; back-propagated CS-H and the LBL-FIX prerequisite.

Slices opened by the resumed memo (2026-05-18):

3. **slice-pre-integration-labelling** — tag the 2 mistagged real-API test classes so the taxonomy is truthful. ½ d. **Shipped `eb6fe68d`.**
4. **slice-03a-cache-concurrency-downscale** — CS-G. ½ d. Local + CI BE −22 % of test-sum. **Shipped `e1cbb4a3`.**
5. **spike-be-parallelism** — NEW (added 2026-05-18 after discovering tests are 100 % serial). Probe `[assembly: Parallelizable(ParallelScope.Fixtures)]`. ½ d. **Next.**
6. **slice-be-parallel-enable** — gated by spike. Apply the attribute + fix the residual isolation issues. 0–2 d. Single biggest wall-clock lever in the catalog.
7. **slice-03b-path-scoped-integrations** — CS-H. 2 d. Local-default-fast + CI −56 % of test-sum on PRs that don't touch connectors.
8. **spike-fe-profile** — diagnose root causes of the 3 slowest FE files. ½ d. Gates the FE refactor slice.
9. **spike-cs-b-setup-split** — measure setup-vs-body in `JiraWriteBackTest`. ½ d. Gates CS-B's 2-3 d slice.

Slices 10+ depend on the spike outcomes and are deliberately TBD.

Each firm slice contains at least one user-visible value story — the developer-as-user observes new CI artifacts (Slice-01) or a written memo (Slice-02). No slice is purely `@infrastructure`.

---

## Wave: DISCUSS / [REF] Outcome KPIs

KPI contracts (appended to `docs/product/kpi-contracts.yaml` once concrete numbers are set in the post-memo slices).

| KPI ID | Title | Target | Status |
|---|---|---|---|
| OUT-test-baseline-published | Per-test timing CSVs available on every PR build (BE + FE) | 100 % of PR builds publish the artifact within 7 days of Slice-01 merging | **Firm** (US-01) |
| OUT-test-alternatives-decided | Alternatives memo reviewed and at least one candidate selected | Memo merged and a candidate slice opened within 14 days of Slice-01 | **Firm** (US-02) |
| OUT-test-speed-backend-local | `dotnet test` local wall-clock | TBD — set in the chosen candidate's slice brief, calibrated to Slice-01 numbers | **Deferred** |
| OUT-test-speed-frontend-local | `pnpm test` local wall-clock | TBD — set in the chosen candidate's slice brief, calibrated to Slice-01 numbers | **Deferred** |
| OUT-test-speed-ci-backend | PR CI backend job wall-clock | TBD — set in the chosen candidate's slice brief | **Deferred** |
| OUT-test-coverage-invariant | API-coverage invariant green-rate (if CS-A or CS-C wins) | 100 % on release-tag pipelines | **Conditional on selected candidate** |
| OUT-test-mutation-kill-rate | Per-feature mutation kill rate | ≥ 80 % | **Firm** (CLAUDE.md gate) |

All KPIs are `per_instance` (local dev) or `vendor_demo_only` (CI runs on the LetPeopleWork GitHub org).

---

## Wave: DISCUSS / [REF] Definition of Ready (9-item gate)

1. **Persona identified**: ✓ `lighthouse-developer`.
2. **JTBD captured**: ✓ `job-dev-test-feedback-velocity` in `docs/product/jobs.yaml`.
3. **Stories sized**: ✓ 2 firm stories (US-01, US-02), each ≤ 1 day. Candidate stories deliberately NOT in DoR yet.
4. **Acceptance criteria testable**: ✓ Every firm AC has a measurable artifact / numeric threshold / binary gate.
5. **Out-of-scope explicit**: ✓ See section above (6 items).
6. **Pre-requisites enumerated**: ✓ See section above.
7. **KPIs measurable**: ✓ Two firm KPIs in this delta; the rest deferred to candidate slices by design.
8. **Cross-cutting concerns surfaced**: ✓ Coverage invariant (D5), mutation kill rate (D9), CI-learnings preservation (in ACs and alternatives memo template).
9. **Handoff target named**: ✓ DESIGN wave is light-touch for US-01 (mechanical reporter wiring). US-02's memo is reviewed by user before any candidate slice opens.

---

## Wave: DISCUSS / [REF] Changed assumptions

None. DISCUSS extends — it does not contradict — any prior wave or SSOT entry. DISCOVER did not run for this feature (it's an internal-quality initiative, not a customer-validated product opportunity); the evidence base is `docs/ci-learnings.md` + git history + ADO #5020 maintainer narrative + the user's 2026-05-17 instruction to keep options open.

---

## Wave: DISCUSS / [REF] Wave-decisions summary

- **Primary job**: `job-dev-test-feedback-velocity`.
- **Walking skeleton scope**: N/A.
- **Feature type**: Cross-cutting (developer experience; backend + frontend test stacks only).
- **Constraints established**: Coverage invariant preserved (D5); mutation kill rate ≥ 80 % preserved (D9); CI-learnings rules preserved.
- **Open by design**: The specific mechanism — cadence-split vs test-rewrite vs cassette-replay vs config-tune — stays open until US-02 memo lands.
- **Upstream changes**: None.
- **Handoff**: DESIGN wave (light-touch, mostly mechanical reporter wiring for US-01).
