# Product brief: story\-6382\-s09a\-c\-contracts

## Request

```text
Story 6382 S09a feature C, contracts: each context that other contexts reach publishes the types they may name in Modules/<X>/Contracts (namespace = folder), chosen by the measured closure of feature A's ContextContractsMembership; Licensing and Access, then Flow metrics and Forecasting, then Work Items, then Tracker integration (with the CI path classifier), then Organisation set-up (its open host narrowed to Contracts). Each slice's RED oracle is the contracts rule R-C1 with that slice's ratchet entries deleted; only namespace and using lines change.
```

## Work

L

```text
Every cross-context reference goes through the publisher's Contracts namespace or is a listed, owned residue edge, so the contracts-only rule holds and S09b can turn Contracts into an assembly boundary, with no behaviour change.
```

### Rework

None.

## Promises

### P1

Outcome

```text
Licensing and Access publish their contract types (ILicenseService, IRbacAdministrationService and its signature closure) in their Contracts namespace; R-C1 holds with their entries deleted; behaviour unchanged.
```

#### Amendment

None.

### P2

Outcome

```text
Flow metrics and Forecasting publish their contract types (the metrics queries, ForecastThroughputStatus, the ports Forecasting fills, what Refinement and Promises read of Forecasting) in Contracts; R-C1 holds with their entries deleted; behaviour unchanged.
```

#### Amendment

None.

### P3

Outcome

```text
Work Items publishes its contract types (blocked items, rule evaluation ports, refresh log, what the forecast waits for) in Contracts; R-C1 holds with its entries deleted; behaviour unchanged.
```

#### Amendment

None.

### P4

Outcome

```text
Tracker integration publishes its contract types (the connector port and capability interfaces, the factory, IDescriptionBlockMerger, CsvWorkTrackingOptionNames, IWriteBackTarget) in Contracts, and the CI path classifier treats a Contracts path as shared; R-C1 holds with its entries deleted; behaviour unchanged.
```

#### Amendment

None.

### P5

Outcome

```text
Organisation set-up publishes its contract types in Contracts and its open host narrows from Application or Domain by right to Contracts; after this slice R-C1 lists only the owned non-contract residue; behaviour unchanged.
```

#### Amendment

None.

## Scope

```text
The Contracts folders of Licensing, Access, Flow metrics, Forecasting, Work Items, Tracker integration and Organisation set-up; the namespace and using lines that follow; ContextContractsMembership and R-C1 ratchet entries; the CI path classifier and its test (C4).
```

```text
Domain event types and RX1 (feature D); per-module rule narrowing and docs (feature D3); Contracts folders for Promises, Refinement and Reports (none expected); any behaviour, registration, schema, route or DTO change; fixing a non-contract residue edge.
```

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
R-C1 with the Licensing and Access entries deleted fails until their contract types move to Contracts; then it and every other Architecture rule pass.
```

#### Promises

P1

#### Depends on

None.

#### Slice work

M

```text
Licensing and Access are named by other contexts only through Contracts.
```

##### Rework

None.

### S2

```text
R-C1 with the Flow metrics and Forecasting entries deleted fails until their contract types move; then every Architecture rule passes.
```

#### Promises

P2

#### Depends on

S1

#### Slice work

M

```text
Flow metrics and Forecasting are named by other contexts only through Contracts.
```

##### Rework

None.

### S3

```text
R-C1 with the Work Items entries deleted fails until its contract types move; then every Architecture rule passes.
```

#### Promises

P3

#### Depends on

S2

#### Slice work

S

```text
Work Items is named by other contexts only through Contracts.
```

##### Rework

None.

### S4

```text
R-C1 with the Tracker integration entries deleted fails until its contract types move; the path classifier test classifies a Tracker integration Contracts path as shared; then every Architecture rule and the classifier test pass.
```

#### Promises

P4

#### Depends on

S3

#### Slice work

M

```text
Tracker integration is named only through Contracts, and CI still selects the right suites for a change there.
```

##### Rework

None.

### S5

```text
R-C1 with the Organisation set-up entries deleted, and its open host narrowed to Contracts, fails until the types move; then R-C1 holds only the owned residue and every Architecture rule passes.
```

#### Promises

P5

#### Depends on

S4

#### Slice work

M

```text
Organisation set-up is named only through Contracts; the ratchet list is down to its owned residue.
```

##### Rework

None.
