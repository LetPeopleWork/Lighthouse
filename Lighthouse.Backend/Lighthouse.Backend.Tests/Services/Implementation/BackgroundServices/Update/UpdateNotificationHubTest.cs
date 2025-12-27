using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Concurrent;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices.Update
{
    [TestFixture]
    public class UpdateNotificationHubTests
    {
        private Mock<IHubCallerClients> clientsMock;
        private Mock<IGroupManager> groupsMock;
        private Mock<HubCallerContext> contextMock;
        private ConcurrentDictionary<UpdateKey, UpdateStatus> updateStatuses;

        [SetUp]
        public void SetUp()
        {
            clientsMock = new Mock<IHubCallerClients>();
            groupsMock = new Mock<IGroupManager>();
            contextMock = new Mock<HubCallerContext>();
            updateStatuses = new ConcurrentDictionary<UpdateKey, UpdateStatus>();
        }

        [Test]
        public async Task SubscribeToUpdate_ValidUpdateType_AddsToGroup()
        {
            var updateType = "Team";
            var id = 1;
            var connectionId = "test-connection-id";
            contextMock.Setup(c => c.ConnectionId).Returns(connectionId);

            using var subject = CreateSubject();
            await subject.SubscribeToUpdate(updateType, id);

            groupsMock.Verify(g => g.AddToGroupAsync(connectionId, new UpdateKey(UpdateType.Team, id).ToString(), default), Times.Once);
        }

        [Test]
        public async Task UnsubscribeFromUpdate_ValidUpdateType_RemovesFromGroup()
        {
            var updateType = "Team";
            var id = 1;
            var connectionId = "test-connection-id";
            contextMock.Setup(c => c.ConnectionId).Returns(connectionId);

            using var subject = CreateSubject();
            await subject.UnsubscribeFromUpdate(updateType, id);

            groupsMock.Verify(g => g.RemoveFromGroupAsync(connectionId, new UpdateKey(UpdateType.Team, id).ToString(), default), Times.Once);
        }

        [Test]
        public void GetUpdateStatus_ValidUpdateType_ReturnsUpdateStatus()
        {
            var updateType = "Team";
            var id = 1;
            var updateKey = new UpdateKey(UpdateType.Team, id);
            var expectedStatus = new UpdateStatus { UpdateType = UpdateType.Team, Id = id, Status = UpdateProgress.InProgress };
            updateStatuses[updateKey] = expectedStatus;

            using var subject = CreateSubject();
            var result = subject.GetUpdateStatus(updateType, id);

            Assert.That(result, Is.EqualTo(expectedStatus));
        }

        [Test]
        public void GetUpdateStatus_InvalidUpdateType_ReturnsNull()
        {
            var updateType = "InvalidType";
            var id = 1;

            using var subject = CreateSubject();
            var result = subject.GetUpdateStatus(updateType, id);

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task SubscribeToAllUpdates_AddsConnectionToGlobalUpdatesGroup()
        {
            var connectionId = "test-connection-id";
            contextMock.Setup(c => c.ConnectionId).Returns(connectionId);

            using var subject = CreateSubject();
            await subject.SubscribeToAllUpdates();

            groupsMock.Verify(g => g.AddToGroupAsync(connectionId, "GlobalUpdates", default), Times.Once);
        }

        [Test]
        public async Task UnsubscribeFromAllUpdates_RemovesConnectionFromGlobalUpdatesGroup()
        {
            var connectionId = "test-connection-id";
            contextMock.Setup(c => c.ConnectionId).Returns(connectionId);

            using var subject = CreateSubject();
            await subject.UnsubscribeFromAllUpdates();

            groupsMock.Verify(g => g.RemoveFromGroupAsync(connectionId, "GlobalUpdates", default), Times.Once);
        }

        private UpdateNotificationHub CreateSubject()
        {
            var hub = new UpdateNotificationHub(updateStatuses, Mock.Of<ILogger<UpdateNotificationHub>>())
            {
                Clients = clientsMock.Object,
                Groups = groupsMock.Object,
                Context = contextMock.Object
            };

            return hub;
        }
    }
}
