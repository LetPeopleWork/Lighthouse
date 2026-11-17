# Product brief: story\-6359\-s03a\-c\-edges\-and\-rules

## Request

```text
Story #6359 S03a feature C of three (edges and rules): give Tracker integration, now living in Modules/TrackerIntegration after feature B, its own module rules with inline lists measured from the IL (R-T1..R-T8 of the design's Ratchet design, with D-7's one listed edge and D-14's three Application types that name connectors listed by exact type name, and full-name keys where a short name is shared), retire the old leaf pattern WorkTrackingIntegrationPattern and the old classifier and dev-test alternatives, and pin that every unit test of the module sits under Tests/Modules/TrackerIntegration and no file of the context is left in Services/Interfaces or Services/Implementation apart from the named exceptions. Design authority: docs/feature/story-6359-s03a-package-tracker-integration/feature-delta.md.
```

## Work

M

```text
A change that makes Tracker integration reach another context, or another context reach its adapters, fails a named rule instead of passing unnoticed
```

### Rework

None.

## Promises

### P1

Outcome

```text
Tracker integration has its own module rules: Domain reaches only the kernel and the base library; outside code reaches Domain, Application and Adapters only by the legal namespaces plus inline lists measured from the IL; Application reaches no EF Core, Data, API or other module's Adapters except listed entries; the module reaches no other context except the one listed Promises renderer edge; no module type reaches a repository except the listed Application services. Each list is compared both ways and a made-up offender in each position is caught.
```

### P2

Outcome

```text
The old leaf pattern WorkTrackingIntegrationPattern and the old Services/Factories alternatives in the CI path classifier and dev-test selection are gone, and a change to a connector under the module still selects that tracker's live suite.
```

### P3

Outcome

```text
Every unit test of the module sits under Lighthouse.Backend.Tests/Modules/TrackerIntegration, and no file of the context is left under Services/Interfaces, Services/Implementation or Factories apart from the named exceptions; a made-up test or source file in an old folder is caught.
```

### P4

Outcome

```text
Nothing a user, a tracker or the database sees changes: production code is unchanged apart from what a rule forces, every existing test stays green, and the live connector suites stay green in CI.
```

## Scope

None.

```text
Moving repositories, the connection save service, connection usage, the write-back round, encryption or demo data (D-1, D-4, D-6); shrinking any listed edge (later Stories own them: S03d the Promises renderer edge, S09a the connector construction, S03i connection save).
```

## Decisions

```text
Rigor thorough for feature C: architect, acceptance designer and crafter on opus; reviewers and EXAMINE on sonnet (maintainer, 2026-11-16).
```

#### Status

Confirmed


```text
Push per slice with CI watched; rebase before push allowed today; mutation once at the end of S03a on frozen code (maintainer, 2026-11-16 and 2026-11-17).
```

#### Status

Confirmed


```text
EXAMINE observes through the architecture-rules probe with a class-name fragment, the test-path classifier and ci-live-suites-show; expectations name only what those probes can show (lesson of feature B, 2026-11-17).
```

#### Status

Confirmed


## Journey

None.

## Slices

### S1

```text
C1: the module boundary rules with inline lists measured from the IL replace the old leaf pattern; the old classifier and dev-test alternatives are removed.
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
C2: the unit-test placement rule and the no-file-left check for the old folders.
```

#### Promises

P3

P4

#### Depends on

None.

#### Slice work

None.
