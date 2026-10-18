using System.Net;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Tests.API.Integration.TaskManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Lighthouse.Backend.Tests.API.Integration.ResponsiveDuringUpdates
{
    /// <summary>
    /// After a Jira outage Ingrid's Activity list holds one running refresh and a queue behind it. She
    /// clicks Cancel all: what is queued leaves at once, what is running stops, nothing she stopped comes
    /// back as a follow-up, removals are left to finish, and anything asked for afterwards runs as usual.
    ///
    /// The driving port is the route the control calls, over HTTP, against the production queue. A
    /// tracker that answers one page at a time holds the running refresh open, as in per-row Stop.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6311-responsive-during-updates")]
    [Category("slice-05")]
    [Category("us-04")]
    public class Slice05CancelAllScenarios : TaskManagerAcceptanceTest
    {
        private const string CancelAllRoute = "/api/latest/update/tasks/cancel";

        private const int PagesTheTrackerCouldGiveForever = 10_000;

        private static readonly TimeSpan TheLongestAScenarioWaits = TimeSpan.FromSeconds(30);

        private TaskCompletionSource theTrackerMayAnswer = null!;

        private TaskCompletionSource theRemovalMayFinish = null!;

        private TaskCompletionSource theRemovalFinished = null!;

        private TaskCompletionSource whatWasHeldWasLetGo = null!;

        [SetUp]
        public void TheTrackerAnswersOnePageAtATimeUntilWeSaySo()
        {
            theTrackerMayAnswer = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            theRemovalMayFinish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            theRemovalFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            whatWasHeldWasLetGo = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            ConnectorMock
                .Setup(c => c.GetWorkItemsForTeam(It.IsAny<Team>(), It.IsAny<CancellationToken>()))
                .Returns(async (Team _, CancellationToken stopping) =>
                {
                    for (var page = 0; page < PagesTheTrackerCouldGiveForever && !theTrackerMayAnswer.Task.IsCompleted; page++)
                    {
                        stopping.ThrowIfCancellationRequested();
                        await Task.Delay(20, CancellationToken.None);
                    }

                    return [];
                });
        }

        [TearDown]
        public async Task LetEverythingGatedFinish()
        {
            theTrackerMayAnswer.TrySetResult();
            theRemovalMayFinish.TrySetResult();
            await TheQueueGoesIdle();
        }

        [Test]
        [Ignore(PendingEpic6311.Slice05)]
        [Category("ac-05.1")]
        [Category("walking_skeleton")]
        [Category("driving_port")]
        [Category("contract-shape:bounded-change")]
        public async Task Cancel_all_clears_every_queued_refresh_at_once_and_stops_the_running_one()
        {
            var running = GivenATeamThatIsRefreshedOnSchedule();
            var waiting = GivenATeamThatIsRefreshedOnSchedule();
            var portfolio = SeedPortfolio(SeedConnection(), $"Q4 Platform {Guid.NewGuid():N}");

            await WhenARefreshIsUnderWay(UpdateType.Team, running.Id);
            await WhenARefreshIsAlsoAskedFor(UpdateType.Team, waiting.Id);
            await WhenARefreshIsAlsoAskedFor(UpdateType.Features, portfolio);

            await WhenTheOperatorCancelsAll();
            var rightAfter = await TheTaskList();

            theTrackerMayAnswer.TrySetResult();
            await TheQueueGoesIdle();
            var atTheEnd = await TheTaskList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rightAfter.Where(row => Text(row, "status") == nameof(UpdateProgress.Queued)), Is.Empty,
                    $"Queued work was still listed after Cancel all was accepted. Got: {Describe(rightAfter)}");
                Assert.That(TheBrowserWasTold.LastStatusOf(UpdateType.Team, running.Id), Is.EqualTo(UpdateProgress.Cancelled),
                    $"The running refresh was not stopped. The browser was told: {TheBrowserWasTold.Describe()}");
                Assert.That(atTheEnd, Is.Empty, $"Something was still listed once the queue went idle. Got: {Describe(atTheEnd)}");
            }
        }

        [Test]
        [Ignore(PendingEpic6311.Slice05)]
        [Category("ac-05.4")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_removal_on_the_list_is_left_to_finish()
        {
            var running = GivenATeamThatIsRefreshedOnSchedule();
            var leaving = GivenATeamThatIsRefreshedOnSchedule();

            await WhenARefreshIsUnderWay(UpdateType.Team, running.Id);
            await GivenTheRemovalOfATeamIsWaiting(leaving);

            await WhenTheOperatorCancelsAll();
            var rightAfter = await TheRowFor(UpdateType.TeamDelete, leaving.Id);

            theTrackerMayAnswer.TrySetResult();
            theRemovalMayFinish.TrySetResult();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Text(rightAfter, "status"), Is.AnyOf(nameof(UpdateProgress.Queued), nameof(UpdateProgress.InProgress)),
                    "A removal stopped half-way leaves the Team's rows in the database while its caller is told it is gone.");
                Assert.That(async () => await theRemovalFinished.Task.WaitAsync(TheLongestAScenarioWaits), Throws.Nothing,
                    "The removal never ran after Cancel all.");
            }
        }

        [Test]
        [Ignore(PendingEpic6311.Slice05)]
        [Category("ac-05.2")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_follow_up_parked_behind_the_running_refresh_never_runs()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();

            await WhenARefreshIsUnderWay(UpdateType.Team, team.Id);
            Factory.Services.GetRequiredService<ITeamUpdater>().TriggerUpdate(team.Id);

            await WhenTheOperatorCancelsAll();
            theTrackerMayAnswer.TrySetResult();
            await TheQueueGoesIdle();

            var whatTheBrowserWasTold = TheBrowserWasTold.For(UpdateType.Team, team.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(whatTheBrowserWasTold, Has.Member(UpdateProgress.Cancelled),
                    $"The running refresh was not stopped, so there was nothing for a follow-up to come back after. Told: {TheBrowserWasTold.Describe()}");
                Assert.That(whatTheBrowserWasTold.Count(progress => progress == UpdateProgress.InProgress), Is.EqualTo(1),
                    $"The refresh stopped by Cancel all came straight back as its own follow-up. Told: {TheBrowserWasTold.Describe()}");
                Assert.That(whatTheBrowserWasTold[^1], Is.EqualTo(UpdateProgress.Cancelled));
            }
        }

        [Test]
        [Ignore(PendingEpic6311.Slice05)]
        [Category("ac-05.2")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_forecast_held_until_a_Team_finishes_is_let_go_without_running()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();

            await WhenARefreshIsUnderWay(UpdateType.Team, team.Id);
            GivenAForecastIsHeldBehind(team);

            var heldBeforeTheClick = Factory.Services.GetRequiredService<IUpdateQueueService>()
                .IsHeld(new UpdateKey(UpdateType.Forecasts, team.Id));

            await WhenTheOperatorCancelsAll();
            theTrackerMayAnswer.TrySetResult();
            await TheQueueGoesIdle();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(heldBeforeTheClick, Is.True, "The forecast was never held behind the Team, so there was nothing to clear.");
                Assert.That(whatWasHeldWasLetGo.Task.IsCompleted, Is.False,
                    "A forecast held behind the Team ran once the Team stopped, so Cancel all did not clear it.");
            }
        }

        [Test]
        [Ignore(PendingEpic6311.Slice05)]
        [Category("ac-05.3")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_refresh_asked_for_after_Cancel_all_runs_as_usual()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();

            await WhenARefreshIsUnderWay(UpdateType.Team, team.Id);
            await WhenTheOperatorCancelsAll();
            theTrackerMayAnswer.TrySetResult();
            await TheQueueGoesIdle();

            await TheTeamRefreshRuns(team.Id);

            Assert.That(TheBrowserWasTold.LastStatusOf(UpdateType.Team, team.Id), Is.EqualTo(UpdateProgress.Completed),
                $"A scheduled refresh after Cancel all did not run. The browser was told: {TheBrowserWasTold.Describe()}");
        }

        [Test]
        [Ignore(PendingEpic6311.Slice05)]
        [Category("ac-05.5")]
        [Category("edge")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task Cancel_all_with_nothing_to_cancel_is_accepted_and_can_be_asked_again()
        {
            await WhenTheOperatorCancelsAll();
            await WhenTheOperatorCancelsAll();

            Assert.That(await TheTaskList(), Is.Empty);
        }

        [Test]
        [Ignore(PendingEpic6311.Slice05)]
        [Category("ac-05.5")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task Cancel_all_is_refused_to_somebody_who_is_not_a_system_administrator()
        {
            using var factory = RootFactory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                {
                    var refuses = new Mock<IRbacAdministrationService>();
                    refuses
                        .Setup(s => s.CanSatisfyRequirementAsync(
                            It.IsAny<System.Security.Claims.ClaimsPrincipal>(),
                            It.IsAny<Lighthouse.Backend.Models.Authorization.RbacGuardRequirement>(),
                            It.IsAny<int?>(),
                            It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

                    services.RemoveAll<IRbacAdministrationService>();
                    services.AddScoped(_ => refuses.Object);
                }));

            using var client = factory.CreateClient();
            using var cancelAll = await client.PostAsync(new Uri(CancelAllRoute, UriKind.Relative), null);

            Assert.That(cancelAll.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden),
                "Stopping everything on an instance is at least as privileged as reading what is running.");
        }

        // --- Given ---

        private async Task GivenTheRemovalOfATeamIsWaiting(SeededTeam team)
        {
            Factory.Services.GetRequiredService<IUpdateQueueService>().EnqueueUpdate(UpdateType.TeamDelete, team.Id, async _ =>
            {
                await theRemovalMayFinish.Task;
                theRemovalFinished.TrySetResult();
            });

            await TheStatusReaches(UpdateType.TeamDelete, team.Id, UpdateProgress.Queued);
        }

        private void GivenAForecastIsHeldBehind(SeededTeam team)
            => Factory.Services.GetRequiredService<IUpdateQueueService>().HoldUntilQueuedWorkClears(
                new UpdateKey(UpdateType.Forecasts, team.Id),
                [new UpdateKey(UpdateType.Team, team.Id)],
                () => whatWasHeldWasLetGo.TrySetResult());

        // --- When ---

        private async Task WhenARefreshIsUnderWay(UpdateType updateType, int id)
        {
            Trigger(updateType, id);
            await TheStatusReaches(updateType, id, UpdateProgress.InProgress);
        }

        private async Task WhenARefreshIsAlsoAskedFor(UpdateType updateType, int id)
        {
            Trigger(updateType, id);
            await TheStatusReaches(updateType, id, UpdateProgress.Queued);
        }

        private async Task WhenTheOperatorCancelsAll()
        {
            using var client = Factory.CreateClient();
            using var response = await client.PostAsync(new Uri(CancelAllRoute, UriKind.Relative), null);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent),
                $"Cancel all is the driving port for this slice; it answered {(int)response.StatusCode}.");
        }

        private void Trigger(UpdateType updateType, int id)
        {
            if (updateType == UpdateType.Features)
            {
                Factory.Services.GetRequiredService<IPortfolioUpdater>().TriggerUpdate(id);
                return;
            }

            Factory.Services.GetRequiredService<ITeamUpdater>().TriggerUpdate(id);
        }

        private async Task TheStatusReaches(UpdateType updateType, int id, UpdateProgress reached)
        {
            var store = Factory.Services.GetRequiredService<IUpdateStatusStore>();
            var key = new UpdateKey(updateType, id);
            var deadline = DateTime.UtcNow + TheLongestAScenarioWaits;

            while (DateTime.UtcNow < deadline)
            {
                if (store.TryGet(key, out var status) && status?.Status == reached)
                {
                    return;
                }

                await Task.Delay(20);
            }

            Assert.Fail($"{updateType} {id} never reached {reached}; the scenario cannot cancel it.");
        }
    }
}
