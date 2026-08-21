using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Linear
{
    /// <summary>
    /// What a Linear Project says it is waiting on.
    ///
    /// Linear records a dependency once and offers it from both ends. A Project's own relations are the
    /// ones where it is the source - what it blocks. Its inverse relations are the ones where it is the
    /// target - what it is waiting on, which is the only direction Lighthouse reads. Reading the near
    /// side instead would produce a count that looks right on every screen while pointing every edge
    /// backwards, so the fixtures here carry both directions on purpose.
    /// </summary>
    [TestFixture]
    public class LinearDependencyRelationTest
    {
        private const string BlockedProjectId = "11111111-1111-1111-1111-111111111111";
        private const string BlockingProjectId = "22222222-2222-2222-2222-222222222222";

        [Test]
        public async Task GetFeaturesForProject_AProjectWaitsOnTheOneThatBlocksIt()
        {
            var features = await FeaturesFrom(ProjectsWithOneDependency());

            var blocked = features.Single(feature => feature.ReferenceId == BlockedProjectId);
            var waitedOn = blocked.DependsOnReferences.Select(reference => reference.ReferenceId);

            var expected = new[] { BlockingProjectId };
            Assert.That(waitedOn, Is.EqualTo(expected));
        }

        [Test]
        public async Task GetFeaturesForProject_TheProjectDoingTheBlockingWaitsOnNothing()
        {
            var features = await FeaturesFrom(ProjectsWithOneDependency());

            var blocking = features.Single(feature => feature.ReferenceId == BlockingProjectId);

            Assert.That(blocking.DependsOnReferences, Is.Empty);
        }

        [Test]
        public async Task GetFeaturesForProject_OneRelationBetweenTwoProjectsIsOneEdgeAndNotTwo()
        {
            var features = await FeaturesFrom(ProjectsWithOneDependency());

            var edges = features.Sum(feature => feature.DependsOnReferences.Count);

            Assert.That(edges, Is.EqualTo(1));
        }

        [Test]
        public async Task GetFeaturesForProject_TheReferenceIsTheProjectIdExactlyAsLinearReturnedIt()
        {
            var features = await FeaturesFrom(ProjectsWithOneDependency());

            var blocked = features.Single(feature => feature.ReferenceId == BlockedProjectId);
            var blocking = features.Single(feature => feature.ReferenceId == BlockingProjectId);

            Assert.That(blocked.DependsOnReferences.Single().ReferenceId, Is.EqualTo(blocking.ReferenceId),
                "The reference has to be the very string the other Feature is keyed by, or it resolves to "
                + "nothing and the screen reads as though there were no dependency at all.");
        }

        [Test]
        public async Task GetFeaturesForProject_EveryReferenceIsMarkedAsHavingComeFromTheTracker()
        {
            var features = await FeaturesFrom(ProjectsWithOneDependency());

            var blocked = features.Single(feature => feature.ReferenceId == BlockedProjectId);

            Assert.That(blocked.DependsOnReferences.Single().Source, Is.EqualTo(DependencySource.TrackerLink));
        }

        [Test]
        public async Task GetFeaturesForProject_AProjectWaitingOnSeveralCarriesOneReferenceEach()
        {
            var features = await FeaturesFrom(AProjectBlockedByTwo());

            var waitedOn = features
                .Single(feature => feature.ReferenceId == BlockedProjectId)
                .DependsOnReferences
                .Select(reference => reference.ReferenceId);

            var expected = new[] { BlockingProjectId, "33333333-3333-3333-3333-333333333333" };
            Assert.That(waitedOn, Is.EquivalentTo(expected));
        }

        [Test]
        [TestCase("null")]
        [TestCase("{\"nodes\": null}")]
        [TestCase("{\"nodes\": []}")]
        [TestCase("{\"nodes\": [{}]}")]
        [TestCase("{\"nodes\": [{\"project\": null}]}")]
        [TestCase("{\"nodes\": [{\"project\": {\"id\": null}}]}")]
        [TestCase("{\"nodes\": [{\"project\": {\"id\": \"\"}}]}")]
        public async Task GetFeaturesForProject_APayloadItCannotReadYieldsNothingAndThrowsNothing(string inverseRelations)
        {
            var features = await FeaturesFrom(ProjectsResponse(
                AProject(BlockedProjectId, "Blocked", inverseRelations)));

            Assert.That(features.Single().DependsOnReferences, Is.Empty);
        }

        [Test]
        public async Task GetFeaturesForProject_AProjectWithNoRelationsAtAllIsUnchangedInEveryOtherRespect()
        {
            var features = await FeaturesFrom(ProjectsResponse(AProject(BlockedProjectId, "Blocked", "null")));

            var feature = features.Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.Name, Is.EqualTo("Blocked"));
                Assert.That(feature.State, Is.EqualTo("In Progress"));
                Assert.That(feature.DependsOnReferences, Is.Empty);
            }
        }

        /// <summary>
        /// The relations ride on the projects query the refresh already sends. Asking for them separately
        /// would be a second round trip per Portfolio, and asking for the near side as well would put the
        /// wrong direction one careless line away from being read.
        /// </summary>
        [Test]
        public async Task GetFeaturesForProject_TheProjectsQueryAsksForTheFarSideOnlyAndAsksOnce()
        {
            var sentQueries = new List<string>();
            await FeaturesFrom(ProjectsWithOneDependency(), sentQueries.Add);

            var projectsQuery = sentQueries.Find(query => query.Contains("projects(", StringComparison.Ordinal)) ?? string.Empty;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(projectsQuery, Does.Contain("inverseRelations"));
                Assert.That(projectsQuery.Replace("inverseRelations", string.Empty, StringComparison.Ordinal),
                    Does.Not.Contain("relations"),
                    "Asking for the near side too would leave the wrong direction one careless line away.");
                Assert.That(sentQueries.FindAll(query => query.Contains("projects(", StringComparison.Ordinal)), Has.Count.EqualTo(1));
            }
        }

        private static async Task<List<Feature>> FeaturesFrom(string projectsResponse, Action<string>? recordQuery = null)
        {
            var subject = CreateSubject(HandlerReturning(projectsResponse, recordQuery));

            return await subject.GetFeaturesForProject(CreatePortfolio());
        }

        private static string ProjectsWithOneDependency()
            => ProjectsResponse(
                AProject(BlockedProjectId, "Blocked", BlockedBy(BlockingProjectId)),
                AProject(BlockingProjectId, "Blocking", "{\"nodes\": []}"));

        private static string AProjectBlockedByTwo()
            => ProjectsResponse(
                AProject(BlockedProjectId, "Blocked", BlockedBy(BlockingProjectId, "33333333-3333-3333-3333-333333333333")));

        private static string BlockedBy(params string[] blockerIds)
        {
            var nodes = Array.ConvertAll(
                blockerIds,
                id => "{\"type\": \"blocks\", \"project\": {\"id\": \"" + id + "\"}}");

            return "{\"nodes\": [" + string.Join(",", nodes) + "]}";
        }

        private static string AProject(string id, string name, string inverseRelations)
            => "{\"id\": \"" + id + "\""
                + ", \"name\": \"" + name + "\""
                + ", \"status\": {\"id\": \"s1\", \"name\": \"Active\"}"
                + ", \"url\": \"https://linear.app/x\""
                + ", \"sortOrder\": 1.0"
                + ", \"createdAt\": \"2026-01-01T00:00:00.000Z\""
                + ", \"inverseRelations\": " + inverseRelations + "}";

        private static string ProjectsResponse(params string[] projects)
            => "{\"data\": {\"projects\": {\"nodes\": [" + string.Join(",", projects) + "]"
                + ", \"pageInfo\": {\"hasNextPage\": false, \"endCursor\": null}}}}";

        private static HttpMessageHandler HandlerReturning(string projectsResponse, Action<string>? recordQuery)
        {
            var mock = new Mock<HttpMessageHandler>();
            mock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
                    recordQuery?.Invoke(body);

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(projectsResponse, Encoding.UTF8, "application/json"),
                    };
                });

            return mock.Object;
        }

        private static LinearWorkTrackingConnector CreateSubject(HttpMessageHandler handler)
            => new(Mock.Of<ILogger<LinearWorkTrackingConnector>>(), new FakeCryptoService(), handler);

        private static Portfolio CreatePortfolio()
        {
            var connection = new WorkTrackingSystemConnection
            {
                WorkTrackingSystem = WorkTrackingSystems.Linear,
                Name = "Linear Connection",
            };
            connection.Options.Add(new WorkTrackingSystemConnectionOption
            {
                Key = LinearWorkTrackingOptionNames.ApiKey,
                Value = "key",
                IsSecret = true,
            });

            var portfolio = new Portfolio
            {
                Name = "Demo Portfolio",
                WorkTrackingSystemConnection = connection,
                StateMappings =
                [
                    new StateMapping { Name = "In Progress", States = ["Active"] },
                ],
            };

            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add("Project");
            portfolio.ToDoStates.Clear();
            portfolio.DoingStates.Clear();
            portfolio.DoingStates.Add("In Progress");
            portfolio.DoneStates.Clear();

            return portfolio;
        }
    }
}
