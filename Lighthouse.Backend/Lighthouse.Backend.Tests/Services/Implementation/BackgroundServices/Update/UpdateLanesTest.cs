using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices.Update
{
    /// <summary>
    /// The lanes on their own, without an instance around them. What the acceptance scenarios observe
    /// through a running host is asserted here against the class that actually decides it, which is the
    /// only place the behaviour can be pinned without paying for a web host per assertion.
    /// </summary>
    public class UpdateLanesTest
    {
        private static readonly UpdateKey ATeam = new(UpdateType.Team, 1);

        private static readonly UpdateKey AnotherTeam = new(UpdateType.Team, 2);

        private static readonly UpdateKey APortfolio = new(UpdateType.Features, 3);

        private static readonly UpdateKey ATeamRemoval = new(UpdateType.TeamDelete, 1);

        /// <summary>
        /// Long enough that a machine under load is not mistaken for a lane that never ran the work, and
        /// short enough that a lane which genuinely never runs it fails rather than hangs.
        /// </summary>
        private static readonly TimeSpan PatienceForWorkToRun = TimeSpan.FromSeconds(10);

        /// <summary>
        /// A window for claims that something does NOT happen. Deliberately a small fraction of the
        /// patience above: it catches work that starts immediately, which is what the claim is about.
        /// </summary>
        private static readonly TimeSpan WindowForSomethingThatShouldNotHappen = TimeSpan.FromMilliseconds(300);

        private Mock<ILogger> loggerMock;

        private List<string> whatWasLogged;

        [SetUp]
        public void Setup()
        {
            whatWasLogged = [];
            loggerMock = new Mock<ILogger>();
            loggerMock
                .Setup(logger => logger.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
                .Callback(new InvocationAction(invocation =>
                {
                    var state = invocation.Arguments[2];
                    var error = (Exception?)invocation.Arguments[3];
                    var format = invocation.Arguments[4];

                    var message = (string?)format.GetType()
                        .GetMethod("Invoke")!
                        .Invoke(format, [state, error]);

                    whatWasLogged.Add($"{invocation.Arguments[0]}: {message}");
                }));
        }

        [Test]
        public async Task WriteTo_TheLaneRunsTheWork()
        {
            using var subject = new UpdateLanes(loggerMock.Object);
            var ran = new TaskCompletionSource();

            var taken = subject.WriteTo(ATeam, () =>
            {
                ran.TrySetResult();
                return Task.CompletedTask;
            });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(taken, Is.True, "An open lane takes the work it is handed.");
                Assert.That(await Finished(ran.Task), Is.True, "The work was taken and then never run.");
            }
        }

        /// <summary>
        /// The whole reason the class exists. A portfolio refresh that never finishes held every team
        /// refresh behind it for hours; a lane each is what stops that.
        /// </summary>
        [Test]
        public async Task WriteTo_WorkOfOneKindIsStuck_WorkOfAnotherKindStillRuns()
        {
            using var subject = new UpdateLanes(loggerMock.Object);
            var thePortfolioMayFinish = new TaskCompletionSource();
            var theTeamRan = new TaskCompletionSource();

            subject.WriteTo(APortfolio, () => thePortfolioMayFinish.Task);
            subject.WriteTo(ATeam, () =>
            {
                theTeamRan.TrySetResult();
                return Task.CompletedTask;
            });

            var teamGotGoing = await Finished(theTeamRan.Task);
            thePortfolioMayFinish.TrySetResult();

            Assert.That(teamGotGoing, Is.True,
                "The team's lane is not the portfolio's, so a portfolio refresh that will not finish is "
                + "nothing for a team refresh to wait for.");
        }

        /// <summary>
        /// The deliberate half. Two refreshes of one kind at once would double what one work tracking
        /// system is asked for, which is the opposite of what the instance that reported this needs.
        /// </summary>
        [Test]
        public async Task WriteTo_TwoOfTheSameKind_TheSecondWaitsForTheFirst()
        {
            using var subject = new UpdateLanes(loggerMock.Object);
            var theFirstMayFinish = new TaskCompletionSource();
            var theSecondRan = new TaskCompletionSource();

            subject.WriteTo(ATeam, () => theFirstMayFinish.Task);
            subject.WriteTo(AnotherTeam, () =>
            {
                theSecondRan.TrySetResult();
                return Task.CompletedTask;
            });

            var secondJumpedTheQueue = await Happened(theSecondRan.Task, WindowForSomethingThatShouldNotHappen);
            theFirstMayFinish.TrySetResult();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(secondJumpedTheQueue, Is.False, "Work of one kind runs one at a time.");
                Assert.That(await Finished(theSecondRan.Task), Is.True,
                    "Once the first one is done the lane has to reach the second, or waiting would be stalling.");
            }
        }

        /// <summary>
        /// A removal runs where refreshes of the same kind of entity run. Side by side they would be two
        /// things writing one entity at once.
        /// </summary>
        [Test]
        public async Task WriteTo_ARemoval_WaitsForTheRefreshOfTheSameKindOfEntity()
        {
            using var subject = new UpdateLanes(loggerMock.Object);
            var theRefreshMayFinish = new TaskCompletionSource();
            var theRemovalRan = new TaskCompletionSource();

            subject.WriteTo(ATeam, () => theRefreshMayFinish.Task);
            subject.WriteTo(ATeamRemoval, () =>
            {
                theRemovalRan.TrySetResult();
                return Task.CompletedTask;
            });

            var removalJumpedTheQueue = await Happened(theRemovalRan.Task, WindowForSomethingThatShouldNotHappen);
            theRefreshMayFinish.TrySetResult();

            Assert.That(removalJumpedTheQueue, Is.False,
                "A removal shares the lane of the entity type it removes.");
        }

        [Test]
        public async Task WriteTo_WorkThrows_TheLaneSaysWhatFailedAndKeepsGoing()
        {
            using var subject = new UpdateLanes(loggerMock.Object);
            var theNextOneRan = new TaskCompletionSource();

            subject.WriteTo(ATeam, () => throw new InvalidOperationException("the tracker said no"));
            subject.WriteTo(AnotherTeam, () =>
            {
                theNextOneRan.TrySetResult();
                return Task.CompletedTask;
            });

            var carriedOn = await Finished(theNextOneRan.Task);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(carriedOn, Is.True,
                    "One piece of work failing must not take its lane down - everything of that kind would "
                    + "stop moving for the life of the process, and nothing would say so.");
                Assert.That(whatWasLogged, Has.Some.Contains("Error").And.Some.Contains("Team"),
                    $"A lane can now die on its own while the others carry on, so what failed has to be named. Logged: {string.Join(" | ", whatWasLogged)}");
            }
        }

        [Test]
        public async Task DrainAsync_ReturnsOnlyOnceTheWorkInFlightIsDone()
        {
            var subject = new UpdateLanes(loggerMock.Object);
            var theWorkMayFinish = new TaskCompletionSource();
            var started = new TaskCompletionSource();

            subject.WriteTo(APortfolio, async () =>
            {
                started.TrySetResult();
                await theWorkMayFinish.Task;
            });

            await Finished(started.Task);
            var draining = subject.DrainAsync(CancellationToken.None);

            var returnedEarly = await Happened(draining, WindowForSomethingThatShouldNotHappen);
            theWorkMayFinish.TrySetResult();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(returnedEarly, Is.False,
                    "A drain that returns while work is still running leaves it going against a database the "
                    + "host is about to take away.");
                Assert.That(await Finished(draining), Is.True, "A drain that never returns is a shutdown that hangs.");
            }
        }

        [Test]
        public void WriteTo_AfterDispose_TheLaneRefusesTheWork()
        {
            var subject = new UpdateLanes(loggerMock.Object);
            subject.Dispose();

            var taken = subject.WriteTo(ATeam, () => Task.CompletedTask);

            Assert.That(taken, Is.False,
                "A closed lane will never run what it is handed, and the caller has to be told so it can give "
                + "back everything queuing that work claimed.");
        }

        private static Task<bool> Finished(Task work) => Happened(work, PatienceForWorkToRun);

        private static async Task<bool> Happened(Task work, TimeSpan within)
        {
            var reached = await Task.WhenAny(work, Task.Delay(within));

            return reached == work;
        }
    }
}
