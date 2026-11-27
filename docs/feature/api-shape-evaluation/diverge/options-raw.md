# Options (raw): API shape for Lighthouse

Generation only. No option is scored or ranked here. Probabilities are stated estimates of how typical each candidate
is within the distribution it was drawn from (verbalized sampling), not measurements.

## HMW question

How might we make a change to what the backend offers reach the frontend and the CLI/MCP clients typed and checked,
with as little hand-written code and as few new concepts as possible?

(No mechanism named; serves OUT-1 and OUT-2 directly, OUT-3 to OUT-6 as constraints. Revision 1 dropped "from one
place", which leaned towards generation.)

## SCAMPER candidates

Each lens: one distribution, summing to 1. Tail = stated probability ≤ 0.2.

| Lens | Candidate | p | Tail? |
|---|---|---|---|
| **S** Substitute the transport | GraphQL schema, one endpoint, codegen clients | 0.60 | |
| | OData query options on the REST endpoints | 0.25 | |
| | Protobuf contract over gRPC-Web, generated TS and C# | 0.15 | tail |
| **C** Combine with an adjacent job | One client package used by frontend, CLI and MCP | 0.50 | |
| | Generated clients plus page-shaped reads together | 0.35 | |
| | MCP tools served from inside the backend process | 0.15 | tail |
| **A** Adapt from another domain | Snapshot test of the published API document (from UI snapshot tests) | 0.55 | |
| | Consumer-recorded contract tests (from microservices, Pact) | 0.20 | tail |
| | Expand/contract for API fields (from database migrations) | 0.25 | |
| **M** Magnify the one-source dimension | Generate types, zod, hooks and mocks from the existing API document | 0.70 | |
| | Write the contract first in TypeSpec, generate both sides | 0.20 | tail |
| | Keep hand-written types, add a lint rule per boundary | 0.10 | tail |
| **P** Put to other use | Generate the MCP tool list from the API document | 0.40 | |
| | Publish the generated client as an SDK for users' scripts | 0.40 | |
| | Give AI agents the document as the spec they code against | 0.20 | tail |
| **E** Eliminate | Eliminate the change: keep the current calls, continue the zod roadmap | 0.40 | |
| | Eliminate the frontend service classes: components call a typed generic fetch | 0.45 | |
| | Eliminate the UI contract: server-rendered UI | 0.15 | tail |
| **R** Reverse | The server decides what a page needs: one read endpoint per page | 0.55 | |
| | The frontend's zod schemas become the contract; backend tests check answers against them | 0.30 | |
| | Clients ask the server what it supports instead of a gate table | 0.15 | tail |

## Crazy 8s supplements

One distribution, summing to 1.

| Candidate | p | Tail? |
|---|---|---|
| C# DTOs translated to TS types by a source generator, no API document | 0.45 | |
| A CI job that only diffs the published document and fails on a removal | 0.35 | |
| A typed route table shared by controllers and frontend (one constant file per module) | 0.20 | tail |

## Curated six

### Option 1: bare API calls

**Core idea**: Nothing changes in kind. A developer keeps writing the DTO, the zod schema, the TS service method and the client's method by hand; the zod roadmap continues boundary by boundary.
**Key mechanism**: Hand-written types on each side, runtime parse with zod at converted frontend boundaries, ADR-055 version-gate rule in the clients.
**Key assumption**: The cost of the hand-written copies stays small at the current rate of change, and drift like Bug #6254 stays rare.
**SCAMPER origin**: E (eliminate the change)
**Closest competitor**: Any REST API with hand-written clients; Lighthouse today.
**name_origin**: user-words ("bare API calls")
**stated_probability**: 0.40 within E; 0.135 restated over the six

### Option 2: graphql

**Core idea**: The UI and the clients send one query to one endpoint and name the fields they need; types for each query are generated from the schema.
**Key mechanism**: Hot Chocolate 16 schema built from types that each module folder contributes (`[QueryType]` partials, `[ExtendObjectType]` on `Team`/`Portfolio`); resolvers call the existing application services; GraphQL Code Generator client preset with TanStack Query in the frontend and the clients; REST removed except the file transfer endpoints.
**Key assumption**: Per-field RBAC, DataLoaders for computed fields and cost limits stay readable, and the time saved in consumers exceeds the time added in resolvers.
**SCAMPER origin**: S (substitute the transport)
**Closest competitor**: Hot Chocolate + GraphQL Code Generator; Courier's own frontends.
**name_origin**: user-words ("graphql")
**stated_probability**: 0.60 within S; 0.205 restated over the six

### Option 3: Generated client from the API document

**Core idea**: The backend's existing OpenAPI document becomes the contract; TS types, zod schemas, query hooks and test mocks are generated from it in the frontend and in the clients; CI fails when the document changes in a breaking way.
**Key mechanism**: Build-time document emission from the controllers, a generator (orval or openapi-typescript), a committed document with a CI diff.
**Key assumption**: The document can be made exact enough (nullability, enums, unions) that generated types match real answers.
**SCAMPER origin**: M (magnify the one-source dimension)
**Closest competitor**: orval / openapi-typescript / Kiota users.
**name_origin**: coined (this wave minted it)
**stated_probability**: 0.70 within M; 0.235 restated over the six

### Option 4: One read endpoint per page

**Core idea**: A screen fetches one answer shaped for it (for example Team › Metrics or a Portfolio page); writes and the clients' routes stay as they are.
**Key mechanism**: Composite read endpoints on the CQRS-lite read side that call the existing query services and return a page-shaped DTO.
**Key assumption**: Most development effort per screen is assembling many calls in the UI, not keeping types in sync.
**SCAMPER origin**: R (the server decides what a page needs)
**Closest competitor**: Backend-for-frontend endpoints.
**name_origin**: coined (this wave minted it)
**stated_probability**: 0.55 within R; 0.185 restated over the six

### Option 5: Consumer-recorded contract tests

**Core idea**: The frontend and the clients record, in tests, what they expect from each call; the backend's test run replays those expectations and fails when it no longer meets one.
**Key mechanism**: pact-js consumer tests produce pact files; PactNet verifies them in `dotnet test`; a Pact Broker shares them between the two repositories.
**Key assumption**: Drift is the main pain, and hand-written types are acceptable if drift is caught before release.
**SCAMPER origin**: A (adapt from microservices)
**Closest competitor**: Pact / PactFlow.
**name_origin**: coined (this wave minted it)
**stated_probability**: 0.20 within A (tail); 0.07 restated over the six

### Option 6: One client package for UI, CLI and MCP

**Core idea**: The frontend calls the API through the same typed client package that the CLI and the MCP server use, so one hand-written client exists instead of two.
**Key mechanism**: The client package (today in `lighthouse-clients`) grows to cover the frontend's calls and is consumed by the frontend; the frontend's service classes are removed.
**Key assumption**: The frontend and the clients need largely the same calls, and release coupling between the two repositories is manageable.
**SCAMPER origin**: C (combine with an adjacent job)
**Closest competitor**: Medusa (`@medusajs/js-sdk` in its own admin), Spree (`@spree/admin-sdk`).
**name_origin**: coined (this wave minted it)
**stated_probability**: 0.50 within C; 0.17 restated over the six

## Diversity test

| # | Mechanism | Assumption about the developer/consumer | Cost profile |
|---|---|---|---|
| 1 | Hand-written types + runtime parse | Copies are cheap enough | None now, recurring per change |
| 2 | New transport + executed schema + codegen | Consumers gain more than resolvers cost | Very high one-off, new recurring concepts |
| 3 | Generation from a document derived from code | The document can be made exact | Medium one-off, low recurring |
| 4 | Server-composed read per screen | Effort is in assembling calls | Low per page, grows with pages |
| 5 | Verification of recorded expectations, no generation | Drift, not copying, is the pain | Medium one-off (broker, tests), recurring per interaction |
| 6 | Code sharing across consumers, hand-written | UI and clients need the same calls | Medium-high one-off across two repos |

All six differ on all three dimensions; no pair shares mechanism, assumption and cost. No merge.

**Head-clustering read.** Restated distribution over the six: 0.135, 0.205, 0.235, 0.185, 0.07, 0.17 (sum 1.00).
Grouped by mechanism family: schema-generated types (2, 3) 0.44; same-transport hand-written (1, 4, 6) 0.49 — but
those three use three different mechanisms (status quo, server composition, sharing); verification (5) 0.07. No
single mechanism holds most of the mass. The tail candidate (5, verification without generation) has a mechanism
that differs from every head candidate. Gate condition "one tail candidate whose mechanism differs from every head
candidate" holds.

## Eliminated or merged at curation

| Candidate | Why not in the six |
|---|---|
| OData | Variation of "substitute the transport" with a narrower fit (entities only, net11 not listed); kept 2 as the representative |
| gRPC-Web / protobuf | Same mechanism family as 2 (new transport + IDL + codegen); 2 is the requester's own direction |
| Generated clients plus page-shaped reads | A combination of 3 and 4, not a new mechanism; noted for the recommendation |
| MCP tools inside the backend | A clients-release decision, separate from the contract job (R§6) |
| Snapshot test of the document; CI diff only | Part of 3's mechanism; merged into 3 |
| Expand/contract for fields | A practice usable under any option; not a direction |
| TypeSpec contract-first | Same generation mechanism as 3 with the authority reversed; C# server emitter is alpha ([G20]); 3 kept. So the brief's "OpenAPI-first generated client" is read here as "clients generated from the OpenAPI document the code already emits" (code-first document), not "contract written before the code" |
| Lint rule per boundary | Variation of 1 |
| MCP tools generated from the document; published SDK; document as agent spec | Uses of 3's output, not separate directions |
| Typed generic fetch | The consumption style of 3 (openapi-fetch); merged into 3 |
| Server-rendered UI | Removes the frontend but not the CLI/MCP contract; fails the job (disruption check) |
| Frontend zod schemas as the contract | Reverse authority of 3; keeps hand-writing in the frontend; variation of 3/5 |
| Capability negotiation instead of gates | Addresses OUT-6 only; a later refinement of any option |
| C# DTOs → TS by source generator | Same generation mechanism as 3 without the clients' side; merged into 3 |
| Shared typed route table | Variation of 6 |

## SCAMPER coverage

S, C, A, M, P, E, R each produced candidates (table above); P's candidates all merged into 3 as uses of its output.
