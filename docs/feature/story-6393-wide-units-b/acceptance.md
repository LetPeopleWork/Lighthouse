# Acceptance brief: story\-6393\-wide\-units\-b

## Slices

### S1

##### Covers

P2

```text
The reader picks a named cycle time definition for the percentiles, a slower earlier pick answers after a faster later one
```

```text
The scoped values and info are those of the latest pick only; the scope id is the latest pick
```

```text
The percentile widget never shows numbers of a pick the reader has replaced
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.hooks\.test\.tsx \:\: shows the answers of the latest selection only\, even when an earlier one answers last

```text
A response for a superseded selection overwrites the current one (no generation guard on a selection)
```

###### Obligations


##### Covers

P2

```text
A named definition is picked and either the percentile values fetch or the info fetch fails
```

```text
The scope id, scoped values and scoped info all return to Default (null)
```

```text
The widget never presents Default numbers under a named selection
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.hooks\.test\.tsx \:\: returns to Default when fetching the percentile values or info for a named definition fails

```text
Only the failing half is cleared, the scope id stays named, or the failure is swallowed and the old scope stays
```

###### Obligations


##### Covers

P2

```text
A scope is chosen, then the entity, the window start or the window end changes while the answers are still on their way, then they arrive
```

```text
The scope returns to Default at the change and the late answers do not bring it back
```

```text
A scope chosen for one team or window never leaks into the next
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.hooks\.test\.tsx \:\: drops a chosen scope and ignores answers still on their way when the reader moves to another entity or a window that starts earlier or ends later

```text
The reset effect does not bump the generation, so a late answer for the old entity repopulates the scope; or the reset ignores a change of the window start or end
```

###### Obligations


##### Covers

P2

```text
A named definition is picked and either the percentile values fetch or the info fetch fails
```

```text
The scope id, scoped values and scoped info all return to Default (null)
```

```text
The widget never presents Default numbers under a named selection
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.hooks\.test\.tsx \:\: returns to Default when fetching \.\.\. fails \(L1\/F73\)

```text
Only the failing half is cleared, the scope id stays named, or the failure is swallowed and the old scope stays
```

###### Obligations


##### Covers

P2

```text
A scope is chosen, then the entity id or the window end changes while the answers are still on their way, then they arrive
```

```text
The scope returns to Default at the change and the late answers do not bring it back
```

```text
A scope chosen for one team or window never leaks into the next
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.hooks\.test\.tsx \:\: drops a chosen scope and ignores answers still on their way \(L1\/F74\)

```text
The reset effect does not bump the generation, so a late answer for the old entity repopulates the scope; or the reset ignores window changes
```

###### Obligations


##### Covers

P1

```text
A state is open and the reader changes the Work Item selection
```

```text
The service is asked again for the same state with the new item ids
```

```text
The drill-down shows rows for the narrowed selection, not the old one
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.hooks\.test\.tsx \:\: asks again with the new Work Item selection when the selection changes while a state is open

```text
The item ids are missing from the query key, so the old rows stay and no second request is made
```

###### Obligations


### S2

##### Covers

P1

```text
Ask applicability with some Work Items chosen, one stretch chosen, and for every known question
```

```text
Each narrowed Cumulative Time per State question follows its own choice, and exactly the known questions are answered
```

```text
The narrowed state-time chart is asked only when the reader narrowed it
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/hooks\/metricsDataModules\.test\.tsx\:\:Which metrics questions an owner can ask

```text
the two choice flags swapped, or a question missing from the answer
```

###### Obligations


##### Covers

P1

```text
Run the query helper for an answer, an empty answer, a disabled question and a failing question
```

```text
Fresh no-retry options; the request reaches the question; an empty answer is an answer; a disabled question is not asked; a failure is asked once and logged with its message
```

```text
Metrics are asked fresh once and a failure is reported
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/hooks\/metricsDataModules\.test\.tsx\:\:The metrics query helper

```text
retries on failure, caching, or reading an empty answer as still loading
```

###### Obligations

None.

##### Covers

P1

```text
Move the window while the next answer is on its way, then let it arrive
```

```text
The previous window answer stays marked as placeholder, then the new answer replaces it
```

