# Acceptance brief: story\-6504\-childless\-feature\-size

## Slices

### S1

##### Covers

P2

P8

```text
Portfolio Ocean (default size 4, two Teams) is refreshed with default sizing on or never touched; childless Feature F, F with a tracker estimate of 9, and F in an override state with one child
```

```text
F has 4 remaining split across its Teams and is flagged as sized by the default; with estimate 9 it has 9; the override-state Feature gets the default
```

```text
With the switch on, nothing changes: Features without child Work Items show the default count with today's warning
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DefaultFeatureSizingTest\.cs

```text
A selector that ignores the switch entirely would still pass these on-cases; the off-cases and the wire table catch it
```

###### Obligations




##### Covers

P1

P3

P4

```text
The owner saves Ocean's settings with default sizing off (default size 4, with or without a tracker estimate of 9), Ocean is refreshed, the forecast runs for Ocean's Features and a Delivery holds childless Feature F
```

```text
F has 0 remaining and no total, no Team carries work for it, it is not flagged as sized by the default, its size is 0; F is forecast to need no days and a Delivery holding only F is expected to complete today, with no special forecast handling
```

```text
F shows 0 remaining Work Items with no invented count and no Team carrying work; forecasts and Deliveries count it as 0
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DefaultFeatureSizingTest\.cs

```text
A fix that zeroes only the default size but still spreads a tracker estimate across Teams passes the first case but fails the estimate-9 case; a fix that skips F in forecasts instead of counting 0 is not told apart from counting 0
```

###### Obligations



##### Covers

P1

```text
Default sizing is off in Ocean: a finished childless Feature, and a childless Feature in a Portfolio that calculates its default from history
```

```text
The finished Feature stays empty; the childless Feature counts as 0 with no work for any Team although a historical size exists
```

```text
Finished Features are untouched and a calculated default is never applied while off
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DefaultFeatureSizingTest\.cs

```text
Guarding only the explicit default size but not the percentile-calculated default passes the finished case but fails the history case
```

###### Obligations



##### Covers

P7

```text
The owner saves Ocean's settings with default size 7, an override state and a size-estimate field mapped, with default sizing off and then on again
```

```text
After each save the settings read back carry default size 7, the same override state, the same field mapping, and the switch as saved
```

```text
Switching off and on again loses none of the stored values
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DefaultFeatureSizingTest\.cs

```text
A switch-off that clears the stored values on the frontend only is not seen here; the component test pins that it changes only the switch
```

###### Obligations


##### Covers

P7

P8

```text
The settings form opens with default sizing off, then on; a Portfolio saved before the switch existed (field absent) is opened; the owner toggles the switch; the form is shown under renamed terms
```

```text
Off: default size field, percentile option and its percentile and history fields, size-estimate field and override states are disabled yet show 7, 85, 90, the mapping and the state; on: enabled with the same values; the switch comes first and toggling changes only the switch; an absent field reads on with everything enabled
```

```text
Settings without effect are greyed out and keep their values; the switch says what off means in the user's own terms; older Portfolios read on
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/ProjectSettings\/Advanced\/FeatureSizeComponent\.test\.tsx

```text
Disabling only the default size field but not the mapping or override states fails the first case; hiding rather than disabling fails every enabled/disabled check
```

###### Obligations





##### Covers

P8

```text
A payload from an older client is saved on a new Portfolio and on one that held the switch on or off; explicit true and false payloads are saved too
```

```text
A new Portfolio reads on; an absent field keeps the stored value; an explicit value moves it
```

```text
Existing and new Portfolios start with the switch on and an older script or client can never turn it off
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DTO\/PortfolioSettingDtoCompatibilityTest\.cs

```text
A plain bool that defaults to false would fail the absent cases; a column default of true in the database is not observable here (no declared migration locator)
```

###### Obligations



##### Covers

P1

```text
The owner switches default sizing off in the settings form and the save is accepted; a save is rejected after switching off and a later save is accepted; later saves leave it off; a loaded-off Portfolio is saved or switched on
```

```text
The usage event for switching off is reported exactly once, on the accepted on-to-off save only: none for a rejected save, none for a save that leaves it off, turns it on or leaves it on; the saved settings carry the switch
```

```text
Only a real opt-out is counted, without any detail about the Portfolio
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/ProjectSettings\/ModifyProjectSettings\.usageData\.test\.tsx

```text
Reporting on the click instead of on the accepted save fails the rejected-save case; the create path has no declared test locator
```

###### Obligations

None.

### S2

##### Covers

P6

```text
Portfolio Ocean (default size 4, override state In Progress, default sizing off) is refreshed with Feature G In Progress holding 1, 4 or 6 child Work Items
```

```text
G keeps its real child count as remaining and total, only the owning Team carries it, G has that size and is not marked as using the default size
```

```text
The owner sees G with its real child count, not the default size 4
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DefaultFeatureSizingOverrideStatesTest\.cs\:\:A\_Feature\_in\_an\_override\_state\_keeps\_its\_real\_child\_count\_while\_default\_sizing\_is\_off

