# Story 6382 — S09a Context contracts: each context publishes its contracts, and each cross-context edge has one owner
**Finished:** 2026-11-28 · ADO User Story #6382 · Epic #6510 "Modules: package the bounded contexts"

## The problem

After the S03 Stories every bounded context had its own module, but nothing said which part of a module another context
may use. Each module's rules listed the edges into other contexts by hand. The same edge could sit in two lists, an
edge into any public type was as legal as an edge into a port, and the composition root (`Program.cs`) still registered
every context's services itself. The domain events sat in one shared `Models.Events` folder, so a reader could not see
which context published an event, and Forecasting still called Work Items directly to write a round's refresh-log entry.

## What changed

Four nwdes features on `main`, 13 slices. Design: `docs/feature/story-6382-s09a-context-contracts/feature-delta.md`;
per-feature records under `docs/feature/story-6382-s09a-{a-rules-first,b-module-start-up,c-contracts,d-events-and-rules}/`.
No schema, route, DTO, usage-data event or configuration key changed.

- **Feature A, rules first.** The contracts rule (`ContextContractsArchUnitTest`) exists before any move: the set of
  contexts is measured from the compiled code, and every edge from one context into another that is not a contract is
  listed by name with its owner.
- **Feature B, each module starts itself.** All ten contexts register through their own `Add<Context>` method, and
  `Program` keeps only those calls, the host set-up and the event-handler block. A preservation test compared the
  container before and after, service by service.
- **Feature C, contracts published.** Licensing, Access, Flow metrics, Forecasting, Work Items, Tracker integration and
  Organisation set-up each publish a `Contracts` namespace with the types other contexts use. Organisation set-up's open
  host narrows from "its Application and Domain by right" to "its Contracts".
- **Feature D, events and rules.**
  - D1: the 15 domain event types live in their publisher's `Contracts`; only `IDomainEvent` stays in the kernel.
  - D2: Forecasting no longer commands Work Items. At the end of a round, `ForecastUpdater` publishes
    `ForecastRoundFinished` with the round's outcome (succeeded, failed, cancelled or skipped). Work Items'
    `ForecastRoundFinishedRefreshLogHandler` writes the same refresh-log entry as before. If that write fails, the
    dispatcher logs it and the round still completes.
  - D3: each cross-context edge has one owning rule. Module-to-module entries leave the per-module lists, and the
    contracts rule owns them alone (8 residue edges remain, listed in ADR-270).
- **Docs:** ADR-270 (the contracts layer, the "context contract" term, where each event lives, the Forecasting to Work
  Items event edge, the residue table); `ARCHITECTURE.md` §4 (contracts, events in their publisher's contracts, the CI
  test-selection payoff, not built).

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | N/A, because nothing a user sees changed: no page under `docs/` describes the code layout or the refresh log's writer. |
| Screenshots | N/A, because no UI changed. |
| Demo data | N/A, because no behaviour or data a user sees changed. |
| Website assets | N/A, because no asset under `docs/assets/` changed. |
| Clients (CLI, MCP) | N/A, because no route or DTO changed. |
| Context map, `ARCHITECTURE.md` | Done in D3 (`f3c90a470`) and the candidate review round (`e5297a459`): ADR-270 and `ARCHITECTURE.md` §4 match the code. |
| Usage-data event | N/A, because the Story changes no behaviour a user can use (DEVOPS); `ForecastRoundFinished` is a domain event, not a usage-data event. |
| Mutation | Done for feature D: Stryker.NET over `ForecastUpdater`, `ForecastRoundFinished` and `ForecastRoundFinishedRefreshLogHandler`, **91.84 %** (86 killed, 4 timeout, 6 survived, 2 no coverage; the handler 13 of 13 killed). Survivors are log calls, an equivalent `stopwatch.Stop()` and a defensive guard; see `docs/feature/story-6382-s09a-d-events-and-rules/mutation/results.md`. Features A to C changed production code only in `namespace`, `using` and registration lines (B pinned the container service by service), so they were not mutated. |
| Release notes | N/A; written at /release. |

## Gates (feature D, the last push)

- `dotnet build Lighthouse.Backend`: 0 warnings, on `cc508785b` (rebased onto `origin/main`).
- Full filtered backend suite: 15660 passed, 0 failed, 2 skipped (9 m 20 s). Architecture namespace: 2484 passed.
- Slice reviews: D1 accepted after one round, D2 accepted, D3 accepted after one round. Whole-candidate review:
  accepted after one round (two stale doc sentences).
- EXAMINE: accepted. E1, E3 and E4 met (architecture rules 2488/2488; refresh 343/343, forecast 128/128 and reactions 3/3 answers identical to the parent `7bddf4ea4`); E2 waived by the maintainer, because no public probe can force a failed, cancelled or skipped round or a throwing refresh-log write, and the D2 oracle covers those cases.
- Features A to C: their gates, reviews and EXAMINE are in their own records; each was pushed when it closed.

## Decisions taken in AFK mode that the maintainer may revisit

- **The open-host check leaves out Refinement's adapters-only rule (D3 review round).** The check selects only the
  rules that read set-up's Adapters and Application. Refinement's forbidden-code rule reads set-up only through "any
  module's adapters", and its set-up edges belong to the contracts rule. A planted break still turns the check red.
- **Two feature C tests were re-stated, not kept (D3).** Tracker integration now lists no edge into set-up, because the
  connection-health cadence reads the settings port through set-up's Contracts. The oracle owner adopted the new
  tests, and the change was re-approved independently.
- **Rebase onto main (feature D).** Main's new Within SLE handler imported the refresh events from the old namespace.
  It now imports Work Items' Contracts (`cc508785b`). Main's two new lines in the handler-order pin were re-approved
  as part of D2's oracle.
- **EXAMINE E2 waived (maintainer, 2026-11-28).** The waiver records its reason and coverage in the feature's expectations.
- **Rigor:** D2 `thorough` (Opus, reviewers Sonnet); D1 and D3 `standard` (Sonnet, reviewers Haiku); the candidate
  review Sonnet.

## Knowingly left open

- Eight cross-context edges are still not contracts; ADR-270's residue table lists each with its owner.
- Refinement's `TheForbiddenCodeAndSetUpBehindItsContracts` is an alias of `TheForbiddenCode`, kept because an approved
  oracle looks it up by that name. Rename both when that oracle is next revised.
- The "Tracker integration lists no edge into set-up" check exists in two test classes; both are approved oracles.
- The per-assembly split is not done: Story 6383 records the decision not to split now.
