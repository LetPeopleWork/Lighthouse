# Acceptance brief: story\-6563\-sle\-attainment

## Slices

### S5

##### Covers

P1

P11

```text
A Team or Portfolio with SLE 70 % within 10 days, 11 Work Items closed in November (8 within 10 days, one at exactly 10), others closed in October, December or still open, no Premium licence; ask the Within SLE read for November
```

```text
Measured, 11 closed, 8 within, share 72.7, target 70 % within 10 days, answered with no licence
```

```text
The reader sees 72.7 % (8 / 11) against a target of 70 % without Premium
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WithinSle\/WithinSleInfoAtTheServicePortTest\.cs

The\_widget\_shows\_the\_share\_within\_the\_SLE\_with\_its\_counts\_and\_target\_without\_a\_Premium\_licence

```text
items closed outside the range were counted only if the closed count were not asserted as 11
```

###### Obligations

None.

##### Covers

P1

```text
Five Work Items closed in November in exactly the SLE days; four closed of 3 days; no SLE set
```

```text
Exactly-SLE items are within (100 %); 4 closed is TooFew with no share; no SLE is NoSle with no share
```

```text
A Work Item finishing on the SLE day counts as within; too few or no SLE are not judged
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WithinSle\/WithinSleInfoAtTheServicePortTest\.cs

A\_Work\_Item\_finished\_in\_exactly\_the\_SLE\_days\_is\_within

```text
a minimum of 4 or 6 would pass only if the 4-closed case were not asserted; it is
```

###### Obligations

None.

##### Covers

P1

```text
Generated lists of 0 to 40 Cycle Times against SLE days 1 to 60, including 4 and 5 closed and a Cycle Time equal to the SLE days
```

```text
Counts and share equal a direct loop count, TooFew below 5
```

```text
The share always matches the Work Items that finished within the SLE
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WithinSle\/WithinSleInfoAtTheServicePortTest\.cs

The\_share\_within\_the\_SLE\_follows\_a\_direct\_count\_of\_the\_Cycle\_Times\_whatever\_their\_order

```text
a share over all items instead of closed ones passes only without open or out-of-range items; the November example covers that
```

###### Obligations


##### Covers

P1

```text
Render the widget from a server answer of 8 of 11 within 10 days, then with a renamed Work Items term, then with counts that disagree with the share
```

```text
72.7 % (8 / 11), of Work Items closed within 10 days or less, Target 70 %; renamed term shown; the server share shown not recounted
```

```text
The widget reads as agreed and in the reader's own words
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WithinSleWidget\.test\.tsx

WithinSleWidget

```text
a widget that recounts passes the first case but not the disagreeing-counts case
```

###### Obligations

None.

##### Covers

P13

```text
On a Team's Flow Overview select View Data on the Within SLE widget
```

```text
The widget is shown; the closed-items dialog lists as many Work Items as the share counts, with Closed and Cycle Time columns, Cycle Times in non-increasing order
```

```text
The reader can list the Work Items behind the share, longest first
```

##### Disposition

Tested

Lighthouse\.EndToEndTests\/tests\/specs\/metrics\/WithinSle\.spec\.ts

a delivery lead sees the share of closed Work Items within the SLE next to its target and can list those Work Items\, longest first

```text
a dialog in the wrong order or population passes only if the order and row count are not compared; both are
```

###### Obligations

None.

##### Covers

P1

```text
Inspect where the share rule, the SLE value, the read port and its implementation live
```

```text
Domain types stay pure in FlowMetrics Domain; the port and its implementation sit in Application
```

```text
none: structure that keeps the share owned in one place
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsModuleLayoutArchUnitTest\.cs

FlowMetricsModuleLayoutArchUnitTest

```text
a domain type reaching an application type would pass if the layout test did not cover new types
```

###### Obligations


##### Covers

P1

```text
Inspect every backend acceptance test for a started web host
```

```text
No new acceptance test starts a host
```

```text
none: keeps scenarios fast at the service port
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.cs

TestHostUseGuardTest

```text
a host-starting scenario passes only if absent from the guard's list
```

###### Obligations


##### Covers

P1

```text
Inspect the widget registry for every dashboard widget
```

```text
Each widget, including Within SLE, has a fetch requirement and a trend policy
```