```text
An override rule that ignores the switch but only differs when real count is below the default (counts 4 and 6 pass); or one that keeps the count but still flags G as using the default
```

###### Obligations


##### Covers

P6

```text
Portfolio Ocean (default size 4, override state In Progress, default sizing on) is refreshed with Feature G In Progress holding 1 child Work Item
```

```text
G is re-sized to the default 4 and marked as using the default size, as today
```

```text
With default sizing on the owner still sees today's default-sized count
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DefaultFeatureSizingOverrideStatesTest\.cs\:\:A\_Feature\_in\_an\_override\_state\_with\_one\_child\_is\_still\_sized\_by\_the\_default\_while\_default\_sizing\_is\_on

```text
An override rule that never applies the default even when the switch is on
```

###### Obligations


### S3

##### Covers

P5

```text
A refreshed Feature with no child Work Items, a Feature that is finished, one sized by the default and one with real child Work Items are sent to the Feature list and the Feature detail
```

```text
Only the refreshed childless Feature, not finished and not sized by the default, says it has no child Work Items yet; all other cases say it does not
```

```text
The list and timeline can tell a Feature with no child Work Items from one with real counts, so the note never claims a false cause
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DTO\/FeatureHasNoChildWorkItemsYetTest\.cs

```text
A payload that says no child Work Items yet whenever total work is 0, or that leaves the field out and so reads as false for every Feature
```

###### Obligations

None.

##### Covers

P5

```text
The Portfolio Feature list shows a refreshed Feature with no child Work Items while default Feature sizing is off, under a Feature list under a tenant that renamed Feature, Portfolio and Work Items
```

```text
The row's warning icon and tooltip say exactly the note in the tenant's own words (Tasks, Bet, Program) and no default-size sentence
```

```text
The Portfolio owner reads why a Feature shows 0 instead of mistaking it for finished
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioFeatureList\.test\.tsx \:\: with default sizing off\, the childless row says it has no child Work Items yet and no default\-size sentence

```text
A note with the default words Work Items, Feature or Portfolio instead of the tenant's terms, or the note shown beside the default-size sentence
```

###### Obligations

None.

##### Covers

P5

```text
The Feature list shows a Feature with real child counts
```

```text
No warning
```

```text
A Feature with real counts is not marked
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioFeatureList\.test\.tsx \:\: a Feature with real counts shows no warning

```text
A note shown on every row
```

###### Obligations

None.

##### Covers

P5

```text
The Feature list shows a Feature with no work counted but not flagged
```

```text
No warning
```

```text
Only the refreshed childless Feature is marked, never one that merely counts 0
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioFeatureList\.test\.tsx \:\: a Feature with no work counted but not flagged shows no warning

```text
A note keyed on total work alone
```

###### Obligations

None.

##### Covers

P5

```text
The Feature list shows a Feature counted by the default size, default sizing on
```

```text
The default-size sentence only, no note
```

```text
With the switch on nothing changes for the Portfolio owner
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioFeatureList\.test\.tsx \:\: with default sizing on\, a Feature counted by the default size keeps its default\-size sentence only

```text
The default-size sentence replaced by the note, or both shown
```

###### Obligations

None.

##### Covers

P5

```text
The Feature list shows a default-sized Feature with no work counted
```

```text
Exactly the default-size sentence, never the note
```

```text
A guessed count is never labelled as a missing child count
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioFeatureList\.test\.tsx \:\: a default\-sized Feature with no work counted gets the default\-size sentence and never the note

```text
A note keyed on total work alone shown beside or instead of the default-size sentence
```

###### Obligations

None.

##### Covers

P5

```text
The sentence owner is asked for a childless Feature
```

```text
The note alone
```

```text
Every place that lists a Feature's warnings says the same about it
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioFeatureList\.test\.tsx \:\: the sentence owner gives the note alone for a childless Feature

```text
The note missing, or built from the default-size flag
```

###### Obligations

None.

##### Covers

P5

```text
The sentence owner is asked for a default-sized Feature
```

```text
The default-size sentence alone
```

```text
Today's warning is unchanged
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioFeatureList\.test\.tsx \:\: the sentence owner gives the default\-size sentence alone for a default\-sized Feature

```text
The note appended to the default-size sentence
```

###### Obligations

None.

##### Covers

P5

```text
The sentence owner is asked for a Feature that is neither
```

```text
No sentence
```

```text
A clean Feature stays clean
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioFeatureList\.test\.tsx \:\: the sentence owner gives nothing for a Feature that is neither

```text
The note given for any zero-work Feature
```

###### Obligations

None.

##### Covers

P5

```text
The Delivery timeline reads a childless Feature with warnings on
```

```text
The bar carries the note as a warning
```

