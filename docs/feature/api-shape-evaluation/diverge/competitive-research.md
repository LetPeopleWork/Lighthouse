# Competitive research: how existing tools serve the job

**Verdict.** Three families serve the job "change the contract once, every consumer typed and checked":
(1) **schema-generated consumers**: GraphQL codegen, or OpenAPI generators; (2) **verification without generation**:
Pact consumer-driven contracts; (3) **one shared client**: products whose own dashboard uses their public SDK
(Medusa, Spree). The schema-generated families are the only ones that remove hand-written copies (OUT-1) *and* catch
drift at build time (OUT-2). GraphQL is the only one that also changes the transport, and the extra work it brings
(per-field RBAC, N+1, cost limits, a new error model) is the work that teams who left GraphQL name as the cost.
Gate G2: PASS.

Sources: `../research/research.md` (cited as **R§n** / **[n]**, 42 sources, accessed 2026-11-27) and a narrow
gap search for this wave (cited as **[Gn]**, accessed 2026-11-27, list at the end). Nothing here is a generic market
claim; every row names a product and a source.

## 1. Products and how they serve the job

| Product | Family | What it does well for the job | Where it fails the job | Assumption it makes about the user | Source |
|---|---|---|---|---|---|
| **Hot Chocolate 16** (MIT) + **GraphQL Code Generator** client preset | Schema-generated, new transport | One executed schema; typed documents per operation; schema snapshot test fails on any change; module folders can extend shared types (`[ExtendObjectType]`); MSW mocks by operation name | Per-field RBAC adapter needed (today's guard reads the route); list filtering on every path; DataLoaders for computed fields under lists; cost limits on by default; Sonar does not analyse `.graphql`; an added field makes an old server reject the whole operation, so client gates do not shrink; file transfer stays outside | Data is mostly `IQueryable` entities; the client team benefits from picking fields | R§1–§6, [1][2][4][5][7][8][13][18][25][27][28][29] |
| **orval 8** / **openapi-typescript 7** / **hey-api** / **Kiota** on the existing Swashbuckle 11 document | Schema-generated, same transport | Lighthouse already publishes the document (`/api/docs`); generators emit TS types, zod schemas, TanStack Query hooks and MSW mocks; a CI diff of the document is the contract check Bug #6254 asked for | No field selection; the many-calls shape stays; the v1/latest pair and gates stay; quality of the generated types depends on how exact the document is; hey-api is "in initial development" | The server-side code is the source of truth and its document is accurate | R§7, [33][34][35][36][37] |
| **Page-shaped composite reads** (BFF on a CQRS-lite read side) | Same transport, fewer calls | Fixes the many-calls and chain shape measured in the spike (metrics pages, Portfolio chain) | Typing unchanged; each new page needs a new endpoint; clients do not benefit | The screen, not the entity, is the right unit of a read | R§7, spike findings |
| **OData** (`Microsoft.AspNetCore.OData` 9.5) | Query language on REST | `$select`/`$expand`/`$filter` on entities | net11 not listed; strengths assume `IQueryable` entities, not computed metrics/forecasts; TS typing weak | The data is entity tables | R§7, [38][39] |
| **Pact** (PactNet 5.0.1, pact-js 17.1.4, Pact Broker / PactFlow) | **Non-obvious: verification, no generation** | Catches a provider change that breaks what a consumer recorded; PactFlow's bi-directional mode compares consumer pacts with the provider's OpenAPI document | No generated types (expectations hand-written per interaction, each with upkeep cost); PactNet targets netstandard2.0 only, last release Mar 2025; bi-directional mode needs hosted PactFlow (free plan: 2 integrations); a broker is "too much infrastructure" for services that deploy together; steep learning curve | Separate teams own consumer and provider and deploy independently | [G1]–[G14] |
| **Medusa** (`@medusajs/js-sdk`), **Spree** (`@spree/admin-sdk`) | **Non-obvious: one shared client** | Their own admin dashboard calls the same SDK that external integrators use, so the dashboard exercises the public client every day | Release coupling: Medusa's dashboard pins the shared TanStack version; Spree ties code to the app version. No documented case of a web app plus CLI/MCP on one package | The external clients and the UI need the same calls | [G16][G17][G18] |
| **TypeSpec** (Microsoft) | Contract written first, both sides generated | One contract language emitting OpenAPI | C# server emitter is `0.58.0-alpha`; TS client emitter pre-1.0 | The team will author the contract before the code | [G20][G21][G23][G24] |

## 2. What teams that moved report

| Report | Direction | What it says for this job | Source |
|---|---|---|---|
| Brito & Valente 2020 (22 students) | — | GraphQL: less effort to write client queries (9 vs 6 min median) | [40] |
| sennder 2019 | Left GraphQL | Left for "query performance and access control"; accepted more endpoints and code | [20] |
| Bessey 2024 | Left GraphQL | Field-level auth, cost limits, N+1, "business logic is forced into the transport layer"; recommends OpenAPI with generated types for ≤3 controlled clients | [19] |
| Courier | Hybrid | GraphQL for own frontends, REST for the customer-facing API | [41] |
| Speakeasy (sells OpenAPI tooling; biased) | — | Pact overhead outweighs its value for most teams | [G15] |

No dated case of a small single-process monolith team moving to GraphQL for developer experience was found (R§8 gap).
Lighthouse has ≤3 controlled clients (frontend, CLI, MCP) plus users' scripts: Bessey's threshold case.

## 3. Repo facts that decide fit

| Fact | Consequence | Source |
|---|---|---|
| OpenAPI already generated (Swashbuckle 11), linked to users as "the API documentation" | Generation needs no new protocol; GraphQL would replace what users script against | R§0 |
| Enums serialise as strings out and accept ints in | The document must describe the real shape, or generated types lie; a prototype must check this | maintainer's working notes (not re-verified here; the prototype checks it) |
| Behaviour-bearing models (`Feature`, `Team`, `Portfolio`) are built from parsed data with `fromParsed` | Any generated type replaces the DTO and the deserializer, not the model class | zod adoption roadmap |
| `RbacGuardAttribute` reads the unit id from the route; denied read = 404 | Kept as is by every REST option; rebuilt for GraphQL | R§2.1 |
| Clients' version gate: new endpoint → gate, additive field → no gate (ADR-055) | REST options keep the rule; GraphQL makes additive fields gate-worthy | R§3.4 |
| Rule: backend acceptance scenarios drive the application-service port | Tests are mostly transport-independent; transport choice changes only adapter-edge tests | CLAUDE.md, R§4 |

## 4. Gaps left open

- HC on a strict port layer with computed DTOs (R gap); GraphQL codegen and gql.tada under TypeScript 7 (R gap).
- PactNet maintenance status beyond the release date; Pact Broker licence name ([G] gaps).
- No real case of one SDK shared by a web app and a CLI/MCP pair ([G] gap).

## Gate G2

| Check | Result |
|---|---|
| 3+ real products named | PASS (Hot Chocolate, GraphQL Code Generator, orval, openapi-typescript, hey-api, Kiota, OData, Pact/PactFlow, Medusa, Spree, TypeSpec) |
| Non-obvious alternative (different category, same job) | PASS: Pact (testing tool, no API change) and Medusa/Spree's shared SDK (code sharing) |
| No generic market claims | PASS (survey figure in R§8 is not used as an argument) |

## Gap sources (accessed 2026-11-27)

[G1] https://www.nuget.org/packages/PactNet · [G2] https://github.com/pact-foundation/pact-net/releases ·
[G3] https://github.com/pact-foundation/pact-js/releases · [G4] https://docs.pact.io/getting_started/sharing_pacts ·
[G5] https://pactflow.io/pricing · [G6] https://pactflow.io/bi-directional-contract-testing/ ·
[G7] https://support.smartbear.com/pactflow-on-premises/docs/en/user-guide/contract-testing/bi-directional-contract-testing/overview.html ·
[G8] https://docs.pact.io/faq · [G9] https://support.smartbear.com/api-hub/contract-testing/docs/en/user-guide/contract-testing/bi-directional-contract-testing/provider.html ·
[G10] https://dev.to/pcg/consumer-driven-contract-tests-lessons-learned-3hmj ·
[G11] https://dev.to/deepaksatyam/pact-is-great-its-also-why-most-teams-dont-do-contract-testing-5c14 ·
[G12] https://pactflow.io/why-pact-implementations-fail-and-what-you-can-do-to-avoid-it/ ·
[G13] https://aaltodoc.aalto.fi/server/api/core/bitstreams/e035e9e7-b7a8-43c2-8c37-8020ae36dfee/content ·
[G14] https://playbook.microsoft.com/code-with-engineering/automated-testing/cdc-testing/ ·
[G15] https://www.speakeasy.com/blog/pact-vs-openapi · [G16] https://docs.medusajs.com/learn/fundamentals/admin/tips ·
[G17] https://docs.medusajs.com/resources/js-sdk · [G18] https://spreecommerce.org/docs/developer/dashboard/public-api ·
[G20] https://registry.npmjs.org/@typespec/http-server-csharp/latest · [G21] https://registry.npmjs.org/@typespec/http-client-js/latest ·
[G23] https://typespec.io/release-notes/typespec-1-16-0/ · [G24] https://typespec.io/docs/emitters/clients/http-client-js/reference/
