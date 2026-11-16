# Architecture brief: story\-6359\-s03a\-a\-rules\-first

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Feature A changes no production code: only test-side architecture rule files, RepositoryRecognition, the CI test-selection scripts and the examine command builder change. No file of the Tracker integration context moves here; the moves are feature B (story-6359-s03a-b-move), the module boundary rules, the unit-test placement rule and retiring old alternatives are feature C.
```

#### D2

```text
D-0 No schema change: no entity, DbSet, migration or table name changes; entities stay in Models.
```

#### D3

```text
D-3 Target path: Lighthouse.Backend/Lighthouse.Backend/Modules/TrackerIntegration with Domain, Application (plus Application/OAuth, Application/ConnectionHealth), Adapters/Connectors/{AzureDevOps,Csv,Jira,Linear,ServiceNow,Auth,OAuth}, Adapters/OAuth, Adapters/Background; namespace equals folder (Lighthouse.Backend.Modules.TrackerIntegration.<Layer>[.<Sub>]); tests mirror it under Lighthouse.Backend.Tests/Modules/TrackerIntegration. Feature A rules and scripts widen to exactly these paths.
```

#### D4

```text
Ratchet: every rule reads the compiled IL (IlDependencies over Mono.Cecil); lists are inline, compared both ways and never loosened. A widening adds the module namespace or path beside the old one; old alternatives are retired only in feature C once nothing lives there.
```

#### D5

```text
D-7 confirmed by the maintainer: the Portfolio/Delivery side of the leaf rule does not gain Services.(Implementation|Interfaces).DeliverySources in A1; that widening waits for C1, where the JiraWorkTrackingConnector to IDeliveryForecastBlockRenderer edge is listed for S03d. Widening it in A1 would turn A1 red on that existing edge.
```

#### D6

```text
D-11 CI selection: path-classifier.sh selects live connector suites by path, so A2 widens each tracker regex to also match Modules/TrackerIntegration/Adapters/Connectors/<Tracker>/ (production and tests), Jira also Modules/TrackerIntegration/Domain/ParentSourceSelector.cs, and SHARED_REGEX also Adapters/Connectors/(Auth|OAuth)/, Modules/TrackerIntegration/Application/, Modules/TrackerIntegration/Domain/ and Adapters/OAuth/. Old alternatives stay until C1/C2.
```

#### D7

```text
D-12 Behaviour-free proof reuses EXAMINE commands architecture-rules, ef-model-unchanged, backend-tests, connector-throws-count (after the A2 path fix), refresh-answers-compare and forecast-answers-compare; no new examine command.
```

#### D8

```text
Formal verification not applicable: the claims are which types and paths a rule or script sees and which IL edges exist, decided exactly by the compiled IL and the classifier with a made-up offender or path per rule as falsifier; no state, ordering, concurrency, recovery or liveness changes, so a Quint/TLA+/Agda model would restate the namespace map and could not fail. No checker was probed or run; any requested model claim stays INDETERMINATE.
```

#### D9

```text
Paradigm object_oriented (CLAUDE.md, repository). The rule carriers are the existing static test-side owners: ModuleBoundariesArchUnitTest.WorkTrackingIntegrationPattern (leaf rule), RepositoryRecognition.PlatformPersistencePattern (repository recognition), ModuleRules/ModuleWording (module rules), ConnectorRatchetFixtures (connector fixtures); scripts are procedural bash/pwsh. No domain decision is introduced, so no new domain owner.
```

#### D10

```text
Rigor thorough for feature A (crafter, acceptance designer, architect on opus; reviewers and EXAMINE on sonnet); push per slice with CI watched; rebase allowed when trunk moved.
```

#### D11

```text
D-2 Models admission: a Models type moves to Tracker integration only if it is (a) not an EF entity, (b) not on the wire, (c) not reached by a kernel (Models) type, and (d) not used by any code that stays outside the module (staying services, controllers, other contexts, events consumed elsewhere). So ConnectionFetchShapeChanged stays in Models/Events (published by the staying save service, handled by Work Items); WriteBackFieldUpdate, WriteBackItemResult, WriteBackResult and WriteBackValueSources stay in the kernel as the connector port's shared vocabulary (port signatures use only kernel types); OAuthCompleteResult stays, because the staying OAuthController reads it from IOAuthService.CompleteAsync.
```
Governed slices: S3

### Constraints

#### K1

```text
Behaviour-free: no route, DTO, response, UI, schema, DI registration or Program.cs change in feature A; UI sketch and loading/failure looks N/A.
```

#### K2

```text
Live connector categories (JiraIntegration, LinearIntegration, AdoIntegration, ServiceNowIntegration, GithubIntegration) are never run locally; CI runs them on push. The shared Linear key can rate-limit.
```

#### K3

```text
dotnet build ends with zero warnings; tests carry no nWave scenario tags; comments are rare and never cite internal ids.
```

#### K4

```text
Every existing architecture rule stays green on today code after the widening; no rule is loosened and no inline list grows except by an entry the authority names.
```

#### K5

```text
Input bounds: none stored or validated; the populations are the compiled types of Lighthouse.Backend and the repo-relative paths fed to the classifier. No bound on their count is set by the authority.
```

### Reuse

ModuleRules

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs\:15

Reuse

```text
Owns module rules and their wording; feature A copies no rule.
```

WorkItemsRulesSeeTheModuleArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsRulesSeeTheModuleArchUnitTest\.cs\:21

Reuse

```text
Made-up offender under a module fixture approach reused for A1 oracle (also ForecastingRulesSeeTheModuleArchUnitTest).
```

WorkItemsMembershipArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsMembershipArchUnitTest\.cs\:15

Reuse

```text
Membership-list shape reused for the A3 oracle.
```

WorkTrackingIntegrationPattern

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:13

Extend

```text
The leaf rule pattern in ModuleBoundariesArchUnitTest; A1 adds Services.Factories and Modules.TrackerIntegration; replaced by R-T7/R-T8 in C1.
```

PlatformPersistencePattern

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryRecognition\.cs\:34

Extend

```text
RepositoryRecognition platform persistence recognition; A1 adds Modules.TrackerIntegration.(Application|Adapters).OAuth so the connector no-repository rule keeps its meaning after B4.
```

ConnectorRatchetFixtures

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorRatchetFixtures\.cs\:1

Reuse

```text
S06 connector port fixtures; only locates types by typeof instead of names.
```

JIRA\_REGEX

Scripts\/test\-selection\/path\-classifier\.sh\:13

Extend

```text
path-classifier.sh is the sole owner of live-suite selection by path; A2 widens its tracker and shared regexes.
```

connector\-throws\-count

Scripts\/examine\/build\-examine\-commands\:1222

Extend

```text
Existing examine command; A2 makes it locate connector folders wherever they live.
```

### Impact

None.

### Agreement

path\-classifier\.sh classification output \(live\-suite flags per tracker and shared\)

Scripts\/test\-selection\/path\-classifier\.sh\:13

Producer

Migrated

```text
Regexes widened to module paths; output shape unchanged.
```

path\-classifier\.sh classification output \(live\-suite flags per tracker and shared\)

\.github\/workflows\/ci\_changes\.yml\:1

Consumer

UnchangedCompatible

```text
Calls the classifier; categories are by TestCategory, not path.
```

path\-classifier\.sh classification output \(live\-suite flags per tracker and shared\)

\.github\/workflows\/ci\_backend\.yml\:1

Consumer

UnchangedCompatible

```text
Consumes the flags only.
```

path\-classifier\.sh classification output \(live\-suite flags per tracker and shared\)

Scripts\/test\-selection\/dev\-test\.sh\:1

Consumer

UnchangedCompatible

```text
Uses the same classifier.
```

path\-classifier\.sh classification output \(live\-suite flags per tracker and shared\)

Scripts\/test\-selection\/dev\-test\.ps1\:22

Consumer

Migrated

```text
Carries its own copies of the tracker regexes; widened identically in A2.
```

### Not applicable

contract_tests

```text
No independently released producer and consumer: the classifier, its callers, rule files and the production assembly change and ship in the same commit of one repository; test-path-classifier.sh exercises the real classifier.
```

prefactoring

```text
Rules and scripts are widened additively in place; no preserving move precedes the change, and the existing rules staying green on today code is itself the preservation check.
```

### Boundaries

```text
Maintainer runs the architecture rule tests (dotnet test over Lighthouse.Backend.Tests/Architecture) and the CI test-selection script (bash Scripts/test-selection/test-path-classifier.sh) and the generated examine command connector-throws-count
```

#### Driven ports

```text
Compiled IL of Lighthouse.Backend read through IlDependencies/ArchUnitNET
```

```text
Repository file tree read by path-classifier.sh and build-examine-commands
```

```text
Test-side rules and scripts read the production assembly and paths; production code never depends on them.
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
A type or path of the context under the old location or under Modules/TrackerIntegration: both are seen by every rule and the classifier
```

story\-6359\-s03a\-a\-rules\-first

Excluded

```text
A type under Modules/TrackerIntegration that reaches Work Items, Forecasting, the update code or a repository without being caught
```

story\-6359\-s03a\-a\-rules\-first

Unknown

```text
A host-side log configuration outside the repository naming a moved logger category
```

maintainer

### Models

None.

### Closure

```text
Rules, classifier and membership see the module
```

Safety

Pass

DesignedNotBuilt

```text
Each slice oracle has a made-up offender or path falsifier executed against the real IL or classifier
```

##### Evidence

None.

```text
Rules, classifier and membership see the module
```

Inhabitation

Pass

DesignedNotBuilt

```text
Existing rules stay green on today code; membership list measured from the current IL
```

##### Evidence

None.

```text
Rules, classifier and membership see the module
```

PublicReachability

Pass

DesignedNotBuilt

```text
Observed through dotnet test and bash test-path-classifier.sh, the maintainer and CI entry points
```

##### Evidence

None.

```text
Rules, classifier and membership see the module
```

Liveness

Pass

DesignedNotBuilt

```text
Not applicable: synchronous one-shot test and script runs, no progress or fairness claim
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
Assumption negated: CI selects live suites by test category only
```

```text
A connector change selects its live suite
```

```text
All live suites run for shared paths
```

```text
A connector change under the module path that runs no live suite
```

```text
A2 widens before any move; C1 removes old alternatives
```

story\-6359\-s03a\-a\-rules\-first

#### Y2

Stressor

```text
Assumption negated: rules key only on namespaces, never on paths, strings or names
```

```text
Each rule keeps guarding the context after the move
```

```text
Rules locate files by name and read full names from typeof
```

```text
A rule that silently stops matching after a move (passes vacuously)
```

```text
A1 rewrites path/string keyed rules; per-hit content of the 23 rule files checked in A1
```

story\-6359\-s03a\-a\-rules\-first

#### Y3

Stressor

```text
Assumption negated: the ACL depends on no downstream context
```

```text
Leaf rule green on today code
```

```text
One listed edge JiraWorkTrackingConnector to IDeliveryForecastBlockRenderer
```

```text
Widening DeliverySources in A1 (goes red) or a growing list
```

```text
Listed in C1, removed by S03d
```

story\-6359\-s03a\-c\-edges\-and\-rules

#### Y4

Bypass

```text
Reflection or assembly scan by namespace string; JSON carrying .NET type names
```

```text
Same DI resolution and serialized bytes
```

```text
none found by text search
```

```text
Namespace string lookups that break after a move
```

```text
A3 checks strings in the IL; full suite catches a scan
```

story\-6359\-s03a\-a\-rules\-first

#### Y5

Stressor

```text
Second set (walkthrough, not derived from): worktree without premium licence fixture; parallel S03d moving DeliverySources
```

```text
Rule results unaffected
```

```text
2 environmental Licensing failures in a worktree
```

```text
Reading those as regressions
```

```text
Coordinate S03d through the lists, each only shrinks
```

story\-6359\-s03a\-a\-rules\-first

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs

Extend

```text
WorkTrackingIntegrationPattern gains ^Lighthouse\.Backend\.Services\.Factories$ and ^Lighthouse\.Backend\.Modules\.TrackerIntegration(\.|$); Portfolio/Delivery side not widened to DeliverySources (D-7, C1).
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryRecognition\.cs

