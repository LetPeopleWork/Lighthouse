using System.Net;
using System.Text;
using System.Text.Json;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Implementation.Licensing;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Tests.API.Integration.ForecastRealityCheck;
using Lighthouse.Backend.Tests.API.Integration.Refinement;
using Lighthouse.Backend.Tests.Contracts.PortFixtures;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using static Lighthouse.Backend.Tests.API.Integration.ForecastRealityCheck.RealityCheckAnswer;

namespace Lighthouse.Backend.Tests.Contracts.Forecasting
{
    /// <summary>
    /// What a caller of the Forecast Reality Check sees on the wire: both routes, the request body it accepts
    /// and refuses, who is answered and who is told the Team was not found, and the single back test that
    /// sits beside it. What the check works out is asserted at the check's service instead.
    ///
    /// The same host also carries the one scenario that runs the shipped engine end to end, and the check that
    /// the check's port fixtures register their services as the host does, so the three share one host start.
    /// </summary>
    public class RealityCheckWireContractTest : WireContractTest
    {
        private const string LatestRoute = "/api/latest/forecast/reality-check/";

        private const string VersionedRoute = "/api/v1/forecast/reality-check/";

        private const string NoOptions = "{}";

        private const int LevelsPerCheck = 4;

        private const int ANumberNoTeamWasEverGiven = 987_654;

        private const string ARequestCarryingDatesFromLastYear =
            "{\"startDate\":\"2025-06-01\",\"endDate\":\"2025-07-01\",\"historicalStartDate\":\"2025-03-01\",\"historicalEndDate\":\"2025-06-01\"}";

        private static readonly int[] TheConfidenceLevels = [50, 70, 85, 95];

        private static readonly string[] TheMembersAnUnevaluatedCellLeavesEmpty = ["forecast", "actualCompleted", "outcome", "levelOutcomes"];

        private static readonly Type[] TheEnumsOnTheAnswer =
            [typeof(Determination), typeof(CurrentSettingStanding), typeof(NotTestedReason), typeof(SufficiencyReason), typeof(CellOutcome), typeof(LevelReading)];

        /// <summary>
        /// Words whose presence in a property name would mean the answer names, orders or bounds a
        /// winner - or ships a rendered clause in one instance's vocabulary.
        /// </summary>
        private static readonly string[] WordsThatWouldNameAWinner =
            ["recommend", "best", "rank", "winner", "bound", "windowScore", "sentence", "clause"];

        private static DateOnly TodayDay => RealityCheckForecastScript.TodayDay;

        /// <summary>
        /// The host's forecast service reads this one script for the life of the host, so each scenario
        /// starts by putting it back to what a new one says.
        /// </summary>
        private RealityCheckForecastScript Forecasts { get; } = new();

        private RealityCheckSeedSteps Seed { get; set; } = null!;

        /// <summary>The host's registrations as the product and the test host make them, before this fixture changes any.</summary>
        private List<ServiceDescriptor> hostAsItIs = [];

