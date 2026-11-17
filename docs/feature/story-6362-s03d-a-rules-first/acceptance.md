# Acceptance brief: story\-6362\-s03d\-a\-rules\-first

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
Running the architecture-rules probe lists the Promises-related architecture tests as Passed, and the tests whose names state a made-up offender is reported (a Delivery service under Modules.Promises reaching a repository, Forecasting or the update code) show Passed, so such an offender is caught.
```

#### E2

##### Promises

P2

```text
The backend-tests probe on the Promises membership test shows it Passed, and its test names state that a missing listed type and an unlisted extra type each make it fail, for both the types that move and the Models types admitted into the module.
```

#### E3

##### Promises

P3

```text
The deliveries-answers-compare probe prints DIFFERENT when two revisions differ in one Delivery answer, and IDENTICAL when no Delivery answer differs (including base against candidate of this feature).
```

#### E4

##### Promises

P4

```text
The test-selection-show probe shows a path under Modules/Promises selects the backend suite only, and the Tracker integration port path for the Jira connector or Program.cs selects every live connector suite; the recorded selection scenarios pass.
```

#### E5

##### Promises

P5

```text
The ef-model-unchanged probe reports the database model unchanged, deliveries-answers-compare of base against candidate prints IDENTICAL, and the architecture-rules and backend-tests probes show every existing test Passed with none Failed.
```

## Charter

None.

## Examination

None.
