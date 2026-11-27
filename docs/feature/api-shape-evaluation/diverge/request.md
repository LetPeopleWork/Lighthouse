# DIVERGE request: API shape for Lighthouse

Feature id: `api-shape-evaluation`. Written by the host on 2026-11-27 from the requester's own turns in the session.

## The request

source: requester-verbatim

> would like to do an investigation on a continuous improvement item (potentially - item does nto exist yet): with
> the split into modules that is going on now, we have a nicer separation between concerns. going forward, I was
> wondering if it would help if we'd switch to graphql (potentially with federation?) instead of bare API calls.
> that way, the ui could fetch what it needs and we just have one neat endpoint and dont need to make many calls.
> what's the best way to explore this?

## Constraints the requester set

source: requester-verbatim

> I do think we could get rid of REST if we wanted, by adjusting the clients too. for sure we dont want to maintain
> two endpoint types. but that would be a conclusion and should nOT drive the decision. i agree the modules are not
> separated yet, i like the idea that if we ever do it we would be able to, but that is not the driver. so yeah,
> let's do step 1 for now. note: even if we dont create an item, th e result should be an ADR

## The driver, after the speed spike

source: requester-verbatim

> also easier development. I wonder whether the cheap fixes can be tracked in a story, as graphql itself (if we
> pick it) would be something for later, adn the small fixes can probably be tackled right now?
>
> Then I'd go for Option B

The requester answered "also easier development" to the host's question "Is speed your only reason to look at
GraphQL, or also easier development and a cleaner contract?". Option B was: research and compare directions on
the non-speed drivers, then write the ADR.

## Context the host adds

source: host-summary

- A measuring spike (`../spike/findings.md`) settled speed: slow pages are dominated by uncompressed JavaScript,
  not by call count; one-request reads would save about 0.5 s on slow 4G on two page types. The cheap speed fixes
  are split out as ADO Story #6712 and are **not** part of this comparison.
- Research (`../research/research.md`, 42 sources) covers GraphQL on .NET 11 (Hot Chocolate 16), authorization,
  developer experience, testing tooling, module-owned schema, the CLI/MCP clients, and the alternatives
  (OpenAPI-generated clients, page-shaped read endpoints, OData). It makes no recommendation.
- Fixed: one API surface only; if the surface changes, the clients in the separate `lighthouse-clients` repository
  move too. Single-process modular monolith (ADR-027, ADR-270). Module-owned schema is a nice-to-have.
- The outcome becomes an ADR whatever direction wins, including "keep REST as it is".
