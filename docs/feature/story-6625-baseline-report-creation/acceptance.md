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

### S2

##### Covers

P1

P7

```text
Read a saved report with a 30-day Then whose stored Now was 45 days, on 15 Nov 2026
```

```text
The Now shows 30 days ending today, Then window and frozen values are unchanged, the stored payload is not rewritten
```

```text
An old report compares equal lengths after the update
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/BaselineReportCreation\/EqualLengthReportsScenarios\.cs

```text
Now still shows the stored 45 days, or the read rewrites the stored member
```

###### Obligations

None.

##### Covers

P8

```text
Edit, with no Now length or Now 30, a report whose Then now lies before the oldest kept day
```

```text
Edit is saved; Now is 30 days ending today
```

```text
An old report stays editable
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/BaselineReportCreation\/EqualLengthReportsScenarios\.cs

```text
Edit re-checks Then or Now against the kept history
```

###### Obligations

None.

##### Covers

P9

P8

```text
Edit a 90-day Then report with Now 45, 120, 90 and no Now length
```

```text
45 and 120 refused with now-length-differs-from-then and nothing written; 90 and none saved with no Now length written
```

```text
A different Now length is refused with a clear reason, an equal or absent one is accepted
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/BaselineReportCreation\/EqualLengthReportsScenarios\.cs

```text
A server that accepts Now 45 or writes the Now length it was given
```

###### Obligations

None.

##### Covers

P1

```text
Inspect the Reports module for who builds a Now window
```

```text
Only the window value builds a Now window, the template reads Now through it, and the policy keeps no Now members
```

```text
Every report's Now comes from one place, so equal length cannot drift
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleArchUnitTest\.cs

```text
A second type computes a Now window or the policy keeps NowWindow
```

###### Obligations


##### Covers

P7

```text
Read and list a saved report whose Now was stored 45 days, next to the same report without that stored member
```

```text
Both read and list answers are identical, so no notice or extra member is shown, and the Now window a PDF export reads is 30 days ending today
```

```text
After the update nothing new is shown on the old report and its exports read the equal-length Now
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/BaselineReportCreation\/EqualLengthReportsScenarios\.cs

```text
A notice flag or changed list entry is added for old reports, or the export Now window stays 45 days
```

###### Obligations

None.

##### Covers

P1

```text
Create a report with a 14-day Then ending today, and one with a 90-day Then ending 45 days ago, on 15 Nov 2026, then read each
```

```text
14-day Then 2 to 15 Nov reads Now 2 to 15 Nov; 90-day Then 4 Jul to 1 Oct reads Now 18 Aug to 15 Nov
```

```text
A new report's Now is as long as its Then and ends today
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/BaselineReportCreation\/EqualLengthReportsScenarios\.cs

```text
Now keeps a fixed default length or ends on Then's end
```

###### Obligations



### S3

##### Covers

P10

P1

```text
Open a 30-day report whose Throughput Total is 52 in Then and 65 in Now
```

```text
The Total line shows 13 (+25 %) marked better beside Total 65 Work Items, before per day, which keeps its own change 0.44 per day (+25 %); the summary header lists Throughput as improved even when per day is unmoved
```

```text
Martin reads the Throughput Total compared between Then and Now
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/ThroughputPanel\.test\.tsx

```text
The Total line is drawn with the per day magnitude (13 per day), or a Total mark without a percent, or the summary judging only per day
```

###### Obligations


##### Covers

P11

```text
Open a report whose Throughput Total and per day are 0 in Then and 0 in Now
```

```text
Both lines read No change (both 0) with no mark and no percent; Throughput counts as unchanged in the summary; WIP and Work Item Age panels keep their No change look with a mark
```

```text
A zero Throughput reads as no change on both lines
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/ThroughputPanel\.test\.tsx

```text
The both-0 line shows the grey dash mark, or the both-0 wording leaks into WIP or Work Item Age panels, or only the Total line has the wording
```

###### Obligations


##### Covers

P12

```text
Open a 14-day report whose Throughput Total is 0 in Then and 30 in Now
```

```text
The Total line reads 30 (Then was 0) with a rise mark and no percent
```

```text
Martin sees the rise from nothing without a meaningless percent
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/ThroughputPanel\.test\.tsx

```text
A percent or infinity shown over a Then of 0, or the rise mark missing
```

###### Obligations


##### Covers

P13

```text
Open a 30-day report whose Throughput Total is 91 in Then and 91 in Now
```

```text
Total and per day show the grey dash mark and No change, never both 0, and count as unchanged in the summary
```

```text
Equal non-zero Throughput reads as no change
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/ThroughputPanel\.test\.tsx

