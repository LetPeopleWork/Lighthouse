using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.API.Integration.Refinement;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Contracts.Refinement
{
    /// <summary>
    /// The yardstick on the wire: what the votes are cast against, as the browser reads it, and that an SLE saved
    /// through the Team settings is what the next read of the tab answers. Which yardstick a Team gets is asserted
    /// at the port; these rows hold the settings save as the action and the names and texts the browser reads.
    /// </summary>
    public partial class RefinementWithoutSignInWireContractTest
    {
        private const string YardstickFromTheSle = "Sle";

        private const string YardstickFromTheCycleTimeFallback = "CycleTimeFallback";

        private const string NoYardstick = "Unavailable";

        [Test]
        public async Task Setting_an_SLE_replaces_the_fallback_on_the_next_read()
        {
            var meridian = GivenMeridianWithItsCycleTimes85thPercentileOfTwelveDaysExpects(probability: 0, days: 0);
            var before = await WhenTheRefinementTabIsRead(meridian);
            ThenTheYardstickOnTheWireIs(before, YardstickFromTheCycleTimeFallback, days: 12, probability: 85);

            await WhenTheAdminSavesTheSle(meridian, probability: 75, days: 10);
            var after = await WhenTheRefinementTabIsRead(meridian);

            ThenTheYardstickOnTheWireIs(after, YardstickFromTheSle, days: 10, probability: 75);
            ThenTheSameWorkItemsAreListed(before, after);
        }

        [Test]
        public async Task Clearing_the_SLE_brings_the_fallback_back_on_the_next_read()
        {
            var meridian = GivenMeridianWithItsCycleTimes85thPercentileOfTwelveDaysExpects(probability: 75, days: 10);
            var before = await WhenTheRefinementTabIsRead(meridian);
            ThenTheYardstickOnTheWireIs(before, YardstickFromTheSle, days: 10, probability: 75);

            await WhenTheAdminSavesTheSle(meridian, probability: 0, days: 0);
            var after = await WhenTheRefinementTabIsRead(meridian);

            ThenTheYardstickOnTheWireIs(after, YardstickFromTheCycleTimeFallback, days: 12, probability: 85);
            ThenTheSameWorkItemsAreListed(before, after);
        }

        // The browser writes "no number" from the source text alone, and reads both numbers as absent only when
        // they are sent as null; a member left out would read as missing, not as "no number".
        [Test]
        public async Task A_yardstick_without_a_number_is_sent_as_Unavailable_with_null_days_and_probability()
        {
            var equinox = new RefinementPortGivens(Services).GravityRefinesFiveWorkItems("Team Equinox", sleProbability: 0, sleRange: 0);

            var tab = await WhenTheRefinementTabIsRead(equinox);

            var yardstick = TheYardstickMemberOf(tab);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(KindOf(yardstick, "source"), Is.EqualTo(JsonValueKind.String), "the source is sent as its name, not its number");
                Assert.That(yardstick.GetProperty("source").GetString(), Is.EqualTo(NoYardstick),
                    "a Team with no SLE and nothing finished has no yardstick, and the browser says so only for this text");
                Assert.That(KindOf(yardstick, "days"), Is.EqualTo(JsonValueKind.Null), "without a number the days are sent as null");
                Assert.That(KindOf(yardstick, "probability"), Is.EqualTo(JsonValueKind.Null), "without a number the probability is sent as null");
            }
        }

        // --- Given ---

        /// <summary>
        /// Team Meridian refines five Work Items, stores this SLE, and finished twenty Work Items over the last twenty
        /// days whose cycle times have an 85th percentile of 12 days.
        /// </summary>
        private int GivenMeridianWithItsCycleTimes85thPercentileOfTwelveDaysExpects(int probability, int days)
        {
            var givens = new RefinementPortGivens(Services);
            var meridian = givens.GravityRefinesFiveWorkItems("Team Meridian", probability, days);
            givens.TheTeamFinished(meridian, Slice10SleYardstickTest.MeridiansRecentlyFinishedWorkItems);
            return meridian;
        }

        // --- When ---

        private async Task<JsonElement> WhenTheRefinementTabIsRead(int teamId)
        {
            using var response = await Client.GetAsync($"/api/latest/teams/{teamId}/refinement");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"the Refinement tab could not be read: {await response.Content.ReadAsStringAsync()}");

            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return body.RootElement.Clone();
        }

        /// <summary>The Team settings form, as the Team stores it, saved with only the SLE changed.</summary>
        private async Task WhenTheAdminSavesTheSle(int teamId, int probability, int days)
        {
            TeamSettingDto form;
            using (var scope = NewScope())
            {
                var team = scope.ServiceProvider.GetRequiredService<IRepository<Team>>().GetById(teamId)
                    ?? throw new InvalidOperationException($"no Team {teamId} is stored to save settings for");
                var today = scope.ServiceProvider.GetRequiredService<ILighthouseClock>().Today;
                form = new TeamSettingDto(team, today) { ServiceLevelExpectationProbability = probability, ServiceLevelExpectationRange = days };
            }

            using var save = await Client.PutAsJsonAsync($"/api/latest/teams/{teamId}", form);
            Assert.That(save.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"The admin could not set the SLE. {await save.Content.ReadAsStringAsync()}");
        }

        // --- Then ---

        private static void ThenTheYardstickOnTheWireIs(JsonElement tab, string source, int days, int probability)
        {
            var yardstick = TheYardstickMemberOf(tab);
            Assert.That((TextIn(yardstick, "source"), NumberIn(yardstick, "days"), NumberIn(yardstick, "probability")),
                Is.EqualTo(((string?)source, (int?)days, (int?)probability)),
                "the next read after a settings save must answer the yardstick the save left the Team with, as its source text and two numbers");
        }

        private static void ThenTheSameWorkItemsAreListed(JsonElement before, JsonElement after)
            => Assert.That(RowsOn(after), Is.Not.Empty.And.EqualTo(RowsOn(before)),
                "saving the SLE changes what the votes are cast against, never which Work Items are in refinement");

        private static JsonElement TheYardstickMemberOf(JsonElement tab)
        {
            Assert.That(tab.TryGetProperty("yardstick", out var yardstick) && yardstick.ValueKind == JsonValueKind.Object, Is.True,
                $"The Refinement tab's answer does not say what the votes are cast against. Body: {tab}");
            return yardstick;
        }

        private static List<(string? ReferenceId, string? Name, string? Url, string? State, string? ParentReferenceId)> RowsOn(JsonElement tab)
        {
            Assert.That(tab.TryGetProperty("workItems", out var rows) && rows.ValueKind == JsonValueKind.Array, Is.True,
                $"The Refinement tab's answer carries no list of Work Items. Body: {tab}");

            return [.. rows.EnumerateArray().Select(row => (
                TextIn(row, "referenceId"),
                TextIn(row, "name"),
                TextIn(row, "url"),
                TextIn(row, "state"),
                TextIn(row, "parentReferenceId")))];
        }

        private static string? TextIn(JsonElement owner, string member)
            => owner.TryGetProperty(member, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

        private static int? NumberIn(JsonElement owner, string member)
            => owner.TryGetProperty(member, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetInt32() : null;
    }
}
