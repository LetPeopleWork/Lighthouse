# Story 6193 — clients mutation results (slices 02–05)

- Tool: StrykerJS 10, command runner, sandboxed, `TZ=America/Adak`, line ranges the story touched.
  Configs in this folder: `stryker.6193.clients.json` (baseline) and `stryker.6193.clients.rerun.json`
  (the 13 files re-run after the follow-up tests).
- Baseline: **82.21 %** (1225 killed of 1490).
- After four test-only commits (`5835751`, `b5319cb`, `7204eb7`, `b778680`): **92.56 %** over the
  re-run's 13 files (1332 killed, 107 survived). No production code changed; no bug found.

| File | Before | After |
|---|---|---|
| cli `bin.ts` | 51.5 % | 97.0 % |
| cli `cliSession.ts` | 61.0 % | 82.9 % |
| cli `index.ts` | 89.4 % | 97.6 % |
| cli `refinementCommands.ts` | 91.9 % | 97.3 % |
| cli `usageDataQuestion.ts` | 95.2 % | 100 % |
| client `index.ts` | 67.4 % | 96.5 % |
| client `ownerOnlyJsonFile.ts` | 83.7 % | 83.7 % |
| client `usageData.ts` | 90.9 % | 97.9 % |
| client `usageDataReporter.ts` | 93.3 % | 95.1 % |
| client `usageDataStore.ts` | 85.5 % | 96.8 % |
| mcp-core `usageDataPort.ts` | 96.0 % | 98.0 % |
| mcp-http `bin.ts` | 82.2 % | 85.6 % |
| mcp-stdio `localServer.ts` | 66.3 % | 88.8 % |

## Survivors left, and why

- **`cliSession.ts` `openBrowser` (26 of 39).** Wired into the command dependencies, called by no
  command. It predates this story (moved out of `bin.ts` unchanged); removing it is a follow-up.
- **Equivalent.** An empty `readFile` encoding feeding `JSON.parse`; `writeFile` options followed by
  `chmod 0o600`; `{method: "GET"}` → `{}`; an empty `catch` returning `undefined` where `false` was;
  URL whitespace and trailing slashes that `new URL` and the client strip again; `"unavailable"` checks
  whose removal falls through to the same NOTHING plan; `null` read results that `readContent` catches.
- **Unreachable.** The interactive connect wizard's `prompt` (reads the real stdin); mcp-http's
  in-memory `answer()` (the operator port never asks) and its `renew` guards (the token cannot change
  mid-flight in one process); `switchUsageDataOn` rejecting.
- **Observable only by time.** The grant's 5 s timeout signal (3 mutants), left unkilled rather than
  adding 5 s to every run.
- **`ownerOnlyJsonFile.ts` (15).** Lock-race and timing details: the stale-lock boundary, the retry
  interval, `pause`, a vanished lock's retry path.
