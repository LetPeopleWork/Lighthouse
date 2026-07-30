using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class DataRetrievalSchemaDtoTest
    {
        [Test]
        [TestCase(WorkTrackingSystems.Linear, "linear.team", "wizard-select", true, false)]
        [TestCase(WorkTrackingSystems.AzureDevOps, "ado.wiql", "freetext", true, true)]
        [TestCase(WorkTrackingSystems.Jira, "jira.jql", "freetext", true, true)]
        [TestCase(WorkTrackingSystems.Csv, "csv.filedata", "file-upload", true, true)]
        [TestCase(WorkTrackingSystems.ServiceNow, "servicenow.query", "freetext", true, false)]
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
