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
