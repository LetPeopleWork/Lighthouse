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
