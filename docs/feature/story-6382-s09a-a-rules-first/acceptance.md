# Acceptance brief: story\-6382\-s09a\-a\-rules\-first

## Slices

### S1

##### Covers

P1

```text
Each published type on the membership map is looked up in the compiled code
```

```text
Each type is found as a top-level type in exactly one of its old namespace and its context's Contracts namespace; one test line per type names its context, both namespaces and whether it is a seed or a closure member
```

```text
A reviewer sees what each context publishes listed one line per type, measured from the compiled code
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsMembershipArchUnitTest\.cs

ThePublishedType\_IsFoundAsATopLevelTypeInExactlyItsOldOrItsContractsNamespace

```text
a map entry naming the right type under a wrong old namespace fails here only if the type is absent in both places; the closure test catches a wrong context
```

###### Obligations


##### Covers

P1

```text
The published set of the ten contexts is measured from the compiled code (shapes another context reaches, the events, and what their public signatures name) and compared with the map; a made-up pair of contexts lists a calculator as published and leaves out a record its query returns
```

```text
The production map equals the measurement both ways, with the seven reached behaviour classes listed as not published; for the made-up pair the check names exactly the published calculator and the missing record
```

```text
A reviewer can trust the published set is complete and holds no algorithm
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsMembershipArchUnitTest\.cs

ThePublishedSetOfEveryContext\_EqualsTheClosureMeasuredFromTheCompiledCode\_BothWays\; TheClosureCheck\_NamesASeedThatHoldsBehaviour\_AndAClosureMemberMissingFromTheMap

```text
a type reached only through a string or reflection, or a constant inlined by the compiler, leaves no edge in the compiled code and is not measured
```

###### Obligations


##### Covers

P1

```text
Every regex field of the test assembly that sees a published type is evaluated on the old and the Contracts name of every published type, one test line per declared field with its role; a made-up set of fields holds an undeclared one, a front door not widened, a widened one, an old location seeing Contracts and one keeping off it
```

```text
Today the nine front doors written as Modules.X.Application fail as no longer seeing their context's published types; after they read Modules.X.(Application|Contracts) every field keeps its role; for the made-up set exactly the undeclared field, the unwidened front door and the old location seeing Contracts are named
```

```text
A reviewer sees every rule pattern classed front door or old location, and the front doors widened to Application or Contracts
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsMembershipArchUnitTest\.cs

TheRulePattern\_KeepsItsDeclaredRole\_WhenThePublishedTypesMoveIntoContracts\; EveryRulePatternThatSeesAPublishedType\_IsDeclared\_AndEveryDeclaredPatternIsARulePattern\; TheRoleCheck\_NamesAnUndeclaredPattern\_AFrontDoorNotWidened\_AndAnOldLocationSeeingTheContractsName

```text
a pattern held in a C# property, not a field, is not read by the law
```

###### Obligations


##### Covers

P1

```text
Every string literal of the test project is read for a published type's old full name or file path, written out or inside a grouped alternation; a made-up file holds one of each, a half-renamed entry and a listed entry found nowhere
```

```text
The production strings equal the fixed rename list of 43 entries, each file holding one form; for the made-up file the written-out and the grouped old names, the half-renamed entry and the absent entry are each named
```

```text
No test string keeps naming a published type by its old place after the move
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsMembershipArchUnitTest\.cs

EveryTestStringThatNamesAPublishedTypeByItsOldName\_WrittenOutOrGrouped\_IsOnTheRenameList\_InExactlyOneOfItsForms\; TheRenameListCheck\_NamesAWrittenOutAndAGroupedOldName\_AListedStringFoundInNeitherForm\_AndOneFoundInBoth

```text
a name built at run time from separate parts is not seen by the sweep
```

###### Obligations


##### Covers

P1

```text
The whole architecture test namespace runs before and after the front doors are widened
```

```text
Every existing architecture rule keeps its verdict; the only failures before widening are the nine front-door lines
```

```text
No rule verdict changes while the rules learn about Contracts
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture

Lighthouse\.Backend\.Tests\.Architecture namespace

```text
a rule that matches nothing passes on both runs
```

###### Obligations


##### Covers

P1

```text
The membership, role and rename checks run against the compiled code and the test project before any type moves
```

```text
Every published type, every rule field that sees one and every old-name string is accounted for, so a rule keeps its role when the type moves into its context's Contracts
```

```text
Later moves into Contracts happen under rules that keep seeing the moved types
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsMembershipArchUnitTest\.cs

