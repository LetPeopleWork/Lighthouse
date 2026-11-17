# Acceptance brief: story\-6625\-baseline\-report\-creation

## Slices

### S1

##### Covers

P1

P2

```text
On 15 Nov 2026 a Team Admin of Lightspeed opens Create Report, picks a Then of 30 days, keeps the default end and creates the report
```

```text
The Then step reads 'Then 17 Sep – 16 Oct 2026 (30 days)' and 'Now 17 Oct – 15 Nov 2026 (30 days, ends today)' with no overlap or same-days line; Next leads straight to Metrics & name; the create request has Then ending 16 Oct 2026, 30 days, and no Now length
```

```text
She sees both windows back to back by date and is never asked about Now
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportsTab\.createWizard\.test\.tsx\:\:starts a 30\-day report back to back

```text
A wizard that shows the right dates but still sends a Now length equal to Then in the request (the request is checked to have no Now length member)
```

###### Obligations

None.

##### Covers

P3

```text
On 15 Nov 2026 she sets Then to end 15 Nov 2026 with 14 days and creates the report
```

```text
Then and Now both read 2 Nov – 15 Nov 2026 (14 days), the line 'Now and Then are the same days today. Now moves forward from tomorrow; the comparison becomes meaningful as days pass.' shows, no overlap line, and the report is created with Then ending 15 Nov 2026, 14 days
```

```text
She is told the windows are the same days today and can still create the report
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportsTab\.createWizard\.test\.tsx\:\:for a Then ending today\, and still creates the report

```text
A wizard showing the same-days line also for a Then ending yesterday (covered by the windowsRelationOf table and the window law)
```

###### Obligations

None.

##### Covers

P4

```text
On 15 Nov 2026 she sets Then to end 31 Oct 2026 with 30 days
```

```text
Then reads 2 Oct – 31 Oct 2026 (30 days), Now reads 17 Oct – 15 Nov 2026 (30 days, ends today), the line 'Now overlaps Then until 31 Oct 2026' shows and Next stays enabled
```

```text
She sees exactly until which day the two windows share days
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportsTab\.createWizard\.test\.tsx\:\:for a Then ending inside Now

```text
A relation that shows the overlap one day early, for a Then ending the day before Now starts (covered by the window law and its table row)
```

###### Obligations

None.

##### Covers

P5

```text
Lightspeed keeps history from 18 Sep 2026; on 15 Nov 2026 she picks a 30-day Then left at its default end, then tries 29 days; a Portfolio Apollo with the same history gets 30 days
```

```text
The line 'Lightspeed keeps history from 18 Sep 2026: too little for a 30-day Then and Now. Pick a shorter length or a later end date.' shows, Next is disabled, length and end stay 30 days and 16 Oct 2026; with 29 days the line goes and Next is enabled; for Apollo the line names Apollo
```

```text
She learns which owner's history is too short and what to change, and nothing is shifted silently
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportsTab\.createWizard\.test\.tsx\:\:Then and Now side by side on the Then step \> refuses a length the kept history cannot hold with \"Lightspeed keeps history from 18 Sep 2026\: \.\.\.\"

```text
A wizard naming the owner by its kind word only ('This Team') even when the detail page handed it the name (the line is compared in full with the owner's name)
```

###### Obligations

None.

##### Covers

P2

P3

P4

```text
Any Then of 14 to 400 days ending up to 800 days before 15 Nov 2026, or left at its default end, including the Then ending today, the day before Now starts and the day Now starts
```

```text
Then and Now have the same length, Now ends today; the relation is same days exactly when Then ends today, overlap until Then's last day exactly when Then ends on or after Now's first day, else apart; the default end is always apart
```

