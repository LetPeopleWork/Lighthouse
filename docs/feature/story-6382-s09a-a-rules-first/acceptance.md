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
