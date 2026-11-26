# Acceptance brief: story\-6657\-write\-back\-round

## Slices

### S1

##### Covers

P1

```text
The eight write-back round types and the round's staying neighbours are looked up in the compiled code by their names, and a made-up type and made-up neighbours are looked up the same way.
```

```text
Each round type is found in exactly one of its old Services namespace and its new Forecasting WriteBack namespace; each staying neighbour exists with an owner and a reason; the made-up entries, and only they, are named.
```

```text
A maintainer can trust that the map the move rules read names every round type where it really is, before, during and after the move.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackMembershipArchUnitTest\.cs\:\:EveryRoundType\_IsFoundInItsOldOrItsNewNamespace\_AndEveryNeighbourThatStays\_IsFoundWithItsOwnerAndReason\; Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackMembershipArchUnitTest\.cs\:\:TheMembershipCheck\_NamesAMadeUpRoundTypeFoundInNeitherNamespace\_AndAMadeUpNeighbourThatIsNowhereOrHasNoOwner

```text
A round type existed in its old place under a different arity or as a nested type only; the lookup is by exact top-level full name.
```

###### Obligations


##### Covers

P1

```text
The shared namespace-move law is run with the write-back map over every rule pattern of the test assembly, and over made-up rule keys.
```

```text
Every pattern that sees a round type is declared: parts of the code keep seeing it, old locations never see the new place, and the round pattern and Forecasting patterns see every round type in its new place. Today it fails only because the round pattern does not exist yet. The made-up blind, widened-old-location and undeclared keys, and the blind or missing admitted round patterns, are named.
```

```text
No architecture rule silently stops seeing a write-back round type when the round moves.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackMembershipArchUnitTest\.cs\:\:EveryRulePatternThatSeesARoundType\_IsDeclared\_AndKeepsItsRoleOnTheSubjectItsRuleReads\; Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackMembershipArchUnitTest\.cs\:\:TheRoleCheck\_NamesEachMadeUpPatternThatBreaksItsRole\_AndPassesTheOnesThatKeepIt

```text
A rule pattern held as a property or built inside a method body sees the round; the law reads only constant and static read-only string fields.
```

###### Obligations


##### Covers

P1

```text
A made-up Work Items Application type reaches a made-up type in the round's real new namespace, and every Work Items outbound rule judges the edge.
```

```text
The Work Items rules that refuse it are exactly the ones the membership records; measured today: none, because Work Items refuses the round only by its old full names.
```

```text
The maintainer knows whether Work Items still refuses the round after the move before its old names are taken away.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackMembershipArchUnitTest\.cs\:\:AMadeUpWorkItemsTypeReachingARoundTypeInItsNewPlace\_IsRefusedByExactlyTheWorkItemsRulesTheMembershipRecords

```text
A Work Items rule refuses the real round types only by their full names; a made-up type cannot carry those names.
```

###### Obligations


##### Covers

P1

```text
Every string literal of the test project is swept for the old Services path of a round type and compared with the fixed rename list; a made-up rule file and list are swept the same way.
```

```text
The literals naming a round type by its old path are exactly the listed ones, each file holding exactly one form unless the entry may leave; the made-up unlisted string, the listed string found nowhere and the half-renamed string are each named.
```

```text
No test string keeps naming the round by its old Services path unnoticed when the round moves.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackMembershipArchUnitTest\.cs\:\:EveryTestStringThatNamesARoundTypeByItsOldPath\_IsOnTheRenameList\_InExactlyOneOfItsForms\; Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackMembershipArchUnitTest\.cs\:\:TheRenameListCheck\_NamesAnUnlistedOldPathString\_AListedStringFoundInNeitherForm\_AndOneFoundInBoth

```text
An old path written in a comment or built at run time from parts; the sweep reads literals outside comments only.
```

###### Obligations


##### Covers

P1

```text
A made-up Promises, Access or Licensing Application type reaches a made-up round port in a made-up Forecasting WriteBack namespace, and its twin reaches a made-up Forecasting port outside the round; each context's outbound predicate judges both, handed a round pattern naming the made-up round.
```

```text
Each predicate refuses exactly the type joining the round and not its twin. Today all three refuse nothing, because they read only the eight old full names.
```

```text
A Promises, Access or Licensing type cannot take part in the write-back round unseen, wherever the round lives.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackMembershipArchUnitTest\.cs\:\:AMadeUpTypeOfAContextThatMayNotJoinTheRound\_ReachingARoundTypeInItsNewPlace\_IsRefusedByTheRoundPattern\_AndItsTwinReachingForecastingIsNot \(PromisesMayNotJoinTheRound\_ThoughItMayAskForecasting\, AccessMayNotJoinTheRound\, LicensingMayNotJoinTheRound\)

```text
The real rule hands the predicate a pattern other than ModuleRules.TheWriteBackRound; the real-rule wiring is observed by the existing product rule tests of the three classes.
```

###### Obligations




##### Covers

P1

```text
The existing made-up Access and Promises fixtures reach the round's trigger port, and the product rules read the production Promises, Access and Licensing types.
```

```text
The fixtures are refused and no production Promises, Access or Licensing type reaches the round, with the predicates reading the round pattern.
```

```text
The existing guards keep refusing the round while it moves.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs\:\:APromisesTypeReachingTheWriteBackRound\_IsNamed\; Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:\:AnAccessTypeReachingTheWriteBackRound\_IsNamed\; the ModuleEdgesIntoUpstreamCode product rows of the Promises\, Access and Licensing boundary tests

```text
Only the old-name alternatives of the round pattern are exercised by these fixtures; the new place is covered by the falsifier row.
```

###### Obligations





## Expectations

### Rows

#### E1

##### Promises

P1

```text
For every architecture and layout rule that judges the write-back round, the architecture-rules probe shows the same pass/fail verdict after the round moves as before it, with no rule loosened and no rule dropped or silently emptied; rules still see the round's types.
```

#### E2

##### Promises

P2

```text
The write-back round's types and its unit tests are listed under Forecasting's Application/WriteBack place, and no longer under the old loose Services namespaces; types that stay in those loose namespaces are untouched; the backend tests and the EF model probe show no behaviour change.
```

#### E3

##### Promises

P3

```text
Rules refer to the round only by its Forecasting namespace (the old Services path no longer names it), and the architecture-rules probe lists boundary rules specific to the write-back round that pass, each catching a made-up offender.
```

## Charter

None.

## Examination

None.
