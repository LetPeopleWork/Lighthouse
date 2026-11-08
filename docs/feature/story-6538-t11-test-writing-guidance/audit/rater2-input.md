# Second rater: label the tests of ten files, blind

## The labelling rule (the only guidance you get)

One word per test, by one rule: would the same assertion hold on the return value of a function that needs no DOM?

- **Logic** (L): arithmetic, formatting, ordering, filtering, validation, a rule's branches, a state change, building a link.
- **UI** (U): needs the DOM. Role or name, focus, click wiring, layout, copy in place, a MUI control's behaviour, a view's loading or failure look, routing.
- **Mixed** (M): one render checks both. Counted apart.

One example of each, judged from the test title:

| Class | Example | Why |
|---|---|---|
| Logic | `ForecastLevel.test.tsx`, "should set the level, IconComponent, and color for probability <= 50" | A probability goes in, a level comes out. No click, no layout |
| UI | `AppErrorBoundary.test.tsx`, "reloads the page when the reload button is pressed" | Needs a rendered button and a click |
| Mixed | `DateRangePresets.test.tsx`, "offers a team every window it was given, in the order it was given them" | The ordering is a rule; it is proved through a render |

## The ten files

Paths are under `Lighthouse.Frontend/src/` in `/storage/repos/Lighthouse`. Titles are in the order Vitest lists them (`describe > ... > test`). A title with `%s`, `$name` or similar is one test (a parametrised one counts once).

### File 1: `components/Common/Charts/WorkItemAgingChart.test.tsx` (72 tests)

