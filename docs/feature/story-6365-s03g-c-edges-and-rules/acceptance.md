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


## Expectations

### Rows

#### E1

##### Promises

P1

```text
Searching the test project for the Refinement path fragments Services.Implementation.Refinement, Services.Interfaces.Refinement, Services/Implementation/Refinement and Services/Interfaces/Refinement finds no rule constant or string literal that names Refinement that way; Refinement is named only as Modules.Refinement.
```

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

#### E4

##### Promises

P2

```text
backend-tests Refinement shows that each made-up offender for each R-R rule and each list position fails the rule, and the failure message names the offending edge in the Refinement wording.
```

#### E5

##### Promises

P3

```text
ef-model-unchanged reports the database model identical to the model before feature C, so nothing the database sees changes.
```

#### E6

##### Promises

P3

```text
The whole filtered backend-tests run finishes with zero failures, and the candidate loosens no rule or list relative to the prior rule files.
```

## Charter

None.

## Examination

None.
