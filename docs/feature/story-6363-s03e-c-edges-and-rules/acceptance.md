# Acceptance brief: story\-6363\-s03e\-c\-edges\-and\-rules

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
Running the architecture tests for the Access module rules passes, and each Access rule list (types per layer, Domain reach, outsider reach, Application reach, no other context) is inline by full type name and compared both ways against the compiled code.
```

#### E2

##### Promises

P1

```text
A made-up Access offender at each list position (including one that reaches only through an attribute) is reported as a failure by a named Access rule, and no type is left under the old Services Auth or Authorization namespaces.
```

#### E3

##### Promises

P2

```text
Running the architecture tests for the Licensing module rules passes: Licensing types sit only in Application and Adapters/Http, none under the old Services Licensing namespaces, no reach to Access or other contexts, and the outsider lists (ForecastUpdater included) and platform reach list are compared both ways against compiled code.
```

#### E4

##### Promises

P2

```text
A made-up Licensing offender against each Licensing rule fails a named rule, and the Forecasting and Work Items lists naming Access or Licensing types use the new type names.
```

#### E5

##### Promises

P3

```text
The placement test passes: every Access and Licensing unit test is under Tests/Modules/Access or Tests/Modules/Licensing, no file of either context is left in Services/Interfaces or Services/Implementation (production or tests), no exception exists for the old Licensing test folder, and RbacIdentityPattern is gone with its assertion carried by the new module rules.
```

#### E6

##### Promises

P3

```text
A made-up Access or Licensing file placed in an old folder is caught and named by the placement test.
```

#### E7

##### Promises

P4

```text
The existing rules EveryActionDeclaresItsAccess, AccessTablesSingleReader, LicenseGateSingleSource and the Reports and Refinement guard rules still run and pass with all assertions kept, and the other existing tests stay green; no route, guard, scheme, RBAC or licence answer changes.
```

## Charter

None.

## Examination

None.
