# Acceptance brief: story\-6366\-s03h\-a\-rules\-first

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
Observing the compiled test assembly through the architecture-rules probe, the Reports module membership list shows each moving Reports type with its layer (Domain, Application or Application/Metrics) and its old and new namespace, and each staying Reports file with an owner and reason; every listed type is found under exactly its old or new namespace, and no listed name is absent.
```

#### E2

##### Promises

P1

```text
A Domain type in the Reports membership list depends on nothing beyond the kernel and the base library; the probe shows no Domain Reports type referencing any other assembly or namespace, and the membership test fails if a listed type is missing or a listed name does not exist.
```

#### E3

##### Promises

P2

```text
For each moving Reports type, every architecture rule pattern in the test assembly gives the same match answer under the new Modules.Reports namespace as under the old namespace, as shown by architecture-rules probes run against Reports class fragments.
```

#### E4

##### Promises

P2

```text
No alternative added to a rule pattern matches any production type outside Modules.Reports; probing the rules with non-Reports production class fragments shows no new match.
```

#### E5

##### Promises

P3

```text
No rule constant or string literal in the test project names Reports by the old Services.Implementation.Reports, Services.Interfaces.Reports or Services/(Implementation|Interfaces)/Reports path, except entries of a fixed rename list; the check fails when a listed literal no longer exists or an unlisted one appears, and the list can only shrink.
```

#### E6

##### Promises

P4

```text
Nothing a user, a tracker or the database sees changes: the ef-model-unchanged probe reports the EF model identical to before, and no production file moves or changes behaviour in feature A.
```

#### E7

##### Promises

P4

```text
Every existing backend test stays green after the change, as seen through the backend-tests and test-selection-show probes.
```

## Charter

None.

## Examination

None.
