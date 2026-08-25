# ADR-178: Publishing a forecast to a Jira Release is a connector capability of its own, not a second staged type in the write-back collector

- **Status**: **Accepted, point 4 answered in DELIVER** (2026-08-25). The publish runs in a
  `PortfolioForecastsUpdated` handler — see the closing note.
- **Date**: 2026-08-22
- **Feature**: epic-5565-delivery-date-sync (ADO Epic #5565, slices 04-05 / ADO #4463, #5832)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

Slice 04's brief closes with an explicit instruction: *"This is a new write class, not another
`WriteBackFieldUpdate` — that type names an issue and a field. DESIGN decides whether the Version write
joins the existing collector as a second staged type or sits beside it. Do not force it into the
existing shape just because the shape is there."*

The existing seam is real and well-shaped. `IWriteBackTriggerService` resolves intents without I/O,
`IWriteBackCollector` stages them and flushes once per connection per execution (ADR-144), and
`PortfolioUpdater` already stages twice inside one refresh — once for Features, once after the forecast
run. A Version write happens in the same pass, against the same connection, for the same reason. The
pull toward reusing the collector is strong, and it is wrong.

## Decision

**A separate capability interface on the Jira connector, invoked by its own service at the same seam.
The write-back collector is left untouched.**

1. **`IDeliveryForecastPublisher` is a new capability interface**, composed into
   `IJiraWorkTrackingConnector` beside `IDeliverySourceProvider` — the same move ADR-166 made for the
   inbound half, and the same move `IBoardInformationProvider` made before either.

2. **The capability is declared separately from the inbound one.** `SupportsDeliverySources` and
   `SupportsDeliveryForecastPublishing` are two questions, because D2 makes capability a property of the
   connection and slice 05's whole premise is that a connection may read Releases while being refused
   the write. One flag covering both would make "reads but cannot write" unrepresentable, which is the
   exact state the Epic exists to report.

3. **`IDeliveryForecastPublisher.PublishAsync(connection, DeliveryForecastPublication)` returns a closed
   result type** — `Published`, `Refused(string reason)`, `TargetMissing` — mirroring ADR-170's
   four-arm resolution rather than throwing for expected outcomes.

4. **The publish runs after the forecast completes.** The exact seam is **reopened** — see the
   invalidation note below. It does not stage into the collector either way.

## Rejected alternatives

**A second staged type in `WriteBackCollector`.** The collector's contract is issue-shaped in three
independent places, and a Version write would have to lie in all three:

- `WriteBackFieldUpdate` requires `WorkItemId` and `TargetFieldReference`. A Version has no work item,
  and `description` is not a field reference in the `customfield_10042` sense the type means. Passing a
  version id as a work item id makes every existing consumer's assumption silently false.
- The collector **deduplicates on `(connection, work item, target field)`**. For Version writes that key
  is meaningless — and worse, two Deliveries bound to two Releases would collide or not depending on how
  the fake work item id was synthesised.
- `WriteBackItemResult` carries `NotificationSuppression`, which exists because issue edits mail
  watchers. Version writes have no such parameter (see ADR-179's suppression note), so every result
  would carry a field that can only ever say `NotApplicable`.

`IWriteBackService.WriteFieldsToWorkItems` is named for what it does. Widening it to mean "…and also
sometimes a project version" would make the name a lie in a seam that three connectors implement.

**One combined capability flag.** Rejected as above: it makes the read-yes-write-no state
unrepresentable, and that state is measured and common (slice 00 Q3).

**A domain-event handler on `PortfolioForecastsUpdated`.** ~~The publish must run after the forecast and
before the refresh log closes, and ADR-168 already rejected an event for the inbound sibling on the
grounds that an event moves the ordering contract into `Program.cs`.~~ **This rejection did not survive.**
It rested on there being no event at the right point; the invalidation note below records that there now
is one, and slice 04 chose it. Read the closing note before acting on this paragraph.

## Consequences

- One new interface, one new service, and **one new handler plus two `Program.cs` registrations** -
  `PortfolioUpdater` is not touched at all, because the forecast no longer runs there. The write-back
  seam, its ArchUnit test (`QuietWriteBackSeamArchUnitTest`) and its three connectors are untouched.
- The two capabilities can disagree per connection, which is what slice 05 reports on.
- Registering the publisher in `Program.cs` pulls in the full backend Integration suite, the cost
  already recorded for the inbound provider. It is the same edit, not a second one.

## Invalidation — the forecast seam moved under this ADR (2026-08-22, after rebase)

Written when `PortfolioUpdater` ran the forecast inline and staged forecast write-back beside it. It no
longer does. Epic #5792 decoupled the forecast: `PortfolioUpdater` now ends with
`forecastUpdater.TriggerUpdate(project.Id)` and a separate `ForecastUpdater` runs the simulation and
publishes `PortfolioForecastsUpdated`. The reason recorded upstream is that forecasting inline ran the
unseeded simulation twice per portfolio in a bulk refresh, so an operator watched the delivery date
settle and then move.

**What survives**: points 1-3 and the whole rejected-alternatives section. Publishing is still its own
capability, still two flags, still not a `WriteBackFieldUpdate` — none of that reasoning touches where
the call sits.

**What is reopened**: point 4. There is no forecast run in `PortfolioUpdater` to sit after any more, so
the publish belongs at the `ForecastUpdater` seam. That also weakens this ADR's rejection of a
domain-event handler, which was argued on the grounds that no event sits at the right point;
`PortfolioForecastsUpdated` now does, and `DeliveryMetricSnapshotRecordingHandler` already consumes it.

**Not decided here.** Choosing between a direct call in `ForecastUpdater` and a handler on
`PortfolioForecastsUpdated` is slice 04's to make, with the ordering question it drags along: the
inbound re-sync sets the target date, and the forecast now runs in a different execution, so
whether a published forecast is measured against a freshly synced date is no longer obvious. That
ordering claim is the one this Epic must not get wrong.

## Point 4 answered (2026-08-25, DELIVER slice 04)

**The publish is a `DeliveryForecastPublishingHandler` on `PortfolioForecastsUpdated`**, registered beside
`DeliveryMetricSnapshotRecordingHandler`.

The invalidation note above reopened the choice between a direct call in `ForecastUpdater` and a handler.
The handler wins on the reason the note itself gives: there is no forecast run left in `PortfolioUpdater`
to sit after, the event now exists at exactly the right point, and a sibling already consumes it. It also
buys the posture the feature needs - publishing is the last thing a round does and the least important, so
a Jira that will not take today's numbers must not cost the refresh that produced them. A handler is
best-effort by construction; a call line inside the forecast is not.

**The ordering claim this ADR said the Epic must not get wrong holds.** The fetch pass re-syncs every bound
Delivery from its source *before* it asks for the forecast, so the target date a published likelihood is
measured against is the freshest there is.

**One trap, recorded because it cost a debugging pass.** With two handlers registered for one event,
`GetRequiredService<IDomainEventHandler<PortfolioForecastsUpdated>>` returns whichever was registered
*last*. Three fixtures resolved the handler that way, silently began exercising the new one, and reported
that no snapshot had been recorded - a failure naming the recorder and pointing nowhere near the cause.
Resolve by type (`GetServices<...>().OfType<...>().Single()`) when more than one handler can listen.

**A second capability flag is real but unrealised in production** (point 2). `SupportsDeliveryForecastPublishing`
returns `true` for every Jira connection, because the permission it would report is per project and no
site-wide answer could be true of everything a connection touches. The "reads Releases but is refused the
write" state the flag was justified by is therefore carried by `Delivery.LastPublishRefusalReason`, not by
the flag. The flag stays - it is how a *future* connector says it cannot write at all - but the
justification in point 2 should be read as forward-looking rather than as describing today.
