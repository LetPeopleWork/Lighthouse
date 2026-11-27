# ADR-288: Keep REST, and make the OpenAPI document the contract that the frontend's and the clients' types are generated from
Status: Accepted (2026-11-27)

**Keeps**: ADR-027 and ADR-270 (single-process modular monolith; module-owned API schema is not a goal of this
decision). ADR-055, ADR-062 and ADR-072 (the clients' version-gate table stays hand-written).
**Revisits**: ADR-223's "the client's method types stay as they are" and its "no zod schemas in `client` for now":
the clients' types become generated (they gain types, not a runtime zod dependency).
**Replaces**: the zod adoption evaluation's rule that each frontend schema is written by hand beside its model and
its `z.infer` is the source of truth (`docs/feature/zod-adoption-evaluation/`). Kept from it: zod 4,
`BaseApiService.parse`, parse-then-construct for behaviour-bearing classes, `z.coerce.date()`, checking only at
trust boundaries.

## Context

```text
The question was whether Lighthouse should replace its REST API with GraphQL, possibly with a schema each module
contributes to, so that the UI fetches what it needs from one endpoint. Two drivers were named: fewer calls (speed)
and easier development of the contract between backend, frontend and clients. Fixed beforehand: there is only ever
one API surface (if it changes, the CLI and MCP clients move too), Lighthouse stays a single-process modular
monolith (ADR-027, ADR-270), and module-owned schema is welcome but not a driver.

Speed. A measuring spike on 16 pages, cold and in-app, on fast, slow-4G and slow-3G lines, found that slow pages
wait for JavaScript, not for API calls: the server sends the frontend and the API answers uncompressed (3.4 MB of
JavaScript that gzip makes 1.0 MB), and that is 75-90 % of a slow page's wait. Calls are many on a cold load (25-53),
but about 26 are the app shell, many are repeats of one uncached hook, and the chains are short (the longest page
chain is 4 steps, about 0.7 s on slow 4G). One request per page would save about 0.5-1 s on two page types
(Metrics, Portfolio).

Development. Today the contract is written three times by hand: as C# DTOs, as TypeScript interfaces (partly with
zod schemas) in about 30 frontend services, and as types in the clients' package (73 requests, 42 of them cast to
unknown). Drift between them is found late: commit 8c0b99756 (2026-08-22) reshaped a Portfolio's Delivery list from
an array to {active, archived}, and the clients read it wrongly until a post-release smoke test failed (Bug #6254).
The backend already publishes an OpenAPI document (Swashbuckle), but nothing is generated from it and nothing
checks it.
```

## Decision

```text
Keep REST, with one API surface. Do not adopt GraphQL.

The backend's OpenAPI document becomes the contract. It is made exact by configuration, not by hand (shown on 39
operations, see Consequences): Swashbuckle reads the C# nullable annotations (SupportNonNullableReferenceTypes),
marks nullable references as nullable (UseAllOfToExtendReferenceSchemas), marks every property that
System.Text.Json always writes as required (one schema filter), and answers are JSON only. A string that carries a
closed set of values becomes a C# enum, so the document can say so.

The document is committed at Lighthouse.Backend/openapi/v1.json. CI rebuilds it and fails when the committed copy
is out of date. CI also diffs it against main's committed copy with oasdiff (breaking, --fail-on ERR). This is how
a deliberate breaking change is accepted: the same commit regenerates the committed document, so the break is
visible in review and the diff against main passes once it is merged. Each release attaches the document as a
release asset.

The document describes /api/v1 only (the /api/latest aliases are filtered out). So the frontend generates schemas
and types, not HTTP functions: it keeps its own calls to /latest.

The frontend generates its zod schemas and TypeScript types from the committed document with one pinned generator
(orval at the time of writing), dates as coerced Date values as the frontend already does. Services move to the
generated schemas one at a time, when they are touched or as improvement items: the generated schema checks the
answer at the boundary (BaseApiService.parse); the hand-written HTTP layer, error mapping and the behaviour-bearing
classes built from parsed answers stay. No generated test mocks for now.

The clients keep a copy of the released document under openapi/ in their repository, generate their types from
it, and typecheck against them in their own CI. A scheduled job downloads the newest release's document and opens
a pull request with the regenerated types. Version gates for older self-hosted servers stay hand-written (the
ADR-055 table); a diff between the oldest supported and the newest released document lists the routes that need
one.

The speed findings are fixed separately and do not depend on this decision (Story #6712: compression, a shared
cache for data every page uses, a smaller update check, parallel version checks).
```

## Alternatives

```text
GraphQL with a schema each module contributes to (Hot Chocolate 16). Best end-to-end typing; module type extensions
fit the module layout. Rejected: it rewrites about 265 backend actions and the clients' 73 requests; it breaks
users' own scripts against the documented REST API; the RBAC guard reads unit ids from the route, so every
unit-scoped field needs a new authorization handler; a denied read turns from 404 into null plus an error; it adds
DataLoaders and query-cost limits; SonarCloud does not analyse .graphql files; version gates get stricter (an older
server rejects a whole query that names one unknown field). Its speed gain is about 0.5-1 s on two page types. For
one maintainer working with AI agents, the cost is larger than the gain.

Keep the bare calls with hand-written zod schemas (ranked second). No new tooling, but the contract stays written
three times and drift is still found late. It stays the fallback if the document cannot be kept exact.

One composite read endpoint per screen. Fixes the many-calls shape, which the spike showed is not the problem, and
adds endpoints shaped by today's screens.

A shared client package used by frontend and clients. One hand-written copy instead of two, but still hand-written.

Consumer-recorded contract tests (Pact). PactNet's last release was in March 2025 and it targets netstandard2.0
only.

OData. Its strengths assume IQueryable entities; the metric and forecast reads, which are a large part of the API,
are computed answers.
```

## Consequences

```text
A field is written once, in C#; the frontend's and the clients' types follow by regeneration, and a change that
breaks them fails CI in the repository that made it, not after a release. A prototype on the Team Metrics endpoints
and the Portfolio Delivery list showed this: with today's document 36 of 39 real answers pass the generated zod and
every generated field is optional; with the three settings and the filter 39 of 39 pass and the generated types
are as strict as the hand-written ones. Replaying Bug #6254, oasdiff flags the array-to-object change, and the
clients' typecheck fails where the MCP server reads the answer. The CLI, which passes the answer on as unknown,
does not fail: the check only bites where a consumer is typed.

Generating also exposes where hand-written types were wrong (a field typed optional that the backend sends as
null), so moving a service can surface small fixes in its consumers.

The first document with the new settings differs from today's in about 1,275 places; that commit sets the
baseline. Generated code must be excluded from Sonar, and either formatted by Biome before the up-to-date check or
excluded from Biome. The generator is pinned, so a generator upgrade is a deliberate change.

Not shown yet: answers outside the 39 tested operations; 56 properties the document marks nullable that were never
null in the demo data; request bodies and query parameters (date-only parameters are declared date-time today); the
release-asset download in the clients' repository; and typing an answer over two server versions. The follow-up
items take these on.

GraphQL can be reconsidered if the API's consumers change (for example many third parties needing field
selection), not because of speed.
```
