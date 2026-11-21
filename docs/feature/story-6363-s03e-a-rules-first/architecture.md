# Architecture brief: story\-6363\-s03e\-a\-rules\-first

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Confirmed (D-0, D-12, P5): feature A changes no production code, schema, DI registration, configuration or Program.cs. Only test-side architecture rule files, ModuleRules, the EXAMINE command builder, its two test scripts and .nwave/examine.json change. Nothing moves in feature A; the moves and the licence test data are feature B (story-6363-s03e-b-move); the modules own rules, outbound lists and test placement are feature C.
```

#### D2

```text
Confirmed (D-1): the Access membership is the 27 files of the ADR-270 Access set (RBAC administration, Feature move authorization, FeatureReadability, GroupClaimParser, OIDC group snapshot writer, current user profile, API keys, API-key principal factory, embed sessions, auth mode resolver, auth configuration validator, their interfaces, and the ASP.NET pieces ApiKeyAuthenticationHandler, DisabledAuthenticationHandler, SmartAuthSchemeSelector, BlockedModeFilter, RbacGuardAttribute, AccessMarkerAttribute, SignedInOnlyAttribute, CheckedInHandlerAttribute); the Licensing membership is LicenseService, LicenseVerifier, LicenseGuardAttribute, ILicenseService, ILicenseVerifier plus the public_key.pem resource. Entities, wire types, repositories, seeders, controllers, DTOs, API helpers, configuration, AuthPostureBanner and VoterIdentityResolver stay with their named owner. A2 measures the list from the IL and pins it.
```
Governed slices: S2

#### D3

```text
Confirmed (D-2): a Models type is admitted into Access or Licensing only if it is not an EF entity, not on the wire, not reached by a kernel type and not used by code that stays outside. Candidates (E11): ApiKeyOwnerResolutionState, ApiKeyValidationResult, AuthConfigurationValidationResult, EmbedSessionTokenRedemption, EffectiveAccess, PermissionRule, RbacOperationResult. A2 evaluates each candidate against the IL and pins the admitted list by name; a candidate that fails a condition stays in Models.
```
Governed slices: S2

#### D4

```text
Confirmed (D-3): target namespaces are Lighthouse.Backend.Modules.Access.Domain, .Application, .Adapters.Http and Lighthouse.Backend.Modules.Licensing.Application, .Adapters.Http. A type is Domain if its IL reaches only the kernel and the BCL; ApiKeyPrincipalFactory, FeatureReadability and GroupClaimParser are measured by A2 and their layer pinned. The eight ASP.NET pieces and LicenseGuardAttribute are Adapters.Http; Licensing has no Domain.
```
Governed slices: S2

#### D5

```text
Confirmed (D-7): Access Application reaching Licensing Application is legal by namespace (Licensing is upstream of every context). The A1 widening must not turn the existing AuthModeResolver and RbacAdministrationService reads of ILicenseService into offenders; Licensing reaches no Access type.
```
Governed slices: S1

#### D6

```text
Confirmed (D-8, Ratchet design: Rules first): A1 adds AccessModule ^Lighthouse\.Backend\.Modules\.Access(\.|$) and LicensingModule ^Lighthouse\.Backend\.Modules\.Licensing(\.|$) to ModuleRules and adds them as alternatives to every rule key that names the old Auth, Authorization or Licensing namespaces: ModuleBoundariesArchUnitTest.RbacIdentityPattern, TrackerIntegrationModuleBoundariesArchUnitTest.RefinementReportsAccessAndLicensing, WorkItemsModuleBoundariesArchUnitTest.AccessAndLicensing, PromisesModuleBoundariesArchUnitTest.AccessAndLicensing, and ForecastingModuleBoundariesArchUnitTest.TheLicence (Licensing alternative only). A1 re-greps every Architecture rule file for these namespaces at its start and widens what it finds; the list is not trusted as complete. Old alternatives stay until C3 retires them.
```
Governed slices: S1

#### D7

```text
Confirmed (D-9, D-11 and design review round 1): full-name strings, global:: fixture references and typeof uses follow the type in the B slice that moves it, not in feature A. No classifier slice: BACKEND_REGEX already classifies every Lighthouse.Backend path as backend-only, so the selection stays observable through test-selection-show.
```

#### D8

```text
Confirmed (D-12, X3): a new EXAMINE command access-answers-compare (CREATE_NEW, thin, on the shared capture and compare pieces of build-examine-commands in the shape of deliveries-answers-compare) starts each revision in-process with authentication off, reads auth mode, session, my-summary, RBAC status, system-wide rules, everyone-signed-in, API keys and licence status, imports the premium licence fixture, reads the licence status again, and compares the bodies: IDENTICAL exit 0, DIFFERENT exit 1, and it fails when nothing was compared. Its compare stage is reachable on two capture folders (--compare-captures BASE CANDIDATE). Declared in .nwave/examine.json with an access-answers-selftest probe before the first EXAMINE.
```
Governed slices: S3

#### D9

```text
Confirmed (D-12, X4): a new EXAMINE command ci-auth-suites-show (CREATE_NEW beside ci-live-suites-show, reusing its run lookup by first-parent revision) prints, for the newest finished Build And Deploy Lighthouse run at or before a revision, the conclusion of the Verify Authentication job and of its steps Run Auth Playwright tests, Run RBAC Playwright tests and Run proxied-auth Playwright tests (step names read from ci_verifyauth.yml lines 137, 206, 277; the job name as gh lists it is measured by A4 from a recorded run). A step absent from the run is shown as missing, never as passed. gh is reached through PATH, so the slice test runs the command against a recorded gh fixture. Declared in .nwave/examine.json.
```
Governed slices: S4

#### D10

```text
Confirmed (D-13 and maintainer answers 2026-11-17): rigor thorough for feature A (architect, acceptance designer, crafter opus; reviewers and EXAMINE sonnet); no review window; the mutation bar applies once at the end of S03e to the moved production code, and feature A changes no production code. Usage-data event, UI sketch, loading and failure looks: not applicable.
```

#### D11

```text
Carriers: rule baselines are inline string lists in their test classes, keyed by full type name and compared both ways (ADR-268); the IL is read through IlDependencies over Mono.Cecil; module namespace keys are const strings in ModuleRules, owner ModuleRules; the membership, layer and admissions lists are a static class AccessAndLicensingMembership in the Architecture folder (shape of PromisesMembership: names without namespace, a full name only where a short name is shared), owner the A2 oracle; made-up offenders live in fixture namespaces under Lighthouse.Backend.Modules.Access, Lighthouse.Backend.Modules.Licensing and their old-place twins, owner the A1 oracle; the two EXAMINE commands are generated bash scripts under Scripts/examine/bin, owner build-examine-commands. No production carrier, effect, error type or per-process state changes.
```

#### D12

```text
Formal verification not applicable (feature-delta Formal verification): every feature A claim is decided exactly by the compiled IL with a made-up offender per rule, by fixture capture pairs for the compare command and by a recorded gh fixture for the CI probe. No state, ordering, concurrency, recovery or liveness claim changes; a Quint, TLA+ or Agda model would restate the namespace map and could not fail, so no model is delivered. Property tests not applicable: every domain is finite and enumerated. Paradigm object_oriented is kept; the targets are test-side rules and bash scripts outside the OO domain core, so no domain owner, value object or Object Calisthenics rule is in play.
```

### Constraints

#### K1

```text
Behaviour-free: routes, DTOs, responses, authentication schemes, cookie names, configuration keys, RbacGuard on every action, DI, handler order and the EF model stay the same; every existing test stays green.
```

#### K2

```text
Never loosen a rule: a widening adds the Modules.Access or Modules.Licensing namespace to what a rule sees; exemptions are exact per type (that type and its nested types by full name), never a namespace or layer. Every rule edit is proved by a reverted throwaway probe.
```

#### K3

```text
IlDependencies.TypesIn also returns nested types; an exact-set check compares top-level types (the part before +). IlDependencies misses catch-clause, event and attribute named-argument references.
```

#### K4

```text
One oracle file per slice; an approved oracle or support file of this feature is never edited, not even a using line; oracles name production types as strings. No nWave scenario tags in code; the Lighthouse test-writing skill wins over nWave timing and PBT defaults.
```

#### K5

```text
The EXAMINE capture runs with authentication off (one subject); auth-on answers are covered by the Verify Authentication runs (ci-auth-suites-show). OIDC sign-in with a real identity provider outside CI and embed handshakes from a foreign host are not observable by any probe. No stored or validated input bound changes; none is introduced.
```

### Reuse

ModuleRules

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:15

Extend

```text
Owns the module namespace constants and shared rules; gains AccessModule and LicensingModule beside PromisesModule; no rule copied.
```

PromisesRulesSeeTheModuleArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesRulesSeeTheModuleArchUnitTest\.cs\:21

Reuse

```text
Its made-up offender under a module namespace beside a twin in the old place, asserted by reading each rule key, is the approach A1 follows.
```

PromisesMembership

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesMembership\.cs\:9

Reuse

```text
Shape for the Access and Licensing membership lists: names without namespace so one list holds before and after the move, a full name only where a short name is shared.
```

IlDependencies

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/IlDependencies\.cs\:13

Reuse

```text
Reads types and edges from the compiled IL over Mono.Cecil; every A1 and A2 check reads through it; no second IL reader.
```

deliveries\-answers\-compare

Scripts\/examine\/build\-examine\-commands\:670

Reuse

```text
Its shared capture pieces and --compare-captures stage carry the new access-answers-compare; the new command needs other endpoints, the licence import and its own nothing-compared message.
```

ci\-live\-suites\-show

Scripts\/examine\/build\-examine\-commands\:1483

Reuse

```text
Its lookup of the newest finished CI run at or before a revision carries ci-auth-suites-show; the new command reads a different job and its steps.
```

build\-examine\-commands

Scripts\/examine\/build\-examine\-commands\:1

Extend

```text
Owns every generated EXAMINE command and the shared pieces; gains two commands and one selftest, no copy of the pieces.
```

### Impact

None.

### Agreement

None.

### Not applicable

agreement

```text
No shared contract changes shape: .nwave/examine.json gains entries within its existing format, the generated commands are read by the examiner only, and no wire, DTO, schema or classifier output changes.
```

contract_tests

```text
No producer and consumer change apart: one deployable, no wire or persisted format changes.
```

### Boundaries

```text
Architecture rule tests over the compiled production IL (dotnet test, Architecture namespace); the EXAMINE command builder and its generated access-answers-compare and ci-auth-suites-show, each driven by its bash test script
```

#### Driven ports

```text
IlDependencies module reader over Mono.Cecil
```

```text
git revisions and the backend HTTP API, run in-process with authentication off, for the answers capture
```

```text
gh CLI on PATH for CI runs and jobs (GitHub Actions API)
```

```text
Tests and scripts read production; production never references a test, fixture or script.
```

#### Failures

None.

### Observations

None.

### Equality

None.

### Constructors

None.

### Laws

None.

### Refuted

None.

### Claims

None.

### States

Admitted

```text
Production unchanged; every rule naming an Access or Licensing namespace also sees Modules.Access and Modules.Licensing; membership, layers and admissions pinned by name; both probes declared.
```

story\-6363\-s03e\-a\-rules\-first

Excluded

```text
A type under Modules.Access or Modules.Licensing that a rule keyed on the old namespaces would miss; a loosened rule or a namespace-wide exemption; an IDENTICAL verdict on a changed or empty comparison; a failed or missing auth step shown as passed.
```

story\-6363\-s03e\-a\-rules\-first

Unknown

```text
The admitted Models types and the layer of ApiKeyPrincipalFactory, FeatureReadability and GroupClaimParser (A2 measures); which listed endpoints answer with authentication off on demo data (A3 measures); the Verify Authentication job name as gh lists it (A4 measures).
```

story\-6363\-s03e\-a\-rules\-first

### Models

None.

### Closure

```text
Feature A guards for the Access and Licensing move
```

Safety

Pass

DesignedNotBuilt

```text
A made-up offender under Modules.Access or Modules.Licensing is named by each widened rule; a list check fails on a missing and on an extra entry; the compare command cannot answer IDENTICAL for a changed or empty comparison; the CI probe cannot show a failed or missing step as passed.
```

##### Evidence

None.

```text
Feature A guards for the Access and Licensing move
```

Inhabitation

Pass

DesignedNotBuilt

```text
Today code passes every widened rule and the pinned membership lists; two identical capture folders answer IDENTICAL; a recorded run with all steps passing shows each as success.
```

##### Evidence

None.

```text
Feature A guards for the Access and Licensing move
```

PublicReachability

Pass

DesignedNotBuilt

```text
Each guard is reached through its public driver: dotnet test on the Architecture namespace, bash on the two probe test scripts, and the examiner probes architecture-rules, access-answers-compare, access-answers-selftest and ci-auth-suites-show declared in .nwave/examine.json.
```

##### Evidence

None.

```text
Feature A guards for the Access and Licensing move
```

Liveness

Pass

DesignedNotBuilt

```text
Not applicable: no queue, schedule, retry or eventual progress claim; every guard is a bounded synchronous run.
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
Assumption negated: rules see Access and Licensing wherever they live. A rule keyed on Services...Auth, Authorization or Licensing misses a moved type, and Forecasting TheLicence stops seeing ILicenseService.
```

```text
Every rule naming Access or Licensing names the made-up Modules offender
```

```text
Rule keys gain the Modules.Access and Modules.Licensing alternatives
```

```text
A moved type escaping a rule; a widening that makes the legal Access to Licensing edge red
```

```text
A key built by string concatenation that the re-grep misses: the both-ways list comparisons fail loudly on rename in B
```

story\-6363\-s03e\-a\-rules\-first

#### Y2

Bypass

```text
IlDependencies misses catch-clause, event and attribute named-argument references
```

```text
Edges through signatures, fields, calls, constructors and attribute types
```

```text
Those three reference kinds stay unseen
```

```text
Claiming the rules see them
```

```text
C1 falsifier with an attribute-only offender; named-argument blind spot recorded
```

story\-6363\-s03e\-c\-edges\-and\-rules

#### Y3

Stressor

```text
Assumption negated: every listed Access and Licensing endpoint answers with authentication off on demo data
```

```text
No false IDENTICAL
```

```text
The command fails loudly when nothing was compared or an answer is not 2xx
```

```text
IDENTICAL on an empty or failing comparison
```

```text
A3 reports which endpoints answer auth-off; auth-on answers stay with the Verify Authentication runs
```

story\-6363\-s03e\-a\-rules\-first

#### Y4

Stressor

```text
Assumption negated: the Verify Authentication job and step names in CI stay as ci_verifyauth.yml names them today
```

```text
A renamed or absent step is shown as missing
```

```text
The probe prints missing for a step it cannot find
```

```text
A missing or failed step shown as passed; no run found treated as success
```

```text
A workflow rename needs the probe step names updated by its owner
```

story\-6363\-s03e\-a\-rules\-first

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs

Extend

```text
Add the AccessModule and LicensingModule namespace constants.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs

