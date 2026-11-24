# Acceptance brief: story\-6365\-s03g\-c\-edges\-and\-rules

## Slices

### S1

##### Covers

P1

```text
Sweep every rule constant of the test assembly and every string literal of the test project sources for the four former Refinement forms: plain name, regex alternative, group member, old folder path
```

```text
No constant and no literal outside the declared measurement files names Refinement by a former Services namespace or path
```

```text
No architecture rule still points at where Refinement used to live, so none keeps a dead alternative
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementRulesNameOnlyTheModuleArchUnitTest\.cs\#NoRuleConstantNamesRefinementByItsFormerPlace\;NoStringLiteralOutsideTheMeasurementFilesNamesRefinementByItsFormerPlace

```text
A rule narrowed in its constant but still spelled as a former form in a literal or in a composite built from another constant; a form written with unescaped dots or a forward-slash path
```

###### Obligations


##### Covers

P1

```text
Compare the declared measurement files with the files that keep a former form, and run the sweep over made-up offenders in each of the four forms plus a clean Modules-only rule
```

```text
Listed files and files with a former form are the same set both ways; each made-up former form is named and the clean rule is not; a listed file with no hit is named stale
```

```text
An exemption cannot linger after its need is gone, and a new former form cannot hide in a listed file or pass unseen
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementRulesNameOnlyTheModuleArchUnitTest\.cs\#TheListedMeasurementFilesAreExactlyTheFilesThatKeepAFormerForm\;MadeUpRuleConstantsInEachFormerFormAreNamedAndAModulesOnlyConstantIsNot\;AMadeUpLiteralInAFormerFormOutsideTheMeasurementFilesIsNamed\;AListedMeasurementFileWithNoFormerFormLeftIsNamedAsStale

```text
A sweep that reads only escaped regex text and misses the plain name or the folder path; a sweep that exempts a measurement file whether or not it still has a former form
```

###### Obligations


##### Covers

P1

P3

```text
Match every Modules.Refinement alternative of the Refinement rule constants against the types of the Refinement module, over a made-up alternative anchored to no type
```

```text
Each narrowed alternative still matches a Refinement type of the module, the seven named rule constants and their five owners are all present, and a dead alternative is named
```

```text
Narrowing keeps every rule's verdict: a rule cannot pass by looking at nothing
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementRulesNameOnlyTheModuleArchUnitTest\.cs\#EveryNarrowedRefinementPatternStillMatchesATypeOfTheModule\;AMadeUpPatternAnchoredToNoRefinementTypeIsNamedDead

```text
A pattern narrowed so far that it matches no Refinement type; a rule constant dropped altogether with its Refinement part
```

###### Obligations


### S2

##### Covers

P2

```text
A maintainer adds a Refinement type outside its layer, leaves one under Services, or lets Refinement Domain reach a service
```

```text
Each listed Refinement type sits in its layer with none left under Services, the layers hold exactly their lists, and the Domain reaches only the kernel and the base library; a made-up misplaced type, an unlisted layer type and a Domain type reaching a service are each named while compliant twins pass
```

```text
Refinement keeps its layers and a stray type is caught and named in Refinement wording
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleBoundariesArchUnitTest\.cs

```text
A layer check that only compares counts, or a Domain rule that allows any Services type, would still pass; the made-up offenders and the exact two-way layer lists refuse both
```

###### Obligations


##### Covers

P2

```text
An outside type starts calling Refinement Domain or Application, or a listed caller stops doing so
```

```text
Only the API, the composition root and Refinement Application reach the Domain, only the API and the composition root reach the Application, both lists are empty on today's code; a made-up newcomer is named while the API, root and a listed caller are not, a deleted listed caller is named as offending, and a gone entry is named for deletion; the failure speaks of Refinement and the API
```

