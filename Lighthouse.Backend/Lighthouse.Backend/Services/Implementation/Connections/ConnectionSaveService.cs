using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Connections
{
    public class ConnectionSaveService(
        IRepository<WorkTrackingSystemConnection> connectionRepository,
        IRepository<Team> teamRepository,
        IRepository<Portfolio> portfolioRepository,
        IDomainEventDispatcher domainEventDispatcher,
        ITerminologyService terminologyService,
        ILogger<ConnectionSaveService> logger)
        : IConnectionSaveService
    {
        private const string NoField = "none";

        private static readonly OwnerWords TeamWords = new("Team", "Teams");

        private static readonly OwnerWords PortfolioWords = new("Portfolio", "Portfolios");

        private static readonly ConnectionSaveOutcome Saved = new([]);

        public Task<ConnectionSaveHandle?> Open(int connectionId, CancellationToken cancellationToken)
        {
            var connection = connectionRepository.GetById(connectionId);
            return Task.FromResult(connection is null ? null : HandleFor(connection));
        }

        private ConnectionSaveHandle HandleFor(WorkTrackingSystemConnection connection)
        {
            var teams = TeamsAsStoredOn(connection.Id);
            var portfolios = PortfoliosAsStoredOn(connection.Id);
            return new ConnectionSaveHandle(connection)
            {
                DefaultsBeforeSave = ConnectionFieldDefaults.All.ToDictionary(
                    fieldDefault => fieldDefault.Setting, fieldDefault => HeldOn(connection, fieldDefault)),
                StoredFieldsBeforeSave = connection.AdditionalFieldDefinitions.ToDictionary(field => field.Id, field => field.DisplayName),
                TeamFetchesBeforeSave = WhatEachFetches(teams),
                PortfolioFetchesBeforeSave = WhatEachFetches(portfolios),
                TeamParentSourcesBeforeSave = WhatEachReadsItsParentFrom(teams),
                PortfolioParentSourcesBeforeSave = WhatEachReadsItsParentFrom(portfolios),
            };
        }

        public async Task<ConnectionSaveOutcome> Commit(ConnectionSaveHandle handle, Guid? concurrencyToken, bool startUpdates, CancellationToken cancellationToken)
        {
            var connection = handle.Connection;
            var fieldsInUse = FieldsStillInUseAmong(handle.RemovedFields(), connection);
            if (fieldsInUse.Count > 0)
            {
                return new ConnectionSaveOutcome(fieldsInUse);
            }

            connectionRepository.Update(connection);

            if (concurrencyToken.HasValue)
            {
                connectionRepository.ApplyConcurrencyTokenForEdit(connection, concurrencyToken.Value);
            }

            await connectionRepository.Save();
            await PointTheDefaultsAtTheFieldsAddedInThisSave(handle);
            var updatesQueued = await StartUpdatesForTheOwnersWhoseFetchMoved(handle, startUpdates, cancellationToken);
            foreach (var fieldDefault in ConnectionFieldDefaults.All)
            {
                LogTheChangedDefault(handle, ChangeOf(handle, fieldDefault), updatesQueued.For(fieldDefault.HeldFor), WhatTheListMoved(fieldDefault, updatesQueued.ParentSourcesMoved));
            }

            return Saved;
        }

        // Read from the database rather than from what this request has loaded, so a Team or Portfolio saved
        // with the field after this save was opened still blocks its removal. A setting that follows a default
        // holds no value of its own, so only the owners that chose the field themselves are found here. A list
        // is stored as text and the old column holds only its first field, so neither can be filtered on: every
        // owner of the connection is read, and the removed fields are matched against them in memory.
        private List<AdditionalFieldInUse> FieldsStillInUseAmong(List<RemovedAdditionalField> removed, WorkTrackingSystemConnection connection)
        {
            if (removed.Count == 0)
            {
                return [];
            }

            var terms = OwnerTerms.From(terminologyService.GetAll());
            var defaults = ConnectionFieldDefaults.All.Select(fieldDefault => new DefaultNamingAField(
                $"{OwnerTerms.PossessiveOfPlural(terms.PluralFor(fieldDefault.HeldFor))} {fieldDefault.NameIn(terms)}", fieldDefault.FieldOn(connection)));
            return AdditionalFieldInUse.AmongTheRemoved(
                removed, defaults, [.. WhatEachTeamOwnsOn(connection.Id), .. WhatEachPortfolioOwnsOn(connection.Id)]);
        }

        private List<OwnerNamingFields> WhatEachTeamOwnsOn(int connectionId)
            => [.. teamRepository.GetAllByPredicate(team => team.WorkTrackingSystemConnectionId == connectionId)
                .OrderBy(team => team.Id)
                .Select(team => new { team.Name, team.ParentSources, team.OwnEstimationAdditionalFieldDefinitionId })
                .AsEnumerable()
                .Select(team => new OwnerNamingFields(team.Name, [.. FieldsListedIn(team.ParentSources), team.OwnEstimationAdditionalFieldDefinitionId]))];

        private List<OwnerNamingFields> WhatEachPortfolioOwnsOn(int connectionId)
            => [.. portfolioRepository.GetAllByPredicate(portfolio => portfolio.WorkTrackingSystemConnectionId == connectionId)
                .OrderBy(portfolio => portfolio.Id)
                .Select(portfolio => new
                {
                    portfolio.Name,
                    portfolio.ParentSources,
                    portfolio.OwnDependencyOverrideAdditionalFieldDefinitionId,
                    portfolio.OwnSizeEstimateAdditionalFieldDefinitionId,
                    portfolio.OwnFeatureOwnerAdditionalFieldDefinitionId,
                    portfolio.OwnEstimationAdditionalFieldDefinitionId,
                })
                .AsEnumerable()
                .Select(portfolio => new OwnerNamingFields(portfolio.Name, [
                    .. FieldsListedIn(portfolio.ParentSources),
                    portfolio.OwnDependencyOverrideAdditionalFieldDefinitionId,
                    portfolio.OwnSizeEstimateAdditionalFieldDefinitionId,
                    portfolio.OwnFeatureOwnerAdditionalFieldDefinitionId,
                    portfolio.OwnEstimationAdditionalFieldDefinitionId,
                ]))];

        private static IEnumerable<int?> FieldsListedIn(ParentSources? list)
            => list?.Entries.Select(entry => entry.AdditionalFieldDefinitionId) ?? [];

        private async Task PointTheDefaultsAtTheFieldsAddedInThisSave(ConnectionSaveHandle handle)
        {
            List<PendingDefault> pending = [.. ConnectionFieldDefaults.All
                .Where(fieldDefault => handle.DefaultsAddedInThisSave.ContainsKey(fieldDefault.Setting))
                .Select(fieldDefault => new PendingDefault(fieldDefault, handle.DefaultsAddedInThisSave[fieldDefault.Setting]))];
            if (pending.Count == 0)
            {
                return;
            }

            foreach (var each in pending)
            {
                each.Default.PointAt(handle.Connection, each.Field.Id);
            }

            try
            {
                await connectionRepository.Save();
            }
            catch (Exception exception)
            {
                // The new field is already stored, but the default could not follow it. Without saying so the
                // failed save reads as if nothing was stored, and nobody can tell the owners now follow no field.
                // The host logs the failure as an error, so it is not logged here as well.
                throw new InvalidOperationException(WhatCouldNotFollowItsNewField(handle.Connection.Name, pending), exception);
            }
        }

        private static string WhatCouldNotFollowItsNewField(string connectionName, List<PendingDefault> pending)
        {
            if (pending is [var only])
            {
                return $"Connection {connectionName} was saved with its new field {only.Field.DisplayName}, but the {NameInTheError(only.Default)} could not be set to it and is now {NoField}";
            }

            var fields = string.Join(" and ", pending.Select(each => each.Field.DisplayName).Distinct());
            var defaults = string.Join(" and ", pending.Select(each => $"the {NameInTheError(each.Default)} could not be set to {each.Field.DisplayName}"));
            return $"Connection {connectionName} was saved with its new fields {fields}, but {defaults}; each is now {NoField}";
        }

        private async Task<UpdatesQueued> StartUpdatesForTheOwnersWhoseFetchMoved(ConnectionSaveHandle handle, bool startUpdates, CancellationToken cancellationToken)
        {
            var parentSourcesMoved = ParentSourcesMoved.NotKnown;

            try
            {
                var moved = OwnersTheSaveMoved(handle);
                parentSourcesMoved = moved.ParentSources;

                var fetchShapeChange = startUpdates ? FetchShapeChangeFor(handle, moved) : null;
                if (fetchShapeChange == null)
                {
                    return new(0, 0, parentSourcesMoved);
                }

                await domainEventDispatcher.PublishAsync(fetchShapeChange, cancellationToken);
                return new(fetchShapeChange.TeamIds.Count, fetchShapeChange.PortfolioIds.Count, parentSourcesMoved);
            }
#pragma warning disable CA1031 // The save is already stored: failing the request would tell the admin it was not and invite a second save that changes nothing, and every Team and Portfolio whose fetch moved still downloads everything at its next scheduled update.
            catch (Exception exception)
#pragma warning restore CA1031
            {
                logger.LogError(exception, "Connection {ConnectionId} was saved, but its Teams and Portfolios could not be checked or told to update now; they will pick up the change at their next scheduled update", handle.ConnectionId);
                return new(0, 0, parentSourcesMoved);
            }
        }

        private void LogTheChangedDefault(ConnectionSaveHandle handle, ChangedConnectionDefault changed, int updatesQueued, string parentSourcesMoved)
        {
            var connection = handle.Connection;
            if (changed.After.IsTheSameDefaultAs(changed.Before))
            {
                return;
            }

            try
            {
                logger.LogInformation(
                    "Connection {ConnectionName} changed the {Default:l} default from {PreviousField} to {NewField}; {Followers:l}; {Queued:l}{ParentSourcesMoved:l}.",
                    connection.Name,
                    changed.Name,
                    changed.Before.Description ?? NoField,
                    changed.After.Description ?? NoField,
                    WhoFollows(changed.FollowersOf(connection), changed.Owners),
                    HowManyQueued(updatesQueued),
                    parentSourcesMoved);
            }
#pragma warning disable CA1031 // The save is already stored: failing the request over its log entry would tell the admin it was not.
            catch (Exception exception)
#pragma warning restore CA1031
            {
                logger.LogError(exception, "Connection {ConnectionId} was saved, but the change to its {Owners:l}' default could not be logged", handle.ConnectionId, changed.Owners.Many);
            }
        }

        private static NamedField HeldOn(WorkTrackingSystemConnection connection, ConnectionFieldDefault fieldDefault)
        {
            var fieldId = fieldDefault.FieldOn(connection);
            return new(fieldId, connection.DisplayNameOfField(fieldId), fieldDefault.BundleOn?.Invoke(connection));
        }

        private static OwnerWords WordsFor(DefaultHeldFor heldFor) => heldFor == DefaultHeldFor.Teams ? TeamWords : PortfolioWords;

        private static string NameInTheError(ConnectionFieldDefault fieldDefault) => $"{WordsFor(fieldDefault.HeldFor).Many}' default {fieldDefault.FieldName}";

        private ChangedConnectionDefault ChangeOf(ConnectionSaveHandle handle, ConnectionFieldDefault fieldDefault)
        {
            var owners = WordsFor(fieldDefault.HeldFor);
            return new(
                $"{owners.Many}' {fieldDefault.FieldName}",
                handle.DefaultBeforeSave(fieldDefault.Setting),
                HeldOn(handle.Connection, fieldDefault),
                owners,
                fieldDefault.HeldFor == DefaultHeldFor.Teams
                    ? FollowersIn(teamRepository, fieldDefault.Setting)
                    : FollowersIn(portfolioRepository, fieldDefault.Setting));
        }

        // In the order the owners were created, which is the order an admin knows them by on their list.
        private static Func<WorkTrackingSystemConnection, List<string>> FollowersIn<TOwner>(IRepository<TOwner> repository, ConnectionDefaultSetting setting)
            where TOwner : WorkTrackingSystemOptionsOwner
            => connection => [.. repository.GetAllByPredicate(owner => owner.WorkTrackingSystemConnectionId == connection.Id)
                .AsEnumerable()
                .Where(owner => owner.FollowsConnectionDefault(setting))
                .OrderBy(owner => owner.Id)
                .Select(owner => owner.Name)];

        private static string WhoFollows(List<string> followers, OwnerWords owners) => followers.Count switch
        {
            0 => $"no {owners.One} follows it",
            1 => $"1 {owners.One} follows it: {followers[0]}",
            _ => $"{followers.Count} {owners.Many} follow it: {string.Join(", ", followers)}",
        };

        private static string HowManyQueued(int updatesQueued) => updatesQueued switch
        {
            0 => "no update queued",
            1 => "1 update queued",
            _ => $"{updatesQueued} updates queued",
        };

        // Only the parent default can move a list on a connection save, because an owner's own list is saved with
        // the owner, so the other defaults' entries say nothing about it.
        private static string WhatTheListMoved(ConnectionFieldDefault fieldDefault, ParentSourcesMoved moved)
        {
            if (fieldDefault.Setting is not (ConnectionDefaultSetting.ParentOverrideForTeams or ConnectionDefaultSetting.ParentOverrideForPortfolios))
            {
                return string.Empty;
            }

            var owners = WordsFor(fieldDefault.HeldFor);
            return moved.For(fieldDefault.HeldFor) switch
            {
                null => $"; parent sources moved an unknown number of {owners.Many}",
                0 => $"; parent sources moved no {owners.One}",
                1 => $"; parent sources moved 1 {owners.One}",
                var count => $"; parent sources moved {count} {owners.Many}",
            };
        }

        // Kept synchronous so the architecture test sees this service build the event: in a Release build an
        // async method's body moves into a struct that the test cannot attribute to its owner.
        private static ConnectionFetchShapeChanged? FetchShapeChangeFor(ConnectionSaveHandle handle, OwnersMoved moved)
            => moved.TeamFetches.Count == 0 && moved.PortfolioFetches.Count == 0
                ? null
                : new ConnectionFetchShapeChanged(handle.ConnectionId, moved.TeamFetches, moved.PortfolioFetches);

        private OwnersMoved OwnersTheSaveMoved(ConnectionSaveHandle handle)
        {
            var teams = TeamsAsStoredOn(handle.ConnectionId);
            var portfolios = PortfoliosAsStoredOn(handle.ConnectionId);
            return new(
                OwnersWhoseFetchMoved.Between(handle.TeamFetchesBeforeSave, WhatEachFetches(teams)),
                OwnersWhoseFetchMoved.Between(handle.PortfolioFetchesBeforeSave, WhatEachFetches(portfolios)),
                new ParentSourcesMoved(
                    OwnersWhoseFetchMoved.Between(handle.TeamParentSourcesBeforeSave, WhatEachReadsItsParentFrom(teams)).Count,
                    OwnersWhoseFetchMoved.Between(handle.PortfolioParentSourcesBeforeSave, WhatEachReadsItsParentFrom(portfolios)).Count));
        }

        // Read untracked, with the connection as it is stored: before the save, the edit this request is making
        // must not show yet, and after it, what counts is what the database now holds.
        private List<Team> TeamsAsStoredOn(int connectionId)
            => [.. teamRepository.GetAllByPredicate(team => team.WorkTrackingSystemConnectionId == connectionId)
                .AsNoTracking()
                .Include(team => team.WorkTrackingSystemConnection.AdditionalFieldDefinitions)];

        private List<Portfolio> PortfoliosAsStoredOn(int connectionId)
            => [.. portfolioRepository.GetAllByPredicate(portfolio => portfolio.WorkTrackingSystemConnectionId == connectionId)
                .AsNoTracking()
                .Include(portfolio => portfolio.WorkTrackingSystemConnection.AdditionalFieldDefinitions)];

        private static Dictionary<int, string> WhatEachFetches<TOwner>(List<TOwner> owners)
            where TOwner : WorkTrackingSystemOptionsOwner
            => owners.ToDictionary(owner => owner.Id, owner => FetchFingerprint.For(owner));

        private static Dictionary<int, string> WhatEachReadsItsParentFrom<TOwner>(List<TOwner> owners)
            where TOwner : WorkTrackingSystemOptionsOwner
            => owners.ToDictionary(owner => owner.Id, owner => FetchFingerprint.RenderParentSources(owner.TheEffectiveParentSources()));
    }
}
