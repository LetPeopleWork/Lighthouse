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

## Follow-up 2026-10-08

The two weakest files of this run, re-run scoped (`mutate` = the two files whole) at lighthouse-clients
`ecf1e60` before the killing tests and at `b6ee9f0` after them. Same setup as above (StrykerJS 10.0.0,
`command` runner, sandbox, `tsconfigFile: no-tsconfig-rewrite.json`, concurrency 11, `TZ=America/Adak`).
Killing tests: `b6ee9f0 test(client): kill surviving mutants in the voter key store and Lighthouse URLs
(story 6156)`, tests only, no production code changed. 200 mutants, 0 timeouts, 0 errors, 0 no-coverage
in both runs; 4 and 3 minutes.

| file | slice 17 (`2088950`) | before (`ecf1e60`) | after (`b6ee9f0`) | survivors left |
| --- | ---: | ---: | ---: | --- |
| `client/src/voterKeyStore.ts` | 71.60 % (48 of 169) | 76.33 % (40 of 169) | **86.39 %** (23 of 169) | all equivalent, timing-only or race-only |
| `client/src/lighthouseUrl.ts` | 77.42 % (7 of 31) | 64.52 % (11 of 31) | **80.65 %** (6 of 31) | all equivalent |
| both | | 74.50 % (51) | **85.50 %** (29) | |

The "before" column is lower than slice 17's for `lighthouseUrl.ts`: the slice-17 run counted some kills
that came from whole-suite flakes under load (`--bail=1` stops at any failing test), and a few of those
mutants survive a quieter run. The same effect moved seven mutants (`lighthouseUrl.ts` line 6; `voterKeyStore.ts`
lines 133, 160, 174, 233) from killed in the "before" run to survived in the "after" run; each is listed below.

### Killed (29 of the 51 "before" survivors)

| where | what survived | killing test |
| --- | --- | --- |
| `voterKeyStore.ts` 74 | the default folder `~/.config/lighthouse-clients` (`.config` → `""`) | the default path asserted in full |
| `voterKeyStore.ts` 66 | `STANDALONE_VOTER_KEY_SCOPE` → `""` | the standalone constant is kept under `standalone` |
| `voterKeyStore.ts` 87 | the `typeof … === "object"` check (three mutants) | a file whose `keys` is text, `null`, a number map or missing reads as unreadable and is never written over |
| `voterKeyStore.ts` 95, 104 | the unreadable-file error itself, and any read error other than "no file" read as an empty file | the same, plus a folder where the file should be; asserted with the exact `Error`, because `rejects.toThrow("text")` also passes when the rejection is `undefined` |
| `voterKeyStore.ts` 141-142 | an error other than "lock exists" swallowed as "lock taken by someone else" | a save into a folder it may not write to fails at once with `EACCES` instead of retrying forever |
| `voterKeyStore.ts` 163-165 | the give-up check and its message | a lock another client keeps holding: the save gives up after at least 4.5 s with the "in use" message and leaves that lock alone |
| `voterKeyStore.ts` 197-198 | the cleanup after a failed write or rename | a write and a rename that fail (mocked `ENOSPC`): the error comes through, the old file is whole, no temporary file is left |
| `voterKeyStore.ts` 223 | the legacy name's trim and trailing-slash run | a key an earlier version kept, found under the URL given with spaces and `//` |
| `voterKeyStore.ts` 225, 237 | `/api` matched anywhere instead of as the last segment; a name that is no URL | the name a key is kept under: `/apis`, `/myapi`, `/api/team-a`, `/team-a/api/`, `my-lighthouse/` |
| `voterKeyStore.ts` 242-243 | the legacy move dropping every other key | a move with a second Lighthouse's key in the file keeps that key |
| `voterKeyStore.ts` 274 | `keys?.[…]` → `keys[…]` on an unreadable file | loading a URL from an unreadable file reads no key instead of throwing |
| `lighthouseUrl.ts` 13 | the http(s) scheme check | `ftp:`, `file:` and `mailto:` URLs are no Lighthouse URL |
| `lighthouseUrl.ts` 17-18 | one trailing slash stripped instead of all; a path dropped | a path with `//` at the end; a path kept |
| `lighthouseUrl.ts` 23 | the `catch` returning `undefined` | text that is no URL reads as `null` |

