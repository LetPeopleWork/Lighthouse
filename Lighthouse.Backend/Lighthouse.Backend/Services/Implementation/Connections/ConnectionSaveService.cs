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

        private static readonly UpdatesQueued NoUpdatesQueued = new(0, 0);

        public Task<ConnectionSaveHandle?> Open(int connectionId, CancellationToken cancellationToken)
        {
            var connection = connectionRepository.GetById(connectionId);
            var handle = connection is null
                ? null
                : new ConnectionSaveHandle(connection)
                {
                    DefaultsBeforeSave = ConnectionFieldDefaults.All.ToDictionary(
                        fieldDefault => fieldDefault.Setting, fieldDefault => DefaultOf(connection, fieldDefault.FieldOn(connection))),
                    StoredFieldsBeforeSave = connection.AdditionalFieldDefinitions.ToDictionary(field => field.Id, field => field.DisplayName),
                    TeamFetchesBeforeSave = WhatEachTeamFetchesOn(connectionId),
                    PortfolioFetchesBeforeSave = WhatEachPortfolioFetchesOn(connectionId),
                };
            return Task.FromResult(handle);
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
                LogTheChangedDefault(handle, ChangeOf(handle, fieldDefault), updatesQueued.For(fieldDefault.HeldFor));
            }

            return Saved;
        }

        // Read from the database rather than from what this request has loaded, so a Team or Portfolio saved
        // with the field after this save was opened still blocks its removal. A setting that follows a default
        // holds no value of its own, so only the owners that chose the field themselves are found here.
        private List<AdditionalFieldInUse> FieldsStillInUseAmong(List<RemovedAdditionalField> removed, WorkTrackingSystemConnection connection)
        {
            if (removed.Count == 0)
            {
                return [];
            }

            var removedIds = removed.Select(field => (int?)field.Id).ToList();
            var terms = OwnerTerms.From(terminologyService.GetAll());
            var defaults = ConnectionFieldDefaults.All.Select(fieldDefault => new DefaultNamingAField(
                $"{OwnerTerms.PossessiveOfPlural(terms.PluralFor(fieldDefault.HeldFor))} {fieldDefault.FieldName}", fieldDefault.FieldOn(connection)));
            return AdditionalFieldInUse.AmongTheRemoved(
                removed, defaults, [.. TeamsOwningAnyOf(removedIds, connection.Id), .. PortfoliosOwningAnyOf(removedIds, connection.Id)]);
        }

        private List<OwnerNamingFields> TeamsOwningAnyOf(List<int?> fieldIds, int connectionId)
            => [.. teamRepository.GetAllByPredicate(team => team.WorkTrackingSystemConnectionId == connectionId
                    && (fieldIds.Contains(team.OwnParentOverrideAdditionalFieldDefinitionId)
                        || fieldIds.Contains(team.EstimationAdditionalFieldDefinitionId)))
                .OrderBy(team => team.Id)
                .Select(team => new { team.Name, team.OwnParentOverrideAdditionalFieldDefinitionId, team.EstimationAdditionalFieldDefinitionId })
                .AsEnumerable()
                .Select(team => new OwnerNamingFields(team.Name, [team.OwnParentOverrideAdditionalFieldDefinitionId, team.EstimationAdditionalFieldDefinitionId]))];

        private List<OwnerNamingFields> PortfoliosOwningAnyOf(List<int?> fieldIds, int connectionId)
            => [.. portfolioRepository.GetAllByPredicate(portfolio => portfolio.WorkTrackingSystemConnectionId == connectionId
                    && (fieldIds.Contains(portfolio.OwnParentOverrideAdditionalFieldDefinitionId)
                        || fieldIds.Contains(portfolio.OwnDependencyOverrideAdditionalFieldDefinitionId)
                        || fieldIds.Contains(portfolio.SizeEstimateAdditionalFieldDefinitionId)
                        || fieldIds.Contains(portfolio.FeatureOwnerAdditionalFieldDefinitionId)
                        || fieldIds.Contains(portfolio.EstimationAdditionalFieldDefinitionId)))
                .OrderBy(portfolio => portfolio.Id)
                .Select(portfolio => new
                {
                    portfolio.Name,
                    portfolio.OwnParentOverrideAdditionalFieldDefinitionId,
                    portfolio.OwnDependencyOverrideAdditionalFieldDefinitionId,
                    portfolio.SizeEstimateAdditionalFieldDefinitionId,
                    portfolio.FeatureOwnerAdditionalFieldDefinitionId,
                    portfolio.EstimationAdditionalFieldDefinitionId,
                })
                .AsEnumerable()
                .Select(portfolio => new OwnerNamingFields(portfolio.Name, [
                    portfolio.OwnParentOverrideAdditionalFieldDefinitionId,
                    portfolio.OwnDependencyOverrideAdditionalFieldDefinitionId,
                    portfolio.SizeEstimateAdditionalFieldDefinitionId,
                    portfolio.FeatureOwnerAdditionalFieldDefinitionId,
                    portfolio.EstimationAdditionalFieldDefinitionId,
                ]))];

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
            if (!startUpdates)
            {
                return NoUpdatesQueued;
            }

            try
            {
                var fetchShapeChange = FetchShapeChangeForTheOwnersWhoseFetchMoved(handle);
                if (fetchShapeChange == null)
                {
                    return NoUpdatesQueued;
                }

                await domainEventDispatcher.PublishAsync(fetchShapeChange, cancellationToken);
                return new(fetchShapeChange.TeamIds.Count, fetchShapeChange.PortfolioIds.Count);
            }
