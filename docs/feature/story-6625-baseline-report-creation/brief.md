# Product brief: story\-6625\-baseline\-report\-creation

## Request

```text
ADO Story #6625 (unparented, follow-up of Closed Epic #5878 Baseline). Make creating a Baseline Then / Now report intuitive, from user feedback (Nick Brown): Then and Now can differ in length (30 vs 45 days), a Then ending today leaves no room for a Now, delete is hard to find, Throughput is not compared. Maintainer direction: Now always has the same length as Then. Out of scope: the shown metric set (Story #6649). DISCOVER and DIVERGE skipped by the maintainer 2026-11-15.
```

## Work

M

```text
A user creates a Then / Now report that is a fair like-for-like comparison without having to understand window rules, can find delete, and sees Throughput compared.
```

### Rework

None.

## Promises

### P1

Outcome

```text
A {Team} or {Portfolio} Admin creates a Then & Now report whose Now is always exactly as long as its Then, without choosing a Now length or understanding window rules, so every report, new or already saved, is a like-for-like comparison; and the report compares the {Throughput} Total of Then against Now.
```

### P2

Scenario

```text
A new report starts with Then and Now back to back
```

```text
today is 15 Nov 2026 and Elena, a {Team} Admin of Team Lightspeed, creates a Then & Now report
```

```text
she picks a Then length of 30 days and keeps the default end date
```

```text
the Then step shows Then 17 Sep - 16 Oct 2026 and Now 17 Oct - 15 Nov 2026, both 30 days, with no overlap line, asks no Now question, and the saved report has a 30-day Now ending today
```

### P3

Scenario

```text
A Then ending today is allowed and says the windows are the same days
```

```text
today is 15 Nov 2026 and Elena creates a Then & Now report
```

```text
she sets Then to end on 15 Nov 2026 with a length of 14 days
```

```text
the Then step shows Then 2 Nov - 15 Nov 2026 and Now 2 Nov - 15 Nov 2026 and reads 'Now and Then are the same days today. Now moves forward from tomorrow; the comparison becomes meaningful as days pass.', and she can create the report
```

### P4

Scenario

```text
A Then that partly overlaps Now names the last shared day
```

```text
today is 15 Nov 2026 and Elena creates a Then & Now report
```

```text
she sets Then to end on 31 Oct 2026 with a length of 30 days
```

```text
the Then step shows Now 17 Oct - 15 Nov 2026 and reads 'Now overlaps Then until 31 Oct 2026'
```

### P5

Scenario

```text
Too little kept history for the chosen length is refused and explained
```

```text
Team Lightspeed's kept history starts one day after the first day the back-to-back Then would need
```

```text
Elena picks that Then length
```

```text
the Then step says Team Lightspeed has too little history for this length, she cannot move on until she picks another length or end date, and nothing is shifted or shortened silently
```

### P6

Scenario

```text
Reports created for several owners at once get a Now as long as Then
```

```text
Elena creates Then & Now reports for several Teams at once from the Reports page
```

```text
she sets a Then length of 30 days
```

```text
the wizard asks no Now question and every saved report has a 30-day Now ending today
```

### P7

Scenario

```text
A saved report with a longer Now compares equal lengths after the update
```

```text
a report saved with a Then of 30 days and a Now of 45 days
```

```text
Lighthouse is updated to this version
```

```text
the report shows a 30-day Now ending today, its Then window and frozen values are unchanged, past PDF exports are untouched, and no notice is shown
```

### P8

Scenario

```text
Edit report changes name and shown metrics only
```

```text
Elena may edit a Then & Now report
```

```text
she opens Edit report
```

```text
she can change the name and the shown metrics, no Now length is offered, and Then is not checked again against the kept history, so an old report whose Then is now older than the kept history stays editable
```

### P9

Scenario

```text
A Now length different from Then is refused
```

```text
a request to create or edit a Then & Now report with a Then of 90 days
```

```text
it carries a Now length of 45 days
```

```text
it is refused with a clear message that Now is always as long as Then; the same request with a Now of 90 days or with no Now length is accepted
```

### P10

Scenario

```text
Higher Throughput Total shows its change
```

```text
a Then & Now report of 30-day windows with a {Throughput} Total of 52 in Then and 65 in Now
```

```text
Martin opens the report
```

