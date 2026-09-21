using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.API.Integration.TaskManager
{
    /// <summary>
    /// DISTILL step definitions for User Story #6055 slice 02.
    ///
    /// The contract these steps fix, which is what DELIVER has to build:
    ///
    /// <c>GET /api/latest/update/tasks</c> answers a JSON array whose every object still carries
    /// <c>updateType</c>, <c>id</c>, <c>name</c>, <c>status</c> and <c>elapsedMs</c> exactly as before.
    /// <c>waitingBehind</c> is no longer a string: it is absent for work that is running or whose lane is
    /// free, and otherwise an object carrying <c>name</c> (the holder's display name), <c>updateType</c>
    /// (what the holder is doing) and <c>isSameEntity</c> (whether the holder is this row's own entity).
    /// The browser assembles the sentence; this answer carries facts.
    ///
    /// Work is admitted straight through the status store rather than by running a refresh. That is
    /// deliberate: the self-reference pairs need one specific piece of work running and one specific
    /// piece queued, and driving them through the real queue would mean holding a tracker open and
    /// waiting on a forecast trigger to fire — timing, for a claim that is not about timing. The
    /// production-data scenario is the one that drives the real path end to end.
    /// </summary>
    public partial class Story6055ActivityNamesTheWorkTest : TaskManagerAcceptanceTest
    {
        private const string ProductionData =
            "production data — needs a real connection on the dogfood instance; run by hand at slice close";

        private readonly List<UpdateKey> admittedByHand = [];

        /// <summary>
        /// Slice 02 of #5511 has an identical record, private to its own fixture. Duplicated rather than
        /// promoted to the base class: a two-field struct is cheaper to repeat than a shared type that
        /// couples two stories' fixtures together, and the base class is where the mechanisms live, not
        /// the seed data.
        /// </summary>
        private readonly record struct SeededPortfolio(int Id, string Name);

        private SeededPortfolio GivenAPortfolioThatIsRefreshedOnSchedule()
        {
            var portfolioName = $"Portfolio {Guid.NewGuid():N}";
            return new SeededPortfolio(SeedPortfolio(SeedConnection(), portfolioName), portfolioName);
        }

        /// <summary>
        /// Work admitted by hand never runs, so nothing takes it back out of the store and the idle wait
        /// would sit out its whole deadline for a key that was only ever a fixture.
        /// </summary>
        [TearDown]
        public async Task ForgetTheWorkThatWasOnlyEverAFixture()
        {
            var store = Factory.Services.GetRequiredService<IUpdateStatusStore>();

            foreach (var key in admittedByHand)
            {
                store.Remove(key);
            }

            admittedByHand.Clear();

            await TheQueueGoesIdle();
        }

        // --- Given ---

        private void GivenARefreshOfThatPortfolioIsRunning(SeededPortfolio portfolio)
            => Admit(UpdateType.Features, portfolio.Id, UpdateProgress.InProgress);

        private void GivenARemovalOfThatPortfolioIsRunning(SeededPortfolio portfolio)
            => Admit(UpdateType.PortfolioDelete, portfolio.Id, UpdateProgress.InProgress);

        private void GivenAForecastOfThatPortfolioIsQueued(SeededPortfolio portfolio)
            => Admit(UpdateType.Forecasts, portfolio.Id, UpdateProgress.Queued);

        private void GivenARemovalOfThatPortfolioIsQueued(SeededPortfolio portfolio)
            => Admit(UpdateType.PortfolioDelete, portfolio.Id, UpdateProgress.Queued);

        private void GivenARefreshOfThatPortfolioIsQueued(SeededPortfolio portfolio)
            => Admit(UpdateType.Features, portfolio.Id, UpdateProgress.Queued);

        private void GivenARefreshOfThatTeamIsQueued(SeededTeam team)
            => Admit(UpdateType.Team, team.Id, UpdateProgress.Queued);

        /// <summary>
        /// A Team whose id equals a Portfolio's. Two entities that happen to share an integer — the case
        /// an implementation comparing ids alone gets wrong, and the reason the comparison has to be over
        /// the entity a piece of work is about rather than over its number.
        /// </summary>
        private void GivenARefreshIsQueuedForATeamWhoseIdIs(int id)
            => Admit(UpdateType.Team, id, UpdateProgress.Queued);

        private void Admit(UpdateType updateType, int id, UpdateProgress status)
        {
            var store = Factory.Services.GetRequiredService<IUpdateStatusStore>();
            var key = new UpdateKey(updateType, id);

            store.TryAdmit(key, new UpdateStatus { UpdateType = updateType, Id = id, Status = status });
            admittedByHand.Add(key);
        }

        // --- When ---

        private async Task WhenARealRefreshOfThatPortfolioTriggersItsForecast(SeededPortfolio portfolio)
        {
            Factory.Services.GetRequiredService<IPortfolioUpdater>().TriggerUpdate(portfolio.Id);

            var store = Factory.Services.GetRequiredService<IUpdateStatusStore>();
            var deadline = DateTime.UtcNow.AddSeconds(30);

            while (DateTime.UtcNow < deadline)
            {
                if (store.TryGet(new UpdateKey(UpdateType.Forecasts, portfolio.Id), out _))
                {
                    return;
                }

                await Task.Delay(20);
            }

            Assert.Fail(
                $"The refresh of portfolio {portfolio.Id} never triggered its forecast, so the pair this "
                + "scenario is about never existed.");
        }

        // --- Then ---

        private async Task ThenTheQueuedRowSaysItIsBehindItsOwn(
            UpdateType queued, int id, UpdateType whatTheHolderIsDoing)
        {
            var behind = await TheWaitingBehindOf(queued, id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Bool(behind, "isSameEntity"), Is.True,
                    "The holder is this row's own entity, and the row saying otherwise is the whole defect: "
                    + "an operator reads its own name back and cannot tell the two rows apart.");

                Assert.That(Text(behind, "updateType"), Is.EqualTo(whatTheHolderIsDoing.ToString()),
                    "The clause names what the HOLDER is doing. Echoing the queued row's own type would read "
                    + "correctly whenever the two happen to match and be wrong the rest of the time.");
            }
        }

        private async Task ThenTheQueuedRowSaysItIsBehindTheEntityNamed(
            UpdateType queued, int id, string holderName)
        {
            var behind = await TheWaitingBehindOf(queued, id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Bool(behind, "isSameEntity"), Is.False,
                    "A different entity is holding the lane. Calling that a self-reference would drop the one "
                    + "name the operator needs.");

                Assert.That(Text(behind, "name"), Is.EqualTo(holderName),
                    "Naming the holder is the difference between waiting and wedged, and it is the reading that "
                    + "already shipped.");
            }
        }

        private async Task ThenTheRowIsWaitingForNothing(UpdateType updateType, int id)
        {
            var row = await TheRowFor(updateType, id);

            Assert.That(
                !row.TryGetProperty("waitingBehind", out var behind) || behind.ValueKind is JsonValueKind.Null,
                Is.True,
                "Nothing is running, so this row is behind nothing. Naming an arbitrary row instead would put a "
                + "dependency in front of an operator that does not exist, and they have no way to tell it from "
                + "a real one.");
        }

        private async Task ThenTheRowStillCarriesEveryFieldItAlwaysDid(
            UpdateType updateType, int id, string name)
        {
            var row = await TheRowFor(updateType, id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Text(row, "updateType"), Is.EqualTo(updateType.ToString()));
                Assert.That(Number(row, "id"), Is.EqualTo(id));
                Assert.That(Text(row, "name"), Is.EqualTo(name));
                Assert.That(Text(row, "status"), Is.EqualTo(nameof(UpdateProgress.Queued)));
                Assert.That(Milliseconds(row, "elapsedMs"), Is.Not.Null.And.GreaterThanOrEqualTo(0),
                    "elapsedMs is how long the row has been in the state it is in, and the store stamped that "
                    + "moment when it admitted this work, so there is a duration to report. Asking only whether "
                    + "the field is there would agree with a row that answers nothing on every path, because a "
                    + "null is still serialised.");
            }
        }

        /// <summary>
        /// The invariant behind both slice-02 stories, asserted over the whole list rather than one row:
        /// whatever any row is waiting for, it is not itself.
        /// </summary>
        private async Task ThenNoRowNamesItsOwnEntityAsWhatItIsWaitingFor()
        {
            var rows = await TheTaskList();

            foreach (var row in rows)
            {
                if (!row.TryGetProperty("waitingBehind", out var behind) || behind.ValueKind is JsonValueKind.Null)
                {
                    continue;
                }

                if (Bool(behind, "isSameEntity") == true)
                {
                    continue;
                }

                Assert.That(Text(behind, "name"), Is.Not.EqualTo(Text(row, "name")),
                    $"A row claiming to wait for a different entity that carries its own name is the reported "
                    + $"defect wearing the new shape. Got: {Describe(rows)}");
            }
        }

        private async Task<JsonElement> TheWaitingBehindOf(UpdateType updateType, int id)
        {
            var row = await TheRowFor(updateType, id);

            // One assertion rather than two, because the second is meaningless without the first and the
            // analyzer reads a sequential pair as independent.
            var describesItsHolder =
                row.TryGetProperty("waitingBehind", out var behind) && behind.ValueKind is JsonValueKind.Object;

            Assert.That(describesItsHolder, Is.True,
                $"{updateType} {id} is queued behind something, so the row has to say what — as a described piece "
                + "of work, not a bare name. A missing field means the row explains nothing; a string "
                + $"means the browser has to guess which of two rows about one entity it is looking at. Got: {row}");

            return behind;
        }

        private static long? Milliseconds(JsonElement row, string property)
            => row.TryGetProperty(property, out var value) && value.ValueKind is JsonValueKind.Number
                ? value.GetInt64()
                : null;
    }
}
