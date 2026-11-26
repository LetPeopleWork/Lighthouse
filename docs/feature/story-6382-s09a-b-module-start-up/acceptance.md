# Acceptance brief: story\-6382\-s09a\-b\-module\-start\-up

## Slices

### S1

##### Covers

P1

```text
Run the composition-root rule over the compiled application for Flow metrics, Forecasting, Promises, Licensing and Access.
```

```text
Each of the five contexts has its own start-up module with one Add method that extends the service collection, and only the composition root calls it. Registrations and lookups are compared, each with its own list and both ways. The composition root may register only two kinds of the context's types: its domain-event handlers of the one ordered block, and the cross-context wiring it keeps. That wiring is a statement that names types of two contexts: for Flow metrics, the throughput filter, the data-sufficiency and how-many forecaster services; for Promises, the block renderer. The composition root may look up only three kinds of the context's types: what the handler block forwards to, the two services it needs to set the host up (the forecast lane and the identity-provider group writer), and the cross-context wiring's Forecasting types (the forecast filter rule service and the Forecasting answers to Flow metrics). Before the move, the rule fails for each context. The failure names the missing module and every type that is still registered or looked up.
```

```text
A maintainer sees for each context that its start-up lives in its own module and that the composition root only calls it.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CompositionRootNamesModulesOnlyThroughTheirModuleArchUnitTest\.cs

EachOfTheFiveContexts\_RegistersThroughTheAddMethodOfItsOwnModule\; EachOfTheFiveContextsModules\_IsStartedByTheCompositionRootAlone\; TheCompositionRoot\_RegistersNoTypeOfTheFiveContexts\_ButTheirDomainEventHandlersAndWhatItLooksUpToSetTheHostUp

```text
A context type that the composition root reaches only by reflection on a type name string stays unseen. The review reads the diff for it.
```

###### Obligations



##### Covers

P1

```text
Run the same check over a made-up composition root that registers a made-up context's types itself in every form: Add, TryAdd and Configure; a hosted service; a typed HTTP client with its message handler; a typed client of a named HTTP client; a keyed service; typeof through a non-generic overload; a generic service descriptor and a descriptor built with typeof; a registered generic type; and a factory that looks a type up inside its lambda. The root also registers a type of a context whose name begins alike, and a type outside the contexts.
```

```text
Every registered type of the made-up context is reported by its full name. The type looked up inside the lambda is reported as a lookup, not as a registration. The lookalike context's type and the type outside the contexts are not reported.
```

```text
A registration left in the composition root is named to the maintainer by its full type name, whichever way it is written.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CompositionRootNamesModulesOnlyThroughTheirModuleArchUnitTest\.cs

AMadeUpCompositionRootRegisteringAContextsTypes\_IsReportedNamingEachTypeForEveryWayOfRegistering\_ButNotATypeOfAContextWhoseNameBeginsAlike

```text
A registration made through an extension method of another library that takes no generic argument and no typeof, for example by assembly scanning, would go unreported.
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
Take each allowed entry of the five contexts off its own list (registrations or lookups) in turn. Compare a made-up composition root with a listed registration and a listed lookup that it does not make. Look for the module of made-up contexts that have none, whose module lacks its Add method, or whose module is not a static class.
```

```text
Each entry taken off is named in addition to what the full lists leave named, on its own side only, and nothing else changes. The registration and the lookup that are not made are named as gone. Each missing or malformed module is named with what is wrong with it.
```

```text
Every allowed entry is needed and the lists can only shrink. A malformed start-up module is refused with the reason.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CompositionRootNamesModulesOnlyThroughTheirModuleArchUnitTest\.cs

AnAllowedEntryOfTheCompositionRoot\_IsNamedOnceItIsTakenOffItsList\; AnAllowedEntryAMadeUpCompositionRootDoesNotRegisterOrLookUp\_IsReportedAsGone\; AMadeUpContextWithoutAWellFormedStartUpModule\_IsReported

```text
A module whose Add method has the right shape but registers nothing passes these checks. The preservation test catches the missing registrations.
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





##### Covers

P1

```text
Run the check over a made-up composition root that registers a service whose name is on the lookup list only, and looks that service up; then over one that only looks it up.
```

```text
The first root is reported for registering the service, and its lookup is not reported. The second root is not reported at all.
```

```text
A service that the host may look up to set itself up must still be registered by its own module.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CompositionRootNamesModulesOnlyThroughTheirModuleArchUnitTest\.cs

AMadeUpCompositionRootRegisteringAServiceListedOnlyAsLookedUp\_IsReported\; AMadeUpCompositionRootOnlyLookingUpAServiceListedAsLookedUp\_IsNotReported

```text
A host set-up lookup listed for one context does not excuse the same service when another context's list is read. Each context has its own lists.
```

###### Obligations


##### Covers

P1

```text
Run the check over a made-up composition root that looks up a made-up context's types with GetServices, a required keyed lookup, a keyed lookup and the activator utilities, and looks up a lookalike context's type with GetService.
```

```text
Exactly the four looked-up types of the made-up context are reported as lookups. The lookalike context's type is not reported, and nothing is reported as registered.
```

```text
A composition root that reaches into a context through a lookup is named, whichever lookup it uses.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CompositionRootNamesModulesOnlyThroughTheirModuleArchUnitTest\.cs

AMadeUpCompositionRootLookingUpAContextsTypes\_IsReportedNamingEachTypeForEveryWayOfLookingUp

```text
A non-generic lookup that passes the type with typeof is read as a registration, not a lookup. It is still reported, but on the registration side.
```

###### Obligations


##### Covers

P1

```text
Run the check over a made-up composition root that keeps one statement registering a made-up context's service built from a type of another made-up context, first with that registration listed, then with it taken off the list.
```

```text
While the registration is listed, nothing is reported. Once it is taken off, exactly that registration is reported, and the other context's looked-up type is not counted for the first context.
```

```text
Cross-context wiring stays in the composition root only by name, and an unlisted statement is named.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CompositionRootNamesModulesOnlyThroughTheirModuleArchUnitTest\.cs

AMadeUpCrossContextWiringLine\_PassesWhileListed\_AndIsNamedOnceItIsTakenOffTheList

```text
A statement listed as cross-context wiring that in fact names only one context still passes. The slice review checks each listed statement against its line.
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
