using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.OAuth;

namespace Lighthouse.Backend.Tests.Services.Implementation.OAuth
{
    [TestFixture]
    public class OAuthSchemaExtensionsTest
    {
        private static readonly string[] FirstKeys = ["first.oauth"];
        private static readonly string[] SecondKeys = ["second.oauth"];

        [Test]
        public void TwoInstances_HoldDisjointExtraKeys_WithoutBleed()
        {
            var first = new OAuthSchemaExtensions(FirstKeys);
            var second = new OAuthSchemaExtensions(SecondKeys);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.ExtraOAuthKeys, Is.EquivalentTo(FirstKeys));
                Assert.That(second.ExtraOAuthKeys, Is.EquivalentTo(SecondKeys));
            }
        }

        [Test]
        public void ParameterlessConstructor_YieldsEmptyExtras()
        {
            var extensions = new OAuthSchemaExtensions();

            Assert.That(extensions.ExtraOAuthKeys, Is.Empty);
        }

        [Test]
        public void Constructor_NullKeys_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new OAuthSchemaExtensions(null!));
        }
    }
}
