using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;

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

        private static readonly ConnectionSaveOutcome Saved = new(Refused: false);

        public Task<ConnectionSaveHandle?> Open(int connectionId, CancellationToken cancellationToken)
        {
            var connection = connectionRepository.GetById(connectionId);
            var handle = connection is null
                ? null
                : new ConnectionSaveHandle(connection, FetchFingerprint.ForConnection(connection))
                {
                    TeamsParentOverrideDefaultBeforeSave = new NamedField(connection.ParentOverrideForTeams, connection.DisplayNameOfField(connection.ParentOverrideForTeams)),
                };
            return Task.FromResult(handle);
        }

        public async Task<ConnectionSaveOutcome> Commit(ConnectionSaveHandle handle, Guid? concurrencyToken, bool startUpdates, CancellationToken cancellationToken)
        {
            var connection = handle.Connection;
            connectionRepository.Update(connection);

            if (concurrencyToken.HasValue)
            {
                connectionRepository.ApplyConcurrencyTokenForEdit(connection, concurrencyToken.Value);
            }

            await connectionRepository.Save();
            await PointTheDefaultAtTheFieldAddedInThisSave(handle);
            LogTheChangedDefault(handle);
            await StartUpdatesIfFetchShapeMoved(handle, startUpdates, cancellationToken);

            return Saved;
        }

        private async Task PointTheDefaultAtTheFieldAddedInThisSave(ConnectionSaveHandle handle)
        {
            if (handle.TeamsParentOverrideDefaultAddedInThisSave is not { } addedField)
            {
                return;
            }

            handle.Connection.ParentOverrideForTeams = addedField.Id;
            await connectionRepository.Save();
        }

        private async Task StartUpdatesIfFetchShapeMoved(ConnectionSaveHandle handle, bool startUpdates, CancellationToken cancellationToken)
        {
            if (!startUpdates || FetchFingerprint.ForConnection(handle.Connection) == handle.FetchShapeBeforeSave)
            {
                return;
            }

            try
            {
                var fetchShapeChange = FetchShapeChangeForUsersOf(handle.Connection);
                if (fetchShapeChange != null)
                {
                    await domainEventDispatcher.PublishAsync(fetchShapeChange, cancellationToken);
                }
            }
#pragma warning disable CA1031 // The save is already stored: failing the request would tell the admin it was not and invite a second save that changes nothing, and every Team and Portfolio still downloads the new fields at its next scheduled update.
            catch (Exception exception)
#pragma warning restore CA1031
            {
                logger.LogError(exception, "Connection {ConnectionId} was saved, but its Teams and Portfolios could not be told to update now; they will pick up the change at their next scheduled update", handle.ConnectionId);
            }
        }

        private void LogTheChangedDefault(ConnectionSaveHandle handle)
        {
            var connection = handle.Connection;
            var before = handle.TeamsParentOverrideDefaultBeforeSave;
            if (connection.ParentOverrideForTeams == before.FieldId)
            {
                return;
            }

            try
            {
                var followers = TeamsFollowingTheDefaultOf(connection);
                var previousField = before.FieldName ?? NoField;
                var newField = connection.DisplayNameOfField(connection.ParentOverrideForTeams) ?? NoField;

                if (followers.Count == 0)
                {
                    logger.LogInformation("Connection {ConnectionName}: the Teams' default Parent Override Field changed from {PreviousField} to {NewField}; no Team follows it", connection.Name, previousField, newField);
                }
                else
                {
                    logger.LogInformation("Connection {ConnectionName}: the Teams' default Parent Override Field changed from {PreviousField} to {NewField}; {FollowerCount} Teams follow it: {Followers:l}", connection.Name, previousField, newField, followers.Count, string.Join(", ", followers));
                }
            }
#pragma warning disable CA1031 // The save is already stored: failing the request over its log entry would tell the admin it was not.
            catch (Exception exception)
#pragma warning restore CA1031
            {
                logger.LogError(exception, "Connection {ConnectionId} was saved, but the change to its Teams' default could not be logged", handle.ConnectionId);
            }
        }

        // In the order the Teams were created, which is the order an admin knows them by on the Teams list.
        private List<string> TeamsFollowingTheDefaultOf(WorkTrackingSystemConnection connection)
            => [.. teamRepository.GetAllByPredicate(team => team.WorkTrackingSystemConnectionId == connection.Id)
                .AsEnumerable()
                .Where(team => team.ParentOverrideSource == SettingSource.ConnectionDefault)
                .OrderBy(team => team.Id)
                .Select(team => team.Name)];

        // Kept synchronous so the architecture test sees this service build the event: in a Release build an
        // async method's body moves into a struct that the test cannot attribute to its owner.
        private ConnectionFetchShapeChanged? FetchShapeChangeForUsersOf(WorkTrackingSystemConnection connection)
        {
            var teamIds = teamRepository.GetAllByPredicate(t => t.WorkTrackingSystemConnectionId == connection.Id).Select(t => t.Id).ToList();
            var portfolioIds = portfolioRepository.GetAllByPredicate(p => p.WorkTrackingSystemConnectionId == connection.Id).Select(p => p.Id).ToList();

            if (teamIds.Count == 0 && portfolioIds.Count == 0)
            {
                return null;
            }

            return new ConnectionFetchShapeChanged(connection.Id, teamIds, portfolioIds);
        }
    }
}