Extend

```text
RbacIdentityPattern (line 22) also matches Modules.Access and Modules.Licensing; its not-empty assertion keeps holding on today code.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs

Extend

```text
RefinementReportsAccessAndLicensing (line 58) also matches Modules.Access and Modules.Licensing.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs

Extend

```text
AccessAndLicensing (line 55) also matches Modules.Access and Modules.Licensing; list strings are renamed in B, not here.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs

Extend

```text
AccessAndLicensing (line 60, no Access or Licensing from Promises) also matches Modules.Access and Modules.Licensing.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs

Extend

```text
TheLicence (line 59, the licence selector used at lines 124-128) also matches Modules.Licensing; the listed ForecastFilterRuleService edge stays listed.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs

Extend

```text
Only if the oracle shows a Modules.Access or Modules.Licensing type reaching a repository goes unreported; listed entries stay.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingRulesSeeTheModulesArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~AccessAndLicensingRulesSeeTheModulesArchUnitTest"
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

None.

#### Public oracle

```text
The architecture rule tests run (dotnet test, Architecture namespace) with made-up Access and Licensing types under Lighthouse.Backend.Modules.Access and Lighthouse.Backend.Modules.Licensing (fixture namespaces compiled into the IL the rules read) that reach Work Items, Tracker integration, Promises or a repository, each beside its twin under the old Services Auth, Authorization or Licensing namespace, plus a made-up Forecasting Application type reaching a made-up Modules.Licensing type, a made-up Access Application type reaching a made-up Licensing Application type, and a namesake just outside Modules.Access and Modules.Licensing.
```

