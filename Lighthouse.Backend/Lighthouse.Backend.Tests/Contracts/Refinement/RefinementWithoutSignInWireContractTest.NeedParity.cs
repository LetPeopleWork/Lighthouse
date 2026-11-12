using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Tests.API.Integration.Refinement;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Contracts.Refinement
{
    /// <summary>
    /// The need on the wire: that the range the Refinement tab sends is what the manual forecast route answers
    /// for the same working days, and the texts the browser reads when there is no number. How the need is
    /// worked out is asserted at the port. Also held here: the port fixture's stand-ins for the instance
    /// settings and for sign-in must not let a scenario pass on something no instance without sign-in has.
    /// </summary>
    public partial class RefinementWithoutSignInWireContractTest
    {
        private const int WorkingDaysOfAWeeklyCycle = 7;

        // A Team that finishes two Work Items every day is forecast fourteen over the seven working days of its
        // cycle, whatever the engine draws, so both reads of the shipped engine must agree exactly. The manual
        // forecast is asked for the day as many working days ahead as the cycle has. With so even a history
        // every likelihood reads fourteen: this proves the horizon, and the band's own tests prove which
        // likelihood each end is read at.
        [Test]
        public async Task The_range_uses_the_same_horizon_as_the_manual_forecast()
        {
            var gravity = GivenGravityFinishesTwoADayAndRefinesOnTuesdays();

            var tab = await WhenTheRefinementTabIsRead(gravity);

            await ThenTheRangeIsTheManualForecastForTheSevenWorkingDaysOfTheNextCycle(tab, gravity);
        }

        // The browser and the clients choose what to say from the reason's text alone; sent as its number, or
        // under another name, it would read as an unknown reason.
        [Test]
        public async Task A_need_without_a_number_names_its_reason_as_text()
        {
            var (littleHistory, noRefinementStates) = GivenOneTeamWithTooLittleHistoryAndOneWithoutRefinementStates();

            var (littleHistorysNeed, noRefinementStatesNeed) = await WhenTheNeedOfBothIsRead(littleHistory, noRefinementStates);

            ThenEachNeedNamesItsReasonAsText(littleHistorysNeed, noRefinementStatesNeed);
        }

        // --- What the port fixture stands in for ---

        // A port scenario answers the instance settings from a stand-in. A setting no scenario was given must
        // fail the scenario that reads it, naming the setting, instead of handing back an empty default that no
        // seeded database holds.
        [Test]
        public void A_setting_no_port_scenario_was_given_fails_loud_naming_the_setting()
        {
            using var settings = GivenThePortFixturesSettingsStandIn();

            var failure = WhenASettingNoScenarioWasGivenIsRead(settings.StandIn);

            ThenTheReadFailsNaming(failure, nameof(IAppSettingService.GetSurveyNudgeNextEligibleAt));
        }

        // A port scenario runs with sign-in as its fixture says. That must be what an instance without sign-in
        // answers, or a scenario could pass on a sign-in mode no such instance has.
        [Test]
        public void A_port_scenario_runs_with_sign_in_as_an_instance_without_sign_in_has_it()
        {
            var portAnswer = GivenWhatThePortFixtureAnswersForSignIn();

            var hostAnswer = WhenThisInstanceWithoutSignInIsAskedWhetherSignInIsOn();

            ThenTheSignInAnswersAgree(portAnswer, hostAnswer);
        }

        // The comparison above must be able to fail: a fixture that said sign-in is on is caught by it.
        [Test]
        public void A_port_fixture_that_says_sign_in_is_on_is_caught_by_the_sign_in_comparison()
        {
            var plantedAnswer = GivenAPortFixtureThatSaysSignInIsOn();

            var hostAnswer = WhenThisInstanceWithoutSignInIsAskedWhetherSignInIsOn();

            ThenTheSignInComparisonFindsTheyDiffer(plantedAnswer, hostAnswer);
        }

        // --- Given ---

        /// <summary>Gravity finished two Work Items on each day of its Throughput window, today included, and refines on Tuesdays.</summary>
        private int GivenGravityFinishesTwoADayAndRefinesOnTuesdays()
        {
            var givens = new RefinementPortGivens(Services);
            var gravity = givens.GravityRefinesSixWorkItems();
            givens.TheTeamFinished(gravity, FinishedEveryDay(perDay: 2, days: RefinementPortGivens.ThroughputHistoryDays));
            givens.TheTeamRefines(gravity, RefinementPortGivens.RefinesOnTuesdays);
            return gravity;
        }

        /// <summary>
        /// Gravity refines on Thursdays but finished work on only four days, one short of what a forecast needs;
        /// Team Zenith has chosen no state to refine in.
        /// </summary>
        private (int LittleHistory, int NoRefinementStates) GivenOneTeamWithTooLittleHistoryAndOneWithoutRefinementStates()
        {
            var givens = new RefinementPortGivens(Services);
            var gravity = givens.GravityRefinesSixWorkItems();
            givens.TheTeamFinished(gravity, FinishedEveryDay(perDay: 3, days: 4));
            givens.TheTeamRefines(gravity, RefinementPortGivens.RefinesOnThursdays);

            var zenith = givens.ATeam("Team Zenith", new TeamStates([RefinementPortGivens.Backlog], [RefinementPortGivens.Implementation], [RefinementPortGivens.Done]),
                RefinementPortGivens.ThroughputHistoryDays);
            return (gravity, zenith);
        }

        /// <summary>The settings stand-in a port scenario is given, exactly as the port fixture registers it.</summary>
        private static PortSettings GivenThePortFixturesSettingsStandIn()
        {
            var services = RefinementAtThePortTest.WhatItRegisters.BuildServiceProvider();
            var scope = services.CreateScope();
            return new PortSettings(services, scope, scope.ServiceProvider.GetRequiredService<IAppSettingService>());
        }

        private static RuntimeAuthStatus GivenWhatThePortFixtureAnswersForSignIn()
        {
            using var services = RefinementAtThePortTest.WhatItRegisters.BuildServiceProvider();
            using var scope = services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IAuthModeResolver>().Resolve();
        }

        private static RuntimeAuthStatus GivenAPortFixtureThatSaysSignInIsOn() => new() { Mode = AuthMode.Enabled };

        private static FinishedWorkItem[] FinishedEveryDay(int perDay, int days)
            => [.. Enumerable.Range(0, days).SelectMany(daysAgo => Enumerable.Range(0, perDay)
                .Select(index => new FinishedWorkItem($"DONE-{daysAgo}-{index}", CycleTimeDays: 1, FinishedDaysAgo: daysAgo)))];

        // --- When ---

        private async Task<(JsonElement First, JsonElement Second)> WhenTheNeedOfBothIsRead(int first, int second)
            => (await TheNeedOnTheRefinementTabOf(first), await TheNeedOnTheRefinementTabOf(second));

        private static MockException? WhenASettingNoScenarioWasGivenIsRead(IAppSettingService standIn)
        {
            try
            {
                standIn.GetSurveyNudgeNextEligibleAt();
                return null;
            }
            catch (MockException failure)
            {
                return failure;
            }
        }

        private RuntimeAuthStatus WhenThisInstanceWithoutSignInIsAskedWhetherSignInIsOn()
        {
            using var scope = NewScope();
            return scope.ServiceProvider.GetRequiredService<IAuthModeResolver>().Resolve();
        }

        // --- Then ---

        /// <summary>
        /// The cycle runs from the next Tuesday, or today on a Tuesday, to the Tuesday after it: seven working days.
        /// The manual forecast for seven working days from today targets the day a week from today.
        /// </summary>
        private async Task ThenTheRangeIsTheManualForecastForTheSevenWorkingDaysOfTheNextCycle(JsonElement tab, int teamId)
        {
            var today = TheInstancesToday();
            var cycleStart = today.AddDays(((int)DayOfWeek.Tuesday - (int)today.DayOfWeek + 7) % 7);
            var need = tab.GetProperty("need");
            var manual = await TheManualForecastFor(teamId, today.AddDays(WorkingDaysOfAWeeklyCycle));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(NumberIn(need, "low"), Is.EqualTo(HowManyAt(manual, 50)),
                    "the low end of the tab's range must be what the manual forecast route answers at its median for the same working days");
                Assert.That((NumberIn(need, "low"), NumberIn(need, "high"), NumberIn(need, "horizonWorkingDays")), Is.EqualTo(((int?)14, (int?)14, (int?)WorkingDaysOfAWeeklyCycle)),
                    "two a day over the seven working days of the cycle, read at both ends");
                Assert.That((TextIn(need, "cycleStart"), TextIn(need, "cycleEnd")), Is.EqualTo(((string?)DayText(cycleStart), (string?)DayText(cycleStart.AddDays(7)))),
                    "the cycle runs from the next Tuesday to the Tuesday after it, sent as days");
            }
        }

        private static void ThenEachNeedNamesItsReasonAsText(JsonElement littleHistorysNeed, JsonElement noRefinementStatesNeed)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(KindOf(littleHistorysNeed, "unavailableReason"), Is.EqualTo(JsonValueKind.String), "the reason is sent as its name, not its number");
                Assert.That(TextIn(littleHistorysNeed, "unavailableReason"), Is.EqualTo("InsufficientData"),
                    "a Team with work finished on fewer than five days gets the guard forecasts use, under the text the browser reads");
                Assert.That(TextIn(noRefinementStatesNeed, "unavailableReason"), Is.EqualTo("NoRefinementStates"),
                    "a Team that chose no state to refine in has no number, under the text the browser reads");
            }
        }

        private static void ThenTheReadFailsNaming(MockException? failure, string setting)
            => Assert.That(failure?.Message ?? "no failure: the stand-in answered with a default", Does.Contain(setting),
                $"the port fixture's settings stand-in answered {setting}, which no scenario gave it, with a default instead of failing; "
                + "make the stand-in strict, so that a setting a scenario reads but nobody stubbed fails loud and names the setting");

        private static void ThenTheSignInAnswersAgree(RuntimeAuthStatus portAnswer, RuntimeAuthStatus hostAnswer)
            => Assert.That(SignInAnswersAgree(portAnswer, hostAnswer), Is.True,
                $"the port fixture answers sign-in as {portAnswer}, but an instance without sign-in answers {hostAnswer}; "
                + "the port fixture's sign-in stand-in must answer what such an instance answers");

        private static void ThenTheSignInComparisonFindsTheyDiffer(RuntimeAuthStatus plantedAnswer, RuntimeAuthStatus hostAnswer)
            => Assert.That(SignInAnswersAgree(plantedAnswer, hostAnswer), Is.False,
                $"a port fixture answering {plantedAnswer} passed the comparison with an instance answering {hostAnswer}, so the comparison cannot fail");

        /// <summary>Both the mode and any misconfiguration message must be the same.</summary>
        private static bool SignInAnswersAgree(RuntimeAuthStatus portAnswer, RuntimeAuthStatus hostAnswer) => portAnswer == hostAnswer;

        private DateOnly TheInstancesToday()
        {
            using var scope = NewScope();
            return scope.ServiceProvider.GetRequiredService<ILighthouseClock>().Today;
        }

        /// <summary>The Team's manual How Many forecast for this target date, as the Forecasts tab asks for it.</summary>
        private async Task<JsonElement> TheManualForecastFor(int teamId, DateOnly targetDate)
        {
            var body = new JsonObject { ["targetDate"] = DayText(targetDate) + "T00:00:00Z" };

            using var response = await Client.PostAsync($"/api/latest/forecast/manual/{teamId}",
                new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"));
            var answer = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"The manual forecast answered {(int)response.StatusCode}: {answer}");

            using var document = JsonDocument.Parse(answer);
            return document.RootElement.Clone();
        }

        private static int? HowManyAt(JsonElement manualForecast, int probability)
            => manualForecast.TryGetProperty("howManyForecasts", out var levels) && levels.ValueKind == JsonValueKind.Array
                ? levels.EnumerateArray().Where(level => NumberIn(level, "probability") == probability).Select(level => NumberIn(level, "value")).FirstOrDefault()
                : null;

        private static string DayText(DateOnly day) => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        /// <summary>The port fixture's own services, kept alive while its settings stand-in is read.</summary>
        private sealed class PortSettings(ServiceProvider services, IServiceScope scope, IAppSettingService standIn) : IDisposable
        {
            public IAppSettingService StandIn { get; } = standIn;

            public void Dispose()
            {
                scope.Dispose();
                services.Dispose();
            }
        }
    }
}
