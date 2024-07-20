using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    public class WorkTrackingSystemConnectionTest
    {
        [Test]
        public void GetWorkTrackingSystemConnectionOptionByKey_NoOptionWithKey_Throws()
        {
            var subject = CreateSubject();

            Assert.Throws<ArgumentException>(() => subject.GetWorkTrackingSystemConnectionOptionByKey("MyKey"));
        }

        [Test]
        public void GetWorkTrackingSystemConnectionOptionByKey_OptionAvailable_ReturnsValue()
        {
            var subject = CreateSubject();
            var value = subject.GetWorkTrackingSystemConnectionOptionByKey("Key");

            Assert.That(value, Is.EqualTo("Value"));
        }

        private WorkTrackingSystemConnection CreateSubject()
        {
            var subject = new WorkTrackingSystemConnection();
            var option = new WorkTrackingSystemConnectionOption
            {
                Key = "Key",
                Value = "Value"
            };

            subject.Options.Add(option);

            return subject;
        }
    }
}
