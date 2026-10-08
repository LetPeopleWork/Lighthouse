# DISTILL — slice 05, the shared MCP server, operator's call (US-06)

Repository `/storage/repos/lighthouse-clients`, commit `96df092`. The existing
`runMcpHttpRuntime(env, write, writeError, onServerStarted)` on a real port, SDK clients over HTTP, the
fake Lighthouse on a real socket, `HOME` a temporary directory. No scaffold: the runtime exists today.

## Files

| File | Pending | Active |
|---|---|---|
| `packages/mcp-http/src/usageData.e2e.test.ts` | 27 | 1 |

## Scenarios

- Active guard: no usage data request when `LIGHTHOUSE_USAGE_DATA` is not set.
- Off unless the operator says on: `Usage data: off` at start-up when unset; `on` / `ON` / `On` switch it
  on and say `Usage data: on (LIGHTHOUSE_USAGE_DATA)` once; `off` / `OFF` / `""` stay off without a
  warning; `@error` a typo starts off with one warning naming on and off; `DO_NOT_TRACK` wins over `on`.
- Switched on: Priya's refresh is reported with source `Mcp` and she is never asked; forecast and
  portfolio rows; every caller gets the same result as with usage data off (with a positive control that
  the event was handed in); three callers at once cause one grant; a restart requests a fresh grant; an
  instance installed less than three days ago still reports (`mayAsk` false is about asking, not
  sending).
- `@version-skew` A vetoed Lighthouse is asked again only after an hour (fake `Date`) and reported to once
  the veto is lifted.
- What it never does: `@security` send an API key or bearer token on a usage data call; write anything
  under its home directory. The "requests no grant" cases each first assert the server started with usage
  data on, so they cannot pass on today's server.

## DELIVER order

Start-up line and variable parsing, then the grant and reporting, then the veto re-read.
