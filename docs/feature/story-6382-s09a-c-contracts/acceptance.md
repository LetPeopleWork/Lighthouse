# Acceptance brief: story\-6382\-s09a\-c\-contracts

## Slices

### S1

##### Covers

P1

```text
Read the map of what Licensing and Access publish, without events
```

```text
Exactly the two Access types and the one Licensing type the other contexts reach are listed
```

```text
The published list cannot shrink to nothing and pass unseen
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingAndAccessPublishTheirContractsArchUnitTest\.cs

EachContextPublishesTheTypesTheOthersReach\_AndTheListOfThemIsNotEmpty

```text
The list is emptied or loses a type, because every other check then looks at nothing
```

###### Obligations




##### Covers

P1

```text
Look up each published type of Licensing and Access in the compiled code
```

```text
Each is found exactly once, in the Contracts namespace of its own context
```

```text
Other contexts have one published place to name each of these types
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingAndAccessPublishTheirContractsArchUnitTest\.cs

EachPublishedTypeIsFoundOnceAndInTheContractsOfItsOwnContext

```text
A type is kept in its old place, present in both places, or put into another context's Contracts, only if the check looked at the old place or counted loosely
```

###### Obligations




##### Covers

P1

```text
Look in the old namespace of each published type
```

```text
None of them is found there
```

```text
Nothing of Licensing and Access can be reached outside its Contracts
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingAndAccessPublishTheirContractsArchUnitTest\.cs

NoPublishedTypeIsLeftInTheNamespaceItHadBefore

```text
A copy stays behind in the old namespace while the new one also exists
```

###### Obligations




##### Covers

P1

```text
Scan the compiled production and test code outside Licensing and Access for references to the old names
```

```text
No type names any published type through its old namespace
```

```text
Every use of Licensing and Access goes through their Contracts
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingAndAccessPublishTheirContractsArchUnitTest\.cs

NoTypeOutsideLicensingAndAccessNamesAPublishedTypeByItsOldName

```text
A type outside the two contexts still uses the old name, only if the scan skipped nested, test or generic uses
```

###### Obligations




## Expectations

### Rows

#### E1

##### Promises

P1

```text
Licensing and Access contract types (license service, RBAC administration service and the types they use) are available from their Contracts namespaces, and the product behaves as before for licence checks and access decisions.
```

```text
The oracle sees structure and build success; it does not see that licence checks and RBAC decisions still give the same answers for the same users.
```

yes

None.

None.

#### E2

##### Promises

P2

```text
Flow metrics and Forecasting contract types (metrics queries, forecast throughput status, ports Forecasting fills, what Refinement and Promises read of Forecasting) are available from Contracts, and metrics and forecasts show the same numbers as before.
```

```text
The oracle does not see whether forecast and metric values a user reads are unchanged, nor that Refinement and Promises still read the same forecast data.
```

yes

None.

None.

#### E3

##### Promises

P3

```text
Work Items contract types (blocked items, rule evaluation ports, refresh log, what the forecast waits for) are available from Contracts, and blocked-item rules, refreshes and forecast waiting behave as before.
```

```text
The oracle does not see whether a user still gets the same blocked-item results, refresh log entries and forecast timing after refresh.
```

yes

None.

None.

#### E4

##### Promises

P4

```text
Tracker integration contract types (connector port and capabilities, factory, description block merger, CSV option names, write-back target) are available from Contracts; a change touching a Contracts path is treated as shared by CI; connecting, syncing and writing back to trackers behave as before.
```

```text
The oracle does not see real connector sync or write-back results, nor that the CI classifier marks a Contracts-only change as shared.
```

yes

None.

None.

#### E5

##### Promises

P5

```text
Organisation set-up contract types are available from Contracts, its open host exposes only Contracts rather than Application or Domain, the remaining contract-breach list (R-C1) names only owned non-contract residue, and set-up behaves as before.
```

```text
The oracle does not see that set-up still works for a user, nor that the residue list is complete and free of contract types.
```

yes

None.

None.

#### E6

##### Promises

P1

P2

P3

P4

P5

```text
With the entries for these five areas deleted from the contract-breach list (R-C1), the rule still passes, and the product behaves as before for users.
```

```text
Deleting entries proves the list is smaller, not that no contract breach hides elsewhere or that user-visible behaviour is unchanged.
```

yes

None.

None.

### Waivers

None.

## Charter

None.

## Examination

None.
