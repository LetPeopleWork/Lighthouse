# Slice 03 — Clients and the Daily Flow Review read Lighthouse's staleness (US-03)

Executable scenarios for the `lighthouse-clients` repository. DISTILL could not write them there (this
session does not touch that repository), so they are not yet RED-classified the way slices 01 and 02 are.
The wording of user-facing copy is pinned here as literal text; tests compare against the literal, never
against the constant that produces it.

## DELIVER steps for this slice

1. **Write every scenario below as `it.skip` / `it.skip.each` in the file named, copy both fixtures, run the
   suite once with the skips removed to confirm each scenario is RED for the missing behaviour (not for a
   setup or import error), put the skips back, and record the classification beside
   `distill/red-classification.md`.** Only then implement.
2. Un-skip one scenario at a time, starting with the walking skeleton (the MCP tool scenario), and make it
   pass.
3. Bump the versions and set the gate (see Versioning) once slice 01's release is known.

Test mechanisms follow the project's ATDD infrastructure policy rows for the clients: `lh` through
`runCliSession(args, io, { env, terminal, now })`, the local MCP server through
`createLocalLighthouseMcpServer(env)` over `InMemoryTransport`, the server faked by `aFakeLighthouse(...)`
(`test-support/fakeLighthouse.ts`), and the skill held by `skills/skills.drift.test.ts`,
`skills/skills.evals.test.ts` and its `evals/cases.json`. Pending marker: `it.skip` / `it.skip.each`.

## The wire-shape contract fixture

`wip-read-with-staleness.json` (beside this file) is the Team WIP read as Lighthouse answers it once
slice 01 ships: one Work Item stale for its time in Review, one Blocked and stale with the context reason
beside it, one not stale. DELIVER copies it byte for byte to
`lighthouse-clients/test-support/stalenessContract/wip-read-with-staleness.json`, the way
`test-support/usageDataContract/` holds the usage data contract, and every scenario below that needs a WIP
answer serves those bytes from `aFakeLighthouse`. The second fixture, `wip-read-without-staleness.json`
(also beside this file, copied the same way), is the same three Work Items with `isStale` and `staleReasons`
removed: what an older Lighthouse answers.

Field names and values the clients may rely on (backend acceptance tests in
`Lighthouse.Backend.Tests/API/Integration/Staleness/` hold the server to the same names):

| Field | Wire value | Absent means |
|---|---|---|
| `isStale` | `true` / `false` | Lighthouse does not say (older server) |
| `staleReasons[].kind` | `"TimeInState"`, `"BlockedDuration"`, `"TimeInStateWhileBlocked"` | — |
| `staleReasons[].days` | whole days on the instance calendar, at least 1 | — |
| `staleReasons[].stateName` | the state's name, on the two time-in-state kinds only | Blocked time belongs to no state |

## `packages/client/src/metricsWording.ts` — reading the fields

```gherkin
@us-03 @AC-1 @contract-shape:pure-function
Scenario: A Work Item Lighthouse marks stale keeps its reasons on the way through
  Given the WIP read in the contract fixture
  When the client reads the Work Items in progress
  Then GR-058 is stale because it has been 12 days in Review
  And GR-061 is stale because it has been Blocked 6 days, with 12 days in In Progress beside it
  And GR-063 is not stale and carries no reason

@us-03 @AC-1 @error @contract-shape:pure-function
Scenario: A reason of a kind this client does not know is dropped and the Work Item is kept
  Given a WIP read whose only stale Work Item carries a reason of kind "SomethingNewer" and one of kind "TimeInState"
  When the client reads the Work Items in progress
  Then the Work Item is still read, still stale, and carries only the time-in-state reason

@us-03 @AC-3 @error @contract-shape:pure-function
Scenario: An older Lighthouse that says nothing about staleness is read as saying nothing
  Given the WIP read without staleness
  When the client reads the Work Items in progress
  Then no Work Item is marked stale and none is marked not stale
```

