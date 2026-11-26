# Acceptance brief: story\-6382\-s09a\-b\-module\-start\-up

## Slices

### S1

##### Covers

P1

```text
Run the composition-root rule over the compiled application for Flow metrics, Forecasting, Promises, Licensing and Access.
```

```text
Each of the five contexts has its own start-up module with one Add method extending the service collection. Only the composition root calls that method. The composition root registers or looks up no type of the context except two kinds, compared both ways: its domain-event handlers in the one ordered block, and the two services it looks up to set the host up (the forecast lane that the start-up banner reads, and the group writer of the identity-provider sign-in). Before the move, the rule fails for each context and names the missing module and every type that the composition root still registers.
```

```text
A maintainer sees for each context that its start-up lives in its own module and that the composition root only calls it.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CompositionRootNamesModulesOnlyThroughTheirModuleArchUnitTest\.cs

EachOfTheFiveContexts\_RegistersThroughTheAddMethodOfItsOwnModule\; EachOfTheFiveContextsModules\_IsStartedByTheCompositionRootAlone\; TheCompositionRoot\_RegistersNoTypeOfTheFiveContexts\_ButTheirDomainEventHandlersAndWhatItLooksUpToSetTheHostUp

```text
A context type registered through a non-generic overload with a typeof argument, or by reflection, stays in the composition root unseen; the review reads the diff for it.
```

###### Obligations



##### Covers

P1

```text
Run the same check over a made-up composition root that registers a made-up context's types itself: with Add, TryAdd and Configure, a registered generic type, a factory that looks a type up inside its lambda, a type of a context whose name begins alike, and a type outside the contexts.
```

```text
Every one of the made-up context's types is reported by its full name, including the types a generic registration is built from and the type looked up inside the lambda; the lookalike context's type and the type outside the contexts are not.
```

```text
A registration left in the composition root, whichever way it is written, is named to the maintainer by its full type name.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CompositionRootNamesModulesOnlyThroughTheirModuleArchUnitTest\.cs

AMadeUpCompositionRootRegisteringAContextsTypes\_IsReportedNamingEachType\_ButNotATypeOfAContextWhoseNameBeginsAlike

```text
A registration that passes the type with typeof to a non-generic overload is not read, so such a made-up registration would go unreported.
```

###### Obligations


##### Covers

P1

```text
Run the module-caller check over made-up types: a made-up Beta module whose Add method starts the Alpha module, and a made-up type elsewhere that starts the Alpha module, beside the made-up composition root that calls it.
```

```text
Exactly the Beta module and the outsider are reported beside the composition root; Alpha's own module and a composition root that does not call the module are not.
```

```text
A second caller of a context's start-up, including another context's module, is named.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CompositionRootNamesModulesOnlyThroughTheirModuleArchUnitTest\.cs

AMadeUpModuleOrOtherTypeStartingAContextsModule\_IsReportedBesideTheCompositionRoot

```text
A caller that reaches the module only by reflection is not an IL reference and would not be reported.
```

###### Obligations


##### Covers

P1

```text
Run all three checks over a made-up composition root that starts the made-up Alpha context only through its module and registers one type outside the contexts.
```

```text
The Alpha module is accepted as well formed, the composition root is found calling it, and no Alpha type is reported as registered by the composition root.
```

```text
A context that starts only through its module passes, so the lists hold no entry nobody needs.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CompositionRootNamesModulesOnlyThroughTheirModuleArchUnitTest\.cs

AMadeUpCompositionRootStartingAContextOnlyThroughItsModule\_IsNotReported

```text
A check that reports less than it should still passes here; the made-up offenders catch that.
```

###### Obligations


##### Covers

P1

```text
Take each allowed entry of the five contexts off its list in turn; compare a made-up composition root with a listed entry it does not register; look for the module of made-up contexts that have none, whose module lacks its Add method, or whose module is not a static class.
```

```text
Each entry taken off is named in addition to what the full list leaves named, and nothing else changes; the unregistered listed entry is named as gone; each missing or malformed module is named with what is wrong with it.
```