```text
none: the widget loads its data and shows its trend
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/categoryMetadata\.test\.ts

categoryMetadata

```text
a widget without entries passes if the test does not enumerate the registry
```

###### Obligations


##### Covers

P1

P11

```text
On an instance without a Premium licence, open a Team's or Portfolio's Flow Overview and hold the Within SLE answer; then let it fail
```

```text
The widget is placed without a licence; while the answer is on its way the frame reads loading; when it fails the frame reads error and shows the could-not-load message
```

```text
Without Premium the reader sees the widget, with a loading look or a could-not-load message, never an empty area
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WithinSleWidget\.loading\.test\.tsx

the Within SLE widget on a team\'s Flow Overview

```text
an empty frame passes only if status and message are not read; both are
```

###### Obligations

None.

### S6

##### Covers

P2

```text
Render the Flow Overview answering 72.7 percent (Sustain), 60 and 50 percent (Observe) and 49.9 percent (Act) against a target of 70
```

```text
The widget header carries the matching status word and the Cycle Time Percentiles status stays the unchanged existing rule
```

```text
The status word the reader sees follows the percentiles rule
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WithinSleWidget\.verdicts\.test\.tsx

the Within SLE widget\'s verdict

```text
A neutral status for every measured share, or a status word that disagrees with the share
```

###### Obligations


##### Covers

P12

```text
Render the widget for a measured share of 72.7, 60 and 49.9 percent
```

```text
The header status word and its green, amber or red state agree; the colours themselves are not pinned
```

```text
The status word always comes with the colour state of the share
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WithinSleWidget\.verdicts\.test\.tsx

says Sustain and shows the change against the preceding period when the share is at or above the target

```text
The colour state shown without the status word
```

###### Obligations


##### Covers

P3

```text
Read the widget's trend policy; render the widget with a server comparison of up (+4.1 pts) and with one of none
```

```text
The policy is previous-period; the header trend shows the up arrow and +4.1 pts for up and is absent for none; the body holds no change of its own
```

```text
The reader sees the change against the preceding period in the header
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/categoryMetadata\.withinSle\.test\.ts

compares the share with the previous period

```text
A header trend shown for none, or a down arrow for up
```

###### Obligations


##### Covers

P3

```text
Ask the widget for November when October held five closed (three within 10 days, two on the period edges) plus two within-SLE items just before it, then variants with a fallen share, a too-few period on either side, and no SLE
```

```text
Direction up with +12.7 pts, down with -27.3 pts, and none with no change text when either period is too few or no SLE is set
```

```text
The header trend compares with the preceding period of the same length, and nothing when unjudged
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WithinSle\/WithinSleInfoVerdictsAtTheServicePortTest\.cs

WithinSleInfoVerdictsAtTheServicePortTest

```text
A previous period one day too long or short, or a comparison between unjudged periods
```

###### Obligations


##### Covers

P4

```text
Render the widget with no SLE set
```

```text
The widget reads No SLE set, the status is Act, no trend
```

```text
The reader is told no SLE is set and sees Act
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WithinSleWidget\.verdicts\.test\.tsx

reads that no SLE is set\, says Act and shows no change

```text
Act missing, or an empty body
```

###### Obligations


##### Covers

P5

```text
Render the widget with 3 and with 4 closed Work Items and an SLE set, and with exactly 5 closed
```

```text
3 and 4 read N Work Items closed - too few to judge (needs at least 5) with no status and no trend; 5 closed is judged and shows its status
```

```text
The reader gets a count and no pass or fail below five, and a verdict from five
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WithinSleWidget\.verdicts\.test\.tsx

says \%i closed and that this is too few to judge\, with no status and no change

```text
A status or trend for too few, or a threshold of 6
```

###### Obligations


##### Covers

P2

```text
Open View Data on the withinSle widget, close it, reopen it; render without opening; read the event name's number
```

```text
WithinSleListOpened is reported exactly twice for two opens and never on render; the event name keeps value 31 and carries only its name
```

```text
Each opening of the closed Work Items list counts as one engaged use, rendering counts nothing
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WithinSleWidget\.verdicts\.test\.tsx

reports the list opened once per open of View Data\, and nothing while it only renders

```text
Reported once for the whole session, on render, or renumbered
```

###### Obligations


##### Covers

P2

```text
Compute the rule for shares of 72.7, 70, 69.9, 60, 50 and 49.9 percent against a target of 70, and 65 and 64.9 against 85; no SLE; too few
```

```text
Sustain at or above the target, Observe up to and including 20 points below, Act beyond; no SLE is Act; too few has no status
```

```text
The status the reader sees follows the percentiles rule
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ragRulesTimeAndAge\.test\.ts

