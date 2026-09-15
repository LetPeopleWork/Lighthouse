using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    [Authorize]
    public class UpdateNotificationHub : Hub
    {
        private readonly IUpdateStatusStore statusStore;
        private readonly IRefreshLogService refreshLogService;
        private readonly ILogger<UpdateNotificationHub> logger;
        private const string GlobalUpdatesGroup = "GlobalUpdates";

        public UpdateNotificationHub(IUpdateStatusStore statusStore, IRefreshLogService refreshLogService, ILogger<UpdateNotificationHub> logger)
        {
            this.statusStore = statusStore;
            this.refreshLogService = refreshLogService;
            this.logger = logger;
        }

        public async Task SubscribeToAllUpdates()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GlobalUpdatesGroup);
            logger.LogDebug("Client {ConnectionId} subscribed to all updates", Context.ConnectionId);
        }

        public async Task UnsubscribeFromAllUpdates()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GlobalUpdatesGroup);
            logger.LogDebug("Client {ConnectionId} unsubscribed from all updates", Context.ConnectionId);
        }

        public async Task SubscribeToUpdate(string updateType, int id)
        {
            if (TryParseUpdateType(updateType, out var parsedUpdateType))
            {
                var updateKey = new UpdateKey(parsedUpdateType, id);
                await Groups.AddToGroupAsync(Context.ConnectionId, updateKey.ToString());
            }
        }

        public async Task UnsubscribeFromUpdate(string updateType, int id)
        {
            if (TryParseUpdateType(updateType, out var parsedUpdateType))
            {
                var updateKey = new UpdateKey(parsedUpdateType, id);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, updateKey.ToString());
            }
        }

        public UpdateStatus? GetUpdateStatus(string updateType, int id)
        {
            if (!TryParseUpdateType(updateType, out var parsedUpdateType))
            {
                return null;
            }

            var updateKey = new UpdateKey(parsedUpdateType, id);
            if (statusStore.TryGet(updateKey, out var updateStatus))
            {
                return updateStatus;
            }

            return HowTheLastRunEnded(updateKey);
        }

        /// <summary>
        /// The store holds a key only while the work is in flight, so a page opened at nine in the morning
        /// after a refresh broke at three finds nothing there - and shows the same healthy icon it would
        /// show for an instance that is perfectly well. The answer outlives the run in the refresh log,
        /// which is the only place it still exists.
        /// </summary>
        private UpdateStatus? HowTheLastRunEnded(UpdateKey updateKey)
        {
            if (RefreshTypeFor(updateKey.UpdateType) is not RefreshType refreshType)
            {
                return null;
            }

            var lastRun = refreshLogService.GetRefreshLogs()
                .Where(log => log.Type == refreshType && log.EntityId == updateKey.Id)
                .OrderByDescending(log => log.ExecutedAt)
                .FirstOrDefault();

            if (lastRun is null)
            {
                return null;
            }

            return new UpdateStatus
            {
                UpdateType = updateKey.UpdateType,
                Id = updateKey.Id,
                Status = WhatTheLastRunSaysHappened(lastRun),
            };
        }

        /// <summary>
        /// A cancelled run is its own answer. Reported as Failed here the header contradicts the row the
        /// operator just cancelled in the task list, one screen away.
        /// </summary>
        private static UpdateProgress WhatTheLastRunSaysHappened(RefreshLog lastRun)
        {
            if (lastRun.Cancelled)
            {
                return UpdateProgress.Cancelled;
            }

            return lastRun.Success ? UpdateProgress.Completed : UpdateProgress.Failed;
        }

        /// <summary>
        /// Only the three a detail page ever subscribes to. A delete has no refresh log and nothing left to
        /// ask about once it has happened.
        /// </summary>
        private static RefreshType? RefreshTypeFor(UpdateType updateType) => updateType switch
        {
            UpdateType.Team => RefreshType.Team,
            UpdateType.Features => RefreshType.Portfolio,
            UpdateType.Forecasts => RefreshType.Forecast,
            _ => null,
        };

        private bool TryParseUpdateType(string updateType, out UpdateType parsedUpdateType)
        {
            if (Enum.TryParse(updateType, true, out parsedUpdateType))
            {
                return true;
            }

            logger.LogWarning("Invalid update type: {UpdateType}", updateType);

            return false;
        }
    }
}