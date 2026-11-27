# Product brief: story\-6563\-sle\-attainment

## Request

```text
[the customer] could not find where the SLE is shown. [the informant] looked on the Flow Metrics tab and did not see it until he found the toggle. Benjamin agreed: "where is it actually? We should have that somewhere."

The SLE is a goal, so the point is to see it next to how the Team actually performs, for example 72.7% of items within 10 days against a target of 70%.

Raised by [the customer]

- Make the SLE more prominent instead of hiding it behind chart toggles and a small header indicator.
- See how actual performance against the SLE develops over time. [the informant]: tracking this over time is key.
- Use wording like "or less" so it is clear to people new to flow.

Details to be worked out when we start on it.

(ADO Story #6563 description; customer and informant names replaced because customer identifiers are not committed. Maintainer's scope choice after DIVERGE, 2026-11-27: "Mix + daily record" - one new dedicated SLE widget in the overview, plus the SLE over time in Predictability as a persisted daily record.)
```

## Work

L

```text
A person reading a Team's or Portfolio's metrics sees, without a toggle, how many closed Work Items finished within the SLE against its target, and how that share has moved day by day, so they can say whether the SLE is kept and whether it is getting better or worse.
```

### Rework

None.

## Promises

### P1

Outcome

```text
A reader of a Team's or Portfolio's Flow Overview sees a widget of its own, titled Within SLE, that states the share of the Work Items closed in the selected date range that finished within the SLE as a large number with the count, then the sentence in smaller text, then the target, for example "72.7 % (8 / 11)", "of Work Items closed within 10 days or less", "Target 70 %".
```

#### Amendment

```text
Maintainer, sketch review of the SLE widget, 2026-11-27
```

```text
Maintainer's words: "the initial percentage number should be big, then "of work items close ..." smaller text. also it should be 72.7 % (8 / 11). then below: "Target 70%". also the number on top should be colored accordingly (green if we above or at, amber slightly below, red if much below)."
```

### P2

Scenario

```text
Status follows the percentiles rule
```

```text
Given an SLE is set and at least 5 Work Items closed in the selected range
```

```text
When the reader opens the Flow Overview
```

```text
Then the widget's status is Sustain when the share is at or above the target, Observe when it is below by up to 20 percentage points, and Act when it is more than 20 points below; the status of Cycle Time Percentiles is unchanged.
```

#### Amendment

None.

### P3

Scenario

```text
Change against the preceding period
```

```text
Given an SLE is set
```

```text
When the reader opens the Flow Overview
```

```text
Then the widget's header trend indicator compares the share with the share in the preceding period of the same length, as the other Flow Overview widgets do; the widget body shows no change of its own.
```

#### Amendment

None.

### P4

Scenario

```text
No SLE set
```

```text
Given the Team or Portfolio has no SLE set
```

```text
When the reader opens the Flow Overview
```

```text
Then the widget reads that no SLE is set and its status is Act, prompting to define an SLE.
```

#### Amendment

None.

### P5

Scenario

```text
Too few closed Work Items in the range
```

```text
Given an SLE is set and fewer than 5 Work Items closed in the selected range
```

```text
When the reader opens the Flow Overview
```

```text
Then the widget shows how many closed and that this is too few to judge, and gives no pass or fail status.
```

#### Amendment

None.

### P6

Outcome

```text
A reader of a Team's or Portfolio's Predictability category sees a chart of the share within the SLE day by day, against that day's SLE target, for the Work Items closed in the trailing 30, 60 or 90 days, chosen with a toggle, over the days inside the selected date range.
```

#### Amendment

None.

### P7

Scenario

```text
Each day keeps its own SLE
```

```text
Given days were recorded while the SLE was 70% within 10 days
```

```text
When the SLE is changed
```

```text
Then those recorded days keep their share and target as recorded, and only later days use the new SLE.
```

#### Amendment

None.

### P8

Scenario

```text
Past days are filled in
```

```text
Given a Team or Portfolio whose stored history reaches back before the first recorded day
```

```text
When the reader opens the chart for a range with missing days
```

```text
Then the missing days are worked out from stored history in the same way as Percentiles Over Time, measured against the SLE set when they are filled in, and the chart says that filled-in days use that SLE.
```

#### Amendment

None.

### P9

Scenario

```text
Too few closed Work Items on a day
```

```text
Given a day whose trailing window holds fewer than 5 closed Work Items
```

```text
When the chart is drawn
```

```text
Then that day draws no point, and the chart says why.
```

#### Amendment

None.

### P10

Scenario

```text
No SLE set for the chart
```

```text
Given the Team or Portfolio has no SLE set
```

