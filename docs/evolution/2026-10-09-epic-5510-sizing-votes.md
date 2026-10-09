# Sizing votes — Epic 5510

**Pushed to `main`, not yet released** · ADO Epic #5510 *"Sizing votes against the SLE"* (`Community`, Resolved; it
closes with the release that carries it) · Stories #6148 (the SLE yardstick), #6149 (cast a vote), #6151 (readiness by
votes), #6150 (comments and the log), #6153 (votes with an account), #6154 (take a vote back), #6155 / #6156 (votes in
the clients), #6217 (a Refinement skill for assistants), all Closed; #6152 (votes hidden until you vote) Removed ·
Lighthouse commits `b79bb4846` (DISTILL, 2026-10-03) … `acbd64cf2` (clients mutation evidence, 2026-10-06) and
`ea472daa2` (mutation follow-up, 2026-10-08), then the user docs `10f98534b` and screenshots `4b49bf9cb` (2026-10-09);
about 175 commits name an E3 Story, interleaved on `main` with Epic 5881's · `lighthouse-clients` `8004dfd` …
`2088950` (#6155, #6156) and `588006a` (follow-up kill tests), `0bf7e0e` … `1514c85` (skills) · 50 roadmap steps
(10-01 … 10-06, 11-01 … 11-13, 13-01 … 13-11, 12-01 … 12-09, 15-01 … 15-03, 16-01 … 16-04, 17-01 … 17-04), every
step committed. The skill Story #6217 has its own workspace, `docs/feature/story-6217-lighthouse-skills/`.

Epic 5510 is the third of five Epics (E1–E5) cut from one combined workspace,
`docs/feature/epic-5510-5881-refinement/`. E1 #6136, the Refinement tab, is recorded in
[2026-10-03-epic-6136-refinement-tab.md](./2026-10-03-epic-6136-refinement-tab.md); E2 #5881, the refinement need,
was delivered alongside this Epic and is recorded in
[2026-10-09-epic-5881-refinement-need.md](./2026-10-09-epic-5881-refinement-need.md), which also gives the interleaved
order the two were built in. E4 #6137 (live sessions) and E5 #6138 (sizing calibration) are not started, so the
workspace stays where it is.

## What users get

The Refinement tab now answers **"is this Work Item small enough?"**: everybody votes, at any time and without a
meeting, on whether it is doable within the Team's SLE, and enough Yes votes make it Ready.

- **The question heads the vote column**: "Doable within 7 days? ⓘ" with the Team's SLE ("SLE 75% of work items in 7
  days or less"); without an SLE, the 85th percentile of the Team's Cycle Time over its Throughput window ("No SLE set,
  based off 85% of historical cycle time"); with nothing finished yet, "Doable within our SLE?".
- **Three answers on every row**: **Yes** and **No** in one click; **Yes, if…** opens a small dialog that asks "What
  has to be true for a Yes?" and will not vote without a condition. The voter's answer stays pressed; clicking another
  changes the vote, clicking the pressed one again **takes the vote back** ("Click again to take back your vote").
  Voting is open on every Work Item in refinement, all the time; there is no round.
- **Votes**: "No votes", "1 vote", "3 votes", with a comment icon when somebody wrote something. Clicking it opens
  **Votes and comments**: the split ("2 Yes · 1 Yes, if… · 0 No", hover a number to see who), then only what people
  **wrote** — comments and each "Yes, if…" condition, oldest first — and **Add a comment**. A comment from somebody
  who has not voted is an **open question** until they vote. Comments cannot be edited or deleted. Every vote and
  comment is visible to everybody, voted or not.
- **Readiness** (headed **Votes say** when the Team has stage rules): "Ready", "2 more Yes needed", "1 more voter
  needed" or "Needs discussion". Settings → Refinement → **Readiness by votes**: Yes votes needed (3) and voters needed
  (3) by default, and two discussion rules, both on by default — 1 or more No, or 2 or more "Yes, if…". A "Yes, if…"
  counts as a Yes towards Ready. Without stage rules the heading reads "· 2 ready by votes".
- **Warnings**: one column, last in the grid, with a single warning icon when a row needs attention (somebody asked
  and has not voted; the stage and the votes disagree) and nothing on a clean row; sortable.
- **Who you vote as**: with sign-in, your account, and Lighthouse asks for nothing (a sign-in not linked to a person
  is told "Your sign-in isn't linked to a person in Lighthouse, so it can't vote. Ask an admin to link your
  account."). Without sign-in, the first vote asks **Who is voting?** for a name "Kept in this browser only.", changed
  later from the dialog's "Voting as … · Change your name".
- **From the terminal and an assistant** (pushed, not yet released): `lh refinement get` gains Votes, Readiness and
  Warnings columns; `lh refinement vote | comment | take-back` and `lh config voter set --name`; MCP
  `lighthouse_team_refinement_vote | comment | voteTakeBack`, whose descriptions tell the assistant to show the user
  what it will send and call only after they confirm. A **Refinement skill** for assistants prepares a Product Owner
  ("is the Team ready?") and a developer ("what must I prep or vote on?") and ships as its own zip with the next client
  release.
- Demo data: on Team Gravity, Jonas Weber, Mo Okafor and Ana Lima have already voted; GR-059 is one Yes short of Ready
  and Ana's condition sits on GR-051.

Voting, comments and readiness are free for every installation. Votes under a verified account need sign-in, which is
Premium.

## What shipped

**Backend**

- `SizingLogEntries` (migration `AddSizingLogEntries` on both providers): Team, Work Item reference (not a foreign
  key, so a tracker refresh cannot orphan votes), kind (vote, comment, take-back), answer, comment (at most 2,000
  characters), voter key, voter profile and display name, when, channel (`Web`, `Cli`, `Assistant`; `LiveSession`
  waits for presenter mode) and the yardstick the vote was cast against. **Append-only**: the repository port offers
  `Append` and reads; a voter's current vote is their latest vote or take-back.
- `RefinementVotesController` — `POST …/work-items/{workItemId}/votes`, `POST …/comments`, `DELETE …/votes/mine`
  (which may name the answer it takes back, so a vote changed meanwhile in another session stays). Guarded by the new
  `TeamContribute` requirement, which is exactly Team read and non-disclosing, and rate-limited by
  `RefinementContribution` (30 entries a minute per voter). Only Work Items currently in refinement take entries.
- `VoterIdentityResolver` derives the voter on the server: `account:<subject>` with sign-in (refused when the credential
  belongs to no person), `self:` plus a hash of the browser's or client's random key and a required name without it.
  No response ever returns a key; entries carry `isMine`. `SizingRefusal` names every refusal with a code a client can
  act on.
- `SleYardstickResolver` (slice 10) and `RefinementResolution` (static, no I/O) for readiness, missing votes, the two
  discussion rules and the open question; `GET …/work-items/{workItemId}/log` returns the log and the names behind each
  part of the split.
- Found on the way and fixed: a call with an API key overwrote its owner's profile name and email, so a vote cast with
  the key was stored under "api-key-user". An API-key call now never creates or rewrites a profile.
- `RefinementModuleArchUnitTest` now also pins that the log port offers no edit, nothing updates or deletes in the
  table, every reader-level write lives in the votes controller, the tab's read touches no write command, and the
  migration cascades with the Team and sets null with a deleted profile.

**Frontend**

- `VoteControl`, `useVoteCasting`, `VoterNamePrompt`, `ConditionPrompt`, `VotesAndCommentsDialog`, `useCommentAdding`,
  `useSizingLog`, `ReadinessCell`, `YardstickQuestion`, `refinementWarnings`, `SizingLogService`, and the voter kept in
  `voterStore` / `useVoterIdentity`. The voter key is 32 random bytes from `crypto.getRandomValues`, because a page
  served over plain HTTP has no `crypto.randomUUID`; a browser that refuses storage still votes for as long as the page
  is open.
- `ReadinessSettings` in Settings → Refinement.
- E2E: the second walking skeleton in `specs/teams/Refinement.spec.ts` — a voter gives a name, says Yes on GR-059 and
  the votes make it Ready, on demo data.

**Clients** (`lighthouse-clients`, pushed, not yet released)

- The read gains the vote facts; the writes send the channel (`Cli` or `Assistant`) and, without sign-in, a name from
  `--as` or the stored one plus a random key minted per Lighthouse address and kept owner-only beside the client's
  config. The CLI refuses a nameless vote before calling the server, and a "Yes, if…" without `--comment`.
- The shared HTTP MCP server refuses sizing writes on an instance without sign-in, and with sign-in any write that
  brings no credential of its own, so it can never vote as the key it was started with.
- Story #6217: `skills/lighthouse-refinement/` with its eval cases; each skill folder is zipped on its own
  ([ADR-229](../product/architecture/adr-229-each-skill-is-a-folder-under-skills-zipped-on-its-own-under-its-folder-name.md)).

**Usage data** (listed in `docs/settings/usagedata.md`)

- `TeamSizingVoteCast = 13` and `TeamSizingReadinessReached = 14`, each with the closed enum `sizing_moment`
  (`NoCadence`, `OnRefinementDay`, `OnOtherDay`). A test per event reads the forwarded payload and finds nothing about
  the voter: no name, key, comment, address or Work Item. Since Story #6193 the CLI and MCP report them too.

Architecture: [ADR-216](../product/architecture/adr-216-the-sizing-log-is-append-only-and-keyed-by-a-voter-key.md),
[ADR-217](../product/architecture/adr-217-a-sizing-vote-is-a-write-gated-by-team-read-through-a-named-requirement.md),
[ADR-218](../product/architecture/adr-218-stage-readiness-and-the-hidden-split-are-one-pure-resolution-on-read.md)
(all built, with their delivery amendments in the status lines), `brief.md` → "Application Architecture —
epic-5510-5881-refinement" → "Built — E2 #5881 and E3 #5510", `ARCHITECTURE.md` §4 module 8, "A sizing log is
appended to, never edited" and "The reader-write rule".

## The maintainer's review — decisions and reversals

The voting UI changed more than any other part of the workspace. Later calls win over earlier ones.

- **The yardstick is one question with an info icon** (2026-10-03), no hint line and no settings link; the SLE tooltip
  uses the Team's word for Work Items. After the slice 03–04 review it moved into the vote column's header.
- **"Yes, but…" became "Yes, if…"** wherever a user reads it (2026-10-03). The stored value did not change.
- **Votes are never hidden** (2026-10-04, from the sketches). DESIGN and DISTILL had built the rule that you cannot see
  how others voted until you vote, in the API as well as the screen. The maintainer dropped it: a Product Owner who
  never votes still needs to read how the Team voted, and "not at first glance" is enough — the grid shows the count,
  the split is one click away. Story #6152 was Removed and its scenarios deleted.
- **Two discussion rules instead of one veto** (review of slices 11 and 13, 2026-10-04): 1 or more No, or 2 or more
  "Yes, if…", each with its own checkbox and threshold, either enough.
- **Comments and conditions** went through three shapes. First an optional "Condition" dialog; then (2026-10-05, before
  slice 12) "a condition is just a comment", "Yes, if…" in one click and one "Add a comment" box for everybody; then,
  in the running app, the **Warnings** column, a dialog with no vote trail (hover names on the split instead), and
  "Yes, if…" **requiring** its condition again in the screen while the server keeps it optional for the clients.
  Where an entry came from (web, terminal, assistant) is stored and counted but never shown. Comments cannot be
  deleted, and none is planned.
- **Take back by clicking your answer again** (2026-10-05), instead of a button in the dialog; nothing is written to
  the list of what people wrote.
- **Voting with sign-in asks for nothing** (2026-10-06): no name dialog, nothing kept in the browser, no "Voting as".
  A sign-in with no person behind it is told so in plain words.
- **Client votes** (2026-10-06): the list columns, one-line answers ("Recorded: Ana Lima — Yes, if… on GR-051. GR-051:
  2 more Yes needed."), never deriving a name from the operating system, and the confirm-first wording for assistants.
- **Icons in the Readiness column** (a check for Ready, a stop for discussion) were left as an idea to revisit once
  every readiness source exists.

## Decisions taken autonomously

- **DISTILL** (`distill/wave-decisions.md`, section "E3 Sizing votes"): the tab's read says whether the server knows the
  voter (account) or the browser must name one; readiness left out of a save stays as stored, and a Team that never set
  it reads 3 Yes and 3 voters; half an SLE (days without probability, or the reverse) counts as no SLE; a credential
  that belongs to no person cannot comment or take back either; taking back a vote you do not have answers with the
  unchanged row and writes nothing; the rate limit is pinned through the shipped settings (the 31st entry in a minute is
  refused and the current vote is unchanged); the yardstick captured on each vote is pinned below acceptance level,
  because nothing reads it until E5.
- **While the maintainer was away (2026-10-06)**: a take-back may name the answer it takes back; an API-key call never
  creates or rewrites its owner's profile. Both were held for the maintainer's review with the slices and confirmed
  at finalize (2026-10-09).
- **Clients copy** (`distill/clients-slice-17.md`): "Yes, if…" keeps its ellipsis in the Votes column; an unvoted row
  shows "No votes"; with sign-in the one-liner says "your"; "No vote of yours on GR-051 to take back from this client."
  exits 0 and sends nothing; `lh config voter` has show and set but no clear, as `config output` has. Approved at the
  maintainer's walk-through on 2026-10-06.

## Lessons

- **A rule built through two waves fell at its first sketch.** Hiding the split until you vote had an ADR, an API
  shape and ten backend scenarios before anybody looked at a screen; one sketch removed it. Together with E1's rebuilds
  this produced the rule to walk the UI sketches at the start of DISTILL.
- **The condition prompt was built three times in one day.** Each version matched what was decided at the time; the
  difference between "a reason for my vote" and "what has to be true first" only became clear in the running app.
  When a screen hinges on a distinction like that, review the running build before the slice's refactor, not after.
- **The browser's environment is part of the contract.** The first voter key came from `crypto.randomUUID`, which a
  page served over plain HTTP (a LAN install, the shipped docker-compose) does not have, so the first vote failed on
  exactly the installs that vote without sign-in. A Work Item reference holding a slash reached the server escaped and
  matched nothing. Both were found by review, not by the suite.
- **A fallback credential that is fine for reads is wrong for writes in someone's name.** The shared MCP server falls
  back to the key it was started with; for a vote that would have made every caller vote as one person. Writes now
  refuse without the caller's own credential.
- **A new caller surfaces old defects.** Resolving a voter's profile through an API key exposed that API-key calls had
  been rewriting their owner's profile all along.
- **Untriaged mutants become a follow-up.** The clients' votes run passed the gate overall (86.27 %) with two files
  below it and its survivors unclassified at the deadline; closing that took a separate pass two days later.

## Quality

| Gate | Result |
|---|---|
| Backend and frontend suites, build, Biome | Green before every push (backend run without the live-connector categories); CI green after each push, and each Story was closed only on green CI |
| E2E walking skeletons | The vote-to-Ready spec green live on demo data before each push from slice 13 on, alongside the other Refinement specs |
| Adversarial review (Opus), every slice | Findings fixed test-first; slice 10 had none blocking or high (2 medium and 7 low fixed), slice 12 twelve |
| Mutation, gate 80 % ([ledger](../feature/epic-5510-5881-refinement/mutation/results.md)) | Yardstick (#6148): backend 100 %, frontend 94.23 % · cast a vote (#6149): 97.73 % / 94.59 % · readiness (#6151): 100 % on the slice's lines / 92.75 % · comments and log (#6150): 93.43 % / 89.20 % · account votes (#6153): 97.85 %, no frontend change · take-back (#6154): 97.66 % / 97.67 % · client votes, server half (#6156, [ledger](../feature/epic-5510-5881-refinement/mutation/slice-17b-backend.md)): 100 % · client votes, clients half ([ledger](../feature/epic-5510-5881-refinement/mutation/clients-slice-17.md)): 86.27 %, then `voterKeyStore.ts` 86.39 % and `lighthouseUrl.ts` 80.65 % after the follow-up (`ea472daa2`) · skills (#6217 with #6245 and #6246, [ledger](../feature/story-6217-lighthouse-skills/mutation/clients.md)): 88.86 % → 96.50 % |

## DELIVER checklist

| Item | Answer |
|---|---|
| User-facing docs prose | **Done** in `10f98534b`: `docs/teams/detail.md` (voting, who you vote as, Votes and comments, readiness, warnings), `docs/teams/edit.md` (readiness by votes), `docs/concepts/concepts.md`. The usage-data rows were written with their slices. The Refinement skill's paragraph in `docs/aiintegration.md` is owed by Story #6217 once a client release carries its zip, so the page never links a file that does not exist yet |
| Per-feature screenshots | **Done** in `4b49bf9cb`: `features/refinement.png` (the vote, Votes, Readiness and Warnings columns) and `features/refinement_comments.png` (Votes and comments on GR-051) |
| Demo data | **Done** — three demo voters on Team Gravity, GR-059 one Yes short of Ready, a condition on GR-051; the vote-to-Ready walking skeleton runs on it |
| Website assets | **Done, not pushed** — website repository commit `117eb96`: Refinement on the Lighthouse page, What's New, a pricing row, and a third Sizing Poker next step pointing at the same question inside Lighthouse; one commit ahead of `origin/main` |
| Usage-data event in `docs/settings/usagedata.md` | **Done** — `TeamSizingVoteCast`, `TeamSizingReadinessReached` and the `sizing_moment` field, each tested to carry nothing personal |
| Lighthouse-Clients CLI / MCP | **Delivered and pushed (#6155, #6156, #6217), not released** — see below |
| Terminology | **N/A, because** every renamed word (Work Item, Refinement, SLE, Cycle Time) already existed; "Yes, if…" is an answer, not a configurable term |
| RBAC | **Done** — `TeamContribute`, the one write a Team reader may make, equal to Team read and non-disclosing; readiness settings ride the Team admin's settings write |

## Still open, knowingly

- **Not released.** Neither this Epic nor E1 or E2 is in a release (the last is `v26.10.3.6`). Epic #5510 stays
  Resolved until then; the baselines of the vote measures (votes cast away from the Refinement day, Work Items made
  Ready before it) wait for that release.
- **The clients release is owed** — votes in the CLI and MCP and the Refinement skill are on `lighthouse-clients`
  `main` but not published; the next client release needs `pnpm release:version` first. `docs/aiintegration.md` gains
  the skill once that release exists (Story #6217's own workspace tracks it).
- **The website commit `117eb96` is not pushed.**
- **Grids show the first 100 rows** (Story #6202): a Team with more than 100 Work Items in refinement cannot vote on
  the rest from the tab. Pre-existing.
- **The yardstick captured on each vote is stored and read by nothing** until sizing calibration (E5 #6138); the
  domain event the log was designed to publish for E4 and E5 is not published yet.
- **Icons in the Readiness column** — the maintainer's idea to revisit at the end of the Epic; not revisited.
- **Slice 10's check of how large real fallback values get** (a Team without an SLE) was not run: the dev instance was
  down at the time.
- **Clients votes survivors outside the two weakest files** were not classified one by one; the run passed the gate
  overall.
- **Traceability tag comments in the scenario files cite internal ids**, as E1 recorded; still a project-wide call.
- **E4 #6137 (presenter mode for a live session, and a spike for remote sessions) and E5 #6138 (sizing calibration,
  Premium) are not started.** The workspace stays for them.

Delivery history: `docs/feature/epic-5510-5881-refinement/` (shared with E1, E2, E4 and E5).
