using Lighthouse.Backend.Extensions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Dependencies;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;

namespace Lighthouse.Backend.Services.Implementation.WorkItems
{
#pragma warning disable S107
    public class WorkItemService(
        ILogger<WorkItemService> logger,
        IWorkTrackingConnectorFactory workTrackingConnectorFactory,
        IRepository<Feature> featureRepository,
        IWorkItemRepository workItemRepository,
        IPortfolioMetricsService portfolioMetricsService,
        IRepository<Team> teamRepository,
        IPortfolioRepository portfolioRepository,
        IWorkItemStateTransitionRepository stateTransitionRepository,
        IFeatureStateTransitionRepository featureStateTransitionRepository,
        IDomainEventDispatcher domainEventDispatcher,
        IBlockedItemService blockedItemService,
        IFeatureBlockedTransitionRepository featureBlockedTransitionRepository,
        IFeatureOrdering featureOrdering,
        IRepository<OptionalFeature> optionalFeatureRepository,
        IDependencyReconciler dependencyReconciler,
        IDependencyRefreshReporter dependencyRefreshReporter)
        : IWorkItemService
#pragma warning restore S107
    {
        private readonly Dictionary<int, int> defaultWorkItemsBasedOnPercentile = new();

        public async Task<SyncOutcome> UpdateFeaturesForPortfolio(Portfolio portfolio)
        {
            logger.LogInformation("Updating Features for Portfolio {PortfolioName}", portfolio.Name);

            var fetchShape = FetchShape.Of(portfolio);

            var outcome = (await RefreshFeatures(portfolio, fetchShape.Changed)) with { Reason = fetchShape.Reason };
            await RefreshParentFeatures(portfolio, fetchShape.Changed);

            await UpdateRemainingWorkForPortfolio(portfolio);

            dependencyRefreshReporter.ReportOn(portfolio);

            portfolio.RefreshUpdateTime();

            // Recorded beside UpdateTime, and only once the fetch it describes actually completed.
            portfolio.FetchFingerprint = fetchShape.Fingerprint;

            // Stryker disable once all: what an update completed is now said by the one "Update completed" summary line (Epic #5687), whose text UpdateServiceBase pins; this is Debug trace.
            logger.LogDebug("Done Updating Features for Portfolio {PortfolioName}", portfolio.Name);

            return outcome;
        }

        public async Task<SyncOutcome> UpdateWorkItemsForTeam(Team team)
        {
            logger.LogInformation("Updating Work Items for Team {TeamName}", team.Name);

            var fetchShape = FetchShape.Of(team);

            var outcome = (await RefreshWorkItems(team, fetchShape.Changed)) with { Reason = fetchShape.Reason };

            // What is left under a feature is the sum of its teams' open work, so a team that just
            // refreshed changes that sum for every portfolio it delivers into. A team reaches a portfolio
            // by working on one of its features and by nothing else.
            foreach (var portfolioId in portfolioRepository.GetPortfolioIdsForTeam(team.Id))
            {
                var portfolio = portfolioRepository.GetById(portfolioId);

                if (portfolio != null)
                {
                    await UpdateRemainingWorkForPortfolio(portfolio);
                }
            }

            // The team half has no RefreshUpdateTime on this path, so this write has to save itself.
            team.FetchFingerprint = fetchShape.Fingerprint;
            await teamRepository.Save();

            // Stryker disable once all: the team half of the same trace — the summary line, not this one, is what an operator reads.
            logger.LogDebug("Done Updating Work Items for Team {TeamName}", team.Name);

            return outcome;
        }

        /// <summary>
        /// What this cycle would ask the tracker for, whether that differs from what the last completed
        /// cycle asked, and whether anybody caused the difference. The three travel together because the
        /// fingerprint that gets compared has to be the one that gets stored afterwards.
        ///
        /// The reason is not the same question as the mode. An instance that never recorded a fingerprint
        /// and one whose fingerprint moved both take the expensive path, but only the second was caused by
        /// anybody - blaming configuration for the first would be a lie.
        /// </summary>
        private sealed record FetchShape(string Fingerprint, bool Changed, string? Reason)
        {
            /// <summary>
            /// Asked of the base a team and a portfolio share, so there is one comparison rather than two.
            /// Nothing recorded reads as changed, which is what gives an instance upgrading into this
            /// release one full cycle.
            /// </summary>
            public static FetchShape Of(WorkTrackingSystemOptionsOwner queryOwner)
            {
                var fingerprint = FetchFingerprint.For(queryOwner);
                var changed = queryOwner.FetchFingerprint != fingerprint;
                var reason = changed && queryOwner.FetchFingerprint != null ? SyncOutcome.ConfigurationChanged : null;

                return new FetchShape(fingerprint, changed, reason);
            }
        }

        private async Task<SyncOutcome> RefreshWorkItems(Team team, bool fetchShapeChanged)
        {
            // Stryker disable once all: one of three copies of an announcement the operator only ever sees once; the scenario counts it by level, not by wording.
            logger.LogDebug("Updating Work Items for Team {TeamName}", team.Name);

            var syncTime = DateTime.UtcNow;
            var connector = workTrackingConnectorFactory.GetWorkTrackingConnector(team.WorkTrackingSystemConnection.WorkTrackingSystem);

            var storedWorkItems = workItemRepository.GetAllByPredicate(wi => wi.TeamId == team.Id).ToList();

            var fetch = await ResolveRemoteFetch(connector, team, storedWorkItems, fetchShapeChanged);

            var itemsWithTransitions = SyncDownloadedItems(connector, team, storedWorkItems, fetch.WorkItems, syncTime);
            var itemsRemovedThisCycle = RemoveItemsThatLeftTheQuery(storedWorkItems, fetch.StillOnTheTracker);

            await workItemRepository.Save();

            var events = new List<IDomainEvent>();
            foreach (var syncedItem in itemsWithTransitions)
            {
                var newTransitions = SyncStateTransitions(syncedItem.PersistedItem, syncedItem.SyncedTransitions);
                events.AddRange(CollectDomainEvents(team, syncedItem, newTransitions));
            }

            events.AddRange(CollectStalenessEvents(team, EverythingTheTeamStillHas(storedWorkItems, itemsWithTransitions, itemsRemovedThisCycle), syncTime));

            await stateTransitionRepository.Save();
            await workItemRepository.Save();

            await PublishDomainEvents(events);

            return fetch.Outcome;
        }

        /// <summary>
        /// How much this cycle downloads, and the download itself. The opt-in gates the scan as well as the
        /// mode decision, so an instance that never volunteered for the cheaper refresh never reaches the
        /// tracker for one - a connector that could be swept but was not volunteered is precisely the
        /// data-loss exposure the opt-in exists to confine.
        /// </summary>
        private async Task<RemoteFetch> ResolveRemoteFetch(IWorkTrackingConnector connector, Team team, List<WorkItem> storedWorkItems, bool fetchShapeChanged)
        {
            var operatorAskedForTheCheaperRefresh = TheOperatorAskedForTheCheaperRefresh();

            var scan = operatorAskedForTheCheaperRefresh
                ? await ScanRemoteIdentities(connector, team)
                : new IdentityScan(TrackerCanBeScanned: false, Succeeded: false, Stamps: []);

            var mode = SyncModeResolver.Resolve(
                operatorAskedForTheCheaperRefresh,
                scan.TrackerCanBeScanned,
                storedWorkItems,
                scan.Succeeded,
                fetchShapeChanged);

            return mode == SyncMode.Delta
                ? await FetchOnlyWhatMoved(connector, team, storedWorkItems, scan.Stamps)
                : await FetchEverything(connector, team);
        }

        /// <summary>
        /// Downloaded payloads only. SyncWorkItem, WithSyncDeltaTransition and SyncStateTransitions all
        /// write, so an item whose remote stamp did not move must never reach this loop.
        /// </summary>
        private List<SyncedItem> SyncDownloadedItems(IWorkTrackingConnector connector, Team team, List<WorkItem> storedWorkItems, List<WorkItem> downloaded, DateTime syncTime)
        {
            var itemsWithTransitions = new List<SyncedItem>();

            foreach (var item in downloaded)
            {
                var existingItem = storedWorkItems.SingleOrDefault(wi => wi.ReferenceId == item.ReferenceId);
                var priorState = existingItem?.State;
                var wasBlocked = WasBlocked(team, existingItem);
                var persistedItem = SyncWorkItem(item, existingItem);

                var syncedTransitions = WithSyncDeltaTransition(connector, team.WorkTrackingSystemConnection, persistedItem, item.SyncedTransitions, priorState, syncTime);
                itemsWithTransitions.Add(new SyncedItem(persistedItem, syncedTransitions, wasBlocked));
            }

            return itemsWithTransitions;
        }

        /// <summary>Removal is a set difference against the whole query, never against what was downloaded.</summary>
        private List<WorkItem> RemoveItemsThatLeftTheQuery(List<WorkItem> storedWorkItems, HashSet<string> stillOnTheTracker)
        {
            var itemsRemovedThisCycle = storedWorkItems.FindAll(stored => !stillOnTheTracker.Contains(stored.ReferenceId));

            foreach (var itemToRemove in itemsRemovedThisCycle)
            {
                workItemRepository.Remove(itemToRemove.Id);
                logger.LogDebug("Removed Work Item {WorkItemId}", itemToRemove.ReferenceId);
            }

            return itemsRemovedThisCycle;
        }

        /// <summary>
        /// What one refresh has to work with: the payloads it downloaded, every reference id the query
        /// still returns - removal is a set difference against the whole query, never against what was
        /// downloaded - and what to report to the operator.
        /// </summary>
        private sealed record RemoteFetch(List<WorkItem> WorkItems, HashSet<string> StillOnTheTracker, SyncOutcome Outcome);

        private sealed record IdentityScan(bool TrackerCanBeScanned, bool Succeeded, List<RemoteRecordStamp> Stamps);

        /// <summary>
        /// Read per update, inside that update's own scope - never cached in a field or at
        /// startup, so turning the option on takes effect on the next cycle without a restart. No row means
        /// nobody volunteered, which is the same answer as off.
        /// </summary>
        private bool TheOperatorAskedForTheCheaperRefresh()
            => optionalFeatureRepository
                .GetByPredicate(feature => feature.Key == OptionalFeatureKeys.DeltaSyncKey)?.Enabled == true;

        /// <summary>The scan: the same query, asking only for identity plus the remote change stamp.</summary>
        private async Task<IdentityScan> ScanRemoteIdentities(IWorkTrackingConnector connector, Team team)
        {
            if (!connector.SupportsIncrementalSync(team.WorkTrackingSystemConnection))
            {
                return new IdentityScan(TrackerCanBeScanned: false, Succeeded: false, Stamps: []);
            }

            try
            {
                return new IdentityScan(TrackerCanBeScanned: true, Succeeded: true, Stamps: [.. await connector.SweepWorkItemsForTeam(team)]);
            }
            // A half-scanned query is the one answer never allowed, so any scan failure falls back to the
            // whole query - loudly, or nobody learns the cheap path stopped working.
#pragma warning disable CA1031
            catch (Exception exception)
#pragma warning restore CA1031
            {
                // The sink renders the exception as ':: <Type>: <Message>', so interpolating exception.Message
                // into the template as well would print the failure text twice on the operator's one line.
                logger.LogWarning(
                    exception,
                    "Identity scan failed for Team {TeamName} - downloading the whole query instead.",
                    team.Name);

                return new IdentityScan(TrackerCanBeScanned: true, Succeeded: false, Stamps: []);
            }
        }

        private async Task<RemoteFetch> FetchEverything(IWorkTrackingConnector connector, Team team)
        {
            var recordsFromTracker = (await connector.GetWorkItemsForTeam(team)).ToList();
            var actualWorkItems = DeduplicateByReferenceId(recordsFromTracker, team.Name, workItem => workItem.ReferenceId);

            return new RemoteFetch(
                actualWorkItems,
                [.. actualWorkItems.Select(workItem => workItem.ReferenceId)],
                SyncOutcome.FullSync(recordsFromTracker.Count));
        }

        /// <summary>The download: full payloads for the records whose stamp moved, and for nothing else.</summary>
        private async Task<RemoteFetch> FetchOnlyWhatMoved(IWorkTrackingConnector connector, Team team, List<WorkItem> storedWorkItems, List<RemoteRecordStamp> sweptRecords)
        {
            var sweptOnce = DeduplicateByReferenceId(sweptRecords, team.Name, record => record.ReferenceId);
            var movedReferenceIds = sweptOnce
                .FindAll(record => HasMoved(record, storedWorkItems))
                .ConvertAll(record => record.ReferenceId);

            List<WorkItem> downloaded = movedReferenceIds.Count == 0
                ? []
                : DeduplicateByReferenceId((await connector.GetWorkItemsForTeam(team, movedReferenceIds)).ToList(), team.Name, workItem => workItem.ReferenceId);

            return new RemoteFetch(
                downloaded,
                [.. sweptOnce.Select(record => record.ReferenceId)],
                SyncOutcome.DeltaSync(sweptRecords.Count, downloaded.Count));
        }

        // Compared per item against the stored stamp. No global watermark, so clock skew and
        // server-time drift stay out of the design. A record nobody stored yet has always moved.
        private static bool HasMoved<TStored>(RemoteRecordStamp record, List<TStored> storedRecords) where TStored : WorkItemBase
        {
            var stored = storedRecords.Find(candidate => candidate.ReferenceId == record.ReferenceId);

            return stored?.LastChangedRemote != record.ChangedAt;
        }

        // Jira DC offset pagination over an unordered JQL can return the same ReferenceId twice in one fetch;
        // persisting both breaks the SingleOrDefault above on every later sync (docs/ci-learnings.md 2026-05-25).
        // The identity sweep enumerates the very same query, so it collapses under the very same rule, and
        // so does the portfolio's Feature query - one collapse rule, one warning, named after whoever synced.
        private List<T> DeduplicateByReferenceId<T>(List<T> fetchedRecords, string syncedFor, Func<T, string> referenceIdOf)
        {
            var groupedByReferenceId = fetchedRecords.GroupBy(referenceIdOf).ToList();
            var duplicatedGroups = groupedByReferenceId.Where(group => group.Count() > 1).ToList();

            if (duplicatedGroups.Count > 0)
            {
                logger.LogWarning(
                    "Work Tracking System returned {DroppedCopies} duplicate copies for {SyncedFor} - keeping the first copy of each. Affected Reference Ids: {ReferenceIds}",
                    duplicatedGroups.Sum(group => group.Count() - 1),
                    syncedFor,
                    string.Join(",", duplicatedGroups.Select(group => group.Key)));
            }

            return groupedByReferenceId.Select(group => group.First()).ToList();
        }

        private bool WasBlocked(Team team, WorkItem? existingItem)
        {
            if (existingItem == null)
            {
                return false;
            }

            return blockedItemService.IsBlocked(existingItem, team);
        }

        private List<IDomainEvent> CollectDomainEvents(Team team, SyncedItem syncedItem, IReadOnlyList<WorkItemStateTransition> newTransitions)
        {
            var workItem = syncedItem.PersistedItem;

            var events = new List<IDomainEvent>();
            events.AddRange(newTransitions.Select(transition => new WorkItemTransitioned(workItem.Id, transition.FromState, transition.ToState)));

            if (!syncedItem.WasBlockedBeforeSync && blockedItemService.IsBlocked(workItem, team))
            {
                events.Add(new WorkItemBlocked(workItem.Id, ResolveBlockReason(workItem)));
            }

            if (syncedItem.WasBlockedBeforeSync && !blockedItemService.IsBlocked(workItem, team))
            {
                events.Add(new WorkItemUnblocked(workItem.Id));
            }

            return events;
        }

        /// <summary>
        /// Staleness is time-driven, so it is derived for everything the team still has rather than for
        /// what this cycle happened to download. An item that stopped changing is
        /// exactly the item a delta cycle stops fetching, and exactly the item that goes stale.
        /// </summary>
        private static List<IDomainEvent> CollectStalenessEvents(Team team, List<WorkItem> workItems, DateTime syncTime)
        {
            var events = new List<IDomainEvent>();

            foreach (var workItem in workItems)
            {
                AddStalenessEventIfThresholdCrossed(team, workItem, syncTime, events);
            }

            return events;
        }

        /// <summary>
        /// Everything stored for the team as this cycle leaves it: what was already there, minus what the
        /// tracker no longer returns - raising staleness for a just-deleted item would name a dead id -
        /// plus whatever this cycle added.
        /// </summary>
        private static List<WorkItem> EverythingTheTeamStillHas(List<WorkItem> storedWorkItems, List<SyncedItem> syncedItems, List<WorkItem> itemsRemovedThisCycle)
        {
            var survivors = storedWorkItems.FindAll(stored => !itemsRemovedThisCycle.Contains(stored));
            survivors.AddRange(syncedItems
                .ConvertAll(syncedItem => syncedItem.PersistedItem)
                .FindAll(persistedItem => !survivors.Contains(persistedItem)));

            return survivors;
        }

        private static void AddStalenessEventIfThresholdCrossed(Team team, WorkItem workItem, DateTime syncTime, List<IDomainEvent> events)
        {
            var isStaleNow = IsStale(team, workItem, syncTime);
            if (isStaleNow && !workItem.WasStaleAtLastSync)
            {
                workItem.WasStaleAtLastSync = true;
                events.Add(new WorkItemBecameStale(workItem.Id, team.StalenessThresholdDays));
                return;
            }

            if (!isStaleNow)
            {
                workItem.WasStaleAtLastSync = false;
            }
        }

        private static string ResolveBlockReason(WorkItem workItem)
        {
            // The blocked DECISION is owned by IBlockedItemService, which evaluates the rule set; this only
            // supplies human-readable reason text for the WorkItemBlocked event, so the item's current
            // state is the simplest faithful description of "why" without re-deriving the rule match.
            return workItem.State;
        }

        private static bool IsStale(Team team, WorkItem workItem, DateTime syncTime)
        {
            if (workItem.StateCategory != StateCategories.Doing || !workItem.CurrentStateEnteredAt.HasValue)
            {
                return false;
            }

            return (syncTime - workItem.CurrentStateEnteredAt.Value).TotalDays > team.StalenessThresholdDays;
        }

        private async Task PublishDomainEvents(IReadOnlyList<IDomainEvent> events)
        {
            foreach (var domainEvent in events)
            {
                await PublishDomainEvent(domainEvent);
            }
        }

        private async Task PublishDomainEvent(IDomainEvent domainEvent)
        {
            switch (domainEvent)
            {
                case WorkItemTransitioned transitioned:
                    await domainEventDispatcher.PublishAsync(transitioned);
                    break;
                case WorkItemBlocked blocked:
                    await domainEventDispatcher.PublishAsync(blocked);
                    break;
                case WorkItemUnblocked unblocked:
                    await domainEventDispatcher.PublishAsync(unblocked);
                    break;
                case WorkItemBecameStale becameStale:
                    await domainEventDispatcher.PublishAsync(becameStale);
                    break;
                case FeatureBlocked featureBlocked:
                    await domainEventDispatcher.PublishAsync(featureBlocked);
                    break;
                case FeatureUnblocked featureUnblocked:
                    await domainEventDispatcher.PublishAsync(featureUnblocked);
                    break;
            }
        }

        private sealed record SyncedItem(WorkItem PersistedItem, IReadOnlyList<WorkItemStateTransition> SyncedTransitions, bool WasBlockedBeforeSync);

        private static IReadOnlyList<WorkItemStateTransition> WithSyncDeltaTransition(
            IWorkTrackingConnector connector,
            WorkTrackingSystemConnection connection,
            WorkItem persistedItem,
            IReadOnlyList<WorkItemStateTransition> syncedTransitions,
            string? priorState,
            DateTime syncTime)
        {
            if (connector.SupportsTransitionHistory(connection))
            {
                return syncedTransitions;
            }

            if (string.IsNullOrEmpty(priorState) || string.Equals(priorState, persistedItem.State, StringComparison.Ordinal))
            {
                return syncedTransitions;
            }

            var syntheticTransition = new WorkItemStateTransition
            {
                FromState = priorState,
                ToState = persistedItem.State,
                TransitionedAt = syncTime,
            };

            return [.. syncedTransitions, syntheticTransition];
        }

        private WorkItem SyncWorkItem(WorkItem item, WorkItem? existingItem)
        {
            if (existingItem == null)
            {
                workItemRepository.Add(item);
                logger.LogDebug("Added Work Item {WorkItemId}", item.ReferenceId);
                return item;
            }

            existingItem.Update(item);
            workItemRepository.Update(existingItem);
            logger.LogDebug("Updated Work Item {WorkItemId}", item.ReferenceId);
            return existingItem;
        }

        private List<WorkItemStateTransition> SyncStateTransitions(WorkItem workItem, IReadOnlyList<WorkItemStateTransition> syncedTransitions)
        {
            var existingTransitions = stateTransitionRepository
                .GetAllByPredicate(transition => transition.WorkItemId == workItem.Id)
                .ToList();

            var newTransitions = syncedTransitions
                .Where(transition => !existingTransitions.Exists(stored =>
                    stored.ToState == transition.ToState && stored.TransitionedAt == transition.TransitionedAt))
                .Select(transition => new WorkItemStateTransition
                {
                    WorkItemId = workItem.Id,
                    FromState = transition.FromState,
                    ToState = transition.ToState,
                    TransitionedAt = transition.TransitionedAt,
                })
                .ToList();

            newTransitions.ForEach(stateTransitionRepository.Add);

            workItem.CurrentStateEnteredAt = DeriveCurrentStateEnteredAt(workItem, existingTransitions.Concat(newTransitions));

            return newTransitions;
        }

        private static DateTime? DeriveCurrentStateEnteredAt(WorkItem workItem, IEnumerable<WorkItemStateTransition> transitions)
        {
            var matchingTransitions = transitions
                .Where(transition => transition.ToState == workItem.State)
                .Select(transition => transition.TransitionedAt)
                .ToList();

            return matchingTransitions.Count == 0
                ? null
                : matchingTransitions.Max();
        }

        private static DateTime? DeriveCurrentStateEnteredAt(Feature feature, IEnumerable<FeatureStateTransition> transitions)
        {
            var matchingTransitions = transitions
                .Where(transition => transition.ToState == feature.State)
                .Select(transition => transition.TransitionedAt)
                .ToList();

            return matchingTransitions.Count == 0
                ? null
                : matchingTransitions.Max();
        }

        private async Task UpdateRemainingWorkForPortfolio(Portfolio portfolio)
        {
            // Stryker disable once all: entry trace of the remaining-work pass; the pass itself is the statements below, which Stryker mutates on their own.
            logger.LogDebug("Updating Remaining Work for Portfolio {PortfolioName}", portfolio.Name);
            defaultWorkItemsBasedOnPercentile.Remove(portfolio.Id);

            RefreshRemainingWork(portfolio);

            ExtrapolateNotBrokenDownFeatures(portfolio);

            await featureRepository.Save();

            // Stryker disable once all: exit trace of that same pass; nothing branches on it and no caller reads it.
            logger.LogDebug("Done Updating Remaining Work for Portfolio {PortfolioName}", portfolio.Name);
        }

        private void RefreshRemainingWork(Portfolio project)
        {
            foreach (var feature in project.Features)
            {
                feature.ClearFeatureWork();
                feature.IsUsingDefaultFeatureSize = false;

                var allWorkForFeature = workItemRepository.GetAllByPredicate(wi => wi.ParentReferenceId == feature.ReferenceId).ToList();

                var teamsWithWork = allWorkForFeature
                    .Where(w => w.Team != null)
                    .Select(w => w.Team)
                    .DistinctBy(t => t.Id)
                    .ToList();

                foreach (var team in teamsWithWork)
                {
                    var totalWorkForFeatureForTeam = allWorkForFeature.Where(f => f.TeamId == team.Id).ToList();
                    var remainingWorkForFeatureForTeam = totalWorkForFeatureForTeam.Where(x => x.StateCategory != StateCategories.Done).ToList();

                    feature.AddOrUpdateWorkForTeam(team, remainingWorkForFeatureForTeam.Count, totalWorkForFeatureForTeam.Count);
                }
            }

            foreach (var feature in project.Features)
            {
                feature.FeatureWork.RemoveAll(f => f.TotalWorkItems == 0);
            }
        }

        private void ExtrapolateNotBrokenDownFeatures(Portfolio portfolio)
        {
            foreach (var feature in portfolio.GetFeaturesToOverrideWithDefaultSize())
            {
                var actualTotal = feature.FeatureWork.Sum(x => x.TotalWorkItems);
                var defaultSize = GetDefaultRemainingWork(portfolio);

                if (actualTotal < defaultSize)
                {
                    feature.ClearFeatureWork();
                }
            }

            // Stryker disable once all: per-record narration. Its control asserts the set is present at Debug by fragment, so emptying one line deliberately leaves the others.
            logger.LogDebug("Extrapolating Not Broken Down Features for Portfolio {PortfolioName}", portfolio.Name);

            foreach (var feature in portfolio.GetFeaturesToExtrapolate())
            {
                // Stryker disable once all: same set — the extrapolation decision it narrates is the assignment below, whose own mutant is killed.
                logger.LogDebug("Feature {FeatureName} has no Work - Extrapolating", feature.Name);
                feature.IsUsingDefaultFeatureSize = true;

                var remainingWork = GetExtrapolatedRemainingWork(portfolio, feature);

                AssignExtrapolatedWorkToTeams(portfolio, feature, remainingWork);

                // Stryker disable once all: same set — the work it reports was assigned by AssignExtrapolatedWorkToTeams above, whose mutants are killed.
                logger.LogDebug("Added {RemainingWork} Items to Feature {FeatureName}", remainingWork, feature.Name);
            }
        }

        private void AssignExtrapolatedWorkToTeams(Portfolio portfolio, Feature feature, int remainingWork)
        {
            var involvedTeams = portfolio.Teams.ToList();

            var owningTeams = involvedTeams.Count > 0
                ? involvedTeams
                : teamRepository.GetAll().ToList();

            if (portfolio.OwningTeam != null)
            {
                // Stryker disable once all: trace of the owning-team branch; the null guard it sits under is mutated on its own and killed.
                logger.LogDebug("Owning Team for Portfolio is {TeamName} - using this for Default Work Assignment", portfolio.OwningTeam.Name);
                owningTeams = [portfolio.OwningTeam];
            }

            var featureOwnerValue =
                feature.GetAdditionalFieldValue(portfolio.FeatureOwnerAdditionalFieldDefinitionId);

            if (!string.IsNullOrEmpty(featureOwnerValue))
            {
                // Stryker disable once all: trace of the feature-owner branch; the IsNullOrEmpty guard it sits under is mutated on its own and killed.
                logger.LogDebug("Feature Owner Field for Project is configured - Getting value for Feature {FeatureName}: {OwnerValue}", feature.Name, featureOwnerValue);

                var featureOwners = teamRepository.GetAll().Where(t => featureOwnerValue.Contains(t.Name)).ToList();

                // Stryker disable once all: covers the join separator too — the joined string is built for this message and read nowhere else.
                logger.LogDebug("Found following teams defined in Feature Owner field: {Owners}", string.Join(",", featureOwners.Select(t => t.Name)));
                if (featureOwners.Count > 0)
                {
                    owningTeams = featureOwners;
                }
            }

            var numberOfTeams = owningTeams.Count;
            if (numberOfTeams == 0)
            {
                logger.LogWarning("No teams available for extrapolation of feature {FeatureName} in portfolio {PortfolioName}", feature.Name, portfolio.Name);
                return;
            }

            var buckets = SplitIntoBuckets(remainingWork, numberOfTeams);
            for (var index = 0; index < numberOfTeams; index++)
            {
                var team = owningTeams[index];
                var totalWork = buckets[index];
                feature.AddOrUpdateWorkForTeam(team, totalWork, totalWork);

                // Stryker disable once all: narrates the AddOrUpdateWorkForTeam call above, whose removal is killed.
                logger.LogDebug("Added {TotalWork} Items for Feature {FeatureName} to Team {TeamName}", totalWork, feature.Name, team.Name);
            }
        }

        private int GetExtrapolatedRemainingWork(Portfolio project, Feature feature)
        {
            if (feature.EstimatedSize > 0)
            {
                return feature.EstimatedSize;
            }

            return GetDefaultRemainingWork(project);
        }

        private int GetDefaultRemainingWork(Portfolio project)
        {
            if (defaultWorkItemsBasedOnPercentile.TryGetValue(project.Id, out var defaultItems))
            {
                return defaultItems;
            }

            defaultItems = project.DefaultAmountOfWorkItemsPerFeature;

            if (project.UsePercentileToCalculateDefaultAmountOfWorkItems)
            {
                // Stryker disable once all: trace of the percentile branch; the UsePercentileToCalculateDefaultAmountOfWorkItems guard above is mutated on its own and killed.
                logger.LogDebug("Using Percentile to Calculate Default Amount of Work Items for Project {Project}", project.Name);

                /* Use ProjectMetricsService to Get Values */
                // Bug #5567 decision 4: both stay UTC. A history window is an instant offset, not a
                // calendar day; an off-by-one only widens the percentile's sample.
                var endDate = DateTime.UtcNow;

                var historyInDays = project.PercentileHistoryInDays ?? 90;
                var startDate = DateTime.UtcNow.AddDays(-historyInDays);
                var closedFeatures = portfolioMetricsService.GetCycleTimeDataForPortfolio(project, startDate, endDate);

                var historicalFeatureSize = closedFeatures.Where(f => f.Size > 0).Select(f => f.Size);

                // Stryker disable once all: covers the join separator too — the joined sample exists for this message; the percentile is computed from the sequence, not the string.
                logger.LogDebug("Features had following number of child items: {ChildItems}", string.Join(",", historicalFeatureSize));

                if (historicalFeatureSize.Any())
                {
                    defaultItems = PercentileCalculator.CalculatePercentile(historicalFeatureSize.ToList(), project.DefaultWorkItemPercentile);

                    // Stryker disable once all: reports the value CalculatePercentile just returned; the value is what the caller uses, the sentence is not.
                    logger.LogDebug("{Percentile} Percentile Based on Last {Days} days is {DefaultItems}", project.DefaultWorkItemPercentile, project.PercentileHistoryInDays, defaultItems);
                }
            }

            defaultWorkItemsBasedOnPercentile.Add(project.Id, defaultItems);
            return defaultItems;
        }

        private static int[] SplitIntoBuckets(int itemCount, int numBuckets)
        {
            var buckets = new int[numBuckets];
            int quotient = itemCount / numBuckets;
            int remainder = itemCount % numBuckets;

            for (int i = 0; i < numBuckets; i++)
            {
                buckets[i] = quotient;
            }

            for (int i = 0; i < remainder; i++)
            {
                buckets[i]++;
            }

            return buckets;
        }

        private async Task<SyncOutcome> RefreshFeatures(Portfolio portfolio, bool fetchShapeChanged)
        {
            var connector = GetWorkItemServiceForWorkTrackingSystem(portfolio.WorkTrackingSystemConnection.WorkTrackingSystem);

            var downloadedFeatures = new List<Feature>();
            var featuresWithTransitions = new List<(Feature persistedFeature, IReadOnlyList<WorkItemStateTransition> syncedTransitions)>();
            var syncedFeatures = new List<SyncedFeature>();

            // Read BEFORE UpdateFeatures below clears the collection: what the portfolio already stores is
            // the resolver's input, and an empty or unstamped set is what keeps an upgraded instance on the
            // full path for one cycle.
            var storedFeatures = portfolio.Features.ToList();

            var fetch = await ResolveRemoteFeatureFetch(connector, portfolio, storedFeatures, fetchShapeChanged);

            foreach (var feature in fetch.Features)
            {
                // Read the PRE-UPDATE per-portfolio blocked verdict BEFORE AddOrUpdateFeature mutates the
                // persisted feature in place - otherwise the prior state is destroyed
                // before the rising/falling edge can be observed.
                var existingFeature = featureRepository.GetByPredicate(f => f.ReferenceId == feature.ReferenceId);
                var wasObservedBeforeSync = existingFeature != null;
                var wasBlockedBeforeSync = existingFeature != null && blockedItemService.IsBlocked(existingFeature, portfolio);

                var featureFromDatabase = AddOrUpdateFeature(feature, existingFeature);

                downloadedFeatures.Add(featureFromDatabase);
                featuresWithTransitions.Add((featureFromDatabase, feature.SyncedTransitions));

                // Parent features are captured through RefreshParentFeatures and never emit blocked
                // spells - exclude them from the eligible edge-detection set.
                if (!featureFromDatabase.IsParentFeature)
                {
                    syncedFeatures.Add(new SyncedFeature(featureFromDatabase, wasObservedBeforeSync, wasBlockedBeforeSync));
                }
            }

            var featuresTheQueryStillReturns = TheFeaturesTheQueryStillReturns(fetch.StillOnTheTracker, downloadedFeatures);

            // The whole surviving set, not the downloads: a Feature nobody had to download this cycle is
            // still one this portfolio holds, and a Feature left with no portfolio claim is deleted
            // outright by the orphaned-Feature cleanup the updater runs.
            portfolio.UpdateFeatures(featureOrdering.Order(featuresTheQueryStillReturns));

            await featureRepository.Save();

            // Edge detection runs in the SAME second pass as SyncFeatureStateTransitions because feature.Id
            // is 0 until the Save above — the FeatureBlocked/Unblocked events must carry the persisted id.
            var events = new List<IDomainEvent>();
            foreach (var (persistedFeature, syncedTransitions) in featuresWithTransitions)
            {
                SyncFeatureStateTransitions(persistedFeature, syncedTransitions);
            }

            foreach (var syncedFeature in syncedFeatures)
            {
                events.AddRange(CollectFeatureBlockedEvents(portfolio, syncedFeature));
            }

            await featureStateTransitionRepository.Save();
            await featureRepository.Save();

            await PublishDomainEvents(events);

            await SweepDepartedFeatureSpells(portfolio, featuresTheQueryStillReturns);

            return fetch.Outcome;
        }

        /// <summary>
        /// What one portfolio refresh has to work with: the Feature payloads it downloaded, every reference
        /// id the query still returns, and what to report to the operator. The second is the one the
        /// portfolio's membership is rebuilt from - never the first. Ordered rather than a set, because
        /// Features tied on their order value fall back to arrival order until the save gives them an id.
        /// </summary>
        private sealed record RemoteFeatureFetch(List<Feature> Features, List<string> StillOnTheTracker, SyncOutcome Outcome);

        private static async Task<RemoteFeatureFetch> FetchEveryFeature(IWorkTrackingConnector connector, Portfolio portfolio)
        {
            var recordsFromTracker = (await connector.GetFeaturesForProject(portfolio)).ToList();

            return new RemoteFeatureFetch(
                recordsFromTracker,
                recordsFromTracker.ConvertAll(feature => feature.ReferenceId),
                SyncOutcome.FullSync(recordsFromTracker.Count));
        }

        /// <summary>
        /// The portfolio half of the same decision, mirroring the team path deliberately rather than
        /// sharing it: a Feature is not a Work Item, and merging the two routines would mean refactoring
        /// the shipped team path inside a change about portfolios. The opt-in gates the scan as well as the
        /// mode decision, so a connector nobody volunteered is never approached at all.
        /// </summary>
        private async Task<RemoteFeatureFetch> ResolveRemoteFeatureFetch(IWorkTrackingConnector connector, Portfolio portfolio, List<Feature> storedFeatures, bool fetchShapeChanged)
        {
            var operatorAskedForTheCheaperRefresh = TheOperatorAskedForTheCheaperRefresh();

            var scan = operatorAskedForTheCheaperRefresh
                ? await ScanRemoteFeatureIdentities(connector, portfolio)
                : new IdentityScan(TrackerCanBeScanned: false, Succeeded: false, Stamps: []);

            var mode = SyncModeResolver.Resolve(
                operatorAskedForTheCheaperRefresh,
                scan.TrackerCanBeScanned,
                storedFeatures,
                scan.Succeeded,
                fetchShapeChanged);

            return mode == SyncMode.Delta
                ? await FetchOnlyTheFeaturesThatMoved(connector, portfolio, storedFeatures, scan.Stamps)
                : await FetchEveryFeature(connector, portfolio);
        }

        /// <summary>The scan for the portfolio: the same Feature query, asking only for identity plus the remote change stamp.</summary>
        private async Task<IdentityScan> ScanRemoteFeatureIdentities(IWorkTrackingConnector connector, Portfolio portfolio)
        {
            if (!connector.SupportsIncrementalSync(portfolio.WorkTrackingSystemConnection))
            {
                return new IdentityScan(TrackerCanBeScanned: false, Succeeded: false, Stamps: []);
            }

            try
            {
                return new IdentityScan(TrackerCanBeScanned: true, Succeeded: true, Stamps: [.. await connector.SweepFeaturesForPortfolio(portfolio)]);
            }
            // A half-scanned query is the one answer never allowed, so any scan failure falls back to the
            // whole query - loudly, or nobody learns the cheap path stopped working.
#pragma warning disable CA1031
            catch (Exception exception)
#pragma warning restore CA1031
            {
                // The sink renders the exception itself, so interpolating its message into the template as
                // well would print the failure text twice on the operator's one line.
                logger.LogWarning(
                    exception,
                    "Identity scan failed for Portfolio {PortfolioName} - downloading the whole query instead.",
                    portfolio.Name);

                return new IdentityScan(TrackerCanBeScanned: true, Succeeded: false, Stamps: []);
            }
        }

        /// <summary>
        /// The download for the portfolio: full Feature payloads for the records whose stamp moved, and
        /// for nothing else. A cycle in which nothing moved asks for nothing - a keyed query for an empty
        /// key set is still a remote round trip.
        /// </summary>
        private async Task<RemoteFeatureFetch> FetchOnlyTheFeaturesThatMoved(IWorkTrackingConnector connector, Portfolio portfolio, List<Feature> storedFeatures, List<RemoteRecordStamp> sweptRecords)
        {
            var sweptOnce = DeduplicateByReferenceId(sweptRecords, portfolio.Name, record => record.ReferenceId);
            var movedReferenceIds = sweptOnce
                .FindAll(record => HasMoved(record, storedFeatures))
                .ConvertAll(record => record.ReferenceId);

            List<Feature> downloaded = movedReferenceIds.Count == 0
                ? []
                : DeduplicateByReferenceId((await connector.GetFeaturesForProject(portfolio, movedReferenceIds)).ToList(), portfolio.Name, feature => feature.ReferenceId);

            return new RemoteFeatureFetch(
                downloaded,
                sweptOnce.ConvertAll(record => record.ReferenceId),
                SyncOutcome.DeltaSync(sweptOnce.Count, downloaded.Count));
        }

        /// <summary>
        /// The portfolio's Feature set, resolved against storage from the reference ids the query still
        /// returns. A cycle that downloads only what moved still holds everything the query answers with,
        /// and UpdateFeatures is Clear + AddRange - so handing it the downloads would drop every quiet
        /// Feature's claim and let the orphaned-Feature cleanup delete the row. Resolution goes through the
        /// repository rather than through the portfolio's own collection, because a Feature another
        /// portfolio's cycle already stored is stored and still new to this one.
        /// </summary>
        private List<Feature> TheFeaturesTheQueryStillReturns(List<string> stillOnTheTracker, List<Feature> downloadedFeatures)
        {
            var downloadedByReferenceId = new Dictionary<string, Feature>();
            foreach (var downloaded in downloadedFeatures)
            {
                downloadedByReferenceId.TryAdd(downloaded.ReferenceId, downloaded);
            }

            var survivors = new List<Feature>();
            foreach (var referenceId in stillOnTheTracker)
            {
                // Just downloaded wins over a lookup: until the save below, a Feature this cycle added is
                // tracked but not yet answerable by a query.
                var survivor = downloadedByReferenceId.TryGetValue(referenceId, out var justDownloaded)
                    ? justDownloaded
                    : featureRepository.GetByPredicate(stored => stored.ReferenceId == referenceId);

                if (survivor != null)
                {
                    survivors.Add(survivor);
                }
            }

            return survivors;
        }

        private List<IDomainEvent> CollectFeatureBlockedEvents(Portfolio portfolio, SyncedFeature syncedFeature)
        {
            var feature = syncedFeature.PersistedFeature;
            var events = new List<IDomainEvent>();

            // A feature already blocked the first time capture sees it opens no spell. Without an earlier
            // not-blocked reading there is no moment when it became blocked, and inventing one would date
            // the spell from whenever this instance happened to start looking.
            if (!syncedFeature.WasObservedBeforeSync)
            {
                return events;
            }

            var isBlockedNow = blockedItemService.IsBlocked(feature, portfolio);

            if (!syncedFeature.WasBlockedBeforeSync && isBlockedNow)
            {
                events.Add(new FeatureBlocked(feature.Id, portfolio.Id, feature.State));
            }
            else if (syncedFeature.WasBlockedBeforeSync && !isBlockedNow)
            {
                events.Add(new FeatureUnblocked(feature.Id, portfolio.Id));
            }

            return events;
        }

        private async Task SweepDepartedFeatureSpells(Portfolio portfolio, List<Feature> refreshedFeatures)
        {
            // Empty-refresh guard: a transient connector failure returning zero features must not
            // silently close every open spell. A portfolio genuinely holding zero features has no open
            // spells, so skipping the sweep is free.
            if (refreshedFeatures.Count == 0)
            {
                return;
            }

            var refreshedFeatureIds = refreshedFeatures.Select(f => f.Id).ToHashSet();
            var openSpells = featureBlockedTransitionRepository.GetOpenSpellsForPortfolio(portfolio.Id);

            var departedEvents = openSpells
                .Where(spell => !refreshedFeatureIds.Contains(spell.Key))
                .Select(spell => (IDomainEvent)new FeatureUnblocked(spell.Key, portfolio.Id))
                .ToList();

            await PublishDomainEvents(departedEvents);
        }

        private sealed record SyncedFeature(Feature PersistedFeature, bool WasObservedBeforeSync, bool WasBlockedBeforeSync);

        private void SyncFeatureStateTransitions(Feature feature, IReadOnlyList<WorkItemStateTransition> syncedTransitions)
        {
            var existingTransitions = featureStateTransitionRepository
                .GetAllByPredicate(transition => transition.FeatureId == feature.Id)
                .ToList();

            var newTransitions = syncedTransitions
                .Where(transition => !existingTransitions.Exists(stored =>
                    stored.ToState == transition.ToState && stored.TransitionedAt == transition.TransitionedAt))
                .Select(transition => new FeatureStateTransition
                {
                    FeatureId = feature.Id,
                    FromState = transition.FromState,
                    ToState = transition.ToState,
                    TransitionedAt = transition.TransitionedAt,
                })
                .ToList();

            newTransitions.ForEach(featureStateTransitionRepository.Add);

            feature.CurrentStateEnteredAt = DeriveCurrentStateEnteredAt(feature, existingTransitions.Concat(newTransitions));
        }

        private Feature AddOrUpdateFeature(Feature feature, Feature? featureFromDatabase)
        {
            var referencesFromTracker = feature.DependsOnReferences.ToList();
            var storedFeature = TheStoredFeatureFor(feature, featureFromDatabase);

            // On both branches: a Feature seen for the first time carries the links somebody drew on it
            // just as one already on file does. Reconciling only where a row already existed would leave
            // a new Feature waiting on nothing until the refresh after next, which looks like the tracker
            // simply had no links yet rather than like a bug.
            dependencyReconciler.Reconcile(storedFeature, referencesFromTracker);

            return storedFeature;
        }

        private Feature TheStoredFeatureFor(Feature feature, Feature? featureFromDatabase)
        {
            if (featureFromDatabase == null)
            {
                featureRepository.Add(feature);
                logger.LogDebug("Found New Feature {FeatureName}", feature.Name);
                return feature;
            }

            featureFromDatabase.Update(feature);
            logger.LogDebug("Updated Existing Feature {FeatureName}", feature.Name);
            return featureFromDatabase;
        }

        private async Task RefreshParentFeatures(Portfolio project, bool fetchShapeChanged)
        {
            var workItemService = GetWorkItemServiceForWorkTrackingSystem(project.WorkTrackingSystemConnection.WorkTrackingSystem);
            var parentFeatureIds = project.Features.Where(f => !string.IsNullOrEmpty(f.ParentReferenceId)).Select(f => f.ParentReferenceId).Distinct().ToList();

            if (parentFeatureIds.Count == 0)
            {
                logger.LogDebug("No Parent Features found for Project {ProjectName}", project.Name);
                return;
            }

            var parentFeatures = await ResolveRemoteParentFeatureFetch(workItemService, project, parentFeatureIds, fetchShapeChanged);

            foreach (var parentFeature in parentFeatures)
            {
                parentFeature.IsParentFeature = true;

                var existingParentFeature = featureRepository.GetByPredicate(f => f.ReferenceId == parentFeature.ReferenceId);
                AddOrUpdateFeature(parentFeature, existingParentFeature);
            }

            await featureRepository.Save();
        }

        /// <summary>
        /// The parent half of the same decision, over the keys the portfolio STORES. The download is the
        /// existing keyed <c>GetParentFeaturesDetails</c> asked for a shorter key list, and the mode goes
        /// through the same resolver - over the stored PARENTS, which are not members of
        /// <c>portfolio.Features</c> and so are resolved from storage by reference id. The parent sweep
        /// stays out of <see cref="SyncOutcome"/>: the summary line's counts are the Feature half's. The
        /// fetch shape is the PORTFOLIO's own answer, passed down rather than compared a second time.
        /// </summary>
        private async Task<List<Feature>> ResolveRemoteParentFeatureFetch(IWorkTrackingConnector connector, Portfolio portfolio, List<string> parentFeatureIds, bool fetchShapeChanged)
        {
            var storedParentFeatures = TheStoredParentFeatures(parentFeatureIds);
            var operatorAskedForTheCheaperRefresh = TheOperatorAskedForTheCheaperRefresh();

            var scan = operatorAskedForTheCheaperRefresh
                ? await ScanRemoteParentFeatureIdentities(connector, portfolio, parentFeatureIds)
                : new IdentityScan(TrackerCanBeScanned: false, Succeeded: false, Stamps: []);

            var mode = SyncModeResolver.Resolve(
                operatorAskedForTheCheaperRefresh,
                scan.TrackerCanBeScanned,
                storedParentFeatures,
                scan.Succeeded,
                fetchShapeChanged);

            return mode == SyncMode.Delta
                ? await FetchOnlyTheParentFeaturesThatMoved(connector, portfolio, parentFeatureIds, storedParentFeatures, scan.Stamps)
                : await connector.GetParentFeaturesDetails(portfolio, parentFeatureIds);
        }

        private List<Feature> TheStoredParentFeatures(List<string> parentFeatureIds)
        {
            var storedParentFeatures = new List<Feature>();

            foreach (var parentFeatureId in parentFeatureIds)
            {
                var stored = featureRepository.GetByPredicate(feature => feature.ReferenceId == parentFeatureId);

                if (stored != null)
                {
                    storedParentFeatures.Add(stored);
                }
            }

            return storedParentFeatures;
        }

        /// <summary>The scan for the parent half: the same keyed query, asking only for identity plus the remote change stamp.</summary>
        private async Task<IdentityScan> ScanRemoteParentFeatureIdentities(IWorkTrackingConnector connector, Portfolio portfolio, List<string> parentFeatureIds)
        {
            if (!connector.SupportsIncrementalSync(portfolio.WorkTrackingSystemConnection))
            {
                return new IdentityScan(TrackerCanBeScanned: false, Succeeded: false, Stamps: []);
            }

            try
            {
                return new IdentityScan(TrackerCanBeScanned: true, Succeeded: true, Stamps: [.. await connector.SweepParentFeatures(portfolio, parentFeatureIds)]);
            }
            // Same rule again: a half-scanned key list falls back to downloading every parent, and says so.
#pragma warning disable CA1031
            catch (Exception exception)
#pragma warning restore CA1031
            {
                // The sink renders the exception itself, so interpolating its message into the template as
                // well would print the failure text twice on the operator's one line.
                logger.LogWarning(
                    exception,
                    "Parent Feature identity scan failed for Portfolio {PortfolioName} - downloading every parent instead.",
                    portfolio.Name);

                return new IdentityScan(TrackerCanBeScanned: true, Succeeded: false, Stamps: []);
            }
        }

        /// <summary>
        /// The download half for parent Features, over the keys the portfolio stores. A cycle in which no
        /// parent moved asks for nothing at all - a keyed query for an empty key set is still a remote
        /// round trip.
        /// </summary>
        private async Task<List<Feature>> FetchOnlyTheParentFeaturesThatMoved(IWorkTrackingConnector connector, Portfolio portfolio, List<string> parentFeatureIds, List<Feature> storedParentFeatures, List<RemoteRecordStamp> sweptRecords)
        {
            var sweptOnce = DeduplicateByReferenceId(sweptRecords, portfolio.Name, record => record.ReferenceId);

            var keysToDownload = parentFeatureIds.FindAll(
                parentFeatureId => TheSweepDidNotVouchForThisParent(parentFeatureId, sweptOnce, storedParentFeatures));

            return keysToDownload.Count == 0
                ? []
                : DeduplicateByReferenceId(await connector.GetParentFeaturesDetails(portfolio, keysToDownload), portfolio.Name, feature => feature.ReferenceId);
        }

        // Inverts the Feature half's rule: parents are excluded from the orphaned-Feature cleanup by
        // !f.IsParentFeature, so a stored key the sweep did not answer for is DOWNLOADED, never read as
        // departed.
        private static bool TheSweepDidNotVouchForThisParent(string parentFeatureId, List<RemoteRecordStamp> sweptOnce, List<Feature> storedParentFeatures)
        {
            var swept = sweptOnce.Find(record => record.ReferenceId == parentFeatureId);

            return swept == null || HasMoved(swept, storedParentFeatures);
        }

        private IWorkTrackingConnector GetWorkItemServiceForWorkTrackingSystem(WorkTrackingSystems workTrackingSystem)
        {
            return workTrackingConnectorFactory.GetWorkTrackingConnector(workTrackingSystem);
        }
    }
}