```text
When the reader opens the chart
```

```text
Then the chart says that no SLE is set instead of drawing a line.
```

#### Amendment

None.

### P11

Scenario

```text
Free
```

```text
Given an instance without a Premium licence
```

```text
When a reader opens the Flow Overview or the Predictability category
```

```text
Then the widget and the chart are shown.
```

#### Amendment

None.

### P12

Scenario

```text
Status colours the share
```

```text
Given an SLE is set and at least 5 Work Items closed in the selected range
```

```text
When the reader opens the Flow Overview
```

```text
Then the large share is green when the widget's status is Sustain, amber when Observe and red when Act, and the status word in the header says the same.
```

#### Amendment

None.

### P13

Scenario

```text
View Data lists the Work Items behind the share
```

```text
Given Work Items closed in the selected range
```

```text
When the reader selects View Data in the widget's header
```

```text
Then the closed-items dialog of Cycle Time Percentiles opens with those Work Items, each with its close day and Cycle Time coloured against the SLE, longest first.
```

#### Amendment

None.

### P14

Scenario

```text
Days the stored history does not reach
```

```text
Given a range that reaches before the first Work Item the Team or Portfolio finished, or after its last successful sync
```

```text
When the reader opens the chart
```

```text
Then those days draw no point, and a range with nothing recorded shows the same sentence as Percentiles Over Time.
```

#### Amendment

None.

### P15

Scenario

```text
Filled-in days look different
```

```text
Given a range with days filled in from stored history
```

```text
When the chart is drawn
```

```text
Then filled-in days draw as open markers and recorded days as filled markers, and a note under the chart says filled-in days are measured against today's SLE.
```

#### Amendment

None.

## Scope

```text
A new SLE widget on the Flow Overview of Teams and Portfolios: the share within the SLE over the selected range, the count and the target; status and header trend indicator.
```

```text
A persisted daily record of the share within the SLE for trailing 30, 60 and 90 days, storing the SLE in force that day, filled in for past days like Percentiles Over Time.
```

```text
A chart of that daily record in the Predictability category of Teams and Portfolios.
```

```text
Docs for both, in the configurable terms, at finalize.
```

```text
ADR-270's Flow metrics glossary and ARCHITECTURE.md section 4 updated for the new concepts.
```

```text
The status tooltips' wording ('@ 10 days' to 'or less') and the Work Item Aging chart's SLE line default: maintainer answered Neither (2026-11-27).
```

```text
Readouts in the Quick Settings header and a configurable info bar: maintainer called it too much for now.
```

```text
An SLE panel in a Then & Now report; flow metric reports are planned separately.
```

```text
Lighthouse-Clients CLI and MCP: a follow-up Story once the API is settled; no client version bump here.
```

```text
Named Cycle Times: the SLE targets the default Cycle Time only.
```

```text
Any change to SLE Risk or to the Cycle Time Percentiles widget and its status.
```

```text
Knowing which SLE was set on a day before the daily record starts; filled-in days use today's SLE (Q4).
```

## Decisions

```text
Q1: the widget counts the Work Items closed in the selected date range, like its sibling widgets; filters apply.
```

#### Status

Confirmed


```text
Q2: the change against the preceding period lives in the widget's header trend indicator (previous-period policy), not in the widget body.
```

#### Status

Confirmed


```text
Q3: the daily record has trailing 30, 60 and 90 day horizons with a toggle, like Percentiles Over Time.
```

#### Status

Confirmed


```text
Q5: fewer than 5 closed Work Items gives no verdict on the widget and no point on the chart.
```

#### Status

Confirmed


```text
Q6: both the widget and the chart appear for Teams and Portfolios.
```

#### Status

Confirmed


```text
Q7: the widget reuses the Cycle Time Percentiles status rule (Sustain at or above target, Observe up to 20 points below, Act beyond or no SLE); Cycle Time Percentiles keeps its own status.
```

#### Status

Confirmed


```text
Q8: free, no Premium gate.
```

#### Status

Confirmed


```text
Q9: the tooltip wording and the Aging chart line default are out of this Story.
```

#### Status

Confirmed


```text
Q10: with no SLE set the widget says so and its status is Act.
```

#### Status

Confirmed


```text
Q11: Lighthouse-Clients follow in a later Story.
```

#### Status

Confirmed


```text
The header trend indicator shows no comparison when either the selected or the preceding period has fewer than 5 closed Work Items.
```

#### Status

Proposed


```text
The loading and the could-not-load looks of the widget and the chart (DESIGN, WidgetShell status).
```

#### Status

Open

Design

