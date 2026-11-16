# Acceptance brief: story\-6359\-s03a\-a\-rules\-first

## Slices

### S1

##### Covers

P2

P1

```text
The maintainer runs the architecture rule tests with made-up connectors under the Tracker integration module that reach Work Items, Forecasting or the update code, beside the same connector in the old connectors place, a connector reading only a Feature, one reaching a delivery source, and a namesake just outside the module.
```

```text
The leaf rule names the three module connectors and the old-place connector; it does not name the Feature reader, the delivery-source connector or the namesake.
```

```text
A connector that moves into the module and breaks the leaf boundary is still reported by the architecture tests.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationRulesSeeTheModuleArchUnitTest\.cs\:\:AConnectorUnderTheModuleReachingWorkItemsForecastingOrTheUpdateCode\_IsNamedByTheLeafRule

```text
A pattern that sees only part of the module, such as Adapters, would still name these Adapters.Connectors offenders while a module Domain or Application type reaching Work Items escapes.
```

###### Obligations


##### Covers

P2

P1

```text
The maintainer runs the architecture rule tests with a made-up factory in the Services factories namespace reaching Work Items, the same factory in the old factories namespace, and a namesake whose namespace only starts alike.
```

```text
The leaf rule names both factories and not the namesake.
```

```text
The half of the connector factory that lives in the Services factories namespace is guarded like the other half.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationRulesSeeTheModuleArchUnitTest\.cs\:\:AFactoryInTheServicesFactoriesNamespaceReachingWorkItems\_IsNamedByTheLeafRule

```text
The Services factories alternative also takes in sub-namespaces below it; the namesake only checks a sibling prefix.
```

###### Obligations


##### Covers

P2

P1

```text
The maintainer runs the architecture rule tests with a made-up connector under the module reaching the database context, the same connector in the old connectors place, a module connector reading its OAuth token through the module token place, and a namesake outside the module reaching the database.
```

```text
The no-repository rule names the module connector and the old-place connector; it does not name the OAuth token reader or the namesake.
```

```text
A connector that moves into the module and reads stored records is still reported; reading OAuth tokens stays allowed.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationRulesSeeTheModuleArchUnitTest\.cs\:\:AConnectorUnderTheModuleReachingTheDatabase\_IsNamedByTheNoRepositoryRule\_WhileItsOAuthTokensStayAllowed

```text
A rule that missed a module repository under Adapters.Persistence while still catching the database context; that path belongs to the shared stored-records pattern, not to this case.
```

###### Obligations


##### Covers

P1

```text
The maintainer runs the architecture rule tests over the module OAuth token places (Application, Adapters and a provider below Adapters), the old OAuth places, and two namesakes that only share a prefix.
```

```text
Every module and old OAuth place counts as the persistence platform; neither namesake does.
```

```text
The OAuth token service stays recognised as a platform service, not a repository, after it moves into the module.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationRulesSeeTheModuleArchUnitTest\.cs\:\:TheOAuthTokenPlaceUnderTheModule\_CountsAsThePersistencePlatform\_WhileANamesakeDoesNot

```text
The connector-side OAuth folder under Adapters.Connectors is also taken for the platform; the design leaves that open, so it is not pinned.
```

###### Obligations

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
Before any file moves, every architecture rule, the CI live-suite selection, the examine commands and the membership list already recognise the Modules/TrackerIntegration paths; a maintainer running the architecture rules and the membership test sees them pass on today's code.
```

#### E2

##### Promises

P2

```text
When a made-up connector type under Lighthouse.Backend.Modules.TrackerIntegration reaches Work Items, Forecasting, the update code or a repository, the Tracker integration architecture rules report it as an offender, as they do for the same type under the old Services/Implementation/WorkTrackingConnectors path.
```

#### E3

##### Promises

P3

```text
Running connector-throws-count and the test-time budgets finds the Modules/TrackerIntegration connector folders (Jira, Linear, Azure DevOps, ServiceNow) and does not report folder not found.
```

#### E4

##### Promises

P3

```text
A changed production or test file under Modules/TrackerIntegration/Adapters/Connectors/<Tracker>/ selects that tracker's live suite in CI, and a change under the shared Application, Domain or OAuth paths selects all live suites, as for the old path.
```

#### E5

##### Promises

P4

```text
The membership test for Tracker integration, with its admitted Models types, passes on today's compiled code, fails when a listed type is missing, and fails when the list names a type that does not exist.
```

## Charter

None.

## Examination

None.
