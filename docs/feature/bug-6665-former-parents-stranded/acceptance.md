# Acceptance brief: bug\-6665\-former\-parents\-stranded

## Slices

### S1

##### Covers

P1

P2

P6

```text
A store already holds a Team, a Portfolio with one Feature with remaining work, and a former parent item in no Portfolio that no Feature names as its parent, with remaining work for the Team and a forecast; the clean-up that every refresh and every start runs is run
```

```text
Only the Portfolio Feature is stored; the Team's Features tab and the Team's forecast set show only it; its remaining work is unchanged; the former item's remaining work and forecast are gone
```

```text
The Team lead no longer sees the stranded item on the Team's Features tab, and the Team's forecasts no longer give it throughput, after the next refresh or start, with no migration
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/OrphanedFeatureCleanupServiceTests\.cs\:\:AFormerFeatureInNoPortfolioThatIsNoOnesParent\_IsDeletedWithItsRemainingWorkAndForecast\_AndLeavesTheTeamsFeaturesTab

```text
the stranded item kept with only its remaining work deleted is caught by the stored-Features check; a clean-up that also deletes the Portfolio Feature is caught by every check
```

###### Obligations




##### Covers

P1

```text
A Feature that was never a parent is in no Portfolio, with remaining work and a forecast; the clean-up runs
```

```text
The Feature, its remaining work and its forecast are gone
```

```text
A Feature that left every Portfolio does not stay on the Team's Features tab
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/OrphanedFeatureCleanupServiceTests\.cs\:\:AFeatureInNoPortfolioThatWasNeverAParent\_IsDeletedWithItsRemainingWorkAndForecast

```text
a rule that deletes every Feature outside a Portfolio, parents included; the parent-kept scenario catches that
```

###### Obligations


##### Covers

P1

```text
A parent item flagged as a parent is in no Portfolio, and a Feature in a Portfolio names it as its parent; the clean-up runs
```

```text
Both the parent item and the Portfolio Feature are kept
```

```text
A Portfolio Feature still shows its parent's name
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/OrphanedFeatureCleanupServiceTests\.cs\:\:AParentItemThatAFeatureInAPortfolioStillNamesAsItsParent\_IsKept

```text
today's rule that keeps every flagged item; the only-child-outside and stranded-item scenarios catch that
```

###### Obligations


##### Covers

P1

P2

```text
A flagged parent item is in no Portfolio; its only child is in no Portfolio; a Feature in a Portfolio names a different parent; the clean-up runs
```

```text
Only the Portfolio Feature is stored and on the Team's Features tab; the parent item and its child are gone
```

```text
A parent whose children all left every Portfolio no longer shows on the Team's Features tab
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/OrphanedFeatureCleanupServiceTests\.cs\:\:AParentItemWhoseOnlyChildIsInNoPortfolio\_IsDeletedWithThatChild

```text
a rule that keeps a flagged item whenever any stored Feature, in a Portfolio or not, names it as parent
```

###### Obligations


##### Covers

P6

```text
A store with a Portfolio Feature and a Feature in no Portfolio is cleaned once, then the clean-up runs again
```

```text
The second run deletes nothing and the stored Features and their remaining work are unchanged
```

```text
Repeated refreshes and starts never take more Features away
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/OrphanedFeatureCleanupServiceTests\.cs\:\:ASecondCleanupRightAfterTheFirst\_DeletesNothingAndChangesNothing

```text
a first run that deletes too little; the keep rule itself is pinned by the other scenarios
```

###### Obligations


##### Covers

P3

```text
A Portfolio holds a Feature whose parent is a flagged item in no Portfolio; another Portfolio holds an unrelated Feature; the first Portfolio is removed and saved and its deletion event is handled by the Work Items reaction
```

```text
Only the other Portfolio's Feature is stored and on the Team's Features tab; the deleted Portfolio's Feature and its former parent are gone at once
```

```text
After deleting a Portfolio, the Team lead no longer sees its Features or their parents on the Team's Features tab, without waiting for a refresh or restart
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/Policies\/PortfolioDeletedOrphanedFeatureCleanupHandlerTest\.cs\:\:DeletingAPortfolio\_RemovesItsFeatureAndThatFeaturesFormerParentFromTheTeamsFeaturesTabAtOnce

