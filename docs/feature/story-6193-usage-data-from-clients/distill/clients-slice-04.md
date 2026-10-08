# DISTILL — slice 04, the local MCP server asks once (US-05)

Repository `/storage/repos/lighthouse-clients`, commit `96df092`. The server as `runMcpStdioRuntime` builds
it, through the scaffold `createLocalLighthouseMcpServer(env)` (`packages/mcp-stdio/src/localServer.ts`),
connected to an SDK `Client` over `InMemoryTransport.createLinkedPair()`. The client may or may not declare
elicitation and answers it as each scenario says; the fake Lighthouse is on a real socket and the answers
file lives in a temporary home shared with `lh` where a scenario needs both.

## Files

| File | Pending | Active |
|---|---|---|
| `packages/mcp-stdio/src/usageData.test.ts` | 30 | — |

## Scenarios

- Priya is asked once in her assistant, in the approved words, while her forecast reaches her unchanged;
  her later tool calls are reported with source `Mcp` and she is never asked again.
- Marco's decline is kept, and his `lh` asks nothing afterwards (one answers file for both).
- `@error` A closed question keeps nothing, is not asked again this session, and is asked in the next.
- `@boundary` After 50 seconds of silence the forecast is handed over and nothing is kept (fake timers).
- Three calls at once ask once.
- Lena ran `lh config usage-data on`; her refresh through an assistant that cannot ask is reported with
  source `Mcp`.
- The stored answer decides: nothing asked or sent when the assistant cannot ask and nobody answered, when
  `lh` holds a No, under the veto, a young install, a predating or Mcp-less or usage-data-less Lighthouse,
  `DO_NOT_TRACK`; a yes `lh` gave since the server started is read, not asked for again.
- The six mapped tools report the web's events with source `Mcp` (6 rows); the unmapped tools report
  nothing (5 rows including `lighthouse_team_list`); `@error` a refused refresh reports nothing.
- `@kpi` A refresh returns at once (under 900 ms) though the Lighthouse never takes the event.

## DELIVER order

Replace `createLocalLighthouseMcpServer`'s body and have `runMcpStdioRuntime` connect what it returns;
then the question, the stored answer, the tools.

## Note for DELIVER

16 of these are claims of absence and pass on day one once the scaffold is replaced. Un-skip them next to
the positive scenario in the same describe.