```text
the Total line shows the change and % change (13 more, +25%) marked as better, per day keeps its own change, and the CHANGE summary judges {Throughput} on its Total line too
```

### P11

Scenario

```text
Throughput Total of 0 on both sides reads as no change
```

```text
a Then & Now report whose {Throughput} Total is 0 in Then and 0 in Now
```

```text
Martin opens the report
```

```text
the Total line reads 'No change (both 0)' with no arrow and no %, and counts as no change in the CHANGE summary
```

### P12

Scenario

```text
Throughput Total from a Then of 0
```

```text
a 14-day Then & Now report whose {Throughput} Total is 0 in Then and 30 in Now
```

```text
Martin opens the report
```

```text
the Total line reads '▲ 30 (Then was 0)' with no %
```

### P13

Scenario

```text
Equal non-zero Throughput Totals read as no change
```

```text
a 30-day Then & Now report whose {Throughput} Total is 91 in Then and 91 in Now
```

```text
Martin opens the report
```

```text
the Total line shows the existing grey dash mark and 'No change', and counts as no change in the CHANGE summary
```

### P14

Scenario

```text
Throughput not captured shows no Total change
```

```text
a report created before {Throughput} was captured
```

```text
Martin opens the report
```

```text
no Total change is shown and {Throughput} is not judged in the CHANGE summary
```

## Scope

```text
Single-owner Create wizard on the {Team} and {Portfolio} Reports tab: no Now step; back-to-back default; Then step shows both windows by date with the same-days, overlap-until and too-little-history messages.
```

```text
Bulk Create wizard on the app-level Reports page: no 'Same as Then / Custom' Now choice; every saved report's Now = Then.
```

```text
Every saved report's Now length snapped to its Then length once.
```

```text
Edit report: name and shown metrics only.
```

```text
The API refuses a create or edit carrying a Now length different from Then's; absent or equal is accepted.
```

```text
{Throughput} Total line gains change and % change and counts in the CHANGE summary.
```

```text
Which metrics and aggregations a Then & Now report shows (the 'Numberwang' feedback) - ADO Story #6649.
```

```text
Finding Edit and Delete in the report list: split out to ADO Story #6650 'Tables: fit the width and show when columns are off screen', which fixes it for every table in the shared DataGridBase.
```

```text
Editing Then, or checking Then again against the kept history on Edit.
```

```text
A notice to owners about reports whose Now was snapped to Then's length.
```

```text
Usage-data events: answered in DEVOPS, not here.
```

## Decisions

```text
Now always has the same length as Then (maintainer reply on ADO #6625). Reverses epic-5878 D43 (Now length chosen and saved separately), D59 and D62 (the Now step), and the Now-length part of D44 (Edit report).
```

#### Status

Confirmed


```text
Q1: new reports default to Then and Now back to back (Then ends the day before the last L days). Ending Then today stays allowed (5878 D38 kept: Now may overlap Then).
```

#### Status

Confirmed


```text
Q1b: when the kept history is too short for the chosen Then, the Then step refuses and explains that the {Team}/{Portfolio} has too little history for this length; the user picks another length or end. No silent shift or shortening.
```

#### Status

Confirmed


```text
Q1c: a Then ending today (identical windows) is allowed with 'Now and Then are the same days today. Now moves forward from tomorrow; the comparison becomes meaningful as days pass.'; a partial overlap reads 'Now overlaps Then until <Then's last day>'; no line when the windows share no day.
```

#### Status

Confirmed


```text
Q2: every saved report's Now is snapped to Then's length once. Then and its frozen values, and past PDF exports, are untouched. No notice to owners, also where a snapped report newly overlaps.
```

#### Status

Confirmed


```text
Q3: the {Throughput} Total line gets change and % change; per day keeps its own; Total counts in CHANGE. Both 0 reads 'No change (both 0)' (no arrow, no %, no change). Then 0 and Now > 0 keeps '▲ N (Then was 0)'. Not captured: no change. % rounding as 5878 defines. Per day showing no change while Total moves by 1 is accepted. Reverses 5878 D53's rule that the Total line carries no change mark and does not count.
```

#### Status

Confirmed


```text
Equal non-zero {Throughput} Totals use 5878's existing equal-value form (grey dash mark and 'No change'), as the maintainer delegated unless a reason to ask appeared; none did.
```

