# Acceptance brief: story\-6393\-wide\-units\-a

## Slices

### S1

##### Covers

P1

```text
Run the size rule over the real source tree
```

```text
The offender list exists and names exactly BaseMetricsView.tsx and ragRules.ts, and no other production file is over 1000 lines or listed while short
```

```text
A maintainer opening any production frontend file finds it at 1000 lines or fewer unless it is named on the list
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/fileSize\.enforcement\.test\.ts

```text
The list names the right files but a third wide file such as the connection form stays over the line unreported
```

###### Obligations


##### Covers

P1

```text
Run the size rule over planted projects: 1001 lines, 1000 lines, 1500-line test files, test kit, fixture, declaration and src/tests files
```

```text
Only the 1001-line file is reported with its length; 1000 lines and the excluded files are not
```

```text
A new over-wide file is refused by name and length; test code and exact-limit files are left alone
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/fileSize\.enforcement\.test\.ts

```text
The limit is 1100, or the scan counts test kits and src/tests files
```

###### Obligations


##### Covers

P1

```text
Run the size rule with a listed file that is now short or gone, a listed file still wide, and a folder that cannot be read
```

```text
The short or missing entry is reported stale, the still-wide one is accepted, and the unreadable folder fails loudly
```

```text
The list only shrinks and a broken scan never passes as clean
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/fileSize\.enforcement\.test\.ts

```text
A scan that skips unreadable folders and reports no files
```

###### Obligations


##### Covers

P2

```text
Run the existing connection form, save-question, edit-connection and wizard tests against the split form
```

```text
They pass unmodified; removing the upgrade link text, blaming a concurrent edit for every save error, or dropping read-only on OAuth locked fields turns them red
```

```text
The connection form looks and behaves exactly as before
```

##### Disposition

NotTested

```text
Existing unmodified tests cover it; no new oracle row is written for them, and they already pass on today's code so no RED applies
```

crafter

### S2

##### Covers

P2

```text
Call each status rule from its family module with the existing rows, moved by family from the old rules test file
```

```text
All 183 moved rows pass unmodified and every rule gives the same answer as before
```

```text
Every metric widget shows the same status colour and tip as before the split
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ragRulesOverview\.test\.ts\, Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ragRulesTimeAndAge\.test\.ts\, Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ragRulesVolumeAndFlow\.test\.ts

```text
A rule changed while moving (two bands swapped) is still accepted if its moved row was loosened; rows keep exact names and assertions so this is caught on review of the 1:1 id mapping
```

###### Obligations


##### Covers

P1

```text
Ask each rule family module which rules it exports, and look for the old catch-all rules file
```

```text
Overview, time-and-age and volume-and-flow modules export exactly their own rules and the old rules file is gone
```

```text
The status rules are split into three files with no catch-all left, and the size rule no longer lists it
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ragRulesOverview\.test\.ts

```text
A family that also exports another family's rule, or a barrel that re-exports, would pass the exact-export list only if names match; the missing old file check rejects a leftover barrel
```

###### Obligations


### S3

##### Covers

P2

```text
Call the Work Item lookup builder as a function with a run chart record, a cycle time record, an in-progress record, a size chart Feature and a Team Work Item sharing an id, for the five moved lookup rows
```

```text
Each row returns the record it returned before: cycle time and age from the richest source, size chart Features winning, a Team's own Work Item kept over a Feature with the same id, run-chart-only items still present
```

```text
Every metrics list opens the same Work Items with the same fields as before the split
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/workItemLookup\.test\.ts

```text
A lookup that lets a Feature overwrite a Team Work Item would still pass the size-chart-wins row only; the Team row fails it
```

###### Obligations

None.

##### Covers

P2

```text
Call the View Data builder as a function with in-flight items, a published target and each owner kind, for the 37 moved payload rows
```

```text
Each payload carries or omits the risk column as before, names its dialog context and owner kind, counts Time in State to the range's last day, measures age on the last day for the four over-the-range lists, and a team with no target gets no column
```

```text
Every metrics widget list shows the same columns, context and ages as before the split
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetViewData\.test\.ts

```text
A builder that drops the risk column from one in-flight list fails its each-row; one that adds a new unclassified payload fails the every-list-classified row
```

###### Obligations

None.

### S4

##### Covers

P2

```text
Ask whether Estimation vs Cycle Time and every other chart get a frame, with and without a chart, loading and failed
```

```text
Estimation vs Cycle Time is held back until it has a chart or its data failed to load; every other chart is placed from the first render
```

```text
The dashboard layout does not reshuffle and no empty frame appears for an estimation chart that may never exist
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/dashboardFrame\.test\.ts

```text
isPlaced always returns true, or holds back every chart
```

###### Obligations

None.

##### Covers

P2

```text
Ask the frame helpers for the columns status, the frame state and the body of a chart in each loading, failed and ready combination
```

```text
Only the aging chart waits on the Doing states; the worst status wins; a commit pending reads loading; only a ready chart with an older picture is dimmed; the chart body shows only once data and columns arrived
```

```text
A chart under way shows a spinner, never an empty chart that reads as no data
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/dashboardFrame\.test\.ts

```text
the columns status is ignored for aging, or the body shows a chart drawn from empty defaults
```

###### Obligations

None.

##### Covers

P1

```text
Run the size rule over the source tree after the metrics view lost its widget nodes and frame helpers
```

```text
BaseMetricsView.tsx is at most 1000 lines, its entry is off the offender list and the list is empty; a stale entry or a 1001-line file is named
```

```text
No production frontend file is over the limit unless listed, and the list only shrinks
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/fileSize\.enforcement\.test\.ts

```text
the entry is kept after the file is under 1000 lines
```

###### Obligations


## Expectations

### Rows

#### E1

##### Promises

P1

```text
A maintainer opening any production frontend file finds it at 1000 lines or fewer, unless the file is on the size-rule list, and that list does not grow.
```

#### E2

##### Promises

P2

```text
After the split, every Team and Portfolio unit that was split shows the same screens, controls and behaviour as before, and the existing tests pass without edits, except tests that moved with their code.
```

#### E3

##### Promises

P3

```text
Team and Portfolio member and group-mapping actions behave the same from the user's side, and the pages no longer call those access methods directly; the behaviour is reached through one shared hook.
```

#### E4

##### Promises

P4

```text
With default terms, the Team and Portfolio Access tab member failure messages read as they do today; with custom terms, they show the user's configured Team or Portfolio word in lower case.
```

## Charter

None.

## Examination

None.