```text
A new caller of Refinement is caught and the failure names it in Refinement wording
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleBoundariesArchUnitTest\.cs

```text
A rule that treats all of Lighthouse.Backend.API or the whole root namespace as legal, or one that ignores calls written only inside an async method, would still pass; the newcomer fixtures include an async-only caller and unlisted namespaces
```

###### Obligations


##### Covers

P2

```text
A Refinement Application type starts holding an EF Core set, the data context, an API type, a module adapter, Tracker integration, an update queue type or an update interface
```

```text
Today's Application reaches none of them with an empty list; a made-up type for each of the seven targets, the update interfaces (update service, team and portfolio updaters) included, is named with its edge in Refinement wording, and a compliant twin that reaches a clock and legal neighbour Applications passes; the forbidden-code pattern is the update code of the Access rules plus the update queue
```

```text
Refinement cannot start depending on storage, the API, adapters, trackers or the update pipeline unseen
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleBoundariesArchUnitTest\.cs

```text
A forbidden-code pattern missing one target, for example the update interfaces under Services.Interfaces.Update, would still pass; one fixture per target, including an update-interface fixture, refuses that
```

###### Obligations


##### Covers

P2

```text
A Refinement Application type starts reaching Access, Organisation set-up, the Platform lane or a repository along an edge not on the list, or a listed edge disappears
```

```text
The 14 edges measured from the compiled code (Access identity, blackout service, clock, recurrence, Work Item and sizing-log repositories and the generic repository) equal the inline list both ways; deleting any entry is named as offending, a listed edge with no code is named for deletion, and a made-up unlisted Access, set-up, Platform lane or repository edge is named
```

```text
A new edge from Refinement into those contexts is caught, and a removed edge forces the list to shrink
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleBoundariesArchUnitTest\.cs

```text
A list copied from the design expectation (missing the blackout edge of the need calculator and the generic repository edges) would still pass if only one direction were compared; the two-way comparison against the IL refuses it
```

###### Obligations


##### Covers

P3

```text
The Refinement rule set runs through dotnet test on today's code with no production file edited
```

```text
Every Refinement rule and list passes on the real compiled code with nothing loosened, so nothing a user, API client, tracker or database sees changes
```

```text
No visible change; only test-side rules were added
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleBoundariesArchUnitTest\.cs

```text
A rule set that passes because its patterns match nothing would still pass on real code; the C1 pattern check and the non-empty examined-type checks in the shared rules refuse blind patterns
```

###### Obligations


##### Covers

P2

```text
A Refinement rule constant names the module without anchoring its end, or a new alternative is added to the Tracker integration, Access, Licensing or per-type Refinement patterns
```

```text
Every Modules.Refinement alternative of the Refinement rule constants (the shared pattern, the Tracker integration, Access and Licensing composites, the per-type patterns and this file's own patterns) fails to match a namespace that only starts like the module's; a made-up constant with an open end is named and its anchored twin is not. This is where the lookalike check that the retired move law carried now lives
```

