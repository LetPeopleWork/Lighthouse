# Mutation testing — slice 06, every report through the clients (Story #6434)

Repository: `lighthouse-clients`, slice commits d79bdba..606d4ab. StrykerJS 10.1.1 with the Vitest runner, installed
in that repository for this slice (04ca64f, 4ad235e). Config: `stryker.6434.clients.json`, scoped to the changed line
ranges; Vitest scoped to the slice's suites by `vitest.stryker.6434.mutation.ts`, because the full suite's timezone
tests (America/Adak) fail under Stryker's runner.

| File (changed lines) | First run | After kills |
|---|---|---|
| packages/cli/src/reportCommands.ts | 91.67 % | 95.83 % |
| packages/client/src/index.ts | 100 % | 100 % |
| packages/client/src/reportWording.ts | 96.10 % | 98.70 % |
| packages/mcp-core/src/reportTools.ts | 92.31 % | 92.31 % |
| **All** | **94.83 %** (110 killed, 6 survived) | **97.41 %** (113 killed, 3 survived) |

Killed in aaf58bc: terminology read for `--json` (now pinned: a JSON list never asks for the terms), and a row with an
owner but no id accepted as a report.

Remaining survivors, all equivalent or cosmetic:

- `reportCommands.ts:138` `terms === undefined` → `false`: for `--json`/`--toon` the summariser is never called, so
  passing one changes nothing.
- `reportWording.ts:250` `?? ""` → `?? "Stryker was here!"`: either key misses `OWNER_KINDS`, same result.
- `reportTools.ts:88` the tool description emptied: assistant-facing prose; no test pins description wording for any
  tool.