1. WorkItemAgingChart component > renders correctly with in-progress items
2. WorkItemAgingChart component > displays correct chart title
3. WorkItemAgingChart component > renders fallback message when there are no items
4. WorkItemAgingChart component > renders percentile chips with correct labels
5. WorkItemAgingChart component > renders service level expectation chip when provided
6. WorkItemAgingChart component > does not render service level expectation chip when not provided
7. WorkItemAgingChart component > toggles percentile visibility when chip is clicked
8. WorkItemAgingChart component > toggles service level expectation visibility when chip is clicked
9. WorkItemAgingChart component > groups items correctly by state and age
10. WorkItemAgingChart component > handles empty percentile values array
11. WorkItemAgingChart component > renders chart components correctly
12. WorkItemAgingChart component > renders reference lines for visible percentiles
13. WorkItemAgingChart component > handles items with missing state gracefully
14. WorkItemAgingChart component > handles items with states not in doingStates list
15. WorkItemAgingChart component > correctly extracts age from work items
16. WorkItemAgingChart component > Blocked items functionality > renders blocked items with red color
17. WorkItemAgingChart component > Blocked items functionality > should color groups using the legend type color mapping
18. WorkItemAgingChart component > Blocked items functionality > groups blocked and regular items correctly
19. WorkItemAgingChart component > Blocked items functionality > marks group as blocked if any item in group is blocked
20. WorkItemAgingChart component > Blocked items functionality > renders fallback message when blocked items array is empty
21. WorkItemAgingChart component > Case-insensitive state matching > matches states regardless of casing
22. WorkItemAgingChart component > Case-insensitive state matching > handles mixed casing in doingStates configuration
23. WorkItemAgingChart component > gives the bubble dialog a Time in State column counted to the chart's day
24. WorkItemAgingChart component > stale dots show the staleness Lighthouse decided > reds the dot of a Work Item Lighthouse marks stale, however recently it moved
25. WorkItemAgingChart component > stale dots show the staleness Lighthouse decided > does not red the dot of a Work Item Lighthouse says is not stale, however long it has sat
26. WorkItemAgingChart component > stale dots show the staleness Lighthouse decided > does not red the dot of a Work Item an older Lighthouse said nothing about
27. WorkItemAgingChart component > stale dots show the staleness Lighthouse decided > reds the dot once a fresh answer marks the Work Item stale
28. WorkItemAgingChart component > uses explicit axis IDs and x-axis height for stable tick rendering
29. WorkItemAgingChart component > Per-state pace band overlay > renders no pace-band rects by default even when perStatePercentileValues is provided
30. WorkItemAgingChart component > Per-state pace band overlay > shows the pace-bands toggle only when band data is available
31. WorkItemAgingChart component > Per-state pace band overlay > turns the pace bands on and off through the background control
32. WorkItemAgingChart component > Per-state pace band overlay > keeps the work item type chips and percentile chips untouched by the choice
33. WorkItemAgingChart component > Per-state pace band overlay > spans each state band rect across x in [stateIndex - HALF_WIDTH, stateIndex + HALF_WIDTH]
34. WorkItemAgingChart component > Per-state pace band overlay > anchors the bottom band at the axis minimum and caps the top band at the axis maximum
35. WorkItemAgingChart component > Per-state pace band overlay > colours the stack from greenest at the floor to red at the top by band position
36. WorkItemAgingChart component > Per-state pace band overlay > paints the floor band the greenest colour and the top band red
37. WorkItemAgingChart component > Per-state pace band overlay > clamps the colour of bands beyond the palette to the reddest non-top colour
38. WorkItemAgingChart component > Per-state pace band overlay > paints the top band red even when fewer than four percentiles are returned
39. WorkItemAgingChart component > Per-state pace band overlay > leaves states before the first populated one bare while carrying it forward to the right
40. WorkItemAgingChart component > Per-state pace band overlay > collects all pace-band rects into a single svg group so the overlay is one z-layer
41. WorkItemAgingChart component > Per-state pace band overlay > applies the scales when projecting data coordinates to pixels
42. WorkItemAgingChart component > Per-state pace band overlay > emits no rects for a mapped state whose percentile set is empty
43. WorkItemAgingChart component > Per-state pace band overlay > emits no rects for a state that carries observations but is not in the workflow order
44. WorkItemAgingChart component > Per-state pace band overlay > emits a full-column stack of five bands when all four percentiles are present and distinct
45. WorkItemAgingChart component > Per-state pace band overlay > orders the stacked bands by ascending percentile value regardless of input order
46. WorkItemAgingChart component > Per-state pace band overlay > keys each rect by its state and percentile boundary so React reconciles them stably
47. WorkItemAgingChart component > Per-state pace band overlay > collapses zero-height bands and keeps the surviving 50-70 band green when upper percentiles coincide
48. WorkItemAgingChart component > Per-state pace band overlay > repeats the nearest populated left neighbour's bands for an empty state at the empty state's x position
49. WorkItemAgingChart component > Per-state pace band overlay > renders nothing for a leading empty state with no populated state to its left
50. WorkItemAgingChart component > Per-state pace band overlay > renders pace bands at a readable fill opacity
51. WorkItemAgingChart component > Cycle time / work item age reference-line selector > percentile line labels drop the term prefix (Story 5508 slice 01) > labels work item age reference lines with the percentage alone (AC1)
52. WorkItemAgingChart component > Cycle time / work item age reference-line selector > percentile line labels drop the term prefix (Story 5508 slice 01) > leaves the cycle time labels exactly as they are today (AC2)
53. WorkItemAgingChart component > Cycle time / work item age reference-line selector > percentile line labels drop the term prefix (Story 5508 slice 01) > labels both sources identically, so switching changes only the values (AC1 + AC2)
54. WorkItemAgingChart component > Cycle time / work item age reference-line selector > draws the cycle time reference lines by default and no work item age lines
55. WorkItemAgingChart component > Cycle time / work item age reference-line selector > swaps to work item age lines and removes the cycle time lines, then round-trips back to cycle time
56. WorkItemAgingChart component > Cycle time / work item age reference-line selector > leaves the pace-band overlay untouched when swapping the reference-line source
57. WorkItemAgingChart component > Cycle time / work item age reference-line selector > renders no work item age lines when in-progress work item age percentiles are all zero
58. WorkItemAgingChart component > Cycle time / work item age reference-line selector > keeps the y-axis anchored to cycle time when work item age with smaller percentiles is selected
59. WorkItemAgingChart component > Cycle time / work item age reference-line selector > keeps the pace bands unchanged when swapping the reference-line source to work item age
60. WorkItemAgingChart component > Cycle time / work item age reference-line selector > renders only the highest percentile line when work item age percentiles collapse to one value
61. WorkItemAgingChart component > Cycle time / work item age reference-line selector > keeps the highest percentile per distinct value when some collapse and others do not
62. WorkItemAgingChart component > Cycle time / work item age reference-line selector > lets the next-highest work item age percentile at a shared value show when the highest is toggled off
63. Work Item Age Band beside the dot the coach clicked > hands the dialog behind a dot the same band column the list dialog gets
64. Work Item Age Band beside the dot the coach clicked > reads a dot's items against the state that dot belongs to
65. Work Item Age Band beside the dot the coach clicked > gives every item behind one dot the same band, because one dot is one state at one age
66. Work Item Age Band beside the dot the coach clicked > says no history for a dot in a state nothing finished has ever left
67. Work Item Age Band beside the dot the coach clicked > offers no band column at all when the team has no finished history
68. Work Item Age Band beside the dot the coach clicked > re-reads the bands when the team or the date range behind them changes
69. Work Item Age Band beside the dot the coach clicked > plots exactly the same dots it plotted before the band existed
70. the dialog's band and the chart's zone name the same thing > agrees for every age on $name
71. the risk beside the dot the coach clicked > shows the risk on the items behind a bubble
72. the risk beside the dot the coach clicked > shows no risk column for a team that published no target

