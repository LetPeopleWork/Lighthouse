# Product brief: story\-6655\-outlier\-toggle\-and\-unpl

## Request

```text
Story 6655 Outlier Config and on/off: Was playing around with the new outliers in the cycle time scatterplot, and while I like it, I think it should be adjusted like this: - would be nice to toggle it on/off, so if I really wanna see all items, it should show (on by default, no need to store the state and start on always...) - been thinking if we should use the UNPL from the CT PBC as a limit instead of 5x median... I think basing it on UNPL does make more sense (in my mind). Later we *may* configure it more but for now I would not configure it at all. With the UNPL the question is, does it still work. Following an example from a team, where we have 20+ outliers, and comparison with the CT PBC (way less).
```

## Work

M

```text
A reader of the cycle time scatterplot can see every item on demand, and the items flagged as outliers match the ones the cycle time process behaviour chart calls unusual.
```

### Rework

None.

## Promises

### P1

Outcome

```text
A reader of the Cycle Time scatterplot can switch outlier handling off to see every closed Work Item drawn on the chart, and when it is on, the Work Items set apart as outliers are exactly the ones the Cycle Time process behaviour chart shows above its upper natural process limit (UNPL).
```

### P2

Scenario

```text
Outlier handling starts on every time the chart opens
```

```text
a Team whose closed Work Items in the selected date range include Work Items above the Cycle Time UNPL
```

```text
the reader opens the Metrics tab, or reloads it after switching outlier handling off
```

```text
outlier handling is on: the Work Items above the UNPL are not drawn on the plot and the outlier chip counts them
```

### P3

Scenario

```text
The reader sees every Work Item on demand
```

```text
the Cycle Time scatterplot shows an outlier chip with 3 outliers
```

```text
the reader switches outlier handling off
```

```text
all closed Work Items, the 3 former outliers included, are drawn on the plot, the y-axis rises to fit the tallest of them, and the outlier chip is gone; switching it on again restores the chip and the shorter axis
```

### P4

Scenario

```text
Outliers are the Work Items above the Cycle Time UNPL
```

```text
the Cycle Time process behaviour chart of the same Team and date range has an UNPL of 30 days, and closed Work Items with Cycle Times of 12, 30, 31 and 90 days
```

```text
the reader looks at the Cycle Time scatterplot with outlier handling on
```

```text
the Work Items of 31 and 90 days are the outliers, the 12 and 30 day Work Items are drawn, and the outlier list and the help name the limit as the upper natural process limit, not as 5 times the median
```

### P5

Scenario

```text
Without process limits, no Work Item is set apart
```

```text
the Cycle Time process behaviour chart has no limits for the selected range, because its baseline is invalid (for example shorter than 14 days) or holds fewer than 2 closed Work Items
```

```text
the reader looks at the Cycle Time scatterplot with outlier handling on
```

```text
every closed Work Item is drawn, no outlier count is shown, and the reader can see that outliers need the process behaviour chart's limits
```

## Scope

```text
An on/off control for outlier handling on the Cycle Time scatterplot, on by default, not stored: every load starts with it on.
```

```text
Replace the 5 x median outlier rule with the Cycle Time process behaviour chart's UNPL for the same Team or Portfolio and date range, including its configured baseline when one is set.
```

```text
The outlier help text, the outlier list title and the chip wording name the new limit in the configured Terminology words.
```

```text
A defined result when the process behaviour chart has no limits (invalid baseline, fewer than 2 baseline Work Items): no outliers set apart, and the reader is told why.
```

```text
Any configuration of the outlier rule (multiple, limit choice, per Team setting): the maintainer said not to configure it for now.
```

```text
Remembering the on/off choice across loads, sessions or users: the maintainer said no need to store it; it starts on every time.
```

```text
Changes to the process behaviour chart itself, its baseline rules or its special-cause rules.
```

```text
Outlier handling on any chart other than the Cycle Time scatterplot.
```

## Decisions

```text
Outlier handling has an on/off control, on by default, its state is not stored, and it starts on at every load (maintainer, in the Request).
```

#### Status

Confirmed


```text
No configuration of the outlier rule in this Story; a later Story may add it (maintainer, in the Request).
```

#### Status

Confirmed


```text
The limit is the UNPL exactly as the Cycle Time process behaviour chart computes it: average plus 2.66 times the average moving range of the baseline Cycle Times, the baseline being the configured baseline dates or, without one, the selected date range. An outlier is a Work Item the chart marks above that UNPL (compared unrounded, as the chart does), so the two charts never disagree on which Work Items are unusual.
```

#### Status

Proposed


```text
Only the above-UNPL rule (large change, upward) defines an outlier. The chart's other signals (below the lower limit, two-sigma and one-sigma runs, shifts) do not set a Work Item apart on the scatterplot, because a long Cycle Time is what squeezes the other dots.
```

#### Status

Proposed


```text
When the process behaviour chart has no usable limits (status BaselineInvalid, or fewer than 2 baseline Work Items, where the UNPL equals the single value or 0), no Work Item is set apart and the chip area says outliers need the process behaviour chart's limits; the scatterplot never falls back to 5 x median silently.
```

#### Status

Proposed


```text
The scatterplot gets the UNPL whether or not the process behaviour chart widget is shown on the same tab; today the Cycle Time process behaviour chart data is fetched only when that widget is asked for, so DESIGN decides how the limit reaches the scatterplot.
```

