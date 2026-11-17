# Acceptance brief: story\-6655\-outlier\-toggle\-and\-unpl

## Slices

### S1

##### Covers

P1

P3

```text
A reader opens the Cycle Time scatterplot with closed Work Items of 2, 3, 3, 4 and 60 days, switches Hide outliers off, then on again
```

```text
The switch is on at first, the 60-day item is not drawn and the chip says 1 outlier; switched off all 5 items are drawn, the axis fits 60 days or more, no chip is shown and the switch-off is reported once by name only; switched on again the chip and the shorter axis return and nothing more is reported
```

```text
The reader can see every Work Item on demand and bring the outlier handling back
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outlierSwitch\.test\.tsx

```text
A switch that only hides the chip but still leaves the 60-day item undrawn or the axis short would pass a chip-only check; the test also reads the drawn items and axis top
```

###### Obligations




##### Covers

P2

```text
A reader switches Hide outliers off, leaves the chart and opens it again
```

```text
The switch is on again, the 60-day item is not drawn and the chip says 1 outlier
```

```text
Outlier handling starts on every time the chart opens
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outlierSwitch\.test\.tsx

```text
A switch kept in a module variable or storage that survives a remount, or one that starts off, would pass a test that only checks the first render
```

###### Obligations


### S2

##### Covers

P1

P4

```text
A Team whose baseline gives an upper natural process limit of exactly 30 days asks for the outliers of a range with closed Work Items of 12, 30, 31 and 90 days, and for its Cycle Time process behaviour chart
```

```text
The outliers are exactly the 31 and 90 day Work Items, and the process behaviour chart shows a limit of 30 with the same two Work Items above it
```

```text
The reader sees the same unusual Work Items on the scatterplot and on the process behaviour chart
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersFollowTheProcessBehaviourChartTest\.cs\:\:The\_Work\_Items\_above\_a\_limit\_of\_30\_days\_are\_the\_outliers\_and\_the\_process\_behaviour\_chart\_marks\_the\_same\_ones\_above\_its\_limit

```text
a rule that sets apart items at or above the limit plus one day, which agrees with days > limit for whole-day limits
```

###### Obligations



##### Covers

P1

```text
A Team whose baseline gives a limit of 30.6 days asks for the outliers of a range with Work Items of 20, 30 and 31 days
```

```text
The 31-day Work Item is an outlier although the process behaviour chart shows its limit rounded to 31
```

```text
A Work Item just above the limit is set apart even where the chart's rounded limit hides the difference
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersFollowTheProcessBehaviourChartTest\.cs\:\:A\_31\_day\_Work\_Item\_is\_an\_outlier\_when\_the\_limit\_is\_30\_point\_6\_days\_even\_though\_the\_chart\_shows\_the\_limit\_as\_31

```text
a comparison with the limit rounded down to whole days
```

###### Obligations



##### Covers

P1

```text
A Team whose baseline Work Items all took 20 days asks for the outliers of Work Items of 19, 20 and 21 days
```

```text
Only the 21-day Work Item is an outlier
```

```text
With a steady baseline only longer Work Items are set apart
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersFollowTheProcessBehaviourChartTest\.cs\:\:When\_every\_baseline\_Work\_Item\_took\_the\_same\_time\_only\_the\_Work\_Items\_longer\_than\_that\_are\_outliers

```text
a rule using the limit plus a fixed margin below one day
```

###### Obligations



##### Covers

P1

```text
A Team with a 30-day upper and an 11-day lower limit asks for the outliers of Work Items of 3, 12 and 31 days
```

```text
Only the 31-day Work Item is an outlier, while the process behaviour chart marks the 3-day Work Item as a large change
```

```text
Short Work Items flagged by the chart are never set apart as outliers
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersFollowTheProcessBehaviourChartTest\.cs\:\:A\_Work\_Item\_far\_below\_the\_lower\_limit\_is\_not\_an\_outlier\_although\_the\_chart\_marks\_it\_as\_a\_large\_change