```text
Each rule that names an Access or Licensing namespace names the made-up Modules offender exactly as it names its twin in the old place, and Forecasting licence selector catches the Forecasting type reaching the made-up Modules.Licensing type; the Access to Licensing edge and the namesake are not named; every existing architecture test stays green on today code.
```

```text
Today the made-up offenders under Modules.Access and Modules.Licensing pass the rules (RED). A widening that misses one rule key, ModuleRules or TheLicence leaves an offender unreported; a widening that matches the namesake, makes the Access to Licensing edge red or loosens a rule fails the oracle or an existing test.
```

##### Promises

P1

P5

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

RbacIdentityPattern

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:22

Extend

```text
The module map key for Access and Licensing; widened, never narrowed; retired in C3.
```

RefinementReportsAccessAndLicensing

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:58

Extend

```text
Tracker integration leaf rule key naming Access and Licensing; widened, never narrowed.
```

AccessAndLicensing

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:55

Extend

```text
Work Items outbound target key; widened, never narrowed.
```

AccessAndLicensing

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs\:60

Extend

```text
Promises rule key (no Access or Licensing); widened, never narrowed.
```

TheLicence

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:59

Extend

```text
Forecasting outbound licence selector; widened with the Licensing module only.
```

RepositoryLeakRatchetArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs\:25

