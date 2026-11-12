using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.API.Integration.Refinement;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Contracts.Refinement
{
    /// <summary>
    /// The need on the wire: that the range the Refinement tab sends is what the manual forecast route answers
    /// for the same working days, and the texts the browser reads when there is no number. How the need is
    /// worked out is asserted at the port.
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

        private static FinishedWorkItem[] FinishedEveryDay(int perDay, int days)
            => [.. Enumerable.Range(0, days).SelectMany(daysAgo => Enumerable.Range(0, perDay)
                .Select(index => new FinishedWorkItem($"DONE-{daysAgo}-{index}", CycleTimeDays: 1, FinishedDaysAgo: daysAgo)))];

        // --- When ---

        private async Task<(JsonElement First, JsonElement Second)> WhenTheNeedOfBothIsRead(int first, int second)
            => (await TheNeedOnTheRefinementTabOf(first), await TheNeedOnTheRefinementTabOf(second));

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
    }
}
