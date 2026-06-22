using System.Globalization;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.AzureDevOps
{
    [Category("AdoIntegration")]
    public class AzureDevOpsWriteBackTest
    {
        private const string TargetDateField = "Microsoft.VSTS.Scheduling.TargetDate";
        private const string TargetDateFieldReference = "Target Date";
        
        private const string AdditionalInfoField = "Custom.AdditionalInformation";
        
        private const string AgeField = "Custom.Age";
        private const string AgeFieldReference = "Age";

        private const string OrganizationUrl = "https://dev.azure.com/huserben";
        private const string TeamProject = "CMFTTestTeamProject";
        private const string IntegrationTokenEnvironmentVariable = "AzureDevOpsLighthouseIntegrationTestToken";

        // Write-back integration tests mutate real Azure DevOps work items, and every write adds a
        // revision. Azure DevOps caps a work item at 10000 revisions and then refuses all further API
        // updates, so reusing fixed items eventually burns them permanently. Instead we create a fresh
        // Feature and User Story per fixture run and destroy them in teardown, keeping revision counts low.
        private string featureId = string.Empty;
        private string storyId = string.Empty;
        private string secondStoryId = string.Empty;
        private readonly List<int> createdWorkItemIds = [];

        [OneTimeSetUp]
        public async Task CreateScratchWorkItems()
        {
            var personalAccessToken = Environment.GetEnvironmentVariable(IntegrationTokenEnvironmentVariable);
            if (string.IsNullOrEmpty(personalAccessToken))
            {
                // No integration token (e.g. fork PRs): non-integration tests in this fixture still run
                // without touching Azure DevOps; integration tests will be skipped by category filter.
                return;
            }

            using var connection = CreateVssConnection(personalAccessToken);
            var witClient = await connection.GetClientAsync<WorkItemTrackingHttpClient>();

            featureId = await CreateScratchWorkItem(witClient, "Feature");
            storyId = await CreateScratchWorkItem(witClient, "User Story");
            secondStoryId = await CreateScratchWorkItem(witClient, "User Story");
        }

        [OneTimeTearDown]
        public async Task RemoveScratchWorkItems()
        {
            if (createdWorkItemIds.Count == 0)
            {
                return;
            }

            var personalAccessToken = Environment.GetEnvironmentVariable(IntegrationTokenEnvironmentVariable);
            if (string.IsNullOrEmpty(personalAccessToken))
            {
                return;
            }

            using var connection = CreateVssConnection(personalAccessToken);
            var witClient = await connection.GetClientAsync<WorkItemTrackingHttpClient>();

            foreach (var id in createdWorkItemIds)
            {
                try
                {
                    // The integration PAT cannot delete work items (VS403145), so instead transition the
                    // scratch items to the terminal "Removed" state. That is a plain update the PAT is
                    // allowed to make and keeps them off active boards.
                    var patchDocument = new JsonPatchDocument
                    {
                        new JsonPatchOperation
                        {
                            Operation = Operation.Add,
                            Path = "/fields/System.State",
                            Value = "Removed"
                        }
                    };

                    await witClient.UpdateWorkItemAsync(patchDocument, id, suppressNotifications: true);
                }
                catch (Exception ex)
                {
                    // Best-effort cleanup: a leftover scratch item is harmless, a failed teardown is not
                    // worth failing the run over. Surface it for diagnosis without throwing.
                    TestContext.Progress.WriteLine($"Failed to remove scratch work item {id}: {ex.Message}");
                }
            }

            createdWorkItemIds.Clear();
        }

        private async Task<string> CreateScratchWorkItem(WorkItemTrackingHttpClient witClient, string workItemType)
        {
            var patchDocument = new JsonPatchDocument
            {
                new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.Title",
                    Value = $"Lighthouse WriteBack scratch {workItemType} {DateTime.UtcNow:O}"
                }
            };

            var created = await witClient.CreateWorkItemAsync(patchDocument, TeamProject, workItemType);
            var id = created.Id!.Value;
            createdWorkItemIds.Add(id);
            return id.ToString(CultureInfo.InvariantCulture);
        }

        private static VssConnection CreateVssConnection(string personalAccessToken)
        {
            return new VssConnection(new Uri(OrganizationUrl), new VssBasicCredential(string.Empty, personalAccessToken));
        }

        [Test]
        [Category("Integration")]
        public async Task WriteFieldsToWorkItems_SingleUpdate_SucceedsAndReturnsResult()
        {
            var subject = CreateSubject();
            var connection = CreateWorkTrackingSystemConnection();

            var updates = new List<WriteBackFieldUpdate>
            {
                new() { WorkItemId = storyId, TargetFieldReference = AdditionalInfoField, Value = "42" }
            };

            var result = await subject.WriteFieldsToWorkItems(connection, updates);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.ItemResults, Has.Count.EqualTo(1));
                Assert.That(result.AllSucceeded, Is.True);
                Assert.That(result.ItemResults[0].WorkItemId, Is.EqualTo(storyId));
            }
        }

        [Test]
        [Category("Integration")]
        public async Task WriteFieldsToWorkItems_MultipleUpdates_SucceedsForAll()
        {
            var subject = CreateSubject();
            var connection = CreateWorkTrackingSystemConnection();

            var updates = new List<WriteBackFieldUpdate>
            {
                new() { WorkItemId = storyId, TargetFieldReference = AdditionalInfoField, Value = "10" },
                new() { WorkItemId = secondStoryId, TargetFieldReference = AdditionalInfoField, Value = "20" }
            };

            var result = await subject.WriteFieldsToWorkItems(connection, updates);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.ItemResults, Has.Count.EqualTo(2));
                Assert.That(result.AllSucceeded, Is.True);
            }
        }

        [Test]
        [Category("Integration")]
        public async Task WriteFieldsToWorkItems_InvalidFieldReference_ReturnsFailure()
        {
            var subject = CreateSubject();
            var connection = CreateWorkTrackingSystemConnection();

            var updates = new List<WriteBackFieldUpdate>
            {
                new() { WorkItemId = storyId, TargetFieldReference = "NonExistent.FieldThatDoesNotExist", Value = "42" }
            };

            var result = await subject.WriteFieldsToWorkItems(connection, updates);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.ItemResults, Has.Count.EqualTo(1));
                Assert.That(result.ItemResults[0].Success, Is.False);
                Assert.That(result.ItemResults[0].ErrorMessage, Is.Not.Null.And.Not.Empty);
            }
        }

        [Test]
        [Category("Integration")]
        public async Task WriteFieldsToWorkItems_InvalidWorkItemId_ReturnsFailure()
        {
            var subject = CreateSubject();
            var connection = CreateWorkTrackingSystemConnection();

            var updates = new List<WriteBackFieldUpdate>
            {
                new() { WorkItemId = "999999999", TargetFieldReference = "System.Title", Value = "should fail" }
            };

            var result = await subject.WriteFieldsToWorkItems(connection, updates);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.ItemResults, Has.Count.EqualTo(1));
                Assert.That(result.ItemResults[0].Success, Is.False);
                Assert.That(result.ItemResults[0].ErrorMessage, Is.Not.Null.And.Not.Empty);
            }
        }

        [Test]
        public async Task WriteFieldsToWorkItems_EmptyUpdates_ReturnsEmptyResult()
        {
            var subject = CreateSubject();
            var connection = CreateWorkTrackingSystemConnection();

            var result = await subject.WriteFieldsToWorkItems(connection, []);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.ItemResults, Is.Empty);
                Assert.That(result.AllSucceeded, Is.True);
            }
        }

        [Test]
        public async Task WriteFieldsToWorkItems_NonNumericWorkItemId_ReturnsFailure()
        {
            var subject = CreateSubject();
            var connection = CreateWorkTrackingSystemConnection();

            var updates = new List<WriteBackFieldUpdate>
            {
                new() { WorkItemId = "not-a-number", TargetFieldReference = "System.Title", Value = "test" }
            };

            var result = await subject.WriteFieldsToWorkItems(connection, updates);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.ItemResults, Has.Count.EqualTo(1));
                Assert.That(result.ItemResults[0].Success, Is.False);
                Assert.That(result.ItemResults[0].ErrorMessage, Does.Contain("not-a-number"));
            }
        }

        [Test]
        [Category("Integration")]
        [TestCase(TargetDateField)]
        [TestCase(TargetDateFieldReference)]
        public async Task WriteDate_IsoFormat_WritesDateFieldAndReadBackMatches(string fieldReference)
        {
            var subject = CreateSubject();
            var connection = CreateWorkTrackingSystemConnection();

            var targetDate = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc);
            var isoValue = targetDate.ToString("o");

            var writeResult = await subject.WriteFieldsToWorkItems(connection, [
                new WriteBackFieldUpdate { WorkItemId = featureId, TargetFieldReference = fieldReference, Value = isoValue }
            ]);

            Assert.That(writeResult.AllSucceeded, Is.True);

            var readBackValue = await ReadBackFeatureAdditionalField(subject, connection, featureId, fieldReference, 100);

            Assert.That(readBackValue, Is.Not.Null.And.Not.Empty);
            var parsedDate = DateTime.Parse(readBackValue!, CultureInfo.InvariantCulture);
            Assert.That(parsedDate.Date, Is.EqualTo(targetDate.Date));
        }

        [Test]
        [Category("Integration")]
        public async Task WriteDate_ShortDateFormat_WritesDateFieldAndReadBackMatches()
        {
            var subject = CreateSubject();
            var connection = CreateWorkTrackingSystemConnection();

            var targetDate = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc);
            var shortDateValue = targetDate.ToString("yyyy-MM-dd");

            var writeResult = await subject.WriteFieldsToWorkItems(connection, [
                new WriteBackFieldUpdate { WorkItemId = featureId, TargetFieldReference = TargetDateField, Value = shortDateValue }
            ]);

            Assert.That(writeResult.AllSucceeded, Is.True);

            var readBackValue = await ReadBackFeatureAdditionalField(subject, connection, featureId, TargetDateField, 101);

            Assert.That(readBackValue, Is.Not.Null.And.Not.Empty);
            var parsedDate = DateTime.Parse(readBackValue!, CultureInfo.InvariantCulture);
            Assert.That(parsedDate.Date, Is.EqualTo(targetDate.Date));
        }

        [Test]
        [Category("Integration")]
        public async Task WriteDate_UsStyleFormat_WritesDateFieldAndReadBackMatches()
        {
            var subject = CreateSubject();
            var connection = CreateWorkTrackingSystemConnection();

            var targetDate = new DateTime(2026, 12, 25, 0, 0, 0, DateTimeKind.Utc);
            var usFormatValue = targetDate.ToString("MM/dd/yyyy");

            var writeResult = await subject.WriteFieldsToWorkItems(connection, [
                new WriteBackFieldUpdate { WorkItemId = featureId, TargetFieldReference = TargetDateField, Value = usFormatValue }
            ]);

            Assert.That(writeResult.AllSucceeded, Is.True);

            var readBackValue = await ReadBackFeatureAdditionalField(subject, connection, featureId, TargetDateField, 102);

            Assert.That(readBackValue, Is.Not.Null.And.Not.Empty);
            var parsedDate = DateTime.Parse(readBackValue!, CultureInfo.InvariantCulture);
            Assert.That(parsedDate.Date, Is.EqualTo(targetDate.Date));
        }

        [Test]
        [Category("Integration")]
        public async Task WriteDateAsText_IsoFormat_WritesToTextFieldAndReadBackMatches()
        {
            var subject = CreateSubject();
            var connection = CreateWorkTrackingSystemConnection();

            var targetDate = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc);
            var isoValue = $"{targetDate:yyyy-MM-dd HH:mm:ss} UTC";

            var writeResult = await subject.WriteFieldsToWorkItems(connection, [
                new WriteBackFieldUpdate { WorkItemId = featureId, TargetFieldReference = AdditionalInfoField, Value = isoValue }
            ]);

            Assert.That(writeResult.AllSucceeded, Is.True, () => string.Join("; ", writeResult.ItemResults.Where(r => !r.Success).Select(r => $"{r.WorkItemId}/{r.TargetFieldReference}: {r.ErrorMessage}")));

            var readBackValue = await ReadBackFeatureAdditionalField(subject, connection, featureId, AdditionalInfoField, 103);

            Assert.That(readBackValue, Is.Not.Null.And.Not.Empty);
            Assert.That(readBackValue, Does.Contain("2026"));
        }

        [Test]
        [Category("Integration")]
        public async Task WriteDateAsText_EuropeanFormat_WritesToTextFieldAndReadBackMatches()
        {
            var subject = CreateSubject();
            var connection = CreateWorkTrackingSystemConnection();

            var formattedDate = "25.12.2026";

            var writeResult = await subject.WriteFieldsToWorkItems(connection, [
                new WriteBackFieldUpdate { WorkItemId = featureId, TargetFieldReference = AdditionalInfoField, Value = formattedDate }
            ]);

            Assert.That(writeResult.AllSucceeded, Is.True);

            var readBackValue = await ReadBackFeatureAdditionalField(subject, connection, featureId, AdditionalInfoField, 104);

            Assert.That(readBackValue, Is.Not.Null.And.Not.Empty);
            Assert.That(readBackValue, Does.Contain("2026"));
        }

        [Test]
        [Category("Integration")]
        public async Task WriteDateAsText_LongDateFormat_WritesToTextFieldAndReadBackMatches()
        {
            var subject = CreateSubject();
            var connection = CreateWorkTrackingSystemConnection();

            var targetDate = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);
            var longDateValue = targetDate.ToString("MMMM dd, yyyy");

            var writeResult = await subject.WriteFieldsToWorkItems(connection, [
                new WriteBackFieldUpdate { WorkItemId = featureId, TargetFieldReference = AdditionalInfoField, Value = longDateValue }
            ]);

            Assert.That(writeResult.AllSucceeded, Is.True);

            var readBackValue = await ReadBackFeatureAdditionalField(subject, connection, featureId, AdditionalInfoField, 105);

            Assert.That(readBackValue, Is.Not.Null.And.Not.Empty);
            Assert.That(readBackValue, Does.Contain("2026"));
        }

        [Test]
        [Category("Integration")]
        [TestCase(AgeField)]
        [TestCase(AgeFieldReference)]
        public async Task WriteNumericValue_Age_WritesToStoryAndReadBackMatches(string fieldReference)
        {
            var subject = CreateSubject();
            var connection = CreateWorkTrackingSystemConnection();

            var ageValue = "42";

            var writeResult = await subject.WriteFieldsToWorkItems(connection, [
                new WriteBackFieldUpdate { WorkItemId = storyId, TargetFieldReference = fieldReference, Value = ageValue }
            ]);

            Assert.That(writeResult.AllSucceeded, Is.True);

            var readBackValue = await ReadBackStoryAdditionalField(subject, connection, storyId, fieldReference, 106);

            Assert.That(readBackValue, Is.Not.Null.And.Not.Empty);
            Assert.That(readBackValue, Does.Contain("42"));
        }

        [Test]
        [Category("Integration")]
        public async Task WriteNumericValue_CycleTime_WritesToStoryAndReadBackMatches()
        {
            var subject = CreateSubject();
            var connection = CreateWorkTrackingSystemConnection();

            var cycleTimeValue = "7";

            var writeResult = await subject.WriteFieldsToWorkItems(connection, [
                new WriteBackFieldUpdate { WorkItemId = storyId, TargetFieldReference = AgeField, Value = cycleTimeValue }
            ]);

            Assert.That(writeResult.AllSucceeded, Is.True);

            var readBackValue = await ReadBackStoryAdditionalField(subject, connection, storyId, AgeField, 107);

            Assert.That(readBackValue, Is.Not.Null.And.Not.Empty);
            Assert.That(readBackValue, Does.Contain("7"));
        }

        [Test]
        [Category("Integration")]
        public async Task WriteMultipleFieldTypes_DateAndNumeric_AllSucceedAndReadBackCorrectly()
        {
            var subject = CreateSubject();
            var connection = CreateWorkTrackingSystemConnection();

            var targetDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

            var updates = new List<WriteBackFieldUpdate>
            {
                new() { WorkItemId = featureId, TargetFieldReference = TargetDateField, Value = targetDate.ToString("o") },
                new() { WorkItemId = storyId, TargetFieldReference = AdditionalInfoField, Value = "September 01, 2026" },
                new() { WorkItemId = secondStoryId, TargetFieldReference = AgeField, Value = "15" }
            };

            var writeResult = await subject.WriteFieldsToWorkItems(connection, updates);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(writeResult.AllSucceeded, Is.True, () => string.Join("; ", writeResult.ItemResults.Where(r => !r.Success).Select(r => $"{r.WorkItemId}/{r.TargetFieldReference}: {r.ErrorMessage}")));
                Assert.That(writeResult.SuccessCount, Is.EqualTo(3));

                var dateReadBack = await ReadBackFeatureAdditionalField(subject, connection, featureId, TargetDateField, 108);
                Assert.That(dateReadBack, Is.Not.Null.And.Not.Empty);
                var parsedDate = DateTime.Parse(dateReadBack!, CultureInfo.InvariantCulture);
                Assert.That(parsedDate.Date, Is.EqualTo(targetDate.Date));

                var textReadBack = await ReadBackStoryAdditionalField(subject, connection, storyId, AdditionalInfoField, 109);
                Assert.That(textReadBack, Is.Not.Null.And.Not.Empty);
                Assert.That(textReadBack, Does.Contain("2026"));

                var numericReadBack = await ReadBackStoryAdditionalField(subject, connection, secondStoryId, AgeField, 110);
                Assert.That(numericReadBack, Is.Not.Null.And.Not.Empty);
                Assert.That(numericReadBack, Does.Contain("15"));
            }
        }

        private static async Task<string?> ReadBackFeatureAdditionalField(
            AzureDevOpsWorkTrackingConnector subject,
            WorkTrackingSystemConnection connection,
            string workItemId,
            string fieldReference,
            int fieldDefId)
        {
            var additionalFieldDef = new AdditionalFieldDefinition
            {
                Id = fieldDefId,
                DisplayName = fieldReference,
                Reference = fieldReference
            };

            var portfolio = new Portfolio
            {
                Name = "WriteBackReadTest",
                DataRetrievalValue = $"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Id}] = '{workItemId}'"
            };
            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add("Feature");

            portfolio.WorkTrackingSystemConnection = connection;
            connection.AdditionalFieldDefinitions.Clear();
            connection.AdditionalFieldDefinitions.Add(additionalFieldDef);

            var features = await subject.GetFeaturesForProject(portfolio);
            var feature = features.SingleOrDefault(f => f.ReferenceId == workItemId);

            return feature?.AdditionalFieldValues.GetValueOrDefault(fieldDefId);
        }

        private static async Task<string?> ReadBackStoryAdditionalField(
            AzureDevOpsWorkTrackingConnector subject,
            WorkTrackingSystemConnection connection,
            string workItemId,
            string fieldReference,
            int fieldDefId)
        {
            var additionalFieldDef = new AdditionalFieldDefinition
            {
                Id = fieldDefId,
                DisplayName = fieldReference,
                Reference = fieldReference
            };

            var team = new Team
            {
                Name = "WriteBackReadTest",
                DataRetrievalValue = $"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Id}] = '{workItemId}'"
            };

            team.WorkTrackingSystemConnection = connection;
            connection.AdditionalFieldDefinitions.Clear();
            connection.AdditionalFieldDefinitions.Add(additionalFieldDef);

            var workItems = await subject.GetWorkItemsForTeam(team);
            var workItem = workItems.SingleOrDefault(wi => wi.ReferenceId == workItemId);

            return workItem?.AdditionalFieldValues.GetValueOrDefault(fieldDefId);
        }

        private static WorkTrackingSystemConnection CreateWorkTrackingSystemConnection()
        {
            var organizationUrl = "https://dev.azure.com/huserben";
            var personalAccessToken = Environment.GetEnvironmentVariable("AzureDevOpsLighthouseIntegrationTestToken") ?? "fake-token-for-non-integration-tests";

            var connectionSetting = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.AzureDevOps, Name = "Test Setting", AuthenticationMethodKey = AuthenticationMethodKeys.AzureDevOpsPat };
            connectionSetting.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.Url, Value = organizationUrl, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken, Value = personalAccessToken, IsSecret = true },
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.RequestTimeoutInSeconds, Value = "100", IsSecret = false },
            ]);

            return connectionSetting;
        }

        private static AzureDevOpsWorkTrackingConnector CreateSubject()
        {
            return new AzureDevOpsWorkTrackingConnector(Mock.Of<ILogger<AzureDevOpsWorkTrackingConnector>>(), TestAuthStrategyFactory.CreateRealFactory(new FakeCryptoService()));
        }
    }
}