Extend

```text
Owns the repository-dependency ratchet; extended only if the oracle shows a Modules.Access or Modules.Licensing type reaching a repository goes unreported.
```

##### Impact

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:27

Changed

```text
New AccessModule and LicensingModule constants.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:22

Changed

```text
Key also sees Modules.Access and Modules.Licensing.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:58

Changed

```text
Key also sees Modules.Access and Modules.Licensing.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsModuleBoundariesArchUnitTest\.cs\:55

Changed

```text
Key also sees Modules.Access and Modules.Licensing.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs\:60

Changed

```text
Key also sees Modules.Access and Modules.Licensing.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:59

Changed

```text
Licence selector also sees Modules.Licensing.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs\:25

UnchangedCompatible

```text
Expected unchanged; edited only if the oracle shows a Modules.Access or Modules.Licensing type reaching a repository goes unreported.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1

UnchangedCompatible

```text
No production edit in feature A.
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
The rule keys are widened in place; no preserving move makes the widening easier.
```

##### Boundaries

None.

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

###### L1

```text
For each widened rule key, a made-up type under Modules.Access or Modules.Licensing is named exactly when its twin under the old Access or Licensing namespace is named; the legal Access to Licensing edge and a namesake outside the modules are never named.
```

Invariant

Infrastructure

AccessAndLicensingRulesSeeTheModulesArchUnitTest

###### Treatment

Finite

###### Cases

```text
Access offender reaching Work Items
```

```text
Access or Licensing offender reaching Tracker integration
```

```text
Promises reaching a made-up Access or Licensing type
```

```text
Access offender reaching a repository
```

```text
Forecasting Application type reaching a made-up Modules.Licensing type
```

```text
Access Application reaching Licensing Application: not named
```

```text
namesake just outside Modules.Access and Modules.Licensing: not named
```

```text
today code: no offender named
```

```text
Closed finite set of rule keys and cases; each case is enumerated, no generator adds coverage.
```

###### Evidence

None.

##### Refuted

None.

##### Claims

None.

##### States

None.

##### Models

None.

##### Closure

None.

##### Risks

None.

##### Architectural tests

###### T1

```text
Every architecture rule that names an Access, Authorization or Licensing namespace also sees Lighthouse.Backend.Modules.Access and Lighthouse.Backend.Modules.Licensing, proved by a made-up offender there.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingRulesSeeTheModulesArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~AccessAndLicensingRulesSeeTheModulesArchUnitTest"
```

##### Contract tests

None.

### S2

#### Targets

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingMembershipArchUnitTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~AccessAndLicensingMembershipArchUnitTest"
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingMembership\.cs

#### Public oracle

```text
The architecture rule tests run the membership check over the compiled production IL: the Access and Licensing membership, each type layer (Domain or Application, and Adapters.Http for the ASP.NET pieces) and the admitted Models types listed by name in AccessAndLicensingMembership, compared with the top-level types the IL finds in the old Services Auth, Authorization and Licensing namespaces together with Modules.Access and Modules.Licensing, and the Models candidates evaluated against the four admission conditions.
```

```text
The check passes on today code and finds each listed type in its old or its new namespace; a type listed as Domain reaches only the kernel and the BCL; each admitted Models type meets the four conditions and each candidate left out names the condition it fails. A failure names each missing listed type, each name that does not exist and each unlisted type that appears.
```

```text
A list with one entry removed, one made-up entry added, or a type that does not exist fails and names it; a type listed as Domain that reaches an Application or platform type fails. A check that compares one way only, counts entries, reads only the old or only the new namespace, or matches a shared short name without its namespace passes those cases and is rejected.
```

##### Promises

P2

P5

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

PromisesMembershipArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesMembershipArchUnitTest\.cs\:18

Reuse

```text
Shape of the namespace-agnostic both-ways membership comparison and the four-condition admission check.
```

##### Impact

None.

##### Agreement

None.

##### Not applicable

impact

```text
No crafter target: the acceptance designer authors the oracle and its membership list; no production or rule file changes.
```

prefactoring

```text
No code target.
```

##### Boundaries

None.

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

###### L2

```text
The listed membership equals the set of top-level types the IL finds in the old Access and Licensing namespaces and in Modules.Access and Modules.Licensing, both ways, each listed name found exactly once in its old or its new namespace; a Domain-listed type reaches only the kernel and the BCL.
```

Invariant

Infrastructure

AccessAndLicensingMembershipArchUnitTest

###### Treatment

Finite

###### Cases

```text
list equals IL: pass
```

```text
listed type missing: named
```

```text
listed name that does not exist: named
```

```text
unlisted type present: named
```

```text
type found in the new namespace instead of the old: pass
```

```text
nested type: reduced to its outer type
```

```text
Domain-listed type reaching beyond kernel and BCL: named
```

```text
Models candidate failing a condition: left out with the condition named
```

```text
Closed finite set of cases; each case is enumerated, no generator adds coverage.
```

###### Evidence

None.

##### Refuted

None.

##### Claims

None.

##### States

None.

##### Models

None.

##### Closure

None.

##### Risks

None.

##### Architectural tests

###### T2

```text
The Access and Licensing membership, each type layer and the Models admissions are measured from the IL and pinned both ways, namespace-agnostic.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingMembershipArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~AccessAndLicensingMembershipArchUnitTest"
```

##### Contract tests

None.

### S3

#### Targets

Scripts\/examine\/build\-examine\-commands

Extend

```text
Generate access-answers-compare on the shared capture and compare pieces with a --compare-captures stage, and access-answers-selftest that runs the slice test script.
```

\.nwave\/examine\.json

Extend

```text
Declare access-answers-compare and access-answers-selftest as admitted EXAMINE probes.
```

Scripts\/examine\/test\-access\-answers\-compare\.sh

#### Verification

```text
bash Scripts/examine/test-access-answers-compare.sh
bash Scripts/examine/build-examine-commands
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

