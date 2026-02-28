using System.Globalization;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.AzureDevOps
{
    public class AzureDevOpsWriteBackTest
    {
        private const string TargetDateField = "Microsoft.VSTS.Scheduling.TargetDate";
        private const string AdditionalInfoField = "Custom.AdditionalInformation";
        private const string AgeField = "Custom.Age";

        private const string FeatureId = "370";

        [Test]
        [Category("Integration")]
        public async Task WriteFieldsToWorkItems_SingleUpdate_SucceedsAndReturnsResult()
        {
            var subject = CreateSubject();
            var connection = CreateWorkTrackingSystemConnection();

            var updates = new List<WriteBackFieldUpdate>
            {
                new() { WorkItemId = "377", TargetFieldReference = AdditionalInfoField, Value = "42" }
            };

            var result = await subject.WriteFieldsToWorkItems(connection, updates);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.ItemResults, Has.Count.EqualTo(1));
                Assert.That(result.AllSucceeded, Is.True);
                Assert.That(result.ItemResults[0].WorkItemId, Is.EqualTo("377"));
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
                new() { WorkItemId = "377", TargetFieldReference = AdditionalInfoField, Value = "10" },
                new() { WorkItemId = "365", TargetFieldReference = AdditionalInfoField, Value = "20" }
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
                new() { WorkItemId = "377", TargetFieldReference = "NonExistent.FieldThatDoesNotExist", Value = "42" }
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
        public async Task WriteDate_IsoFormat_WritesDateFieldAndReadBackMatches()
        {
            var subject = CreateSubject();
            var connection = CreateWorkTrackingSystemConnection();

            var targetDate = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc);
            var isoValue = targetDate.ToString("o");

            var writeResult = await subject.WriteFieldsToWorkItems(connection, [
                new WriteBackFieldUpdate { WorkItemId = FeatureId, TargetFieldReference = TargetDateField, Value = isoValue }
            ]);

            Assert.That(writeResult.AllSucceeded, Is.True);

            var readBackValue = await ReadBackFeatureAdditionalField(subject, connection, FeatureId, TargetDateField, 100);

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
                new WriteBackFieldUpdate { WorkItemId = FeatureId, TargetFieldReference = TargetDateField, Value = shortDateValue }
            ]);

            Assert.That(writeResult.AllSucceeded, Is.True);

            var readBackValue = await ReadBackFeatureAdditionalField(subject, connection, FeatureId, TargetDateField, 101);

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
                new WriteBackFieldUpdate { WorkItemId = FeatureId, TargetFieldReference = TargetDateField, Value = usFormatValue }
            ]);

            Assert.That(writeResult.AllSucceeded, Is.True);

            var readBackValue = await ReadBackFeatureAdditionalField(subject, connection, FeatureId, TargetDateField, 102);

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
                new WriteBackFieldUpdate { WorkItemId = FeatureId, TargetFieldReference = AdditionalInfoField, Value = isoValue }
            ]);

            Assert.That(writeResult.AllSucceeded, Is.True, () => string.Join("; ", writeResult.ItemResults.Where(r => !r.Success).Select(r => $"{r.WorkItemId}/{r.TargetFieldReference}: {r.ErrorMessage}")));

            var readBackValue = await ReadBackFeatureAdditionalField(subject, connection, FeatureId, AdditionalInfoField, 103);

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
                new WriteBackFieldUpdate { WorkItemId = FeatureId, TargetFieldReference = AdditionalInfoField, Value = formattedDate }
            ]);

            Assert.That(writeResult.AllSucceeded, Is.True);

            var readBackValue = await ReadBackFeatureAdditionalField(subject, connection, FeatureId, AdditionalInfoField, 104);

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
                new WriteBackFieldUpdate { WorkItemId = FeatureId, TargetFieldReference = AdditionalInfoField, Value = longDateValue }
            ]);

            Assert.That(writeResult.AllSucceeded, Is.True);

            var readBackValue = await ReadBackFeatureAdditionalField(subject, connection, FeatureId, AdditionalInfoField, 105);

            Assert.That(readBackValue, Is.Not.Null.And.Not.Empty);
            Assert.That(readBackValue, Does.Contain("2026"));
        }

        [Test]
        [Category("Integration")]
        public async Task WriteNumericValue_Age_WritesToStoryAndReadBackMatches()
        {
            var subject = CreateSubject();
            var connection = CreateWorkTrackingSystemConnection();

            var ageValue = "42";

            var writeResult = await subject.WriteFieldsToWorkItems(connection, [
                new WriteBackFieldUpdate { WorkItemId = "366", TargetFieldReference = AgeField, Value = ageValue }
            ]);

            Assert.That(writeResult.AllSucceeded, Is.True);

            var readBackValue = await ReadBackStoryAdditionalField(subject, connection, "366", AgeField, 106);

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
                new WriteBackFieldUpdate { WorkItemId = "366", TargetFieldReference = AgeField, Value = cycleTimeValue }
            ]);

            Assert.That(writeResult.AllSucceeded, Is.True);

            var readBackValue = await ReadBackStoryAdditionalField(subject, connection, "366", AgeField, 107);

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
                new() { WorkItemId = FeatureId, TargetFieldReference = TargetDateField, Value = targetDate.ToString("o") },
                new() { WorkItemId = "367", TargetFieldReference = AdditionalInfoField, Value = "September 01, 2026" },
                new() { WorkItemId = "375", TargetFieldReference = AgeField, Value = "15" }
            };

            var writeResult = await subject.WriteFieldsToWorkItems(connection, updates);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(writeResult.AllSucceeded, Is.True, () => string.Join("; ", writeResult.ItemResults.Where(r => !r.Success).Select(r => $"{r.WorkItemId}/{r.TargetFieldReference}: {r.ErrorMessage}")));
                Assert.That(writeResult.SuccessCount, Is.EqualTo(3));

                var dateReadBack = await ReadBackFeatureAdditionalField(subject, connection, FeatureId, TargetDateField, 108);
                Assert.That(dateReadBack, Is.Not.Null.And.Not.Empty);
                var parsedDate = DateTime.Parse(dateReadBack!, CultureInfo.InvariantCulture);
                Assert.That(parsedDate.Date, Is.EqualTo(targetDate.Date));

                var textReadBack = await ReadBackStoryAdditionalField(subject, connection, "367", AdditionalInfoField, 109);
                Assert.That(textReadBack, Is.Not.Null.And.Not.Empty);
                Assert.That(textReadBack, Does.Contain("2026"));

                var numericReadBack = await ReadBackStoryAdditionalField(subject, connection, "375", AgeField, 110);
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

            var connectionSetting = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.AzureDevOps, Name = "Test Setting" };
            connectionSetting.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.Url, Value = organizationUrl, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken, Value = personalAccessToken, IsSecret = true },
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.RequestTimeoutInSeconds, Value = "100", IsSecret = false },
            ]);

            return connectionSetting;
        }

        private static AzureDevOpsWorkTrackingConnector CreateSubject()
        {
            return new AzureDevOpsWorkTrackingConnector(Mock.Of<ILogger<AzureDevOpsWorkTrackingConnector>>(), new FakeCryptoService());
        }
    }
}