```text
The chart shows the previous window dimmed while the next loads
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/hooks\/metricsDataModules\.test\.tsx\:\:The metrics query helper

```text
an empty area while loading, or the old answer never replaced
```

###### Obligations

None.

##### Covers

P1

```text
Draw an answer, then the next window of the same owner fails
```

```text
The last drawn answer stays
```

```text
The chart stays behind the could-not-load note
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/hooks\/metricsDataModules\.test\.tsx\:\:The metrics query helper

```text
the last-drawn memory not being kept on a failure
```

###### Obligations


##### Covers

P1

```text
Draw an answer for one owner, then another owner id or another owner type with the same id fails
```

```text
No answer and no data are shown
```

```text
One owner never sees another owner's numbers behind a failure
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/hooks\/metricsDataModules\.test\.tsx\:\:The metrics query helper

```text
the last answer shown for any owner, or an owner told apart by id only
```

###### Obligations


##### Covers

P1

```text
Render the metrics hook for a team with every request on and record the service calls
```

```text
Requests start in the one order the view always started them
```

```text
Metrics load in the same order as before
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/hooks\/metricsDataModules\.test\.tsx\:\:The metrics view\'s requests

```text
the query calls reordered or split into sub-hooks that start in another order
```

###### Obligations

None.

##### Covers

P1

```text
Run the layer-boundary rule over the code after the move
```

```text
The allowed-leak list names exactly the leaking file pairs and has not grown
```

```text
The layer rule stays honest after the split
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/layerBoundaries\.enforcement\.test\.ts

```text
a new leak pair added to make the build pass
```

###### Obligations


##### Covers

P3

```text
Run the size rule over the production files after the move
```

```text
No production file reaches 1000 lines and the offender list is empty
```

```text
The metrics data hook is split into parts of readable size
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/fileSize\.enforcement\.test\.ts

```text
the hook still carrying all three responsibilities under the limit by chance
```

###### Obligations


##### Covers

P1

```text
Ask which metrics questions a team, a team that also carries portfolio-only methods, a portfolio, a feature-sizes-only service and a bare service can ask
```

```text
Each owner shape is left without exactly its own set of questions
```

```text
A team, a portfolio and other owners see only the metrics they can have
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/hooks\/metricsDataModules\.test\.tsx\:\:Which metrics questions an owner can ask

```text
the risk question being asked of every owner, or a team with extra methods being treated as a portfolio
```

###### Obligations





##### Covers

P1

```text
Make the owner key for an owner, the same owner twice with another name, and the owner after an update
```

```text
The key carries the id and update moment only; equal owners give equal keys; an update gives another key
```

```text
An open dashboard asks again after the owner is updated
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/hooks\/metricsDataModules\.test\.tsx\:\:Whose metrics a request asks for

```text
a key that drops the update moment or adds the name
```

###### Obligations



### S3

##### Covers

P1

```text
Open the unbind confirmation, press Cancel, press Stop following twice, close and reopen
```

```text
The question names the source and the Delivery, Cancel hands back a cancel, Stop following confirms exactly once then switches off, and is on again after reopening
```

```text
The reader is asked before letting go of a source and cannot send the change twice
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliverySectionParts\.test\.tsx

```text
A confirm button that never calls back, calls twice, stays enabled after the first press, or stays off after reopening
```

###### Obligations

None.

##### Covers

P1

```text
Open the work items tab loading, empty and with two Features
```

```text
Loading shows the loading sentence, an empty Delivery says it holds no Features, otherwise each Feature is listed with its Team link
```

```text
The Delivery's Features list reads as before
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliverySectionParts\.test\.tsx

```text
A tab that shows the empty sentence while loading, drops the rows, or loses the Team column
```

###### Obligations

None.

##### Covers

P1

```text
Press archive, edit and delete on a Delivery row with and without premium
```

```text
Each action hands the Delivery back once without opening the row; archive is off without premium while edit and delete stay on
```

```text
A reader gets the row actions they may use
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliverySectionParts\.test\.tsx

```text
An action missing for one role, a click that also opens the row, or archive that fires while disabled
```

###### Obligations

None.

##### Covers

P1

```text
Render the marker beside a Delivery's name for a reader who may and one who may not let go
```

```text
No button without the right; with it a button that asks to stop following without opening the row
```

```text
Only a reader who may edit is offered the way out
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliverySectionParts\.test\.tsx

```text
A marker that is always a button, never a button, or lets the click through to the row
```

###### Obligations

None.

##### Covers

P1

```text
Render the teams on a Feature row with and without work
```

