# Story 6383 — S09b One assembly per context? Decided: not now (ADR-289)
**Finished:** 2026-11-28 · ADO User Story #6383 · Epic #6510 "Modules: package the bounded contexts"

## The problem

ADR-027 D5 kept all module boundaries in one assembly, with the single-binary publish as its reason. Once the
bounded contexts were packaged and published their contracts (Story 6382), the question came back: should each
context become its own assembly, so the compiler enforces the boundaries?

## What changed

The maintainer decided on 2026-11-26: not now. This Story records that decision. It changes docs only.

- **ADR-289** "Module boundaries stay in one assembly": one backend assembly stays; the `Contracts` namespaces and
  the architecture rules that read the compiled code are the boundary. It replaces ADR-027 D5's reason (the single
  binary is not what blocks a split; cycles between contexts and the older `Services`, `API` and `Data` code are),
  answers each weigh-point, names what a later split must keep (one binary in every topology, one `DbContext`,
  expand-only migrations, the data split per context), and gives the reopen preconditions and triggers, each with
  how it is checked and by whom.
- **ADR-027** D5 and its alternatives line point to ADR-289; the dated text is unchanged.
- **`ARCHITECTURE.md` §4** gains one line: all contexts share one assembly, and why.
- **The Epic 6264 catalogue** marks S09b decided.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | Done: ADR-289, ADR-027, `ARCHITECTURE.md` §4; no page under `docs/` for users describes the assembly layout. |
| Screenshots | N/A, because no UI changed. |
| Demo data | N/A, because no behaviour or data changed. |
| Website assets | N/A, because no asset under `docs/assets/` changed. |
| Clients (CLI, MCP) | N/A, because no route or DTO changed. |
| Context map, `ARCHITECTURE.md` | Done: `ARCHITECTURE.md` §4; ADR-270's map is unchanged (no context, edge or term changes). |
| Usage-data event | N/A, because nothing a user can use changed. |
| Mutation | N/A, because no production code changed. |
| Release notes | N/A; written at /release. |

## Decisions taken in AFK mode that the maintainer may revisit

- **The design's rule test and measurement runs were dropped**, following the 2026-11-26 decision that this Story is
  the ADR only. ADR-289 says which rule does not exist yet (the context-cycle rule) and that its counts are text
  measurements.
- **Route:** worked directly, not through nwdes. nwdes treats only its own guide folders as prose, so a docs-only
  slice here would have needed an oracle pinning wording. An independent review (Haiku) took the place of the
  oracle; it ran two rounds (12 findings, then 2), all fixed.
- **Delivered in a worktree beside Story 6368**, on the maintainer's question whether it could run in parallel.

## Knowingly left open

- The context-cycle rule (`ContextDependencyGraphArchUnitTest`) is a possible future item, not part of this decision.
- No build-time baseline (trigger T2) is recorded.
