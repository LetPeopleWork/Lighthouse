# Feature delta: Show Arrivals in reports (Story #6535)

Wave record for the DISCUSS wave. Earlier waves: `diverge/` (recommendation, job analysis, research). The Story stays
standalone in the ADO board (no parent Epic) and no new ADO items are made; its slices stay inside this Story.

Original Request (kept as given): "Show Arrivals in reports."
The requester said: the number of arrivals in the given timeframe; compare arrivals with {Throughput}; if arrivals is
bigger than {Throughput}, it is a bad sign. The maintainer added: what matters is Then against Now of that balance.

## Wave: DISCUSS / [REF] Persona, job and the one question

- **Persona:** the flow coach (also a Team that tests its own change). Existing persona and job; no new job.
- **Job:** `job-flow-coach-show-whether-flow-changed` (`docs/product/jobs.yaml`). The job's wording is unchanged, but its functional dimension says each panel shows change and percent change and counts points beyond limits; the Arrivals gap line deviates on purpose (M7, M11). Arrivals
  become one more metric in the catalog the reader already ticks.
- **The one question the Arrivals panel answers:** "Do we now start more or less work than we finish, compared with
  before?"
- **The decision it supports:** whether to act on intake. If the gap per day moved up, work piles up as WIP and the
  coach talks about limiting what is started. If it moved down, the coach can say the intake and the finish rate
  are closer together than before. The panel shows; the reader interprets (no verdict, no chart, no generated text).
- **Form:** the existing Then / Now / Change panel. A chart is not needed for the question (visualization skill, section 1).
- **Persona's emotion:** not supplied. Recorded as unknown.

## Wave: DISCUSS / [REF] Decisions

### Maintainer decisions (2026-11-15, LOCKED)

| Id | Decision |
|---|---|
| M1 | One new Then & Now metric, **Arrivals**, with a checkbox in the wizard's metrics step. Its panel shows, for Then and for Now: arrivals total (context), arrivals per day, {Throughput} per day, and the gap per day (arrivals per day minus {Throughput} per day), worded with its sign, for example "0.26 more started than finished per day" or "0.26 fewer started than finished per day". The change mark sits on the gap. |
| M2 | Direction of good for the gap: **lower is better**. No new direction kind. |
| M3 | The gap per day is the balance number. No ratio. |
| M4 | No maximum and no median per day. |
| M5 | The Story stays standalone in ADO. No new ADO items. |
| M6 | Arrivals follows the earlier Then & Now rule for metrics added later (Epic 5878 D27): reports created before release show "Not captured" for it and cannot tick Arrivals; only new reports carry it. |
| M7 | No percent change on the gap. The change line shows only the change in per-day units. |
| M8 | One slice: the Arrivals metric end to end, including "not captured" and the PDF. |
| M9 | The panel applies to Team and Portfolio reports. Portfolio arrivals are {Features} started. |
| M10 | "Balanced" is decided by the shown value: if the gap rounds to 0.00 per day, the panel reads "as many started as finished"; the sign wording follows the rounded value. |
| M11 | Deviation from the job text, recorded on purpose: the Arrivals gap line shows the change in per-day units without a percent (M7, because the gap can cross zero), and, like the other panels today (Epic 5878 D49), it shows no count of points beyond limits. `docs/product/jobs.yaml` is not edited here; the coordinator owns it. |

### Standing rules, defaults and assumptions (not maintainer decisions of this Story)

| Id | Default | Status |
|---|---|---|
| P2 | Then of the Arrivals panel is never rebuilt from today's data; where Then has no value it reads "not captured". Old reports: see M6. | Proposed, default (old reports: locked, M6) |
| P3 | An arrival is a {Work Item} started, the same as on the Metrics tab (`docs/metrics/flow-metrics.md`). No new definition. | Standing rule |
| P4 | User-facing copy uses the terminology words ({Throughput}, {Work Items}, {Features}), never "Epic" or "Story". | Standing rule (project instructions) |
| P5 | The gap is counted per day, so a Then of 90 days and a Now of 30 days compare fairly. The gap is worked out from the unrounded per-day values, then shown rounded. | Assumption; DESIGN confirms |
| P6 | The panel adds no new read of live data for Then: Then is frozen when the report is made, as the other panels. | Assumption from the report model; DESIGN confirms |