```text
Only teams with work are linked to their page; otherwise Unassigned
```

```text
The row names who carries the work
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliverySectionParts\.test\.tsx

```text
A list of all teams, or no Unassigned word when nobody has work
```

###### Obligations

None.

##### Covers

P1

```text
Ask how the Features were chosen for bound, rule-based, manual and unbindable Deliveries
```

```text
Bound beats rule-based, each state has its own icon and sentence, and the way-out suffix appears only when allowed
```

```text
The badge beside a Delivery's name says the same as before
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliverySectionParts\.test\.tsx

```text
Wrong sentence or icon for one source state, bound ranked below rule-based, or the suffix always or never added
```

###### Obligations

None.

##### Covers

P1

```text
Ask what the header chip says for a forecast, a capped forecast, a team without history, no likelihood and thin data
```

```text
The joint sentence with date and percentage, the capped value, Cannot forecast, Cannot forecast, Not enough data
```

```text
The header chip carries the same words as before
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliverySectionParts\.test\.tsx

```text
Thin data outranking no forecast, a changed wording, or no cap on near-certain values
```

###### Obligations

None.

##### Covers

P1

```text
Render the source notices for a bound Delivery with an unavailable source and refused forecast, one of them, and an unbound Delivery
```

```text
Both, only the one that applies, or none
```

```text
A Delivery warns only about a source it follows
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliverySectionParts\.test\.tsx

```text
Showing a notice for an unbound Delivery, or hiding one of two standing notices
```

###### Obligations

None.

##### Covers

P3

```text
Run the size rule over the production files
```

```text
DeliverySection stays under the line limit and no production file is listed as an offender
```

```text
The section is small enough to read and change
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/fileSize\.enforcement\.test\.ts

```text
A section that stays near 1000 lines or an offender list that quietly grows
```

###### Obligations


### S4

##### Covers

P1

P4

```text
Ask whether no date, yesterday, today and tomorrow are launch dates, and what message each gets
```

```text
No date is required; yesterday and today must be in the future; tomorrow is accepted with no message; the delivery word is the tenant's
```

```text
A reader who types today or a past day is refused, with a message naming the fix
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/deliveryFormRules\.test\.ts

```text
Today counts as future, or the message says Delivery instead of the renamed term, or an empty date is called past
```

###### Obligations






##### Covers

P1

P4

```text
Ask whether an empty name, a name of spaces, a name and a padded name are names
```

```text
Empty and spaces are refused in the delivery word; real names, padded or not, are accepted
```

```text
A launch cannot be saved with a blank name
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/deliveryFormRules\.test\.ts

```text
A name of spaces passes
```

###### Obligations



##### Covers

P1

P4

```text
Ask what the form asks next for a manual tab with all three things wrong and then fixed one by one, for the rule-based tab, and for a source tab with blank name and date
```

```text
Name, then date, then past date, then the tab need, then nothing; a source tab speaks alone; rule tab asks for validation
```

```text
The reader is told one missing thing at a time in a fixed order; on a Release tab only the Release is asked for
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/deliveryFormRules\.test\.ts

```text
Date asked before name, a source tab also reporting name and date, or the tab need coming first
```

###### Obligations




##### Covers

P1

P4

```text
Ask which tabs are offered when no delivery, a manual, a rule-based, a Fix Version-bound and a delivery bound to an unoffered source is edited
```

```text
All tabs, except a delivery bound to an offered source gets that source tab alone
```

```text
Editing a delivery that follows a Release offers no way out of it
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/deliveryFormRules\.test\.ts

```text
Every tab offered to a bound delivery, or the first source tab instead of the matching one
```

###### Obligations




##### Covers

P1

P4

```text
Ask what the source picker is told it follows for no delivery, no reference, another tab, Manual tab and the matching tab
```

```text
Nothing except on the tab of the followed source, where entry id, name and date are returned
```

```text
A bound delivery shows the Release it follows only in that Release's picker
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/deliveryFormRules\.test\.ts

```text
The followed Release shown in another source's picker or in Manual
```

###### Obligations





##### Covers

P1

P4

```text
Write dates held by the tracker into the date field
```

```text
Zero-padded year-month-day read in UTC, including 23:30 and 00:00 UTC
```

