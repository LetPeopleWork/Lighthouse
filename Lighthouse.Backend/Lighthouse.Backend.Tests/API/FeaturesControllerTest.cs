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

        private Mock<IRepository<Feature>> featureRepositoryMock;

        [SetUp]
        public void Setup()
        {
            featureRepositoryMock = new Mock<IRepository<Feature>>();

            parentFeatures.Clear();

            featureRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Feature, bool>>>()))
                .Returns((Expression<Func<Feature, bool>> predicate) => parentFeatures.Where(predicate.Compile()).AsQueryable());
        }

        [Test]
        public void GetParentFeatures_FeatureReferenceNotFound_ReturnsEmptyList()
        {
            var subject = CreateSubject();

            var response = subject.GetParentFeaturesById(["1886"]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = response.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                var parentFeatures = okResult.Value as List<FeatureDto>;

                Assert.That(parentFeatures, Is.Empty);
            };
        }

        [Test]
        public void GetParentFeatures_FeatureReferenceFound_ReturnsFeatureDto()
        {
            var feature = new Feature
            {
                ReferenceId = "1886",
                Url = "https://example.com/feature/1886",
                Order = "12",
                Name = "Test Feature",
            };
            parentFeatures.Add(feature);

            var subject = CreateSubject();

            var response = subject.GetParentFeaturesById(["1886"]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = response.Result as OkObjectResult;

                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                var parentFeatures = okResult.Value as List<FeatureDto>;
                Assert.That(parentFeatures, Has.Count.EqualTo(1));

                Assert.That(parentFeatures[0].ReferenceId, Is.EqualTo("1886"));
                Assert.That(parentFeatures[0].Url, Is.EqualTo("https://example.com/feature/1886"));
                Assert.That(parentFeatures[0].Name, Is.EqualTo("Test Feature"));
            };
        }

        [Test]
        public void GetParentFeatures_FeatureReferenceFoundWithMultipleIds_ReturnsAllMatchingFeatures()
        {
            var feature1 = new Feature
            {
                ReferenceId = "1886",
                Url = "https://example.com/feature/1886",
                Order = "12",
                Name = "Test Feature 1",
            };
            var feature2 = new Feature
            {
                ReferenceId = "1887",
                Url = "https://example.com/feature/1887",
                Order = "13",
                Name = "Test Feature 2",
            };
            parentFeatures.Add(feature1);
            parentFeatures.Add(feature2);
            
            var subject = CreateSubject();
            
            var response = subject.GetParentFeaturesById(["1886", "1887"]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = response.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                var parentFeatures = okResult.Value as List<FeatureDto>;
                Assert.That(parentFeatures, Has.Count.EqualTo(2));

                Assert.That(parentFeatures[0].ReferenceId, Is.EqualTo("1886"));
                Assert.That(parentFeatures[0].Url, Is.EqualTo("https://example.com/feature/1886"));
                Assert.That(parentFeatures[0].Name, Is.EqualTo("Test Feature 1"));

                Assert.That(parentFeatures[1].ReferenceId, Is.EqualTo("1887"));
                Assert.That(parentFeatures[1].Url, Is.EqualTo("https://example.com/feature/1887"));
                Assert.That(parentFeatures[1].Name, Is.EqualTo("Test Feature 2"));
            };
        }

        [Test]
        public void GetParentFeatures_FeaturesFound_ReturnsInCorrectOrder()
        {

            var feature1 = new Feature
            {
                ReferenceId = "1886",
                Url = "https://example.com/feature/1886",
                Order = "86",
                Name = "Test Feature 1",
            };

            var feature2 = new Feature
            {
                ReferenceId = "1887",
                Url = "https://example.com/feature/1887",
                Order = "18",
                Name = "Test Feature 2",
            };
            parentFeatures.Add(feature1);
            parentFeatures.Add(feature2);

            var subject = CreateSubject();

            var response = subject.GetParentFeaturesById(["1886", "1887"]);

            using (Assert.EnterMultipleScope())
            {
                var okResult = response.Result as OkObjectResult;
                var parentFeatures = okResult.Value as List<FeatureDto>;

                Assert.That(parentFeatures[0].ReferenceId, Is.EqualTo("1887"));
                Assert.That(parentFeatures[1].ReferenceId, Is.EqualTo("1886"));
            };
        }

        private FeaturesController CreateSubject()
        {
            return new FeaturesController(featureRepositoryMock.Object);
        }
    }
}
