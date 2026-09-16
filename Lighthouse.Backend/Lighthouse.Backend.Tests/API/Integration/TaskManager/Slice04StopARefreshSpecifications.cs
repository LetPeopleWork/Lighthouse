using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;
using System.Net;

namespace Lighthouse.Backend.Tests.API.Integration.TaskManager
{
    /// <summary>
    /// DISTILL step definitions (Specifications) for Epic #5511 slice 04 — stop a refresh.
    ///
    /// The contract these steps fix, which is what DELIVER has to build:
    ///
    /// <c>POST /api/latest/update/tasks/{updateType}/{id}/cancel</c>, System-Administrator-guarded,
    /// answering <c>204</c>. Idempotent in the strongest sense: it is accepted for work that is running,
    /// waiting, already finished, or was never admitted at all, and answers the same way for each. The row
    /// an operator clicked was drawn before they clicked it, so every one of those is an ordinary outcome
    /// rather than a mistake to report back.
    ///
    /// Queued work leaves the store without the tracker ever being contacted. Running work stops at its
    /// next page, still finishes its write-back round and still releases anything held behind its key, and
    /// the browser is told <c>Cancelled</c>.
    ///
    /// A tracker that answers one page at a time is how a running refresh is held open long enough to be
    /// cancelled. A synthetic sleep would prove only that a token was observed somewhere; paging is where
    /// the wall-clock actually is, and AC-04.8 asks for a refresh long enough to be cancelled mid-flight
    /// rather than one arranged to be.
    /// </summary>
    public partial class Slice04StopARefreshTest : TaskManagerAcceptanceTest
    {
        private TaskCompletionSource theTrackerMayAnswer = null!;

        private readonly List<string> teamsTheTrackerWasAskedAbout = [];

        private int pagesTheTrackerWasAskedFor;

        /// <summary>
        /// Runs before the harness tears the host down, because NUnit unwinds from the derived class
        /// outwards. A scenario that left the tracker gated would otherwise leave a refresh parked in the
        /// queue while the database underneath it is deleted.
        /// </summary>
        [TearDown]
        public async Task LetAnyGatedRefreshFinish()
        {
            theTrackerMayAnswer?.TrySetResult();
            await TheQueueGoesIdle();
        }

        // --- Given ---

        private void GivenTheTrackerDoesNotAnswerUntilWeSaySo()
        {
            theTrackerMayAnswer = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            ConnectorMock
                .Setup(c => c.GetWorkItemsForTeam(It.IsAny<Team>(), It.IsAny<CancellationToken>()))
                .Returns(async (Team team, CancellationToken _) =>
                {
                    lock (teamsTheTrackerWasAskedAbout)
                    {
                        teamsTheTrackerWasAskedAbout.Add(team.Name);
                    }

                    await theTrackerMayAnswer.Task;
                    return [];
                });
        }

        /// <summary>
        /// A refresh held open the way a real slow one is held open: the connector is still going, page
        /// after page, and each page is a point at which it could notice it has been asked to stop. The
        /// count is what makes "it stopped" observable - a refresh that ran to completion would ask for
        /// every page there is.
        /// </summary>
        private void GivenTheTrackerAnswersOnePageAtATimeUntilWeSaySo()
        {
            theTrackerMayAnswer = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            ConnectorMock
                .Setup(c => c.GetWorkItemsForTeam(It.IsAny<Team>(), It.IsAny<CancellationToken>()))
                .Returns(async (Team team, CancellationToken stopping) =>
                {
                    lock (teamsTheTrackerWasAskedAbout)
                    {
                        teamsTheTrackerWasAskedAbout.Add(team.Name);
                    }

                    // A tracker with more to give than this refresh will ever ask for. Each turn of the loop
                    // is a page, and a page is where a real connector notices it has been told to stop - so
                    // a refresh that carries on looping here is one that ignored the ask.
                    for (var page = 0; page < PagesTheTrackerCouldGiveForever; page++)
                    {
                        stopping.ThrowIfCancellationRequested();
                        Interlocked.Increment(ref pagesTheTrackerWasAskedFor);
                        await Task.Delay(20, CancellationToken.None);
                    }

                    await theTrackerMayAnswer.Task;
                    return [];
                });
        }

