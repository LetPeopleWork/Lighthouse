# Acceptance brief: story\-6363\-s03e\-a\-rules\-first

## Slices

### S1

##### Covers

P1

```text
A made-up Access service and a made-up Licensing service under their new modules reach a repository, each beside its twin in the old place, a namesake just outside the module and a service that reads values only; the module map's Access and Licensing key is run over them.
```

```text
The module map names the service under the module exactly as it names its twin in the old place; the namesake and the values-only service are not named.
```

```text
A rule built on the module map keeps seeing Access and Licensing code after it moves to its module.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingRulesSeeTheModulesArchUnitTest\.cs\:\:AServiceUnderTheModuleReachingARepository\_IsNamedByTheModuleMapsAccessAndLicensingNamespaces\_AsItsTwinInTheOldPlaceIs

```text
It still passes a key that also takes in an unrelated module; the other-context and shared-constant checks catch that. A key without the end anchor takes in Modules.AccessFixtures and fails; a key that sees only one of the two modules fails the other case.
```

###### Obligations



##### Covers

P1

```text
The shared module rules' Access and Licensing namespace constants are run over a made-up service under each module that reaches a repository, a namesake just outside the module, the same kind of service under the other module and a service that reads values only.
```

```text
Each constant names the service under its own module only; the namesake, the other module's service and the values-only service are not named.
```

```text
The modules' own rules have a namespace to point at that sees exactly one module.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingRulesSeeTheModulesArchUnitTest\.cs\:\:AServiceUnderTheModuleReachingARepository\_IsNamedByTheSharedModuleRulesNamespaceOfItsModule\_WhileANamesakeAndTheOtherModuleAreNot

```text
It still passes a constant written without the start anchor, which no real namespace outside Lighthouse.Backend would exercise. A constant without the end anchor, one combining both modules, or a missing constant fails.
```

###### Obligations



##### Covers

P1

```text
A made-up Work Items service, a made-up connector and a made-up Delivery service each reach a made-up Access service and a made-up Licensing service under the new modules, a twin in the old place and a namesake just outside; each context's own Access and Licensing key is run over them.
```

```text
Each context's rule names the type reaching the module exactly as it names the one reaching the old place; the one reaching the namesake is not named.
```

```text
Work Items, Tracker integration and Promises keep being stopped from reaching Access and Licensing except through their listed edges, before and after the move.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingRulesSeeTheModulesArchUnitTest\.cs\:\:AServiceOfAnotherContextReachingAnAccessOrLicensingServiceUnderTheModule\_IsNamedByThatContextsRule\_AsOneReachingTheOldPlaceIs

```text
It still passes a key widened with an extra unrelated namespace; the existing tests of each rule on today's code then show a new offender. A key that misses one context, one module or the end anchor fails a case.
```

###### Obligations






##### Covers

P1

```text
Made-up Forecasting Application types reach a made-up Licensing service under the new module, its twin in the old place, a Licensing namesake, a made-up Access service under the new module and one in the old place; Forecasting's licence selector is run over them.
```

```text
The selector names the types reaching Licensing under the module and in the old place; the namesake and both Access types are not named.
```

```text
Forecasting keeps asking the licence only through its listed edge after Licensing moves.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingRulesSeeTheModulesArchUnitTest\.cs\:\:AForecastingApplicationTypeReachingALicensingServiceUnderTheModule\_IsNamedByForecastingsLicenceSelector\_AsOneReachingTheOldPlaceIs

```text
It still passes if the listed forecast filter rule service edge were dropped from the list; the existing Forecasting rule on today's code catches that. A selector that misses Modules.Licensing, takes in Access, or loses its end anchor fails.
```

###### Obligations



##### Covers

P1

```text
A made-up Access service under the new module asks a made-up Licensing service under the new module, beside the same pair in the old places; the module map's Access and Licensing key is applied to both ends of each edge.
```

```text
Both ends of both edges fall inside the module map's Access and Licensing module, so the edge is never a crossing between modules.
```

```text
Access asking the licence stays legal after both move, as it is today.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingRulesSeeTheModulesArchUnitTest\.cs\:\:AnAccessServiceAskingTheLicenceUnderTheModules\_StaysInsideOneModuleOnTheModuleMap\_AsItsTwinInTheOldPlaceDoes

```text
It still passes if a later rule written with the separate shared constants treated Access to Licensing as a crossing; such a rule belongs to the modules' own rules, outside this feature. A map that sees only one of the two modules fails.
```

###### Obligations


##### Covers

P5

```text
Every existing architecture test and the whole backend suite run on today's code after the widening.
```

```text
No existing rule names a new offender and every existing test stays green; the module map's Access and Licensing group is still not empty.
```

```text
Nothing a user, a tracker or the database sees changes.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture \(every existing architecture test\, run by the declared full backend test command\)

```text
It still passes a widening that matches nothing at all today; the module-place checks in this oracle catch that. A widening that loosens a rule or drops an old alternative turns an existing test red.
```

###### Obligations


### S2

##### Covers

P2

```text
The architecture rule tests read the compiled code and compare the Access and Licensing types they find, in the old Services Auth, Authorization and Licensing namespaces and under Modules.Access and Modules.Licensing, with the measured lists of each module and layer, and judge the seven named shared Models candidates against the four admission conditions.
```

```text
On today's code every listed type is found once in its old or new namespace and no unlisted type appears; Domain types reach only the shared Models, the base library and the types listed as Domain as the code holds them by full name, and Application classes reach more; four candidates are admitted and the other three are left out naming the condition they fail. A missing listed type, a made-up listed name, an unlisted type, a Domain type reaching the Application or the platform, a Domain type reaching an Application type that shares its short name with a Domain-listed type, or a shared short name in two namespaces fails and is named.
```

