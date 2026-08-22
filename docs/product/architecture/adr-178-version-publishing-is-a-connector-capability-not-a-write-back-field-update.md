# ADR-178: Publishing a forecast to a Jira Release is a connector capability of its own, not a second staged type in the write-back collector

- **Status**: **Proposed** (DESIGN, 2026-08-22)
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

4. **`PortfolioUpdater` calls a `IDeliveryForecastPublishingService` after the forecast run**, beside
   the existing `writeBackCollector.Stage(...)` for forecasts. It does not stage into the collector.

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

**A domain-event handler on `PortfolioForecastsUpdated`.** The publish must run after the forecast and
before the refresh log closes, and ADR-168 already rejected an event for the inbound sibling on the
grounds that an event moves the ordering contract into `Program.cs`. The same reasoning holds; it is not
re-litigated here.

## Consequences

- One new interface, one new service, one new call line in `PortfolioUpdater`. The write-back seam,
  its ArchUnit test (`QuietWriteBackSeamArchUnitTest`) and its three connectors are untouched.
- The two capabilities can disagree per connection, which is what slice 05 reports on.
- Registering the publisher in `Program.cs` pulls in the full backend Integration suite, the cost
  already recorded for the inbound provider. It is the same edit, not a second one.
