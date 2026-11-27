# Acceptance brief: story\-6382\-s09a\-d\-events\-and\-rules

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
Using the public architecture rules and backend tests, a user sees every one of the 15 domain event types in the Contracts of the context that publishes it, IDomainEvent being the only event type left in Models.Events, and the backend test suite and the architecture rules pass as before.
```

```text
A rule or test count passing does not show each event sits in its publishing context's Contracts rather than another context's; it also cannot show that no event was renamed or its payload changed.
```

yes

None.

```text
architecture-rules and backend-tests probes against the built candidate
```

#### E2

##### Promises

P2

```text
After a forecast round that succeeds, fails, is cancelled or is skipped, the refresh log shows the same entry as before this feature; a failing log write is logged and the round still completes; the forecast updater no longer names the refresh-log service.
```

```text
Compare answers show entries, but cannot show which component wrote them, nor reliably force a log-write failure from outside; the absence of the service name in the updater is a structural fact public probes see only through an architecture rule.
```

yes

None.

```text
refresh-answers-compare and forecast-answers-compare against the base revision; architecture-rules probe
```

#### E3

##### Promises

P3

```text
The architecture rules show every module-to-module edge governed by contracts rule R-C1 only, and a per-module boundary list naming a module-to-module edge makes the rules fail; ADR-270 and ARCHITECTURE.md section 4 describe the contracts layer and the CI test-selection payoff.
```

```text
A passing rule run does not show the rule would fail on a planted per-module list entry unless a negative control is captured; reading the documents shows wording but not that it matches shipped behaviour.
```

yes

None.

```text
architecture-rules probe with a negative control; public reading of the two documents
```

#### E4

##### Promises

P1

P2

P3

```text
Across the feature the product behaves as before: forecast and refresh answers equal those of the base revision and the backend tests pass, with no new user-visible change.
```

```text
Answer comparisons cover only the sampled forecast and refresh scenarios; behaviour outside them, and timing or ordering of event handling, stays unobserved.
```

yes

None.

```text
refresh-answers-compare, forecast-answers-compare and backend-tests probes
```

### Waivers

None.

## Charter

None.

## Examination

None.
