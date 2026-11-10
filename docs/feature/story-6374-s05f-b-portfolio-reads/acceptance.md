# Acceptance brief: story\-6374\-s05f\-b\-portfolio\-reads

## Slices

### S1

##### Covers

P1

```text
Scan every production source file for calls of the general predicate read and compare the callers with the allow-list of 13 files, which no longer names the Portfolio metrics service
```

```text
Fails while the Portfolio metrics service still calls the general predicate read, naming that file as a caller not on the list; passes once it reads only through the named reads and the scanned callers equal the 13 listed files
```

```text
The Portfolio metrics cannot go back to the general predicate read without a test naming the file
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs\:\:GetAllByPredicate\_IsCalledOnlyFromTheFilesOnTheList

```text
A named read that internally builds a different predicate, or a call made through reflection or a delegate; the provider parity rows catch the first
```

###### Obligations




##### Covers

P2

```text
On Sqlite, seed two Portfolios with Features in every state category including Unknown, one Feature in both Portfolios, one in none, and transitions for several Features and for one not asked about; read each Portfolio through every named read and through the predicate it replaces, each in a fresh context, and read the transitions of the chosen Features both ways
```

```text
Each named read gives the same Features or transitions in the same sequence, with the same Portfolios, Feature work with Team, forecasts and start forecasts with simulation results and dependency references loaded, all tracked, as its predicate; the doing-or-done read matches both spellings of its predicate; no predicate comes back empty
```

```text
The Portfolio metrics read the same data from the same database once they switch to the named reads
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/PortfolioMetricsReadsParityOnBothProvidersTests\.cs\:\:Every\_named\_Portfolio\_read\_gives\_what\_its\_predicate\_gives\_on\_Sqlite

```text
A named read that differs only on a data shape the fixture does not hold, such as ordering ties or transition row order on much larger data
```

###### Obligations





##### Covers

P2

```text
On Postgres, seed two Portfolios with Features in every state category including Unknown, one Feature in both Portfolios, one in none, and transitions for several Features and for one not asked about; read each Portfolio through every named read and through the predicate it replaces, each in a fresh context, and read the transitions of the chosen Features both ways
```

```text
Each named read gives the same Features or transitions in the same sequence, with the same Portfolios, Feature work with Team, forecasts and start forecasts with simulation results and dependency references loaded, all tracked, as its predicate; the doing-or-done read matches both spellings of its predicate; no predicate comes back empty
```

```text
The Portfolio metrics read the same data from the same database once they switch to the named reads
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/PortfolioMetricsReadsParityOnBothProvidersTests\.cs\:\:Every\_named\_Portfolio\_read\_gives\_what\_its\_predicate\_gives\_on\_Postgres

```text
A named read that differs only on a data shape the fixture does not hold, such as ordering ties or transition row order on much larger data
```

###### Obligations





##### Covers

P3

```text
Run the existing Portfolio metrics tests with only their read stubs retargeted to the named reads and an explicit empty answer for the transition read
```

```text
Every existing Assert, Is and Verify line passes unchanged; a read nobody set up answers null and makes the test fail rather than pass
```

```text
Every Portfolio metrics answer the existing tests pin stays the same
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/PortfolioMetricsServiceTests\.cs

```text
A named read that the double answers regardless of its own predicate or includes; the provider parity rows catch that
```

###### Obligations

None.

##### Covers

P3

```text
Serialize every Portfolio metrics answer from the instance backup on the base revision and on the candidate, and compare
```

```text
Identical answers, exit 0
```

```text
Every Portfolio metrics answer a real instance shows is unchanged
```

##### Disposition

Tested

Scripts\/examine\/bin\/metrics\-answers\-compare

```text
A changed answer on a data shape absent from the backup; the comparison stays INDETERMINATE when the backup is absent in the examiner environment
```

###### Obligations


##### Covers

P3

```text
Count the database statements per Portfolio metrics answer before and after the switch to the named reads
```

```text
The same number of statements per answer
```

```text
The Portfolio metrics do not get slower through extra database round trips
```

##### Disposition

NotTested

```text
No Portfolio statement-count test exists and the design declares none: today's Feature read already runs at once and every transition query is enumerated once by its caller, so reading inside the repository adds no statement; the reviewer checks the call sites and the answer comparison shows no changed answer
```

reviewer

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
A maintainer reading the Portfolio metrics code and the GetAllByPredicate allow-list finds no remaining use of GetAllByPredicate by the Portfolio metrics service, and no allow-list entry naming it.
```

#### E2

##### Promises

P2

```text
For each named Portfolio read, a user comparing it with the predicate it replaced sees the same features or transitions, in the same order, with the same related data loaded and tracked, on both SQLite and Postgres.
```

#### E3

##### Promises

P3

```text
A user viewing Portfolio metrics sees exactly the same answers as before, and each answer costs the same number of database queries.
```

## Charter

None.

## Examination

None.