#### Status

Proposed


```text
While the limit is loading or could not be loaded, the scatterplot shows no stale outlier count: it reuses the existing loading chip wording (Outliers, loading) and the WidgetShell failure look; DESIGN sketches both states.
```

#### Status

Proposed


```text
Control: a labelled MUI Switch, copy Hide outliers, in the scatterplot title row next to the Cycle Time selector, on by default. Switched off: every Work Item drawn, y-axis fits all, outlier chip hidden. Wording finalised at the DELIVER sketch review.
```

#### Status

Proposed


```text
New help sentence: An outlier is a closed Work Item whose Cycle Time is above the upper natural process limit of the Cycle Time process behaviour chart for this date range. List title: Work Items with a Cycle Time above the upper natural process limit (N days). Terms come from Terminology.
```

#### Status

Proposed


```text
Maintainer 2026-11-17: a named Cycle Time definition uses the same rule as the default: its UNPL is computed the same way (XmR, same baseline) over that definition's Cycle Times. The outlier rule must be designed as a replaceable definition (e.g. a strategy/policy passed in), so another outlier definition can be supplied later without reworking callers; still no user configuration now.
```

#### Status

Confirmed


```text
Maintainer 2026-11-17: no separate check of the UNPL count on the team that showed 20+ outliers is needed before DELIVER; the UNPL rule is accepted as designed.
```

#### Status

Confirmed


```text
DISCUSS checklist, RBAC impact: N/A, because the control is view-only and local, adds no action, page or data, and the limit comes from Cycle Time data a reader of the Metrics tab can already see.
```

#### Status

Proposed


```text
DISCUSS checklist, Lighthouse-Clients CLI and MCP: N/A, because no API route or DTO changes if the scatterplot uses the existing Cycle Time process behaviour chart answer; DESIGN revisits this if the named-definition answer needs a new field or route.
```

#### Status

Proposed


```text
DISCUSS checklist, website marketing surface: no new line or page, because this refines an existing chart behaviour; at finalize, check that no website or docs copy still says 5 times the median.
```

#### Status

Proposed


```text
DISCUSS checklist, context map: touches Flow metrics only; publishes and reacts to no domain event (view behaviour, no state change); UI surface is the Metrics tab of a Team's or Portfolio's page (ADR-271), unchanged. New glossary terms for Flow metrics in ADR-270: Outlier (a closed Work Item whose Cycle Time is above the Cycle Time UNPL) and Upper natural process limit (UNPL); neither is in the glossary today.
```

#### Status

Proposed


```text
Visualization checklist: the question is which closed Work Items are unusually long, and can I see them all; awkward data covered by the no-limits scenario and the existing all-visible-are-outliers state; form unchanged (scatter); title and axes unchanged and labelled; the info popover and docs state the UNPL rule and the switch; no new colour; the switch carries a text label, not colour alone; loading, failure and no-limits states are distinct from no data; check phone width, dark mode, enlarged and PDF at finalize.
```

#### Status

Proposed


## Journey

```text
The reader opens the Metrics tab of a Team and looks at the Cycle Time scatterplot; outlier handling is on and the longest Work Items are set apart in the outlier chip.
```

```text
Not supplied beyond the maintainer liking the outliers.
```

```text
The reader compares the outliers with the Cycle Time process behaviour chart and sees the two agree on which Work Items are unusual.
```

```text
Not supplied; the Request records that 20+ outliers against far fewer on the process behaviour chart prompted the change.
```

```text
The reader wants to see every Work Item and switches outlier handling off; all dots are drawn and the axis fits them.
```

```text
Not supplied.
```

```text
The reader reloads or comes back later and finds outlier handling on again.
```

```text
Not supplied.
```

## Slices

### S1

```text
Walking skeleton: on the Cycle Time scatterplot of a Team's Metrics tab, a Hide outliers switch is on at every load; switching it off draws every closed Work Item with the y-axis fitting all of them and hides the outlier chip; switching it on restores the chip and the shorter axis. Outliers are still found by today's rule in this slice.
```

#### Promises

P1

P2

P3

#### Depends on

None.

#### Slice work

S

```text
A reader can see every Work Item on demand.
```

##### Rework

None.

### S2

```text
On the Cycle Time scatterplot with the default Cycle Time, the Work Items set apart as outliers are exactly the ones the Cycle Time process behaviour chart of the same Team or Portfolio and date range marks above its UNPL (its configured baseline included), whether or not that chart is shown on the tab; the outlier list title and the info popover name the upper natural process limit in Terminology words, and while the limit loads or fails no stale count is shown.
```

#### Promises

P1

P4

#### Depends on

S1

#### Slice work

M

```text
The outliers a reader sees agree with the process behaviour chart.
```

##### Rework

None.

### S3

```text
When the Cycle Time process behaviour chart has no usable limits for the range (invalid baseline, such as one shorter than 14 days, or fewer than 2 baseline Work Items), the scatterplot sets no Work Item apart, draws every Work Item, and tells the reader that outliers need the process behaviour chart's limits; it never falls back to 5 x median.
```

#### Promises

P1

P5

#### Depends on

S2

#### Slice work

S

```text
A reader is never shown outliers from a limit that does not exist.
```

##### Rework

None.
