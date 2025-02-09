using Lighthouse.Backend.API;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Preview;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.TeamFoundation.TestManagement.WebApi;
using Moq;
using NuGet.Configuration;

namespace Lighthouse.Backend.Tests.API
{
    public class PreviewFeaturesControllerTest
    {
        private Mock<IRepository<PreviewFeature>> repositoryMock;

        [SetUp]
        public void Setup()
        {
            repositoryMock = new Mock<IRepository<PreviewFeature>>();
        }

        [Test]
        public void GetAllFeatures_ReturnsFromRepository()
        {
            var features = new List<PreviewFeature>
            {
                new PreviewFeature { Id = 0, Key = "Key1", Name = "Feature 1", Description = "Foo", Enabled = false },
                new PreviewFeature { Id = 1, Key = "Key2", Name = "Feature 2", Description = "Bar", Enabled = true },
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
        public void GetPreviewFeautreByKey_KeyDoesNotExist_ReturnsNotFound()
        {
            repositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<PreviewFeature, bool>>())).Returns((PreviewFeature)null);

            var subject = CreateSubject();

            var response = subject.GetPreviewFeatureByKey("InexistingKey");

            Assert.Multiple(() =>
            {
                var notFoundResult = response.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            });
        }

        [Test]
        public void GetPreviewFeautreByKey_KeyExists_ReturnsFeature()
        {
            var feature = new PreviewFeature { Id = 0, Key = "Key1", Name = "Feature 1", Description = "Foo", Enabled = false };
            repositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<PreviewFeature, bool>>())).Returns(feature);

            var subject = CreateSubject();

            var response = subject.GetPreviewFeatureByKey("Key1");

            Assert.Multiple(() =>
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = response.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                Assert.That(okResult.Value, Is.EqualTo(feature));
            });
        }

        [Test]
        public async Task UpdatePreviewFeature_FeatureWithIdDoesNotExist_ReturnsNotFound()
        {
            var feature = new PreviewFeature { Id = 0, Key = "Key1", Name = "Feature 1", Description = "Foo", Enabled = false };
            repositoryMock.Setup(x => x.GetById(1)).Returns((PreviewFeature)null);

            var subject = CreateSubject();

            var response = await subject.UpdatePreviewFeature(1, feature);

            Assert.Multiple(() =>
                {
                    var notFoundResult = response.Result as NotFoundResult;
                    Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
                });
        }

        [Test]
        public async Task UpdatePreviewFeature_FeatureWithIdExists_Updates()
        {
            var feature = new PreviewFeature { Id = 0, Key = "Key1", Name = "Feature 1", Description = "Foo", Enabled = false };
            repositoryMock.Setup(x => x.GetById(0)).Returns(feature);

            var subject = CreateSubject();

            var response = await subject.UpdatePreviewFeature(0, feature);

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

        private PreviewFeaturesController CreateSubject()
        {
            return new PreviewFeaturesController(repositoryMock.Object);
        }
    }
}
