# Story 6563 — see whether we keep our SLE

ADO Story #6563 "Make the SLE easier to find and compare with actual performance". No parent Epic. Size L (revised
from M at DISCUSS: two new surfaces, one of them a new daily record with fill-in of past days). Route: spine waves,
then nwdes at DELIVER.

- DISCOVER (2026-11-27): `docs/product/jobs.yaml`, `job-flow-coach-see-whether-we-keep-our-sle`. VALIDATED at
  low-to-medium confidence (one informant, one customer workshop).
- DIVERGE (2026-11-27): `diverge/recommendation.md`. Two review rounds; the last round's findings were fixed by the
  diverger and re-checked by the host. Top score: the share as text in Cycle Time Percentiles (4.35). The maintainer
  chose their own mix instead (below).
- DISCUSS (2026-11-27): promises P1–P11, decisions and slices S1–S4 are in `handoff.json`; read them in `brief.md`.

## DIVERGE: maintainer scope choice (2026-11-27)

Asked as one question with four options (mix + columns, widget only, text only, mix + daily record). Answer:
"Mix + daily record" — one new dedicated SLE widget on the Flow Overview, plus a persisted daily record of the share
within the SLE in Predictability, built like Percentiles Over Time.

## DISCUSS: maintainer decisions (2026-11-27)

