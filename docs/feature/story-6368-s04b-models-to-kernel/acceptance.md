# Acceptance brief: story\-6368\-s04b\-models\-to\-kernel

## Slices

### S1

##### Covers

P1

```text
Compare the catalogue with every top-level type the compiled backend has in the Models namespaces
```

```text
Each Models type has exactly one row and every row names a type that exists; a missing type is named with its measured row
```

```text
A maintainer sees every Models type with one standing, and a new Models type fails naming itself
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelCatalogueArchUnitTest\.cs

KernelCatalogueArchUnitTest\.EveryModelsType\_HasExactlyOneCatalogueRow\_AndEveryRowNamesAModelsType

```text
the catalogue matched the compiled types by short name only, so two same-named types in different Models namespaces would share one row
```

###### Obligations



##### Covers

P1

```text
Compare a made-up catalogue that misses a planted Models type used by one context and holds a row for a type nobody wrote
```

```text
The check fails naming the planted type and the row for the missing type
```

```text
A type added to the Models without a row, or a row left after its type is gone, is named in the failure
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelCatalogueArchUnitTest\.cs

KernelCatalogueArchUnitTest\.TheCatalogueCheck\_NamesAPlantedTypeMissingFromTheCatalogue\_AndARowForATypeThatDoesNotExist

```text
the comparison named only the first difference of each direction when several types are missing
```

###### Obligations



##### Covers

P1

```text
Measure every owned row's blockers from the compiled code and the database model and compare them with the row
```

```text
Every claimed blocker is true and every measured blocker is claimed; a free row names the one context that uses it as owner
```

```text
Each row's reason to stay in the Models is true today, so no type stays without cause
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelCatalogueArchUnitTest\.cs

KernelCatalogueArchUnitTest\.EveryOwnedRow\_ClaimsExactlyTheBlockersMeasuredFromTheCompiledCodeAndTheDatabaseModel

```text
a wire type was reached only through a controller helper the controller does not call directly, which the measurement cannot see
```

###### Obligations



##### Covers

P1

```text
Check made-up rows claiming Entity for a type the database does not store and Wire for a type no controller sends, and rows whose claims are true
```

```text
The false claims fail naming the type and the blocker; the true claims pass
```

```text
A row that keeps a type in the Models for a false reason fails, naming the type and the reason
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelCatalogueArchUnitTest\.cs

KernelCatalogueArchUnitTest\.TheBlockerCheck\_AcceptsARowWhoseClaimIsTrue

```text
the check accepted any row whose claims are a superset of the measured blockers in one direction only
```

###### Obligations



##### Covers

P1

```text
Compare the movers list with every Models type that has no measured blocker
```

```text
Each free type is on the list with its owner and the namespace it moves to (the owner's Domain or Application, or Platform for the platform lane), and nothing else is
```

```text
The maintainer sees which Models types are free to move now and where each one goes
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelCatalogueArchUnitTest\.cs

KernelCatalogueArchUnitTest\.EveryFreeType\_IsOnTheMoversList\_WithItsOwnerAndTheNamespaceItMovesTo

```text
the destination layer were chosen Domain for a type whose only non-kernel reference is to a mover bound for Application of another owner
```

###### Obligations


##### Covers

P1

```text
Compare an empty movers list with a made-up backend whose planted type is used by one context only
```

```text
The check fails naming the planted type and the namespace it moves to
```

```text
A type that could move but is not listed fails, naming where it belongs
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelCatalogueArchUnitTest\.cs

KernelCatalogueArchUnitTest\.TheMoversCheck\_NamesAFreeTypeLeftOffTheList\_WithTheNamespaceItMovesTo

```text
the failure named the owner context but not the layer the type moves to
```

###### Obligations


##### Covers

P1

```text
Look each mover up in the compiled code under its Models name and its new name
```

```text
Each mover is found in exactly one of the two places
```

```text
A move leaves no copy behind and no mover is lost by a rename
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelCatalogueArchUnitTest\.cs

KernelCatalogueArchUnitTest\.TheFreeType\_IsFoundAsATopLevelTypeInExactlyItsModelsOrItsNewNamespace

```text
a mover moved to a namespace other than the one listed, which fails here as found in neither place only when the old one is gone
```

###### Obligations


##### Covers

P1

```text
Ask every declared rule pattern whether it keeps its role when each free Models type moves to its owner
```

```text
Old locations such as the kernel pattern do not see a mover in its new place, and parts of the code keep seeing it; the Reports view of the Models is red until widened
```

```text
Architecture rules keep guarding a type while it moves out of the Models
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelCatalogueArchUnitTest\.cs

