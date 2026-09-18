using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;
using System.Net;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.API.Integration.UpdateQueueLanes
{
    /// <summary>
    /// DISTILL step definitions (Specifications) for ADO #5877 slice 02 — nothing holds a lane forever.
    ///
    /// The contract these steps fix, which is what DELIVER has to build:
    ///
    /// Every run except a removal carries a deadline armed from the instance's own
    /// <see cref="TimeProvider"/> when the run starts, and linked into the same cancellation source an
    /// operator's Stop reaches. A run that passes it reaches <c>Cancelled</c>, stops asking the tracker
    /// for pages, frees its lane and leaves a record.
    ///
    /// The record is the <c>RefreshLog</c> row the updater already writes in its <c>finally</c>, plus
    /// one nullable reason column. The bound writes the token <c>run-limit-exceeded</c> there — a token
    /// and not a sentence, because a sentence composed in the backend would hardcode words the tenant
    /// may have renamed. Where two stops land in the same instant, the operator's is the one recorded:
    /// the reason is derived from which source fired rather than from a flag written at stop time.
    ///
    /// The bound itself is one instance-wide <c>AppSettings</c> row, <c>Update:MaxRunMinutes</c>, in
    /// whole minutes, not seeded, defaulting to 180 and clamped to [5 minutes, 24 hours]. Absent,
    /// unparseable and non-positive all fall back to the default rather than being clamped up — a typed
    /// zero clamped to the floor would give an instance that cannot complete any refresh at all.
    /// <c>GET /api/latest/appsettings/UpdateRunLimit</c> answers the value the instance will actually
    /// use, as <c>{ "minutes": N }</c>, under the guard that controller's writes already carry.
    ///
    /// The clock is faked for the whole host. A scenario that waited out a real bound would either take
    /// the bound's own length or force a bound nobody would ever configure, and the second proves
    /// nothing about the first.
    /// </summary>
    public partial class Slice02NothingHoldsALaneForeverTest : UpdateQueueLanesAcceptanceTest
    {
        private const string RunLimitRoute = "/api/latest/appsettings/UpdateRunLimit";

        /// <summary>
        /// The token the bound writes on the run it ended. Pinned here rather than read from the
        /// production constant, because the point of the record is that it keeps saying the same thing
        /// to whoever reads it later.
        /// </summary>
        private const string TheBoundsReason = "run-limit-exceeded";

        /// <summary>
        /// Comfortably longer than any scenario here takes, so nothing is ended by the bound except
        /// where the scenario moves the clock past it on purpose.
        /// </summary>
        private const string AGenerousBoundInMinutes = "120";

        private const string SomethingNobodyCouldHaveMeant = "the day after tomorrow";

        private static readonly TimeSpan LongerThanTheGenerousBound = TimeSpan.FromHours(3);

        private FakeTimeProvider theInstanceTimer = null!;

        private TaskCompletionSource? theRemovalMayFinish;

        protected override void ConfigureAdditionalServices(IServiceCollection services)
        {
            theInstanceTimer = new FakeTimeProvider();

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(theInstanceTimer);
        }

        /// <summary>
        /// Runs before the harness teardown, because NUnit unwinds from the derived class outwards. A
        /// removal left gated would otherwise keep the queue busy while the harness waits for it to go
        /// idle, and then while the database underneath it is deleted.
        /// </summary>
        [TearDown]
        public void LetAnyGatedRemovalFinish()
        {
            theRemovalMayFinish?.TrySetResult();
        }

        // --- Given ---

        private void GivenTheRunLimitIsRecordedAsWhateverAnOperatorLeftBehind(string? recorded)
        {
            if (recorded is null)
            {
                GivenNoRunLimitHasEverBeenRecorded();
                return;
            }

            GivenTheInstanceRunLimitIsRecordedAs(recorded);
        }

        /// <summary>
        /// A second ask while the first run is still going. The queue collapses it into one follow-up
        /// that runs once the first leaves, which is the run whose clock has to start fresh.
        /// </summary>
        private void GivenAnotherRefreshOfThatTeamIsAskedForWhileTheFirstIsStillGoing(SeededTeam team)
        {
            Factory.Services.GetRequiredService<ITeamUpdater>().TriggerUpdate(team.Id);
        }

        /// <summary>
        /// A removal the bound could reach if it were allowed to. It is held open the same way a real
        /// one is — the caller that asked for it is still waiting on the answer — so the scenario can
        /// move the clock past the bound while the removal is genuinely mid-flight.
        /// </summary>
        private void GivenARemovalOfThatTeamIsUnderWayAndWillNotFinishUntilWeSaySo(SeededTeam team)
        {
            var gate = theRemovalMayFinish ?? new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            theRemovalMayFinish = gate;

            Factory.Services.GetRequiredService<IUpdateQueueService>()
                .EnqueueUpdate(UpdateType.TeamDelete, team.Id, _ => gate.Task);
        }

        // --- When ---

        /// <summary>
        /// The instance's own clock moved past the bound, not the test's patience run out. The deadline
        /// is armed from this provider, so moving it is what a run outliving its bound looks like from
        /// inside the process.
        /// </summary>
        private void WhenTheRunHasBeenGoingLongerThanTheBoundAllows()
        {
            theInstanceTimer.Advance(LongerThanTheGenerousBound);
        }

        private async Task WhenThatRefreshHasFinishedOneWayOrAnother(SeededTeam team)
        {
            ReleaseTheTracker();

            var settled = await ItLeavesTheQueue(KeyOf(team));

            Assert.That(settled, Is.True,
                "A run that never leaves the queue leaves its key admitted for good, and the entity can never be "
                + "refreshed again - which is a worse instance than the one that was too slow.");
        }

        // --- Then ---

        private async Task ThenThatTeamRefreshEndsWithoutAnybodyAskingItTo(SeededTeam team)
        {
            ReleaseTheTracker();

            var ended = await TheBrowserIsToldItReached(KeyOf(team), UpdateProgress.Cancelled);

            Assert.That(ended, Is.True,
                "Nobody pressed anything. A run that outlives the bound has to end itself, or an instance can "
                + "still be lost to a single refresh that nobody is watching. "
                + $"The browser was told: {TheBrowserWasTold.Describe()}");
        }

        private async Task ThenThatTeamRefreshGetsGoing(SeededTeam team)
        {
            ReleaseTheTracker();

            var gotGoing = await ItReaches(KeyOf(team), UpdateProgress.InProgress);

            Assert.That(gotGoing, Is.True,
                "A bound that ends the run and leaves the lane held has replaced one wedged instance with "
                + "another, and the operator is no better off than before.");
        }

        private async Task ThenThatTeamRefreshIsNotStoppedOutOfHand(SeededTeam team)
        {
            var stillGoing = await ItStaysAt(KeyOf(team), UpdateProgress.InProgress);

            Assert.That(stillGoing, Is.True,
                "A run limit nobody could have meant must fall back to the default, never to zero. Read as no "
                + "time at all it kills every refresh on the instance the moment it starts - and the operator "
                + "sees an instance that has stopped working entirely rather than one that is slow.");
        }

        private async Task ThenThatRefreshStopsAskingTheTrackerForPages(SeededTeam team)
        {
            var ended = await TheBrowserIsToldItReached(KeyOf(team), UpdateProgress.Cancelled);
            var pagesWhenItEnded = PagesTheTrackerHasBeenAskedFor;

            await Task.Delay(WindowForSomethingThatShouldNotHappen);

            var pagesAfterwards = PagesTheTrackerHasBeenAskedFor;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ended, Is.True,
                    "The bound fired and the run never reached a terminal state, so nothing was stopped. "
                    + $"The browser was told: {TheBrowserWasTold.Describe()}");
                Assert.That(pagesAfterwards, Is.EqualTo(pagesWhenItEnded),
                    "A refresh that carries on paging after the bound fired is a bound that is a log line rather "
                    + "than a lever, and the pages it reads afterwards are exactly the ones the bound exists to "
                    + "stop. How far into the next page it gets is a measurement against a real connector, taken "
                    + "on Tenant Zero rather than asserted here.");
            }
        }

        private async Task ThenTheFollowUpRunsToCompletion(SeededTeam team)
        {
            ReleaseTheTracker();

            var finished = await TheBrowserIsToldItReached(KeyOf(team), UpdateProgress.Completed);

            Assert.That(finished, Is.True,
                "The bound is per run. A follow-up that inherited the elapsed time of the run it follows would "
                + "be killed the instant it started, so every refresh of a slow entity after the first would die "
                + "on arrival - which makes the reported symptom worse rather than better. "
                + $"The browser was told: {TheBrowserWasTold.Describe()}");
        }

        private async Task ThenThatRemovalIsStillGoing(SeededTeam team)
        {
            var removal = new UpdateKey(UpdateType.TeamDelete, team.Id);
            var stillGoing = await ItStaysAt(removal, UpdateProgress.InProgress);

            Assert.That(stillGoing, Is.True,
                "A removal stopped half-way tells the caller waiting on it that the entity has gone while its row "
                + "is still in the database. Removals are already refused by the Stop control for that reason, and "
                + "a bound that ended them behind the operator's back would reintroduce it without a button.");
        }

        private async Task ThenTheRecordedRunNamesTheBoundAsWhatEndedIt(SeededTeam team)
        {
            var recorded = await TheRecordedRunOf(team.Name);
            var reason = recorded is { } named ? Text(named, "reason") : null;
            var cancelled = recorded is { } marked ? Flag(marked, "cancelled") : null;
            var row = DescribeRow(recorded);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(recorded, Is.Not.Null,
                    "There is no record of the run at all, so there is nothing for an operator to read afterwards.");
                Assert.That(cancelled, Is.True,
                    "A run the instance ended is not a failed run - nothing was learned about whether refreshing "
                    + "works - so it belongs beside the cancels rather than in the failure count.");
                Assert.That(reason, Is.EqualTo(TheBoundsReason),
                    "An operator who pressed nothing must not read a history saying they did. Without a reason on "
                    + "the row there is no way at all to tell the two apart, and a record that says something the "
                    + $"reader knows is untrue stops being believed about everything else. Row: {row}");
            }
        }

        private async Task ThenTheRecordedRunDoesNotBlameTheBound(SeededTeam team)
        {
            var recorded = await TheRecordedRunOf(team.Name);
            var reason = recorded is { } named ? Text(named, "reason") : null;
            var cancelled = recorded is { } marked ? Flag(marked, "cancelled") : null;
            var row = DescribeRow(recorded);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(recorded, Is.Not.Null, "There is no record of the run the operator stopped.");
                Assert.That(cancelled, Is.True, "The operator stopped it, so it is recorded as stopped.");
                Assert.That(reason, Is.Not.EqualTo(TheBoundsReason),
                    "Two stops can land in the same instant, and the operator pressed one of them. Telling somebody "
                    + "their own deliberate stop was the instance giving up on a slow refresh sends them looking for "
                    + $"a problem that is not there. Row: {row}");
            }
        }

        private void ThenTheInstanceWarnedThatItEndedThatRefreshItself(SeededTeam team)
        {
            var warnings = CapturedLogs.Warnings;
            var saidSo = warnings.Any(line => line.Contains(team.Name, StringComparison.Ordinal));

            Assert.That(saidSo, Is.True,
                "The existing cancel line reads as somebody having pressed something and sits below the level an "
                + "operator sees at default settings, so an instance that ended its own run would be the one event "
                + "in this story hardest to find afterwards - and it is the one they most want. "
                + $"Warnings logged: {string.Join(" | ", warnings)}");
        }

        private async Task ThenTheInstanceSaysItsRunLimitIs(int minutes)
        {
            using var client = Factory.CreateClient();
            using var response = await client.GetAsync(new Uri(RunLimitRoute, UriKind.Relative));
            var answered = response.StatusCode;
            var body = await response.Content.ReadAsStringAsync();
            var minutesAnswered = MinutesIn(body);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answered, Is.EqualTo(HttpStatusCode.OK),
                    $"The run limit is edited in Settings, so the instance has to answer it. {RunLimitRoute} answered "
                    + $"{(int)answered}: '{body}'");
                Assert.That(minutesAnswered, Is.EqualTo(minutes),
                    "The field has to show the bound the instance will actually use. Showing back whatever was typed "
                    + "leaves an operator who typed something unusable believing it took, and the only way left to "
                    + $"find out is to watch refreshes. Answered: '{body}'");
            }
        }

        /// <summary>
        /// Reads the answered bound without letting a body that is not the expected shape throw out of
        /// the multiple-assertion scope — an exception there discards the grouped report, and the
        /// status code is usually the line that explains the shape.
        /// </summary>
        private static int? MinutesIn(string body)
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                return Number(document.RootElement, "minutes");
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string DescribeRow(JsonElement? row) => row?.ToString() ?? "(no row at all)";

        /// <summary>
        /// Every way the row can be left, and the bound the instance uses for each. Absent, unparseable
        /// and non-positive all fall back to the default rather than clamping up to the floor: a typed
        /// zero clamped to five minutes gives an instance that cannot complete any refresh, which is the
        /// one outcome worse than no bound at all.
        /// </summary>
        private static TestCaseData[] EveryWayARunLimitCanBeRecorded()
        {
            return
            [
                new TestCaseData(null, 180).SetName("Nobody has ever set one"),
                new TestCaseData(string.Empty, 180).SetName("The field was cleared"),
                new TestCaseData(SomethingNobodyCouldHaveMeant, 180).SetName("Something that is not a number"),
                new TestCaseData("0", 180).SetName("Zero, which would stop every refresh at once"),
                new TestCaseData("-5", 180).SetName("A negative, which means nothing here"),
                new TestCaseData("1", 5).SetName("Below the floor, raised to it"),
                new TestCaseData("2000", 1440).SetName("Above the ceiling, lowered to it"),
                new TestCaseData("45", 45).SetName("An ordinary value an operator chose"),
            ];
        }

        /// <summary>
        /// Waits for the key to leave the store, which is what every terminal path ends with. A stop is
        /// asynchronous by nature — it asks, and the run ends when it next looks — so nothing about how
        /// a run finished can be read at the moment the bound fires.
        /// </summary>
        private async Task<bool> ItLeavesTheQueue(UpdateKey key)
        {
            var store = Factory.Services.GetRequiredService<IUpdateStatusStore>();
            var deadline = DateTime.UtcNow.Add(PatienceForWorkToMove);

            while (DateTime.UtcNow < deadline)
            {
                if (!store.TryGet(key, out _))
                {
                    return true;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(20));
            }

            return false;
        }

        private static bool? Flag(JsonElement row, string property)
            => row.TryGetProperty(property, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False
                ? value.GetBoolean()
                : null;
    }
}
