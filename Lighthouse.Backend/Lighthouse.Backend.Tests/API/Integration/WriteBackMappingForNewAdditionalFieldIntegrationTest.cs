using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    // The settings screen hands unsaved rows negative placeholder ids, so a mapping added against a field
    // added in the same edit arrives pointing at that placeholder. These drive the real HTTP pipeline on
    // SQLite with foreign keys enforced, where a placeholder that reaches the database fails the save.
    [TestFixture]
    public class WriteBackMappingForNewAdditionalFieldIntegrationTest
    {
        private const int NewFieldPlaceholderId = -1;
        private const string NewFieldReference = "customfield_1";

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private static readonly JsonSerializerOptions ServedJson = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

        private HttpClient client = null!;

        [SetUp]
        public void Init()
        {
            rootFactory = new TestWebApplicationFactory<Program>();

            var licenseServiceMock = new Mock<ILicenseService>();
            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory)
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<ILicenseService>();
                        services.AddScoped(_ => licenseServiceMock.Object);
                    });
                });

            client = factory.CreateClient();
            client.AsSystemAdmin();

            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
        }

        [TearDown]
        public void Cleanup()
        {
            using (var scope = factory.Services.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureDeleted();
            }

            client.Dispose();
            factory.Dispose();
            rootFactory.Dispose();
        }

        [Test]
        public async Task Saving_a_connection_with_a_new_field_and_a_mapping_to_it_persists_the_mapping_against_the_new_field()
        {
            var connectionId = GivenAConnection();
            var payload = await ReadConnection(connectionId);
            payload.AdditionalFieldDefinitions.Add(ANewField());
            payload.WriteBackMappingDefinitions.Add(AMappingTo(NewFieldPlaceholderId, id: -1));

            var response = await client.PutAsJsonAsync($"/api/latest/worktrackingsystemconnections/{connectionId}", payload);

            await ThenTheMappingTargetsThePersistedNewField(response, connectionId);
        }

        [Test]
        public async Task Creating_a_connection_with_a_new_field_and_a_mapping_to_it_persists_the_mapping_against_the_new_field()
        {
            var payload = new WorkTrackingSystemConnectionDto
            {
                Name = "New connection",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                AuthenticationMethodKey = AuthenticationMethodKeys.GetDefaultForSystem(WorkTrackingSystems.Jira),
            };
            payload.AdditionalFieldDefinitions.Add(ANewField());
            payload.WriteBackMappingDefinitions.Add(AMappingTo(NewFieldPlaceholderId, id: -1));

            var response = await client.PostAsJsonAsync("/api/latest/worktrackingsystemconnections", payload);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
            var created = JsonSerializer.Deserialize<WorkTrackingSystemConnectionDto>(body, ServedJson);

            await ThenTheMappingTargetsThePersistedNewField(response, created!.Id);
        }

        [Test]
        public async Task Moving_an_existing_mapping_to_a_new_field_persists_the_mapping_against_the_new_field()
        {
            var connectionId = GivenAConnectionWithAMappingToAnExistingField();
            var payload = await ReadConnection(connectionId);
            payload.AdditionalFieldDefinitions.Add(ANewField());
            payload.WriteBackMappingDefinitions.Single().AdditionalFieldDefinitionId = NewFieldPlaceholderId;

            var response = await client.PutAsJsonAsync($"/api/latest/worktrackingsystemconnections/{connectionId}", payload);

            await ThenTheMappingTargetsThePersistedNewField(response, connectionId);
        }

        private async Task ThenTheMappingTargetsThePersistedNewField(HttpResponseMessage response, int connectionId)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            var served = JsonSerializer.Deserialize<WorkTrackingSystemConnectionDto>(body, ServedJson);
            var persisted = ReadPersistedConnection(connectionId);
            var newFieldId = persisted.AdditionalFieldDefinitions.Single(f => f.Reference == NewFieldReference).Id;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(newFieldId, Is.Positive);
                Assert.That(persisted.WriteBackMappingDefinitions.Single().AdditionalFieldDefinitionId, Is.EqualTo(newFieldId));
                Assert.That(served!.WriteBackMappingDefinitions.Single().AdditionalFieldDefinitionId, Is.EqualTo(newFieldId));
            }
        }

        private static AdditionalFieldDefinitionDto ANewField()
            => new() { Id = NewFieldPlaceholderId, DisplayName = "Forecast", Reference = NewFieldReference };

        private static WriteBackMappingDefinitionDto AMappingTo(int fieldId, int id)
            => new()
            {
                Id = id,
                ValueSource = WriteBackValueSource.WorkItemAgeCycleTime,
                AppliesTo = WriteBackAppliesTo.Team,
                AdditionalFieldDefinitionId = fieldId,
            };

        private async Task<WorkTrackingSystemConnectionDto> ReadConnection(int connectionId)
        {
            var response = await client.GetAsync($"/api/latest/worktrackingsystemconnections/{connectionId}");
            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
            return JsonSerializer.Deserialize<WorkTrackingSystemConnectionDto>(body, ServedJson)!;
        }

        private int GivenAConnection() => SeedConnection(withMappingToExistingField: false);

        private int GivenAConnectionWithAMappingToAnExistingField() => SeedConnection(withMappingToExistingField: true);

        private int SeedConnection(bool withMappingToExistingField)
        {
            using var scope = factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>();

            var connection = new WorkTrackingSystemConnection
            {
                Name = "Existing connection",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                AuthenticationMethodKey = AuthenticationMethodKeys.GetDefaultForSystem(WorkTrackingSystems.Jira),
            };
            var existingField = new AdditionalFieldDefinition { DisplayName = "Existing", Reference = "customfield_existing" };
            connection.AdditionalFieldDefinitions.Add(existingField);
            repository.Add(connection);
            repository.Save().GetAwaiter().GetResult();

            if (withMappingToExistingField)
            {
                connection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
                {
                    ValueSource = WriteBackValueSource.WorkItemAgeCycleTime,
                    AppliesTo = WriteBackAppliesTo.Team,
                    AdditionalFieldDefinitionId = existingField.Id,
                });
                repository.Update(connection);
                repository.Save().GetAwaiter().GetResult();
            }

            return connection.Id;
        }

        private WorkTrackingSystemConnection ReadPersistedConnection(int connectionId)
        {
            using var scope = factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>();
            return repository.GetById(connectionId)
                ?? throw new InvalidOperationException($"Connection {connectionId} not found");
        }
    }
}
