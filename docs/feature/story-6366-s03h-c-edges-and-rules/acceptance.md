# Acceptance brief: story\-6366\-s03h\-c\-edges\-and\-rules

## Slices

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
