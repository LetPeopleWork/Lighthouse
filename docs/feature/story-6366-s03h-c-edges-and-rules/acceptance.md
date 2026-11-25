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