### File 2: `components/Common/FeatureGrid/FeatureGrid.test.tsx` (14 tests)

1. FeatureGrid > selectable mode > should render features with checkboxes
2. FeatureGrid > selectable mode > should show checked checkboxes for selected features
3. FeatureGrid > selectable mode > should call onChange when checkbox is clicked
4. FeatureGrid > selectable mode > should remove feature from selection when unchecked
5. FeatureGrid > selectable mode > should render Select All and Clear Selection buttons
6. FeatureGrid > selectable mode > should enable checkbox interaction
7. FeatureGrid > readonly mode > should render features with all checkboxes checked and disabled
8. FeatureGrid > readonly mode > should not render Select All and Clear Selection buttons in readonly mode
9. FeatureGrid > readonly mode > should have dimmed (lower opacity) checkboxes in readonly mode
10. FeatureGrid > readonly mode > should display feature reference and name
11. FeatureGrid > default mode > should default to selectable mode when mode is not specified
12. FeatureGrid > loading > shows the grid busy while its features load, and not busy when loading is not set
13. FeatureGrid > feature filtering > should filter out features with invalid state categories
14. FeatureGrid > feature filtering > should sort Done features to the bottom

### File 3: `components/Common/ProjectSettings/ModifyProjectSettings.queryCommit.test.tsx` (6 tests)

1. ModifyProjectSettings query field > saves nothing when the query is focused and left without a change
2. ModifyProjectSettings query field > saves nothing while the query is being typed and the field is still focused
3. ModifyProjectSettings query field > saves the edited query once, after the field is left
4. ModifyProjectSettings query field > saves nothing when the query is edited back to what was saved before the field is left
5. ModifyProjectSettings query field > saves the edited query when the page is left straight after the field
6. ModifyProjectSettings query field > saves nothing when the page is left without an edit

### File 4: `components/Common/WorkItemsDialog/WorkItemsDialog.test.tsx` (82 tests)

