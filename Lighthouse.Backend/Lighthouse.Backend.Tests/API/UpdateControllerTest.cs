using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.API;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Concurrent;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.API
{
    [TestFixture]
    public class UpdateControllerTest
    {
        private ConcurrentDictionary<UpdateKey, UpdateStatus> updateStatuses;

        [SetUp]
        public void Setup()
        {
            updateStatuses = new ConcurrentDictionary<UpdateKey, UpdateStatus>();
        }

        [Test]
        public void UpdateController_HasAuthorizeAttribute()
        {
            var attribute = typeof(UpdateController)
                .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Cast<AuthorizeAttribute>()
                .SingleOrDefault();

            Assert.That(attribute, Is.Not.Null);
        }

        [Test]
        public void GetUpdateStatus_NoActiveUpdates_ReturnsFalseAndZero()
        {
            var subject = CreateSubject();

            var result = subject.GetUpdateStatus();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = result.Result as OkObjectResult;
                var response = okResult.Value as UpdateController.UpdateStatusResponse;

                Assert.That(response.HasActiveUpdates, Is.False);
                Assert.That(response.ActiveCount, Is.Zero);
            }
        }

        [Test]
        public void GetUpdateStatus_HasQueuedUpdate_ReturnsTrueAndCount()
        {
            var updateKey = new UpdateKey(UpdateType.Team, 1);
            var updateStatus = new UpdateStatus { UpdateType = UpdateType.Team, Id = 1, Status = UpdateProgress.Queued };
            updateStatuses[updateKey] = updateStatus;

            var subject = CreateSubject();

            var result = subject.GetUpdateStatus();
            using (Assert.EnterMultipleScope())
            {

                Assert.That(result, Is.Not.Null);
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = result.Result as OkObjectResult;
                var response = okResult.Value as UpdateController.UpdateStatusResponse;
                Assert.That(response.HasActiveUpdates, Is.True);
                Assert.That(response.ActiveCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void GetUpdateStatus_HasInProgressUpdate_ReturnsTrueAndCount()
        {
            var updateKey = new UpdateKey(UpdateType.Features, 2);
            var updateStatus = new UpdateStatus { UpdateType = UpdateType.Features, Id = 2, Status = UpdateProgress.InProgress };
            updateStatuses[updateKey] = updateStatus;

            var subject = CreateSubject();

            var result = subject.GetUpdateStatus();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = result.Result as OkObjectResult;
                var response = okResult.Value as UpdateController.UpdateStatusResponse;
                Assert.That(response.HasActiveUpdates, Is.True);
                Assert.That(response.ActiveCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void GetUpdateStatus_HasCompletedUpdate_ReturnsFalseAndZero()
        {
            var updateKey = new UpdateKey(UpdateType.Forecasts, 3);
            var updateStatus = new UpdateStatus { UpdateType = UpdateType.Forecasts, Id = 3, Status = UpdateProgress.Completed };
            updateStatuses[updateKey] = updateStatus;

            var subject = CreateSubject();

            var result = subject.GetUpdateStatus();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = result.Result as OkObjectResult;
                var response = okResult.Value as UpdateController.UpdateStatusResponse;
                Assert.That(response.HasActiveUpdates, Is.False);
                Assert.That(response.ActiveCount, Is.Zero);
            }
        }

        [Test]
        public void GetUpdateStatus_MultipleUpdates_IncludesOnlyActive()
        {
            // Add active updates
            updateStatuses[new UpdateKey(UpdateType.Team, 1)] = new UpdateStatus { UpdateType = UpdateType.Team, Id = 1, Status = UpdateProgress.Queued };
            updateStatuses[new UpdateKey(UpdateType.Features, 2)] = new UpdateStatus { UpdateType = UpdateType.Features, Id = 2, Status = UpdateProgress.InProgress };

            // Add inactive updates
            updateStatuses[new UpdateKey(UpdateType.Forecasts, 3)] = new UpdateStatus { UpdateType = UpdateType.Forecasts, Id = 3, Status = UpdateProgress.Completed };
            updateStatuses[new UpdateKey(UpdateType.Team, 4)] = new UpdateStatus { UpdateType = UpdateType.Team, Id = 4, Status = UpdateProgress.Failed };

            var subject = CreateSubject();

            var result = subject.GetUpdateStatus();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = result.Result as OkObjectResult;
                var response = okResult.Value as UpdateController.UpdateStatusResponse;
                Assert.That(response.HasActiveUpdates, Is.True);
                Assert.That(response.ActiveCount, Is.EqualTo(2));
            }
        }

        [TestCase(UpdateType.TeamDelete)]
        [TestCase(UpdateType.PortfolioDelete)]
        public async Task CancelTask_ARemoval_IsRefusedAndNeverReachesTheQueue(UpdateType removal)
        {
            var queue = new Mock<IUpdateQueueService>();
            var subject = CreateSubject(queue.Object);

            var result = await subject.CancelTask(removal, 5);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
                Assert.That((result as BadRequestObjectResult)?.Value, Is.EqualTo("A deletion cannot be cancelled once it has been asked for."));
            }

            queue.Verify(q => q.CancelAsync(It.IsAny<UpdateKey>()), Times.Never,
                "A removal stopped half-way leaves its rows in the database while the caller is told the entity has gone.");
        }

        [Test]
        public async Task CancelTask_ARefresh_IsAcceptedAndHandedToTheQueue()
        {
            var queue = new Mock<IUpdateQueueService>();
            var subject = CreateSubject(queue.Object);

            var result = await subject.CancelTask(UpdateType.Team, 6);

            Assert.That(result, Is.InstanceOf<NoContentResult>());
            queue.Verify(q => q.CancelAsync(new UpdateKey(UpdateType.Team, 6)), Times.Once);
        }

        /// <summary>
        /// The dictionary is still what backs the store these tests seed, but the controller only ever sees
        /// the port (Epic #5511 slice 02, AC-02.2). Reading a dictionary directly is what made the old
        /// endpoint answer about one replica on a multi-replica instance.
        /// </summary>
        private UpdateController CreateSubject()
        {
            return CreateSubject(Mock.Of<IUpdateQueueService>());
        }

        private UpdateController CreateSubject(IUpdateQueueService updateQueueService)
        {
            return new UpdateController(
                new InProcessUpdateStatusStore(updateStatuses, Clocks.SystemUtc),
                new UpdateTaskNaming(Mock.Of<IRepository<Team>>(), Mock.Of<IPortfolioRepository>()),
                Clocks.SystemUtc,
                updateQueueService);
        }
    }
}