using Lighthouse.Backend.Modules.FlowMetrics.Domain;

namespace Lighthouse.Backend.Tests.Modules.FlowMetrics.Domain
{
    public class ServiceLevelExpectationTest
    {
        [TestCase(0, 14)]
        [TestCase(101, 14)]
        [TestCase(85, 0)]
        [TestCase(0, 0)]
        public void Of_APercentileOutsideOneToAHundredOrLessThanOneDay_IsNoSle(int percentile, int days)
        {
            Assert.That(ServiceLevelExpectation.Of(percentile, days), Is.Null);
        }

        [TestCase(1, 1)]
        [TestCase(100, 1)]
        [TestCase(85, 14)]
        public void Of_TheEdgesOfTheValidRange_AreAnSle(int percentile, int days)
        {
            var sle = ServiceLevelExpectation.Of(percentile, days);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(sle, Is.Not.Null);
                Assert.That(sle!.Percentile, Is.EqualTo(percentile));
                Assert.That(sle.Days, Is.EqualTo(days));
            }
        }

        [Test]
        public void TwoSlesWithTheSamePercentileAndDays_AreEqual()
        {
            var sle = ServiceLevelExpectation.Of(85, 14);
            var same = ServiceLevelExpectation.Of(85, 14);

            var equalAsSle = sle!.Equals(same);
            var equalAsObject = sle.Equals((object?)same);
            var sameHash = sle.GetHashCode() == same!.GetHashCode();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(equalAsSle, Is.True);
                Assert.That(equalAsObject, Is.True);
                Assert.That(sameHash, Is.True);
            }
        }

        [TestCase(70, 14)]
        [TestCase(85, 21)]
        public void SlesThatDifferInPercentileOrDays_AreNotEqual(int percentile, int days)
        {
            var sle = ServiceLevelExpectation.Of(85, 14);

            var equal = sle!.Equals(ServiceLevelExpectation.Of(percentile, days));

            Assert.That(equal, Is.False);
        }

        [Test]
        public void AnSle_IsNotEqualToNothingOrToAnotherKindOfValue()
        {
            var sle = ServiceLevelExpectation.Of(85, 14);

            var equalToNothing = sle!.Equals(null);
            var equalToText = sle.Equals((object)"85 % in 14 days");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(equalToNothing, Is.False);
                Assert.That(equalToText, Is.False);
            }
        }
    }
}
