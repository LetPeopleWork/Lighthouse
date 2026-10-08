# RED classification — Story #6193, slices 01–05

**Wave**: DISTILL · **Date**: 2026-10-08 · **Lighthouse commit**: `42bcd476c` · **Clients commit**: `96df092`.

## Lighthouse (slice 01)

Every `[Ignore]` was lifted once and the class run. **18 of 18 pending cases fail on an assertion about
missing behaviour (`MISSING_FUNCTIONALITY`)**: the source is not on the forwarded message, a foreign source
is accepted (202, not 400), `acceptedSources` is absent from the state, the docs page has no source row.
None is `IMPORT_ERROR`, `FIXTURE_BROKEN`, `SETUP_FAILURE` or `WRONG_ASSERTION`.

Six cases first came back green and are now active guards rather than pending: today's browser batch, the
administrator's stop for `Cli` and `Mcp`, and a client without a live grant (three ways). They hold on
today's code by design and must hold on every slice.

## lighthouse-clients (slices 02–05)

Every skipped case was un-skipped once (a `.unskipped.test.ts` copy, `it.skip` → `it`,
`vitest run --reporter=json`), classified, and the copy deleted. The CLI and local MCP files were run
twice: against the committed scaffolds, and against a temporary shim (never committed) that wires today's
`lh` and today's local server into the same two entry points, to prove the scenarios fail on behaviour and
not on their own setup.

| File | Pending | Active | Against the scaffold | Against today's behaviour (shim) | Class |
|---|---|---|---|---|---|
| `packages/cli/src/usageDataQuestion.test.ts` | 61 | 0 | 61 throw | 34 assertion, 27 hold | MISSING_FUNCTIONALITY (scaffold) |
| `packages/cli/src/usageDataConfig.test.ts` | 27 | 0 | 27 throw | 27 assertion | MISSING_FUNCTIONALITY (scaffold) |
| `packages/cli/src/usageDataEvents.test.ts` | 68 | 0 | 68 throw | 44 assertion, 24 hold | MISSING_FUNCTIONALITY (scaffold) |
| `packages/cli/src/usageDataGuards.test.ts` | 0 | 8 | — | 8 pass (guards) | — |
| `packages/mcp-stdio/src/usageData.test.ts` | 30 | 0 | 30 throw | 14 assertion, 16 hold | MISSING_FUNCTIONALITY (scaffold) |
| `packages/mcp-http/src/usageData.e2e.test.ts` | 27 | 1 | — (no scaffold) | 27 assertion, guard passes | MISSING_FUNCTIONALITY |
| **Total** | **213** | **9** | | | |

"Throw" is `Not yet implemented -- RED scaffold`. "Assertion" is a Vitest assertion about what the
usage data path should have done: no question, no request recorded by the fake Lighthouse, no answer kept,
no start-up line, today's `Unknown config subcommand: usage-data`. One of them is an `ENOENT` on the
answers file `lh` should have written, which is the same missing behaviour.

**The 67 "hold" cases are claims of absence** (asks nothing, sends nothing, reports nothing, reads the
same). Today's `lh` and local server never ask or send, so these hold the moment the scaffold is replaced
and cannot drive code on their own. They stay pending rather than active because their driving port is a
scaffold; DELIVER un-skips each next to the positive scenario in the same describe that proves the
behaviour exists. This is a documented gap, not a fix: a per-scenario positive control would double
those scenarios.

mcp-http has no scaffold, so its absence cases were rewritten before this classification: five came back
green on the first run. The "same result" case now also asserts the event was handed in; the four "requests
no grant" cases first assert the start-up line says usage data is on. All 27 are now red against today's
server.

## Suite

- Lighthouse: `dotnet build` 0 warnings, 0 errors; the story class with the existing pipe and disclosure
  tests: 30 passed, 18 skipped.
- lighthouse-clients: `pnpm run ci` exit 0 (lint, test, typecheck, build, typecheck:tests). 71 files
  passed, 4 skipped; **1502 passed, 213 skipped (1715)**. The baseline before DISTILL was 1493 passed.
