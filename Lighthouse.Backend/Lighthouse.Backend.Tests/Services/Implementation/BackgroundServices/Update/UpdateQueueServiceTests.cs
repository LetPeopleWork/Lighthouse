using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Concurrent;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices.Update
{
    [TestFixture]
    public class UpdateQueueServiceTests
    {
        private Mock<IHubContext<UpdateNotificationHub>> hubContextMock;
        private Mock<IServiceScopeFactory> serviceScopeFactoryMock;
        private Mock<IClientProxy> clientProxyMock;

        private ConcurrentDictionary<UpdateKey, UpdateStatus> updateStatuses;

        [SetUp]
        public void SetUp()
        {
            hubContextMock = new Mock<IHubContext<UpdateNotificationHub>>();
            clientProxyMock = new Mock<IClientProxy>();
            hubContextMock.Setup(ctx => ctx.Clients.Group(It.IsAny<string>())).Returns(clientProxyMock.Object);

            serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();

            var serviceScopeMock = new Mock<IServiceScope>();
            var serviceProviderMock = new Mock<IServiceProvider>();

            serviceScopeFactoryMock.Setup(f => f.CreateScope()).Returns(serviceScopeMock.Object);
            serviceScopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);

            updateStatuses = new ConcurrentDictionary<UpdateKey, UpdateStatus>();
        }

        [Test]
        public void EnqueueUpdate_NewUpdate_QueuesUpdate()
        {
            var updateType = UpdateType.Team;
            var id = 1;

            var subject = CreateSubject();
            subject.EnqueueUpdate(updateType, id, _ => Task.Delay(300));

            Assert.That(updateStatuses.ContainsKey(new UpdateKey(updateType, id)), Is.True);
        }

        [Test]
        public void EnqueueUpdate_ExistingUpdate_DoesNotQueueUpdate()
        {
            var updateType = UpdateType.Team;
            var id = 1;

            var subject = CreateSubject();
            subject.EnqueueUpdate(updateType, id, _ => Task.Delay(300));
            subject.EnqueueUpdate(updateType, id, _ => Task.CompletedTask);

            Assert.Multiple(() =>
            {
                Assert.That(updateStatuses.ContainsKey(new UpdateKey(updateType, id)), Is.True);
                Assert.That(updateStatuses, Has.Count.EqualTo(1));
            });
        }

        [Test]
        public async Task EnqueueUpdate_ExecutesUpdateEventually()
        {
            var updateStatus = new UpdateStatus { UpdateType = UpdateType.Team, Id = 1, Status = UpdateProgress.Queued };
            var updateKey = new UpdateKey(updateStatus.UpdateType, updateStatus.Id);

            bool wasExecuted = false;

            var updateTask = new Func<IServiceProvider, Task>(_ =>
            {
                wasExecuted = true;
                return Task.CompletedTask;
            });

            var subject = CreateSubject();
            subject.EnqueueUpdate(updateStatus.UpdateType, updateStatus.Id, updateTask);

            while (!wasExecuted)
            {
                await Task.Delay(100);
            }

            Assert.Multiple(() =>
            {
                Assert.That(wasExecuted, Is.True);
                Assert.That(updateStatuses.ContainsKey(updateKey), Is.False);
            });
        }

        [Test]
        public async Task EnqueueUpdate_NotifiesAboutQueuedOrInProgress()
        {
            var updateStatus = new UpdateStatus { UpdateType = UpdateType.Team, Id = 1, Status = UpdateProgress.Queued };
            var updateKey = new UpdateKey(updateStatus.UpdateType, updateStatus.Id);

            var wasNotified = false;

            clientProxyMock.Setup(client => client.SendCoreAsync(updateKey.ToString(), It.IsAny<object[]>(), default)).Callback((string key, object[] parameters, CancellationToken token) =>
            {
                updateStatus = (UpdateStatus)parameters[0];
                wasNotified = updateStatus.Status == UpdateProgress.Queued || updateStatus.Status == UpdateProgress.InProgress;
            });

            var subject = CreateSubject();
            subject.EnqueueUpdate(updateStatus.UpdateType, updateStatus.Id, _ => Task.CompletedTask);

            while (!wasNotified)
            {
                await Task.Delay(100);
            }

            Assert.That(wasNotified, Is.True);
        }

        [Test]
        public async Task EnqueueUpdate_NotifiesAboutCompletion()
        {
            var updateStatus = new UpdateStatus { UpdateType = UpdateType.Team, Id = 1, Status = UpdateProgress.Queued };
            var updateKey = new UpdateKey(updateStatus.UpdateType, updateStatus.Id);

            var wasNotified = false;

            clientProxyMock.Setup(client => client.SendCoreAsync(updateKey.ToString(), It.IsAny<object[]>(), default)).Callback((string key, object[] parameters, CancellationToken token) =>
            {
                updateStatus = (UpdateStatus)parameters[0];
                wasNotified = updateStatus.Status == UpdateProgress.Completed;
            });

            var subject = CreateSubject();
            subject.EnqueueUpdate(updateStatus.UpdateType, updateStatus.Id, _ => Task.CompletedTask);

            while (!wasNotified)
            {
                await Task.Delay(100);
            }

            Assert.That(wasNotified, Is.True);
        }

        [Test]
        public async Task ExecuteUpdateAsync_UpdateTaskFails_NotifiesAboutFailureAsync()
        {
            var updateKey = new UpdateKey(UpdateType.Team, 1);
            var updateStatus = new UpdateStatus { UpdateType = updateKey.UpdateType, Id = updateKey.Id, Status = UpdateProgress.Queued };

            var updateTask = new Func<IServiceProvider, Task>(_ => throw new Exception("Test exception"));

            var wasNotified = false;

            clientProxyMock.Setup(client => client.SendCoreAsync(updateKey.ToString(), It.IsAny<object[]>(), default)).Callback((string key, object[] parameters, CancellationToken token) =>
            {
                updateStatus = (UpdateStatus)parameters[0];
                wasNotified = updateStatus.Status == UpdateProgress.Failed;
            });

            var subject = CreateSubject();
            subject.EnqueueUpdate(updateStatus.UpdateType, updateStatus.Id, updateTask);

            while (!wasNotified)
            {
                await Task.Delay(100);
            }

            Assert.That(wasNotified, Is.True);
        }

        private UpdateQueueService CreateSubject()
        {
            return new UpdateQueueService(Mock.Of<ILogger<UpdateQueueService>>(), hubContextMock.Object, updateStatuses, serviceScopeFactoryMock.Object);
        }
    }
}
