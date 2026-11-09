# Acceptance brief: story\-6358\-s02c\-flow\-metrics\-rules

## Slices

### S1

##### Covers

P1

```text
Read the compiled production assembly and list every type outside the Flow metrics module that references a type in the module's Domain
```

```text
The list equals the six listed types: the Portfolio, Portfolios, Team and Teams controllers, the daily series summary of the Metrics reports and the work item service; a seventh type fails and is named with what it reaches, and a listed type that stops reaching the Domain fails and is named with delete
```

```text
A maintainer who adds a direct dependency on the Flow metrics calculations from outside the module, or removes one, sees the architecture test fail and name the type
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundDomainAndAdaptersArchUnitTest\.cs\:\:TheModuleDomain\_IsReachedFromOutsideOnlyByTheListedTypes

```text
A new outside type reaches the Domain only through a member the IL reader does not see, such as a reflection lookup by string name (accepted design limit of reading the IL)
```

###### Obligations


##### Covers

P1

```text
Read the compiled production assembly and list every type outside the Flow metrics module that references a type in the module's Adapters
```

```text
The only such type is the composition root Lighthouse.Backend.Program; any other fails and is named with what it reaches, and a composition root that stops reaching the Adapters fails and is named with delete
```

```text
A maintainer who uses a Flow metrics storage or background adapter directly from outside the module sees the architecture test fail and name the type
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundDomainAndAdaptersArchUnitTest\.cs\:\:TheModuleAdapters\_AreReachedFromOutsideOnlyByTheCompositionRoot

```text
An outside type reaches an adapter only through dependency injection by a runtime-built Type or a reflection lookup by name, which leaves no type reference in the IL (accepted design limit)
```

###### Obligations


##### Covers

P1

```text
Run the Domain rule and the Adapters rule on made-up types where only the allowed outside type reaches the layer, a type inside the made-up module reaches it too, and another outside type reaches only the module's Application
```

```text
Each rule finds exactly the allowed type and passes
```

```text
A maintainer whose code follows the rule sees the architecture test pass; a type inside the module or a caller of the Application is never flagged
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundDomainAndAdaptersArchUnitTest\.cs\:\:OnlyTheAllowedOutsideTypeReachingTheLayer\_PassesTheRule

```text
The rule also counted a type sitting in a namespace that merely starts with the module's name (for example a FlowMetricsX namespace) as inside; no such made-up type exists, and the namespace patterns end at a dot or the end
```

###### Obligations


##### Covers

P1

```text
Run each rule on made-up types where an outside type that is not on the list reaches the layer next to the allowed one
```

```text
The rule fails; the message names the newcomer, and only it, as not on the list, says which layer type it reaches, and asks to delete nothing
```

```text
A maintainer who adds a new outside dependency on the module's Domain or Adapters sees the test fail naming the new type and what it reaches
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundDomainAndAdaptersArchUnitTest\.cs\:\:AnOutsideTypeNotOnTheList\_FailsTheRuleAndIsNamed

```text
The message wording around the names changed while still naming the newcomer and its target; the test pins the shared comparison's sentence, so a reworded sentence fails here as well as in the comparison's own tests
```

###### Obligations


##### Covers

P1

```text
Run each rule on made-up types where an outside type reaches the layer only inside the body of an async method
```

```text
The rule fails and names that type as not on the list, with the layer type it reaches
```

```text
A dependency written only inside an async method is caught like any other
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundDomainAndAdaptersArchUnitTest\.cs\:\:AnOutsideTypeReachingTheLayerOnlyInsideAnAsyncMethod\_FailsTheRuleAndIsNamed

```text
An edge written only inside a lambda in a non-async method was missed; the IL reader walks every compiler-made nested type the same way, but this test only builds the async state machine
```

###### Obligations


##### Covers

P1

```text
Run the Domain rule with a list that also names an outside type that no longer reaches the Domain, and the Adapters rule with a composition root that no longer reaches the Adapters
```

```text
Each rule fails; the message names that entry, and only it, with delete, and names no newcomer
```

```text
A maintainer who removes the last dependency of a listed type is told to delete it from the list, so the dependency cannot come back unseen
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundDomainAndAdaptersArchUnitTest\.cs\:\:AListedTypeThatNoLongerReachesTheLayer\_FailsTheRuleAndIsNamedForDeletion

```text
A listed type was renamed so the list holds a name that no longer exists; that is reported as stale the same way, so the test accepts it, which is the wanted behaviour
```

###### Obligations


##### Covers

P1

```text
Compare a list of found entries with a written list through the shared two-way comparison: equal lists, a newcomer, a stale entry, both at once, reordered and repeated entries, both empty
```

```text
Equal, reordered or repeated lists and two empty lists pass; a newcomer fails named with the fix hint; a stale entry fails named with delete; both at once are named in one failure
```

```text
Every inline list behaves the same: it can only shrink, and its failure says what to add to the code or delete from the list
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs\:\:AListThatMatchesTheCodeExactly\_Passes\, ANewOffenderNotOnTheList\_FailsAndNamesIt\, AListedEntryThatNoLongerOffends\_FailsAndNamesItUntilItIsDeleted\, ANewOffenderAndAStaleEntryAtOnce\_AreBothNamedInOneFailure\, TheOrderOfTheListsAndRepeatedEntries\_DoNotMatter\, NothingOffendingAgainstAnEmptyList\_Passes

```text
The comparison treated names case-insensitively; no case pins two names that differ only by case, and type names are compared ordinally
```

###### Obligations







## Expectations

### Rows

#### E1

##### Promises

P1

```text
Adding a type that reaches into the Flow metrics module from outside its allowed edges, or leaving a listed exception that no longer exists, makes the architecture checks fail and name the type, including when the dependency is used only inside an async method; the module's Domain, Adapters and Application each accept only their listed callers and its Application stays free of EF Core, Data, Adapters and API types.
```

#### E2

##### Promises

P2

```text
The old Metrics type-set rule and the hand-written scanner no longer exist, the Flow metrics module is recognised by its namespaces, and a dependency the old checks would have caught still makes the architecture checks fail.
```

#### E3

##### Promises

P3

```text
The product behaves exactly as before: the full existing test suite passes, the Flow metrics unit tests live under Lighthouse.Backend.Tests/Modules/FlowMetrics with only folder, namespace and using changes, and the database model shows no change.
```

## Charter

None.

## Examination

None.
