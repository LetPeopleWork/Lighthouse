using CMFTAspNet.Services;

namespace CMFTAspNet.Tests.Services
{    
    public class ThroughputServiceTest
    {
        [Test]
        public void GetThroughput_Returns_Two_Weeks()
        {
            var subject = new ThroughputService();

            var throughput = subject.GetThroughput();

            Assert.That(throughput.History, Is.EqualTo(14));
        }

        [Test]
        public void GetThroughput_Returns_ExpectedThroughput()
        {
            var subject = new ThroughputService();

            var throughput = subject.GetThroughput();

            for (var index = 0; index < throughput.History; index++)
            {
                Assert.That(1, Is.EqualTo(throughput.GetThroughputOnDay(index)));
            }
        }
    }
}