```text
Whether the share is computed in the backend for both surfaces; today it is computed twice in the frontend (ragRulesTimeAndAge.ts).
```

#### Status

Open

Design

```text
Where the widget sits in the Flow Overview order and what it is called; the sketch is reviewed at the start of DELIVER.
```

#### Status

Open

Design

```text
Q4: answer 'Record SLE per day', whose option read: 'Each recorded day stores the SLE in force that day, so changing the SLE does not rewrite history. Filled-in past days (before recording) use today's SLE and the chart says so.'
```

#### Status

Confirmed


```text
Q12: answer 'Decide at finalize' - a deferral: whether the website gets a line is decided at finalize.
```

#### Status

Confirmed


```text
Sketch review: the widget is titled 'Within SLE'; View Data reuses the Cycle Time Percentiles closed-items dialog; View Data, the trend and the status sit in the WidgetShell header, not in the widget body.
```

#### Status

Confirmed


```text
Sketch review: the chart has a fixed 0-100 axis 'Within SLE (%)', one accent line, a dashed grey target line labelled at its right end that steps on the day the SLE changed, the 30/60/90 day toggle, and a tooltip with day, share, count, horizon and target.
```

#### Status

Confirmed


```text
Scope: no SLE panel in a Then & Now report, and no change to Cycle Time Percentiles' status - from the maintainer's DIVERGE scope choice 'Mix + daily record' and the Q7 option 'Cycle Time Percentiles keeps its own status unchanged'.
```

#### Status

Confirmed


```text
Named Cycle Times are out: the SLE targets the default Cycle Time, as the Cycle Time Percentiles docs already say.
```

#### Status

Proposed


```text
S1 is the walking skeleton: the widget on the Flow Overview reads real closed Work Items and the owner's real SLE through the running app, end to end, whatever DESIGN decides on where the share is computed.
```

#### Status

Proposed


```text
S2 groups the widget's four verdict states (status, trend, no SLE, too few) and S3 carries the chart's no-SLE and too-few states, so every slice ships without breaking Q5 or Q10.
```

#### Status

Proposed


```text
ADR-270 (Flow metrics glossary: the share within the SLE, the SLE in force on a day) and ARCHITECTURE.md section 4 are updated in this Story.
```

#### Status

Proposed


```text
Which usage-data event shows the feature is used; asked in the DEVOPS wave.
```

#### Status

Open

Product

```text
Which refresh events record the daily record, and whether S3 adds a new read endpoint or extends the over-time one.
```

#### Status

Open

Design

```text
The workshop transcript gap 39:46-40:35 is not yet checked against the recording (maintainer).
```

#### Status

Open

Product

## Journey

None.

## Slices

### S5

```text
The SLE widget on a Team's and a Portfolio's Flow Overview shows the share within the SLE over the selected range, with the count and the target, and is shown without a Premium licence.
```

#### Promises

P1

P11

P13

#### Depends on

None.

#### Slice work

M

```text
The SLE widget on a Team's and a Portfolio's Flow Overview shows the share within the SLE over the selected range, with the count and the target, and is shown without a Premium licence.
```

##### Rework

None.

### S6

```text
The SLE widget shows its status by the percentiles rule, its header trend against the preceding period, 'no SLE set' with Act, and 'too few to judge' below 5 closed Work Items.
```

#### Promises

P2

P3

P4

P5

P12

#### Depends on

S5

#### Slice work

S

```text
The SLE widget shows its status by the percentiles rule, its header trend against the preceding period, 'no SLE set' with Act, and 'too few to judge' below 5 closed Work Items.
```

##### Rework

None.

### S7

```text
The Predictability category shows the share within the SLE day by day for trailing 30, 60 and 90 days, from days recorded as they happen, each day against the SLE in force that day, with no point on a day with fewer than 5 closed Work Items, a 'no SLE set' message, and no point on days the stored history does not reach.
```

#### Promises

P6

P7

P9

P10

P14

#### Depends on

S5

#### Slice work

L

```text
The Predictability category shows the share within the SLE day by day for trailing 30, 60 and 90 days, from days recorded as they happen, each day against the SLE in force that day, with no point on a day with fewer than 5 closed Work Items, a 'no SLE set' message, and no point on days the stored history does not reach.
```

##### Rework

None.

### S8

```text
The daily chart fills in past days from stored history, draws them as open markers, and says that filled-in days are measured against today's SLE.
```

#### Promises

P8

P15

#### Depends on

S7

#### Slice work

M

```text
The daily chart fills in past days from stored history, draws them as open markers, and says that filled-in days are measured against today's SLE.
```

##### Rework

None.