None.

#### Public oracle

```text
bash Scripts/examine/test-access-answers-compare.sh builds the EXAMINE commands and runs access-answers-compare --compare-captures on fixture capture folders it writes: one pair that differs in one Access or Licensing answer (for example the licence status after the import), one identical pair, and one empty pair.
```

```text
The differing pair prints DIFFERENT naming the changed answer and exits 1; the identical pair prints IDENTICAL and exits 0; the empty pair fails because nothing was compared. The command usage names the captured answers: auth mode, session, my-summary, RBAC status, system-wide rules, everyone-signed-in, API keys, licence status before and after a licence import.
```

```text
Today the command does not exist (RED). A command that always prints IDENTICAL, always prints DIFFERENT, or answers IDENTICAL when nothing was compared fails the script.
```

##### Promises

P3

P5

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

compare\_captures

Scripts\/examine\/build\-examine\-commands\:364

Reuse

```text
Shared compare stage over two capture folders; fails when a side is not a capture folder or an answer is not 2xx.
```

deliveries\-answers\-selftest

Scripts\/examine\/build\-examine\-commands\:1548

Reuse

```text
Model for the selftest probe that runs the slice test script for the examiner.
```

commands

\.nwave\/examine\.json\:2

Extend

```text
The commands list owns the admitted EXAMINE probes; two entries added.
```