```text
The overlap and same-days lines never show one day early or name the wrong day
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/thenAndNow\/thenAndNowDraft\.test\.ts\:\:relates every Then of 14 to 400 days\, ending up to 800 days back or at its default end\, to Now by the days they share

```text
A relation correct for every generated day but wrong for Thens longer than 400 days, outside the generated range
```

###### Obligations





##### Covers

P6

```text
On the Reports page a Team Admin creates Then & Now reports for several Teams at once with a Then of 30, 90 or 180 days
```

```text
The wizard names its steps Type, Then, Metrics & name and Owners with no Now step and no 'Same as Then' or 'Custom' Now choice; each Then ends the day before Now starts; the bulk request carries no Now length
```

```text
She is never asked about Now when creating for several owners
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Reports\/BulkReportWizard\.test\.tsx\:\:every Team picked gets the same comparison\, in the order the step lists them\, with no Now length sent

```text
A bulk wizard sending nowLengthDays undefined that serialises away (the request is checked for no Now length member, which undefined would still pass; harmless on the wire)
```

###### Obligations

None.

##### Covers

P1

```text
At the Reports service on 2 Oct 2026 a 90-day Then ending today is created asking for a Now of 45, 0, 14, 90 days or no Now length
```

```text
45, 0 and 14 days are refused with now-length-differs-from-then and the Team holds no report; 90 days and no Now length are created and read back a Now of 2026-07-05 to 2026-10-02, 90 days
```

```text
No report can be made with a Now of another length than its Then
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/BaselineReportCreation\/CreateReportWithEqualNowScenarios\.cs\:\:A\_Now\_length\_is\_accepted\_only\_when\_absent\_or\_equal\_to\_Thens

```text
A server that refuses an unequal Now but still stores the requested Now length on an equal one (the frozen payload is checked to hold no Now length in ThenAndNowTemplateTest)
```

###### Obligations






##### Covers

P1

P2

```text
At the Reports service a 30-day Then ending the day before Now starts is created with no Now length
```

```text
The report reads Then 2026-08-04 to 2026-09-02 and Now 2026-09-03 to 2026-10-02, both 30 days
```

```text
The saved report has a 30-day Now ending today, back to back with Then
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/BaselineReportCreation\/CreateReportWithEqualNowScenarios\.cs\:\:A\_report\_left\_at\_its\_default\_end\_starts\_back\_to\_back\_and\_reads\_a\_Now\_as\_long\_as\_Then\_ending\_today

```text
A server that reads Now from a stored length equal to Then's (indistinguishable at the port in this slice; S2 drops the stored read)
```

###### Obligations

None.

##### Covers

P6

```text
At the bulk Reports service three Teams get a 30-day Then ending the day before Now starts, with no Now length
```

```text
Every Team's report is created and reads a Now of 2026-09-03 to 2026-10-02, 30 days
```

```text
Every report made for several owners at once has a Now as long as its Then ending today
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/BaselineReportCreation\/CreateReportWithEqualNowScenarios\.cs\:\:Reports\_created\_for\_several\_Teams\_at\_once\_each\_read\_a\_Now\_as\_long\_as\_Then\_ending\_today

```text
A bulk create that accepts a Now length different from Then for several owners (the single-owner refusal rows cover the shared Freeze rule, not the bulk path)
```

###### Obligations

None.

##### Covers

P1

P5

```text
Seeded Thens of 14 to 400 days ending up to 800 days back, for Teams keeping 1 to 900 days or every item, including a Then ending today and one starting exactly on the oldest kept day
```

```text
A Then starting before the oldest kept day is refused then-before-cutoff; every other Then is created and reads a Now of the same length ending today that starts on or after the oldest kept day
```

```text
Whenever Then fits the kept history, the matching Now fits too
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/BaselineReportCreation\/CreateReportWithEqualNowScenarios\.cs\:\:A\_Then\_accepted\_at\_create\_has\_a\_Now\_of\_its\_length\_inside\_the\_kept\_history

```text
A policy wrong only for Thens longer than 400 days or Teams keeping more than 900 days, outside the generated range
```

###### Obligations




##### Covers

P5

```text
The server refuses a Create for then-before-cutoff naming 3 Oct 2025; the Team and Portfolio pages hand their names to the Reports tab
```