```text
a rule that sets apart large-change points only when they lie above the average
```

###### Obligations



##### Covers

P1

```text
The same Team asks for the outliers of a named Cycle Time from Implementation to Done, whose shown Work Items took 12, 30, 31 and 90 named days after 60 days in the Backlog
```

```text
The outliers are the 31 and 90 named-day Work Items, not every shown Work Item as their default Cycle Times would give
```

```text
A reader who picks a named Cycle Time sees the outliers of that Cycle Time
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersFollowTheProcessBehaviourChartTest\.cs\:\:A\_named\_Cycle\_Time\_sets\_apart\_the\_Work\_Items\_whose\_named\_duration\_is\_above\_the\_limit\_of\_the\_named\_durations

```text
a named rule whose baseline uses default Cycle Times, which equal the named ones in this baseline
```

###### Obligations



##### Covers

P1

```text
Any list of up to 50 plotted Work Items with Cycle Times of 0 to 400 days, and a verdict naming some of them and some Work Items not plotted
```

```text
Outliers are exactly the plotted Work Items the verdict names, longest first; every other plotted Work Item is drawn in its order; none is dropped or doubled; a verdict id not plotted is ignored
```

```text
The scatterplot follows the backend verdict and never loses a Work Item
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/utils\/charts\/outliers\.test\.ts\:\:splits the plotted items into the outliers the verdict names\, longest first\, and every other item in its order \(L3\)

```text
a partition that sorts outliers with equal Cycle Times in an unstable order, which the law allows
```

###### Obligations


##### Covers

P1

```text
Any plot and verdict as above, with a limit of 0 to 400 days
```

```text
The axis top is never below a drawn Work Item and, with outliers set apart, is at most the limit unless a drawn Work Item is longer
```

```text
Every drawn dot stays on the chart and the axis stops at the limit
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/utils\/charts\/outliers\.test\.ts\:\:never cuts the axis below a drawn item and\, with outliers set apart\, stops it at the limit unless a drawn item is longer \(L3\)

```text
an axis that keeps headroom above the tallest drawn item when that item is above the limit
```

###### Obligations


##### Covers

P4

```text
The outlier help and the outlier list title are read with renamed words Ticket and Lead Time and limits of 30.6, 30 and 30.64 days
```

```text
The help says a closed Ticket whose Lead Time is above the upper natural process limit of the Lead Time process behaviour chart for this date range; the title says Tickets with a Lead Time above the upper natural process limit (30.6 days), (30 days) and (30.6 days)
```

```text
The reader learns the limit by its name and value, in their own words, not as 5 times the median
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/utils\/charts\/outliers\.test\.ts\:\:titles the list with a limit of \%s days\, shown with at most one decimal

```text
a title that rounds to one decimal half-up instead of to the nearest, which agrees on these limits
```

###### Obligations


##### Covers

P1

P4

```text
The scatterplot of closed Work Items of 12, 30, 31 and 90 days gets a verdict naming the 31 and 90 day Work Items with a limit of 30 days
```

```text
12 and 30 are drawn, the axis stops at 30, the chip says 2 outliers and is enabled, the verdict was asked for the default Cycle Time, and opening the list reports its usage event
```

```text
The reader sees the Work Items above the limit set apart in the chip
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outliers\.test\.tsx\:\:sets apart the 31 and 90 day Work Items the backend judges above a limit of 30 days and stops the axis at the limit

```text
a chart that fetches the verdict once and keeps it after the Cycle Time selection or the date range changes
```

###### Obligations

None.

##### Covers

P1

```text
The scatterplot's verdict is still on its way, and in another case cannot be loaded
```

```text
While on its way every Work Item is drawn, the chip reads Outliers, loading, disabled, no count shows and the frame reports loading; when it fails the frame shows its could-not-load message and no count
```

```text
The reader never takes a partial or stale chart for the answer
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outliers\.test\.tsx\:\:sets nothing apart and shows no count while the verdict is on its way