Code facts that ground this (read in DISCUSS): the Throughput panel (`panels/ThroughputPanel.tsx`) already shows
total and per day for each side and compares per day; the panel registry (`panels/reportPanelRegistry.ts`) keeps one
entry per metric with a fixed `directionOfGood` of `"lower"` or `"higher"`; the header summary
(`panels/changeSummary.ts`) groups metrics as improved, worse, mixed, unchanged from each panel's change lines;
the backend has one windowed metric per key (`ThroughputReportMetric.cs`, with `total` and `perDay`, built on
`WindowedReportMetric`). Arrivals fit this seam without a new direction kind.

## Wave: DISCUSS / [REF] Journey

**Before.** A flow coach has run a way-of-working change for six weeks. A client asks: "Did we stop starting too much?"
The coach opens the Metrics tab, reads arrivals and {Throughput} for two date ranges, and does the subtraction by hand.
The frozen Then & Now report has no arrivals panel, so the answer is not in the record that goes to the client.

**After.** The coach creates a Then & Now report, and in the metrics step ticks **Arrivals**. The report shows one panel.

Worked example (Team report, Then 90 days, Now 30 days):

```
Arrivals                         Then (90 days)            Now (30 days)             Change
Total started                    99 Work Items             27 Work Items
Arrivals per day                 1.10 per day              0.90 per day
Throughput per day               0.80 per day              1.00 per day
Gap per day                      0.30 more started         0.10 fewer started        -0.40 per day (better)
                                 than finished per day     than finished per day
```

Then started 0.30 per day more than it finished. Now starts 0.10 per day fewer than it finishes. The gap moved down by
0.40 per day, and lower is better, so the change mark reads as better. The header summary lists Arrivals under
"improved". The coach reads it, the client reads it, and nobody has to subtract.

