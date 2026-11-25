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
The owner saves Ocean's settings with default sizing off (default size 4, with or without a tracker estimate of 9) and Ocean is refreshed; Feature F has no child Work Items
```

```text
F has 0 remaining and no total, no Team carries work for it, it is not flagged as sized by the default, its size is 0, so no '0 of 4' can appear and forecasts take 0
```

```text
F shows 0 remaining Work Items with no invented count and no Team carrying work; forecasts count it as 0
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DefaultFeatureSizingTest\.cs

```text
A fix that zeroes only the default size but still spreads a tracker estimate across Teams would pass the first case but fail the estimate-9 case
```

###### Obligations



##### Covers

P1

```text
Default sizing is off in Ocean: a Feature with one child Work Item in an override state, a finished childless Feature, and a Portfolio calculating its default from history
```

```text
The child-bearing Feature keeps its real count of 1 and is not flagged as default; the finished Feature stays empty; no historical size is looked up
```

```text
Real child counts are never replaced by a guess, finished Features are untouched and no hidden history lookup happens while off
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DefaultFeatureSizingTest\.cs

```text
Guarding only the childless selector but not the override-state selector would pass the finished and history cases but fail the one-child case
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
The settings form opens with default sizing off, then on; the owner toggles the switch; the form is shown under renamed terms
```

```text
Off: default size 7 field, percentile option and its percentile and history fields, size-estimate field and override states are disabled yet show 7, 85, 90, the mapping and the state; on: enabled with the same values; the switch comes first and toggling changes only the switch; a form of a Portfolio saved before the switch reads on
```

```text
Settings without effect are greyed out and keep their values; the switch says what off means in the user's own terms
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/ProjectSettings\/Advanced\/FeatureSizeComponent\.test\.tsx

```text
Disabling only the default size field but not the mapping or override states would fail the first case; hiding rather than disabling fails every enabled/disabled check
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
The owner switches default sizing off in the settings form and the save is accepted; later saves leave it off; a loaded-off Portfolio is saved or switched on
```

```text
The usage event for switching off is reported exactly once, on the accepted on-to-off save, never on a save that leaves it off, turns it on or leaves it on; the saved settings carry the switch
```

```text
Only a real opt-out is counted, without any detail about the Portfolio
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/ProjectSettings\/ModifyProjectSettings\.usageData\.test\.tsx

```text
Reporting on the click instead of on the accepted save would pass the first case only if the save succeeds; the create path has no declared test locator
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