Extend

```text
PlatformPersistencePattern gains Modules.TrackerIntegration.(Application|Adapters).OAuth.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencySingleDecisionArchUnitTest\.cs

Extend

```text
Where it keys on a source path, full-name string or folder: locate files by name under the production source and read full names from typeof; using-only otherwise; no rule loosened.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CalendarDayAnchorSeamArchUnitTest\.cs

Extend

```text
Where it keys on a source path, full-name string or folder: locate files by name under the production source and read full names from typeof; using-only otherwise; no rule loosened.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/JiraRouteLiteralsTest\.cs

Extend

```text
Where it keys on a source path, full-name string or folder: locate files by name under the production source and read full names from typeof; using-only otherwise; no rule loosened.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FeatureSourceCapabilityArchUnitTest\.cs

Extend

```text
Where it keys on a source path, full-name string or folder: locate files by name under the production source and read full names from typeof; using-only otherwise; no rule loosened.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackTargetCapabilityArchUnitTest\.cs

Extend

```text
Where it keys on a source path, full-name string or folder: locate files by name under the production source and read full names from typeof; using-only otherwise; no rule loosened.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectionHealthSingleWriterArchUnitTest\.cs

Extend

```text
Where it keys on a source path, full-name string or folder: locate files by name under the production source and read full names from typeof; using-only otherwise; no rule loosened.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorsDeclareNoUnsupportedMembersArchUnitTest\.cs

Extend

```text
Where it keys on a source path, full-name string or folder: locate files by name under the production source and read full names from typeof; using-only otherwise; no rule loosened.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorPortSpeaksPortfolioArchUnitTest\.cs

