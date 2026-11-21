# Product brief: story\-6363\-s03e\-a\-rules\-first

## Request

```text
ADO Story #6363 S03e Package Access and Licensing, feature A (rules first): before any file moves, every architecture rule that names Access or Licensing by namespace also sees Modules/Access and Modules/Licensing; the membership of both modules and each type's layer are measured from the IL and fixed; an answers probe for Access and Licensing and a CI probe for the live authentication, RBAC and proxy-auth runs exist. DESIGN and DEVOPS: docs/feature/story-6363-s03e-package-access-licensing/feature-delta.md (AFK propose mode, maintainer answers 2026-11-17).
```

## Work

M

```text
The Access and Licensing move can happen without any architecture rule silently losing sight of the code it guards
```

### Rework

None.

## Promises

### P1

Outcome

```text
Every architecture rule that names Access or Licensing by namespace also sees Modules/Access and Modules/Licensing: a made-up Access or Licensing type there that reaches Work Items, Tracker integration, Promises or a repository is caught, and a made-up Forecasting Application type reaching a made-up Modules.Licensing type is caught by Forecasting's licence selector.
```

### P2

Outcome

```text
The membership of the Access and Licensing modules, the layer of each type (Domain or Application) and the Models types admitted into them are measured from the compiled code and fixed in a test that finds each listed type in its old or its new namespace and fails when a listed type is missing or a listed name does not exist.
```

### P3

Outcome

```text
An access-answers examine command compares the Access and Licensing answers of two revisions (auth mode, session, my-summary, RBAC status, system-wide rules, everyone-signed-in, API keys, licence status before and after a licence import) and says DIFFERENT with exit 1 when one answer changed and IDENTICAL with exit 0 when none did.
```

### P4

Outcome

```text
A CI examine command shows, for the newest finished CI run at or before a revision, the Verify Authentication job conclusion and the conclusion of its authentication, RBAC and proxy-auth steps, and shows a failed step as failed.
```

### P5

Outcome

```text
Nothing a user, a tracker or the database sees changes: production code is unchanged and every existing test stays green.
```

## Scope

```text
Widening every old-namespace Access, Authorization and Licensing pattern in the Architecture rule files (found by a re-grep at A1's start, including Forecasting's licence selector and S03d's Promises rules); the IL-measured membership, layer and Models admission lists of both modules; the access-answers-compare and ci-auth-suites-show examine commands, declared in .nwave/examine.json.
```

```text
Moving any Access or Licensing file, test or licence test data (feature B, B1-B4); the modules' own rules, outbound lists and test placement (feature C); a CI test-selection classifier slice (dropped: already backend-only, shown by test-selection-show); Access persistence edges (catalogue note, Epic 6507); OIDC sign-in with a real identity provider outside CI and embed handshakes from a foreign host (left to the Keycloak runs and unit tests).
```

## Decisions

```text
Rigor thorough for feature A: architect, acceptance designer and crafter on opus; reviewers and EXAMINE on sonnet (maintainer, 2026-11-17).
```

#### Status

Confirmed


```text
S03e starts only after S03d is closed (shared rule files); size M, about 7.5 days, 11 slices in three features (A 4, B 4, C 3); no review window (behaviour-free); UI sketch, loading and failure looks N/A; usage-data event N/A because nothing a user sees or does changes (maintainer, 2026-11-17).
```

#### Status

Confirmed


```text
EXAMINE observes through admitted probes only: architecture-rules (Access, Licensing, Rbac), ef-model-unchanged, test-selection-show, ci-live-suites-show, backend-tests, and the access-answers-compare and ci-auth-suites-show commands once A3 and A4 add them.
```

#### Status

Confirmed


```text
A5 (CI classifier cases for the module paths) dropped in design review round 1: BACKEND_REGEX already classifies every Lighthouse.Backend path as backend-only, so it has no RED; the selection stays observable through test-selection-show.
```

#### Status

Confirmed


## Journey

None.

## Slices

### S1

```text
A1: every architecture rule that names Access or Licensing by namespace also sees Modules/Access and Modules/Licensing; made-up offenders there, including a Forecasting type reaching a made-up Modules.Licensing type, are caught.
```

#### Promises

P1

P5

#### Depends on

None.

#### Slice work

S

```text
No rule silently stops seeing Access or Licensing code when it moves
```

##### Rework

None.

### S2

```text
A2: the membership of both modules, each type's layer and the Models admissions are measured from the IL and fixed in a namespace-agnostic test.
```

#### Promises

P2

P5

#### Depends on

None.

#### Slice work

S

```text
The move in feature B has a fixed, measured list of what moves and where
```

##### Rework

None.

### S3

```text
A3: the access-answers-compare examine command says DIFFERENT (exit 1) for two captures differing in one answer and IDENTICAL (exit 0) for identical ones.
```

#### Promises

P3

P5

#### Depends on

None.

#### Slice work

S

```text
A changed Access or Licensing answer after the move is caught at EXAMINE
```

##### Rework

None.

### S4

```text
A4: the ci-auth-suites-show examine command shows the Verify Authentication job and its authentication, RBAC and proxy-auth steps for a revision, a failed step as failed.
```

#### Promises

P4

P5

#### Depends on

None.

#### Slice work

S

```text
The live authentication, RBAC and proxy-auth runs of the move push are visible at EXAMINE
```

##### Rework

None.