1. WorkItemsDialog Component > renders with default props
2. WorkItemsDialog Component > renders with cycleTime metric
3. WorkItemsDialog Component > displays items sorted by age in descending order with age metric
4. WorkItemsDialog Component > displays items sorted by cycle time in descending order with cycleTime metric
5. WorkItemsDialog Component > calls onClose when close button is clicked
6. WorkItemsDialog Component > renders links for items with URLs
7. WorkItemsDialog Component > displays plain text for items without URLs
8. WorkItemsDialog Component > displays empty state message when no items
9. WorkItemsDialog Component > doesn't open dialog when open prop is false
10. WorkItemsDialog Component > Service Level Expectation (SLE) functionality > renders without styling when no SLE is provided
11. WorkItemsDialog Component > Service Level Expectation (SLE) functionality > applies risky color to items above SLE
12. WorkItemsDialog Component > Service Level Expectation (SLE) functionality > applies realistic color to items at SLE
13. WorkItemsDialog Component > Service Level Expectation (SLE) functionality > applies realistic color to items at or above 70% of SLE
14. WorkItemsDialog Component > Service Level Expectation (SLE) functionality > applies certain color to items below 50% of SLE
15. WorkItemsDialog Component > Service Level Expectation (SLE) functionality > applies colors correctly with cycleTime metric
16. WorkItemsDialog Component > Service Level Expectation (SLE) functionality > applies realistic color to items at exact SLE boundary
17. WorkItemsDialog Component > Service Level Expectation (SLE) functionality > applies realistic color to items at exact 70% SLE
18. WorkItemsDialog Component > Service Level Expectation (SLE) functionality > applies confident color to items at exact 50% SLE
19. WorkItemsDialog Component > Blocked items functionality > displays blocked icon next to time value for blocked items
20. WorkItemsDialog Component > Blocked items functionality > blocked icons have correct tooltip
21. WorkItemsDialog Component > Blocked items functionality > does not display blocked icon for non-blocked items
22. WorkItemsDialog Component > Blocked items functionality > displays blocked icon with correct styling
23. WorkItemsDialog Component > Blocked items functionality > blocked icon appears in time column, not dedicated column
24. WorkItemsDialog Component > Blocked items functionality > blocked items are sorted correctly with time metrics
25. WorkItemsDialog Component > Blocked items functionality > blocked icon works with different time metrics
26. WorkItemsDialog Component > Blocked items functionality > blocked icon appears with SLE coloring
27. WorkItemsDialog Component > Blocked items functionality > displays blocked-duration tooltip when blockedSince is present
28. WorkItemsDialog Component > Blocked items functionality > does not display blocked-duration tooltip when blockedSince is absent on blocked item
29. WorkItemsDialog Component > Blocked items functionality > shows standard blocked tooltip when blockedSince is an invalid date string
30. WorkItemsDialog Component > Blocked items functionality > shows standard blocked tooltip when blockedSince is far in the future
31. WorkItemsDialog Component > Blocked items functionality > displays blocked-duration tooltip with correct boundary rounding when blockedSince is exactly 48h ago
32. WorkItemsDialog Component > Blocked items functionality > omits blocked badge entirely when item is not blocked and blockedSince absent
33. WorkItemsDialog Component > Time in State column functionality > renders a 'Time in State' column with badges when timeInStateColumn provided
34. WorkItemsDialog Component > Time in State column functionality > the %s list showing Time in State is sorted by Age, oldest first
35. WorkItemsDialog Component > Time in State column functionality > a row Lighthouse marks stale shows the stale badge, counted to the column's day, with no threshold given
36. WorkItemsDialog Component > Time in State column functionality > a row Lighthouse says is not stale shows no stale badge, however long it has sat
37. WorkItemsDialog Component > Feature owning team functionality > displays 'Owned by' column for Features with owning teams
38. WorkItemsDialog Component > Feature owning team functionality > does not display 'Owned by' column for regular WorkItems
39. WorkItemsDialog Component > Feature owning team functionality > does not display 'Owned by' column when Features have no owning teams
40. WorkItemsDialog Component > Feature owning team functionality > displays 'Owned by' column when at least one Feature has an owning team
41. Work Item Age Band column > reading the band on every in-flight row > heads the column with the configured term and shows a band on every row
42. Work Item Age Band column > reading the band on every in-flight row > names each item's band in the words a flow coach reads out
43. Work Item Age Band column > reading the band on every in-flight row > paints a band the colour the chart paints the zone it names
44. Work Item Age Band column > reading the band on every in-flight row > leaves an item with nothing to compare against muted and unpainted
45. Work Item Age Band column > reading the band on every in-flight row > shows the band whether or not the chart's coloured zones are switched on
46. Work Item Age Band column > reading the band on every in-flight row > stays visible for a coach whose saved column arrangement predates it
47. Work Item Age Band column > reading the band on every in-flight row > sits beside the age it qualifies when nothing has been rearranged
48. Work Item Age Band column > reading the band on every in-flight row > shows no band column at all for a team with no finished history
49. Work Item Age Band column > reading the band on every in-flight row > leaves every other column exactly as it is when no band is offered
50. Work Item Age Band column > ordering, cutting and carrying out the list > orders worst band first however old the items themselves are
51. Work Item Age Band column > ordering, cutting and carrying out the list > puts the sixteen-day item above the twenty-five-day one, which ordering by age never would
52. Work Item Age Band column > ordering, cutting and carrying out the list > leaves two items in the same band in the order they arrived in
53. Work Item Age Band column > ordering, cutting and carrying out the list > never lets an item with no history head a worst-first list
54. Work Item Age Band column > ordering, cutting and carrying out the list > offers exactly the six band names to cut the list down by
55. Work Item Age Band column > ordering, cutting and carrying out the list > writes the band into the exported file in words, not as a number
56. Work Item Age Band column > ordering, cutting and carrying out the list > carries every in-flight item into the file, not only what is left on screen
57. Work Item Age Band column > ordering, cutting and carrying out the list > keeps copying and exporting behind a licence while ordering stays free
58. Work Item Age Band column > ordering, cutting and carrying out the list > keeps a band name the offered list never carried at the far end, not ahead of everything
59. Work Item Age Band column > ordering, cutting and carrying out the list > leaves a band name the offered list never carried unpainted
60. Work Item Age Band column > ordering, cutting and carrying out the list > does not take over which column the dialog opens sorted by
61. SLE Risk column > reading the risk on every in-flight row > heads the column with the configured term and shows a whole-number percentage on every row
62. SLE Risk column > reading the risk on every in-flight row > leaves the cell empty for an item the answer never mentioned
63. SLE Risk column > reading the risk on every in-flight row > paints the worst risks in the same colour the chart paints its worst zone
64. SLE Risk column > reading the risk on every in-flight row > leaves an item with no answer muted and unpainted
65. SLE Risk column > reading the risk on every in-flight row > shows no risk column at all for a team that published no target
66. SLE Risk column > reading the risk on every in-flight row > stays visible for a coach whose saved column arrangement predates it
67. SLE Risk column > reading the risk on every in-flight row > arrives after the columns a saved arrangement already names
68. SLE Risk column > reading the risk on every in-flight row > tells a reader what the number rests on without putting it in the cell
69. SLE Risk column > reading the risk on every in-flight row > carries no name of its own for a row that makes no claim
70. SLE Risk column > reading the risk on every in-flight row > sits beside the band it belongs with when both are offered
71. SLE Risk column > ordering and carrying out the list > orders by the risk itself, not by the spelling of the risk
72. SLE Risk column > ordering and carrying out the list > never lets an item with no answer head a worst-first list
73. SLE Risk column > ordering and carrying out the list > carries the percentage into the export, not the number behind it
74. SLE Risk column > ordering and carrying out the list > does not take over which column the dialog opens sorted by
75. the room the dialog opens with > opens wide enough for the columns it now carries
76. the room the dialog opens with > remembers a coach's choice of size across a reopen
77. the room the dialog opens with > remembers the size under a key named for the size
78. the room the dialog opens with > opens anyway when the browser will not remember anything
79. the room the dialog opens with > names its close control, so nothing has to find it by position
80. Warnings column > carries whatever the caller found worth warning about
81. Warnings column > is absent entirely from a dialog nobody wired it into
82. the exported file > is named after the dialog's title, each run of spaces one underscore

