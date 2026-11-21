# Acceptance brief: story\-6363\-s03e\-a\-rules\-first

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
A made-up Access or Licensing type placed under Modules/Access or Modules/Licensing that reaches Work Items, Tracker integration, Promises or a repository is reported by the architecture rules, exactly as one in the old namespaces is; and a made-up Forecasting Application type reaching a made-up Modules.Licensing type is reported by Forecasting's licence rule.
```

#### E2

##### Promises

P2

```text
A maintainer can see, in a test, the fixed list of Access and Licensing types with the layer (Domain or Application) of each and the Models types admitted; the test finds each listed type in its old or new namespace and fails when a listed type is missing or a listed name does not exist.
```

#### E3

##### Promises

P3

```text
Running the access-answers examine command on two revisions that give the same auth mode, session, my-summary, RBAC status, system-wide rules, everyone-signed-in, API keys and licence status (before and after a licence import) prints IDENTICAL and exits 0; if any one of these answers differs it prints DIFFERENT and exits 1.
```

#### E4

##### Promises

P4

```text
Running the CI examine command for a revision shows the Verify Authentication job conclusion and the conclusions of its authentication, RBAC and proxy-auth steps from the newest finished CI run at or before that revision, and a step that failed is shown as failed, not as passed or missing.
```

#### E5

##### Promises

P5

```text
A user, a tracker or the database sees no change in behaviour or data after this feature, and the existing test suites still pass.
```

## Charter

None.

## Examination

None.
