using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
﻿using Lighthouse.Backend.Extensions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Models.Staleness;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.Parents;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Dependencies;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;

namespace Lighthouse.Backend.Services.Implementation.WorkItems
{
#pragma warning disable S107 // Every parameter is a distinct store or port the refresh drives; bundling them into a parameter object would only hide the arity, not the coupling.
    public class WorkItemService(
        ILogger<WorkItemService> logger,
        IWorkTrackingConnectorFactory workTrackingConnectorFactory,
        IFeatureRepository featureRepository,
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
        IDependencyReconciler dependencyReconciler,
        IDependencyRefreshReporter dependencyRefreshReporter,
        UpdateCancellationContext cancellationContext,
        ILighthouseClock clock)
        : IWorkItemService
#pragma warning restore S107
    {
        private readonly Dictionary<int, int> defaultWorkItemsBasedOnPercentile = new();

        /// <summary>
        /// Whether the refresh this call belongs to has been told to stop. Read from the ambient context the
        /// update queue sets and handed to the connector explicitly: the pipeline is where the ambient
        /// belongs, and a driven adapter should be told rather than have to go looking.
        /// </summary>
        private CancellationToken Stopping => cancellationContext.Current;

        public async Task<SyncOutcome> UpdateFeaturesForPortfolio(Portfolio portfolio)
        {
            logger.LogInformation("Updating Features for Portfolio {PortfolioName}", portfolio.Name);

            var fetchShape = FetchShape.Of(portfolio);

            var featureHalf = await RefreshFeatures(portfolio, fetchShape.Changed);
            await RefreshParentFeatures(portfolio, fetchShape.Changed, featureHalf.Downloaded);
            var outcome = featureHalf.Outcome with { Reason = fetchShape.Reason };

            await UpdateRemainingWorkForPortfolio(portfolio);

            dependencyRefreshReporter.ReportOn(portfolio);

            portfolio.RefreshUpdateTime(clock.Now.UtcDateTime);

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

            var syncTime = clock.Now.UtcDateTime;
            var connector = workTrackingConnectorFactory.GetWorkTrackingConnector(team.WorkTrackingSystemConnection.WorkTrackingSystem);

            var storedWorkItems = workItemRepository.GetAllByPredicate(wi => wi.TeamId == team.Id).ToList();

            var fetch = await ResolveRemoteFetch(connector, team, storedWorkItems, fetchShapeChanged);

            var itemsWithTransitions = SyncDownloadedItems(connector, team, storedWorkItems, fetch.WorkItems, syncTime);
            var itemsRemovedThisCycle = RemoveItemsThatLeftTheQuery(storedWorkItems, fetch.StillOnTheTracker);

            await workItemRepository.Save();

            var events = new List<IDomainEvent>();
            foreach (var syncedItem in itemsWithTransitions)
            {
                var newTransitions = SyncStateTransitions(syncedItem);
                events.AddRange(CollectDomainEvents(team, syncedItem, newTransitions));
            }

            events.AddRange(CollectStalenessEvents(team, EverythingTheTeamStillHas(storedWorkItems, itemsWithTransitions, itemsRemovedThisCycle)));

            await stateTransitionRepository.Save();
            await workItemRepository.Save();

            await PublishDomainEvents(events);

            return fetch.Outcome;
        }

        /// <summary>
        /// How much this cycle downloads, and the download itself. Every refresh scans first, because the
        /// scan is what lets it skip the records that did not move. That is safe to do unconditionally: a
        /// connector that cannot enumerate its query reliably says so before any request is sent, and a scan
        /// that fails or is refused downloads the whole query, exactly as if no scan had been tried.
        /// </summary>
        private async Task<RemoteFetch> ResolveRemoteFetch(IWorkTrackingConnector connector, Team team, List<WorkItem> storedWorkItems, bool fetchShapeChanged)
        {
            var scan = await ScanRemoteIdentities(connector, team);

            var mode = SyncModeResolver.Resolve(
                scan.TrackerCanBeScanned,
                storedWorkItems,
                scan.Succeeded,
                fetchShapeChanged);

            var fetch = mode == SyncMode.Delta
                ? await FetchOnlyWhatMoved(connector, team, storedWorkItems, scan.Stamps)
                : await FetchEverything(connector, team);

            var placements = PlaceWhatTheLinksLeftOpen(team, ParentPlacementReport.Team(team.Name), ParentNarrowing.Narrow, fetch.WorkItems);
            var findings = ReportWhatTheSettingsLeftUnread($"Team {team.Name}", fetch.Outcome, fetch.WorkItems, _ => true);

            return fetch with { Outcome = CountingThePlacements(fetch.Outcome, placements) with { Findings = findings } };
        }

        private static SyncOutcome CountingThePlacements(SyncOutcome outcome, (int Placed, int NotPlaced) placements)
            => outcome with
            {
                RecordsPlacedFromLinksNamingMoreThanOneParent = placements.Placed,
                RecordsWhoseLinksNamedMoreThanOneParent = placements.NotPlaced,
            };

        /// <summary>
        /// Hangs each downloaded record whose links named several issues under one of them, before anything
        /// is saved. A Team first sets aside the candidates Lighthouse does not hold as Features. A Portfolio
        /// sets nothing aside: the issues above its Features are stored only once a refresh has read them, so
        /// setting aside what Lighthouse does not hold would throw away every parent the first time it is
        /// seen. Among what is left the oldest link wins, then the issue created first, then the lowest key.
        /// The connector cannot make that choice, because only the stored Features say which candidate is one
        /// and when it was created, so it hands every candidate on and the choice is made here. The stored
        /// Features are read once for the whole fetch, and not at all when no record needs them. Records with
        /// one link or none are not touched: the connector already gave them their parent. What was decided
        /// is written as a warning, because a parent picked on the administrator's behalf, or not picked at
        /// all, is something they are asked to check.
        /// </summary>
        private (int Placed, int NotPlaced) PlaceWhatTheLinksLeftOpen<TRecord>(IWorkItemQueryOwner owner, string ownerInTheLog, ParentNarrowing narrowing, List<TRecord> downloaded)
            where TRecord : WorkItemBase
        {
            var undecided = downloaded.FindAll(LinksNameMoreThanOneParent);

            if (undecided.Count == 0)
            {
                return (0, 0);
            }

            var placements = PlaceAmongTheirCandidates(owner, narrowing, undecided);

            foreach (var line in ParentPlacementReport.Lines(ownerInTheLog, placements))
            {
                logger.LogWarning("{ParentPlacementLine}", line);
            }

            var placed = placements.Count(record => record.Placement.Outcome == ParentPlacementOutcome.Placed);

            return (placed, undecided.Count - placed);
        }

        private List<RecordPlacement> PlaceAmongTheirCandidates<TRecord>(IWorkItemQueryOwner owner, ParentNarrowing narrowing, List<TRecord> undecided)
            where TRecord : WorkItemBase
        {
            var storedFeatures = featureRepository.GetStoredFeatureCreatedDates();
            var placements = new List<RecordPlacement>();

            foreach (var record in undecided)
            {
                var links = ParentResolution.From(record.ParentCandidatesFromLinks);
                var placement = links.Place(storedFeatures, narrowing);

                if (placement.Outcome == ParentPlacementOutcome.Placed)
                {
                    ParentSourceSelector.HangUnder(owner, record, placement.Key);
                }

                placements.Add(new RecordPlacement(record.ReferenceId, links, placement));
            }

            return placements;
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
                itemsWithTransitions.Add(new SyncedItem(persistedItem, syncedTransitions, item.TransitionsAreItsWholeHistory, wasBlocked));
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

        /// <summary>The scan: the same query, asking only for identity plus the remote change stamp.</summary>
        private async Task<IdentityScan> ScanRemoteIdentities(IWorkTrackingConnector connector, Team team)
        {
            if (!connector.SupportsIncrementalSync(team.WorkTrackingSystemConnection))
            {
                return new IdentityScan(TrackerCanBeScanned: false, Succeeded: false, Stamps: []);
            }

            try
            {
                return new IdentityScan(TrackerCanBeScanned: true, Succeeded: true, Stamps: [.. await connector.SweepWorkItemsForTeam(team, Stopping)]);
            }
            // A half-scanned query is the one answer never allowed, so any scan failure falls back to the
            // whole query - loudly, or nobody learns the cheap path stopped working.
            // A cancel is not a tracker failure. Caught below it is logged as a scan that broke, and the
            // refresh is then escalated to a full download - spending the rate limit the operator cancelled
            // to protect, and telling them the cheap path stopped working when in fact they stopped it.
            catch (OperationCanceledException)
            {
                throw;
            }
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
            var recordsFromTracker = (await connector.GetWorkItemsForTeam(team, Stopping)).ToList();
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
                : DeduplicateByReferenceId((await connector.GetWorkItemsForTeam(team, movedReferenceIds, Stopping)).ToList(), team.Name, workItem => workItem.ReferenceId);

            return new RemoteFetch(
                downloaded,
                [.. sweptOnce.Select(record => record.ReferenceId)],
                SyncOutcome.DeltaSync(sweptRecords.Count, downloaded.Count));
        }

        /// <summary>
        /// Read from the candidates themselves, merged by key, so two links to the same issue are one
        /// candidate rather than a question.
        /// </summary>
        private static bool LinksNameMoreThanOneParent(WorkItemBase record)
            => ParentResolution.From(record.ParentCandidatesFromLinks).IsAmbiguous;

        // Compared per item against the stored stamp. No global watermark, so clock skew and
        // server-time drift stay out of the design. A record nobody stored yet has always moved.
        private static bool HasMoved<TStored>(RemoteRecordStamp record, List<TStored> storedRecords) where TStored : WorkItemBase
        {
            var stored = storedRecords.Find(candidate => candidate.ReferenceId == record.ReferenceId);

            return stored?.LastChangedRemote != record.ChangedAt;
        }

        // Offset paging over an unordered query, as Jira Data Center does it, can return the same ReferenceId
        // twice in one fetch. Storing both copies breaks the SingleOrDefault lookups on every later sync, and
        // placing both would count and name one record twice. So every download and every identity scan, for
        // a Team, a Portfolio's Features and the issues above them alike, keeps the first copy of each under
        // this one rule and one warning, named after whoever synced.
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
        private List<IDomainEvent> CollectStalenessEvents(Team team, List<WorkItem> workItems)
        {
            var events = new List<IDomainEvent>();
            var policy = StalenessPolicy.For(team);

            foreach (var workItem in workItems)
            {
                AddStalenessEventIfThresholdCrossed(team, policy, workItem, events);
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

        private void AddStalenessEventIfThresholdCrossed(Team team, StalenessPolicy policy, WorkItem workItem, List<IDomainEvent> events)
        {
            var verdict = JudgeStaleness(team, policy, workItem);
            var isStaleNow = verdict.IsStale;
            if (isStaleNow && !workItem.WasStaleAtLastSync)
            {
                workItem.WasStaleAtLastSync = true;
                events.Add(new WorkItemBecameStale(workItem.Id, policy.ThresholdCrossedBy(verdict)));
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

        /// <summary>
        /// Judged by the same rule the WIP reads answer with. The cheap checks come first, so an item with
        /// nothing to judge never pays for a blocked evaluation.
        /// </summary>
        private StalenessVerdict JudgeStaleness(Team team, StalenessPolicy policy, WorkItem workItem)
        {
            if (workItem.StateCategory != StateCategories.Doing || !workItem.CurrentStateEnteredAt.HasValue)
            {
                return StalenessVerdict.NotStale;
            }

            var isBlocked = blockedItemService.IsBlocked(workItem, team);
            var candidate = new StalenessCandidate(
                workItem.StateCategory,
                workItem.State,
                workItem.CurrentStateEnteredAt,
                isBlocked,
                isBlocked ? workItem.CurrentStateEnteredAt : null);

            return policy.Evaluate(candidate, clock.Zone, clock.Today);
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
                    await domainEventDispatcher.PublishAsync(transitioned, CancellationToken.None);
                    break;
                case WorkItemBlocked blocked:
                    await domainEventDispatcher.PublishAsync(blocked, CancellationToken.None);
                    break;
                case WorkItemUnblocked unblocked:
                    await domainEventDispatcher.PublishAsync(unblocked, CancellationToken.None);
                    break;
                case WorkItemBecameStale becameStale:
                    await domainEventDispatcher.PublishAsync(becameStale, CancellationToken.None);
                    break;
                case FeatureBlocked featureBlocked:
                    await domainEventDispatcher.PublishAsync(featureBlocked, CancellationToken.None);
                    break;
                case FeatureUnblocked featureUnblocked:
                    await domainEventDispatcher.PublishAsync(featureUnblocked, CancellationToken.None);
                    break;
            }
        }

        private sealed record SyncedItem(WorkItem PersistedItem, IReadOnlyList<WorkItemStateTransition> SyncedTransitions, bool TransitionsAreItsWholeHistory, bool WasBlockedBeforeSync);

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

        /// <returns>The transitions that are moves the item made, as opposed to corrections of a stored one.</returns>
        private List<WorkItemStateTransition> SyncStateTransitions(SyncedItem syncedItem)
        {
            var workItem = syncedItem.PersistedItem;
            var syncedTransitions = syncedItem.SyncedTransitions;

            var existingTransitions = stateTransitionRepository
                .GetAllByPredicate(transition => transition.WorkItemId == workItem.Id)
                .ToList();

            var addedTransitions = syncedTransitions
                .Where(transition => !existingTransitions.Exists(stored => IsTheSameMove(stored.ToState, stored.TransitionedAt, transition)))
                .Select(transition => new WorkItemStateTransition
                {
                    WorkItemId = workItem.Id,
                    FromState = transition.FromState,
                    ToState = transition.ToState,
                    TransitionedAt = transition.TransitionedAt,
                })
                .ToList();

            var removedTransitions = MayReplaceTheStoredHistory(syncedTransitions, syncedItem.TransitionsAreItsWholeHistory)
                ? existingTransitions.FindAll(stored => !syncedTransitions.Any(transition => IsTheSameMove(stored.ToState, stored.TransitionedAt, transition)))
                : [];

            addedTransitions.ForEach(stateTransitionRepository.Add);
            removedTransitions.ForEach(stateTransitionRepository.Remove);

            workItem.CurrentStateEnteredAt = DeriveCurrentStateEnteredAt(workItem, existingTransitions.Except(removedTransitions).Concat(addedTransitions));

            return WithoutCorrections(addedTransitions, removedTransitions);
        }

        // A row added in the same refresh that drops a row for the same move is that move with its time
        // corrected, not a move the item made. This also takes a genuine second move between the same two
        // states for a correction when the same reading drops a row for that move; the history cannot tell
        // the two apart.
        private static List<WorkItemStateTransition> WithoutCorrections(List<WorkItemStateTransition> addedTransitions, List<WorkItemStateTransition> removedTransitions)
        {
            var notYetCorrected = new List<WorkItemStateTransition>(removedTransitions);
            var moves = new List<WorkItemStateTransition>();

            foreach (var added in addedTransitions)
            {
                var corrected = notYetCorrected.Find(removed => removed.FromState == added.FromState && removed.ToState == added.ToState);
                if (corrected == null)
                {
                    moves.Add(added);
                }
                else
                {
                    notYetCorrected.Remove(corrected);
                }
            }

            return moves;
        }

        private static bool IsTheSameMove(string storedToState, DateTime storedTransitionedAt, WorkItemStateTransition transition)
            => storedToState == transition.ToState && storedTransitionedAt == transition.TransitionedAt;

        // Only a whole history may stand in for what is stored. An empty one never does: a reading that
        // brought back nothing is far more likely to have lost the history than to have found none.
        private static bool MayReplaceTheStoredHistory(IReadOnlyList<WorkItemStateTransition> syncedTransitions, bool transitionsAreItsWholeHistory)
            => transitionsAreItsWholeHistory && syncedTransitions.Count > 0;

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

                // Bug #5567 decision 4: both stay UTC instants. A history window is an offset from now,
                // not a calendar day; an off-by-one only widens the percentile's sample.
                var endDate = clock.Now.UtcDateTime;

                var historyInDays = project.PercentileHistoryInDays is int configuredDays and > 0 ? configuredDays : Portfolio.DefaultPercentileHistoryInDays;
                var startDate = endDate.AddDays(-historyInDays);
                var closedFeatures = portfolioMetricsService.GetCycleTimeDataForPortfolio(project, startDate, endDate);

                var historicalFeatureSize = closedFeatures.Where(f => f.Size > 0).Select(f => f.Size);

                // Stryker disable once all: covers the join separator too — the joined sample exists for this message; the percentile is computed from the sequence, not the string.
                logger.LogDebug("Features had following number of child items: {ChildItems}", string.Join(",", historicalFeatureSize));

                if (historicalFeatureSize.Any())
                {
                    defaultItems = PercentileCalculator.CalculatePercentile(historicalFeatureSize.ToList(), project.DefaultWorkItemPercentile);

                    // Stryker disable once all: reports the value CalculatePercentile just returned; the value is what the caller uses, the sentence is not.
                    logger.LogDebug("{Percentile} Percentile Based on Last {Days} days is {DefaultItems}", project.DefaultWorkItemPercentile, historyInDays, defaultItems);
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

        private async Task<(SyncOutcome Outcome, HashSet<string> Downloaded)> RefreshFeatures(Portfolio portfolio, bool fetchShapeChanged)
        {
            var connector = GetWorkItemServiceForWorkTrackingSystem(portfolio.WorkTrackingSystemConnection.WorkTrackingSystem);

            var downloadedFeatures = new List<Feature>();
            var featuresWithTransitions = new List<FeatureWithTransitions>();
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
                featuresWithTransitions.Add(new FeatureWithTransitions(featureFromDatabase, feature.SyncedTransitions, feature.TransitionsAreItsWholeHistory));

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
            featuresWithTransitions.ForEach(SyncFeatureStateTransitions);

            foreach (var syncedFeature in syncedFeatures)
            {
                events.AddRange(CollectFeatureBlockedEvents(portfolio, syncedFeature));
            }

            await featureStateTransitionRepository.Save();
            await featureRepository.Save();

            await PublishDomainEvents(events);

            await SweepDepartedFeatureSpells(portfolio, featuresTheQueryStillReturns);

            return (fetch.Outcome, fetch.Features.Select(feature => feature.ReferenceId).ToHashSet(StringComparer.Ordinal));
        }

        /// <summary>
        /// What one portfolio refresh has to work with: the Feature payloads it downloaded, every reference
        /// id the query still returns, and what to report to the operator. The second is the one the
        /// portfolio's membership is rebuilt from - never the first. Ordered rather than a set, because
        /// Features tied on their order value fall back to arrival order until the save gives them an id.
        /// </summary>
        private sealed record RemoteFeatureFetch(List<Feature> Features, List<string> StillOnTheTracker, SyncOutcome Outcome);

        private async Task<RemoteFeatureFetch> FetchEveryFeature(IWorkTrackingConnector connector, Portfolio portfolio, CancellationToken stopping)
        {
            var recordsFromTracker = (await connector.GetFeaturesForProject(portfolio, stopping)).ToList();
            var actualFeatures = DeduplicateByReferenceId(recordsFromTracker, portfolio.Name, feature => feature.ReferenceId);

            return new RemoteFeatureFetch(
                actualFeatures,
                actualFeatures.ConvertAll(feature => feature.ReferenceId),
                SyncOutcome.FullSync(recordsFromTracker.Count));
        }

        /// <summary>
        /// The portfolio half of the same decision, mirroring the team path deliberately rather than
        /// sharing it: a Feature is not a Work Item, so the two download different records into different
        /// shapes. It scans unconditionally for the same reason the team path does: a scan that fails or is
        /// refused downloads every Feature.
        /// </summary>
        private async Task<RemoteFeatureFetch> ResolveRemoteFeatureFetch(IWorkTrackingConnector connector, Portfolio portfolio, List<Feature> storedFeatures, bool fetchShapeChanged)
        {
            var scan = await ScanRemoteFeatureIdentities(connector, portfolio);

            var mode = SyncModeResolver.Resolve(
                scan.TrackerCanBeScanned,
                storedFeatures,
                scan.Succeeded,
                fetchShapeChanged);

            var fetch = mode == SyncMode.Delta
                ? await FetchOnlyTheFeaturesThatMoved(connector, portfolio, storedFeatures, scan.Stamps)
                : await FetchEveryFeature(connector, portfolio, Stopping);

            var placements = PlaceWhatTheLinksLeftOpen(portfolio, ParentPlacementReport.Portfolio(portfolio.Name), ParentNarrowing.DoNotNarrow, fetch.Features);
            var findings = ReportWhatTheSettingsLeftUnread($"Portfolio {portfolio.Name}", fetch.Outcome, fetch.Features, setting => TheSettingIsRead(portfolio, setting));

            return fetch with { Outcome = CountingThePlacements(fetch.Outcome, placements) with { Findings = findings } };
        }

        /// <summary>
        /// Every record read through a misconfigured setting shows the same misconfiguration, so it is
        /// reported once per kind and setting for the whole refresh, never once per record. Only a whole-query refresh
        /// judges: a cheap one reads the few records that moved, which says nothing about the setting, and
        /// reporting nothing from it leaves the last whole-query refresh's word standing.
        /// </summary>
        private List<RefreshFinding> ReportWhatTheSettingsLeftUnread(
            string owner, SyncOutcome outcome, IEnumerable<WorkItemBase> records, Func<RefreshFindingSetting, bool> theSettingIsRead)
        {
            if (outcome.Mode != SyncMode.Full)
            {
                return [];
            }

            var findings = records
                .SelectMany(record => record.RefreshFindings)
                .Where(finding => theSettingIsRead(finding.Setting))
                .DistinctBy(finding => (finding.Kind, finding.Setting))
                .ToList();

            foreach (var finding in findings)
            {
                WarnAbout(owner, finding);
            }

            return findings;
        }

        /// <summary>
        /// A Portfolio that ignores dependencies never reads its Dependency Field, so what that field matched
        /// or missed is nothing for its administrator to fix.
        /// </summary>
        private static bool TheSettingIsRead(Portfolio portfolio, RefreshFindingSetting setting)
            => !(portfolio.IgnoreDependencies && setting == RefreshFindingSetting.DependencyField);

        private void WarnAbout(string owner, RefreshFinding finding)
        {
            var setting = TheNameAnAdministratorKnows(finding.Setting);

            if (finding.Kind == RefreshFindingKind.NamedLinkPointsBothWays)
            {
                logger.LogWarning(
                    "{Owner}: its {Setting} names '{Named}', which points both ways. Name one end: {Ends}.",
                    owner,
                    setting,
                    finding.Named,
                    string.Join(" or ", finding.Seen.Select(seen => $"'{seen}'")));
                return;
            }

            if (finding.Kind == RefreshFindingKind.NamedLinkMatchedNothing)
            {
                logger.LogWarning(
                    "{Owner}: its {Setting} names '{Named}', and no Feature carries that link. Links seen: {LinksSeen}.",
                    owner,
                    setting,
                    finding.Named,
                    string.Join(", ", finding.Seen));
                return;
            }

            if (finding.Kind == RefreshFindingKind.RelationTypeCannotBeReadHere)
            {
                logger.LogWarning(
                    "{Owner}: its {Setting} names '{Named}', an Azure DevOps relation type, which only the Dependency Field reads, so it was read as if nothing were named.",
                    owner,
                    setting,
                    finding.Named);
                return;
            }

            if (finding.Kind == RefreshFindingKind.RelationTypeHasNoDirection)
            {
                logger.LogWarning(
                    "{Owner}: its {Setting} names '{Named}', which has no direction. Name a relation type with two ends, such as 'System.LinkTypes.Dependency-Reverse'.",
                    owner,
                    setting,
                    finding.Named);
                return;
            }

            if (finding.Kind == RefreshFindingKind.RelationTypeNamedWithoutItsEnd)
            {
                logger.LogWarning(
                    "{Owner}: its {Setting} names '{Named}' without its end. Name one end: '{ForwardEnd}' or '{ReverseEnd}'.",
                    owner,
                    setting,
                    finding.Named,
                    $"{finding.Named}-Forward",
                    $"{finding.Named}-Reverse");
                return;
            }

            logger.LogWarning(
                "{Owner}: its {Setting} names '{Named}', which the refresh could not read as configured. What it saw: {Seen}",
                owner,
                setting,
                finding.Named,
                string.Join(", ", finding.Seen.Select(seen => $"'{seen}'")));
        }

        // The settings are named after the screen's labels, so spacing the words is the label.
        private static string TheNameAnAdministratorKnows(RefreshFindingSetting setting)
            => string.Concat(setting.ToString().Select((letter, at) => at > 0 && char.IsUpper(letter) ? $" {letter}" : $"{letter}"));

        /// <summary>The scan for the portfolio: the same Feature query, asking only for identity plus the remote change stamp.</summary>
        private async Task<IdentityScan> ScanRemoteFeatureIdentities(IWorkTrackingConnector connector, Portfolio portfolio)
        {
            if (!connector.SupportsIncrementalSync(portfolio.WorkTrackingSystemConnection))
            {
                return new IdentityScan(TrackerCanBeScanned: false, Succeeded: false, Stamps: []);
            }

            try
            {
                return new IdentityScan(TrackerCanBeScanned: true, Succeeded: true, Stamps: [.. await connector.SweepFeaturesForPortfolio(portfolio, Stopping)]);
            }
            // A half-scanned query is the one answer never allowed, so any scan failure falls back to the
            // whole query - loudly, or nobody learns the cheap path stopped working.
            // A cancel is not a tracker failure. Caught below it is logged as a scan that broke, and the
            // refresh is then escalated to a full download - spending the rate limit the operator cancelled
            // to protect, and telling them the cheap path stopped working when in fact they stopped it.
            catch (OperationCanceledException)
            {
                throw;
            }
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
                : DeduplicateByReferenceId((await connector.GetFeaturesForProject(portfolio, movedReferenceIds, Stopping)).ToList(), portfolio.Name, feature => feature.ReferenceId);

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

        private sealed record FeatureWithTransitions(Feature PersistedFeature, IReadOnlyList<WorkItemStateTransition> SyncedTransitions, bool TransitionsAreItsWholeHistory);

        private void SyncFeatureStateTransitions(FeatureWithTransitions featureWithTransitions)
        {
            var feature = featureWithTransitions.PersistedFeature;
            var syncedTransitions = featureWithTransitions.SyncedTransitions;

            var existingTransitions = featureStateTransitionRepository
                .GetAllByPredicate(transition => transition.FeatureId == feature.Id)
                .ToList();

            var addedTransitions = syncedTransitions
                .Where(transition => !existingTransitions.Exists(stored => IsTheSameMove(stored.ToState, stored.TransitionedAt, transition)))
                .Select(transition => new FeatureStateTransition
                {
                    FeatureId = feature.Id,
                    FromState = transition.FromState,
                    ToState = transition.ToState,
                    TransitionedAt = transition.TransitionedAt,
                })
                .ToList();

            var removedTransitions = MayReplaceTheStoredHistory(syncedTransitions, featureWithTransitions.TransitionsAreItsWholeHistory)
                ? existingTransitions.FindAll(stored => !syncedTransitions.Any(transition => IsTheSameMove(stored.ToState, stored.TransitionedAt, transition)))
                : [];

            addedTransitions.ForEach(featureStateTransitionRepository.Add);
            removedTransitions.ForEach(featureStateTransitionRepository.Remove);

            feature.CurrentStateEnteredAt = DeriveCurrentStateEnteredAt(feature, existingTransitions.Except(removedTransitions).Concat(addedTransitions));
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

        /// <summary>
        /// A Portfolio Feature can also be the issue above another of its Features. When the Feature half has
        /// already downloaded it in this refresh, the sweep does not ask for it again: a second reading would
        /// place it a second time, against the Features this refresh has just stored, and could save another
        /// parent over the one the Portfolio's counted line names.
        /// </summary>
        private async Task RefreshParentFeatures(Portfolio project, bool fetchShapeChanged, HashSet<string> downloadedByTheFeatureHalf)
        {
            var workItemService = GetWorkItemServiceForWorkTrackingSystem(project.WorkTrackingSystemConnection.WorkTrackingSystem);
            var parentFeatureIds = project.Features
                .Where(f => !string.IsNullOrEmpty(f.ParentReferenceId))
                .Select(f => f.ParentReferenceId)
                .Distinct()
                .Where(parentFeatureId => !downloadedByTheFeatureHalf.Contains(parentFeatureId))
                .ToList();

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
        /// <c>portfolio.Features</c> and so are resolved from storage by reference id. A parent whose links
        /// name several issues is placed among them the way a Portfolio's Features are, and named on a line
        /// of its own, but the parent sweep stays out of <see cref="SyncOutcome"/>: the Portfolio's row
        /// counts the Portfolio's own Features. A parent the tracker sends twice is placed and stored once.
        /// The fetch shape is the PORTFOLIO's own answer, passed down rather than compared a second time.
        /// </summary>
        private async Task<List<Feature>> ResolveRemoteParentFeatureFetch(IWorkTrackingConnector connector, Portfolio portfolio, List<string> parentFeatureIds, bool fetchShapeChanged)
        {
            var storedParentFeatures = TheStoredParentFeatures(parentFeatureIds);
            var scan = await ScanRemoteParentFeatureIdentities(connector, portfolio, parentFeatureIds);

            var mode = SyncModeResolver.Resolve(
                scan.TrackerCanBeScanned,
                storedParentFeatures,
                scan.Succeeded,
                fetchShapeChanged);

            var parentFeatures = mode == SyncMode.Delta
                ? await FetchOnlyTheParentFeaturesThatMoved(connector, portfolio, parentFeatureIds, storedParentFeatures, scan.Stamps)
                : DeduplicateByReferenceId(await connector.GetParentFeaturesDetails(portfolio, parentFeatureIds, Stopping), portfolio.Name, feature => feature.ReferenceId);

            _ = PlaceWhatTheLinksLeftOpen(portfolio, ParentPlacementReport.ParentsOfThePortfoliosFeatures(portfolio.Name), ParentNarrowing.DoNotNarrow, parentFeatures);

            return parentFeatures;
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
                return new IdentityScan(TrackerCanBeScanned: true, Succeeded: true, Stamps: [.. await connector.SweepParentFeatures(portfolio, parentFeatureIds, Stopping)]);
            }
            // Same rule again: a half-scanned key list falls back to downloading every parent, and says so.
            // A cancel is not a tracker failure. Caught below it is logged as a scan that broke, and the
            // refresh is then escalated to a full download - spending the rate limit the operator cancelled
            // to protect, and telling them the cheap path stopped working when in fact they stopped it.
            catch (OperationCanceledException)
            {
                throw;
            }
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
                : DeduplicateByReferenceId(await connector.GetParentFeaturesDetails(portfolio, keysToDownload, Stopping), portfolio.Name, feature => feature.ReferenceId);
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