        /// <summary>
        /// Far more pages than any scenario here lets run, so "it stopped" cannot be satisfied by a tracker
        /// that simply ran out of things to say.
        /// </summary>
        private const int PagesTheTrackerCouldGiveForever = 10_000;

        private void GivenTheTrackerAnswersNormallyAgain()
        {
            theTrackerMayAnswer?.TrySetResult();
            ConnectorMock.Setup(c => c.GetWorkItemsForTeam(It.IsAny<Team>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        }

        /// <summary>
        /// Something parked behind the key that is about to be cancelled, through the production hold that
        /// every updater uses. The release is what a later assertion watches for.
        /// </summary>
        private void GivenSomethingIsHeldBehind(SeededTeam team)
        {
            WhatWasHeldBehind = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            Factory.Services.GetRequiredService<IUpdateQueueService>().HoldUntilQueuedWorkClears(
                new UpdateKey(UpdateType.Forecasts, team.Id),
                [new UpdateKey(UpdateType.Team, team.Id)],
                () => WhatWasHeldBehind.TrySetResult());
        }

        private TaskCompletionSource WhatWasHeldBehind = null!;

        // --- When ---

        private Task WhenARefreshOfThatTeamIsUnderWay(SeededTeam team)
            => StartRefreshAndWaitUntil(team, UpdateProgress.InProgress);

        private Task WhenARefreshOfThatTeamIsAlsoAskedFor(SeededTeam team)
            => StartRefreshAndWaitUntil(team, UpdateProgress.Queued);

        private async Task WhenTheOperatorCancels(SeededTeam team)
        {
            using var client = Factory.CreateClient();
            using var response = await client.PostAsync(CancelRouteFor(UpdateType.Team, team.Id), null);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent),
                $"Cancel is the driving port for this slice; it answered {(int)response.StatusCode}.");
        }

        private async Task StartRefreshAndWaitUntil(SeededTeam team, UpdateProgress reached)
        {
            var key = new UpdateKey(UpdateType.Team, team.Id);

            Factory.Services.GetRequiredService<ITeamUpdater>().TriggerUpdate(team.Id);

            var store = Factory.Services.GetRequiredService<IUpdateStatusStore>();
            var deadline = DateTime.UtcNow.AddSeconds(30);

            while (DateTime.UtcNow < deadline)
            {
                if (store.TryGet(key, out var status) && status?.Status == reached)
                {
                    return;
                }

                await Task.Delay(20);
            }

            Assert.Fail($"{key.UpdateType} {key.Id} never reached {reached}; the scenario cannot cancel it.");
        }

        // --- Then ---

        private async Task ThenTheTrackerWasNeverAskedAbout(SeededTeam team)
        {
            // The cancelled team is queued behind one that is still gated, so nothing moves until the
            // tracker is let go. Releasing here is what lets the cancelled key reach the runner at all -
            // and reaching it without contacting the tracker is the whole claim.
            theTrackerMayAnswer.TrySetResult();
            await TheQueueSettles(team);

            lock (teamsTheTrackerWasAskedAbout)
            {
                Assert.That(teamsTheTrackerWasAskedAbout, Does.Not.Contain(team.Name),
                    "Work that never started is the one case where stopping it can be absolute. Reaching the "
                    + "tracker at all spends the rate limit an operator cancelled to protect.");
            }
        }

        private async Task ThenThatRefreshStopsBeforeItHasReadEveryPage(SeededTeam team)
        {
            var pagesWhenCancelled = Volatile.Read(ref pagesTheTrackerWasAskedFor);
            theTrackerMayAnswer.TrySetResult();

            await TheQueueSettles(team);

            Assert.That(Volatile.Read(ref pagesTheTrackerWasAskedFor), Is.EqualTo(pagesWhenCancelled),
                "A refresh that carried on paging after being cancelled is a button that does not work - and "
                + "the pages it reads after the click are exactly the ones an operator pressed it to stop.");
        }

