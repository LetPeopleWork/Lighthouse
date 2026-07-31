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
        //
        // The table is said out loud rather than parametrised over. ForTeam takes the system and
        // nothing else, so a case per table would run one case three times while reading as a
        // table-independence claim; the connection with no options at all is the case that actually
        // states it.
        [Test]
        public void AServiceNowTeam_IsAskedWhichKindsOfWorkAreItsOwn_WhateverTableItsConnectionReads()
        {
            var rootedAtAHierarchy = new TeamSettingDto(ATeamReading(TheWholeHierarchy), Today).DataRetrievalSchema;
            var onAConnectionThatNamedNoTable = new TeamSettingDto(ATeamOnAConnectionWithNoOptions(), Today).DataRetrievalSchema;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rootedAtAHierarchy.IsWorkItemTypesRequired, Is.True);
                Assert.That(rootedAtAHierarchy.Key, Is.EqualTo("servicenow.query"),
                    "Still the ServiceNow arm — a fallback that happens to require the field would pass this for the wrong reason.");
                Assert.That(onAConnectionThatNamedNoTable.IsWorkItemTypesRequired, Is.True,
                    "The connection's options are not an input to the schema at all any more, and this says so rather than leaving it to be inferred.");
            }
        }

        private static Team ATeamReading(string table)
        {
            var team = ATeamOnAConnectionWithNoOptions();

            team.WorkTrackingSystemConnection.Options.Add(new WorkTrackingSystemConnectionOption
            {
                Key = ServiceNowWorkTrackingOptionNames.WorkItemTable,
                Value = table,
                IsOptional = true,
            });

            return team;
        }

        private static Team ATeamOnAConnectionWithNoOptions()
        {
            return new Team
            {
                Name = "Service Desk",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Name = "Acme ServiceNow",
                    WorkTrackingSystem = WorkTrackingSystems.ServiceNow,
                },
            };
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
