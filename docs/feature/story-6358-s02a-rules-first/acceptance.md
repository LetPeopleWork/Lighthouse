# Acceptance brief: story\-6358\-s02a\-rules\-first

## Slices

### S1

##### Covers

P1

```text
Run the rule that keeps the application core off the API over a made-up core type that uses an API type, once placed under Services and once under Modules
```

```text
The rule reports the made-up type in both places
```

```text
A maintainer who moves a core type into a module is still told when it reaches into the API
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ApplicationCoreRulesReachModulesArchUnitTest\.cs\:\:ApplicationCoreRulesReachModulesArchUnitTest\.ACoreTypeThatReachesTheApi\_IsReportedWhereverTheCoreTypeLives

```text
The core pattern widened to Modules only for types reached through a field, since the made-up type reaches the API through a property and not inside a method body
```

###### Obligations

None.

##### Covers

P1

```text
Run the parent-placement rule with a made-up placement type that uses a core type, the core type once under Services and once under Modules
```

```text
The rule reports the made-up placement type in both cases
```

```text
Deciding under which parent a record hangs stays unable to reach the application core after core types move into modules
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ApplicationCoreRulesReachModulesArchUnitTest\.cs\:\:ApplicationCoreRulesReachModulesArchUnitTest\.AParentPlacementKernelThatReachesTheCore\_IsReportedWhereverTheCoreTypeLives

```text
The real test passing a different kernel list than the default call uses; the oracle only proves the default core pattern, not which kernel types the real test names
```

###### Obligations

None.

##### Covers

P1

```text
Run the retired-Delivery read-path rule with a made-up view type that uses a core type, the core type once under Services and once under Modules
```

```text
The rule reports the made-up view type in both cases
```

```text
The view of a retired Delivery stays unable to reach anything it could recalculate from after core types move into modules
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ApplicationCoreRulesReachModulesArchUnitTest\.cs\:\:ApplicationCoreRulesReachModulesArchUnitTest\.AnArchivedReadPathTypeThatReachesTheCore\_IsReportedWhereverTheCoreTypeLives

```text
The Data or forecast-model targets of the rule dropped while Modules is added; the oracle only adds the Modules target and relies on the existing real-code test for the others
```

###### Obligations

None.

##### Covers

P1

```text
Run the shared-kernel rule with its default core argument over a made-up Model that uses a core type, the core type once under Services and once under Modules
```

```text
The rule reports the made-up Model in both cases
```

```text
The Models stay unable to depend on the application core after core types move into modules
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ApplicationCoreRulesReachModulesArchUnitTest\.cs\:\:ApplicationCoreRulesReachModulesArchUnitTest\.AModelThatReachesTheCore\_IsReportedWhereverTheCoreTypeLives

```text
The update-queue rule left Services-only; it guards a namespace that is not moving in this Story and is outside this oracle
```

###### Obligations

None.

##### Covers

P1

```text
Run the no-service-locator handler rule with its default selection over a made-up domain-event handler that looks up a collaborator, once in the domain-events namespace under Services and once under Modules
```

```text
The rule reports the made-up handler in both places
```

```text
A domain-event handler that moves into a module is still told when it looks up its collaborators
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ApplicationCoreRulesReachModulesArchUnitTest\.cs\:\:ApplicationCoreRulesReachModulesArchUnitTest\.AHandlerThatUsesTheServiceLocator\_IsReportedWhereverItLives

```text
Handlers selected by a namespace that happens to include Modules rather than by implementing the handler interface; the real-code tests below close that by requiring every real handler outside the domain-events namespace to be examined
```

###### Obligations

None.

##### Covers

P1

```text
Run the awaited-save handler rule with its default selection over a made-up domain-event handler that saves through a repository without awaiting, once in the domain-events namespace under Services and once under Modules
```

```text
The rule reports the made-up handler in both places
```

```text
A domain-event handler that moves into a module is still told when it does not await its save
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ApplicationCoreRulesReachModulesArchUnitTest\.cs\:\:ApplicationCoreRulesReachModulesArchUnitTest\.AHandlerThatSavesWithoutAwaiting\_IsReportedWhereverItLives

```text
Handlers selected by a namespace that happens to include Modules rather than by implementing the handler interface; the real-code tests close that
```

###### Obligations

None.

##### Covers

P1

```text
On the real code, run the API rule with its default core argument and with the Services-only pattern
```

```text
Every type the Services-only run looks at is also looked at by the default run
```

```text
Widening the rule never lets a real core type escape it
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ApplicationCoreRulesReachModulesArchUnitTest\.cs\:\:ApplicationCoreRulesReachModulesArchUnitTest\.TheApiRule\_LooksAtEveryRealCoreTypeItLookedAtBefore

```text
Examined widened while the forbidden API target was narrowed; the existing real-code API test and the made-up offenders guard the target
```

###### Obligations

None.

##### Covers

P1

```text
On the real code, run the no-service-locator handler rule with its default selection, with today's domain-events-namespace selection, and list every concrete type implementing the domain-event handler interface
```

```text
The default run looks at every type the namespace selection looked at, the dispatcher included, and at every real handler, including the six outside the domain-events namespace
```

```text
Every real domain-event handler is checked, wherever it lives, and the dispatcher stays checked
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ApplicationCoreRulesReachModulesArchUnitTest\.cs\:\:ApplicationCoreRulesReachModulesArchUnitTest\.TheServiceLocatorRule\_LooksAtEveryRealTypeItLookedAtBeforeAndAtEveryRealHandler

```text
A handler that implements the interface only through a base class; the oracle finds handlers by their directly declared interfaces, which is how every real handler declares it today
```

###### Obligations

None.

##### Covers

P1

```text
On the real code, run the awaited-save handler rule with its default selection, with today's domain-events-namespace selection, and list every concrete type implementing the domain-event handler interface
```

```text
The default run looks at every type the namespace selection looked at and at every real handler, including the six outside the domain-events namespace
```

```text
Every real domain-event handler is checked for awaited saves, wherever it lives
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ApplicationCoreRulesReachModulesArchUnitTest\.cs\:\:ApplicationCoreRulesReachModulesArchUnitTest\.TheAwaitedSaveRule\_LooksAtEveryRealTypeItLookedAtBeforeAndAtEveryRealHandler

```text
A handler that implements the interface only through a base class; the oracle finds handlers by their directly declared interfaces
```

###### Obligations

None.

##### Covers

P3

```text
Run every existing architecture test on the real code after the rules are widened
```

```text
Every architecture test that passed before still passes, with any newly examined real offender named on an inline baseline with its reason
```

```text
The maintainer gets the same architecture guarantees as before and no production behaviour changes
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture \(every test in the Lighthouse\.Backend\.Tests\.Architecture namespace\)

```text
A production file edited in a way no architecture test sees; that part of the promise is checked by the reviewer on the diff, not by a test
```

###### Obligations

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
Every architecture rule that checks or forbids the application core (types under Lighthouse.Backend.Services) applies in the same way to types under Lighthouse.Backend.Modules, so a type moved into a module still fails a rule it would have failed before the move.
```

#### E2

##### Promises

P2

```text
A test shows the order in which domain-event handlers run for TeamDataRefreshed and for PortfolioFeaturesRefreshed, and it fails if a move or registration change reorders them.
```

#### E3

##### Promises

P3

```text
After the change, no production file is edited and every test that existed before still passes.
```

## Charter

None.

## Examination

None.