`InProgressItem` gains `isStale: boolean | undefined` and `staleReasons: readonly StaleReason[] | undefined`
(the same "undefined = Lighthouse does not say" model as `isBlocked`). An `isStale` that is not a boolean
reads as `undefined`; it never fails the Work Item.

## `describeStaleNow` beside `describeBlockedNow`

```gherkin
@us-03 @AC-1 @contract-shape:pure-function
Scenario: The WIP headline counts the stale Work Items
  Given the WIP read in the contract fixture
  When the WIP headline is described for a Team
  Then it carries the line "Stale Work Items  2" after the Blocked line

@us-03 @AC-3 @error @contract-shape:pure-function
Scenario: Against an older Lighthouse the headline carries no Stale line at all
  Given the WIP read without staleness
  When the WIP headline is described for a Team
  Then it carries no Stale line
```

"Stale" is the literal word the dashboard shows (`BaseMetricsView.tsx`, `Stale ${terms.workItems}`):
there is no Terminology key for it. "Work Items" is the instance's term.

## `describeWhatWipLeavesUnsaid`

```gherkin
@us-03 @AC-3 @error @contract-shape:pure-function
Scenario: An older Lighthouse is said not to say which Work Items are stale
  Given the WIP read without staleness
  When what the WIP answer leaves unsaid is described for a Team
  Then one of the sentences is "Lighthouse does not say which Work Items are Stale."

@us-03 @AC-3 @contract-shape:pure-function
Scenario: A Lighthouse that says is not described as silent
  Given the WIP read in the contract fixture
  When what the WIP answer leaves unsaid is described for a Team
  Then no sentence mentions which Work Items are Stale
```

## `packages/mcp-core/src/index.ts` — `lighthouse_team_metrics_wip` (and the Portfolio tool)

```gherkin
@walking_skeleton @driving_port @us-03 @AC-1 @contract-shape:bounded-change
Scenario: An assistant reading the WIP tool sees which Work Items are stale and why
  Given a Lighthouse that answers the Team WIP read with the contract fixture
  When the assistant calls lighthouse_team_metrics_wip for the Team over the local MCP server
  Then the tool's Work Items carry isStale and staleReasons exactly as Lighthouse sent them
  And its summary says "Stale Work Items: 2"

@driving_port @us-03 @AC-3 @error @contract-shape:bounded-change
Scenario: Against an older Lighthouse the WIP tool's summary says Lighthouse does not say
  Given a Lighthouse that answers the Team WIP read without staleness
  When the assistant calls lighthouse_team_metrics_wip for the Team
  Then the summary says "Lighthouse does not say which Work Items are Stale."
  And no Work Item carries isStale

@us-03 @AC-1 @contract-shape:pure-function
Scenario: Both WIP tool descriptions name the staleness fields
  Given the tools the local MCP server lists
  When the descriptions of lighthouse_team_metrics_wip and lighthouse_portfolio_metrics_wip are read
  Then each names isStale and staleReasons and says they are absent on an older Lighthouse
```

`summariseCurrentWip` takes the Stale line from `describeStaleNow` and the silence sentence from
`describeWhatWipLeavesUnsaid`, so the tool and `lh` share one wording.

## `packages/cli/src/metricsOutput.ts` — `lh metrics team --metrics wip`

```gherkin
@driving_port @us-03 @AC-1 @contract-shape:bounded-change
Scenario: lh shows the stale count in the WIP headline
  Given a Lighthouse that answers the Team WIP read with the contract fixture
  When the user runs "lh metrics team --id 7 --metrics wip"
  Then the headline shows "Stale Work Items  2" below the Blocked line

@driving_port @us-03 @AC-1 @contract-shape:bounded-change
Scenario: lh passes the staleness fields through untouched in JSON
  Given a Lighthouse that answers the Team WIP read with the contract fixture
  When the user runs "lh metrics team --id 7 --metrics wip --json"
  Then every Work Item's isStale and staleReasons equal the fixture's

@driving_port @us-03 @AC-3 @error @contract-shape:bounded-change
Scenario: lh against an older Lighthouse says it does not say which are stale
  Given a Lighthouse that answers the Team WIP read without staleness
  When the user runs "lh metrics team --id 7 --metrics wip"
  Then the output says "Lighthouse does not say which Work Items are Stale."
  And it shows no Stale count
```

