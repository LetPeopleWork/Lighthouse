# Wave Decisions — DISTILL — epic-5510-5881-refinement (Epic #6136, E1 only)

**Agent**: Quinn (`nw-acceptance-designer`) · **Date**: 2026-10-03 · **Mode**: autonomous subagent, maintainer AFK
**Scope**: E1 — slice 01 (US-01, ADO #6139) and slice 02 (US-02, ADO #6140). E2–E5 are not distilled here.
**Predecessors**: DISCUSS (DD-1..DD-22), DESIGN (DSN-1..DSN-22, ADR-214..218), DEVOPS (DVO-1..DVO-8).
Full reasoning: `feature-delta.md` → `## Wave: DISTILL / …`. RED evidence: `distill/red-classification.md`.

## Phase 0

`[lang-mode] csharp+typescript` (backend `*.csproj`, frontend `package.json` + `tsconfig.json`, E2E Playwright).
`[policy-mode] inherit` — `docs/architecture/atdd-infrastructure-policy.md` read and applied; no port in scope was
missing, so no row was appended (the one fake added, `ILighthouseClock` → `FakeLighthouseClock`, is already a row).
`[port-mode] n/a` — the policy records that the Python state-delta port does not apply to this C#/TS project; the
universe guard is asserted directly (Work Item row count, settings read-back unchanged, a second read identical).

## Reconciliation gate (DISCUSS ↔ DESIGN ↔ DEVOPS)

**Reconciliation passed — 0 contradictions.** Three DISCUSS statements are superseded by DESIGN, and each is a
documented, reasoned correction in `design/upstream-changes.md`, not a conflict; the scenarios apply DESIGN's reading:

| DISCUSS | DESIGN / DEVOPS reading applied | Source |
|---|---|---|
| AC-2.1 "same backlog order the Team's forecasts use" | the tracker's rank via `FeatureComparer.CompareOrderValues`, ties by id | DSN-6 |
| AC-2.2 "… category, age" on every row | Work Item Age on Doing rows only (MQ-4 default) | DSN-6 note |
| Slice 01 OUT "Terminology key (02)" | `refinement`/`refinements` land in slice 01 | DSN-20 |
| DD-16 tab-open reuses `TeamTabOpened` | + route key `TeamDetail_Refinement`; `TeamRefinementConfigured` name-only, slice 01 | DVO-1 |

Settled maintainer calls re-checked: the word is **Refinement** (renameable); everything in slices 01–02 is
**Community** (no licence gate in any scenario — the premium demo scenario is only where Team Gravity lives);
votes, need number and pull-only async belong to later slices and nothing here contradicts them; the usage-data
events DEVOPS assigned to slices 01–02 each have scenarios.

## Decisions taken autonomously (DST-n)

| # | Decision | Why this default |
|---|---|---|
| DST-1 | **Wire shape pinned by the scenarios.** Write: `refinement: { states: [{ state }] }` on `PUT /teams/{id}`; absent or `null` = unchanged; `[]` = none chosen. Read (`GET …/settings`, PUT answer): each entry also carries `isMapped`. `GET /teams/{id}` gains `refinementConfigured`. `GET /teams/{id}/refinement` → `{ refinementConfigured, workItems: [{ referenceId, name, url, state, stateCategory: "ToDo"\|"Doing", workItemAge: number\|null }] }` | DESIGN names the DTOs and fields but not their JSON; these follow the existing camelCase DTO shape, string enums out, and DSN-5/DSN-9 wording |
| DST-2 | **A flagged (no longer mapped) state survives being sent back.** Only states *added* in a save must be To Do or Doing; a state already stored and now unmapped is kept and stays flagged when the autosaving form re-sends it | Reconciles DSN-4's "kept and flagged, never dropped" with "⊆ To Do ∪ Doing at save" under ADR-029 autosave, which re-sends every section: refusing would block every later edit to the Team |
| DST-3 | **Work Items in a flagged state are not listed** | The section's own flag promises "its Work Items cannot appear" (US-01 example 3) |
| DST-4 | **A Team with no refinement states answers its tab's read with `refinementConfigured: false` and no rows (200)**, not 404/409 | Mirrors the "stated, not an error" rule of the empty state; 404 stays reserved for "you may not read this Team" (non-disclosing, TeamRead) and "no such Team" |
| DST-5 | **An address naming the Refinement tab of a Team without refinement states lands on Forecasts** | Same rule the page already applies to the Features tab of a Team without Features (S1 pattern) |
| DST-6 | **`TeamRefinementConfigured` is reported by the Team page**, where the settings form is handed its save, when a save it accepted turns `refinementConfigured` false → true; never on later saves, never on a refused one. The scenarios stand in for the form and call that save | DVO-1 defines *when*; the page is the one place that sees both the save and the Team read that follows it. Consent is the reporter's, pinned once for every event by the existing reporter tests |
| DST-7 | **The terminology keys are not added to `TERMINOLOGY_KEYS` in DISTILL**; tests spell `"refinement"`/`"refinements"` | The existing guard `TerminologyContext.test.tsx` requires the seeder to carry every listed key — adding the key without the seeder row reds the suite. DELIVER adds key, seeder row and fallback together |
| DST-8 | **300-row guardrail split by layer**: the backend read alone answers in < 2 s (second, warm call, 300 rows in rank order); the frontend asserts the heading counts all 300 and the list starts at the top, not a DOM row count | Leaves DELIVER free to virtualise the list; a jsdom render time is not a budget anyone can hold |
| DST-9 | **E2E walking skeleton on demo scenario 12** (premium; it is the scenario that seeds Team Gravity, beside Team Zenith). Gravity configured (Backlog, Analysing, Next); **Zenith is the unconfigured demo Team** (US-01 example 2). First row asserted is **GR-051**, not GR-058 | Under DSN-6 the CSV rank is file order, and GR-051 is Gravity's first Backlog/Analysing/Next row; the "GR-058 first" in US-02 predates DSN-6. Tooltip asserted is the editor copy, because E2E runs without sign-in where everybody edits |
| DST-10 | **Copy pinned by the scenarios** (from DISCUSS where it gives words, else the plainest sentence): tooltips verbatim from DD-15; heading `"{n} {Work Items} in {Refinement}"` (singular for 1); empty state `"No {Work Items} in {Refinement} states right now"`; option labels `"Backlog (To Do)"`; Doing note `"already counts in {WIP} and {Cycle Time}"`; flag `"{state} is no longer mapped; its {Work Items} cannot appear"`; age `"4 days"`; row category `"To Do"` / `"Doing"`. Renamed-term assertions are case-insensitive mid-sentence | DELIVER may reword only by changing the scenario in the same commit |
| DST-11 | **No backend scaffolds**: the backend scenarios are black-box over HTTP/JSON, so nothing they reference is missing at compile time. Frontend scaffolds: `RefinementService` and `RefinementView` throw `Not yet implemented -- RED scaffold`; model types and `ITeamSettings.refinement` / `IApiServiceContext.refinementService` added. **No EF migration** (DELIVER, `CreateMigration`) | Precedent 22e43e1f9 (backend, no scaffolds) and c3820c0d3 (frontend scaffolds that throw) |
| DST-12 | **Tier B not declared** | Config-shaped settings + one read; two short journeys, no domain-rich generated input |
| DST-13 | **MQ-4 default followed** (To Do rows carry no age). Still open with the maintainer; reversing it changes one scenario | DESIGN: "none blocks DISTILL" |

## Mandate-12 (informational)

Step methods live in `*Specifications.cs` partial classes and the shared `RefinementAcceptanceTest` harness; every
scenario body is Given/When/Then calls only. Domain values are typed (`TeamUnderTest`, `TrackerWorkItem`,
`RefinementRowReading`, `RefinementStateReading`, `StateCategories`). **Step-reuse ratio 2.24×** (103 step calls /
46 distinct steps across the two backend scenario files) — the natural ceiling for a settings-plus-list feature.

## Completeness audit (Phase 2.5)

**11 / 15 → ACCEPTABLE_WITH_DOCUMENTED_GAPS.** Passing: C1a (no states, empty list, cleared), C1b (equal and
9-vs-10 ranks, 1 vs N heading, 300 rows), C2a (state documented in the slice docstrings: none → chosen → flagged →
cleared), C2b (illegal event per state: open-by-address unconfigured, newly chosen unmapped, read without a role),
C3 (0/1/many states and rows), C4a (save again flagged, re-save reports nothing, read twice identical), C5a
(admin / reader / RBAC off / no role / system admin), C5b (renamed term changes words only), C6a (Done, Icebox, unknown
Team), C6b (400 naming the state, 403, 404 ×2), C7c (settings write is concurrency-safe by the existing token, pinned by
`TeamConcurrencyTokenIntegrationTest`; nothing new to race). Gaps, all `AT_GAP_IN_DELIVERY_SCOPE`, none blocking:
C4b (clearing on a Team that never had states), C6c (closed error set), C7a (the tab's read failing — the page's
existing error snackbar is assumed), C7b (interruption mid-save — inherited autosave behaviour). Also not pinned
at acceptance level: the "survives a Team refresh" half of AC-1.2 — a refresh reads Work Items through the
connector and never writes Team settings, so DELIVER covers it with a unit test on the refresh path if it touches
the Team row. **0 SPECIFICATION_AMBIGUITY.**

## Upstream notes (for the maintainer, not blockers)

- US-02's example "GR-058 first" and "14 Work Items" are illustrative: on the demo data under DSN-6 Gravity lists
  **63** Work Items with **GR-051** first. If GR-058 first matters for screenshots, DELIVER reorders the demo CSV.
- MQ-4 (age on To Do rows) remains open; the default is what the scenarios pin.

## Handoff

DELIVER slice 01 enables `Slice01RefinementStatesTest`, the slice-01 cases of `TeamDetail.refinementTab.test.tsx`,
`ModifyTeamSettings.refinement.test.tsx` and the slice-01 cases of `TeamRefinementUsageEventsTests`; slice 02 the
rest, then the E2E skeleton (run live before un-fixme'ing it). One scenario at a time.

## Review follow-up

- **DST-14** The E2E skeleton runs on premium demo scenario 12 because it is the only scenario that seeds Team
  Gravity; nothing in E1 is licence-gated. DELIVER slice 01 adds a backend demo-data scenario so AC-2.6 is guarded
  before the E2E is un-fixme'd.
- **Maintainer to confirm** (product-facing defaults, not blockers): DST-9 (GR-051 first, 63 Work Items on demo
  Gravity), DST-10 (the copy pinned by the scenarios) and DST-13 (no age on To Do rows, MQ-4).


---

# Wave Decisions — DISTILL — E3 Sizing votes (Epic #5510), slices 10–17b

**Agent**: Quinn (`nw-acceptance-designer`) · **Date**: 2026-10-03 · **Mode**: autonomous subagent, maintainer AFK
**Scope**: E3 only — slices 10 (US-10, #6148), 11 (US-11, #6149), 12 (US-12, #6150), 13 (US-13, #6151), 14 (US-14,
#6152), 15 (US-15, #6153), 16 (US-16, #6154), 17a (US-17a, #6155), 17b (US-17b, #6156). E1 is delivered; E2, E4, E5
are not distilled here. The E1 block above is left as written.
**Predecessors**: DISCUSS (DD-1..DD-22), DESIGN (DSN-1..DSN-22, ADR-214..218), DEVOPS (DVO-1..DVO-8), DELIVER E1.
Full reasoning: `feature-delta.md` → `## Wave: DISTILL / … — E3 (#5510)`. RED evidence: `distill/red-classification.md`
→ "Epic #5510 (E3)".

## Phase 0

`[lang-mode] csharp+typescript` (unchanged). `[policy-mode] inherit` — `docs/architecture/atdd-infrastructure-policy.md`
read and applied; no port in scope is missing from it (driving: the real host over HTTP; driven internal: real EF on
SQLite / Postgres per CI leg; external: the licence and the instance clock faked, usage-data collector captured), so no
row was appended. `[port-mode] n/a` — as E1: the universe guard is asserted directly (row counts and readings before and
after, settings read-back, "nothing counted on any listed Work Item").

## Reconciliation gate (DISCUSS ↔ DESIGN ↔ DEVOPS, E3 scope)

**Reconciliation passed — 0 contradictions.** Every DISCUSS statement DESIGN or DEVOPS reads differently is a
documented correction (`design/upstream-changes.md`, `devops/upstream-changes.md`), applied as written:

| DISCUSS | Reading applied | Source |
|---|---|---|
| AC-10.1 "over the Team's metrics window" | over the Team's Throughput history window | DSN-10 |
| AC-11.2 / DD-10 "name per browser" | name plus a random per-browser voter key; "mine" and take-back follow the key | DSN-12 |
| AC-11.4 / AC-15.2 "TeamRead"; slice-15 brief "403 without Team read" | `TeamContribute` = the Team read predicate; non-readers get 404 | DSN-13, ADR-217 |
| AC-12.2 / DD-11 open question | open while the asker has no current vote | DSN-15 |
| AC-13.2 / DD-5 | votes alone decide until stages (03) and rules (08) exist | DSN-14 |
| AC-14.2 presenter exemption | presenter is E4; nothing here reveals a split | DSN-16 |
| US-17b command names | `lh refinement vote … --answer yes-but --comment …`, `lighthouse_team_refinement_vote` | DSN-21 |
| K4 two properties | one closed enum `sizingMoment`; `NoCadence` until slice 04 | DVO-2 |
| ADR-217 partition "by subject" | by presented handle, else address, plus the address ceiling | DVO-5 |

Settled maintainer calls re-checked against every scenario: votes are always open on every Work Item in refinement,
whatever its stage or position (slice 11, three cases); async is pull — nothing is pushed, readiness names what is
missing (slice 13); everything here is Community (no licence gate anywhere; named votes come only with sign-in, slice
15); minimum Yes ≥ 1 (slice 13 refusals); the SLE fallback is the 85th percentile of the default cycle time over the
Throughput window only, with no definition choice (slice 10); every user-facing word goes through Terminology (slice
10's renamed-terms cases); votes and readiness join the shared grid as columns, no age, no category, nothing pruned
by flag.

## Decisions taken autonomously (DST-15 onwards; E1 used DST-1..DST-14)

| # | Decision | Why this default |
|---|---|---|
| DST-15 | **Maintainer decision (2026-10-03), recorded here and in `feature-delta.md`: the slice-10 UI.** The tab shows the existing heading and exactly one more line, never more: the question followed by an info icon whose detail is a hover tooltip. SLE set (75% / 7 days): "Doable within 7 days?" ⓘ "SLE 75% of items in 7 days or less". No SLE, fallback 12 days: "Doable within 12 days?" ⓘ "No SLE set, based off 85% of historical cycle time". No SLE, nothing finished: "Doable within our SLE?" ⓘ "No SLE is set and no Work Items have finished yet". No visible hint line, **no settings link — AC-10.2 is dropped**. SLE / Cycle Time / Work Items / Refinement through Terminology. The server answers facts `{source: Sle\|CycleTimeFallback\|Unavailable, days?, probability?}`; the browser composes sentence and tooltip | Supersedes the US-10 elevator-pitch copy and AC-10.2. "items" in the SLE tooltip is the maintainer's word, kept verbatim |
| DST-16 | **Usage-data numbers**: `TeamSizingVoteCast = 13` (slice 11), `TeamSizingReadinessReached = 14` (slice 13). The highest member of `UsageDataEventName` today is `TeamRefinementConfigured = 12`, and `docs/settings/usagedata.md` lists nothing beyond it. New closed enum `UsageDataSizingMoment` starts with `NoCadence = 0`; `OnRefinementDay`, `OnOtherDay` (04) and `InLiveSession` (18) are appended by their slices. Wire part `sizingMoment` in, `sizing_moment` out. The scenarios pin the order the names were appended in, not the integers | DEVOPS's integers were provisional; these are the next free ones, nothing renumbered. E3's two slices land before E2's 05 and E4's 18, so they take 13 and 14 |
| DST-17 | **Wire shape pinned by the scenarios.** Tab read adds `yardstick {source, days, probability}` (fallback probability = 85), `voterIdentity: "Account"\|"SelfDeclared"`, `readyByVotesCount`; each row adds `voteCount`, `myVote`, `split {yes, yesBut, no}` (null when hidden), `readiness: "Ready"\|"MoreYesNeeded"\|"MoreVotersNeeded"\|"NeedsDiscussion"`, `missingVotes` (null when Ready or in discussion), `hasComments`, `hasOpenQuestion`. Log: `{hidden, voteCount?, entries: [{kind, answer, comment, voterName, channel, recordedAt, isMine}]}`. Vote body `{answer, channel, comment?, voterName?}` answered 200 with the row; comment body `{comment, channel, voterName?}`; `DELETE …/votes/mine` answered 200 with the row. Header `X-Lighthouse-Voter-Key`. Refusals are ProblemDetails with `code`: `voter-name-required` 400, `voter-key-required` 400, `work-item-not-in-refinement` 409, `vote-needs-a-person` 403; the other 400s (unknown answer or channel, a name over 100 characters, an empty or over-2,000-character comment) pin the status only. The route segment `{workItemId}` is the Work Item's reference, escaped | DESIGN names fields but not their JSON; these follow the E1 camelCase / string-enum shape and `ProblemDetails.Extensions["code"]` as the existing filters do. Rows carry no other id than the reference |
| DST-18 | **`voterIdentity` is a fact on the tab read** so the browser knows whether to ask for a name | DESIGN says "no name prompt when authentication is enabled" without saying where the browser learns it; the tab's own read is the one place every voter already asks |
| DST-19 | **`refinement.readiness` absent or null in a save leaves readiness unchanged**; a Team that never set it reads 3 Yes, 3 voters, no veto | DSN-3's null-means-unchanged rule, one level down — an older form must not reset it |
| DST-20 | **Half an SLE is no SLE**: probability without days, or days without probability, falls back | DSN-10 "when both SLE fields > 0" |
| DST-21 | **Frontend write port is its own `SizingLogService`** (`castVote`, `addComment`, `takeBackMyVote`, `getLog`, voter key passed explicitly), beside `RefinementService` (the tab's read, which sends the stored voter key header) | Mirrors the backend's read/write split (DSN-19) and leaves E1's `IRefinementService` test literals untouched |
| DST-22 | **Copy pinned, provisional until the maintainer has seen a sketch** (CLAUDE.md: sketch any UI first — DELIVER shows it before building): row buttons "Yes" / "Yes, but…" / "No" with `aria-pressed` on the voter's own; columns "Your vote" and "Votes" on the shared grid; "No votes" / "1 vote" / "3 votes"; name prompt dialog, textbox "Your name", "Vote" / "Cancel"; "Change your name" → "Save"; "Ready" / "2 more Yes needed" / "1 more voter needed" / "Needs discussion"; heading "3 Work Items in Refinement · 1 ready by votes" on its own single line; "Yes, but…" prompt textbox "Condition" (may stay empty); "Ask a question" → "Question" → "Send"; "Open question"; "Votes and comments" → a list, days as "Wed 7 Oct", "via the command line" / "via an assistant", "Jonas Weber took back their vote"; hidden log "Vote first to see the 3 votes and their comments"; split "3 Yes · 0 Yes, but… · 1 No"; "Take back my vote"; settings "Yes votes needed", "Voters needed", "Send to discussion", "Counting" ("No" / "No or Yes, but…"), errors "At least one Yes vote is needed", "Voters needed cannot be fewer than Yes votes needed" | DISCUSS gives words for a few of these only. The ready count lives in the heading so the maintainer's "heading plus one line" stays true. DELIVER may reword only by changing the scenario in the same commit |
| DST-23 | **An empty refinement asks no question** | Nothing to vote on |
| DST-24 | **SUPERSEDED by the maintainer 2026-10-03: Yes and No may also carry an optional, unprompted comment; still one click to vote.** **The UI prompts for a comment only on "Yes, but…"**; Yes and No stay one click. The server accepts a comment on every answer (AC-12.1), so clients and a later UI can add one | AC-11.5 (≤ 2 interactions per vote) and AC-12.1 pull against each other on Yes/No; the click budget wins in the UI. Flagged upstream |
| DST-25 | **17a/17b here are the Lighthouse half**: a client is a voter with its own key; `Cli` / `Assistant` are recorded and shown; a nameless client vote is refused; a personal API key votes as its owner; a key nobody owns is refused. The CLI and MCP scenarios (commands, `lh config voter set`, refusing before calling, the tool wording of DD-19 (d), mcp-http refusing auth-off votes, the voter store) belong in `lighthouse-clients` and are distilled there together with slice 09, whose read command 17a extends | One commit, one repository; 17a's client surface does not exist until E2's slice 09 |
| DST-26 | **A key nobody owns cannot comment or take back either** (403 `vote-needs-a-person`) | DSN-12 names votes; a comment or take-back with no voter has no author either |
| DST-27 | **Taking back a vote you do not have answers 200 with the unchanged row** and appends nothing | DSN-15 "idempotent no-op" |
| DST-28 | **Rate limit pinned through the shipped settings**: the 31st entry from one voter key within a minute → 429, and the voter's current vote is unchanged | One scenario covers both DEVOPS asks: the policy is configured in `appsettings.json` and it bites |
| DST-29 | **Test placement and harness**: same folder and split as E1 (`API/Integration/Refinement/`, `Slice1n…Scenarios.cs` + `…Specifications.cs`); new shared harness `SizingVotesAcceptanceTest : RefinementAcceptanceTest` with three instances — without sign-in (default), sign-in with roles, sign-in without roles with the product's own API-key handler. The E1 harness gained an overridable authentication hook, the SLE and Throughput window on seeded Teams and in the settings form (so an admin's save no longer zeroes the SLE), and protected form/put/read helpers; E1 scenarios behave as before | Precedent E1 (black-box over HTTP/JSON, no backend scaffolds) |
| DST-30 | **No backend scaffolds**: every backend scenario is black-box over HTTP/JSON. Frontend scaffolds: `SizingLogService` throws `Not yet implemented -- RED scaffold`; model types added as optional members; `IApiServiceContext.sizingLogService` (+ default and mock entries) | Precedent DST-11 |
| DST-31 | **Tier B not declared**: readiness, the open question and the hidden split are one pure resolution (ADR-218); DELIVER covers its combinations with unit property tests. Acceptance stays example-based through the real host | Mandate 9: real-host acceptance is layer 3 |
| DST-32 | **The yardstick captured on each vote (DSN-22) is not observable through any port in E3** — it is E5's. DELIVER 11 pins it below acceptance | No reader exists yet |
| DST-33 | **Second E2E walking skeleton** (DVO-7) on demo scenario 12, `fixme`: needs demo votes — GR-059 with Yes from Jonas Weber and Mo Okafor, so the E2E voter's Yes makes it Ready; plus DISCUSS's examples (GR-073 Ready by Jonas and Mo's Yes and Ana's "Yes, but…"; Ana's condition on GR-051; Ana's No on GR-054). DELIVER 11/13 seeds them | DISCUSS demo-data checklist; the E2E establishes nothing it does not say |
| DST-34 | **Not pinned at acceptance level, DELIVER covers below**: the fallback ignores a Team's named cycle-time definitions (D24); presenter reveal (E4) | Seeding named definitions needs state history the harness does not build; presenter is out of scope |
| DST-35 | **Readiness is admin-only through the same settings write E1 already guards** (`Only_a_Team_admin_can_change_the_refinement_states`); not repeated | One write, one guard |

## Delivery order for these scenarios

Per DD-22 the E3 slices interleave with E2: **10 → 11 → 13** first (then E2's 03–06), **12** (then 07), **15 → 14**
(then 08), **16** (then 09), **17a → 17b**. Each slice un-skips only its own cases; a later slice's case may need an
earlier slice's behaviour (comments arrive in 12, so 14's "asking a question does not reveal the split" waits for 12,
which is delivered first), never a later one.

## Mandate-12 (informational)

Step methods live in `*Specifications.cs` partial classes and the shared `SizingVotesAcceptanceTest` harness; every
scenario body is Given/When/Then calls. Domain values are typed (`Voter`, `Answer`, `Channel`, `VetoCounts`,
`FinishedWorkItem`, `VotedRowReading`, `LogEntryReading`, `ReadinessReading`, `YardstickReading`, `SplitReading`).
**Step-reuse ratio 2.69×** (336 step calls / 125 distinct steps across the eight E3 scenario files) — the natural
ceiling for a vote log with three identity modes.

## Completeness audit (Phase 2.5)

**13 / 15 → COMPLETE.** Passing: C1a (no votes, no SLE, nothing finished, empty refinement), C1b (100/101-character
name, 2,000/2,001-character comment, 31-character key, veto 1/2, 0/-1 Yes, voters = Yes), C2a (a vote's life: none →
cast → changed → taken back → cast again; a question: open → closed by the asker), C2b (illegal per state: vote off
refinement, take back with no vote, question without a vote does not unlock), C3 (0/1/many votes, voters, Work Items),
C4a (taking back twice, re-voting, two sessions of one account), C4b (take back with nothing to take back), C5a
(without sign-in, sign-in with roles, sign-in without roles, Team admin, Viewer, no role, unowned and personal API
keys), C5b (renamed terms), C6a (unknown Team, unknown Work Item, Work Item outside refinement), C6b (each refusal
code with its status), C6c (closed refusal set: four codes, the rest status-only), C7a (a refused vote, a failed take
back and an unreadable log each say why in the UI). Gaps, both `AT_GAP_IN_DELIVERY_SCOPE`, neither blocking: C7b
(a vote interrupted mid-flight — the append-only log makes a retry a second entry, and the latest counts, so nothing is
lost; not scenario-pinned), C7c (two voters at once — no read-modify-write exists, DSN-11; not scenario-pinned). **0
SPECIFICATION_AMBIGUITY.**

## Upstream notes (for the maintainer, not blockers)

- DST-15 drops AC-10.2 (the settings link) and the US-10 pitch copy — recorded as the maintainer's call.
- DST-22's copy is provisional; DELIVER's sketch-first step settles it.
- DST-24: AC-11.5 and AC-12.1 conflict for Yes/No in the UI; the scenarios keep Yes/No at one click.
- DST-25: the `lighthouse-clients` half of 17a/17b is owed with slice 09.
- The SLE tooltip says "items" (maintainer's copy); every other sentence uses the Work Item term.

## Handoff

DELIVER slice 10 enables `Slice10SleYardstickTest` and `RefinementView.yardstick.test.tsx`; 11 `Slice11CastAVoteTest`,
the slice-11 cases of `TeamSizingUsageEventsTests`, `RefinementView.votes.test.tsx` (first block),
`SizingLogService.test.ts` (slice-11 cases) and `RefinementService.voterKey.test.ts`; 13 `Slice13ReadinessTest`, the
slice-13 usage cases, `RefinementView.readiness.test.tsx`, `ModifyTeamSettings.readiness.test.tsx` and then the E2E
skeleton (run live first); then 12, 15, 14, 16, 17a, 17b in that order. One scenario at a time.

## Amendment 2026-10-04 (maintainer)

Two maintainer decisions, taken from the voting-UI sketches before slices 11–16 were built (recorded in full in
`feature-delta.md`, "Maintainer decision — the voting UI, and slice 14 dropped"). The scenarios were changed to match;
nothing else about this wave moved.

- **Slice 14 is dropped (US-14, #6152 Removed).** Every vote split and every comment is visible to everybody, voted
  or not: a Product Owner who never votes still reads how the Team voted. `Slice14HiddenSplitScenarios.cs` and
  `Slice14HiddenSplitSpecifications.cs` are deleted (10 backend cases), with the frontend's three slice-14 cases.
- **No hiding on the wire.** A row's `split {yes, yesBut, no}` is always sent; the log answers `{entries: [...]}`
  only — `hidden` and the hidden log's `voteCount` are gone from the scenarios, the harness (`LogIsHidden`,
  `VoteCountOfAHiddenLog`, `PendingSlice14`) and the frontend `ISizingLog` type. `ISizingSplit` on a row is no
  longer nullable.
- **Rewritten instead of deleted, one per stack where it still checks something:** backend slice 11 *Every reader
  sees how the votes split* (two voters and somebody who has not voted read the same split) and slice 12 *Somebody
  who never voted reads how the votes split and every comment*; frontend slice 12 *lets a reader who never voted
  read how the votes split and every comment*. Slice 16 *Taking back hides the split again* became *Taking back takes
  the vote out of the split*. Slice 17a's two split cases became one, *A client is told how the votes split*. The
  readiness case "shown to a reader who has not voted, without the split" became *gives readiness its own column,
  right after the votes*.
- **"Yes, if…" is the label of the conditional answer** wherever a user reads it: the button, the split
  ("3 Yes · 0 Yes, if… · 1 No"), the condition dialog title, the log, and Counting's "No or Yes, if…". The stored
  and wire value stays `YesBut`; no type, member or identifier is renamed.
- **The approved copy and placement replace the provisional copy of DST-22 where they differ**: the name dialog is
  "Who is voting?" with "Your name", "Kept in this browser only." and Cancel / Vote; the Votes cell opens
  **Votes and comments** (split, log, "Ask a question", "Voting as <name>" with "Change your name" when sign-in is
  off, "Take back my vote", Close) — so changing the name, asking a question and taking back a vote are now reached
  from that dialog, not the row; "Readiness" is its own column right after "Votes"; "Yes, if…" opens a dialog with
  an optional "Condition" and Cancel / Vote. The E2E page object finds the name dialog by its title.
- **No usage-data event carries personal data.** `TeamSizingUsageEventsTests` gains *The event from a browser that
  just voted carries nothing about the voter* (one case per event, pending with slices 11 and 13): the browser votes
  under a distinctive name, key, comment and address, then hands the event in carrying the same key and address; the
  collector receives only the event name, its moment and the instance facts every event carries, `$ip` is empty, and
  none of the name, key, comment, address or Work Item reaches it. Un-skipped once, it fails for the right reason
  (the vote route answers 404).

Case counts after the amendment: backend 125 runnable (slice 11 27, 12 20, 13 22, 15 9, 16 11, 17a/17b 12, usage
data 15, slice 10 9), frontend 63 (votes 17, comments 11, readiness 8, the rest unchanged).

**Delivery order from here: 11 → 13 → 12 → 15 → 16.** 17a and 17b are out of this run. Each slice still un-skips
only its own cases, with the same `[Ignore(PendingSlice1n)]` / `IgnoreReason` / `it.skip` markers as before.


---

# Wave Decisions — DISTILL — E2 Refinement need (Epic #5881), slices 03–09

**Date**: 2026-10-04 · **Agent**: Quinn (`nw-acceptance-designer`) · **Scope**: slices 03 (US-03, #6141), 04 (US-04,
#6142), 05 (US-05, #6143), 06 (US-06, #6144), 07 (US-07, #6145), 09 (US-09, #6147, Lighthouse half). Slice 08 (US-08,
#6146) is folded into 03 by the maintainer. E1 is delivered; E3 is distilled and in DELIVER; E4 and E5 are not
distilled.

## Phase 0

`[lang-mode] csharp+typescript` (backend NUnit, frontend Vitest + RTL, E2E Playwright) · `[policy-mode] inherit`
(`docs/architecture/atdd-infrastructure-policy.md`; one more stand-in, the scripted forecast, recorded as DST-45) ·
`[port-mode] inherit` (assertions read the HTTP/JSON answers, as E1 and E3).

## Reconciliation gate (DISCUSS ↔ DESIGN ↔ DEVOPS, E2 scope)

**Reconciliation passed — 0 contradictions.** DESIGN's corrections (DSN-1..22) and DEVOPS's usage-data and environment
decisions agree with DISCUSS for E2 once `upstream-changes.md` is applied. What changes here is the maintainer's own
2026-10-04 sketch decisions (stages from rules only, two signals, the line following the displayed order); those
replace DISCUSS/DESIGN wording rather than resolve a disagreement between them, and are back-propagated in
`distill/upstream-issues.md`.

## Decisions taken in this wave (DST-36 onwards; E1 used DST-1..14, E3 DST-15..35)

| # | Decision | Why |
|---|---|---|
| DST-36 | **Stage and votes are two signals.** Stage comes only from the optional "Ready when" / "Being refined when" rules; unmatched is Waiting. Without rules the ready count is the votes' (`readySource: Votes`); with rules it is the stages' (`readySource: Stages`), and votes neither make nor block Ready | Maintainer, 2026-10-04; supersedes DD-5, DSN-14's readiness half, AC-3.1–3.2 |
| DST-37 | **Slice 08 folded into 03.** US-08's Pulsar case is a slice-03 scenario; there is no `PendingSlice08`. The orchestrator marks #6146 Removed after the maintainer confirms | Maintainer |
| DST-38 | **Ready wins** when both rules match a Work Item | DSN-14's precedence, kept for the rules |
| DST-39 | **`signalsDisagree`** is true only when votes have been cast and they disagree with the stage: the stage is Ready and the votes cast fall short of Yes or need discussion, or the votes say Ready and the stage is Waiting or Being refined. **A row nobody has voted on is never flagged** — no votes is no opinion. A Waiting row the votes do not call Ready agrees | Maintainer, 2026-10-04 (answered DISTILL's question: no votes, no ⚠) |
| DST-40 | **No "no state/rule is marked Ready" hint**; a Team with rules that match nothing Ready reads "· 0 ready" | Maintainer |
| DST-41 | **Vote facts unchanged**: `readyByVotesCount` and `madeReady` keep their votes meaning on every Team; the heading reads "· R ready by votes" without rules (slice 13 unchanged) and "· R ready" with rules. The E2E heading regex stays valid for demo Gravity, which has no rules | Older clients and the E3 browser code read them |
| DST-42 | **Wire shape.** Settings `refinement.stageRules {ready, beingRefined}` (each a `WorkItemRuleSet` or null), `.cadence {weekdays: ["Thursday"], intervalWeeks, anchorWeek}`, `.band {lowPercentile, highPercentile}`; an absent member leaves it unchanged (ADR-214). Tab: `stagesConfigured`, `readyCount`, `readySource`, `nextRefinementDate` (ISO day or explicit null), `isRefinementDay`, row `stage` (`Waiting`/`BeingRefined`/`Ready` or null) and `signalsDisagree` (rule: DST-39; false on a row without votes), `need {verdict: Below/In/Above/null, unavailableReason: NoCadence/InsufficientData/NoRefinementStates/null, low, high, lowPercentile, highPercentile, horizonWorkingDays}`. Facts only, never a sentence | AC-5.5; DESIGN's facts-not-copy rule |
| DST-43 | **Cadence normalisation**: the starting week is stored as its Monday whatever day names it; a weekday named twice is one; `weekdays: []` means no cadence and reads back as null; a starting week is required only when `intervalWeeks > 1`; weekdays are English weekday names, anything else refused | AC-4.1; one cadence has one stored form |
| DST-44 | **Dates.** Next Refinement = first cadence day strictly after today in the instance time zone; a blackout day does not move it. The need's horizon is the forecasts' own count of days from today to that date, blackout days left out; on a Refinement day it is the following Refinement | DD-6, AC-4.2, US-04 tech note |
| DST-45 | **Scripted forecast stand-in**: `ForecastWithScriptedHorizons` decorates the shipped `ForecastService`; a scenario scripts How Many per horizon, and only the exact horizon the tab asks for is answered from the script, so a scripted range also proves the horizon. Unscripted horizons run the shipped engine — the parity case (against the manual forecast) and the 300-row guardrail use it over constant Throughput | A verdict scenario must choose its forecast or it asserts sampling noise; policy: non-deterministic port |
| DST-46 | **The line follows the displayed order** (maintainer): the API returns the high end (never cut to the listed count), the date and the percentile; the browser numbers the first N rows as shown and places the line. No `lineAfterPosition` on the wire. The sort is not kept between visits | Maintainer; supersedes DD-4's backlog-order counting and AC-6.1 |
| DST-47 | **Usage data**: `TeamRefinementDayVerdictShown = 15` with closed enum `refinementVerdict` (`UsageDataRefinementVerdict {Below = 0, In = 1, Above = 2, None = 3}`, wire `refinement_verdict`), reported once per tab mount on a Refinement day only; `UsageDataSizingMoment` appends `OnRefinementDay = 1`, `OnOtherDay = 2`. Tests pin the names and their order, not the integers | DEVOPS usage-data rule; never personal data |
| DST-48 | **Copy.** Pinned (maintainer): "Stages (optional)", "Ready when", "Being refined when", "Stage", "Votes say", the breakdown, "Refinement cadence", "Repeat every (weeks)", "Starting week", "Next Refinement: Thu 8 Oct · in 4 days" / "tomorrow", both cadence hints, the three verdict sentences, "enough for Thu 8 Oct (85%) · not needed before then", "All 6 Work Items in Refinement are needed before Thu 8 Oct.". **Still provisional**: the ⚠ tooltip words (tests match `/stage and (the )?votes disagree/i` on the accessible label), the band's labels ("Low end likelihood…", "High end likelihood…") and its refusal wording (tests require both values named), the cadence validation wording (tests use `aria-invalid`) | Maintainer's sketches covered 03–06 |
| DST-49 | **One E2 walking skeleton** in E2E (cadence → next Refinement → verdict → line), `fixme`, demo scenario 12. DVO-7 named E3's; E2 gets its own because each Epic ships alone | CLAUDE.md "each Epic ships on its own" |
| DST-50 | **Rule editor fields** come from the Team's existing work-item rule schema; the frontend tests serve it through `getForecastFilterSchema`. DELIVER may use another source of the same `IWorkItemRuleSchema` shape and adjust the mock | Reuse the rule editor and schema the Team's other rules use |
| DST-51 | **Admin-only** stage rules, cadence and band ride the settings write E1 already guards; the guard is not repeated | Precedent DST-35 |
| DST-52 | **Not pinned at acceptance**: whether the need honours the Team's forecast filter (the parity case compares with the manual forecast, which applies whatever forecasts apply); the "muted" look of rows below the line (styling) | No observable port fact distinguishes them reliably |
| DST-53 | **Tier B not declared**: next-date and verdict are pure functions DELIVER covers with unit property tests (week modulo, inclusive ends); acceptance stays example-based through the real host | Mandate 9: real-host acceptance is layer 3 |
| DST-54 | **Slice 06 has no backend scenarios**: its facts (the unclamped high end, the percentile, the date) are pinned in slice 05; slice 06 is frontend-only | DST-46 |
| DST-55 | **Minimum data** is the forecasts' own guard (`ForecastDataSufficiencyPolicy`, at least five days with finished Work Items); the browser reuses `INSUFFICIENT_FORECAST_DATA_MESSAGE` | AC-5.6 |
| DST-56 | **Slice 09 is the Lighthouse half**: a client reads the need with its own key, a personal API key reads the same on a sign-in-without-roles instance, and older vote facts stay. The CLI/MCP half is owed in `lighthouse-clients` | Precedent DST-25 |

## Mandate-12 (informational)

Domain constants and readers live in the harness (`RefinementNeedAcceptanceTest`: stages, verdicts, reasons, weekdays,
`SaveShape`, typed readings `StageRowReading`, `CadenceReading`, `CadenceFactsReading`, `BandReading`, `NeedReading`).
Step methods delegate to the HTTP driving ports; each Then reads one typed reading. Step reuse: 74 slice step methods
invoked 204 times from the scenarios, plus shared harness steps (`TheAdminHasSet…`, `TodayIs`, `TheTeamIsLikelyToPull`)
— about 2.8×, the natural ceiling for readable scenarios.

## Completeness audit (Phase 2.5)

13/15 → **COMPLETE**. C1a/C1b happy paths per story ✓; C2a/C2b stage and cadence states (no rules / rules, no cadence /
cadence / Refinement day) ✓; C3 boundaries (inclusive ends, interval 0/−1, 1/99, 20/21 conditions, time zone, blackout)
✓; C4a/C4b refusals leave state unchanged and name what is wrong ✓; C5a/C5b mode flags (`readySource`, silent saves)
✓; C6a/C6b/C6c error contracts (400 on bad settings, `unavailableReason`, usage refusals) ✓; C7a auth modes (off,
sign-in without roles) ✓; **C7b** (renamed Terminology) frontend only — partial; **C7c** (two admins saving at once)
not pinned — one JSON value, last write wins as for every Team setting. **0 SPECIFICATION_AMBIGUITY.**

## Handoff

DELIVER runs 03 → 04 → 05 → 06 → 07 → 09 (each Epic ships alone), one scenario at a time, un-skipping per slice as
listed in `feature-delta.md` → "Delivery order — E2". Before any UI step, DELIVER sketches what DST-48 leaves
provisional.