```text
The date field shows the day the tracker's board says
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/deliveryFormRules\.test\.ts

```text
Reading local time instead of UTC, or no zero padding
```

###### Obligations


##### Covers

P3

```text
Run the existing size rule over the frontend source
```

```text
DeliveryCreateModal.tsx ends well under 1000 lines and the offender list stays empty
```

```text
No production file in the frontend stays over the size limit
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/fileSize\.enforcement\.test\.ts

```text
A split that leaves the dialog file over the limit with an entry added to the offender list
```

###### Obligations


## Expectations

### Rows

#### E1

##### Promises

P1

```text
A user sees the same content and can do the same things on the metrics and delivery screens after the split as before it.
```

#### E2

##### Promises

P1

```text
The metrics data requests start in the same order as before the split.
```

#### E3

##### Promises

P1

```text
Existing tests for the split units pass without edits, except tests that moved with the code they cover. Not user-observable; the test run record is the only evidence, so candidate review owns it.
```

#### E4

##### Promises

P2

```text
A user can still use the named-percentiles scope and the state drill-down on the metrics view exactly as before. Where the logic lives is an implementation constraint, not user-observable; candidate review owns it.
```

#### E5

##### Promises

P3

```text
The three named files are each split to well under 1000 lines and the size rule list is empty. Not user-observable; an architectural constraint for candidate review, not a user expectation.
```

#### E6

##### Promises

P4

```text
A user gets the same Delivery form behaviour: invalid or missing dates, a missing or duplicate name, a blocking error, the offered tabs and the bound source behave as before. Testability without rendering the dialog is a developer constraint; candidate review owns it.
```

## Charter

None.

## Examination

### E1

met

#### Observations

```text
Probe 1 (frontend-tests, fragments useMetricsData,DeliverySection,DeliveryCreateModal,NamedPercentiles,StateDrillDown,deliveryFormRules,UnbindConfirmation,WorkItemsTab,deliverySelectionTabs,metrics,Metrics,Delivery): summary 'Test Files 149 passed (149)', 'Tests 2655 passed (2655)', exit 0. Rows shown as user-facing test titles include: metrics widgets loading/could-not-load per request (widgetStatus.test.ts, BaseMetricsView.loading.test.tsx); useMetricsData owner/window/baseline fetch rows (useMetricsData.test.ts: 'should call all core metrics service methods on mount', 'A failed window ... shows nothing rather than the previous owner's answer'); Delivery create dialog rows (DeliveryCreateModal.test.tsx 'should have Save button disabled for past date'; deliveryFormRules.test.ts 'yesterday is judged by whether it is after today', 'today is answered with the message that names its fix', 'a name of nothing but spaces is no name'); DeliverySection overdue rows (DeliverySection.overdue.test.tsx 'says a target that has been and gone is overdue'); DeliverySection.metrics.test.tsx 'does not fetch metrics history while the Work Items tab is active'. No row title names useNamedPercentilesScope, useStateDrillDown, applicabilityFor, metricsOwnerKeyOf, UnbindConfirmationDialog or WorkItemsTab by function name; the named-percentiles scope and state drill-down rows were not found in this run.
```

```text
Saved output of probe 1 (grep of its rows, no new probe): named-percentiles scope rows pass in BaseMetricsView.test.tsx ('Cycle Time Percentiles named scope (lifted state)': Default selection reads default cycle time; Named selection reads the named cycle time and filters rows; no SLE line in dialog). Drill-down rows pass: BaseMetricsView.test.tsx 'passes the active selection to the drill-down items fetch' and 'maps the drill-down payload into rows with a linked name and rounded days contributed'; BaseMetricsView.cumulativeStateTimeDialog.test.tsx 'a late answer for the bar clicked before never lands in the next bar's dialog'. Unbind rows pass: DeliverySection.provenance.test.tsx 'never unbinds on its own', 'asks before unbinding, and says what the delivery keeps'. Header-copy rows pass: DeliverySectionParts.test.tsx 'says a source-bound Delivery is bound to its source, ahead of any rule'. Overdue rows pass: DeliverySection.overdue.test.tsx (AC-03.2). Form rows pass: deliveryFormRules.test.ts (yesterday/today refused, tomorrow accepted, blank name, missing date, one-thing-next ordering); DeliveryCreateModal.test.tsx Save disabled for empty name and past date. Not located by title in the run: any row for duplicate-name refusal, for a blocking error in the Delivery form, for a request-start-order check, or for the UnbindConfirmationDialog/WorkItemsTab part by name.
```