computeWithinSleRag

```text
Observe starting at 15 points, or Act at exactly 20 points below
```

###### Obligations


### S7

##### Covers

P7

```text
A Team recorded on 20 November at 70 % within 10 days, the SLE changed to 85 % within 14 days the same day, and refreshed again that day
```

```text
20 November recorded 10 of 11 at 85 % within 14 days, share 90.9
```

```text
Today shows the SLE in force at the last refresh of the day
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WithinSle\/WithinSleDailyRecordAtTheServicePortTest\.cs

Refreshing\_again\_on\_the\_same\_day\_replaces\_that\_day\_with\_the\_SLE\_in\_force\_at\_the\_last\_refresh

```text
a writer that kept two rows for today and a query that returned only the newer one
```

###### Obligations


##### Covers

P6

```text
A Team with 11 Work Items closed early in November, one closed 40 days and one 80 days before 20 November, refreshed on 20 November; the chart asks for the trailing 30, 60 and 90 days
```

```text
the 30 day horizon counts 11 closed, the 60 day horizon 12, the 90 day horizon 13
```

```text
The 30, 60 and 90 day toggle shows the Work Items closed in that trailing window
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WithinSle\/WithinSleDailyRecordAtTheServicePortTest\.cs

Each\_day\_counts\_the\_Work\_Items\_closed\_in\_its\_trailing\_window\_of\_30\_60\_or\_90\_days

```text
a window off by one day at its far end, since no Work Item closes exactly on a window edge
```

###### Obligations


##### Covers

P9

```text
A Team or Portfolio with SLE 70 % within 10 days and 3 Work Items closed is refreshed on 20 November
```

```text
20 November stored with 3 closed, verdict too few, no share, SLE 70 % within 10 days
```

```text
A day with fewer than 5 closed Work Items draws no point but keeps its count
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WithinSle\/WithinSleDailyRecordAtTheServicePortTest\.cs

A\_day\_with\_fewer\_than\_five\_closed\_Work\_Items\_is\_kept\_with\_its\_count\_and\_draws\_no\_point

```text
a minimum of 4 instead of 5, since 3 closed is too few under both; the minimum itself is pinned by the share rule row
```

###### Obligations


##### Covers

P9

```text
Lists of 0 to 40 Cycle Times, exactly 4 and exactly 5, one equal to the SLE days, from fixed seeds
```

```text
too few below 5, otherwise the share follows a direct count of Cycle Times at or below the SLE days whatever their order
```

```text
The minimum of 5 and the within rule are the same for the chart as for the widget
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WithinSle\/WithinSleInfoAtTheServicePortTest\.cs

The\_share\_within\_the\_SLE\_follows\_a\_direct\_count\_of\_the\_Cycle\_Times\_whatever\_their\_order

```text
a chart writer that bypassed the shared share rule; the chart rows pin 8 of 11 and too few for 3 against that
```

###### Obligations


##### Covers

P10

```text
A Team or Portfolio with no SLE set and 11 closed Work Items is refreshed on 20 November; the chart asks for 20 November
```

```text
no day is answered and no current SLE is named
```

```text
With no SLE set nothing is recorded and the chart can say no SLE is set
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WithinSle\/WithinSleDailyRecordAtTheServicePortTest\.cs

Without\_an\_SLE\_no\_day\_is\_recorded\_and\_the\_chart\_is\_told\_no\_SLE\_is\_set

```text
a writer that stored a row with no SLE that the query then filtered out
```

###### Obligations


##### Covers

P6

```text
A Team recorded on 20 and 21 November; the chart asks for 21 November only
```

```text
only 21 November is answered
```

```text
The chart covers the days inside the selected date range
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WithinSle\/WithinSleDailyRecordAtTheServicePortTest\.cs

Only\_the\_days\_inside\_the\_selected\_range\_are\_answered

```text
a range whose start is exclusive, since start and end are the same day here; the first row pins the inclusive end
```

###### Obligations


##### Covers

P14

```text
A Team recorded only on 21 November; the chart asks for 10 to 20 November
```

```text
no day is answered
```

```text
Days the stored history does not reach draw no point
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WithinSle\/WithinSleDailyRecordAtTheServicePortTest\.cs

A\_range\_before\_anything\_was\_recorded\_answers\_no\_day

```text
a query that answered absent days with a zero count that the frontend then hid
```

###### Obligations


##### Covers

P7

```text
A Team recorded on 20 November at 70 % within 10 days, the SLE changed to 85 % within 14 days, and on 21 November 20 November is filled in from a history of six late Work Items
```

```text
20 November still recorded 8 of 11 at 70 % within 10 days
```

```text
Filling in history never rewrites a recorded day
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WithinSle\/WithinSleDailyRecordAtTheServicePortTest\.cs

