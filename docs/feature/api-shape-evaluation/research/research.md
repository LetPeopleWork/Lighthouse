# Research: GraphQL instead of REST for Lighthouse, for easier development

**Date**: 2026-11-27 | **Researcher**: nw-researcher (Nova) | **Step**: 2 of the API-shape evaluation (research only, no recommendation)
**Inputs**: `docs/feature/api-shape-evaluation/spike/findings.md` (speed settled), maintainer framing (one API surface only; module
separation and federation are nice-to-haves; speed is not a driver).
**Jev**: not used. `"jev": "unauthorized"` holds for this project (CLAUDE.md, "How DELIVER runs on nWave 4").

How to read the confidence marks: **High** = primary docs or the repo say it directly, or 3+ independent sources agree.
**Medium** = 2 sources, or one primary source plus a repo fact. **Low** = one source, anecdote, or my inference.
Text marked *Analysis* is my interpretation, not a sourced fact.

---

## Summary

| # | Question | What the evidence says | Confidence |
|---|---|---|---|
| 1 | GraphQL on .NET | Hot Chocolate 16 (MIT, stable 16.7.7 of 2026-11-21, NuGet lists net11.0 as compatible) is the mature choice; GraphQL.NET 8.8.5 (MIT) is alive but slower-moving. HC's EF Core features (projections, filtering, paging) assume resolvers hand it an `IQueryable`; the v16 way is a `QueryContext<T>` passed **into the service layer**. Lighthouse's reads are mostly computed DTOs (metrics, forecasts), so those features would buy little and the "pass a GraphQL selection into the service" pattern cuts across the ports. DataLoaders are needed wherever a computed field is nested under a list. | High (library facts), Medium (fit) |
| 2 | Authorization, security | HC uses ASP.NET Core policies, so API keys and JWT keep working. Field/type `@authorize` exists. Lighthouse's `RbacGuardAttribute` reads the unit id from the **route**; in GraphQL the id comes from an argument or the parent object, so every unit-scoped field needs a new policy handler. HC 16 enables cost analysis by default; trusted documents are supported but would block third-party scripts that the in-app Swagger page invites today. Read-denied = 404 today becomes `null` + error in GraphQL. | High (HC docs), Medium (mapping) |
| 3 | Developer experience | End-to-end typing is real (GraphQL Code Generator client preset, TanStack Query works with it, no normalised cache). gql.tada depends on a TS language-service plugin; Lighthouse is on TypeScript 7, which ships **no plugin API yet** (expected 7.1) — so codegen, not gql.tada, is the realistic path today. "Versionless" evolution does not remove our client version gates: an unknown field makes an old server reject the **whole** operation, which is stricter than today's additive-field rule. File upload/download stays outside GraphQL. SignalR could stay or become a subscription. | High (tool docs), Medium (gate analysis) |
| 4 | Testing, quality | HC tests run on an `IRequestExecutor` without a host, plus schema snapshot tests; host tests over HTTP also work. Stryker.NET mutates resolvers as plain C# (no GraphQL-specific evidence either way). **SonarCloud does not analyse `.graphql` files.** MSW mocks GraphQL by operation name with typed documents. | High (Sonar, MSW), Medium (HC testing), Low (Stryker) |
| 5 | Module-owned schema | HC merges `[QueryType]`/`[ExtendObjectType]` partial classes from many files into one schema at build time, so each module can contribute its own root fields and extend shared types inside one process. Nothing in HC enforces the module boundary; ArchUnitNET still would. | Medium |
| 6 | Clients (CLI, MCP) | A typed GraphQL client in TS is straightforward. HC 16 ships an MCP adapter that turns stored `.graphql` operations into MCP tools; Apollo's MCP server does the same and warns against letting agents run arbitrary operations. The existing hand-written client (3,395 lines, ~60 `/v1` routes, its own version-gate table) would be rewritten either way. | Medium |
| 7 | Alternatives | The repo **already** emits OpenAPI (Swashbuckle 11, `/api/docs`). Generators that produce TS types, zod schemas, TanStack Query hooks and MSW mocks from it are mature (openapi-typescript 7, orval 8, hey-api — the last still "initial development"). This fixes the hand-typed contract and drift bug class (Bug #6254) without a new protocol, but not field selection or the many-calls shape. Page-shaped read endpoints fix the many-calls shape on the CQRS-lite read side. OData 9.5 targets net8.0 with computed net9/10 compatibility only, and its strengths assume `IQueryable` entities. | High |
| 8 | Teams that moved | Evidence is mostly experience reports, not data. Recurring costs named by teams that left GraphQL: field-level authorisation, N+1/performance, query cost, business logic pulled into the transport layer. Recurring benefit: less client effort per query (one controlled experiment, students, 9 vs 6 min median). No dated case study of a small monolith team moving **to** GraphQL for developer experience was found. | Low–Medium |

**What only a prototype can settle** is listed at the end. The largest unknowns are: how much code per field
GraphQL costs when every field is a computed service call; whether a per-unit RBAC handler on `IResolverContext`
stays readable; and whether codegen + version gates are simpler than today's 30 service classes plus zod.

---

## 0. Current stack, verified in the repo

| Fact | Evidence |
|---|---|
| .NET 11 backend | `Lighthouse.Backend/Lighthouse.Backend/Lighthouse.Backend.csproj:4` (`net11.0`) |
| 49 controller classes under `API/` (incl. 3 embed controllers that are not under `/api`) | `ctx_search "class \w+Controller\b"`, e.g. `API/TeamController.cs:30`, `API/EmbedEntryController.cs:19`. Action counts (~265, ~169 GETs) are from the task brief, not recounted. |
| Every controller carries two routes, `api/v1/...` and `api/latest/...` | e.g. `API/ApiKeyController.cs:13-14`, `API/DeliveriesController.cs:17-18` |
| OpenAPI already generated (Swashbuckle 11.0.1), Swagger UI at `/api/docs`, a `LatestRouteFilter` | `Lighthouse.Backend.csproj:65`; `Program.cs:169-174`, `Program.cs:305-309` |
| Users are pointed at that Swagger page as "the API documentation" | `docs/settings/systeminfo.md:17` |
| A GraphQL **client** is already in the backend (for the Linear connector) | `Lighthouse.Backend.csproj:34-35` (`GraphQL.Client` 6.1.0) |
| SignalR hub `api/updateNotificationHub`, Redis backplane optional | `Program.cs:212`, `Program.cs:315-325`; `Platform/UpdateQueue/UpdateQueueService.cs:824` |
| No HTTP response caching on API reads today (only `no-store` / `no-cache`) | `API/UsageDataController.cs:52`, `Program.cs:186`; no `ResponseCache`/`ETag`/`OutputCache` found |
| File transfer: zip backup download, log download, backup restore upload, licence upload | `API/DatabaseManagementController.cs:48,58`, `API/LogsController.cs:108`, `API/LicenseController.cs:51` |
| Printed reports are a browser route, not a server PDF | `docs/product/architecture/adr-245-a-printed-report-is-its-own-chrome-less-route-that-prints-once-its-fresh-answer-is-ready.md` (title) |
| RBAC on HTTP: `RbacGuardAttribute` (an MVC `IAsyncAuthorizationFilter`) reads the unit id from a **route value**, calls `IRbacAdministrationService.CanSatisfyRequirementAsync`, answers 404 for a denied read and 403 for a denied write | `Modules/Access/Adapters/Http/RbacGuardAttribute.cs:10,28-63,84-109` |
| Modules already exist as folders | `Modules/{Access,FlowMetrics,Forecasting,Licensing,OrganisationSetup,Promises,Refinement,Reports,TrackerIntegration,WorkItems}` |
| Modular monolith, single process, single writer; MediatR rejected on **licence** grounds | `adr-027-…:24,41,44-46` |
| Frontend: TanStack Query 5, axios, zod 4, TypeScript 7, Vitest 5, `@microsoft/signalr` 11 | `Lighthouse.Frontend/package.json:29,38,40,51,75,77` |
| 67 files in `src/services/Api` (≈30 services + tests + guards) | `ctx_glob Lighthouse.Frontend/src/services/Api/*.ts` |
| zod adopted per boundary through `BaseApiService.parse` | `docs/feature/zod-adoption-evaluation/adoption-roadmap.md` (steps 0–7) |
| Clients: one hand-written TS client, 3,395 lines, ~60 `/v1/` route literals, hand-typed responses | `/storage/repos/lighthouse-clients/packages/client/src/index.ts` (e.g. `:2270`, `:2338`, `:3014`) |
| Clients' version gate: `FEATURE_REQUIRES_SERVER_NEWER_THAN`, checked against `/v1/version/current` | `lighthouse-clients/packages/client/src/index.ts:2191-2211,2290-2312`; rule stated in `adr-055-…:17` (new endpoint → gate; additive field → no gate) |
| A real contract-drift bug: server reshaped a `/v1` answer in place; client cast it unchecked; CLI and MCP degraded silently | `docs/evolution/2026-10-10-bug-6254-deliveries-active-archived.md:33-40,179-182` |

---

## 1. GraphQL on .NET today

### 1.1 Libraries

| | Hot Chocolate (ChilliCream) | GraphQL.NET |
|---|---|---|
| Current stable | 16.7.7, 2026-11-21; 16.8 previews daily [1] | 8.8.5, 2026-08-18 [3] |
| Major cadence | v16 announced 2026-05-11, "first new major release … in more than a year" [2] | 8.8.x since Dec 2025 [3] |
| Targets | NuGet lists net8.0–net11.0 as compatible [1] (whether net11.0 is a native target or computed is not shown — check in a prototype) | net5.0 / netstandard2.0 primary; newer frameworks computed [3] |
| Licence | MIT [1] | MIT [3] |
| Downloads | 72.5 M [1] | 103.2 M [3] |
| Notable v16 items | batch resolvers, `QueryContext<T>`, `@requiresOptIn` feature lifecycle, MCP and OpenAPI adapters, new `onError` mode, breaking scalar redesign [2] | — |

**Confidence**: High (NuGet + vendor blog). *Analysis*: ADR-027 rejected MediatR for a licence change; HC is MIT today,
and ChilliCream's docs route MCP-tool publishing through its separate Nitro product, with a self-implemented
`IMcpStorage` as the alternative [6][31]. Nitro's licence terms were not checked here (gap); an adoption should
not depend on it without that check.

### 1.2 EF Core, projections, filtering, paging

- v16 recommends returning/using a `QueryContext<T>`; the resolver or **a service** calls `.With(query)` on an
  `IQueryable`, which applies projection, filtering and sorting. The docs show a `ProductService` taking
  `QueryContext<Product>?` so it can be called "from places that don't have a GraphQL context" [4].
- Projections need public setters; fields a DataLoader needs must be forced in with `.Include()`; do not mix
  `QueryContext<T>` with `[UseProjection]` (analyzer HC0099) [4].
- **Fit with ports-and-adapters (*Analysis*, Medium)**: the HC-native data path pushes a transport-shaped object (the
  client's selection) into the application service and expects an EF `IQueryable` there. Lighthouse controllers call
  application services and query ports (`IReportQueries`, `IRefinementViewQuery`, `IFeatureListService`, seen in
  `API/*Controller.cs` constructors), and most read models are **computed** (metrics in `BaseMetricsService` with an
  in-memory cache, forecasts from a simulation — ADR-027 D6). Projection/filtering helps for entity lists (Teams,
  Portfolios, Features, Work Items); it does not help for computed DTOs, where each GraphQL field would be a resolver
  calling a service method. No source was found that describes HC on top of a strict service/port layer without
  `IQueryable`; that is a gap (see Gaps).

### 1.3 N+1 and DataLoader

- DataLoaders batch key lookups per request; "Both the DataLoader instance and its cache live for exactly one request";
  source-generated with `[DataLoader]`; DbContext scoping via `DataLoaderScope` [5].
- v16 batch resolvers receive all parents at once, as an alternative; "DataLoaders are still the right tool when you
  want batching to live in your application layer" [2].
- Two teams that left GraphQL name N+1 as a main cost: "the N+1 problem is hard to avoid, especially with ORMs" [20];
  dataloaders must be added "just in case" a client nests a field in a list, and auth checks add further N+1 [19].
- *Analysis*: in Lighthouse, a query like `teams { metrics { throughput } }` would call the per-team metrics service
  once per team unless a batch API exists. Today the UI already makes one call per widget (spike: 27 parallel calls),
  so the work is not new, but it moves from the browser into a resolver tree.

**Confidence**: High (mechanics), Medium (Lighthouse impact).

---

## 2. Authorization and security controls

### 2.1 Authorization model

| Topic | Hot Chocolate fact | Source |
|---|---|---|
| Declaring rules | `@authorize` on types and fields; a field rule overrides the type rule; roles = any-of; several policies = all-of | [7] |
| Engine | ASP.NET Core roles, policies, `AuthorizationHandler<T>`; must use `HotChocolate.Authorization.AuthorizeAttribute`, not the MVC one | [7] |
| Resource | Handler resource is the `IResolverContext` (arguments, context data); access to the resolved parent object is not documented on that page | [7] |
| Denied field | field becomes `null` with a GraphQL error (`AUTH_NOT_AUTHENTICATED`); endpoint-wide `RequireAuthorization()` gives HTTP 401 instead | [7] |
| Authentication | any ASP.NET Core scheme; so Lighthouse's API keys, cookies and the JWT bearer for MCP OAuth (ADR-079) stay as they are | [7]; `adr-079-api-jwt-bearer-for-mcp-oauth.md` |

**Mapping onto Lighthouse RBAC (*Analysis*, Medium).**
- `RbacGuardAttribute` is an MVC filter and resolves the unit id from a route key (`RbacGuardAttribute.cs:84-109`).
  In GraphQL there is one route; the id arrives as an argument (`team(id: 7)`) or comes from the parent object
  (`portfolio { teams { … } }`). Each unit-scoped field needs a policy whose handler reads the id from
  `IResolverContext` (argument or parent) and calls the same `IRbacAdministrationService`. The service stays; the
  adapter is new.
- Lists need **filtering**, not just guarding: a `teams` field must return only the teams the caller may read. Today
  that lives in the list endpoints' service calls; in GraphQL every path that reaches a Team (top level, via a
  Portfolio, via a Feature) must apply it. This is the "authorise every field in context" cost named in [19] and [20].
- Today a denied read returns **404** on purpose (non-disclosure, `RbacGuardAttribute.cs:58-60`). The GraphQL
  equivalent is a `null` field with an error, or a filtered list; the error code must not reveal existence. A
  prototype has to show this.
- ADR-001/CLAUDE.md: UI gating derives from `useRbac()` and `/authorization/my-summary`. That does not change with
  the transport.

### 2.2 Abuse controls

| Control | Hot Chocolate 16 | Source |
|---|---|---|
| Cost analysis | **on by default** (`EnforceCostLimits = true`), `MaxFieldCost = 1 000`, `MaxTypeCost = 10 000`; paging boundaries not required by default | [8] |
| Execution timeout | 30 s default | [9] |
| Depth / cycles | field-cycle depth rule on by default outside development; explicit `AddMaxExecutionDepthRule(n)` available | [9] |
| Parser limits | e.g. `MaxAllowedFields` 2 048 | [9] |
| Introspection | can be turned off (`AllowIntrospection(false)`), or allowed per request via an interceptor; the docs do not state the production default | [10] |
| Trusted documents | operations extracted at build time; `OnlyAllowPersistedDocuments = true` blocks anything else; per-request override possible; storage: file system, Redis, Azure Blob | [11], [12] |

- GraphQL Code Generator's client preset can emit `persisted-documents.json` (default hash **sha256**) [13];
  HC's default document hash is **MD5** [11]. One side must be reconfigured (a small, real friction).
- Bessey: a 128-byte introspection query took ~10 s CPU; a crafted invalid query gave ~2 000× memory amplification;
  persisted queries are no option for customer-facing APIs [19]. Low confidence on the numbers (single author,
  different stack), but the class of risk is confirmed by the existence of HC's own defaults [8][9].
- *Analysis*: Lighthouse's API is used by its own UI and clients **and** by users' own scripts (API keys for all users;
  Swagger linked from System Info, `docs/settings/systeminfo.md:17`). Trusted-documents-only mode would break those
  scripts; leaving arbitrary operations open makes cost analysis the main defence and needs per-field cost numbers for
  expensive computed fields (forecasts, Monte-Carlo runs). ADR-005 (rate limiting middleware) counts requests, which
  under GraphQL no longer equals work.

**Confidence**: High (HC docs), Medium (Lighthouse consequences).

---

## 3. Developer experience (the main driver)

### 3.1 End-to-end typing

| Tool | What it does | Source | Fit with Lighthouse |
|---|---|---|---|
| GraphQL Code Generator, `client` preset | `graphql()` function, `TypedDocumentNode<Result, Variables>`, fragment masking on by default, watch mode, optional persisted documents, `documentMode: string` to cut bundle size | [13], [14] | Works with TanStack Query via `graphql-request` [14][15] |
| gql.tada | types inferred inside TypeScript, no generated per-operation files; needs a `.d.ts` introspection file and a TS language-service plugin (`gql.tada/ts-plugin` from TS 5.5) | [16] | **Risk**: Lighthouse uses TypeScript 7 (`package.json:75`). TS 7.0 (2026-07-08) "does not ship with an API"; a new API is expected in 7.1 [17]. No gql.tada statement on TS 7 found. *Analysis*: editor hints likely unavailable until then; type inference itself is plain TS types and may still work under `tsc`. Unverified. |

- Runtime validation: GraphQL guarantees the response matches the schema the server ran, and codegen types match the
  schema the client was built against. *Analysis*: that is the same guarantee zod gives today at each boundary, but
  for free per operation; zod could be dropped at GraphQL boundaries, or kept only for scalars like dates.
- Without a schema-based tool, Lighthouse writes types by hand in two places (frontend services, clients' `index.ts`),
  and Bug #6254 shows the cost: an in-place reshape on `/v1` went unnoticed because the client "casts the parsed answer
  to that type without checking it" (`docs/evolution/2026-10-10-bug-6254-…:35-40`). Any schema-generated client
  (GraphQL or OpenAPI, §7) addresses that class.

### 3.2 Client library and caching

| Client | Caching | Size (rough, inconsistent sources) | Source |
|---|---|---|---|
| TanStack Query + graphql-request | document cache by query key; "React Query does not support normalized caching" | graphql-request ≈ 8 kB (third-party figure) | [15], [21] |
| urql | document cache; optional normalised Graphcache | ≈ 10–15 kB gz | [21] |
| Apollo Client | normalised cache | ≈ 30–40 kB gz | [21] |
| Relay | normalised, compiler-driven, fragments-first | not measured here | — |

*Analysis*: Lighthouse already uses TanStack Query for half its data hooks; keeping it avoids a second cache model.
Bundle size matters here because the spike found JavaScript bytes dominate slow loads.

### 3.3 Adding a field or a screen

| Change | REST today (repo) | GraphQL (*Analysis*, from [4][13]) |
|---|---|---|
| Add a field to an existing answer | DTO property → (zod schema) → TS type/model → use; clients: hand type; no version gate (ADR-055 rule) | resolver/property → schema regenerates → codegen → select it in the operation. Clients: **gate needed** if they select it, because an old server rejects the operation (see 3.4) |
| New widget or screen | new action (+ `RbacGuard`, cache key, route pair v1/latest) → service method → TS service → hook; clients: new method + gate | new field on a type, policy on it → operation in the component → codegen. Clients: new operation + gate |
| Remove or rename | in place on `/v1` breaks old clients silently (Bug #6254) | `@deprecated` field kept until clients move; tools can diff schemas (schema snapshot test, §4) |

The work that does **not** shrink: service/query code, RBAC, caching of computed metrics, the version gate.
The work that shrinks: hand-written TS types and service classes, route plumbing, duplicate fetches for overlapping
data (the UI picks fields).

### 3.4 Schema evolution vs versioned routes and client gates

- GraphQL "takes a strong opinion on avoiding versioning"; new fields and types are added without breaking clients,
  "a versionless API" [18].
- HC 16 adds an opt-in feature lifecycle (`@requiresOptIn`) [2].
- *Analysis, Medium*: "versionless" protects **old clients against a new server**. Lighthouse's gate protects **new
  clients against an old self-hosted server** (`index.ts:2183-2190`). In GraphQL, a document that names a field the
  server does not know fails **validation as a whole**; today an additive field is simply missing and the client copes
  (the `staleness` entry "refuses nothing", `index.ts:2207-2209`). So the gate table stays, and may need more entries,
  unless clients introspect the server's schema at start-up and pick operations accordingly. The `/v1` + `/latest`
  route pair would go; deprecation would replace it.

### 3.5 Errors

- With `application/graphql-response+json`, a response with `data` is 2xx even with errors; validation failure is
  typically 400; the spec is still a draft [22].
- *Analysis*: Lighthouse maps exceptions to 409 (`ConcurrencyConflictExceptionFilter`) and to an archived-delivery
  refusal (`DeliveryArchivedExceptionFilter`) (`Program.cs:290-291`), and `ResponseShapeGuard`/`ApiError` read HTTP
  status on the frontend. In GraphQL these become typed error codes in `errors[].extensions` (or result union types),
  and every client branch on status codes must change.

### 3.6 Files and real-time

- graphql.org: "GraphQL was not designed with file uploads in mind" and recommends keeping binary transfer outside
  GraphQL [23]. *Analysis*: the four file endpoints in §0 stay plain HTTP endpoints. That is a small, permanent
  second surface; it does not contradict "one API" if treated as file transport rather than API.
- HC subscriptions run over WebSocket (graphql-ws) or SSE; in-memory pub/sub by default, Redis/Postgres/NATS for
  several instances; publish with `ITopicEventSender` [24]. Today SignalR sends one global "something updated"
  notification (`UpdateQueueService.cs:824`) with an optional Redis backplane (ADR-075). Either can carry it; no source
  found that one is simpler for this use.

**Confidence**: High for tool facts; Medium for the Lighthouse-specific change cost.

### 3.7 HTTP caching

- GraphQL POST is not HTTP-cacheable; GET is allowed for queries and persisted document ids make GET URLs short [22][12].
- Repo: no API read sets cache headers today (§0). *Analysis*: GraphQL loses nothing Lighthouse uses now.

---

## 4. Testing and quality tooling

| Topic | Evidence | Confidence |
|---|---|---|
| Server tests without a host | HC tests start from an `IRequestExecutor` built from a `ServiceCollection` "without the ASP.NET Core host"; CookieCrumble snapshots; `executor.Schema.MatchSnapshot()` fails on any schema change [25] | Medium (docs page found via search; direct fetch 404) |
| Host tests | No HC doc on `WebApplicationFactory` found; GraphQL is a normal POST endpoint, so host tests work as today (*Analysis*) | Low |
| Lighthouse fit | CLAUDE.md: new backend acceptance scenarios drive the **application-service port**, not the host. *Analysis*: that rule makes most tests transport-independent; GraphQL would add executor/schema tests at the adapter edge, replacing controller tests | Medium |
| Stryker.NET | Mutates C# syntax; resolvers are plain C#. Known Stryker issues with generated code exist (OpenAPI-generated DTOs; GeneratedRegex) [26]; HC uses source generators. No GraphQL-specific report found. | Low |
| SonarCloud | Supported-language list has 47 languages; **GraphQL is not among them** [27]. `.graphql` operation files and the SDL get no Sonar analysis; C# resolvers and TS code do. | High |
| Frontend mocks | MSW `graphql.link(url).query('Name', …)`; accepts a `TypedDocumentNode` and infers types; subscriptions only over WebSocket [28] | High |

---

## 5. Module-owned schema inside one process

- HC merges type extensions into the original type at schema build time; with the source generator, `[QueryType]`,
  `[MutationType]`, `[SubscriptionType]` may sit on several partial classes; `[ExtendObjectType<Book>]` adds fields to
  a type owned elsewhere; without the generator, `[ExtendObjectType(typeof(Query))]` + `AddTypeExtension<T>()` [29].
- A non-v16 page says query classes can be split across assemblies when each uses the generator [29]; not confirmed
  for v16. Lighthouse is one assembly by decision (ADR-027 D5), so this does not matter.
- *Analysis, Medium*: each `Modules/<X>/Adapters/GraphQL/` folder could hold its own root fields and extend `Team`
  or `Portfolio` with its fields (e.g. FlowMetrics adds `Team.metrics`, Forecasting adds `Team.forecast`). That
  mirrors the module map in ADR-270 well. HC enforces nothing about who may extend what; the boundary is kept by the
  existing ArchUnitNET rules. Cross-module fields on a shared type are where coupling would show; a prototype should
  try one.
- Federation (Fusion) is out of scope per the framing; HC 16's Fusion runs as a separate gateway app [2].

---

## 6. The clients (CLI and MCP server)

- Today: one hand-written client, ~60 `/v1` route literals, hand types, its own version-gate table
  (`lighthouse-clients/packages/client/src/index.ts:2191-2312`). MCP tools are curated, e.g. reports as "four
  read-only tools that return the reports' raw values" (ADR-247 title); `@modelcontextprotocol/sdk` 1.26–1.29 and
  zod 4 in `packages/mcp-*` (`package.json` files).
- A GraphQL client in a Node CLI is the same codegen + `graphql-request` stack as in the browser [14].
- **MCP mapping**: HC 16 has an MCP adapter (`AddMcp()`, `MapGraphQLMcp()`, Streamable HTTP at `/graphql/mcp`);
  each tool is a stored `.graphql` operation whose variables become tool arguments; tools come from an `IMcpStorage`,
  normally filled by ChilliCream's Nitro, otherwise self-implemented [30][31]. How caller credentials reach the
  execution is not described [31]. Apollo's MCP server works the same way and says to "Limit `execute` and mutation
  access to use-cases that need dynamic operations" [32].
- *Analysis, Medium*: curated MCP tools map 1:1 to named GraphQL operations, which fits Lighthouse's current design of
  a small set of reviewed tools. Moving the MCP server **into** the Lighthouse process (HC adapter) would be a separate
  decision: it changes the clients' release model and the consent and OAuth flow (ADR-228, ADR-079). Keeping the
  external MCP server and just switching its transport is the smaller change.
- Migration size: every one of the ~60 client calls is rewritten as an operation; the version-gate mechanism stays
  (§3.4). Estimate belongs in the comparison step.

---

## 7. Alternatives that target the same gains without GraphQL

| Option | Maturity on .NET 11 / TS | Would fix | Would not fix |
|---|---|---|---|
| **OpenAPI-first generation** from the existing document | ASP.NET Core 10+ has built-in `Microsoft.AspNetCore.OpenApi` for controllers and build-time generation via `Microsoft.Extensions.ApiDescription.Server`; docs current for .NET 11 [33]. Repo already runs Swashbuckle 11 (`csproj:65`). TS side: openapi-typescript 7 (types only, "runtime-free", OAS 3.0/3.1) [34]; orval v8 (TanStack Query hooks, zod, MSW handlers with Faker data) [35]; hey-api (20+ plugins, "in initial development", pin exact version) [36]; Kiota (Microsoft, TS among its languages; own runtime) [37]; NSwag not researched (gap). | Hand-written TS types and services in frontend **and** clients; drift like Bug #6254 (a CI diff of the generated document is the "contract check" the bug record asks for, `…bug-6254…:179-180`); zod schemas generated rather than written; MSW mocks generated | Field selection; many calls per page; the `/v1`+`/latest` pair and the gates stay as they are |
| **Page-shaped composite reads** (BFF on the CQRS-lite read side) | Plain controllers + query services; ADR-027 D6 already names a read side | The many-calls and chain shape the spike measured (metrics pages, Portfolio chain) | Typing (unless combined with OpenAPI generation); each new page needs a new endpoint |
| **OData** (`Microsoft.AspNetCore.OData`) | 9.5.0 (2026-07-14) targets net8.0, net9/10 computed, net11 not listed; 10.0 in preview since 2025 [38][39] | `$select`/`$expand`/`$filter` on `IQueryable` entities | Computed DTOs (metrics, forecasts); TS typing (generators target .NET mostly); module ownership |

*Analysis*: OpenAPI generation and composite reads are independent of each other and can be combined; together
they target the same two pains (hand-typed contract, many calls) that GraphQL targets, without field selection.

---

## 8. Evidence from teams that moved

| Report | Kind | Direction | What they say | Source |
|---|---|---|---|---|
| Brito & Valente, "REST vs GraphQL: A Controlled Experiment" (2020) | Experiment, 22 students, 8 queries | — | GraphQL needed less effort to **write client queries**: 9 vs 6 min median; gap grows with complex REST endpoints | [40] |
| sennder (2019, after ~3 years) | Company blog | GraphQL → command/query endpoints | Dropped GraphQL for "Query performance and access control"; accepted "an increased number of endpoints and lines of code" | [20] |
| M. Bessey, "Why, after 6 years, I'm over GraphQL" (2024) | Individual, SaaS | Away from GraphQL | Field-level auth, cost/rate limiting, parser attacks, N+1, "business logic is forced into the transport layer"; recommends OpenAPI with generated types when you control ≤3 clients | [19] |
| Courier | Vendor blog | Hybrid | Kept GraphQL for own frontends, REST for customer-facing API | [41] |
| Postman State of the API 2023 | Survey | — | REST 86 %, GraphQL 29 % of respondents (reported second-hand; 2025 figure not found) | [42] (secondary) |

- All movement reports are anecdotes; the one experiment measures client-query effort only, with students.
- No dated case study was found of a **small team with a single-process monolith** moving to GraphQL for developer
  experience and reporting cost and benefit. That is the case Lighthouse is in; treat it as a gap.
- *Analysis*: the reasons given for leaving (auth per field, N+1, cost control) are the same items §1–§2 flag as the
  Lighthouse-specific work; the reasons given for staying (typed client, less client effort) are the items §3 and §7
  show are partly reachable through OpenAPI generation.

---

## Contradictions between sources

1. **Default list size in HC cost analysis**: the options table says `DefaultListSize` = 50; the "List Size" section of
   the same page says it "defaults to 1" [8]. Check in the prototype.
2. **Document hash**: HC defaults to MD5 [11], GraphQL Code Generator's persisted documents default to sha256 [13].
3. **Versionless vs our gates**: graphql.org presents versionless evolution as removing breaking changes [18]; for
   new-client/old-server pairs it makes an added field *more* breaking (whole-operation validation). Not a source
   conflict, but a framing difference that matters here (§3.4).
4. **HC v16 docs status**: some v16 pages are labelled preview while 16.7.7 is stable on NuGet [1][4]. Some doc URLs
   (testing guide) returned 404 on direct fetch; content was taken from search snippets [25].
5. **Client bundle sizes** vary by source and date [21]; measure in the repo instead.

## Knowledge gaps

| Gap | Searched | Why it matters |
|---|---|---|
| HC on a strict port/service layer with computed DTOs (no `IQueryable`) | HC docs (projections, DataLoader), web search | The main fit question for Lighthouse |
| gql.tada / GraphQL LSP under TypeScript 7 | gql.tada docs, issues search, TS 7.0 announcement | Decides codegen vs inference |
| Stryker.NET with HC source generators | Stryker docs and issues | Mutation gate is per feature |
| HC introspection default in production for v16 | introspection and request-limits pages | Security baseline |
| HC MCP adapter credential propagation | blog + docs | MCP OAuth (ADR-079) |
| NSwag maintenance status in 2026 | not searched (budget) | One of the OpenAPI options |
| Postman 2025 GraphQL share | report pages not readable | Context only |
| Small-monolith REST→GraphQL case study with numbers | several searches | The closest peer evidence |

## Open questions only a prototype can settle

1. **Code per field**: implement one Team page slice (Team + 3 metric widgets + forecast) as HC types and resolvers
   calling the existing services. Count files/lines vs the current controller + TS service + zod + hook path.
2. **RBAC adapter**: one policy handler that reads a unit id from an argument or parent object and calls
   `IRbacAdministrationService`; does it keep 404-style non-disclosure; does list filtering stay in one place?
3. **N+1 under nesting**: `portfolio { teams { metrics } }` — how many service calls, and does a DataLoader fit
   the metrics cache?
4. **Codegen under TypeScript 7 + Biome + `pnpm build`**: does the client preset run cleanly, does gql.tada's plugin
   load, what is the bundle delta of graphql-request + generated documents?
5. **Version gate**: can a client survive an old server without a gate entry per field (e.g. by schema introspection
   at start-up), or does the gate table grow?
6. **Tooling gates**: does `TreatWarningsAsErrors` + SonarAnalyzer accept HC source-generated code; does Stryker.NET
   run on a project with HC's generators; what does Sonar say about resolver classes?
7. **Cost limits for computed fields**: what field costs keep a forecast or a 27-widget metrics query inside the
   defaults, and what happens to user scripts if trusted documents are enforced?
8. **The OpenAPI counter-prototype**: generate TS types + zod + TanStack hooks from the existing Swagger document
   for the same Team slice and the clients' Delivery list (Bug #6254), to compare like with like.

---

## Sources

All accessed 2026-11-27.

| # | Source | Domain / kind | Reputation |
|---|---|---|---|
| [1] | NuGet, HotChocolate package page. https://www.nuget.org/packages/HotChocolate/ | registry | High |
| [2] | ChilliCream, "What's new for Hot Chocolate 16", 2026-05-11. https://chillicream.com/blog/2026-05-11-hot-chocolate-16 | vendor primary | Medium-High (vendor) |
| [3] | NuGet, GraphQL (GraphQL.NET) package page. https://www.nuget.org/packages/GraphQL/ ; releases https://github.com/graphql-dotnet/graphql-dotnet/releases | registry | High |
| [4] | ChilliCream, HC v16 Projections. https://chillicream.com/docs/hotchocolate/v16/resolvers-and-data/projections/ | vendor docs | High |
| [5] | ChilliCream, HC v16 DataLoader. https://chillicream.com/docs/hotchocolate/v16/resolvers-and-data/dataloader | vendor docs | High |
| [6] | ChilliCream, Nitro MCP adapter docs. https://www.chillicream.com/docs/nitro/adapters/mcp | vendor docs | High |
| [7] | ChilliCream, HC v16 Authorization. https://chillicream.com/docs/hotchocolate/v16/security/authorization | vendor docs | High |
| [8] | ChilliCream, HC v16 Cost analysis. https://chillicream.com/docs/hotchocolate/v16/security/cost-analysis | vendor docs | High |
| [9] | ChilliCream, HC Request limits. https://chillicream.com/docs/hotchocolate/v16/security/request-limits | vendor docs | High |
| [10] | ChilliCream, HC Introspection. https://chillicream.com/docs/hotchocolate/v16/security/introspection | vendor docs | High |
| [11] | ChilliCream, HC v16 Security overview. https://chillicream.com/docs/hotchocolate/v16/security | vendor docs | High |
| [12] | ChilliCream, HC Trusted documents. https://chillicream.com/docs/hotchocolate/v16/performance/trusted-documents | vendor docs | High |
| [13] | The Guild, GraphQL Code Generator client preset. https://the-guild.dev/graphql/codegen/plugins/presets/preset-client | tool docs | High |
| [14] | The Guild, Codegen React guide. https://the-guild.dev/graphql/codegen/docs/guides/react-vue | tool docs | High |
| [15] | TanStack Query, GraphQL. https://tanstack.com/query/latest/docs/framework/react/graphql | tool docs | High |
| [16] | gql.tada, Get started / Installation. https://gql-tada.0no.co/get-started/installation | tool docs | High |
| [17] | Microsoft, "Announcing TypeScript 7.0", 2026-07-08. https://devblogs.microsoft.com/typescript/announcing-typescript-7-0/ | official | High |
| [18] | graphql.org, Schema design (versioning). https://graphql.org/learn/schema-design/ | foundation | High |
| [19] | M. Bessey, "Why, after 6 years, I'm over GraphQL", 2024-05-24. https://bessey.dev/blog/2024/05/24/why-im-over-graphql/ | individual blog | Medium (anecdote) |
| [20] | sennder, "The rise and fall of GraphQL at sennder", 2019-06-10. https://www.sennder.com/blog/the-rise-and-fall-of-graphql-at-sennder | company blog | Medium (anecdote) |
| [21] | urql comparison (https://formidable.com/open-source/urql/docs/comparison), pkgpulse (https://www.pkgpulse.com/guides/apollo-client-vs-urql-2026), via search | mixed | Medium-Low |
| [22] | graphql.org, Serving over HTTP. https://graphql.org/learn/serving-over-http/ | foundation | High |
| [23] | graphql.org, File uploads. https://graphql.org/learn/file-uploads/ | foundation | High |
| [24] | ChilliCream, HC v16 Subscriptions. https://chillicream.com/docs/hotchocolate/v16/defining-a-schema/subscriptions | vendor docs | High |
| [25] | ChilliCream, HC v16 Testing guide (via search snippet; direct fetch 404). https://chillicream.com/docs/hotchocolate/v16/guides/testing/ | vendor docs | Medium |
| [26] | Stryker.NET mutant schemata (https://stryker-mutator.io/docs/stryker-net/technical-reference/mutant-schemata/); issue #2881 (https://github.com/stryker-mutator/stryker-net/issues/2881); PR #3106 | tool docs/issues | Medium |
| [27] | SonarSource, SonarQube Cloud languages overview. https://docs.sonarsource.com/sonarqube-cloud/advanced-setup/languages/overview/ | vendor docs | High |
| [28] | MSW, `graphql` API. https://mswjs.io/docs/api/graphql | tool docs | High |
| [29] | ChilliCream, HC v16 Extending types. https://chillicream.com/docs/hotchocolate/v16/building-a-schema/extending-types/ | vendor docs | High (via search snippet) |
| [30] | ChilliCream, HC v16 MCP adapter guide. https://www.chillicream.com/docs/hotchocolate/v16/guides/mcp-adapter | vendor docs | High (via search snippet) |
| [31] | ChilliCream, "From GraphQL to MCP in Two Lines", 2026-05-28. https://chillicream.com/blog/2026-05-28-mcp-hotchocolate-fusion | vendor blog | Medium-High |
| [32] | Apollo, Apollo MCP Server: Define tools. https://www.apollographql.com/docs/apollo-mcp-server/define-tools | vendor docs | High |
| [33] | Microsoft Learn, OpenAPI support in ASP.NET Core (aspnetcore-11.0, updated 2026-11-02). https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/overview | official | High |
| [34] | openapi-typescript docs. https://openapi-ts.dev/introduction | tool docs | High |
| [35] | orval. https://orval.dev/ | tool docs | High |
| [36] | Hey API, openapi-ts get started. https://heyapi.dev/openapi-ts/get-started | tool docs | High |
| [37] | Microsoft Learn, Kiota overview. https://learn.microsoft.com/en-us/openapi/kiota/overview | official | High |
| [38] | Microsoft Learn, ASP.NET Core OData 8 overview (2022). https://learn.microsoft.com/en-us/odata/webapi-8/overview | official, dated | High (dated) |
| [39] | NuGet, Microsoft.AspNetCore.OData. https://www.nuget.org/packages/Microsoft.AspNetCore.OData/ | registry | High |
| [40] | G. Brito, M. T. Valente, "REST vs GraphQL: A Controlled Experiment", arXiv:2003.04761, 2020. https://arxiv.org/abs/2003.04761 | academic | High (small sample) |
| [41] | Courier, "Why you can't replace REST with GraphQL". https://dev.to/courier/why-you-can-t-replace-rest-with-graphql-2fik | vendor blog | Medium-Low |
| [42] | Nordic APIs / DevX summaries of Postman State of the API (https://nordicapis.com/a-deep-dive-into-the-state-of-the-api-2025/); Postman 2025 report https://www.postman.com/state-of-api/2025/ (not readable) | secondary | Medium-Low |

Repo paths are cited inline; all are relative to `/storage/repos/Lighthouse/.claude/worktrees/glowing-crafting-globe/`
except `lighthouse-clients/…`, which is `/storage/repos/lighthouse-clients/`.

## Research metadata

Web sources examined ≈ 45, cited 42. Tool failures: two direct fetches returned 404 (HC testing guide, one MSW page;
alternatives used); no shell tool, so action counts were not recounted. Confidence spread across findings:
High ≈ 55 %, Medium ≈ 35 %, Low ≈ 10 %. Nothing committed.
