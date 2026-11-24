# Acceptance brief: bug\-6656\-named\-cycle\-time\-own\-population

## Slices

### S1

##### Covers

P1

```text
A Team and a Portfolio with the workflow Analysis, In Progress, Ready for Release, In Production, Done and the named Cycle Time Analysis to Ready for Release. A reached Ready for Release on 5 Nov and was Done on 20 Nov; B reached it on 12 Nov and is still there; C reached it on 28 Oct and was Done on 3 Nov; D is In Progress. The named Cycle Time data is asked for 1 to 30 Nov.
```

```text
Exactly A and B are shown. A has 10 days and the end date 5 Nov; B has 6 days and the end date 12 Nov. C and D are not shown.
```

```text
The scatterplot of a named Cycle Time can show every Work Item that reached its end state in the dates, closed or not, on the day it reached it.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/NamedCycleTime\/NamedCycleTimePopulationTest\.cs\:\:The\_named\_Cycle\_Time\_shows\_every\_item\_that\_reached\_the\_end\_state\_in\_the\_dates\_closed\_or\_not

```text
The end date carries the day of the first move into the end state rather than the last, for an item that left the end state and came back; no item in this fixture moves back.
```

###### Obligations

None.

##### Covers

P1

```text
The same Team and Portfolio and items A, B, C, D. The named Cycle Time percentiles are asked for 1 to 30 Nov.
```

```text
P50, P70, P85 and P95 are all 6 days, from A (10 days) and B (6 days) only; 10 would mean B is left out or C is counted.
```

```text
The percentile lines of a named Cycle Time match the items the scatterplot shows for it.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/NamedCycleTime\/NamedCycleTimePopulationTest\.cs\:\:The\_named\_Cycle\_Time\_percentiles\_count\_only\_the\_items\_that\_reached\_the\_end\_state\_in\_the\_dates

```text
An item that reached the end state on the last day of the dates is left out by an exclusive end bound; no fixture item sits on a range edge.
```

###### Obligations

None.

##### Covers

P1

```text
The same Team and Portfolio and items A, B, C, D. The Cycle Time percentiles widget is asked for 1 to 30 Nov with the named Cycle Time.
```

```text
The widget's current P50, P70, P85 and P95 are all 6 days, from A and B only.
```

```text
The Cycle Time percentiles widget of a named Cycle Time agrees with its scatterplot.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/NamedCycleTime\/NamedCycleTimePopulationTest\.cs\:\:The\_named\_Cycle\_Time\_percentiles\_widget\_counts\_only\_the\_items\_that\_reached\_the\_end\_state\_in\_the\_dates

```text
The widget's comparison with the previous period still uses the items closed in that period; only the current percentiles are checked.
```

###### Obligations

None.

##### Covers

P1

```text
A Team and a Portfolio with the same named Cycle Time. Eight items reached Ready for Release in November after 3 days each and were Done; B has waited in Ready for Release since 12 Nov after 43 days; C took 200 days, reached Ready for Release on 28 Oct and was Done on 3 Nov. The named Cycle Time outliers are asked for 1 to 30 Nov.
```

```text
The named Cycle Time has a limit and B is the only outlier. C is neither an outlier nor part of the limit; with C in the limit B would not stand out.
```

```text
The outliers marked on a named Cycle Time scatterplot are the dots it shows that stand out, including items not yet closed.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/NamedCycleTime\/NamedCycleTimePopulationTest\.cs\:\:The\_named\_Cycle\_Time\_outliers\_are\_judged\_among\_the\_items\_that\_reached\_the\_end\_state\_in\_the\_dates

```text
A configured baseline period keeps using the items closed in it; this fixture uses the shown dates as the baseline.
```

###### Obligations

None.

##### Covers

P1

```text
The same Team and Portfolio and items A, B, C, D. The Cycle Time data is asked for 1 to 30 Nov without a named Cycle Time.
```

```text
Exactly A and C are shown, the items closed in the dates, as before.
```

