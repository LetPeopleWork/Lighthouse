# Lighthouse decides staleness once, on the server — Story 6247

Delivered 2026-10-16 in two of three slices, seven steps. Free, on every licence, no new setting. ADO User
Story #6247, a follow-up from the Lighthouse skills work (#6246), no parent Epic. The third slice, the
Lighthouse-Clients CLI and MCP tools and the Daily Flow Review skill reading the server's answer, is still to
come from the `lighthouse-clients` repository.

Which Work Items were stale was worked out in the browser, so `lh`, the MCP tools and the Daily Flow Review
skill could not tell; a second, different rule sat behind the sync-time "became stale" signal. Copying the
browser's rule into the clients would have made a third. Lighthouse now decides staleness once, with the rule
the dashboard already used, and says so on the Team and Portfolio WIP reads: each Work Item in progress carries
`isStale` and the reasons for it. The dashboard shows that answer everywhere, and the browser's copy of the
rule is gone.

## What shipped

The rule, unchanged from what the dashboard showed before except for the first line:

- Only Work Items in a Doing state can be stale.
- Blocked takes precedence: a Blocked Work Item is judged by its blocked days, not its time in state.
- Time in state is stale when the days are **above** the staleness threshold.
- Blocked is stale when the blocked days are **at or above** the blocked staleness threshold.
- A threshold of 0 turns its trigger off.
- Days are counted the way Work Item Age counts them (the entered day is day 1), as of the read's date, in
  the instance's time zone.
- The sync-time `WorkItemBecameStale` signal uses the same rule and names the threshold of the reason that
  fired.

The wire: `GET /api/latest/teams/{id}/metrics/wip?asOfDate=` and `GET /api/latest/portfolios/{id}/metrics/wip?asOfDate=`
carry `isStale` (bool) and `staleReasons` (`[{ kind, days, stateName? }]`, `kind` one of `TimeInState`,
`BlockedDuration`, `TimeInStateWhileBlocked`), both omitted when null. The last kind is context only: a Blocked
Work Item past the time-in-state threshold shows it, and it does not make the item stale. Every other read is
byte-for-byte unchanged.

| Step | What it changed |
| --- | --- |
| 01-01: the Team WIP read judges staleness | The pure `StalenessPolicy` (`For(owner)`, `Evaluate(candidate, zone, asOfDay)`) and its `StalenessVerdict`; `WorkItemDto.CarryStaleness` builds the policy's input from the DTO's own resolved fields after the as-of projection, so the badge and the verdict read the same values. |
| 01-02: the rule at its boundaries | Time in state 5 → not stale, 6 → stale at threshold 5; Blocked 2 → not, 3 → stale at threshold 3; thresholds off one at a time and both; the as-of day, not today. |
| 01-03: the Portfolio WIP read | A Feature is judged against the Portfolio's thresholds, including in a past state the Portfolio read passes separately. |
| 01-04: the refresh signal | `WorkItemService` evaluates the same policy with the instance clock's today and the blocked decision from `IBlockedItemService`; `WorkItemBecameStale` fires once on the flip into stale, with the threshold that was crossed. |
| 02-01: the badge and the dialog column | The Time in State badge is red exactly when Lighthouse says `isStale`, and its tooltip formats Lighthouse's reasons (`Blocked 6d`, `12d in Review`). |
| 02-02: the chart and the Flow Overview | The aging chart's red dots, the Stale count and its status, and the aging-pace data count what Lighthouse marked stale. On a past range, the dialogs' "N days in State" label counts to the range end. |
| 02-03: the browser's copy is gone | `utils/staleness/` and its tests are deleted; a source guard fails the build if a shipped frontend file names the old rule or compares days with a staleness threshold. |

The look did not change: the same red badge, the same tooltip format, the same red dot. The one number a
reader sees move is the past-range dialog label, which used to count to today.

**ADR-240** ([`docs/product/architecture/adr-240-lighthouse-decides-staleness-once-on-the-server.md`](../product/architecture/adr-240-lighthouse-decides-staleness-once-on-the-server.md))
is **Proposed**: it was taken in AFK mode on the architect's recommendation and waits for the maintainer to
confirm. It supersedes ADR-026's placement of the rule in the browser and amends ADR-070 (the blocked-duration
trigger keeps its semantics and moves with the rule).

## Decisions

### DISCUSS (maintainer, 2026-10-16)

- **The Team and Portfolio WIP reads carry the verdict.** Both owners have both thresholds, and the UI shows
  Stale on both. No other read the UI shows a Stale badge on turned up at DESIGN.
- **Each Work Item carries `isStale` and its reasons**, from a closed set of reason kinds, so a reader can say
  *why* without knowing the rule.