KernelCatalogueArchUnitTest\.TheRulePattern\_KeepsItsDeclaredRole\_WhenTheFreeModelsTypesMove

```text
a pattern matched the mover's new namespace only by accident through an over-wide alternative reaching other contexts
```

###### Obligations




##### Covers

P1

```text
Enumerate every string pattern of the test assembly that sees a mover under its old or new name
```

```text
Each one is declared with a role or as an owner pattern, and every declaration still names a rule pattern
```

```text
No rule that finds a free type by name is forgotten when the type moves
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelCatalogueArchUnitTest\.cs

KernelCatalogueArchUnitTest\.EveryRulePatternThatSeesAFreeModelsType\_IsDeclared\_AndEveryDeclaredPatternIsARulePattern

```text
a rule built its pattern at run time from pieces that are not string fields, which the enumeration does not read
```

###### Obligations


##### Covers

P1

```text
Run the role check on a made-up rule class whose pattern takes in the context a planted type moves to, declared nowhere
```

```text
The check names the undeclared field
```

```text
A new rule pattern that sees a moving type fails until its role is declared
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelCatalogueArchUnitTest\.cs

KernelCatalogueArchUnitTest\.TheRoleCheck\_NamesAnUndeclaredPatternThatSeesAFreeType

```text
the check named undeclared patterns that see the new name but not ones that see only the old name
```

###### Obligations


##### Covers

P1

```text
Ask each pattern that takes in an owner whether it matches the new name of every mover of that owner
```

```text
Each owner pattern matches every listed owner's movers in their new namespace
```

```text
A context's rules guard the types that move into it from the first commit they land
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelCatalogueArchUnitTest\.cs

KernelCatalogueArchUnitTest\.TheRulePatternOfAnOwner\_SeesEveryFreeTypeOfThatOwnerUnderItsNewName

```text
the owners listed for a pattern were fewer than the owners it actually takes in
```

###### Obligations


##### Covers

P1

```text
Scan every string literal of the test project for a mover named by its Models name or Models file path
```

```text
Each such string is on the rename list with its new text, and every listed string is still found
```

```text
No test string keeps naming a moved type's old place, so no rule quietly stops seeing it
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelCatalogueArchUnitTest\.cs

KernelCatalogueArchUnitTest\.EveryTestStringThatNamesAFreeTypeByItsModelsName\_IsOnTheRenameList\_AndEveryListedStringIsFound

```text
a string named the mover in a grouped alternation such as Models.(A|B), which the scan does not expand
```

###### Obligations


##### Covers

P4

```text
Compare the candidate with the revision before the Story
```

```text
No schema, migration, route, DTO, payload or answer differs
```

```text
Nothing a user or a client sees changes
```

##### Disposition

NotTested

```text
S1 changes no production code; that no schema, migration, route, DTO, payload or usage-data event changes is observed by the unchanged backend suites and the EF model probe on the whole candidate
```

host verification and EXAMINE

##### Covers

P1

```text
Compare the rows that stand as Kernel with the one declared list of types the shared kernel may hold, and check a made-up catalogue that claims the kernel for an ordinary type
```

```text
The Kernel rows are exactly the domain-event base type and the two entity markers; a Kernel row for any other type, or a missing one, fails naming the type and the fix
```

```text
No type escapes the blocker check by being called kernel, and the kernel cannot grow unseen
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/KernelCatalogueArchUnitTest\.cs

KernelCatalogueArchUnitTest\.TheKernelCheck\_NamesARowThatClaimsTheKernelForATypeTheKernelMayNotHold

```text
the declared list itself were widened with an ordinary type in the same change, which only review of that list can catch
```

###### Obligations



### S2

##### Covers

P2

```text
Run the Architecture tests after the platform-owned usage-data types left the shared Models; each type is looked up by name in the compiled backend
```

```text
Each of the four types is found exactly once, under the platform lane's usage-data namespace, and nowhere under Models or in a module
```

```text
The platform lane holds its own usage-data types; nothing is left behind in the shared kernel
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformOwnedModelsTypesLiveInPlatformArchUnitTest\.cs

ThePlatformType\_IsFoundOnlyInTheLanesNamespaceOfItsArea