ContextContractsMembershipArchUnitTest

```text
a type reached only by reflection or a string is not measured
```

###### Obligations


##### Covers

P1

```text
The whole architecture test namespace runs before and after the front doors are widened
```

```text
Every existing architecture rule keeps its verdict
```

```text
No rule verdict changes while the rules learn about Contracts
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture

Lighthouse\.Backend\.Tests\.Architecture namespace

```text
a rule that matches nothing passes on both runs
```

###### Obligations


### S2

##### Covers

P2

```text
The contexts the compiled backend holds are read from its namespaces and compared with the ten packaged contexts the rule names
```

```text
Exactly the ten, none missing and no unnamed module root; with the reading still returning nothing, all ten are named as not found
```

```text
A reviewer sees a new module root refused until it is named, and a named context that vanished refused until it is taken off
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsArchUnitTest\.cs

Lighthouse\.Backend\.Tests\.Architecture\.ContextContractsArchUnitTest\.TheContextsReadFromTheCompiledCode\_AreExactlyTheTenPackagedContexts

```text
the reading returns the ten names written out instead of reading the code; the made-up root check catches that
```

###### Obligations




##### Covers

P2

```text
The contexts are read from the compiled test code, which holds a made-up module root, and compared with what it holds minus that root plus a context that does not exist
```

```text
Exactly the made-up root is named as not named, as one name and not the contexts below it, exactly the made-up gone context is named as missing, and the reading lists each name once in order
```

```text
A reviewer sees that the contexts come from the code, so a written-down or empty reading cannot pass
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsArchUnitTest\.cs

Lighthouse\.Backend\.Tests\.Architecture\.ContextContractsArchUnitTest\.TheContextCheck\_NamesAMadeUpModuleRootOfTheCompiledCode\_AndANamedContextThatIsGone\_ReadInOrderAndOnceEach

```text
a reading that also returns names one level deeper beside the root; the order and once-each check and the exact not-named list catch that
```

###### Obligations



##### Covers

P2

```text
Each of the ten packaged contexts in turn is taken off the list and the contexts of the compiled backend are compared with the rest
```

```text
Exactly the context taken off is named as not named, and nothing is named as gone
```

```text
A reviewer sees that every position of the list of contexts is compared with the code
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsArchUnitTest\.cs

Lighthouse\.Backend\.Tests\.Architecture\.ContextContractsArchUnitTest\.TheContextCheck\_NamesAPackagedContextOfTheCompiledCode\_OnceItIsTakenOffTheList

```text
a comparison that ignores one particular context; the case for that context catches it
```

###### Obligations



##### Covers

P2

```text
Every edge from a type of one packaged context to a type of another, outside that context's Contracts, is read from the compiled backend and compared with the listed edges
```

```text
The edges found equal the listed edges both ways: all 73 measured edges are listed, among them the write-back service reaching the write-back target and the delivery forecast publishing service reaching the forecast publisher
```

```text
A reviewer sees any new cross-context edge outside what a context publishes refused, and a listed edge that is no longer made refused until it is deleted
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsArchUnitTest\.cs

Lighthouse\.Backend\.Tests\.Architecture\.ContextContractsArchUnitTest\.EveryEdgeFromOneContextIntoAnotherOutsideItsContracts\_IsListed\_AndEveryListedEdgeIsStillMade

```text
a check that counts edges into Contracts as offending; it names nothing today because no Contracts exists yet, and the made-up contexts check catches it
```

###### Obligations



##### Covers

P2

```text
Made-up contexts are checked: one type reaches another context's Domain, one reaches the Domain of a context whose name begins alike, one listed type reaches another context's Application, and others reach another context's Contracts, the kernel and their own Domain; the list also holds an edge no type makes
```

```text
Exactly the two edges into another Domain are named as not listed and exactly the edge no type makes is named as gone; the listed edge and the edges into Contracts, the kernel and the own context are not named
```

```text
A reviewer sees the rule bite on a planted offender and pass its legal twins
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsArchUnitTest\.cs

Lighthouse\.Backend\.Tests\.Architecture\.ContextContractsArchUnitTest\.TheSurfaceCheck\_NamesEdgesIntoAnotherContextsDomain\_AndAListedEdgeNoTypeMakes\_ButNotEdgesIntoContractsTheKernelOrTheOwnContext

```text
a check that tells contexts apart by name prefix; the context whose name begins alike catches it
```