#### Status

Confirmed


```text
Q4/Q6: the list-actions findability problem is split out to ADO Story #6650 (shared DataGridBase, all tables). This feature keeps one value: a like-for-like comparison.
```

#### Status

Confirmed


```text
Q5: the API refuses a create or edit whose Now length differs from Then's with a clear message; absent or equal is accepted.
```

#### Status

Confirmed


```text
Edit cannot change Then, so Edit never checks Then against the kept history; an old report whose Then falls before the kept history stays editable (name, shown metrics).
```

#### Status

Confirmed


```text
5878 decisions reversed or reshaped: D43 (separate saved Now length), D59 and D62 (the Now step), D19 (create flow loses its Now step), D44 and AC-10.2 (Now length editable), D63 (Edit opens on Now; now opens on Metrics & name), D53 (Total excluded from CHANGE). D47's per-day comparison stays, its unequal-length reason no longer applies. D21 and D37 Then validation kept; a valid Then always fits a Now of the same length.
```

#### Status

Confirmed


```text
Whether the snap migration bumps the report's concurrency version: with a bump, an Edit open at that moment is refused as stale even if nothing was changed; without it, an old browser page resending Now 45 is refused by the Q5 rule. Also the gap between deploy and migration. (Quint S2 traces.)
```

#### Status

Open

Design

```text
Quint evidence: quint 0.33.1, model baseline.qnt sha256 88a325f5...0477f and s1grid.qnt sha256 f4615960...dac92, quint run --max-steps 12 --max-samples 20000/50000 --seed 6625, deterministic renderer render.py; random simulation, not exhaustive; model day 1000 = 2026-11-15. Invariants held: Now length = Then length after migration; overlap message iff a shared day; until-date = Then's last day; change only when both totals captured; % only when Then > 0; API accepts only equal or absent Now; Then >= 14 days and not after today. Helper assumptions: check order length, after today, kept history, Now rule; per day rounded to tenths (shipped panel uses 2 decimals, so the 41-of-61 'per day unchanged while Total moves' figure overstates the frequency; behaviour accepted either way); % half away from zero.
```

#### Status

Confirmed


```text
DISCUSS checklist: RBAC no change (create/edit/delete stay TeamWrite/PortfolioWrite via useRbac()). Lighthouse-Clients N/A: CLI and MCP only list and read reports, never send a Now length; no client release. Website no change: 'compares it with a window that moves with today' stays true. Context map: Reports context only (reads Flow metrics as today); no new glossary terms; no domain events; UI surfaces = Reports tab of {Team}/{Portfolio} pages (ADR-271) and the app-level Reports page.
```

#### Status

Confirmed


## Journey

```text
Elena opens the Reports tab of Team Lightspeed and selects Create Report
```

```text
Nick: creating a report should be more intuitive and easier to explain to users
```

```text
She picks Then's end date and length; the step shows Then and Now by date, the same length, back to back by default
```

```text
Nick: confused that a 30-day window could be compared with a 45-day one, and that a Then ending today left no room for a Now
```

```text
She picks the shown metrics and a name and creates the report
```

```text
None supplied
```

```text
Martin opens the report and reads the {Throughput} Total compared between Then and Now
```

```text
Nick: expected the Throughput count to be compared
```

## Slices

### S1

```text
Creating a Then & Now report (single owner and bulk) asks no Now question, starts back to back, shows both windows by date with the same-days, overlap-until and too-little-history messages, and saves Now = Then
```

#### Promises

P1

P2

P3

P4

P5

P6

#### Depends on

None.

#### Slice work

S

```text
A new report is always like-for-like and its windows are visible before creating
```

##### Rework

None.

### S2

```text
Every saved report compares equal lengths: existing reports snapped once, Edit offers name and metrics only, the API refuses an unequal Now
```

#### Promises

P1

P7

P8

P9

#### Depends on

S1

#### Slice work

S

```text
Old and edited reports are like-for-like too
```

##### Rework

None.

### S3

```text
The {Throughput} Total line shows its change and % change and counts in the CHANGE summary
```

#### Promises

P1

P10

P11

P12

P13

P14

#### Depends on

S2

#### Slice work

S

```text
Readers see Throughput compared, safe because every report now has equal windows
```

##### Rework

None.
