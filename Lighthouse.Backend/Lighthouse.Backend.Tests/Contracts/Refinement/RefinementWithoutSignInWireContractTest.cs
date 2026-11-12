using System.Collections;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Tests.API.Integration.Refinement;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Contracts.Refinement
{
    /// <summary>
    /// What a browser or a client sees on the wire when it reads the Refinement tab of an instance without
    /// sign-in. What the tab works out is asserted at the port; this class holds only the names and shapes the
    /// callers read.
    /// </summary>
    public partial class RefinementWithoutSignInWireContractTest : WireContractTest
    {
        [SetUp]
        public void StartFromTheSeededDatabase()
        {
            using var scope = NewScope();
            foreach (var seeder in scope.ServiceProvider.GetServices<ISeeder>())
            {
                seeder.Seed().GetAwaiter().GetResult();
            }
        }

        // The browser and the clients read both ends of the cycle and expect null when there is none; a member
        // left out of the answer would read as missing, not as "no cycle".
        [Test]
        public async Task A_Team_with_no_number_states_its_need_as_nulls_and_names_no_date()
        {
            var givens = new RefinementPortGivens(Services);
            var teamId = givens.ATeam("Team Gravity", RefinementPortGivens.GravitysStates, throughputHistoryDays: 30);
            givens.TheTeamRefines(teamId, RefinementPortGivens.RefinesInBacklogAnalysingAndNext, RefinementPortGivens.ReadyWhenTaggedReady);

            var need = await TheNeedOnTheRefinementTabOf(teamId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(need.GetProperty("unavailableReason").GetString(), Is.EqualTo("NoCadence"), "a Team that has set no cadence has no number");
                Assert.That(KindOf(need, "cycleStart"), Is.EqualTo(JsonValueKind.Null));
                Assert.That(KindOf(need, "cycleEnd"), Is.EqualTo(JsonValueKind.Null));
            }
        }

        private static IEnumerable<TestCaseData> EveryInputOfThePortBuilder()
            => RefinementPortGivens.EveryInput.Select(input => new TestCaseData(input).SetArgDisplayNames(input.ToString()));

        // A port scenario writes a Team's set-up straight onto the Team, past the save that tidies what the settings
        // form sends. Each input the port may write is sent through that save here, as the form sends it for a Team
        // that has not yet said how it refines, and both Teams must end up storing the same SLE, states, Throughput
        // history and way of refining.
        [TestCaseSource(nameof(EveryInputOfThePortBuilder))]
        public async Task A_Team_set_up_through_its_settings_holds_what_a_port_scenario_sets_up(RefinementInput input)
        {
            var givens = new RefinementPortGivens(Services);
            var setUpAtThePort = givens.ATeam("Team set up at the port", RefinementPortGivens.GravitysStates, throughputHistoryDays: 30, sleProbability: 85, sleRange: 7);
            givens.TheTeamRefines(setUpAtThePort, input);
            var savedThroughItsSettings = givens.ATeam("Team saved through its settings", RefinementPortGivens.GravitysStates, throughputHistoryDays: 30, sleProbability: 85, sleRange: 7);

            await TheAdminSavesTheSettingsFormWith(savedThroughItsSettings, input);

            Assert.That(WhatTheTeamStores(savedThroughItsSettings), Is.EqualTo(WhatTheTeamStores(setUpAtThePort)),
                $"the settings save stores \"{input}\" otherwise than a port scenario writes it");
        }

        private static IEnumerable<TestCaseData> PortFixturesStandingInForTheSettings()
        {
            yield return new TestCaseData(nameof(RefinementAtThePortTest), (Func<IServiceCollection>)(() => RefinementAtThePortTest.WhatItRegisters))
                .SetArgDisplayNames(nameof(RefinementAtThePortTest));
        }

        // A port fixture seeds nothing and answers the instance settings from a stand-in, so a scenario could pass on
        // a value no database holds. Every setting the stand-in answers must be what a newly seeded database says.
        [TestCaseSource(nameof(PortFixturesStandingInForTheSettings))]
        public void Every_setting_a_port_scenario_is_given_is_what_a_seeded_database_holds(string fixtureName, Func<IServiceCollection> whatItRegisters)
        {
            using var portServices = whatItRegisters().BuildServiceProvider();
            using var portScope = portServices.CreateScope();
            var standIn = portScope.ServiceProvider.GetRequiredService<IAppSettingService>();
            using var hostScope = NewScope();
            var seeded = hostScope.ServiceProvider.GetRequiredService<IAppSettingService>();

            var settingsAnswered = Mock.Get(standIn).Setups.Select(setup => setup.Expression).ToList();
            Assert.That(settingsAnswered, Is.Not.Empty, $"{fixtureName} answers no setting, so there is nothing to compare");

            using (Assert.EnterMultipleScope())
            {
                foreach (var setting in settingsAnswered)
                {
                    var read = setting.Compile();
                    Assert.That(JsonSerializer.Serialize(read.DynamicInvoke(standIn)), Is.EqualTo(JsonSerializer.Serialize(read.DynamicInvoke(seeded))),
                        $"{fixtureName} answers {setting} otherwise than a seeded database");
                }
            }
        }

        private async Task TheAdminSavesTheSettingsFormWith(int teamId, RefinementInput input)
        {
            var howItRefines = new RefinementSettings();
            input.ApplyTo(howItRefines);

            TeamSettingDto form;
            using (var scope = NewScope())
            {
                var team = scope.ServiceProvider.GetRequiredService<IRepository<Team>>().GetById(teamId)
                    ?? throw new InvalidOperationException($"no Team {teamId} is stored to save settings for");
                var today = scope.ServiceProvider.GetRequiredService<ILighthouseClock>().Today;
                form = new TeamSettingDto(team, today) { Refinement = new RefinementSettingsDto(howItRefines) };
            }

            using var save = await Client.PutAsJsonAsync($"/api/latest/teams/{teamId}", form);
            Assert.That(save.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"the settings save was refused: {await save.Content.ReadAsStringAsync()}");
        }

        /// <summary>What the Team stores of the set-up a port scenario gives it: its SLE, its states, its Throughput history and how it refines.</summary>
        private string WhatTheTeamStores(int teamId)
        {
            using var scope = NewScope();
            var team = scope.ServiceProvider.GetRequiredService<IRepository<Team>>().GetById(teamId)
                ?? throw new InvalidOperationException($"no Team {teamId} is stored");

            return PublicPropertiesOf(new
            {
                team.ServiceLevelExpectationProbability,
                team.ServiceLevelExpectationRange,
                team.ToDoStates,
                team.DoingStates,
                team.DoneStates,
                team.ThroughputHistory,
                team.RefinementSettings,
            });
        }

        // Every public property is listed, also those the JSON writer is told to skip, so a member it would leave out
        // still has to agree.
        private static string PublicPropertiesOf(object? value, string path = "")
            => value switch
            {
                null => $"{path} = null\n",
                string or ValueType => $"{path} = {Convert.ToString(value, CultureInfo.InvariantCulture)}\n",
                IEnumerable items => string.Concat(items.Cast<object?>().Select((item, index) => PublicPropertiesOf(item, $"{path}[{index}]"))),
                _ => string.Concat(value.GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(property => property.GetIndexParameters().Length == 0)
                    .OrderBy(property => property.Name, StringComparer.Ordinal)
                    .Select(property => PublicPropertiesOf(property.GetValue(value), $"{path}.{property.Name}"))),
            };

        private async Task<JsonElement> TheNeedOnTheRefinementTabOf(int teamId)
        {
            using var response = await Client.GetAsync($"/api/latest/teams/{teamId}/refinement");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return body.RootElement.GetProperty("need").Clone();
        }

        private static JsonValueKind KindOf(JsonElement owner, string member)
            => owner.TryGetProperty(member, out var value) ? value.ValueKind : JsonValueKind.Undefined;
    }
}
