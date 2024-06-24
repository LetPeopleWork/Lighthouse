using Lighthouse.Backend.API;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.WorkTracking;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    public class WorkTrackingSystemOptionsControllerTest
    {
        [Test]
        public void OnGet_IsTeamOption_ReturnsOptionsProvidedByFactory()
        {
            var factoryMock = new Mock<IWorkTrackingOptionsFactory>();
            var subject = new WorkTrackingSystemOptionsController(factoryMock.Object);

            var expectedOptions = new List<WorkTrackingSystemOption<Team>>
            {
                new WorkTrackingSystemOption<Team>("Key", "Value", true)
            };

            factoryMock.Setup(x => x.CreateOptionsForWorkTrackingSystem<Team>(WorkTrackingSystems.AzureDevOps)).Returns(expectedOptions);

            // Act
            var result = subject.GetTeamWorktrackingOptions(WorkTrackingSystems.AzureDevOps);

            CollectionAssert.AreEqual(expectedOptions, result);
        }
        [Test]
        public void OnGet_IsProjectOption_ReturnsOptionsProvidedByFactory()
        {
            var factoryMock = new Mock<IWorkTrackingOptionsFactory>();
            var subject = new WorkTrackingSystemOptionsController(factoryMock.Object);

            var expectedOptions = new List<WorkTrackingSystemOption<Project>>
            {
                new WorkTrackingSystemOption<Project>("Key", "Value", true)
            };

            factoryMock.Setup(x => x.CreateOptionsForWorkTrackingSystem<Project>(WorkTrackingSystems.AzureDevOps)).Returns(expectedOptions);

            // Act
            var result = subject.GetProjectWorktrackingOptions(WorkTrackingSystems.AzureDevOps);

            CollectionAssert.AreEqual(expectedOptions, result);
        }
    }
}
