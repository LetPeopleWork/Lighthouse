using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// What a Jira issue says it is waiting on. Jira writes a link once and offers it from both ends:
    /// the waiting issue sees an inwardIssue, the issue being waited on sees an outwardIssue. Only the
    /// first is read, because taking both would record every dependency in the instance twice.
    /// </summary>
    public class JiraDependencyLinkTest
    {
        [Test]
        public void ExtractDependencyReferences_YieldsTheIssueAnInwardBlockingLinkPointsAt()
        {
            var fields = FieldsWith(BlockedByLink("PROJ-42"));

            var references = fields.ExtractDependencyReferences();

            var expected = new[] { "PROJ-42" };

            Assert.That(references, Is.EqualTo(expected));
        }

        [Test]
        public void ExtractDependencyReferences_YieldsNothingForAnIssueThatOnlyBlocksOthers()
        {
            var fields = FieldsWith(BlocksLink("PROJ-42"));

            var references = fields.ExtractDependencyReferences();

            Assert.That(references, Is.Empty);
        }

        [Test]
        public void ExtractDependencyReferences_YieldsOnlyTheInwardEndWhenBothDirectionsAreLinked()
        {
            var fields = FieldsWith(BlockedByLink("PROJ-1"), BlocksLink("PROJ-2"));

            var references = fields.ExtractDependencyReferences();

            var expected = new[] { "PROJ-1" };

            Assert.That(references, Is.EqualTo(expected));
        }

        [Test]
        public void ExtractDependencyReferences_YieldsEveryInwardBlockingLinkInTheOrderJiraWroteThem()
        {
            var fields = FieldsWith(BlockedByLink("PROJ-1"), BlockedByLink("PROJ-2"));

            var references = fields.ExtractDependencyReferences();

            var expected = new[] { "PROJ-1", "PROJ-2" };

            Assert.That(references, Is.EqualTo(expected));
        }

        /// <summary>
        /// Jira has several link types and only one of them means waiting. "relates to" and "duplicates"
        /// are links in exactly the same shape, so reading every inwardIssue would turn every loosely
        /// related issue into a blocker.
        /// </summary>
        [Test]
        [TestCase("relates to")]
        [TestCase("duplicates")]
        [TestCase("is cloned by")]
        public void ExtractDependencyReferences_YieldsNothingForAnInwardLinkThatDoesNotMeanWaiting(string inwardName)
        {
            var fields = FieldsWith(InwardLink(inwardName, "PROJ-42"));

            var references = fields.ExtractDependencyReferences();

            Assert.That(references, Is.Empty);
        }

        [Test]
        [TestCase("Is Blocked By")]
        [TestCase("IS BLOCKED BY")]
        public void ExtractDependencyReferences_ReadsTheLinkNameWithoutMindingItsCapitalisation(string inwardName)
        {
            var fields = FieldsWith(InwardLink(inwardName, "PROJ-42"));

            var references = fields.ExtractDependencyReferences();

            var expected = new[] { "PROJ-42" };

            Assert.That(references, Is.EqualTo(expected));
        }

        [Test]
        public void ExtractDependencyReferences_KeepsTheKeyExactlyAsJiraWroteIt()
        {
            var fields = FieldsWith(BlockedByLink("lower-99"));

            var references = fields.ExtractDependencyReferences();

            var expected = new[] { "lower-99" };

            Assert.That(references, Is.EqualTo(expected));
        }

        [Test]
        public void ExtractDependencyReferences_YieldsNothingWhenTheIssueCarriesNoLinksAtAll()
        {
            var fields = Fields("{}");

            var references = fields.ExtractDependencyReferences();

            Assert.That(references, Is.Empty);
        }

        [Test]
        [TestCase("""{"issuelinks": []}""")]
        [TestCase("""{"issuelinks": null}""")]
        [TestCase("""{"issuelinks": "not a list"}""")]
        [TestCase("""{"issuelinks": [{}]}""")]
        [TestCase("""{"issuelinks": [{"type": {"inward": "is blocked by"}}]}""")]
        [TestCase("""{"issuelinks": [{"type": {}, "inwardIssue": {"key": "PROJ-1"}}]}""")]
        [TestCase("""{"issuelinks": [{"type": {"inward": "is blocked by"}, "inwardIssue": {}}]}""")]
        [TestCase("""{"issuelinks": [{"type": {"inward": "is blocked by"}, "inwardIssue": {"key": ""}}]}""")]
        [TestCase("""{"issuelinks": [{"type": {"inward": null}, "inwardIssue": {"key": "PROJ-1"}}]}""")]
        public void ExtractDependencyReferences_YieldsNothingAndThrowsNothingForAPayloadItCannotRead(string rawFields)
        {
            var fields = Fields(rawFields);

            var references = fields.ExtractDependencyReferences();

            Assert.That(references, Is.Empty);
        }

        [Test]
        public void ExtractDependencyReferences_ReadsTheGoodLinksBesideOneItCannotRead()
        {
            var fields = Fields("""
                {"issuelinks": [
                    {"type": {"inward": "is blocked by"}},
                    {"type": {"inward": "is blocked by"}, "inwardIssue": {"key": "PROJ-7"}}
                ]}
                """);

            var references = fields.ExtractDependencyReferences();

            var expected = new[] { "PROJ-7" };

            Assert.That(references, Is.EqualTo(expected));
        }

        [Test]
        public void InwardLinkNames_NamesEveryInwardLinkTheIssueCarries()
        {
            var fields = FieldsWith(InwardLink("is halted by", "PROJ-1"), InwardLink("relates to", "PROJ-2"));

            var names = fields.InwardLinkNames();

            var expected = new[] { "is halted by", "relates to" };

            Assert.That(names, Is.EqualTo(expected));
        }

        /// <summary>
        /// An outward link is the far end of somebody else's dependency, so an instance that has renamed
        /// its link type is not evidenced by one. Naming them would send an administrator looking at the
        /// wrong half of their configuration.
        /// </summary>
        [Test]
        public void InwardLinkNames_SaysNothingAboutLinksPointingTheOtherWay()
        {
            var fields = FieldsWith(BlocksLink("PROJ-42"));

            var names = fields.InwardLinkNames();

            Assert.That(names, Is.Empty);
        }

        [Test]
        public void InwardLinkNames_SaysNothingWhenTheIssueCarriesNoLinksAtAll()
        {
            var fields = Fields("{}");

            var names = fields.InwardLinkNames();

            Assert.That(names, Is.Empty);
        }

        [Test]
        public async Task GetFeaturesForProject_AFeatureCarriesTheIssuesItsInwardLinksPointAt()
        {
            var recorded = new RecordedRequests();
            var subject = AConnectorReturning(recorded, AnEpic("PROJ-1", BlockedByLink("PROJ-2")));

            var features = await subject.GetFeaturesForProject(JiraConnectorTestSetup.APortfolioOnJiraCloud());

            var waitedOn = features.Single().DependsOnReferences.Select(reference => reference.ReferenceId);
            var expected = new[] { "PROJ-2" };

            Assert.That(waitedOn, Is.EqualTo(expected));
        }

        [Test]
        public async Task GetFeaturesForProject_AFeatureThatOnlyBlocksOthersWaitsOnNothing()
        {
            var recorded = new RecordedRequests();
            var subject = AConnectorReturning(recorded, AnEpic("PROJ-1", BlocksLink("PROJ-2")));

            var features = await subject.GetFeaturesForProject(JiraConnectorTestSetup.APortfolioOnJiraCloud());

            Assert.That(features.Single().DependsOnReferences, Is.Empty);
        }

        [Test]
        public async Task GetFeaturesForProject_EveryReferenceIsMarkedAsHavingComeFromTheTracker()
        {
            var recorded = new RecordedRequests();
            var subject = AConnectorReturning(recorded, AnEpic("PROJ-1", BlockedByLink("PROJ-2")));

            var features = await subject.GetFeaturesForProject(JiraConnectorTestSetup.APortfolioOnJiraCloud());

            Assert.That(features.Single().DependsOnReferences.Single().Source, Is.EqualTo(DependencySource.TrackerLink));
        }

        /// <summary>
        /// Everything the connector already read off a Jira issue, read again with the link mapping in
        /// place. The claim is that adding dependencies moved nothing else, which only a comparison over
        /// the whole mapped Feature can make.
        /// </summary>
        [Test]
        public async Task GetFeaturesForProject_ReadingTheLinksLeavesEveryOtherMappedValueAlone()
        {
            var recorded = new RecordedRequests();
            var withoutLinks = await AConnectorReturning(recorded, AnEpic("PROJ-1"))
                .GetFeaturesForProject(JiraConnectorTestSetup.APortfolioOnJiraCloud());

            var withLinks = await AConnectorReturning(new RecordedRequests(), AnEpic("PROJ-1", BlockedByLink("PROJ-2")))
                .GetFeaturesForProject(JiraConnectorTestSetup.APortfolioOnJiraCloud());

            var before = withoutLinks.Single();
            var after = withLinks.Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(after.ReferenceId, Is.EqualTo(before.ReferenceId));
                Assert.That(after.Name, Is.EqualTo(before.Name));
                Assert.That(after.State, Is.EqualTo(before.State));
                Assert.That(after.Type, Is.EqualTo(before.Type));
                Assert.That(after.Order, Is.EqualTo(before.Order));
                Assert.That(after.ParentReferenceId, Is.EqualTo(before.ParentReferenceId));
                Assert.That(after.CreatedDate, Is.EqualTo(before.CreatedDate));
                Assert.That(after.StartedDate, Is.EqualTo(before.StartedDate));
                Assert.That(after.ClosedDate, Is.EqualTo(before.ClosedDate));
                Assert.That(after.EstimatedSize, Is.EqualTo(before.EstimatedSize));
                Assert.That(after.OwningTeam, Is.EqualTo(before.OwningTeam));
                Assert.That(before.DependsOnReferences, Is.Empty);
            }
        }

        /// <summary>
        /// The links ride on the response the fetch already asks for. Asking for them separately would
        /// undo Epic 5687's Data Center sweep, which is the reason this is asserted rather than assumed.
        /// </summary>
        [Test]
        public async Task GetFeaturesForProject_ReadingTheLinksCostsTheRefreshNoRequestOfItsOwn()
        {
            var withoutLinks = new RecordedRequests();
            await AConnectorReturning(withoutLinks, AnEpic("PROJ-1"))
                .GetFeaturesForProject(JiraConnectorTestSetup.APortfolioOnJiraCloud());

            var withLinks = new RecordedRequests();
            await AConnectorReturning(withLinks, AnEpic("PROJ-1", BlockedByLink("PROJ-2")))
                .GetFeaturesForProject(JiraConnectorTestSetup.APortfolioOnJiraCloud());

            Assert.That(withLinks.Paths, Is.EqualTo(withoutLinks.Paths));
        }

        [Test]
        public async Task GetFeaturesForProject_TheSearchStillAsksForEveryFieldAndNothingIsAddedToIt()
        {
            var recorded = new RecordedRequests();
            var subject = AConnectorReturning(recorded, AnEpic("PROJ-1", BlockedByLink("PROJ-2")));

            await subject.GetFeaturesForProject(JiraConnectorTestSetup.APortfolioOnJiraCloud());

            Assert.That(recorded.FieldsAskedForInTheLastSearch(), Is.EqualTo("*all"));
        }

        /// <summary>
        /// The sweep is the change-detection pass and never maps a field, so a dependency it downloaded
        /// would be thrown away unread. Keeping it at identity plus the stamp is what took a Data Center
        /// portfolio refresh from about eight minutes to about two seconds, and widening it here is the
        /// one edit in this slice that could hand that back.
        /// </summary>
        [Test]
        public async Task SweepFeaturesForPortfolio_TheIdentitySweepStillAsksForNothingButIdentityAndTheStamp()
        {
            var recorded = new RecordedRequests();
            var subject = AConnectorReturning(recorded, AnEpic("PROJ-1", BlockedByLink("PROJ-2")));
            var portfolio = JiraConnectorTestSetup.APortfolioOnJiraCloud();

            // The sweep refuses an instance Lighthouse has never reached, because the two Jira deployments
            // page their results differently and neither endpoint answers on the other's path.
            await subject.GetFeaturesForProject(portfolio);

            await subject.SweepFeaturesForPortfolio(portfolio);

            Assert.That(recorded.FieldsAskedForInTheLastSearch(), Is.EqualTo("key,updated"));
        }

        /// <summary>
        /// A Jira administrator can rename "is blocked by", and a renamed instance looks from the outside
        /// exactly like one that has no dependencies at all. Saying which names were seen turns a silent
        /// nothing into something an administrator can act on, and keeps the answer in the mapper rather
        /// than growing a second setting beside the Portfolio's own.
        /// </summary>
        [Test]
        public async Task GetFeaturesForProject_LinksThatNoneOfThemMatchAreReportedWithTheNamesThatWereSeen()
        {
            var logger = new RecordingLogger<JiraWorkTrackingConnector>();
            var portfolio = JiraConnectorTestSetup.APortfolioOnJiraCloud();
            var subject = AConnectorReturning(logger, AnEpic("PROJ-1", InwardLink("is halted by", "PROJ-2")));

            await subject.GetFeaturesForProject(portfolio);

            var warning = logger.Warnings.Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(warning, Does.Contain(portfolio.Name));
                Assert.That(warning, Does.Contain("is halted by"));
                Assert.That(warning, Does.Contain("is blocked by"));
            }
        }

        [Test]
        public async Task GetFeaturesForProject_TheNamesAreListedOnceEachHoweverManyIssuesCarryThem()
        {
            var logger = new RecordingLogger<JiraWorkTrackingConnector>();
            var subject = AConnectorReturning(
                logger,
                AnEpic("PROJ-1", InwardLink("is halted by", "PROJ-9")),
                AnEpic("PROJ-2", InwardLink("is halted by", "PROJ-9")),
                AnEpic("PROJ-3", InwardLink("waits for", "PROJ-9")));

            await subject.GetFeaturesForProject(JiraConnectorTestSetup.APortfolioOnJiraCloud());

            var warning = logger.Warnings.Single();

            using (Assert.EnterMultipleScope())
            {
                // The whole list as the operator reads it, rather than each name somewhere in the line:
                // asserting the names separately passes just as happily on "is halted bywaits for", and
                // an unreadable run of words is the one way this sentence can fail to do its job.
                Assert.That(warning, Does.Contain("is halted by, waits for"));
                Assert.That(Occurrences(warning, "is halted by"), Is.EqualTo(1));
            }
        }

        [Test]
        public async Task GetFeaturesForProject_AnInstanceWhereSomeLinkDidMatchIsNotWarnedAt()
        {
            var logger = new RecordingLogger<JiraWorkTrackingConnector>();
            var subject = AConnectorReturning(
                logger,
                AnEpic("PROJ-1", BlockedByLink("PROJ-9")),
                AnEpic("PROJ-2", InwardLink("is halted by", "PROJ-9")));

            await subject.GetFeaturesForProject(JiraConnectorTestSetup.APortfolioOnJiraCloud());

            Assert.That(logger.Warnings, Is.Empty);
        }

        /// <summary>
        /// Having no dependencies is the ordinary case, not a misconfiguration. Warning about it would
        /// train every reader of the log to skip the line that matters.
        /// </summary>
        [Test]
        public async Task GetFeaturesForProject_AnInstanceThatSimplyHasNoLinksIsNotWarnedAt()
        {
            var logger = new RecordingLogger<JiraWorkTrackingConnector>();
            var subject = AConnectorReturning(logger, AnEpic("PROJ-1"));

            await subject.GetFeaturesForProject(JiraConnectorTestSetup.APortfolioOnJiraCloud());

            Assert.That(logger.Warnings, Is.Empty);
        }

        [Test]
        public async Task GetFeaturesForProject_AnIssueThatOnlyBlocksOthersIsNoEvidenceOfARename()
        {
            var logger = new RecordingLogger<JiraWorkTrackingConnector>();
            var subject = AConnectorReturning(logger, AnEpic("PROJ-1", BlocksLink("PROJ-9")));

            await subject.GetFeaturesForProject(JiraConnectorTestSetup.APortfolioOnJiraCloud());

            Assert.That(logger.Warnings, Is.Empty);
        }

        /// <summary>
        /// The report that names the inward links Lighthouse did see exists for the instance that renamed
        /// the one it reads. A Portfolio reading a field of its own has deliberately stopped reading links
        /// at all, so telling it its links are named wrong describes a decision it made on purpose, in the
        /// words of a misconfiguration - and points the administrator at renaming a link type to fix
        /// something that is not broken.
        ///
        /// The field being empty is the whole point of the setup. A Portfolio whose field has entries in it
        /// is waiting on something, and the report goes quiet on its own for that reason alone - so a test
        /// written that way passes just as well without any of this and proves nothing. Empty is also where
        /// an administrator setting the field up actually stands.
        /// </summary>
        [Test]
        public async Task GetFeaturesForProject_APortfolioReadingItsOwnFieldIsNotToldItsLinksAreNamedWrong()
        {
            var logger = new RecordingLogger<JiraWorkTrackingConnector>();
            var subject = AConnectorReturning(logger, AnEpic("PROJ-1", InwardLink("is halted by", "PROJ-2")));

            var portfolio = APortfolioReadingAFieldOfItsOwn();

            var features = await subject.GetFeaturesForProject(portfolio);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(features.Single().DependsOnReferences, Is.Empty,
                    "The named field is empty, so this Feature waits on nothing - the links are not a fallback.");
                Assert.That(logger.Warnings, Is.Empty,
                    "Nothing should be said about how links are named to a Portfolio that is not reading links.");
            }
        }

        /// <summary>
        /// The report is narrowed, not removed. The instance that renamed its inward link name and still
        /// reads its links is the one it was written for, and without it that instance has an empty column
        /// and nothing to go on.
        /// </summary>
        [Test]
        public async Task GetFeaturesForProject_TheSamePortfolioNamingNoFieldIsToldAboutItsLinksAgain()
        {
            var logger = new RecordingLogger<JiraWorkTrackingConnector>();
            var subject = AConnectorReturning(logger, AnEpic("PROJ-1", InwardLink("is halted by", "PROJ-2")));

            var portfolio = APortfolioReadingAFieldOfItsOwn();
            portfolio.DependencyOverrideAdditionalFieldDefinitionId = null;

            await subject.GetFeaturesForProject(portfolio);

            Assert.That(logger.Warnings.Single(), Does.Contain("is halted by"));
        }

        /// <summary>
        /// A Portfolio pointed at a field of its own. The field resolves to nothing here, which is what a
        /// Portfolio whose Features have simply not had it filled in yet looks like.
        /// </summary>
        private static Portfolio APortfolioReadingAFieldOfItsOwn()
        {
            var portfolio = JiraConnectorTestSetup.APortfolioOnJiraCloud();

            portfolio.WorkTrackingSystemConnection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition
            {
                Id = 1,
                DisplayName = "Waits On",
                Reference = "Waits On",
            });

            portfolio.DependencyOverrideAdditionalFieldDefinitionId = 1;

            return portfolio;
        }

        private static int Occurrences(string text, string needle)
            => (text.Length - text.Replace(needle, string.Empty, StringComparison.Ordinal).Length) / needle.Length;

        private static JiraWorkTrackingConnector AConnectorReturning(ILogger<JiraWorkTrackingConnector> logger, params string[] issues)
            => JiraConnectorTestSetup.AConnectorOver(
                StubTransport.RespondingWith(request => JiraWireFormat.ACloudResponseTo(request, issues)),
                logger);

        private static JiraWorkTrackingConnector AConnectorReturning(RecordedRequests recorded, params string[] issues)
            => JiraConnectorTestSetup.AConnectorOver(StubTransport.RespondingWith((request, body) =>
            {
                recorded.Record(request, body);

                return JiraWireFormat.ACloudResponseTo(request, issues);
            }));

        private static string AnEpic(string key, params string[] links) => JiraWireFormat.AnEpic(key, links);

        private static string BlockedByLink(string key) => JiraWireFormat.BlockedByLink(key);

        private static string InwardLink(string inwardName, string key) => JiraWireFormat.InwardLink(inwardName, key);

        private static string BlocksLink(string key) => JiraWireFormat.BlocksLink(key);

        private static JsonElement FieldsWith(params string[] links)
            => Fields($$"""{"issuelinks": [{{string.Join(",", links)}}]}""");

        private static JsonElement Fields(string rawFields)
            => JsonDocument.Parse(rawFields).RootElement.Clone();
    }
}
