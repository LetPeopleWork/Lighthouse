# Acceptance brief: story\-6361\-s03c\-a\-rules\-first

## Slices

### S1

##### Covers

P1

```text
The shared rule for who may reach a module's Application runs over a made-up module that is not Flow metrics, where only the API, the composition root and its nested type, a listed caller and a caller of the Domain reach it
```

```text
The rule finds exactly the listed caller and passes
```

```text
Another module can use the shared Application rule with its own namespaces and compliant code stays green
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRulesCatchANewOffenderArchUnitTest\.cs\:\:AnotherModuleWhoseApplicationIsReachedOnlyByAllowedCallers\_PassesTheSharedApplicationRule

```text
A rule that ignores the outside pattern and reads every namespace, since no other type in the test project reaches the made-up Application
```

###### Obligations

None.

##### Covers

P1

```text
The same made-up module gains an outside caller of its policies that is not on the list
```

```text
The failure names the newcomer as not on the list and says which policy it reaches, and asks to delete nothing
```

```text
A new caller of another module's Application is caught and named
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRulesCatchANewOffenderArchUnitTest\.cs\:\:AnOutsideCallerOfAnotherModulesApplicationNotOnTheList\_FailsTheSharedApplicationRuleAndIsNamed

```text
A rule whose failure names the newcomer but words the reached type differently than the dependent reaches target form
```

###### Obligations

None.

##### Covers

P1

```text
The same made-up module gains an outside caller that reaches its Application only inside an async method
```

```text
The failure names that caller as not on the list and says which Application service it reaches
```

```text
A caller hidden in an async body is caught in another module too
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRulesCatchANewOffenderArchUnitTest\.cs\:\:ACallerReachingAnotherModulesApplicationOnlyInsideAnAsyncMethod\_FailsTheSharedApplicationRuleAndIsNamed

```text
A rule that reads async bodies only for direct method calls and misses one reached through a field of the state machine
```

###### Obligations

None.

##### Covers

P1

```text
The list of the made-up module still names a caller that no longer reaches its Application
```

```text
The failure names that listed caller for deletion and names no newcomer
```

```text
A list of allowed callers in another module can only shrink
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRulesCatchANewOffenderArchUnitTest\.cs\:\:AListedCallerThatNoLongerReachesAnotherModulesApplication\_FailsTheSharedApplicationRuleAndIsNamedForDeletion

```text
A rule that names the stale entry but words the deletion hint differently from the shared list comparison
```

###### Obligations

None.

##### Covers

P1

```text
The shared edge rule runs over a made-up Application of another module that reaches a neighbouring module only through the listed edge
```

```text
The rule finds exactly that edge and passes
```

```text
Another module can use the shared edge rule with its own namespaces and compliant code stays green
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRulesCatchANewOffenderArchUnitTest\.cs\:\:AnotherModulesApplicationReachingItsNeighbourOnlyThroughAListedEdge\_PassesTheSharedEdgeRule

```text
A rule that ignores the Application parameter and reads every made-up Application, if it then only met edges that are listed
```

###### Obligations

None.

##### Covers

P1

```text
A listed type of the made-up Application also reaches a second type of the neighbour
```

```text
The failure names only the new edge to the second type, and asks to delete nothing
```

```text
A new edge into a neighbour module is caught even from a type already on the list
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRulesCatchANewOffenderArchUnitTest\.cs\:\:AnotherModulesListedTypeReachingASecondNeighbourType\_FailsTheSharedEdgeRuleAndNamesTheNewEdge

```text
A rule that names the new edge but would also accept a list keyed by type rather than by edge if every type had one edge
```

###### Obligations

None.

##### Covers

P1

```text
A made-up Application reaches the neighbour only inside an async method and its list is empty
```

```text
The failure names that edge as not on the list
```

```text
An edge hidden in an async body is caught in another module too
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRulesCatchANewOffenderArchUnitTest\.cs\:\:AnotherModulesApplicationReachingItsNeighbourOnlyInsideAnAsyncMethod\_FailsTheSharedEdgeRuleAndIsNamed

```text
A rule that reports the async edge under the compiler-made state machine name only if the reader strips it the same way
```

###### Obligations

None.

##### Covers

P1

```text
The list of the made-up Application still names an edge that is gone
```

```text
The failure names that edge for deletion and names no new edge
```

```text
A list of allowed edges in another module can only shrink
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRulesCatchANewOffenderArchUnitTest\.cs\:\:AListedEdgeOfAnotherModulesApplicationThatIsGone\_FailsTheSharedEdgeRuleAndIsNamedForDeletion

