using Lighthouse.Backend.API;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

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
        public void GetUpdateStatus_NoActiveUpdates_ReturnsFalseAndZero()
        {
            var subject = CreateSubject();

            var result = subject.GetUpdateStatus();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = result.Result as OkObjectResult;
            var response = okResult.Value as UpdateController.UpdateStatusResponse;
            Assert.That(response.HasActiveUpdates, Is.False);
            Assert.That(response.ActiveCount, Is.EqualTo(0));
        }

        [Test]
        public void GetUpdateStatus_HasQueuedUpdate_ReturnsTrueAndCount()
        {
            var updateKey = new UpdateKey(UpdateType.Team, 1);
            var updateStatus = new UpdateStatus { UpdateType = UpdateType.Team, Id = 1, Status = UpdateProgress.Queued };
            updateStatuses[updateKey] = updateStatus;

            var subject = CreateSubject();

            var result = subject.GetUpdateStatus();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = result.Result as OkObjectResult;
            var response = okResult.Value as UpdateController.UpdateStatusResponse;
            Assert.That(response.HasActiveUpdates, Is.True);
            Assert.That(response.ActiveCount, Is.EqualTo(1));
        }

        [Test]
        public void GetUpdateStatus_HasInProgressUpdate_ReturnsTrueAndCount()
        {
            var updateKey = new UpdateKey(UpdateType.Features, 2);
            var updateStatus = new UpdateStatus { UpdateType = UpdateType.Features, Id = 2, Status = UpdateProgress.InProgress };
            updateStatuses[updateKey] = updateStatus;

            var subject = CreateSubject();

            var result = subject.GetUpdateStatus();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = result.Result as OkObjectResult;
            var response = okResult.Value as UpdateController.UpdateStatusResponse;
            Assert.That(response.HasActiveUpdates, Is.True);
            Assert.That(response.ActiveCount, Is.EqualTo(1));
        }

        [Test]
        public void GetUpdateStatus_HasCompletedUpdate_ReturnsFalseAndZero()
        {
            var updateKey = new UpdateKey(UpdateType.Forecasts, 3);
            var updateStatus = new UpdateStatus { UpdateType = UpdateType.Forecasts, Id = 3, Status = UpdateProgress.Completed };
            updateStatuses[updateKey] = updateStatus;

            var subject = CreateSubject();

            var result = subject.GetUpdateStatus();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = result.Result as OkObjectResult;
            var response = okResult.Value as UpdateController.UpdateStatusResponse;
            Assert.That(response.HasActiveUpdates, Is.False);
            Assert.That(response.ActiveCount, Is.EqualTo(0));
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

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = result.Result as OkObjectResult;
            var response = okResult.Value as UpdateController.UpdateStatusResponse;
            Assert.That(response.HasActiveUpdates, Is.True);
            Assert.That(response.ActiveCount, Is.EqualTo(2));
        }

        private UpdateController CreateSubject()
        {
            return new UpdateController(updateStatuses);
        }
    }
}