```text
A type left under Models, moved into a module instead of the platform lane, or copied so it exists in both places
```

###### Obligations




##### Covers

P2

```text
Read the catalogue of the types still under the shared Models after the usage-data types moved
```

```text
No catalogue row and no movers entry names any of the four usage-data types
```

```text
The catalogue that keeps Models shrinking lists only what is still in the Models
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformOwnedModelsTypesLiveInPlatformArchUnitTest\.cs

ThePlatformType\_IsNoLongerOnTheModelsCatalogueOrItsMoversList

```text
A row or movers entry for a moved type left on the catalogue
```

###### Obligations


##### Covers

P4

```text
Run the existing platform lane membership and boundary architecture tests after the move
```

```text
They still pass unchanged: the lane keeps holding its types and no rule was weakened
```

```text
Nothing a user or client sees changes; the move is behaviour-free
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembershipArchUnitTest\.cs

PlatformLaneMembershipArchUnitTest

```text
A rule edited so it no longer finds the moved types
```

###### Obligations


## Expectations

### Rows

#### E1

##### Promises

P1

```text
Every type left in the Models folder is listed on the kernel catalogue as either Kernel or owned by a named context with a stated blocker and the Story that frees it; no type is missing and no row names a type that is gone.
```

```text
A passing rule run does not show the catalogue matches the compiled Models types, nor that each blocker is true; it sees only what the rules enumerate.
```

None.

None.

```text
architecture-rules
```

#### E2

##### Promises

P1

```text
A catalogue row claiming a type is blocked, or is Kernel, is true when checked against the compiled code and the EF model (the named dependency or mapping really exists).
```

```text
Rules pass or fail on stated claims; they do not show a true blocker was not silently dropped or a Kernel type is really one of the three allowed base types.
```

None.

None.

```text
architecture-rules
```

#### E3

##### Promises

P2

```text
Types nothing blocks are no longer in Models: Platform types sit under Platform/<Area>, context types under Modules/<Owner>/Domain or Application, and their unit tests sit beside them.
```

```text
Green rules do not show every unblocked type actually moved or that its tests moved and still run.
```

None.

None.

```text
architecture-rules; backend-tests
```

#### E4

##### Promises

P2

```text
The full backend test suite and every module's boundary and placement rules stay green after the moves.
```

```text
A narrowed or summarised run can hide a skipped or excluded test class; only the named run counts.
```

None.

None.

```text
architecture-rules; backend-tests
```

#### E5

##### Promises

P3

```text
Adding an uncatalogued type to Models, or making a catalogue claim false, makes the architecture rules fail with a message naming the type and the fix; the moved types are accepted only at their new namespace, with no old-path allowance.
```

```text
The oracle may only check the passing state; a rule that never fails is not seen without a deliberate violation.
```

None.

None.

```text
architecture-rules
```

#### E6

##### Promises

P4

```text
Every API answer a user or client reads (platform, access and licensing, deliveries, metrics, forecast, refresh) is identical to the revision before the Story, with no status change.
```

```text
Tests of the moved types do not compare real wire answers across revisions; compared answers cover only the probed endpoints and demo data.
```

None.

None.

```text
platform-answers-compare, access-answers-compare, deliveries-answers-compare, metrics-answers-compare, forecast-answers-compare, refresh-answers-compare, each with base origin/main at start
```

#### E7

##### Promises

P4

```text
The database model is unchanged for both providers: no pending model change and no new migration against the last one.
```

```text
Moving types can change a mapped name or namespace-derived value; unit tests do not ask EF.
```

None.

None.

```text
ef-model-unchanged
```

#### E8

##### Promises

P4

```text
No usage-data event, route or DTO payload is added, removed or renamed by the Story; the event names and values match the revision before.
```

```text
Answer compares do not list the usage-data event catalogue or route table; a renamed namespace could alter a serialised name unseen.
```

None.

None.

```text
platform-answers-compare, backend-tests
```

#### E9

##### Promises

P5

```text
ARCHITECTURE.md section 4 and ADR-270 Decision 7 describe the kernel as it now stands and point to the catalogue for every other Models type; a reader finds no statement that contradicts the catalogue.
```

```text
No automated check reads the prose; a green build says nothing about whether the documents match what shipped.
```

None.

None.

```text
reading the two documents with examine read
```

### Waivers

None.

## Charter

None.

## Examination

None.