Extend

```text
Where it keys on a source path, full-name string or folder: locate files by name under the production source and read full names from typeof; using-only otherwise; no rule loosened.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorRatchetFixtures\.cs

Extend

```text
Where it keys on a source path, full-name string or folder: locate files by name under the production source and read full names from typeof; using-only otherwise; no rule loosened.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ServiceNowHistoryPurityArchUnitTest\.cs

Extend

```text
Where it keys on a source path, full-name string or folder: locate files by name under the production source and read full names from typeof; using-only otherwise; no rule loosened.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ServiceNowValidationVerdictPurityArchUnitTest\.cs

Extend

```text
Where it keys on a source path, full-name string or folder: locate files by name under the production source and read full names from typeof; using-only otherwise; no rule loosened.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CategoryListOwnershipGuardTest\.cs

Extend

```text
Where it keys on a source path, full-name string or folder: locate files by name under the production source and read full names from typeof; using-only otherwise; no rule loosened.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/SecretCustodySeamArchUnitTest\.cs

Extend

```text
Where it keys on a source path, full-name string or folder: locate files by name under the production source and read full names from typeof; using-only otherwise; no rule loosened.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationRulesSeeTheModuleArchUnitTest\.cs

#### Verification

```text
pwsh Lighthouse.Backend/Test-Fast.ps1
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~TrackerIntegrationRulesSeeTheModuleArchUnitTest"
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~Lighthouse.Backend.Tests.Architecture"
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRules\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsRulesSeeTheModuleArchUnitTest\.cs

#### Public oracle

```text
The maintainer runs the architecture rule tests (dotnet test, Architecture namespace) with a made-up connector type under Lighthouse.Backend.Modules.TrackerIntegration that reaches Work Items, Forecasting, the update code or a repository, compiled into the IL the rules read (WorkItemsRulesSeeTheModule fixture approach).
```

```text
The rules that guard the Tracker integration context (leaf rule and connectors-do-not-depend-on-any-repository rule) report that type as an offender, exactly as they do for the same type under Services/Implementation/WorkTrackingConnectors; every existing architecture rule stays green on today code.
```

```text
Today: the made-up offender under the module passes the rules (RED). A widening that misses the module namespace, Services.Factories, or the module OAuth namespaces in RepositoryRecognition leaves that offender unreported; widening DeliverySources early turns the existing Jira to renderer edge red.
```

##### Promises

P1

P2

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

ForecastingRulesSeeTheModuleArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingRulesSeeTheModuleArchUnitTest\.cs\:1

Reuse

```text
Second precedent of the made-up offender fixture; the A1 oracle follows it, no new fixture owner.
```

ConnectionHealthSingleWriterArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectionHealthSingleWriterArchUnitTest\.cs\:12

Extend

```text
Existing rule keeps its owner; extended to see the module path or namespace (typeof or locate-by-name), never copied or loosened.
```

WriteBackTargetCapabilityArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackTargetCapabilityArchUnitTest\.cs\:20

Extend

```text
Existing rule keeps its owner; extended to see the module path or namespace (typeof or locate-by-name), never copied or loosened.
```

DependencySingleDecisionArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencySingleDecisionArchUnitTest\.cs\:35

Extend

```text
Existing rule keeps its owner; extended to see the module path or namespace (typeof or locate-by-name), never copied or loosened.
```

CalendarDayAnchorSeamArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CalendarDayAnchorSeamArchUnitTest\.cs\:32

Extend

```text
Existing rule keeps its owner; extended to see the module path or namespace (typeof or locate-by-name), never copied or loosened.
```

CategoryListOwnershipGuardTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CategoryListOwnershipGuardTest\.cs\:12

Extend

```text
Existing rule keeps its owner; extended to see the module path or namespace (typeof or locate-by-name), never copied or loosened.
```

SecretCustodySeamArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/SecretCustodySeamArchUnitTest\.cs\:21

Extend

```text
Existing rule keeps its owner; extended to see the module path or namespace (typeof or locate-by-name), never copied or loosened.
```

FeatureSourceCapabilityArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FeatureSourceCapabilityArchUnitTest\.cs\:19

Extend

```text
Existing rule keeps its owner; extended to see the module path or namespace (typeof or locate-by-name), never copied or loosened.
```

JiraRouteLiteralsTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/JiraRouteLiteralsTest\.cs\:11

Extend

```text
Existing rule keeps its owner; extended to see the module path or namespace (typeof or locate-by-name), never copied or loosened.
```

ServiceNowHistoryPurityArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ServiceNowHistoryPurityArchUnitTest\.cs\:16

Extend

```text
Existing rule keeps its owner; extended to see the module path or namespace (typeof or locate-by-name), never copied or loosened.
```

ServiceNowValidationVerdictPurityArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ServiceNowValidationVerdictPurityArchUnitTest\.cs\:14

Extend

```text
Existing rule keeps its owner; extended to see the module path or namespace (typeof or locate-by-name), never copied or loosened.
```

ConnectorPortSpeaksPortfolioArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorPortSpeaksPortfolioArchUnitTest\.cs\:19

Extend

```text
Existing rule keeps its owner; extended to see the module path or namespace (typeof or locate-by-name), never copied or loosened.
```

ConnectorsDeclareNoUnsupportedMembersArchUnitTest

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorsDeclareNoUnsupportedMembersArchUnitTest\.cs\:17

Extend

```text
Existing rule keeps its owner; extended to see the module path or namespace (typeof or locate-by-name), never copied or loosened.
```

##### Impact

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleBoundariesArchUnitTest\.cs\:13

Changed

```text
Pattern widened to the module namespace and Services.Factories.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryRecognition\.cs\:34

