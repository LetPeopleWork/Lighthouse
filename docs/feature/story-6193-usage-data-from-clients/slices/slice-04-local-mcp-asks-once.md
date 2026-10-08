# Slice 04 — The local MCP server asks once through the assistant

**Story** #6193 / US-05 · **Job** `job-user-decide-once-whether-lighthouse-may-learn-from-me` · **Repo**
`lighthouse-clients` (`mcp-core`, `mcp-stdio`, `client`) · **Estimate** ~6h

## Goal

The local MCP server asks once via elicitation where the assistant supports it, shares the answer with `lh`
on the same machine, and reports the six mapped tool events with `source: Mcp`.

## IN scope

- Elicitation once per Lighthouse (D11): accept → yes, decline → No, cancel/close/time-out → no answer.
- The shared store from slice 02 (D4); `DO_NOT_TRACK` (D14); the veto (D7); the D3 check.
- The reporting hook in `mcp-core` for `lighthouse_forecast_manual`, `_team_refresh`, `_portfolio_refresh`,
  `_team_refinement_vote`, `_team_refinement_get` (D8), reused by slice 05.
- `mcp-stdio/README.md`; `docs/settings/usagedata.md` sentence on the assistant's question.

## OUT of scope

- `mcp-http` (slice 05). Any new tool.

## Learning hypothesis

**Disproves that elicitation reaches enough people to matter** — if Claude Desktop and the MCP Inspector do
not show the form, or show it in a way that loses the tool's answer, the stored-answer path is the only one
and the docs say "use `lh config usage-data on`".

## Acceptance criteria

AC-05.1 … AC-05.5. Risk carrier: **AC-05.5**, the question and a real forecast in a real assistant.

## Dependencies

Slice 02 (store, reporter); slice 03 (moment and verdict rules in `client`).

## Reference class

Story #6156's MCP vote tools (`refinementTools.ts`) — the stdio-only voter key read from the shared file.

## Pre-slice SPIKE

**Recommended, ≤ 1h**: which assistants in use (Claude Desktop, Claude Code, the Inspector) declare the
elicitation capability, and how each behaves when a tool call waits on it.