Filling\_in\_a\_day\_that\_was\_recorded\_leaves\_it\_as\_it\_was\_recorded

```text
a fill that rewrote only the 60 or 90 day rows of a recorded day
```

###### Obligations


##### Covers

P7

```text
A Team with SLE 85 % within 14 days; 20 November filled in from a history with nothing closed; the chart asks for the trailing 90 days
```

```text
20 November filled in with 0 closed, 0 within, SLE 85 % within 14 days
```

```text
A filled-in day with nothing closed is kept with its count
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WithinSle\/WithinSleDailyRecordAtTheServicePortTest\.cs

A\_filled\_in\_day\_with\_nothing\_closed\_is\_kept\_with\_a\_count\_of\_zero\_on\_every\_horizon

```text
a missing 30 day row, since only 90 days is read here; the two-copies row reads all three horizons
```

###### Obligations


##### Covers

P7

```text
A Team with SLE 70 % within 10 days; today 20 November is filled in from six late Work Items, then refreshed the same day
```

```text
20 November recorded 8 of 11 at 70 % within 10 days
```

```text
Today refreshed after a fill-in counts as recorded
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WithinSle\/WithinSleDailyRecordAtTheServicePortTest\.cs

Today\_filled\_in\_and\_then\_refreshed\_is\_a\_recorded\_day

```text
a refresh that overwrote today only when its counts differed
```

###### Obligations


##### Covers

P10

```text
A Team with no SLE set; on 21 November 20 November is filled in from five Cycle Times
```

```text
no day is answered and no current SLE is named
```

```text
Filling in history without an SLE writes nothing
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WithinSle\/WithinSleDailyRecordAtTheServicePortTest\.cs

Without\_an\_SLE\_filling\_in\_a\_past\_day\_writes\_nothing

```text
a fill that wrote a row with a last known SLE, since no SLE was ever set here
```

###### Obligations


##### Covers

P7

```text
Two copies of the application fill in 20 November for the same Team at the same time from different histories
```

```text
the second save is not an error; one row per horizon 30, 60 and 90; the first copy's 3 of 5 within at 85 % within 14 days
```

```text
Two app copies never stack two points on one day
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WithinSle\/WithinSleDailyRecordAtTheServicePortTest\.cs

Two\_copies\_of\_the\_application\_filling\_in\_the\_same\_day\_leave\_one\_day\_with\_the\_first\_copys\_counts

```text
a save that dropped the second copy's whole pass instead of only the colliding day, since only one day is filled here
```

###### Obligations


##### Covers

P10

```text
The chart is answered with no SLE set
```

```text
the chart says No SLE set and draws no chart
```

```text
The reader is told no SLE is set instead of seeing a line
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WithinSleOverTimeWidget\.test\.tsx

says that no SLE is set instead of drawing a line

```text
the message shown inside an otherwise empty chart frame with axes
```

###### Obligations


##### Covers

P14

```text
The chart is answered with an SLE set and no recorded day in the range
```

```text
the Percentiles Over Time sentence, Nothing to show for the selected range. Days appear here as Lighthouse records them., and no chart
```

```text
A range with nothing recorded says so in the same words as Percentiles Over Time
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WithinSleOverTimeWidget\.test\.tsx

shows the Percentiles Over Time sentence when nothing was recorded in the range

```text
the sentence also shown while the answer is loading; the loading row pins status loading instead
```

###### Obligations


##### Covers

P6

```text
The chart's answer is on its way, then fails
```

```text
status loading while waiting; status error with the could-not-load message after the failure
```

```text
A slow or failed load never looks like no data
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WithinSleOverTimeWidget\.test\.tsx

reads loading while its answer is on the way\, and could\-not\-load with its message when the answer fails

```text
an older answer still drawn undimmed beside the error message
```

###### Obligations


##### Covers

P6

```text
The chart shows the trailing 30 days; the reader switches to 60 days while that answer is still on its way, then it arrives
```

```text
first an ask for 30 days with 30 days pressed and no usage report; after the switch an ask for 60 days, status loading, one report WithinSleHorizonSwitched with nothing else; then the 60 day answer drawn and status ready
```

```text
The toggle shows the chosen horizon and never the old one as current; each switch is counted with no details
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WithinSleOverTimeWidget\.test\.tsx