```text
a chart that shows an answer from an older key while a new verdict loads
```

###### Obligations

None.

##### Covers

P1

```text
The scatterplot's verdict request fails
```

```text
The frame reports an error and shows the could-not-load message, with no count
```

```text
A failed load never looks like no outliers
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outliers\.test\.tsx\:\:shows the could\-not\-load message when the verdict cannot be loaded

```text
a chart that retries silently before reporting the failure
```

###### Obligations

None.

##### Covers

P4

```text
The outlier help is read with renamed words Ticket and Lead Time
```

```text
An outlier is a closed Ticket whose Lead Time is above the upper natural process limit of the Lead Time process behaviour chart for this date range.
```

```text
The help names the upper natural process limit, not 5 times the median
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/utils\/charts\/outliers\.test\.ts\:\:states the rule in the configured words

```text
none: the sentence is pinned in full
```

###### Obligations


##### Covers

P1

```text
The edge cases of the partition and axis: nothing plotted, a verdict id not plotted, and a drawn Work Item of 45 days above a 30-day limit
```

```text
Nothing is set apart for an empty plot; the unknown id is ignored and 90 and 31 are set apart; the axis stays at or above 45
```

```text
The chart stays whole in its edge cases
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/utils\/charts\/outliers\.test\.ts\:\:keeps the axis above a drawn item that is longer than the limit

```text
an axis capped at the tallest drawn item without headroom
```

###### Obligations





##### Covers

P1

```text
The scatterplot's verdict names the 90-day Work Item above a 30-day limit while the 95% line is at 33 days and the SLE at 35
```

```text
The two lines above the axis are named in markers and in the chip: 1 outlier not drawn on the chart. Above the axis: 95% at 33 days, SLE at 35 days. Show the list.
```

```text
Lines above the limit are named, not lost
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outliers\.test\.tsx\:\:names the percentile and SLE lines that lie above a limit of 30 days

```text
a chip that names only the lines whose value is visible on the axis
```

###### Obligations

None.

##### Covers

P1

P4

```text
A Portfolio whose baseline gives an upper natural process limit of exactly 30 days asks for the outliers of a range with closed Features of 12, 30, 31 and 90 days, and for its Cycle Time process behaviour chart
```

```text
The outliers are exactly the 31 and 90 day Features, and the Portfolio's process behaviour chart shows a limit of 30 with the same two Features above it
```

```text
On a Portfolio's Metrics tab the scatterplot and the process behaviour chart agree on the unusual Features
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersFollowTheProcessBehaviourChartTest\.cs\:\:The\_Features\_of\_a\_Portfolio\_above\_a\_limit\_of\_30\_days\_are\_the\_outliers\_and\_its\_process\_behaviour\_chart\_marks\_the\_same\_ones

```text
a Portfolio rule that compares with the rounded chart limit, which agrees at a whole-day limit of 30
```

###### Obligations


##### Covers

P1

```text
The scatterplot asks a Team with a 30-day limit and closed Work Items of 12, 30, 31 and 90 days for its outliers over the web
```

```text
The answer is 200 with status as the word Ready, upperNaturalProcessLimit 30 as a number and outlierWorkItemIds naming the 31 and 90 day Work Items
```

```text
The chart can read the verdict the backend works out
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/CycleTimeOutliersWireContractTest\.cs\:\:A\_Team\_answers\_its\_outliers\_with\_a\_status\_word\_the\_limit\_and\_the\_outlier\_Work\_Item\_ids

```text
an answer that adds fields the frontend does not read
```

###### Obligations

None.

##### Covers

P1

```text
A reader opens the scatterplot fed by a verdict setting apart the 60-day Work Item, switches outlier handling off and on again, and opens the chart anew
```

```text
Hide outliers starts on; off draws every Work Item, removes the chip and reports OutlierHandlingSwitchedOff once; on restores the partition and chip; a new chart starts on again
```

