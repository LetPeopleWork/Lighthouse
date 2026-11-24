# Acceptance brief: story\-6365\-s03g\-a\-rules\-first

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
A single membership test for the Refinement module exists and passes when run through backend-tests (fragment naming it): it lists every moving Refinement type with layer, old and new namespace, and every staying Refinement file with owner and reason.
```

#### E2

##### Promises

P1

```text
Negative control: when a listed type name does not exist, or a listed type is found in neither its old nor its new namespace, the membership test fails; with the real code it does not.
```

#### E3

##### Promises

P1

```text
Layer rule: a type listed as Domain reaches nothing beyond the kernel, the BCL and other Refinement Domain types; architecture-rules with a Refinement fragment shows this rule present and passing, and a Domain type that reaches an outside type would fail it.
```

#### E4

##### Promises

P1

```text
The membership test also records, in a readable form, every suppression, analyser, CI-selection, test-budget, host-user and Stryker target that names a moving type; test-selection-show lists the CI selection entries and each moving type named there appears in that record.
```

#### E5

##### Promises

P2

```text
For each moving Refinement type, each architecture rule pattern in the test assembly gives the same verdict under the new Modules.Refinement namespace as under the old one; architecture-rules over the whole rule set passes, and a probe type in the new namespace is matched exactly where its old-namespace twin was.
```

#### E6

##### Promises

P2

```text
No added namespace alternative in a rule matches a production type outside Modules.Refinement: a type outside the module (for example another module's type) keeps the verdict it had before the rules were changed.
```

#### E7

##### Promises

P2

```text
The rules that used the exact old namespace (equality, DoNotResideInNamespace, the exact-namespace assertion) now read from the membership map and keep their verdict through the move; architecture-rules for those fragments passes, and removing a type from the map makes the dependent rule fail rather than pass silently.
```

#### E8

##### Promises

P3

```text
A check exists that fails when an unlisted rule constant or string literal in the test project names Refinement by Services.Implementation.Refinement, Services.Interfaces.Refinement or Services/Implementation/Refinement; with the real tree it passes through backend-tests.
```

#### E9

##### Promises

P3

```text
The fixed rename list is checked both ways: a listed literal that no longer exists fails the check, an unlisted new one fails it, and the list may only shrink compared with the pre-move search.
```

#### E10

##### Promises

P4

```text
ef-model-unchanged reports the EF model identical to before feature A, for every supported database provider (no migration, no schema or mapping change).
```

#### E11

##### Promises

P4

```text
No production file is moved, renamed or behaviourally changed by feature A: the only differences are in the test project; a Refinement type keeps its old namespace and the API (routes, DTOs, responses) is unchanged.
```

#### E12

##### Promises

P4

```text
Every existing backend test stays green (whole-suite run excluding live-connector categories), and test-selection-show still selects the same tests as before for the CI selection.
```

## Charter

None.

## Examination

None.