```text
A rule written for Refinement never silently constrains or excuses a neighbouring namespace
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementModuleBoundariesArchUnitTest\.cs

```text
A constant ending in the bare module name, which also matches a neighbouring namespace, would still pass any rule that only checks that each alternative matches a Refinement type; matching a lookalike namespace refuses it
```

###### Obligations


## Expectations

### Rows

#### E2

##### Promises

P1

```text
architecture-rules run on the candidate reports every existing architecture rule (RefinementPattern, Tracker integration, Access and Licensing lists, per-type patterns of RefinementModuleArchUnitTest) with the same verdict as on today code, with no rule newly failing and none removed.
```

#### E3

##### Promises

P2

```text
architecture-rules Refinement reports a Refinement rule set R-R1 to R-R6 whose listed types sit in their stated layers, Domain reaches only the kernel and BCL, outside code reaches Domain only from API, the composition root and Refinement Application, and Application reaches no EF Core, Data, API, module Adapters, Tracker integration or update code, with the Access, Organisation set-up, Platform lane and repository reach limited to the measured list.
```

#### E5

##### Promises

P3

```text
ef-model-unchanged reports the database model identical to the model before feature C, so nothing the database sees changes.
```

#### E7

##### Promises

P1

```text
backend-tests RefinementRulesNameOnly and architecture-rules Refinement report every narrowing check passing, including the checks that no rule constant and no string literal names Refinement by Services.Implementation.Refinement, Services.Interfaces.Refinement, Services/Implementation/Refinement or Services/Interfaces/Refinement; Refinement is named only as Modules.Refinement. That the source text holds no old form is owned by the whole-candidate review, not by a public probe.
```

#### E8

##### Promises

P2

```text
backend-tests RefinementModuleBoundaries reports every made-up-offender case passing, one per rule R-R1 to R-R6 and per list position: each made-up offender makes its rule fail. That the failure message speaks Refinement's own wording is owned by the oracle approval and the whole-candidate review, not by a public probe.
```

#### E9

##### Promises

P3

```text
The whole filtered backend-tests run finishes with zero failures. That no rule or list was loosened against the earlier rule files is owned by the whole-candidate review, not by a public probe.
```

## Charter

None.

## Examination

### E2

met

#### Observations

```text
Probe architecture-rules (full, no fragment; examiner workspace): exit 0; Total tests 1813, Passed 1813, Test Run Successful; includes RefinementPattern, Tracker integration, Access and Licensing list tests, per-type RefinementModuleArchUnitTest patterns (all Passed). Output file /tmp/claude-1000/-storage-repos-Lighthouse/b2180cf9-f85c-4987-9198-d31a827aa267/scratchpad/arch-all.out. Per-rule-name comparison with the pre-candidate run was not available to the examiner.
```

```text
Probe architecture-rules Refinement (examiner workspace): exit 0; 141/141 Passed, Test Run Successful. Includes R-R rule tests: EveryTypeThatStays_IsInTheCompiledCodeWhereListed; TheRefinementTypesInTheCompiledCode_AreExactlyTheTypesTheMapListsToMove; TheForecastingApplication_IsReachedFromOutsideOnlyBy...; ARefinementApplicationTypeReaching{UnlistedAccessType,UnlistedSetUpType,UnlistedPlatformLaneType,UnlistedRepository,EntityFrameworkCoreDbSet,TheDataLayer,TheApi,ModuleAdapter,TrackerIntegration,TheUpdateCode,TheUpdateContracts}_IsNamed; ARefinementDomainTypeReachingAService_IsNamed; AFileReachingBeyondTheKernelListedAsDomain_IsNamedWithWhatItReaches; ARefinementApplicationReachingOnlyALegalNeighbourAndTheClock_PassesBothOutboundRules; AnOutsideTypeOffTheList_ReachingTheRefinementApplication/Domain_IsNamed. Output file /tmp/claude-1000/-storage-repos-Lighthouse/b2180cf9-f85c-4987-9198-d31a827aa267/scratchpad/arch-refinement.out
```

### E3

met

#### Observations

```text
Probe architecture-rules Refinement (examiner workspace): exit 0; 141/141 Passed, Test Run Successful. Includes R-R rule tests: EveryTypeThatStays_IsInTheCompiledCodeWhereListed; TheRefinementTypesInTheCompiledCode_AreExactlyTheTypesTheMapListsToMove; TheForecastingApplication_IsReachedFromOutsideOnlyBy...; ARefinementApplicationTypeReaching{UnlistedAccessType,UnlistedSetUpType,UnlistedPlatformLaneType,UnlistedRepository,EntityFrameworkCoreDbSet,TheDataLayer,TheApi,ModuleAdapter,TrackerIntegration,TheUpdateCode,TheUpdateContracts}_IsNamed; ARefinementDomainTypeReachingAService_IsNamed; AFileReachingBeyondTheKernelListedAsDomain_IsNamedWithWhatItReaches; ARefinementApplicationReachingOnlyALegalNeighbourAndTheClock_PassesBothOutboundRules; AnOutsideTypeOffTheList_ReachingTheRefinementApplication/Domain_IsNamed. Output file /tmp/claude-1000/-storage-repos-Lighthouse/b2180cf9-f85c-4987-9198-d31a827aa267/scratchpad/arch-refinement.out
```

```text
Probe backend-tests RefinementModuleBoundaries (examiner workspace): exit 0; 33/33 Passed, Test Run Successful. Includes EveryRefinementType_LivesInItsLayer_AndNoneIsLeftUnderServices (per layer), NoRefinementAlternativeOfTheRuleConstantsMatchesALookalikeNamespace, TheRefinementDomain_ReachesOnlyTheSharedKernelAndTheBaseLibrary. Output file /tmp/claude-1000/-storage-repos-Lighthouse/b2180cf9-f85c-4987-9198-d31a827aa267/scratchpad/be-boundaries.out
```

```text
Non-probe (examiner limit): the Refinement rule content (inline list equal to IL measurement in both directions, measured Access/Set-up/Platform/Repository list) is shown only by passing test names; the list contents were not read.
```

### E5

met

#### Observations

```text
Probe ef-model-unchanged (examiner workspace): exit 0; Sqlite: 'No changes have been made to the model since the last migration.'; Postgres: same. Output file /tmp/claude-1000/-storage-repos-Lighthouse/b2180cf9-f85c-4987-9198-d31a827aa267/scratchpad/ef.out
```

### E7

met

#### Observations

```text
Probe architecture-rules Refinement (examiner workspace): exit 0; 141/141 Passed, Test Run Successful. Includes R-R rule tests: EveryTypeThatStays_IsInTheCompiledCodeWhereListed; TheRefinementTypesInTheCompiledCode_AreExactlyTheTypesTheMapListsToMove; TheForecastingApplication_IsReachedFromOutsideOnlyBy...; ARefinementApplicationTypeReaching{UnlistedAccessType,UnlistedSetUpType,UnlistedPlatformLaneType,UnlistedRepository,EntityFrameworkCoreDbSet,TheDataLayer,TheApi,ModuleAdapter,TrackerIntegration,TheUpdateCode,TheUpdateContracts}_IsNamed; ARefinementDomainTypeReachingAService_IsNamed; AFileReachingBeyondTheKernelListedAsDomain_IsNamedWithWhatItReaches; ARefinementApplicationReachingOnlyALegalNeighbourAndTheClock_PassesBothOutboundRules; AnOutsideTypeOffTheList_ReachingTheRefinementApplication/Domain_IsNamed. Output file /tmp/claude-1000/-storage-repos-Lighthouse/b2180cf9-f85c-4987-9198-d31a827aa267/scratchpad/arch-refinement.out
```

```text
Probe backend-tests RefinementRulesNameOnly (examiner workspace): exit 0; 11/11 Passed, Test Run Successful. Includes MadeUpRuleConstantsInEachFormerFormAreNamedAndAModulesOnlyConstantIsNot, NoRuleConstantNamesRefinementByItsFormerPlace, NoStringLiteralOutsideTheMeasurementFilesNamesRefinementByItsFormerPlace, TheListedMeasurementFilesAreExactlyTheFilesThatKeepAFormerForm. Output file /tmp/claude-1000/-storage-repos-Lighthouse/b2180cf9-f85c-4987-9198-d31a827aa267/scratchpad/be-nameonly.out
```

```text
Non-probe (examiner limit): no public observation shows the absence of old Refinement namespace or folder strings in test-project sources, nor compares rule files against the prior rule files for loosening. Source/diff inspection is outside the examiner boundary; the only related public evidence is passing oracle tests (O3, O5) whose content is not visible. Global absence is a candidate-review item.
```

### E8

met

#### Observations

```text
Probe backend-tests RefinementModuleBoundaries (examiner workspace): exit 0; 33/33 Passed, Test Run Successful. Includes EveryRefinementType_LivesInItsLayer_AndNoneIsLeftUnderServices (per layer), NoRefinementAlternativeOfTheRuleConstantsMatchesALookalikeNamespace, TheRefinementDomain_ReachesOnlyTheSharedKernelAndTheBaseLibrary. Output file /tmp/claude-1000/-storage-repos-Lighthouse/b2180cf9-f85c-4987-9198-d31a827aa267/scratchpad/be-boundaries.out
```

### E9

met

#### Observations

```text
Probe backend-tests (bare full filtered suite; examiner workspace, one run): Test Run Successful; Total 14685, Passed 14673, Skipped 12, Failed 0. Output file /tmp/claude-1000/-storage-repos-Lighthouse/b2180cf9-f85c-4987-9198-d31a827aa267/scratchpad/examine-full-backend.out. The 12 skipped tests were not identified by the examiner.
```
