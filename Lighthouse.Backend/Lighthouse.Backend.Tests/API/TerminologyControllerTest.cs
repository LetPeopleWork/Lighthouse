using Lighthouse.Backend.API;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    [TestFixture]
    public class TerminologyControllerTest
    {
        private Mock<ITerminologyService> terminologyServiceMock;
        private TerminologyController subject;

        [SetUp]
        public void SetUp()
        {
            terminologyServiceMock = new Mock<ITerminologyService>();
            subject = new TerminologyController(terminologyServiceMock.Object);
        }

        [Test]
        public void TerminologyController_HasAuthorizeAttribute()
        {
            var attribute = typeof(TerminologyController)
                .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Cast<AuthorizeAttribute>()
                .SingleOrDefault();

            Assert.That(attribute, Is.Not.Null);
        }

        [Test]
        public void UpdateTerminology_HasSystemAdminRbacGuardAttribute()
        {
            var method = typeof(TerminologyController).GetMethod(nameof(TerminologyController.UpdateTerminology));
            var attribute = method?
                .GetCustomAttributes(typeof(RbacGuardAttribute), inherit: true)
                .Cast<RbacGuardAttribute>()
                .SingleOrDefault();

            Assert.That(attribute, Is.Not.Null);
            Assert.That(attribute!.Requirement, Is.EqualTo(RbacGuardRequirement.SystemAdmin));
        }

        [Test]
        public void GetAllTerminology_ServiceReturnsData_ReturnsOkWithData()
        {
            var expectedTerminology = new List<TerminologyEntry>
            {
                new TerminologyEntry { Key = "feature", Description = "A large work item", DefaultValue = "Epic" },
                new TerminologyEntry { Key = "story", Description = "A small work item", DefaultValue = "User Story" }
            };

            terminologyServiceMock.Setup(x => x.GetAll()).Returns(expectedTerminology);

            var result = subject.GetAllTerminology();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                Assert.That(okResult.Value, Is.EqualTo(expectedTerminology));
            }
        }

        [Test]
        public void GetAllTerminology_ServiceReturnsEmptyList_ReturnsOkWithEmptyData()
        {
            var expectedTerminology = new List<TerminologyEntry>();
            terminologyServiceMock.Setup(x => x.GetAll()).Returns(expectedTerminology);

            var result = subject.GetAllTerminology();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                Assert.That(okResult.Value, Is.EqualTo(expectedTerminology));
            }
        }

        [Test]
        public async Task UpdateTerminology_ValidData_ReturnsOk()
        {
            var terminologyData = new List<TerminologyEntry>
            {
                new() { Key = "feature", Description = "A large work item", DefaultValue = "Epic" },
                new() { Key = "story", Description = "A small work item", DefaultValue = "Task" }
            };
            
            terminologyServiceMock.Setup(x => x.UpdateTerminology(terminologyData)).Returns(Task.CompletedTask);

            var result = await subject.UpdateTerminology(terminologyData);

            Assert.That(result, Is.InstanceOf<OkResult>());
            var okResult = result as OkResult;
            Assert.That(okResult.StatusCode, Is.EqualTo(200));
            terminologyServiceMock.Verify(x => x.UpdateTerminology(terminologyData), Times.Once);
        }
    }
}
