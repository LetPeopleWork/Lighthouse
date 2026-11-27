# Taste evaluation: API shape for Lighthouse

**Verdict.** Generation from the published document (#3) leads at **3.75**; keeping the current calls (#1) is second
at **3.45**; a composite read per screen (#4) third at **3.20**; the GraphQL transport (#2) is fifth at **2.60**. First and second place hold under all three weight sets tried (locked, default, developer-tool); the lead is
0.30–0.42. No option falls under the DVF threshold.

Revision 1 (after review round 1): T2 recounted on one basis with a written rubric (#3: 4→3, #6: 4→3); #3's T3
5→4 for the step that carries the document into the clients' repository; that step added to §7. Places 3 and 4
changed (#4 and #6 swapped). Weights unchanged.

## 1. Constraint check (before DVF)

| # | One API surface only | Single-process monolith | Result |
|---|---|---|---|
| 1 | Yes (REST) | Yes | pass |
| 2 | Yes after migration (GraphQL + plain file endpoints, R§3.6); two surfaces during a migration window | Yes (no federation) | pass, migration window is a cost |
| 3 | Yes (REST) | Yes | pass |
| 4 | Yes (REST, more endpoints) | Yes | pass |
| 5 | Yes (no API change) | Yes | pass |
| 6 | Yes (REST) | Yes | pass |

## 2. DVF filter

D = addresses the job (OUT-1, OUT-2 first). F = buildable by one maintainer plus AI agents with today's tools.
V = sustainable to maintain, keeps users' scripts and the clients' release model working.

| # | Option | D | F | V | Total | Kept? | Reason |
|---|---|---|---|---|---|---|---|
| 1 | bare API calls | 2 | 5 | 3 | 10 | yes | OUT-1/OUT-2 unchanged (4 copies per field; clients cast unchecked, Bug #6254); nothing to build |
| 2 | graphql | 4 | 2 | 2 | 8 | yes | Typed end to end, schema snapshot (R§3.1, §4), but new RBAC adapter, DataLoaders, cost limits, error model, TS 7 tooling risk (R§1–§3); rewrite of ~265 actions and ~60 client routes; users' scripts break; departure reports name these exact costs [19][20] |
| 3 | Generated client from the API document | 4 | 4 | 4 | 12 | yes | Removes hand types in frontend and clients, generates zod and mocks, CI diff catches drift (R§7); document already exists; incremental |
| 4 | One read endpoint per page | 2 | 4 | 3 | 9 | yes | Serves call assembly, not OUT-1/OUT-2; endpoint count grows |
| 5 | Consumer-recorded contract tests | 3 | 3 | 2 | 8 | yes | Catches drift (OUT-2) but not copies (OUT-1); PactNet netstandard2.0, last release Mar 2025 [G1]; broker across two repos [G10] |
| 6 | One client package for UI, CLI and MCP | 3 | 3 | 2 | 8 | yes | Two hand clients become one (OUT-1 partly); package must grow ~4× and serve old and new servers [G16][G18] |

Threshold: total < 6 eliminates. None eliminated.

## 3. Weights (locked before scoring)

| Criterion | Weight | Default | Why it moved |
|---|---|---|---|
| DVF (average) | 30 % | 30 % | — |
| T1 Subtraction | 20 % | 20 % | — |
| T2 Concept count | 25 % | 20 % | Very small team (one maintainer plus AI agents): every concept is held by the same person and re-explained to every agent prompt (caller brief, OUT-4) |
| T3 Progressive disclosure | 10 % | 15 % | Read here as "the first adoption step is one small action"; it matters, but less than concepts |
| T4 Speed-as-Trust | 15 % | 15 % | Read here as **contract feedback speed**: how soon a developer learns that a change broke a consumer (compile time 5, CI 4, CI verification step 3, runtime 2, user report 1). Page speed is settled and is not scored (spike) |

Locked 2026-11-27 before any taste score was written. The revision changed scores, not weights.

**Correlation, stated.** D (via OUT-1/OUT-2), T1 (hand copies removed) and T4 (drift feedback) all measure the two
under-served outcomes: about 65 % of the weight. That is deliberate (the job analysis picked them) and applies to every
option alike. T2 is the one criterion that pulls the other way, towards changing nothing.

**T2 counting basis (revision 1).** Count the new things a developer must know to add one field end to end: new
artifacts to keep, new steps to run, new runtime pieces. Tools the frontend already uses (TanStack Query, zod, MSW,
REST) are not new. 5 = none; 4 = one; 3 = two, learnt one after the other; 2 = three or more, some interdependent;
1 = a new mental model.

**T4 rubric.** Compile time in every consumer 5; compile time in the frontend plus a CI step in the clients 4; a CI
verification step only 3; runtime check 2; user report 1.

## 4. Scoring matrix

Handles carried unchanged from options-raw.md.

| # | Option (handle) | name_origin | DVF avg | T1 | T2 | T3 | T4 | Weighted |
|---|---|---|---|---|---|---|---|---|
| 3 | Generated client from the API document | coined | 4.00 | 4 | 3 | 4 | 4 | **3.75** |
| 1 | bare API calls | user-words ("bare API calls") | 3.33 | 2 | 5 | 5 | 2 | **3.45** |
| 4 | One read endpoint per page | coined | 3.00 | 3 | 4 | 4 | 2 | **3.20** |
| 6 | One client package for UI, CLI and MCP | coined | 2.67 | 4 | 3 | 3 | 3 | **3.10** |
| 2 | graphql | user-words ("graphql") | 2.67 | 3 | 1 | 2 | 5 | **2.60** |
| 5 | Consumer-recorded contract tests | coined | 2.67 | 2 | 2 | 3 | 3 | **2.45** |

Arithmetic (DVF×0.30 + T1×0.20 + T2×0.25 + T3×0.10 + T4×0.15):
3: 1.20+0.80+0.75+0.40+0.60 = 3.75 · 1: 1.00+0.40+1.25+0.50+0.30 = 3.45 · 4: 0.90+0.60+1.00+0.40+0.30 = 3.20 ·
6: 0.80+0.80+0.75+0.30+0.45 = 3.10 · 2: 0.80+0.60+0.25+0.20+0.75 = 2.60 · 5: 0.80+0.40+0.50+0.30+0.45 = 2.45

## 5. Score breakdown

| # | T1 Subtraction | T2 Concepts (counted) | T3 First step | T4 Contract feedback |
|---|---|---|---|---|
| 3 | 4: one generator replaces hand TS types, service plumbing, zod schemas and mocks; the hook generation could be dropped without loss of core | 3: (a) the committed document is the contract, with its CI diff; (b) regenerate after a change. Generated zod, hooks and mocks use tools the frontend already has | 4: in this repo one action (commit the document, diff it in CI); the clients' repository needs the document carried over (release asset) before its first route moves | 4: compile time in the frontend; in the clients, a CI step that regenerates from the newest released document and compiles. Not 5: the document can be imprecise (enum shape, nullability) |
| 1 | 2: four hand copies of each shape remain | 5: zero new concepts | 5: nothing to adopt | 2: runtime zod parse where converted; clients cast unchecked (Bug #6254) |
| 6 | 4: removes ~30 frontend service classes; one client left | 3: (a) the frontend calls a package instead of its services; (b) cross-repo versioning and publishing of that package | 3: package publishing and cross-repo setup before the first call moves | 3: UI and clients cannot drift from each other; backend drift caught only when tests run against a real backend (E2E in CI) |
| 4 | 3: removes call assembly in the UI, adds one endpoint per page | 4: (a) a page read model | 4: one page at a time | 2: typing unchanged, same as today |
| 2 | 3: one schema and generated types, but adds DataLoaders, policies per field, cost settings, operation documents, plain file endpoints | 1: a new mental model (schema, resolvers, DataLoader, cost analysis, fragments, codegen, errors in `extensions`) | 2: server, RBAC adapter and codegen pipeline must exist before the first field; coexistence breaks "one surface" | 5: the executed schema is exact; codegen fails at compile time; schema snapshot test |
| 5 | 2: adds pact files, broker and verification; hand types stay | 2: (a) consumer tests on a mock server; (b) pact files; (c) provider states; (d) broker; (e) provider verification | 3: one interaction at a time, after the broker exists | 3: caught in a CI verification step, not at compile time |

## 6. Sensitivity

| Weight set | 3 | 1 | 4 | 6 | 2 | 5 |
|---|---|---|---|---|---|---|
| Locked (30/20/25/10/15) | 3.75 | 3.45 | 3.20 | 3.10 | 2.60 | 2.45 |
| Default (30/20/20/15/15) | 3.80 | 3.45 | 3.20 | 3.10 | 2.65 | 2.50 |
| Developer tool (25/15/20/15/25) | 3.80 | 3.38 | 3.10 | 3.07 | 2.87 | 2.57 |

Lead of #3 over #1: 0.30 / 0.35 / 0.42. Under the locked weights, a one-point change on any single taste criterion
(T1–T4) of #3 moves it by at most 0.25 (T2), and one point on a single DVF lens by 0.10, so neither changes first
place; two points on T2, one point each on T2 and T4, or a full point on the DVF average (three lens points) would
(the last ties it with #1 at 3.45). #4 and #6 are 0.03–0.10 apart; a one-point change in either's T2 swaps them.

The weight change does not decide the ranking. What would move #2 up: a prototype showing T2 at 3 (the concepts
stay inside a small adapter) and F at 4; even then (DVF 2.67→3.33, T2 1→3) it reaches 3.30, still below #3.

## 7. Migration cost and confirming prototype per option

Base sizes: backend ~49 controllers / ~265 actions; frontend ~30 service classes; client package ~60 routes; the
version-gate table (ADR-055).

| # | Migration cost | A small prototype must show |
|---|---|---|
| 3 | Backend: build-time document emission, a committed document with a CI diff; the document also published with each release (release asset) so the clients' repository can take it; attributes only where the document is imprecise (response types, nullability, enum shape); routes untouched. Frontend: ~30 services replaced or thinned boundary by boundary; behaviour-bearing models keep `fromParsed`. Clients: a CI step that fetches the newest released document, regenerates and compiles; ~60 routes switch to generated types/functions against `/v1`. Gates: unchanged rule. Size: medium, incremental, no big bang. | For the Team › Metrics slice and the clients' Delivery list: generated types compile clean under TS 7 + Biome + `pnpm build`; generated zod parses real demo answers (enums, dates, nulls); Bug #6254's reshape, replayed, fails the backend CI diff, the frontend compile, and the clients' CI after regeneration; how the document reaches the clients' repository; hand-written lines removed vs added; bundle delta |
| 1 | None. Continue the zod roadmap as boy-scout work. Gates unchanged. | No prototype. Count hand-written shape copies and drift incidents over the next three Stories that touch the API |
| 6 | Package must grow from ~60 routes to cover the frontend's ~265 actions (or the subset it calls); frontend's ~30 services replaced; release coupling across two repos; the frontend ships with its server, the clients with old servers too. Size: medium-large. | The frontend's Delivery list calling the shared package; how a frontend-only field reaches the package without a clients release; gate behaviour unchanged |
| 4 | Backend: ~10–15 new page reads (metrics pages, Portfolio, Team settings); old endpoints stay for the clients, so the surface grows. Frontend: page hooks swap. Clients: none. Size: small-medium per page. | Team › Metrics as one read: files touched to add a widget vs today |
| 2 | Backend: ~265 actions → types and resolvers in module folders; RBAC policy handler on `IResolverContext`, list filtering on every path, DataLoaders, cost settings; ~4 file endpoints stay plain HTTP. Frontend: ~30 services → operations + codegen; status-code branches → error codes. Clients: ~60 routes → operations; gate table stays and grows (an added field breaks old servers). v1/latest pair → deprecation. Users' scripts and Swagger docs replaced. Size: very large, several Epics, a two-surface window or a big bang. | R's open questions 1–7: code per computed field for a Team slice; RBAC handler keeping 404-style non-disclosure; N+1 for `portfolio { teams { metrics } }`; codegen under TS 7; gate behaviour; Sonar/Stryker on HC generators; cost limits for forecasts |
| 5 | Backend: PactNet provider verification project and provider states. Frontend: pact-js consumer tests for ~30 services. Clients: consumer tests for ~60 routes. A self-hosted broker (bi-directional mode needs hosted PactFlow [G5]). Hand types stay. Size: medium, recurring per interaction. | One consumer pact for the Delivery list; provider verification in `dotnet test` on .NET 11 (PactNet targets netstandard2.0); does it catch Bug #6254's reshape |
