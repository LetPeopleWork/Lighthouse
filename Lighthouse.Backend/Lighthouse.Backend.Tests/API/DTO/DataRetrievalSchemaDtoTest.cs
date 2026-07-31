using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class DataRetrievalSchemaDtoTest
    {
        private const string TheWholeHierarchy = "task";

        private const string ALeafTable = ServiceNowWorkTrackingOptionNames.DefaultWorkItemTable;

        private static readonly DateOnly Today = new(2026, 7, 31);

        [Test]
        [TestCase(WorkTrackingSystems.Linear, "linear.team", "wizard-select", true, false)]
        [TestCase(WorkTrackingSystems.AzureDevOps, "ado.wiql", "freetext", true, true)]
        [TestCase(WorkTrackingSystems.Jira, "jira.jql", "freetext", true, true)]
        [TestCase(WorkTrackingSystems.Csv, "csv.filedata", "file-upload", true, true)]
        [TestCase(WorkTrackingSystems.ServiceNow, "servicenow.query", "freetext", true, false)]
        public void ForTeam_ReturnsCorrectSchema(WorkTrackingSystems system, string expectedKey, string expectedInputKind, bool expectedIsRequired, bool expectedIsWorkItemTypesRequired)
        {
            var schema = DataRetrievalSchemaDto.ForTeam(system, ALeafTable);

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
            var schema = DataRetrievalSchemaDto.ForTeam(WorkTrackingSystems.Linear, ALeafTable);

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
            var schema = DataRetrievalSchemaDto.ForTeam(WorkTrackingSystems.ServiceNow, ALeafTable);

            Assert.That(schema.WizardHint, Is.Null);
        }

        // Story #5611 slice 01, AC-B4 / ADR-123 decision 6. A team reading a whole ServiceNow
        // hierarchy has to say which kinds of work are its own, so the settings screen and the create
        // wizard both show the field and both refuse an empty save. Neither component changes — they
        // already gate on this flag; what changes is what the schema says.
        [Test]
        public void ATeamOnAWholeServiceNowHierarchy_IsAskedWhichKindsOfWorkAreItsOwn()
        {
            var settings = new TeamSettingDto(ATeamReading(TheWholeHierarchy), Today);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(settings.DataRetrievalSchema.IsWorkItemTypesRequired, Is.True);
                Assert.That(settings.DataRetrievalSchema.Key, Is.EqualTo("servicenow.query"),
                    "Still the ServiceNow arm — a fallback that happens to require the field would pass this for the wrong reason.");
            }
        }

        // AC-B5. The shipped configuration keeps hiding the field and keeps saving without it.
        [Test]
        public void ATeamOnASingleKindOfServiceNowWork_IsNotAskedForKindsOfWorkAtAll()
        {
            var settings = new TeamSettingDto(ATeamReading(ServiceNowWorkTrackingOptionNames.DefaultWorkItemTable), Today);

            Assert.That(settings.DataRetrievalSchema.IsWorkItemTypesRequired, Is.False);
        }

        // A connection that never named a table reads the shipped default, so it behaves as a
        // single-kind team rather than as one on the whole hierarchy.
        [Test]
        public void ATeamOnAServiceNowConnectionThatNamedNoTable_IsNotAskedForKindsOfWorkEither()
        {
            var settings = new TeamSettingDto(ATeamReading(string.Empty), Today);

            Assert.That(settings.DataRetrievalSchema.IsWorkItemTypesRequired, Is.False);
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
        // Both tables, so the ServiceNow arm is covered on the branch that requires kinds of work as
        // well as the one that does not — a conditional arm is two arms to fall through (#5611 T-2).
        [Test]
        [TestCase(ALeafTable, TestName = "SchemaFactories_EveryDeclaredWorkTrackingSystem_DoesNotUseTheQueryFallback")]
        [TestCase(TheWholeHierarchy, TestName = "SchemaFactories_EveryDeclaredWorkTrackingSystemOnAHierarchyRoot_DoesNotUseTheQueryFallback")]
        public void SchemaFactories_EveryDeclaredWorkTrackingSystem_DoesNotUseTheQueryFallback(string workItemTable)
        {
            using (Assert.EnterMultipleScope())
            {
                foreach (var system in Enum.GetValues<WorkTrackingSystems>())
                {
                    Assert.That(DataRetrievalSchemaDto.ForTeam(system, workItemTable).Key, Is.Not.EqualTo("query"), $"Team schema for {system} falls through to the fallback arm");
                    Assert.That(DataRetrievalSchemaDto.ForPortfolio(system).Key, Is.Not.EqualTo("query"), $"Portfolio schema for {system} falls through to the fallback arm");
                }
            }
        }
    }
}