```text
The move of Access and Licensing has one fixed, measured list of what moves and to which layer; any drift from it fails a test that names the type.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingMembershipArchUnitTest\.cs

```text
A type that moves to the wrong layer of the right module after the move, since before the move the lists and not the namespaces carry the layer; the module layout rules of the later feature own that.
```

###### Obligations










##### Covers

P5

```text
The slice adds only a test and its list; the whole backend is built and its test suite run without the live tracker categories.
```

```text
The backend builds with no warnings, no production file changes, and every existing backend test stays green next to the new membership test.
```

```text
Nothing a user, a tracker or the database sees changes.
```

##### Disposition

Tested

Lighthouse\.Backend \(full backend suite\, live tracker categories excluded\)

```text
A production change no existing test observes; the diff of the slice, which touches only the two test files, rules that out.
```

###### Obligations


### S3

##### Covers

P3

```text
Two recorded captures of the Access and Licensing answers that differ only in the licence status after the premium licence import are compared.
```

```text
The comparison ends on DIFFERENT and exits 1; its per-answer lines show the licence status after the import as 1 compared, 1 different and the unchanged auth mode as 1 compared, 0 different; the differences name the changed answer.
```

```text
The examiner sees a changed Access or Licensing answer flagged, and which answer it is.
```

##### Disposition

Tested

Scripts\/examine\/test\-access\-answers\-compare\.sh \:\: one Access or Licensing answer changed

```text
A comparison that flags only the first differing answer when several differ would still pass, because only one answer differs here.
```

###### Obligations


##### Covers

P3

```text
Two separate recorded captures holding the same ten Access and Licensing answers are compared.
```

```text
The comparison ends on IDENTICAL over all 10 answers and exits 0.
```

```text
The examiner sees that a move left every Access and Licensing answer as it was.
```

##### Disposition

Tested

Scripts\/examine\/test\-access\-answers\-compare\.sh \:\: identical Access and Licensing answers

```text
A comparison that ignores whitespace-only differences in a body would still pass, because the identical pair is a byte copy.
```

###### Obligations


##### Covers

P3

```text
Two empty capture folders are compared.
```

```text
The comparison does not exit 0, is not called IDENTICAL, and says that nothing was compared.
```

```text
The examiner cannot mistake a capture that recorded nothing for proof that nothing changed.
```

##### Disposition

Tested

Scripts\/examine\/test\-access\-answers\-compare\.sh \:\: nothing captured

```text
A comparison that accepts a capture holding only some of the ten answers would still pass, because only the fully empty case is checked here.
```

###### Obligations


##### Covers

P3

```text
The examiner reads the usage of the Access and Licensing answers command.
```

```text
The usage names every captured answer: auth mode, session, my summary, RBAC status, system-wide rules, everyone signed in, API keys, and the licence status before and after a licence import.
```

```text
The examiner knows from the usage which answers a green run proves unchanged.
```

##### Disposition

Tested

Scripts\/examine\/test\-access\-answers\-compare\.sh \:\: usage names the captured answers

```text
A usage that names an answer the capture does not actually read would still pass; whether the capture reads it is judged by review and by the examiner's full run.
```

###### Obligations

None.

##### Covers

P3

```text
The examiner looks up the admitted examine probes and runs the Access and Licensing answers selftest.
```

```text
Both the answers command and its selftest are admitted examine probes, and the selftest runs these Access and Licensing compare checks, passes, and prints their total.
```

```text
The examiner can run the command and see for themselves that it tells answers apart.
```

##### Disposition

Tested

Scripts\/examine\/test\-access\-answers\-compare\.sh \:\: admitted examine probes\; selftest

```text
A selftest that runs these checks but drops their output past the total would still pass.
```

###### Obligations

None.

##### Covers

P5

```text
The full backend test suite, without the live tracker connectors, runs on the candidate.
```

```text
Every existing test stays green; the slice adds only an examine command, its selftest and their probe entries, no production code.
```

```text
Nothing a user, a tracker or the database sees changes.
```

##### Disposition

Tested

dotnet test Lighthouse\.Backend with the live\-connector exclusion filter \(declared verification\)

```text
A production change that no existing test observes would still pass; review of the diff, which touches only Scripts and .nwave, closes that.
```

###### Obligations

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
A made-up Access or Licensing type placed under Modules/Access or Modules/Licensing that reaches Work Items, Tracker integration, Promises or a repository is reported by the architecture rules, exactly as one in the old namespaces is; and a made-up Forecasting Application type reaching a made-up Modules.Licensing type is reported by Forecasting's licence rule.
```

#### E2

##### Promises

P2

```text
A maintainer can see, in a test, the fixed list of Access and Licensing types with the layer (Domain or Application) of each and the Models types admitted; the test finds each listed type in its old or new namespace and fails when a listed type is missing or a listed name does not exist.
```

#### E3

##### Promises

P3

```text
Running the access-answers examine command on two revisions that give the same auth mode, session, my-summary, RBAC status, system-wide rules, everyone-signed-in, API keys and licence status (before and after a licence import) prints IDENTICAL and exits 0; if any one of these answers differs it prints DIFFERENT and exits 1.
```

#### E4

##### Promises

P4

```text
Running the CI examine command for a revision shows the Verify Authentication job conclusion and the conclusions of its authentication, RBAC and proxy-auth steps from the newest finished CI run at or before that revision, and a step that failed is shown as failed, not as passed or missing.
```

#### E5

##### Promises

P5

```text
A user, a tracker or the database sees no change in behaviour or data after this feature, and the existing test suites still pass.
```

## Charter

None.

## Examination

None.
