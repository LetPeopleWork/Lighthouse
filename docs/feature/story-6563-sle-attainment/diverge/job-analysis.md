# Job analysis — story-6563-sle-attainment (DIVERGE)

Raw request: `request.md` in this folder (host-written; requester-verbatim blocks unchanged). DISCOVER job:
`job-flow-coach-see-whether-we-keep-our-sle` in `docs/product/jobs.yaml`.

Names: a workshop customer and the informant (no names, per repo rule). Copy uses the seeded terms "SLE" and
"Work Items".

## 1. Job extraction (5 Whys)

| Layer | Question | Answer |
|---|---|---|
| Tactical | What was asked? | "Make the SLE more prominent", show it next to actual performance, show it over time, say "or less" (three proposed fixes). |
| Operational | Why look for the SLE at all? | It is a goal. A goal means nothing until it sits next to what happened: 72.7% of items within 10 days against 70%. |
| Operational | Why does the comparison matter? | A coach has to say, in a retro or to a stakeholder, whether the Team keeps what it published. Today the coach counts dots under a line or hovers a status indicator (habit, inference). |
| Strategic | What decision is behind it? | Whether to keep the SLE, tighten it, or change how the work flows, and whether a change made things better or worse. |
| Physical | Irreducible function | Compare an outcome with a promise, over a stretch of time, and show which way the gap moves. |

