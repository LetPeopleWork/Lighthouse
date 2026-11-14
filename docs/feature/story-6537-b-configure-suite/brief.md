# Product brief: story\-6537\-b\-configure\-suite

## Request

```text
Story 6537 (T10), suite 2: the ConfigureOncePerConnection family. Move each rule its host tests prove to the lowest layer that can prove it (a unit test of the computing class or of the connector, else the service port). 135 of its 163 host rows can move by the screen (section 7 of the story's screen.md); wire, real-store concurrency and end-to-end rows stay. Behaviour-free: no production file changes.
```

## Work

M

```text
The backend Integration tier spends less time re-proving connection-default, named-link and settings-save rules through a web host, and every rule still has a test that goes red when its production line breaks
```

### Rework

None.

## Promises

### P1

Outcome

```text
Each rule a ConfigureOncePerConnection host test proves is proven by a test at the lowest layer that can prove it (a unit test of the computing class or of the connector, else the service port), so the backend Integration tier takes less time
```

### P2

Outcome

```text
No rule loses its test: every removed host row has an id-table row naming the test that now proves its rule, and one deliberate break of that rule's production line turns that test red
```

### P3

Outcome

```text
No production file changes, the test-host guard lists do not grow, and at least one host row per route still proves the wiring
```

## Scope

None.

```text
The 28 host rows the screen keeps (wire, real-store concurrency, end-to-end, or a rule whose owner was not located), ClockIsolationScenarios, live connector tests, and any production change
```

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
The save-pipeline rules of Slice05bReferencedFieldsAndOlderPayloads and Slice05aTeamsParentOverrideDefault (referenced fields, older payloads, a team's parent override against the connection default) are proven by unit tests of the classes that compute them; their wire and concurrency rows stay
```

#### Promises

P1

P2

P3

#### Depends on

None.

#### Slice work

None.

### S2

```text
The Jira named-link and refresh-history rules of Slice02NamedLinkThatMatchedNothing and Slice01JiraDependenciesFromANamedLink are proven by unit tests of the Jira connector and the refresh log; their end-to-end rows stay
```

#### Promises

P1

P2

P3

#### Depends on

None.

#### Slice work

None.

### S3

```text
The Azure DevOps relation rules of Slice04AzureDevOpsDependenciesFromANamedRelationEnd and Slice03AzureDevOpsRelationTypeAccepted, for a portfolio and for a team, are proven by unit tests of the Azure DevOps connector and the settings that accept a relation type; their wire rows stay
```

#### Promises

P1

P2

P3

#### Depends on

None.

#### Slice work

None.

### S4

```text
The estimation and portfolio-default rules of Slice12EstimationDefault, Slice09PortfoliosDependencyFieldDefault, Slice10PortfoliosSizeEstimateFieldDefault and Slice11PortfoliosFeatureOwnerFieldDefault are proven by unit tests of the effective-setting and save classes; their wire rows stay
```

#### Promises

P1

P2

P3

#### Depends on

None.

#### Slice work

None.

### S5

```text
The default-change rules of Slice06DefaultChangeUpdatesTheFollowers and Slice08PortfoliosParentOverrideDefault (followers take a changed default, an override keeps its own value) are proven by unit tests of the save and effective-setting classes; their end-to-end rows stay
```

#### Promises

P1

P2

P3

#### Depends on

None.

#### Slice work

None.