```text
Every allowed entry is needed and the lists can only shrink; a malformed start-up module is refused with the reason.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CompositionRootNamesModulesOnlyThroughTheirModuleArchUnitTest\.cs

AnAllowedEntryOfTheCompositionRoot\_IsNamedOnceItIsTakenOffTheList\; AnAllowedEntryAMadeUpCompositionRootDoesNotRegister\_IsReportedAsGone\; AMadeUpContextWithoutAWellFormedStartUpModule\_IsReported

```text
A module whose Add method has the right shape but registers nothing passes these checks; the preservation test catches the missing registrations.
```

###### Obligations


##### Covers

P1

```text
Read the domain-event handlers per event from the start-up registrations after the move.
```

```text
The seven pinned events keep their handlers in the written-out order, with the pin file unchanged.
```

```text
Every reaction to a refresh, a deletion or a forecast update runs in the same order as before.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventHandlerOrderPinTest\.cs

DomainEventHandlerOrderPinTest

```text
Handlers of events outside the seven pinned ones could be reordered unseen; this slice moves no handler line.
```

###### Obligations


##### Covers

P1

```text
Read every type of the ten contexts the composition root names at all, after the last move.
```

```text
Only start-up modules, handler-block entries, order-kept registrations and the measured host set-up types are named; a made-up root naming another type is reported; the listed contexts equal the packaged contexts.
```

```text
The composition root names the contexts only through their modules.
```

##### Disposition

NotTested

```text
This is the third slice's rule over all ten contexts, written from this slice's measure of the names the composition root still uses; its RED depends on the second slice's moves.
```

acceptance designer of the third slice of this feature

##### Covers

P1

```text
Build the start-up service collection without a host before and after the move, and compare per service type the ordered lifetime, key and implementation; plant probes: two background-service lines swapped, one lifetime changed, one line dropped.
```

```text
Before and after the move the container equals the baseline written out on the pre-move revision; each probe fails naming the service type it changed.
```

```text
The application starts with exactly the services, lifetimes and order it had before the move.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/StartUpRegistrationsPreservationTest\.cs

StartUpRegistrationsPreservationTest \(craft target\: the crafter\'s first commit on the pre\-move revision\, green there before any line moves\)

```text
Registrations under branches the Testing environment does not take (stub sign-in off, Redis set), and two factories asking the same services with different constants, are not told apart; the slice review reads those lines for byte equality.
```

###### Obligations





## Expectations

### Rows

#### E1

##### Promises

P1

```text
The architecture-rules probe for the composition-root rule (fragment CompositionRoot) prints passing test lines for the five contexts Flow metrics, Forecasting, Promises, Licensing and Access, each showing that its start-up registrations are made by its own Add<X> method; and ef-model-unchanged reports the data model unchanged. No web host is started to see this.
```

```text
The rule reads compiled code only; it does not show that every service resolves at run time or that registration order inside the container is unchanged, and a passing line says nothing about contexts it does not list.
```

yes

no

None.

#### E2

##### Promises

P2

```text
The architecture-rules probe (fragments CompositionRoot and DomainEventHandlerOrderPin) prints passing test lines for the five contexts Work Items, Tracker integration, Refinement, Reports and Organisation set-up, each registered through its own Add<X> method, and a passing line for the single ordered domain-event handler block; ef-model-unchanged reports the data model unchanged.
```

```text
The handler-order pin proves the handler list and order, not that every handler behaves the same; the container's contents are seen only through the rule, not by resolving services from a running host.
```

yes

no

None.

#### E3

##### Promises

P3

```text
The architecture-rules probe (fragment ProgramNamesModulesOnly) prints a passing line saying Program names only the Add<X> calls, host set-up and the handler block, and the temporary preservation test no longer appears in the backend-tests probe output for its fragment (no such test is found), while the composition-root and handler-order lines still pass.
```

```text
A passing rule line does not show the last green run of the pin was recorded before it was deleted; that belongs to review records, and the probe cannot show Program text or order.
```

yes

no

None.

### Waivers

None.

## Charter

None.

## Examination

None.