```text
The reader can see every Work Item and finds outlier handling on at every load
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outlierSwitch\.test\.tsx\:\:starts on\, draws every item and drops the chip when switched off\, and restores both when switched on again

```text
a switch that is remembered within one page session but not across mounts, which the remount test rejects only for a fresh mount
```

###### Obligations

None.

##### Covers

P4

```text
The reader clicks the outlier chip of a scatterplot whose verdict sets apart TZ-301 above a 30-day limit
```

```text
The Work Items list opens straight away titled Work Items with a Cycle Time above the upper natural process limit (30 days), listing TZ-301, and the opening is reported once
```

```text
The list names the limit the outliers are above
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outlierChip\.test\.tsx\:\:opens the Work Items list straight from the chip with no step in between and reports the opening once

```text
a title that shows the limit rounded to whole days, which agrees at 30
```

###### Obligations

None.

##### Covers

P4

```text
The reader with Tasks and Lead Time as their words opens the outlier list, and reads the scatterplot help on a Team and a Portfolio
```

```text
The title is Tasks with a Lead Time above the upper natural process limit (30 days); the help says an outlier is a closed Task whose Lead Time is above the upper natural process limit of the Lead Time process behaviour chart for this date range
```

```text
The outlier words follow the reader's terminology
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outlierList\.test\.tsx\:\:titles the list in the reader\'s own words for Work Items and Cycle Time

```text
help text that keeps one hard-coded word in a part the pattern does not pin
```

###### Obligations

None.

##### Covers

P1

```text
The reader changes the Cycle Time and the date range of a scatterplot, with a late answer for a Cycle Time already left
```

```text
The chip and the drawn Work Items follow the verdict of the selected Cycle Time and of each date range, with its own limit; a late answer never lands; a loading range shows no count
```

```text
The outliers always belong to what the reader currently looks at
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outlierViews\.test\.tsx\:\:follows the verdict of each date range for the chip and the axis

```text
a chart that keeps the verdict of the previous range until the new one lands, while the frame is ready
```

###### Obligations

None.

##### Covers

P1

```text
The scatterplot asks a Portfolio with a 30-day limit and closed Features of 12, 30, 31 and 90 days for its outliers over the web
```

```text
The answer is 200 with status Ready as a word, upperNaturalProcessLimit 30 as a number and outlierWorkItemIds naming the 31 and 90 day Features
```

```text
A Portfolio's scatterplot can read the verdict the backend works out
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/CycleTimeOutliersWireContractTest\.cs\:\:A\_Portfolio\_answers\_its\_outliers\_with\_a\_status\_word\_the\_limit\_and\_the\_outlier\_Feature\_ids

```text
an answer that adds fields the frontend does not read
```

###### Obligations

None.

##### Covers

P1

```text
The axis is worked out with a limit of 5 days but no outlier set apart, and with outliers but no known limit
```

```text
Without outliers the limit is ignored and the axis fits 100 and 50 at 110; with no limit the axis fits 10 and 20 at 22
```

```text
The axis never cuts drawn Work Items or lines when there is nothing above it
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/utils\/charts\/outliers\.mutation\.test\.ts\:\:fits every drawn item and line when outliers exist but no limit is known

```text
an axis with a different headroom rule that rounds to the same values
```

###### Obligations


##### Covers

P4

```text
The outlier list title is built with Stories and Lead Time as the words and a limit of 30.6 days
```

```text
Stories with a Lead Time above the upper natural process limit (30.6 days)
```

