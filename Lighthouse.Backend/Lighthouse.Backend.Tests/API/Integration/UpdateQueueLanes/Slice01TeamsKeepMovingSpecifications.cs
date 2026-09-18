using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.API.Integration.UpdateQueueLanes
{
    /// <summary>
    /// DISTILL step definitions (Specifications) for ADO #5877 slice 01 — teams keep moving.
    ///
    /// The contract these steps fix, which is what DELIVER has to build:
    ///
    /// Work is routed into a lane by its update type at the moment it is admitted, and each lane runs
    /// one thing at a time. Team, Portfolio and Forecast are the three lanes; a removal runs in the
    /// lane of the entity it removes, so a removal and a refresh of the same kind of entity still
    /// serialise and a removal never waits on an unrelated kind.
    ///
    /// <c>GET /api/latest/update/tasks</c> keeps its route, its guard and its response shape. Only
    /// <c>waitingBehind</c> changes meaning: it names the entity holding the asking row's OWN lane, and
    /// is absent when that lane is free — rather than naming whichever running row the store happened
    /// to hand back first, which was correct only while one thing could run at a time.
    ///
    /// A refresh round still says its piece once however many executions were in it, and a shutdown
    /// drain still returns only once every lane is done.
    /// </summary>
    public partial class Slice01TeamsKeepMovingTest : UpdateQueueLanesAcceptanceTest
    {
        private readonly List<UpdateKey> admittedByHand = [];

        /// <summary>
        /// Work admitted by hand never runs, so nothing will ever take it back out of the store and the
        /// harness's wait for an idle queue would sit out its whole deadline for a key that was only
        /// ever a fixture. Runs before the harness teardown, because NUnit unwinds from the derived
        /// class outwards.
        /// </summary>
        [TearDown]
        public void ForgetWorkThatWasOnlyEverAFixture()
        {
            var store = Factory.Services.GetRequiredService<IUpdateStatusStore>();

            foreach (var key in admittedByHand)
            {
                store.Remove(key);
            }

            admittedByHand.Clear();
        }

        // --- Given ---

        /// <summary>
        /// A queued row whose own lane nothing is holding. Admitting straight through the store is how
        /// that state is reachable without racing the reader for it — and under Redis with more than one
        /// replica it is also the ordinary shape of work another pod admitted.
        /// </summary>
        private void GivenARefreshOfThatTeamIsAdmittedWithoutTheQueueEverReachingIt(SeededTeam team)
            => AdmitDirectly(new UpdateKey(UpdateType.Team, team.Id));

        /// <summary>
        /// A portfolio whose one feature is delivered by that team, which is what puts the team in the set
        /// of refreshes a forecast for this portfolio waits for, and what makes the team's refresh ask for
        /// one when it ends.
        /// </summary>
        private SeededPortfolio GivenAPortfolioDeliveredBy(SeededTeam team)
        {
            var portfolio = GivenAPortfolioThatIsRefreshedOnSchedule();

            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var feature = new Feature
            {
                Name = $"Feature {Guid.NewGuid():N}",
                ReferenceId = "FTR-1",
                Type = "Epic",
                State = "In Progress",
                StateCategory = StateCategories.Doing,
                Order = "1",
            };

            feature.Portfolios.Add(sp.GetRequiredService<IRepository<Portfolio>>().GetById(portfolio.Id)!);
            feature.FeatureWork.Add(new FeatureWork(sp.GetRequiredService<IRepository<Team>>().GetById(team.Id)!, 3, 3, feature));

            var features = sp.GetRequiredService<IRepository<Feature>>();
            features.Add(feature);
            features.Save().GetAwaiter().GetResult();

            return portfolio;
        }

        /// <summary>
        /// Only the portfolio's half of the tracker is held. The overlap this scenario needs is a team
        /// refresh that starts, finishes and asks for its forecast while the portfolio refresh feeding the
        /// same forecast is still fetching — which is unreachable while one gate holds both halves.
        /// </summary>
        private void GivenTheTrackerHoldsOnlyThePortfolioRefreshOpenUntilWeSaySo()
        {
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            thePortfolioRefreshMayFinish = gate;

            ConnectorMock
                .Setup(c => c.GetWorkItemsForTeam(It.IsAny<Team>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            ConnectorMock
                .Setup(c => c.GetFeaturesForProject(It.IsAny<Portfolio>(), It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    await gate.Task;
                    return [];
                });
        }

        private TaskCompletionSource? thePortfolioRefreshMayFinish;

        /// <summary>
        /// Runs before the harness tears the host down. A scenario that failed before releasing the gate
        /// would otherwise leave a portfolio refresh fetching against a database about to be deleted.
        /// </summary>
        [TearDown]
        public void LetThePortfolioRefreshFinish()
        {
            thePortfolioRefreshMayFinish?.TrySetResult();
        }

        /// <summary>
        /// A removal asked for the way the delete path asks for one — through the queue, as its own
        /// update type. The work itself is inert: what is under test is which lane the removal lands
        /// in, and that is decided from the update type at the moment it is admitted.
        /// </summary>
        private void GivenARemovalOfThatTeamIsAskedFor(SeededTeam team)
        {
            Factory.Services.GetRequiredService<IUpdateQueueService>()
                .EnqueueUpdate(UpdateType.TeamDelete, team.Id, _ => Task.CompletedTask);
        }

        private void AdmitDirectly(UpdateKey key)
        {
            var store = Factory.Services.GetRequiredService<IUpdateStatusStore>();

            store.TryAdmit(key, new UpdateStatus { UpdateType = key.UpdateType, Id = key.Id, Status = UpdateProgress.Queued });
            admittedByHand.Add(key);
        }

        // --- When ---

        /// <summary>
        /// Asked for and not waited on. Whether it gets going is the promise, so a step that waited for
        /// it to get going would have already decided the scenario's answer.
        /// </summary>
        private void WhenARefreshOfThatTeamIsAskedFor(SeededTeam team)
        {
            Factory.Services.GetRequiredService<ITeamUpdater>().TriggerUpdate(team.Id);
        }

        private Task WhenThatPortfolioRefreshRunsToTheEnd(SeededPortfolio portfolio)
            => ThePortfolioRefreshRuns(portfolio.Id);

        /// <summary>
        /// Started and run out, in its own lane, while the portfolio refresh beside it is still fetching.
        /// Waited for through the browser push rather than the store, because the store drops the key as
        /// the run ends and the forecast the team asks for is asked for inside that run.
        /// </summary>
        private async Task WhenARefreshOfThatTeamRunsToTheEndBesideIt(SeededTeam team)
        {
            Factory.Services.GetRequiredService<ITeamUpdater>().TriggerUpdate(team.Id);

            var finished = await TheBrowserIsToldItReached(KeyOf(team), UpdateProgress.Completed);

            Assert.That(finished, Is.True,
                "The team refresh has to have finished while the portfolio refresh is still going, or the two "
                + $"never overlapped and the scenario has nothing to observe. The browser was told: {TheBrowserWasTold.Describe()}");
        }

        // --- Then ---

        private async Task ThenThatTeamRefreshGetsGoing(SeededTeam team)
        {
            var gotGoing = await ItReaches(KeyOf(team), UpdateProgress.InProgress);

            Assert.That(gotGoing, Is.True,
                "A portfolio refresh that will not finish held every team refresh behind it for 3h38m, and the "
                + "operator read a working instance as a dead one. The team's lane is not the portfolio's, so "
                + "the team has nothing to wait for.");
        }

        private async Task ThenThatTeamRefreshIsStillGoing(SeededTeam team)
        {
            var stillGoing = await ItStaysAt(KeyOf(team), UpdateProgress.InProgress);

            Assert.That(stillGoing, Is.True,
                "Stopping one refresh must not stop the instance. A control that takes everything else down "
                + "with it is too dangerous to press, which makes it no better than no control.");
        }

        private async Task ThenThatPortfolioRefreshIsStillGoing(SeededPortfolio portfolio)
        {
            var stillGoing = await ItStaysAt(KeyOf(portfolio), UpdateProgress.InProgress);

            Assert.That(stillGoing, Is.True,
                "The portfolio refresh is the thing being worked around, not something to be finished early. "
                + "If it has ended, the scenario proved that a team refresh follows a portfolio one - which is "
                + "the behaviour being replaced.");
        }

        private async Task ThenThatPortfolioRefreshNeverGetsGoingWhileTheOtherHoldsItsLane(SeededPortfolio waiting)
        {
            var waitedItsTurn = await ItStaysAt(KeyOf(waiting), UpdateProgress.Queued);

            Assert.That(waitedItsTurn, Is.True,
                "Two portfolio refreshes at once would double what one tracker is asked for at a time, on an "
                + "on-premise instance already close to its limits. Keeping work of one kind serial is the "
                + "deliberate half of this change, not the part that was left undone.");
        }

        /// <summary>
        /// Read several times over, because the answer is picked out of a store several threads are
        /// writing to. One correct read of a value chosen by taking the first running row it finds says
        /// nothing at all about the next one — which is why the existing lane-holder specifications,
        /// green throughout, are not evidence here.
        /// </summary>
        private async Task ThenEveryReadOfThatQueuedRowNames(
            SeededTeam waiting,
            SeededTeam holdingTheTeamLane,
            SeededPortfolio runningInAnotherLane)
        {
            for (var read = 0; read < ReadsThatMakeAnArbitraryAnswerShowItself; read++)
            {
                var row = await TheRowFor(UpdateType.Team, waiting.Id);
                var waitingBehind = Text(row, "waitingBehind");

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(waitingBehind, Is.EqualTo(holdingTheTeamLane.Name),
                        $"Read {read + 1}: a queued row names what is holding its own lane. Anything else is a "
                        + "dependency the instance invented, shown to an operator trying to work out what is stuck.");
                    Assert.That(waitingBehind, Is.Not.EqualTo(runningInAnotherLane.Name),
                        $"Read {read + 1}: naming the portfolio tells an operator the team is waiting for it. It "
                        + "is not, and that is the one sentence this list exists to stop being said.");
                }
            }
        }

        /// <summary>
        /// Enough reads that a value chosen arbitrarily from a set of two has to show itself, and few
        /// enough that the scenario is still a scenario rather than a soak.
        /// </summary>
        private const int ReadsThatMakeAnArbitraryAnswerShowItself = 20;

        private async Task ThenThatQueuedRowIsWaitingBehindNothing(SeededTeam team)
        {
            var row = await TheRowFor(UpdateType.Team, team.Id);
            var namesNobody = !row.TryGetProperty("waitingBehind", out var behind) || behind.ValueKind is JsonValueKind.Null;

            Assert.That(namesNobody, Is.True,
                "Nothing is holding this row's lane, so there is nobody for it to be waiting behind. Naming the "
                + "running portfolio would be a fact the instance made up, and the operator has no way to tell.");
        }

        private void ThenTheRoundSaidItsPieceExactlyOnce(SeededPortfolio portfolio)
        {
            var lines = TheRoundSummaryLinesNaming(portfolio.Name);

            Assert.That(lines, Has.Count.EqualTo(1),
                "A refresh and the forecast it triggers are two executions of one round, and with a lane each "
                + "they can both be in it at once. Two lines is one refresh read as two; none is a round that "
                + "finished without anybody speaking for it, which is also how everything it staged gets "
                + $"dropped without a word. Lines seen: {string.Join(" | ", lines)}");
        }

        /// <summary>
        /// Counted on the production seam rather than on a double: a forecast execution writes one refresh
        /// history row about itself, so this counts runs that actually happened and cannot be satisfied by
        /// something that was never called. The gate is released first and the queue is given until it
        /// settles, because a second forecast that has not run yet is the failure this is looking for.
        /// </summary>
        private async Task ThenThatPortfolioIsForecastExactlyOnce(SeededPortfolio portfolio)
        {
            LetThePortfolioRefreshFinish();
            await TheQueueGoesIdle();

            var history = await TheRefreshHistory();
            var forecasts = history
                .Where(row => Text(row, "type") == nameof(RefreshType.Forecast) && Number(row, "entityId") == portfolio.Id)
                .ToList();

            Assert.That(forecasts, Has.Count.EqualTo(1),
                "The simulation is not seeded, so a second forecast of the same portfolio moves the delivery date "
                + "the first one just showed, and the operator has no way to tell which one to believe. None at all "
                + "means the refresh that asked for one was dropped, and the portfolio keeps the date it had until "
                + $"the next periodic refresh. Rows recorded: {Describe(history)}");
        }

        private async Task ThenThatPortfolioRefreshStops(SeededPortfolio portfolio)
        {
            ReleaseTheTracker();

            var stopped = await TheBrowserIsToldItReached(KeyOf(portfolio), UpdateProgress.Cancelled);

            Assert.That(stopped, Is.True,
                "The refresh the operator asked to stop is the one that has to stop. "
                + $"The browser was told: {TheBrowserWasTold.Describe()}");
        }

        /// <summary>
        /// The drain is asked for while both lanes are held, and only then is the tracker let go. A
        /// drain that returned early would return here, with the release still ahead of it.
        /// </summary>
        private async Task ThenShuttingDownReturnsOnlyOnceBothLanesAreDone(SeededPortfolio portfolio, SeededTeam team)
        {
            var store = Factory.Services.GetRequiredService<IUpdateStatusStore>();
            var draining = Factory.Services.GetRequiredService<IUpdateQueueService>().DrainAsync(CancellationToken.None);

            ReleaseTheTracker();

            var returned = await Task.WhenAny(draining, Task.Delay(PatienceForWorkToMove));
            var stillAdmitted = store.TryGet(KeyOf(portfolio), out _) || store.TryGet(KeyOf(team), out _);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(returned, Is.SameAs(draining),
                    "A drain that never returns is a shutdown that hangs, and the host kills it with work "
                    + "half-done against a database it is about to take away.");
                Assert.That(stillAdmitted, Is.False,
                    "Work still admitted after the drain returned is work another replica can see and nobody "
                    + "is doing - and it is the reason the entity can never be refreshed again.");
            }
        }

        private async Task ThenThatRemovalIsWaitingForTheTeamRefreshAndNotForThePortfolioOne(
            SeededTeam team,
            SeededPortfolio runningInAnotherLane)
        {
            var removal = new UpdateKey(UpdateType.TeamDelete, team.Id);
            var waitedItsTurn = await ItStaysAt(removal, UpdateProgress.Queued);
            var row = await TheRowFor(UpdateType.TeamDelete, team.Id);
            var waitingBehind = Text(row, "waitingBehind");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(waitedItsTurn, Is.True,
                    "A removal that ran beside a refresh of the same team would be two things writing one entity "
                    + "at once. Sharing the entity type's lane is what keeps that impossible.");
                Assert.That(waitingBehind, Is.EqualTo(team.Name),
                    "The removal is waiting for the refresh of the same team, which is the only thing in its way.");
                Assert.That(waitingBehind, Is.Not.EqualTo(runningInAnotherLane.Name),
                    "A removal held up by an unrelated portfolio refresh is the reported bug with a different "
                    + "pair of update types in it.");
            }
        }
    }
}
