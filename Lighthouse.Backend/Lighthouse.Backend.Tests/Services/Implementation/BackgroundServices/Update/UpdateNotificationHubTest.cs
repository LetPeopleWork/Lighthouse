using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
﻿using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Concurrent;
using Lighthouse.Backend.Tests.TestHelpers;

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

        /// <summary>
        /// Epic #5511, found by a retroactive adversarial review of slice 01. The store holds a key only
        /// while the work is in flight, so a page opened after a refresh has finished asks about a key that
        /// is gone - and every such page showed the healthy icon whether the last run worked or broke. The
        /// refresh log outlives the run and is the only place that answer still exists.
        /// </summary>
        [TestCase(false, UpdateProgress.Failed, TestName = "GetUpdateStatus_TheKeyIsGoneAndTheLastRunFailed_SaysItFailed")]
        [TestCase(true, UpdateProgress.Completed, TestName = "GetUpdateStatus_TheKeyIsGoneAndTheLastRunWorked_SaysItCompleted")]
        public void GetUpdateStatus_NothingIsInFlight_AnswersFromTheLastRecordedRefresh(bool lastRunSucceeded, UpdateProgress expected)
        {
            var refreshLog = new Mock<IRefreshLogService>();
            refreshLog.Setup(s => s.GetRefreshLogs()).Returns(
            [
                new RefreshLog { Type = RefreshType.Team, EntityId = 12, Success = !lastRunSucceeded, ExecutedAt = new DateTime(2031, 4, 16, 9, 0, 0, DateTimeKind.Utc) },
                new RefreshLog { Type = RefreshType.Team, EntityId = 12, Success = lastRunSucceeded, ExecutedAt = new DateTime(2031, 4, 17, 9, 0, 0, DateTimeKind.Utc) },
                new RefreshLog { Type = RefreshType.Team, EntityId = 99, Success = !lastRunSucceeded, ExecutedAt = new DateTime(2031, 4, 18, 9, 0, 0, DateTimeKind.Utc) },
            ]);

            using var subject = CreateSubject(refreshLog.Object);

            var status = subject.GetUpdateStatus(nameof(UpdateType.Team), 12);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(status, Is.Not.Null,
                    "A refresh that has already run is not nothing, and answering null makes it indistinguishable "
                    + "from an entity that has never been refreshed at all.");
                Assert.That(status!.Status, Is.EqualTo(expected),
                    "The most recent run for this entity is the one the icon is reporting on - not an older one, "
                    + "and not another entity's.");
            }
        }

        [Test]
        public void GetUpdateStatus_NothingIsInFlightAndNothingWasEverRecorded_AnswersNothing()
        {
            var refreshLog = new Mock<IRefreshLogService>();
            refreshLog.Setup(s => s.GetRefreshLogs()).Returns([]);

            using var subject = CreateSubject(refreshLog.Object);

            Assert.That(subject.GetUpdateStatus(nameof(UpdateType.Team), 12), Is.Null,
                "An entity that has never been refreshed has nothing to report, and inventing a status for it "
                + "would put a verdict on the icon that no run ever earned.");
        }

        /// <summary>
        /// The other half of the same question. A cancel is not a failure, and the header saying so
        /// contradicts the row the operator just cancelled in the task list one screen away.
        /// </summary>
        [Test]
        public void GetUpdateStatus_TheKeyIsGoneAndTheLastRunWasCancelled_SaysItWasCancelled()
        {
            var refreshLog = new Mock<IRefreshLogService>();
            refreshLog.Setup(s => s.GetRefreshLogs()).Returns(
            [
                new RefreshLog { Type = RefreshType.Team, EntityId = 12, Success = true, ExecutedAt = new DateTime(2031, 4, 16, 9, 0, 0, DateTimeKind.Utc) },
                new RefreshLog { Type = RefreshType.Team, EntityId = 12, Success = false, Cancelled = true, ExecutedAt = new DateTime(2031, 4, 17, 9, 0, 0, DateTimeKind.Utc) },
            ]);

            using var subject = CreateSubject(refreshLog.Object);

            var status = subject.GetUpdateStatus(nameof(UpdateType.Team), 12);

            Assert.That(status!.Status, Is.EqualTo(UpdateProgress.Cancelled),
                "Reported as Failed, an operator who stopped a refresh on purpose is shown a broken entity "
                + "and goes looking for the breakage.");
        }

        private UpdateNotificationHub CreateSubject() => CreateSubject(Mock.Of<IRefreshLogService>());

        private UpdateNotificationHub CreateSubject(IRefreshLogService refreshLogService)
        {
            var hub = new UpdateNotificationHub(new InProcessUpdateStatusStore(updateStatuses, Clocks.SystemUtc), refreshLogService, Mock.Of<ILogger<UpdateNotificationHub>>())
            {
                Clients = clientsMock.Object,
                Groups = groupsMock.Object,
                Context = contextMock.Object
            };

            return hub;
        }
    }
}
