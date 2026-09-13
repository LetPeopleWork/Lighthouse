using System.Globalization;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    public class InstanceCulturePinTest
    {
        /// <summary>
        /// Proves the culture pin took effect on this test host. A pin that quietly stopped working
        /// reads as covered while every formatting assertion silently follows whatever regional
        /// format the machine happens to have, which is worse than no pin at all. Never ignored.
        /// </summary>
        [Test]
        public void TestHost_RunsUnderThePinnedInstanceCulture()
        {
            Assert.That(
                CultureInfo.CurrentCulture.Name,
                Is.EqualTo(InstanceCulturePin.InstanceCultureName));
        }
    }
}