        private async Task ThenTheBrowserWasToldThatRefreshWasCancelled(SeededTeam team)
        {
            theTrackerMayAnswer.TrySetResult();
            await TheQueueSettles(team);

            Assert.That(TheBrowserWasTold.LastStatusOf(UpdateType.Team, team.Id), Is.EqualTo(UpdateProgress.Cancelled),
                "A row that still says 'running' after a cancel reads as a button that did nothing. "
                + $"The browser was told: {TheBrowserWasTold.Describe()}");
        }

        private async Task ThenTheWriteBackRoundWasAccountedFor()
        {
            theTrackerMayAnswer.TrySetResult();
            await TheQueueGoesIdle();

            Assert.That(WriteBackFlushes.Value, Is.GreaterThan(0),
                "A round that never finishes drops every write it staged, silently. A cancelled run owes its "
                + "round the same report a run that finished does - the same invariant a failed run carries, "
                + "reached through a different door.");
        }

        private async Task ThenWhatWasHeldBehindItWasLetGo()
        {
            theTrackerMayAnswer.TrySetResult();

            var released = await Task.WhenAny(WhatWasHeldBehind.Task, Task.Delay(TimeSpan.FromSeconds(30)));

            Assert.That(released, Is.SameAs(WhatWasHeldBehind.Task),
                "Held work waits on a key leaving the queue, not on it leaving successfully. A cancel that "
                + "strands it parks that work until something unrelated happens to poke the same key.");
        }

        private async Task ThenAFreshRefreshOfThatTeamCompletes(SeededTeam team)
        {
            await TheQueueGoesIdle();
            TheBrowserWasTold.Clear();

            await RunUpdate(sp => sp.GetRequiredService<ITeamUpdater>().TriggerUpdate(team.Id));

            Assert.That(TheBrowserWasTold.LastStatusOf(UpdateType.Team, team.Id), Is.EqualTo(UpdateProgress.Completed),
                "A cancel has to leave a state a later refresh corrects. If the next run cannot finish, the "
                + "cancel traded a runaway refresh for a stuck entity, which is the worse of the two. "
                + $"The browser was told: {TheBrowserWasTold.Describe()}");
        }

