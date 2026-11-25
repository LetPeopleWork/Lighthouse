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
Ask every constant and static read-only string field of the test assembly that matches a moving type's old or new name, on the subject its rule reads, whether it keeps its declared role through the move; and check each pattern on OrganisationSetupPatternRoles.NamedByTheModuleOnly
```

```text
RED before the S1 rule edits, exactly the five Context patterns that see a set-up type in the old place and not in the new: the Work Items set-up pattern and its composite, the blackout seam's service implementation pattern, the board port's implementation pattern and the weekly recurrence full-name pattern, 80 type breaks in all; every Place pattern and every folder-only pattern holds; made-up keys show an undeclared pattern, a blind context, its composite and an old location widened into the new place are each named and a correctly widened context is not; no OrganisationSetup alternative reaches outside the module. Once the last slice names set-up by its module only, a Context pattern that sees moved types only in their new place passes only if it is on NamedByTheModuleOnly (exactly the Work Items set-up pattern, the blackout seam's and the board port's implementation patterns, Tracker integration's other contexts and the weekly recurrence pattern), and each entry must be a rule pattern of the test assembly, see no moved type in its old place and see one only in its new place; any other pattern that stops seeing the old place still fails
```

```text
A maintainer sees, by class and field, every rule that would silently stop seeing the set-up types at the move, and which rules rightly stay in the old place; after the move only the rules declared finished may stop naming the old place
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



##### Covers

P1

```text
Ask every rule pattern of the test assembly whether it keeps its role through the platform move, and check each pattern on PlatformLanePatternRoles.NoLongerReadTheLooseServicesNamespaces against the compiled product
```

```text
The Work Items set-up pattern, the blackout seam's and the board port's implementation patterns no longer read the loose Services.Implementation and Services.Interfaces namespaces and pass only because they are on that list; each entry is a rule pattern of the test assembly, matches no platform type whose old home was a loose namespace, sees a platform area only in its new place, and no platform type is still compiled into the loose namespaces (all 35 that lived there have left); a made-up entry that still reads the loose namespaces or names no rule pattern is named
```

```text
Narrowing set-up to its module cannot hide a platform type left behind in the loose Services namespaces
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneRulesSeeTheMoveArchUnitTest\.cs

```text
a platform type that lives in a loose namespace but is missing from the platform membership map
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






### S4

##### Covers

P3

```text
Read every outbound rule and outbound list of Work Items, Flow metrics, Forecasting, Promises, Refinement, Reports, Access and Licensing, and the compiled edges of Work Items into set-up.
```

```text
No downstream rule reads set-up's Application or Domain as foreign and no downstream list names an edge into set-up, while each rule still reads one named foreign namespace that is not set-up (the loose services, Licensing, Access, Work Items, the data layer or a set-up adapter; for Work Items' target, Licensing, since it names the loose services by full name once set-up has moved) and Work Items still reaches set-up's settings and Feature ordering in the compiled code. RED at S4 start: Work Items' list names RefreshLogService to IAppSettingService and WorkItemService to IFeatureOrdering, and Work Items' target reads all four set-up namespaces as foreign.
```

```text
Contexts that build on Organisation set-up use its settings, calendar and Feature ordering without an exception on a list, and still cannot reach anything else unseen.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupIsAnOpenHostArchUnitTest\.cs

```text
A downstream rule that admits set-up and also some other foreign namespace than the one checked for that rule.
```

###### Obligations





##### Covers

P3

```text
Read Tracker integration's other-contexts rule and its list, and apply the rule to a made-up Tracker integration type that asks the settings.
```

```text
Tracker integration still reads all four set-up namespaces as another context, its list holds exactly the connection-health cadence edge into the settings, and the made-up type's edge to the settings is named.
```

```text
Tracker integration keeps translating what the trackers send; a second dependency on set-up is caught before it ships.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupIsAnOpenHostArchUnitTest\.cs

```text
A Tracker integration type reaching set-up only through an event declaration or a named attribute argument, which the IL reader does not see.
```

###### Obligations





##### Covers

P3

```text
Read every construction of a Team deleted, blackout change, Feature ordering change or Feature rank change event in the compiled product, and in a made-up controller that publishes a blackout change.
```

```text
Outside set-up's Application only the Team controller constructs Team deleted, exactly as listed, and the made-up controller's blackout change is named.
```

```text
No second place can announce a set-up change that set-up never made.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupIsAnOpenHostArchUnitTest\.cs

```text
An event built by reflection or by a factory in another assembly, which no constructor call in the product's IL shows.
```

###### Obligations




##### Covers

P3

```text
Run set-up's own boundary rules and the placement check of its unit tests on the compiled product and the test project.
```

```text
Every set-up type lives in its layer and none is left under Services; set-up's Domain reaches only the kernel and the base library; its Application reaches the API, adapters, the data layer, EF Core and downstream contexts only along the listed edges (Feature ranking and its seeder into the database, the connection save into Work Items' fetch fingerprint and EF Core); Tracker integration reaches set-up only along the cadence edge; each deleted entry and a made-up stale entry are named; the nine moved unit tests sit in their layer folder with its namespace, and a made-up misplaced tree is reported.
```

```text
Organisation set-up keeps its own boundaries as it grows, and its tests are found beside it.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupModuleBoundariesArchUnitTest\.cs

