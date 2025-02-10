using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class WorkTrackingSystemConnectionOptionDtoTest
    {
        [Test]
        public void CreateOption_IsNotSecret_StoresValue()
        {
            var notASecret = "I am the cutest";
            var connectionOption = new WorkTrackingSystemConnectionOption
            {
                Id = 12,
                Key = "Cutiepie",
                Value = notASecret,
                IsSecret = false
            };

            var subject = new WorkTrackingSystemConnectionOptionDto(connectionOption);

            Assert.That(subject.Value, Is.EqualTo(notASecret));
        }
        [Test]
        public void CreateOption_IsSecret_DoesNotStoreValue()
        {
            var secret = "I know what you did last summer";
            var connectionOption = new WorkTrackingSystemConnectionOption
            {
                Id = 12,
                Key = "Mystery",
                Value = secret,
                IsSecret = true
            };

            var subject = new WorkTrackingSystemConnectionOptionDto(connectionOption);

            Assert.That(subject.Value, Is.EqualTo(string.Empty));
        }
    }
}
