# Acceptance brief: story\-6468\-s03i\-package\-organisation\-set\-up

## Slices

### S1

##### Covers

P1

```text
Before any file moves, look up every Organisation set-up type of the membership map in the compiled code, and every staying set-up file with its owner and reason
```

```text
All 27 moving types are found in their old or their new namespace, and every staying file exists with an owner and a reason; a made-up type found in neither namespace, a made-up staying file that does not exist and one without an owner are each named
```

```text
A maintainer sees which set-up types move, where to, and which set-up files stay with whom and why, before the move starts
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupMembershipArchUnitTest\.cs

```text
a moving type listed under the wrong layer whose wrong new namespace no code ever has, while it still sits in its old namespace
```

###### Obligations





##### Covers

P1

```text
Read the compiled references of the two Domain candidates, the weekly recurrence and the recurring-rule extensions, and the references the move takes out of namespace scope
```

```text
Each candidate reaches only the kernel, the base library and the other candidate; a made-up candidate that reaches the database is named with the database context; the only reference that compiles today only by sharing a namespace and needs a using after the split is the blackout service calling the recurring-rule extensions
```

```text
A maintainer knows the two calendar calculations can go to the module's Domain and which file needs a using line when they do
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupMembershipArchUnitTest\.cs

```text
a Domain candidate that reaches a set-up port through an attribute argument, which the compiled-reference reader does not see
```

###### Obligations





##### Covers

P1

```text
Ask every constant and static read-only string field of the test assembly that matches a moving type's old or new name, on the subject its rule reads, whether it keeps its declared role through the move
```

```text
RED before the S1 rule edits, exactly the five Context patterns that see a set-up type in the old place and not in the new: the Work Items set-up pattern and its composite, the blackout seam's service implementation pattern, the board port's implementation pattern and the weekly recurrence full-name pattern, 80 type breaks in all; every Place pattern and every folder-only pattern holds; made-up keys show an undeclared pattern, a blind context, its composite and an old location widened into the new place are each named and a correctly widened context is not; no OrganisationSetup alternative reaches outside the module
```

```text
A maintainer sees, by class and field, every rule that would silently stop seeing the set-up types at the move, and which rules rightly stay in the old place
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupMembershipArchUnitTest\.cs

```text
a Context pattern widened to only the Application namespace, which stops seeing the two Domain types
```

###### Obligations



##### Covers

P1

```text
Read every string literal of the test project that names a moving set-up type or file by its old Services path and compare it with the fixed rename list
```

```text
Every such literal is on the list for its file, in its old or its new form, and every listed string still exists in one form unless its list row may leave; a row may leave when its list entry becomes legal, when it lists a file that stays in Services, or when it is the expected offender of a made-up type whose rule stops reading the moved type as forbidden (Refinement's unlisted set-up fixture; Reports' unlisted set-up and Promises', Access' and Licensing' unlisted platform fixtures that name the settings port); a made-up unlisted old full name and a listed string found in neither form are each named
```

```text
A maintainer knows every test string the move must rename, so no list or rule is left naming an empty old place
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupMembershipArchUnitTest\.cs

```text
an old full name built at run time from pieces in a method body, which no literal sweep can read
```

###### Obligations



### S2

##### Covers

P2

```text
Run the calendar, Feature ordering and ranking and optional-feature layout rule on the compiled application and its test project.
```

```text
Each of the 18 types is found only in its Organisation set-up module namespace (2 Domain, 12 Application, 4 Application.OptionalFeatures), none is left in the loose Services namespaces, and the five unit tests sit beside the module in the namespace of their folder; a made-up type left behind or put in the wrong layer is named.
```

```text
The first half of Organisation set-up lives in its module, with its unit tests beside it, and every other architecture rule stays green.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CalendarOrderingAndOptionalFeaturesLiveInOrganisationSetupArchUnitTest\.cs

```text
A type moved into the module but also kept in its old namespace, or a Domain candidate placed in Application, would pass a rule that only checked the old namespace was empty; the namespace-only and layer rules name both.
```

###### Obligations






### S3

##### Covers

P2

```text
Run the settings, terminology and connections placement rules on the compiled assembly, the test sources and the Tracker integration edge list
```

```text
Each of the 9 types is found only in its module namespace (4 in Application, 5 in Application.Connections), none in the loose Services namespaces; the 4 unit tests sit beside them in their layer folder with the folder namespace; Tracker integration lists exactly the ConnectionHealthCadence to IAppSettingService edge. Before the move every one of these is reported by name
```

```text
Settings, terminology, connection defaults and connection usage live in the Organisation set-up module with their unit tests, and the application behaves as before
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/SettingsTerminologyAndConnectionsLiveInOrganisationSetupArchUnitTest\.cs

```text
A type copied into the module but kept in its old namespace, a connection type placed in the plain Application namespace, a unit test left in an old folder, a second or missing Tracker edge: all are named by the made-up offender checks, so none passes
```

###### Obligations






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
