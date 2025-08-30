using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Factories
{
    public class WorkTrackingSystemFactoryTest
    {
        [Test]
        [TestCase(WorkTrackingSystems.Jira)]
        [TestCase(WorkTrackingSystems.AzureDevOps)]
        public void CreateDefaultConnectionForWorkTrackingSystem_CreatesDefaultConnection(WorkTrackingSystems workTrackingSystem)
        {
            var subject = CreateSubject();

            var defaultConnection = subject.CreateDefaultConnectionForWorkTrackingSystem(workTrackingSystem);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(defaultConnection.Id, Is.Zero);
                Assert.That(defaultConnection.Name, Is.EqualTo($"New {workTrackingSystem} Connection"));
                Assert.That(defaultConnection.WorkTrackingSystem, Is.EqualTo(workTrackingSystem));
            };
        }

        [Test]
        public void CreateOptionsForWorkTrackingSystem_GivenAzureDevOps_ReturnsCorrectOptions()
        {
            var subject = CreateSubject();

            var connection = subject.CreateDefaultConnectionForWorkTrackingSystem(WorkTrackingSystems.AzureDevOps);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(connection.Options, Has.Count.EqualTo(2));
                Assert.That(ContainsOption(connection.Options, AzureDevOpsWorkTrackingOptionNames.Url), Is.True);
                Assert.That(ContainsOption(connection.Options, AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken, true), Is.True);
            };
        }

        [Test]
        public void CreateOptionsForWorkTrackingSystem_GivenJira_ReturnsCorrectOptions()
        {
            var subject = CreateSubject();

            var connection = subject.CreateDefaultConnectionForWorkTrackingSystem(WorkTrackingSystems.Jira);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(connection.Options, Has.Count.EqualTo(3));
                Assert.That(ContainsOption(connection.Options, JiraWorkTrackingOptionNames.Url), Is.True);
                Assert.That(ContainsOption(connection.Options, JiraWorkTrackingOptionNames.Username, false, true), Is.True);
                Assert.That(ContainsOption(connection.Options, JiraWorkTrackingOptionNames.ApiToken, true), Is.True);
            };
        }

        [Test]
        public void CreateOptionsForWorkTrackingSystem_GivenLinear_ReturnsCorrectOptions()
        {
            var subject = CreateSubject();

            var connection = subject.CreateDefaultConnectionForWorkTrackingSystem(WorkTrackingSystems.Linear);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(connection.Options, Has.Count.EqualTo(1));
                Assert.That(ContainsOption(connection.Options, LinearWorkTrackingOptionNames.ApiKey, true, false), Is.True);
            };
        }

        [Test]
        public void CreateOptionsForWorkTrackingSystem_GivenCsv_ReturnsCorrectOptions()
        {
            var subject = CreateSubject();

            var connection = subject.CreateDefaultConnectionForWorkTrackingSystem(WorkTrackingSystems.Csv);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(connection.Options, Has.Count.EqualTo(15));
                Assert.That(ContainsOption(connection.Options, CsvWorkTrackingOptionNames.Delimiter), Is.True);
                Assert.That(ContainsOption(connection.Options, CsvWorkTrackingOptionNames.DateTimeFormat), Is.True);
                Assert.That(ContainsOption(connection.Options, CsvWorkTrackingOptionNames.TagSeparator), Is.True);
                Assert.That(ContainsOption(connection.Options, CsvWorkTrackingOptionNames.IdHeader), Is.True);
                Assert.That(ContainsOption(connection.Options, CsvWorkTrackingOptionNames.NameHeader), Is.True);
                Assert.That(ContainsOption(connection.Options, CsvWorkTrackingOptionNames.StateHeader), Is.True);
                Assert.That(ContainsOption(connection.Options, CsvWorkTrackingOptionNames.TypeHeader), Is.True);
                Assert.That(ContainsOption(connection.Options, CsvWorkTrackingOptionNames.StartedDateHeader), Is.True);
                Assert.That(ContainsOption(connection.Options, CsvWorkTrackingOptionNames.ClosedDateHeader), Is.True);
                Assert.That(ContainsOption(connection.Options, CsvWorkTrackingOptionNames.CreatedDateHeader, false, true), Is.True);
                Assert.That(ContainsOption(connection.Options, CsvWorkTrackingOptionNames.ParentReferenceIdHeader, false, true), Is.True);
                Assert.That(ContainsOption(connection.Options, CsvWorkTrackingOptionNames.TagsHeader, false, true), Is.True);
                Assert.That(ContainsOption(connection.Options, CsvWorkTrackingOptionNames.UrlHeader, false, true), Is.True);
                Assert.That(ContainsOption(connection.Options, CsvWorkTrackingOptionNames.OwningTeamHeader, false, true), Is.True);
                Assert.That(ContainsOption(connection.Options, CsvWorkTrackingOptionNames.EstimatedSizeHeader, false, true), Is.True);

                Assert.That(GetOptionValue(connection.Options, CsvWorkTrackingOptionNames.Delimiter), Is.EqualTo(","));
                Assert.That(GetOptionValue(connection.Options, CsvWorkTrackingOptionNames.DateTimeFormat), Is.EqualTo("yyyy-MM-dd HH:mm:ss"));
                Assert.That(GetOptionValue(connection.Options, CsvWorkTrackingOptionNames.TagSeparator), Is.EqualTo(";"));
                Assert.That(GetOptionValue(connection.Options, CsvWorkTrackingOptionNames.IdHeader), Is.EqualTo("ID"));
                Assert.That(GetOptionValue(connection.Options, CsvWorkTrackingOptionNames.NameHeader), Is.EqualTo("Name"));
                Assert.That(GetOptionValue(connection.Options, CsvWorkTrackingOptionNames.StateHeader), Is.EqualTo("State"));
                Assert.That(GetOptionValue(connection.Options, CsvWorkTrackingOptionNames.TypeHeader), Is.EqualTo("Type"));
                Assert.That(GetOptionValue(connection.Options, CsvWorkTrackingOptionNames.StartedDateHeader), Is.EqualTo("Started Date"));
                Assert.That(GetOptionValue(connection.Options, CsvWorkTrackingOptionNames.ClosedDateHeader), Is.EqualTo("Closed Date"));
                Assert.That(GetOptionValue(connection.Options, CsvWorkTrackingOptionNames.CreatedDateHeader), Is.EqualTo("Created Date"));
                Assert.That(GetOptionValue(connection.Options, CsvWorkTrackingOptionNames.ParentReferenceIdHeader), Is.EqualTo("Parent Reference Id"));
                Assert.That(GetOptionValue(connection.Options, CsvWorkTrackingOptionNames.TagsHeader), Is.EqualTo("Tags"));
                Assert.That(GetOptionValue(connection.Options, CsvWorkTrackingOptionNames.UrlHeader), Is.EqualTo("Url"));
                Assert.That(GetOptionValue(connection.Options, CsvWorkTrackingOptionNames.OwningTeamHeader), Is.EqualTo("Owning Team"));
                Assert.That(GetOptionValue(connection.Options, CsvWorkTrackingOptionNames.EstimatedSizeHeader), Is.EqualTo("Estimated Size"));
            }
            ;
        }

        private bool ContainsOption(IEnumerable<WorkTrackingSystemConnectionOption> options, string key, bool isSecret = false, bool isOptional = false)
        {
            return options.Any(option => option.Key == key && option.IsSecret == isSecret && option.IsOptional == isOptional);
        }

        private string GetOptionValue(IEnumerable<WorkTrackingSystemConnectionOption> options, string key)
        {
            return options.Single(o => o.Key == key).Value;
        }

        private WorkTrackingSystemFactory CreateSubject()
        {
            return new WorkTrackingSystemFactory(Mock.Of<ILogger<WorkTrackingSystemFactory>>());
        }
    }
}
