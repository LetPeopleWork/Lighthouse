using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Tests.API.Integration.TaskManager;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using System.Net;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.API.Integration.UpdateQueueLanes
{
    /// <summary>
    /// DISTILL acceptance harness for ADO #5877 — the update queue's lanes and the wall-time bound.
    ///
    /// It extends the task-manager harness rather than standing up a second one, because the driving
    /// port is the same in both: a refresh triggered on an updater and run by the production update
    /// queue in its own DI scope, observed through the task list, the browser push, the refresh history
    /// and the operator-visible log. Everything on that path stays production; only the work-tracking
    /// connector, the forecast service and the licence service are doubles.
    ///
    /// What is added here is the vocabulary a lane story needs and a single-lane story did not: a
    /// portfolio that refreshes on schedule, a tracker that holds every kind of refresh open at once,
    /// and a way to read the refresh history and write the instance's run limit.
    /// </summary>
    public abstract class UpdateQueueLanesAcceptanceTest : TaskManagerAcceptanceTest
    {
        /// <summary>
        /// The instance-wide setting holding the longest a single run may take, in whole minutes. Named
        /// here rather than read from the production constant so that renaming the key is a decision
        /// somebody makes against a failing test rather than a rename that quietly takes the setting
        /// away from every instance that already has a row for it.
        /// </summary>
        protected const string RunLimitSettingKey = "Update:MaxRunMinutes";

        protected const string RefreshHistoryRoute = "/api/latest/systeminfo/refreshlog";

        /// <summary>
        /// Far more pages than any scenario here lets run, so "it stopped" cannot be satisfied by a
        /// tracker that simply ran out of things to say.
        /// </summary>
        private const int PagesTheTrackerCouldGiveForever = 10_000;

        private const int PollIntervalMs = 20;

        private TaskCompletionSource? theTrackerMayAnswer;

        private int pagesTheTrackerWasAskedFor;

        protected readonly record struct SeededPortfolio(int Id, string Name);

        /// <summary>
        /// Runs before the harness tears the host down, because NUnit unwinds from the derived class
        /// outwards. A scenario that left the tracker gated would otherwise leave refreshes parked in
        /// several lanes while the database underneath them is deleted.
        /// </summary>
        [TearDown]
        public async Task LetEveryGatedRefreshFinish()
        {
            ReleaseTheTracker();
            await TheQueueGoesIdle();
        }

        // --- Given ---

        /// <summary>
        /// The name is unique per call because these fixtures assert on names: two portfolios sharing
        /// one would let an assertion pass against the wrong row.
        /// </summary>
        protected SeededPortfolio GivenAPortfolioThatIsRefreshedOnSchedule()
        {
            var portfolioName = $"Portfolio {Guid.NewGuid():N}";
            return new SeededPortfolio(SeedPortfolio(SeedConnection(), portfolioName), portfolioName);
        }

        /// <summary>
        /// Every kind of refresh held open at once, and held rather than merely slowed. Starvation is
        /// the failure mode this story is about, and a test that waits for a slow refresh to finish
        /// passes just as well against the bug: the second refresh does start, eventually, which is
        /// exactly the complaint.
        /// </summary>
        protected void GivenTheTrackerHoldsEveryRefreshOpenUntilWeSaySo()
        {
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            theTrackerMayAnswer = gate;

            ConnectorMock
                .Setup(c => c.GetWorkItemsForTeam(It.IsAny<Team>(), It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    await gate.Task;
                    return [];
                });

            ConnectorMock
                .Setup(c => c.GetFeaturesForProject(It.IsAny<Portfolio>(), It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    await gate.Task;
                    return [];
                });
        }

        /// <summary>
        /// A refresh held open the way a real slow one is held open: the connector is still going, page
        /// after page, and each page is a point at which it could notice it has been asked to stop. The
        /// count is what makes "it stopped" observable — a refresh that ran to completion would ask for
        /// every page there is.
        /// </summary>
        protected void GivenTheTrackerAnswersOnePageAtATimeUntilWeSaySo()
        {
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            theTrackerMayAnswer = gate;

            ConnectorMock
                .Setup(c => c.GetWorkItemsForTeam(It.IsAny<Team>(), It.IsAny<CancellationToken>()))
                .Returns(async (Team _, CancellationToken stopping) =>
                {
                    await PageUntilStopped(gate, stopping);
                    return [];
                });

            ConnectorMock
                .Setup(c => c.GetFeaturesForProject(It.IsAny<Portfolio>(), It.IsAny<CancellationToken>()))
                .Returns(async (Portfolio _, CancellationToken stopping) =>
                {
                    await PageUntilStopped(gate, stopping);
                    return [];
                });
        }

        private async Task PageUntilStopped(TaskCompletionSource gate, CancellationToken stopping)
        {
            for (var page = 0; page < PagesTheTrackerCouldGiveForever; page++)
            {
                stopping.ThrowIfCancellationRequested();
                Interlocked.Increment(ref pagesTheTrackerWasAskedFor);
                await Task.Delay(PollIntervalMs, CancellationToken.None);
            }

            await gate.Task;
        }

        protected int PagesTheTrackerHasBeenAskedFor => Volatile.Read(ref pagesTheTrackerWasAskedFor);

        protected void ReleaseTheTracker()
        {
            theTrackerMayAnswer?.TrySetResult();
        }

        /// <summary>
        /// Records the instance's run limit as an operator would have left it — including the values an
        /// operator could not have meant. Written straight through the repository because the read is
        /// what is under test here, and going through a settings route would make the scenario depend on
        /// the write path answering correctly before the read has been shown to.
        /// </summary>
        protected void GivenTheInstanceRunLimitIsRecordedAs(string value)
        {
            using var scope = Factory.Services.CreateScope();
            var settings = scope.ServiceProvider.GetRequiredService<IRepository<AppSetting>>();
            var existing = settings.GetByPredicate(setting => setting.Key == RunLimitSettingKey);

            if (existing == null)
            {
                settings.Add(new AppSetting { Key = RunLimitSettingKey, Value = value });
            }
            else
            {
                existing.Value = value;
                settings.Update(existing);
            }

            settings.Save().GetAwaiter().GetResult();
        }

        protected void GivenNoRunLimitHasEverBeenRecorded()
        {
            using var scope = Factory.Services.CreateScope();
            var settings = scope.ServiceProvider.GetRequiredService<IRepository<AppSetting>>();
            var existing = settings.GetByPredicate(setting => setting.Key == RunLimitSettingKey);

            if (existing == null)
            {
                return;
            }

            settings.Remove(existing);
            settings.Save().GetAwaiter().GetResult();
        }

        // --- When ---

        protected Task WhenARefreshOfThatTeamIsUnderWay(SeededTeam team)
            => TriggerAndWaitUntil(
                sp => sp.GetRequiredService<ITeamUpdater>().TriggerUpdate(team.Id),
                KeyOf(team),
                UpdateProgress.InProgress);

        protected Task WhenARefreshOfThatTeamIsAlsoAskedFor(SeededTeam team)
            => TriggerAndWaitUntil(
                sp => sp.GetRequiredService<ITeamUpdater>().TriggerUpdate(team.Id),
                KeyOf(team),
                UpdateProgress.Queued);

        protected Task WhenARefreshOfThatPortfolioIsUnderWay(SeededPortfolio portfolio)
            => TriggerAndWaitUntil(
                sp => sp.GetRequiredService<IPortfolioUpdater>().TriggerUpdate(portfolio.Id),
                KeyOf(portfolio),
                UpdateProgress.InProgress);

        protected Task WhenARefreshOfThatPortfolioIsAlsoAskedFor(SeededPortfolio portfolio)
            => TriggerAndWaitUntil(
                sp => sp.GetRequiredService<IPortfolioUpdater>().TriggerUpdate(portfolio.Id),
                KeyOf(portfolio),
                UpdateProgress.Queued);

        protected async Task WhenTheOperatorStops(UpdateKey key)
        {
            using var client = Factory.CreateClient();
            using var response = await client.PostAsync(CancelRouteFor(key), null);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent),
                $"Cancel is a driving port for this story; it answered {(int)response.StatusCode}.");
        }

        // --- Then ---

        protected static UpdateKey KeyOf(SeededTeam team) => new(UpdateType.Team, team.Id);

        protected static UpdateKey KeyOf(SeededPortfolio portfolio) => new(UpdateType.Features, portfolio.Id);

        protected static Uri CancelRouteFor(UpdateKey key)
            => new($"{TaskListRoute}/{key.UpdateType}/{key.Id}/cancel", UriKind.Relative);

        /// <summary>
        /// Every assertion in this folder waits rather than looks. Admission is synchronous but the work
        /// itself runs on the queue's own threads and several of the pushes on the path are dispatched
        /// rather than awaited, so a read taken the instant after a trigger says nothing about whether
        /// the instance did the thing or has not got round to it yet.
        /// </summary>
        protected async Task<bool> ItReaches(UpdateKey key, UpdateProgress progress)
        {
            var store = Factory.Services.GetRequiredService<IUpdateStatusStore>();
            var deadline = DateTime.UtcNow.Add(PatienceForWorkToMove);

            while (DateTime.UtcNow < deadline)
            {
                if (store.TryGet(key, out var status) && status?.Status == progress)
                {
                    return true;
                }

                await Task.Delay(PollIntervalMs);
            }

            return false;
        }

        /// <summary>
        /// Whether the browser was ever told this key reached a state — the only way to observe a
        /// terminal status, because the store drops the key the moment the run ends.
        /// </summary>
        protected async Task<bool> TheBrowserIsToldItReached(UpdateKey key, UpdateProgress progress)
        {
            var deadline = DateTime.UtcNow.Add(PatienceForWorkToMove);

            while (DateTime.UtcNow < deadline)
            {
                if (TheBrowserWasTold.For(key.UpdateType, key.Id).Contains(progress))
                {
                    return true;
                }

                await Task.Delay(PollIntervalMs);
            }

            return false;
        }

        /// <summary>
        /// A negative claim over a window, used where the promise is that something does NOT happen —
        /// a lane that must not be taken, a run that must not be stopped. The window is deliberately a
        /// tiny fraction of anything it is asserting about, so it catches a mechanism that fires at once
        /// rather than one that fires a little late; it is not a timing budget and nothing here is
        /// calibrated against a local measurement.
        /// </summary>
        protected async Task<bool> ItStaysAt(UpdateKey key, UpdateProgress progress)
        {
            var store = Factory.Services.GetRequiredService<IUpdateStatusStore>();
            var deadline = DateTime.UtcNow.Add(WindowForSomethingThatShouldNotHappen);

            while (DateTime.UtcNow < deadline)
            {
                if (!store.TryGet(key, out var status) || status?.Status != progress)
                {
                    return false;
                }

                await Task.Delay(PollIntervalMs);
            }

            return true;
        }

        protected static TimeSpan PatienceForWorkToMove => TimeSpan.FromSeconds(30);

        protected static TimeSpan WindowForSomethingThatShouldNotHappen => TimeSpan.FromSeconds(2);

        /// <summary>
        /// What the instance recorded about its refreshes, read the way Settings → System Info reads it.
        /// The entity is returned raw, so a column added to the refresh log shows up here without the
        /// route or the response type changing.
        /// </summary>
        protected async Task<IReadOnlyList<JsonElement>> TheRefreshHistory()
        {
            using var client = Factory.CreateClient();
            using var response = await client.GetAsync(new Uri(RefreshHistoryRoute, UriKind.Relative));
            var answered = response.StatusCode;
            var body = await response.Content.ReadAsStringAsync();

            Assert.That(answered, Is.EqualTo(HttpStatusCode.OK),
                $"The refresh history is where an operator reads what happened; it answered {(int)answered}.");

            using var document = JsonDocument.Parse(body);
            return [.. document.RootElement.EnumerateArray().Select(element => element.Clone())];
        }

        protected async Task<JsonElement?> TheRecordedRunOf(string entityName)
        {
            var history = await TheRefreshHistory();

            return history
                .Where(row => Text(row, "entityName") == entityName)
                .Select(row => (JsonElement?)row)
                .LastOrDefault();
        }

        /// <summary>
        /// The one line a refresh round writes about itself, as an operator reads it at default
        /// reporting settings. A round that says its piece twice and a round that never says it at all
        /// are both visible here, which is what makes "once, together" assertable without reaching
        /// inside the round.
        /// </summary>
        protected List<string> TheRoundSummaryLinesNaming(string entityName)
            => [.. TheOperatorVisibleLines.Where(line =>
                line.Contains("Update completed", StringComparison.Ordinal)
                && line.Contains(entityName, StringComparison.Ordinal))];

        private async Task TriggerAndWaitUntil(Action<IServiceProvider> trigger, UpdateKey key, UpdateProgress reached)
        {
            trigger(Factory.Services);

            var arrived = await ItReaches(key, reached);

            Assert.That(arrived, Is.True,
                $"{key.UpdateType} {key.Id} never reached {reached}; the scenario has nothing to observe.");
        }
    }
}