Another example, a worse change: Then arrivals 0.70 per day, {Throughput} 0.70 per day (gap 0, "as many started as
finished"); Now arrivals 0.95 per day, {Throughput} 0.60 per day (gap 0.35 more started than finished per day). The gap
moved up by 0.35 per day, which is worse.

## Wave: DISCUSS / [REF] User story and acceptance criteria

### US-01 Compare the arrivals-to-{Throughput} balance, Then against Now

**As** a flow coach, **I want** the report to show how many {Work Items} were started against how many were finished
per day, Then and Now, **so that** I can say whether intake and finish rate moved closer together or further apart.
(The business motivation beyond the requester's words is not supplied; the benefit above comes from the job.)

Acceptance criteria, in business language:

1. **Arrivals can be chosen.** Given I create a Then & Now report, when I reach the metrics step, then "Arrivals" is
   a checkbox I can tick with the other metrics, and its name follows my terminology settings where it uses them.
2. **The panel shows both sides.** Given a report with Arrivals, when I open it, then Then and Now each show the total started, started
   per day, {Throughput} per day and the gap per day.
3. **The gap reads in words, with its sign.** Given Now started 0.90 and finished 1.00 per day, then the gap reads
   "0.10 fewer started than finished per day". Given Then started 1.10 and finished 0.80, it reads "0.30 more started
   than finished per day".
4. **The change is on the gap.** Given the gap went from +0.30 to -0.10, then the change line shows -0.40 per day with
   the mark for "better", and no percent change is shown. Given it went from 0 to +0.35, the mark is "worse". The marks never rest on colour alone, as
   in the other panels.
5. **Lower is better.** A gap that falls is better, also when it falls from below zero to further below zero (the
   direction is fixed; the panel gives no verdict text).
6. **Per day, not per window.** Given Then is 90 days and Now is 30 days, when I open the report, then both sides are compared on per-day values.
7. **Nothing finished in a window.** Given nothing finished in Now, when I open the report, then {Throughput} per day shows a real 0.00 (not a
   dash) and the gap equals arrivals per day.
8. **Nothing started in a window.** Given nothing started in Now, when I open the report, then arrivals show 0 and 0.00 per day and the gap
   reads "fewer started than finished" by the {Throughput} per day (or equal, if nothing finished either).
9. **Balanced is the shown value (M10).** Given a side's gap rounds to 0.00 per day (for example 0.004 started more
   than finished), when I open the report, then that side reads "as many started as finished per day", never
   "0.00 more" or "0.00 fewer". Given a gap of 0.006, it shows "0.01 more started than finished per day".
10. **Old reports (M6).** Given a report made before Arrivals existed, when I open it, then Arrivals is not offered on
    it and, where shown, reads "Not captured for this report".
11. **Portfolio.** Given a Portfolio report, then the panel counts {Features} started and finished, with the same
    words and rules.
12. **Header summary.** Given a report with Arrivals, then the Arrivals metric appears in the improved, worse, mixed or
    unchanged group by its gap change, like the other metrics.
13. **Frozen Then.** Given the report was made, when the underlying history changes later, then Then does not change.
14. **PDF.** Given I export the report as PDF, then the Arrivals panel prints with the same values and words.

## Wave: DISCUSS / [REF] Slices

Value is the usable panel in the report (M8: one slice). It extends the existing Then & Now path; no new skeleton.

| Id | What the user sees | Size | Riskiest assumption |
|---|---|---|---|
| 01 | The user ticks **Arrivals** in the wizard and the finished report shows the full panel for Then and Now: total, arrivals per day, {Throughput} per day, the signed gap in words, the change mark and per-day change on the gap (no percent), and the header summary entry. Team and Portfolio. "Not captured" where Then has no value, and the PDF prints the panel. | S, about 1 day | "A signed per-day gap, worded in plain terms, is understood at one look, and a change mark on a number that can be negative reads correctly" (P5, AC 3 to 5). Falsified if a reader says "more started than finished" when the gap is negative. |

Slice 01 is the only slice and the first usable one, so it is the one that would emit a usage-data event, if DEVOPS chooses one (open question OQ4).

## Wave: DISCUSS / [REF] What DESIGN must answer

1. **Gap wording rules:** the exact words for more, fewer and balanced; the unit text; the singular and plural of
   the terminology words; whether "started" and "finished" are tied to a terminology key or fixed words.
2. **Rounding:** the gap is worked out from unrounded values (P5); "balanced" and the sign wording follow the shown,
   rounded value (M10). DESIGN names the rounding rule and where it is applied.
3. **The change on the gap:** the change line is the difference of the gaps (-0.40 per day) in per-day units; no percent (M7).
4. **Header summary:** the panel has one comparable line (the gap). Confirm how `summariseChange` treats it when the
   gap change is zero or when Then is not captured (the metric says nothing about how it moved).
5. **What is stored for Then:** the frozen values (total, perDay for arrivals; the gap derived or stored), the backend metric
   key and shape beside `ThroughputReportMetric`, and the wire contract for old reports (P6).
6. **{Throughput} repeated:** the panel repeats {Throughput} per day so it reads alone. If the Throughput metric is also
   ticked, both show the same number; DESIGN confirms this is stored once or twice and that the two never disagree.
7. **Loading, failure and too little data:** reuse the report's existing looks (frame status, "Not captured for this
   report", dashes with a reason). A sketch of the panel in all three states, with the wizard checkbox, is needed at the
   start of DELIVER.
8. **Reads at phone width, in dark mode, enlarged and in the PDF.** Reuse the panel frame.
9. **ⓘ help and docs:** what a value is, which direction is good (lower), and a "how to read it" passage in
   `docs/reports/thenandnow.md` (visualization skill, section 8).
10. **Metric order and name:** where Arrivals sits in the catalog and the metric-name term used in the wizard and the panel title.
11. **Clients:** what the report read through CLI and MCP returns for the new key (see the checklist).

### Visualization review (section 8), answered at DISCUSS

| Item | Answer |
|---|---|
| One question and its decision | Written above. |
| Awkward data | Covered in AC 7 to 10 (nothing finished, nothing started, balanced, not captured, tiny windows). A window of one day and a window with very few items: DESIGN looks at the demo data. |
| Simplest form | The existing panel; no chart. |
| Title, axes | Title with the terminology word for the metric; no axis (no chart). |
| ⓘ and docs | Required (DESIGN item 9). |
| Colour | Existing marks; meaning never by colour alone. |
| States | Reuse existing loading, failure and not-captured looks. |
| Phone, dark, enlarged, PDF | DESIGN item 8; AC 14. |

### DISCUSS checklist

| Item | Answer |
|---|---|
| **RBAC impact** | **N/A, because** no new action, page or permission. Creating a report keeps its existing write permission on the owner and reading keeps its read permission; Arrivals is one more metric inside the same report. Gating stays through `useRbac()` and `IRbacAdministrationService`. |
| **Lighthouse-Clients CLI/MCP** | **Deferred to DESIGN (OQ3), owner DESIGN:** the report DTO gains a metric key; DESIGN checks whether the client types list keys; the `lighthouse-reports` skill must learn the gap wording in any case. The clients read a report's raw values per metric key (`lh report`, MCP report tools, and the `lighthouse-reports` skill). The report DTO gains one metric key and its value names. If the client types list the allowed keys, `lighthouse-client` needs the key, which means a changeset and a manual `pnpm release:version` before the clients release. The `lighthouse-reports` skill must learn how to present Arrivals: quote the values exactly, word the gap with its sign, lower is better, never rebuild Then from today's metrics. A skill-only change needs no changeset. The clients live in a separate repo; this wave records what must follow and changes nothing there. |
| **Website / marketing surface** | **N/A for now, because** it is one more metric in an existing report. Finalize checks whether the website lists the report metrics by name and adds Arrivals if so (assets are hot-linked from `docs/assets`, copy confirmed with the maintainer first). |
| **Context map** | **Done.** Contexts touched: **Reports** (new metric key, panel, frozen values), reading from **Flow metrics** through the same metrics contract (conformist: "a report and the tab never disagree"). New glossary terms: none. "Arrivals" is already a Flow metrics term; "gap per day" is panel wording, not a new domain concept. New concept: none; the metric catalog of Reports gains one entry (ADR-270 "Reports" row, "metric catalog"; DESIGN confirms no change to the map or glossary text, and tells the finalize checklist if one is needed). Domain events: none published, none reacted to. **UI surface (ADR-271):** the report wizard's metrics step and the report page of a unit's Reports tab, unchanged; no move towards the target is needed or cheap here beyond that. |
| **Terminology** | Uses {Throughput}, {Work Items}, {Work Item}, {Features}; "Arrivals" is the metric name today (the Metrics tab calls it Arrivals); whether it needs a terminology key is DESIGN item 10. |
| **Docs and screenshots** | Owed at finalize: `docs/reports/thenandnow.md` (new metric, how to read the gap) and the matching screenshot, one per theme, after the maintainer confirms. |
| **Demo data** | No change planned; DESIGN checks the demo history gives a readable example. |
| **Usage-data event** | Open for DEVOPS (OQ4). |
| **EF migrations** | N/A, because the frozen values are expected to fit the existing report metric storage; DESIGN confirms. |
| **Premium gating** | Unchanged: Arrivals follows the report's existing licence rules. |
| **ADO** | Story #6535 exists and stays standalone; no new items (M5). Not touched by this wave. |
| **Continuous improvement backlog** | Not run in this wave; recommended for the start of DESIGN (`/continuous-improvement before 6535`), as the Story touches the report panel registry and the backend windowed metric. |

## Wave: DISCUSS / [REF] Open questions (not decisions)

| Id | Question | Owner | Needed by |
|---|---|---|---|
| OQ3 | Do the client types list the metric keys, so that a client release is needed, or are keys free text? | DESIGN (reads the clients repo) | DESIGN |
| OQ4 | Which usage-data event, if any, shows Arrivals is used? A name-only event, or "N/A, because" (for example, the existing report-created event already counts use and no property can say which metric was ticked without a closed enum). | DEVOPS | DEVOPS |
| OQ5 | Is "started" and "finished" in the gap wording tied to the terminology words, or fixed? | DESIGN | DESIGN |

Evidence limits: one customer request, relayed second hand; the requester's sentence is the only observed example.
No Quint exploration was run: the behavior is an arithmetic on two per-day values with a sign, and no operation here
has order, repetition or reversal questions that change the product decision (judgment: not relevant; reason recorded).
Jev is refused by configuration, so no semantic call was made.
