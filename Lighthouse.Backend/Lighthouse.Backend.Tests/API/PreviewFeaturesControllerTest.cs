using Lighthouse.Backend.API;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    public class OptionalFeaturesControllerTest
    {
        private Mock<IRepository<OptionalFeature>> repositoryMock;

        [SetUp]
        public void Setup()
        {
            repositoryMock = new Mock<IRepository<OptionalFeature>>();
        }

        [Test]
        public void GetAllFeatures_ReturnsFromRepository()
        {
            var features = new List<OptionalFeature>
            {
                new OptionalFeature { Id = 0, Key = "Key1", Name = "Feature 1", Description = "Foo", Enabled = false },
                new OptionalFeature { Id = 1, Key = "Key2", Name = "Feature 2", Description = "Bar", Enabled = true },
            };

            repositoryMock.Setup(x => x.GetAll()).Returns(features);

            var subject = CreateSubject();

            var response = subject.GetAll();

            Assert.Multiple(() =>
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = response.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                Assert.That(okResult.Value, Is.EqualTo(features));
            });
        }

        [Test]
        public void GetOptionalFeautreByKey_KeyDoesNotExist_ReturnsNotFound()
        {
            repositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<OptionalFeature, bool>>())).Returns((OptionalFeature)null);

            var subject = CreateSubject();

            var response = subject.GetOptionalFeatureByKey("InexistingKey");

            Assert.Multiple(() =>
            {
                var notFoundResult = response.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            });
        }

        [Test]
        public void GetOptionalFeautreByKey_KeyExists_ReturnsFeature()
        {
            var feature = new OptionalFeature { Id = 0, Key = "Key1", Name = "Feature 1", Description = "Foo", Enabled = false };
            repositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<OptionalFeature, bool>>())).Returns(feature);

            var subject = CreateSubject();

            var response = subject.GetOptionalFeatureByKey("Key1");

            Assert.Multiple(() =>
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = response.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                Assert.That(okResult.Value, Is.EqualTo(feature));
            });
        }

        [Test]
        public async Task UpdateOptionalFeature_FeatureWithIdDoesNotExist_ReturnsNotFound()
        {
            var feature = new OptionalFeature { Id = 0, Key = "Key1", Name = "Feature 1", Description = "Foo", Enabled = false };
            repositoryMock.Setup(x => x.GetById(1)).Returns((OptionalFeature)null);

            var subject = CreateSubject();

            var response = await subject.UpdateOptionalFeature(1, feature);

            Assert.Multiple(() =>
                {
                    var notFoundResult = response.Result as NotFoundResult;
                    Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
                });
        }

        [Test]
        public async Task UpdateOptionalFeature_FeatureWithIdExists_Updates()
        {
            var feature = new OptionalFeature { Id = 0, Key = "Key1", Name = "Feature 1", Description = "Foo", Enabled = false };
            repositoryMock.Setup(x => x.GetById(0)).Returns(feature);

            var subject = CreateSubject();

            var response = await subject.UpdateOptionalFeature(0, feature);

            Assert.Multiple(() =>
                {
                    Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                    var okResult = response.Result as OkObjectResult;
                    Assert.That(okResult.StatusCode, Is.EqualTo(200));
                    Assert.That(okResult.Value, Is.EqualTo(feature));

                    repositoryMock.Verify(x => x.Update(feature));
                    repositoryMock.Verify(x => x.Save());
                });
        }

        private OptionalFeaturesController CreateSubject()
        {
            return new OptionalFeaturesController(repositoryMock.Object);
        }
    }
}