```text
Equal non-zero sides reading both 0 or showing 0 percent
```

###### Obligations


##### Covers

P14

```text
Open a report whose Then never captured Throughput
```

```text
No Total change is shown (a dash, no mark) and Throughput is not judged in the summary
```

```text
An older report shows no invented Throughput change
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/changeSummary\.test\.ts

```text
A change drawn against a missing Then, or the metric counted as unchanged
```

###### Obligations


##### Covers

P10

P11

P12

P13

```text
Compare the Throughput Total line for 52 to 65, 65 to 52, 0 to 30, 91 to 91, 0 to 0 and a missing Then, through the panel's change lines
```

```text
Total is a change line before per day; delta, percent, mark and verdict as the shown values give, percent absent over a Then of 0, no change for a missing Then
```

```text
The report summary and panel judge Throughput the same way
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/thenNowComparison\.test\.ts

```text
A percent over a Then of 0 or a Total judged with the wrong direction of good
```

###### Obligations


##### Covers

P10

```text
Draw every panel from one read and tally the summary
```

```text
Throughput draws better and neutral marks, equal to what the summary tallies; other panels unchanged
```

```text
What the reader sees equals what the header counts
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/reportPanelRegistry\.parity\.test\.tsx

```text
A panel drawing a Total line the summary does not tally
```

###### Obligations


##### Covers

P10

```text
Compare Throughput over any equal windows of 14 to 400 days with totals 0 to 3000, including totals differing by 1 over 365 days and both totals 0
```

```text
The Total and per day lines are both judged and never point opposite ways, so Throughput is never placed as mixed; a difference of 1 over a long window is improved; both 0 is unchanged
```

```text
Throughput never reads as both better and worse at once
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/changeSummary\.test\.ts\:\:never judges Throughput mixed\, because its Total and per day change never point in opposite directions

```text
A Total line judged against the wrong direction of good, or a Total line not judged at all
```

###### Obligations




##### Covers

P1

```text
Read a saved report whose older version stored a 45-day Now beside a 30-day Then
```

```text
Now is 30 days ending today; Then and the stored member are untouched
```

```text
A saved report compares equal lengths
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/BaselineReportCreation\/EqualLengthReportsScenarios\.cs\:\:A\_saved\_report\_whose\_Now\_was\_stored\_45\_days\_long\_reads\_a\_30\_day\_Now\_ending\_today\_with\_Then\_and\_its\_stored\_member\_untouched

```text
A Now still read from a stored length
```

###### Obligations



##### Covers

P1

```text
Edit the name and shown metrics of a report whose Then starts before the oldest kept day
```

```text
The edit is accepted and the report stays editable; already covered by the committed scenario
```

```text
An old report can still be renamed and its metrics changed
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/BaselineReportCreation\/EqualLengthReportsScenarios\.cs\:\:A\_report\_whose\_Then\_lies\_before\_the\_oldest\_kept\_day\_stays\_editable

```text
An edit refused because Then predates the kept history
```

###### Obligations



##### Covers

P1

```text
Create reports over generated Thens of 14 to 400 days ending today up to 800 days back, including a 14-day Then ending today and a 90-day Then ending 45 days back
```

```text
Each report reads a Now as long as its Then inside the kept history; no dedicated read scenario names the 14-day-ending-today or 90-day-ending-45-days-ago case, so the generated law is its nearest committed observation
```

```text
Every new report compares equal lengths
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/BaselineReportCreation\/CreateReportWithEqualNowScenarios\.cs\:\:A\_Then\_accepted\_at\_create\_has\_a\_Now\_of\_its\_length\_inside\_the\_kept\_history

```text
A Now of another length for one particular Then, if the generator never draws that Then
```

###### Obligations



##### Covers

P10

```text
Open the whole report of equal 90-day windows with Throughput Total 52 then and 65 now, and variants with the same Total and tiny per day moves
```

```text
The Throughput panel shows two change lines 13 (+25 %) and 0.14 per day (+24 %), named total and per day; the header judges Throughput on its Total line too
```

```text
The report page and its header agree on how Throughput moved
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportView\.test\.tsx

```text
A header that judges per day only
```

###### Obligations


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
A 30-day report with Throughput Total 52 in Then and 65 in Now shows beside 'Total 65 Work Items' the change '▲ 13 (+25 %)' marked as better; the per-day line keeps its own change '▲ 0.44 per day (+25 %)'; and the CHANGE summary judges Throughput on its Total line.
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
