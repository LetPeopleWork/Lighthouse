using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Lighthouse.Backend.Services.Implementation.Update
{
    public class UpdateNotificationHub : Hub
    {
        private readonly ConcurrentDictionary<UpdateKey, UpdateStatus> updateStatuses;
        private readonly ILogger<UpdateNotificationHub> logger;

        public UpdateNotificationHub(ConcurrentDictionary<UpdateKey, UpdateStatus> updateStatuses, ILogger<UpdateNotificationHub> logger)
        {
            this.updateStatuses = updateStatuses;
            this.logger = logger;
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
            if (TryParseUpdateType(updateType, out var parsedUpdateType))
            {
                var updateKey = new UpdateKey(parsedUpdateType, id);
                updateStatuses.TryGetValue(updateKey, out var updateStatus);
                return updateStatus;
            }

            return null;
        }

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