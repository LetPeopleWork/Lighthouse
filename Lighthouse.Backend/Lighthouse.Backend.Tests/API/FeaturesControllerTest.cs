using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Linq.Expressions;
using System.Security.Claims;

namespace Lighthouse.Backend.Tests.API
{
    public class FeaturesControllerTest
    {
        private readonly List<Feature> parentFeatures = new List<Feature>();
        private readonly List<Feature> features = new List<Feature>();
        private readonly List<WorkItem> workItems = new List<WorkItem>();

        private static int featureCounter = 0;

        private const string TheFeatureThatWaits = "F-WAITING";

        private const string TheBlockerBehindTheWall = "F-HIDDEN";

        private static readonly string[] OnlyTheFeatureThatWaits = [TheFeatureThatWaits];

        private Mock<IFeatureRepository> featureRepositoryMock;
        private Mock<IWorkItemRepository> workItemRepositoryMock;
        private Mock<IBlackoutPeriodService> blackoutPeriodServiceMock;
        private Mock<IRbacAdministrationService> rbacAdministrationServiceMock;

        [SetUp]
        public void Setup()
        {
            featureRepositoryMock = new Mock<IFeatureRepository>();
            workItemRepositoryMock = new Mock<IWorkItemRepository>();
            blackoutPeriodServiceMock = new Mock<IBlackoutPeriodService>();
            blackoutPeriodServiceMock.Setup(s => s.GetEffectiveBlackoutDays(It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns([]);
            rbacAdministrationServiceMock = new Mock<IRbacAdministrationService>();

            features.Clear();
            parentFeatures.Clear();
            workItems.Clear();

            // Mirrors FeatureRepository.GetAllByPredicate, which orders by FeatureComparer before returning -
            // the controller no longer re-sorts what the repository already sorted.
            featureRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Feature, bool>>>()))
                .Returns((Expression<Func<Feature, bool>> predicate) => features.Union(parentFeatures).Where(predicate.Compile()).OrderBy(f => f, new FeatureComparer()).AsQueryable());

            // The same stand-in store the two reads above answer from, so a Feature this fixture holds is
            // one a dependency naming it resolves against.
            featureRepositoryMock.Setup(x => x.GetAllReferenceIds())
                .Returns(() => features.Union(parentFeatures).Select(f => f.ReferenceId).ToList());

            featureRepositoryMock.Setup(x => x.GetById(It.IsAny<int>()))
                .Returns((int id) => features.Union(parentFeatures).SingleOrDefault(f => f.Id == id));

            workItemRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<WorkItem, bool>>>()))
                .Returns((Expression<Func<WorkItem, bool>> predicate) => workItems.Where(predicate.Compile()).AsQueryable());