##### Impact

script

Scripts\/examine\/build\-examine\-commands\:670

Changed

```text
New generated access-answers-compare beside deliveries-answers-compare, and access-answers-selftest.
```

config

\.nwave\/examine\.json\:2

Changed

```text
Two new probe entries.
```

script

Scripts\/examine\/build\-examine\-commands\:364

UnchangedCompatible

```text
Existing compare commands keep their behaviour; the shared pieces are reused, not changed.
```

##### Agreement

None.

##### Not applicable

architectural_tests

```text
No dependency, boundary or owner law; the obligation is behavioural and covered by the script oracle.
```

prefactoring

```text
The compare stage already takes capture folders (deliveries-answers-compare --compare-captures); no preserving move is needed.
```

##### Boundaries

None.

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

###### L3

```text
compare(X, X) is IDENTICAL with exit 0; compare(X, Y) with one Access or Licensing answer changed is DIFFERENT with exit 1 naming it; a comparison of empty captures fails.
```

Algebraic

Adapter

access\-answers\-compare

###### Treatment

Finite

###### Cases

```text
identical pair
```

```text
one answer changed
```

```text
nothing compared
```

```text
Closed finite set of cases; each case is enumerated, no generator adds coverage.
```

###### Evidence

None.

##### Refuted

None.

