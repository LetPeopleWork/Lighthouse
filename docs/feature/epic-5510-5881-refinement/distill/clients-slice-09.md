# DISTILL — slice 09, Lighthouse-Clients half (Story #6147 "CLI and MCP: Refinement need and list")

Scope: the clients answer "how much should we refine?" from the same read as the Refinement tab
(`GET /api/v1/teams/{id}/refinement`), put the facts into the web's words using the instance's
Terminology, and hand the raw facts over unchanged on request. The Lighthouse half (the facts on the wire)
is `Lighthouse.Backend.Tests/API/Integration/Refinement/Slice09ClientNeedScenarios.cs`.

Repository: `/storage/repos/lighthouse-clients`. Test style: Vitest, driving ports only — the client
through `createLighthouseClient` with a fetch fixture, the CLI through `runCliCommand` with an in-memory
client, the MCP tool through `createMcpCoreRuntime(...).callTool`. Every scenario is `it.skip` /
`it.skip.each` (never `describe.skip`); fixtures are plain data and local functions, so nothing
unimplemented runs at collection time. Each scenario was un-skipped once and confirmed red for the missing
behaviour (client: no `getTeamRefinement` / `getTerminology`; CLI: `refinement` is an unknown group; MCP:
unknown tool), then re-skipped.

Example data mirrors the server half: Team Gravity (id 3), Tue 6 Oct, next Refinement Thu 8 Oct in 2 days,
3 ready, `Below`, range 5–8 at 50/85 %, nine Work Items GR-051 … GR-080 (GR-080 has no parent).

## Maintainer decisions (2026-10-06, binding)

> 1. `lh refinement get --team-id <id>` default (`--pretty`) output is the web's words, not a facts dump:
>    heading `Team Gravity · Next Refinement: Thu 8 Oct · in 2 days`, the need sentence
>    (`describeNeed`), then a table `# · Work Item · Parent · State` with rows numbered up to
>    min(high, listed) and the line placed and worded by the web's rule (`enoughForPlacement`).
>    No cadence: `Team Gravity · No Refinement cadence` plus a hint line. `--json` / `--toon` return the
>    raw facts unchanged.
> 2. Words come from the instance's Terminology: the client fetches `GET /terminology/all` (new client
>    method) and falls back to the seeded defaults if that call fails.
> 3. MCP `lighthouse_team_refinement_get` returns the raw facts plus a `summary` field holding the same
>    verdict sentence (heading + need sentence). The tool description explains verdict / low / high / cycle.
> 4. Names per DESIGN: client `getTeamRefinement(teamId)`, CLI `lh refinement get --team-id <id>`,
>    MCP `lighthouse_team_refinement_get`.
> 5. This Story introduces a per-command pretty-renderer seam in the CLI; the generic formatter stays the
>    fallback for every other command. Converting the other commands is a later Story, not this one.
> 6. Vote facts (voteCount, split, readiness) are 17a's. The 09 list shows #, Work Item (id + name),
>    Parent, State only. Facts on the wire are never re-derived (no band maths in the client).

### Decided in DISTILL (copy the maintainer has not seen — confirm at DELIVER 09c-02)

- **Line text** is the web's full sentence, `enough for the next Refinement (85%) · not needed before then`
  (the sketch abbreviated it); decorations around it (`── … ──`) are the renderer's and not pinned.
- **No-cadence hint** uses the web's *reader* form, because the CLI cannot know whether the caller may change
  the Team's settings: `A Team admin can set a Refinement cadence to see how many Work Items are needed`.
- **InsufficientData**: heading keeps the next Refinement; the line under it is the forecasts' own
  `Not enough data yet — need at least 5 days with completed items to forecast.`
