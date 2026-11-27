# Recommendation: API shape for Lighthouse

**Verdict.** Keep REST. Generate the frontend's and the clients' types, checks and test mocks [new] from the OpenAPI
document [new] that the backend already publishes, and fail CI when that document changes in a breaking way. Score
3.75 of 5, ahead of keeping the bare API calls as they are (3.45). GraphQL scores 2.60: it gives the best type
checking, but it brings the most new concepts and the largest migration, and the job is easier development for a very
small team.

Traceability: job (`job-analysis.md`: OUT-1 hand-written copies, OUT-2 undetected drift) → evidence
(`competitive-research.md`, `../research/research.md` §3, §7, §8) → scores (`taste-evaluation.md` §4, revision 1)
→ this page. Jev: not used (`"jev": "unauthorized"`).

## Top 3

### Generate types, checks and test mocks from the OpenAPI document [new] — Score 3.75

**Why it scores well**: It removes the hand-written TS types, zod [new] schemas and mocks in the frontend and the
hand-typed answers in the clients (OUT-1). A committed copy of the document with a CI diff [new] catches a reshape
like Bug #6254 before release (OUT-2). The document already exists (the `/api/docs` page), so the first step in this
repository is one small action, and each service can move alone.
**Core trade-off**: Two new things to learn (the committed document is the contract [new]; regenerate after a
change). No field selection; the UI still makes many calls per page; the `/v1` + `/latest` route pair [new] and the
version gate [new] stay as they are.
**Key risk**: The document must describe the real answers exactly (enum shape, nullability, unions). If it does not,
generated types are wrong in a way that compiles.
**Hire criteria**: When most of the cost of a change is writing the same shape again in each consumer, and the clients
must keep working against older self-hosted servers.
**Migration cost**: Backend: emit the document at build time, commit it, diff it in CI, and publish it with each
release as a release asset [new]; add response attributes only where the document is imprecise; no route changes.
Frontend: ~30 service classes replaced or thinned one at a time; the model classes that carry behaviour stay and are
built from the parsed answer, as today. Clients: a CI step fetches the newest released document, regenerates and
compiles; ~60 routes switch to generated types and functions against `/v1`. Version gates: same rule as today.
Medium, incremental.
**Prototype must show**: for the Team › Metrics page and the clients' Delivery list — generated code builds clean with
the frontend's normal build and lint; generated zod schemas accept real demo answers (enums, dates, nulls); a replay of
Bug #6254's reshape fails the backend CI diff, the frontend compile and the clients' CI after regeneration; how the
document reaches the clients' repository; hand-written lines removed against added; bundle size change.

### Keep the bare API calls and continue the zod roadmap — Score 3.45

**Why it scores well**: Zero new concepts and nothing to adopt; zod already checks the converted frontend boundaries.
**Core trade-off**: Each field is still written by hand in up to four places; the clients still cast answers
unchecked, which is how Bug #6254 reached the CLI and MCP.
**Key risk**: The rate of contract change and of agent-written code grows, and the copies drift more often.
**Hire criteria**: When the API changes rarely, or when no time is free for even a small prototype [new].
**Migration cost**: None.
**Prototype must show**: Nothing to build. Count the hand-written shape copies and drift incidents over the next three
Stories that touch the API; if they stay near zero, this direction is enough.

### A composite read per screen, built on the existing query services [new] — Score 3.20

**Why it scores well**: One familiar concept; one page at a time; a metrics page or a Portfolio page gets its data in
one answer instead of many calls, which is the part of "the ui could fetch what it needs" that REST can serve.
**Core trade-off**: Typing and drift are unchanged (OUT-1, OUT-2 not served); each new page needs a new endpoint, and
the old endpoints stay for the clients, so the endpoint count grows.
**Key risk**: The effort per screen is not in assembling calls, so the gain is small.
**Hire criteria**: When a page needs many related numbers at once and a widget added later should not mean a new call.
**Migration cost**: ~10–15 new page reads (metrics pages, Portfolio, Team settings); page hooks swap in the frontend;
the clients unchanged. Small-medium per page. Can be added on top of the recommendation later.
**Prototype must show**: Team › Metrics as one read: files touched to add a widget, against today.