        protected override WebApplicationFactory<Program> AdjustTheHost(TestWebApplicationFactory<Program> unchanged)
        {
            var script = Forecasts;
            var readBeforeAnyChange = unchanged.WithWebHostBuilder(builder =>
                builder.ConfigureTestServices(services => hostAsItIs = [.. services]));

            return TestWebApplicationFactory<Program>
                .WithTestAuthentication(readBeforeAnyChange)
                .WithWebHostBuilder(builder => builder.ConfigureServices(services => services.PinWhatTheCheckDependsOn(script)));
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

        /// <summary>
        /// The one scenario that runs the shipped forecast engine end to end. Everything else scripts the
        /// forecast so it can say which levels held; this one proves the real engine's answer is read the
        /// same way.
        /// </summary>
        [Test]
        public async Task Maria_runs_the_reality_check_on_Ocean_Explorer_and_gets_an_answer_without_giving_a_date()
        {
            var oceanExplorer = GivenOceanExplorerFinishingWorkEveryDay();
            Forecasts.RunAsShipped();
            var storedBefore = Seed.WhatIsStoredFor(oceanExplorer);

            var answer = await ReadTheAnswer(await WhenSomebodyWhoCanReadTheTeamRunsTheCheck(oceanExplorer));

            using (Assert.EnterMultipleScope())
            {
                ThenEveryCheckEndsToday(answer);
                ThenSixteenChecksWereRunAndEveryOneCouldBeEvaluated(answer);
                ThenEachLevelHeldExactlyWhenTheTeamDeliveredAtLeastItsForecast(answer);
                ThenTheDenominatorCountsWhatWasEvaluated(answer);
                ThenNothingAboutTheTeamChanged(oceanExplorer, storedBefore);
            }
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

        // --- Every member, by the name the browser reads ---

        [Test]
        public async Task Every_member_of_a_real_answer_has_the_name_the_browser_reads()
        {
            var oceanExplorer = GivenOceanExplorerFinishingWorkEveryDay();
            Forecasts.RunAsShipped();

            var answer = await TheAnswerTo(oceanExplorer);

            ThenEveryMemberIsReadByItsName(answer, oceanExplorer);
        }

        [Test]
        public async Task A_cell_that_cannot_be_evaluated_states_its_members_as_null_and_days_as_plain_dates()
        {
            var coastalSurvey = GivenCoastalSurveyWhichFinishesWorkInBursts();

            var answer = await TheAnswerTo(coastalSurvey);

            using (Assert.EnterMultipleScope())
            {
                ThenEveryUnevaluatedCellStatesItsEmptyMembersAsNull(answer);
                ThenEveryDayIsAPlainDate(answer);
            }
        }

        [Test]
        public void Every_reason_and_outcome_travels_as_its_name()
        {
            var theHostsOptions = Services.GetRequiredService<IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>>().Value.JsonSerializerOptions;

            var everyMember = TheEnumsOnTheAnswer.SelectMany(type => Enum.GetValues(type).Cast<Enum>()).ToList();
            var notByName = everyMember
                .Select(member => (Member: member, Wire: JsonSerializer.Serialize(member, member.GetType(), theHostsOptions)))
                .Where(sent => sent.Wire != $"\"{sent.Member}\"")
                .Select(sent => $"{sent.Member.GetType().Name}.{sent.Member} goes out as {sent.Wire}")
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(notByName, Is.Empty, "the browser compares these by their names, so a number reads as none of them");
                Assert.That(everyMember, Has.Count.GreaterThan(TheEnumsOnTheAnswer.Length), "each of the six has members to send");
            }
        }

        // --- Nothing on the wire can rank a window ---

        [TestCase(30)]
        [TestCase(45)]
        public async Task No_part_of_the_answer_on_the_wire_can_rank_a_window(int samplingWindowDays)
        {
            var team = GivenATeamFinishingWorkEveryDaySetTo(samplingWindowDays);
            GivenTheWindowsBehaveDifferently(samplingWindowDays);

            var answer = await TheAnswerTo(team);

            using (Assert.EnterMultipleScope())
            {
                ThenNothingInTheAnswerCarriesANumberPerWindow(answer);
                ThenTheAnswerCarriesFactsAndNoSentence(answer);
            }
        }

        [Test]
        public async Task The_windows_of_an_off_ladder_setting_rank_nothing_on_the_wire()
        {
            var team = GivenATeamFinishingWorkEveryDaySetTo(45);
            Forecasts.EveryCheckOf(45, HeldUpTo.NoLevel);

            var answer = await TheAnswerTo(team);

            ThenNothingInTheAnswerCarriesANumberPerWindow(answer);
        }

        [Test]
        public async Task A_check_with_no_forecast_puts_no_negative_number_on_the_wire()
        {
            var oceanExplorer = GivenOceanExplorerFinishingWorkEveryDay();
            Forecasts.CannotBeWorkedOutFor(30, 28);

            var answer = await TheAnswerTo(oceanExplorer);

            ThenNoNegativeNumberReachesTheAnswer(answer);
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

        // --- The port fixtures register their services as the host does ---

        private static IEnumerable<TestCaseData> PortFixtures()
        {
            yield return PortFixture<Slice01OneSentenceAboutYourSamplingWindowTest>();
            yield return PortFixture<Slice04TheAnswerOpensInADialogTest>();
            yield return PortFixture(nameof(RefinementAtThePortTest), () => RefinementAtThePortTest.WhatItRegisters);
        }

        private static TestCaseData PortFixture<TFixture>() where TFixture : RealityCheckAtThePortTest, new()
            => PortFixture(typeof(TFixture).Name, () => new TFixture().WhatItRegisters);

        private static TestCaseData PortFixture(string fixtureName, Func<IServiceCollection> whatItRegisters)
            => new TestCaseData(fixtureName, whatItRegisters).SetArgDisplayNames(fixtureName);

        [TestCaseSource(nameof(PortFixtures))]
        public void Every_service_a_port_fixture_registers_lives_as_long_as_it_does_on_the_host(string fixtureName, Func<IServiceCollection> whatItRegisters)
        {
            var differences = new PortFixtureLifetimes(hostAsItIs).HowItDiffersFromTheHost(whatItRegisters());

            Assert.That(differences, Is.Empty, $"{fixtureName} registers services unlike the host");
        }

        /// <summary>
        /// This host swaps the licence for a stand-in and the access check for one that reads the test caller's
        /// claims, so a port fixture compared with the host as it runs here would be compared with the swaps. The
        /// licence and the access check show which of the two the comparison reads.
        /// </summary>
        [Test]
        public void The_port_fixtures_are_compared_with_the_host_before_this_fixture_swapped_anything()
        {
            var hostBeforeTheSwaps = new PortFixtureLifetimes(hostAsItIs);
            var licenceBeforeTheSwaps = hostBeforeTheSwaps.OnTheHost(typeof(ILicenseService));
            var accessCheckBeforeTheSwaps = hostBeforeTheSwaps.OnTheHost(typeof(IRbacAdministrationService));
            using var scope = NewScope();
            var licenceThisHostAnswersWith = scope.ServiceProvider.GetRequiredService<ILicenseService>();
            var accessCheckThisHostAnswersWith = scope.ServiceProvider.GetRequiredService<IRbacAdministrationService>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(licenceBeforeTheSwaps, Is.Not.Null, "the host registers a licence");
                Assert.That(licenceBeforeTheSwaps!.Lifetime, Is.EqualTo(ServiceLifetime.Scoped));
                Assert.That(PortFixtureLifetimes.WhatIsBuilt(licenceBeforeTheSwaps), Is.EqualTo(typeof(LicenseService)),
                    "the comparison reads the product's own licence, not the stand-in");
                Assert.That(licenceThisHostAnswersWith, Is.Not.InstanceOf<LicenseService>(),
                    "the host these scenarios run on answers with the stand-in, so the swap came after the reading");
                Assert.That(accessCheckBeforeTheSwaps, Is.Not.Null, "the host registers an access check");
                Assert.That(PortFixtureLifetimes.WhatIsBuilt(accessCheckBeforeTheSwaps!), Is.EqualTo(typeof(RbacAdministrationService)),
                    "the comparison reads the product's own access check, not the one the test sign-in puts in");
                Assert.That(accessCheckThisHostAnswersWith, Is.InstanceOf<ClaimsDrivenRbacAdministrationService>(),
                    "the host these scenarios run on answers with the test sign-in's access check");
            }
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

        private int GivenATeamFinishingWorkEveryDaySetTo(int samplingWindowDays)
        {
            var team = Seed.GivenATeam($"Team at {samplingWindowDays} days", samplingWindowDays);
            Seed.GivenTheTeamFinishedAWorkItemEveryDay(team);
            return team;
        }

        /// <summary>
        /// One window over-forecasts every time, so the windows that hold up are a proper subset and there is
        /// something an answer that ranked windows would have ranked.
        /// </summary>
        private void GivenTheWindowsBehaveDifferently(int samplingWindowDays)
        {
            var theOddOneOut = samplingWindowDays == 60 ? 90 : 60;
            Forecasts.EveryCheckOf(theOddOneOut, HeldUpTo.NoLevel);
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

        private static void ThenTheDenominatorCountsWhatWasEvaluated(RealityCheckAnswer answer)
        {
            var evaluable = answer.Cells.Count(cell => cell.IsSufficient);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.RunsAttempted, Is.EqualTo(answer.Cells.Count));
                Assert.That(answer.RunsEvaluated, Is.EqualTo(evaluable));
                Assert.That(answer.LevelsPerRun, Is.EqualTo(LevelsPerCheck));
                Assert.That(answer.ScoresEvaluated, Is.EqualTo(answer.RunsEvaluated * answer.LevelsPerRun),
                    "the scores the answer claims are the ones actually evaluated, not the ones attempted");
            }
        }

        private static void ThenEachLevelHeldExactlyWhenTheTeamDeliveredAtLeastItsForecast(RealityCheckAnswer answer)
        {
            var disagreements = new List<string>();

            foreach (var cell in answer.Cells.Where(cell => cell.IsSufficient))
            {
                if (cell.ForecastByLevel.Count != LevelsPerCheck || cell.ActualCompleted is null)
                {
                    disagreements.Add($"{cell.SamplingWindowDays}d/{cell.HorizonDays}d carries no complete forecast or no actual");
                    continue;
                }

                var byLevel = cell.ForecastByLevel;
                if (!(byLevel[95] <= byLevel[85] && byLevel[85] <= byLevel[70] && byLevel[70] <= byLevel[50]))
                {
                    disagreements.Add($"{cell.SamplingWindowDays}d/{cell.HorizonDays}d reads its levels out of order");
                }

                disagreements.AddRange(TheConfidenceLevels
                    .Where(level => cell.HeldByLevel.GetValueOrDefault(level) != (cell.ActualCompleted >= byLevel[level]))
                    .Select(level => $"{cell.SamplingWindowDays}d/{cell.HorizonDays}d at {level}%: forecast {byLevel[level]}, actual {cell.ActualCompleted}"));
            }

            Assert.That(disagreements, Is.Empty,
                "a level held exactly when the Team delivered at least what that level forecast");
        }

        private void ThenNothingAboutTheTeamChanged(int teamId, RealityCheckSeedSteps.TeamAsStored before)
        {
            Assert.That(Seed.WhatIsStoredFor(teamId), Is.EqualTo(before),
                "the check reads; it never writes a Team setting, a Work Item or anything else");
        }

        private static void ThenEveryMemberIsReadByItsName(RealityCheckAnswer answer, int teamId)
        {
            var evaluated = answer.Cells.Where(cell => cell.IsSufficient).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.TeamId, Is.EqualTo(teamId));
                Assert.That(answer.TeamName, Is.EqualTo("Ocean Explorer"));

                // Ocean Explorer is seeded with no forecast filter, so the controller reports none applied and
                // nothing left out.
                Assert.That(answer.FilterApplied, Is.False);
                Assert.That(answer.ExcludedSummary, Is.Null);

                Assert.That(answer.AnchorDate, Is.EqualTo(TodayDay));
                Assert.That(answer.ConfidenceLevels, Is.EqualTo(TheConfidenceLevels));
                Assert.That(answer.StandardWindowDays, Is.SubsetOf(answer.SampledWindowDays));
                Assert.That(answer.MinimumActiveDays, Is.Positive);
                Assert.That(answer.RunsAttempted, Is.EqualTo(answer.Cells.Count));
                Assert.That(answer.RunsEvaluated, Is.EqualTo(evaluated.Count));
                Assert.That(answer.LevelsPerRun, Is.EqualTo(LevelsPerCheck));
                Assert.That(answer.ScoresEvaluated, Is.EqualTo(evaluated.Count * LevelsPerCheck));
                Assert.That(answer.SoundWindowDays.Concat(answer.UnevaluatedWindowDays), Is.SubsetOf(answer.SampledWindowDays));
                Assert.That(answer.Determination, Is.Not.Empty);
                Assert.That(answer.CurrentSettingDays, Is.EqualTo(30));
                Assert.That(answer.CurrentSettingWasTested, Is.True);
                Assert.That(answer.CurrentSettingNotTestedReason, Is.Null);
                Assert.That(answer.CurrentSettingStanding, Is.Not.Empty);
                Assert.That(TheConfidenceLevels.Select(level => answer.Coverage(level).Reading), Has.None.Empty);
                Assert.That(answer.ScoredPeriods.Select(period => period.HorizonDays), Is.EqualTo(answer.SampledHorizonDays));

                Assert.That(evaluated, Has.Count.EqualTo(16), "this Team finishes work every day, so the shipped engine evaluates every check");
                Assert.That(evaluated.Where(cell => !cell.HeldByLevel.Keys.Order().SequenceEqual(TheConfidenceLevels)), Is.Empty,
                    "every evaluated check says for each level whether it held");
                Assert.That(evaluated.Where(cell => !ScoresTheForecastItShows(cell)), Is.Empty,
                    "each level's outcome names the forecast it was scored against, the one the check shows at that level");
            }
        }

