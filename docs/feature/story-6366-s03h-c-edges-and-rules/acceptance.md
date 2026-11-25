# Acceptance brief: story\-6366\-s03h\-c\-edges\-and\-rules

## Slices

### S1

##### Covers

P1

P3

```text
Check each narrowed Reports alternative against the types of the Reports module, and the joint Tracker integration pattern for Reports under Services, for the Modules.Reports alternative and for the old Auth, Authorization and Licensing guard
```

```text
Every Modules.Reports alternative matches a Reports module type, a made-up dead alternative is named, and the joint Tracker integration pattern names Reports by its module only while keeping the old Auth, Authorization and Licensing alternatives under Services; a made-up pattern keeping Reports under Services or dropping the guard is named and a narrowed one is not
```

```text
Reports rules keep constraining Reports, the Tracker integration rule still catches a connector reaching the old Access and Licensing places, and no dead Reports alternative remains
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsRulesNameOnlyTheModuleArchUnitTest\.cs\:\:EveryNarrowedReportsPatternStillMatchesATypeOfTheModule\, AMadeUpPatternAnchoredToNoReportsTypeIsNamedDead\, TheJointTrackerIntegrationPatternNamesReportsOnlyByTheModule\_AndKeepsTheOldAccessAndLicensingGuard\, AMadeUpJointPatternKeepingReportsUnderServicesOrDroppingTheGuardIsNamedAndTheNarrowedOneIsNot

```text
A joint pattern that drops Reports entirely, leaving it unconstrained, is caught only by the Modules.Reports and owner checks
```

###### Obligations


##### Covers

P1

```text
Sweep every rule constant of the test assembly and every string literal of the test sources for the old Reports forms (plain name, regex alternative, group member, folder path), with only exact-path exclusions that must spell an old form; the exclusion list is compared both ways
```

```text
No constant and no literal outside the exact exclusions names Reports by a former Services namespace or path; each made-up offender in each form is named, a Modules-only twin is not, and a listed exclusion with no old form is named stale
```

```text
No rule still points at where Reports used to live
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsRulesNameOnlyTheModuleArchUnitTest\.cs\:\:NoRuleConstantNamesReportsByItsFormerPlace\, NoStringLiteralOutsideTheMeasurementFilesNamesReportsByItsFormerPlace\, TheListedMeasurementFilesAreExactlyTheFilesThatKeepAFormerForm\, MadeUpRuleConstantsInEachFormerFormAreNamedAndAModulesOnlyConstantIsNot\, AMadeUpLiteralInAFormerFormOutsideTheMeasurementFilesIsNamed\, AListedMeasurementFileWithNoFormerFormLeftIsNamedAsStale

```text
A rule narrowed in its constant but kept as a literal in another file, or an exclusion list left wider than needed, would still pass
```

###### Obligations







### S2

##### Covers

P2

```text
Run Reports' layer rule, its kernel-only Domain rule and its four listed edge rules over today's compiled code
```

```text
Each listed type sits in its layer and none under Services; Domain reaches only the kernel and System; the inbound lists (empty for Domain, the report repository for Application) and the outbound lists (3 EF Core edges including the one the bulk remover only names in a catch clause, 12 Flow metrics, Licensing, set-up and platform edges) equal the measured sets both ways
```

```text
Today's Reports passes its own rules, so a later new edge is the only thing that can fail them
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs\#AReportsListedRule\_MatchesWhatTheCompiledCodeReaches\_InBothDirections

```text
A list with an extra stale entry, a missing entry, or a catch-only edge left off the list would pass if the comparison ran one way only or the reader skipped catch clauses; it runs both ways and reads catch clauses
```

###### Obligations

None.

##### Covers

P2

```text
Delete each entry in turn from every Reports list and add an entry that no longer exists
```

```text
Each deleted entry is named as offending in Reports wording and each gone entry is named for deletion
```

```text
A list that drifts from the code is told which entry to add or delete
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs\#EveryEntryDeletedFromAReportsList\_IsNamedAsOffending\_AndAnEntryWithNoEdgeIsNamedForDeletion

```text
A list that silently ignores a deleted or stale entry would pass
```

###### Obligations



##### Covers

P2

```text
Run the layer and layer-contents rules on a made-up misplaced type and on a made-up unlisted layer type
```

```text
The misplaced type and the unlisted type are named; the well placed twin and the full list pass
```

```text
A type put in the wrong Reports layer or added without a decision is named
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs\#AReportsTypeLeftUnderServices\_IsNamed\_AndAWellPlacedTwinIsNot

```text
A rule that names every type as offending, or none, would pass a single-sided check
```

###### Obligations



##### Covers

P2

```text
Run the Domain rule and both inbound rules on made-up Domain, API, composition-root, listed and newcomer types
```

```text
A Domain type reaching a service, an outside newcomer reaching Domain and an outside newcomer reaching Application are each named in Reports wording; the API, the composition root and the listed callers are not
```

```text
A new caller of Reports' Domain or Application is caught, the legal callers are not
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs\#AnOutsideTypeOffTheList\_ReachingTheReportsApplication\_IsNamed\_AndTheApiTheCompositionRootAndTheListedCallerAreNot

```text
A rule that flags the API or the composition root, or none of the newcomers, would pass
```

###### Obligations



##### Covers

P2

```text
Run the forbidden-code rule on made-up Application types that hold a DbSet or reach data, API, adapters, Tracker integration, RBAC or update code
```

```text
Each offender is named with its edge in Reports wording
```