### E2

met

#### Observations

```text
Probe 2 (frontend-tests metricsDataModules,deliveryFormRules, no name filter, unedited output): summary 'Test Files 2 passed (2)', 'Tests 47 passed (47)', exit 0. Rows: src/hooks/metricsDataModules.test.tsx > 'The metrics view's requests > start in the order the view has always started them' passes; 'Whose metrics a request asks for' and 'The metrics query helper' rows pass (keeps last answer drawn for same owner when next window fails; shows no answer when another owner id fails). src/pages/.../deliveryFormRules.test.ts rows pass: dates (yesterday and today refused, tomorrow accepted, no date answered with fix message); name ('nothing typed', 'only spaces' refused); 'one thing the form asks for next'; 'the tabs an edit dialog offers' (offers only the tab of the source a delivery follows); 'what the source picker is told it already follows'. No row in this probe names a duplicate-name refusal or a blocking error of the Delivery form.
```

### E3

unmet

#### Observations

```text
Probe 1 (frontend-tests, fragments useMetricsData,DeliverySection,DeliveryCreateModal,NamedPercentiles,StateDrillDown,deliveryFormRules,UnbindConfirmation,WorkItemsTab,deliverySelectionTabs,metrics,Metrics,Delivery): summary 'Test Files 149 passed (149)', 'Tests 2655 passed (2655)', exit 0. Rows shown as user-facing test titles include: metrics widgets loading/could-not-load per request (widgetStatus.test.ts, BaseMetricsView.loading.test.tsx); useMetricsData owner/window/baseline fetch rows (useMetricsData.test.ts: 'should call all core metrics service methods on mount', 'A failed window ... shows nothing rather than the previous owner's answer'); Delivery create dialog rows (DeliveryCreateModal.test.tsx 'should have Save button disabled for past date'; deliveryFormRules.test.ts 'yesterday is judged by whether it is after today', 'today is answered with the message that names its fix', 'a name of nothing but spaces is no name'); DeliverySection overdue rows (DeliverySection.overdue.test.tsx 'says a target that has been and gone is overdue'); DeliverySection.metrics.test.tsx 'does not fetch metrics history while the Work Items tab is active'. No row title names useNamedPercentilesScope, useStateDrillDown, applicabilityFor, metricsOwnerKeyOf, UnbindConfirmationDialog or WorkItemsTab by function name; the named-percentiles scope and state drill-down rows were not found in this run.
```

```text
Non-probe, examiner-side: user-observable behaviour of split units was judged from the captured test titles and their pass status only. No browser capture, no before/after baseline, no network trace was taken; the request start order (E2) and the line counts of the three named files (E5) are not visible from any public surface in this workspace; the candidate checkout and its diffs were not read (source-blind). Test-file unmodified status (E3) cannot be established publicly. Public docs read attempt: nwdes examine read docs/guides refused, path absent; workspace root read returned no listing.
```

observation_gap

### E4

met

#### Observations

```text
Saved output of probe 1 (grep of its rows, no new probe): named-percentiles scope rows pass in BaseMetricsView.test.tsx ('Cycle Time Percentiles named scope (lifted state)': Default selection reads default cycle time; Named selection reads the named cycle time and filters rows; no SLE line in dialog). Drill-down rows pass: BaseMetricsView.test.tsx 'passes the active selection to the drill-down items fetch' and 'maps the drill-down payload into rows with a linked name and rounded days contributed'; BaseMetricsView.cumulativeStateTimeDialog.test.tsx 'a late answer for the bar clicked before never lands in the next bar's dialog'. Unbind rows pass: DeliverySection.provenance.test.tsx 'never unbinds on its own', 'asks before unbinding, and says what the delivery keeps'. Header-copy rows pass: DeliverySectionParts.test.tsx 'says a source-bound Delivery is bound to its source, ahead of any rule'. Overdue rows pass: DeliverySection.overdue.test.tsx (AC-03.2). Form rows pass: deliveryFormRules.test.ts (yesterday/today refused, tomorrow accepted, blank name, missing date, one-thing-next ordering); DeliveryCreateModal.test.tsx Save disabled for empty name and past date. Not located by title in the run: any row for duplicate-name refusal, for a blocking error in the Delivery form, for a request-start-order check, or for the UnbindConfirmationDialog/WorkItemsTab part by name.
```

