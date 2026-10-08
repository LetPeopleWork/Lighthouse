# ADR-226: A client keeps one usage-data answer per Lighthouse in its own owner-only file beside the voter keys, shared by `lh` and the local MCP server

- **Status**: Proposed (2026-10-08, DESIGN wave for ADO Story #6193). Interaction mode = **propose**, maintainer AFK.
- **Feature**: `story-6193-usage-data-from-clients` — repo `lighthouse-clients` (package `client`)
- **Relies on**: ADR-225 (`AcceptedSources`), ADR-190 §2 (the server's gate and liveness window), ADR-191 §4 (a
  re-grant mints a new pseudonym).

## Context

The maintainer decided that `lh` asks once per Lighthouse URL (M1, M3), that a No is final for that URL (M5), and that
`lh` and the local MCP server share one stored answer (M7, A16). A client says yes by calling the browser's own
`POST consent` with `granted`, which returns a token. The token is a capability: it can revoke, and it is what the
server resolves to the pseudonym. So it must be stored, owner-only, per Lighthouse, by whichever of the two surfaces
answered first, and read by the other.

The clients already keep one secret per Lighthouse shared by both surfaces: `voter-keys.json`
(`client/src/voterKeyStore.ts`). It is owner-only, takes a lock file for one writer at a time, re-reads under the
lock, writes atomically by rename, never writes over a file it cannot read, and names a Lighthouse with
`getVoterKeyScope`. All of that is exactly what this answer needs.

## Decision

1. **A second file, `usage-data.json`, in the voter key file's directory** (the directory of
   `$LIGHTHOUSE_CLI_CONFIG_PATH`, else `~/.config/lighthouse-clients/`), mode `0600` in a `0700` directory.
2. **The same scope rule**: `getVoterKeyScope(lighthouse)` — `standalone` for the desktop app, otherwise the
   normalised server URL. One function decides "which Lighthouse" for both stores, so `lh` and `mcp-stdio` always
   agree on whose answer they are reading.
3. **The format**:
   `{ "version": 1, "answers": { "<scope>": { "answer": "yes", "token": "…", "confirmedAt": "<ISO-8601 UTC>" } } }`,
   or `{ "answer": "no", "decidedAt": "<ISO-8601 UTC>" }` for a No and for an off after a yes (both final). No entry
   means "not asked yet". A file whose version or shape is not this is **unreadable**: never written over, no
   question asked, nothing sent.
4. **The lock, the atomic write and the readable/unreadable read are extracted** from `voterKeyStore.ts` into one
   internal module (`client/src/ownerOnlyJsonFile.ts`), behaviour-preserving, in a refactor commit before the slice's
   feature commit, with the voter key tests unchanged and green. Both stores use it. The voter key messages keep
   their wording.
5. **Liveness without asking again.** A yes stores `confirmedAt`. Before a send, when `confirmedAt` is more than
   24 hours old, the client reads `GET state` with its token:
   - `AcceptedSources` lacks the client's source → send nothing, change nothing (the server cannot label it today);
   - `Decision` is `Granted` → the read itself refreshed the server's `LastSeenAt`; update `confirmedAt`, send;
   - anything else (the row was pruned after 30 unseen days) → **re-grant silently** (`POST consent`), store the new
     token, send. The person said yes and has not changed their mind; the new pseudonym is the correct ADR-191 §4
     consequence of the old one having lapsed.
   A send also keeps the grant alive on its own (the ingest path touches `LastSeenAt`), so a client in daily use
   reads `state` at most once a day.
6. **First answer wins.** Recording an answer from a question (`lh`'s prompt, MCP elicitation) is a compare-and-set
   under the lock: written only if the scope still has no entry, so a person who answered in the terminal while the
   assistant was asking is not overruled by the second answer. `lh config usage-data on|off` replaces the entry
   unconditionally — it is the person's explicit, later decision.
7. **Off after a yes**: `DELETE consent` with the token, then the entry becomes `no` and the token is gone. If the
   delete cannot reach Lighthouse the entry still becomes `no`; the orphaned grant stops counting after the liveness
   window (ADR-190 §2).
8. **A No is never posted.** It stays in this file (AFK default A2). The web posts `declined` so the server can stop
   re-asking a browser that has nowhere else to remember; a client remembers itself.
9. **The token never appears in any output, log, error or tool result**, and the store's own error messages name the
   file, never its contents.
10. **`mcp-http` keeps no file.** It holds the same record in memory (ADR-228).

## Alternatives considered

- **Widen `voter-keys.json` with a `usageData` member.** One file, one lock. Rejected, decisively: every released
  `lh` and `mcp-stdio` rewrites that file as `{ version: 1, keys }` on its next voter-key save. A person running an
  older MCP server beside a newer `lh` (an MCPB bundle is updated separately from npm) would have their stored No
  silently erased, and would be asked again — the one promise (M5) this store exists to keep.
- **Inside `cli-config.json`**. Rejected: `mcp-stdio` does not read the CLI's config, and `bin.ts` writes that file
  without a lock, so two surfaces writing it would lose answers.
- **The OS keychain**. Rejected: a native dependency in three distribution channels (npm, Bun binaries, MCPB),
  unavailable on headless Linux, and the voter key — a capability of the same weight — already lives in a `0600`
  file.
- **One store per surface**. Rejected by the maintainer (A16): one answer per Lighthouse per machine.

## Consequences

- Positive: `lh` and the local MCP server hold one answer, one token and therefore one pseudonym per Lighthouse per
  machine; older clients cannot erase it; an unreadable usage-data file never blocks voting, and the reverse.
- Positive: the lock and atomic-write code exists once, so a fix to it reaches both stores.
- Negative: a second file next to the first, and a refactor of a shipped module. The refactor is pinned by the
  existing `voterKeyStore.test.ts` and `voterKeyStore.atomicWrite.test.ts`, which must pass unedited.
- Negative: one extra `GET state` per Lighthouse per machine per day while usage data is on.
- Earned Trust: the store's guarantees are probed, not assumed — two real processes recording different answers at
  once (the voter key's atomic-write test pattern) must leave exactly the first; a truncated or foreign-shaped file
  must be left byte-identical; on POSIX the file's mode must read back as `0600`.