###### Obligations



##### Covers

P2

```text
Each of the 73 listed edges in turn is taken off the list and the edges of the compiled backend are compared with the rest
```

```text
Exactly the edge taken off is named as not listed, and nothing is named as gone
```

```text
A reviewer sees that every listed edge is one the code makes, and that deleting one brings it back as a failure
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsArchUnitTest\.cs

Lighthouse\.Backend\.Tests\.Architecture\.ContextContractsArchUnitTest\.TheSurfaceCheck\_NamesAListedEdgeOfTheCompiledCode\_OnceItIsTakenOffTheList

```text
a listed edge with a misspelt name that the code never makes; the both-ways comparison of the whole list catches it
```

###### Obligations



##### Covers

P2

```text
Every edge from a type in a Contracts of the compiled backend is read and compared with an empty list
```

```text
No edge from a Contracts reaches anything but the kernel, the base library, Microsoft.Extensions and the Contracts of any context; today no Contracts exists, so nothing is found
```

```text
A reviewer sees a published type refused once it drags in its own context's Domain, Application or Adapters or other code
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsArchUnitTest\.cs

Lighthouse\.Backend\.Tests\.Architecture\.ContextContractsArchUnitTest\.NoContractsTypeReachesBeyondTheKernelTheBaseLibraryMicrosoftExtensionsAndTheContracts

```text
a check that never looks at Contracts types; it passes today because none exists, and the made-up Contracts check catches it
```

###### Obligations



##### Covers

P2

```text
Made-up Contracts types are checked: one reaches its own context's Application, its twin reaches the kernel, the base library, Microsoft.Extensions and another context's Contracts
```

```text
Exactly the type reaching its own Application is named; its twin is not
```

```text
A reviewer sees the inward rule bite on a planted offender and pass its legal twin
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsArchUnitTest\.cs

Lighthouse\.Backend\.Tests\.Architecture\.ContextContractsArchUnitTest\.TheInwardCheck\_NamesAContractsTypeReachingItsOwnApplication\_ButNotOneReachingTheKernelTheBaseLibraryMicrosoftExtensionsOrAnotherContracts

```text
a check that also allows the platform lane or Services from a Contracts; no planted type reaches those, so that wrong allowance survives
```

###### Obligations



##### Covers

P2

```text
The measured published set, the declared role of every rule pattern and the sweep of old names in test strings are checked as before
```

```text
Every published type is found in its old or its Contracts namespace and the set equals the closure, every pattern keeps its role, and no test string names a published type by its old name off the rename list, the new rule's list included
```

```text
A reviewer sees the earlier measurements still hold with the new rule in place
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ContextContractsMembershipArchUnitTest\.cs

Lighthouse\.Backend\.Tests\.Architecture\.ContextContractsMembershipArchUnitTest

```text
a string of the new rule naming a published type without the modules root; the sweep does not read that form, and the rule builds full names from it at run time
```

###### Obligations









##### Covers

P2

```text
Every architecture rule of the backend is run with the new rule and its made-up contexts in place
```

```text
Every existing rule keeps its verdict; only the context-reading checks fail until the contexts are read from the code
```

```text
A reviewer sees the new rule added without changing any other rule's verdict
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture

Lighthouse\.Backend\.Tests\.Architecture

```text
an existing rule over the test code that would also read the made-up contexts' root; none matches it today
```

###### Obligations



## Expectations

### Rows

#### E1

##### Promises

P1

```text
The architecture-rules probe for the module-boundary and unit-test-placement rule classes lists a passing test line for every context's published set, each measured from the compiled code, and for every rule pattern classified as a front door or a place, with front doors covering Application and Contracts. Every rule line in those classes passes, and the ef-model-unchanged probe reports the data model unchanged.
```

```text
A passing rule line does not show that the measured sets are complete or that a pattern was classified correctly; a rule that matches nothing also passes, and the probe cannot show a loosened rule from its output alone.
```

yes

no

None.

#### E2

##### Promises

P2

```text
The architecture-rules probe with the ContextContracts fragment prints passing lines for the contracts-only rules R-C1, R-C2 and R-C3, including lines showing that the listed cross-context edges match the measured edges in both directions and that a made-up offender is caught for each rule and for each list position.
```

```text
Passing lines name tests but not the edges; the probe cannot show whether the list is the true complete set of cross-context edges, or that a planted offender represents a real violation.
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
