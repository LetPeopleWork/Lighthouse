# Job analysis: API shape for Lighthouse (`api-shape-evaluation`)

**Verdict.** The job is not "switch to GraphQL" and not "one endpoint". It is: **when the contract between the backend
and its consumers changes, make the change once and have every consumer typed and checked against it.** Speed is
settled (spike) and is not part of the job. Job level: strategic; irreducible function: one source for the contract
shape, carried to every consumer, with drift caught before a user sees it. Gate G1: PASS.

Jev: not used. `"jev": "unauthorized"` holds for this project (CLAUDE.md). All judgments below are local.

## 1. Raw request

Pointer, not a copy: `request.md` in this folder. Blocks used as the requester's words: the three `requester-verbatim`
blocks ("The request", "Constraints the requester set", "The driver, after the speed spike"). The `host-summary`
block is context, never quoted as the requester's words.

## 2. Job extraction (5 Whys)

| Layer | Statement | Source |
|---|---|---|
| Tactical (the proposal) | "switch to graphql (potentially with federation?) instead of bare API calls" | request.md, verbatim |
| Operational | "the ui could fetch what it needs and we just have one neat endpoint and dont need to make many calls" | request.md, verbatim |
| Why many calls matter? | Speed was the first guess. The spike measured it: calls cost ~0.5 s on two page types on slow 4G; uncompressed JavaScript costs 4–10 s. Speed fixes go to Story #6712. | spike/findings.md |
| Why look further? | The requester answered "also easier development" (verbatim). | request.md |
| Why is development not easy today? | One field travels through 4 hand-written copies: C# DTO, zod schema (where adopted), TS type/model, the clients' hand-typed response. A screen adds an action, a v1/latest route pair, an `RbacGuard`, a TS service method, a hook, and in the clients a method plus a version-gate entry. A server reshape reached the CLI and MCP unnoticed (Bug #6254). | research.md §0, §3.3; zod roadmap; ADR-055 |
| Strategic (the job) | Change the API contract once; every consumer gets the change typed and checked; nothing breaks silently. | derived |
| Physical (irreducible) | One source for the contract shape → carried to each consumer → checked at the boundary before release. | derived |

A further "why" ("so the product improves faster") is a life-goal answer. Stop at the strategic layer.

## 3. Job statements

| Type | Statement | Evidence |
|---|---|---|
| Functional | When I add or change a field, a screen or a client command that crosses the API, I want the change to reach the frontend and the CLI/MCP clients typed and checked from one place, so I can ship it in one step without writing the same shape by hand several times and without a consumer breaking silently. | research.md §0, §3.3; Bug #6254 |
| Emotional | From "did I update every copy, and will the CLI break on an old server?" to confidence that a broken contract fails the build, not a user. | Bug #6254 record (silent CLI/MCP degradation); hypothesis for the feeling itself, not stated by the requester |
| Social | Users who script against the API (Swagger at `/api/docs`, linked from System Info) and contributors see a contract that is documented and stable. | research.md §0 (`docs/settings/systeminfo.md:17`); inference |

Who does the job: a very small team, essentially one maintainer plus AI agents (caller brief). So the amount of
hand-written code and the number of concepts an agent must hold both count as part of the job.

## 4. Disruption check

Is there a higher-level job that makes this job unnecessary? Only if there were no separate consumers: a
server-rendered UI with no client package. The CLI and MCP clients are shipped products in their own repository,
and users' scripts use the API, so the contract stays. The job stands. Module-owned schema and federation are
nice-to-haves, not part of the job: the requester likes that the modules could one day be separated, and says that
this is not the driver (request.md, second verbatim block).

## 5. Outcome statements (ODI)

| ID | Outcome statement |
|---|---|
| OUT-1 | Minimize the number of places a developer writes the same response shape by hand when a field is added. |
| OUT-2 | Minimize the likelihood that a server contract change reaches the CLI, the MCP server or the frontend undetected. |
| OUT-3 | Minimize the time it takes to bring a new screen's data from a backend service to a rendered component. |
| OUT-4 | Minimize the number of new concepts a developer or an AI agent must hold to change the contract. |
| OUT-5 | Minimize the time it takes to build test doubles that match the current contract for a consumer test. |
| OUT-6 | Minimize the number of client version-gate entries a contract change requires. |

## 6. Opportunity candidates

Desk estimates (one maintainer's context, no survey). Formula: Importance + max(0, Importance − Satisfaction).

| Outcome | Importance | Satisfaction | Score | Status |
|---|---|---|---|---|
| OUT-1 hand-written copies per field | 8 | 3 | 13 | Under-served |
| OUT-2 undetected drift | 9 | 4 | 14 | Under-served |
| OUT-3 screen data to component | 6 | 4 | 8 | Low priority (below 10, mildly under-served) |
| OUT-4 concepts to hold | 8 | 6 | 10 | Appropriately served |
| OUT-5 test doubles match contract | 6 | 4 | 8 | Low priority (below 10, mildly under-served) |
| OUT-6 gate entries per change | 5 | 6 | 5 | Over-served |

Reading: OUT-2 and OUT-1 carry the opportunity. OUT-4 is a constraint more than a gap: today's REST is familiar, so
any option that adds concepts pays here. OUT-6 is served by ADR-055's rule (additive field = no gate); an option that
makes additive fields need gates moves it backwards.

Satisfaction rationale: OUT-3 = 4 because a new screen reuses the established per-widget endpoint pattern (ADR-055:
each tile has its own small endpoint) and the existing service classes; the spike counts 27 calls on a metrics page,
which is many calls but no extra work per screen beyond one action and one hook (revision 1: was 5, lowered for want
of stronger evidence). OUT-2 = 4 because zod catches drift at the frontend boundaries converted so far
(zod roadmap steps 1–6), but the clients cast unchecked (Bug #6254). OUT-1 = 3 because zod's `z.infer` already
removed one copy on converted boundaries; the clients still hand-type.

## Gate G1

| Check | Result |
|---|---|
| Job at strategic or physical level | PASS (strategic statement + physical function) |
| No feature references in the job statement | PASS (no GraphQL, OpenAPI, endpoint or tool named) |
| At least 3 ODI outcome statements | PASS (6) |
