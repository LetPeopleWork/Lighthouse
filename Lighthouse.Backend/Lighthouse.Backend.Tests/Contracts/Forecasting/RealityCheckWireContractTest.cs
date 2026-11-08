using System.Net;
using System.Text;
using System.Text.Json;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Tests.API.Integration.ForecastRealityCheck;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lighthouse.Backend.Tests.Contracts.Forecasting
{
    /// <summary>
    /// What a caller of the Forecast Reality Check sees on the wire: both routes, the request body it accepts
    /// and refuses, who is answered and who is told the Team was not found, and the single back test that
    /// sits beside it. What the check works out is asserted at the check's service instead.
    /// </summary>
    public class RealityCheckWireContractTest : WireContractTest
    {
        private const string LatestRoute = "/api/latest/forecast/reality-check/";

        private const string VersionedRoute = "/api/v1/forecast/reality-check/";

        private const string NoOptions = "{}";

        private const int LevelsPerCheck = 4;

        private const int ANumberNoTeamWasEverGiven = 987_654;

        /// <summary>Far fewer runs than production's ten thousand; a scenario needs a spread, not precision.</summary>
        private const int SimulatedRunsWhenTheEngineRuns = 1_000;

        private const long PinnedStartingNumber = 4172;

        private const string ARequestCarryingDatesFromLastYear =
            "{\"startDate\":\"2025-06-01\",\"endDate\":\"2025-07-01\",\"historicalStartDate\":\"2025-03-01\",\"historicalEndDate\":\"2025-06-01\"}";

        private static DateOnly TodayDay => RealityCheckForecastScript.TodayDay;

        /// <summary>
        /// The host's forecast service reads this one script for the life of the host, so each scenario
        /// starts by putting it back to what a new one says.
        /// </summary>
        private RealityCheckForecastScript Forecasts { get; } = new();

        private RealityCheckSeedSteps Seed { get; set; } = null!;

        protected override WebApplicationFactory<Program> AdjustTheHost(TestWebApplicationFactory<Program> unchanged)
        {
            var script = Forecasts;

            return TestWebApplicationFactory<Program>
                .WithTestAuthentication(unchanged)
                .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
                {
                    services.RemoveAll<ILighthouseClock>();
                    services.AddSingleton<ILighthouseClock>(new FakeLighthouseClock(RealityCheckForecastScript.Today));

                    services.RemoveAll<ILicenseService>();
                    services.AddSingleton(PortDoubles.PremiumLicence());

                    services.RemoveAll<IDrawStreamFactory>();
                    services.AddSingleton<IDrawStreamFactory>(new DrawsFromAPinnedStartingNumber(PinnedStartingNumber));
                    services.RemoveAll<ForecastSimulationLimits>();
                    services.AddSingleton(new ForecastSimulationLimits(
                        SimulatedRunsWhenTheEngineRuns,
                        ForecastSimulationLimits.Default.MostDaysOneSimulatedRunMayCover));

                    services.RemoveAll<IForecastService>();
                    services.AddScoped<ForecastService>();
                    services.AddScoped<IForecastService>(provider =>
                        script.Over(provider.GetRequiredService<ForecastService>()));
                }));
        }

        [OneTimeSetUp]
        public void PrepareTheSeedSteps()
        {
            Seed = new RealityCheckSeedSteps(Services, Forecasts, TodayDay);
        }

        [SetUp]
        public void StartTheScenarioFromTheSeededDatabase()
        {
            Forecasts.Reset();
            Seed.StartAfresh();

            using var scope = NewScope();
            foreach (var seeder in scope.ServiceProvider.GetServices<ISeeder>())
            {
                seeder.Seed().GetAwaiter().GetResult();
            }
        }

        [TearDown]
        public async Task ForgetTheTeamsThisScenarioSeeded()
        {
            await SharedHostCleanup.ForgetOwners(Services, Seed.OwnersSeeded);
        }

        // --- The check answers ---

        [Test]
        public async Task The_same_check_answers_on_the_versioned_route_as_well()
        {
            var oceanExplorer = GivenOceanExplorerFinishingWorkEveryDay();

            var answer = await ReadTheAnswer(await WhenSomebodyWhoCanReadTheTeamRunsTheCheck(oceanExplorer, route: VersionedRoute));

            ThenSixteenChecksWereRunAndEveryOneCouldBeEvaluated(answer);
        }

        [TestCase(NoOptions)]
        [TestCase("{\"applyFilterOverride\":null}")]
        [TestCase("{\"applyFilterOverride\":false}")]
        [TestCase("{\"applyFilterOverride\":true}")]
        public async Task The_check_answers_whether_or_not_the_forecast_filter_choice_is_given(string options)
        {
            var oceanExplorer = GivenOceanExplorerFinishingWorkEveryDay();

            var answer = await ReadTheAnswer(await WhenSomebodyWhoCanReadTheTeamRunsTheCheck(oceanExplorer, options));

            ThenSixteenChecksWereRunAndEveryOneCouldBeEvaluated(answer);
        }

        [TestCase("{\"applyFilterOverride\":\"yes\"}")]
        [TestCase("{\"applyFilterOverride\":1}")]
        [TestCase("{\"applyFilterOverride\":")]
        public async Task A_request_whose_filter_choice_is_not_yes_no_or_unset_is_refused_and_nothing_is_checked(string options)
        {
            var oceanExplorer = GivenOceanExplorerFinishingWorkEveryDay();
            await GivenTheCheckAnswersForATeamThatExists(oceanExplorer);

            using var refused = await WhenSomebodyWhoCanReadTheTeamRunsTheCheck(oceanExplorer, options);

            await ThenTheRequestIsRefusedWithoutASingleCheck(refused);
        }

        /// <summary>
        /// The check takes no dates. A caller that sends some anyway - an old client, a hand-written
        /// script - must not be able to move the check off today.
        /// </summary>
        [Test]
        public async Task Dates_sent_with_the_request_are_ignored_and_every_check_still_ends_today()
        {
            var oceanExplorer = GivenOceanExplorerFinishingWorkEveryDay();

            var answer = await ReadTheAnswer(await WhenSomebodyWhoCanReadTheTeamRunsTheCheck(oceanExplorer, ARequestCarryingDatesFromLastYear));

            ThenEveryCheckEndsToday(answer);
        }

        // --- Who may ask ---

        [Test]
        public async Task Tom_who_can_read_the_Team_but_not_change_it_gets_the_whole_answer()
        {
            var oceanExplorer = GivenOceanExplorerFinishingWorkEveryDay();

            var answer = await ReadTheAnswer(await WhenTomWhoCanOnlyReadTheTeamRunsTheCheck(oceanExplorer));

            ThenSixteenChecksWereRunAndEveryOneCouldBeEvaluated(answer);
        }

        /// <summary>
        /// Refused as "not found", exactly as the single back-test refuses, so the answer never reveals
        /// that a Team the person may not see exists at all.
        /// </summary>
        [Test]
        public async Task Somebody_who_cannot_read_the_Team_is_refused_and_learns_nothing_about_it()
        {
            var oceanExplorer = GivenOceanExplorerFinishingWorkEveryDay();
            var harbourPilots = Seed.GivenAnotherTeam("Harbour Pilots");

            using var refused = await WhenSomebodyWhoCanReadOnlyAnotherTeamRunsTheCheck(oceanExplorer, harbourPilots);

            await ThenTheyAreToldItWasNotFoundWithoutASingleCheck(refused);
        }

        [Test]
        public async Task A_Team_that_does_not_exist_is_answered_as_not_found()
        {
            var oceanExplorer = GivenOceanExplorerFinishingWorkEveryDay();
            await GivenTheCheckAnswersForATeamThatExists(oceanExplorer);

            using var answer = await WhenTheCheckIsRunForATeamNobodyCreated();

            ThenThereIsNoSuchTeam(answer);
        }

        // --- Read-only, end to end ---

        /// <summary>Nothing is stored between runs, so a second press over the same history answers identically.</summary>
        [Test]
        public async Task Running_the_check_twice_gives_the_same_answer_twice()
        {
            var coastalSurvey = GivenCoastalSurveyWhichFinishesWorkInBursts();

            var first = await TheAnswerTo(coastalSurvey);
            var second = await TheAnswerTo(coastalSurvey);

            ThenTheTwoAnswersAreTheSame(first, second);
        }

        // --- What must keep working beside it ---

        /// <summary>
        /// The single back-test sits in the same group on the same tab and its contract is not widened.
        /// It is also the harness's own proof that seeding, the pinned clock and the scripted forecast reach
        /// the real forecast endpoints.
        /// </summary>
        [Test]
        public async Task The_single_back_test_beside_the_check_still_answers_as_it_did()
        {
            var oceanExplorer = GivenOceanExplorerFinishingWorkEveryDay();

            using var response = await WhenMariaBackTestsTheLastFourWeeksByHand(oceanExplorer);

            await ThenTheBackTestAnswersWithFourLevelsAndWhatTheTeamDelivered(response);
        }

        // --- Given ---

        private int GivenOceanExplorerFinishingWorkEveryDay()
        {
            var team = Seed.GivenATeam("Ocean Explorer", 30);
            Seed.GivenTheTeamFinishedAWorkItemEveryDay(team);
            return team;
        }

        private int GivenCoastalSurveyWhichFinishesWorkInBursts()
        {
            var team = Seed.GivenATeam("Coastal Survey", 30);
            Seed.GivenTheTeamFinishedAWorkItemEvery(team, 5);
            return team;
        }

        private async Task GivenTheCheckAnswersForATeamThatExists(int teamId) => await TheAnswerTo(teamId);

        // --- When ---

        private async Task<HttpResponseMessage> WhenTheCheckIsRunBy(Func<HttpClient, HttpClient> identity, int teamId, string body = NoOptions, string route = LatestRoute)
        {
            identity(Client);
            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            return await Client.PostAsync($"{route}{teamId}", content);
        }

        private Task<HttpResponseMessage> WhenSomebodyWhoCanReadTheTeamRunsTheCheck(int teamId, string body = NoOptions, string route = LatestRoute)
            => WhenTheCheckIsRunBy(client => client.AsTeamViewer(teamId, "maria-santos"), teamId, body, route);

        private Task<HttpResponseMessage> WhenTomWhoCanOnlyReadTheTeamRunsTheCheck(int teamId)
            => WhenTheCheckIsRunBy(client => client.AsTeamViewer(teamId, "tom-becker"), teamId);

        private Task<HttpResponseMessage> WhenSomebodyWhoCanReadOnlyAnotherTeamRunsTheCheck(int teamId, int theTeamTheyCanRead)
            => WhenTheCheckIsRunBy(client => client.AsTeamViewer(theTeamTheyCanRead, "harbour-pilot"), teamId);

        private Task<HttpResponseMessage> WhenTheCheckIsRunForATeamNobodyCreated()
            => WhenTheCheckIsRunBy(client => client.AsSystemAdmin(), ANumberNoTeamWasEverGiven);

        private async Task<HttpResponseMessage> WhenMariaBackTestsTheLastFourWeeksByHand(int teamId)
        {
            Client.AsTeamViewer(teamId, "maria-santos");
            var body =
                $"{{\"startDate\":\"{TodayDay.AddDays(-28):yyyy-MM-dd}\",\"endDate\":\"{TodayDay:yyyy-MM-dd}\"," +
                $"\"historicalStartDate\":\"{TodayDay.AddDays(-58):yyyy-MM-dd}\",\"historicalEndDate\":\"{TodayDay.AddDays(-28):yyyy-MM-dd}\"}}";

            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            return await Client.PostAsync($"/api/latest/forecast/backtest/{teamId}", content);
        }

        private async Task<RealityCheckAnswer> TheAnswerTo(int teamId)
        {
            using var response = await WhenSomebodyWhoCanReadTheTeamRunsTheCheck(teamId);
            return await ReadTheAnswer(response);
        }

        private Task<RealityCheckAnswer> ReadTheAnswer(HttpResponseMessage response) => RealityCheckAnswer.ReadFrom(response, Forecasts);

        // --- Then ---

        private static void ThenEveryCheckEndsToday(RealityCheckAnswer answer)
        {
            var endingElsewhere = answer.Cells.Where(cell => cell.ScoredPeriodEnd != TodayDay).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.AnchorDate, Is.EqualTo(TodayDay),
                    "every check ends today; an answer anchored on any other day scores a period nobody asked about");
                Assert.That(endingElsewhere, Is.Empty,
                    "a check that does not end today is measured against a different stretch of time than the one the answer claims");
                Assert.That(answer.Cells, Is.Not.Empty, "an answer with no checks satisfies 'every check ends today' by having none");
            }
        }

        private static void ThenSixteenChecksWereRunAndEveryOneCouldBeEvaluated(RealityCheckAnswer answer)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.Cells, Has.Count.EqualTo(16));
                Assert.That(answer.Cells.Where(cell => !cell.IsSufficient), Is.Empty,
                    "this Team finishes work every day, so every check has plenty of history");
                Assert.That(answer.RunsEvaluated, Is.EqualTo(16));
            }
        }

        private static void ThenTheTwoAnswersAreTheSame(RealityCheckAnswer first, RealityCheckAnswer second)
        {
            Assert.That(second.Root.GetRawText(), Is.EqualTo(first.Root.GetRawText()),
                "nothing is kept between runs, so the same history answers the same way");
        }

        private static async Task ThenTheyAreToldItWasNotFoundWithoutASingleCheck(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
                    "a person who cannot read the Team is told it was not found, like the back-test, so the answer never reveals that it exists");
                Assert.That(body, Does.Not.Contain("cells"));
            }
        }

        private static async Task ThenTheRequestIsRefusedWithoutASingleCheck(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(body, Does.Not.Contain("cells"));
            }
        }

        private static void ThenThereIsNoSuchTeam(HttpResponseMessage response)
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        private static async Task ThenTheBackTestAnswersWithFourLevelsAndWhatTheTeamDelivered(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            using var document = JsonDocument.Parse(body);
            var result = document.RootElement;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(RealityCheckAnswer.Field(result, "percentiles").GetArrayLength(), Is.EqualTo(LevelsPerCheck));
                Assert.That(RealityCheckAnswer.Field(result, "actualThroughput").GetInt32(), Is.GreaterThan(0),
                    "the Team finished work every day of the four weeks it was back-tested over");
            }
        }
    }
}