```text
a reaction that works but that the controller never publishes to; registration is pinned by the handler-order test, the publish is not observed here
```

###### Obligations


##### Covers

P3

```text
A flagged parent item is only in the Portfolio being deleted and a Feature in another Portfolio names it as its parent; the Portfolio is removed and saved and its deletion event is handled
```

```text
The parent item and its child are both still stored
```

```text
Deleting a Portfolio does not take away a parent that a Feature in another Portfolio still shows
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/Policies\/PortfolioDeletedOrphanedFeatureCleanupHandlerTest\.cs\:\:DeletingAPortfolio\_KeepsAParentItemThatAFeatureInAnotherPortfolioStillNamesAsItsParent

```text
a Portfolio removal that keeps deleting its sole-Portfolio Features but spares flagged ones
```

###### Obligations


##### Covers

P3

```text
The start-up registrations and the compiled product are read
```

```text
PortfolioDeleted has exactly the orphaned-Feature clean-up reaction as its handler; the reaction is listed as a Work Items policy in the membership, layout, boundary and test-placement lists, with its one edge to the clean-up service
```

```text
A Portfolio deletion reaches the clean-up in the running product
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventHandlerOrderPinTest\.cs\:\:HandlersOfAPortfolioDeletion\_AreTheOrphanedFeatureCleanupAlone

```text
the controller never publishing the event after the save
```

###### Obligations


##### Covers

P3

```text
A Portfolio is deleted through the Portfolio delete action; its queued delete runs
```

```text
The Portfolio is removed, the removal is saved, and then a Portfolio-deleted event carrying that Portfolio's id is published, in that order
```

```text
Deleting a Portfolio in the app triggers the clean-up of its Features at once
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfolioControllerTest\.cs\:\:DeletePortfolio\_PublishesPortfolioDeletedForThatPortfolioAfterTheSave

```text
an event published with the right id but by another publisher path than the queued delete, which the dispatcher would still route the same way
```

###### Obligations


### S2

##### Covers

P1

P4

```text
A Team and another Team, one Portfolio. A former Feature, stored as a parent item, is in no Portfolio and has remaining work for both Teams and a forecast. A real Feature in the Portfolio is ranked below it, and another Feature in the Portfolio names the former Feature as its parent. The clean-up runs.
```

```text
The former Feature is still stored and its child still shows its name as parent. It has no remaining work left for any Team, and the work of the two Portfolio Features is unchanged. Neither Team's Features tab shows it, and the forecasts of the two Teams take in only the two Portfolio Features, the real Feature first.
```

```text
A Team lead sees only Features that belong to a Portfolio on the Team's Features tab, the Feature's page still shows its parent's name, and the forecasts of the real Features are no longer pushed later by a stranded item.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/OrphanedFeatureCleanupServiceTests\.cs\:\:AFormerFeatureKeptOnlyAsTheParentOfAFeatureInAPortfolio\_LosesItsRemainingWork\_SoNoTeamShowsItAndTheFeaturesBelowItMoveUpInTheForecasts

```text
A clean-up that removes the remaining work of every Feature outside every Portfolio but also the forecasts of the kept parent; forecast rows of a kept parent are left unasserted here because the design keeps them and this slice does not pin them.
```

###### Obligations




### S3

##### Covers

P5

```text
A Portfolio holds a Feature whose parent is a former Feature in no Portfolio, never stored as a parent item, with two state changes and one forecast; the clean-up runs three times
```

```text
After every run the parent is still stored with the same Id, its two state changes and its forecast; the second and third runs delete nothing
```

