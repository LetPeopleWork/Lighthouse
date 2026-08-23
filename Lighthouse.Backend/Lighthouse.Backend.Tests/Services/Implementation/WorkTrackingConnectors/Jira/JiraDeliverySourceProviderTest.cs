using System.Net;
using System.Text;
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

        private static readonly DeliverySourceProject TheDemoProject = new("LGH", "Lighthouse Demo");

        private static readonly string[] TheDatedReleaseOnItsOwn = [TheDatedRelease];
        private static readonly string[] ThreeDatedReleases = [TheDatedRelease, "10007", "10008"];
        private static readonly string[] TwoProjectsThatBothNameARelease44 = ["PROJ", "REL"];
        private static readonly string[] TheWorkOnTheDatedRelease = ["LGH-1", "LGH-2"];

        /// <summary>
        /// Copied from what a real Jira answered on 2026-08-22. The middle entry has no releaseDate key at
        /// all - not a null, the key is simply absent - which is how Jira reports a Release nobody dated,
        /// and how two of the three Releases on that instance came back.
        /// </summary>
        private const string CapturedVersionsPayload = """
            [
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
            var options = JiraReleaseVersionReader.ReadOptions(CapturedVersionsPayload, TheDemoProject);

            var undated = options.Single(option => option.Id == "10005");

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
            var options = JiraReleaseVersionReader.ReadOptions(CapturedVersionsPayload, TheDemoProject);

            var archived = options.Single(option => option.Id == "10006");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(archived.IsRetiredAtSource, Is.True);
                Assert.That(archived.IsSelectable, Is.False);
                Assert.That(archived.BlockedBecause, Is.EqualTo(SourceOptionBlockReason.RetiredAtSource),
                    "an archived Release carries a date and is still refused, so having a date is not on its own enough.");
            }
        }

        [Test]
        public void A_Release_that_already_shipped_stays_selectable()
        {
            var options = JiraReleaseVersionReader.ReadOptions(CapturedVersionsPayload, TheDemoProject);

            var shipped = options.Single(option => option.Id == "10004");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(options, Has.Count.EqualTo(3),
                    "every version Jira returns becomes an option; none are filtered out on the way through.");
                Assert.That(shipped.IsReleasedAtSource, Is.True);
                Assert.That(shipped.Date, Is.EqualTo(new DateTime(2026, 8, 22, 0, 0, 0, DateTimeKind.Utc)));
                Assert.That(shipped.IsSelectable, Is.True,
                    "a shipped Release is routinely still being tracked to closure, which is exactly when a forecast is worth having.");
                Assert.That(shipped.BlockedBecause, Is.Null);
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
                .WithProject("PROJ", "The work")
                .WithProject("REL", "Release coordination")
                .WithReleaseIn("REL", TheDatedRelease, TheDatedReleaseName, "2026-08-22");

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
                .WithProject("PROJ", "The work")
                .WithProject("REL", "Release coordination")
                .WithReleaseIn("PROJ", "10101", "Release 44", "2026-09-01")
                .WithReleaseIn("REL", "10202", "Release 44", "2026-09-01");

            var subject = JiraConnectorTestSetup.AConnectorOver(jira.Handler);
            var options = await subject.GetOptions(AJiraCloudConnection(), JiraReleaseSourceKey);

            Assert.That(options.Select(option => option.Project.Key), Is.EquivalentTo(TwoProjectsThatBothNameARelease44),
                "the two rows read identically otherwise, so without the project the reader picks one of them at random.");
        }

        [Test]
        public async Task A_project_whose_Releases_cannot_be_read_costs_the_reader_only_that_project()
        {
            var jira = AJira()
                .WithProject("PROJ", "The work")
                .WithProject("LOCKED", "Someone else's project")
                .WithReleaseIn("PROJ", TheDatedRelease, TheDatedReleaseName, "2026-08-22")
                .WithUnreadableVersionsIn("LOCKED");

            var subject = JiraConnectorTestSetup.AConnectorOver(jira.Handler);
            var options = await subject.GetOptions(AJiraCloudConnection(), JiraReleaseSourceKey);

            Assert.That(options.Select(option => option.Id), Is.EquivalentTo(TheDatedReleaseOnItsOwn),
                "one project the credential may list but not read must not take the whole picker away from a reader with perfectly good projects to choose from.");
        }

        [Test]
        public async Task Asking_twice_in_quick_succession_asks_Jira_once()
        {
            var jira = AJira()
                .WithProject("PROJ", "The work")
                .WithReleaseIn("PROJ", TheDatedRelease, TheDatedReleaseName, "2026-08-22");

            var subject = JiraConnectorTestSetup.AConnectorOver(jira.Handler);
            var connection = AJiraCloudConnection();

            await subject.GetOptions(connection, JiraReleaseSourceKey);
            await subject.GetOptions(connection, JiraReleaseSourceKey);

            Assert.That(jira.VersionListRequests, Has.Count.EqualTo(1),
                "the picker is typed into, and a request per keystroke would cost one call per project every time.");
        }

        [Test]
        public async Task A_Release_resolves_to_the_reference_ids_of_the_work_that_carries_it()
        {
            var jira = AJira()
                .WithRelease(TheDatedRelease, TheDatedReleaseName, "2026-08-22")
                .WithWorkOn("LGH-1", TheDatedRelease)
                .WithWorkOn("LGH-2", TheDatedRelease)
                .WithWorkOn("LGH-3", "99999");

            var snapshot = SnapshotOf(await Resolve(jira, TheDatedRelease));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(snapshot.Name, Is.EqualTo(TheDatedReleaseName));
                Assert.That(snapshot.Date, Is.EqualTo(new DateTime(2026, 8, 22, 0, 0, 0, DateTimeKind.Utc)));
                Assert.That(snapshot.MemberReferenceIds, Is.EquivalentTo(TheWorkOnTheDatedRelease),
                    "what comes back is the reference the tracker knows the work by; which of those the Portfolio actually holds is not the adapter's question to answer.");
            }
        }

        [Test]
        public async Task A_Release_somebody_deleted_in_Jira_resolves_to_nothing_found()
        {
            var jira = AJira().WithNoSuchRelease(TheDeletedRelease);

            var resolution = await Resolve(jira, TheDeletedRelease);

            Assert.That(resolution, Is.InstanceOf<DeliverySourceResolution.NotFound>(),
                "Jira answered, and the answer was that the Release is gone - which is the one case that may retire the binding.");
        }

        [Test]
        public async Task A_Release_nobody_dated_resolves_to_having_no_date_and_still_names_itself()
        {
            var jira = AJira().WithRelease(TheUndatedRelease, TheUndatedReleaseName, releaseDate: null);

            var resolution = await Resolve(jira, TheUndatedRelease);

            Assert.That(resolution, Is.EqualTo(new DeliverySourceResolution.NoDate(TheUndatedReleaseName)),
                "the Release is there and only its date is missing, so the sentence a reader needs names the Release they have to go and date.");
        }

        [Test]
        public async Task A_Release_Jira_could_not_be_asked_about_is_unavailable_and_never_missing()
        {
            var jira = AJira().WithUnreadableRelease(TheDatedRelease);

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
            var jira = AJira().WithRelease(TheDatedRelease, TheDatedReleaseName, "2026-08-22");
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
                .WithRelease(TheDatedRelease, TheDatedReleaseName, "2026-08-22")
                .WithRelease("10007", "Release 2.0", "2026-09-01")
                .WithRelease("10008", "Release 3.0", "2026-09-15");

            var resolutions = await ResolveAll(jira, ThreeDatedReleases);

            var jql = QueryValue(jira.SearchRequests.Single(), "jql");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolutions, Has.Count.EqualTo(3),
                    "one verdict comes back per Release asked about, so a caller never has to guess what a missing key meant.");
                Assert.That(jql, Is.EqualTo("fixVersion in (10004, 10007, 10008)"),
                    "Jira matches a bare number against the version id and a quoted word against the version name, and two Releases may share a name. Asked for: " + jql);
                Assert.That(jql, Does.Not.Contain(TheDatedReleaseName),
                    "a refresh that keyed on the name would follow whoever renamed the Release, or silently pick the other one. Asked for: " + jql);
            }
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
            private readonly Dictionary<string, string> releasesById = new(StringComparer.Ordinal);
            private readonly Dictionary<string, HttpStatusCode> refusalsById = new(StringComparer.Ordinal);
            private readonly Dictionary<string, List<string>> releaseIdsByIssueKey = new(StringComparer.Ordinal);
            private readonly List<DeliverySourceProject> projects = [];
            private readonly Dictionary<string, List<string>> versionsByProjectKey = new(StringComparer.Ordinal);
            private readonly HashSet<string> projectsRefusingTheirVersions = new(StringComparer.Ordinal);

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

            public List<Uri> Requests { get; } = [];

            public bool RefusesTheSearch { get; set; }

            public IReadOnlyList<Uri> SearchRequests =>
                [.. Requests.Where(uri => uri.AbsolutePath.Contains("/search", StringComparison.Ordinal)
                    && !uri.AbsolutePath.EndsWith("/project/search", StringComparison.Ordinal))];

            public IReadOnlyList<Uri> VersionListRequests =>
                [.. Requests.Where(uri => uri.AbsolutePath.EndsWith("/versions", StringComparison.Ordinal))];

            public JiraStub WithProject(string key, string name)
            {
                projects.Add(new DeliverySourceProject(key, name));

                return this;
            }

            public JiraStub WithReleaseIn(string projectKey, string id, string name, string? releaseDate)
            {
                var date = releaseDate is null ? string.Empty : $",\"releaseDate\":\"{releaseDate}\"";

                if (!versionsByProjectKey.TryGetValue(projectKey, out var versions))
                {
                    versions = [];
                    versionsByProjectKey[projectKey] = versions;
                }

                versions.Add($"{{\"id\":\"{id}\",\"name\":\"{name}\",\"archived\":false,\"released\":false{date}}}");

                return this;
            }

            public JiraStub WithUnreadableVersionsIn(string projectKey)
            {
                projectsRefusingTheirVersions.Add(projectKey);

                return this;
            }

            public JiraStub WithRelease(string id, string name, string? releaseDate)
            {
                var date = releaseDate is null ? string.Empty : $",\"releaseDate\":\"{releaseDate}\"";
                releasesById[id] = $"{{\"id\":\"{id}\",\"name\":\"{name}\",\"archived\":false,\"released\":false{date}}}";

                return this;
            }

            public JiraStub WithNoSuchRelease(string id)
            {
                refusalsById[id] = HttpStatusCode.NotFound;

                return this;
            }

            public JiraStub WithUnreadableRelease(string id)
            {
                refusalsById[id] = HttpStatusCode.InternalServerError;

                return this;
            }

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
                Requests.Add(uri);

                var path = uri.AbsolutePath;

                if (path.EndsWith("rest/api/2/serverInfo", StringComparison.Ordinal))
                {
                    return Ok("{\"deploymentType\":\"Cloud\"}");
                }

                if (path.EndsWith("/project/search", StringComparison.Ordinal))
                {
                    return Ok($"{{\"isLast\":true,\"values\":[{string.Join(",", projects.Select(ProjectJson))}]}}");
                }

                if (path.EndsWith("/versions", StringComparison.Ordinal))
                {
                    return RespondWithVersionsOf(path.Split('/')[^2]);
                }

                if (path.Contains("/version/", StringComparison.Ordinal))
                {
                    return RespondAboutRelease(path[(path.LastIndexOf('/') + 1)..]);
                }

                if (path.Contains("search", StringComparison.Ordinal))
                {
                    return RespondToSearch(uri);
                }

                return Ok("{}");
            }

            private static string ProjectJson(DeliverySourceProject project)
                => $"{{\"key\":\"{project.Key}\",\"name\":\"{project.Name}\"}}";

            private HttpResponseMessage RespondWithVersionsOf(string projectKey)
            {
                if (projectsRefusingTheirVersions.Contains(projectKey))
                {
                    return Refuse(HttpStatusCode.Forbidden);
                }

                var versions = versionsByProjectKey.TryGetValue(projectKey, out var known) ? known : [];

                return Ok($"[{string.Join(",", versions)}]");
            }

            private HttpResponseMessage RespondAboutRelease(string id)
            {
                if (refusalsById.TryGetValue(id, out var refusal))
                {
                    return Refuse(refusal);
                }

                return releasesById.TryGetValue(id, out var payload) ? Ok(payload) : Refuse(HttpStatusCode.NotFound);
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
                Mock.Of<IWorkTrackingAuthStrategyFactory>());
        }
    }
}