#pragma warning disable CA1031 // The save is already stored: failing the request would tell the admin it was not and invite a second save that changes nothing, and every Team and Portfolio whose fetch moved still downloads everything at its next scheduled update.
            catch (Exception exception)
#pragma warning restore CA1031
            {
                logger.LogError(exception, "Connection {ConnectionId} was saved, but its Teams and Portfolios could not be told to update now; they will pick up the change at their next scheduled update", handle.ConnectionId);
                return NoUpdatesQueued;
            }
        }

        private void LogTheChangedDefault(ConnectionSaveHandle handle, ChangedConnectionDefault changed, int updatesQueued)
        {
            var connection = handle.Connection;
            if (changed.FieldIdAfter == changed.Before.FieldId)
            {
                return;
            }

            try
            {
                logger.LogInformation(
                    "Connection {ConnectionName} changed the {Default:l} default from {PreviousField} to {NewField}; {Followers:l}; {Queued:l}.",
                    connection.Name,
                    changed.Name,
                    changed.Before.FieldName ?? NoField,
                    connection.DisplayNameOfField(changed.FieldIdAfter) ?? NoField,
                    WhoFollows(changed.FollowersOf(connection), changed.Owners),
                    HowManyQueued(updatesQueued));
            }
#pragma warning disable CA1031 // The save is already stored: failing the request over its log entry would tell the admin it was not.
            catch (Exception exception)
#pragma warning restore CA1031
            {
                logger.LogError(exception, "Connection {ConnectionId} was saved, but the change to its {Owners:l}' default could not be logged", handle.ConnectionId, changed.Owners.Many);
            }
        }

        private static NamedField DefaultOf(WorkTrackingSystemConnection connection, int? fieldId)
            => new(fieldId, connection.DisplayNameOfField(fieldId));

        private static OwnerWords WordsFor(DefaultHeldFor heldFor) => heldFor == DefaultHeldFor.Teams ? TeamWords : PortfolioWords;

        private static string NameInTheError(ConnectionFieldDefault fieldDefault) => $"{WordsFor(fieldDefault.HeldFor).Many}' default {fieldDefault.FieldName}";

        private ChangedConnectionDefault ChangeOf(ConnectionSaveHandle handle, ConnectionFieldDefault fieldDefault)
        {
            var owners = WordsFor(fieldDefault.HeldFor);
            return new(
                $"{owners.Many}' {fieldDefault.FieldName}",
                handle.DefaultBeforeSave(fieldDefault.Setting),
                fieldDefault.FieldOn(handle.Connection),
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

        // Kept synchronous so the architecture test sees this service build the event: in a Release build an
        // async method's body moves into a struct that the test cannot attribute to its owner.
        private ConnectionFetchShapeChanged? FetchShapeChangeForTheOwnersWhoseFetchMoved(ConnectionSaveHandle handle)
        {
            var teamIds = OwnersWhoseFetchMoved.Between(handle.TeamFetchesBeforeSave, WhatEachTeamFetchesOn(handle.ConnectionId));
            var portfolioIds = OwnersWhoseFetchMoved.Between(handle.PortfolioFetchesBeforeSave, WhatEachPortfolioFetchesOn(handle.ConnectionId));

            if (teamIds.Count == 0 && portfolioIds.Count == 0)
            {
                return null;
            }

            return new ConnectionFetchShapeChanged(handle.ConnectionId, teamIds, portfolioIds);
        }

        // Read untracked, with the connection as it is stored: before the save, the edit this request is making
        // must not show yet, and after it, what counts is what the database now holds.
        private Dictionary<int, string> WhatEachTeamFetchesOn(int connectionId)
            => teamRepository.GetAllByPredicate(team => team.WorkTrackingSystemConnectionId == connectionId)
                .AsNoTracking()
                .Include(team => team.WorkTrackingSystemConnection.AdditionalFieldDefinitions)
                .AsEnumerable()
                .ToDictionary(team => team.Id, team => FetchFingerprint.For(team));

        private Dictionary<int, string> WhatEachPortfolioFetchesOn(int connectionId)
            => portfolioRepository.GetAllByPredicate(portfolio => portfolio.WorkTrackingSystemConnectionId == connectionId)
                .AsNoTracking()
                .Include(portfolio => portfolio.WorkTrackingSystemConnection.AdditionalFieldDefinitions)
                .AsEnumerable()
                .ToDictionary(portfolio => portfolio.Id, portfolio => FetchFingerprint.For(portfolio));
    }
}