##### Claims

None.

##### States

None.

##### Models

None.

##### Closure

None.

##### Risks

None.

##### Architectural tests

None.

##### Contract tests

None.

### S4

#### Targets

Scripts\/examine\/build\-examine\-commands

Extend

```text
Generate ci-auth-suites-show beside ci-live-suites-show, reusing its run lookup.
```

\.nwave\/examine\.json

Extend

```text
Declare ci-auth-suites-show as an admitted EXAMINE probe.
```

Scripts\/examine\/test\-ci\-auth\-suites\-show\.sh

#### Verification

```text
bash Scripts/examine/test-ci-auth-suites-show.sh
bash Scripts/examine/build-examine-commands
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

None.

#### Public oracle

```text
bash Scripts/examine/test-ci-auth-suites-show.sh builds the EXAMINE commands and runs ci-auth-suites-show for a revision with a recorded gh fixture on PATH: one finished run whose Verify Authentication job has the auth and proxied-auth steps passed and the RBAC step failed, and one whose three steps all passed.
```

```text
For each revision the command prints the CI run, the Verify Authentication job conclusion and one line each for the authentication, RBAC and proxy-auth steps with their conclusion; the failed RBAC step is shown as failure and the job as failure; the all-passed run shows success on every line.
```

```text
Today the command does not exist (RED). A command that prints success for every step, reads the backend job instead of Verify Authentication, shows a missing step as passed, or ignores the step conclusions fails the script.
```

##### Promises

P4

P5

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

commands

\.nwave\/examine\.json\:2

Extend

```text
The commands list owns the admitted EXAMINE probes; one entry added.
```

##### Impact

script

Scripts\/examine\/build\-examine\-commands\:1483

Changed

```text
New generated ci-auth-suites-show beside ci-live-suites-show.
```

config

\.nwave\/examine\.json\:2

Changed

```text
New probe entry.
```

ci

\.github\/workflows\/ci\_verifyauth\.yml\:137

UnchangedCompatible

```text
Read only: the step names the probe shows; the workflow does not change.
```

##### Agreement

None.

##### Not applicable

architectural_tests

```text
No dependency, boundary or owner law; the obligation is behavioural and covered by the script oracle.
```

prefactoring

```text
The run lookup of ci-live-suites-show is reused as it is; no preserving move makes the new command easier.
```

##### Boundaries

None.

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

###### L4

```text
For a found run, each of the three auth steps is printed with exactly the conclusion the run records, and the job line with the job conclusion; a step absent from the run is printed as missing; no finished run within the lookup window exits 1.
```

Invariant

Adapter

ci\-auth\-suites\-show

###### Treatment

Finite

###### Cases

```text
all three steps success
```

```text
RBAC step failure, others success
```

```text
step absent from the run: missing
```

```text
no finished run found: exit 1
```

```text
Closed finite set of recorded fixtures; each case is enumerated, no generator adds coverage.
```

###### Evidence

None.

##### Refuted

None.

##### Claims

None.

##### States

None.

##### Models

None.

##### Closure

None.

##### Risks

None.

##### Architectural tests

None.

##### Contract tests

None.

## ADRs

None.