Disruption check: a higher job would be "the system tells the coach when the promise breaks" (alerts). That is
`job-flow-coach-act-before-sle-breach` (items in progress, before a breach). It is a different question: this job is
the after-the-fact hit rate, which that job calls "a retro input, not a standup one". It does not remove this job.
A second thought: a share above target can mean the target is too loose (the existing Sustain text says "consider
tightening the target"). So "better or worse" needs the target on the same picture as the share.

## 2. Job statements

- **Functional**: When I review my Team's or Portfolio's flow, in a retro or in front of a stakeholder, I want to see
  how many closed Work Items finished within the SLE we published, against its target, and how that share has moved
  over time, so I can say whether we keep our promise and whether a change made it better, instead of pointing at a line
  on a chart (the registered `job_story` in `jobs.yaml`; the first draft of this section left out the last two clauses).
- **Emotional** (from DISCOVER; a proposal, not an observation): from "we set an SLE, but I cannot tell if we meet it"
  to confident.
- **Social** (same caveat): be the coach who says "we kept our SLE in five of the last six weeks", not the one who
  finds the line and counts dots.

Level check: strategic (a decision about a promise), no feature named in the statement. Gate G1 met.

## 3. What the code already does (verified 2026-11-27, read on this branch)

| Fact | Where |
|---|---|
| The share within the SLE is computed over the selected range, for the Cycle Time Percentiles widget status only. It shows as tooltip text: "72.7% of Work Items are within the SLE target of 70% @ 10 days". | `ragRulesTimeAndAge.ts`, `calculateSLEStats` (private) and `computeCycleTimePercentilesRag` |
| The Cycle Time Scatterplot status computes the opposite share (above the SLE) a second time, with its own thresholds (+10 points). | `computeCycleTimeScatterplotRag` |
| A tile pattern with a dash when no target exists. A zero would read as good news. | `SleRiskWidget.tsx` |
| The SLE is two whole numbers on the owner, shared by Team and Portfolio. | `WorkTrackingSystemOptionsOwner.cs`: `ServiceLevelExpectationProbability`, `ServiceLevelExpectationRange` |
| Percentiles Over Time stores one row a day per owner: P50, P70, P85, P95, with a horizon of 30, 60 or 90 days (Cycle Time) or none (Work Item Age). A past day is filled in from stored Work Items against **today's** configuration; a recorded day is never changed; a day with nothing to measure draws no point. | `PercentileSnapshotWriter.cs`, `PercentilesOverTimeSnapshot.cs`, `docs/metrics/predictability.md` |
| Lighthouse already judged that a percentage over one to four items is noise, and dropped it from one status rule. | `computeWorkItemAgePercentilesRag` (comment) |
| A named Cycle Time makes the Cycle Time Percentiles status neutral, because the SLE targets the default Cycle Time. | `docs/metrics/flow-overview.md` |

Not found in what was read: any history of the SLE value (when the target changed). Only two current values are held.
That matters for any view over time (see option 3 and 4 in `options-raw.md`). Not searched beyond the model fields above.

## 4. ODI outcome statements

| # | Outcome | Evidence |
|---|---|---|
| O1 | Minimize the time it takes to find whether the Team keeps its SLE | The informant looked on the Flow Metrics tab and did not find it until the chart toggle (workshop); part fixed since 2026-11-14 for the scatterplot only |
| O2 | Minimize the likelihood that a reader misreads the share against the target (for example 72.7% against 70%) | The informant's own example; DISCOVER force "will 72.7% against 70% read as pass or fail to people new to flow?" |
| O3 | Minimize the time it takes to see whether the share within the SLE rose or fell between two periods | "Tracking this over time is key" (the informant) |
| O4 | Minimize the likelihood that a share over few closed Work Items is read as a trend | DISCOVER anxiety; arithmetic below |
| O5 | Minimize the effort required to tell a stakeholder how often the SLE was kept | Job social dimension (proposal); habit of counting dots (inference) |
| O6 | Minimize the likelihood that a reader new to flow misreads an SLE as "exactly N days" | "Use wording like 'or less'" (requester) |

Small-n arithmetic (the informant's example is 8 of 11): one Work Item moves an 11-item share by 9.1 points. 7 of 11 is
63.6%, which is 6.4 points under the 70% target, and the status rule turns amber at any share below target. With 3
items, one item moves the share by 33.3 points. Portfolios close fewer items than Teams, so the effect is larger there.

## 5. Under-served candidates

All desk estimates (DISCOVER: importance 3, satisfaction 2; one informant, one organisation; confidence low to
medium). Most under-served in the evidence: O3 (named "key") and O1/O2 (the first complaint). O4 is an anxiety, not a
measured pain. O5 and O6 are inferences or small. Riskiest assumption (from DISCOVER, for DEVOPS to answer with a
usage-data event): readers use the trend, not only today's number.

## 6. Open questions carried into DIVERGE (host-summary in `request.md`, not the requester's words)

1. Where the comparison lives: a widget of its own, a value on an existing widget, the header, elsewhere.
2. How "over time" is computed: a rolling window per day, one value per closed period, or a persisted snapshot.
3. How a small number of closed Work Items is shown honestly.

## 7. New signals after the first draft (four requester-verbatim blocks in `request.md`)

1. The question "so no new widget in the flow overview?". It is a question, not a decision. It is read as a sign that a
   visible place of its own on the Flow Overview matters. The option set holds a Flow Overview widget option.
2. The maintainer's "gut feeling": one new dedicated SLE widget in the overview, perhaps the SLE over time in Predictability,
   and as an alternative "tweaking the quick settings to also give us information" (the SLE and how well it is kept, the
   System WIP Limit beside the number of items in progress, "etc."). Both are in the option set.
3. The maintainer's view that the second direction could grow into "a quick info bar that is freely configurable", which
   "for me is too much for now", and that the widgets and reports people can build are preferred. Recorded as a scope signal
   in the scoring (Viability of the quick-settings option) and in the recommendation.
4. The maintainer's reply that reports not reusing widgets today is "not an issue" because "we plan on having flow metric
   reports", and the request to score "my mix". Recorded as a stated plan (a direction, not a capability) in the Viability
   sensitivity rows; the mix is scored as two package rows.

No outcome statement changes: O1 (find it) is the outcome signals 1 and 2 bear on, and O3 (over time) the one the mix adds.
