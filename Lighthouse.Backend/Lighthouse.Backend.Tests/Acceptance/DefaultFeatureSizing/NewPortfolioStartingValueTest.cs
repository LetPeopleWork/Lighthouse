using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.TrackerIntegration.Contracts;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Lighthouse.Backend.Modules.Access.Application;
using Lighthouse.Backend.Modules.Access.Contracts;
using Lighthouse.Backend.Modules.OrganisationSetup.Application;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Lighthouse.Backend.Tests.TestDoubles;

namespace Lighthouse.Backend.Tests.Acceptance.DefaultFeatureSizing
{
    /// <summary>
    /// What default Feature sizing holds for a Portfolio once it is stored and read back, on the real SQLite
    /// database. The in-memory provider ignores column defaults, so it cannot tell a Portfolio saved on from one
    /// saved off; only the real mapping can. A new Portfolio starts off, naming the switch keeps what is named, a
    /// clone keeps the value of the Portfolio it copies, and a Portfolio that is already on stays on.
    /// </summary>
    [TestFixture]
    public class NewPortfolioStartingValueTest
    {
        public enum WhatTheCreateSays
        {
            NothingAboutTheSwitch,
            SwitchOn,
            SwitchOff,
        }

        public enum WhatTheConnectionHolds
        {
            NoSizeFieldDefault,
            ASizeFieldDefault,
        }

        private ServicePortHarness harness = null!;

        [SetUp]
        public void OpenAnEmptyDatabase() => harness = new ServicePortHarness();

        [TearDown]
        public void CloseTheDatabase() => harness.Dispose();

        [Test]
        [TestCase(WhatTheConnectionHolds.NoSizeFieldDefault, WhatTheCreateSays.NothingAboutTheSwitch, false)]
        [TestCase(WhatTheConnectionHolds.NoSizeFieldDefault, WhatTheCreateSays.SwitchOn, true)]
        [TestCase(WhatTheConnectionHolds.NoSizeFieldDefault, WhatTheCreateSays.SwitchOff, false)]
        [TestCase(WhatTheConnectionHolds.ASizeFieldDefault, WhatTheCreateSays.NothingAboutTheSwitch, true)]
        [TestCase(WhatTheConnectionHolds.ASizeFieldDefault, WhatTheCreateSays.SwitchOn, true)]
        [TestCase(WhatTheConnectionHolds.ASizeFieldDefault, WhatTheCreateSays.SwitchOff, false)]
        public async Task A_new_Portfolio_starts_with_default_sizing_off_unless_its_connection_names_a_size_field_default_or_its_create_names_the_switch(WhatTheConnectionHolds connection, WhatTheCreateSays create, bool storedOn)
        {
            var library = new Library(harness).OnAConnectionHolding(connection);

            var id = await library.ACreateIsSaved(create);

            library.TheStoredSwitchOf(id, Is.EqualTo(storedOn),
                $"A Portfolio created {Describe(create)} on a connection with {(connection == WhatTheConnectionHolds.ASizeFieldDefault ? "a" : "no")} Portfolio Size Estimate Field default should be stored with default Feature sizing {(storedOn ? "on" : "off")}, because a new Portfolio counts Features without child Work Items as 0 unless its owner says otherwise or its connection names a size field to read. Start a new Portfolio on only when the connection names that default, and keep a named value.");
        }

        [Test]
        public async Task A_Portfolio_created_before_the_connection_names_a_size_field_default_stays_off()
        {
            var library = new Library(harness);
            var existing = await library.ACreateIsSaved(WhatTheCreateSays.NothingAboutTheSwitch);

            library.TheAdminNamesTheSizeFieldDefaultOnTheConnection();

            library.TheStoredSwitchOf(existing, Is.False,
                "A Portfolio created before its connection named a size field default should still be off, because only a new Portfolio starts on through the connection and a stored value never moves. Run the connection rule at create only.");
        }