Changed

```text
Platform persistence pattern widened to module OAuth namespaces (E2).
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DependencySingleDecisionArchUnitTest\.cs\:1

Changed

```text
One of the 13 path/name/typeof keyed rule files rewritten to locate by name or typeof; same for the other 12 listed as S1 targets.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsMembershipArchUnitTest\.cs\:15

UnchangedCompatible

```text
Selector names leave Work Items lists only in B1.
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1

UnchangedCompatible

```text
No production change in feature A.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectionHealthSingleWriterArchUnitTest\.cs\:12

Changed

```text
Path, full-name string or folder keys rewritten to locate by name or typeof; using-only otherwise.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackTargetCapabilityArchUnitTest\.cs\:20

Changed

```text
Path, full-name string or folder keys rewritten to locate by name or typeof; using-only otherwise.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CalendarDayAnchorSeamArchUnitTest\.cs\:32

Changed

```text
Path, full-name string or folder keys rewritten to locate by name or typeof; using-only otherwise.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/CategoryListOwnershipGuardTest\.cs\:12

Changed

```text
Path, full-name string or folder keys rewritten to locate by name or typeof; using-only otherwise.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/SecretCustodySeamArchUnitTest\.cs\:21

Changed

```text
Path, full-name string or folder keys rewritten to locate by name or typeof; using-only otherwise.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FeatureSourceCapabilityArchUnitTest\.cs\:19

Changed

```text
Path, full-name string or folder keys rewritten to locate by name or typeof; using-only otherwise.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/JiraRouteLiteralsTest\.cs\:11

