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

None.