```text
A new set-up type that no layer list names, since the layer rule checks the listed types only.
```

###### Obligations


### S5

##### Covers

P4

```text
Run the Context-field check: every Context rule pattern that is not folder-only and not on the exact list of 23 broad whole-backend, project-folder and Services-and-modules fields is matched against the old namespace or full name of each of the 27 moved types, with an empty rename list; the broad list is asserted equal to its expected names and each a declared Context field; planted fields (an old alternative, only the old place, Services alternated with the module) are named, a narrowed one passes
```

```text
RED at S5 start: names RecurringBlackoutEventsSeamArchUnitTest.ServiceImplementationNamespace, RefinementModuleArchUnitTest.WeeklyRecurrenceAndItsNestedTypesPattern, TrackerIntegrationApplicationLayoutArchUnitTest.AnImplementationNamespace, TrackerIntegrationModuleBoundariesArchUnitTest.OtherContexts, WorkItemsModuleBoundariesArchUnitTest.OrganisationSetUp (and no other field); GREEN once each names only the module
```

```text
A rule cannot keep naming the old Services place for set-up once the module exists, with or without naming the module
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupRulesNameOnlyTheModuleArchUnitTest\.cs

```text
an old Services alternative is re-added to any Context field not on the broad list it fails naming that field
```

###### Obligations











##### Covers

P4

```text
Sweep every source file of the test project for string literals naming a moved type by old full name or path with an empty rename list, excluding by exact path only the five files that must spell old names; assert the exclusion list equals exactly those paths, each existing; a planted old literal and a planted extra and missing exclusion path are named
```

```text
No literal names an old full name or path (already satisfied after S2-S4); the exclusion list is exactly the five paths and each exists; an extra or missing path is named
```

```text
No rule or list names a place the types no longer live in, and no real rule file can hide from the sweep
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupRulesNameOnlyTheModuleArchUnitTest\.cs

```text
an old full-name literal is re-added to a rule file or an extra path is added to the exclusion list it fails naming it
```

###### Obligations




##### Covers

P4

```text
Match every rule pattern declared as an old place (Place) against the new namespace or full name of each moved type
```

```text
No Place pattern matches a moved type in its new place (already satisfied; Place patterns untouched)
```

```text
Rules that ask whether a type is left in the old place never find it in the module
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupRulesNameOnlyTheModuleArchUnitTest\.cs

```text
the module is added to an old-place pattern it fails naming the field and type
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
