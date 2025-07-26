using Lighthouse.Backend.API;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
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
                new TerminologyEntry { Key = "feature", Description = "A large work item", DefaultValue = "Epic" },
                new TerminologyEntry { Key = "story", Description = "A small work item", DefaultValue = "Task" }
            };
            
            terminologyServiceMock.Setup(x => x.UpdateTerminology(terminologyData)).Returns(Task.CompletedTask);

            var result = await subject.UpdateTerminology(terminologyData);

            Assert.That(result, Is.InstanceOf<OkResult>());
            var okResult = result as OkResult;
            Assert.That(okResult.StatusCode, Is.EqualTo(200));
            terminologyServiceMock.Verify(x => x.UpdateTerminology(terminologyData), Times.Once);
        }

        [Test]
        public async Task UpdateTerminologyAsync_NullData_ReturnsBadRequest()
        {
            var result = await subject.UpdateTerminology(null!);

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = result as BadRequestObjectResult;
            Assert.That(badRequestResult!.StatusCode, Is.EqualTo(400));
        }
    }
}
