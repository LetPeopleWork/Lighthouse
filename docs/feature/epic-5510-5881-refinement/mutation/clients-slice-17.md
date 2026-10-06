# Mutation testing — slices 17a/17b "Votes in the clients" (stories 6155, 6156), lighthouse-clients

- Repo: `lighthouse-clients`, production code frozen at `2088950`. No killing tests written (deadline moved to 17:05; gate already met).
- Tool: StrykerJS 10.0.0, `command` runner (`vitest run --bail=1`), sandbox mode, `tsconfigFile: no-tsconfig-rewrite.json`, concurrency 11, `TZ=America/Adak`. Setup reused from slice 09 (`clients-slice-09.md`).
- Gate ≥ 80 %: **86.27 %** (817 killed / 130 survived / 947 mutants; 0 timeouts, 0 errors, 0 no-coverage). 16 min.

## Scores per file

| file | scope | mutants | killed | survived | score |
| --- | --- | ---: | ---: | ---: | ---: |
| `client/src/voterKeyStore.ts` | whole file | 169 | 121 | 48 | 71.60 % |
| `client/src/refinementVoting.ts` | whole file | 30 | 27 | 3 | 90.00 % |
| `client/src/refinementVoteWording.ts` | whole file | 143 | 131 | 12 | 91.61 % |
| `client/src/lighthouseUrl.ts` | whole file | 31 | 24 | 7 | 77.42 % |
| `client/src/index.ts` | 1198-1199, 1606-1626, 1720-1759, 2101-2111, 2249-2329 | 101 | 92 | 9 | 91.09 % |
| `cli/src/refinementCommands.ts` | whole file | 178 | 165 | 13 | 92.70 % |
| `cli/src/index.ts` | 1131-1150, 2314-2366, 2412-2413 | 69 | 64 | 5 | 92.75 % |
| `mcp-core/src/refinementTools.ts` | whole file | 200 | 167 | 33 | 83.50 % |
| `mcp-core/src/index.ts` | 1581-1587, 1703-1705 | 7 | 7 | 0 | 100.00 % |
| `mcp-http/src/bin.ts` | 187-192, 245-267, 339-345 | 19 | 19 | 0 | 100.00 % |
| **all** | | **947** | **817** | **130** | **86.27 %** |

Line spans from `git diff -U0 8004dfd..HEAD`, type-only and import-only hunks left out.

## Survivors (unclassified — not triaged before the deadline), by line

- `voterKeyStore.ts` (48): 66, 74, 87-95, 102-107, 124-142 (lock timing constants, stale-lock check), 160-174 (lock wait / give-up / retry backoff), 190-198 (atomic write: temp name, chmod, cleanup on failure), 223-243 (legacy scope / API path / legacy-key move), 274.
- `refinementTools.ts` (33): 167-179, 194-195, 219, 251, 276, 348, 362, 402-410, 449-468.
- `refinementCommands.ts` (13): 126, 161, 175, 205, 232, 269, 273, 317-318, 356, 371.
- `refinementVoteWording.ts` (12): 126-131 (`NAME_TOO_LONG` regex / `isNameTooLong`), 169-176 (`describeVoteRefusal` key-required and name-too-long branches).
- `client/index.ts` (9): 1624, 1725-1726, 1738-1739 (`getProblemString` trim/empty checks), 2322.
- `lighthouseUrl.ts` (7): 6, 13, 18.
- `cli/index.ts` (5): 2324, 2325, 2333, 2345, 2349 (config voter).
- `refinementVoting.ts` (3): 24, 33.

Biggest gap: `voterKeyStore.ts` concurrency/lock and atomic-write paths; many are likely timing-equivalent (backoff constants) but the legacy-move and temp-file cleanup ones look real.

## Command

```
TZ=America/Adak $S/node_modules/.bin/stryker run $S/../mut6156/stryker.config.json   # from the lighthouse-clients root, $S = scratchpad/mut6147
```
`git status` in lighthouse-clients was clean after the run.
