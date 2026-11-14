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

### S2

##### Covers

P2

```text
The module map's own Forecasting pattern, read from the module map, runs over a made-up outsider reaching a made-up Forecasting type below Modules.Forecasting, and over its twin reaching a namesake in Modules.ForecastingFixtures
```

```text
The outsider reaching the module is named as reaching Forecasting; the twin reaching the namesake is not
```

```text
The module map and the Flow metrics rule that reads its pattern keep treating Forecasting as Forecasting after it moves to Modules/Forecasting
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingRulesSeeTheModuleArchUnitTest\.cs\:\:ATypeReachingForecastingUnderTheModule\_IsCaughtAsReachingForecastingByTheModuleMap

```text
A pattern that matches the new place but is used by the module map only in some of its rules; the module map's own rules on the real code are what show each rule uses the pattern
```

###### Obligations

None.

##### Covers

P2

```text
The simulated-run isolation rule's own forecast-components key runs over a made-up step below Modules.Forecasting and an outsider in Modules.ForecastingFixtures, both holding one run's working state
```

```text
The outsider is named; the made-up step in the module is treated as inside the forecast and not named
```

```text
Moving the simulation into Modules/Forecasting neither turns the isolation rule red nor lets an outsider hold a run's working state unseen
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingRulesSeeTheModuleArchUnitTest\.cs\:\:ARunsWorkingStateHeldUnderTheModule\_IsInsideTheForecast\_WhileAnOutsiderHoldingItIsNamed

```text
A key widened correctly while the rule still matches it as an exact namespace; the rule on the real code then flags the simulation itself and turns red
```

###### Obligations

None.

##### Covers

P2

```text
The reality check's own key for the services its verdict rules may not reach runs over a made-up verdict rule reaching a made-up service below Modules.Forecasting.Application, and a twin reading only plain values
```

```text
The verdict rule reaching the made-up forecast service is named; the twin is not
```

```text
The reality check's verdict stays decided from values alone after the forecast services move to Modules/Forecasting
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingRulesSeeTheModuleArchUnitTest\.cs\:\:AServiceUnderTheModule\_StaysOutOfReachOfTheRealityCheckVerdictRules

```text
Exempting the two policies by their old full names instead of by type; that is not observable before they move, and the first reality check rule fails loudly when they leave their old names
```

###### Obligations

None.

##### Covers

P2

```text
The dependency-aware forecast rule's own update-components key runs over a made-up background job below Modules.Forecasting.Adapters.Background reaching the forecast service, a job there that does not, and an outsider that asks for a forecast
```

```text
The background job reaching the forecast service is named; the other job and the outsider are not
```

```text
After the forecast updater moves, no second background component in its new place can start a forecast behind the admission check
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingRulesSeeTheModuleArchUnitTest\.cs\:\:ABackgroundComponentUnderTheModuleReachingForTheForecastService\_IsNamed

```text
Exempting the forecast updater by a name fragment instead of by type; no made-up type can be placed to tell the two apart before the move
```

###### Obligations

None.

##### Covers

P2

```text
The fast-with-many-Features rules' own keys for the forecast service file and the forecast updater file are read and compared with the production source file names
```

```text
Each key is a bare file name, and exactly one production source file carries that name
```

```text
The round rules keep reading the forecast service and the updater wherever their files move, as they already do for the forecast threads' file
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingRulesSeeTheModuleArchUnitTest\.cs\:\:TheForecastRoundFiles\_AreFoundByTheirNameWhereverTheyLive

```text
Keys reduced to file names while the rules still join them to the old folder; the rules on the real code then fail to open the file and turn red
```

###### Obligations

None.

##### Covers

P2

```text
Refinement's own key for the background updates it may not reach runs over a made-up Refinement read starting a made-up forecast round below Modules.Forecasting.Adapters.Background, and a twin asking a made-up forecast service below Modules.Forecasting.Application
```

```text
The read starting a round is named; the read asking for a forecast is not
```

```text
Refinement still cannot start a background forecast round after the updater leaves BackgroundServices, and may still read forecast answers
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingRulesSeeTheModuleArchUnitTest\.cs\:\:ABackgroundUpdateUnderTheModule\_StaysOutOfReachOfRefinement\_WhileTheForecastAnswersStayOpen

```text
A widened key the Refinement rule declares but does not pass to its forbidden set; the rule on the real code cannot show that, because no Refinement type reaches an updater
```

###### Obligations

None.

##### Covers

P2

```text
The staleness rule's own services key runs over a made-up staleness rule reaching a made-up Forecasting calculation below Modules.Forecasting.Domain, and a twin reading only plain values
```

```text
The rule reaching the made-up Forecasting calculation is named; the twin is not
```

```text
The staleness rule stays free of Forecasting after Forecasting leaves Services
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingRulesSeeTheModuleArchUnitTest\.cs\:\:AForecastingTypeUnderTheModule\_StaysOutOfReachOfTheStalenessRule

```text
A key widened to Services and Modules while the rule keeps an inline Services-only pattern beside it; no real staleness type reaches a service to show it
```

###### Obligations

None.

##### Covers

P2

```text
The recurring blackout rule and the rules keyed on Forecasting types by type or short name (update holds, forecast filter seam, domain-event handler order)
```

```text
No change: no Forecasting type sits in the recurring blackout rule's subject or target set, and a type key sees the type wherever it lives
```

```text
These rules keep guarding what they guard today when Forecasting moves
```

##### Disposition

NotTested

```text
No Forecasting namespace, folder or file key to widen: the recurring blackout rule's subject is a Models entity and API metrics controllers and its targets are the exact Services.Implementation namespace, the blackout service port and repository contracts, none of which holds a Forecasting type; the other three key on the types themselves, which no made-up type can stand in for
```

none declared

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
