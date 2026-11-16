# Acceptance brief: story\-6360\-s03b\-c\-edge\-and\-rules

## Slices

### S1

##### Covers

P1

```text
The compiled backend is read for every Work Items type that references the Flow metrics Team metrics service; the same rule is run on a made-up Work Items type that updates a Team's metrics inside an async method and on one that only announces the refresh.
```

```text
No Work Items type reaches the Team metrics service, and the Team refresh service is among the types read; on the made-up types the rule fails and names only the one that updates the metrics, with what it reaches.
```

```text
Work Items no longer tells Flow metrics to update a Team's metrics; putting that call back is caught before release.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsDoesNotCommandFlowMetricsArchUnitTest\.cs

```text
Work Items looks the Team metrics service up by name at run time, which the compiled references cannot show; none exists today.
```

###### Obligations




##### Covers

P1

```text
The start-up code's registrations are read and the reactions to a Team refresh are listed in the order the container hands them out, without starting the application.
```

```text
The Flow metrics reaction that updates the Team's metrics comes first, then the memo of reconstructions, the forecast trigger, the blocked-count, percentiles and process-behaviour recorders and the two demo backfills, in that order; today's list without the metrics reaction, or the metrics reaction registered after the blocked-count recorder, fails.
```

```text
After every Team refresh the Team's metrics are brought up to date before any snapshot is recorded, so snapshots never record the previous refresh's metrics as today's.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventHandlerOrderPinTest\.cs

```text
A dispatcher that stops honouring registration order; the dispatcher itself is unchanged and the port scenarios observe the order the reactions actually run in.
```

###### Obligations





##### Covers

P1

P4

```text
A CSV-backed Team with automatic Feature WIP is refreshed on 18 October 2026 09:30, its metrics are read, and a day later it is refreshed again from an upload where its two Work Items in progress under FEAT-1 and FEAT-2 have left, one in progress under FEAT-3 and one done on 15 October are new.
```

```text
The refresh returns normally with a refresh history row marked successful; the Team's metrics are saved first, then the seven other reactions run in their order, and no reaction fails; the Team is stored updated 2026-10-19 09:30 with Feature WIP 1; throughput from 1 to 19 October answers 2 and the Features in progress answer FEAT-3, not the cached 1 and FEAT-1, FEAT-2.
```

```text
After a Team refresh the user sees the same metrics, refresh time and Feature WIP as before the change, computed from the refreshed Work Items.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/TeamMetricsFollowTheTeamRefreshTest\.cs

```text
A metrics update that runs first but saves values equal to the old ones by coincidence; the upload moves every value, so that cannot happen with this data.
```

###### Obligations



##### Covers

P1

```text
The same Team is refreshed on 18 October and its metrics read; a day later it reads the new upload and the next save of its metrics is made to fail, then it is refreshed.
```

```text
The refresh returns normally and its refresh history row is marked successful; exactly one reaction failure is logged and it names TeamDataRefreshedTeamMetricsUpdateHandler; the failed metrics save comes first and all seven other reactions still run in their order; the Team keeps updated 2026-10-18 09:30 and Feature WIP 2; throughput answers 2 and Features in progress FEAT-3, computed from the refreshed Work Items.
```

```text
When saving a Team's metrics fails, Refresh History still shows the refresh as succeeded, the Team shows its previous refresh time and is due again so the next refresh retries, the other reactions to the refresh still happen, and no chart shows the metrics of before.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/TeamMetricsFollowTheTeamRefreshTest\.cs

```text
A failure inside another reaction, or a failure other than the save; only the injected metrics save failure is exercised.
```

###### Obligations




##### Covers

P1

```text
A Team refresh is announced for a Team that was deleted after its Work Items were saved.
```

```text
No Team metrics are saved, no reaction failure is logged, and the seven other reactions run in their order.
```

```text
Deleting a Team while it refreshes leaves no error in the log and no metrics behind.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/TeamMetricsFollowTheTeamRefreshTest\.cs

```text
A metrics reaction that is not registered at all; the order pin and the refresh scenarios catch that.
```

###### Obligations


## Expectations

### Rows

#### E1

##### Promises

P1

```text
After a Team's data refresh, the Team's flow metrics are brought up to date without the Work Items refresh itself driving them; the metrics are updated before the other reactions to that refresh, and if a reaction fails the outcome is the same defined behaviour every time (the refresh result and the remaining reactions behave as documented).
```

#### E2

##### Promises

P2

```text
Work Items' architecture rules are enforced: a change that makes Work Items' core reach beyond the shared kernel and the base library, or that puts anything other than domain-event handlers in its policies, or breaks its allowed application dependencies, is caught, and a baseline that has gone stale in either direction is caught too; Flow metrics' rules no longer mention the Work Items refresh service.
```

#### E3

##### Promises

P3

```text
A maintainer finds every Work Items unit test under the Work Items module folder, grouped by layer, and adding a Work Items unit test elsewhere is flagged; API and Integration tests remain in their existing places.
```

#### E4

##### Promises

P4

```text
For the same data, refreshing a Team gives the same refresh result, the same Team metrics and the same forecasts as before this feature; users see no change in numbers or behaviour.
```

## Charter

None.

## Examination

None.
