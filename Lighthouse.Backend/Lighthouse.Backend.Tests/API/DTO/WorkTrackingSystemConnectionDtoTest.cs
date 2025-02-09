using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.WorkTracking;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class WorkTrackingSystemConnectionDtoTest
    {
        [Test]
        public void Create_SetsIdAndNameCorrect()
        {
            var workTrackingSystemConnection = new WorkTrackingSystemConnection();
            workTrackingSystemConnection.Id = 37;
            workTrackingSystemConnection.Name = "My Connect";
            workTrackingSystemConnection.WorkTrackingSystem = WorkTrackingSystems.AzureDevOps;

            var subject = new WorkTrackingSystemConnectionDto(workTrackingSystemConnection);

            Assert.Multiple(() =>
            {
                Assert.That(subject.Id, Is.EqualTo(workTrackingSystemConnection.Id));
                Assert.That(subject.Name, Is.EqualTo(workTrackingSystemConnection.Name));
                Assert.That(subject.WorkTrackingSystem, Is.EqualTo(workTrackingSystemConnection.WorkTrackingSystem));
            });
        }

        [Test]
        public void Create_SetsTrackingOptions()
        {
            var workTrackingSystemConnection = new WorkTrackingSystemConnection();
            workTrackingSystemConnection.Options.Add(new WorkTrackingSystemConnectionOption());

            var subject = new WorkTrackingSystemConnectionDto(workTrackingSystemConnection);

            Assert.That(subject.Options, Has.Count.EqualTo(1));
        }
    }
}
