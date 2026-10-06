# DISTILL — slices 17a + 17b, Lighthouse-Clients half (Stories #6155 "read votes and readiness", #6156 "cast, comment and take back a vote")

Scope: the clients show how the votes stand on every Work Item in refinement (17a), and let a person cast a
vote, add a comment and take their vote back from the terminal or through an assistant (17b), through the
same endpoints and under the same identity rules as the web page. One pass, one client release (minor).
The Lighthouse half (the facts and refusals on the wire) is
`Lighthouse.Backend.Tests/API/Integration/Refinement/Slice17ClientVotesScenarios.cs`; the example data is
shared with it (Ana Lima, Priya, Team Gravity id 3, GR-051 PDF export, GR-054 API version, GR-073 Bulk import).

Repository: `/storage/repos/lighthouse-clients`, scenarios committed as `8004dfd` (tests + empty changeset).
Test style: Vitest at the driving ports. Unlike slice 09, the CLI and MCP scenarios run the **production
client** (`createLighthouseClient`) against a fetch fixture that answers by method and path, so the wire facts
the server depends on (channel, name, key header, `?channel=` on take-back) are pinned end to end through the
surface the user touches; only Lighthouse itself is faked. The mcp-http scenarios start the real HTTP server
against a recording `node:http` Lighthouse, as its existing e2e tests do. Every scenario is `it.skip` /
`it.skip.each`; fixtures are plain data and local functions, nothing unimplemented runs at collection time.

Reconciliation passed — 0 contradictions between DISCUSS (DD-19, US-17a/b), DESIGN (DSN-12/17/21, DD-19
verdicts, identity model, MQ-3) and the maintainer's 2026-10-06 decisions below. DEVOPS was skipped for this
Epic; no environment matrix applies beyond auth off / auth on, which the scenarios cover.

## Maintainer decisions (2026-10-06, binding)

