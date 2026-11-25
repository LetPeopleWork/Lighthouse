# Acceptance brief: story\-6657\-write\-back\-round

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
For every architecture and layout rule that judges the write-back round, the architecture-rules probe shows the same pass/fail verdict after the round moves as before it, with no rule loosened and no rule dropped or silently emptied; rules still see the round's types.
```

#### E2

##### Promises

P2

```text
The write-back round's types and its unit tests are listed under Forecasting's Application/WriteBack place, and no longer under the old loose Services namespaces; types that stay in those loose namespaces are untouched; the backend tests and the EF model probe show no behaviour change.
```

#### E3

##### Promises

P3

```text
Rules refer to the round only by its Forecasting namespace (the old Services path no longer names it), and the architecture-rules probe lists boundary rules specific to the write-back round that pass, each catching a made-up offender.
```

## Charter

None.

## Examination

None.
