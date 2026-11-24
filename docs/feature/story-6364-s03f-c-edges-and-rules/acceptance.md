# Acceptance brief: story\-6364\-s03f\-c\-edges\-and\-rules

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
A maintainer sees, from the architecture-rules run, that the layout rule for the platform lane passes today: every type in the membership map sits in its Platform area, namespace equal to folder, and none is left under Services. A made-up listed type under Services or in the wrong area would fail the rule and the failure names the type and its expected area.
```

#### E2

##### Promises

P2

```text
A maintainer sees, from the architecture-rules run, that the two boundary rules pass with an empty baseline: no Platform type reaches an API type or a module's Adapters. A made-up offending platform type would fail the matching rule and the message speaks of the platform lane, not a module.
```

#### E3

##### Promises

P3

```text
A maintainer sees that the platform lane's edges into Services are one inline list by full type name, matching the 19 edges the IL shows, checked both ways. A made-up new edge not on the list fails, and a listed edge that no longer exists fails. The list is not loosened.
```

#### E4

##### Promises

P4

```text
A future contributor who puts a unit test of a platform type in an old Tests/Services folder is stopped by the placement rule, whose message names the test and the Tests/Platform/<Area> folder it belongs in. The tests that stay by design (update pipelines, context gold tests, seeders, repositories, zip host fixtures) are not flagged, and today's suite passes the rule.
```

#### E5

##### Promises

P5

```text
A maintainer sees that every production file left in Services/Interfaces and Services/Implementation is named per file with its owner, and that none is a platform file. A made-up new file there fails the rule, and a listed file that no longer exists fails too.
```

#### E6

##### Promises

P6

```text
Nothing a user, API client, tracker or the database sees changes: the platform-answers and refresh-reactions comparisons report IDENTICAL, the EF model is unchanged, feature A's preservation law and B's five layout rules and every existing architecture rule keep their verdicts with nothing loosened, the lane's module edges are not listed a second time, the filtered backend suite is green and the build has zero warnings. The test-selection show confirms the two new oracles are part of the selection.
```

## Charter

None.

## Examination

None.
