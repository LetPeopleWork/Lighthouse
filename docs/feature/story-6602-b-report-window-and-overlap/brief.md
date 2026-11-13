# Product brief: story\-6602\-b\-report\-window\-and\-overlap

## Request

```text
Story #6602, feature B: move ReportWindowBoundaryScenarios from the web host to the application-service port on a new Reports port base (ReportsAtThePortTest), behaviour-free, leaving HostUsersStillToMove in the commit that moves it; then drop the Slice05NeedAndVerdictTest rows that repeat NeedOverOneCycleTest, each mapped to its twin, and hoist the duplication the feature A reviews carried forward. Design: docs/feature/story-6602-refinement-and-report-window-at-the-port/feature-delta.md
```

## Work

M

```text
The report-window class proves its 15 boundary rows without a web host, and the Refinement need rules are proven once, so the backend Integration tier is faster and no rule loses its test
```

### Rework

None.

## Promises

### P1

Outcome

```text
ReportWindowBoundaryScenarios runs at the application-service port on a new Reports port base, ReportsAtThePortTest, without a web host, and HostUsersStillToMove no longer names it
```

### P2

Outcome

```text
Every test id of ReportWindowBoundaryScenarios survives the move (15 rows at the port, no new wire row), with no assertion lost, and the port fixture check compares the Reports port registrations with the host's
```

### P3

Outcome

```text
Each Slice05NeedAndVerdictTest row that repeats NeedOverOneCycleTest is dropped only when a NeedOverOneCycleTest row proves the same rule with the same inputs; the id comparison shows exactly the dropped ids and every other id survives
```

### P4

Outcome

```text
The Refinement port test members that feature A's reviews found duplicated (the Gravity ready-rule builder, the tab and need readers, the shared constants and script, TheTabAsReadBy) exist once, in the port base or the port builder
```

## Scope

```text
ReportWindowBoundaryScenarios, ReportsAtThePortTest, RealityCheckWireContractTest port fixtures (B13), TestHostUseGuardTest ratchet line; Slice05NeedAndVerdictSpecifications, NeedOverOneCycleSpecifications, Slice10SleYardstickSpecifications, RefinementAtThePortTest, RefinementPortGivens
```

```text
Any production code change
```

```text
Moving any other Reports or Refinement class off the host
```

```text
Folding the host NeedForecastScript into ScriptedForecasts
```

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
The report-window class runs at the service port on a new Reports port base
```

#### Promises

P1

P2

#### Depends on

None.

#### Slice work

M

```text
ReportWindowBoundaryScenarios proves its 15 boundary rows at the port; the Reports port base exists for any later Reports move
```

##### Rework

None.

### S2

```text
The need-and-verdict class no longer repeats the need-over-one-cycle rows, and the Refinement port test members exist once
```

#### Promises

P3

P4

#### Depends on

S1

#### Slice work

M

```text
Each need rule is proven once, each dropped row is mapped to its twin, and the shared port test members live in one place
```

##### Rework

None.