```text
The default Cycle Time scatterplot and the widgets that read the same list do not change.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/NamedCycleTime\/NamedCycleTimePopulationTest\.cs\:\:The\_default\_Cycle\_Time\_still\_shows\_the\_items\_closed\_in\_the\_dates

```text
The default answer drops or changes the named values each closed item carried; only which items are shown is checked.
```

###### Obligations

None.

##### Covers

P1

```text
A team user opens the scatterplot for a named Cycle Time (definition 7) and the service answers one item that reached Ready for Release on 5 Nov
```

```text
The team's cycle time data asks the service for definition 7 and returns that item with its named value of 5 days ending 5 Nov
```

```text
The team scatterplot for a named Cycle Time shows that measure's own items, each placed at the day it reached the end state
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamMetricsControllerTest\.cs\:\:GetCycleTimeDataForTeam\_NamedDefinition\_ReturnsTheDefinitionsItemsWithTheirEndDates

```text
the controller forwarded any positive id other than the one asked for while the service answered the same items for it
```

###### Obligations

None.

##### Covers

P1

```text
A team user opens the scatterplot without a named Cycle Time, or with definition 0
```

```text
The team's cycle time data asks the service for the default Cycle Time (no definition) and returns its closed items unchanged
```

```text
The default team scatterplot still shows the items closed in the range
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamMetricsControllerTest\.cs\:\:GetCycleTimeDataForTeam\_NoNamedDefinition\_AsksForTheDefaultCycleTime

```text
a negative definition id were forwarded to the service instead of being treated as no definition
```

###### Obligations

None.

##### Covers

P1

```text
A portfolio user opens the scatterplot for a named Cycle Time (definition 7) and the service answers one Feature that reached Ready for Release on 12 Nov
```

```text
The portfolio's cycle time data asks the service for definition 7 and returns that Feature with its named value of 5 days ending 12 Nov
```

```text
The portfolio scatterplot for a named Cycle Time shows that measure's own Features, each placed at the day it reached the end state
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfolioMetricsControllerTests\.cs\:\:GetCycleTimeData\_NamedDefinition\_ReturnsTheDefinitionsFeaturesWithTheirEndDates

```text
the controller forwarded any positive id other than the one asked for while the service answered the same Features for it
```

###### Obligations

None.

##### Covers

P1

```text
A portfolio user opens the scatterplot without a named Cycle Time, or with definition 0
```

```text
The portfolio's cycle time data asks the service for the default Cycle Time (no definition) and returns its closed Features unchanged
```

```text
The default portfolio scatterplot still shows the Features closed in the range
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfolioMetricsControllerTests\.cs\:\:GetCycleTimeData\_NoNamedDefinition\_AsksForTheDefaultCycleTime

```text
a negative definition id were forwarded to the service instead of being treated as no definition
```

###### Obligations

None.

### S2

##### Covers

P2

```text
Show the Cycle Time scatterplot with the default Cycle Time; Work Item A closed 20 Nov and C closed 3 Nov; open the dot of A.
```

```text
Dots sit on the closed dates (3 Nov, 20 Nov) at the default days; the x-axis says Date; nothing is fetched for a named measure; the list title reads Work Items closed on <20 Nov> with Cycle Time of 15 days.
```

```text
A reader who keeps the default Cycle Time sees the same chart as before the fix.
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.namedMeasure\.test\.tsx \> keeps the default Cycle Time on the closed dates\, without asking for a named measure\'s items

```text
the default view fetches named data lazily after the first paint
```

###### Obligations

None.

##### Covers

P2

```text
Select the named measure Analysis to Ready for Release, whose fetch returns A (reached it 5 Nov, 6 days) and B (not closed, reached it 12 Nov, 4 days); C reached it 28 Oct; open the dot on 5 Nov.
```

```text
The fetch is asked for that measure; dots for A on 5 Nov and B on 12 Nov at the named days, none for C; the x-axis says Date reached Ready for Release; the list title reads Work Items that reached Ready for Release on <5 Nov> with Cycle Time of 6 days and lists A.
```

