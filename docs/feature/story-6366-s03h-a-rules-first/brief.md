# Product brief: story\-6366\-s03h\-a\-rules\-first

## Request

```text
ADO Story #6366 S03h Package Reports, feature A (rules first): the Reports membership is measured from the IL and fixed in a namespace-agnostic test, with each moving type's layer (Domain, Application or Application/Metrics), its old and its new namespace, and every Reports file that stays with its owner and reason (A1); every rule pattern that sees a moving Reports type today gives the same answer for it under Modules.Reports, and no rule string names Reports by its old path outside a fixed rename list (A2). Design: docs/feature/story-6366-s03h-package-reports/feature-delta.md.
```

## Work

S

```text
The rules see the Reports move before any file moves, so the move in feature B cannot silently loosen a rule.
```

### Rework

None.

## Promises

### P1

Outcome

```text
The membership of the Reports module is measured from the compiled code and fixed in a test: each moving Reports type with its layer (Domain, Application or Application/Metrics), its old and its new namespace, and each Reports file that stays (ReportRepository, the Models/Reports types, the API controllers and DTOs) with its owner and reason. The test finds each listed type in its old or its new namespace, and fails when a listed type is missing, a listed name does not exist, or a Domain type reaches beyond the kernel and the base library.
```

### P2

Outcome

```text
Every rule pattern in the test assembly gives the same answer for each moving Reports type under its new Modules.Reports namespace as under its old one, and no added alternative matches a production type outside Modules.Reports.
```

### P3

Outcome

```text
No rule constant or string literal in the test project names Reports by its old Services.Implementation.Reports, Services.Interfaces.Reports or Services/(Implementation|Interfaces)/Reports path, except the entries of a fixed rename list taken from the pre-move search; the check fails when a listed literal no longer exists or an unlisted one appears, and the list only shrinks.
```

### P4

Outcome

```text
Nothing a user, a tracker or the database sees changes: no production file moves or changes behaviour in feature A, and every existing test stays green.
```

## Scope

None.

```text
Moving any production file (feature B), narrowing the rules to the new namespace only and the new Reports module rules R-RP1 to R-RP6 (feature C); ReportRepository and Models/Reports stay where they are.
```

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
A1: the Reports membership is measured from the IL and fixed in a namespace-agnostic test that finds each listed type in its old or new namespace, fails when a listed type is missing or a listed name does not exist or a Domain type reaches beyond the kernel, and records the targets that name moving types.
```

#### Promises

P1

P4

#### Depends on

None.

#### Slice work

None.

### S2

```text
A2: every rule pattern gives the same answer for each moving Reports type under its old and its new namespace, no added alternative matches a production type outside Modules.Reports, and no rule string names Reports by its old path outside the fixed rename list.
```

#### Promises

P2

P3

P4

#### Depends on

None.

#### Slice work

None.
