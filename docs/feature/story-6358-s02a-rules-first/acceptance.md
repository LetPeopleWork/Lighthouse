# Acceptance brief: story\-6358\-s02a\-rules\-first

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
Every architecture rule that checks or forbids the application core (types under Lighthouse.Backend.Services) applies in the same way to types under Lighthouse.Backend.Modules, so a type moved into a module still fails a rule it would have failed before the move.
```

#### E2

##### Promises

P2

```text
A test shows the order in which domain-event handlers run for TeamDataRefreshed and for PortfolioFeaturesRefreshed, and it fails if a move or registration change reorders them.
```

#### E3

##### Promises

P3

```text
After the change, no production file is edited and every test that existed before still passes.
```

## Charter

None.

## Examination

None.