### Not in the top 3

| Direction (by mechanism) | Score | Why lower |
|---|---|---|
| A single hand-written client package [new] that the frontend also imports | 3.10 | Two hand-written clients become one, and Medusa and Spree build their own dashboards on their public SDK [new]; but the backend can still drift from it, and release coupling across two repositories adds a second new concept |
| graphql with schema parts [new] contributed by each of the modules | 2.60 | Best contract check (5), but a new mental model (T2 = 1), per-field access checks [new], batch loaders [new], query cost limits [new], a different way of reporting errors, and a rewrite of ~265 actions and ~60 client routes; an added field makes an old server reject the whole query, so version gates grow; users' scripts break. Teams that left GraphQL name these costs [19][20] |
| Expectation files the consumers record and the backend replays [new] | 2.45 | Catches drift without removing any hand-written copy; the .NET Pact library [new] last released in March 2025 and targets an old .NET baseline; needs a broker [new] across two repositories |

## Recommended option

Generate the consumers from the OpenAPI document [new]. It leads under all three weight sets (3.75–3.80), by 0.30–0.42
over keeping the bare API calls. Under the locked weights, a one-point change on any single taste criterion of the
recommended direction moves it by at most 0.25, and one point on a single DVF lens by 0.10, so neither changes first
place; a two-point drop in its concept score would, and a full point off its DVF average would tie it. It serves the two under-served outcomes (OUT-2 score 14, OUT-1 score 13) with two new things to
learn, and it keeps the one API surface and the scripts that users have written against the API. Critical weakness,
flagged: the whole direction rests on the document being exact, and on carrying it into the clients' repository. That
is why the first step is a prototype, not a migration.

The modules keep owning their controllers and so their part of the document. A schema owned module by module, in the
GraphQL sense, is not reached; the requester named module separation as not the driver.

## Dissenting case

The scoring's second place is keeping the bare API calls (3.45). The case for it: it has the best concept score (5),
nothing to migrate, and the zod roadmap already closes part of OUT-2 in the frontend. It loses on subtraction (2) and
contract feedback (2), because the clients cast answers unchecked and every field is still written by hand. The margin
is 0.30. If the prototype shows the document cannot be made exact without many hand-written attributes, or cannot be
carried into the clients' repository simply, generation buys little over zod, and this dissent wins: keep REST as it
is and add only a CI diff of the document.

## Decision for DISCUSS

Proceed with generating the frontend's and the clients' types, zod [new] schemas and test mocks [new] from the OpenAPI
document [new] the backend already publishes, with a committed document [new] and a CI diff [new] as the drift check [new] and the
document published as a release asset [new] for the clients, keeping REST and the version gate [new] rule, assuming a
small prototype [new] on the Team › Metrics page and the clients' Delivery list shows that the document matches the
real answers.

graphql is not chosen now. The ADR records it as rejected for this job, with the prototype questions in
`../research/research.md` ("Open questions only a prototype can settle") as the condition for reopening it.

## For the requester

- **Recommended:** keep REST. The backend already publishes a description of its API, the OpenAPI document [new].
  Generate the frontend's types, checks and test data from it, and the clients' types too. Then you write a field
  once, in the backend. A CI check [new] stops a change that would break the CLI or the MCP server.
- **Why not graphql now:** it checks types best. But it adds many new concepts: field-level access rules [new],
  batch loading [new], query cost limits [new], a different way of reporting errors. It also means rewriting about 265 backend actions
  and about 60 client calls. Scripts that users have written against the API would break. For one maintainer with AI
  agents, that cost is larger than the gain.
- **Second choice (the dissent):** keep the bare API calls as they are and continue the zod [new] checks. This wins if
  the prototype shows the API description cannot be made exact.
- **Next step:** a small prototype [new] on one Team page and on the clients' Delivery list. It must show that the
  generated code builds clean and that it catches a change like Bug #6254. Then the ADR.
- **Speed:** not part of this. The cheap fixes are in Story #6712.