asks for the trailing 30 days first\, then the horizon the reader switches to\, never showing the old horizon\'s answer as current\, and reports each switch

```text
a switch to 90 days reported twice, since only one switch is made
```

###### Obligations


##### Covers

P6

```text
The refresh handlers of a Team and of a Portfolio as the application registers them
```

```text
the Within SLE recorder runs after the metrics update and the other recorders, in the pinned order
```

```text
The day is recorded from fresh metrics, not stale ones
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventHandlerOrderPinTest\.cs

DomainEventHandlerOrderPinTest

```text
none known: the full order list is compared
```

###### Obligations


##### Covers

P6

```text
The production assembly's FlowMetrics types and files
```

```text
writer, query, family list and family reader in Application, their contracts in Application, the recorder in Policies, the repository in Adapters/Persistence and seen by every repository rule
```

```text
The daily record lives where the rest of the flow metrics live
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsModuleLayoutArchUnitTest\.cs

FlowMetricsModuleLayoutArchUnitTest

```text
an extra unlisted helper type outside the module
```

###### Obligations


##### Covers

P6

```text
The migration that adds the daily record table
```

```text
it only adds; no column or table is dropped or renamed
```

```text
An upgrade or a restored backup keeps every existing row
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ExpandOnlyMigrationGuardTest\.cs

ExpandOnlyMigrationGuardTest

```text
a migration that adds the table without its unique index; the two-copies row observes the index
```

###### Obligations


##### Covers

P6

```text
The chart endpoint is asked for a horizon of 45 days
```

```text
refused with 400 naming the allowed horizons 30, 60 and 90
```

```text
Only the 30, 60 and 90 day horizons are served
```

##### Disposition

NotTested

```text
The refusal is an HTTP status of the controller action; the design declares the application-service port but no wire-contract locator and no action member for within-sle-over-time, so the status cannot be observed without inventing the action signature
```

design

##### Covers

P7

P6

```text
A Team or Portfolio with SLE 70 % within 10 days and 11 Work Items closed early in November is refreshed on 20 November; the SLE is changed to 85 % within 14 days; it is refreshed on 21 November; the chart asks for the trailing 30 days from 20 to 22 November
```

```text
20 November recorded 8 of 11 at 70 % within 10 days, share 72.7; 21 November recorded 10 of 11 at 85 % within 14 days, share 90.9; no day for 22 November; current SLE 85 % within 14 days
```

```text
Past days keep the share and target they were measured with; only later days use the new SLE
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WithinSle\/WithinSleDailyRecordAtTheServicePortTest\.cs

A\_recorded\_day\_keeps\_its\_share\_and\_target\_after\_the\_SLE\_is\_changed\_and\_only\_later\_days\_use\_the\_new\_SLE

```text
a refresh that also wrote rows for the 60 and 90 day horizons with wrong counts, since only the 30 day horizon is read here
```

###### Obligations



##### Covers

P7

```text
A Team with SLE 85 % within 14 days; on 21 November nobody recorded 20 November and it is filled in from Cycle Times of 3, 9, 14, 15 and 30 days
```

```text
20 November filled in, 3 of 5 within, SLE 85 % within 14 days
```

```text
A filled-in past day is measured against today's SLE and marked as filled in
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WithinSle\/WithinSleDailyRecordAtTheServicePortTest\.cs

A\_past\_day\_nobody\_recorded\_is\_filled\_in\_against\_the\_SLE\_set\_now\_and\_marked\_as\_filled\_in

```text
a fill that read the trailing window of the wrong day, since the history here answers the same Cycle Times for every window
```

###### Obligations



##### Covers

P6

P9

P14

```text
Predictability is answered 20 November 72.7 % at 70 %, 21 November 90.9 % at 85 %, 22 November 3 closed too few, 23 November not recorded
```

```text
a share line with 72.7 and 90.9 and no point on 22 or 23 November, never 0 %; a target line stepping from 70 to 85; a note that days with fewer than 5 closed Work Items draw no point; share axis fixed 0 to 100; axes labelled Within SLE (%) and Date; status ready
```

```text
The reader sees the share day by day against that day's target, with gaps where there is too little or no data
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WithinSleOverTimeWidget\.test\.tsx

draws each day\'s share against the target in force that day\, with no point for a day too few to judge or never recorded

```text
a filled-in day drawn with the same marker as a recorded day; the open-marker note is not pinned because its copy is not agreed
```

