using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class DataRetrievalSchemaDtoTest
    {
        private const string TheWholeHierarchy = "task";

        private static readonly DateOnly Today = new(2026, 7, 31);

        [Test]
        [TestCase(WorkTrackingSystems.Linear, "linear.team", "wizard-select", true, false)]
        [TestCase(WorkTrackingSystems.AzureDevOps, "ado.wiql", "freetext", true, true)]
        [TestCase(WorkTrackingSystems.Jira, "jira.jql", "freetext", true, true)]
        [TestCase(WorkTrackingSystems.Csv, "csv.filedata", "file-upload", true, true)]
        [TestCase(WorkTrackingSystems.ServiceNow, "servicenow.query", "freetext", true, true)]
        public void ForTeam_ReturnsCorrectSchema(WorkTrackingSystems system, string expectedKey, string expectedInputKind, bool expectedIsRequired, bool expectedIsWorkItemTypesRequired)
        {
            var schema = DataRetrievalSchemaDto.ForTeam(system);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(schema.Key, Is.EqualTo(expectedKey));
                Assert.That(schema.InputKind, Is.EqualTo(expectedInputKind));
                Assert.That(schema.IsRequired, Is.EqualTo(expectedIsRequired));
                Assert.That(schema.IsWorkItemTypesRequired, Is.EqualTo(expectedIsWorkItemTypesRequired));
                Assert.That(schema.DisplayLabel, Is.Not.Empty);
            }
        }

        [Test]
        public void ForTeam_Linear_HasWizardHint()
        {
            var schema = DataRetrievalSchemaDto.ForTeam(WorkTrackingSystems.Linear);

            Assert.That(schema.WizardHint, Is.EqualTo("linear-team-select"));
        }

        [Test]
        [TestCase(WorkTrackingSystems.Linear, "linear.projects", "none", false, false)]
        [TestCase(WorkTrackingSystems.AzureDevOps, "ado.wiql", "freetext", true, true)]
        [TestCase(WorkTrackingSystems.Jira, "jira.jql", "freetext", true, true)]
        [TestCase(WorkTrackingSystems.Csv, "csv.filedata", "file-upload", true, true)]
        [TestCase(WorkTrackingSystems.ServiceNow, "servicenow.query", "none", false, false)]
        public void ForPortfolio_ReturnsCorrectSchema(WorkTrackingSystems system, string expectedKey, string expectedInputKind, bool expectedIsRequired, bool expectedIsWorkItemTypesRequired)
        {
            var schema = DataRetrievalSchemaDto.ForPortfolio(system);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(schema.Key, Is.EqualTo(expectedKey));
                Assert.That(schema.InputKind, Is.EqualTo(expectedInputKind));
                Assert.That(schema.IsRequired, Is.EqualTo(expectedIsRequired));
                Assert.That(schema.IsWorkItemTypesRequired, Is.EqualTo(expectedIsWorkItemTypesRequired));
                Assert.That(schema.DisplayLabel, Is.Not.Empty);
            }
        }

        [Test]
        public void ForPortfolio_Linear_HasNoWizardHint()
        {
            var schema = DataRetrievalSchemaDto.ForPortfolio(WorkTrackingSystems.Linear);

            Assert.That(schema.WizardHint, Is.Null);
        }

        [Test]
        public void ForTeam_ServiceNow_HasNoWizardHint()
        {
            var schema = DataRetrievalSchemaDto.ForTeam(WorkTrackingSystems.ServiceNow);

            Assert.That(schema.WizardHint, Is.Null);
        }

        // Story #5611, AC-B4 / ADR-123 decision 6 as amended 2026-07-31. Every ServiceNow team says
        // which kinds of work are its own — whatever table its connection reads. A field that is
        // hidden and still honoured by the read is the hazard the conditional carried, and no
        // ServiceNow team was ever shipped for it to protect. Neither component changes: they
        // already gate on this flag; what changes is what the schema says.
        [Test]
        [TestCase(TheWholeHierarchy, TestName = "AServiceNowTeamOnAHierarchyRoot_IsAskedWhichKindsOfWorkAreItsOwn")]
        [TestCase(ServiceNowWorkTrackingOptionNames.DefaultWorkItemTable, TestName = "AServiceNowTeamOnALeafTable_IsAskedWhichKindsOfWorkAreItsOwn")]
        [TestCase("", TestName = "AServiceNowTeamOnAConnectionThatNamedNoTable_IsAskedWhichKindsOfWorkAreItsOwn")]
        public void AServiceNowTeam_IsAskedWhichKindsOfWorkAreItsOwn(string table)
        {
            var settings = new TeamSettingDto(ATeamReading(table), Today);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(settings.DataRetrievalSchema.IsWorkItemTypesRequired, Is.True);
                Assert.That(settings.DataRetrievalSchema.Key, Is.EqualTo("servicenow.query"),
                    "Still the ServiceNow arm — a fallback that happens to require the field would pass this for the wrong reason.");
            }
        }

        private static Team ATeamReading(string table)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = "Acme ServiceNow",
                WorkTrackingSystem = WorkTrackingSystems.ServiceNow,
            };

            connection.Options.Add(new WorkTrackingSystemConnectionOption
            {
                Key = ServiceNowWorkTrackingOptionNames.WorkItemTable,
                Value = table,
                IsOptional = true,
            });

            return new Team { Name = "Service Desk", WorkTrackingSystemConnection = connection };
        }

        // Gives the switch the exhaustiveness the frontend Record gets from its type system (Bug #5613).
        [Test]
        public void SchemaFactories_EveryDeclaredWorkTrackingSystem_DoesNotUseTheQueryFallback()
        {
            using (Assert.EnterMultipleScope())
            {
                foreach (var system in Enum.GetValues<WorkTrackingSystems>())
                {
                    Assert.That(DataRetrievalSchemaDto.ForTeam(system).Key, Is.Not.EqualTo("query"), $"Team schema for {system} falls through to the fallback arm");
                    Assert.That(DataRetrievalSchemaDto.ForPortfolio(system).Key, Is.Not.EqualTo("query"), $"Portfolio schema for {system} falls through to the fallback arm");
                }
            }
        }
    }
}
