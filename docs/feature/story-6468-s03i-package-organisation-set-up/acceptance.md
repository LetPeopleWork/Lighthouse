# Acceptance brief: story\-6468\-s03i\-package\-organisation\-set\-up

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
Run architecture-rules with class fragments OrganisationSetupMembershipArchUnitTest and OrganisationSetupRulesNameOnlyTheModuleArchUnitTest: one pass line per test, none failing. Every architecture rule keeps its verdict for every moving Organisation set-up type, and each rule pattern that matches a moving type's old name is classified as standing for the context or for the place.
```

#### E2

##### Promises

P3

P4

```text
Run architecture-rules with the oracle class fragments for the Organisation set-up boundary rules (OrganisationSetupMembershipArchUnitTest, OrganisationSetupRulesNameOnlyTheModuleArchUnitTest and the slices' boundary and event-publisher classes): one pass line per test. Downstream modules reach Organisation set-up by namespace with no list entry, Tracker integration only along its one listed edge, Organisation set-up has its own boundary rules read from the IL, no second controller publishes a set-up event unseen, and every rule naming Organisation set-up uses Modules.OrganisationSetup only, with no rule constant or literal naming a moved type by its old place.
```

#### E3

##### Promises

P2

```text
Organisation set-up types live in Modules/OrganisationSetup in their measured layer with unit tests beside them: test-selection-show on the new module paths shows the moved tests selected, and backend-tests with a fragment shows the named tests green. The application behaves as before: ef-model-unchanged reports the EF model unchanged for every provider, and the full filtered backend-tests suite, run as a background shell job (about 8 minutes) and read when it ends, shows 0 failed. That only namespaces changed in production files, and Program.cs order, are not shown by any probe and are left to the whole-candidate review.
```

## Charter

None.

## Examination

None.