```text
The list title names the limit in the reader's words
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/utils\/charts\/outlierWording\.mutation\.test\.ts\:\:puts the configured terms into the list title and the empty\-plot copy

```text
a title that rounds 30.65 up where the product rounds it to nearest even
```

###### Obligations

None.

### S3

##### Covers

P5

P1

```text
A Team with no configured baseline is asked for the outliers of a 10-day range holding Work Items of 5, 5, 5 and 90 days
```

```text
No limits: no Work Item is set apart, the 90-day one included
```

```text
The reader is never shown outliers from a limit that does not exist, and no 5 x median rule sets the 90-day Work Item apart
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersWithoutProcessLimitsTest\.cs\:\:Lighthouse\.Backend\.Tests\.Acceptance\.CycleTimeOutliers\.CycleTimeOutliersWithoutProcessLimitsTest\.A\_10\_day\_range\_without\_a\_configured\_baseline\_has\_no\_limits\_so\_no\_Work\_Item\_is\_set\_apart

```text
A 13-day range judged as valid; only 10 and 14 days are pinned, so a minimum moved between 11 and 14 days passes
```

###### Obligations



##### Covers

P5

```text
A Team whose configured baseline is 10 days long, holding 8 closed Work Items with a limit of 30 days, is asked for the outliers of a 28-day range holding Work Items of 12, 31 and 90 days
```

```text
No limits: no Work Item is set apart, although the baseline holds enough Work Items
```

```text
An invalid configured baseline never gives outliers, however many Work Items it holds
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersWithoutProcessLimitsTest\.cs\:\:Lighthouse\.Backend\.Tests\.Acceptance\.CycleTimeOutliers\.CycleTimeOutliersWithoutProcessLimitsTest\.A\_configured\_baseline\_of\_10\_days\_has\_no\_limits\_even\_with\_enough\_closed\_Work\_Items\_in\_it

```text
A rule that validates the configured baseline but then judges against the selected range instead of the baseline dates
```

###### Obligations



##### Covers

P5

```text
A Team with no configured baseline is asked for the outliers of a 28-day range holding a single 90-day Work Item
```

```text
No limits: the single Work Item is not set apart
```

```text
One Work Item is never treated as a limit; the reader sees it drawn
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersWithoutProcessLimitsTest\.cs\:\:Lighthouse\.Backend\.Tests\.Acceptance\.CycleTimeOutliers\.CycleTimeOutliersWithoutProcessLimitsTest\.A\_30\_day\_range\_with\_a\_single\_closed\_Work\_Item\_has\_no\_limits

```text
A rule that counts 0-day Work Items towards the 2 needed; no 0-day Work Item is in the range
```

###### Obligations



##### Covers

P5

```text
A Team whose configured 24-day baseline holds no closed Work Item is asked for the outliers of a 28-day range holding Work Items of 5, 5, 5 and 90 days
```

```text
No limits: no Work Item of the range is set apart
```

```text
An empty baseline never gives outliers from the range's own Work Items
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersWithoutProcessLimitsTest\.cs\:\:Lighthouse\.Backend\.Tests\.Acceptance\.CycleTimeOutliers\.CycleTimeOutliersWithoutProcessLimitsTest\.A\_configured\_baseline\_without\_any\_closed\_Work\_Item\_has\_no\_limits\_so\_no\_Work\_Item\_of\_the\_range\_is\_set\_apart

```text
A rule that falls back to the selected range only when the baseline holds exactly 1 Work Item
```

###### Obligations



##### Covers

P5

P1

```text
A Team with no configured baseline is asked for the outliers of a 14-day range holding Work Items of 10 and 12 days
```

```text
Limits: the verdict has limits (neither Work Item is above them)
```

```text
A valid 14-day baseline with 2 Work Items gives the reader limits, as the process behaviour chart does
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersWithoutProcessLimitsTest\.cs\:\:Lighthouse\.Backend\.Tests\.Acceptance\.CycleTimeOutliers\.CycleTimeOutliersWithoutProcessLimitsTest\.A\_14\_day\_range\_with\_2\_closed\_Work\_Items\_has\_limits

```text
Limits with a wrong value; the limit and the ids are pinned by the rows that follow the process behaviour chart, not here
```

###### Obligations



##### Covers

P5