Changed

```text
Path, full-name string or folder keys rewritten to locate by name or typeof; using-only otherwise.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorRatchetFixtures\.cs\:8

Changed

```text
Path, full-name string or folder keys rewritten to locate by name or typeof; using-only otherwise.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ServiceNowHistoryPurityArchUnitTest\.cs\:16

Changed

```text
Path, full-name string or folder keys rewritten to locate by name or typeof; using-only otherwise.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ServiceNowValidationVerdictPurityArchUnitTest\.cs\:14

Changed

```text
Path, full-name string or folder keys rewritten to locate by name or typeof; using-only otherwise.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorPortSpeaksPortfolioArchUnitTest\.cs\:19

Changed

```text
Path, full-name string or folder keys rewritten to locate by name or typeof; using-only otherwise.
```

test\-rule

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorsDeclareNoUnsupportedMembersArchUnitTest\.cs\:17

Changed

```text
Path, full-name string or folder keys rewritten to locate by name or typeof; using-only otherwise.
```

##### Agreement

None.

##### Not applicable

agreement

```text
Rule files are private to the test assembly; no producer or consumer outside this commit reads them.
```

prefactoring

```text
Additive widening in place; existing rules green on today code are the preservation check.
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
The leaf rule and the connector no-repository rule govern every type under Lighthouse.Backend.Modules.TrackerIntegration as well as the old WorkTrackingConnectors and Factories namespaces, including types added later.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationRulesSeeTheModuleArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~TrackerIntegrationRulesSeeTheModuleArchUnitTest"
```