- **The frontend reads the server's answer** and its own rule is deleted.
- **One rule: the dashboard's.** Blocked takes precedence, time in state is `>` its threshold, blocked duration
  is `≥` its threshold, 0 means off, only Work Items in progress.
- **Measured as of the read's date.** This also fixed the aging-pace data, which judged a past range against
  today while the Stale count judged it against the range end.
- **The sync-time signal uses the same rule.** It has no subscriber, so changing it changed nothing a user saw,
  and keeping a second rule behind it would have contradicted the story.
- **The clients and the Daily Flow Review are in the story, as its last slice.** They pass the fields through
  and never compute staleness; against an older Lighthouse without the fields the skill says Lighthouse does
  not say, and never guesses. The maintainer then took slice 03 out of this run, to do from the clients
  repository.
- **Days are counted in the instance's time zone**, the clock Work Item Age uses, not the browser's UTC dates.
  Near midnight a badge may read one day different; that is the cost of one rule.
- DEVOPS was skipped by the maintainer: a read-model field is not a feature to count, so there is no usage-data
  event.

### DESIGN (architect, AFK, 2026-10-16)

- **A pure domain policy**, `Models/Staleness/StalenessPolicy`, with no clock dependency: the zone and the day
  are passed in as values. An entity method was rejected because the Portfolio read passes a Feature's past
  state separately, so a method on the Feature would judge today's state on a past read; an injected service was
  rejected as ceremony around arithmetic. An ArchUnitNET rule keeps `Models.Staleness` free of `Services`,
  `API` and `Data`.
- **The wire shape**: flat `isStale` and `staleReasons` next to the `isBlocked`/`blockedSince` pair the clients
  already read, rather than a nested object; omitted when null, so a read that does not judge staleness, or an
  older server, says nothing instead of "not stale". `StaleReasonDto` is kept apart from the domain's
  `StaleReason`, so a domain rename cannot change the wire silently.
- **The Doing-only tightening**: the browser judged every Work Item the WIP read returned, so one returned in a
  Done state (a past read with no state history, say) could read stale there. It cannot on the server. This is
  the one deliberate departure from the old rule.
- **`WorkItemBecameStale` and `WasStaleAtLastSync` stay.** Removing the code would leave a dead column, and
  dropping the column is not an expand-only migration.
- **No Terminology key for Stale exists**, so "Stale" stays the literal label the dashboard already shows.

### DISTILL (maintainer, AFK)

- **No UI sketches**: nothing new to look at.
- **The dialog's label counts to the range end on a past range**, so it agrees with the reason beside it. Marked
  for the maintainer to revisit; the alternative is counting the label to today and accepting the two numbers
  differing.

## Quality gates

- **Backend.** The last full filtered suite (connector categories excluded) gave 10307 passed, 0 failed, after
  the slice 01 review fixes; nothing on the backend changed after it except the kill tests from mutation.
  `dotnet build` had 0 errors; its only warning is the NU1903 advisory on
  `Microsoft.AspNetCore.SignalR.Common` 10.0.12, which this story did not introduce. `Program.cs` is untouched.
- **Frontend.** The last full `pnpm test` gave 7787 passed, after the final review fixes; `pnpm build` and
  Biome were clean.
- **The final full-suite run** before the push was stopped at the maintainer's word, so the kill tests from
  mutation ran in their own targeted runs only.
- **Mutation** ([`mutation/results.md`](../feature/story-6247-server-marks-stale/mutation/results.md), gate 80 %).
  - Backend, the story's changed lines: 84.62 % → **92.31 %** (36 of 39) after one kill round. The three
    survivors are equivalent, each argued in the results file. Whole files: 63.84 % → 64.59 %; the rest sit on
    `WorkItemService` sync code the story did not change.
  - Frontend, the story's changed lines: 92.31 % → **98.08 %** (51 of 52). The one survivor is equivalent.
- **Reviews.** Every DISTILL and slice 01 review approved with fixes, folded in before the next step. The final
  adversarial review across both slices **REJECTED** the story on one high finding, F1: on a past range east of
  Greenwich, the dialog's "N days in State" label read one day less than Lighthouse's reason in its tooltip
  ("11d in Review — 12d in Review"). The range end arrives as the reader's local midnight, and the label counted
  to that instant's UTC day, which in Zurich is still the day before. The label now counts to the reader's
  calendar day, as the dialog's Work Item Age column already did, and a new test pins that the WIP and Stale
  lists are given the range end. The other three findings were fixed in the same round: the one-rule source
  guard missed a comparison Biome had wrapped onto two lines (F2); a Stale Overview test claimed to exclude
  Blocked Work Items, when the Blocked one dropped out because Lighthouse said it was not stale (F3); and a
  reason naming no state now reads as the Work Item's current state instead of "undefined", and a reason of a
  kind the frontend does not know is left out of the tooltip (F4). Each fix was RED-proven.
