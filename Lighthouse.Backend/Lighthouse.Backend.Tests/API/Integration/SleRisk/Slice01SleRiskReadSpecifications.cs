using System.Globalization;
using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.SleRisk
{
    /// <summary>
    /// DISTILL step definitions for Epic #4127 slice 01.
    ///
    /// The contract: for an in-flight item of age a, against a published target of R days, over the
    /// cycle times of the work finished inside the asked-for window, the answer is
    ///
    ///     count(T > R AND T >= a) / count(T >= a)
    ///
    /// and nothing else. Two counts. Both boundaries matter and both are pinned by their own scenario:
    /// an item that finished on the target day met it, and an item as old as a finished one is still
    /// being compared against it.
    ///
    /// Everything here reads the answer off the wire. The value is shown in four places before this
    /// Epic is done — a dialog column, a card chip, chart zones and a tracker field — so what the
    /// endpoint says is the only thing all four can agree with.
    /// </summary>
    public partial class Slice01SleRiskReadTest : SleRiskAcceptanceTest
    {
        private HttpResponseMessage response = null!;

        private string body = string.Empty;

        [TearDown]
        public void DisposeResponse()
        {
            response?.Dispose();
        }

        // --- Given ---

        private int GivenATeamThatPromisesTenDays() => GivenATeamPromising(10);

        private int GivenATeamThatPromisesSixDays() => GivenATeamPromising(6);

        private int GivenATeamThatPromisesThirtyDays() => GivenATeamPromising(30);

        private int GivenATeamWithNoTarget() => GivenATeamPromising(0);

        private int GivenATeamPromising(int rangeInDays)
        {
            TheTeamUnderTest = SeedTeamWithTarget(rangeInDays);
            return TheTeamUnderTest;
        }

        private void GivenTheTeamHasFinished(params int[] cycleTimesInDays)
            => SeedFinishedItems(TheTeamUnderTest, cycleTimesInDays);

        /// <summary>
        /// The same distribution several times over. A scenario about the arithmetic needs enough
        /// finished work at the age it asks about for the answer to be given at all — the shape of
        /// the distribution is what it is pinning, and the copies are what let it be shown.
        /// </summary>
        private void GivenTheTeamHasFinishedSeveralOfEach(int copies, params int[] cycleTimesInDays)
        {
            for (var copy = 0; copy < copies; copy++)
            {
                SeedFinishedItems(TheTeamUnderTest, cycleTimesInDays);
            }
        }

        /// <summary>
        /// Finished, but closed before the window the scenario asks about — so it exists, and is
        /// still not evidence.
        /// </summary>
        private void GivenTheTeamAlsoFinishedLongAgo(params int[] cycleTimesInDays)
        {
            foreach (var cycleTime in cycleTimesInDays)
            {
                SeedFinishedItem(TheTeamUnderTest, cycleTime, closedDaysBeforeWindowStart: 30);
            }
        }

        private string GivenAnItemOpenFor(int ageInDays) => SeedInFlightItem(TheTeamUnderTest, ageInDays);

        private void GivenTheTeamNowPromises(int rangeInDays)
            => ChangeTheTargetOf(TheTeamUnderTest, rangeInDays);

        /// <summary>
        /// The team widens how far back it counts throughput. The evidence window is the other half
        /// of what the answer depends on, and it moves independently of the target.
        /// </summary>
        private void GivenTheTeamNowLooksBackFurther()
            => ChangeTheHistoryOf(TheTeamUnderTest, 365);

        /// <summary>
        /// The team stops rolling its evidence window with the calendar and pins it. Everything it
        /// has finished stays inside; what changes is that the window no longer moves overnight.
        /// </summary>
        private void GivenTheTeamPinsItsHistory()
            => PinTheHistoryOf(TheTeamUnderTest, startDaysAgo: 90, endDaysAgo: 0);

        private void GivenADayHasPassed() => AdvanceTheInstanceToTomorrow();

        private int GivenAPortfolio()
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var portfolio = new Portfolio
            {
                Name = $"Portfolio {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Name = $"Connection {Guid.NewGuid():N}",
                    WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
                },
                ServiceLevelExpectationRange = 10,
                ServiceLevelExpectationProbability = 80,
            };

            var repository = sp.GetRequiredService<IRepository<Portfolio>>();
            repository.Add(portfolio);
            repository.Save().GetAwaiter().GetResult();

            return portfolio.Id;
        }

        // --- When ---

        private async Task WhenTheRiskIsAskedFor(int teamId)
        {
            Client.AsTeamAdmin(teamId);
            await TheAnswerTo(SleRiskRoute(teamId));
        }

        /// <summary>
        /// Asks for the chart background first, then for the per-item risk, as the same caller.
        ///
        /// Signing in as a team admin is what makes the first answer mean anything. A caller with no
        /// grant on this team is also told 404 rather than 403, so a 404 from an anonymous client
        /// would be indistinguishable from "you may not look" and would pass whether the route
        /// existed or not.
        /// </summary>
        private async Task WhenBothTheBackgroundAndThePerItemRiskAreAskedFor(int teamId)
        {
            Client.AsTeamAdmin(teamId);

            var zones = new Uri(
                $"/api/latest/teams/{teamId}/metrics/sleRisk/zones"
                + $"?startDate={WindowStart:yyyy-MM-dd}&endDate={WindowEnd:yyyy-MM-dd}",
                UriKind.Relative);

            await TheAnswerTo(zones);
            zonesResponse = response;

            await TheAnswerTo(SleRiskRoute(teamId));
        }

        private HttpResponseMessage? zonesResponse;

        private void ThenTheBackgroundIsGoneAndThePerItemRiskIsNot()
        {
            Assert.That(zonesResponse, Is.Not.Null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(zonesResponse!.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
                    "The chart background that painted where the odds turn was withdrawn, so there is "
                    + "nothing left to ask for.");

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "The per-item risk is what survives, and a deletion that took it with them would "
                    + $"be a far worse outcome than the one being fixed. Body: {body}");
            }
        }

        /// <summary>
        /// The route binds no dates, so anything sent is ignored by the framework rather than by a
        /// line of ours. A bundle built before this slice still sends them, and must still be right.
        /// </summary>
        private async Task WhenTheRiskIsAskedForWithAStrayRange(int teamId)
        {
            Client.AsTeamAdmin(teamId);
            await TheAnswerTo(new Uri(
                $"/api/latest/teams/{teamId}/metrics/sleRisk"
                + $"?startDate={WindowStart:yyyy-MM-dd}&endDate={WindowEnd:yyyy-MM-dd}",
                UriKind.Relative));
        }

        private async Task WhenSomeoneWithoutAccessToTheTeamAsks(int teamId)
        {
            // A viewer with no grant on this team. Not anonymous — an anonymous refusal would pass
            // even if the route ignored team scope entirely.
            Client.AsViewer();
            await TheAnswerTo(SleRiskRoute(teamId));
        }

        private async Task WhenTheRiskIsAskedForThePortfolio(int portfolioId)
        {
            Client.AsPortfolioAdmin(portfolioId);
            await TheAnswerTo(new Uri(
                $"/api/latest/portfolios/{portfolioId}/metrics/sleRisk"
                + $"?startDate={WindowStart:yyyy-MM-dd}&endDate={WindowEnd:yyyy-MM-dd}",
                UriKind.Relative));
        }

        /// <summary>
        /// A scenario may ask more than once — that is how anything about remembering an answer gets
        /// observed at all — so each answer replaces the one before it and disposes of it.
        /// </summary>
        private async Task TheAnswerTo(Uri route)
        {
            response?.Dispose();
            response = await Client.GetAsync(route);
            body = await response.Content.ReadAsStringAsync();
        }

        // --- Then ---

        private void ThenTheItemsChanceOfMissingIs(string referenceId, int expectedPercent)
        {
            var entry = TheEntryFor(referenceId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(entry.TryGetProperty("risk", out var risk), Is.True,
                    $"Every entry carries a risk. Got: {body}");
                Assert.That(risk.ValueKind, Is.Not.EqualTo(JsonValueKind.Null),
                    $"{referenceId} has finished work to be compared against, so it has an answer. Got: {body}");
                Assert.That(risk.GetInt32(), Is.EqualTo(expectedPercent),
                    $"{referenceId} should read {expectedPercent}%. Body: {body}");
            }
        }

        /// <summary>
        /// Drives the write-back's resolution beside the read, in one scope on one day, and compares
        /// the two against each other rather than each against an expectation. An expectation both
        /// could satisfy while disagreeing is exactly what let 27% and 18 ship together.
        /// </summary>
        private void ThenTheBoardWouldBeWrittenTheSameNumbersTheScreensShow(int teamId, params string[] referenceIds)
        {
            ThenTheAnswerArrived();

            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var team = sp.GetRequiredService<IRepository<Team>>().GetById(teamId)!;
            team.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                ValueSource = WriteBackValueSource.SleRisk,
                AppliesTo = WriteBackAppliesTo.Team,
                AdditionalFieldDefinition = new AdditionalFieldDefinition { Reference = "Custom.Risk", DisplayName = "Risk" },
            });

            var plan = sp.GetRequiredService<IWriteBackTriggerService>().ResolveWriteBackForTeam(team);
            var written = plan
                .Where(update => update.TargetFieldReference == "Custom.Risk")
                .ToDictionary(update => update.WorkItemId, update => update.Value);

            using (Assert.EnterMultipleScope())
            {
                foreach (var referenceId in referenceIds)
                {
                    var onScreen = TheEntryFor(referenceId).GetProperty("risk").GetInt32();

                    Assert.That(written.ContainsKey(referenceId), Is.True,
                        $"The screens answered for {referenceId} and the board was told nothing. "
                        + $"Written: [{string.Join(", ", written.Select(w => $"{w.Key}={w.Value}"))}]");
                    Assert.That(written[referenceId], Is.EqualTo(onScreen.ToString(CultureInfo.InvariantCulture)),
                        $"{referenceId} reads {onScreen} on the screens. A coach who sees one number in "
                        + "Lighthouse and another on their board has to decide which to believe.");
                }
            }
        }

        private void ThenNothingIsSaidAboutAnyItem()
        {
            ThenTheAnswerArrived();

            using var document = JsonDocument.Parse(body);
            Assert.That(document.RootElement.EnumerateArray().Any(), Is.False,
                "A team that published no target made no promise, so no item of theirs can be at risk "
                + $"of breaking one. Body: {body}");
        }

        private void ThenTheyAreTurnedAway()
        {
            Assert.That(response.StatusCode,
                Is.EqualTo(HttpStatusCode.Forbidden).Or.EqualTo(HttpStatusCode.NotFound),
                $"The risk names the team's own work and is readable by whoever may read the team. Body: {body}");
        }

        private void ThenThereIsNoSuchQuestionToAsk()
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
                "A feature can sit in several portfolios, each with its own target and its own history, "
                + "so it would have several answers and no way to pick one. The route not existing is "
                + $"how that stays decided. Body: {body}");
        }

        // --- Helpers ---

        /// <summary>
        /// The team each Given in a scenario adds to. Seeding returns the id, but the scenario reads
        /// better when only the first Given mentions it, so it is remembered here.
        /// </summary>
        private int TheTeamUnderTest { get; set; }

        private void ThenTheAnswerArrived()
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"The read is the driving port for this whole slice. Body: {body}");
        }

        private JsonElement TheEntryFor(string referenceId)
        {
            ThenTheAnswerArrived();

            using var document = JsonDocument.Parse(body);

            var matches = document.RootElement
                .EnumerateArray()
                .Where(entry => entry.TryGetProperty("referenceId", out var reference)
                    && reference.GetString() == referenceId)
                .Select(entry => entry.Clone())
                .ToList();

            Assert.That(matches, Has.Count.EqualTo(1),
                $"Expected exactly one entry for {referenceId}. Body: {body}");

            return matches[0];
        }
    }
}