        [Test]
        [TestCase(WhatTheCreateSays.SwitchOn, true)]
        [TestCase(WhatTheCreateSays.SwitchOff, false)]
        public async Task A_cloned_Portfolio_keeps_the_default_sizing_of_the_Portfolio_it_copies(WhatTheCreateSays sourceHolds, bool cloneHolds)
        {
            var library = new Library(harness);
            var source = await library.ACreateIsSaved(sourceHolds);

            var clone = await library.TheStoredSettingsOfAreSavedAsANewPortfolio(source);

            library.TheStoredSwitchOf(clone, Is.EqualTo(cloneHolds),
                $"A clone of a Portfolio with default Feature sizing {(cloneHolds ? "on" : "off")} should have it {(cloneHolds ? "on" : "off")}, because the form sends the copied value. Store the value the clone names.");
        }

        [Test]
        public async Task A_Portfolio_that_is_already_on_stays_on_when_an_older_client_saves_it_without_naming_the_switch()
        {
            var library = new Library(harness);
            var existing = await library.ACreateIsSaved(WhatTheCreateSays.SwitchOn);

            await library.AnOlderClientSavesTheSettingsOf(existing);

            library.TheStoredSwitchOf(existing, Is.True,
                "A Portfolio that held default Feature sizing on should still hold it after a save that does not name the switch, because only an explicit value may move it and forecasts must not move on an upgrade. Keep the stored value.");
        }

        private static string Describe(WhatTheCreateSays create) => create switch
        {
            WhatTheCreateSays.NothingAboutTheSwitch => "without naming the switch",
            WhatTheCreateSays.SwitchOn => "naming the switch on",
            WhatTheCreateSays.SwitchOff => "naming the switch off",
            _ => throw new ArgumentOutOfRangeException(nameof(create), create, null),
        };

        /// <summary>Saves and reads Portfolios the way the create route and the settings route do, on the harness's database.</summary>
        private sealed class Library(ServicePortHarness harness)
        {
            private const string WhatAnOlderClientSends = """
                {
                  "id": 0,
                  "name": "Ocean",
                  "workItemTypes": ["Feature"],
                  "toDoStates": ["New"],
                  "doingStates": ["Active"],
                  "doneStates": ["Done"],
                  "overrideRealChildCountStates": [],
                  "dataRetrievalValue": "type = Feature",
                  "usePercentileToCalculateDefaultAmountOfWorkItems": false,
                  "defaultWorkItemPercentile": 85,
                  "percentileHistoryInDays": 90,
                  "defaultAmountOfWorkItemsPerFeature": 25,
                  "owningTeam": null,
                  "serviceLevelExpectationProbability": 80,
                  "serviceLevelExpectationRange": 25,
                  "systemWIPLimit": 2,
                  "stalenessThresholdDays": 0,
                  "blockedStalenessThresholdDays": 0,
                  "doneItemsCutoffDays": 365,
                  "stateMappings": [],
                  "cycleTimeDefinitions": [],
                  "waitStates": [],
                  "estimationCategoryValues": [],
                  "involvedTeams": []
                }
                """;

            private static readonly JsonSerializerOptions AsTheRouteReadsIt = new() { PropertyNameCaseInsensitive = true };
            private static readonly JsonSerializerOptions AsTheFormReadsIt = new(JsonSerializerDefaults.Web);

            private int? connectionId;
            private WhatTheConnectionHolds connectionHolds = WhatTheConnectionHolds.NoSizeFieldDefault;

            public Library OnAConnectionHolding(WhatTheConnectionHolds holds)
            {
                connectionHolds = holds;
                return this;
            }

            public void TheAdminNamesTheSizeFieldDefaultOnTheConnection()
            {
                using var scope = harness.NewScope();
                var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                NameTheSizeFieldDefault(context, context.WorkTrackingSystemConnections.Single(connection => connection.Id == connectionId));
            }

            public async Task<int> ACreateIsSaved(WhatTheCreateSays create)
            {
                var payload = JsonNode.Parse(WhatAnOlderClientSends)!.AsObject();
                if (create != WhatTheCreateSays.NothingAboutTheSwitch)
                {
                    payload["infersDefaultFeatureSize"] = create == WhatTheCreateSays.SwitchOn;
                }

                return await Create(payload);
            }