```text
A reader sees every Work Item that reached the measure's end state in the range, closed or not, on the day it got there.
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.namedMeasure\.test\.tsx \> shows the measure\'s own Work Items on the day each reached its end state

```text
a dot groups items by end date but the tooltip still says closed
```

###### Obligations

None.

##### Covers

P2

```text
Select the named measure while its Work Items are still on their way.
```

```text
The frame is loading; the chart body is dimmed to 0.4 under a spinner; the previous default dots stay visible beneath.
```

```text
A reader sees the old dots are not the current answer while the new one loads.
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.namedMeasure\.test\.tsx \> dims the previous dots under a spinner while the measure\'s Work Items are on their way

```text
the x-axis label already switches to the named end state while the old dots are dimmed
```

###### Obligations

None.

##### Covers

P2

```text
Select the named measure and the fetch of its Work Items fails.
```

```text
The frame shows its could-not-load message and no dot is visible as the answer.
```

```text
A reader is told the chart could not be loaded instead of seeing the previous dots as if current.
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.namedMeasure\.test\.tsx \> says the chart could not be loaded\, and shows no dots as current\, when the measure\'s Work Items cannot be fetched

```text
the chart retries the failed fetch silently in the background
```

###### Obligations

None.

##### Covers

P2

```text
Select Analysis to Ready for Release (slow answer), then Analysis to In Production (answer: D reached In Production 18 Nov, 12 days); then the slow answer arrives.
```

```text
Only D's dot on 18 Nov stays; the x-axis says Date reached In Production.
```

```text
A reader who switches measures never sees an older measure's answer replace the one they chose.
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.namedMeasure\.test\.tsx \> never lets a late answer for an earlier measure replace the selected one\'s

```text
the late answer is shown briefly while switching back to the earlier measure
```

###### Obligations

None.

##### Covers

P2

```text
With the named measure Analysis to Ready for Release selected on the Cycle Time scatterplot, open the dot of B, which reached Ready for Release 4 days after entering Analysis and is not closed
```

```text
The list that opens heads its Cycle Time column with the measure's name and shows 4 days for B
```

```text
The reader sees the Cycle Time the dot stands for in the list, also for a Work Item that is not closed
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.namedMeasure\.test\.tsx\:\:the list a named Cycle Time\'s dot opens \> shows each listed Work Item\'s Cycle Time for the selected measure\, also for one that is not closed

```text
the Closed column of a not-closed Work Item stays empty beside the named days; no promise covers that column
```

###### Obligations

None.

##### Covers

P2

```text
Select the named measure Analysis to Ready for Release for the Cycle Time percentiles, then go back to Default
```

```text
The measure's own Work Items for the same team and range are fetched with the measure's id, A and B (not closed) are offered as its list, and none are offered on Default
```

```text
The percentiles widget knows which Work Items its named numbers count
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/namedPercentilesViewData\.test\.tsx\:\:View Data of the Cycle Time percentiles with a named Cycle Time \> fetches the named measure\'s own Work Items for the range when the measure is selected\, and drops them on Default

```text
a late answer for an earlier measure, or a failed fetch, leaves a stale list in place; no case covers either
```

###### Obligations

None.

##### Covers

P2

```text
Open View Data of the Cycle Time percentiles with the named measure Analysis to Ready for Release selected, the measure's own Work Items being A (closed) and B (not closed), and the default closed list A and C; then open it on Default
```

```text
The named list holds A and B, not C, carries the measure as its Cycle Time scope and is titled Work Items that reached Ready for Release; on Default it holds A and C and keeps the title Closed Work Items
```

```text
View Data lists the same Work Items the named percentiles count, including one that has reached the end state but is not closed, under a title that does not call them closed
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/namedPercentilesViewData\.test\.tsx\:\:View Data of the Cycle Time percentiles with a named Cycle Time \> lists and titles the named measure\'s own Work Items\, also one that is not closed\, and none that only the default closed list holds

```text
the title takes the end state but ignores a renamed Work Items term; the test uses the seeded default terms only
```

###### Obligations

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
Selecting a named Cycle Time shows figures computed only from the items whose crossing into that measure's end state falls inside the chosen date range, whether or not those items are closed, and each such item carries its own end date.
```

#### E2

##### Promises

P1

```text
An item that is closed but never crossed the named measure's end state in the range does not appear in that named Cycle Time, and an open item that did cross it in the range does appear.
```

