using System.Net;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// Refreshing a Team from recorded answers of an account that reads Jira in German. The search result
    /// carries the translated name and an id; the work item must carry the name an English account sees.
    /// </summary>
    [TestFixture]
    public class JiraRefreshOriginalNamesTest
    {
        private static readonly DateTime StartedAt = new(2026, 3, 2, 9, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime ClosedAt = new(2026, 3, 9, 15, 30, 0, DateTimeKind.Utc);

        [Test]
        [Ignore("The refresh does not read the original names yet")]
        public async Task A_German_account_refreshes_work_items_in_the_original_states_and_types()
        {
            var instance = new JiraInstanceStub()
                .ReadBy(JiraWireFormat.AGermanReadersStatuses, JiraWireFormat.AGermanReadersIssueTypes)
                .Serving(
                    JiraWireFormat.AnIssueSeenIn("PAY-1", "10001", "Geschichte", "1", "Zu erledigen"),
                    JiraWireFormat.AnIssueSeenIn("PAY-2", "10001", "Geschichte", "3", "In Arbeit"),
                    JiraWireFormat.AnIssueSeenIn("PAY-3", "10002", "Fehler", "5", "Fertig"));

            var items = (await Refreshing(instance)).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(items.Select(item => item.State), Is.EqualTo(new List<string> { "To Do", "In Progress", "Done" }));
                Assert.That(items.Select(item => item.Type), Is.EqualTo(new List<string> { "Story", "Story", "Bug" }));
                Assert.That(items.Select(item => item.StateCategory), Is.EqualTo(new List<StateCategories> { StateCategories.ToDo, StateCategories.Doing, StateCategories.Done }));
            }
        }

        [Test]
        public async Task An_English_account_refreshes_the_same_work_items_the_same_way()
        {
            var instance = new JiraInstanceStub().Serving(
                JiraWireFormat.AnIssueSeenIn("PAY-1", "10001", "Story", "1", "To Do"),
                JiraWireFormat.AnIssueSeenIn("PAY-3", "10002", "Bug", "5", "Done"));

            var items = (await Refreshing(instance)).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(items.Select(item => item.State), Is.EqualTo(new List<string> { "To Do", "Done" }));
                Assert.That(items.Select(item => item.Type), Is.EqualTo(new List<string> { "Story", "Bug" }));
            }
        }

        [Test]
        [Ignore("The refresh does not read the original names yet")]
        public async Task Started_and_closed_dates_come_out_the_same_for_the_German_and_the_English_account()
        {
            var german = new JiraInstanceStub()
                .ReadBy(JiraWireFormat.AGermanReadersStatuses, JiraWireFormat.AGermanReadersIssueTypes)
                .Serving(JiraWireFormat.AnIssueSeenIn("PAY-3", "10001", "Geschichte", "5", "Fertig", TheHistory()));
            var english = new JiraInstanceStub()
                .Serving(JiraWireFormat.AnIssueSeenIn("PAY-3", "10001", "Story", "5", "Done", TheHistory()));

            var readInGerman = (await Refreshing(german)).Single();
            var readInEnglish = (await Refreshing(english)).Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(readInEnglish.ClosedDate, Is.Not.Null, "The comparison means nothing if the English read finds no dates.");
                Assert.That(readInGerman.StartedDate, Is.EqualTo(readInEnglish.StartedDate));
                Assert.That(readInGerman.ClosedDate, Is.EqualTo(readInEnglish.ClosedDate));
            }
        }

        [Test]
        public async Task A_custom_status_an_administrator_named_in_German_is_the_state_for_an_English_account()
        {
            var instance = new JiraInstanceStub()
                .ReadBy(
                    JiraWireFormat.AnEnglishStatuses.Append(JiraWireFormat.AStatus("10155", "In Prüfung", "In Prüfung", "indeterminate", "In Progress")),
                    JiraWireFormat.AnEnglishIssueTypes)
                .Serving(JiraWireFormat.AnIssueSeenIn("PAY-5", "10001", "Story", "10155", "In Prüfung"));
            var team = ATeam();
            team.DoingStates.Add("In Prüfung");

            var item = (await Refreshing(instance, team)).Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(item.State, Is.EqualTo("In Prüfung"));
                Assert.That(item.Type, Is.EqualTo("Story"));
                Assert.That(item.StateCategory, Is.EqualTo(StateCategories.Doing));
            }
        }

        [Test]
        [Ignore("The refresh does not read the original names yet")]
        public async Task A_custom_status_an_administrator_named_in_German_is_the_state_for_a_German_account()
        {
            var instance = new JiraInstanceStub()
                .ReadBy(
                    JiraWireFormat.AGermanReadersStatuses.Append(JiraWireFormat.AStatus("10155", "In Prüfung", "In Prüfung", "indeterminate", "In Arbeit")),
                    JiraWireFormat.AGermanReadersIssueTypes)
                .Serving(JiraWireFormat.AnIssueSeenIn("PAY-5", "10001", "Geschichte", "10155", "In Prüfung"));
            var team = ATeam();
            team.DoingStates.Add("In Prüfung");

            var item = (await Refreshing(instance, team)).Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(item.State, Is.EqualTo("In Prüfung"));
                Assert.That(item.Type, Is.EqualTo("Story"));
                Assert.That(item.StateCategory, Is.EqualTo(StateCategories.Doing));
            }
        }

        [Test]
        [Ignore("The refresh does not read the original names yet")]
        public async Task A_status_that_appears_after_the_lists_are_read_again_is_resolved_with_one_extra_read_of_each_list()
        {
            var instance = new JiraInstanceStub()
                .ReadBy(JiraWireFormat.AGermanReadersStatuses, JiraWireFormat.AGermanReadersIssueTypes)
                .Serving(JiraWireFormat.AnIssueSeenIn("PAY-5", "10001", "Geschichte", "10155", "In Prüfung"));
            instance.StatusListOnReread = (HttpStatusCode.OK, JiraWireFormat.TheStatusListOf(
                JiraWireFormat.AGermanReadersStatuses.Append(JiraWireFormat.AStatus("10155", "In Prüfung", "In Prüfung", "indeterminate", "In Arbeit"))));
            var team = ATeam();
            team.DoingStates.Add("In Prüfung");

            var item = (await Refreshing(instance, team)).Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(item.State, Is.EqualTo("In Prüfung"));
                Assert.That(instance.ReadsOf(JiraWireFormat.StatusListEndpoint), Is.EqualTo(2));
                Assert.That(instance.ReadsOf(JiraWireFormat.IssueTypeListEndpoint), Is.EqualTo(2));
            }
        }

        [Test]
        [Ignore("The refresh does not read the original names yet")]
        public async Task A_second_missing_id_in_the_same_refresh_fails_without_a_third_read()
        {
            var instance = new JiraInstanceStub().ReadBy(JiraWireFormat.AGermanReadersStatuses, JiraWireFormat.AGermanReadersIssueTypes);
            instance.SearchPages.Add([JiraWireFormat.AnIssueSeenIn("PAY-5", "10001", "Geschichte", "10155", "In Prüfung")]);
            instance.SearchPages.Add([JiraWireFormat.AnIssueSeenIn("PAY-6", "10001", "Geschichte", "10156", "Wird überprüft")]);
            instance.StatusListOnReread = (HttpStatusCode.OK, JiraWireFormat.TheStatusListOf(
                JiraWireFormat.AGermanReadersStatuses.Append(JiraWireFormat.AStatus("10155", "In Prüfung", "In Prüfung", "indeterminate", "In Arbeit"))));

            var stopped = await Assert.ThrowsAsync<JiraReadException>(() => Refreshing(instance));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stopped!.Verdict.TechnicalDetails, Does.Contain("10156"));
                Assert.That(instance.ReadsOf(JiraWireFormat.StatusListEndpoint), Is.EqualTo(2));
                Assert.That(instance.ReadsOf(JiraWireFormat.IssueTypeListEndpoint), Is.EqualTo(2));
            }
        }

        [Test]
        [Ignore("The refresh does not read the lists on an unidentified deployment yet")]
        public async Task A_deployment_that_cannot_be_identified_reads_the_lists_and_gives_a_German_account_the_original_names()
        {
            var instance = new JiraInstanceStub("Unknown")
                .ReadBy(JiraWireFormat.AGermanReadersStatuses, JiraWireFormat.AGermanReadersIssueTypes)
                .Serving(JiraWireFormat.AnIssueSeenIn("PAY-2", "10001", "Geschichte", "3", "In Arbeit"));

            var item = (await Refreshing(instance)).Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(item.State, Is.EqualTo("In Progress"));
                Assert.That(item.Type, Is.EqualTo("Story"));
                Assert.That(instance.ReadsOf(JiraWireFormat.StatusListEndpoint), Is.EqualTo(1));
            }
        }

        [Test]
        [Ignore("The refresh does not read the lists on an unidentified deployment yet")]
        public async Task A_deployment_that_cannot_be_identified_uses_the_name_of_an_entry_that_has_no_original_name()
        {
            var statuses = new[] { JiraWireFormat.AStatus("3", "In Progress", null, null, "In Progress") };
            var issueTypes = new[] { JiraWireFormat.AnIssueType("10001", "Story", null) };
            var instance = new JiraInstanceStub("Unknown")
                .ReadBy(statuses, issueTypes)
                .Serving(JiraWireFormat.AnIssueSeenIn("PAY-2", "10001", "Story", "3", "In Progress"));

            var item = (await Refreshing(instance)).Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(item.State, Is.EqualTo("In Progress"));
                Assert.That(item.Type, Is.EqualTo("Story"));
                Assert.That(instance.ReadsOf(JiraWireFormat.StatusListEndpoint), Is.EqualTo(1));
            }
        }

        [Test]
        public async Task Data_Center_is_read_as_before_without_asking_for_the_lists()
        {
            var instance = new JiraInstanceStub("Server").Serving(
                JiraWireFormat.AnIssueSeenIn("PAY-1", "10001", "Story", "3", "In Progress"));

            var item = (await Refreshing(instance)).Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(item.State, Is.EqualTo("In Progress"));
                Assert.That(item.Type, Is.EqualTo("Story"));
                Assert.That(instance.ReadsOf(JiraWireFormat.StatusListEndpoint), Is.Zero);
                Assert.That(instance.ReadsOf(JiraWireFormat.IssueTypeListEndpoint), Is.Zero);
            }
        }

        [Test]
        [Ignore("The refresh does not read the lists yet")]
        public async Task A_refresh_reads_each_list_once_however_many_pages_the_search_has()
        {
            var instance = new JiraInstanceStub();
            instance.SearchPages.Add([JiraWireFormat.AnIssueSeenIn("PAY-1", "10001", "Story", "1", "To Do")]);
            instance.SearchPages.Add([JiraWireFormat.AnIssueSeenIn("PAY-2", "10001", "Story", "3", "In Progress")]);
            instance.SearchPages.Add([JiraWireFormat.AnIssueSeenIn("PAY-3", "10001", "Story", "5", "Done")]);

            var items = await Refreshing(instance);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(items.Count(), Is.EqualTo(3));
                Assert.That(instance.ReadsOf(JiraWireFormat.StatusListEndpoint), Is.EqualTo(1));
                Assert.That(instance.ReadsOf(JiraWireFormat.IssueTypeListEndpoint), Is.EqualTo(1));
            }
        }

        [Test]
        [Ignore("The refresh does not read the lists yet")]
        public async Task A_download_by_key_split_into_several_queries_reads_each_list_once()
        {
            var instance = new JiraInstanceStub().Serving(JiraWireFormat.AnIssueSeenIn("PAY-1", "10001", "Story", "1", "To Do"));
            var keys = Enumerable.Range(1, 450).Select(number => $"PAY-{number}").ToList();

            await JiraConnectorTestSetup.AConnectorOver(instance.Handler())
                .GetWorkItemsForTeam(ATeam(), keys, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(instance.SearchBodies, Has.Count.GreaterThan(1), "The keys have to span several queries for the count to mean anything.");
                Assert.That(instance.ReadsOf(JiraWireFormat.StatusListEndpoint), Is.EqualTo(1));
                Assert.That(instance.ReadsOf(JiraWireFormat.IssueTypeListEndpoint), Is.EqualTo(1));
            }
        }

        [Test]
        public async Task Checking_what_changed_makes_no_call_for_the_lists()
        {
            var instance = new JiraInstanceStub().Serving(JiraWireFormat.AnIssueSeenIn("PAY-1", "10001", "Story", "1", "To Do"));

            var team = ATeam();
            await Refreshing(instance, team);
            var statusReadsBefore = instance.ReadsOf(JiraWireFormat.StatusListEndpoint);
            var issueTypeReadsBefore = instance.ReadsOf(JiraWireFormat.IssueTypeListEndpoint);

            var stamps = await JiraConnectorTestSetup.AConnectorOver(instance.Handler())
                .SweepWorkItemsForTeam(team, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stamps, Has.Count.EqualTo(1));
                Assert.That(instance.ReadsOf(JiraWireFormat.StatusListEndpoint), Is.EqualTo(statusReadsBefore));
                Assert.That(instance.ReadsOf(JiraWireFormat.IssueTypeListEndpoint), Is.EqualTo(issueTypeReadsBefore));
            }
        }

        [Test]
        [Ignore("The refresh does not read the lists yet")]
        public async Task A_list_that_could_not_be_read_is_asked_for_again_on_the_next_refresh()
        {
            var instance = new JiraInstanceStub().Serving(JiraWireFormat.AnIssueSeenIn("PAY-1", "10001", "Story", "1", "To Do"));
            instance.StatusList = (HttpStatusCode.ServiceUnavailable, "{}");
            var connector = JiraConnectorTestSetup.AConnectorOver(instance.Handler());
            var team = ATeam();

            await Assert.ThrowsAsync<JiraReadException>(
                () => connector.GetWorkItemsForTeam(team, CancellationToken.None));

            instance.StatusList = (HttpStatusCode.OK, JiraWireFormat.TheStatusListOf(JiraWireFormat.AnEnglishStatuses));

            var items = await connector.GetWorkItemsForTeam(team, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(items.Count(), Is.EqualTo(1));
                Assert.That(instance.ReadsOf(JiraWireFormat.StatusListEndpoint), Is.EqualTo(2));
            }
        }

        [Test]
        public async Task The_query_built_from_the_configured_names_is_the_same_under_any_account_language()
        {
            var german = new JiraInstanceStub().ReadBy(JiraWireFormat.AGermanReadersStatuses, JiraWireFormat.AGermanReadersIssueTypes);
            var english = new JiraInstanceStub();

            await Refreshing(german);
            await Refreshing(english);

            var jql = JiraWireFormat.JqlOfSearch(german.SearchBodies.Single());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(jql, Is.EqualTo(TheExpectedQuery));
                Assert.That(JiraWireFormat.JqlOfSearch(english.SearchBodies.Single()), Is.EqualTo(jql));
            }
        }

        private const string TheExpectedQuery =
            "(project = PROJ) AND (issuetype = \"Story\")  AND (status = \"To Do\" OR status = \"In Progress\" OR status = \"Done\")  ";

        private static string[] TheHistory()
            =>
            [
                JiraWireFormat.AStatusChangeAt(StartedAt, "To Do", "In Progress"),
                JiraWireFormat.AStatusChangeAt(ClosedAt, "In Progress", "Done"),
            ];

        private static Team ATeam() => JiraConnectorTestSetup.ATeamOnJiraCloud();

        private static Task<IEnumerable<WorkItem>> Refreshing(JiraInstanceStub instance, Team? team = null)
            => JiraConnectorTestSetup.AConnectorOver(instance.Handler()).GetWorkItemsForTeam(team ?? ATeam(), CancellationToken.None);
    }
}