##### Contract tests

None.

### S2

#### Targets

Scripts\/test\-selection\/path\-classifier\.sh

Extend

```text
JIRA/ADO/LINEAR/SERVICENOW regexes also match Modules/TrackerIntegration/Adapters/Connectors/<Tracker>/ (production and tests), Jira also Modules/TrackerIntegration/Domain/ParentSourceSelector.cs; SHARED_REGEX also Adapters/Connectors/(Auth|OAuth)/, Modules/TrackerIntegration/Application/, Modules/TrackerIntegration/Domain/, Adapters/OAuth/.
```

Scripts\/test\-selection\/dev\-test\.ps1

Extend

```text
Its own copies of the tracker regexes widened identically.
```

Scripts\/examine\/build\-examine\-commands

Extend

```text
connector-throws-count locates the Csv, Linear and ServiceNow connector folders wherever they live (old or module path) instead of the hard-coded old folder.
```

Scripts\/test\-selection\/test\-path\-classifier\.sh

#### Verification

```text
bash Scripts/test-selection/test-path-classifier.sh
bash Scripts/examine/build-examine-commands
Scripts/examine/bin/connector-throws-count
python3 -m unittest discover -s Scripts/test-timings/tests
dotnet build Lighthouse.Backend
```

#### Supports

Scripts\/examine\/build\-examine\-commands

#### Public oracle

```text
The CI test selection classifies a changed file under Modules/TrackerIntegration/Adapters/Connectors/<Tracker>/ (production or test) for Jira, Linear, Azure DevOps or ServiceNow, or under the module shared Application, Domain or OAuth paths (bash test-path-classifier.sh); and connector-throws-count runs against a tree where the connector folders live under the module path.
```

