# ADR-228: MCP consent is elicited once inside the first successful tool call (stdio), or set by the operator and held in memory (HTTP)

- **Status**: Proposed (2026-10-08, DESIGN wave for ADO Story #6193). Interaction mode = **propose**, maintainer AFK.
- **Feature**: `story-6193-usage-data-from-clients` — repo `lighthouse-clients` (packages `mcp-core`, `mcp-stdio`,
  `mcp-http`)
- **Relies on**: ADR-226 (the shared store), ADR-227 (plan, reporter, MCP port).

## Context

The local MCP server asks once through **MCP elicitation** where the assistant supports it, and otherwise uses the
stored answer, off when there is none (M7). Accept is a yes, decline a final No, cancel, close or time-out no answer
(D11). The shared HTTP server never asks anyone: its operator decides with `LIGHTHOUSE_USAGE_DATA=on`, off by default
(M8), the grant is held in memory only (A17), and the docs say plainly that the operator decides for the server's
users (maintainer decision after DISCUSS).

An elicitation is a request from server to client. The SDK in use (`@modelcontextprotocol/sdk` ^1.26) exposes the
client's declared capabilities after initialisation and can send an elicitation related to the tool request being
handled. Many MCP clients time a tool call out (the TypeScript SDK's default request timeout is 60 seconds).

## Decision

1. **A usage-data port in `mcp-core`**, optional on `McpCoreRuntimeDependencies`. `registerMcpTools` calls it after
   each tool's result is computed, with the occurrences, whether Lighthouse answered, and — when the client declared
   the `elicitation` capability — an `ask` function bound to the current tool request. `mcp-core` owns the copy
   (the approved text, A12) and the mapping of the client's answer; it reads no environment and opens no file.
2. **`mcp-stdio` elicits inside the first successful tool call, after the result is in hand and before it is
   returned.** Only when: `DO_NOT_TRACK` is not set, the store has no entry for this Lighthouse (re-read just before
   asking, since `lh` may have answered meanwhile), `state` lists `Mcp` in `AcceptedSources` and says `MayAsk`, the
   capability is declared, and no question has been put in this process yet. The request has no fields
   (an empty-object schema), so the assistant shows the message with its own accept and decline controls.
   - accept → yes: grant, then record it if the scope is still undecided (ADR-226 §6);
   - decline → No, recorded, final;
   - cancel, close, an error, or **50 seconds without an answer** → no answer, nothing recorded, not asked again until
     the next process start.
   The tool call that asked reports nothing, as in `lh`. Concurrent tool calls never wait for the question and never
   start a second one. The tool's own result is returned unchanged in every case.
3. **`mcp-stdio` uses the shared store** under its voter-key scope, so an answer given in `lh` stops the assistant
   asking, and the reverse. An assistant without elicitation never asks; `lh config usage-data on` against the same
   URL turns it on.
4. **`mcp-http` reads `LIGHTHOUSE_USAGE_DATA` once at start-up**: `on` in any case is on; unset or `off` is off; any
   other non-empty value is off with one warning line on stderr. `DO_NOT_TRACK` overrides it. Start-up prints one
   line, `Usage data: on (LIGHTHOUSE_USAGE_DATA)` or `Usage data: off`.
5. **`mcp-http` holds one grant per process, in memory.** It is requested on the first event, once (a single
   in-flight request shared by concurrent callers), only when `state` lists `Mcp` and does not say
   `AdministratorDisabled`; the young-install `MayAsk` rule does not apply, because nobody is being asked. Without a
   grant it re-reads `state` at most hourly, so a lifted veto takes effect without a restart. The record is ADR-226's
   shape and refresh rule, never written to disk; a restart is a new grant and a new pseudonym, and the old row ages
   out. The unit counted is one server process shared by its callers.
6. **No caller's credential is used for usage data** (ADR-227 §4): `mcp-http`'s per-request API key or bearer token
   never travels on a consent or ingest call.

## Alternatives considered

- **Elicit before the tool's Lighthouse call.** Rejected: it would ask while the Lighthouse may be unreachable, and it
  would differ from `lh`, which asks only after a command succeeded.
- **Return the result first and elicit afterwards, outside any request.** It would never delay a result. Rejected:
  whether an assistant shows an elicitation that belongs to no request is not something the protocol promises, and a
  question that is silently dropped looks like a cancel and is retried every session.
- **No bound on the wait.** Rejected: an assistant that times the tool call out at 60 seconds would lose the tool's
  answer, which AC-05.4 forbids. Fifty seconds stays inside the most common client default.
- **Persist the HTTP grant to a volume** (A17). Rejected: a mounted secret per deployment for an advisory count; the
  chart has no passthrough for it either.
- **Per-user consent on `mcp-http`**. Out of scope (M8).

## Consequences

- Positive: one question per Lighthouse per machine across the terminal and the assistant; the shared server follows
  one operator decision and never prompts anyone.
- Negative: the first successful tool call that asks returns only after the person answers, up to 50 seconds. That is
  the delay AC-05.4 allows ("beyond the person's answer").
- Negative: the accept and decline controls are labelled by the assistant, not by us; the approved copy's
  `[ Send usage data ] [ No ]` line describes the intent, and the message itself says what each choice does.
- Negative: an assistant whose tool timeout is shorter than 50 seconds loses the result of the one call that asked if
  the person takes longer than that timeout. Probe: an in-process SDK client with the elicitation capability and a
  slow answer pins the bound; the README names it.
- Chart users stay off until the chart can pass the variable (A13, follow-up).
