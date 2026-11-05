using System.Net;
using System.Text;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Tests.TestHelpers;
using Moq;
using Moq.Protected;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// What a Jira refresh hands over about each record's parent: one reading per position of the owner's
    /// list, in the list's order, read through the whole connector from what Jira answers. Choosing among
    /// them is the refresh's business, so a reading only says what its source found.
    /// </summary>
    [TestFixture]
    public class JiraParentReadingsTest
    {
        private const int IsCausedByDefinition = 11;

        private const int ProgramRefDefinition = 12;

        private const int NothingTheInstanceKnowsDefinition = 13;

        private const string Item = "PAY-142";

        private static readonly JiraLinkType Causes = new("Causes", "is caused by", "causes");

        private static readonly string ProgramRefField = JiraWireFormat.TheCustomFieldIdAt(0);

        private static readonly string EpicLinkField = JiraWireFormat.TheCustomFieldIdAt(1);

        private static readonly string ParentLinkField = JiraWireFormat.TheCustomFieldIdAt(2);

        private static readonly string[] TheFieldsTheInstanceDefines = ["Program Ref", "Epic Link", "Parent Link"];

        private static readonly string[] TwoCandidates = ["PAY-7", "PAY-8"];

        private static readonly ParentSourceEntry OwnParent = ParentSourceEntry.TrackerParent;

        private static readonly ParentSourceEntry IsCausedBy = ParentSourceEntry.Field(IsCausedByDefinition);

        private static readonly ParentSourceEntry ProgramRef = ParentSourceEntry.Field(ProgramRefDefinition);

        private static readonly ParentSourceEntry NothingTheInstanceKnows = ParentSourceEntry.Field(NothingTheInstanceKnowsDefinition);

        private static readonly ParentSourceEntry[] ALinkAFieldAndTheOwnParent = [IsCausedBy, ProgramRef, OwnParent];

        private static string Value(string fieldId, string key) => $", \"{fieldId}\": \"{key}\"";

        private static string Parent(string key) => $", \"parent\": {{\"key\": \"{key}\"}}";

        [Test]
        public async Task Every_Work_Item_carries_one_reading_per_list_position_a_link_entry_with_its_candidates()
        {
            var furtherFields = Value(ProgramRefField, "PAY-12") + Parent("PAY-1");

            var readings = await TheReadingsATeamGets(ALinkAFieldAndTheOwnParent, furtherFields, Causes.LinkWhoseInwardIssueIs("PAY-8"), Causes.LinkWhoseInwardIssueIs("PAY-7"));

            AssertAllThreeWereRead(readings);
        }

        [Test]
        public async Task Every_Feature_carries_one_reading_per_list_position_a_link_entry_with_its_candidates()
        {
            var furtherFields = Value(ProgramRefField, "PAY-12") + Parent("PAY-1");

            var readings = await TheReadingsAPortfolioGets(ALinkAFieldAndTheOwnParent, furtherFields, Causes.LinkWhoseInwardIssueIs("PAY-8"), Causes.LinkWhoseInwardIssueIs("PAY-7"));

            AssertAllThreeWereRead(readings);
        }

        private static void AssertAllThreeWereRead(ParentReadings readings)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(readings.Positions.Select(position => position.Source), Is.EqualTo(ALinkAFieldAndTheOwnParent), "The readings keep the list's order.");
                Assert.That(readings.Positions[0].Reading.Kind, Is.EqualTo(ParentReadingKind.LinkCandidates));
                Assert.That(readings.Positions[0].Reading.Links.Candidates, Is.EqualTo(TwoCandidates));
                Assert.That(readings.Positions[1].Reading, Is.EqualTo(ParentReading.FieldValue("PAY-12")));
                Assert.That(readings.Positions[2].Reading, Is.EqualTo(ParentReading.TrackersOwn("PAY-1")));
            }
        }

        /// <summary>
        /// A link entry stays a link entry when no link answers it. The field that shows its key is found by
        /// that kind, so a record that lost its last link would otherwise go on showing the old key.
        /// </summary>
        [Test]
        public async Task A_link_entry_with_no_matching_link_reads_as_links_naming_nobody()
        {
            var readings = await TheReadingsATeamGets([IsCausedBy], string.Empty, JiraWireFormat.BlockedByLink("PAY-7"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(readings.Positions[0].Reading.Kind, Is.EqualTo(ParentReadingKind.LinkCandidates));
                Assert.That(readings.Positions[0].Reading.Links.Candidates, Is.Empty);
            }
        }

        private static IEnumerable<TestCaseData> FieldsThatFoundNothing()
        {
            yield return new TestCaseData(ProgramRef, string.Empty).SetName("A field the issue does not carry reads as no value");
            yield return new TestCaseData(ProgramRef, Value(ProgramRefField, string.Empty)).SetName("A field left empty reads as no value");
            yield return new TestCaseData(ProgramRef, $", \"{ProgramRefField}\": null").SetName("A field Jira writes as null reads as no value");
            yield return new TestCaseData(NothingTheInstanceKnows, string.Empty).SetName("A field the instance does not define reads as no value");
            yield return new TestCaseData(ParentSourceEntry.Field(99), string.Empty).SetName("An entry naming a field that is gone reads as no value");
        }

        [TestCaseSource(nameof(FieldsThatFoundNothing))]
        public async Task A_field_entry_that_found_nothing_reads_as_no_value(ParentSourceEntry entry, string furtherFields)
        {
            var readings = await TheReadingsATeamGets([entry], furtherFields);

            Assert.That(readings.Positions[0].Reading, Is.EqualTo(ParentReading.NoValue));
        }

        private static IEnumerable<TestCaseData> WhatATeamsOwnParentReads()
        {
            yield return new TestCaseData(Parent("PAY-1"), ParentReading.TrackersOwn("PAY-1")).SetName("A Team reads the key Jira names");
            yield return new TestCaseData(Value(EpicLinkField, "PAY-2"), ParentReading.TrackersOwn("PAY-2")).SetName("A Team with no key reads the Epic Link");
            yield return new TestCaseData(Parent("PAY-1") + Value(EpicLinkField, "PAY-2"), ParentReading.TrackersOwn("PAY-1")).SetName("A Team reads the key ahead of the Epic Link");
            yield return new TestCaseData(Value(ParentLinkField, "PAY-3"), ParentReading.NoValue).SetName("A Team does not read the Parent Link");
            yield return new TestCaseData(string.Empty, ParentReading.NoValue).SetName("A Team with neither reads no value");
        }

        [TestCaseSource(nameof(WhatATeamsOwnParentReads))]
        public async Task A_Teams_own_parent_reading_takes_the_key_then_the_instances_field(string furtherFields, ParentReading expected)
        {
            var readings = await TheReadingsATeamGets([OwnParent], furtherFields);

            Assert.That(readings.Positions[0].Reading, Is.EqualTo(expected));
        }

        private static IEnumerable<TestCaseData> WhatAPortfoliosOwnParentReads()
        {
            yield return new TestCaseData(Parent("PAY-1"), ParentReading.TrackersOwn("PAY-1")).SetName("A Portfolio reads the key Jira names");
            yield return new TestCaseData(Value(ParentLinkField, "PAY-3"), ParentReading.TrackersOwn("PAY-3")).SetName("A Portfolio with no key reads the Parent Link");
            yield return new TestCaseData(Parent("PAY-1") + Value(ParentLinkField, "PAY-3"), ParentReading.TrackersOwn("PAY-1")).SetName("A Portfolio reads the key ahead of the Parent Link");
            yield return new TestCaseData(Value(EpicLinkField, "PAY-2"), ParentReading.NoValue).SetName("A Portfolio does not read the Epic Link");
            yield return new TestCaseData(string.Empty, ParentReading.NoValue).SetName("A Portfolio with neither reads no value");
        }

        [TestCaseSource(nameof(WhatAPortfoliosOwnParentReads))]
        public async Task A_Portfolios_own_parent_reading_takes_the_key_then_the_instances_field(string furtherFields, ParentReading expected)
        {
            var readings = await TheReadingsAPortfolioGets([OwnParent], furtherFields);

            Assert.That(readings.Positions[0].Reading, Is.EqualTo(expected));
        }

        /// <summary>
        /// A list that leaves the own parent out gets no reading of it, however much the issue says about it.
        /// </summary>
        [Test]
        public async Task A_list_without_the_own_parent_reads_neither_the_key_nor_the_instances_field()
        {
            var furtherFields = Value(ProgramRefField, "PAY-12") + Parent("PAY-1") + Value(EpicLinkField, "PAY-2");

            var readings = await TheReadingsATeamGets([ProgramRef], furtherFields);

            Assert.That(readings.Positions, Is.EqualTo(new[] { new ParentReadingAt(ProgramRef, ParentReading.FieldValue("PAY-12")) }));
        }

        /// <summary>
        /// An owner that never chose anything reads the tracker's own parent, so it gets that one reading.
        /// </summary>
        [Test]
        public async Task An_owner_that_chose_nothing_reads_the_own_parent_alone()
        {
            var team = JiraConnectorTestSetup.ATeamOnJiraCloud();
            DefineTheAdditionalFields(team.WorkTrackingSystemConnection);

            var readings = await TheReadingsOf(team, Parent("PAY-1"));

            Assert.That(readings.Positions, Is.EqualTo(new[] { new ParentReadingAt(OwnParent, ParentReading.TrackersOwn("PAY-1")) }));
        }

        private static async Task<ParentReadings> TheReadingsATeamGets(ParentSourceEntry[] entries, string furtherFields, params string[] links)
        {
            var team = JiraConnectorTestSetup.ATeamOnJiraCloud();
            DefineTheAdditionalFields(team.WorkTrackingSystemConnection);
            team.OwnParentSources(ParentSources.Of(entries).List!);

            return await TheReadingsOf(team, furtherFields, links);
        }

        private static async Task<ParentReadings> TheReadingsOf(Team team, string furtherFields, params string[] links)
        {
            var connector = JiraConnectorTestSetup.AConnectorOver(AJiraServing(JiraWireFormat.AnIssueCarrying(Item, "Story", furtherFields, links)));

            var workItems = await connector.GetWorkItemsForTeam(team, CancellationToken.None);

            return workItems.Single().ParentReadings;
        }

        private static async Task<ParentReadings> TheReadingsAPortfolioGets(ParentSourceEntry[] entries, string furtherFields, params string[] links)
        {
            var portfolio = JiraConnectorTestSetup.APortfolioOnJiraCloud();
            DefineTheAdditionalFields(portfolio.WorkTrackingSystemConnection);
            portfolio.OwnParentSources(ParentSources.Of(entries).List!);

            var connector = JiraConnectorTestSetup.AConnectorOver(AJiraServing(JiraWireFormat.AnIssueCarrying(Item, "Epic", furtherFields, links)));

            var features = await connector.GetFeaturesForProject(portfolio, CancellationToken.None);

            return features.Single().ParentReadings;
        }

        private static void DefineTheAdditionalFields(WorkTrackingSystemConnection connection)
        {
            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = IsCausedByDefinition, DisplayName = "Is caused by", Reference = Causes.Inward });
            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = ProgramRefDefinition, DisplayName = "Program Ref", Reference = "Program Ref" });
            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = NothingTheInstanceKnowsDefinition, DisplayName = "Long gone", Reference = "Long gone" });
        }

        private static HttpMessageHandler AJiraServing(string issue)
        {
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((request, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(TheAnswerTo(request.RequestUri?.AbsolutePath ?? string.Empty, issue), Encoding.UTF8, "application/json"),
                }));

            return handler.Object;
        }

        private static string TheAnswerTo(string path, string issue) => path switch
        {
            _ when path.EndsWith(JiraWireFormat.ServerInfoEndpoint, StringComparison.Ordinal) => JiraWireFormat.ACloudDeployment,
            _ when JiraWireFormat.TheNameListAnsweredAt(path) is { } theNameList => theNameList,
            _ when path.EndsWith(JiraWireFormat.FieldListEndpoint, StringComparison.Ordinal) => JiraWireFormat.TheFieldListDefining(TheFieldsTheInstanceDefines),
            _ when path.EndsWith(JiraWireFormat.IssueLinkTypeEndpoint, StringComparison.Ordinal) => JiraWireFormat.TheLinkTypeListDefining([JiraWireFormat.ALinkTypeDefinition(0, Causes)]),
            _ when path.Contains(JiraWireFormat.SearchEndpoint, StringComparison.Ordinal) => JiraWireFormat.OnePageOf([issue]),
            _ => "{}",
        };
    }
}