```text
A Team with a valid configured baseline whose limit is 30 days is asked for the outliers of a 28-day range of Work Items of 12, 31 and 90 days, for a named Cycle Time it does not have
```

```text
No limits: no Work Item is set apart, although the default Cycle Time would set apart the 31 and 90 day ones
```

```text
An unknown named Cycle Time never shows outliers judged on another Cycle Time
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersWithoutProcessLimitsTest\.cs\:\:Lighthouse\.Backend\.Tests\.Acceptance\.CycleTimeOutliers\.CycleTimeOutliersWithoutProcessLimitsTest\.An\_unknown\_named\_Cycle\_Time\_has\_no\_limits\_even\_when\_the\_default\_Cycle\_Time\_has\_them

```text
A named Cycle Time that exists but is invalid judged as valid; only the unknown id is pinned
```

###### Obligations


##### Covers

P5

```text
A Portfolio whose configured baseline is 10 days long, holding 8 closed Features with a limit of 30 days, is asked for the outliers of a 28-day range holding Features of 12, 31 and 90 days
```

```text
No limits: no Feature is set apart
```

```text
A Portfolio reader is never shown outliers from a limit that does not exist
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersWithoutProcessLimitsTest\.cs\:\:Lighthouse\.Backend\.Tests\.Acceptance\.CycleTimeOutliers\.CycleTimeOutliersWithoutProcessLimitsTest\.A\_Portfolio\_whose\_configured\_baseline\_is\_10\_days\_has\_no\_limits\_so\_no\_Feature\_is\_set\_apart

```text
A Portfolio with fewer than 2 baseline Features judged as limited; only the invalid baseline is pinned for a Portfolio
```

###### Obligations



##### Covers

P5

P1

```text
The Cycle Time scatterplot of Work Items of 2, 4, 6, 6, 8 and 58 days gets an answer that the process behaviour chart has no limits
```

```text
All six Work Items are drawn, the axis reaches 58 days, no outlier chip or count shows, the chip place reads 'No outliers: the Cycle Time process behaviour chart has no limits for this date range', and the frame is ready
```

```text
The reader sees every Work Item, no outlier count, and that outliers need the process behaviour chart's limits
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outlierChip\.test\.tsx\:\:Without process behaviour chart limits no Work Item is set apart \> draws every Work Item\, shows no outlier count and says the process behaviour chart has no limits for this date range

```text
The no-limits text with a hard-coded 'Cycle Time' instead of the configured term; the test renders the default term only, and the info popover's no-limits sentence is not pinned
```

###### Obligations


## Expectations

### Rows

#### E1

##### Promises

P1

```text
A reader can switch outlier handling off and then sees every closed Work Item drawn on the Cycle Time scatterplot.
```

#### E2

##### Promises

P1

P4

```text
With outlier handling on, the Work Items set apart as outliers are exactly those the Cycle Time process behaviour chart of the same Team and date range shows above its UNPL; none above it is drawn, none at or below it is set apart.
```

#### E3

##### Promises

P2

```text
Every time the chart opens, or the Metrics tab is reloaded after switching outlier handling off, outlier handling is on: Work Items above the UNPL are not drawn and the outlier chip counts them.
```

#### E4

##### Promises

P3

```text
Switching outlier handling off draws all closed Work Items including the former outliers, raises the y-axis to fit the tallest, and removes the outlier chip; switching it on again restores the chip and the shorter axis.
```

#### E5

##### Promises

P4

```text
With an UNPL of 30 days and Cycle Times of 12, 30, 31 and 90 days, the 31 and 90 day items are outliers and the 12 and 30 day items are drawn; the outlier list and help call the limit the upper natural process limit, never 5 times the median.
```

#### E6

##### Promises

P5

```text
When the process behaviour chart has no limits (invalid baseline such as under 14 days, or fewer than 2 closed Work Items), every closed Work Item is drawn, no outlier count shows, and the reader can see that outliers need the process behaviour chart's limits.
```

## Charter

None.

## Examination

None.
