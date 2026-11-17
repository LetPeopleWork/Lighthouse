# Product brief: story\-6359\-s03a\-b\-move

## Request

```text
Story #6359 S03a feature B of three (the move): move the Tracker integration types feature A's membership test lists (TrackerIntegrationMembership.Moves, plus the four admitted Models OAuth value types OAuthFlowContext, OAuthRefreshContext, OAuthStateClaims, OAuthTokens) out of Services and Factories into Lighthouse.Backend/Modules/TrackerIntegration, namespace = folder: the translation rules (category crossing, state-transition mapper, parent and dependency source selectors, dependency field references) into Domain (B1); the connector port, capabilities, factories, auth schema and board types into Application (B2); the five connectors, their helpers, the auth strategies, the OAuth schema helpers and Jira's issue factory into Adapters/Connectors/<Tracker> (B3); OAuth and connection health into Application/OAuth, Application/ConnectionHealth, Adapters/OAuth and Adapters/Background (B4). Unit tests move with the files they test (D-9). Pure moves in their own commits, edits apart. Behaviour-free. Design: docs/feature/story-6359-s03a-package-tracker-integration/feature-delta.md (slices B1-B4, D-0 to D-12, Ratchet design, Component decomposition).
```

## Work

L

```text
Tracker integration lives in its own module folder, so feature C can give it its own module rules
```

### Rework

None.

## Promises

### P1

Outcome

```text
Tracker integration lives in Modules/TrackerIntegration in the layers the design names, and nothing a user, a tracker or the database sees changes.
```

### P2

Outcome

```text
The category crossing, the state-transition mapper, the parent and dependency source selectors and the dependency field references live in Lighthouse.Backend.Modules.TrackerIntegration.Domain and reach only the shared kernel and the base library; Work Items' rules no longer hold the selectors as their own; a type the Domain rule rejects lives in Application, recorded
```

### P3

Outcome

```text
The connector port and its capabilities, both factories with their interfaces, the auth schema and the board types live in Lighthouse.Backend.Modules.TrackerIntegration.Application; the factory interface and class share that namespace; the board port names no implementation namespace
```

### P4

Outcome

```text
The five connectors, their helpers, the auth strategies, the OAuth schema helpers and Jira's issue factory live in Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.<Tracker>; their unit tests sit in the mirrored test folder; the CI test selection still picks that tracker's live suite
```

### P5

Outcome

```text
The OAuth service, registry, state tokens, exceptions and provider port live in Application.OAuth with the four admitted Models value types; connection health in Application.ConnectionHealth; the OAuth providers in Adapters.OAuth; the health prober in Adapters.Background; no type of the membership Moves list is left under Services or Factories
```

### P6

Outcome

```text
Every existing test is green unmodified apart from moves, using lines and rule-list strings; refresh answers, forecasts, routes, DTOs, DI lifetimes, handler order and the EF model are identical; the live connector suites are green in CI
```

## Scope

```text
B1: move the five translation-rule types and their unit tests into Modules/TrackerIntegration/Domain; Work Items' rules lose the selectors.
```

```text
B2: move the connector port, capabilities, factories, auth schema and board types and their tests into Modules/TrackerIntegration/Application.
```

```text
B3: move the five connectors, helpers, auth strategies, OAuth schema helpers and IssueFactory with their tests into Adapters/Connectors/<Tracker>; Stryker configs and test-time budgets follow.
```

```text
B4: move OAuth, connection health, the OAuth providers and the prober with the four admitted Models value types and their tests.
```

```text
The module's own boundary rules, the unit-test placement rule and retiring the old leaf pattern and old classifier alternatives: feature C.
```

```text
Repositories, the connection save service, connection usage, the write-back round, encryption, demo data, controllers, API DTOs and entities stay where they are (D-1, D-4, D-6).
```

```text
Any behaviour a user sees: nothing changes; UI sketch and loading/failure looks are N/A.
```

## Decisions

```text
Rigor standard for feature B: crafter, acceptance designer and architect on sonnet; reviewers and EXAMINE on haiku (maintainer, 2026-11-16).
```

#### Status

Confirmed


```text
Push per slice, CI watched per push, each push runs all live connector suites in CI (maintainer, 2026-11-16 and 2026-11-17; overrides D-11's push-once recommendation).
```

#### Status

Confirmed


```text
Unit tests move with their production files in the same slice, in a separate move commit (D-9); Program.cs registrations and their order stay, only using lines change (D-10).
```

#### Status

Confirmed


```text
Hold after feature B: B4 pushed with CI green, whole-candidate review and EXAMINE accepted (maintainer, 2026-11-17).
```

#### Status

Confirmed


## Journey

```text
The maintainer runs dotnet build and the architecture tests locally after each slice; the moved types sit in their layer and every rule stays green.
```

```text
Not supplied.
```

```text
The maintainer pushes each slice; CI runs the live connector suites and stays green.
```

```text
Not supplied.
```

## Slices

### S1

```text
B1: the category crossing, the state-transition mapper, ParentSourceSelector, DependencySourceSelector and DependencyFieldReferences live in Modules/TrackerIntegration/Domain, which reaches only the kernel; Work Items' membership, boundary and layout rules no longer list the selectors. Oracle TrackerIntegrationDomainLayoutArchUnitTest is RED first (types not in Domain). The feature's walking skeleton.
```

#### Promises

P1

P2

P6

#### Depends on

None.

#### Slice work

None.

### S2

```text
B2: the connector port (15 types), the factories and their interfaces, the auth schema and Board/BoardInformation live in Modules/TrackerIntegration/Application; the two S06 leaks are fixed by placement. Oracle TrackerIntegrationApplicationLayoutArchUnitTest is RED first.
```

#### Promises

P1

P3

P6

#### Depends on

None.

#### Slice work

None.

### S3

```text
B3: the five connectors, their helpers, the auth strategies, the OAuth schema helpers and IIssueFactory/IssueFactory live in Modules/TrackerIntegration/Adapters/Connectors/<Tracker> with their unit tests mirrored; CSV test-data entries in the test csproj, Stryker configs and test-time budgets follow. Oracle TrackerIntegrationConnectorsLayoutArchUnitTest is RED first.
```

#### Promises

P1

P4

P6

#### Depends on

None.

#### Slice work

None.

### S4

```text
B4: OAuth (service, registry, state tokens, exceptions, ports and the four admitted Models value types) in Application/OAuth, connection health in Application/ConnectionHealth, the OAuth providers in Adapters/OAuth, ConnectionHealthProber in Adapters/Background; no Moves type left under Services or Factories. Oracle TrackerIntegrationOAuthAndHealthLayoutArchUnitTest is RED first.
```

#### Promises

P1

P5

P6

#### Depends on

None.

#### Slice work

None.
