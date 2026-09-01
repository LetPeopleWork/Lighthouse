using Lighthouse.Backend.API;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Implementation.OptionalFeatures;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    public class OptionalFeaturesControllerTest
    {
        // Spelled out here rather than shared with the controller on purpose: the test is what proves the
        // wording a refused administrator actually reads, so it must fail if the controller changes it.
        private const string PremiumRefusal = "Access Denied: Premium Features Required";

        private Mock<IRepository<OptionalFeature>> repositoryMock;

        private Mock<ILicenseService> licenseServiceMock;

        [SetUp]
        public void Setup()
        {
            repositoryMock = new Mock<IRepository<OptionalFeature>>();
            licenseServiceMock = new Mock<ILicenseService>();
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

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = response.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                Assert.That(okResult.Value, Is.EqualTo(features));
            }
        }

        [Test]
        public void GetOptionalFeatureByKey_KeyDoesNotExist_ReturnsNotFound()
        {
            repositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<OptionalFeature, bool>>())).Returns((OptionalFeature)null);

            var subject = CreateSubject();

            var response = subject.GetOptionalFeatureByKey("InexistingKey");

            using (Assert.EnterMultipleScope())
            {
                var notFoundResult = response.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            }
        }

        [Test]
        public void GetOptionalFeatureByKey_KeyExists_ReturnsFeature()
        {
            var feature = new OptionalFeature { Id = 0, Key = "Key1", Name = "Feature 1", Description = "Foo", Enabled = false };
            repositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<OptionalFeature, bool>>())).Returns(feature);

            var subject = CreateSubject();

            var response = subject.GetOptionalFeatureByKey("Key1");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = response.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                Assert.That(okResult.Value, Is.EqualTo(feature));
            }
        }

        [Test]
        public async Task UpdateOptionalFeature_SettingWithKeyDoesNotExist_ReturnsNotFound()
        {
            var feature = new OptionalFeature { Id = 0, Key = "Key1", Name = "Feature 1", Description = "Foo", Enabled = false };
            repositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<OptionalFeature, bool>>())).Returns((OptionalFeature)null);

            var subject = CreateSubject();

            var response = await subject.UpdateOptionalFeature("InexistingKey", feature);

            using (Assert.EnterMultipleScope())
            {
                var notFoundResult = response.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            }
        }

        [Test]
        public async Task UpdateOptionalFeature_SettingWithKeyExists_Updates()
        {
            var feature = new OptionalFeature { Id = 0, Key = "Key1", Name = "Feature 1", Description = "Foo", Enabled = false };
            repositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<OptionalFeature, bool>>())).Returns(feature);

            var subject = CreateSubject();

            var response = await subject.UpdateOptionalFeature("Key1", feature);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = response.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                Assert.That(okResult.Value, Is.EqualTo(feature));

                repositoryMock.Verify(x => x.Update(feature));
                repositoryMock.Verify(x => x.Save());
            }
        }

        [Test]
        [TestCase(false, false, true, StatusCodes.Status200OK)]
        [TestCase(false, true, true, StatusCodes.Status200OK)]
        [TestCase(true, false, false, StatusCodes.Status403Forbidden)]
        [TestCase(true, true, true, StatusCodes.Status200OK)]
        public async Task UpdateOptionalFeature_IsPremium_OnlyEnablesIfUserHasLicense(bool isPremiumFeature, bool hasLicense, bool executeUpdate, int expectedStatusCode)
        {
            var feature = new OptionalFeature { Id = 0, Key = "Key1", Name = "Feature 1", Description = "Foo", Enabled = false, IsPremium = isPremiumFeature };
            repositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<OptionalFeature, bool>>())).Returns(feature);
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(hasLicense);

            var subject = CreateSubject();

            var response = await subject.UpdateOptionalFeature("Key1", feature);

            var result = response.Result as ObjectResult;
            Assert.That(result, Is.Not.Null);

            var expectedBody = expectedStatusCode == StatusCodes.Status403Forbidden ? PremiumRefusal : (object)feature;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.StatusCode, Is.EqualTo(expectedStatusCode));
                Assert.That(result.Value, Is.EqualTo(expectedBody));
            }

            var expectedExecutionTimes = executeUpdate ? Times.Once() : Times.Never();
            repositoryMock.Verify(x => x.Update(feature), expectedExecutionTimes);
            repositoryMock.Verify(x => x.Save(), expectedExecutionTimes);
        }

        [Test]
        public void UpdateOptionalFeature_HasSystemAdminRbacGuardAttribute()
        {
            var method = typeof(OptionalFeaturesController).GetMethod(nameof(OptionalFeaturesController.UpdateOptionalFeature));
            var attribute = method?.GetCustomAttributes(typeof(RbacGuardAttribute), inherit: true)
                .Cast<RbacGuardAttribute>()
                .SingleOrDefault();

            Assert.That(attribute, Is.Not.Null);
            Assert.That(attribute!.Requirement, Is.EqualTo(RbacGuardRequirement.SystemAdmin));
        }

        private OptionalFeaturesController CreateSubject()
        {
            // The real registry, holding only the applier that stores the value and does nothing else.
            // The settings these tests use have no consequences of their own, and building the registry
            // for real is what proves the controller reaches an applier at all rather than a stand-in.
            var registry = new OptionalFeatureApplierRegistry([], new DefaultOptionalFeatureApplier(repositoryMock.Object));

            return new OptionalFeaturesController(repositoryMock.Object, licenseServiceMock.Object, registry);
        }
    }
}