## `packages/client/src/index.ts` — the version gate

```gherkin
@us-03 @AC-5 @contract-shape:pure-function
Scenario: The gate names the last Lighthouse release without the staleness fields
  Given the released Lighthouse version that ships slice 01
  When FEATURE_REQUIRES_SERVER_NEWER_THAN.staleness is read
  Then it is the release just before it
```

The runtime does not refuse on the gate: like `isBlocked`, the fields' presence decides, so an older
server is read as "does not say" rather than as an error. The gate documents which server first answers.
Its value is set when slice 01 is released (DESIGN open question).

## `skills/lighthouse-daily-flow-review/` — SKILL.md and its evals

SKILL.md changes, which `skills.drift.test.ts` holds to the tool's real output:

- Step 3 names `isStale` and `staleReasons` among each Work Item's fields.
- A decision **Stale** after **Blocked**: `isStale` true and not already listed as Blocked, the longest
  reason first, quoting Lighthouse's reason ("12 days in Review"). Ask what is holding it.
- "When Lighthouse cannot say" gains **Stale unknown**: when no Work Item carries `isStale`, say Lighthouse
  does not say which Work Items are stale. Never work it out from ages or thresholds.
- The calm-day rule also requires no stale Work Item.

Eval cases (`evals/cases.json`, fixtures under `evals/fixtures/`, `story: "6247"`):

```gherkin
@us-03 @AC-2 @contract-shape:bounded-change
Scenario: The daily names a stale Work Item with Lighthouse's reason
  Given fixture "gravity-stale" — GR-058 stale for 12 days in Review, not Blocked, nothing else to decide
  When the coach asks "What should Team Gravity decide in today's daily?"
  Then the answer names GR-058 with "12 days in Review" and its link, and asks what is holding it
  And the tools called include lighthouse_team_metrics_wip

@us-03 @AC-2 @error @contract-shape:unbounded-preservation
Scenario: A Blocked Work Item that is also stale is named once, as Blocked
  Given fixture "gravity-stale-and-blocked" — GR-061 Blocked 6 days and stale for it
  When the coach asks for the daily
  Then GR-061 appears once, under Blocked, and is not listed again as stale

@us-03 @AC-3 @error @guardrail @contract-shape:unbounded-preservation
Scenario: Against an older Lighthouse the daily says it cannot tell which are stale
  Given fixture "gravity-older-lighthouse" — the WIP read carries no isStale
  When the coach asks for the daily
  Then the answer says Lighthouse does not say which Work Items are stale
  And it lists no Work Item as stale on its own reckoning

@us-03 @AC-4 @contract-shape:unbounded-preservation
Scenario: A calm day is still one line only when nothing is stale either
  Given fixture "gravity-calm" with one Work Item marked stale
  When the coach asks for the daily
  Then the answer does not say "Nothing to decide today. Everything is flowing."
  And it names the stale Work Item
```

`gravity-calm` itself stays calm: every Work Item gains `"isStale": false`, so the existing calm-day case
keeps meaning what it meant.

## The day a read asks about

`asOfDate` is reduced with `DateOnly.FromDateTime`; an offset instant from a non-browser client is reduced to its
UTC date — clients must send a date or local-noon value.

## Versioning (AC-5)

Minor version bump on `client`, `cli`, `mcp-core`, and on `mcp-http` / `mcp-stdio` as they pick it up, through
`pnpm release:version` before the release.

## Scenario count

18 Gherkin scenarios: 4 eval cases, 14 package scenarios. Error and edge paths: 8 of 18 (44%). One walking
skeleton: the MCP tool scenario, because the assistant reading the tool is the user's path the skill rides.
