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
        ILogger<ConnectionSaveService> logger)
        : IConnectionSaveService
    {
        private const string NoField = "none";

        private const string TeamsParentOverrideDefault = "Teams' Parent Override Field";

        private static readonly ConnectionSaveOutcome Saved = new([]);

        public Task<ConnectionSaveHandle?> Open(int connectionId, CancellationToken cancellationToken)
        {
            var connection = connectionRepository.GetById(connectionId);
            var handle = connection is null
                ? null
                : new ConnectionSaveHandle(connection)
                {
                    TeamsParentOverrideDefaultBeforeSave = TeamsParentOverrideDefaultOf(connection),
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
            await PointTheDefaultAtTheFieldAddedInThisSave(handle);
            var updatesQueued = await StartUpdatesForTheOwnersWhoseFetchMoved(handle, startUpdates, cancellationToken);
            LogTheChangedDefault(handle, updatesQueued);

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
            DefaultNamingAField[] defaults = [new(TeamsParentOverrideDefault, connection.ParentOverrideForTeams)];
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
                        || fieldIds.Contains(portfolio.DependencyOverrideAdditionalFieldDefinitionId)
                        || fieldIds.Contains(portfolio.SizeEstimateAdditionalFieldDefinitionId)
                        || fieldIds.Contains(portfolio.FeatureOwnerAdditionalFieldDefinitionId)
                        || fieldIds.Contains(portfolio.EstimationAdditionalFieldDefinitionId)))
                .OrderBy(portfolio => portfolio.Id)
                .Select(portfolio => new
                {
                    portfolio.Name,
                    portfolio.OwnParentOverrideAdditionalFieldDefinitionId,
                    portfolio.DependencyOverrideAdditionalFieldDefinitionId,
                    portfolio.SizeEstimateAdditionalFieldDefinitionId,
                    portfolio.FeatureOwnerAdditionalFieldDefinitionId,
                    portfolio.EstimationAdditionalFieldDefinitionId,
                })
                .AsEnumerable()
                .Select(portfolio => new OwnerNamingFields(portfolio.Name, [
                    portfolio.OwnParentOverrideAdditionalFieldDefinitionId,
                    portfolio.DependencyOverrideAdditionalFieldDefinitionId,
                    portfolio.SizeEstimateAdditionalFieldDefinitionId,
                    portfolio.FeatureOwnerAdditionalFieldDefinitionId,
                    portfolio.EstimationAdditionalFieldDefinitionId,
                ]))];

        private async Task PointTheDefaultAtTheFieldAddedInThisSave(ConnectionSaveHandle handle)
        {
            if (handle.TeamsParentOverrideDefaultAddedInThisSave is not { } addedField)
            {
                return;
            }

            handle.Connection.ParentOverrideForTeams = addedField.Id;
            try
            {
                await connectionRepository.Save();
            }
            catch (Exception exception)
            {
                // The new field is already stored, but the default could not follow it. Without saying so the
                // failed save reads as if nothing was stored, and nobody can tell the Teams now follow no field.
                // The host logs the failure as an error, so it is not logged here as well.
                throw new InvalidOperationException(
                    $"Connection {handle.Connection.Name} was saved with its new field {addedField.DisplayName}, but the Teams' default Parent Override Field could not be set to it and is now {NoField}",
                    exception);
            }
        }

        private async Task<int> StartUpdatesForTheOwnersWhoseFetchMoved(ConnectionSaveHandle handle, bool startUpdates, CancellationToken cancellationToken)
        {
            if (!startUpdates)
            {
                return 0;
            }

            try
            {
                var fetchShapeChange = FetchShapeChangeForTheOwnersWhoseFetchMoved(handle);
                if (fetchShapeChange == null)
                {
                    return 0;
                }

                await domainEventDispatcher.PublishAsync(fetchShapeChange, cancellationToken);
                return fetchShapeChange.TeamIds.Count + fetchShapeChange.PortfolioIds.Count;
            }
#pragma warning disable CA1031 // The save is already stored: failing the request would tell the admin it was not and invite a second save that changes nothing, and every Team and Portfolio whose fetch moved still downloads everything at its next scheduled update.
            catch (Exception exception)
#pragma warning restore CA1031
            {
                logger.LogError(exception, "Connection {ConnectionId} was saved, but its Teams and Portfolios could not be told to update now; they will pick up the change at their next scheduled update", handle.ConnectionId);
                return 0;
            }
        }

        private void LogTheChangedDefault(ConnectionSaveHandle handle, int updatesQueued)
        {
            var connection = handle.Connection;
            var before = handle.TeamsParentOverrideDefaultBeforeSave;
            var after = TeamsParentOverrideDefaultOf(connection);
            if (after.FieldId == before.FieldId)
            {
                return;
            }

            try
            {
                var followers = TeamsFollowingTheDefaultOf(connection);
                logger.LogInformation(
                    "Connection {ConnectionName} changed the Teams' Parent Override Field default from {PreviousField} to {NewField}; {Followers:l}; {Queued:l}.",
                    connection.Name,
                    before.FieldName ?? NoField,
                    after.FieldName ?? NoField,
                    WhoFollows(followers),
                    HowManyQueued(updatesQueued));
            }
#pragma warning disable CA1031 // The save is already stored: failing the request over its log entry would tell the admin it was not.
            catch (Exception exception)
#pragma warning restore CA1031
            {
                logger.LogError(exception, "Connection {ConnectionId} was saved, but the change to its Teams' default could not be logged", handle.ConnectionId);
            }
        }

        private static NamedField TeamsParentOverrideDefaultOf(WorkTrackingSystemConnection connection)
            => new(connection.ParentOverrideForTeams, connection.DisplayNameOfField(connection.ParentOverrideForTeams));

        // In the order the Teams were created, which is the order an admin knows them by on the Teams list.
        private List<string> TeamsFollowingTheDefaultOf(WorkTrackingSystemConnection connection)
            => [.. teamRepository.GetAllByPredicate(team => team.WorkTrackingSystemConnectionId == connection.Id)
                .AsEnumerable()
                .Where(team => team.FollowsParentOverrideDefault)
                .OrderBy(team => team.Id)
                .Select(team => team.Name)];

        private static string WhoFollows(List<string> followers) => followers.Count switch
        {
            0 => "no Team follows it",
            1 => $"1 Team follows it: {followers[0]}",
            _ => $"{followers.Count} Teams follow it: {string.Join(", ", followers)}",
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