            rbacAdministrationServiceMock
                .Setup(x => x.GetReadablePortfolioIdsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ClaimsPrincipal _, IEnumerable<int> ids, CancellationToken _) => ids.Distinct().ToArray());
        }

        [Test]
        public void FeaturesController_HasAuthorizeAttribute()
        {
            var attribute = typeof(FeaturesController)
                .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Cast<AuthorizeAttribute>()
                .SingleOrDefault();

            Assert.That(attribute, Is.Not.Null);
        }

        [Test]
        public async Task GetParentFeatures_FeatureReferenceNotFound_ReturnsEmptyList()
        {
            var subject = CreateSubject();

            var response = await subject.GetFeatureDetailsByReference(["1886"]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = response.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                var result = okResult.Value as List<FeatureDto>;

                Assert.That(result, Is.Empty);
            }
        }

        [Test]
        public async Task GetParentFeatures_FeatureReferenceFound_ReturnsFeatureDto()
        {
            var feature = CreateFeatureByReferenceId("1886");
            parentFeatures.Add(feature);

            var subject = CreateSubject();

            var response = await subject.GetFeatureDetailsByReference(["1886"]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = response.Result as OkObjectResult;

                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                var result = okResult.Value as List<FeatureDto>;
                Assert.That(result, Has.Count.EqualTo(1));

                Assert.That(result[0].ReferenceId, Is.EqualTo("1886"));
                Assert.That(result[0].Url, Is.EqualTo("https://example.com/feature/1886"));
                Assert.That(result[0].Name, Is.EqualTo("Feature 1886"));
            }
        }

        [Test]
        public async Task GetParentFeatures_FeatureReferenceFoundWithMultipleIds_ReturnsAllMatchingFeatures()
        {
            var feature1 = CreateFeatureByReferenceId("1886");
            var feature2 = CreateFeatureByReferenceId("1887");

            parentFeatures.Add(feature1);
            parentFeatures.Add(feature2);

            var subject = CreateSubject();

            var response = await subject.GetFeatureDetailsByReference(["1886", "1887"]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = response.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                var result = okResult.Value as List<FeatureDto>;
                Assert.That(result, Has.Count.EqualTo(2));

                Assert.That(result[0].ReferenceId, Is.EqualTo("1886"));
                Assert.That(result[0].Url, Is.EqualTo("https://example.com/feature/1886"));
                Assert.That(result[0].Name, Is.EqualTo("Feature 1886"));

                Assert.That(result[1].ReferenceId, Is.EqualTo("1887"));
                Assert.That(result[1].Url, Is.EqualTo("https://example.com/feature/1887"));
                Assert.That(result[1].Name, Is.EqualTo("Feature 1887"));
            }
        }

        [Test]
        public async Task GetParentFeatures_FeaturesFound_ReturnsInCorrectOrder()
        {
            var feature1 = CreateFeatureByReferenceId("1886");
            feature1.Order = "12";

            var feature2 = CreateFeatureByReferenceId("1887");
            feature2.Order = "2";

            parentFeatures.Add(feature1);
            parentFeatures.Add(feature2);

            var subject = CreateSubject();

            var response = await subject.GetFeatureDetailsByReference(["1886", "1887"]);

            using (Assert.EnterMultipleScope())
            {
                var okResult = response.Result as OkObjectResult;
                var result = okResult.Value as List<FeatureDto>;

                Assert.That(result[0].ReferenceId, Is.EqualTo("1887"));
                Assert.That(result[1].ReferenceId, Is.EqualTo("1886"));
            }
        }

        [Test]
        public async Task GetFeatureDetails_NoParameter_ReturnsBadRequest()
        {
            var subject = CreateSubject();

            var response = await subject.GetFeatureDetailsById(new List<int>());

            Assert.That(response.Result, Is.InstanceOf<BadRequestResult>());
        }

        [Test]
        public async Task GetFeatureDetails_SingleId_DoesNotExist_ReturnsEmptyList()
        {
            var subject = CreateSubject();

            var response = await subject.GetFeatureDetailsById(new List<int> { 1886 });


            var okResult = response.Result as OkObjectResult;
            var result = okResult.Value as List<FeatureDto>;

            Assert.That(result, Has.Count.EqualTo(0));
        }

        [Test]
        public async Task GetFeatureDetails_SingleId_Exists_ReturnsFeatureDto()
        {
            var feature = CreateFeatureById(1886);
            features.Add(feature);

            var subject = CreateSubject();

            var response = await subject.GetFeatureDetailsById(new List<int> { 1886 });

            using (Assert.EnterMultipleScope())
            {
                var okResult = response.Result as OkObjectResult;
                var result = okResult.Value as List<FeatureDto>;

                Assert.That(result, Has.Count.EqualTo(1));

                var featureDto = result[0];
                Assert.That(featureDto.Id, Is.EqualTo(1886));
            }
        }

        [Test]
        public async Task GetFeatureDetails_SingleId_WithUnreadableLinkedPortfolios_SkipsFeature()
        {
            var visiblePortfolio = new Portfolio { Id = 1, Name = "Visible" };
            var hiddenPortfolio = new Portfolio { Id = 2, Name = "Hidden" };

            var visibleFeature = CreateFeatureById(1886);
            visibleFeature.Portfolios.Add(visiblePortfolio);

            var hiddenFeature = CreateFeatureById(1887);
            hiddenFeature.Portfolios.Add(hiddenPortfolio);

            features.Add(visibleFeature);
            features.Add(hiddenFeature);

            rbacAdministrationServiceMock
                .Setup(x => x.GetReadablePortfolioIdsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([visiblePortfolio.Id]);

            var subject = CreateSubject();

            var response = await subject.GetFeatureDetailsById(new List<int> { 1886, 1887 });

            var okResult = response.Result as OkObjectResult;
            var result = okResult!.Value as List<FeatureDto>;

            Assert.That(result!.Select(x => x.Id), Is.EqualTo([1886]));
        }

        [Test]
        public async Task GetFeatureDetails_SingleId_FiltersUnreadableProjectReferences()
        {
            var visiblePortfolio = new Portfolio { Id = 1, Name = "Visible" };
            var hiddenPortfolio = new Portfolio { Id = 2, Name = "Hidden" };

            var feature = CreateFeatureById(1886);
            feature.Portfolios.Add(visiblePortfolio);
            feature.Portfolios.Add(hiddenPortfolio);
            features.Add(feature);

            rbacAdministrationServiceMock
                .Setup(x => x.GetReadablePortfolioIdsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([visiblePortfolio.Id]);

            var subject = CreateSubject();
            var response = await subject.GetFeatureDetailsById(new List<int> { 1886 });

            var okResult = response.Result as OkObjectResult;
            var result = okResult!.Value as List<FeatureDto>;

            Assert.That(result![0].Projects.Select(x => x.Id), Is.EqualTo(new[] { visiblePortfolio.Id }));
        }

        /// <summary>
        /// Deliberate, and deliberately unlike the field beside it. The count of what a Feature waits on is
        /// taken across every Feature this Lighthouse holds, including ones the caller may not open, while
        /// the list of Portfolios standing in the way of a move next to it shows only what the caller may
        /// see. Counting only readable blockers would report nothing at all here, and a Feature that is
        /// waiting would look like a Feature that is free to go - the one thing the number exists to deny.
        /// Saying "waiting on one, and I cannot show you which" tells the truth and leaves the reader
        /// somewhere to go and ask; the blocker's name, its Portfolio and its state never leave the server.
        /// </summary>
        [Test]
        public async Task GetFeatureDetails_BlockerInAnUnreadablePortfolio_IsStillCounted()
        {
            var visiblePortfolio = new Portfolio { Id = 1, Name = "Visible" };
            var hiddenPortfolio = new Portfolio { Id = 2, Name = "Hidden" };

            var blocker = new Feature { Id = 4365, ReferenceId = TheBlockerBehindTheWall, Name = "Rebuild the search index" };
            blocker.Portfolios.Add(hiddenPortfolio);

            var waiting = new Feature { Id = 4366, ReferenceId = TheFeatureThatWaits, Name = "Publish the partner catalogue" };
            waiting.Portfolios.Add(visiblePortfolio);
            waiting.ReplaceDependsOnReferences(
                [new FeatureDependencyReference(waiting.Id, TheBlockerBehindTheWall, DependencySource.TrackerLink)]);

            features.Add(waiting);
            features.Add(blocker);

            rbacAdministrationServiceMock
                .Setup(x => x.GetReadablePortfolioIdsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([visiblePortfolio.Id]);

            var subject = CreateSubject();

            var response = await subject.GetFeatureDetailsById([waiting.Id, blocker.Id]);
            var result = (response.Result as OkObjectResult)!.Value as List<FeatureDto>;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result!.Select(dto => dto.ReferenceId), Is.EqualTo(OnlyTheFeatureThatWaits),
                    "The blocker has to stay out of the payload, or a count that reaches past the wall would not be what this is measuring.");

                Assert.That(result[0].DependsOnCount, Is.EqualTo(1),
                    "The Feature waits on one thing the caller may not read, and the count has to say so.");
            }
        }

        [Test]
        public async Task GetFeatureDetails_MultipleIds_AllExists_ReturnsFeatureDtos()
        {
            var feature1 = CreateFeatureById(18);
            features.Add(feature1);
            var feature2 = CreateFeatureById(86);
            features.Add(feature2);

            var subject = CreateSubject();

            var response = await subject.GetFeatureDetailsById(new List<int> { 18, 86 });

            using (Assert.EnterMultipleScope())
            {
                var okResult = response.Result as OkObjectResult;
                var result = okResult.Value as List<FeatureDto>;

                Assert.That(result, Has.Count.EqualTo(2));

                var featureDto1 = result[0];
                Assert.That(featureDto1.Id, Is.EqualTo(18));

                var featureDto2 = result[1];
                Assert.That(featureDto2.Id, Is.EqualTo(86));
            }
        }

        [Test]
        public async Task GetFeatureDetails_MultipleIds_SomeExist_ReturnsExistingFeatureDtos_SkipsMissing()
        {
            var feature = CreateFeatureById(1886);
            features.Add(feature);

            var subject = CreateSubject();

            var response = await subject.GetFeatureDetailsById(new List<int> { 1886, 1896 });

            using (Assert.EnterMultipleScope())
            {
                var okResult = response.Result as OkObjectResult;
                var result = okResult.Value as List<FeatureDto>;

                Assert.That(result, Has.Count.EqualTo(1));

                var featureDto = result[0];
                Assert.That(featureDto.Id, Is.EqualTo(1886));
            }
        }

        [Test]
        public async Task GetFeatureWorkItems_FeatureDoesNotExist_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var response = await subject.GetFeatureWorkItems(99);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());
                var notFoundResult = response.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            }
        }

        [Test]
        public async Task GetFeatureWorkItems_FeatureExists_ReturnsChildWorkItems()
        {
            var feature = CreateFeatureById(1886);
            feature.ReferenceId = "FTR-1886";
            features.Add(feature);

            workItems.Add(CreateWorkItem(1, "FTR-1886", "STORY-1", "Story 1"));
            workItems.Add(CreateWorkItem(2, "FTR-1886", "STORY-2", "Story 2"));
            workItems.Add(CreateWorkItem(3, "FTR-1000", "STORY-3", "Story 3"));

            var expectedStories = new[] { "STORY-1", "STORY-2" };

            var subject = CreateSubject();

            var response = await subject.GetFeatureWorkItems(1886);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = response.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                var items = okResult.Value as IEnumerable<WorkItemDto>;
                Assert.That(items?.Count(), Is.EqualTo(2));
                Assert.That(items?.Select(x => x.ReferenceId), Is.EquivalentTo(expectedStories));
            }
        }

        [Test]
        public async Task GetFeatureWorkItems_WhenFeatureHasNoReadablePortfolio_ReturnsNotFound()
        {
            var hiddenPortfolio = new Portfolio { Id = 2, Name = "Hidden" };
            var feature = CreateFeatureById(1886);
            feature.ReferenceId = "FTR-1886";
            feature.Portfolios.Add(hiddenPortfolio);
            features.Add(feature);

            rbacAdministrationServiceMock
                .Setup(x => x.GetReadablePortfolioIdsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<int>());

            var subject = CreateSubject();
            var response = await subject.GetFeatureWorkItems(1886);

            Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());
        }

        private Feature CreateFeatureByReferenceId(string referenceId)
        {
            var feature = new Feature
            {
                Id = featureCounter++,
                ReferenceId = referenceId,
                Url = $"https://example.com/feature/{referenceId}",
                Name = $"Feature {referenceId}",
            };

            return feature;
        }

        private Feature CreateFeatureById(int id)
        {
            var feature = new Feature
            {
                Id = id++,
                ReferenceId = $"{featureCounter++}",
                Url = $"https://example.com/feature/{id}",
                Name = $"Feature {id}",
            };

            return feature;
        }

        private WorkItem CreateWorkItem(int id, string parentReferenceId, string referenceId, string name)
        {
            return new WorkItem
            {
                Id = id,
                ParentReferenceId = parentReferenceId,
                ReferenceId = referenceId,
                Name = name,
                Type = "Story",
                State = "Doing",
                StateCategory = StateCategories.Doing,
            };
        }

        private FeaturesController CreateSubject()
        {
            var featurePositionMapMock = new Mock<IFeaturePositionMap>();
            featurePositionMapMock
                .Setup(x => x.GetAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => features.Union(parentFeatures)
                    .Select((feature, index) => (feature.Id, Position: index + 1))
                    .ToDictionary(entry => entry.Id, entry => entry.Position));

            // These tests are about the read paths, so the move surface answers "may move" for everything.
            // Deciding here who may move what would make every row's authorization this fixture's opinion
            // rather than the real rule's; that rule is judged where it lives, against the real service.
            var featureMoveAuthorizationMock = new Mock<IFeatureMoveAuthorization>();
            featureMoveAuthorizationMock
                .Setup(x => x.GetVerdictsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<IReadOnlyCollection<Feature>>(), It.IsAny<ISet<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ClaimsPrincipal _, IReadOnlyCollection<Feature> requested, ISet<int> _, CancellationToken _) =>
                    requested.ToDictionary(feature => feature.Id, _ => FeatureMoveVerdict.Allowed));

            return new FeaturesController(featureRepositoryMock.Object, workItemRepositoryMock.Object, blackoutPeriodServiceMock.Object, rbacAdministrationServiceMock.Object, Mock.Of<Lighthouse.Backend.Services.Interfaces.WorkItems.IBlockedItemService>(), featurePositionMapMock.Object, featureMoveAuthorizationMock.Object, Mock.Of<IFeatureRankingService>(), Mock.Of<IFeatureOrderingPolicyProvider>(), new Lighthouse.Backend.Tests.TestDoubles.FakeLighthouseClock(DateTimeOffset.UtcNow), new Lighthouse.Backend.Services.Implementation.Dependencies.DependencyHonourPolicy());
        }
    }
}
