# Product brief: story\-6362\-s03d\-b\-move

## Request

```text
Story #6362 S03d Package Promises (Deliveries), feature B of three (the move): move the Promises types measured in feature A into Modules/Promises (DeliveryForecastBlock with its percentile record into Domain; the rule service, source resolver, source sync, forecast publishing, forecast block renderer, their interfaces and the metric-values projector into Application; the two 'forecasts updated' handlers into Application/Policies) with their unit tests, behaviour-free, and pin the order of the two PortfolioForecastsUpdated handlers. Design authority: docs/feature/story-6362-s03d-package-promises-deliveries/feature-delta.md.
```

## Work

M

```text
Promises lives in its own module folder, so its rules can be enforced by namespace in feature C, with nothing a user, a tracker or the database sees changed
```

### Rework

None.

## Promises

### P1

Outcome

```text
Every Promises type measured in feature A lives under Modules/Promises in its designed layer: the forecast block records in Domain, the services, interfaces and projector in Application, the two forecasts-updated handlers in Application/Policies; none is left in its old namespace, and the Domain reaches only the shared kernel.
```

### P2

Outcome

```text
The unit tests of the moved types live under the matching Modules/Promises test folder, and the architecture rules that list a moved type name it in its new place, still compared both ways.
```

### P3

Outcome

```text
When a Portfolio's forecasts are updated, its Delivery metric snapshot is recorded before its forecasts are published, and a test pins that order.
```

### P4

Outcome

```text
Nothing a user, a tracker or the database sees changes: every Delivery answer, forecast publication, route, DI registration and the EF model stay the same, and every existing test stays green.
```

## Scope

None.

```text
Promises' own module rules, the Jira connector port inversion and the test-placement rule (feature C); repositories, the update code, the write-back round (#6657), ordering and ranking (S03i); any rename or signature change.
```

## Decisions

```text
Rigor standard for feature B: architect, acceptance designer and crafter on sonnet; reviewers and EXAMINE on haiku (maintainer, 2026-11-17).
```

#### Status

Confirmed


```text
Push per slice with CI watched; rebase before push allowed; no review window (behaviour-free); mutation once at the end of S03d on frozen code (maintainer, 2026-11-17).
```

#### Status

Confirmed


```text
EXAMINE observes through admitted probes only (architecture-rules, backend-tests, ef-model-unchanged, deliveries-answers-compare and deliveries-answers-selftest, refresh-reactions-show, test-selection-show, ci-live-suites-show); expectations name only what those can show, and only what a test name or a probe line states.
```

#### Status

Confirmed


## Journey

None.

## Slices

### S1

```text
B1: the forecast block records, services, interfaces and projector live in Modules/Promises Domain and Application, with their tests.
```

#### Promises

P1

P2

P4

#### Depends on

None.

#### Slice work

None.

### S2

```text
B2: the two forecasts-updated handlers live in Modules/Promises/Application/Policies, and their order is pinned.
```

#### Promises

P1

P2

P3

P4

#### Depends on

None.

#### Slice work

None.
