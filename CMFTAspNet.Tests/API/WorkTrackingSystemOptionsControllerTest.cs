using CMFTAspNet.API;
using CMFTAspNet.Factories;
using CMFTAspNet.WorkTracking;
using Moq;

namespace CMFTAspNet.Tests.API
{
    public class WorkTrackingSystemOptionsControllerTest
    {
        [Test]
        public void OnGet_ReturnsOptionsProvidedByFactory()
        {
            var factoryMock = new Mock<IWorkTrackingOptionsFactory>();
            var subject = new WorkTrackingSystemOptionsController(factoryMock.Object);

            var expectedOptions = new List<WorkTrackingSystemOption>
            {
                new WorkTrackingSystemOption("Key", "Value", true)
            };

            factoryMock.Setup(x => x.CreateOptionsForWorkTrackingSystem(WorkTrackingSystems.AzureDevOps)).Returns(expectedOptions);

            // Act
            var result = subject.Get(WorkTrackingSystems.AzureDevOps);

            CollectionAssert.AreEqual(expectedOptions, result);
        }
    }
}