            public async Task<int> TheStoredSettingsOfAreSavedAsANewPortfolio(int source)
            {
                var payload = JsonSerializer.SerializeToNode(StoredSettingsOf(source), AsTheFormReadsIt)!.AsObject();
                payload["id"] = 0;
                payload["name"] = "Ocean copy";

                return await Create(payload);
            }

            public async Task AnOlderClientSavesTheSettingsOf(int id)
            {
                using var scope = harness.NewScope();
                var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                var repository = new PortfolioRepository(context, NullLogger<PortfolioRepository>.Instance);
                var portfolio = repository.GetById(id)!;
                var payload = JsonNode.Parse(WhatAnOlderClientSends)!.AsObject();
                payload["id"] = id;
                payload["workTrackingSystemConnectionId"] = portfolio.WorkTrackingSystemConnectionId;

                portfolio.SyncWithPortfolioSettings(Read(payload), Mock.Of<IRepository<Team>>());
                await repository.Save();
            }

            public void TheStoredSwitchOf(int id, NUnit.Framework.Constraints.IResolveConstraint expectation, string because)
            {
                Assert.That(StoredSettingsOf(id).InfersDefaultFeatureSize, expectation, because);
            }

            private async Task<int> Create(JsonObject payload)
            {
                using var scope = harness.NewScope();
                var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                payload["workTrackingSystemConnectionId"] = ConnectionIn(context);
                var connections = new WorkTrackingSystemConnectionRepository(context, NullLogger<WorkTrackingSystemConnectionRepository>.Instance);
                var controller = new PortfoliosController(
                    new PortfolioRepository(context, NullLogger<PortfolioRepository>.Instance),
                    new TeamRepository(context, NullLogger<TeamRepository>.Instance),
                    Mock.Of<IPortfolioUpdater>(),
                    Mock.Of<IWorkTrackingConnectorFactory>(),
                    connections,
                    Mock.Of<IRbacAdministrationService>(),
                    FeatureOrderingTestHelper.FollowingTheTracker(),
                    new FakeLighthouseClock(TestToday.FrozenNow),
                    new OwnerSettingRefusals(connections, Mock.Of<ITerminologyService>()));

                var response = await controller.CreatePortfolio(Read(payload));

                var created = (response.Result as OkObjectResult)?.Value as PortfolioSettingDto;
                Assert.That(created, Is.Not.Null,
                    $"The create route should answer with the created settings, but answered {response.Result}. Accept a payload that names no switch.");
                return created!.Id;
            }

            private int ConnectionIn(LighthouseAppContext context)
            {
                if (connectionId is null)
                {
                    var connection = new WorkTrackingSystemConnection
                    {
                        Name = "Tracker",
                        WorkTrackingSystem = WorkTrackingSystems.Jira,
                    };
                    context.WorkTrackingSystemConnections.Add(connection);
                    context.SaveChanges();
                    connectionId = connection.Id;
                    if (connectionHolds == WhatTheConnectionHolds.ASizeFieldDefault)
                    {
                        NameTheSizeFieldDefault(context, connection);
                    }
                }

                return connectionId.Value;
            }

            private static void NameTheSizeFieldDefault(LighthouseAppContext context, WorkTrackingSystemConnection connection)
            {
                var field = new AdditionalFieldDefinition { DisplayName = "Story Points", Reference = "customfield_10016", WorkTrackingSystemConnectionId = connection.Id };
                context.Set<AdditionalFieldDefinition>().Add(field);
                context.SaveChanges();
                connection.SizeEstimateFieldForPortfolios = field.Id;
                context.SaveChanges();
            }

            private PortfolioSettingDto StoredSettingsOf(int id)
            {
                using var scope = harness.NewScope();
                var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                var repository = new PortfolioRepository(context, NullLogger<PortfolioRepository>.Instance);

                return new PortfolioSettingDto(repository.GetById(id)!);
            }

            private static PortfolioSettingDto Read(JsonObject payload)
                => JsonSerializer.Deserialize<PortfolioSettingDto>(payload.ToJsonString(), AsTheRouteReadsIt)!;
        }
    }
}