### Survivors left, justified (29)

| n | where | mutant | why it cannot be killed, or need not be |
| ---: | --- | --- | --- |
| 1 | `lighthouseUrl.ts` 6 | `value.trim()` → `value` | `new URL` drops leading and trailing spaces itself, and a blank string throws and reads as `null` either way. Equivalent. |
| 2 | `lighthouseUrl.ts` 7 | the empty-string guard (`false`, empty block) | `new URL("")` throws and the `catch` returns `null`. Equivalent. |
| 3 | `lighthouseUrl.ts` 18 | `pathname.length === 0 \|\| pathname === "/"` (`false` twice, empty block) | after the trailing slashes are stripped the path is never `/`, and an empty path gives `${origin}` + `""`, the same as `origin`. Equivalent; the branch could go. |
| 1 | `voterKeyStore.ts` 88 | `value !== null` → `true` | `Object.values(null)` throws inside the `try`, which reads the file as unreadable, as the guard does. Equivalent. |
| 1 | `voterKeyStore.ts` 93 | `?.code` → `.code` | file system calls always reject with an `Error`; the code is never read off `null`. Equivalent. |
| 1 | `voterKeyStore.ts` 102 | `"utf8"` → `""` | the file still reads and `JSON.parse` takes it as text. Equivalent. |
| 1 | `voterKeyStore.ts` 107 | `?.keys` → `.keys` | a file holding `null` throws inside the `try` and reads as unreadable either way. Equivalent. |
| 1 | `voterKeyStore.ts` 124 | `pause` does not wait | the lock is retried at once instead of after 5-100 ms; same outcome, timing only. |
| 1 | `voterKeyStore.ts` 131 | `>` → `>=` on the 10 s staleness | a one-millisecond boundary. Timing only. |
| 4 | `voterKeyStore.ts` 132-133 | `isStale`'s `catch` (empty block, `ENOENT` → `true`/`false`/`""`) | `stat` fails only when the lock vanished between the failed take and the look at it; every variant retries the take, sooner or later. Race and timing only. |
| 2 | `voterKeyStore.ts` 160 | `rm(lockPath, { force: true })` → no `force` | fails only when two clients clear the same stale lock in the same instant. Race only, not reproducible in a test. |
| 1 | `voterKeyStore.ts` 163 | `>` → `>=` on the 5 s give-up | a one-millisecond boundary. Timing only. |
| 1 | `voterKeyStore.ts` 169 | `Math.min` → `Math.max` | every retry waits 100 ms instead of backing off from 5 ms. Timing only. |
| 2 | `voterKeyStore.ts` 174 | releasing the lock without `force` | the lock is the save's own file and still there, unless another client took it as stale after 10 s. Equivalent short of a save that hangs for 10 s. |
| 3 | `voterKeyStore.ts` 190-193 | the temporary file's write options (all dropped, `encoding` → `""`, `flag` → `""`) | `chmod` right after sets owner-only before the rename, so no one finds the key file readable; `wx` only guards a name that carries the process id and 48 random bits; the data is text, written as UTF-8 either way. Equivalent (the `mode` and `chmod` each back the other up). |
| 3 | `voterKeyStore.ts` 233 | the `standalone` branch (`false`, no `trim`, empty block) | a name that is no URL is kept trimmed and without trailing slashes, which for `standalone` is `standalone`. Equivalent; the branch states the intent. |
| 1 | `voterKeyStore.ts` 274 | `legacyScope === scope ? undefined : …` → always look | when the two names match, the key under that name was already looked for and is not there. Equivalent. |

### Command

```
TZ=America/Adak $S/node_modules/.bin/stryker run $S/stryker.vk-after.json   # from the lighthouse-clients root, $S = scratchpad/mut6218
```
`stryker.vk-before.json` is the same file with the reports in `reports-vk-before`. The sandbox and the
vitest transform caches it left under `/tmp` were removed after the runs; `git status` in
lighthouse-clients was clean.
