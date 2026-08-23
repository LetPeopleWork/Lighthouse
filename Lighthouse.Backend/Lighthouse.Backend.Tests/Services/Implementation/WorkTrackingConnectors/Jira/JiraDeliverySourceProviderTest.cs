using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Lighthouse.Backend.Cache;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliverySources;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Csv;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// Which delivery sources a work tracking connection offers a Delivery to bind its date to.
    ///
    /// Kept out of JiraWorkTrackingConnectorTest on purpose: that class is marked as a live-Jira
    /// integration suite, and the test filter every developer and the build server use excludes it,
    /// so a specification written there would silently never run.
    ///
    /// The subject here is built from mocks and handed a connection with no url and no credentials.
    /// Any attempt to reach a real Jira would fail rather than pass, which is how these specifications
    /// hold the answer to being computed rather than fetched.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5565-delivery-date-sync")]
    [Category("slice-01a")]
    public class JiraDeliverySourceProviderTest
    {
        private static readonly Type[] SystemsThatOfferNothing =
        [
            typeof(AzureDevOpsWorkTrackingConnector),
            typeof(CsvWorkTrackingConnector),
            typeof(LinearWorkTrackingConnector),
            typeof(ServiceNowWorkTrackingConnector),
        ];

        private const string JiraReleaseSourceKey = "jira-release";

        private const string TheDatedRelease = "10004";
        private const string TheDatedReleaseName = "Release 1.0";
        private const string TheUndatedRelease = "10005";
        private const string TheUndatedReleaseName = "Release 2.0";
        private const string TheDeletedRelease = "10006";
        private const string TheShippedRelease = "10009";

        private const string TheReleaseProject = "REL";
        private const string TheWorkProject = "PROJ";
        private const string TheLockedProject = "LOCKED";

        private const string TheDayTheDatedReleaseShipsInJira = "2026-08-22";

        private static readonly DeliverySourceProject TheDemoProject = new("LGH", "Lighthouse Demo");

        private static readonly DateTime TheDayTheDatedReleaseShips = new(2026, 8, 22, 0, 0, 0, DateTimeKind.Utc);

        private static readonly string[] TheDatedReleaseOnItsOwn = [TheDatedRelease];
        private static readonly string[] TheUndatedReleaseOnItsOwn = [TheUndatedRelease];
        private static readonly string[] TheShippedReleaseOnItsOwn = [TheShippedRelease];
        private static readonly string[] ThreeDatedReleases = [TheDatedRelease, "10007", "10008"];
        private static readonly string[] TwoProjectsThatBothNameARelease44 = ["PROJ", "REL"];
        private static readonly string[] TheReleasesOfBothProjects = [TheDatedRelease, TheUndatedRelease];
        private static readonly string[] TheWorkOnTheDatedRelease = ["LGH-1", "LGH-2"];
        private static readonly string[] TheWorkOnAShippedRelease = ["LGH-1"];

        /// <summary>
        /// The version objects are copied from what a real Jira answered on 2026-08-22, inside the page
        /// wrapper the endpoint puts around them. The middle entry has no releaseDate key at all - not a
        /// null, the key is simply absent - which is how Jira reports a Release nobody dated, and how two
        /// of the three Releases on that instance came back.
        /// </summary>
        private const string CapturedVersionsPayload = """
            {
              "isLast": true,
              "values": [
                {
                  "self": "https://example.atlassian.net/rest/api/3/version/10004",
                  "id": "10004",
                  "name": "Release 1.0",
                  "archived": false,
                  "released": true,
                  "releaseDate": "2026-08-22",
                  "projectId": 10001
                },
                {
                  "self": "https://example.atlassian.net/rest/api/3/version/10005",
                  "id": "10005",
                  "name": "Release 2.0",
                  "archived": false,
                  "released": false,
                  "projectId": 10001
                },
                {
                  "self": "https://example.atlassian.net/rest/api/3/version/10006",
                  "id": "10006",
                  "name": "Release 0.9",
                  "archived": true,
                  "released": true,
                  "releaseDate": "2025-01-15",
                  "projectId": 10001
                }
              ]
            }
            """;

        [Test]
        public void A_Jira_connection_offers_its_Releases_as_a_delivery_source()
        {
            var subject = CreateSubject();

            var sources = subject.AvailableSources(UnreachableJiraConnection());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(sources, Has.Count.EqualTo(1),
                    "Jira offers exactly one thing a Delivery date can be bound to today.");
                Assert.That(sources[0].Key, Is.EqualTo("jira-release"),
                    "the key travels in a url and is what a create payload names, so it stays lowercase and stable.");
                Assert.That(sources[0].DisplayName, Is.EqualTo("Jira Release"),
                    "Release is Jira's own word for what is being bound, so it is never renamed to the tenant's vocabulary.");
            }
        }

        [Test]
        public void The_other_work_tracking_systems_offer_no_delivery_sources_at_all()
        {
            var offering = SystemsThatOfferNothing
                .Where(connector => connector.IsAssignableTo(typeof(IDeliverySourceProvider)))
                .ToArray();

            Assert.That(offering, Is.Empty,
                "a system that cannot offer sources says so by not implementing the capability; there is no flag to switch off and no registry to stay out of.");
        }

        [Test]
        public void A_Release_with_no_release_date_is_offered_but_cannot_be_selected()
        {
            var (options, _) = JiraReleaseVersionReader.ReadOptionPage(CapturedVersionsPayload, TheDemoProject);

            var undated = options.Single(option => option.Id == TheUndatedRelease);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(undated.Name, Is.EqualTo("Release 2.0"),
                    "a Release nobody dated is still worth showing, so the reader has to get this far rather than give up on the entry.");
                Assert.That(undated.Date, Is.Null,
                    "Jira leaves the key out rather than sending null, and a missing key means no date - never a payload the reader failed to read.");
                Assert.That(undated.IsSelectable, Is.False);
                Assert.That(undated.BlockedBecause, Is.EqualTo(SourceOptionBlockReason.NoDateSet),
                    "the way out of this one is to set a date in Jira, which is a different errand than picking another Release.");
            }
        }

        [Test]
        public void A_Release_that_was_archived_in_Jira_cannot_be_selected()
        {
            var (options, _) = JiraReleaseVersionReader.ReadOptionPage(CapturedVersionsPayload, TheDemoProject);

            var archived = options.Single(option => option.Id == TheDeletedRelease);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(archived.IsRetiredAtSource, Is.True);
                Assert.That(archived.IsSelectable, Is.False);
                Assert.That(archived.BlockedBecause, Is.EqualTo(SourceOptionBlockReason.RetiredAtSource),
                    "an archived Release carries a date and is still refused, so having a date is not on its own enough. The picker never shows one, but a request that never went through the picker still arrives here.");
            }
        }

        [Test]
        public async Task A_Release_that_already_shipped_is_not_offered_at_all()
        {
            var jira = AJira()
                .WithTheReleaseProject()
                .WithReleaseIn(TheReleaseProject, TheDatedRelease, TheDatedReleaseName, TheDayTheDatedReleaseShipsInJira)
                .WithShippedReleaseIn(TheReleaseProject, TheShippedRelease, "Release 0.8", "2026-07-01");

            var subject = JiraConnectorTestSetup.AConnectorOver(jira.Handler);
            var options = await subject.GetOptions(AJiraCloudConnection(), JiraReleaseSourceKey);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(options.Select(option => option.Id), Is.EquivalentTo(TheDatedReleaseOnItsOwn),
                    "a Release that has already shipped has nothing left to forecast, so offering it only invites someone to bind a Delivery to a date that will never move again.");
                Assert.That(QueryValue(jira.VersionListRequests.Single(), "status"), Is.EqualTo("unreleased"),
                    "Jira is asked to leave them out rather than being asked for everything and then having some dropped here.");
            }
        }

        [Test]
        public void A_source_key_the_connection_never_offered_is_refused_before_Jira_is_asked()
        {
            var subject = CreateSubject();

            Assert.ThrowsAsync<ArgumentException>(
                async () => await subject.GetOptions(UnreachableJiraConnection(), "jira-sprint"),
                "the connection has no url and no credentials, so anything that reached the network would fail differently.");
        }

        [Test]
        public async Task Releases_are_gathered_from_every_project_the_credential_can_see()
        {
            var jira = AJira()
                .WithTheWorkProject()
                .WithTheReleaseProject()
                .WithReleaseIn(TheReleaseProject, TheDatedRelease, TheDatedReleaseName, TheDayTheDatedReleaseShipsInJira);

            var subject = JiraConnectorTestSetup.AConnectorOver(jira.Handler);
            var options = await subject.GetOptions(AJiraCloudConnection(), JiraReleaseSourceKey);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(options.Select(option => option.Id), Is.EquivalentTo(TheDatedReleaseOnItsOwn),
                    "a customer may coordinate its releases in a project holding no work at all, so a Release is never looked for only where the Features are.");
                Assert.That(jira.VersionListRequests, Has.Count.EqualTo(2),
                    "every visible project is asked, because nothing in the Portfolio says which one carries the Releases.");
            }
        }

        [Test]
        public async Task Two_Releases_that_share_a_name_in_different_projects_stay_told_apart()
        {
            var jira = AJira()
                .WithTheWorkProject()
                .WithTheReleaseProject()
                .WithReleaseIn(TheWorkProject, "10101", "Release 44", "2026-09-01")
                .WithReleaseIn(TheReleaseProject, "10202", "Release 44", "2026-09-01");

            var subject = JiraConnectorTestSetup.AConnectorOver(jira.Handler);
            var options = await subject.GetOptions(AJiraCloudConnection(), JiraReleaseSourceKey);

            Assert.That(options.Select(option => option.Project.Key), Is.EquivalentTo(TwoProjectsThatBothNameARelease44),
                "the two rows read identically otherwise, so without the project the reader picks one of them at random.");
        }

        [Test]
        public async Task A_Release_somebody_archived_is_not_offered_at_all()
        {
            var jira = AJira()
                .WithTheReleaseProject()
                .WithReleaseIn(TheReleaseProject, TheDatedRelease, TheDatedReleaseName, TheDayTheDatedReleaseShipsInJira)
                .WithArchivedReleaseIn(TheReleaseProject, TheDeletedRelease, "Release 0.9", "2025-01-15");

            var subject = JiraConnectorTestSetup.AConnectorOver(jira.Handler);
            var options = await subject.GetOptions(AJiraCloudConnection(), JiraReleaseSourceKey);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(options.Select(option => option.Id), Is.EquivalentTo(TheDatedReleaseOnItsOwn),
                    "a reader cannot un-archive a Release from here, so a row they can do nothing about is only noise - unlike a dateless one, which they can go and date.");
                Assert.That(QueryValue(jira.VersionListRequests.Single(), "status"), Is.EqualTo("unreleased"),
                    "Jira is asked to leave them out rather than being asked for everything and then having some dropped here.");
            }
        }

        [Test]
        public async Task A_Release_nobody_dated_is_still_offered_even_though_it_cannot_be_picked()
        {
            var jira = AJira()
                .WithTheReleaseProject()
                .WithReleaseIn(TheReleaseProject, TheUndatedRelease, TheUndatedReleaseName, releaseDate: null);

            var subject = JiraConnectorTestSetup.AConnectorOver(jira.Handler);
            var options = await subject.GetOptions(AJiraCloudConnection(), JiraReleaseSourceKey);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(options.Select(option => option.Id), Is.EquivalentTo(TheUndatedReleaseOnItsOwn),
                    "a missing date is something the reader can go and fix and come straight back to; archived and released both say the Release is finished with, which is why only those two are hidden.");
                Assert.That(options.Single().BlockedBecause, Is.EqualTo(SourceOptionBlockReason.NoDateSet));
            }
        }

        [Test]
        public async Task A_project_carrying_more_Releases_than_one_page_offers_all_of_them()
        {
            var jira = AJira()
                .WithTheReleaseProject()
                .WithReleaseIn(TheReleaseProject, TheDatedRelease, TheDatedReleaseName, TheDayTheDatedReleaseShipsInJira)
                .WithReleaseIn(TheReleaseProject, "10007", "Release 2.0", "2026-09-01");
            jira.VersionsPerPage = 1;

            var subject = JiraConnectorTestSetup.AConnectorOver(jira.Handler);
            var options = await subject.GetOptions(AJiraCloudConnection(), JiraReleaseSourceKey);

            Assert.That(options, Has.Count.EqualTo(2),
                "a long-lived project carries hundreds of Releases, so stopping at the first page would quietly hide most of them.");
        }

        [Test]
        public async Task A_project_whose_Releases_cannot_be_read_costs_the_reader_only_that_project()
        {
            var jira = AJira()
                .WithTheWorkProject()
                .WithProject(TheLockedProject, "Someone else's project")
                .WithReleaseIn(TheWorkProject, TheDatedRelease, TheDatedReleaseName, TheDayTheDatedReleaseShipsInJira)
                .WithUnreadableVersionsIn(TheLockedProject);

            var subject = JiraConnectorTestSetup.AConnectorOver(jira.Handler);
            var options = await subject.GetOptions(AJiraCloudConnection(), JiraReleaseSourceKey);

            Assert.That(options.Select(option => option.Id), Is.EquivalentTo(TheDatedReleaseOnItsOwn),
                "one project the credential may list but not read must not take the whole picker away from a reader with perfectly good projects to choose from.");
        }

        [Test]
        public async Task Two_requests_in_quick_succession_ask_Jira_once()
        {
            var jira = AJira()
                .WithTheWorkProject()
                .WithReleaseIn(TheWorkProject, TheDatedRelease, TheDatedReleaseName, TheDayTheDatedReleaseShipsInJira);

            using var application = AnApplicationServingRequestsOver(jira.Handler);
            var connection = AJiraCloudConnection();

            var (firstConnector, _) = await OptionsInOneRequest(application, connection);
            var (secondConnector, _) = await OptionsInOneRequest(application, connection);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(secondConnector, Is.Not.SameAs(firstConnector),
                    "the second request has to be served by a second connector, or nothing here says anything about what one request can remember for the next.");
                Assert.That(jira.VersionListRequests, Has.Count.EqualTo(1),
                    "opening the form, closing it and opening it again costs one call per project every time it is not remembered, and an instance with hundreds of projects is the normal case.");
            }
        }

        [Test]
        public async Task A_list_missing_a_project_is_shown_once_and_never_remembered()
        {
            var jira = AJira()
                .WithTheWorkProject()
                .WithTheReleaseProject()
                .WithReleaseIn(TheWorkProject, TheDatedRelease, TheDatedReleaseName, TheDayTheDatedReleaseShipsInJira)
                .WithReleaseIn(TheReleaseProject, TheUndatedRelease, TheUndatedReleaseName, null)
                .WithUnreadableVersionsIn(TheReleaseProject);

            using var application = AnApplicationServingRequestsOver(jira.Handler);
            var connection = AJiraCloudConnection();

            await OptionsInOneRequest(application, connection);
            jira.LetsItsVersionsBeReadAgainIn(TheReleaseProject);
            var (_, options) = await OptionsInOneRequest(application, connection);

            Assert.That(options.Select(option => option.Id), Is.EquivalentTo(TheReleasesOfBothProjects),
                "one project refusing for a moment must not leave the picker short for the whole cache lifetime - closing the form and opening it again is the one thing a reader will try, and it has to help.");
        }

        [Test]
        public async Task A_connection_carrying_a_url_that_is_not_one_offers_nothing_rather_than_failing()
        {
            var subject = JiraConnectorTestSetup.AConnectorOver(AJira().WithTheWorkProject().Handler);

            var options = await subject.GetOptions(AConnectionWhoseUrlIsNotAUrl(), JiraReleaseSourceKey);

            Assert.That(options, Is.Empty,
                "the picker asks what can be bound and has to be told 'nothing'; a connection nobody can build a request from is one more way of answering that, not a request that failed.");
        }

        [Test]
        public async Task A_connection_carrying_a_url_that_is_not_one_leaves_a_binding_unavailable_and_never_missing()
        {
            var subject = JiraConnectorTestSetup.AConnectorOver(AJira().WithTheWorkProject().Handler);

            var resolutions = await subject.ResolveMany(
                AConnectionWhoseUrlIsNotAUrl(), JiraReleaseSourceKey, TheDatedReleaseOnItsOwn);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolutions[TheDatedRelease], Is.InstanceOf<DeliverySourceResolution.Unavailable>());
                Assert.That(resolutions[TheDatedRelease], Is.Not.InstanceOf<DeliverySourceResolution.NotFound>(),
                    "nobody asked Jira anything, so nothing has been said about whether the Release still exists - and only 'it is gone' may retire a binding.");
            }
        }

        [Test]
        public async Task A_Release_resolves_to_the_reference_ids_of_the_work_that_carries_it()
        {
            var jira = AJira()
                .WithTheReleaseProject()
                .WithReleaseIn(TheReleaseProject, TheDatedRelease, TheDatedReleaseName, TheDayTheDatedReleaseShipsInJira)
                .WithWorkOn("LGH-1", TheDatedRelease)
                .WithWorkOn("LGH-2", TheDatedRelease)
                .WithWorkOn("LGH-3", "99999");

            var snapshot = SnapshotOf(await Resolve(jira, TheDatedRelease));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(snapshot.Name, Is.EqualTo(TheDatedReleaseName));
                Assert.That(snapshot.Date, Is.EqualTo(TheDayTheDatedReleaseShips));
                Assert.That(snapshot.MemberReferenceIds, Is.EquivalentTo(TheWorkOnTheDatedRelease),
                    "what comes back is the reference the tracker knows the work by; which of those the Portfolio actually holds is not the adapter's question to answer.");
            }
        }

        [Test]
        public async Task A_Release_somebody_deleted_in_Jira_resolves_to_nothing_found()
        {
            var jira = AJira()
                .WithTheReleaseProject()
                .WithReleaseIn(TheReleaseProject, TheDatedRelease, TheDatedReleaseName, TheDayTheDatedReleaseShipsInJira);

            var resolution = await Resolve(jira, TheDeletedRelease);

            Assert.That(resolution, Is.InstanceOf<DeliverySourceResolution.NotFound>(),
                "every project answered in full and this Release was in none of them, which is the one case that may retire the binding.");
        }

        [Test]
        public async Task A_Release_nobody_dated_resolves_to_having_no_date_and_still_names_itself()
        {
            var jira = AJira()
                .WithTheReleaseProject()
                .WithReleaseIn(TheReleaseProject, TheUndatedRelease, TheUndatedReleaseName, releaseDate: null);

            var resolution = await Resolve(jira, TheUndatedRelease);

            Assert.That(resolution, Is.EqualTo(new DeliverySourceResolution.NoDate(TheUndatedReleaseName)),
                "the Release is there and only its date is missing, so the sentence a reader needs names the Release they have to go and date.");
        }

        [Test]
        public async Task A_Release_Jira_could_not_be_asked_about_is_unavailable_and_never_missing()
        {
            var jira = AJira()
                .WithTheReleaseProject()
                .WithUnreadableVersionsIn("REL");

            var resolution = await Resolve(jira, TheDatedRelease);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolution, Is.InstanceOf<DeliverySourceResolution.Unavailable>());
                Assert.That(resolution, Is.Not.InstanceOf<DeliverySourceResolution.NotFound>(),
                    "a Jira that could not be reached has said nothing about whether the Release still exists; reading silence as a deletion retires a perfectly good binding on the strength of a network blip.");
            }
        }

        [Test]
        public async Task A_membership_query_Jira_rejects_leaves_the_Release_unavailable_and_never_missing()
        {
            var jira = AJira()
                .WithTheReleaseProject()
                .WithReleaseIn(TheReleaseProject, TheDatedRelease, TheDatedReleaseName, TheDayTheDatedReleaseShipsInJira);
            jira.RefusesTheSearch = true;

            var resolution = await Resolve(jira, TheDatedRelease);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolution, Is.InstanceOf<DeliverySourceResolution.Unavailable>());
                Assert.That(resolution, Is.Not.InstanceOf<DeliverySourceResolution.NotFound>(),
                    "the Release itself read back fine; only the search for what carries it failed, which says nothing about the Release being gone.");
            }
        }

        [Test]
        public async Task Every_bound_Release_is_asked_about_in_one_search_rather_than_one_each()
        {
            var jira = AJira()
                .WithTheReleaseProject()
                .WithReleaseIn(TheReleaseProject, TheDatedRelease, TheDatedReleaseName, TheDayTheDatedReleaseShipsInJira)
                .WithReleaseIn(TheReleaseProject, "10007", "Release 2.0", "2026-09-01")
                .WithReleaseIn(TheReleaseProject, "10008", "Release 3.0", "2026-09-15");

            var resolutions = await ResolveAll(jira, ThreeDatedReleases);

            var jql = QueryValue(jira.SearchRequests.Single(), "jql");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolutions, Has.Count.EqualTo(3),
                    "one verdict comes back per Release asked about, so a caller never has to guess what a missing key meant.");
                Assert.That(jira.VersionListRequests, Has.Count.EqualTo(1),
                    "three bound Deliveries used to cost three reads of their own; they now come out of the one project sweep, so the bill is a sweep and a search however many Deliveries are bound.");
                Assert.That(jql, Is.EqualTo("fixVersion in (10004, 10007, 10008)"),
                    "Jira matches a bare number against the version id and a quoted word against the version name, and two Releases may share a name. Asked for: " + jql);
                Assert.That(jql, Does.Not.Contain(TheDatedReleaseName),
                    "a refresh that keyed on the name would follow whoever renamed the Release, or silently pick the other one. Asked for: " + jql);
            }
        }

        [Test]
        public async Task A_Delivery_bound_before_its_Release_shipped_keeps_resolving_afterwards()
        {
            var jira = AJira()
                .WithTheReleaseProject()
                .WithShippedReleaseIn(TheReleaseProject, TheShippedRelease, TheDatedReleaseName, TheDayTheDatedReleaseShipsInJira)
                .WithWorkOn("LGH-1", TheShippedRelease);

            var snapshot = SnapshotOf(await Resolve(jira, TheShippedRelease));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(snapshot.Date, Is.EqualTo(TheDayTheDatedReleaseShips),
                    "somebody ticking Release in Jira must not be able to break a Delivery that has been syncing for months; the picker declining to offer it is where the two lifecycles part company, and this is the far side of that.");
                Assert.That(snapshot.MemberReferenceIds, Is.EquivalentTo(TheWorkOnAShippedRelease),
                    "the work carried by a shipped Release is exactly what a team tracking it to closure still wants forecast.");
            }
        }

        [Test]
        public async Task An_archived_Release_a_Delivery_is_already_bound_to_still_resolves()
        {
            var jira = AJira()
                .WithTheReleaseProject()
                .WithArchivedReleaseIn(TheReleaseProject, TheShippedRelease, TheDatedReleaseName, TheDayTheDatedReleaseShipsInJira);

            var snapshot = SnapshotOf(await Resolve(jira, TheShippedRelease));

            Assert.That(snapshot.Date, Is.EqualTo(TheDayTheDatedReleaseShips),
                "archiving is not deleting, and only a Release Jira says is gone may retire a binding - so a refresh looks at every status even though the picker offers one of them.");
        }

        [Test]
        public async Task A_credential_that_cannot_list_the_projects_offers_nothing_rather_than_failing()
        {
            var jira = AJira().WithTheReleaseProject();
            jira.RefusesTheProjectList = true;

            var subject = JiraConnectorTestSetup.AConnectorOver(jira.Handler);
            var options = await subject.GetOptions(AJiraCloudConnection(), JiraReleaseSourceKey);

            Assert.That(options, Is.Empty,
                "one unreadable project already costs the reader only that project; a credential that may not list any at all has to land the same way, or the picker breaks instead of coming up empty.");
        }

        [Test]
        public async Task A_credential_that_cannot_list_the_projects_leaves_a_binding_unavailable_and_never_missing()
        {
            var jira = AJira().WithTheReleaseProject();
            jira.RefusesTheProjectList = true;

            var resolution = await Resolve(jira, TheDatedRelease);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolution, Is.InstanceOf<DeliverySourceResolution.Unavailable>());
                Assert.That(resolution, Is.Not.InstanceOf<DeliverySourceResolution.NotFound>(),
                    "the sweep never got as far as looking, so it has said nothing about whether the Release exists; an empty picker is a fine answer, an empty refresh is a retired binding.");
            }
        }

        [Test]
        public async Task A_sweep_that_lost_a_page_leaves_a_Release_it_never_saw_unavailable_and_never_missing()
        {
            var jira = AJira()
                .WithTheReleaseProject()
                .WithReleaseIn(TheReleaseProject, TheDatedRelease, TheDatedReleaseName, TheDayTheDatedReleaseShipsInJira)
                .WithReleaseIn(TheReleaseProject, TheShippedRelease, "Release 2.0", "2026-09-01")
                .WithVersionsFailingAfterTheFirstPageIn("REL");
            jira.VersionsPerPage = 1;

            var resolution = await Resolve(jira, TheShippedRelease);

            Assert.That(resolution, Is.InstanceOf<DeliverySourceResolution.Unavailable>(),
                "the Release is on the page that never arrived, so it is missing from the sweep for a reason that has nothing to do with it; treating a half-read sweep as the whole truth would retire every binding living past the first page.");
        }

        [Test]
        public void ResolveMany_refuses_a_source_key_the_connection_never_offered_before_Jira_is_asked()
        {
            var subject = CreateSubject();

            Assert.ThrowsAsync<ArgumentException>(
                async () => await subject.ResolveMany(UnreachableJiraConnection(), "jira-sprint", TheDatedReleaseOnItsOwn),
                "the connection has no url and no credentials, so anything that reached the network would fail differently.");
        }

        private static DeliverySourceSnapshot SnapshotOf(DeliverySourceResolution resolution)
        {
            Assert.That(resolution, Is.InstanceOf<DeliverySourceResolution.Resolved>(),
                $"expected the Release to read back, but Jira's answer was taken as {resolution}.");

            return ((DeliverySourceResolution.Resolved)resolution).Snapshot;
        }

        private static async Task<DeliverySourceResolution> Resolve(JiraStub jira, string sourceReference)
        {
            var resolutions = await ResolveAll(jira, [sourceReference]);

            return resolutions[sourceReference];
        }

        private static async Task<IReadOnlyDictionary<string, DeliverySourceResolution>> ResolveAll(JiraStub jira, string[] sourceReferences)
        {
            var subject = JiraConnectorTestSetup.AConnectorOver(jira.Handler);

            return await subject.ResolveMany(
                JiraConnectorTestSetup.ATeamOnJiraCloud().WorkTrackingSystemConnection, JiraReleaseSourceKey, sourceReferences);
        }

        private static JiraStub AJira() => new();

        private static WorkTrackingSystemConnection AJiraCloudConnection()
            => JiraConnectorTestSetup.ATeamOnJiraCloud().WorkTrackingSystemConnection;

        /// <summary>
        /// A connection whose url cannot be turned into one, which is what a typo in the connection form
        /// leaves behind. Nothing reaches the network here: building the client is the step that fails.
        /// </summary>
        private static WorkTrackingSystemConnection AConnectionWhoseUrlIsNotAUrl()
        {
            var connection = AJiraCloudConnection();
            var url = connection.Options.Single(option => option.Key == JiraWorkTrackingOptionNames.Url);
            url.Value = "https://";

            return connection;
        }

        /// <summary>
        /// Lighthouse as it actually serves requests, with the two lifetimes Program.cs gives these: a
        /// connector built again for every request, over one cache for the whole process. Only something
        /// that outlives the connector can hold an answer from one request to the next, and a fixture that
        /// reuses a single connector cannot tell the two apart - which is how a cache that was written and
        /// never read once passed for a working one.
        /// </summary>
        private static ServiceProvider AnApplicationServingRequestsOver(HttpMessageHandler handler)
        {
            var services = new ServiceCollection();

            services.AddSingleton(handler);
            services.AddSingleton<IIssueFactory>(new IssueFactory(Mock.Of<ILogger<IssueFactory>>()));
            services.AddSingleton(Mock.Of<ILogger<JiraWorkTrackingConnector>>());
            services.AddSingleton(AnAuthStrategyFactoryThatSignsNothing());
            services.AddSingleton<Cache<string, object>>();
            services.AddScoped<JiraWorkTrackingConnector>();

            return services.BuildServiceProvider();
        }

        private static IWorkTrackingAuthStrategyFactory AnAuthStrategyFactoryThatSignsNothing()
        {
            var strategyMock = new Mock<IWorkTrackingAuthStrategy>();
            strategyMock
                .Setup(strategy => strategy.ApplyAsync(
                    It.IsAny<HttpRequestMessage>(), It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var factoryMock = new Mock<IWorkTrackingAuthStrategyFactory>();
            factoryMock.Setup(factory => factory.Resolve(It.IsAny<string>())).Returns(strategyMock.Object);

            return factoryMock.Object;
        }

        private static async Task<(JiraWorkTrackingConnector Connector, IReadOnlyList<DeliverySourceOption> Options)>
            OptionsInOneRequest(IServiceProvider application, WorkTrackingSystemConnection connection)
        {
            using var scope = application.CreateScope();
            var connector = scope.ServiceProvider.GetRequiredService<JiraWorkTrackingConnector>();

            return (connector, await connector.GetOptions(connection, JiraReleaseSourceKey));
        }

        private static string QueryValue(Uri uri, string name)
        {
            var pairs = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
            var match = Array.Find(pairs, pair => pair.StartsWith($"{name}=", StringComparison.Ordinal));

            return match is null ? string.Empty : Uri.UnescapeDataString(match[(name.Length + 1)..]);
        }

        /// <summary>
        /// A Jira that answers about its Releases and about what carries them, and records every url it was
        /// asked for. Recording the urls is the only way to see that one search covered every bound Release
        /// rather than one search each - a count that no return value reveals.
        /// </summary>
        private sealed class JiraStub
        {
            private readonly Dictionary<string, List<string>> releaseIdsByIssueKey = new(StringComparer.Ordinal);
            private readonly List<DeliverySourceProject> projects = [];
            private readonly Dictionary<string, List<StubVersion>> versionsByProjectKey = new(StringComparer.Ordinal);
            private readonly HashSet<string> projectsRefusingTheirVersions = new(StringComparer.Ordinal);
            private readonly HashSet<string> projectsRefusingTheirLaterPages = new(StringComparer.Ordinal);

            public JiraStub()
            {
                var handlerMock = new Mock<HttpMessageHandler>();
                handlerMock.Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.IsAny<HttpRequestMessage>(),
                        ItExpr.IsAny<CancellationToken>())
                    .Returns<HttpRequestMessage, CancellationToken>((request, _) => Task.FromResult(Respond(request)));

                Handler = handlerMock.Object;
            }

            public HttpMessageHandler Handler { get; }

            /// <summary>
            /// A queue rather than a list because the connector reads several projects at once, and two
            /// threads adding to a plain list can land on the same slot - which loses a request the
            /// specifications next door are counting, in a way that only shows up under load.
            /// </summary>
            public ConcurrentQueue<Uri> Requests { get; } = new();

            public bool RefusesTheSearch { get; set; }

            public bool RefusesTheProjectList { get; set; }

            public IReadOnlyList<Uri> SearchRequests =>
                [.. Requests.Where(uri => uri.AbsolutePath.Contains("/search", StringComparison.Ordinal)
                    && !uri.AbsolutePath.EndsWith("/project/search", StringComparison.Ordinal))];

            public IReadOnlyList<Uri> VersionListRequests =>
                [.. Requests.Where(uri => uri.AbsolutePath.EndsWith("/version", StringComparison.Ordinal))];

            /// <summary>How many versions this Jira will part with per page, so paging can be provoked with two.</summary>
            public int VersionsPerPage { get; set; } = 50;

            public JiraStub WithProject(string key, string name)
            {
                projects.Add(new DeliverySourceProject(key, name));

                return this;
            }

            public JiraStub WithTheReleaseProject() => WithProject(TheReleaseProject, "Release coordination");

            public JiraStub WithTheWorkProject() => WithProject(TheWorkProject, "The work");

            public JiraStub WithReleaseIn(string projectKey, string id, string name, string? releaseDate)
                => WithVersionIn(projectKey, id, name, releaseDate, archived: false, released: false);

            public JiraStub WithShippedReleaseIn(string projectKey, string id, string name, string releaseDate)
                => WithVersionIn(projectKey, id, name, releaseDate, archived: false, released: true);

            public JiraStub WithArchivedReleaseIn(string projectKey, string id, string name, string releaseDate)
                => WithVersionIn(projectKey, id, name, releaseDate, archived: true, released: true);

            public JiraStub WithUnreadableVersionsIn(string projectKey)
            {
                projectsRefusingTheirVersions.Add(projectKey);

                return this;
            }

            /// <summary>The refusal lifts, which is what a momentary failure looks like from the caller's side.</summary>
            public JiraStub LetsItsVersionsBeReadAgainIn(string projectKey)
            {
                projectsRefusingTheirVersions.Remove(projectKey);

                return this;
            }

            /// <summary>A project that answers its first page and then stops, which is how a sweep loses a page.</summary>
            public JiraStub WithVersionsFailingAfterTheFirstPageIn(string projectKey)
            {
                projectsRefusingTheirLaterPages.Add(projectKey);

                return this;
            }

            private JiraStub WithVersionIn(string projectKey, string id, string name, string? releaseDate, bool archived, bool released)
            {
                var date = releaseDate is null ? string.Empty : $",\"releaseDate\":\"{releaseDate}\"";
                var json =
                    $"{{\"id\":\"{id}\",\"name\":\"{name}\",\"archived\":{Flag(archived)},\"released\":{Flag(released)}{date}}}";

                if (!versionsByProjectKey.TryGetValue(projectKey, out var versions))
                {
                    versions = [];
                    versionsByProjectKey[projectKey] = versions;
                }

                versions.Add(new StubVersion(json, StatusOf(archived, released)));

                return this;
            }

            private static string Flag(bool value) => value ? "true" : "false";

            /// <summary>Jira grades a version as exactly one of these, and archiving outranks releasing.</summary>
            private static string StatusOf(bool archived, bool released)
            {
                if (archived)
                {
                    return "archived";
                }

                return released ? "released" : "unreleased";
            }

            private sealed record StubVersion(string Json, string Status);

            public JiraStub WithWorkOn(string issueKey, string releaseId)
            {
                if (!releaseIdsByIssueKey.TryGetValue(issueKey, out var releaseIds))
                {
                    releaseIds = [];
                    releaseIdsByIssueKey[issueKey] = releaseIds;
                }

                releaseIds.Add(releaseId);

                return this;
            }

            private HttpResponseMessage Respond(HttpRequestMessage request)
            {
                var uri = request.RequestUri ?? new Uri("https://unreached.invalid/");
                Requests.Enqueue(uri);

                var path = uri.AbsolutePath;

                if (path.EndsWith("rest/api/2/serverInfo", StringComparison.Ordinal))
                {
                    return Ok("{\"deploymentType\":\"Cloud\"}");
                }

                if (path.EndsWith("/project/search", StringComparison.Ordinal))
                {
                    return RefusesTheProjectList
                        ? Refuse(HttpStatusCode.Forbidden)
                        : Ok($"{{\"isLast\":true,\"values\":[{string.Join(",", projects.Select(ProjectJson))}]}}");
                }

                if (path.EndsWith("/version", StringComparison.Ordinal))
                {
                    return RespondWithVersionsOf(path.Split('/')[^2], uri);
                }

                if (path.Contains("search", StringComparison.Ordinal))
                {
                    return RespondToSearch(uri);
                }

                return Ok("{}");
            }

            private static string ProjectJson(DeliverySourceProject project)
                => $"{{\"key\":\"{project.Key}\",\"name\":\"{project.Name}\"}}";

            /// <summary>
            /// Answers the way the real endpoint does: only the statuses that were asked for, one page at a
            /// time, saying on each page whether it was the last. Honouring the status filter is what lets a
            /// specification tell "we asked Jira to leave the finished Releases out" apart from "we asked
            /// for everything and then dropped some".
            /// </summary>
            private HttpResponseMessage RespondWithVersionsOf(string projectKey, Uri uri)
            {
                if (projectsRefusingTheirVersions.Contains(projectKey))
                {
                    return Refuse(HttpStatusCode.Forbidden);
                }

                var startAt = int.TryParse(QueryValue(uri, "startAt"), out var parsed) ? parsed : 0;

                if (startAt > 0 && projectsRefusingTheirLaterPages.Contains(projectKey))
                {
                    return Refuse(HttpStatusCode.InternalServerError);
                }

                var wanted = QueryValue(uri, "status").Split(',', StringSplitOptions.RemoveEmptyEntries);
                var known = versionsByProjectKey.TryGetValue(projectKey, out var all) ? all : [];

                var offered = known.Where(version => Array.IndexOf(wanted, version.Status) >= 0).ToList();

                var page = offered.Skip(startAt).Take(VersionsPerPage).ToList();
                var isLast = startAt + page.Count >= offered.Count;

                return Ok(
                    $"{{\"startAt\":{startAt},\"total\":{offered.Count},\"isLast\":{Flag(isLast)}," +
                    $"\"values\":[{string.Join(",", page.Select(version => version.Json))}]}}");
            }

            private HttpResponseMessage RespondToSearch(Uri uri)
            {
                if (RefusesTheSearch)
                {
                    return Refuse(HttpStatusCode.InternalServerError);
                }

                var askedAbout = ReleaseIdsNamedIn(QueryValue(uri, "jql"));

                var issues = releaseIdsByIssueKey
                    .Where(work => work.Value.Exists(askedAbout.Contains))
                    .Select(work => IssueJson(work.Key, work.Value));

                return Ok($"{{\"issues\":[{string.Join(",", issues)}]}}");
            }

            private static HashSet<string> ReleaseIdsNamedIn(string jql)
            {
                var opening = jql.IndexOf('(', StringComparison.Ordinal);
                var closing = jql.LastIndexOf(')');

                if (opening < 0 || closing <= opening)
                {
                    return [];
                }

                return [.. jql[(opening + 1)..closing].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];
            }

            private static string IssueJson(string issueKey, IEnumerable<string> releaseIds)
            {
                var fixVersions = string.Join(",", releaseIds.Select(id => $"{{\"id\":\"{id}\"}}"));

                return $"{{\"key\":\"{issueKey}\",\"fields\":{{\"fixVersions\":[{fixVersions}]}}}}";
            }

            private static HttpResponseMessage Refuse(HttpStatusCode status)
                => new(status) { Content = new StringContent("{}", Encoding.UTF8, "application/json") };

            private static HttpResponseMessage Ok(string body)
                => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }

        private static WorkTrackingSystemConnection UnreachableJiraConnection()
        {
            return new WorkTrackingSystemConnection
            {
                Id = 5565,
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                Name = "A Jira connection that was never reached",
            };
        }

        private static JiraWorkTrackingConnector CreateSubject()
        {
            return new JiraWorkTrackingConnector(
                Mock.Of<IIssueFactory>(),
                Mock.Of<ILogger<JiraWorkTrackingConnector>>(),
                Mock.Of<IWorkTrackingAuthStrategyFactory>(),
                new Lighthouse.Backend.Cache.Cache<string, object>());
        }
    }
}
