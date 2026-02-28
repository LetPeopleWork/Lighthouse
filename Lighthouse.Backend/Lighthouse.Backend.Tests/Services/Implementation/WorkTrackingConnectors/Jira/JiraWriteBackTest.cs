using System.Globalization;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    public class JiraWriteBackTest
    {
        private const string DeliveryDateField = "customfield_10205";
        private const string DescriptionField = "description";
        private const string AgeField = "customfield_10206";

        private const string EpicId = "LGHTHSDMO-1";
        private const string StoryId = "LGHTHSDMO-16";

        [Test]
        [Category("Integration")]
        public async Task WriteFieldsToWorkItems_SingleUpdate_SucceedsAndReturnsResult()
        {
            var subject = CreateSubject();
            var connection = CreateWorkTrackingSystemConnection();

            var updates = new List<WriteBackFieldUpdate>
            {
                new() { WorkItemId = EpicId, TargetFieldReference = DescriptionField, Value = "42" }
            };

            var result = await subject.WriteFieldsToWorkItems(connection, updates);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.ItemResults, Has.Count.EqualTo(1));
                Assert.That(result.AllSucceeded, Is.True);
                Assert.That(result.ItemResults[0].WorkItemId, Is.EqualTo(EpicId));
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
                new() { WorkItemId = EpicId, TargetFieldReference = DescriptionField, Value = "10" },
                new() { WorkItemId = StoryId, TargetFieldReference = DescriptionField, Value = "20" }
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
                new() { WorkItemId = EpicId, TargetFieldReference = "nonexistent_field_xyz", Value = "42" }
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
        public async Task WriteFieldsToWorkItems_InvalidIssueKey_ReturnsFailure()
        {
            var subject = CreateSubject();
            var connection = CreateWorkTrackingSystemConnection();

            var updates = new List<WriteBackFieldUpdate>
            {
                new() { WorkItemId = "NONEXISTENT-99999", TargetFieldReference = "summary", Value = "should fail" }
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
        [Category("Integration")]
        public async Task WriteDate_IsoFormat_WritesDateFieldAndReadBackMatches()
        {
            var subject = CreateSubject();
            var connection = CreateWorkTrackingSystemConnection();

            var targetDate = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc);
            var dateValue = targetDate.ToString("yyyy-MM-dd");

            var writeResult = await subject.WriteFieldsToWorkItems(connection, [
                new WriteBackFieldUpdate { WorkItemId = EpicId, TargetFieldReference = DeliveryDateField, Value = dateValue }
            ]);

            Assert.That(writeResult.AllSucceeded, Is.True);

            var readBackValue = await ReadBackEpicAdditionalField(subject, connection, EpicId, DeliveryDateField, 100);

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
            var dateValue = targetDate.ToString("yyyy-MM-dd");

            var writeResult = await subject.WriteFieldsToWorkItems(connection, [
                new WriteBackFieldUpdate { WorkItemId = EpicId, TargetFieldReference = DeliveryDateField, Value = dateValue }
            ]);

            Assert.That(writeResult.AllSucceeded, Is.True);

            var readBackValue = await ReadBackEpicAdditionalField(subject, connection, EpicId, DeliveryDateField, 101);

            Assert.That(readBackValue, Is.Not.Null.And.Not.Empty);
            var parsedDate = DateTime.Parse(readBackValue!, CultureInfo.InvariantCulture);
            Assert.That(parsedDate.Date, Is.EqualTo(targetDate.Date));
        }

        [Test]
        [Category("Integration")]
        public async Task WriteDate_DifferentDate_WritesDateFieldAndReadBackMatches()
        {
            var subject = CreateSubject();
            var connection = CreateWorkTrackingSystemConnection();

            var targetDate = new DateTime(2026, 12, 25, 0, 0, 0, DateTimeKind.Utc);
            var dateValue = targetDate.ToString("yyyy-MM-dd");

            var writeResult = await subject.WriteFieldsToWorkItems(connection, [
                new WriteBackFieldUpdate { WorkItemId = EpicId, TargetFieldReference = DeliveryDateField, Value = dateValue }
            ]);

            Assert.That(writeResult.AllSucceeded, Is.True);

            var readBackValue = await ReadBackEpicAdditionalField(subject, connection, EpicId, DeliveryDateField, 102);

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
            var isoValue = targetDate.ToString("yyyy-MM-ddTHH:mm:ssZ");

            var writeResult = await subject.WriteFieldsToWorkItems(connection, [
                new WriteBackFieldUpdate { WorkItemId = EpicId, TargetFieldReference = DescriptionField, Value = isoValue }
            ]);

            Assert.That(writeResult.AllSucceeded, Is.True);

            var readBackValue = await ReadBackEpicAdditionalField(subject, connection, EpicId, DescriptionField, 103);

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
                new WriteBackFieldUpdate { WorkItemId = EpicId, TargetFieldReference = DescriptionField, Value = formattedDate }
            ]);

            Assert.That(writeResult.AllSucceeded, Is.True);

            var readBackValue = await ReadBackEpicAdditionalField(subject, connection, EpicId, DescriptionField, 104);

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
            var longDateValue = targetDate.ToString("MMMM dd, yyyy", CultureInfo.InvariantCulture);

            var writeResult = await subject.WriteFieldsToWorkItems(connection, [
                new WriteBackFieldUpdate { WorkItemId = EpicId, TargetFieldReference = DescriptionField, Value = longDateValue }
            ]);

            Assert.That(writeResult.AllSucceeded, Is.True);

            var readBackValue = await ReadBackEpicAdditionalField(subject, connection, EpicId, DescriptionField, 105);

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
                new WriteBackFieldUpdate { WorkItemId = StoryId, TargetFieldReference = AgeField, Value = ageValue }
            ]);

            Assert.That(writeResult.AllSucceeded, Is.True);

            var readBackValue = await ReadBackStoryAdditionalField(subject, connection, StoryId, AgeField, 106);

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
                new WriteBackFieldUpdate { WorkItemId = StoryId, TargetFieldReference = AgeField, Value = cycleTimeValue }
            ]);

            Assert.That(writeResult.AllSucceeded, Is.True);

            var readBackValue = await ReadBackStoryAdditionalField(subject, connection, StoryId, AgeField, 107);

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
                new() { WorkItemId = EpicId, TargetFieldReference = DeliveryDateField, Value = targetDate.ToString("yyyy-MM-dd") },
                new() { WorkItemId = EpicId, TargetFieldReference = DescriptionField, Value = "September 01, 2026" },
                new() { WorkItemId = StoryId, TargetFieldReference = AgeField, Value = "15" }
            };

            var writeResult = await subject.WriteFieldsToWorkItems(connection, updates);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(writeResult.AllSucceeded, Is.True);
                Assert.That(writeResult.SuccessCount, Is.EqualTo(3));

                var dateReadBack = await ReadBackEpicAdditionalField(subject, connection, EpicId, DeliveryDateField, 108);
                Assert.That(dateReadBack, Is.Not.Null.And.Not.Empty);
                var parsedDate = DateTime.Parse(dateReadBack!, CultureInfo.InvariantCulture);
                Assert.That(parsedDate.Date, Is.EqualTo(targetDate.Date));

                var textReadBack = await ReadBackEpicAdditionalField(subject, connection, EpicId, DescriptionField, 109);
                Assert.That(textReadBack, Is.Not.Null.And.Not.Empty);
                Assert.That(textReadBack, Does.Contain("2026"));

                var numericReadBack = await ReadBackStoryAdditionalField(subject, connection, StoryId, AgeField, 110);
                Assert.That(numericReadBack, Is.Not.Null.And.Not.Empty);
                Assert.That(numericReadBack, Does.Contain("15"));
            }
        }

        private static async Task<string?> ReadBackEpicAdditionalField(
            JiraWorkTrackingConnector subject,
            WorkTrackingSystemConnection connection,
            string issueKey,
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
                DataRetrievalValue = $"project = LGHTHSDMO AND key = {issueKey}"
            };
            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add("Epic");

            portfolio.DoneStates.Clear();
            portfolio.DoneStates.Add("Done");
            portfolio.DoingStates.Clear();
            portfolio.DoingStates.Add("In Progress");
            portfolio.ToDoStates.Clear();
            portfolio.ToDoStates.Add("To Do");

            var connectionWithField = CloneConnectionWithAdditionalField(connection, additionalFieldDef);
            portfolio.WorkTrackingSystemConnection = connectionWithField;

            var features = await subject.GetFeaturesForProject(portfolio);
            var feature = features.SingleOrDefault(f => f.ReferenceId == issueKey);

            return feature?.AdditionalFieldValues.GetValueOrDefault(fieldDefId);
        }

        private static async Task<string?> ReadBackStoryAdditionalField(
            JiraWorkTrackingConnector subject,
            WorkTrackingSystemConnection connection,
            string issueKey,
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
                DataRetrievalValue = $"project = LGHTHSDMO AND key = {issueKey}"
            };
            team.WorkItemTypes.Clear();
            team.WorkItemTypes.Add("Story");
            team.WorkItemTypes.Add("Bug");
            team.WorkItemTypes.Add("Task");

            team.DoneStates.Clear();
            team.DoneStates.Add("Done");
            team.DoingStates.Clear();
            team.DoingStates.Add("In Progress");
            team.ToDoStates.Clear();
            team.ToDoStates.Add("To Do");

            var connectionWithField = CloneConnectionWithAdditionalField(connection, additionalFieldDef);
            team.WorkTrackingSystemConnection = connectionWithField;

            var workItems = await subject.GetWorkItemsForTeam(team);
            var workItem = workItems.SingleOrDefault(wi => wi.ReferenceId == issueKey);

            return workItem?.AdditionalFieldValues.GetValueOrDefault(fieldDefId);
        }

        private static WorkTrackingSystemConnection CloneConnectionWithAdditionalField(
            WorkTrackingSystemConnection original,
            AdditionalFieldDefinition additionalFieldDef)
        {
            var clone = new WorkTrackingSystemConnection
            {
                WorkTrackingSystem = original.WorkTrackingSystem,
                Name = original.Name,
                AuthenticationMethodKey = original.AuthenticationMethodKey
            };
            clone.Options.AddRange(original.Options);
            clone.AdditionalFieldDefinitions.Clear();
            clone.AdditionalFieldDefinitions.Add(additionalFieldDef);

            return clone;
        }

        private static WorkTrackingSystemConnection CreateWorkTrackingSystemConnection()
        {
            var organizationUrl = "https://letpeoplework.atlassian.net";
            var username = Environment.GetEnvironmentVariable("JiraLighthouseIntegrationTestUsername") ?? "atlassian.pushchair@huser-berta.com";
            var apiToken = Environment.GetEnvironmentVariable("JiraLighthouseIntegrationTestToken") ?? "fake-token-for-non-integration-tests";

            var connectionSetting = new WorkTrackingSystemConnection
            {
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                Name = "Test Setting",
                AuthenticationMethodKey = AuthenticationMethodKeys.JiraCloud
            };
            connectionSetting.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Url, Value = organizationUrl, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Username, Value = username, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.ApiToken, Value = apiToken, IsSecret = true },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.RequestTimeoutInSeconds, Value = "100", IsSecret = false },
            ]);

            return connectionSetting;
        }

        private static JiraWorkTrackingConnector CreateSubject()
        {
            return new JiraWorkTrackingConnector(
                new IssueFactory(Mock.Of<ILogger<IssueFactory>>()), Mock.Of<ILogger<JiraWorkTrackingConnector>>(), new FakeCryptoService());
        }
    }
}
