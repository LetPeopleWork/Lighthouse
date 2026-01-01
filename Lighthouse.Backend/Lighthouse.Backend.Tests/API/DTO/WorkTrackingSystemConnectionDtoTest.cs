using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;

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

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.Id, Is.EqualTo(workTrackingSystemConnection.Id));
                Assert.That(subject.Name, Is.EqualTo(workTrackingSystemConnection.Name));
                Assert.That(subject.WorkTrackingSystem, Is.EqualTo(workTrackingSystemConnection.WorkTrackingSystem));
            };
        }

        [Test]
        public void Create_SetsTrackingOptions()
        {
            var workTrackingSystemConnection = new WorkTrackingSystemConnection();
            workTrackingSystemConnection.Options.Add(new WorkTrackingSystemConnectionOption());

            var subject = new WorkTrackingSystemConnectionDto(workTrackingSystemConnection);

            Assert.That(subject.Options, Has.Count.EqualTo(1));
        }

        [Test]
        public void Create_SetsAuthenticationMethodKey()
        {
            var workTrackingSystemConnection = new WorkTrackingSystemConnection
            {
                AuthenticationMethodKey = "ado.pat"
            };

            var subject = new WorkTrackingSystemConnectionDto(workTrackingSystemConnection);

            Assert.That(subject.AuthenticationMethodKey, Is.EqualTo("ado.pat"));
        }

        [Test]
        public void Create_AuthenticationMethodKey_WhenEmpty_RemainsEmpty()
        {
            var workTrackingSystemConnection = new WorkTrackingSystemConnection();

            var subject = new WorkTrackingSystemConnectionDto(workTrackingSystemConnection);

            Assert.That(subject.AuthenticationMethodKey, Is.EqualTo(string.Empty));
        }
    }
}