- **Dogfood.** Slice 01, on a local instance with demo data (Team Voyager, as of today): all 22 Work Items in
  progress carried `isStale`; 5 were stale, each with one `BlockedDuration` reason of 13 days against the demo's
  5-day blocked threshold, and `stateName` was omitted, as designed.
- **DES integrity.** All seven steps have complete RED, GREEN and COMMIT traces; `des-verify-integrity` exited 0.

## Lessons learned

- **A local-midnight range end is not a UTC day.** The dashboard hands its widgets the range end as the
  reader's local midnight; reducing that instant to a UTC date lands a day early everywhere east of Greenwich.
  The tests had built the range end as noon UTC, which hid it. Turn an instant into the reader's calendar day
  before counting days to it, and build test dates the way the app really produces them.
- **A line-based source guard misses what a formatter wraps.** Biome breaks a long comparison after its
  operator, so a guard reading one line at a time never saw the threshold. The guard now joins a line ending on
  a comparison operator with the next, and its own self-test table carries a wrapped case on each side.

## Finalize checklist

| Item | Answer |
| --- | --- |
| User docs | Done. `docs/metrics/flow-overview.md` gains "What counts as stale": Lighthouse decides, the rule in plain words, the reason tooltip, the instance's time zone, and the past-range label. Its old line that Blocked Work Items are never counted as stale is corrected. `docs/metrics/flow-metrics.md`'s aging chart note now points to that rule. |
| `docs/aiintegration.md` | Unchanged, because it documents neither the WIP read's fields nor what the MCP tools return, and its Daily Flow Review line (Blocked, past the SLE, at risk, WIP over its limit) is still true until slice 03 ships. The fields and the skill's Stale item are documented with slice 03. |
| Release notes | Owed. A vNext line and the ADO `Release Notes` tag are for the maintainer to confirm. |
| Screenshots | N/A, because nothing looks different: the same red badge, tooltip format and red dot. |
| Lighthouse-Clients | Slice 03, still to come: pass the fields through, a version bump, and a version gate naming the first Lighthouse release with them. Until then `lh … --json` already returns the fields unchanged. |
| Website | N/A, because Stale is already on the site as part of the Flow Overview and no new capability is marketed. |
| Demo data | N/A, because nothing new is seeded; the demo's 5-day blocked threshold already shows stale Work Items (see Dogfood). |
| `ARCHITECTURE.md` | The ADR index names ADR-240, and the full set now reads 001–240 with 240 among the *Proposed*. |
| RBAC | N/A, because the fields ride on reads already gated by Team and Portfolio read permissions. |
| Usage data | N/A, because DEVOPS was skipped by the maintainer: a read-model field is not a feature to count. |
| Migrations | N/A, because nothing new is stored. |

## Known and open

- **Slice 03**: the Lighthouse-Clients CLI and MCP tools and the Daily Flow Review skill reading the server's
  verdict, specified in `distill/slice-03-clients-scenarios.md` with its wire fixtures. A client must send
  `asOfDate` as a date or a local-noon value: an offset instant is reduced to its UTC date.
- **ADR-240 is still Proposed**, waiting for the maintainer's confirmation, as is the past-range label decision.
- **The `Slice01ServerMarksStale` acceptance suite takes about 17 s** and needs an entry on the test-time
  budget allowlist; a scenario added there should make it faster rather than split it.
- **The NU1903 advisory on `Microsoft.AspNetCore.SignalR.Common` 10.0.12** in the test project is unrelated to
  this story.
- **Whether the WIP read is refetched after a thresholds save** was left for DELIVER to check by hand and is not
  recorded. If it is not, a threshold edit shows only on the next read.

## Migrated out of the workspace

Nothing was moved. As for the other single Stories, there is no `docs/architecture/<feature>/`,
`docs/scenarios/<feature>/` or `docs/ux/<feature>/`, and what lasts already lives in its permanent home: ADR-240,
the DESIGN delta in `docs/product/architecture/brief.md`, and `ARCHITECTURE.md`. The workspace stays in place,
nothing deleted, as delivery history and as the input to slice 03.

## ADO

Story #6247 Resolved on push, 2026-10-16. Slice 03 is still part of it.

Workspace: [`docs/feature/story-6247-server-marks-stale/`](../feature/story-6247-server-marks-stale/feature-delta.md).
Commits: `3eeecd167..923a8127d` (22, pushed), plus this local docs commit.
