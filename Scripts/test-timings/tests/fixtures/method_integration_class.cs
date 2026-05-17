using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Mixed
{
    public class MixedTest
    {
        [Test]
        public void UnitOnlyMethod() { }

        [Test]
        [Category("Integration")]
        public void IntegrationMethod() { }

        [TestCase("a")]
        [TestCase("b")]
        [Category("Integration")]
        public void IntegrationParametrized(string value) { }
    }
}
