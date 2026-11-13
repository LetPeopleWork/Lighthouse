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

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.hooks\.test\.tsx \:\: drops a chosen scope and ignores answers still on their way when the reader moves to another entity or window

```text
The reset effect does not bump the generation, so a late answer for the old entity repopulates the scope; or the reset ignores window changes
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
