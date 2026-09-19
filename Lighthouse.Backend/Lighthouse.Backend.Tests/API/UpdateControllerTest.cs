using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.API;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Concurrent;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestDoubles;
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

        /// <summary>
        /// Work of each kind waits only for work of its own kind, so a waiting row is waiting for whatever
        /// is running beside it in that kind, and for nothing at all when nothing of its kind is running.
        /// Naming a refresh of another kind would show an operator a dependency that is not there.
        ///
        /// Read many times over: the answer is resolved out of a store several threads write to, so one
        /// right answer says nothing about the next one.
        /// </summary>
        [Test]
        public void GetTasks_WorkOfSeveralKindsIsRunning_EachWaitingRowNamesWhatIsRunningInItsOwnKind()
        {
            updateStatuses[new UpdateKey(UpdateType.Team, 1)] = Work(UpdateType.Team, 1, UpdateProgress.InProgress);
            updateStatuses[new UpdateKey(UpdateType.Features, 2)] = Work(UpdateType.Features, 2, UpdateProgress.InProgress);
            updateStatuses[new UpdateKey(UpdateType.Team, 3)] = Work(UpdateType.Team, 3, UpdateProgress.Queued);
            updateStatuses[new UpdateKey(UpdateType.TeamDelete, 4)] = Work(UpdateType.TeamDelete, 4, UpdateProgress.Queued);
            updateStatuses[new UpdateKey(UpdateType.Forecasts, 5)] = Work(UpdateType.Forecasts, 5, UpdateProgress.Queued);

            var subject = CreateSubject();

            for (var read = 0; read < ReadsThatMakeAnArbitraryAnswerShowItself; read++)
            {
                var tasks = TasksFrom(subject);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(WaitingBehindOf(tasks, UpdateType.Team, 3), Is.EqualTo(TheRunningTeam),
                        $"Read {read + 1}: the queued team is waiting for the team that is running, not for the portfolio refresh beside it.");
                    Assert.That(WaitingBehindOf(tasks, UpdateType.TeamDelete, 4), Is.EqualTo(TheRunningTeam),
                        $"Read {read + 1}: a removal shares the lane of the entity it removes, so it waits for that team's refresh.");
                    Assert.That(WaitingBehindOf(tasks, UpdateType.Forecasts, 5), Is.Null,
                        $"Read {read + 1}: no forecast is running, so this row is waiting for nothing and must say so.");
                    Assert.That(WaitingBehindOf(tasks, UpdateType.Team, 1), Is.Null,
                        $"Read {read + 1}: work that is running is not waiting for anything, including itself.");
                }
            }
        }

        /// <summary>
        /// How long a row has been in the state it is in is measured on the instance, because a reader's
        /// clock is wrong by whatever their machine is wrong by.
        /// </summary>
        [Test]
        public void GetTasks_WorkHasBeenRunningForAWhile_SaysHowLongOnTheInstanceClock()
        {
            var startedAt = new DateTimeOffset(2026, 9, 19, 8, 0, 0, TimeSpan.Zero);
            var running = Work(UpdateType.Team, 1, UpdateProgress.InProgress);
            running.StartedAt = startedAt;
            updateStatuses[new UpdateKey(UpdateType.Team, 1)] = running;

            var tasks = TasksFrom(CreateSubject(new FakeLighthouseClock(startedAt.AddMinutes(3))));

            Assert.That(tasks.Single().ElapsedMs, Is.EqualTo(180_000));
        }

        /// <summary>
        /// The moment is written by whichever replica handled the transition and read by whichever answers
        /// the call, and their clocks do not agree to the millisecond. Something that started fractionally
        /// in the future has just started - a negative duration would be shown to an operator as one.
        /// </summary>
        [Test]
        public void GetTasks_WorkStartedFractionallyInTheFuture_SaysItHasJustStarted()
        {
            var now = new DateTimeOffset(2026, 9, 19, 8, 0, 0, TimeSpan.Zero);
            var running = Work(UpdateType.Team, 1, UpdateProgress.InProgress);
            running.StartedAt = now.AddSeconds(2);
            updateStatuses[new UpdateKey(UpdateType.Team, 1)] = running;

            var tasks = TasksFrom(CreateSubject(new FakeLighthouseClock(now)));

            Assert.That(tasks.Single().ElapsedMs, Is.Zero);
        }

        /// <summary>
        /// Nothing recorded when this row entered its state, so there is no honest number. Any stand-in is
        /// a duration a reader would believe.
        /// </summary>
        [Test]
        public void GetTasks_NobodyRecordedWhenTheWorkBegan_SaysNothingAboutHowLong()
        {
            updateStatuses[new UpdateKey(UpdateType.Team, 1)] = Work(UpdateType.Team, 1, UpdateProgress.InProgress);

            var tasks = TasksFrom(CreateSubject());

            Assert.That(tasks.Single().ElapsedMs, Is.Null);
        }

        /// <summary>
        /// A delete is not a refresh. Stopping one half-way would tell the caller waiting on it that the
        /// entity had gone while its row is still in the database.
        /// </summary>
        [TestCase(UpdateType.TeamDelete)]
        [TestCase(UpdateType.PortfolioDelete)]
        public async Task CancelTask_ARemoval_IsRefusedAndNothingIsStopped(UpdateType removal)
        {
            var queueServiceMock = new Mock<IUpdateQueueService>();

            var result = await CreateSubject(queueServiceMock.Object).CancelTask(removal, 1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
                queueServiceMock.Verify(queue => queue.CancelAsync(It.IsAny<UpdateKey>()), Times.Never);
            }
        }

        /// <summary>
        /// The row an operator clicked was drawn before they clicked it, so "it finished while you were
        /// reading" is the ordinary case rather than an error.
        /// </summary>
        [Test]
        public async Task CancelTask_ARefresh_IsPassedOnWhateverStateTheWorkIsIn()
        {
            var queueServiceMock = new Mock<IUpdateQueueService>();

            var result = await CreateSubject(queueServiceMock.Object).CancelTask(UpdateType.Team, 7);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<NoContentResult>());
                queueServiceMock.Verify(queue => queue.CancelAsync(new UpdateKey(UpdateType.Team, 7)), Times.Once);
            }
        }

        /// <summary>
        /// Enough reads that an answer picked arbitrarily out of the running work has to show itself.
        /// </summary>
        private const int ReadsThatMakeAnArbitraryAnswerShowItself = 20;

        /// <summary>
        /// What the naming falls back to for an entity no repository knows, which is every entity here.
        /// </summary>
        private const string TheRunningTeam = "Team 1";

        private static UpdateStatus Work(UpdateType updateType, int id, UpdateProgress status)
        {
            return new UpdateStatus { UpdateType = updateType, Id = id, Status = status };
        }

        private static List<UpdateController.UpdateTaskResponse> TasksFrom(UpdateController subject)
        {
            var okResult = subject.GetTasks().Result as OkObjectResult;

            return (List<UpdateController.UpdateTaskResponse>)okResult!.Value!;
        }

        private static string? WaitingBehindOf(List<UpdateController.UpdateTaskResponse> tasks, UpdateType updateType, int id)
        {
            return tasks.Single(task => task.UpdateType == updateType && task.Id == id).WaitingBehind;
        }

        /// <summary>
        /// The dictionary is still what backs the store these tests seed, but the controller only ever sees
        /// the port. Reading a dictionary directly is what made the old endpoint answer about one replica
        /// on a multi-replica instance.
        /// </summary>
        private UpdateController CreateSubject()
        {
            return CreateSubject(Clocks.SystemUtc, Mock.Of<IUpdateQueueService>());
        }

        private UpdateController CreateSubject(FakeLighthouseClock clock)
        {
            return CreateSubject(clock, Mock.Of<IUpdateQueueService>());
        }

        private UpdateController CreateSubject(IUpdateQueueService updateQueueService)
        {
            return CreateSubject(Clocks.SystemUtc, updateQueueService);
        }

        private UpdateController CreateSubject(ILighthouseClock clock, IUpdateQueueService updateQueueService)
        {
            return new UpdateController(
                new InProcessUpdateStatusStore(updateStatuses, clock),
                new UpdateTaskNaming(Mock.Of<IRepository<Team>>(), Mock.Of<IPortfolioRepository>()),
                clock,
                updateQueueService);
        }
    }
}