- **NoRefinementStates / `refinementConfigured=false`** (the web never shows the tab): heading
  `Team Gravity · No Refinement states`, hint `A Team admin needs to choose refinement states first`
  (the web's disabled-tab reader tooltip).
- Without a verdict the list is still printed, unnumbered and without a line (the web marks rows only while a
  verdict is shown).
- A blank Terminology value falls back to the seeded word (the web's `value || defaultValue`).
- **Server-version gate**: `getTeamRefinement` refuses, without calling the endpoint, on a Lighthouse not
  newer than `v26.10.3.6` (the last release without the refinement read), the house pattern for new
  endpoints (`FEATURE_REQUIRES_SERVER_NEWER_THAN`).
- MCP input is `{ id }` like every other per-Team tool; an invalid id is refused with `invalid id`.
- The Team's name comes from the existing `getTeam(id)`; the refinement read does not carry it.

## Scenarios

| # | Scenario | File :: test | Tags |
|---|---|---|---|
| C1 | The client hands over the refinement facts exactly as the server sent them | `packages/client/src/refinement.test.ts` :: hands over the refinement facts for a Team exactly as the server sent them | @driving_port @contract-shape:pure-function |
| C2 | An older Lighthouse is refused with an upgrade message, endpoint never called | same :: tells the caller to upgrade a Lighthouse that has no refinement yet, without asking it | @error @contract-shape:unbounded-preservation |
| C3 | The client hands over the instance's Terminology | same :: hands over the words the instance uses for its terms | @driving_port @contract-shape:pure-function |
| C4 | A failed Terminology read is a failure result the caller can fall back from | same :: reports a failed terminology read as a failure the caller can fall back from | @error @infrastructure-failure |
| L1 | Walking skeleton: Below 5–8, 3 ready — heading, sentence, numbered rows 1–8, line, GR-080 unnumbered, no vote facts | `packages/cli/src/refinement.test.ts` :: tells Priya in the web's words that Team Gravity is below its range, and marks the Work Items needed | @walking_skeleton @driving_port @contract-shape:pure-function |
| L2 | Verdict table ×5: Below (range), In, Above, one number (1, singular Work Item), Refinement day ("until the next Refinement") | same :: says '$says' for $readyCount ready against $low–$high (…) | @driving_port @boundary |
| L3 | Line placement ×4: 9 listed/8 needed; 3/8 ("All 3 … are needed"); 1/8 ("The only … is needed"); 2/0 (line before the first row) | same :: with $listed listed and $high needed, numbers … and draws the line after … | @boundary |
| L4 | No number ×3: NoCadence, InsufficientData, NoRefinementStates with `refinementConfigured=false` — heading + hint, list unnumbered, no line | same :: says why there is no number when the reason is $unavailableReason, … | @error |
| L5 | Renamed Terminology (Story/Stories, Grooming; blank `refinements` keeps the seeded word) | same :: says it in the words the instance has renamed its terms to | @driving_port |
| L6 | Terminology call fails → seeded defaults, exit 0, nothing on stderr | same :: falls back to the seeded words when the instance's terms cannot be read | @error @infrastructure-failure |
| L7 | `--json` / `--toon` hand over the facts unchanged ×2 | same :: hands over the facts unchanged with $flag | @driving_port @contract-shape:unbounded-preservation |
| L8 | Missing / invalid `--team-id` ×2, Lighthouse never asked | same :: refuses $args without asking Lighthouse | @error |
| L9 | A Lighthouse refusal (e.g. older server) goes straight to stderr, exit 1 | same :: passes a Lighthouse refusal straight through | @error |
| L10 | `lh refinement` help lists `lh refinement get --team-id <id>`; `lh help` lists the group | same :: lists the command in the refinement group help and the group in the overview | @driving_port |
| M1 | The tool is listed; its description explains verdict, low, high, cycle and summary; `id` required | `packages/mcp-core/src/refinement.test.ts` :: is offered with a description that explains the verdict, the range and the cycle | @driving_port |
| M2 | Facts plus `summary` (heading + need sentence) | same :: hands an assistant the facts together with the sentence the web page states | @driving_port @contract-shape:pure-function |
| M3 | No cadence: reason in the facts, summary says why | same :: tells an assistant why there is no number for a Team without a cadence | @error |
| M4 | Missing / non-numeric `id` ×2 refused with `invalid id`, Lighthouse never asked | same :: refuses $argumentsPayload without asking Lighthouse | @error |
| M5 | A Lighthouse refusal is an MCP error carrying the reason | same :: passes a Lighthouse refusal straight through | @error |

Counts: 19 scenarios, 31 test cases once the tables expand (client 4, CLI 21, MCP 6); 22 of the 31 are
error / boundary / infrastructure-failure cases. Suite before: 309 passed. After: 309 passed, 31 skipped.

Tier B (state-machine PBT): not declared — a read-and-render with no chained journey. PBT not used: the
verdict and line tables are finite and named from the web's own cases; the facts are never re-derived, so
there is no client-side invariant over an unbounded input to generate against.

## DELIVER steps (clients half), in order

1. **09c-01 — client: refinement read + Terminology.** `getTeamRefinement(teamId)` (wire types as string
   unions, gated on `teamRefinement: "v26.10.3.6"`), `getTerminology()`. Un-skips C1–C4.
2. **09c-02 — CLI: per-command pretty-renderer seam + `lh refinement get`.** Group, help, `--team-id`
   parsing, `getTeam` + `getTeamRefinement` + `getTerminology` (fallback to seeded words), the wording
   (heading, need sentence, no-number lines, line text and placement) written once in a place the MCP tool
   can reuse (the client package is the natural home since both depend on it), the renderer registered for
   this command only; `--json` / `--toon` keep the generic path. Re-sketch the decided-in-DISTILL copy above
   with the maintainer before writing it. Un-skips L1 first (walking skeleton), then L2–L10.
3. **09c-03 — MCP: `lighthouse_team_refinement_get`.** Tool definition + description, facts plus `summary`
   from the shared wording; extend the full tool-list assertion in `runtime.test.ts`. Un-skips M1–M5.
4. **09c-04 — release surface.** Replace the empty changeset with a **minor** changeset for client, cli and
   mcp-core; README / skill docs mention the command and the tool; `pnpm release:version` before the release
   run. No scenarios to un-skip.

## Found inconsistent between the design and the code

- **DSN-9 says the wire carries `lineAfterPosition` and `fewerListedThanHigh`; `RefinementViewDto` has
  neither.** The web places the line itself from `need.high` and the rows shown (`enoughForPlacement.ts`).
  The clients follow the web; with no such facts on the wire, "never re-derive" means "apply the web's
  placement rule to `high` and the listed count", which is placement, not band maths.
- **US-09's elevator pitch (`lighthouse teams refinement 3`, MCP `get_team_refinement`, "next Refinement Thu 8
  Oct · 3 ready · range 5–8 · below") is superseded** by DSN-21 names and decision 1's web wording; the
  scenarios follow the decisions.
- **The refinement read carries no Team name**, but every heading starts with it — the clients need a second
  call (`getTeam`). Cheap, but a field on the read would save a round trip for both clients.
- **The web's no-cadence hint and no-states hint depend on whether the viewer may change settings**; the
  clients cannot know, so they always use the reader form.
- **DESIGN names no server-version gate** for the new read; the house pattern requires one, pinned here at
  `v26.10.3.6`. Move the baseline if a release ships before 09c-01 lands without the read.