Asked as options in three batches. The answers, as given (an option label, or the maintainer's own words in quotes):

1. Q1, which closed Work Items the widget counts: "Selected range" — the dashboard's date range, like its sibling
   widgets; filters apply.
2. Q2, the change against the preceding period: "all charts have this comparison with previous period, right? As
   part of the RAG status. so owe should have that too, but not "inside" the widget but just in the rag indicator".
   In the code this is the widget header's trend indicator (`categoryMetadata.ts`, policy `previous-period`), shown
   beside the status. So: a previous-period trend in the header, no change in the widget body.
3. Q3, how a day's share is computed: "Trailing 30/60/90" — like Percentiles Over Time, with a toggle.
4. Q4, the target of a past day: "Record SLE per day" — each recorded day stores the SLE in force that day;
   filled-in past days use the SLE set when they are filled, and the chart says so.
5. Q5, few closed Work Items: "Min 5, else no verdict" — below 5, the widget shows the count and "too few to judge"
   with no pass or fail, and the chart draws no point that day.
6. Q6, owners: "Teams and Portfolios".
7. Q7, the widget's status rule: "Reuse percentiles rule" — Sustain at or above target, Observe up to 20 points
   below, Act beyond that or with no SLE set. Cycle Time Percentiles keeps its own status unchanged.
8. Q8, licence: "Free".
9. Q9, the tooltip wording ("@ 10 days" to "or less") and the Work Item Aging chart's SLE line default: "Neither" —
   both out of this Story.
10. Q10, no SLE set: "Say so, status Act".
11. Q11, Lighthouse-Clients: "Not now, follow-up" — a client Story once the API is settled.
12. Q12, website: "Decide at finalize".

Readings: where an answer above adds words after the dash ("filters apply" in Q1, "no point that day" in Q5, the
SLE-in-force and fill-in clause in Q4), they are the selected option's own description, which the maintainer chose
with the label. Q4's option read in full: "Each recorded day stores the SLE in force that day, so changing the SLE
does not rewrite history. Filled-in past days (before recording) use today's SLE and the chart says so."

Request coverage, per bullet: "more prominent instead of hiding it behind chart toggles and a small header
indicator" → the Within SLE widget (S1, S2); the Aging chart's toggle itself stays, by Q9. "how actual performance
against the SLE develops over time" → the daily chart (S3, S4). "wording like 'or less'" → the widget sentence
("within 10 days or less"); the existing tooltips stay, by Q9.

Points the maintainer said earlier, during DIVERGE, recorded here because they shape the scope (verbatim in
`diverge/request.md`): a configurable info bar in the Quick Settings is "too much for now"; flow metric reports are
planned, so reports not reusing dashboard widgets today is "not an issue".

## DELIVER start, held early: maintainer sketch review (2026-11-27)

The maintainer asked for all sketches before AFK mode ("let's do all sketches first and move into afk mode then"),
so the DELIVER-start sketch review ran at the end of DISCUSS. Asked one decision at a time; answers as given.

**Sketch 1, the Within SLE widget (Flow Overview, small).** Question: "Do we keep our SLE in this range?" Decision:
keep the target, tighten it, or look at the late Work Items.

1. Title: "Within SLE".
2. View Data: "Yes, reuse closed dialog" — the Cycle Time Percentiles closed-items dialog.
3. Layout: "In general yes, but the initial percentage number should be big, then "of work items close ..." smaller
   text. also it should be 72.7 % (8 / 11). then below: "Target 70%". also the number on top should be colored
   accordingly (green if we above or at, amber slightly below, red if much below)."
4. "on sketch one, view data is in the widget shell not part of the widget itself, right?" — yes: View Data, the
   trend and the status sit in the `WidgetShell` header (`WidgetShell.tsx:484`); the body holds only the number, the
   sentence and the target.

```
┌─ Within SLE ───────── ⓘ  [View Data]  ▲ 4.1 pts  ● Sustain ─┐   ← header: WidgetShell
│               72.7 % (8 / 11)                               │   ← large, coloured by status
│     of Work Items closed within 10 days or less             │   ← smaller
│                   Target 70 %                               │
└─────────────────────────────────────────────────────────────┘
Too few (< 5): number in a neutral colour, "3 Work Items closed — too few to judge (needs at least 5)", no status,
no trend. No SLE set: "No SLE set" with status Act. Loading / failure: WidgetShell status.
```

**Sketch 2, Within SLE Over Time (Predictability, large).** Question: "Is our share within the SLE getting better
or worse?" Decision: did a change we made work, and should we act?

5. Filled-in days: "Open markers + note".
6. Y axis: "Fixed 0–100".
7. Otherwise: "Yes, as sketched" — one accent line; dashed grey target labelled at its right end, stepping on the day
   the SLE changed; 30/60/90 day toggle; tooltip "Nov 20 · 72.7 % (8 / 11) within 10 days or less, trailing 30 days ·
   Target 70 %".

```
┌─ Within SLE Over Time ──────── [ 30 days | 60 days | 90 days ]  ⓘ ─┐
│ Within SLE (%)                                                      │
│ 100 ┤                                                               │
│  70 ┤ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─   Target 70 %   │
│  60 ┤ ○──○──○──●         ●──●      ●──●──● 72.7 %                   │
│   0 ┼──────┬──────────┬──────────┬──────────┬──                     │
│          Nov 1      Nov 10     Nov 20     Nov 27                    │
│                            Date                                     │
│ ○ filled in from history (measured against today's SLE)             │
└─────────────────────────────────────────────────────────────────────┘
No point on a day with fewer than 5 closed Work Items, or a day the stored history does not reach. No SLE set: a
message instead of a line. Nothing recorded: the Percentiles Over Time sentence. Loading / failure: WidgetShell.
```

Visualization checklist (`.claude/skills/visualization/SKILL.md` §8): question and decision written (above); awkward
data covered (fewer than 5, no SLE, no history, filled-in days); simplest form (a tile; a line over time); title in
Terminology words and both axes labelled with units; ⓘ text and the docs' "how to read it" passage are written at
DELIVER, with "higher is better"; grey plus one accent, and the widget's colour always comes with the status word;
no bars; no forecast shown; loading, failure and too-little-data distinct from no data; phone width, dark mode,
enlarged view and the PDF export are checked on the built screen at finalize.

## Rigor profile and AFK options (agreed 2026-11-27, before AFK mode)

| Slice | Profile | Crafter, acceptance designer | Reviewers, examiner |
|---|---|---|---|
| S5 the Within SLE widget | standard | sonnet | haiku |
| S6 the widget's verdict states | standard | sonnet | haiku |
| S7 the daily record and its chart | thorough | opus | sonnet |
| S8 filling in past days | thorough | opus | sonnet |
| DESIGN (architect) | thorough | opus | sonnet |

The whole-candidate review and EXAMINE use sonnet, the strictest reviewer model in the table. AFK options: each slice
ends with refactor + review (mutation once, in the final gates); nothing is pushed until the maintainer says so.

## DISCUSS: what stays open

- **Proposed (Product):** the header trend shows no comparison when either period has fewer than 5 closed Work
  Items. It follows from Q5 but was not asked.
- **Open (Design):** the loading and could-not-load looks of both surfaces (`WidgetShell` status); whether the share
  moves to the backend for both surfaces (today `ragRulesTimeAndAge.ts` computes it twice); the widget's name and its
  place in the Flow Overview order. The UI sketches are reviewed at the start of DELIVER.
- **Open (Product, asked at DEVOPS):** which usage-data event shows the feature is used.
- **Open (Design):** which refresh events record the daily record; whether S3 adds a read endpoint or extends the
  over-time one.
- **Proposed:** S1 is the walking skeleton (real closed Work Items and the real SLE, end to end through the running
  app). S2 groups the widget's four verdict states, which a reviewer judges together in one pass; S3 carries the
  chart's own too-few, no-SLE and no-history states, so no slice ships a state that breaks Q5 or Q10. Named Cycle
  Times are out. ADR-270's glossary and `ARCHITECTURE.md` §4 are updated in this Story.

DISCUSS review (`discuss-review.yaml`, Haiku, 2026-11-27): NEEDS_REVISION with one blocking and 13 other findings.
The host fixed them in one product revision (P1 amended in the maintainer's words, P12–P15 added from the sketch
answers, decisions re-statused, the chart's sad paths moved into S3) and in this file; no second round.
- **Not checked yet:** the transcript gap 39:46–40:35 of the workshop recording (maintainer).

## DISCUSS: improvement backlog check (`/continuous-improvement before 6563`, 2026-11-27)

Board: five open improvement Epics in backlog order (6510 Active; 6507, 6508, 6509, 6511 New), 24 open items, and
two unparented clean-as-we-go items (#6647, #6652). The feature touches Flow metrics: the Team and Portfolio
metrics services and controllers, the over-time snapshots and history filler, and the frontend MetricsView.

| Item | Mark | Why |
|---|---|---|
| S12a #6386 Metrics controllers → read-side query ports (New, soft deps only) | **Before, optional** | S3 adds a read endpoint to both wide metrics controllers. S12a is large (Team, then Portfolio). If S3 builds its series as its own query port, like `IPercentilesOverTimeSeriesQuery`, it follows S12a's target shape anyway, so doing S12a first saves little. |
| S07b #6378 `BaseMetricsService` → composition (New, soft deps only) | **Before, only if DESIGN moves the share to the backend** | The Open design point "compute the share in the backend" would add a capability to the base class S07b removes. If the share stays in the frontend, Unrelated. |
| S09a #6382 Context contracts (Active, in flight) | **Order** | It sets the contracts-only rule between contexts, which Flow metrics' new snapshot must follow. DESIGN builds on whatever S09a has landed on `main` by then. |
| #6647, #6652 Clean as we go (backend, frontend tests) | **During** | Covered by "improve the tests you touch": test classes this feature changes move a layer down as part of its own commits. |
| All other items (S04b, S05b–S05g, S07a, S07c, S07d, S08, S09b, S10, S12b, S12c, FE2–FE6, S13, #6540) | Unrelated | Different code areas. |

Maintainer decision (2026-11-27): "yes go on as recommended" — no improvement item before this Story; S3 follows the
query-port shape; DESIGN decides on S07b once it knows where the share is computed.

### DISCUSS checklist

| Item | Answer |
|---|---|
| RBAC impact | **None new.** Both surfaces are read-only metrics on a Team's or Portfolio's page and follow the existing read permission of that page, through `useRbac()`. No new action, page or setting. The daily record is written by the system, like the percentile snapshots. |
| Lighthouse-Clients CLI / MCP | **Deferred by the maintainer (Q11)** to a follow-up Story once the API is settled. S3 adds a new read endpoint (or extends the over-time one); the clients already read Percentiles Over Time, so the follow-up is a natural extension. No client version bump in this Story. |
| Website marketing surface | **Deferred by the maintainer (Q12)** to finalize, once the widget has a screenshot under `docs/assets/`. |
| Context map | See below. |

### Context map (ADR-270, ADR-271)

- **Contexts touched:** Flow metrics (core) owns the share, the daily record (an over-time snapshot table) and the
  history filler, in `Modules/FlowMetrics`. It reads the SLE from the Team's and Portfolio's settings (Organisation
  set-up, still in the `Models` kernel). The status (RAG) is a signal computed on read; Signals is target only, so it
  stays in Flow metrics, like SLE Risk.
- **New terms (proposed, to be named at DESIGN and added to the Flow metrics glossary):** "share within the SLE" — of
  the Work Items closed in a window, the part whose Cycle Time is at or below the SLE's days; and the daily record of
  it. Glossary words used as they are: SLE, Work Item, Cycle Time, Snapshot day.
- **New concept:** the SLE in force on a day, stored with the daily record. Today the SLE exists only as two current
  values on the owner.
- **Domain events:** none published. The daily record is written in reaction to the same refresh events that record
  the percentile snapshots today (to be confirmed at DESIGN).
- **UI surface (ADR-271):** a unit's page, Metrics tab — the widget in the Flow Overview category and the chart in
  the Predictability category, for Teams and Portfolios. Both are their target surfaces already; nothing moves.

## DESIGN (2026-11-27, AFK, propose mode)

The design is in `handoff.json` (read it in `architecture.md`), with the Proposed ADR
`docs/product/architecture/ADR-003.md` and the Quint model and receipt under `design/`. Design review
(`runs/review.design.json`, sonnet): accepted, no findings. The slices are numbered S5–S8 since the DISCUSS revision.

Taken autonomously in AFK mode, for the maintainer to revisit:

- **Header trend with too few:** the Proposed DISCUSS rule (no comparison when either period has fewer than 5 closed
  Work Items, or no SLE) is adopted. It follows from Q5; S6 pins it.
- **Widget place:** `withinSle` sits in the Flow Overview directly before Cycle Time Percentiles, for Teams and
  Portfolios.
- **Share computed in the backend** for both surfaces (one owner of the rule and the minimum of 5); the frontend's
  `calculateSLEStats` stays only for the Cycle Time Percentiles status, which is out of scope. S07b #6378 is not a
  Before item: `BaseMetricsService` gets nothing new.
- **Program.cs registration** of the recording handler, for handler order after the metrics update; S7 therefore runs
  the full backend Integration suite.

## DEVOPS (2026-11-27, AFK)

No material platform risk: one additive migration through the CreateMigration script, the recording handler reuses the
existing failure containment, and nothing new is configured or called outside. Usage data, two name-only events (taken
autonomously; recorded as shared design decisions): `WithinSleListOpened = 31` (S5, View Data opened on the widget)
and `WithinSleHorizonSwitched = 32` (S7, the horizon switched on the chart, which tests DISCOVER's riskiest assumption
that readers use the trend). Each carries nothing; rendering emits nothing.
