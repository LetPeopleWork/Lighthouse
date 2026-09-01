using System.Net;
using System.Text;
using System.Text.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.BehaviourSettings
{
    /// <summary>
    /// DISTILL acceptance harness (Story 5876 - Behaviour Settings). Single source of truth for HOW the
    /// scenarios of both slices reach the system: through the real ASP.NET host on real SQLite over real
    /// EF, per docs/architecture/atdd-infrastructure-policy.md. Only the licence port and the forecast
    /// runner are faked - the licence because it is the external, non-deterministic verdict the whole of
    /// slice 01 is about, and the forecast runner because it is a background queue, so a scenario that
    /// waited on it would be timing against a thread rather than asserting a promise.
    /// <para>
    /// The ordering seam, the rank seeder, the optional-feature seeder and both write paths stay
    /// production. Substituting any of them would make "nothing moved" a statement about a double.
    /// </para>
    /// </summary>
    public abstract class BehaviourSettingsAcceptanceTest
    {
        protected const string OrderOwnedByThisInstance = "ManualOrder";

        protected const string OrderOwnedByTheTracker = "SourceOrder";

        /// <summary>
        /// The premium fixture slice 01 runs on. No live premium optional feature exists until slice 02
        /// seeds the ordering row, so the refusal is exercised against a row a scenario adds itself -
        /// asserting it against the ordering row would make slice 01 depend on slice 02.
        /// </summary>
        protected const string PremiumFixtureKey = "PremiumFixture";

        /// <summary>
        /// What the product writes into <c>OptionalFeature.Id</c>: the store keys these rows by their key,
        /// so nothing generates the number and every row holds zero.
        /// </summary>
        protected const int IdentityEverySeededSettingCarries = 0;

        /// <summary>
        /// A key no seeder writes, so neither port has anything to find behind it.
        /// </summary>
        protected const string KeyNobodySeeded = "no-such-setting";

        protected TestWebApplicationFactory<Program> RootFactory = null!;
        protected WebApplicationFactory<Program> Factory = null!;
        protected HttpClient Client = null!;
        protected Mock<ILicenseService> LicenseServiceMock = null!;
        protected Mock<IForecastUpdater> ForecastUpdaterMock = null!;

        [SetUp]
        public void Init()
        {
            RootFactory = new TestWebApplicationFactory<Program>();

            LicenseServiceMock = new Mock<ILicenseService>();
            LicenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            ForecastUpdaterMock = new Mock<IForecastUpdater>();

            var connectorMock = new Mock<IWorkTrackingConnector>();
            connectorMock
                .Setup(c => c.GetFeaturesForProject(It.IsAny<Portfolio>()))
                .ReturnsAsync(() => []);
            connectorMock
                .Setup(c => c.GetParentFeaturesDetails(It.IsAny<Portfolio>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(() => []);

            var connectorFactoryMock = new Mock<IWorkTrackingConnectorFactory>();
            connectorFactoryMock
                .Setup(f => f.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>()))
                .Returns(connectorMock.Object);

            Factory = TestWebApplicationFactory<Program>.WithTestAuthentication(RootFactory)
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<ILicenseService>();
                        services.AddScoped(_ => LicenseServiceMock.Object);

                        services.RemoveAll<IWorkTrackingConnectorFactory>();
                        services.AddScoped(_ => connectorFactoryMock.Object);

                        services.RemoveAll<IForecastUpdater>();
                        services.AddSingleton(_ => ForecastUpdaterMock.Object);
                    });
                });

            Client = Factory.CreateClient();

            using var setupScope = Factory.Services.CreateScope();
            var dbContext = setupScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            RunEverySeeder();
        }

        [TearDown]
        public void Cleanup()
        {
            using (var teardownScope = Factory.Services.CreateScope())
            {
                var dbContext = teardownScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                dbContext.Database.EnsureDeleted();
            }

            Client.Dispose();
            Factory.Dispose();
            RootFactory.Dispose();
        }

        // --- Seeding (preconditions only - never the expected output) ---

        protected void RunEverySeeder()
        {
            using var scope = Factory.Services.CreateScope();

            foreach (var seeder in scope.ServiceProvider.GetServices<ISeeder>())
            {
                seeder.Seed().GetAwaiter().GetResult();
            }
        }

        protected int SeedPortfolio(string name)
        {
            using var scope = Factory.Services.CreateScope();

            var portfolio = new Portfolio
            {
                Name = name,
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Name = $"Connection {Guid.NewGuid():N}",
                    WorkTrackingSystem = WorkTrackingSystems.Jira,
                },
                DoneItemsCutoffDays = 365,
                DataRetrievalValue = "project = TEST",
                WorkItemTypes = ["Epic"],
                ToDoStates = ["New"],
                DoingStates = ["In Progress"],
                DoneStates = ["Done"],
            };

            var portfolioRepository = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
            portfolioRepository.Add(portfolio);
            portfolioRepository.Save().GetAwaiter().GetResult();

            return portfolio.Id;
        }

        /// <summary>
        /// One Feature carrying the source-system order value verbatim. The place is left null wherever the
        /// scenario is about what the seed produces - a scenario may not seed the places it then asserts.
        /// </summary>
        protected int SeedFeature(string name, string referenceId, string sourceOrder, int? manualRank, params int[] portfolioIds)
        {
            using var scope = Factory.Services.CreateScope();

            var portfolioRepository = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
            var featureRepository = scope.ServiceProvider.GetRequiredService<IRepository<Feature>>();

            var feature = new Feature
            {
                Name = name,
                ReferenceId = referenceId,
                Type = "Epic",
                State = "New",
                StateCategory = StateCategories.ToDo,
                Order = sourceOrder,
                ManualRank = manualRank,
            };

            foreach (var portfolioId in portfolioIds)
            {
                feature.Portfolios.Add(portfolioRepository.GetById(portfolioId)!);
            }

            featureRepository.Add(feature);
            featureRepository.Save().GetAwaiter().GetResult();

            return feature.Id;
        }

        protected void SeedPremiumOptionalFeature(string key, string name, string description)
            => SeedOptionalFeature(key, name, description, isPremium: true);

        /// <summary>
        /// A behaviour setting added by a scenario, carrying the same identity every seeded row carries.
        /// The store keys these rows by their key and nothing generates the number, so the product writes
        /// zero into every one of them; a fixture that invented a unique number here would be the only
        /// place in the system where a behaviour setting can be told apart by it.
        /// </summary>
        protected void SeedOptionalFeature(string key, string name, string description, bool isPremium)
        {
            using var scope = Factory.Services.CreateScope();

            var repository = scope.ServiceProvider.GetRequiredService<IRepository<OptionalFeature>>();

            var feature = new OptionalFeature
            {
                Id = IdentityEverySeededSettingCarries,
                Key = key,
                Name = name,
                Description = description,
                Enabled = false,
                IsPremium = isPremium,
            };

            repository.Add(feature);
            repository.Save().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Rewinds the instance to the shape it had before this story: the ordering choice living in the
        /// app setting and no optional-feature row for it. Re-running the seeders afterwards is the
        /// upgrade, and is the only moment the value can be carried across - the seeder never overwrites
        /// the stored on/off of a key it already knows, so a release that migrates wrongly cannot be
        /// repaired by a later seed.
        /// </summary>
        protected void SeedInstanceAsItWasBeforeTheUpgrade(string? storedPolicy)
        {
            using (var scope = Factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

                var alreadyMigrated = context.OptionalFeatures
                    .Where(f => f.Key == FeatureOrderingOptionalFeatureKey)
                    .ToList();
                context.OptionalFeatures.RemoveRange(alreadyMigrated);

                var setting = context.AppSettings.FirstOrDefault(s => s.Key == AppSettingKeys.FeatureOrderingPolicy);

                if (storedPolicy == null)
                {
                    if (setting != null)
                    {
                        context.AppSettings.Remove(setting);
                    }
                }
                else if (setting == null)
                {
                    context.AppSettings.Add(new AppSetting { Key = AppSettingKeys.FeatureOrderingPolicy, Value = storedPolicy });
                }
                else
                {
                    setting.Value = storedPolicy;
                }

                context.SaveChanges();
            }

            RunEverySeeder();
        }

        /// <summary>
        /// The key the ordering row is seeded under, spelled out rather than read off the production
        /// constant. This literal is the wire identity a browser addresses the row by, and the frontend
        /// suite names the same string; repointing this at a constant would let a rename pass both suites
        /// and still break the settings page.
        /// </summary>
        protected const string FeatureOrderingOptionalFeatureKey = "FeatureOrdering";

        protected void TheInstanceIsNotLicensedForPremium()
        {
            LicenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(false);
        }

        protected void TheCallerAdministersTheWholeInstance()
        {
            Client.DefaultRequestHeaders.Remove(TestAuthHandler.SubjectHeader);
            Client.DefaultRequestHeaders.Remove(TestAuthHandler.RolesHeader);
            Client.DefaultRequestHeaders.Add(TestAuthHandler.SubjectHeader, "test-admin");
            Client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, ClaimsDrivenRbacAdministrationService.SystemAdminGrant);
        }

        // --- Driving-port interaction ---

        protected async Task<(HttpStatusCode Status, string Body)> GetOptionalFeatures()
        {
            var response = await Client.GetAsync("/api/latest/optionalfeatures");
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// The toggle port, addressed the way a caller has to address it. The body stays raw JSON rather
        /// than the shipped entity, so these scenarios judge the wire contract a client really sends: a
        /// rename on the server side cannot keep them green. It is sent whole, the way the settings page
        /// sends it - a partial body is rejected by model validation before the endpoint is reached, which
        /// would answer a question no scenario asked.
        /// <para>
        /// The scenario names the setting by its key, and so does the route. The number in the body is
        /// along for the ride: the store keys these rows by their key, so choosing a row by the number
        /// would be ambiguous the moment a second row exists.
        /// </para>
        /// </summary>
        protected async Task<(HttpStatusCode Status, string Body)> ToggleOptionalFeature(string key, bool enabled)
        {
            var stored = ReadStoredOptionalFeature(key);

            Assert.That(stored.Found, Is.True,
                $"No behaviour setting is stored under '{key}', so there is nothing to switch.");

            var payload = JsonSerializer.Serialize(new
            {
                id = IdentityEverySeededSettingCarries,
                key,
                name = stored.Name,
                description = stored.Description,
                enabled,
                isPremium = stored.IsPremium,
                isPreview = stored.IsPreview,
            });

            using var body = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await Client.PostAsync($"/api/latest/optionalfeatures/{key}", body);

            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// The same port aimed at a setting nobody can name. It claims to be premium, so a premium check
        /// hoisted above the lookup fires whether it reads the request or the store - a body claiming
        /// otherwise would let one of those two hoists through unnoticed.
        /// </summary>
        protected async Task<(HttpStatusCode Status, string Body)> ToggleASettingThatDoesNotExist()
        {
            var payload = JsonSerializer.Serialize(new
            {
                id = IdentityEverySeededSettingCarries,
                key = KeyNobodySeeded,
                name = "No such setting",
                description = string.Empty,
                enabled = true,
                isPremium = true,
                isPreview = false,
            });

            using var body = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await Client.PostAsync($"/api/latest/optionalfeatures/{KeyNobodySeeded}", body);

            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// The read port aimed at one setting rather than at the whole list, which is how a caller asks
        /// for the setting it names.
        /// </summary>
        protected async Task<(HttpStatusCode Status, string Body)> GetOptionalFeature(string key)
        {
            var response = await Client.GetAsync($"/api/latest/optionalfeatures/{key}");
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        protected async Task<(HttpStatusCode Status, string Body)> GetOrderingPolicy()
        {
            var response = await Client.GetAsync("/api/latest/appsettings/FeatureOrdering");
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        protected async Task<(HttpStatusCode Status, string Body)> SetOrderingPolicyThroughTheAlias(string policy)
        {
            using var body = new StringContent($"{{\"policy\":\"{policy}\"}}", Encoding.UTF8, "application/json");
            var response = await Client.PutAsync("/api/latest/appsettings/FeatureOrdering", body);

            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        protected async Task<(HttpStatusCode Status, string Body)> GetAllFeatures()
        {
            var response = await Client.GetAsync("/api/latest/features");
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        // --- Driven-port probes (the store, read directly) ---

        /// <summary>
        /// How a behaviour setting reads in the store. The identity is deliberately absent: it is not part
        /// of what a setting is, and including it would make an unrelated scenario fail the day the rows
        /// stop all carrying zero.
        /// </summary>
        protected (bool Found, bool Enabled, bool IsPremium, bool IsPreview, string Name, string Description) ReadStoredOptionalFeature(string key)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var stored = context.OptionalFeatures.AsNoTracking().FirstOrDefault(f => f.Key == key);

            return stored == null
                ? (false, false, false, false, string.Empty, string.Empty)
                : (true, stored.Enabled, stored.IsPremium, stored.IsPreview, stored.Name, stored.Description);
        }

        protected string? ReadStoredAppSetting(string key)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            return context.AppSettings.AsNoTracking().FirstOrDefault(s => s.Key == key)?.Value;
        }

        protected List<(string ReferenceId, int? ManualRank, string SourceOrder)> ReadStoredOrderingColumns()
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            return context.Features
                .AsNoTracking()
                .OrderBy(feature => feature.Id)
                .Select(feature => new { feature.ReferenceId, feature.ManualRank, feature.Order })
                .ToList()
                .Select(row => (row.ReferenceId, row.ManualRank, row.Order))
                .ToList();
        }

        // --- Parsing ---

        protected static List<JsonElement> ParseOptionalFeatureRows((HttpStatusCode Status, string Body) response)
        {
#pragma warning disable NUnit2045 // Guard-then-parse, not independent asserts: under Assert.Multiple the JSON read below would run on a failed response and throw over the clear message.
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                $"The Behaviour Settings read port must answer. Body: {Excerpt(response.Body)}");
            Assert.That(response.Body.TrimStart(), Does.StartWith("["),
                $"The read port must return a JSON array. Body starts: {Excerpt(response.Body)}");
#pragma warning restore NUnit2045

            using var document = JsonDocument.Parse(response.Body);
            return [.. document.RootElement.EnumerateArray().Select(element => element.Clone())];
        }

        protected static string Excerpt(string body) => body[..Math.Min(160, body.Length)];
    }
}