```text
The parent of a Portfolio Feature keeps its history and forecasts across refreshes instead of being deleted and stored again
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/OrphanedFeatureCleanupServiceTests\.cs\:\:AFormerFeatureThatAFeatureInAPortfolioNamesAsItsParent\_StaysTheSameItemWithItsHistoryAndForecastOverRepeatedCleanups

```text
a separate step deleted only the parent's start forecasts, which the scenario does not seed; its when-forecast and state changes are pinned
```

###### Obligations



## Expectations

### Rows

#### E1

##### Promises

P1

```text
A Feature that has left every Portfolio is not listed on any Team's Features tab and is not part of any forecast, unless it still parents a Portfolio Feature; then it appears only as that parent's name.
```

#### E2

##### Promises

P2

```text
After a Feature with remaining work for a Team leaves its only Portfolio query and the Portfolio refreshes, and it parents no Portfolio Feature, it is gone: absent from the Team's Features tab and from the forecast order.
```

#### E3

##### Promises

P3

```text
When a Portfolio is deleted, its Features that are in no other Portfolio and parent no Portfolio Feature vanish from Team Features tabs immediately, with no further refresh or restart.
```

#### E4

##### Promises

P4

```text
A former Feature that still parents a Feature in another Portfolio keeps showing as that child's parent name, but is on no Team's Features tab and in no forecast, so Features ranked below it are no longer pushed later.
```

#### E5

##### Promises

P5

```text
A non-Portfolio parent of a Portfolio Feature stays the same item across repeated Portfolio refreshes and clean-ups: never deleted and re-created, its history and forecasts remain.
```

#### E6

##### Promises

P6

```text
An existing instance already holding a no-Portfolio, non-parent Feature with old progress and forecasts has it removed by the next Portfolio refresh or start, with no manual data migration step.
```

## Charter

None.

## Examination

### E1

met

#### Observations

```text
Probe backend-tests OrphanedFeatureCleanup: 16/16 pass, incl. former Feature no-one's-parent deleted with work+forecast and leaves Team tab; former parent kept only as name loses FeatureWork (features below move up); parent same item over repeated cleanups with history; second cleanup returns nothing.
```

```text
Probes PortfolioDeleted (10/10) and DeletePortfolio (9/9): deleting a Portfolio removes its Feature and former parent from Team Features tab at once; parent still named by Feature in another Portfolio is kept; PortfolioDeleted published after save; Update-after-refresh invokes cleanup (1/1).
```

### E2

met

#### Observations

```text
Probe backend-tests OrphanedFeatureCleanup: 16/16 pass, incl. former Feature no-one's-parent deleted with work+forecast and leaves Team tab; former parent kept only as name loses FeatureWork (features below move up); parent same item over repeated cleanups with history; second cleanup returns nothing.
```

### E3

met

#### Observations

```text
Probes PortfolioDeleted (10/10) and DeletePortfolio (9/9): deleting a Portfolio removes its Feature and former parent from Team Features tab at once; parent still named by Feature in another Portfolio is kept; PortfolioDeleted published after save; Update-after-refresh invokes cleanup (1/1).
```

### E4

met

#### Observations

```text
Probe backend-tests OrphanedFeatureCleanup: 16/16 pass, incl. former Feature no-one's-parent deleted with work+forecast and leaves Team tab; former parent kept only as name loses FeatureWork (features below move up); parent same item over repeated cleanups with history; second cleanup returns nothing.
```

### E5

met

#### Observations

```text
Probe backend-tests OrphanedFeatureCleanup: 16/16 pass, incl. former Feature no-one's-parent deleted with work+forecast and leaves Team tab; former parent kept only as name loses FeatureWork (features below move up); parent same item over repeated cleanups with history; second cleanup returns nothing.
```

### E6

met

#### Observations

```text
Probe backend-tests OrphanedFeatureCleanup: 16/16 pass, incl. former Feature no-one's-parent deleted with work+forecast and leaves Team tab; former parent kept only as name loses FeatureWork (features below move up); parent same item over repeated cleanups with history; second cleanup returns nothing.
```

```text
Start-time cleanup not directly observed (Startup probe 116 pass, none named cleanup). Refresh-triggered cleanup observed on seeded stale data; no migration step seen. Probes are tests, not an installed host.
```
