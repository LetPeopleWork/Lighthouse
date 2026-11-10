# Acceptance brief: story\-6374\-s05f\-a\-team\-reads

## Slices

### S1

##### Covers

P1

```text
Scan every production source file for calls to the general predicate read, with the Team metrics service taken off the allow-list and the list held at 14 entries
```

```text
Fails while the Team metrics service still calls the general predicate read, naming that file as a caller not on the list; passes once it reads only through the named reads and the scanned callers equal the 14 listed files
```

```text
Team metrics read their data only through the named reads of the repositories that own it
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs\:\:GetAllByPredicate\_IsCalledOnlyFromTheFilesOnTheList

```text
The service reaches the general predicate read through a delegate or reflection, or a named read builds a different predicate internally; the latter is caught by the provider parity rows
```

###### Obligations




##### Covers

P2

```text
On a migrated SQLite file with two Teams holding items in all four state categories, transitions for several items, and Features with a default size, zero and non-zero Team work and two Portfolios, run each named read and the predicate it replaces, each in a fresh context
```

```text
For each Team and each read: the same item ids in the same sequence, the Team loaded and the items tracked; the count equals the predicate's count and the length of the to-do-or-doing read; both spellings of doing-or-done give the same sequence; the transitions of the chosen items match; the Features read matches with all related data loaded, for any state and for each single state
```

```text
Team metrics computed from the named reads count and order the same items as today on SQLite
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/TeamMetricsReadsParityOnBothProvidersTests\.cs\:\:Every\_named\_Team\_read\_gives\_what\_its\_predicate\_gives\_on\_Sqlite

```text
A row-order difference that only appears on data much larger than the fixture, or the count read loading rows and counting them in memory (same number)
```

###### Obligations





##### Covers

P2

```text
The same seeded instance and the same read-by-read comparison on a Postgres database in a container
```

```text
Every named read gives the same ids in the same sequence, the same loaded related data and tracking, and the same count as the predicate it replaces, on Postgres
```

```text
Team metrics computed from the named reads count and order the same items as today on Postgres
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/TeamMetricsReadsParityOnBothProvidersTests\.cs\:\:Every\_named\_Team\_read\_gives\_what\_its\_predicate\_gives\_on\_Postgres

```text
A plan-dependent row order on Postgres that only shows on data much larger than the fixture
```

###### Obligations





##### Covers

P3

```text
Ask the Forecast Reality Check for its answers on real SQLite with the real work-item repository, counting database statements
```

```text
The statement count per answer stays 20 and 24, with the assertion unchanged
```

```text
The Reality Check and the Team metrics it reads do not get slower through extra database round trips
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ForecastRealityCheck\/RealityCheckQueryCountTest\.cs\:\:One\_check\_on\_a\_cold\_cache\_reads\_the\_Teams\_finished\_work\_once\_per\_window\_it\_asks\_about

```text
A statement that changed shape while the count stays equal
```

###### Obligations


##### Covers

P3

```text
Run the existing Team metrics tests with only their read stubs retargeted to the named reads
```

```text
Every existing Assert, Is and Verify line passes unchanged; the check that no Features are read where none should be now names the Features read with the same never
```

```text
Every Team metrics answer the existing tests pin stays the same
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/TeamMetricsServiceTests\.cs

```text
A named read that the mock stubs regardless of its predicate or include; the provider parity rows catch that
```

###### Obligations

None.

##### Covers

P3

```text
Serialize every Team metrics answer from the instance backup on the base revision and on the candidate, and compare
```

```text
Identical answers, exit 0
```

```text
Every Team metrics answer a real instance shows is unchanged
```

##### Disposition

Tested

Scripts\/examine\/bin\/metrics\-answers\-compare

```text
A changed answer on a data shape absent from the backup; the comparison stays INDETERMINATE when the backup is absent in the examiner environment
```

###### Obligations


##### Covers

P1

```text
Run the Flow metrics module architecture rules in the Release configuration
```

```text
The module's application layer still has no edge to EF Core or the data layer and no newcomer on the inbound lists, with unchanged baselines
```

```text
Reading through the owner repositories adds no new dependency to the Flow metrics module
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsApplicationOutboundArchUnitTest\.cs

```text
A dependency added through a type the rules do not scan
```

###### Obligations


## Expectations

### Rows

#### E1

##### Promises

P1

```text
A user reading Team metrics gets the same behavior as before, while the Team reads no longer go through the generic predicate lookup; the generic predicate allow-list no longer lists the Team metrics service.
```

#### E2

##### Promises

P2

```text
Each Team read returns the same items, in the same order, with the same related data and the same change-tracking behavior as the predicate read it replaces, on both SQLite and Postgres.
```

#### E3

##### Promises

P3

```text
Every Team metrics answer a user sees is identical to before, and each answer costs the same number of database queries.
```

## Charter

None.

## Examination

None.
