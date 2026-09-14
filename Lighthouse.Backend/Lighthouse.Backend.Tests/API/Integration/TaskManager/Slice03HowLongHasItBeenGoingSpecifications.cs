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
    /// DISTILL step definitions (Specifications) for Epic #5511 slice 03 — how long has it been going.
    ///
    /// The contract these steps fix, which is what DELIVER has to build:
    ///
    /// Each row of <c>GET /api/latest/update/tasks</c> gains one further field, <c>elapsedMs</c>: how long
    /// the work has been in the state the row's <c>status</c> names, computed on the server. A running row
    /// counts from the moment it started, a waiting row from the moment it was admitted, so the field
    /// reads as "running for" or "queued for" without the browser having to know which moment it is
    /// looking at. It is a whole number of milliseconds, it is never negative, and it is <c>null</c> when
    /// the moment behind it was never recorded.
    ///
    /// One field rather than two timestamps, deliberately. Shipping the moments themselves would leave the
    /// subtraction to the browser, and a browser subtracting a server timestamp from its own clock is
    /// precisely what AC-03.4 forbids — the reader's clock is wrong by whatever their laptop is wrong by,
    /// so two people looking at one instance would read different answers off the same row.
    /// </summary>
    public partial class Slice03HowLongHasItBeenGoingTest : TaskManagerAcceptanceTest
    {
        /// <summary>
        /// Far enough from any real "now" that a duration accidentally computed against the wall clock
        /// comes out in decades rather than plausibly close to the expected answer.
        /// </summary>
        private static readonly DateTimeOffset TheInstantTheInstanceBelievesIn =
            new(2031, 4, 17, 9, 30, 0, TimeSpan.Zero);

        /// <summary>
        /// The queue can take a few milliseconds of the pinned clock's stillness to move a key, so an
        /// answer is allowed to be a beat away from the instant the test set. It is far tighter than the
        /// differences any of these scenarios turn on, which are seconds, minutes and hours.
        /// </summary>
        private static readonly TimeSpan CloseEnough = TimeSpan.FromMilliseconds(250);

        private FakeLighthouseClock theInstanceClock = null!;

        private TaskCompletionSource theTrackerMayAnswer = null!;

        private readonly List<UpdateKey> admittedByHand = [];

        private readonly record struct SeededTeam(int Id, string Name);

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

        private SeededTeam GivenATeamThatIsRefreshedOnSchedule()
        {
            var teamName = $"Team {Guid.NewGuid():N}";
            return new SeededTeam(SeedTeam(SeedConnection(), teamName), teamName);
        }

        /// <summary>
        /// A refresh that is genuinely in flight. Nothing about how long something has been running can be
        /// observed against work that has already finished, and the queue runs one thing at a time, so a
        /// held fetch is also what puts a second refresh honestly in the queue behind it.
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

        private void GivenWorkForThatTeamWasAdmittedAnHourAgo(SeededTeam team)
        {
            AdmitThroughTheStore(team);
            WhenTheInstanceClockMovesOn(TimeSpan.FromHours(1));
        }

        /// <summary>
        /// Mid-rolling-upgrade: the replica that admitted this was running the build before the moments
        /// existed, so the entry sits in the store with nothing recorded against it. Written straight into
        /// the store's own state rather than through the port, because going through the port is exactly
        /// what would stamp the moments this scenario is about not having.
        /// </summary>
        private void GivenWorkForThatTeamWasAdmittedByAReplicaThatRecordedNoMoments(SeededTeam team)
            => PutIntoTheStoreWithoutGoingThroughThePort(team, UpdateProgress.Queued, queuedAt: null, startedAt: null);

        private void GivenThatWorkIsRunningButOnlyItsAdmissionWasEverRecorded(SeededTeam team)
            => PutIntoTheStoreWithoutGoingThroughThePort(
                team,
                UpdateProgress.InProgress,
                queuedAt: theInstanceClock.Now - TimeSpan.FromMinutes(40),
                startedAt: null);

        /// <summary>
        /// The queue advances a key to its terminal status and removes it two statements later, so this is a
        /// window rather than a resting place - unless the replica holding it dies in between, and then the
        /// row stays for good with nothing to reap it.
        /// </summary>
        private void GivenThatWorkFinishedAnHourAgoButIsStillInTheStore(SeededTeam team)
            => PutIntoTheStoreWithoutGoingThroughThePort(
                team,
                UpdateProgress.Completed,
                queuedAt: theInstanceClock.Now - TimeSpan.FromHours(1),
                startedAt: theInstanceClock.Now - TimeSpan.FromMinutes(59));

        private void GivenThatWorkWasStampedByAReplicaWhoseClockIsAheadOfThisOne(SeededTeam team)
            => PutIntoTheStoreWithoutGoingThroughThePort(
                team,
                UpdateProgress.InProgress,
                queuedAt: theInstanceClock.Now + TimeSpan.FromSeconds(30),
                startedAt: theInstanceClock.Now + TimeSpan.FromSeconds(30));

        private void PutIntoTheStoreWithoutGoingThroughThePort(
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

        private void AdmitThroughTheStore(SeededTeam team)
        {
            var key = new UpdateKey(UpdateType.Team, team.Id);

            Factory.Services.GetRequiredService<IUpdateStatusStore>()
                .TryAdmit(key, new UpdateStatus { UpdateType = UpdateType.Team, Id = team.Id, Status = UpdateProgress.Queued });

            admittedByHand.Add(key);
        }

        // --- When ---

        private void WhenTheInstanceClockMovesOn(TimeSpan howLong)
            => theInstanceClock.SetInstant(theInstanceClock.Now + howLong);

        /// <summary>
        /// The shape a coalesced follow-up leaves behind. <c>UpdateQueueService</c> reaches this through
        /// its rerun path when a trigger lands while the same key is already running; driving it at the
        /// port makes the scenario a statement about the moment rather than about queue timing.
        /// </summary>
        private void WhenTheSameWorkIsCoalescedIntoAFollowUp(SeededTeam team)
            => Factory.Services.GetRequiredService<IUpdateStatusStore>()
                .Requeue(new UpdateKey(UpdateType.Team, team.Id));

        private Task WhenARefreshOfThatTeamIsUnderWay(SeededTeam team)
            => StartRefreshAndWaitUntil(team, UpdateProgress.InProgress);

        private Task WhenARefreshOfThatTeamIsAlsoAskedFor(SeededTeam team)
            => StartRefreshAndWaitUntil(team, UpdateProgress.Queued);

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

            Assert.Fail($"{key.UpdateType} {key.Id} never reached {reached}; the scenario cannot ask how long it has been going.");
        }

        private async Task TheQueueGoesIdle()
        {
            var store = Factory.Services.GetRequiredService<IUpdateStatusStore>();
            var deadline = DateTime.UtcNow.AddSeconds(30);

            while (store.HasActiveWork() && DateTime.UtcNow < deadline)
            {
                await Task.Delay(20);
            }
        }

        // --- Then ---

        private async Task ThenTheRowForThatTeamSaysItHasBeenGoingFor(SeededTeam team, TimeSpan expected)
        {
            var row = await TheRowFor(UpdateType.Team, team.Id);
            var elapsed = ElapsedOn(row);

            Assert.That(elapsed, Is.Not.Null,
                "The row has to say how long it has been going; a refresh that started moments ago and one going for "
                + $"forty minutes are the same row without it. Got: {row}");

            Assert.That(elapsed!.Value, Is.EqualTo(expected).Within(CloseEnough),
                $"The instance's clock says {expected} has passed in the state this row is in. Got: {row}");
        }

        private async Task ThenTheRowForThatTeamIsListedByNameWithNoDurationAtAll(SeededTeam team)
        {
            var row = await TheRowFor(UpdateType.Team, team.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Text(row, "name"), Is.EqualTo(team.Name),
                    "A missing moment costs the row its duration and nothing else; dropping the row, or the list, "
                    + "would make a rolling upgrade look like an outage.");

                Assert.That(
                    !row.TryGetProperty("elapsedMs", out var value) || value.ValueKind is JsonValueKind.Null,
                    Is.True,
                    "With no moment recorded there is no honest number to give, and any stand-in - zero, or the "
                    + $"admission of a run already under way - is a duration a reader would believe. Got: {row}");
            }
        }

        private async Task ThenTheTaskListDoesNotMention(SeededTeam team)
        {
            var rows = await TheTaskList();

            Assert.That(
                rows.Any(row => Text(row, "updateType") == nameof(UpdateType.Team) && Number(row, "id") == team.Id),
                Is.False,
                $"A refresh that has finished is not something the instance is doing. Got: {Describe(rows)}");
        }

        private async Task<TimeSpan> TheElapsedTimeReportedFor(SeededTeam team)
        {
            var row = await TheRowFor(UpdateType.Team, team.Id);
            var elapsed = ElapsedOn(row);

            Assert.That(elapsed, Is.Not.Null, $"The row has to say how long it has been going. Got: {row}");

            return elapsed!.Value;
        }

        private static void ThenTheAnswerGrewByExactly(TimeSpan before, TimeSpan after, TimeSpan expectedGrowth)
        {
            Assert.That(after - before, Is.EqualTo(expectedGrowth).Within(CloseEnough),
                "Between the two readings the instance's clock moved by this much and the test's own by milliseconds. "
                + "A duration computed anywhere but against the instance clock passes every other scenario here and "
                + "fails this one, which is the point of it.");
        }

        private static TimeSpan? ElapsedOn(JsonElement row)
            => row.TryGetProperty("elapsedMs", out var value) && value.ValueKind is JsonValueKind.Number
                ? TimeSpan.FromMilliseconds(value.GetInt64())
                : null;
    }
}
