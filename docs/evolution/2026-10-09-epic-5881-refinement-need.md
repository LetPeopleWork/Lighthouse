# Refinement need — Epic 5881

**Pushed to `main`, not yet released** · ADO Epic #5881 *"Refinement need: refine enough, then stop"* (`Community`,
Resolved; it closes with the release that carries it) · Stories #6141 (stages, with #6146 folded in and Removed),
#6142 (cadence), #6143 (need and verdict), #6144 (the # column and the "enough for" line), #6145 (the band), #6147
(clients), #6204 (the need covers one Refinement cycle), all Closed · Lighthouse commits `c74aadc3f` (DISTILL,
2026-10-04) … `663d4da18` (#6204 mutation, 2026-10-06) and `98ffecefc` (clients mutation evidence), then the user docs
`10f98534b` and screenshots `4b49bf9cb` (2026-10-09); about 130 commits name an E2 Story, interleaved on `main` with
Epic 5510's · `lighthouse-clients` `a4d9a0a` … `debd656` (#6147) · 44 roadmap steps (03-01 … 03-09, 04-01 … 04-12,
05-01 … 05-10, 06-01 … 06-03, 07-01 … 07-05, 09-01, 6204-01 … 6204-04), every step committed.

Epic 5881 is the second of five Epics (E1–E5) cut from one combined workspace,
`docs/feature/epic-5510-5881-refinement/`. E1 #6136, the Refinement tab, is recorded in
[2026-10-03-epic-6136-refinement-tab.md](./2026-10-03-epic-6136-refinement-tab.md). E3 #5510, sizing votes, was
delivered alongside this Epic and is recorded in [2026-10-09-epic-5510-sizing-votes.md](./2026-10-09-epic-5510-sizing-votes.md).
The two were delivered interleaved, in the order the maintainer fixed at DISCUSS: votes 10 → 11 → 13, then stages 03 →
cadence 04 → need 05 → line 06 → comments 12 → band 07 → account votes 15 → take-back 16 → #6204 → clients 09 →
client votes 17a → 17b. E4 #6137 (live sessions) and E5 #6138 (sizing calibration) are not started, so the workspace
stays where it is.

## What users get

The Refinement tab now answers **"how much should we refine?"**: enough to last until the Refinement after the next
one, and then stop.

- **Settings → Refinement** gains three blocks, each optional and admin-only, saved with the rest of the Team settings:
  - **Stages (optional)** — "Ready when" and "Being refined when", built with the same rule editor the Team's other
    rules use. Whatever neither rule matches is Waiting; when both match, Ready wins.
  - **Refinement cadence** — weekday checkboxes, "Repeat every (weeks)" (1 to 52) and, when it repeats less often
    than weekly, a "Starting week". The same form as a recurring blackout rule.
  - **Work Items needed before the next Refinement** — "Low end likelihood" and "High end likelihood", 50 and 85 by
    default, each between 50% and 95%, the low end below the high end. An info icon says where the range comes from.