> **17a** — `lh refinement get` list gains columns `#  Work Item  Parent  State  Votes  Readiness  Warnings`:
> ```
>  1  GR-051  PDF export   GR-010  Refinement  1 Yes · 1 Yes, if*  2 more Yes needed
>  2  GR-054  API version  GR-010  Refinement  3 Yes · 1 No    Needs discussion     open question
>  3  GR-073  Bulk import  GR-012  Refinement  3 Yes           Ready                stage disagrees
> ```
> Votes = the split, omitting zero parts, `*` after it when the caller has voted (`myVote`); Readiness = the
> web's words (`describeReadiness`); Warnings empty when clean, `open question` (`hasOpenQuestion`),
> `stage disagrees` (`signalsDisagree`), comma-joined if both. MCP: the facts already on the wire, summary
> unchanged. The client sends its voter key header on the read if it has one, so `myVote` / `isMine` apply.
>
> **17b** — `lh refinement vote --team-id <id> --work-item <ref> --answer yes|yes-but|no [--comment <text>]
> [--as <name>]`, `lh refinement comment --team-id <id> --work-item <ref> --text <text> [--as <name>]`,
> `lh refinement take-back --team-id <id> --work-item <ref>`, `lh config voter set --name <name>` (show/clear
> if that is the house pattern for `config output`). One line each:
> ```
> Recorded: Ana Lima — Yes, if… on GR-051. GR-051: 2 more Yes needed.
> Recorded: Ana Lima — Yes on GR-073. That made GR-073 Ready.
> Recorded: Ana Lima's comment on GR-054.
> Took back Ana Lima's vote on GR-051. GR-051: 3 more Yes needed.
> ```
> "That made X Ready." only when `madeReady`. Auth off and no name from `--as` or the stored name → the CLI
> refuses before calling the server: `Give your name with --as "<name>", or store it once: lh config voter set
> --name "<name>".` Never derive a name from OS user / git / hostname. `--answer yes-but` without `--comment` →
> `A "Yes, if…" needs its condition: add --comment "<what has to be true>"`. Voter key: CLI and MCP-stdio mint
> one random key (≥ 32 chars, crypto random) per Lighthouse URL on first write and keep it in the client's
> local config store; take-back works from that same client only. With sign-in on, no name is needed or sent.
> Channel: CLI sends `Cli`, MCP sends `Assistant` (vote, comment and take-back — take-back via `?channel=`).
> Server refusals in plain words (`vote-needs-a-person` → "This key belongs to no person, so it cannot vote.
> Use a personal API key."; mirror the web where it has words). MCP tools `lighthouse_team_refinement_vote`,
> `lighthouse_team_refinement_comment`, `lighthouse_team_refinement_voteTakeBack`, writes, never read-only.
> Input `{ id, workItem, answer, comment?, voterName? }`. The vote and comment descriptions state: *"Records the
> USER's own sizing judgement under their name. Never call this on your own initiative or on someone else's
> behalf: show the user the Work Item, the answer and any comment you intend to send, and call only after they
> explicitly confirm."* and tell the assistant to ask the user for their name and never infer it. mcp-http
> refuses votes, comments and take-backs on an auth-off instance (MQ-3). Version gate for the writes: the
> slice 09 baseline pattern (`v26.10.3.6` is the last release without them).

### Decided in DISTILL (copy and behaviour the maintainer has not seen — confirm before 17c-02 / 17c-04)

1. **"Yes, if…" keeps its ellipsis in the Votes column**: `1 Yes · 1 Yes, if…*` (the sketch had `Yes, if*`;
   the answer's label everywhere else is "Yes, if…"). Parts in the order Yes · Yes, if… · No.
2. **A row nobody has voted on shows `No votes`** in the Votes column (the web's `describeVoteCount`), not a
   blank. The sketch did not cover the case.
3. **With sign-in on, the line says "your"**: `Recorded: your Yes on GR-051. GR-051: 1 more Yes needed.` —
   the write's answer carries no voter name, so the CLI cannot name the account (see inconsistencies).
   Take-back without a stored name, and every MCP take-back, likewise: `Took back your vote on GR-051. …`.
4. **Nothing to take back**: `No vote of yours on GR-051 to take back from this client.` on stdout, exit 0,
   and no take-back is sent — both when this client keeps no key for this Lighthouse and when the refinement
   read (with its key) shows no `myVote` on that Work Item. MCP says the same sentence, not an error.
5. **`lh config voter`** shows `Voter name: Ana Lima`, or `No voter name stored. Store one with: lh config
   voter set --name "<name>"`. `set` answers `Voter name set to Ana Lima.` **No `clear`**: `config output`
   has show and set only, so the house pattern has no clear; setting again replaces the name. A blank `--name`
   is refused naming `Missing --name`; a name over 100 characters with the server's own sentence `A name is
   at most 100 characters.` (the stored name is kept). The key is never printed.
6. **Refusals in words** (CLI stderr and MCP error text; the web's sentence where it has one):
   - `work-item-not-in-refinement` → `This work item is no longer in refinement.` (the web's, in the
     instance's terms lower-cased)
   - `comment-required` → `A comment needs some text.`; `comment-too-long` → `A comment is at most 2000
     characters.` (the web's)
   - 400 without a code titled `A name is at most 100 characters.` → that sentence
   - `voter-name-required` → CLI: the `Give your name with --as …` sentence; MCP: `Ask the user for their name
     and send it as voterName; never guess it.`
   - `vote-needs-a-person` → `This key belongs to no person, so it cannot vote. Use a personal API key.`
   - 429 → `Too many votes or comments from this client. Try again in a minute.` (the web says "browser")
   - mcp-http on an instance without sign-in → `Votes through the shared Lighthouse MCP server need sign-in,
     and this Lighthouse runs without it. Vote from the web page, the lh command line or an MCP server on your
     own machine.`
   - `voter-key-required` is not pinned: neither client can reach it (both always send a key without
     sign-in; mcp-http refuses first).
7. **MCP**: `answer` takes the wire names `Yes`, `YesBut`, `No` (what an assistant reads in `myVote` and the
   split); a `YesBut` without `comment` is refused before asking Lighthouse with `A "Yes, if…" needs its
   condition: add a comment saying what has to be true.`; bad arguments say `invalid answer` / `invalid
   workItem` (the house `invalid id` form). The comment tool input is `{ id, workItem, comment, voterName? }`,
   take-back `{ id, workItem }`. Results are labelled `vote:`, `comment:`, `takeBack:` and carry the row facts
   as the write left them plus `summary`, the same line the CLI prints. The comment tool's description pins
   the substance of the confirmation sentence (never on its own initiative or on someone else's behalf; only
   after explicit confirmation), since "the answer" does not fit a comment; the vote tool pins it verbatim.
8. **CLI argument refusals** name the missing or wrong option (`--team-id`, `--work-item`, `--answer`,
   `--text`) without asking Lighthouse; an `--answer` other than yes / yes-but / no is refused, a blank
   `--comment` on yes-but counts as no condition. `--json` / `--toon` hand back the row facts as the write
   left them, unchanged.
9. **Help** lists `lh refinement vote --team-id <id> --work-item <ref> --answer yes|yes-but|no`, `lh refinement
   comment --team-id <id> --work-item <ref> --text <text>`, `lh refinement take-back --team-id <id>
   --work-item <ref>` and `lh config voter set --name <name>`.
10. **Driving-port names** (DESIGN named only the commands and tools): client `getTeamRefinement(teamId,
    { voterKey }?)`, `castRefinementVote(teamId, workItem, { answer, channel, comment?, voterName?, voterKey? })`,
    `addRefinementComment(teamId, workItem, { comment, channel, voterName?, voterKey? })`,
    `takeBackRefinementVote(teamId, workItem, { channel, voterKey? })`; CLI dependencies `loadVoterName`,
    `saveVoterName`, `loadVoterKey(lighthouseUrl)`, `saveVoterKey(lighthouseUrl, key)`; mcp-core dependency
    `voterKeyStore: { load, save }` (mcp-stdio supplies one, mcp-http none). The key goes per call, so
    `createClient(connection)` keeps its shape.

## Scenarios

| # | Scenario | File :: test | Tags |
|---|---|---|---|
| C1 | The refinement read carries the client's voter key when it has one, none otherwise ×2 | `packages/client/src/refinementVotes.test.ts` :: reads the refinement with the client's voter key ($voterKey) so the caller's own vote is marked | @driving_port @us-17a @contract-shape:pure-function |
| C2 | A vote goes out with answer, condition, channel, name and key; the row comes back as the vote left it | same :: casts Ana's Yes, if… with its condition, from the command line, and hands back the row as the vote left it | @driving_port @us-17b @contract-shape:bounded-change |
| C3 | A comment goes out with its channel, name and key | same :: adds a comment without a vote, from an assistant | @driving_port @us-17b @contract-shape:bounded-change |
| C4 | A take-back is a DELETE naming its channel, with the key and no name | same :: takes back the vote this client cast, naming the channel it takes it back from and no name | @driving_port @us-17b @contract-shape:bounded-change |
| C5 | Older Lighthouse ×3 writes: upgrade message, nothing sent | same :: tells the caller to upgrade a Lighthouse that takes no votes yet, without sending $write | @error @contract-shape:unbounded-preservation |
| R1 | The list per the sketch: GR-051 / GR-054 / GR-073 with Votes, Readiness, Warnings | `packages/cli/src/refinementVotes.test.ts` :: tells Priya which Work Items are Ready, which need discussion and where the caller has voted | @driving_port @us-17a @contract-shape:pure-function |
| R2 | Votes column ×4: No votes; 2 Yes; 1 Yes, if… · 2 No; all three with `*` | same :: shows the votes as '$shows' | @boundary @us-17a |
| R3 | Readiness ×5: Ready; 1 more Yes needed; 1 more voter needed; 2 more voters needed; Needs discussion | same :: says '$says' for a Work Item that is $readiness | @boundary @us-17a |
| R4 | Warnings ×4: none; open question; stage disagrees; both | same :: warns '$warns' when a question is open: …, and the stage disagrees: … | @boundary @us-17a |
| R5 | The read sends the key kept for this Lighthouse; another Lighthouse's key or none → no header, none minted ×3 | same :: reads with the key this client keeps for this Lighthouse ($sends), … and makes none up | @us-17a @contract-shape:unbounded-preservation |
| V1 | **Walking skeleton**: Ana's Yes, if… with condition, `--as` → one line; wire carries Cli, name, a minted ≥32-char key that is kept | same :: records Ana's Yes, if… with its condition from her terminal, and tells her where GR-051 stands | @walking_skeleton @driving_port @us-17b @contract-shape:bounded-change |
| V2 | "That made GR-073 Ready." when the vote made it Ready | same :: tells Ana when her Yes is the one that made GR-073 Ready | @driving_port @us-17b |
| V3 | Name sources ×3: `--as` only; stored only; both → `--as` wins | same :: votes as '$sends' when the stored name is '$stored' and --as is '$as' | @us-17b |
| V4 | The minted key is reused; another Lighthouse gets its own | same :: keeps the key it minted and votes with it again, and keeps another for another Lighthouse | @us-17b @contract-shape:bounded-change |
| V5 | Sign-in on: no name sent even with one stored; the API key goes out; "your" | same :: with sign-in on, votes as the account behind the key and sends no name | @us-17b |
| V6 | No name ×2 (vote, comment): refused before any request, no key minted | same :: refuses $case without a name before asking Lighthouse, and never makes one up | @error @us-17b @contract-shape:unbounded-preservation |
| V7 | Yes, if… without condition ×2 (absent, blank): refused before any request | same :: refuses a Yes, if… without its condition ($condition) before asking Lighthouse | @error @us-17b |
| V8 | Missing / wrong option ×5 named, no request | same :: names $names when it is missing or wrong, without asking Lighthouse | @error |
| V9 | Ana's question on GR-054 recorded as a comment from the command line | same :: records Ana's question on GR-054 as a comment from the command line | @driving_port @us-17b |
| V10 | Take-back with the kept key, `?channel=Cli`, one line with where GR-051 stands | same :: takes back the vote Ana cast from this client, and tells her where GR-051 stands now | @driving_port @us-17b @contract-shape:bounded-change |
| V11 | Nothing to take back ×2 (no key kept; no `myVote`): says so, sends no take-back | same :: says there is nothing to take back when $situation, and takes nothing back | @error @us-17b @contract-shape:unbounded-preservation |
| V12 | Refusals in words ×7 (not in refinement, comment required / too long, name required, name too long, no person, 429) | same :: puts Lighthouse's refusal into words: '$says' | @error @us-17b |
| V13 | `--json` / `--toon` hand over the row unchanged ×2 | same :: hands over the row as the vote left it, unchanged, with $flag | @driving_port @contract-shape:pure-function |
| N1 | `config voter set` → show → vote without `--as` (chained) | same :: stores Ana's name once, shows it, and votes under it without --as | @driving_port @us-17b |
| N2 | Show with nothing stored gives the hint | same :: says how to store a name when none is stored | @us-17b |
| N3 | Refused names ×3 (absent, blank, 101 chars), stored name kept | same :: refuses to store $says and keeps what was stored | @error |
| N4 | Help lists the three commands and `config voter set` | same :: lists the new commands in the refinement and config help | @driving_port |
| M1 | Tools listed; vote description carries the confirmation sentence verbatim, comment its substance; `voterName` says ask, never infer; required fields | `packages/mcp-core/src/refinementVotes.test.ts` :: are offered with descriptions that make the assistant ask the user first, and ask their name | @driving_port @us-17b |
| M2 | Registered with `readOnlyHint: false`, `idempotentHint: false` ×3; answer enum enforced; the read stays read-only | same :: are registered as writes, never as safe to call freely | @us-17b |
| M3 | Vote through an assistant: Assistant channel, name, minted key saved; facts + summary line | same :: records Ana's Yes, if… through her assistant, marked as cast through an assistant, and states where GR-051 stands | @driving_port @us-17b @contract-shape:bounded-change |
| M4 | Comment and take-back ×2 go out as Assistant with the kept key; summary lines | same :: $label goes out marked as from an assistant, with the key this assistant keeps | @driving_port @us-17b |
| M5 | `lighthouse_team_refinement_get` reads with the kept key | same :: reads the refinement with the key this assistant keeps, so the user's own vote is marked | @us-17a |
| M6 | Refused before asking ×3: YesBut without comment, bad answer, no workItem | same :: refuses $case without asking Lighthouse | @error |
| M7 | Refusals in words ×3: name required, no person, not in refinement | same :: puts Lighthouse's refusal into words: '$says' | @error |
| M8 | Nothing to take back when the assistant never voted | same :: says there is nothing to take back when this assistant has never voted, and takes nothing back | @error @contract-shape:unbounded-preservation |
| H1 | mcp-http on a Lighthouse without sign-in refuses vote, comment, take-back ×3; nothing written | `packages/mcp-http/src/refinementVotes.e2e.test.ts` :: refuses $tool on a Lighthouse without sign-in, and sends Lighthouse nothing to record | @error @real-io @us-17b @contract-shape:unbounded-preservation |
| H2 | mcp-http with sign-in forwards the caller's own key, channel Assistant, no voter key | same :: with sign-in, records the vote as the caller's own credential, through an assistant, with no voter key | @driving_port @real-io @us-17b |

Counts: 37 scenarios, 77 test cases once the tables expand (client 8, CLI 52, MCP 13, mcp-http 4); 34 of the
77 are error cases and 13 more are column boundaries. Suite before: 397 passed. After: 397 passed, 77 skipped.
`pnpm lint`, `pnpm typecheck` green; the commit hook (`pnpm run ci` + changeset check) passed.

**Fail-for-the-right-reason**: every case was run un-skipped once and re-skipped. 73 failed on the missing
behaviour (client: `castRefinementVote` / `addRefinementComment` / `takeBackRefinementVote` not a function, no
voter key header on the read; CLI: `Unknown refinement subcommand: vote|comment|take-back`, `Unknown config
subcommand: voter`, four-column rows, no key header; MCP: `Unknown tool`, tools absent from the list and the
registration; mcp-http: `MCP error -32602: Tool … not found`). Three passed un-skipped and are deliberate
guards of today's behaviour — C1's no-key row and R5's other-Lighthouse and no-key rows (no header, nothing
minted). One more passed for the wrong reason (V8's `--team-id` row matched the help text printed under
"Unknown refinement subcommand"); it now also asserts the refusal is not the unknown-subcommand one.

Tier B (state-machine PBT): not declared. The vote → take-back journey is chained, but its state lives on the
server (whose acceptance covers it); the clients' own state is one name and one key per Lighthouse, whose
cases V3 / V4 / R5 / V11 enumerate. PBT not used: the columns, readiness words and refusals are finite tables
named from the web's own cases.

Adapter coverage: the production client is in the loop of every CLI and MCP scenario (fetch fixture at the HTTP
boundary); mcp-http runs its real server against a real `node:http` Lighthouse (H1, H2). Not covered by a
scenario: the CLI's file-backed voter store in `bin.ts` and mcp-stdio's store — both are adapter wiring for
17c-03 / 17c-05 and get unit tests there, the way `bin.test.ts` covers the config file today.

## DELIVER steps (clients half), in order

1. **17c-01 — client: refinement read with the voter key; the three writes.** `getTeamRefinement(teamId,
   { voterKey })` sends `X-Lighthouse-Voter-Key` when given. `castRefinementVote`, `addRefinementComment`,
   `takeBackRefinementVote` (`DELETE …/votes/mine?channel=…`), gated on a new entry at `v26.10.3.6`. Read the
   problem `code` (and title) from a refusal so the surfaces can word it (today `toResponseError` keeps only
   the status outside 409). Write types as string unions (`VotedRow` = row + `madeReady`). A shared
   `mintVoterKey()` (crypto random, ≥ 32 chars) belongs here so both surfaces mint the same way. Un-skips C1–C5.
2. **17c-02 — CLI: Votes / Readiness / Warnings columns.** Extend `refinementOutput.ts`; the words come from
   the shared wording in `client/src/refinementWording.ts` (readiness, votes) so the MCP summaries reuse them.
   The read sends the key kept for the connection's Lighthouse. **Update slice 09's `cli/src/refinement.test.ts`
   in the same step**: it pins the four-column header and rows exactly (`# Work Item Parent State`,
   `1 GR-051 PDF export GR-010 Refinement`). Re-sketch decided items 1–2 first. Un-skips R1 first, then R2–R5.
3. **17c-03 — CLI voter store.** `loadVoterName` / `saveVoterName` / `loadVoterKey(url)` / `saveVoterKey(url,
   key)` in `RunCliCommandDependencies`, file-backed in `bin.ts` in the existing config file (mode 0600);
   `lh config voter` show / set. Decide where a standalone connection's key lives (its lock file's URL).
   Un-skips N1–N3.
4. **17c-04 — CLI `vote`, `comment`, `take-back`.** Argument checks and the two pre-call refusals, name from
   `--as` then the store, nothing sent with sign-in, key minted on first write, channel `Cli`, the one-line
   outcomes, take-back reading `myVote` first, refusals in words, help text. Re-sketch decided items 3–6, 8
   and 9 first. Un-skips V1 (walking skeleton) first, then V2–V13 and N4.
5. **17c-05 — MCP tools + mcp-http refusal.** Three definitions with the descriptions, input schemas and
   `voterKeyStore` dependency; **fix the suffix rule** so the three are registered as writes (extend
   `isReadOnlyTool` or list writes explicitly — ARCHITECTURE.md §7 changes with it); `lighthouse_team_
   refinement_get` reads with the kept key; mcp-stdio supplies a store (resolve DESIGN's note 3: shared with the
   CLI's config file or its own — if its own, one person is two voters across CLI and assistant, documented);
   mcp-http supplies none and refuses all three on an instance without sign-in, using `queryServerAuthMode`
   (ARCHITECTURE.md §5 says nothing uses it yet — update). Extend the full tool-list assertion in
   `mcp-core/src/runtime.test.ts`. Un-skips M1–M8, H1, H2.
6. **17c-06 — release surface.** Replace the empty changeset with a **minor** changeset for client, cli,
   mcp-core, mcp-stdio and mcp-http; package READMEs, `skill/SKILL.md` (commands, tools, "ask before voting,
   ask the name"), ARCHITECTURE.md; `pnpm release:version` before the release run. No scenarios to un-skip.

## Found inconsistent between the design and the code

- **The verb-suffix rule would mark the three write tools read-only.** `isReadOnlyTool` treats anything not
  ending in `_refresh` / `_create` / `_update` / `_delete` as read-only and idempotent, so `…_vote`,
  `…_comment` and `…_voteTakeBack` (DSN-21's names) would tell MCP clients they are safe to call freely. M2 pins
  the correct annotations; 17c-05 must change the rule or the names.
- **DD-12 / DSN-16 (hidden split) is gone** — slice 14 was removed on 2026-10-04 and `RefinementRowDto` always
  carries the split. US-17a's "Hidden splits stay hidden in the client too" and AC-17a.2 have nothing to test;
  the clients show the split as the maintainer's sketch does.
- **The write's answer names no voter.** `VotedRowDto` is the row plus `madeReady`; with sign-in the client
  cannot say whose vote it recorded, hence "your" (decided item 3). A `voterName` on the answer would let both
  clients name the account.
- **A take-back that found nothing answers exactly like one that did** (`NothingTakenBack` → 200 with the
  row). The clients read `myVote` first to say so honestly; a distinct answer would save that read.
- **`LighthouseApiError` carries no refusal code**, and `voter-name-too-long` is sent without one
  (`namesTheReason: false`), so the clients word it from the problem title — brittle if the title changes.
- **The voter key store is the CLI's, not "the client package's".** DD-19 (b) puts it in the client package's
  local config store; the only config store is the CLI's `cli-config.json`, written by `cli/src/bin.ts`.
  mcp-stdio has none, which is DESIGN's open note 3.
- **A standalone connection has no URL in `CliConnection`**, while the key is kept per Lighthouse URL.
- **US-17b's elevator pitch is superseded**: `lighthouse teams refinement vote 3 GR-051 --yes-but …`, "Yes,
  but…", MCP `vote_on_work_item` → DSN-21's names and the web's "Yes, if…".
- **`parseTeamIdOption` hard-codes "for refinement get"** in its message; vote / comment / take-back need it
  per command (V8 asserts only that the option is named).
- **DESIGN names no server-version gate for the writes**; pinned at `v26.10.3.6` like slice 09's read. If a
  Lighthouse release ships the read without the writes before 17c-01 lands, the two baselines must differ.
- **The take-back defaults to `Web`** on the server when no channel is named, so a client that forgets
  `?channel=` is recorded as the web, silently; C4, V10 and M4 pin the channel.