###### Obligations


## Expectations

### Rows

#### E1

##### Promises

P1

```text
Flow Overview of a Team or Portfolio shows a separate widget titled Within SLE: large share with count like '72.7 % (8 / 11)', smaller sentence 'of Work Items closed within 10 days or less', then 'Target 70 %'.
```

```text
Oracle may check text but not that the number is visibly large, the sentence smaller, or the layout order as a reader sees it.
```

None.

None.

None.

#### E2

##### Promises

P1

P12

```text
The large share is green at or above target, amber slightly below, red much below, and the header status word (Sustain/Observe/Act) agrees with the colour.
```

```text
Oracle may check a status string, not the rendered colour, its agreement with the header word, or the 20-point thresholds.
```

None.

None.

None.

#### E3

##### Promises

P2

```text
With an SLE set and at least 5 closed items, status is Sustain at or above target, Observe up to 20 points below, Act more than 20 below; Cycle Time Percentiles status stays unchanged.
```

```text
Boundaries (exactly target, exactly 20 below) and the untouched Percentiles widget may be unseen by an oracle.
```

None.

None.

None.

#### E4

##### Promises

P3

```text
The header trend indicator compares the share with the preceding period of equal length like other widgets; the body shows no change figure of its own.
```

```text
Oracle may not see the visible absence of a body delta nor that the preceding-period comparison is right for the user's range.
```

None.

None.

None.

#### E5

##### Promises

P4

```text
With no SLE set, the widget says no SLE is set, shows status Act, and prompts to define an SLE.
```

```text
Wording and whether the prompt helps the reader reach SLE definition are not visible to a data-level oracle.
```

None.

None.

None.

#### E6

##### Promises

P5

```text
With an SLE and fewer than 5 closed items, the widget shows how many closed, says this is too few to judge, and shows no pass/fail status.
```

```text
Oracle may miss that no status badge or colour appears at all, including at exactly 4 versus 5 items.
```

None.

None.

None.

#### E7

##### Promises

P6

```text
The Predictability category shows a chart of daily within-SLE share against that day's SLE target for items closed in the trailing 30, 60 or 90 days, chosen with a toggle, over days inside the selected date range.
```

```text
Toggle behaviour and limiting days to the range are interaction and rendering facts an API-level oracle does not see.
```

None.

None.

None.

#### E8

##### Promises

P7

```text
After the SLE is changed, previously recorded days keep their recorded share and target; only later days use the new SLE.
```

```text
Oracle may not observe the chart's historical line across a real SLE change.
```

None.

None.

None.

#### E9

##### Promises

P8

```text
Missing days are filled from stored history the same way as Percentiles Over Time, measured against the SLE at fill time, and the chart states that filled-in days use that SLE.
```

```text
Equivalence to Percentiles Over Time and the on-chart statement are not visible to a narrow oracle.
```

None.

None.

None.

#### E10

##### Promises

P9

```text
A day whose trailing window holds fewer than 5 closed items draws no point and the chart says why.
```

```text
Oracle may see missing data but not the explanatory text shown to the reader.
```

None.

None.

None.

#### E11

##### Promises

P10

```text
With no SLE set, the chart says no SLE is set instead of drawing a line.
```

```text
Oracle may not see absence of a line or the message as rendered.
```

None.

None.

None.

#### E12

##### Promises

P11

```text
Without a Premium licence, the widget and the chart are both shown.
```

```text
Oracle may run only with a licence; the unlicensed instance experience is easy to miss.
```

None.

None.

None.

#### E13

##### Promises

P13

```text
View Data in the widget header opens the Cycle Time Percentiles closed-items dialog listing the closed items with close day and Cycle Time coloured against the SLE, longest first.
```

```text
Oracle may not check dialog identity, ordering, colouring, or that the list matches the share's count.
```

None.

None.

None.

#### E14

##### Promises

P14

```text
Days before the first finished item or after the last successful sync draw no point; a range with nothing recorded shows the same sentence as Percentiles Over Time.
```

```text
Reuse of the exact sentence and edge-day behaviour are not visible without a rendered chart.
```

None.

None.

None.

#### E15

##### Promises

P15

```text
Filled-in days draw as open markers, recorded days as filled markers, and a note under the chart says filled-in days are measured against today's SLE.
```

```text
Marker style and note placement are visual facts a data oracle cannot see.
```

None.

None.

None.

### Waivers

None.

## Charter

None.

## Examination

None.