- **The heading** counts what is ready. Without stage rules the votes decide ("9 Work Items in Refinement · 2 ready by
  votes"); with stage rules the stage decides and the heading is the breakdown ("2 Ready · 3 Being refined · 4
  Waiting"), a **Stage** column follows State, and the votes' column is headed **Votes say**. A row is flagged in the
  **Warnings** column when somebody has voted and the votes disagree with the stage; a row nobody voted on is never
  flagged.
- **The need message.** With a cadence, one alert under the heading, titled with the next Refinement ("Next Refinement:
  Thu 8 Oct · in 4 days", "tomorrow" for one day). It compares the ready count with the range the Team is likely to
  pull between the next Refinement and the one after it:
  - below — "3 ready — below the range of 5–8 Work Items Team Gravity is likely to pull until the Refinement after.
    Refine 2 to 5 more.";
  - in — "6 ready — in the range of 5–8. Nothing more needs refining by then.";
  - above — "11 ready — above the range of 5–8. Stop refining: nothing more is needed by then." Above is shown as
    loudly as below: refined work that waits goes stale.

  An info icon names the forecast behind the range ("a How Many forecast for the 5 working days between the Refinements
  on Wed 8 Oct and Wed 15 Oct … Same forecast as on the Forecasts page."). Equal ends collapse to one number. Too little
  history shows the forecasts' own "Not enough data yet" message, as information rather than a warning. On a Refinement
  day the message already looks to the following one. A Refinement that falls on a blackout day is skipped.
- **Without a cadence** the heading row reads "No Refinement cadence ⓘ"; the icon tells an admin where to set one and a
  reader that a Team admin can.
- **The Work Items needed first.** While a range shows, a **#** column numbers the rows 1 to the high end, then a line
  "enough for the next Refinement (85%) · not needed before then"; the rows below are muted and unnumbered. The
  numbering follows the order the grid **shows**, so a Team whose backlog order is not its refinement order sorts by
  another column. Fewer in refinement than needed: "All 6 Work Items in Refinement are needed before the next
  Refinement."
- **From the terminal and an assistant** (pushed, not yet released): `lh refinement get --team-id <id>` prints the
  same heading, sentence and numbered list in the instance's own words; `--json` / `--toon` hand over the raw facts.
  The MCP tool `lighthouse_team_refinement_get` returns the facts plus a `summary` with the same sentence.
- Demo data: Team Gravity refines on Thursdays, with the default band and no stage rules.

Every word a Team can rename (Work Item, Refinement, Throughput, Team) is the Team's own. Free, available to anyone who
can read the Team.

## What shipped

**Backend**

- `RefinementSettings` gained `StageRules { Ready, BeingRefined }` (each the shared Work Item rule set, or none),
  `Cadence { weekdays, intervalWeeks, anchorWeek }` (starting week stored as its Monday, a weekday named twice stored
  once, no weekdays meaning no cadence, at most every 52 weeks) and `Band { lowPercentile, highPercentile }`.
  `RefinementSettingsValidator` judges them, and a save that changes only some fields is judged against the stored
  values for the rest. Stage rules that name an additional field the Team no longer has drop that condition
  (`StageRuleHealing`). No migration: they live in the JSON value E1 added.
- **Stages**: `StageRuleMatcher` runs the shared rule engine over the Work Items already in refinement, never beyond
  them; `RefinementResolution` (static, no I/O) sets the stage and `signalsDisagree`. The ready count is the votes'
  without rules and the stages' with them (`readySource: Votes | Stages`); votes never make or block a stage-Ready row.
- **Calendar**: `RefinementCadenceCalendar` (static) gives the next Refinement (first cadence day strictly after today
  in the instance time zone that is not blacked out), whether today is a Refinement day, and the **cycle**: from the
  next Refinement (today, on a Refinement day) to the one after it. `RefinementCalendar` fetches blackout days up to
  the end of the cycle and no further. The weekly rule was extracted from the recurring-blackout code into
  `WeeklyRecurrence`, which both now use.
- **Need**: `RefinementNeedCalculator` runs the manual How Many forecast over the Team's Throughput (the Team's own
  throughput filter respected, as forecasts do) for the cycle's working days and reads it at `GetProbability(100 − p)`
  for each end (`NeedBand`); the verdict is Below, In (both ends included) or Above. Without a number it says why:
  `NoRefinementStates`, `NoCadence` or `InsufficientData` (the forecasts' own five-days guard).
- **Wire, facts only**: `stagesConfigured`, `readyCount`, `readySource`, `nextRefinementDate`, `isRefinementDay`,
  `daysUntilNextRefinement`, `need { low, high, lowPercentile, highPercentile, verdict | unavailableReason,
  horizonWorkingDays, cycleStart, cycleEnd }`, per row `stage` and `signalsDisagree`. Every sentence is composed by
  the client. The "enough for" line is placed in the browser, so the answer carries no row position.
- `RefinementModuleArchUnitTest` now also pins the calendar, the band and the resolution as static and service-free,
  and the stage matcher as reaching the rule engine only through its port.

**Frontend**

- `StageRulesSettings` (reusing `DeliveryRuleBuilder`), `RefinementCadenceSettings` (sharing the recurring-blackout
  weekday fields), `RefinementBandSettings`, composed in `RefinementSettingsSection`.
- `NeedVerdict` (one MUI Alert, titled by `NextRefinement`, its info icon through the tab's `InfoTooltip`),
  `needWording` (the three sentences, the equal-ends form, the cycle wording), `stageBreakdown`, `StageCell`,
  `EnoughForLine` placed by `enoughForPlacement` after the N-th row as shown, following a column filter.
  `RefinementView` hands its grid to `RefinementGrid` / `refinementColumns` and its read to `useRefinement`; a late
  re-read can no longer overwrite a newer vote, and an answer for a Team the page has left is dropped.
- E2E: the third walking skeleton in `specs/teams/Refinement.spec.ts` — a Team admin sets a cadence and the tab says how
  many to refine before the next Refinement, on demo data.

**Clients** (`lighthouse-clients`, pushed, not yet released)

- Client `getTeamRefinement(teamId)` and `getTerminology()` (falling back to the seeded words when that call fails),
  with a server-version gate: a Lighthouse not newer than `v26.10.3.6` is refused before the call.
- CLI `lh refinement get --team-id <id>` with the first per-command pretty renderer (the seam Story #6218 then used for
  every command); MCP `lighthouse_team_refinement_get` with a `summary`. The Lighthouse half of #6147 needed no
  production change: its four scenarios passed on first un-skip and stay as guards.

**Usage data** (listed in `docs/settings/usagedata.md`)

- `TeamRefinementDayVerdictShown = 15`, with the closed enum `refinement_verdict` (`Below`, `In`, `Above`, `None`),
  reported once per tab opening, only on a Refinement day, with no five-second wait. Since Story #6193 the CLI and MCP
  tool report it too.
- `sizing_moment` on the vote and readiness events (Epic 5510) gained `OnRefinementDay` and `OnOtherDay`.
- Story #6204 added no event: the need is already counted by these (maintainer, 2026-10-06).

Architecture: [ADR-214](../product/architecture/adr-214-refinement-settings-are-one-json-valued-property-on-the-team.md)
(every setting member built),
[ADR-215](../product/architecture/adr-215-the-need-band-is-the-manual-how-many-for-the-next-refinement-read-at-100-minus-p.md)
(built, amended for the blackout skip and the one-cycle window),
[ADR-218](../product/architecture/adr-218-stage-readiness-and-the-hidden-split-are-one-pure-resolution-on-read.md)
(built as amended), `brief.md` → "Application Architecture — epic-5510-5881-refinement" → "Built — E2 #5881 and E3
#5510", `ARCHITECTURE.md` §4 module 8.

## The maintainer's review — decisions and reversals

Every E2 screen was sketched before it was built; most changed at the sketch, some again in the running app. Later
calls win over earlier ones.

- **No stage per refinement state; stages come only from two optional rules** (sketch review, 2026-10-04). DISCUSS had
  each refinement state carry a stage, with a separate slice for stage rules. The per-state stage is gone (the value
  already stored stays in the JSON, unread), and the rules slice (#6146) was folded into slice 03 and Removed.
- **Stage and votes are two independent signals** (2026-10-04). DESIGN had a matching rule override the votes. Now a
  rule decides the stage only; the ready count follows the votes on a Team without rules and the stage on a Team with
  them, never a sum. A disagreement is flagged only once somebody has voted: no votes is no opinion.
- **A Refinement on a blackout day is skipped** (during slice 04). DESIGN kept such a date; the maintainer reversed it.
- **The tab's layout after the slice 03–04 review**: the yardstick question moved from its own line into the vote
  column's header; with stage rules the heading *is* the breakdown; without a cadence the heading row reads "No
  Refinement cadence ⓘ" with the hint in its tooltip.
- **The verdict alert** (2026-10-05): above the range is a warning like below; the ready number may appear in both the
  heading and the alert; equal ends collapse to one number; too little history is information, not a warning; an
  info icon says where the range comes from; the next Refinement became the alert's **title**, and the sentences say
  "by then".
- **The line says "the next Refinement", not its date** (2026-10-05), and **it follows the order shown, not the backlog
  order**, because a Team may be unable to fix its backlog order (2026-10-04).
- **The band** (2026-10-05): each end between 50% and 95% (DISCUSS said 1–99), the two fields side by side, the
  tooltip's "(100 − p)%" wording approved as written.
- **The need covers one Refinement cycle** (2026-10-06, Story #6204, after slices 05 and 06 had shipped). The first
  window ran from today to the next Refinement, so the number shrank as the day approached. The maintainer redefined the
  need as a replenishment target: what the next Refinement leaves ready must last until the one after it. The number no
  longer depends on which weekday you look.
- **The clients read like the web** (2026-10-06): `lh refinement get` prints the web's sentence and numbered list, the
  MCP tool adds a `summary`. "All `--pretty` commands should read like this" became its own Story, #6218.
- **Each Epic ships on its own** — a standing rule added to `CLAUDE.md` on 2026-10-04 for future splits. The maintainer
  kept the interleaving for this split only.

## Decisions taken autonomously

- **DISTILL** (`distill/wave-decisions.md`, section "E2 Refinement need"): the cadence's stored form (starting week as
  its Monday, duplicate weekdays once, empty means none); scenarios script the forecast for the exact horizon the tab
  asks for, so a scripted range also proves the horizon, while one parity case runs the real engine against the manual
  forecast; the minimum-data message is the forecasts' own; slice 06 has no backend scenarios; one E2 walking skeleton
  of its own, because each Epic ships alone.
- **#6204 design**: the cycle is a calendar fact derived in one pure place; the blackout lookup fetches only the tail
  beyond the first window, so a day it never fetched is never judged; a cadence with no Refinement after the next one
  reads `NoCadence` rather than a new reason; `horizonWorkingDays` kept its name and now counts the cycle; the
  additive `cycleStart` / `cycleEnd` carry the dates.
- **Copy taken while the maintainer was away**, confirmed by the maintainer at finalize: the settings
  info icon reads "…a How Many forecast for the working days between the next Refinement and the one after"; on a
  Refinement day the below-range sentence ends "…until the next Refinement" (the alert's title already names the
  following one); the in-range and above-range wordings when both ends are equal.
- **A one-ended band sent by a client is judged against the default for the other end** (slice 07 review, accepted
  rather than fixed: only the CLI or MCP can send half a band).
- **Clients** (`distill/clients-slice-09.md`): the line is the web's full sentence; the no-cadence hint is the reader's
  form, because the CLI cannot know whether the caller may change settings; a Team without refinement states gets the
  disabled tab's reader tooltip as its hint; without a verdict the list prints unnumbered.

## Lessons

- **A forecast can be right for the wrong window.** Slices 05 and 06 shipped a correct How Many forecast up to the next
  Refinement; watching it shrink as the day came showed that the decision it feeds is "will this last until the one
  after?". Naming the decision a number serves before choosing its window would have saved Story #6204.
- **A field label and the maths behind it can say opposite things.** The band's "likelihood" fields hold a percentile
  that is read at (100 − p); the tooltip had to be checked against the server's reading and its wording approved
  explicitly.
- **A page that reads and writes needs an ordering rule for every late answer.** The reviews found a late re-read
  undoing a newer vote, a vote answered after switching Team landing on the new Team, and the line placed beside a row
  the grid kept out of sight. Each was real and is pinned now.
- **Sketching before DISTILL pays.** E2 was the first Epic whose UI was sketched before its scenarios were written
  (2026-10-04, after E1 and E3's rework); its later changes were refinements, not rebuilds. The rule to walk the
  sketches at the start of DISTILL is now in `CLAUDE.md`.
- **Mutation tooling can lie.** In `lighthouse-clients`, StrykerJS's vitest runner scored a meaningless 6.83 % against
  vitest 5 (the command runner gives sound results), and in-place mode with a temp directory on another filesystem left
  instrumented files behind. Both are written up in `mutation/clients-slice-09.md`.

## Quality

| Gate | Result |
|---|---|
| Backend and frontend suites, build, Biome | Green before every push (backend run without the live-connector categories); CI green after each push, and each Story was closed only on green CI |
| E2E walking skeletons | The Refinement specs (three since slice 06) green live on demo data before each push |
| Adversarial review (Opus), every slice | Findings fixed test-first, for example 10 on slice 06 and 8 on slice 07 (one accepted, above) |
| Mutation, gate 80 % ([ledger](../feature/epic-5510-5881-refinement/mutation/results.md)) | Stages + cadence (#6141, #6142): backend 88.05 %, frontend 83.60 % (about 92.8 % / 88.3 % after the kill tests, checked by hand) · need and verdict (#6143): 92.73 % / 95.32 % · the line (#6144): frontend 92.93 %, no backend code · band (#6145): 80.59 % / 91.30 % · one cycle (#6204): 86.96 % / 94.29 % · clients (#6147, [ledger](../feature/epic-5510-5881-refinement/mutation/clients-slice-09.md)): 85.65 % → 98.86 %. Every remaining survivor is classified in the ledgers |

## DELIVER checklist

| Item | Answer |
|---|---|
| User-facing docs prose | **Done** in `10f98534b`: `docs/teams/detail.md` (heading, need message, the # column and line, Stage column), `docs/teams/edit.md` (stages, cadence, band), `docs/concepts/concepts.md` (refinement no longer "planned"). The usage-data rows were written with their slices |
| Per-feature screenshots | **Done** in `4b49bf9cb`: `features/refinement.png` (the tab with the need message, the # column and the line) and `features/refinement_settings.png`, from demo Team Gravity. These also settle the screenshots E1 deferred |
| Demo data | **Done** — Team Gravity refines on Thursdays (default band, no stage rules), so the need message, the # column and the line show on demo data; guarded by the walking skeleton |
| Website assets | **Done, not pushed** — website repository commit `117eb96` (2026-10-09): Refinement on the Lighthouse page, a What's New card, a pricing row and a Sizing Poker next step; it is one commit ahead of `origin/main` |
| Usage-data event in `docs/settings/usagedata.md` | **Done** — `TeamRefinementDayVerdictShown` and its `refinement_verdict` field; `sizing_moment` widened with the two cadence moments. #6204: N/A, because the need is already counted by these events (maintainer) |
| Lighthouse-Clients CLI / MCP | **Delivered and pushed (#6147), not released** — the clients' last release is from 2026-09-24; see below |
| Terminology | **N/A, because** E2 needed no new term: Refinement / Refinements came with E1, the rest existed |
| RBAC | **N/A, because** the new settings ride the Team settings write E1 already guards (Team admin), and the tab stays a Team read |

## Still open, knowingly

- **Not released.** Neither this Epic nor E1 or E3 is in a release (the last is `v26.10.3.6`). Epic #5881 stays
  Resolved until then. The baselines of the outcome measures this Epic feeds (in range on a Refinement day, stopping
  after an above-range day, the below-range guardrail) wait for that release.
- **The clients release is owed.** `lh refinement get` and the MCP tool are on `lighthouse-clients` `main` but not
  published; the next client release needs `pnpm release:version` first. It also carries E3's votes, Stories #6218 and
  #6193, and the skills.
- **The website commit `117eb96` is not pushed.**
- **Grids show the first 100 rows** (Story #6202, no parent). Every `DataGridBase` grid is cut at 100 rows; a Team with
  more than 100 Work Items in refinement would not see the rest, and the numbering and the line count only shown rows.
  Pre-existing, found by the slice 06 review.
- **The stored per-state stage is unread.** Every refinement state still stores `Stage: Waiting`; a later change may
  stop writing it. Nothing is removed (additive-only migrations).
- **Traceability tag comments in the scenario files cite internal ids**, as E1 recorded; the project-wide call on
  whether tag comments are exempt is still open.
- **E4 #6137 (live sessions, presenter mode) and E5 #6138 (sizing calibration) are not started.** The workspace stays
  for them.

Delivery history: `docs/feature/epic-5510-5881-refinement/` (shared with E1, E3, E4 and E5).
