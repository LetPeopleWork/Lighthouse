using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Linq.Expressions;

namespace Lighthouse.Backend.Tests.API
{
    public class FeaturesControllerTest
    {
        private readonly List<Feature> parentFeatures = new List<Feature>();
        private readonly List<Feature> features = new List<Feature>();
        private readonly List<WorkItem> workItems = new List<WorkItem>();

        private static int featureCounter = 0;

        private Mock<IRepository<Feature>> featureRepositoryMock;
        private Mock<IWorkItemRepository> workItemRepositoryMock;

        [SetUp]
        public void Setup()
        {
            featureRepositoryMock = new Mock<IRepository<Feature>>();
            workItemRepositoryMock = new Mock<IWorkItemRepository>();

            features.Clear();
            parentFeatures.Clear();
            workItems.Clear();

            featureRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Feature, bool>>>()))
                .Returns((Expression<Func<Feature, bool>> predicate) => features.Union(parentFeatures).Where(predicate.Compile()).AsQueryable());

            featureRepositoryMock.Setup(x => x.GetById(It.IsAny<int>()))
                .Returns((int id) => features.Union(parentFeatures).SingleOrDefault(f => f.Id == id));

            workItemRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<WorkItem, bool>>>()))
                .Returns((Expression<Func<WorkItem, bool>> predicate) => workItems.Where(predicate.Compile()).AsQueryable());
        }

        [Test]
        public void GetParentFeatures_FeatureReferenceNotFound_ReturnsEmptyList()
        {
            var subject = CreateSubject();

            var response = subject.GetFeatureDetailsByReference(["1886"]);

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
        public void GetParentFeatures_FeatureReferenceFound_ReturnsFeatureDto()
        {
            var feature = CreateFeatureByReferenceId("1886");
            parentFeatures.Add(feature);

            var subject = CreateSubject();

            var response = subject.GetFeatureDetailsByReference(["1886"]);

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
        public void GetParentFeatures_FeatureReferenceFoundWithMultipleIds_ReturnsAllMatchingFeatures()
        {
            var feature1 = CreateFeatureByReferenceId("1886");
            var feature2 = CreateFeatureByReferenceId("1887");

            parentFeatures.Add(feature1);
            parentFeatures.Add(feature2);

            var subject = CreateSubject();

            var response = subject.GetFeatureDetailsByReference(["1886", "1887"]);

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
        public void GetParentFeatures_FeaturesFound_ReturnsInCorrectOrder()
        {
            var feature1 = CreateFeatureByReferenceId("1886");
            feature1.Order = "12";

            var feature2 = CreateFeatureByReferenceId("1887");
            feature2.Order = "2";

            parentFeatures.Add(feature1);
            parentFeatures.Add(feature2);

            var subject = CreateSubject();

            var response = subject.GetFeatureDetailsByReference(["1886", "1887"]);

            using (Assert.EnterMultipleScope())
            {
                var okResult = response.Result as OkObjectResult;
                var result = okResult.Value as List<FeatureDto>;

                Assert.That(result[0].ReferenceId, Is.EqualTo("1887"));
                Assert.That(result[1].ReferenceId, Is.EqualTo("1886"));
            }
        }

        [Test]
        public void GetFeatureDetails_NoParameter_ReturnsBadRequest()
        {
            var subject = CreateSubject();

            var response = subject.GetFeatureDetailsById(new List<int>());

            Assert.That(response.Result, Is.InstanceOf<BadRequestResult>());
        }

        [Test]
        public void GetFeatureDetails_SingleId_DoesNotExist_ReturnsEmptyList()
        {
            var subject = CreateSubject();

            var response = subject.GetFeatureDetailsById(new List<int> { 1886 });


            var okResult = response.Result as OkObjectResult;
            var result = okResult.Value as List<FeatureDto>;

            Assert.That(result, Has.Count.EqualTo(0));
        }

        [Test]
        public void GetFeatureDetails_SingleId_Exists_ReturnsFeatureDto()
        {
            var feature = CreateFeatureById(1886);
            features.Add(feature);

            var subject = CreateSubject();

            var response = subject.GetFeatureDetailsById(new List<int> { 1886 });

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
        public void GetFeatureDetails_MultipleIds_AllExists_ReturnsFeatureDtos()
        {
            var feature1 = CreateFeatureById(18);
            features.Add(feature1);
            var feature2 = CreateFeatureById(86);
            features.Add(feature2);

            var subject = CreateSubject();

            var response = subject.GetFeatureDetailsById(new List<int> { 18, 86 });

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
        public void GetFeatureDetails_MultipleIds_SomeExist_ReturnsExistingFeatureDtos_SkipsMissing()
        {
            var feature = CreateFeatureById(1886);
            features.Add(feature);

            var subject = CreateSubject();

            var response = subject.GetFeatureDetailsById(new List<int> { 1886, 1896 });

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
        public void GetFeatureWorkItems_FeatureDoesNotExist_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var response = subject.GetFeatureWorkItems(99);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());
                var notFoundResult = response.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            }
        }

        [Test]
        public void GetFeatureWorkItems_FeatureExists_ReturnsChildWorkItems()
        {
            var feature = CreateFeatureById(1886);
            feature.ReferenceId = "FTR-1886";
            features.Add(feature);

            workItems.Add(CreateWorkItem(1, "FTR-1886", "STORY-1", "Story 1"));
            workItems.Add(CreateWorkItem(2, "FTR-1886", "STORY-2", "Story 2"));
            workItems.Add(CreateWorkItem(3, "FTR-1000", "STORY-3", "Story 3"));

            var expectedStories = new[] { "STORY-1", "STORY-2" };

            var subject = CreateSubject();

            var response = subject.GetFeatureWorkItems(1886);

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
            return new FeaturesController(featureRepositoryMock.Object, workItemRepositoryMock.Object);
        }
    }
}
