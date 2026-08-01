using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class DataRetrievalSchemaDtoTest
    {
        // The example DD-5 names: a real encoded query in column form, narrow enough to be one team's.
        private const string WorkedExample = "active=true^assignment_group=Service Desk";

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
        // Stated on the connection that carries no options at all, which is the shape every
        // ServiceNow connection now has: the table is gone from the option set entirely, so there is
        // nothing left for the schema to be a function of.
        [Test]
        public void AServiceNowTeam_IsAskedWhichKindsOfWorkAreItsOwn()
        {
            var schema = new TeamSettingDto(ATeamOnAConnectionWithNoOptions(), Today).DataRetrievalSchema;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(schema.IsWorkItemTypesRequired, Is.True);
                Assert.That(schema.Key, Is.EqualTo("servicenow.query"),
                    "Still the ServiceNow arm — a fallback that happens to require the field would pass this for the wrong reason.");
            }
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

        // Story #5610 slice 01, AC-A1 / AC-A4 / DD-5. The first real user of the connector stopped at
        // an empty box labelled "ServiceNow Query (Encoded Query)" with nothing anywhere in the
        // product saying what an encoded query is. The example is the one DD-5 names; it is pinned
        // against the literal because a test that compares the value to the constant it came from
        // survives blanking the constant.
        [Test]
        public void AServiceNowTeamsQueryField_ShowsAWorkedExampleOfTheQueryItWants()
        {
            var schema = DataRetrievalSchemaDto.ForTeam(WorkTrackingSystems.ServiceNow);

            Assert.That(schema.Placeholder, Is.EqualTo(WorkedExample));
        }

        // AC-A4. The two ways a ServiceNow query fails without saying so, measured in the epic SPIKE
        // (Q3) and hit again in the slice-04 dogfood: a field name the instance does not know is
        // dropped and the query widens to the whole table, and a bad value on a real field matches
        // nothing. This is the last surface before either one costs a user their afternoon, so the
        // help has to name both — and say where ServiceNow will hand them a correct query.
        [Test]
        public void AServiceNowTeamsQueryField_NamesBothWaysAQueryFailsQuietlyAndWhereToGetAGoodOne()
        {
            var help = DataRetrievalSchemaDto.ForTeam(WorkTrackingSystems.ServiceNow).HelpText;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(help, Is.Not.Null.And.Not.Empty);
                Assert.That(help, Does.Contain("whole table").IgnoreCase,
                    "An unknown field name is dropped and the query widens to everything — the refusal ValidateTeamSettings then raises.");
                Assert.That(help, Does.Contain("nothing").IgnoreCase,
                    "A bad value on a real field matches nothing at all, which reads as an empty team rather than a typo.");
                Assert.That(help, Does.Contain("Copy query"),
                    "ServiceNow itself hands out a correct encoded query from a filter breadcrumb. Naming that path is the whole point of the guidance.");
            }
        }

        // AC-A2. The guidance is carried by the schema, so a connector that has nothing to explain
        // renders exactly what it renders today. Stated as an absence claim, which is why it is a pin
        // rather than a red test: it exists to fail the day somebody adds copy to a shared arm.
        [Test]
        [TestCase(WorkTrackingSystems.Jira)]
        [TestCase(WorkTrackingSystems.AzureDevOps)]
        [TestCase(WorkTrackingSystems.Linear)]
        [TestCase(WorkTrackingSystems.Csv)]
        public void AConnectorWithNothingToExplain_LeavesItsQueryFieldExactlyAsItWas(WorkTrackingSystems system)
        {
            var schema = DataRetrievalSchemaDto.ForTeam(system);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(schema.Placeholder, Is.Null);
                Assert.That(schema.HelpText, Is.Null);
            }
        }

        // AC-A6. ServiceNow portfolios render no query field at all, so guidance there would be help
        // for a surface that does not exist.
        [Test]
        public void AServiceNowPortfolio_IsOfferedNoGuidanceForAFieldItNeverRenders()
        {
            var schema = DataRetrievalSchemaDto.ForPortfolio(WorkTrackingSystems.ServiceNow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(schema.InputKind, Is.EqualTo("none"));
                Assert.That(schema.Placeholder, Is.Null);
                Assert.That(schema.HelpText, Is.Null);
            }
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
