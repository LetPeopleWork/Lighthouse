using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Lighthouse.Backend.Services.Implementation.Update
{
    public class UpdateNotificationHub : Hub
    {
        private readonly ConcurrentDictionary<UpdateKey, UpdateStatus> updateStatuses;

        public UpdateNotificationHub(ConcurrentDictionary<UpdateKey, UpdateStatus> updateStatuses)
        {
            this.updateStatuses = updateStatuses;
        }

        public async Task SubscribeToUpdate(UpdateType updateType, int id)
        {
            var updateKey = new UpdateKey(updateType, id);
            await Groups.AddToGroupAsync(Context.ConnectionId, updateKey.ToString());
        }

        public async Task UnsubscribeFromUpdate(UpdateType updateType, int id)
        {
            var updateKey = new UpdateKey(updateType, id);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, updateKey.ToString());
        }

        public Task<UpdateStatus?> GetUpdateStatus(UpdateType updateType, int id)
        {
            var updateKey = new UpdateKey(updateType, id);
            updateStatuses.TryGetValue(updateKey, out var updateStatus);
            return Task.FromResult(updateStatus);
        }
    }
}