```text
After the refused Create the wizard reads 'Lightspeed keeps history from 3 Oct 2025: too little for a 90-day Then and Now. Pick a shorter length or a later end date.'; the Team page hands the tab 'Team Lightspeed' and the Portfolio page 'Project Apollo' (TeamDetail.reportsTab and PortfolioDetail.reportsTab tests)
```

```text
The too-little-history line names the owner whose page she is on, also when the server refuses after Create
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportsTab\.createWizard\.test\.tsx\:\:names the owner and the oldest kept day the server refuses Then with

```text
A detail page passing a stale or different owner's name (the stand-in shows exactly the loaded owner's name)
```

###### Obligations

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
A Team or Portfolio Admin creating a Then & Now report is never asked for a Now length; Now is always exactly as long as Then, and the report compares the Throughput Total of Then against Now.
```

#### E2

##### Promises

P2

```text
With today 15 Nov 2026, Then length 30 days and default end date, the Then step shows Then 17 Sep - 16 Oct 2026 and Now 17 Oct - 15 Nov 2026 (both 30 days), no overlap line, no Now question, and the saved report has a 30-day Now ending today.
```

#### E3

##### Promises

P3

```text
Setting Then to end today (15 Nov 2026, 14 days) shows Then and Now both as 2 Nov - 15 Nov 2026 with the text 'Now and Then are the same days today. Now moves forward from tomorrow; the comparison becomes meaningful as days pass.', and the report can still be created.
```

#### E4

##### Promises

P4

```text
Setting Then to end 31 Oct 2026 with 30 days (today 15 Nov) shows Now 17 Oct - 15 Nov 2026 and the text 'Now overlaps Then until 31 Oct 2026'.
```

#### E5

##### Promises

P5

```text
When the kept history starts one day after the first day the Then would need, picking that length shows a message that the Team has too little history for this length, the user cannot proceed until choosing another length or end date, and no window is silently shifted or shortened.
```

#### E6

##### Promises

P6

```text
Creating Then & Now reports for several owners at once from the Reports page asks no Now question, and every saved report has a Now as long as the chosen Then (30 days) ending today.
```

#### E7

##### Promises

P7

```text
A report saved with Then 30 days and Now 45 days shows, after the update, a 30-day Now ending today; its Then window and frozen values are unchanged, past PDF exports are untouched, and no notice is shown.
```

#### E8

##### Promises

P8

```text
Edit report lets the user change only the name and shown metrics, offers no Now length, and does not re-check Then against kept history, so an old report whose Then predates kept history remains editable.
```

#### E9

##### Promises

P9

```text
A create or edit request with Then 90 days and Now length 45 days is refused with a clear message that Now is always as long as Then; the same request with Now 90 days or no Now length is accepted.
```

#### E10

##### Promises

P10

```text
A 30-day report with Throughput Total 52 in Then and 65 in Now shows beside 'Total 65 Work Items' the change '▲ 13 (+25 %)' marked as better; the per-day line keeps its own change '▲ 0.43 per day (+25 %)'; and the CHANGE summary judges Throughput on its Total line.
```

#### E11

##### Promises

P11

```text
Throughput Total 0 in Then and 0 in Now makes both the Total line and per-day line read 'No change (both 0)' with no arrow and no %, and the CHANGE summary counts Throughput as no change.
```

#### E12

##### Promises

P12

```text
A 14-day report with Throughput Total 0 in Then and 30 in Now shows the Total line as '▲ 30 (Then was 0)' with no %.
```

#### E13

##### Promises

P13

```text
A 30-day report with Throughput Total 91 in both Then and Now shows the Total line with the existing grey dash mark and 'No change', counted as no change in the CHANGE summary.
```

#### E14

##### Promises

P14

```text
A report created before Throughput was captured shows no Total change and Throughput is not judged in the CHANGE summary.
```

## Charter

None.

## Examination

None.