```text
The tracker live suite is selected (all live suites for a shared path), as for the same change under the old path; connector-throws-count prints one line per connector folder and the total instead of folder not found.
```

```text
Today: each module-path scenario classifies false (RED) and connector-throws-count exits 1 with folder not found. A regex widened for production but not tests, or a missed tracker, leaves a scenario false.
```

##### Promises

P1

P3

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

None.

##### Impact

ci\-selection

Scripts\/test\-selection\/path\-classifier\.sh\:13

Changed

```text
Regexes widened (E3).
```

ci\-selection

Scripts\/test\-selection\/dev\-test\.ps1\:22

Changed

```text
Regex copies widened.
```

examine

Scripts\/examine\/build\-examine\-commands\:1222

Changed

```text
connector-throws-count path fix (E4).
```

workflow

\.github\/workflows\/ci\_changes\.yml\:1

UnchangedCompatible

```text
Calls the classifier; flags unchanged in shape.
```

test\-timings

Scripts\/test\-timings\/test\-time\-budget\.json\:59

UnchangedCompatible

```text
Names test classes by full name; rewritten when those tests move in B3. A2 checks the three Python tests under Scripts/test-timings/tests (E11).
```

mutation\-config

Lighthouse\.Backend\/stryker\-config\.json\:7

UnchangedCompatible

```text
Path globs rewritten with the move of their files in B3 (E10).
```

build

Lighthouse\.Backend\/Lighthouse\.Backend\/Lighthouse\.Backend\.csproj\:1

UnchangedCompatible

```text
SDK globbing; A2 confirms no item names a moved path.
```

ci\-selection

Scripts\/test\-selection\/dev\-test\.sh\:19

UnchangedCompatible

```text
Sources path-classifier.sh; picks up the widening without an edit.
```

##### Agreement

None.

##### Not applicable

reuse

```text
Owners recorded in shared reuse: path-classifier.sh and connector-throws-count are extended in place; no new owner.
```

agreement

```text
The classifier output agreement is recorded in shared agreements (producer migrated, workflows and dev-test consumers compatible or migrated).
```

architectural_tests

```text
A2 changes scripts, not a dependency or boundary law; its oracle is the classifier scenario script.
```

prefactoring

```text
Additive regex alternatives; existing classifier scenarios stay green as the preservation check.
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

### S3

#### Targets

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationMembershipArchUnitTest\.cs

#### Verification

```text
pwsh Lighthouse.Backend/Test-Fast.ps1
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~TrackerIntegrationMembershipArchUnitTest"
dotnet build Lighthouse.Backend
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsMembershipArchUnitTest\.cs

#### Public oracle

```text
The maintainer runs the membership test: the D-1 membership list of Tracker integration (102 production types) and its D-2 Models admissions, measured from the compiled IL, compared both ways with the inline list; the inbound and outbound IL edges E5-E8 confirmed.
```

```text
Passes on today code; D-2 admits only Models candidates that are not EF entities, not on the wire, not reached by any Models type and not used by any code that stays outside the module (expected admitted: the OAuth flow/state/token value types; staying: ConnectionFetchShapeChanged, the WriteBack vocabulary, OAuthCompleteResult).
```

```text
Fails when a listed type is missing from the IL or the list names a type that does not exist (RED first with a made-up name); a Models candidate that is an entity, a wire type, reached by a kernel type or used by staying code outside the module must not be admitted.
```

##### Promises

P1

P4

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

None.

##### Impact

None.

##### Agreement

None.

##### Not applicable

reuse

```text
No production target; the oracle reuses the WorkItemsMembershipArchUnitTest shape (shared reuse).
```

impact

```text
No production or support target changes; only the new oracle file is added by the acceptance designer.
```

agreement

```text
No shared contract touched.
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
The Tracker integration membership list equals the measured IL set both ways, and each admitted Models type is neither an EF entity, nor on the wire, nor reached by a Models type, nor used by any type that stays outside the module.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationMembershipArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~TrackerIntegrationMembershipArchUnitTest"
```

##### Contract tests

None.

## ADRs

None.