```text
A rule that names the gone edge but words the deletion hint differently from the shared list comparison
```

###### Obligations

None.

##### Covers

P1

```text
The compiled Flow metrics rule tests are read for what they call, and the test project for a second rules class or a copied Application rule
```

```text
Each of the four Flow metrics rule tests calls the shared module rules, the old Flow metrics rules class is gone, and no Flow metrics test keeps its own Application rule or edge verdict
```

```text
Flow metrics' rules catch their made-up offenders through the one shared class the next module uses
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ModuleRulesCatchANewOffenderArchUnitTest\.cs\:\:TheFlowMetricsRuleTests\_CallTheSharedModuleRules\_AndKeepNoRulesClassOrApplicationRuleOfTheirOwn

```text
A Flow metrics test that calls the shared class for one rule but keeps a private copy of a rule under a different name
```

###### Obligations

None.

##### Covers

P1

```text
The Flow metrics rule tests run over the real code and their own made-up offenders after they call the shared rules
```

```text
Every Flow metrics rule test stays green: the real code passes and each made-up newcomer, async-only caller or edge, and stale entry still fails and is named
```

```text
Flow metrics keeps the same rules with the same meaning after the move to the shared class
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\:\:Lighthouse\.Backend\.Tests\.Architecture\.FlowMetrics

```text
A crafter edit that changes a Flow metrics rule's failure wording while keeping the offender name and the list format
```

###### Obligations

None.

### S3

##### Covers

P3

```text
With the random numbers pinned, ask the forecast service how many Work Items fit in thirty days, given a fixed fourteen-day throughput history
```

```text
The 50th, 70th, 85th and 95th percentiles read 53, 49, 44 and 42 out of 20 simulated runs
```

```text
A How Many forecast over the same history gives the same answer before and after the forecasting code moves
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/Forecasting\/SeededForecastsStayTheSameTest\.cs\:\:How\_many\_Work\_Items\_fit\_in\_thirty\_days\_reads\_the\_same\_percentiles\_from\_the\_same\_history

```text
A change that alters only the forecast creation time or a value outside the four percentiles and the run count
```

###### Obligations

None.

##### Covers

P3

```text
With the draws pinned, ask the forecast service when twenty Work Items are done for a stored Team that finished an uneven fortnight of Work Items
```

```text
The 50th, 70th, 85th and 95th percentiles read 13, 16, 19 and 22 days out of 20 simulated runs
```

```text
A When forecast for the same Team and history gives the same answer before and after the forecasting code moves
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/Forecasting\/SeededForecastsStayTheSameTest\.cs\:\:When\_twenty\_Work\_Items\_are\_done\_reads\_the\_same\_percentiles\_from\_the\_same\_history

```text
A change to the dependency or start-date parts of a forecast, which this Team with no dependencies does not reach
```

###### Obligations

None.

##### Covers

P3

```text
Run the forecast-answers-compare command with a base revision and a candidate revision on the seeded demo data
```

```text
The command captures the forecast answers on both revisions with a fixed seed and reports them identical with exit 0, or names each difference and exits non-zero
```

```text
The maintainer and the examiner can see that the forecast answers on demo data did not change across the move
```

##### Disposition

Tested

Scripts\/examine\/bin\/forecast\-answers\-compare

```text
A capture that leaves out one of the forecast endpoints, so a change there is never compared
```

###### Obligations

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
A module other than Flow metrics can use the same shared rules class, and Flow metrics' own architecture rules, run through it, still flag the same made-up offending types as before.
```

#### E2

##### Promises

P2

```text
For each architecture rule that names Forecasting's namespaces, folders or files, a made-up offending type placed under Modules/Forecasting is flagged by that rule; no such rule overlooks that location.
```

#### E3

##### Promises

P3

```text
With a pinned seed, forecasts give the same percentiles as before the move, and a public command captures the forecast answers on demo data before the move and compares them after, reporting match or difference.
```

## Charter

None.

## Examination

None.