        private static bool ScoresTheForecastItShows(CellReading cell)
            => cell.ForecastByLevel.Count == cell.ForecastValueByLevel.Count
               && cell.ForecastByLevel.All(level => cell.ForecastValueByLevel.TryGetValue(level.Key, out var scored) && scored == level.Value);

        private static void ThenEveryUnevaluatedCellStatesItsEmptyMembersAsNull(RealityCheckAnswer answer)
        {
            var unevaluated = Field(answer.Root, "cells").EnumerateArray()
                .Where(cell => !Field(Field(cell, "sufficiency"), "isSufficient").GetBoolean())
                .ToList();

            var notNull = unevaluated
                .SelectMany(cell => TheMembersAnUnevaluatedCellLeavesEmpty
                    .Where(name => Field(cell, name).ValueKind != JsonValueKind.Null)
                    .Select(name => $"{Text(Field(cell, "samplingWindowDays"))}d/{Text(Field(cell, "horizonDays"))}d {name}"))
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(unevaluated, Is.Not.Empty, "this Team's two-week windows are too thin, so some checks cannot be evaluated");
                Assert.That(notNull, Is.Empty,
                    "a member left out and a member stated as null read differently in the browser, and the contract states these as null");
            }
        }

        private static void ThenEveryDayIsAPlainDate(RealityCheckAnswer answer)
        {
            string[] daysOfACell = ["scoredPeriodStart", "scoredPeriodEnd", "historyWindowStart", "historyWindowEnd"];
            string[] daysOfAPeriod = ["scoredPeriodStart", "scoredPeriodEnd"];

            var days = new List<string> { Text(Field(answer.Root, "anchorDate")) };
            days.AddRange(Field(answer.Root, "cells").EnumerateArray().SelectMany(cell => daysOfACell.Select(name => Text(Field(cell, name)))));
            days.AddRange(Field(answer.Root, "scoredPeriods").EnumerateArray().SelectMany(period => daysOfAPeriod.Select(name => Text(Field(period, name)))));

            Assert.That(days, Has.All.Match(@"^\d{4}-\d{2}-\d{2}$"),
                "the browser reads every day as a calendar day, so a time or an offset on one would move it");
        }

        private static void ThenNothingInTheAnswerCarriesANumberPerWindow(RealityCheckAnswer answer)
        {
            var findings = new List<string>();
            InspectForAnythingThatCouldRankAWindow(answer.Root, "$", findings);

            Assert.That(findings, Is.Empty,
                "no part of the answer may carry a number per sampling window: with nothing to sort by, no ranking is representable");
        }

        private static void ThenTheAnswerCarriesFactsAndNoSentence(RealityCheckAnswer answer)
        {
            var credited = new List<string>();
            CollectTextMentioning(answer.Root, "Brown", credited);

            Assert.That(credited, Is.Empty,
                "the answer carries facts; any mention of the source method belongs beside a statement of where this check departs from it, which is copy the browser composes");
        }

        private static void ThenNoNegativeNumberReachesTheAnswer(RealityCheckAnswer answer)
        {
            var negatives = new List<string>();
            CollectNegativeNumbers(answer.Root, "$", negatives);

            Assert.That(negatives, Is.Empty,
                "the engine's 'no reading' marker is minus one, and it must never reach a count, a band or a cell");
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

        // --- Walking the answer ---

        private static void InspectForAnythingThatCouldRankAWindow(JsonElement element, string path, List<string> findings)
        {
            if (element.ValueKind == JsonValueKind.Array)
            {
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    InspectForAnythingThatCouldRankAWindow(item, $"{path}[{index++}]", findings);
                }

                return;
            }

            if (element.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            var names = element.EnumerateObject().Select(property => property.Name).ToList();

            if (names.Contains("samplingWindowDays") && !names.Contains("horizonDays"))
            {
                findings.Add($"{path} is about one sampling window rather than one check");
            }

            findings.AddRange(names
                .Where(name => int.TryParse(name, out _))
                .Select(name => $"{path}.{name} keys something by a number"));

            findings.AddRange(names
                .Where(name => WordsThatWouldNameAWinner.Any(word => name.Contains(word, StringComparison.OrdinalIgnoreCase)))
                .Select(name => $"{path}.{name}"));

            foreach (var property in element.EnumerateObject())
            {
                InspectForAnythingThatCouldRankAWindow(property.Value, $"{path}.{property.Name}", findings);
            }
        }

        private static void CollectTextMentioning(JsonElement element, string word, List<string> found)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String when (element.GetString() ?? string.Empty).Contains(word, StringComparison.OrdinalIgnoreCase):
                    found.Add(element.GetString()!);
                    break;
                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                    {
                        CollectTextMentioning(item, word, found);
                    }

                    break;
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        CollectTextMentioning(property.Value, word, found);
                    }

                    break;
            }
        }

        private static void CollectNegativeNumbers(JsonElement element, string path, List<string> found)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Number when element.GetDouble() < 0:
                    found.Add($"{path} = {element.GetRawText()}");
                    break;
                case JsonValueKind.Array:
                    var index = 0;
                    foreach (var item in element.EnumerateArray())
                    {
                        CollectNegativeNumbers(item, $"{path}[{index++}]", found);
                    }

                    break;
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        CollectNegativeNumbers(property.Value, $"{path}.{property.Name}", found);
                    }

                    break;
            }
        }
    }
}
