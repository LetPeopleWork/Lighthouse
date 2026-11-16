# Product brief: story\-6359\-s03a\-a\-rules\-first

## Request

```text
Story #6359 S03a feature A: before any Tracker integration file moves, every architecture rule, the CI test selection, the examine commands and the membership list see the future Modules/TrackerIntegration, so the moves in feature B cannot silently escape a rule or skip a live connector suite. Design authority: docs/feature/story-6359-s03a-package-tracker-integration/feature-delta.md slices A1-A3.
```

## Work

M

```text
The packaging moves of Tracker integration stay under every existing guard: rules, CI live-suite selection and examine commands keep working on the new paths.
```

### Rework

None.

## Promises

### P1

Outcome

```text
The Tracker integration code can move to Modules/TrackerIntegration without escaping any guard: every architecture rule, the CI live-suite selection, the examine commands and the membership list already see the new paths before any file moves.
```

### P2

Scenario

```text
A made-up connector under the module that breaks a context boundary is caught
```

```text
a made-up connector type under Lighthouse.Backend.Modules.TrackerIntegration that reaches Work Items, Forecasting, the update code or a repository
```

```text
the maintainer runs the architecture rule tests
```

```text
the rules that guard the Tracker integration context report that type as an offender, as they do today for the same type under Services/Implementation/WorkTrackingConnectors
```

### P3

Scenario

```text
A change to a connector on the new path still selects its live suite in CI
```

```text
a changed file under Modules/TrackerIntegration/Adapters/Connectors/<Tracker>/ (production or test) for Jira, Linear, Azure DevOps or ServiceNow, or under the module's shared Application, Domain or OAuth paths
```

```text
the CI test selection classifies the change
```

```text
it selects that tracker's live connector suite (all live suites for a shared path), as it does for the same change under the old path; and the connector-throws-count examine command and the test-time budgets find the module path instead of reporting folder not found
```

### P4

Scenario

```text
The membership of the context is fixed from the compiled code
```

```text
the membership list of Tracker integration and its admitted Models types, measured from the IL
```

```text
the maintainer runs the membership test
```

```text
it passes on today's code, and fails when a listed type is missing or the list names a type that does not exist
```

## Scope

```text
Widen the existing architecture rules and RepositoryRecognition so they also see Modules/TrackerIntegration (A1).
```

```text
Widen the CI path classifier, the examine commands, the test-time budgets and Stryker configs to the new paths (A2).
```

```text
Measure the membership list and Models admissions from the IL and fix them in a test (A3).
```

```text
Moving any file of the context: feature B (story-6359-s03a-b-move).
```

```text
The module's own boundary rules, the unit-test placement rule and retiring the old leaf pattern and old classifier alternatives: feature C.
```

```text
Any behaviour a user sees: nothing changes; UI sketch and loading/failure looks are N/A.
```

## Decisions

```text
Rigor thorough for feature A: crafter, acceptance designer and architect on opus; reviewers sonnet; EXAMINE on sonnet.
```

#### Status

Confirmed


```text
Push per slice, CI watched per push; rebase allowed when trunk moved (maintainer, 2026-11-16).
```

#### Status

Confirmed


```text
Architecture tests over the compiled IL are the oracles; no production code changes in feature A beyond scripts and test-side rule files.
```

#### Status

Confirmed


```text
The Portfolio/Delivery DeliverySources widening waits for C1, where the Jira to Promises renderer edge is listed (D-7 confirmed); otherwise A1 goes red.
```

#### Status

Confirmed


## Journey

```text
The maintainer runs dotnet build and the architecture tests locally; the rules see the module path.
```

```text
Not supplied.
```

```text
The maintainer pushes; CI selects the live connector suites for the changed paths and stays green.
```

```text
Not supplied.
```

## Slices

### S1

```text
A1: every architecture rule that names the Tracker integration namespaces, folders, paths or types also sees Modules/TrackerIntegration; a made-up connector there that reaches Work Items, Forecasting, the update code or a repository is caught. Acceptance: oracle TrackerIntegrationRulesSeeTheModuleArchUnitTest is RED first (the made-up offender under the module passes today's rules), then green with all existing rules green. The feature's walking skeleton.
```

#### Promises

P1

P2

#### Depends on

None.

#### Slice work

S

```text
The architecture rules keep guarding the context once its files move (about 1 day).
```

##### Rework

None.

### S2

```text
A2: the CI test selection, the examine commands, the test-time budgets and the Stryker configs find connector code under the module paths too; a change to a connector on a module path still selects its live suite. Acceptance: test-path-classifier.sh gains one scenario per tracker plus shared on module paths (RED: classified false today); connector-throws-count run against a module path (RED: folder not found), then green.
```

#### Promises

P1

P3

#### Depends on

S1

#### Slice work

S

```text
A moved connector cannot skip its live suite in CI (about 1 day).
```

##### Rework

None.

### S3

```text
A3: the membership list of Tracker integration and its admitted Models types are measured from the IL and fixed in a test; the inbound and outbound IL edges named at DESIGN are confirmed. Acceptance: TrackerIntegrationMembershipArchUnitTest fails when a listed type is missing from the list or the list names a type that does not exist (RED first), then green on today's code.
```

#### Promises

P1

P4

#### Depends on

S1

#### Slice work

S

```text
Feature B moves exactly the measured set (about half a day).
```

##### Rework

None.