```text
Reports' Application cannot quietly start reaching storage, the API, the RBAC service or the update queue
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs\#AReportsApplicationTypeReachingForbiddenCode\_IsNamedWithTheEdge

```text
A forbidden pattern that omitted RBAC or one update namespace would pass
```

###### Obligations



##### Covers

P2

```text
Run the Flow metrics, Licensing, set-up and platform rule on made-up Application types reaching an unlisted edge of each kind and on a compliant twin, and run each rule over an empty namespace
```

```text
Each unlisted edge is named; the Flow metrics Application twin passes; a rule over nothing fails
```

```text
A new edge to Flow metrics' Domain, Licensing, set-up or the platform is caught, and a rule that checks nothing cannot go green
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs\#AReportsApplicationTypeReachingAnUnlistedFlowMetricsLicensingSetUpOrPlatformType\_IsNamedWithTheEdge

```text
A rule over an empty set that passes, or a Flow metrics Application edge that is wrongly flagged
```

###### Obligations



##### Covers

P3

```text
Check every Reports rule constant of the test project against a made-up lookalike namespace
```

```text
No Reports alternative matches a namespace that only starts like the module's, and an unanchored made-up constant is named
```

```text
No rule is loosened; nothing a user sees changes
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs\#NoReportsAlternativeOfTheRuleConstantsMatchesALookalikeNamespace

```text
An unanchored Reports alternative would silently constrain a neighbouring namespace
```

###### Obligations



##### Covers

P2

```text
Run the layer rule on a made-up misplaced Reports type
```

```text
The misplaced type is named, its twin is not
```

```text
A Reports type left in the wrong place is named
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs\#AReportsTypeLeftUnderServices\_IsNamed\_AndAWellPlacedTwinIsNot

```text
A rule that names nothing
```

###### Obligations


##### Covers

P2

```text
Run the Domain rule on a made-up Domain type reaching a service
```

```text
The type is named, its compliant twin is not
```

```text
A Domain type reaching a service is caught
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs\#AReportsDomainTypeReachingAService\_IsNamed\_AndItsCompliantTwinIsNot

```text
A Domain rule that allows services
```

###### Obligations


##### Covers

P2

```text
Run the Domain inbound rule on made-up outside types
```

```text
The newcomer is named; API, composition root and listed caller are not
```

```text
An outside type reaching Domain is caught
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs\#AnOutsideTypeOffTheList\_ReachingTheReportsDomain\_IsNamed\_AndTheApiTheCompositionRootAndTheListedCallerAreNot

```text
A rule that also flags the API
```

###### Obligations


##### Covers

P2

```text
Run the Application inbound rule on made-up outside types
```

```text
The newcomer is named; legal callers are not
```

```text
An outside type reaching Application off the list is caught
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs\#AnOutsideTypeOffTheList\_ReachingTheReportsApplication\_IsNamed\_AndTheApiTheCompositionRootAndTheListedCallerAreNot

```text
A rule that ignores newcomers
```

###### Obligations


##### Covers

P2

```text
Run the forbidden-code rule on made-up Application offenders
```

```text
Each offender is named with its edge
```

```text
Forbidden reaches are caught
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs\#AReportsApplicationTypeReachingForbiddenCode\_IsNamedWithTheEdge

```text
A pattern missing RBAC or update code
```

###### Obligations


##### Covers

P2

```text
Run the outbound rule on made-up unlisted edges
```

```text
Each unlisted edge is named
```

```text
A new Flow metrics, Licensing, set-up or platform edge is caught
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs\#AReportsApplicationTypeReachingAnUnlistedFlowMetricsLicensingSetUpOrPlatformType\_IsNamedWithTheEdge

```text
An outbound rule that lets one kind through
```

###### Obligations


##### Covers

P2

```text
Delete each entry from each list and add a gone entry
```

```text
Deleted entries named as offending, gone entries named for deletion
```

```text
List drift is named
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs\#EveryEntryDeletedFromAReportsList\_IsNamedAsOffending\_AndAnEntryWithNoEdgeIsNamedForDeletion

```text
A one-way comparison
```

###### Obligations


##### Covers

P2

```text
A type in Reports' Application whose only reference to an EF Core exception is a catch clause is run through the forbidden-code rule
```

```text
The rule names the catch-only edge as offending in Reports wording
```

```text
An edge that is only caught, never constructed or called, can no longer slip into Reports unseen
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs\#ATypeThatOnlyCatchesAnExceptionInACatchClause\_IsSeenReferencingIt

```text
An IL reader that skips exception handlers would pass the rule without naming the edge; the test fails then
```

###### Obligations

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
Running the architecture-rules probe filtered to ReportsRulesNameOnlyTheModule prints a passing verdict for every test in that sweep, so the Reports rules name Modules.Reports only and no rule changes verdict on today's code. Whether any old Reports namespace string remains in the test project is not visible through a probe and is left to the whole-candidate review.
```

#### E2

##### Promises

P2

```text
Running the architecture-rules probe filtered to ReportsModuleBoundaries prints one line each for the rules R-RP1 to R-RP6, and each line, together with one made-up-offender test per rule, shows a passing verdict on the candidate; a made-up-offender test passes only when its rule fails on the offender, so the failure behaviour is observed too. Reports therefore has its own module boundary rule set with its layer, Domain, Application and EF Core lists checked.
```

#### E4

##### Promises

P3

```text
ef-model-unchanged reports the EF Core model as unchanged, and backend-tests run with a fragment shows its selected tests green, so the user-visible surface (API, Program.cs behaviour, configuration) is not shown to have changed. The whole filtered backend-tests run (no fragment) runs as a background shell job, takes about seven minutes and reports 0 failed; no probe shows a source diff, so that no production file changed is left to the whole-candidate review.
```

#### E5

##### Promises

P3

```text
test-selection-show for Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/ReportsModuleBoundariesArchUnitTest.cs prints backend: true and jira, ado, linear, servicenow, github and shared false, so the CI selection a user sees does not change.
```

## Charter

None.

## Examination

None.
