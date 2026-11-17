# Product brief: story\-6362\-s03d\-a\-rules\-first

## Request

```text
Story #6362 S03d Package Promises (Deliveries), feature A of three (rules first): before any Promises file moves into Modules/Promises, make every architecture rule that names a Promises type, namespace or folder also see Modules/Promises; measure the Promises membership and the Models admissions from the IL and fix them in a test; add a Delivery-answers EXAMINE command that reports a changed Delivery answer; pin the CI test selection for Promises paths and for the C2 port path. Design authority: docs/feature/story-6362-s03d-package-promises-deliveries/feature-delta.md.
```

## Work

M

```text
The Promises move in feature B is guarded from its first file: a rule that would miss a moved type, a CI selection that would skip a suite, or a changed Delivery answer is caught
```

### Rework

None.

## Promises

### P1

Outcome

```text
Every architecture rule that names a Promises type, namespace or folder also sees Modules/Promises: a made-up Delivery service under Modules.Promises that reaches a repository, Forecasting or the update code is caught by today's rules.
```

### P2

Outcome

```text
The Promises membership (the types that move) and the Models types admitted into the module are measured from the compiled code and fixed in a test that fails when a listed type is missing or an unlisted one appears.
```

### P3

Outcome

```text
A Delivery-answers examine command compares the Delivery answers of two revisions and says DIFFERENT when one Delivery answer changed and IDENTICAL when none did.
```

### P4

Outcome

```text
CI selection is pinned: a change under Modules/Promises selects the backend suite only; a change to the Tracker integration port the Jira connector will take, or to Program.cs, selects every live connector suite.
```

### P5

Outcome

```text
Nothing a user, a tracker or the database sees changes: production code is unchanged and every existing test stays green.
```

## Scope

None.

```text
Moving any Promises file (feature B); the module's own rules, the C2 port and test placement (feature C); repositories, the update code, the write-back round (#6657), ordering and ranking (S03i).
```

## Decisions

```text
Rigor thorough for feature A: architect, acceptance designer and crafter on opus; reviewers and EXAMINE on sonnet (maintainer, 2026-11-17).
```

#### Status

Confirmed


```text
Push per slice with CI watched; rebase before push allowed; no review window (behaviour-free); mutation once at the end of S03d on frozen code (maintainer, 2026-11-17).
```

#### Status

Confirmed


```text
EXAMINE observes through admitted probes only (architecture-rules with a class fragment, test-selection-show, ci-live-suites-show, ef-model-unchanged, and the Delivery-answers command once A3 adds it); expectations name only what those can show.
```

#### Status

Confirmed


## Journey

None.

## Slices

### S1

```text
A1: the existing rules see Modules/Promises; a made-up offender there is caught.
```

#### Promises

P1

P5

#### Depends on

None.

#### Slice work

None.

### S2

```text
A2: the Promises membership and Models admissions measured from the IL and pinned.
```

#### Promises

P2

P5

#### Depends on

None.

#### Slice work

None.

### S3

```text
A3: the Delivery-answers examine command and its behavioural test.
```

#### Promises

P3

P5

#### Depends on

None.

#### Slice work

None.

### S4

```text
A4: CI selection cases for Promises paths and the C2 port path.
```

#### Promises

P4

P5

#### Depends on

None.

#### Slice work

None.