### File 5: `pages/Portfolios/Detail/Components/DeliveryGrid/DeliveryCreateModal.loadsOnce.test.tsx` (17 tests)

1. the dialog opens with the ways to pick the page already knows > opening Add Launch asks nothing more, and the Jira Release tab is there at once
2. the dialog opens with the ways to pick the page already knows > opened before the page's answer arrived, it shows the busy tab and waits for that same answer
3. the dialog opens with the ways to pick the page already knows > closing and opening it again still asks nothing more
4. the dialog opens with the ways to pick the page already knows > with editing allowed only after the page's answer arrived, as with sign-in on, the sources are asked once more then and never on opening
5. the dialog opens with the ways to pick the page already knows > after the page's answer failed, opening the dialog asks again and offers the Jira Release tab
6. the dialog opens with the ways to pick the page already knows > while the server keeps failing, each opening asks exactly once more and never in a loop
7. the Release list starts when the dialog opens, not when the tab is drawn > with the licence premium, the Jira Releases are asked for while Lena is still on the Manual tab
8. the Release list starts when the dialog opens, not when the tab is drawn > when she then opens the Jira Release tab, the picker is ready from that answer without asking again
9. the Release list starts when the dialog opens, not when the tab is drawn > with the licence not known yet, nothing is asked until it answers premium, and then once
10. the Release list starts when the dialog opens, not when the tab is drawn > moving between the tabs within one opening asks once
11. the Release list starts when the dialog opens, not when the tab is drawn > closing and opening the dialog again asks exactly once more, on opening
12. nothing is fetched that nobody asked for or the server would refuse > a visit to the Deliveries page without opening the dialog asks for no Release list
13. nothing is fetched that nobody asked for or the server would refuse > with a Community licence, opening the dialog and its Jira Release tab asks for no Release list
14. nothing is fetched that nobody asked for or the server would refuse > a Value Stream whose connection offers no source asks for no Release list
15. a late answer never lands in the wrong dialog > an answer for an opening Lena already closed never reaches the picker
16. a late answer never lands in the wrong dialog > an answer for Payments Platform never lands in the dialog opened on Onboarding
17. a late answer never lands in the wrong dialog > a failed answer for an opening Lena closed is not what the next opening shows

### File 6: `pages/Portfolios/Detail/PortfolioDetail.reportsTab.test.tsx` (4 tests)