#### E3

##### Promises

P1

```text
With the default Cycle Time selected, the figures and items shown are the same as before this change.
```

#### E4

##### Promises

P2

```text
With a named Cycle Time selected, the scatterplot plots that measure's own items, each placed on the date it reached the end state.
```

#### E5

##### Promises

P2

```text
While the named Cycle Time scatterplot data is loading, the chart shows a visible loading state and not stale or empty-looking data.
```

#### E6

##### Promises

P2

```text
If the named Cycle Time scatterplot data cannot be loaded, the chart shows a visible failure message instead of an empty chart or the previous measure's points.
```

## Charter

None.

## Examination

### E1

met

#### Observations

```text
Probe backend-tests NamedCycleTime: 84/84 passed incl. 'The_named_Cycle_Time_shows_every_item_that_reached_the_end_state_in_the_dates_closed_or_not' (Team and Portfolio), NamedCycleTimeDays end-state-entry tests, Info_DefinitionIdAbsent_ReturnsTheDefaultTrend_Unchanged. Limit: only tail of output seen; A/B/C/D scenario values and default cycleTimeData unchanged not directly observed; no live API call possible (metrics-answers-compare probe refused for missing revision arg).
```

### E2

met

#### Observations

```text
Probe backend-tests NamedCycleTime: 84/84 passed incl. 'The_named_Cycle_Time_shows_every_item_that_reached_the_end_state_in_the_dates_closed_or_not' (Team and Portfolio), NamedCycleTimeDays end-state-entry tests, Info_DefinitionIdAbsent_ReturnsTheDefaultTrend_Unchanged. Limit: only tail of output seen; A/B/C/D scenario values and default cycleTimeData unchanged not directly observed; no live API call possible (metrics-answers-compare probe refused for missing revision arg).
```

### E3

met

#### Observations

```text
Probe backend-tests NamedCycleTime: 84/84 passed incl. 'The_named_Cycle_Time_shows_every_item_that_reached_the_end_state_in_the_dates_closed_or_not' (Team and Portfolio), NamedCycleTimeDays end-state-entry tests, Info_DefinitionIdAbsent_ReturnsTheDefaultTrend_Unchanged. Limit: only tail of output seen; A/B/C/D scenario values and default cycleTimeData unchanged not directly observed; no live API call possible (metrics-answers-compare probe refused for missing revision arg).
```

```text
Probe frontend-tests CycleTimeScatterPlot: 51/51 passed incl. namedMeasure rows: default stays on closed dates without asking named items; named shows own items on day each reached end state; dims previous dots under spinner while pending; could-not-load message and no dots as current on failure; late answer for earlier measure ignored; dot list shows Cycle Time also for non-closed item. Limit: test runner output, no live UI; x-axis label and dialog title text not separately seen.
```

### E4

met

#### Observations

```text
Probe frontend-tests CycleTimeScatterPlot: 51/51 passed incl. namedMeasure rows: default stays on closed dates without asking named items; named shows own items on day each reached end state; dims previous dots under spinner while pending; could-not-load message and no dots as current on failure; late answer for earlier measure ignored; dot list shows Cycle Time also for non-closed item. Limit: test runner output, no live UI; x-axis label and dialog title text not separately seen.
```

### E5

met

#### Observations

```text
Probe frontend-tests CycleTimeScatterPlot: 51/51 passed incl. namedMeasure rows: default stays on closed dates without asking named items; named shows own items on day each reached end state; dims previous dots under spinner while pending; could-not-load message and no dots as current on failure; late answer for earlier measure ignored; dot list shows Cycle Time also for non-closed item. Limit: test runner output, no live UI; x-axis label and dialog title text not separately seen.
```

### E6

met

#### Observations

```text
Probe frontend-tests CycleTimeScatterPlot: 51/51 passed incl. namedMeasure rows: default stays on closed dates without asking named items; named shows own items on day each reached end state; dims previous dots under spinner while pending; could-not-load message and no dots as current on failure; late answer for earlier measure ignored; dot list shows Cycle Time also for non-closed item. Limit: test runner output, no live UI; x-axis label and dialog title text not separately seen.
```