        private async Task ThenCancellingIsAcceptedFor(SeededTeam team)
        {
            using var client = Factory.CreateClient();
            using var response = await client.PostAsync(CancelRouteFor(UpdateType.Team, team.Id), null);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent),
                "The row was drawn before it was clicked, so work that has already finished - or was never "
                + "admitted - is the ordinary case. Answering an error puts a failure in front of somebody "
                + "who did nothing wrong, and invites them to press it again.");
        }

        /// <summary>
        /// Both halves are the promise. The refusal is what stops a half-finished delete, and the sentence
        /// is the only thing the operator gets: the popover shows the row unchanged afterwards and says
        /// nothing of its own about why the control did not work.
        /// </summary>
        private async Task ThenCancellingARemovalOfThatTeamIsRefusedWithAReasonToShow(SeededTeam team)
        {
            using var client = Factory.CreateClient();
            using var response = await client.PostAsync(CancelRouteFor(UpdateType.TeamDelete, team.Id), null);
            var explanation = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
                    "A deletion is not a refresh. Stopping one half-way leaves the caller waiting on it told "
                    + "the entity has gone while its row is still in the database, so the honest answer to a "
                    + $"control that offers to stop refreshing is to refuse. Answered {(int)response.StatusCode}.");

                Assert.That(explanation, Does.Contain("deletion cannot be cancelled"),
                    "A refusal an operator cannot read is a button that silently does nothing. Nothing else "
                    + $"tells them why the removal is still going. Answered: '{explanation}'");
            }
        }

        private async Task ThenTheTaskListIsAnsweredAndEmpty()
        {
            var rows = await TheTaskList();

            Assert.That(rows, Is.Empty, $"Nothing was ever running. Got: {Describe(rows)}");
        }

        private async Task ThenTheTaskListDoesNotMention(SeededTeam team)
        {
            theTrackerMayAnswer.TrySetResult();
            await TheQueueGoesIdle();

            var rows = await TheTaskList();

            Assert.That(
                rows.Any(row => Text(row, "updateType") == nameof(UpdateType.Team) && Number(row, "id") == team.Id),
                Is.False,
                $"Cancelled work is not work the instance is doing. Got: {Describe(rows)}");
        }

        /// <summary>
        /// Read with the refresh ahead of it still gated — which is the whole difference from
        /// <see cref="ThenTheTaskListDoesNotMention"/>, and the reason that one could not see this. An
        /// operator who has been told the cancel was accepted is looking at the list *now*, with whatever
        /// they did not cancel still running.
        /// </summary>
        private async Task ThenTheTaskListAlreadyDoesNotMention(SeededTeam team)
        {
            var rows = await TheTaskList();

            Assert.That(
                rows.Any(row => Text(row, "updateType") == nameof(UpdateType.Team) && Number(row, "id") == team.Id),
                Is.False,
                "The cancel was accepted, so the instance is no longer going to do this - and the row is the "
                + "only thing saying otherwise. Leaving it until the queue reaches it means an operator who "
                + $"reloads sees work they already stopped. Got: {Describe(rows)}");
        }

        private async Task ThenThatTeamIsStillOnTheTaskList(SeededTeam team)
        {
            var row = await TheRowFor(UpdateType.Team, team.Id);

            Assert.That(Text(row, "status"), Is.Not.EqualTo(nameof(UpdateProgress.Cancelled)),
                "Cancel is per entity. A button that stops one runaway refresh and takes the rest of the "
                + "instance with it is too dangerous to press, which makes it no better than no button.");
        }

        /// <summary>
        /// AC-04.6 asks for the same refusal the list gives, so the scenario asks both and compares rather
        /// than writing a status code down twice and letting them drift apart.
        /// </summary>
        private async Task ThenCancellingRefusesANonAdministratorTheSameWayTheTaskListDoes(SeededTeam team)
        {
            using var factory = RootFactory.WithWebHostBuilder(builder =>
            {
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
                });
            });

            using var client = factory.CreateClient();
            using var cancel = await client.PostAsync(CancelRouteFor(UpdateType.Team, team.Id), null);
            using var taskList = await client.GetAsync(new Uri(TaskListRoute, UriKind.Relative));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(cancel.StatusCode, Is.Not.EqualTo(HttpStatusCode.NoContent),
                    "Stopping every refresh on an instance is at least as privileged as reading what they are.");
                Assert.That(cancel.StatusCode, Is.EqualTo(taskList.StatusCode),
                    "A cancel guarded differently from the list it is reached from is a difference an operator "
                    + "has to learn for no reason.");
            }
        }

        private static Uri CancelRouteFor(UpdateType updateType, int id)
            => new($"{TaskListRoute}/{updateType}/{id}/cancel", UriKind.Relative);

        /// <summary>
        /// A cancel is asynchronous by nature - it asks, and the run stops when it next looks. Settling is
        /// the key leaving the store, which is what every terminal path ends with.
        /// </summary>
        private async Task TheQueueSettles(SeededTeam team)
        {
            var store = Factory.Services.GetRequiredService<IUpdateStatusStore>();
            var key = new UpdateKey(UpdateType.Team, team.Id);
            var deadline = DateTime.UtcNow.AddSeconds(30);

            while (DateTime.UtcNow < deadline)
            {
                if (!store.TryGet(key, out _))
                {
                    return;
                }

                await Task.Delay(20);
            }

            Assert.Fail($"Team {team.Id} was still admitted 30s after being cancelled; a cancel that never "
                + "settles leaves the key unable to be refreshed again at all.");
        }
    }
}