```text
The owner sees the note on the timeline bar where today's warning sits
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioFeatureList\.test\.tsx \:\: the timeline bar carries the note as a warning

```text
A bar note keyed on size 0, or not reaching the bar
```

###### Obligations

None.

##### Covers

P5

```text
The Delivery timeline reads a Feature with no work counted but not flagged
```

```text
No mark on the bar
```

```text
No false note on the timeline
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioFeatureList\.test\.tsx \:\: the timeline leaves a Feature with no work counted but not flagged unmarked

```text
A bar note keyed on size 0
```

###### Obligations

None.

##### Covers

P5

```text
The Delivery's Work Items warnings column is asked about a childless Feature
```

```text
It lists the note
```

```text
The same note appears in the Delivery's Work Items list
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioFeatureList\.test\.tsx \:\: the Delivery\'s Work Items warnings column lists the note

```text
A note that reaches the bar but not the column
```

###### Obligations

None.

##### Covers

P5

```text
The Delivery is asked whether it has anything to warn about, for a childless Feature
```

```text
Yes
```

```text
The warnings control is offered when the note applies
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioFeatureList\.test\.tsx \:\: the Delivery has something to warn about when a Feature has the note

```text
A check that ignores the note
```

###### Obligations

None.

##### Covers

P5

```text
A Delivery containing a childless Feature is exported
```

```text
The Warnings cell says Yes
```

```text
An exported report does not show a childless Feature as clean
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioFeatureList\.test\.tsx \:\: the exported table marks the childless Feature as having a warning

```text
An export that reads only the default-size flag
```

###### Obligations

None.

##### Covers

P5

```text
A Feature payload arrives with hasNoChildWorkItemsYet true
```

```text
The decoded Feature reads true
```

```text
A Feature waiting for child Work Items is marked
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioFeatureList\.test\.tsx \:\: reads hasNoChildWorkItemsYet true as true

```text
A decoder that drops the field and so reads false for every payload
```

###### Obligations

None.

##### Covers

P5

```text
A Feature payload arrives with hasNoChildWorkItemsYet false
```

```text
The decoded Feature reads false
```

```text
A Feature with child Work Items is not marked
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioFeatureList\.test\.tsx \:\: reads hasNoChildWorkItemsYet false as false

```text
A decoder that reads true for every payload
```

###### Obligations

None.

##### Covers

P5

```text
A Feature payload arrives without hasNoChildWorkItemsYet
```

```text
The decoded Feature reads false
```

```text
An older payload never shows the note
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioFeatureList\.test\.tsx \:\: reads an absent hasNoChildWorkItemsYet as false

```text
A schema that requires the field or defaults it to true
```

###### Obligations

None.

##### Covers

P5

```text
A Portfolio is refreshed with default sizing off, then with it on, holding a Feature without child Work Items
```

```text
Off: the Feature payload and the list row say hasNoChildWorkItemsYet true; on: both say false
```

```text
Switching default sizing off is what makes the Feature say it has no child Work Items yet
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DTO\/FeatureHasNoChildWorkItemsYetTest\.cs \:\: A\_refreshed\_Feature\_without\_child\_Work\_Items\_says\_so\_only\_while\_default\_sizing\_is\_off

```text
A flag set from the Feature alone, regardless of the Portfolio switch, or never true after a refresh
```

###### Obligations

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
With default sizing switched off, a Feature with no child Work Items is observed as size 0, with no invented count and no remaining work assigned to any Team; with default sizing on, behaviour is unchanged from today.
```

#### E2

##### Promises

P2

```text
With default sizing on and Default Feature Size 4, a Feature without child Work Items shows 4 remaining Work Items split across its Teams, with the existing default-size warning.
```

#### E3

##### Promises

P3

```text
With default sizing off, a Feature without child Work Items counts as 0 remaining in its own forecast and in a containing Delivery forecast, with no special forecast wording or handling visible.
```

#### E4

##### Promises

P4

```text
With default sizing off and Default Feature Size 4, a Feature without child Work Items shows 0 remaining, no count such as 0 of 4 appears, and no Team carries remaining work for it.
```

#### E5

##### Promises

P5

```text
With default sizing off, a Feature without child Work Items shows the note No child Work Items yet where the default-size warning icon sits, both in the Feature list and on the Delivery timeline, and no default-Feature-size sentence appears.
```

#### E6

##### Promises

P6

```text
With default sizing off, a Feature in an override state such as In Progress shows its real child count of 1, not 4, and is not marked as using the default size.
```

#### E7

##### Promises

P7

```text
With default sizing off, Default Feature Size, the percentile option, the size-estimate field mapping and the override-states selection are greyed out and cannot be changed; switching sizing back on shows 7, the same mapping and the same states.
```

#### E8

##### Promises

P8

```text
Portfolios that existed before this change show default sizing switched on next to Default Feature Size, and a newly created Portfolio does the same, with Feature counts unchanged from today.
```

## Charter

None.

## Examination

None.