1. The Reports tab on a Portfolio page > sits between Metrics and Settings
2. The Reports tab on a Portfolio page > opens the Portfolio's reports for a reader
3. The Reports tab on a Portfolio page > opens one report under the Reports tab from an address that names it
4. The Reports tab on a Portfolio page > opens the tab the address names, not a report, when that tab is not Reports

### File 7: `pages/Reports/ReportsPage.usageData.test.tsx` (10 tests)

1. What the Reports page tells the usage data > showing the page is told once, by name alone
2. What the Reports page tells the usage data > a page whose reports could not be loaded tells nothing
3. What the Reports page tells the usage data > a bulk creation is told once, and one created report per report actually created
4. What the Reports page tells the usage data > two reports created and one not are told as two created reports
5. What the Reports page tells the usage data > a bulk removal is told once, and one deleted report per report this run removed
6. What the Reports page tells the usage data > a bulk removal tells one deleted report for each it removed, and none for one already gone
7. What the Reports page tells the usage data > one PDF of several reports is told once, by name alone, and not as a single export
8. What the Reports page tells the usage data > exporting exactly one picked report is told as the single export, never as one PDF of several
9. What the Reports page tells the usage data > a Retry and a Leave it out on one PDF of several reports count nothing more
10. What the Reports page tells the usage data > deleting one report from its row is told as one deleted report, as on the owner's tab

### File 8: `pages/Teams/Detail/Refinement/VoteControl.test.tsx` (5 tests)

1. a row's answers > casts '%s' as %s
2. a row's answers > casts nothing when the voter clicks the answer they already gave
3. a row's answers > takes back the '%s' the voter clicks again, naming it
4. the take-back hint > sits on the voter's own answer and on no other
5. the take-back hint > is not offered while the vote is being sent

### File 9: `components/App/LetPeopleWork/LighthouseVersion.test.tsx` (13 tests)

1. LighthouseVersion component > renders version button with fetched version number
2. LighthouseVersion component > renders update button if newer version available
3. LighthouseVersion component > does not render update button if no newer version available
4. LighthouseVersion component > renders dialog after button was clicked
5. LighthouseVersion component > renders all releases
6. LighthouseVersion component > renders About dialog when info icon is clicked
7. LighthouseVersion component > displays CE marking information in About dialog
8. LighthouseVersion component > displays compliance information with link to Declaration of Conformity in About dialog
9. LighthouseVersion component > displays copyright information in About dialog
10. LighthouseVersion component > closes About dialog when Close button is clicked
11. LighthouseVersion component > does not render update button for standalone (tauri) distributions
12. LighthouseVersion component > offers the in-app update only to a system admin (admin: %s)
13. LighthouseVersion component > does not call isUpdateSupported for standalone distributions

### File 10: `utils/charts/scatterMarkerUtils.test.tsx` (19 tests)

1. scatterMarkerUtils > getBubbleSize > should return minimum size for count of 1
2. scatterMarkerUtils > getBubbleSize > should increase size with count using square root scale
3. scatterMarkerUtils > getBubbleSize > should cap at maximum size of 20
4. scatterMarkerUtils > getBubbleSize > should handle zero count
5. scatterMarkerUtils > renderMarkerCircle > should render circle with correct attributes
6. scatterMarkerUtils > renderMarkerCircle > should apply highlight styling when isHighlighted is true
7. scatterMarkerUtils > renderMarkerCircle > should not apply stroke when not highlighted
8. scatterMarkerUtils > renderMarkerCircle > should include title element for tooltip
9. scatterMarkerUtils > renderMarkerButton > should render button with correct dimensions
10. scatterMarkerUtils > renderMarkerButton > should call onClick when button is clicked
11. scatterMarkerUtils > renderMarkerButton > should have transparent background and cursor pointer
12. scatterMarkerUtils > getMarkerColor > should return error color when hasBlockedItems is true
13. scatterMarkerUtils > getMarkerColor > should return color from colorMap when type matches
14. scatterMarkerUtils > getMarkerColor > should return providedColor when type is not in colorMap
15. scatterMarkerUtils > getMarkerColor > should return theme primary color as fallback
16. scatterMarkerUtils > getMarkerColor > should return providedColor when type is undefined
17. scatterMarkerUtils > renderFallbackMarker > should render circle and button with default styling
18. scatterMarkerUtils > renderFallbackMarker > should use providedColor when specified
19. scatterMarkerUtils > renderFallbackMarker > should include title with fallback text

