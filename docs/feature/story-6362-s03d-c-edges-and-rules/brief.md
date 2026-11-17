# Product brief: story\-6362\-s03d\-c\-edges\-and\-rules

## Request

```text
Story #6362 S03d Package Promises (Deliveries), feature C of three (edges and rules): give Promises its own module rules by namespace with inline lists (R-P1..R-P7) and shrink the Work Items and Tracker integration lists that named Promises types; invert the one Tracker integration to Promises edge (the Jira connector merging the forecast block) through a port IDescriptionBlockMerger defined in Tracker integration Application and implemented by the renderer, with no change to the text written to Jira; and check that every Promises unit test lives under Tests/Modules/Promises and no Promises source file is left in Services apart from the named exceptions. Design authority: docs/feature/story-6362-s03d-package-promises-deliveries/feature-delta.md.
```

## Work

M

```text
The Promises module keeps its shape on its own: a new reach across its border fails a named rule, and Tracker integration no longer depends on Promises, with nothing a user, a tracker or the database sees changed
```

### Rework

None.

## Promises

### P1

Outcome

```text
Promises has its own module rules: each Promises type lives in its layer, its Domain reaches only the shared kernel, outsiders reach its Domain, Application and Policies only as listed, and its Application reaches no EF Core, Data, API, another module's Adapters, update code, write-back round, Access or Licensing except as listed; each list is measured from the compiled code and compared both ways, and a made-up offender against each rule is named.
```

### P2

Outcome

```text
The Work Items and Tracker integration rules no longer list Promises types by name: Promises is legal there by namespace, and their lists are smaller by exactly the Promises entries.
```

### P3

Outcome

```text
Tracker integration reaches no Promises type: the Jira connector merges the Delivery forecast block through a port that Tracker integration defines and the Promises renderer implements.
```

### P5

Outcome

```text
Nothing a user, a tracker or the database sees changes: the text written to Jira, every Delivery answer, forecast, route, DI behaviour and the EF model stay the same, and every existing test stays green.
```

## Scope

None.

```text
Repositories, the update code, the write-back round (#6657), ordering and ranking (S03i); moving the snapshot handler test off the app host; any rename or signature change beyond the extracted port.
```

## Decisions

```text
Rigor thorough for feature C: architect, acceptance designer and crafter on opus; reviewers and EXAMINE on sonnet (maintainer, 2026-11-17).
```

#### Status

Confirmed


```text
C2 stays in S03d as the last edit of feature C; push per slice with CI watched; rebase before push allowed; no review window (behaviour-free); mutation once at the end of S03d on frozen code, then finalize (maintainer, 2026-11-17).
```

#### Status

Confirmed


```text
EXAMINE observes through admitted probes only; expectations name only what a probe line states, use test-name fragments without dots or slashes, and name up front what no probe can observe (the four blocked-period reactions of refresh-reactions-compare).
```

#### Status

Confirmed


```text
C3 dropped as an nwdes slice (host decision in AFK mode, as A4 was): feature B already moved every Promises unit test and source file, so the placement check has no RED; it passes 29/29 on arrival and a throwaway probe proved it names a misplaced file. The check is kept as a regression guard (Architecture/PromisesUnitTestPlacementTest.cs), committed with feature C and judged by the whole-candidate review.
```

#### Status

Confirmed


## Journey

None.

## Slices

### S1

```text
C1: Promises module rules with measured inline lists; the Work Items and Tracker integration lists lose their Promises entries.
```

#### Promises

P1

P2

P5

#### Depends on

None.

#### Slice work

None.

### S2

```text
C2: the Jira connector merges the forecast block through IDescriptionBlockMerger; Tracker integration reaches no Promises type.
```

#### Promises

P3

P5

#### Depends on

None.

#### Slice work

None.
