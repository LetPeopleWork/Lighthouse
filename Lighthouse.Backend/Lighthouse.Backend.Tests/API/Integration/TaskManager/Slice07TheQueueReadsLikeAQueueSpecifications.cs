using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.API.Integration.TaskManager
{
    /// <summary>
    /// DISTILL step definitions (Specifications) for Epic #5511 slice 07 — the queue reads like a queue.
    ///
    /// The contract these steps fix, which is what DELIVER has to build:
    ///
    /// <c>GET /api/latest/update/tasks</c> answers its rows in the order the queue will work them:
    /// whatever is running first, then whatever is waiting, oldest admission first. A row whose moment
    /// was never recorded goes to the end of its own group and keeps its state. The order is total — two
    /// rows whose moments are identical still come back the same way round on every read, because a list
    /// presented as a sequence makes a claim whether or not it means to.
    ///
    /// No store change, no schema change: <c>QueuedAt</c> and <c>StartedAt</c> are already on
    /// <c>UpdateStatus</c>, put there for the duration that slice 07 removes from the rows.
    /// </summary>
    public partial class Slice07TheQueueReadsLikeAQueueTest : TaskManagerAcceptanceTest
    {
        /// <summary>
        /// Far enough from any real "now" that a moment accidentally taken from the wall clock sorts
        /// visibly wrong rather than plausibly right.
        /// </summary>
        private static readonly DateTimeOffset TheInstantTheInstanceBelievesIn =
            new(2031, 4, 17, 9, 30, 0, TimeSpan.Zero);

        /// <summary>
        /// Enough rows that an unordered store cannot hand back the right sequence by accident. Twelve
        /// arrangements in the right order is one chance in four hundred and seventy-nine million.
        /// </summary>
        private const int ALongQueue = 12;

        /// <summary>
        /// A promise about repetition needs repeating. One read is satisfied by a list that happened to
        /// come out right this time, which is the whole defect: hash order is arbitrary, not wrong.
        /// </summary>
        private const int EnoughReadsToCatchAListThatWanders = 5;

        private FakeLighthouseClock theInstanceClock = null!;

        private TaskCompletionSource theTrackerMayAnswer = null!;

        private readonly List<UpdateKey> admittedByHand = [];

        protected override void ConfigureAdditionalServices(IServiceCollection services)
        {
            theInstanceClock = new FakeLighthouseClock(TheInstantTheInstanceBelievesIn);

            services.RemoveAll<ILighthouseClock>();
            services.AddSingleton<ILighthouseClock>(theInstanceClock);
        }

        /// <summary>
        /// Runs before the harness tears the host down, because NUnit unwinds from the derived class
        /// outwards. A scenario that left the tracker gated would otherwise leave a refresh parked in the
        /// queue while the database underneath it is deleted.
        /// </summary>
        [TearDown]
        public async Task LetAnyGatedRefreshFinish()
        {
            theTrackerMayAnswer?.TrySetResult();

            // Work admitted by hand never runs, so nothing will ever take it back out of the store and the
            // wait below would sit out its whole deadline for a key that was only ever a fixture.
            var store = Factory.Services.GetRequiredService<IUpdateStatusStore>();
            foreach (var key in admittedByHand)
            {
                store.Remove(key);
            }

            admittedByHand.Clear();

            await TheQueueGoesIdle();
        }

        // --- Given ---

        /// <summary>
        /// A refresh that is genuinely in flight. The queue runs one thing at a time, so a held fetch is
        /// also what puts anything else honestly behind it.
        /// </summary>
        private void GivenTheTrackerDoesNotAnswerUntilWeSaySo()
        {
            theTrackerMayAnswer = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            ConnectorMock
                .Setup(c => c.GetWorkItemsForTeam(It.IsAny<Team>(), It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    await theTrackerMayAnswer.Task;
                    return [];
                });
        }

        private void GivenWorkForThatTeamWasAdmitted(SeededTeam team, TimeSpan howLongAgo)
            => PutIntoTheStore(team, UpdateProgress.Queued, queuedAt: theInstanceClock.Now - howLongAgo, startedAt: null);

        /// <summary>
        /// Mid-rolling-upgrade: the replica that admitted this was running the build before the moments
        /// existed, so the entry sits in the store with nothing recorded against it. Written straight into
        /// the store's own state rather than through the port, because going through the port is exactly
        /// what would stamp the moment this scenario is about not having.
        /// </summary>
        private void GivenWorkForThatTeamWasAdmittedByAReplicaThatRecordedNoMoments(SeededTeam team)
            => PutIntoTheStore(team, UpdateProgress.Queued, queuedAt: null, startedAt: null);

        private void GivenWorkForThatTeamIsRunningButItsStartWentUnrecorded(SeededTeam team)
            => PutIntoTheStore(
                team,
                UpdateProgress.InProgress,
                queuedAt: theInstanceClock.Now - TimeSpan.FromMinutes(40),
                startedAt: null);

        /// <summary>
        /// Every row carries the same moment. Two admissions landing inside one tick of the clock is
        /// ordinary rather than exotic, and it is the case where the moments alone settle nothing — so it
        /// is the case that says whether the order is total or merely usually right.
        /// </summary>
        private SeededTeam[] GivenSeveralTeamsAdmittedInTheSameInstant(int howMany)
        {
            var admittedAt = theInstanceClock.Now - TimeSpan.FromMinutes(5);
            var teams = new SeededTeam[howMany];

            for (var i = 0; i < howMany; i++)
            {
                teams[i] = GivenATeamThatIsRefreshedOnSchedule();
                PutIntoTheStore(teams[i], UpdateProgress.Queued, queuedAt: admittedAt, startedAt: null);
            }

            return teams;
        }

        /// <summary>
        /// Admitted newest first, so the store is handed the exact reverse of the answer. Anything that
        /// returns the rows in the order it received them fails every read rather than most of them, and a
        /// scenario that fails only sometimes teaches nobody anything.
        /// </summary>
        private SeededTeam[] GivenAQueueAdmittedInTheReverseOfTheOrderItShouldRead(int howMany)
        {
            var teams = new SeededTeam[howMany];

            for (var i = 0; i < howMany; i++)
            {
                // The last one seeded waited longest, so the array reads oldest-first while the store was
                // filled newest-first.
                teams[howMany - 1 - i] = GivenATeamThatIsRefreshedOnSchedule();
                PutIntoTheStore(
                    teams[howMany - 1 - i],
                    UpdateProgress.Queued,
                    queuedAt: theInstanceClock.Now - TimeSpan.FromMinutes(i + 1),
                    startedAt: null);
            }

            return teams;
        }

        private void PutIntoTheStore(
            SeededTeam team,
            UpdateProgress status,
            DateTimeOffset? queuedAt,
            DateTimeOffset? startedAt)
        {
            var key = new UpdateKey(UpdateType.Team, team.Id);

            Factory.Services.GetRequiredService<ConcurrentDictionary<UpdateKey, UpdateStatus>>()[key] =
                new UpdateStatus
                {
                    UpdateType = UpdateType.Team,
                    Id = team.Id,
                    Status = status,
                    QueuedAt = queuedAt,
                    StartedAt = startedAt,
                };

            admittedByHand.Add(key);
        }

        // --- When ---

        private async Task WhenARefreshOfThatTeamIsUnderWay(SeededTeam team)
        {
            var key = new UpdateKey(UpdateType.Team, team.Id);

            Factory.Services.GetRequiredService<ITeamUpdater>().TriggerUpdate(team.Id);

            var store = Factory.Services.GetRequiredService<IUpdateStatusStore>();
            var deadline = DateTime.UtcNow.AddSeconds(30);

            while (DateTime.UtcNow < deadline)
            {
                if (store.TryGet(key, out var status) && status?.Status == UpdateProgress.InProgress)
                {
                    return;
                }

                await Task.Delay(20);
            }

            Assert.Fail($"{key.UpdateType} {key.Id} never started running; the scenario cannot ask where it is listed.");
        }

        // --- Then ---

        private async Task ThenTheListReads(params SeededTeam[] expected)
        {
            var rows = await TheTaskList();

            Assert.That(NamesIn(rows), Is.EqualTo(expected.Select(team => team.Name).ToList()),
                "The popover presents these rows as a sequence, so their order is a claim about which one the "
                + "queue reaches next. Whatever is running is what everything else is waiting for, and among "
                + $"those the one admitted longest ago is the one that moves first. Got: {Describe(rows)}");
        }

        /// <summary>
        /// The defect is that the order means nothing, not that it is wrong — an unordered store can hand
        /// back the right sequence on any given read, so one read proves nothing. The promise is therefore
        /// about repetition: every read reads as the queue will work it, which also means every read agrees
        /// with the one before it.
        /// </summary>
        private async Task ThenTheListReadsTheSameWayEveryTime(params SeededTeam[] expected)
        {
            var asItShouldRead = AsOneLine(expected.Select(team => team.Name));
            var everyRead = await TheListRead(EnoughReadsToCatchAListThatWanders);

            Assert.That(everyRead, Is.All.EqualTo(asItShouldRead),
                $"Each of the {EnoughReadsToCatchAListThatWanders} reads has to read as the queue will work it: "
                + $"{asItShouldRead}. A single correct read is what an unordered list gives by luck, which is "
                + $"why the promise is about all of them. The reads were: {string.Join(" || ", everyRead)}");
        }

        /// <summary>
        /// Which order a set of tied rows settles into is the instance's business; that it settles into one
        /// at all is the operator's. So this asks only that the answer does not move.
        /// </summary>
        private async Task ThenTheListAlwaysReadsTheSameWayWhateverOrderThatIs(SeededTeam[] admitted)
        {
            var everyRead = await TheListRead(EnoughReadsToCatchAListThatWanders);
            var distinctAnswers = everyRead.Distinct().ToList();
            var everyNameIsThere = everyRead.All(read => admitted.All(team => read.Contains(team.Name, StringComparison.Ordinal)));

            using (Assert.EnterMultipleScope())
            {
                // Without this a list that answered nothing at all would satisfy "it never changes" perfectly.
                Assert.That(everyNameIsThere, Is.True,
                    $"Every read has to list all {admitted.Length} pieces of work that were admitted. "
                    + $"The reads were: {string.Join(" || ", everyRead)}");

                Assert.That(distinctAnswers, Has.Count.EqualTo(1),
                    "Nothing in the moments tells these rows apart, so whatever settles the tie has to settle it "
                    + "the same way every time. A list that re-shuffles between two glances is the defect this "
                    + "slice exists to remove, and equal moments alone do not prevent it. The reads were: "
                    + string.Join(" || ", everyRead));
            }
        }

        private async Task ThenTheRowForThatTeamStillSaysItIs(SeededTeam team, UpdateProgress state)
        {
            var row = await TheRowFor(UpdateType.Team, team.Id);

            Assert.That(Text(row, "status"), Is.EqualTo(state.ToString()),
                "A missing moment costs the row its place in the order and nothing else. Dropping it, or "
                + $"letting it lose what it is, would make a rolling upgrade look like an outage. Got: {row}");
        }

        private async Task<List<string>> TheListRead(int howManyTimes)
        {
            var reads = new List<string>();

            for (var read = 0; read < howManyTimes; read++)
            {
                var rows = await TheTaskList();
                reads.Add(AsOneLine(rows.Select(row => Text(row, "name") ?? "(a row with no name)")));
            }

            return reads;
        }

        private static List<string> NamesIn(IReadOnlyList<JsonElement> rows)
            => [.. rows.Select(row => Text(row, "name") ?? "(a row with no name)")];

        private static string AsOneLine(IEnumerable<string> names) => string.Join(" then ", names);
    }
}
