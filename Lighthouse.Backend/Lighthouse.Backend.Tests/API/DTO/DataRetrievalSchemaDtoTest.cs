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
    }
}