### E5

unmet

#### Observations

```text
Non-probe, examiner-side: user-observable behaviour of split units was judged from the captured test titles and their pass status only. No browser capture, no before/after baseline, no network trace was taken; the request start order (E2) and the line counts of the three named files (E5) are not visible from any public surface in this workspace; the candidate checkout and its diffs were not read (source-blind). Test-file unmodified status (E3) cannot be established publicly. Public docs read attempt: nwdes examine read docs/guides refused, path absent; workspace root read returned no listing.
```

observation_gap

### E6

unmet

#### Observations

```text
Probe 1 (frontend-tests, fragments useMetricsData,DeliverySection,DeliveryCreateModal,NamedPercentiles,StateDrillDown,deliveryFormRules,UnbindConfirmation,WorkItemsTab,deliverySelectionTabs,metrics,Metrics,Delivery): summary 'Test Files 149 passed (149)', 'Tests 2655 passed (2655)', exit 0. Rows shown as user-facing test titles include: metrics widgets loading/could-not-load per request (widgetStatus.test.ts, BaseMetricsView.loading.test.tsx); useMetricsData owner/window/baseline fetch rows (useMetricsData.test.ts: 'should call all core metrics service methods on mount', 'A failed window ... shows nothing rather than the previous owner's answer'); Delivery create dialog rows (DeliveryCreateModal.test.tsx 'should have Save button disabled for past date'; deliveryFormRules.test.ts 'yesterday is judged by whether it is after today', 'today is answered with the message that names its fix', 'a name of nothing but spaces is no name'); DeliverySection overdue rows (DeliverySection.overdue.test.tsx 'says a target that has been and gone is overdue'); DeliverySection.metrics.test.tsx 'does not fetch metrics history while the Work Items tab is active'. No row title names useNamedPercentilesScope, useStateDrillDown, applicabilityFor, metricsOwnerKeyOf, UnbindConfirmationDialog or WorkItemsTab by function name; the named-percentiles scope and state drill-down rows were not found in this run.
```

```text
Saved output of probe 1 (grep of its rows, no new probe): named-percentiles scope rows pass in BaseMetricsView.test.tsx ('Cycle Time Percentiles named scope (lifted state)': Default selection reads default cycle time; Named selection reads the named cycle time and filters rows; no SLE line in dialog). Drill-down rows pass: BaseMetricsView.test.tsx 'passes the active selection to the drill-down items fetch' and 'maps the drill-down payload into rows with a linked name and rounded days contributed'; BaseMetricsView.cumulativeStateTimeDialog.test.tsx 'a late answer for the bar clicked before never lands in the next bar's dialog'. Unbind rows pass: DeliverySection.provenance.test.tsx 'never unbinds on its own', 'asks before unbinding, and says what the delivery keeps'. Header-copy rows pass: DeliverySectionParts.test.tsx 'says a source-bound Delivery is bound to its source, ahead of any rule'. Overdue rows pass: DeliverySection.overdue.test.tsx (AC-03.2). Form rows pass: deliveryFormRules.test.ts (yesterday/today refused, tomorrow accepted, blank name, missing date, one-thing-next ordering); DeliveryCreateModal.test.tsx Save disabled for empty name and past date. Not located by title in the run: any row for duplicate-name refusal, for a blocking error in the Delivery form, for a request-start-order check, or for the UnbindConfirmationDialog/WorkItemsTab part by name.
```

```text
Probe 2 (frontend-tests metricsDataModules,deliveryFormRules, no name filter, unedited output): summary 'Test Files 2 passed (2)', 'Tests 47 passed (47)', exit 0. Rows: src/hooks/metricsDataModules.test.tsx > 'The metrics view's requests > start in the order the view has always started them' passes; 'Whose metrics a request asks for' and 'The metrics query helper' rows pass (keeps last answer drawn for same owner when next window fails; shows no answer when another owner id fails). src/pages/.../deliveryFormRules.test.ts rows pass: dates (yesterday and today refused, tomorrow accepted, no date answered with fix message); name ('nothing typed', 'only spaces' refused); 'one thing the form asks for next'; 'the tabs an edit dialog offers' (offers only the tab of the source a delivery follows); 'what the source picker is told it already follows'. No row in this probe names a duplicate-name refusal or a blocking error of the Delivery form.
```

observation_gap
