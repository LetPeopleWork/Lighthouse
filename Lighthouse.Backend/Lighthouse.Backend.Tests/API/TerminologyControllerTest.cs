using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    [TestFixture]
    public class TerminologyControllerTest
    {
        private Mock<ITerminologyService> terminologyServiceMock;
        private TerminologyController controller;

        [SetUp]
        public void SetUp()
        {
            terminologyServiceMock = new Mock<ITerminologyService>();
            controller = new TerminologyController(terminologyServiceMock.Object);
        }

        [Test]
        public void GetTerminology_ServiceReturnsTerminology_ReturnsOkWithTerminology()
        {
            // Arrange
            var expectedTerminology = new TerminologyDto
            {
                WorkItem = "Task",
                WorkItems = "Tasks"
            };
            terminologyServiceMock.Setup(x => x.GetTerminology()).Returns(expectedTerminology);

            // Act
            var result = controller.GetTerminology();

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                Assert.That(okResult.Value, Is.EqualTo(expectedTerminology));
            }
        }

        [Test]
        public void GetTerminology_ServiceThrowsException_ExceptionPropagates()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Service error");
            terminologyServiceMock.Setup(x => x.GetTerminology()).Throws(expectedException);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => controller.GetTerminology());
            Assert.That(exception.Message, Is.EqualTo("Service error"));
        }

        [Test]
        public void GetTerminology_ServiceReturnsNull_ReturnsOkWithNull()
        {
            var result = controller.GetTerminology();

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                Assert.That(okResult.Value, Is.Null);
            }
        }
    }
}
