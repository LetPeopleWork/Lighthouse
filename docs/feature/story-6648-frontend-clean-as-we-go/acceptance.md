# Acceptance brief: story\-6648\-frontend\-clean\-as\-we\-go

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
Each of the three test files gets faster per test: the median per-test time over at least 4 main runs, measured the same way as the baseline, is lower than before for each file.
```

#### E2

##### Promises

P2

```text
Every test removed from or moved out of the three test files has a named replacement test that now proves the same rule.
```

#### E3

##### Promises

P2

```text
Every moved rule is shown to fail when its production line is deliberately broken, so the moved test still has teeth.
```

#### E4

##### Promises

P3

```text
Users see and experience Lighthouse exactly as before: no production behaviour change and no pixel change in the rendered views.
```

#### E5

##### Promises

P3

```text
The views that remain keep their loading look and their failure look, so a slow or failed load still shows the same loading and could-not-load states as before.
```

## Charter

None.

## Examination

None.
